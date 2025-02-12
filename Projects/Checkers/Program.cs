Exception? exception = null; 
Encoding encoding = Console.OutputEncoding; 

try
{
    Console.OutputEncoding = Encoding.UTF8; 
    Game game = ShowIntroScreenAndGetOption(); // Show intro screen, game options
    Console.Clear(); 
    RunGameLoop(game); // Main Game Loop
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
    Console.WriteLine(exception?.ToString() ?? "Checkers was closed."); // Output abnormal information or prompt that the game is closed
}

// Show intro screen, game options
Game ShowIntroScreenAndGetOption()
{
    Console.Clear();
    Console.WriteLine(); 
    Console.WriteLine("  Checkers"); // Output game title
    Console.WriteLine(); 
    Console.WriteLine("  Checkers is played on an 8x8 board between two sides commonly known as black"); 
    Console.WriteLine("  and white. The objective is simple - capture all your opponent's pieces. An"); 
    Console.WriteLine("  alternative way to win is to trap your opponent so that they have no valid"); 
    Console.WriteLine("  moves left."); // Output game rules introduction
    Console.WriteLine(); 
    Console.WriteLine("  Black starts first and players take it in turns to move their pieces forward");
    Console.WriteLine("  across the board diagonally. Should a piece reach the other side of the board"); 
    Console.WriteLine("  the piece becomes a king and can then move diagonally backwards as well as"); 
    Console.WriteLine("  forwards."); // Output game rules introduction
    Console.WriteLine(); 
    Console.WriteLine("  Pieces are captured by jumping over them diagonally. More than one enemy piece"); 
    Console.WriteLine("  can be captured in the same turn by the same piece. If you can capture a piece"); 
    Console.WriteLine("  you must capture a piece."); // Output game rules introduction
    Console.WriteLine(); 
    Console.WriteLine("  Moves are selected with the arrow keys. Use the [enter] button to select the"); 
    Console.WriteLine("  from and to squares. Invalid moves are ignored."); // Output game rules introduction
    Console.WriteLine(); 
    Console.WriteLine("  Press a number key to choose number of human players:"); // Select the number of players
    Console.WriteLine("    [0] Black (computer) vs White (computer)"); // Output Option 0
    Console.WriteLine("    [1] Black (human) vs White (computer)"); // Output Option 1
    Console.Write("    [2] Black (human) vs White (human)"); // Output Option 2

    int? humanPlayerCount = null; // Initialize the number of players
    while (humanPlayerCount is null) // Loop until the number of players is selected
    {
        Console.CursorVisible = false; 
        switch (Console.ReadKey(true).Key) // Read user keystrokes
        {
            case ConsoleKey.D0 or ConsoleKey.NumPad0: humanPlayerCount = 0; break; // If the 0 key is pressed, set the number of players to 0
            case ConsoleKey.D1 or ConsoleKey.NumPad1: humanPlayerCount = 1; break; // If the 1 key is pressed, set the number of players to 1
            case ConsoleKey.D2 or ConsoleKey.NumPad2: humanPlayerCount = 2; break; // If the 2 key is pressed, set the number of players to 2
        }
    }
    return new Game(humanPlayerCount.Value); // 
}

