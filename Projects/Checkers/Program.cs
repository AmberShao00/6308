/*
Requirement A: Pieces can move in all 8 directions
Requirement B: Make it so that all pieces have movement styles
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum PieceColor
{
    Black,
    White
}

public enum MovementStyle
{
    Normal,
    Knight,
    Bishop,
    Queen
}

public class Piece
{
    public int X { get; set; }
    public int Y { get; set; }
    public PieceColor Color { get; }
    public bool Promoted { get; set; }
    public MovementStyle Style { get; set; }

    public Piece(int x, int y, PieceColor color, MovementStyle style = MovementStyle.Normal)
    {
        X = x;
        Y = y;
        Color = color;
        Style = style;
    }
}

public class Move
{
    public Piece PieceToMove { get; }
    public (int X, int Y) To { get; }
    public Piece? PieceToCapture { get; }

    public Move(Piece pieceToMove, (int X, int Y) to, Piece? pieceToCapture = null)
    {
        PieceToMove = pieceToMove;
        To = to;
        PieceToCapture = pieceToCapture;
    }
}

public class Board
{
    private readonly Piece?[,] squares = new Piece?[8, 8];
    public Piece? Aggressor { get; private set; }

    private static readonly (int X, int Y)[] AllDirections = new[]
    {
        (1, 1), (1, -1), (-1, 1), (-1, -1),  // Diagonal directions
        (1, 0), (-1, 0), (0, 1), (0, -1)     // Straight directions
    };

    private static readonly (int X, int Y)[] KnightMoves = new[]
    {
        (2, 1), (2, -1), (-2, 1), (-2, -1),
        (1, 2), (1, -2), (-1, 2), (-1, -2)
    };

    public IEnumerable<Piece> Pieces =>
        from x in Enumerable.Range(0, 8)
        from y in Enumerable.Range(0, 8)
        where squares[x, y] is not null
        select squares[x, y]!;

    public Piece? this[int x, int y]
    {
        get => squares[x, y];
        set
        {
            if (squares[x, y] is not null)
            {
                var piece = squares[x, y]!;
                squares[piece.X, piece.Y] = null;
            }
            if (value is not null)
            {
                value.X = x;
                value.Y = y;
                squares[x, y] = value;
            }
        }
    }

    public Board()
    {
        // Initialize the board with pieces
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if ((x + y) % 2 == 1)
                {
                    squares[x, y] = new Piece(x, y, PieceColor.White);
                }
            }
            for (int y = 5; y < 8; y++)
            {
                if ((x + y) % 2 == 1)
                {
                    squares[x, y] = new Piece(x, y, PieceColor.Black);
                }
            }
        }
    }

    public List<Move> GetPossibleMoves(PieceColor color)
    {
        var moves = new List<Move>();
        foreach (var piece in Pieces.Where(p => p.Color == color))
        {
            moves.AddRange(GetMovesForPiece(piece));
        }

        var captures = moves.Where(move => move.PieceToCapture is not null).ToList();
        return captures.Any() ? captures : moves;
    }

/*
// The GetMovesForPiece method calculates all the moves of the piece based on the piece type and movement rules.
// The method first determines the movement type (MovementStyle) of the piece, and then calls the corresponding function to calculate the legal movement path according to the different types of pieces.
// Supported piece types include:
// - Normal: Normal pieces, usually only allowed to move forward, and may be allowed to capture pieces diagonally.
// - Knight: Knight, moves according to the 'L' shape rule.
// - Bishop: Bishop, unlimited movement along the diagonal.
// - Rook: Rook, unlimited movement along a straight line (horizontally or vertically).
// - Queen: Queen, combining the movement rules of the rook and bishop, can move freely along both straight lines and diagonals.
// - King: King, moves to an adjacent square around, and can only move one step at a time.
*/
    private List<Move> GetMovesForPiece(Piece piece)
    {
        var moves = new List<Move>();

        switch (piece.Style)
        {
            case MovementStyle.Normal:
                AddNormalMoves(piece, moves);
                break;

            case MovementStyle.Knight:
                foreach (var move in KnightMoves)
                {
                    var newX = piece.X + move.X;
                    var newY = piece.Y + move.Y;
                    if (IsValidPosition(newX, newY) && this[newX, newY]?.Color != piece.Color)
                    {
                        moves.Add(new Move(piece, (newX, newY), this[newX, newY]));
                    }
                }
                break;

            case MovementStyle.Bishop:
                foreach (var dir in AllDirections.Take(4)) // Only diagonal directions
                {
                    AddMovesInDirection(piece, dir.X, dir.Y, moves);
                }
                break;

            case MovementStyle.Queen:
                foreach (var dir in AllDirections)
                {
                    AddMovesInDirection(piece, dir.X, dir.Y, moves);
                }
                break;
        }

        return moves;
    }

    private void AddNormalMoves(Piece piece, List<Move> moves)
    {
        int forward = piece.Color == PieceColor.Black ? -1 : 1;

        // Add normal moves
        foreach (int dx in new[] { -1, 1 })
        {
            int newX = piece.X + dx;
            int newY = piece.Y + forward;

            if (IsValidPosition(newX, newY))
            {
                if (this[newX, newY] is null)
                {
                    moves.Add(new Move(piece, (newX, newY)));
                }
                else if (this[newX, newY]?.Color != piece.Color)
                {
                    int jumpX = newX + dx;
                    int jumpY = newY + forward;
                    if (IsValidPosition(jumpX, jumpY) && this[jumpX, jumpY] is null)
                    {
                        moves.Add(new Move(piece, (jumpX, jumpY), this[newX, newY]));
                    }
                }
            }
        }

        // Add backward moves for promoted pieces
        if (piece.Promoted)
        {
            foreach (int dx in new[] { -1, 1 })
            {
                int newX = piece.X + dx;
                int newY = piece.Y - forward;

                if (IsValidPosition(newX, newY))
                {
                    if (this[newX, newY] is null)
                    {
                        moves.Add(new Move(piece, (newX, newY)));
                    }
                    else if (this[newX, newY]?.Color != piece.Color)
                    {
                        int jumpX = newX + dx;
                        int jumpY = newY - forward;
                        if (IsValidPosition(jumpX, jumpY) && this[jumpX, jumpY] is null)
                        {
                            moves.Add(new Move(piece, (jumpX, jumpY), this[newX, newY]));
                        }
                    }
                }
            }
        }
    }

    private void AddMovesInDirection(Piece piece, int dx, int dy, List<Move> moves)
    {
        int x = piece.X + dx;
        int y = piece.Y + dy;

        while (IsValidPosition(x, y))
        {
            if (this[x, y] == null)
            {
                moves.Add(new Move(piece, (x, y)));
            }
            else if (this[x, y]?.Color != piece.Color)
            {
                moves.Add(new Move(piece, (x, y), this[x, y]));
                break;
            }
            else
            {
                break;
            }
            x += dx;
            y += dy;
        }
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < 8 && y >= 0 && y < 8;
    }

    public void PerformMove(Move move)
    {
        var piece = this[move.PieceToMove.X, move.PieceToMove.Y];
        if (move.PieceToCapture is not null)
        {
            this[move.PieceToCapture.X, move.PieceToCapture.Y] = null;
        }
        this[move.To.X, move.To.Y] = piece;

        // Promote pieces that reach the opposite end
        if (!piece!.Promoted &&
            ((piece.Color == PieceColor.Black && move.To.Y == 0) ||
             (piece.Color == PieceColor.White && move.To.Y == 7)))
        {
            piece.Promoted = true;
        }
    }

    public (Piece, Piece) GetClosestRivalPieces(PieceColor color)
    {
        var myPieces = Pieces.Where(p => p.Color == color);
        var theirPieces = Pieces.Where(p => p.Color != color);

        var closest = (from my in myPieces
                       from their in theirPieces
                       let distance = Math.Abs(my.X - their.X) + Math.Abs(my.Y - their.Y)
                       orderby distance
                       select (my, their)).First();

        return closest;
    }

    public static bool IsTowards((int X, int Y) move, Piece target)
    {
        return Math.Abs(move.X - target.X) + Math.Abs(move.Y - target.Y) <
               Math.Abs(move.X - target.X) + Math.Abs(move.Y - target.Y);
    }
}

