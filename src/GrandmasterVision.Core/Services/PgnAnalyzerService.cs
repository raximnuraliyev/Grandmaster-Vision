namespace GrandmasterVision.Core.Services;

/// <summary>
/// Service for parsing and analyzing PGN (Portable Game Notation) files.
/// </summary>
public class PgnAnalyzerService
{
    private readonly StockfishService _stockfish;

    public PgnAnalyzerService(StockfishService stockfish)
    {
        _stockfish = stockfish;
    }

    /// <summary>
    /// Parse a PGN string into a Game object.
    /// </summary>
    public Game ParsePgn(string pgn)
    {
        var game = new Game();
        var lines = pgn.Split('\n');

        var moveText = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Parse headers
            if (trimmed.StartsWith("["))
            {
                var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"\[(\w+)\s+""([^""]*)""\]");
                if (match.Success)
                {
                    var key = match.Groups[1].Value;
                    var value = match.Groups[2].Value;

                    switch (key)
                    {
                        case "Event": game.Event = value; break;
                        case "Site": game.Site = value; break;
                        case "Date": game.Date = value; break;
                        case "White": game.White = value; break;
                        case "Black": game.Black = value; break;
                        case "Result": game.Result = value; break;
                        case "WhiteElo": game.WhiteElo = int.TryParse(value, out var we) ? we : null; break;
                        case "BlackElo": game.BlackElo = int.TryParse(value, out var be) ? be : null; break;
                        case "ECO": game.ECO = value; break;
                        case "Opening": game.Opening = value; break;
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                moveText.Append(trimmed).Append(' ');
            }
        }

        // Parse moves
        game.Moves = ParseMoves(moveText.ToString());

        return game;
    }

    /// <summary>
    /// Parse move text into individual moves.
    /// </summary>
    private List<GameMove> ParseMoves(string moveText)
    {
        var moves = new List<GameMove>();

        // Remove comments and variations
        moveText = System.Text.RegularExpressions.Regex.Replace(moveText, @"\{[^}]*\}", "");
        moveText = System.Text.RegularExpressions.Regex.Replace(moveText, @"\([^)]*\)", "");

        // Remove result
        moveText = System.Text.RegularExpressions.Regex.Replace(moveText, @"1-0|0-1|1/2-1/2|\*", "");

        // Parse individual moves
        var tokens = moveText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int moveNumber = 1;
        bool isWhiteMove = true;

        foreach (var token in tokens)
        {
            // Skip move numbers
            if (System.Text.RegularExpressions.Regex.IsMatch(token, @"^\d+\.+$"))
            {
                var numMatch = System.Text.RegularExpressions.Regex.Match(token, @"^(\d+)");
                if (numMatch.Success)
                {
                    moveNumber = int.Parse(numMatch.Groups[1].Value);
                    isWhiteMove = !token.Contains("...");
                }
                continue;
            }

            // Skip NAGs (Numeric Annotation Glyphs)
            if (token.StartsWith("$"))
                continue;

            // Valid move
            if (System.Text.RegularExpressions.Regex.IsMatch(token, @"^[KQRBNP]?[a-h]?[1-8]?x?[a-h][1-8](=[QRBN])?[+#]?$|^O-O(-O)?[+#]?$"))
            {
                moves.Add(new GameMove
                {
                    MoveNumber = moveNumber,
                    IsWhite = isWhiteMove,
                    San = token
                });

                if (!isWhiteMove)
                    moveNumber++;

                isWhiteMove = !isWhiteMove;
            }
        }

        return moves;
    }

