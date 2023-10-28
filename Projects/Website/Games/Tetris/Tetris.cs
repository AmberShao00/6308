using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace Website.Games.Tetris;

public class Tetris
{
	public readonly BlazorConsole Console = new();

	public async Task Run()
	{
		Console.OutputEncoding = Encoding.UTF8;
		Console.CursorVisible = false;
		Stopwatch Stopwatch = Stopwatch.StartNew();

		string[] FIELD = new[]
		{
			"╭──────────────────────────────╮",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"│                              │",
			"╰──────────────────────────────╯"
		};

		string[] NEXTTETROMINO = new[]
		{
			"╭─────────╮",
			"│         │",
			"│         │",
			"│         │",
			"│         │",
			"│         │",
			"│         │",
			"│         │",
			"│         │",
			"╰─────────╯"
		};

		string[] SCORE = new[]{
			"╭─────────╮",
			"│         │",
			"╰─────────╯"
		};

		string[] PAUSE = new[]{
			"█████╗ ███╗ ██╗██╗█████╗█████╗",
			"██╔██║██╔██╗██║██║██╔══╝██╔══╝",
			"█████║█████║██║██║ ███╗ █████╗",
			"██╔══╝██╔██║██║██║   ██╗██╔══╝",
			"██║   ██║██║█████║█████║█████╗",
			"╚═╝   ╚═╝╚═╝╚════╝╚════╝╚════╝",
		};

		string[][] TETROMINOS = new[]
		{
			new[]{
				"╭─╮",
				"╰─╯",
				"╭─╮",
				"╰─╯",
				"╭─╮",
				"╰─╯",
				"╭─╮",
				"╰─╯"
			},
			new[]{
				"╭─╮      ",
				"╰─╯      ",
				"╭─╮╭─╮╭─╮",
				"╰─╯╰─╯╰─╯"
			},
			new[]{
				"      ╭─╮",
				"      ╰─╯",
				"╭─╮╭─╮╭─╮",
				"╰─╯╰─╯╰─╯"
			},
			new[]{
				"╭─╮╭─╮",
				"╰─╯╰─╯",
				"╭─╮╭─╮",
				"╰─╯╰─╯"
			},
			new[]{
				"   ╭─╮╭─╮",
				"   ╰─╯╰─╯",
				"╭─╮╭─╮   ",
				"╰─╯╰─╯   "
			},
			new[]{
				"   ╭─╮   ",
				"   ╰─╯   ",
				"╭─╮╭─╮╭─╮",
				"╰─╯╰─╯╰─╯"
			},
			new[]{
				"╭─╮╭─╮   ",
				"╰─╯╰─╯   ",
				"   ╭─╮╭─╮",
				"   ╰─╯╰─╯"
			},
		};

		string[] PLAYFIELD = (string[])FIELD.Clone();
		const int BORDER = 1;
		int FallSpeedMilliSeconds = 1000;
		bool CloseGame = false;
		int Score = 0;
		int PauseCount = 0;
		GameStatus GameStatus = GameStatus.Gameover;

		Random RamdomGenerator = new();

		int INITIALTETROMINOX = Convert.ToInt16(PLAYFIELD[0].Length / 2) - 3;
		int INITIALTETROMINOY = 1;
		Tetromino TETROMINO = new()
		{
			Shape = TETROMINOS[RamdomGenerator.Next(0, TETROMINOS.Length)],
			Next = TETROMINOS[RamdomGenerator.Next(0, TETROMINOS.Length)],
			X = INITIALTETROMINOX,
			Y = INITIALTETROMINOY
		};

		AutoResetEvent AutoEvent = new AutoResetEvent(false);
		Timer? FallTimer = null;
		GameStatus = GameStatus.Playing;

		await Console.WriteLine();
		await Console.WriteLine("  ██████╗█████╗██████╗█████╗ ██╗█████╗");
		await Console.WriteLine("  ╚═██╔═╝██╔══╝╚═██╔═╝██╔═██╗██║██╔══╝");
		await Console.WriteLine("    ██║  █████╗  ██║  █████╔╝██║ ███╗ ");
		await Console.WriteLine("    ██║  ██╔══╝  ██║  ██╔═██╗██║   ██╗");
		await Console.WriteLine("    ██║  █████╗  ██║  ██║ ██║██║█████║");
		await Console.WriteLine("    ╚═╝  ╚════╝  ╚═╝  ╚═╝ ╚═╝╚═╝╚════╝");

		await Console.WriteLine();
		await Console.WriteLine("  Controls:");
		await Console.WriteLine("  WASD or ARROW to move");
		await Console.WriteLine("  Q or E to spin left or right");
		await Console.WriteLine("  P to paused the game, press enter");
		await Console.WriteLine("  key to resume");
		await Console.WriteLine();
		await Console.Write("  Press enter to start tetris...");
		Console.CursorVisible = false;
		await StartGame();
		await Console.Clear();

		FallTimer = new Timer(TetrominoFall, AutoEvent, FallSpeedMilliSeconds, FallSpeedMilliSeconds);

		while (!CloseGame)
		{
			if (CloseGame)
			{
				break;
			}

			await PlayerControl();
			if (GameStatus == GameStatus.Playing)
			{
				await DrawFrame();
				await SleepAfterRender();
			}
		}

		async Task PlayerControl()
		{
			while (await Console.KeyAvailable() && GameStatus == GameStatus.Playing)
			{
				switch ((await Console.ReadKey(true)).Key)
				{
					case ConsoleKey.A or ConsoleKey.LeftArrow:
						if (Collision(Direction.Left)) break;
						TETROMINO.X -= 3;
						break;
					case ConsoleKey.D or ConsoleKey.RightArrow:
						if (Collision(Direction.Right)) break;
						TETROMINO.X += 3;
						break;
					case ConsoleKey.S or ConsoleKey.DownArrow:
						FallTimer.Change(0, FallSpeedMilliSeconds);
						break;
					case ConsoleKey.E:
						TetrominoSpin(Direction.Right);
						break;
					case ConsoleKey.Q:
						TetrominoSpin(Direction.Left);
						break;
					case ConsoleKey.P:
						PauseGame();
						break;
				}
			}
		}

		async Task DrawFrame()
		{
			bool collision = false;
			int yScope = TETROMINO.Y;
			string[] shapeScope = (string[])TETROMINO.Shape.Clone();
			string[] nextShapeScope = (string[])TETROMINO.Next.Clone();
			char[][] frame = new char[PLAYFIELD.Length][];

			//Field
			for (int y = 0; y < PLAYFIELD.Length; y++)
			{
				frame[y] = PLAYFIELD[y].ToCharArray();
			}

			//Draw Tetromino
			for (int y = 0; y < shapeScope.Length && !collision; y++)
			{
				for (int x = 0; x < shapeScope[y].Length; x++)
				{
					int tY = yScope + y;
					int tX = TETROMINO.X + x;
					char charToReplace = PLAYFIELD[tY][tX];
					char charTetromino = shapeScope[y][x];

					if (charTetromino == ' ') continue;

					if (charToReplace != ' ')
					{
						collision = true;
						break;
					}

					frame[tY][tX] = charTetromino;
				}
			}

			//Draw Preview
			for (int yField = PLAYFIELD.Length - shapeScope.Length - BORDER; yField >= 0; yField -= 2)
			{
				if (CollisionPreview(yField, yScope, shapeScope)) continue;

				for (int y = 0; y < shapeScope.Length && !collision; y++)
				{
					for (int x = 0; x < shapeScope[y].Length; x++)
					{
						int tY = yField + y;

						if (yScope + shapeScope.Length > tY) continue;

						int tX = TETROMINO.X + x;
						char charToReplace = PLAYFIELD[tY][tX];
						char charTetromino = shapeScope[y][x];

						if (charTetromino == ' ') continue;

						if (charToReplace != ' ')
						{
							collision = true;
							break;
						}

						frame[tY][tX] = '•';
					}
				}

				break;
			}

			//Next Square
			for (int y = 0; y < NEXTTETROMINO.Length; y++)
			{
				frame[y] = frame[y].Concat(NEXTTETROMINO[y]).ToArray();
			}

			//Score Square
			for (int y = 0; y < SCORE.Length; y++)
			{
				int sY = NEXTTETROMINO.Length + y;
				frame[sY] = frame[sY].Concat(SCORE[y]).ToArray();
			}

			//Draw Next
			for (int y = 0; y < nextShapeScope.Length; y++)
			{
				for (int x = 0; x < nextShapeScope[y].Length; x++)
				{
					int tY = y + BORDER;
					int tX = PLAYFIELD[y].Length + x + BORDER;
					char charTetromino = nextShapeScope[y][x];
					frame[tY][tX] = charTetromino;
				}
			}

			//Draw Score
			char[] score = Score.ToString().ToCharArray();
			for (int scoreX = score.Length - 1; scoreX >= 0; scoreX--)
			{
				int sY = NEXTTETROMINO.Length + BORDER;
				int sX = frame[sY].Length - (score.Length - scoreX) - BORDER;
				frame[sY][sX] = score[scoreX];
			}

			//Draw Pause
			if (GameStatus == GameStatus.Paused)
			{
				for (int y = 0; y < PAUSE.Length; y++)
				{
					int fY = (PLAYFIELD.Length / 2) + y - PAUSE.Length;
					for (int x = 0; x < PAUSE[y].Length; x++)
					{
						int fX = x + BORDER;

						if (x >= PLAYFIELD[fY].Length) break;

						frame[fY][fX] = PAUSE[y][x];
					}
				}
			}

			//Create Render
			StringBuilder render = new();
			for (int y = 0; y < frame.Length; y++)
			{
				render.AppendLine(new string(frame[y]));
			}

			await Console.Clear();
			await Console.Write(render);
			Console.CursorVisible = false;
		}

		char[][] DrawLastFrame(int yS)
		{
			bool collision = false;
			int yScope = yS - 2;
			int xScope = TETROMINO.X;
			string[] shapeScope = (string[])TETROMINO.Shape.Clone();
			string[] nextShapeScope = (string[])TETROMINO.Next.Clone();
			char[][] frame = new char[PLAYFIELD.Length][];

			//Field
			for (int y = 0; y < PLAYFIELD.Length; y++)
			{
				frame[y] = PLAYFIELD[y].ToCharArray();
			}

			//Draw Tetromino
			for (int y = 0; y < shapeScope.Length && !collision; y++)
			{
				for (int x = 0; x < shapeScope[y].Length; x++)
				{
					int tY = yScope + y;
					int tX = xScope + x;
					char charToReplace = PLAYFIELD[tY][tX];
					char charTetromino = shapeScope[y][x];

					if (charTetromino == ' ') continue;

					if (charToReplace != ' ')
					{
						collision = true;
						break;
					}

					frame[tY][tX] = charTetromino;
				}
			}

			return frame;
		}

		bool Collision(Direction direction)
		{
			int xNew = TETROMINO.X;
			int yScope = TETROMINO.Y;
			string[] shapeScope = (string[])TETROMINO.Shape.Clone();
			bool collision = false;

			switch (direction)
			{
				case Direction.Right:
					xNew += 3;
					if (xNew + shapeScope[0].Length > PLAYFIELD[0].Length - BORDER) collision = true;
					break;
				case Direction.Left:
					xNew -= 3;
					if (xNew < BORDER) collision = true;
					break;
				case Direction.None:
					break;
			}

			if (collision) return collision;

			for (int y = 0; y < shapeScope.Length && !collision; y++)
			{
				for (int x = 0; x < shapeScope[y].Length; x++)
				{
					int tY = yScope + y;
					int tX = xNew + x;
					char charToReplace = PLAYFIELD[tY][tX];
					char charTetromino = shapeScope[y][x];

					if (charTetromino == ' ') continue;

					if (charToReplace != ' ')
					{
						collision = true;
						break;
					}
				}
			}

			return collision;
		}

		bool CollisionPreview(int initY, int yScope, string[] shape)
		{
			int xNew = TETROMINO.X;

			for (int yUpper = initY; yUpper >= yScope; yUpper -= 2)
			{
				for (int y = shape.Length - 1; y >= 0; y -= 2)
				{
					for (int x = 0; x < shape[y].Length; x++)
					{
						int tY = yUpper + y;
						int tX = xNew + x;
						char charToReplace = PLAYFIELD[tY][tX];
						char charTetromino = shape[y][x];

						if (charTetromino == ' ') continue;

						if (charToReplace != ' ')
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		async void Gameover()
		{
			GameStatus = GameStatus.Gameover;
			AutoEvent.Dispose();
			FallTimer.Dispose();

			await SleepAfterRender();

			await Console.Clear();
			await Console.WriteLine();
			await Console.WriteLine("      ██████╗  █████╗ ██    ██╗█████╗");
			await Console.WriteLine("     ██╔════╝ ██╔══██╗███  ███║██╔══╝");
			await Console.WriteLine("     ██║  ███╗███████║██╔██═██║█████╗");
			await Console.WriteLine("     ██║   ██║██╔══██║██║   ██║██╔══╝");
			await Console.WriteLine("     ╚██████╔╝██║  ██║██║   ██║█████╗");
			await Console.WriteLine("      ╚═════╝ ╚═╝  ╚═╝╚═╝   ╚═╝╚════╝");
			await Console.WriteLine("       ██████╗██╗  ██╗█████╗█████╗   ");
			await Console.WriteLine("       ██  ██║██║  ██║██╔══╝██╔═██╗  ");
			await Console.WriteLine("       ██  ██║██║  ██║█████╗█████╔╝  ");
			await Console.WriteLine("       ██  ██║╚██╗██╔╝██╔══╝██╔═██╗  ");
			await Console.WriteLine("       ██████║ ╚███╔╝ █████╗██║ ██║  ");
			await Console.WriteLine("       ╚═════╝  ╚══╝  ╚════╝╚═╝ ╚═╝  ");

			await Console.WriteLine();
			await Console.WriteLine($"     Final Score: {Score}");
			await Console.WriteLine($"     Pause Count: {PauseCount}");
			await Console.WriteLine();
			await Console.WriteLine("     Press enter to play again");
			await Console.WriteLine("     Press escape to close the game");
			Console.CursorVisible = false;
			await StartGame();
			RestartGame();
		}

		async Task StartGame(ConsoleKey key = ConsoleKey.Enter)
		{
			ConsoleKey input = default;
			while (input != key && !CloseGame)
			{
				input = (await Console.ReadKey(true)).Key;
				if (input is ConsoleKey.Escape)
				{
					CloseGame = true;
					return;
				}
			}
		}

		void RestartGame()
		{
			PLAYFIELD = (string[])FIELD.Clone();
			FallSpeedMilliSeconds = 1000;
			Score = 0;
			TETROMINO = new()
			{
				Shape = TETROMINOS[RamdomGenerator.Next(0, TETROMINOS.Length)],
				Next = TETROMINOS[RamdomGenerator.Next(0, TETROMINOS.Length)],
				X = INITIALTETROMINOX,
				Y = INITIALTETROMINOY
			};

			AutoEvent = new AutoResetEvent(false);
			FallTimer = new Timer(TetrominoFall, AutoEvent, FallSpeedMilliSeconds, FallSpeedMilliSeconds);
			GameStatus = GameStatus.Playing;
		}

		async void PauseGame()
		{
			PauseCount++;
			FallTimer.Change(Timeout.Infinite, Timeout.Infinite);
			GameStatus = GameStatus.Paused;
			await DrawFrame();

			await ResumeGame();
		}

		async Task ResumeGame(ConsoleKey key = ConsoleKey.Enter)
		{
			ConsoleKey input = default;
			while (input != key && !CloseGame)
			{
				input = (await Console.ReadKey(true)).Key;
				if (input is ConsoleKey.Enter && GameStatus == GameStatus.Paused && FallTimer != null)
				{
					FallTimer.Change(0, FallSpeedMilliSeconds);
					GameStatus = GameStatus.Playing;
					return;
				}
			}
		}

		void AddScoreChangeSpeed(int value)
		{
			Score += value;

			if (Score > 100) return;

			switch (Score)
			{
				case 10:
					FallSpeedMilliSeconds = 900;
					break;
				case 20:
					FallSpeedMilliSeconds = 800;
					break;
				case 30:
					FallSpeedMilliSeconds = 700;
					break;
				case 40:
					FallSpeedMilliSeconds = 500;
					break;
				case 50:
					FallSpeedMilliSeconds = 300;
					break;
				case 60:
					FallSpeedMilliSeconds = 200;
					break;
				case 70:
					FallSpeedMilliSeconds = 100;
					break;
				case 100:
					FallSpeedMilliSeconds = 50;
					break;
			}
		}

		void TetrominoFall(object? e)
		{
			int yAfterFall = TETROMINO.Y;
			bool collision = false;

			if (TETROMINO.Y + TETROMINO.Shape.Length + 2 > PLAYFIELD.Length) yAfterFall = PLAYFIELD.Length - TETROMINO.Shape.Length + 1;
			else yAfterFall += 2;

			//Y Collision
			for (int xCollision = 0; xCollision < TETROMINO.Shape[0].Length;)
			{
				for (int yCollision = TETROMINO.Shape.Length - 1; yCollision >= 0; yCollision -= 2)
				{
					char exist = TETROMINO.Shape[yCollision][xCollision];

					if (exist == ' ') continue;

					char[] lineYC = PLAYFIELD[yAfterFall + yCollision - 1].ToCharArray();

					if (TETROMINO.X + xCollision < 0 || TETROMINO.X + xCollision > lineYC.Length) continue;

					if
					(
						lineYC[TETROMINO.X + xCollision] != ' ' &&
						lineYC[TETROMINO.X + xCollision] != '│'
					)
					{
						char[][] lastFrame = DrawLastFrame(yAfterFall);
						for (int y = 0; y < lastFrame.Length; y++)
						{
							PLAYFIELD[y] = new string(lastFrame[y]);
						}

						TETROMINO.X = INITIALTETROMINOX;
						TETROMINO.Y = INITIALTETROMINOY;
						TETROMINO.Shape = TETROMINO.Next;
						TETROMINO.Next = TETROMINOS[RamdomGenerator.Next(0, TETROMINOS.Length)];

						xCollision = TETROMINO.Shape[0].Length;
						collision = true;
						break;
					}
				}

				xCollision += 3;
			}

			if (!collision) TETROMINO.Y = yAfterFall;

			//Clean Lines
			for (var lineIndex = PLAYFIELD.Length - 1; lineIndex >= 0; lineIndex--)
			{
				string line = PLAYFIELD[lineIndex];
				bool notCompleted = line.Any(e => e == ' ');

				if (lineIndex == 0 || lineIndex == PLAYFIELD.Length - 1) continue;

				if (!notCompleted)
				{
					PLAYFIELD[lineIndex] = "│                              │";
					AddScoreChangeSpeed(1);

					for (int lineM = lineIndex; lineM >= 1; lineM--)
					{
						if (PLAYFIELD[lineM - 1] == "╭──────────────────────────────╮")
						{
							PLAYFIELD[lineM] = "│                              │";
							continue;
						}

						PLAYFIELD[lineM] = PLAYFIELD[lineM - 1];
					}

					lineIndex++;
				}
			}

			//VerifiedCollision 
			if (Collision(Direction.None) && FallTimer != null) Gameover();
		}

		void TetrominoSpin(Direction spinDirection)
		{
			string[] shapeScope = (string[])TETROMINO.Shape.Clone();
			int yScope = TETROMINO.Y;
			string[] newShape = new string[shapeScope[0].Length / 3 * 2];
			int newY = 0;
			int rowEven = 0;
			int rowOdd = 1;

			//Turn
			for (int y = 0; y < shapeScope.Length;)
			{
				switch (spinDirection)
				{
					case Direction.Right:
						SpinRight(newShape, shapeScope, ref newY, rowEven, rowOdd, y);
						break;
					case Direction.Left:
						SpinLeft(newShape, shapeScope, ref newY, rowEven, rowOdd, y);
						break;
				}

				newY = 0;
				rowEven += 2;
				rowOdd += 2;
				y += 2;
			}

			//Verified Collision
			for (int y = 0; y < newShape.Length - 1; y++)
			{
				for (int x = 0; x < newShape[y].Length; x++)
				{
					if (newShape[y][x] == ' ') continue;

					char c = PLAYFIELD[yScope + y][TETROMINO.X + x];
					if (c != ' ') return;
				}
			}

			TETROMINO.Shape = newShape;
		}

		void SpinLeft(string[] newShape, string[] shape, ref int newY, int rowEven, int rowOdd, int y)
		{
			for (int x = shape[y].Length - 1; x >= 0; x -= 3)
			{
				for (int xS = 2; xS >= 0; xS--)
				{
					newShape[newY] += shape[rowEven][x - xS];
					newShape[newY + 1] += shape[rowOdd][x - xS];
				}

				newY += 2;
			}
		}

		void SpinRight(string[] newShape, string[] shape, ref int newY, int rowEven, int rowOdd, int y)
		{
			for (int x = 2; x < shape[y].Length; x += 3)
			{
				if (newShape[newY] == null)
				{
					newShape[newY] = "";
					newShape[newY + 1] = "";
				}

				for (int xS = 0; xS <= 2; xS++)
				{
					newShape[newY] = newShape[newY].Insert(0, shape[rowEven][x - xS].ToString());
					newShape[newY + 1] = newShape[newY + 1].Insert(0, shape[rowOdd][x - xS].ToString());
				}

				newY += 2;
			}
		}

		async Task SleepAfterRender()
		{
			TimeSpan sleep = TimeSpan.FromSeconds(1d / 60d) - Stopwatch.Elapsed;
			if (sleep > TimeSpan.Zero)
			{
				await Console.RefreshAndDelay(sleep);
			}
			Stopwatch.Restart();
		}

	}

	class Tetromino
	{
		public required string[] Shape { get; set; }
		public required string[] Next { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
	}

	enum Direction
	{
		Right,
		Left,
		None
	}

	enum GameStatus
	{
		Gameover,
		Playing,
		Paused
	}

}