public class Player
{
    public PieceColor Color { get; }
    public bool IsHuman { get; }

    public Player(PieceColor color, bool isHuman)
    {
        Color = color;
        IsHuman = isHuman;
    }
}

public class Game
{
    public Board Board { get; }
    public List<Player> Players { get; }
    public PieceColor Turn { get; private set; } = PieceColor.Black;
    public PieceColor? Winner =>
        !Board.GetPossibleMoves(Turn).Any() ? Turn == PieceColor.Black ? PieceColor.White : PieceColor.Black :
        !Board.Pieces.Any(piece => piece.Color == PieceColor.Black) ? PieceColor.White :
        !Board.Pieces.Any(piece => piece.Color == PieceColor.White) ? PieceColor.Black :
        null;

    public Game(int humanPlayerCount)
    {
        Board = new Board();
        Players = new List<Player>
        {
            new Player(PieceColor.Black, humanPlayerCount > 0),
            new Player(PieceColor.White, humanPlayerCount > 1)
        };

        // Randomly assign movement styles to pieces
        foreach (var piece in Board.Pieces)
        {
            piece.Style = (MovementStyle)Random.Shared.Next(Enum.GetValues(typeof(MovementStyle)).Length);
        }
    }

    public void PerformMove(Move move)
    {
        Board.PerformMove(move);
        Turn = Turn == PieceColor.Black ? PieceColor.White : PieceColor.Black;
    }

