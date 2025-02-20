using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Piece color enumeration
enum PieceColor { Black, White }

// Piece class
class Piece
{
    public int X { get; set; } // Piece's X coordinate
    public int Y { get; set; } // Piece's Y coordinate
    public PieceColor Color { get; set; } // Piece color
    public bool Promoted { get; set; } // Whether the piece is promoted to a king

    public Piece(int x, int y, PieceColor color, bool promoted = false)
    {
        X = x;
        Y = y;
        Color = color;
        Promoted = promoted;
    }
}

// Move class
class Move
{
    public Piece PieceToMove { get; set; } // The piece to move
    public Piece? PieceToCapture { get; set; } // The piece to capture (nullable)
    public (int X, int Y) To { get; set; } // The target position of the move

    // Constructor
    public Move(Piece pieceToMove, (int X, int Y) to, Piece? pieceToCapture = null)
    {
        PieceToMove = pieceToMove;
        To = to;
        PieceToCapture = pieceToCapture;
    }
}

// Board class
class Board
{
    public Piece?[,] Grid { get; set; } = new Piece?[8, 8]; // 8x8 grid for the board
    public Piece? Aggressor { get; set; } // Current aggressor (nullable)

    // Indexer to access pieces on the board
    public Piece? this[int x, int y]
    {
        get => Grid[x, y];
        set => Grid[x, y] = value;
    }

    // Get all possible moves for the current color
    public List<Move> GetPossibleMoves(PieceColor color)
    {
        var moves = new List<Move>();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var piece = Grid[x, y];
                if (piece != null && piece.Color == color)
                {
                    moves.AddRange(GetMovesForPiece(piece));
                }
            }
        }
        return moves;
    }

    // Get all possible moves for a specific piece
    private List<Move> GetMovesForPiece(Piece piece)
    {
        var moves = new List<Move>();
        int[] dx = { -1, 1, -1, 1 }; // Horizontal movement directions
        int[] dy = { -1, -1, 1, 1 }; // Vertical movement directions

        for (int i = 0; i < 4; i++)
        {
            int newX = piece.X + dx[i];
            int newY = piece.Y + dy[i];

            if (newX >= 0 && newX < 8 && newY >= 0 && newY < 8)
            {
                var targetPiece = Grid[newX, newY];
                if (targetPiece == null)
                {
                    moves.Add(new Move(piece, (newX, newY)));
                }
                else if (targetPiece.Color != piece.Color)
                {
                    int jumpX = newX + dx[i];
                    int jumpY = newY + dy[i];
                    if (jumpX >= 0 && jumpX < 8 && jumpY >= 0 && jumpY < 8 && Grid[jumpX, jumpY] == null)
                    {
                        moves.Add(new Move(piece, (jumpX, jumpY), targetPiece));
                    }
                }
            }
        }
        return moves;
    }

    // Validate if a move is valid
    public Move? ValidateMove(PieceColor color, (int X, int Y) from, (int X, int Y) to)
    {
        var piece = Grid[from.X, from.Y];
        if (piece == null || piece.Color != color)
        {
            return null;
        }

        var moves = GetMovesForPiece(piece);
        return moves.FirstOrDefault(m => m.To == to);
    }

    // Perform a move
    public void PerformMove(Move move)
    {
        Grid[move.PieceToMove.X, move.PieceToMove.Y] = null;
        Grid[move.To.X, move.To.Y] = move.PieceToMove;
        move.PieceToMove.X = move.To.X;
        move.PieceToMove.Y = move.To.Y;

        if (move.PieceToCapture != null)
        {
            Grid[move.PieceToCapture.X, move.PieceToCapture.Y] = null;
        }
    }

    // Get all pieces on the board
    public IEnumerable<Piece> Pieces => Grid.Cast<Piece?>().Where(p => p != null).Select(p => p!);

    // Get the closest pair of pieces between the current color and the opponent's color
    public (Piece, Piece) GetClosestRivalPieces(PieceColor color)
    {
        var pieces = Pieces.Where(p => p.Color == color).ToList();
        var rivals = Pieces.Where(p => p.Color != color).ToList();
        var minDistance = double.MaxValue;
        Piece a = null!;
        Piece b = null!;

        foreach (var piece in pieces)
        {
            foreach (var rival in rivals)
            {
                var distance = Math.Sqrt(Math.Pow(piece.X - rival.X, 2) + Math.Pow(piece.Y - rival.Y, 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    a = piece;
                    b = rival;
                }
            }
        }
        return (a, b);
    }

    // Check if a move is towards a target piece
    public static bool IsTowards(Move move, Piece target)
    {
        int dx = move.To.X - move.PieceToMove.X;
        int dy = move.To.Y - move.PieceToMove.Y;
        int targetDx = target.X - move.PieceToMove.X;
        int targetDy = target.Y - move.PieceToMove.Y;
        return (dx * targetDx > 0) && (dy * targetDy > 0);
    }
}

