using System.Net.Http.Json;
using System.Text.Json;
using GrandmasterVision.Core.Services;

namespace GrandmasterVision.Client.Services;

/// <summary>
/// Service for communicating with the Chess API backend.
/// </summary>
public class ChessApiService
{
    private readonly HttpClient _httpClient;

    public ChessApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get the best move for a position.
    /// </summary>
    public async Task<BestMoveResponse?> GetBestMoveAsync(string fen, int depth = 20)
    {
        var response = await _httpClient.PostAsJsonAsync("api/analysis/best-move", new
        {
            Fen = fen,
            Depth = depth
        });

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<BestMoveResponse>();
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"API error ({response.StatusCode}): {GetErrorMessage(errorContent)}");
    }

    /// <summary>
    /// Get top moves for analysis.
    /// </summary>
    public async Task<List<MoveAnalysis>?> GetTopMovesAsync(string fen, int numMoves = 3, int depth = 20)
    {
        var response = await _httpClient.PostAsJsonAsync("api/analysis/top-moves", new
        {
            Fen = fen,
            NumMoves = numMoves,
            Depth = depth
        });

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<MoveAnalysis>>();
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"API error ({response.StatusCode}): {GetErrorMessage(errorContent)}");
    }

    /// <summary>
    /// Analyze a full PGN game.
    /// </summary>
    public async Task<GameAnalysis?> AnalyzePgnAsync(string pgn, int depth = 18)
    {
        var response = await _httpClient.PostAsJsonAsync("api/analysis/analyze-pgn", new
        {
            Pgn = pgn,
            Depth = depth
        });

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<GameAnalysis>();
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"API error ({response.StatusCode}): {GetErrorMessage(errorContent)}");
    }

    /// <summary>
    /// Upload an image for board recognition.
    /// </summary>
    public async Task<RecognitionResult?> RecognizeBoardAsync(Stream imageStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync("api/vision/recognize", content);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<RecognitionResult>();
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Vision API error ({response.StatusCode}): {GetErrorMessage(errorContent)}");
    }

    /// <summary>
    /// Identify the opening from a FEN position.
    /// </summary>
    public async Task<OpeningInfo?> IdentifyOpeningAsync(string fen)
    {
        var response = await _httpClient.PostAsJsonAsync("api/analysis/identify-opening", new
        {
            Fen = fen
        });

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<OpeningInfo>();
        }

        return null;
    }

    /// <summary>
    /// Validate a FEN string.
    /// </summary>
    public async Task<FenValidationResult?> ValidateFenAsync(string fen)
    {
        var response = await _httpClient.PostAsJsonAsync("api/analysis/validate-fen", new
        {
            Fen = fen
        });

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<FenValidationResult>();
        }

        return null;
    }

    private static string GetErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                return error.GetString() ?? json;
            }
        }
        catch { }
        return json;
    }
}

public record BestMoveResponse
{
    public string? BestMove { get; init; }
    public double? Evaluation { get; init; }
    public int? MateIn { get; init; }
    public string? PrincipalVariation { get; init; }
    public int Depth { get; init; }
}

public record RecognitionResult
{
    public string? Fen { get; init; }
    public bool Valid { get; init; }
    public ImageSize? ImageSize { get; init; }
}

public record ImageSize
{
    public int Width { get; init; }
    public int Height { get; init; }
}

public record FenValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
}