// Run the main game loop
void RunGameLoop(Game game)
{
    while (game.Winner is null) // Loop until a player wins
    {
        Player currentPlayer = game.Players.First(player => player.Color == game.Turn); // Get the current player
        if (currentPlayer.IsHuman) // If the current player is a human
        {
            while (game.Turn == currentPlayer.Color) // Loop until the current player has finished moving
            {
                (int X, int Y)? selectionStart = null; 
                (int X, int Y)? from = game.Board.Aggressor is not null ? (game.Board.Aggressor.X, game.Board.Aggressor.Y) : null; // 如果有攻击者，设置起点为攻击者的位置
                List<Move> moves = game.Board.GetPossibleMoves(game.Turn); // Get all possible moves for the current player
                if (moves.Select(move => move.PieceToMove).Distinct().Count() is 1) // If only one piece can be moved
                {
                    Move must = moves.First(); 
                    from = (must.PieceToMove.X, must.PieceToMove.Y); // Set the starting point to the position of the chess piece that must be moved
                    selectionStart = must.To; // Set the starting point to the target position that must be moved
                }
                while (from is null) // Loop until the starting point is determined
                {
                    from = HumanMoveSelection(game); // Human player chooses starting point
                    selectionStart = from; // Set the starting point
                }
                (int X, int Y)? to = HumanMoveSelection(game, selectionStart: selectionStart, from: from); 
                Piece? piece = null; 
                piece = game.Board[from.Value.X, from.Value.Y]; // Get the chess piece at the starting position
                if (piece is null || piece.Color != game.Turn) // If the piece does not exist or the piece color is not the current player's color
                {
                    from = null; // Reset starting point
                    to = null; // Reset target location
                }
                if (from is not null && to is not null) // If both the origin and destination locations are valid
                {
                    Move? move = game.Board.ValidateMove(game.Turn, from.Value, to.Value); // Verify that the move worked
                    if (move is not null &&
                        (game.Board.Aggressor is null || move.PieceToMove == game.Board.Aggressor)) // If the move is valid and there is no attacker or the moved piece is an attacker
                    {
                        game.PerformMove(move); // Execute Move
                    }
                }
            }
        }
        else // If the current player is a computer
        {
            List<Move> moves = game.Board.GetPossibleMoves(game.Turn); // Get all possible moves for the current player
            List<Move> captures = moves.Where(move => move.PieceToCapture is not null).ToList(); // Get all moves that can capture the opponent's pieces
            if (captures.Count > 0) // If there is a move that can be captured
            {
                game.PerformMove(captures[Random.Shared.Next(captures.Count)]); // Randomly select a move and execute
            }
            else if(!game.Board.Pieces.Any(piece => piece.Color == game.Turn && !piece.Promoted)) // If there are no unupgraded pieces
            {
                var (a, b) = game.Board.GetClosestRivalPieces(game.Turn); // Get the nearest opponent piece
                Move? priorityMove = moves.FirstOrDefault(move => move.PieceToMove == a && Board.IsTowards(move, b)); // Get Priority Mobile
                game.PerformMove(priorityMove ?? moves[Random.Shared.Next(moves.Count)]); // Performing prioritized or random moves
            }
            else
            {
                game.PerformMove(moves[Random.Shared.Next(moves.Count)]); // Choose a move at random and execute
            }
        }

        RenderGameState(game, playerMoved: currentPlayer, promptPressKey: true); 
        Console.ReadKey(true); //Wait for the user to press any key
    }
}