    /// <summary>
    /// Analyze all moves in a game and identify mistakes/blunders.
    /// </summary>
    public async Task<GameAnalysis> AnalyzeGameAsync(
        Game game,
        int depth = 20,
        CancellationToken cancellationToken = default)
    {
        var analysis = new GameAnalysis
        {
            Game = game,
            MoveEvaluations = new List<AnalyzedMove>()
        };

        // Start from initial position
        var position = FenParser.Parse(FenParser.StartingPosition);
        var currentFen = FenParser.StartingPosition;

        foreach (var move in game.Moves)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get engine evaluation before move
            var beforeAnalysis = await _stockfish.GetTopMovesAsync(currentFen, 3, depth, cancellationToken);

            // Apply move and get new FEN
            // Note: In real implementation, use a proper chess library to apply moves
            var nextFen = ApplyMove(currentFen, move.San);

            // Get evaluation after move
            var afterAnalysis = await _stockfish.GetBestMoveAsync(nextFen, depth, cancellationToken);

            // Calculate centipawn loss
            var topMove = beforeAnalysis.FirstOrDefault();
            var evalBefore = topMove?.Evaluation ?? 0;
            var evalAfter = -(afterAnalysis.Evaluation ?? 0); // Negate opponent's perspective

            var cpLoss = (int)((evalBefore - evalAfter) * 100);
            if (!move.IsWhite) cpLoss = -cpLoss; // Adjust for black

            var category = CategorizeMove(Math.Abs(cpLoss), move.San, topMove?.Move);

            var analyzedMove = new AnalyzedMove
            {
                Move = move,
                FenBefore = currentFen,
                FenAfter = nextFen,
                EvaluationBefore = evalBefore,
                EvaluationAfter = evalAfter,
                CentipawnLoss = Math.Abs(cpLoss),
                Category = category,
                BestMove = topMove?.Move,
                TopMoves = beforeAnalysis,
                Explanation = GenerateExplanation(move, category, topMove)
            };

            analysis.MoveEvaluations.Add(analyzedMove);

            // Update totals
            if (move.IsWhite)
            {
                analysis.WhiteAverageCentipawnLoss += Math.Abs(cpLoss);
                if (category == MoveCategory.Mistake) analysis.WhiteMistakes++;
                if (category == MoveCategory.Blunder) analysis.WhiteBlunders++;
            }
            else
            {
                analysis.BlackAverageCentipawnLoss += Math.Abs(cpLoss);
                if (category == MoveCategory.Mistake) analysis.BlackMistakes++;
                if (category == MoveCategory.Blunder) analysis.BlackBlunders++;
            }

            currentFen = nextFen;
        }

        // Calculate averages
        var whiteMoves = game.Moves.Count(m => m.IsWhite);
        var blackMoves = game.Moves.Count(m => !m.IsWhite);

        if (whiteMoves > 0)
            analysis.WhiteAverageCentipawnLoss /= whiteMoves;
        if (blackMoves > 0)
            analysis.BlackAverageCentipawnLoss /= blackMoves;

        return analysis;
    }

    private MoveCategory CategorizeMove(int cpLoss, string playedMove, string? bestMove)
    {
        // If played move was the best move
        if (playedMove == bestMove)
            return MoveCategory.Best;

        return cpLoss switch
        {
            < 10 => MoveCategory.Excellent,
            < 25 => MoveCategory.Good,
            < 50 => MoveCategory.Inaccuracy,
            < 100 => MoveCategory.Mistake,
            _ => MoveCategory.Blunder
        };
    }

    private string GenerateExplanation(GameMove move, MoveCategory category, MoveAnalysis? bestMove)
    {
        return category switch
        {
            MoveCategory.Best => $"{move.San} was the best move!",
            MoveCategory.Excellent => $"{move.San} was an excellent move.",
            MoveCategory.Good => $"{move.San} was a good move.",
            MoveCategory.Inaccuracy => $"{move.San} was an inaccuracy. {bestMove?.Move} was better.",
            MoveCategory.Mistake => $"{move.San} was a mistake! {bestMove?.Move} was significantly better.",
            MoveCategory.Blunder => $"{move.San} was a blunder! {bestMove?.Move} should have been played.",
            _ => ""
        };
    }

    /// <summary>
    /// Simplified move application - in real implementation, use chess library.
    /// </summary>
    private string ApplyMove(string fen, string san)
    {
        // This is a placeholder - real implementation would use a chess library
        // to properly apply moves and generate new FEN
        return fen;
    }
}

public class Game
{
    public string? Event { get; set; }
    public string? Site { get; set; }
    public string? Date { get; set; }
    public string? White { get; set; }
    public string? Black { get; set; }
    public string? Result { get; set; }
    public int? WhiteElo { get; set; }
    public int? BlackElo { get; set; }
    public string? ECO { get; set; }
    public string? Opening { get; set; }
    public List<GameMove> Moves { get; set; } = new();
}

public class GameMove
{
    public int MoveNumber { get; set; }
    public bool IsWhite { get; set; }
    public string San { get; set; } = "";
}

public class GameAnalysis
{
    public Game? Game { get; set; }
    public List<AnalyzedMove> MoveEvaluations { get; set; } = new();
    public double WhiteAverageCentipawnLoss { get; set; }
    public double BlackAverageCentipawnLoss { get; set; }
    public int WhiteMistakes { get; set; }
    public int WhiteBlunders { get; set; }
    public int BlackMistakes { get; set; }
    public int BlackBlunders { get; set; }
}

public class AnalyzedMove
{
    public GameMove? Move { get; set; }
    public string? FenBefore { get; set; }
    public string? FenAfter { get; set; }
    public double EvaluationBefore { get; set; }
    public double EvaluationAfter { get; set; }
    public int CentipawnLoss { get; set; }
    public MoveCategory Category { get; set; }
    public string? BestMove { get; set; }
    public List<MoveAnalysis>? TopMoves { get; set; }
    public string? Explanation { get; set; }
}