    public int TakenCount(PieceColor color) =>
        12 - Board.Pieces.Count(piece => piece.Color == color);
}

// Main Program
public class Program
{
    public static void Main()
    {
        Exception? exception = null;
        Encoding encoding = Console.OutputEncoding;

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
            Game game = ShowIntroScreenAndGetOption();
            Console.Clear();
            RunGameLoop(game);
            RenderGameState(game, promptPressKey: true);
            Console.ReadKey(true);
        }
        catch (Exception e)
        {
            exception = e;
            throw;
        }
        finally
        {
            Console.OutputEncoding = encoding;
            Console.CursorVisible = true;
            Console.Clear();
            Console.WriteLine(exception?.ToString() ?? "Checkers was closed.");
        }
    }

    static void RenderGameState(Game game, Player? playerMoved = null, (int X, int Y)? selection = null, (int X, int Y)? from = null, bool promptPressKey = false)
    {
        const char BlackPiece = '○';
        const char BlackKing = '☺';
        const char WhitePiece = '◙';
        const char WhiteKing = '☻';
        const char Vacant = '·';

        Console.CursorVisible = false;
        Console.SetCursorPosition(0, 0);
        StringBuilder sb = new();
        sb.AppendLine();
        sb.AppendLine("  Checkers");
        sb.AppendLine();
        sb.AppendLine($"    ╔═══════════════════╗");

        for (int y = 7; y >= 0; y--)
        {
            sb.Append($"  {y + 1} ║  ");
            for (int x = 0; x < 8; x++)
            {
                var piece = game.Board[x, y];
                if (piece != null)
                {
                    char pieceChar = ToChar(piece);
                    var styleIndicator = piece.Style switch
                    {
                        MovementStyle.Normal => " ",
                        MovementStyle.Knight => "K",
                        MovementStyle.Bishop => "B",
                        MovementStyle.Queen => "Q",
                        _ => " "
                    };
                    sb.Append($"{pieceChar}{styleIndicator}");
                }
                else
                {
                    sb.Append($"{Vacant} ");
                }
            }
            sb.AppendLine("  ║");
        }

        sb.AppendLine($"    ╚═══════════════════╝");
        sb.AppendLine($"       A B C D E F G H");
        sb.AppendLine();

        PieceColor? wc = game.Winner;
        PieceColor? mc = playerMoved?.Color;
        PieceColor? tc = game.Turn;
        string w = $"  *** {wc} wins ***";
        string m = $"  {mc} moved       ";
        string t = $"  {tc}'s turn      ";
        sb.AppendLine(
            game.Winner is not null ? w :
            playerMoved is not null ? m :
            t);

        sb.AppendLine("  Legend: K=Knight, B=Bishop, Q=Queen");
        string p = "  Press any key to continue...";
        string s = "                              ";
        sb.AppendLine(promptPressKey ? p : s);
        Console.Write(sb);

        static char ToChar(Piece piece) =>
            (piece.Color, piece.Promoted) switch
            {
                (PieceColor.Black, false) => BlackPiece,
                (PieceColor.Black, true) => BlackKing,
                (PieceColor.White, false) => WhitePiece,
                (PieceColor.White, true) => WhiteKing,
                _ => throw new NotImplementedException(),
            };
    }

    static void RunGameLoop(Game game)
    {
        while (game.Winner is null)
        {
            Player currentPlayer = game.Players.First(player => player.Color == game.Turn);
            if (currentPlayer.IsHuman)
            {
                while (game.Turn == currentPlayer.Color)
                {
                    (int X, int Y)? selectionStart = null;
                    (int X, int Y)? from = game.Board.Aggressor is not null ? (game.Board.Aggressor.X, game.Board.Aggressor.Y) : null;
                    List<Move> moves = game.Board.GetPossibleMoves(game.Turn);
                    if (moves.Select(move => move.PieceToMove).Distinct().Count() is 1)
                    {
                        Move must = moves.First();
                        from = (must.PieceToMove.X, must.PieceToMove.Y);
                        selectionStart = must.To;
                    }
                    while (from is null)
                    {
                        from = HumanMoveSelection(game);
                        selectionStart = from;
                    }
                    (int X, int Y)? to = HumanMoveSelection(game, selectionStart: selectionStart, from: from);
                    Piece? piece = null;
                    piece = game.Board[from.Value.X, from.Value.Y];
                    if (piece is null || piece.Color != game.Turn)
                    {
                        from = null;
                        to = null;
                    }
                    if (from is not null && to is not null)
                    {
                        // Verify that the move complies with requirements
                        var validMoves = game.Board.GetPossibleMoves(game.Turn);
                        var move = validMoves.FirstOrDefault(m =>
                            m.PieceToMove.X == from.Value.X &&
                            m.PieceToMove.Y == from.Value.Y &&
                            m.To.X == to.Value.X &&
                            m.To.Y == to.Value.Y);

                        if (move is not null)
                        {
                            game.PerformMove(move);
                        }
                    }
                }
            }
            else
            {
                // AI player's turn
                List<Move> moves = game.Board.GetPossibleMoves(game.Turn);
                List<Move> captures = moves.Where(move => move.PieceToCapture is not null).ToList();

                if (captures.Count > 0)
                {
                    // Prioritize captures
                    game.PerformMove(captures[Random.Shared.Next(captures.Count)]);
                }
                else if (!game.Board.Pieces.Any(piece => piece.Color == game.Turn && !piece.Promoted))
                {
                    // If all pieces are promoted, try to move towards opponent pieces
                    var (a, b) = game.Board.GetClosestRivalPieces(game.Turn);
                    Move? priorityMove = moves.FirstOrDefault(move => move.PieceToMove == a && Board.IsTowards(move.To, b));
                    game.PerformMove(priorityMove ?? moves[Random.Shared.Next(moves.Count)]);
                }
                else
                {
                    // Random move if no special conditions
                    game.PerformMove(moves[Random.Shared.Next(moves.Count)]);
                }
            }

            RenderGameState(game, playerMoved: currentPlayer, promptPressKey: true);
            Console.ReadKey(true);
        }
    }

    static (int X, int Y)? HumanMoveSelection(Game game, (int X, int Y)? selectionStart = null, (int X, int Y)? from = null)
    {
        (int X, int Y) selection = selectionStart ?? (3, 3);
        while (true)
        {
            RenderGameState(game, selection: selection, from: from);
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.DownArrow: selection.Y = Math.Max(0, selection.Y - 1); break;
                case ConsoleKey.UpArrow: selection.Y = Math.Min(7, selection.Y + 1); break;
                case ConsoleKey.LeftArrow: selection.X = Math.Max(0, selection.X - 1); break;
                case ConsoleKey.RightArrow: selection.X = Math.Min(7, selection.X + 1); break;
                case ConsoleKey.Enter: return selection;
                case ConsoleKey.Escape: return null;
            }
        }
    }