void RenderGameState(Game game, Player? playerMoved = null, (int X, int Y)? selection = null, (int X, int Y)? from = null, bool promptPressKey = false)
{
    const char BlackPiece = '○'; // Black chess piece
    const char BlackKing  = '☺'; // Black King
    const char WhitePiece = '◙'; // White chess pieces
    const char WhiteKing  = '☻'; // White King
    const char Vacant     = '·'; // Vacancies

    Console.CursorVisible = false; 
    Console.SetCursorPosition(0, 0); // Set the cursor position to (0, 0)
    StringBuilder sb = new(); 
    sb.AppendLine(); 
    sb.AppendLine("  Checkers"); // Add a game title
    sb.AppendLine(); 
    sb.AppendLine($"    ╔═══════════════════╗"); 
    sb.AppendLine($"  8 ║  {B(0, 7)} {B(1, 7)} {B(2, 7)} {B(3, 7)} {B(4, 7)} {B(5, 7)} {B(6, 7)} {B(7, 7)}  ║ {BlackPiece} = Black"); 
    sb.AppendLine($"  7 ║  {B(0, 6)} {B(1, 6)} {B(2, 6)} {B(3, 6)} {B(4, 6)} {B(5, 6)} {B(6, 6)} {B(7, 6)}  ║ {BlackKing} = Black King"); 
    sb.AppendLine($"  6 ║  {B(0, 5)} {B(1, 5)} {B(2, 5)} {B(3, 5)} {B(4, 5)} {B(5, 5)} {B(6, 5)} {B(7, 5)}  ║ {WhitePiece} = White"); 
    sb.AppendLine($"  5 ║  {B(0, 4)} {B(1, 4)} {B(2, 4)} {B(3, 4)} {B(4, 4)} {B(5, 4)} {B(6, 4)} {B(7, 4)}  ║ {WhiteKing} = White King"); 
    sb.AppendLine($"  4 ║  {B(0, 3)} {B(1, 3)} {B(2, 3)} {B(3, 3)} {B(4, 3)} {B(5, 3)} {B(6, 3)} {B(7, 3)}  ║"); 
    sb.AppendLine($"  3 ║  {B(0, 2)} {B(1, 2)} {B(2, 2)} {B(3, 2)} {B(4, 2)} {B(5, 2)} {B(6, 2)} {B(7, 2)}  ║ Taken:"); 
    sb.AppendLine($"  2 ║  {B(0, 1)} {B(1, 1)} {B(2, 1)} {B(3, 1)} {B(4, 1)} {B(5, 1)} {B(6, 1)} {B(7, 1)}  ║ {game.TakenCount(White),2} x {WhitePiece}"); 
    sb.AppendLine($"  1 ║  {B(0, 0)} {B(1, 0)} {B(2, 0)} {B(3, 0)} {B(4, 0)} {B(5, 0)} {B(6, 0)} {B(7, 0)}  ║ {game.TakenCount(Black),2} x {BlackPiece}"); 
    sb.AppendLine($"    ╚═══════════════════╝"); 
    sb.AppendLine($"       A B C D E F G H"); 
    sb.AppendLine(); 
    if (selection is not null) 
    {
        sb.Replace(" $ ", $"[{ToChar(game.Board[selection.Value.X, selection.Value.Y])}]"); // Replace the display of the selected position
    }
    if (from is not null) // If there is a starting point
    {
        char fromChar = ToChar(game.Board[from.Value.X, from.Value.Y]); // Get the chess piece character at the starting position
        sb.Replace(" @ ", $"<{fromChar}>"); 
        sb.Replace("@ ",  $"{fromChar}>"); 
        sb.Replace(" @",  $"<{fromChar}"); 
    }
    PieceColor? wc = game.Winner; // Get the winner
    PieceColor? mc = playerMoved?.Color; // Get the color of the player moving
    PieceColor? tc = game.Turn; // Get the player's color for the current round
    string w = $"  *** {wc} wins ***"; // Winning Information
    string m = $"  {mc} moved       "; // Mobile information
    string t = $"  {tc}'s turn      "; 
    sb.AppendLine(
        game.Winner is not null ? w : // If there is a winner, display the winning information
        playerMoved is not null ? m : // If a player moves, display the movement information
        t); // Otherwise, display the current round information
    string p = "  Press any key to continue..."; 
    string s = "                              "; 
    sb.AppendLine(promptPressKey ? p : s); 
    Console.Write(sb); 

    char B(int x, int y) => // Get the character at the specified position on the chessboard
        (x, y) == selection ? '$' : 
        (x, y) == from ? '@' : 
        ToChar(game.Board[x, y]); 

    static char ToChar(Piece? piece) => // Convert chess pieces to characters
        piece is null ? Vacant : // If the chess piece is empty, return the empty space character
        (piece.Color, piece.Promoted) switch // Returns the corresponding character according to the color of the chess piece and whether it is upgraded
        {
            (Black, false) => BlackPiece, // Black chess piece
            (Black, true)  => BlackKing, // Black King
            (White, false) => WhitePiece, // White chess pieces
            (White, true)  => WhiteKing, // White King
            _ => throw new NotImplementedException(), // Other situations show abnormality
        };
}

// Human player chooses to move
(int X, int Y)? HumanMoveSelection(Game game, (int X, int y)? selectionStart = null, (int X, int Y)? from = null)
{
    (int X, int Y) selection = selectionStart ?? (3, 3); // Initialize the selection position to the starting point or the default position (3, 3)
    while (true)
    {
        RenderGameState(game, selection: selection, from: from); 
        switch (Console.ReadKey(true).Key) // Read user keystrokes
        {
            case ConsoleKey.DownArrow:  selection.Y = Math.Max(0, selection.Y - 1); break; 
            case ConsoleKey.UpArrow:    selection.Y = Math.Min(7, selection.Y + 1); break; 
            case ConsoleKey.LeftArrow:  selection.X = Math.Max(0, selection.X - 1); break; 
            case ConsoleKey.RightArrow: selection.X = Math.Min(7, selection.X + 1); break; 
            case ConsoleKey.Enter:      return selection; // Press the Enter key to return to the selected location.
            case ConsoleKey.Escape:     return null; 
        }
    }
}