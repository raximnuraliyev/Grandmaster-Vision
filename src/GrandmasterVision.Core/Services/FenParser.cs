namespace GrandmasterVision.Core.Services;

/// <summary>
/// FEN (Forsyth-Edwards Notation) parser and utilities.
/// </summary>
public static class FenParser
{
    public const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    /// <summary>
    /// Parse a FEN string into a ChessPosition object.
    /// </summary>
    public static ChessPosition Parse(string fen)
    {
        var parts = fen.Split(' ');
        if (parts.Length < 1)
            throw new ArgumentException("Invalid FEN string", nameof(fen));

        var position = new ChessPosition();

        // Parse board
        var ranks = parts[0].Split('/');
        if (ranks.Length != 8)
            throw new ArgumentException("FEN must have 8 ranks", nameof(fen));

        for (int rank = 0; rank < 8; rank++)
        {
            int file = 0;
            foreach (char c in ranks[rank])
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                }
                else
                {
                    position.Board[rank, file] = CharToPiece(c);
                    file++;
                }
            }
        }

        // Parse active color
        if (parts.Length > 1)
            position.WhiteToMove = parts[1] == "w";

        // Parse castling rights
        if (parts.Length > 2)
        {
            var castling = parts[2];
            position.WhiteKingsideCastle = castling.Contains('K');
            position.WhiteQueensideCastle = castling.Contains('Q');
            position.BlackKingsideCastle = castling.Contains('k');
            position.BlackQueensideCastle = castling.Contains('q');
        }

        // Parse en passant square
        if (parts.Length > 3 && parts[3] != "-")
            position.EnPassantSquare = parts[3];

        // Parse halfmove clock
        if (parts.Length > 4)
            position.HalfmoveClock = int.Parse(parts[4]);

        // Parse fullmove number
        if (parts.Length > 5)
            position.FullmoveNumber = int.Parse(parts[5]);

        return position;
    }

    /// <summary>
    /// Convert a ChessPosition back to FEN string.
    /// </summary>
    public static string ToFen(ChessPosition position)
    {
        var sb = new System.Text.StringBuilder();

        // Board
        for (int rank = 0; rank < 8; rank++)
        {
            int emptyCount = 0;
            for (int file = 0; file < 8; file++)
            {
                var piece = position.Board[rank, file];
                if (piece == Piece.None)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        sb.Append(emptyCount);
                        emptyCount = 0;
                    }
                    sb.Append(PieceToChar(piece));
                }
            }
            if (emptyCount > 0)
                sb.Append(emptyCount);
            if (rank < 7)
                sb.Append('/');
        }

        // Active color
        sb.Append(' ');
        sb.Append(position.WhiteToMove ? 'w' : 'b');

        // Castling rights
        sb.Append(' ');
        var castling = "";
        if (position.WhiteKingsideCastle) castling += "K";
        if (position.WhiteQueensideCastle) castling += "Q";
        if (position.BlackKingsideCastle) castling += "k";
        if (position.BlackQueensideCastle) castling += "q";
        sb.Append(string.IsNullOrEmpty(castling) ? "-" : castling);

        // En passant
        sb.Append(' ');
        sb.Append(string.IsNullOrEmpty(position.EnPassantSquare) ? "-" : position.EnPassantSquare);

        // Halfmove clock and fullmove number
        sb.Append(' ').Append(position.HalfmoveClock);
        sb.Append(' ').Append(position.FullmoveNumber);

        return sb.ToString();
    }

    /// <summary>
    /// Validate a FEN string.
    /// </summary>
    public static (bool isValid, string? error) Validate(string fen)
    {
        if (string.IsNullOrWhiteSpace(fen))
            return (false, "FEN string is empty");

        try
        {
            var parts = fen.Split(' ');

            if (parts.Length < 1)
                return (false, "FEN string is empty");

            // Validate board
            var ranks = parts[0].Split('/');
            if (ranks.Length != 8)
                return (false, "FEN must have 8 ranks");

            int whiteKings = 0, blackKings = 0;

            foreach (var rank in ranks)
            {
                int squares = 0;
                foreach (char c in rank)
                {
                    if (char.IsDigit(c))
                    {
                        squares += c - '0';
                    }
                    else if ("KQRBNPkqrbnp".Contains(c))
                    {
                        squares++;
                        if (c == 'K') whiteKings++;
                        if (c == 'k') blackKings++;
                    }
                    else
                    {
                        return (false, $"Invalid piece character: {c}");
                    }
                }
                if (squares != 8)
                    return (false, $"Rank must have 8 squares, found {squares}");
            }

            if (whiteKings != 1)
                return (false, $"Must have exactly 1 white king, found {whiteKings}");
            if (blackKings != 1)
                return (false, $"Must have exactly 1 black king, found {blackKings}");

            // Validate active color
            if (parts.Length > 1 && parts[1] != "w" && parts[1] != "b")
                return (false, "Active color must be 'w' or 'b'");

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Try to parse a FEN string, returning null on failure.
    /// </summary>
    public static ChessPosition? TryParse(string fen)
    {
        var (isValid, _) = Validate(fen);
        if (!isValid) return null;

        try
        {
            return Parse(fen);
        }
        catch
        {
            return null;
        }
    }

    private static Piece CharToPiece(char c) => c switch
    {
        'K' => Piece.WhiteKing,
        'Q' => Piece.WhiteQueen,
        'R' => Piece.WhiteRook,
        'B' => Piece.WhiteBishop,
        'N' => Piece.WhiteKnight,
        'P' => Piece.WhitePawn,
        'k' => Piece.BlackKing,
        'q' => Piece.BlackQueen,
        'r' => Piece.BlackRook,
        'b' => Piece.BlackBishop,
        'n' => Piece.BlackKnight,
        'p' => Piece.BlackPawn,
        _ => Piece.None
    };

    private static char PieceToChar(Piece piece) => piece switch
    {
        Piece.WhiteKing => 'K',
        Piece.WhiteQueen => 'Q',
        Piece.WhiteRook => 'R',
        Piece.WhiteBishop => 'B',
        Piece.WhiteKnight => 'N',
        Piece.WhitePawn => 'P',
        Piece.BlackKing => 'k',
        Piece.BlackQueen => 'q',
        Piece.BlackRook => 'r',
        Piece.BlackBishop => 'b',
        Piece.BlackKnight => 'n',
        Piece.BlackPawn => 'p',
        _ => ' '
    };
}

public class ChessPosition
{
    public Piece[,] Board { get; } = new Piece[8, 8];
    public bool WhiteToMove { get; set; } = true;
    public bool WhiteKingsideCastle { get; set; } = true;
    public bool WhiteQueensideCastle { get; set; } = true;
    public bool BlackKingsideCastle { get; set; } = true;
    public bool BlackQueensideCastle { get; set; } = true;
    public string? EnPassantSquare { get; set; }
    public int HalfmoveClock { get; set; }
    public int FullmoveNumber { get; set; } = 1;

    /// <summary>
    /// Get piece at algebraic notation (e.g., "e4").
    /// </summary>
    public Piece GetPieceAt(string square)
    {
        var (rank, file) = AlgebraicToIndex(square);
        return Board[rank, file];
    }

    /// <summary>
    /// Set piece at algebraic notation.
    /// </summary>
    public void SetPieceAt(string square, Piece piece)
    {
        var (rank, file) = AlgebraicToIndex(square);
        Board[rank, file] = piece;
    }

    private static (int rank, int file) AlgebraicToIndex(string square)
    {
        if (square.Length != 2)
            throw new ArgumentException("Square must be 2 characters", nameof(square));

        int file = square[0] - 'a';
        int rank = 8 - (square[1] - '0');

        return (rank, file);
    }

    /// <summary>
    /// Get ASCII representation of the board.
    /// </summary>
    public string ToAscii()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("  a b c d e f g h");
        for (int rank = 0; rank < 8; rank++)
        {
            sb.Append(8 - rank).Append(' ');
            for (int file = 0; file < 8; file++)
            {
                var piece = Board[rank, file];
                char c = piece switch
                {
                    Piece.WhiteKing => 'K',
                    Piece.WhiteQueen => 'Q',
                    Piece.WhiteRook => 'R',
                    Piece.WhiteBishop => 'B',
                    Piece.WhiteKnight => 'N',
                    Piece.WhitePawn => 'P',
                    Piece.BlackKing => 'k',
                    Piece.BlackQueen => 'q',
                    Piece.BlackRook => 'r',
                    Piece.BlackBishop => 'b',
                    Piece.BlackKnight => 'n',
                    Piece.BlackPawn => 'p',
                    _ => '.'
                };
                sb.Append(c).Append(' ');
            }
            sb.AppendLine((8 - rank).ToString());
        }
        sb.AppendLine("  a b c d e f g h");
        return sb.ToString();
    }
}

public enum Piece
{
    None,
    WhiteKing, WhiteQueen, WhiteRook, WhiteBishop, WhiteKnight, WhitePawn,
    BlackKing, BlackQueen, BlackRook, BlackBishop, BlackKnight, BlackPawn
}