// Player class
class Player
{
    public PieceColor Color { get; set; } // Player color
    public bool IsHuman { get; set; } // Whether the player is human

    public Player(PieceColor color, bool isHuman)
    {
        Color = color;
        IsHuman = isHuman;
    }
}

// Game class
class Game
{
    public Board Board { get; set; } = new Board(); // Board
    public List<Player> Players { get; set; } = new List<Player>(); // List of players
    public PieceColor Turn { get; set; } = PieceColor.Black; // Current turn color
    public PieceColor? Winner { get; set; } // Winner (nullable)


    public Game(int humanPlayerCount)
    {
        Players.Add(new Player(PieceColor.Black, humanPlayerCount > 0));
        Players.Add(new Player(PieceColor.White, humanPlayerCount > 1));

        // Initialize pieces on the board
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if ((x + y) % 2 == 1)
                {
                    Board[x, y] = new Piece(x, y, PieceColor.Black);
                }
            }
        }
        for (int x = 0; x < 8; x++)
        {
            for (int y = 5; y < 8; y++)
            {
                if ((x + y) % 2 == 1)
                {
                    Board[x, y] = new Piece(x, y, PieceColor.White);
                }
            }
        }
    }

    // Perform a move
    public void PerformMove(Move move)
    {
        Board.PerformMove(move);
        if (move.PieceToCapture != null)
        {
            var captures = Board.GetPossibleMoves(Turn).Where(m => m.PieceToCapture != null).ToList();
            if (captures.Count > 0)
            {
                Board.Aggressor = move.PieceToMove;
                return;
            }
        }
        Board.Aggressor = null;
        Turn = Turn == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        CheckForWinner();
    }

    // Check if there is a winner
    private void CheckForWinner()
    {
        var blackPieces = Board.Pieces.Where(p => p.Color == PieceColor.Black).ToList();
        var whitePieces = Board.Pieces.Where(p => p.Color == PieceColor.White).ToList();

        if (blackPieces.Count == 0)
        {
            Winner = PieceColor.White;
        }
        else if (whitePieces.Count == 0)
        {
            Winner = PieceColor.Black;
        }
        else if (Board.GetPossibleMoves(Turn).Count == 0)
        {
            Winner = Turn == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        }
    }

    // Get the number of captured pieces for a specific color
    public int TakenCount(PieceColor color)
    {
        return 12 - Board.Pieces.Count(p => p.Color == color);
    }
}

// Main program
class Program
{
    static void Main(string[] args)
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

