/*
Feature 1: Trap System
- 3 random traps are generated on the board each turn, displayed as `×`.
- Pieces that move onto a trap have a 30% chance of being destroyed.
- Trap positions are updated each turn, and players must avoid them.

Feature 2: Move in eight directions
- All pieces can move in 8 directions (up, down, left, right, and diagonal).
- Capture logic also supports 8 directions, allowing pieces to jump over enemy pieces.
- The King (upgraded) can move forward and backward.

Feature 3: Undo function
- Players can press the `U` key to undo the last move.
- Up to 3 undos are allowed per game.
- After undoing, the game state is restored to the previous step.

Feature 4: Special moves (teleport pieces)
- 10% of pieces gain the ability to teleport to any enemy piece position.
- Teleport pieces can bypass obstacles and move directly to the target.
- The Teleporter Piece looks the same as a normal piece, but has special abilities.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        Exception? exception = null; // Used to catch exceptions
        Encoding encoding = Console.OutputEncoding; // Save the current console encoding
        List<Game> gameHistory = new(); // Used to store game history for undo functionality

        try
        {
            Console.OutputEncoding = Encoding.UTF8; // Set console encoding to UTF-8
            Game game = ShowIntroScreenAndGetOption(); // Display the intro screen and get game options
            gameHistory.Add(game.Clone()); // Add the initial game state to history
            Console.Clear(); // Clear the console
            RunGameLoop(game, gameHistory); // Run the main game loop
            RenderGameState(game, promptPressKey: true); // Render the end game state
            Console.ReadKey(true); // Wait for user input
        }
        catch (Exception e)
        {
            exception = e; // Catch the exception
            throw; // Re-throw the exception
        }
        finally
        {
            Console.OutputEncoding = encoding; // Restore the console encoding
            Console.CursorVisible = true; // Show the cursor
            Console.Clear(); // Clear the console
            Console.WriteLine(exception?.ToString() ?? "Checkers was closed."); // Display exception or closing message
        }
    }

    // Display the intro screen and get game options
    static Game ShowIntroScreenAndGetOption()
    {
        Console.Clear(); // Clear the console
        Console.WriteLine(); // Empty line
        Console.WriteLine("  Checkers"); // Display game title
        Console.WriteLine(); // Empty line
        Console.WriteLine("  Checkers is played on an 8x8 board between two sides commonly known as black"); // Game rules
        Console.WriteLine("  and white. The objective is simple - capture all your opponent's pieces. An"); // Game rules
        Console.WriteLine("  alternative way to win is to trap your opponent so that they have no valid"); // Game rules
        Console.WriteLine("  moves left."); // Game rules
        Console.WriteLine(); // Empty line
        Console.WriteLine("  Black starts first and players take it in turns to move their pieces forward"); // Game rules
        Console.WriteLine("  across the board diagonally. Should a piece reach the other side of the board"); // Game rules
        Console.WriteLine("  the piece becomes a king and can then move diagonally backwards as well as"); // Game rules
        Console.WriteLine("  forwards."); // Game rules
        Console.WriteLine(); // Empty line
        Console.WriteLine("  Pieces are captured by jumping over them diagonally. More than one enemy piece"); // Game rules
        Console.WriteLine("  can be captured in the same turn by the same piece. If you can capture a piece"); // Game rules
        Console.WriteLine("  you must capture a piece."); // Game rules
        Console.WriteLine(); // Empty line
        Console.WriteLine("  Moves are selected with the arrow keys. Use the [enter] button to select the"); // Game rules
        Console.WriteLine("  from and to squares. Invalid moves are ignored."); // Game rules
        Console.WriteLine(); // Empty line
        Console.WriteLine("  Press a number key to choose the number of human players:"); // Prompt to choose players
        Console.WriteLine("    [0] Black (computer) vs White (computer)"); // Option 0
        Console.WriteLine("    [1] Black (human) vs White (computer)"); // Option 1
        Console.Write("    [2] Black (human) vs White (human)"); // Option 2

        int? humanPlayerCount = null; // Initialize player count as null
        while (humanPlayerCount is null) // Loop until a valid option is selected
        {
            Console.CursorVisible = false; // Hide the cursor
            switch (Console.ReadKey(true).Key) // Read user input
            {
                case ConsoleKey.D0 or ConsoleKey.NumPad0: humanPlayerCount = 0; break; // Select 0
                case ConsoleKey.D1 or ConsoleKey.NumPad1: humanPlayerCount = 1; break; // Select 1
                case ConsoleKey.D2 or ConsoleKey.NumPad2: humanPlayerCount = 2; break; // Select 2
            }
        }
        return new Game(humanPlayerCount.Value); // Return a new game instance
    }

    // Main game loop
    static void RunGameLoop(Game game, List<Game> gameHistory)
    {
        while (game.Winner is null) // Loop until there is a winner
        {
            UpdateTraps(game); // Update trap positions
            Player currentPlayer = game.Players.First(player => player.Color == game.Turn); // Get the current player

            // Handle undo functionality
            if (currentPlayer.IsHuman && Console.KeyAvailable) // If it's a human player and a key is pressed
            {
                var key = Console.ReadKey(true).Key; // Read the key
                if (key == ConsoleKey.U && game.UndoCount > 0 && gameHistory.Count > 1) // If 'U' is pressed and undo is available
                {
                    gameHistory.RemoveAt(gameHistory.Count - 1); // Remove the current state
                    game = gameHistory.Last().Clone(); // Revert to the previous state
                    game.UndoCount--; // Decrement undo count
                    RenderGameState(game); // Render the game state
                    continue; // Continue the game loop
                }
            }

            if (currentPlayer.IsHuman) // If it's a human player
            {
                while (game.Turn == currentPlayer.Color) // Loop until the player completes their move
                {
                    (int X, int Y)? selectionStart = null; // Initialize selection start
                    (int X, int Y)? from = game.Board.Aggressor is not null ? (game.Board.Aggressor.X, game.Board.Aggressor.Y) : null; // Start from aggressor if available
                    List<Move> moves = game.Board.GetPossibleMoves(game.Turn); // Get all possible moves
                    if (moves.Select(move => move.PieceToMove).Distinct().Count() is 1) // If only one piece can move
                    {
                        Move must = moves.First(); // Get the mandatory move
                        from = (must.PieceToMove.X, must.PieceToMove.Y); // Set the start position
                        selectionStart = must.To; // Set the selection start
                    }
                    while (from is null) // Loop until a valid start is selected
                    {
                        from = HumanMoveSelection(game); // Player selects the start position
                        selectionStart = from; // Set the selection start
                    }
                    (int X, int Y)? to = HumanMoveSelection(game, selectionStart: selectionStart, from: from); // Player selects the end position
                    Piece? piece = game.Board[from.Value.X, from.Value.Y]; // Get the piece at the start position
                    if (piece is null || piece.Color != game.Turn) // If the piece is invalid or not owned by the player
                    {
                        from = null; // Reset the start position
                        to = null; // Reset the end position
                    }
                    if (from is not null && to is not null) // If both start and end positions are valid
                    {
                        Move? move = game.Board.ValidateMove(game.Turn, from.Value, to.Value); // Validate the move
                        if (move is not null && (game.Board.Aggressor is null || move.PieceToMove == game.Board.Aggressor)) // If the move is valid
                        {
                            game.PerformMove(move); // Perform the move
                        }
                    }
                }
            }
            else // If it's an AI player
            {
                List<Move> moves = game.Board.GetPossibleMoves(game.Turn); // Get all possible moves
                List<Move> captures = moves.Where(move => move.PieceToCapture is not null).ToList(); // Get all capture moves
                if (captures.Count > 0) // If there are capture moves
                {
                    game.PerformMove(captures[Random.Shared.Next(captures.Count)]); // Perform a random capture move
                }
                else if (!game.Board.Pieces.Any(piece => piece.Color == game.Turn && !piece.Promoted)) // If no unpromoted pieces
                {
                    var (a, b) = game.Board.GetClosestRivalPieces(game.Turn); // Get the closest rival pieces
                    Move? priorityMove = moves.FirstOrDefault(move => move.PieceToMove == a && Board.IsTowards(move, b)); // Prioritize moving towards the closest rival
                    game.PerformMove(priorityMove ?? moves[Random.Shared.Next(moves.Count)]); // Perform the priority or random move
                }
                else // Otherwise
                {
                    game.PerformMove(moves[Random.Shared.Next(moves.Count)]); // Perform a random move
                }
            }

            RenderGameState(game, playerMoved: currentPlayer, promptPressKey: true); // Render the game state
            Console.ReadKey(true); // Wait for user input
            gameHistory.Add(game.Clone()); // Add the current state to history
        }
    }

    // Update trap positions
    static void UpdateTraps(Game game)
    {
        game.Board.Traps.Clear(); // Clear current traps
        for (int i = 0; i < 3; i++) // Generate 3 traps
        {
            game.Board.Traps.Add((Random.Shared.Next(8), Random.Shared.Next(8))); // Randomly generate trap positions
        }
    }

    // Render the game state
    static void RenderGameState(Game game, Player? playerMoved = null, (int X, int Y)? selection = null, (int X, int Y)? from = null, bool promptPressKey = false)
    {
        const char BlackPiece = '○'; // Black piece
        const char BlackKing = '☺'; // Black king
        const char WhitePiece = '◙'; // White piece
        const char WhiteKing = '☻'; // White king
        const char Vacant = '·'; // Empty space

        Console.CursorVisible = false; // Hide the cursor
        Console.SetCursorPosition(0, 0); // Set cursor position
        StringBuilder sb = new(); // Create a string builder
        sb.AppendLine(); // Empty line
        sb.AppendLine("  Checkers"); // Display game title
        sb.AppendLine(); // Empty line
        sb.AppendLine($"    ╔═══════════════════╗"); // Top border
        sb.AppendLine($"  8 ║  {B(0, 7, game)} {B(1, 7, game)} {B(2, 7, game)} {B(3, 7, game)} {B(4, 7, game)} {B(5, 7, game)} {B(6, 7, game)} {B(7, 7, game)}  ║ {BlackPiece} = Black"); // Row 8
        sb.AppendLine($"  7 ║  {B(0, 6, game)} {B(1, 6, game)} {B(2, 6, game)} {B(3, 6, game)} {B(4, 6, game)} {B(5, 6, game)} {B(6, 6, game)} {B(7, 6, game)}  ║ {BlackKing} = Black King"); // Row 7
        sb.AppendLine($"  6 ║  {B(0, 5, game)} {B(1, 5, game)} {B(2, 5, game)} {B(3, 5, game)} {B(4, 5, game)} {B(5, 5, game)} {B(6, 5, game)} {B(7, 5, game)}  ║ {WhitePiece} = White"); // Row 6
        sb.AppendLine($"  5 ║  {B(0, 4, game)} {B(1, 4, game)} {B(2, 4, game)} {B(3, 4, game)} {B(4, 4, game)} {B(5, 4, game)} {B(6, 4, game)} {B(7, 4, game)}  ║ {WhiteKing} = White King"); // Row 5
        sb.AppendLine($"  4 ║  {B(0, 3, game)} {B(1, 3, game)} {B(2, 3, game)} {B(3, 3, game)} {B(4, 3, game)} {B(5, 3, game)} {B(6, 3, game)} {B(7, 3, game)}  ║"); // Row 4
        sb.AppendLine($"  3 ║  {B(0, 2, game)} {B(1, 2, game)} {B(2, 2, game)} {B(3, 2, game)} {B(4, 2, game)} {B(5, 2, game)} {B(6, 2, game)} {B(7, 2, game)}  ║ Taken:"); // Row 3
        sb.AppendLine($"  2 ║  {B(0, 1, game)} {B(1, 1, game)} {B(2, 1, game)} {B(3, 1, game)} {B(4, 1, game)} {B(5, 1, game)} {B(6, 1, game)} {B(7, 1, game)}  ║ {game.TakenCount(PieceColor.White),2} x {WhitePiece}"); // Row 2
        sb.AppendLine($"  1 ║  {B(0, 0, game)} {B(1, 0, game)} {B(2, 0, game)} {B(3, 0, game)} {B(4, 0, game)} {B(5, 0, game)} {B(6, 0, game)} {B(7, 0, game)}  ║ {game.TakenCount(PieceColor.Black),2} x {BlackPiece}"); // Row 1
        sb.AppendLine($"    ╚═══════════════════╝"); // Bottom border
        sb.AppendLine($"       A B C D E F G H"); // Column labels
        sb.AppendLine(); // Empty line
        if (selection is not null) // If there is a selection
        {
            sb.Replace(" $ ", $"[{ToChar(game.Board[selection.Value.X, selection.Value.Y])}]"); // Replace selection marker
        }
        if (from is not null) // If there is a start position
        {
            char fromChar = ToChar(game.Board[from.Value.X, from.Value.Y]); // Get the start character
            sb.Replace(" @ ", $"<{fromChar}>"); // Replace start marker
            sb.Replace("@ ", $"{fromChar}>"); // Replace start marker
            sb.Replace(" @", $"<{fromChar}"); // Replace start marker
        }
        PieceColor? wc = game.Winner; // Get the winner
        PieceColor? mc = playerMoved?.Color; // Get the moving player
        PieceColor? tc = game.Turn; // Get the current player
        string w = $"  *** {wc} wins ***"; // Win message
        string m = $"  {mc} moved       "; // Move message
        string t = $"  {tc}'s turn      "; // Current player message
        sb.AppendLine(game.Winner is not null ? w : playerMoved is not null ? m : t); // Add the message
        string p = "  Press any key to continue..."; // Prompt message
        string s = "                              "; // Blank message
        sb.AppendLine(promptPressKey ? p : s); // Add the prompt
        Console.Write(sb.ToString()); // Output to console

        static char B(int x, int y, Game game) => // Get the board position character
            game.Board.Traps.Contains((x, y)) ? '×' : // If it's a trap
            game.Board[x, y] switch // Return the character based on the piece type
            {
                null => Vacant, // Empty space
                Piece p when p.Color == PieceColor.Black && !p.Promoted => BlackPiece, // Black piece
                Piece p when p.Color == PieceColor.Black && p.Promoted => BlackKing, // Black king
                Piece p when p.Color == PieceColor.White && !p.Promoted => WhitePiece, // White piece
                Piece p when p.Color == PieceColor.White && p.Promoted => WhiteKing, // White king
                _ => Vacant // Default to empty space
            };

        static char ToChar(Piece? piece) => // Convert a piece to a character
            piece is null ? Vacant : // If the piece is null, return empty space
            (piece.Color, piece.Promoted) switch // Return the character based on the piece type
            {
                (PieceColor.Black, false) => BlackPiece, // Black piece
                (PieceColor.Black, true) => BlackKing, // Black king
                (PieceColor.White, false) => WhitePiece, // White piece
                (PieceColor.White, true) => WhiteKing, // White king
                _ => throw new NotImplementedException(), // Not implemented
            };
    }

    // Player move selection
    static (int X, int Y)? HumanMoveSelection(Game game, (int X, int y)? selectionStart = null, (int X, int Y)? from = null)
    {
        (int X, int Y) selection = selectionStart ?? (3, 3); // Initialize selection position
        while (true) // Loop until selection is complete
        {
            RenderGameState(game, selection: selection, from: from); // Render the game state
            switch (Console.ReadKey(true).Key) // Read the key
            {
                case ConsoleKey.DownArrow: selection.Y = Math.Max(0, selection.Y - 1); break; // Move down
                case ConsoleKey.UpArrow: selection.Y = Math.Min(7, selection.Y + 1); break; // Move up
                case ConsoleKey.LeftArrow: selection.X = Math.Max(0, selection.X - 1); break; // Move left
                case ConsoleKey.RightArrow: selection.X = Math.Min(7, selection.X + 1); break; // Move right
                case ConsoleKey.Enter: return selection; // Confirm selection
                case ConsoleKey.Escape: return null; // Cancel selection
            }
        }
    }
}

// Game class
class Game
{
    public int UndoCount { get; set; } = 3; // Undo count
    public List<(int X, int Y)> Traps { get; set; } = new(); // Trap positions
    public Board Board { get; set; } = new(); // Game board
    public PieceColor Turn { get; set; } = PieceColor.Black; // Current player's turn
    public List<Player> Players { get; set; } // List of players
    public PieceColor? Winner { get; set; } // Winner of the game
    public int HumanPlayerCount { get; } // Number of human players

    public Game(int humanPlayerCount) // Constructor
    {
        HumanPlayerCount = humanPlayerCount; // Set the number of human players
        Players = new List<Player> // Initialize players
        {
            new Player(PieceColor.Black, humanPlayerCount > 0), // Black player
            new Player(PieceColor.White, humanPlayerCount > 1) // White player
        };
        Board.Initialize(); // Initialize the board
    }

    public Game Clone() => new(HumanPlayerCount) // Clone method
    {
        Board = Board.Clone(), // Clone the board
        Turn = Turn, // Clone the current player
        Players = Players.Select(p => p.Clone()).ToList(), // Clone the players
        Winner = Winner, // Clone the winner
        Traps = new List<(int X, int Y)>(Traps), // Clone the traps
        UndoCount = this.UndoCount // Clone the undo count
    };

    public int TakenCount(PieceColor color) => Board.Pieces.Count(p => p.Color != color && !p.IsAlive); // Count of captured pieces

    public void PerformMove(Move move) // Perform a move
    {
        Board.MovePiece(move); // Move the piece
        Turn = Turn == PieceColor.Black ? PieceColor.White : PieceColor.Black; // Switch turns
        CheckForWinner(); // Check for a winner
    }

    private void CheckForWinner() // Check for a winner
    {
        if (!Board.Pieces.Any(p => p.Color == PieceColor.Black && p.IsAlive)) // If all black pieces are captured
        {
            Winner = PieceColor.White; // White wins
        }
        else if (!Board.Pieces.Any(p => p.Color == PieceColor.White && p.IsAlive)) // If all white pieces are captured
        {
            Winner = PieceColor.Black; // Black wins
        }
    }
}

// Board class
class Board
{
    public List<Piece> Pieces { get; set; } = new(); // List of pieces
    public Piece? Aggressor { get; set; } // Current aggressor
    public List<(int X, int Y)> Traps { get; set; } = new(); // List of traps

    public void Initialize() // Initialize the board
    {
        Pieces.Clear(); // Clear the pieces
        for (int x = 0; x < 8; x++) // Loop through columns
        {
            for (int y = 0; y < 3; y++) // Loop through black piece rows
            {
                if ((x + y) % 2 == 1) // If it's a black piece position
                {
                    Pieces.Add(new Piece(x, y, PieceColor.Black)); // Add a black piece
                }
            }
            for (int y = 5; y < 8; y++) // Loop through white piece rows
            {
                if ((x + y) % 2 == 1) // If it's a white piece position
                {
                    Pieces.Add(new Piece(x, y, PieceColor.White)); // Add a white piece
                }
            }
        }
    }

    public Board Clone() => new() // Clone method
    {
        Pieces = Pieces.Select(p => p.Clone()).ToList(), // Clone the pieces
        Aggressor = Aggressor?.Clone(), // Clone the aggressor
        Traps = new List<(int X, int Y)>(Traps) // Clone the traps
    };

    public Piece? this[int x, int y] => Pieces.FirstOrDefault(p => p.X == x && p.Y == y && p.IsAlive); // Get the piece at a position

    public bool IsWithinBounds(int x, int y) => x >= 0 && x < 8 && y >= 0 && y < 8; // Check if a position is within bounds

    public List<Move> GetPossibleMoves(PieceColor color) // Get all possible moves
    {
        var moves = new List<Move>(); // Initialize the move list
        foreach (var piece in Pieces.Where(p => p.Color == color && p.IsAlive)) // Loop through pieces of the current player
        {
            var directions = new (int, int)[] // 8 directions
            { 
                (-1, -1), (-1, 1), (1, -1), (1, 1),
                (0, 1), (0, -1), (1, 0), (-1, 0) 
            };

            foreach (var (dx, dy) in directions) // Loop through directions
            {
                int x = piece.X + dx, y = piece.Y + dy; // Calculate the target position
                if (IsWithinBounds(x, y) && this[x, y] is null) // If the target position is valid and empty
                {
                    moves.Add(new Move(piece, x, y)); // Add the move
                }

                if (CanCapture(piece, dx, dy, out var captured)) // If a capture is possible
                {
                    moves.Add(new Move(piece, piece.X + 2 * dx, piece.Y + 2 * dy, captured)); // Add the capture move
                }
            }

            if (piece.CanTeleport) // If the piece can teleport
            {
                foreach (var target in Pieces.Where(p => p.Color != color && p.IsAlive)) // Loop through enemy pieces
                {
                    moves.Add(new Move(piece, target.X, target.Y, target)); // Add the teleport move
                }
            }
        }

        foreach (var move in moves.ToArray()) // Loop through all moves
        {
            if (Traps.Contains((move.To.X, move.To.Y)) && Random.Shared.Next(100) < 30) // If the move lands on a trap
            {
                moves.Remove(move); // Remove the move
                Pieces.Remove(move.PieceToMove); // Remove the piece
            }
        }

        return moves; // Return the list of moves
    }

    public bool CanCapture(Piece piece, int dx, int dy, out Piece? captured) // Check if a capture is possible
    {
        captured = null; // Initialize the captured piece
        int x = piece.X + dx, y = piece.Y + dy; // Calculate the target position
        if (!IsWithinBounds(x, y)) return false; // If the target position is invalid

        captured = this[x, y]; // Get the piece at the target position
        if (captured is null || captured.Color == piece.Color) return false; // If the target is empty or the same color

        int x2 = x + dx, y2 = y + dy; // Calculate the jump position
        if (!IsWithinBounds(x2, y2)) return false; // If the jump position is invalid

        return this[x2, y2] is null; // Return whether the jump position is empty
    }

    public Move? ValidateMove(PieceColor color, (int X, int Y) from, (int X, int Y) to) // Validate a move
    {
        var piece = this[from.X, from.Y]; // Get the piece at the start position
        if (piece is null || piece.Color != color) return null; // If the piece is invalid or not owned by the player

        var moves = GetPossibleMoves(color); // Get all possible moves
        return moves.FirstOrDefault(m => m.PieceToMove == piece && m.To.X == to.X && m.To.Y == to.Y); // Return the matching move
    }

    public void MovePiece(Move move) // Move a piece
    {
        var piece = move.PieceToMove; // Get the moving piece
        piece.X = move.To.X; // Update the piece's position
        piece.Y = move.To.Y; // Update the piece's position

        if (move.PieceToCapture is not null) // If there is a capture
        {
            move.PieceToCapture.IsAlive = false; // Mark the captured piece as dead
            Aggressor = piece; // Set the aggressor
        }
        else // If there is no capture
        {
            Aggressor = null; // Clear the aggressor
        }

        if ((piece.Color == PieceColor.Black && piece.Y == 7) || (piece.Color == PieceColor.White && piece.Y == 0)) // If the piece reaches the opposite side
        {
            piece.Promoted = true; // Promote the piece to a king
        }
    }

    public (Piece, Piece) GetClosestRivalPieces(PieceColor color) // Get the closest rival pieces
    {
        var rivals = Pieces.Where(p => p.Color != color && p.IsAlive).ToList(); // Get all enemy pieces
        var pieces = Pieces.Where(p => p.Color == color && p.IsAlive).ToList(); // Get all friendly pieces

        (Piece, Piece) closest = (pieces[0], rivals[0]); // Initialize the closest pieces
        double minDistance = double.MaxValue; // Initialize the minimum distance

        foreach (var piece in pieces) // Loop through friendly pieces
        {
            foreach (var rival in rivals) // Loop through enemy pieces
            {
                double distance = Math.Sqrt(Math.Pow(piece.X - rival.X, 2) + Math.Pow(piece.Y - rival.Y, 2)); // Calculate the distance
                if (distance < minDistance) // If the distance is smaller
                {
                    minDistance = distance; // Update the minimum distance
                    closest = (piece, rival); // Update the closest pieces
                }
            }
        }

        return closest; // Return the closest pieces
    }

    public static bool IsTowards(Move move, Piece target) // Check if a move is towards a target
    {
        int dx = move.To.X - move.PieceToMove.X; // Calculate the X direction
        int dy = move.To.Y - move.PieceToMove.Y; // Calculate the Y direction
        int targetDx = target.X - move.PieceToMove.X; // Calculate the target X direction
        int targetDy = target.Y - move.PieceToMove.Y; // Calculate the target Y direction
        return Math.Sign(dx) == Math.Sign(targetDx) && Math.Sign(dy) == Math.Sign(targetDy); // Return whether the move is towards the target
    }
}

// Piece class
class Piece
{
    public int X { get; set; } // Piece X coordinate
    public int Y { get; set; } // Piece Y coordinate
    public PieceColor Color { get; } // Piece color
    public bool Promoted { get; set; } // Whether the piece is promoted to a king
    public bool IsAlive { get; set; } = true; // Whether the piece is alive
    public bool CanTeleport { get; init; } = Random.Shared.Next(10) == 0; // Whether the piece can teleport

    public Piece(int x, int y, PieceColor color) // Constructor
    {
        X = x; // Set the X coordinate
        Y = y; // Set the Y coordinate
        Color = color; // Set the color
    }

    public Piece Clone() => new(X, Y, Color) // Clone method
    {
        Promoted = Promoted, // Clone the promotion status
        IsAlive = IsAlive, // Clone the alive status
        CanTeleport = CanTeleport // Clone the teleport ability
    };
}

// Player class
class Player
{
    public PieceColor Color { get; } // Player color
    public bool IsHuman { get; } // Whether the player is human

    public Player(PieceColor color, bool isHuman) // Constructor
    {
        Color = color; // Set the color
        IsHuman = isHuman; // Set whether the player is human
    }

    public Player Clone() => new(Color, IsHuman); // Clone method
}

// Move class
class Move
{
    public Piece PieceToMove { get; } // The piece to move
    public (int X, int Y) To { get; } // The target position
    public Piece? PieceToCapture { get; } // The piece to capture

    public Move(Piece pieceToMove, int x, int y, Piece? pieceToCapture = null) // Constructor
    {
        PieceToMove = pieceToMove; // Set the piece to move
        To = (x, y); // Set the target position
        PieceToCapture = pieceToCapture; // Set the piece to capture
    }
}

// Piece color enum
enum PieceColor { Black, White }
