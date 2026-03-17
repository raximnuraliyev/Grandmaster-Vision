using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GrandmasterVision.Client.Services;

/// <summary>
/// Service for fetching opening data from Lichess Opening Explorer API.
/// </summary>
public class LichessExplorerService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LichessExplorerService> _logger;

    public LichessExplorerService(HttpClient httpClient, ILogger<LichessExplorerService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get opening explorer data for a position (masters database).
    /// </summary>
    public async Task<OpeningExplorerResponse?> GetMastersDataAsync(string fen, CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedFen = Uri.EscapeDataString(fen);
            var url = $"https://explorer.lichess.ovh/masters?fen={encodedFen}&moves=5";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<OpeningExplorerResponse>(cancellationToken: cancellationToken);
            }

            _logger.LogWarning("Lichess API returned {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching opening explorer data");
            return null;
        }
    }

    /// <summary>
    /// Get opening explorer data from Lichess player database.
    /// </summary>
    public async Task<OpeningExplorerResponse?> GetLichessDataAsync(
        string fen,
        string? ratings = "1600,1800,2000,2200,2500",
        string? speeds = "blitz,rapid,classical",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedFen = Uri.EscapeDataString(fen);
            var url = $"https://explorer.lichess.ovh/lichess?fen={encodedFen}&moves=5&ratings={ratings}&speeds={speeds}";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<OpeningExplorerResponse>(cancellationToken: cancellationToken);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Lichess explorer data");
            return null;
        }
    }
}

/// <summary>
/// Response from Lichess Opening Explorer API
/// </summary>
public class OpeningExplorerResponse
{
    [JsonPropertyName("white")]
    public long White { get; set; }

    [JsonPropertyName("draws")]
    public long Draws { get; set; }

    [JsonPropertyName("black")]
    public long Black { get; set; }

    [JsonPropertyName("moves")]
    public List<ExplorerMove> Moves { get; set; } = new();

    [JsonPropertyName("topGames")]
    public List<ExplorerGame>? TopGames { get; set; }

    [JsonPropertyName("opening")]
    public ExplorerOpening? Opening { get; set; }

    public long TotalGames => White + Draws + Black;
}

/// <summary>
/// A move from the opening explorer
/// </summary>
public class ExplorerMove
{
    [JsonPropertyName("uci")]
    public string Uci { get; set; } = "";

    [JsonPropertyName("san")]
    public string San { get; set; } = "";

    [JsonPropertyName("white")]
    public long White { get; set; }

    [JsonPropertyName("draws")]
    public long Draws { get; set; }

    [JsonPropertyName("black")]
    public long Black { get; set; }

    [JsonPropertyName("averageRating")]
    public int? AverageRating { get; set; }

    public long TotalGames => White + Draws + Black;

    public double WhitePercentage => TotalGames > 0 ? (double)White / TotalGames * 100 : 0;
    public double DrawPercentage => TotalGames > 0 ? (double)Draws / TotalGames * 100 : 0;
    public double BlackPercentage => TotalGames > 0 ? (double)Black / TotalGames * 100 : 0;
}

/// <summary>
/// A top game from the explorer
/// </summary>
public class ExplorerGame
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("white")]
    public ExplorerPlayer? White { get; set; }

    [JsonPropertyName("black")]
    public ExplorerPlayer? Black { get; set; }

    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }
}

/// <summary>
/// Player info from explorer
/// </summary>
public class ExplorerPlayer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("rating")]
    public int? Rating { get; set; }
}

/// <summary>
/// Opening info from explorer
/// </summary>
public class ExplorerOpening
{
    [JsonPropertyName("eco")]
    public string Eco { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}
