using GrandmasterVision.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrandmasterVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly StockfishService _stockfish;
    private readonly PgnAnalyzerService _pgnAnalyzer;
    private readonly OpeningService _openingService;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(
        StockfishService stockfish,
        PgnAnalyzerService pgnAnalyzer,
        OpeningService openingService,
        ILogger<AnalysisController> logger)
    {
        _stockfish = stockfish;
        _pgnAnalyzer = pgnAnalyzer;
        _openingService = openingService;
        _logger = logger;
    }

    /// <summary>
    /// Get the best move for a given FEN position.
    /// </summary>
    [HttpPost("best-move")]
    public async Task<ActionResult<BestMoveResponse>> GetBestMove(
        [FromBody] BestMoveRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate FEN
            var (isValid, error) = FenParser.Validate(request.Fen);
            if (!isValid)
                return BadRequest(new { error });

            var result = await _stockfish.GetBestMoveAsync(
                request.Fen,
                request.Depth ?? 20,
                cancellationToken);

            return Ok(new BestMoveResponse
            {
                BestMove = result.BestMove,
                Evaluation = result.Evaluation,
                MateIn = result.MateIn,
                PrincipalVariation = result.PrincipalVariation,
                Depth = result.CurrentDepth
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting best move for FEN: {Fen}", request.Fen);
            return StatusCode(500, new { error = "Analysis failed" });
        }
    }

    /// <summary>
    /// Get top N moves for a position.
    /// </summary>
    [HttpPost("top-moves")]
    public async Task<ActionResult<List<MoveAnalysis>>> GetTopMoves(
        [FromBody] TopMovesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (isValid, error) = FenParser.Validate(request.Fen);
            if (!isValid)
                return BadRequest(new { error });

            var result = await _stockfish.GetTopMovesAsync(
                request.Fen,
                request.NumMoves ?? 3,
                request.Depth ?? 20,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top moves for FEN: {Fen}", request.Fen);
            return StatusCode(500, new { error = "Analysis failed" });
        }
    }

    /// <summary>
    /// Evaluate a specific move.
    /// </summary>
    [HttpPost("evaluate-move")]
    public async Task<ActionResult<MoveEvaluation>> EvaluateMove(
        [FromBody] EvaluateMoveRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _stockfish.EvaluateMoveAsync(
                request.FenBefore,
                request.Move,
                request.FenAfter,
                request.Depth ?? 20,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating move: {Move}", request.Move);
            return StatusCode(500, new { error = "Evaluation failed" });
        }
    }

    /// <summary>
    /// Analyze a full game from PGN.
    /// </summary>
    [HttpPost("analyze-pgn")]
    public async Task<ActionResult<GameAnalysis>> AnalyzePgn(
        [FromBody] PgnAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var game = _pgnAnalyzer.ParsePgn(request.Pgn);
            var analysis = await _pgnAnalyzer.AnalyzeGameAsync(
                game,
                request.Depth ?? 18,
                cancellationToken);

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing PGN");
            return StatusCode(500, new { error = "PGN analysis failed" });
        }
    }

    /// <summary>
    /// Identify opening from FEN.
    /// </summary>
    [HttpPost("identify-opening")]
    public async Task<ActionResult<OpeningInfo?>> IdentifyOpening(
        [FromBody] OpeningRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var opening = await _openingService.IdentifyOpeningAsync(request.Fen, cancellationToken);
            return Ok(opening); // Return null if not found, not 404
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying opening");
            return Ok(null); // Don't fail on opening lookup
        }
    }

    /// <summary>
    /// Validate a FEN string.
    /// </summary>
    [HttpPost("validate-fen")]
    public ActionResult<FenValidationResult> ValidateFen([FromBody] ValidateFenRequest request)
    {
        var (isValid, error) = FenParser.Validate(request.Fen);
        return Ok(new FenValidationResult
        {
            IsValid = isValid,
            Error = error
        });
    }
}

// Request/Response DTOs
public record BestMoveRequest(string Fen, int? Depth = 20);
public record TopMovesRequest(string Fen, int? NumMoves = 3, int? Depth = 20);
public record EvaluateMoveRequest(string FenBefore, string Move, string FenAfter, int? Depth = 20);
public record PgnAnalysisRequest(string Pgn, int? Depth = 18);
public record OpeningRequest(string Fen);
public record ValidateFenRequest(string Fen);

public record BestMoveResponse
{
    public string? BestMove { get; init; }
    public double? Evaluation { get; init; }
    public int? MateIn { get; init; }
    public string? PrincipalVariation { get; init; }
    public int Depth { get; init; }
}

public record FenValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
}