// ... previous code ...

static Game ShowIntroScreenAndGetOption()
{
    Console.Clear();
    Console.WriteLine();
    Console.WriteLine("  Enhanced Checkers");
    Console.WriteLine();
    Console.WriteLine("  This is an enhanced version of Checkers with special movement styles:");
    Console.WriteLine("  - Normal: Traditional checker movement");
    Console.WriteLine("  - Knight: Moves like a chess knight");
    Console.WriteLine("  - Bishop: Moves diagonally any distance");
    Console.WriteLine("  - Queen: Moves in any direction any distance");
    Console.WriteLine();
    Console.WriteLine("  Each piece is randomly assigned a movement style at the start.");
    Console.WriteLine("  The movement style is shown next to each piece (K/B/Q or space for Normal).");
    Console.WriteLine();
    Console.WriteLine("  Press a number key to choose number of human players:");
    Console.WriteLine("    [0] Black (computer) vs White (computer)");
    Console.WriteLine("    [1] Black (human) vs White (computer)");
    Console.Write("    [2] Black (human) vs White (human)");

    int? humanPlayerCount = null;
    while (humanPlayerCount is null)
    {
        Console.CursorVisible = false;
        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.D0 or ConsoleKey.NumPad0: humanPlayerCount = 0; break;
            case ConsoleKey.D1 or ConsoleKey.NumPad1: humanPlayerCount = 1; break;
            case ConsoleKey.D2 or ConsoleKey.NumPad2: humanPlayerCount = 2; break;
        }
    }
    return new Game(humanPlayerCount.Value);
}
}