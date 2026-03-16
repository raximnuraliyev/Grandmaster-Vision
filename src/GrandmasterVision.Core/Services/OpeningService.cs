namespace GrandmasterVision.Core.Services;

/// <summary>
/// Service for identifying chess openings using ECO codes.
/// </summary>
public class OpeningService
{
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, OpeningInfo> _ecoDatabase = new();

    public OpeningService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        InitializeCommonOpenings();
    }

    /// <summary>
    /// Identify opening from a FEN position using Lichess API.
    /// </summary>
    public async Task<OpeningInfo?> IdentifyOpeningAsync(string fen, CancellationToken cancellationToken = default)
    {
        try
        {
            // Query Lichess opening explorer
            var url = $"https://explorer.lichess.ovh/masters?fen={Uri.EscapeDataString(fen)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                // Parse response - simplified for demo
                // Real implementation would deserialize JSON
                return new OpeningInfo
                {
                    Name = "Unknown Opening",
                    Eco = "A00"
                };
            }
        }
        catch
        {
            // Fall back to local database
        }

        // Check local ECO database
        return _ecoDatabase.GetValueOrDefault(GetPositionKey(fen));
    }

    /// <summary>
    /// Identify opening from move sequence.
    /// </summary>
    public OpeningInfo? IdentifyFromMoves(IEnumerable<string> moves)
    {
        var moveStr = string.Join(" ", moves);

        // Check common openings
        foreach (var (key, opening) in _ecoDatabase)
        {
            if (moveStr.StartsWith(opening.MainLine ?? ""))
                return opening;
        }

        return null;
    }

    private string GetPositionKey(string fen)
    {
        // Use only position part of FEN as key
        var parts = fen.Split(' ');
        return parts[0];
    }

    private void InitializeCommonOpenings()
    {
        // Starting position
        _ecoDatabase["rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "Starting Position",
            Eco = "A00",
            MainLine = ""
        };

        // Common opening positions
        _ecoDatabase["rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "King's Pawn Game",
            Eco = "B00",
            MainLine = "1. e4"
        };

        _ecoDatabase["rnbqkbnr/pppppppp/8/8/3P4/8/PPP1PPPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "Queen's Pawn Game",
            Eco = "A40",
            MainLine = "1. d4"
        };

        _ecoDatabase["rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "King's Pawn Game: Open Game",
            Eco = "C20",
            MainLine = "1. e4 e5"
        };

        _ecoDatabase["rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "Sicilian Defense",
            Eco = "B20",
            MainLine = "1. e4 c5"
        };

        _ecoDatabase["rnbqkbnr/pppp1ppp/4p3/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "French Defense",
            Eco = "C00",
            MainLine = "1. e4 e6"
        };

        _ecoDatabase["rnbqkbnr/ppp1pppp/3p4/8/4P3/8/PPPP1PPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "Pirc Defense",
            Eco = "B07",
            MainLine = "1. e4 d6"
        };

        _ecoDatabase["rnbqkbnr/pppp1ppp/4p3/8/3P4/8/PPP1PPPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "Indian Defense",
            Eco = "A45",
            MainLine = "1. d4 Nf6"
        };

        _ecoDatabase["rnbqkbnr/ppp1pppp/8/3p4/3P4/8/PPP1PPPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "Queen's Gambit",
            Eco = "D00",
            MainLine = "1. d4 d5"
        };

        _ecoDatabase["rnbqkb1r/pppppp1p/5np1/8/2PP4/8/PP2PPPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "King's Indian Defense",
            Eco = "E60",
            MainLine = "1. d4 Nf6 2. c4 g6"
        };

        _ecoDatabase["rnbqkb1r/pppp1ppp/4pn2/8/2PP4/8/PP2PPPP/RNBQKBNR"] = new OpeningInfo
        {
            Name = "Nimzo-Indian Defense",
            Eco = "E20",
            MainLine = "1. d4 Nf6 2. c4 e6"
        };

        _ecoDatabase["r1bqkbnr/pppp1ppp/2n5/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R"] = new OpeningInfo
        {
            Name = "Italian Game",
            Eco = "C50",
            MainLine = "1. e4 e5 2. Nf3 Nc6"
        };

        _ecoDatabase["r1bqkbnr/pppp1ppp/2n5/1B2p3/4P3/5N2/PPPP1PPP/RNBQK2R"] = new OpeningInfo
        {
            Name = "Ruy Lopez",
            Eco = "C60",
            MainLine = "1. e4 e5 2. Nf3 Nc6 3. Bb5"
        };

        // Sicilian Najdorf
        _ecoDatabase["rnbqkb1r/1p2pppp/p2p1n2/8/3NP3/2N5/PPP2PPP/R1BQKB1R"] = new OpeningInfo
        {
            Name = "Sicilian Defense: Najdorf Variation",
            Eco = "B90",
            MainLine = "1. e4 c5 2. Nf3 d6 3. d4 cxd4 4. Nxd4 Nf6 5. Nc3 a6"
        };
    }
}

public class OpeningInfo
{
    public string Name { get; set; } = "";
    public string Eco { get; set; } = "";
    public string? MainLine { get; set; }
    public string? Description { get; set; }
}