    // Show the intro screen and get the option
    static Game ShowIntroScreenAndGetOption()
    {
        Console.Clear();
        Console.WriteLine();
        Console.WriteLine("  Checkers");
        Console.WriteLine();
        Console.WriteLine("  Checkers is played on an 8x8 board between two sides commonly known as black");
        Console.WriteLine("  and white. The objective is simple - capture all your opponent's pieces. An");
        Console.WriteLine("  alternative way to win is to trap your opponent so that they have no valid");
        Console.WriteLine("  moves left.");
        Console.WriteLine();
        Console.WriteLine("  Black starts first and players take it in turns to move their pieces forward");
        Console.WriteLine("  across the board diagonally. Should a piece reach the other side of the board");
        Console.WriteLine("  the piece becomes a king and can then move diagonally backwards as well as");
        Console.WriteLine("  forwards.");
        Console.WriteLine();
        Console.WriteLine("  Pieces are captured by jumping over them diagonally. More than one enemy piece");
        Console.WriteLine("  can be captured in the same turn by the same piece. If you can capture a piece");
        Console.WriteLine("  you must capture a piece.");
        Console.WriteLine();
        Console.WriteLine("  Moves are selected with the arrow keys. Use the [enter] button to select the");
        Console.WriteLine("  from and to squares. Invalid moves are ignored.");
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

    // Run the main game loop
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
                    Piece? piece = game.Board[from.Value.X, from.Value.Y];

                    if (piece is null || piece.Color != game.Turn)
                    {
                        from = null;
                        to = null;
                    }

                    if (from is not null && to is not null && piece is not null)
                    {
                        Move? move = moves.FirstOrDefault(m => m.PieceToMove.Equals(piece) && m.To == to);
                        if (move is not null &&
                            (game.Board.Aggressor is null || move.PieceToMove == game.Board.Aggressor))
                        {
                            game.PerformMove(move);
                        }
                    }
                }
            }
            else
            {
                List<Move> moves = game.Board.GetPossibleMoves(game.Turn);
                List<Move> captures = moves.Where(move => move.PieceToCapture is not null).ToList();
                if (captures.Count > 0)
                {
                    game.PerformMove(captures[Random.Shared.Next(captures.Count)]);
                }
                else if (!game.Board.Pieces.Any(piece => piece.Color == game.Turn && !piece.Promoted))
                {
                    var (a, b) = game.Board.GetClosestRivalPieces(game.Turn);
                    Move? priorityMove = moves.FirstOrDefault(move => move.PieceToMove == a && Board.IsTowards(move, b));
                    game.PerformMove(priorityMove ?? moves[Random.Shared.Next(moves.Count)]);
                }
                else
                {
                    game.PerformMove(moves[Random.Shared.Next(moves.Count)]);
                }
            }

            RenderGameState(game, playerMoved: currentPlayer, promptPressKey: true);
            Console.ReadKey(true);
        }
    }

    // Render the game state
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
        sb.AppendLine($"  8 ║  {B(0, 7)} {B(1, 7)} {B(2, 7)} {B(3, 7)} {B(4, 7)} {B(5, 7)} {B(6, 7)} {B(7, 7)}  ║ {BlackPiece} = Black");
        sb.AppendLine($"  7 ║  {B(0, 6)} {B(1, 6)} {B(2, 6)} {B(3, 6)} {B(4, 6)} {B(5, 6)} {B(6, 6)} {B(7, 6)}  ║ {BlackKing} = Black King");
        sb.AppendLine($"  6 ║  {B(0, 5)} {B(1, 5)} {B(2, 5)} {B(3, 5)} {B(4, 5)} {B(5, 5)} {B(6, 5)} {B(7, 5)}  ║ {WhitePiece} = White");
        sb.AppendLine($"  5 ║  {B(0, 4)} {B(1, 4)} {B(2, 4)} {B(3, 4)} {B(4, 4)} {B(5, 4)} {B(6, 4)} {B(7, 4)}  ║ {WhiteKing} = White King");
        sb.AppendLine($"  4 ║  {B(0, 3)} {B(1, 3)} {B(2, 3)} {B(3, 3)} {B(4, 3)} {B(5, 3)} {B(6, 3)} {B(7, 3)}  ║");
        sb.AppendLine($"  3 ║  {B(0, 2)} {B(1, 2)} {B(2, 2)} {B(3, 2)} {B(4, 2)} {B(5, 2)} {B(6, 2)} {B(7, 2)}  ║ Taken:");
        sb.AppendLine($"  2 ║  {B(0, 1)} {B(1, 1)} {B(2, 1)} {B(3, 1)} {B(4, 1)} {B(5, 1)} {B(6, 1)} {B(7, 1)}  ║ {game.TakenCount(PieceColor.White),2} x {WhitePiece}");
        sb.AppendLine($"  1 ║  {B(0, 0)} {B(1, 0)} {B(2, 0)} {B(3, 0)} {B(4, 0)} {B(5, 0)} {B(6, 0)} {B(7, 0)}  ║ {game.TakenCount(PieceColor.Black),2} x {BlackPiece}");
        sb.AppendLine($"    ╚═══════════════════╝");
        sb.AppendLine($"       A B C D E F G H");
        sb.AppendLine();
        if (selection is not null)
        {
            sb.Replace(" $ ", $"[{ToChar(game.Board[selection.Value.X, selection.Value.Y])}]");
        }
        if (from is not null)
        {
            char fromChar = ToChar(game.Board[from.Value.X, from.Value.Y]);
            sb.Replace(" @ ", $"<{fromChar}>");
            sb.Replace("@ ", $"{fromChar}>");
            sb.Replace(" @", $"<{fromChar}");
        }
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
        string p = "  Press any key to continue...";
        string s = "                              ";
        sb.AppendLine(promptPressKey ? p : s);
        Console.Write(sb);

        char B(int x, int y) =>
            (x, y) == selection ? '$' :
            (x, y) == from ? '@' :
            ToChar(game.Board[x, y]);

        static char ToChar(Piece? piece) =>
            piece is null ? Vacant :
            (piece.Color, piece.Promoted) switch
            {
                (PieceColor.Black, false) => BlackPiece,
                (PieceColor.Black, true) => BlackKing,
                (PieceColor.White, false) => WhitePiece,
                (PieceColor.White, true) => WhiteKing,
                _ => throw new NotImplementedException(),
            };
    }

    // Human player selects a move
    static (int X, int Y)? HumanMoveSelection(Game game, (int X, int y)? selectionStart = null, (int X, int Y)? from = null)
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
}