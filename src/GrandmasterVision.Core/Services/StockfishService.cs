using System.Diagnostics;
using System.Text;

namespace GrandmasterVision.Core.Services;

/// <summary>
/// Service for communicating with Stockfish chess engine via UCI protocol.
/// </summary>
public class StockfishService : IDisposable
{
    private Process? _stockfishProcess;
    private readonly string _enginePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isReady;

    public StockfishService(string enginePath)
    {
        _enginePath = enginePath;
    }

    /// <summary>
    /// Initialize the Stockfish engine process.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_stockfishProcess != null && !_stockfishProcess.HasExited)
            return;

        _stockfishProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _enginePath,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        _stockfishProcess.Start();

        // Send UCI command and wait for uciok
        await SendCommandAsync("uci", cancellationToken);
        var response = await WaitForResponseAsync("uciok", cancellationToken);

        // Set options for optimal performance
        await SendCommandAsync("setoption name Threads value 4", cancellationToken);
        await SendCommandAsync("setoption name Hash value 256", cancellationToken);

        // Wait for ready
        await SendCommandAsync("isready", cancellationToken);
        await WaitForResponseAsync("readyok", cancellationToken);

        _isReady = true;
    }

    /// <summary>
    /// Get the best move for a given position.
    /// </summary>
    public async Task<AnalysisResult> GetBestMoveAsync(
        string fen,
        int depth = 20,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_isReady)
                await InitializeAsync(cancellationToken);

            // Set position
            await SendCommandAsync($"position fen {fen}", cancellationToken);

            // Start analysis
            await SendCommandAsync($"go depth {depth}", cancellationToken);

            // Collect analysis info
            var result = new AnalysisResult { Fen = fen, Depth = depth };
            var lines = new List<string>();

            while (true)
            {
                var line = await ReadLineAsync(cancellationToken);
                if (line == null) break;

                lines.Add(line);

                if (line.StartsWith("bestmove"))
                {
                    var parts = line.Split(' ');
                    result.BestMove = parts.Length > 1 ? parts[1] : null;
                    result.PonderMove = parts.Length > 3 && parts[2] == "ponder" ? parts[3] : null;
                    break;
                }

                // Parse info lines for evaluation
                if (line.StartsWith("info") && line.Contains("score"))
                {
                    ParseInfoLine(line, result);
                }
            }

            result.RawOutput = lines;
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Get top N moves for analysis.
    /// </summary>
    public async Task<List<MoveAnalysis>> GetTopMovesAsync(
        string fen,
        int numMoves = 3,
        int depth = 20,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_isReady)
                await InitializeAsync(cancellationToken);

            // Set MultiPV to get multiple lines
            await SendCommandAsync($"setoption name MultiPV value {numMoves}", cancellationToken);
            await SendCommandAsync("isready", cancellationToken);
            await WaitForResponseAsync("readyok", cancellationToken);

            // Set position and analyze
            await SendCommandAsync($"position fen {fen}", cancellationToken);
            await SendCommandAsync($"go depth {depth}", cancellationToken);

            var moves = new Dictionary<int, MoveAnalysis>();

            while (true)
            {
                var line = await ReadLineAsync(cancellationToken);
                if (line == null) break;

                if (line.StartsWith("bestmove"))
                    break;

                if (line.StartsWith("info") && line.Contains("multipv"))
                {
                    var analysis = ParseMultiPvLine(line);
                    if (analysis != null)
                    {
                        moves[analysis.Rank] = analysis;
                    }
                }
            }

            // Reset MultiPV
            await SendCommandAsync("setoption name MultiPV value 1", cancellationToken);

            return moves.Values.OrderBy(m => m.Rank).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Analyze a move to determine if it's a mistake/blunder.
    /// </summary>
    public async Task<MoveEvaluation> EvaluateMoveAsync(
        string fenBefore,
        string move,
        string fenAfter,
        int depth = 20,
        CancellationToken cancellationToken = default)
    {
        var beforeAnalysis = await GetBestMoveAsync(fenBefore, depth, cancellationToken);
        var afterAnalysis = await GetBestMoveAsync(fenAfter, depth, cancellationToken);

        var centipawnLoss = CalculateCentipawnLoss(beforeAnalysis, afterAnalysis, move);

        return new MoveEvaluation
        {
            Move = move,
            CentipawnLoss = centipawnLoss,
            BestMove = beforeAnalysis.BestMove,
            Category = CategorizeMove(centipawnLoss),
            EvaluationBefore = beforeAnalysis.Evaluation,
            EvaluationAfter = afterAnalysis.Evaluation
        };
    }

    private void ParseInfoLine(string line, AnalysisResult result)
    {
        var parts = line.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            switch (parts[i])
            {
                case "depth" when i + 1 < parts.Length:
                    result.CurrentDepth = int.Parse(parts[i + 1]);
                    break;
                case "score":
                    if (i + 2 < parts.Length)
                    {
                        if (parts[i + 1] == "cp")
                            result.Evaluation = int.Parse(parts[i + 2]) / 100.0;
                        else if (parts[i + 1] == "mate")
                            result.MateIn = int.Parse(parts[i + 2]);
                    }
                    break;
                case "nodes" when i + 1 < parts.Length:
                    result.NodesSearched = long.Parse(parts[i + 1]);
                    break;
                case "pv" when i + 1 < parts.Length:
                    result.PrincipalVariation = string.Join(" ", parts.Skip(i + 1));
                    break;
            }
        }
    }

    private MoveAnalysis? ParseMultiPvLine(string line)
    {
        var parts = line.Split(' ');
        var analysis = new MoveAnalysis();

        for (int i = 0; i < parts.Length; i++)
        {
            switch (parts[i])
            {
                case "multipv" when i + 1 < parts.Length:
                    analysis.Rank = int.Parse(parts[i + 1]);
                    break;
                case "score":
                    if (i + 2 < parts.Length)
                    {
                        if (parts[i + 1] == "cp")
                            analysis.Evaluation = int.Parse(parts[i + 2]) / 100.0;
                        else if (parts[i + 1] == "mate")
                            analysis.MateIn = int.Parse(parts[i + 2]);
                    }
                    break;
                case "pv" when i + 1 < parts.Length:
                    analysis.Move = parts[i + 1];
                    analysis.Line = string.Join(" ", parts.Skip(i + 1));
                    break;
            }
        }

        return analysis.Move != null ? analysis : null;
    }

    private int CalculateCentipawnLoss(AnalysisResult before, AnalysisResult after, string playedMove)
    {
        // If played move was the best move, no loss
        if (before.BestMove == playedMove)
            return 0;

        // Calculate the evaluation swing
        var evalBefore = before.Evaluation ?? 0;
        var evalAfter = after.Evaluation ?? 0;

        // Negate after eval since it's from opponent's perspective
        var loss = (int)((evalBefore - (-evalAfter)) * 100);
        return Math.Max(0, loss);
    }

    private MoveCategory CategorizeMove(int centipawnLoss)
    {
        return centipawnLoss switch
        {
            0 => MoveCategory.Best,
            < 10 => MoveCategory.Excellent,
            < 25 => MoveCategory.Good,
            < 50 => MoveCategory.Inaccuracy,
            < 100 => MoveCategory.Mistake,
            < 300 => MoveCategory.Blunder,
            _ => MoveCategory.Blunder
        };
    }

    private async Task SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        if (_stockfishProcess?.StandardInput == null)
            throw new InvalidOperationException("Stockfish process not initialized");

        await _stockfishProcess.StandardInput.WriteLineAsync(command.AsMemory(), cancellationToken);
        await _stockfishProcess.StandardInput.FlushAsync(cancellationToken);
    }

    private async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        if (_stockfishProcess?.StandardOutput == null)
            return null;

        return await _stockfishProcess.StandardOutput.ReadLineAsync(cancellationToken);
    }

    private async Task<string> WaitForResponseAsync(string expected, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var line = await ReadLineAsync(cancellationToken);
            if (line == null) break;
            sb.AppendLine(line);
            if (line.StartsWith(expected))
                break;
        }
        return sb.ToString();
    }

    public void Dispose()
    {
        if (_stockfishProcess != null && !_stockfishProcess.HasExited)
        {
            _stockfishProcess.StandardInput?.WriteLine("quit");
            _stockfishProcess.WaitForExit(1000);
            _stockfishProcess.Kill();
            _stockfishProcess.Dispose();
        }
        _lock.Dispose();
    }
}

public class AnalysisResult
{
    public string? Fen { get; set; }
    public int Depth { get; set; }
    public int CurrentDepth { get; set; }
    public string? BestMove { get; set; }
    public string? PonderMove { get; set; }
    public double? Evaluation { get; set; }
    public int? MateIn { get; set; }
    public long NodesSearched { get; set; }
    public string? PrincipalVariation { get; set; }
    public List<string>? RawOutput { get; set; }
}

public class MoveAnalysis
{
    public int Rank { get; set; }
    public string? Move { get; set; }
    public double? Evaluation { get; set; }
    public int? MateIn { get; set; }
    public string? Line { get; set; }
}

public class MoveEvaluation
{
    public string? Move { get; set; }
    public int CentipawnLoss { get; set; }
    public string? BestMove { get; set; }
    public MoveCategory Category { get; set; }
    public double? EvaluationBefore { get; set; }
    public double? EvaluationAfter { get; set; }
}

public enum MoveCategory
{
    Best,
    Excellent,
    Good,
    Inaccuracy,
    Mistake,
    Blunder
}
