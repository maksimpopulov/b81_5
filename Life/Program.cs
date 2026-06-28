using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Drawing.Imaging;

namespace GameOfLife
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;

        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }

        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }
        public int Generation { get; set; }
        private readonly Random rand = new Random();

        public Board(int width, int height, int cellSize, double liveDensity = 0.1)
        {
            CellSize = cellSize;
            Generation = 0;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public Board(int columns, int rows)
        {
            CellSize = 1;
            Generation = 0;
            Cells = new Cell[columns, rows];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
        }

        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
            {
                cell.IsAlive = rand.NextDouble() < liveDensity;
            }
        }

        public void Advance()
        {
            foreach (var cell in Cells)
            {
                cell.DetermineNextLiveState();
            }
            foreach (var cell in Cells)
            {
                cell.Advance();
            }
            Generation++;
        }

        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public int GetAliveCount()
        {
            int count = 0;
            foreach (var cell in Cells)
                if (cell.IsAlive) count++;
            return count;
        }

        public void SaveToFile(string filename)
        {
            var state = new BoardState
            {
                Columns = Columns,
                Rows = Rows,
                CellSize = CellSize,
                Generation = Generation,
                Cells = new List<List<bool>>()
            };

            for (int y = 0; y < Rows; y++)
            {
                var row = new List<bool>();
                for (int x = 0; x < Columns; x++)
                {
                    row.Add(Cells[x, y].IsAlive);
                }
                state.Cells.Add(row);
            }

            string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, json);
        }

        public void LoadFromFile(string filename)
        {
            string json = File.ReadAllText(filename);
            var state = JsonSerializer.Deserialize<BoardState>(json);

            if (state.Columns != Columns || state.Rows != Rows)
                throw new InvalidOperationException("Board dimensions don't match");

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    Cells[x, y].IsAlive = state.Cells[y][x];
                }
            }
            Generation = state.Generation;
        }

        public void LoadPattern(string[] pattern, int offsetX, int offsetY)
        {
            for (int y = 0; y < pattern.Length; y++)
            {
                for (int x = 0; x < pattern[y].Length; x++)
                {
                    if (offsetX + x < Columns && offsetY + y < Rows)
                    {
                        Cells[offsetX + x, offsetY + y].IsAlive = pattern[y][x] == '*';
                    }
                }
            }
        }

        public List<List<Cell>> FindClusters()
        {
            var visited = new bool[Columns, Rows];
            var clusters = new List<List<Cell>>();

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var cluster = new List<Cell>();
                        var queue = new Queue<(int, int)>();
                        queue.Enqueue((x, y));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            var (cx, cy) = queue.Dequeue();
                            cluster.Add(Cells[cx, cy]);

                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    int nx = cx + dx;
                                    int ny = cy + dy;
                                    if (nx >= 0 && nx < Columns && ny >= 0 && ny < Rows)
                                    {
                                        if (Cells[nx, ny].IsAlive && !visited[nx, ny])
                                        {
                                            visited[nx, ny] = true;
                                            queue.Enqueue((nx, ny));
                                        }
                                    }
                                }
                            }
                        }
                        clusters.Add(cluster);
                    }
                }
            }
            return clusters;
        }

        public string ClassifyCluster(List<Cell> cluster)
        {
            if (cluster.Count == 0) return "Empty";

            var pattern = GetClusterPattern(cluster);

            if (cluster.Count == 3 && IsPattern(pattern, new[] { "**", "* " })) return "Block";
            if (cluster.Count == 4 && IsPattern(pattern, new[] { "**", "**" })) return "Block";
            if (cluster.Count == 4 && IsPattern(pattern, new[] { " **", "** " })) return "Tub";
            if (cluster.Count == 6 && IsPattern(pattern, new[] { "***", "* *", "***" })) return "Beehive";
            if (cluster.Count == 6 && IsPattern(pattern, new[] { " ** ", "*  *", " ** " })) return "Beehive";
            if (cluster.Count == 8 && IsPattern(pattern, new[] { " ** ", "*  *", "*  *", " ** " })) return "Loaf";
            if (cluster.Count == 8 && IsPattern(pattern, new[] { " ****", "**** " })) return "Boat";
            if (cluster.Count == 3 && IsPattern(pattern, new[] { " * ", "* *", " * " })) return "Blinker (periodic)";
            if (cluster.Count == 4 && IsPattern(pattern, new[] { "*  ", "** ", " **" })) return "Toad (periodic)";
            if (cluster.Count == 8 && IsPattern(pattern, new[] { " ***", "*** " })) return "Beacon (periodic)";
            if (cluster.Count == 5 && IsPattern(pattern, new[] { "  *", "***", "*  " })) return "Glider (moving)";
            if (cluster.Count == 5 && IsPattern(pattern, new[] { " * ", "  *", "***" })) return "Glider (moving)";
            if (cluster.Count == 12 && IsPattern(pattern, new[] { "  **", " ** ", " ** ", "  **" })) return "LWSS (moving)";

            return $"Unknown ({cluster.Count} cells)";
        }

        private string GetClusterPattern(List<Cell> cluster)
        {
            int minX = Columns, minY = Rows, maxX = 0, maxY = 0;
            foreach (var cell in cluster)
            {
                for (int x = 0; x < Columns; x++)
                    for (int y = 0; y < Rows; y++)
                        if (Cells[x, y] == cell)
                        {
                            minX = Math.Min(minX, x);
                            minY = Math.Min(minY, y);
                            maxX = Math.Max(maxX, x);
                            maxY = Math.Max(maxY, y);
                        }
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            var pattern = new char[height][];
            for (int i = 0; i < height; i++)
                pattern[i] = new string('.', width).ToCharArray();

            foreach (var cell in cluster)
            {
                for (int x = 0; x < Columns; x++)
                    for (int y = 0; y < Rows; y++)
                        if (Cells[x, y] == cell)
                            pattern[y - minY][x - minX] = '*';
            }

            return string.Join("\n", pattern.Select(row => new string(row)));
        }

        private bool IsPattern(string actual, string[] expected)
        {
            return actual.Trim() == string.Join("\n", expected).Trim();
        }

        public Dictionary<string, int> ClassifyAllFigures()
        {
            var clusters = FindClusters();
            var classification = new Dictionary<string, int>();

            foreach (var cluster in clusters)
            {
                string type = ClassifyCluster(cluster);
                if (!classification.ContainsKey(type))
                    classification[type] = 0;
                classification[type]++;
            }

            return classification;
        }
    }

    public class BoardState
    {
        public int Columns { get; set; }
        public int Rows { get; set; }
        public int CellSize { get; set; }
        public int Generation { get; set; }
        public List<List<bool>> Cells { get; set; }
    }

    public class Settings
    {
        public int Width { get; set; } = 80;
        public int Height { get; set; } = 40;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.3;
        public int SleepMs { get; set; } = 100;
        public int MaxGenerations { get; set; } = 1000;
        public int StableThreshold { get; set; } = 5;
    }

    class Program
    {
        static Board board;
        static Settings settings;
        static string settingsFile = "Settings.json";
        static int generation = 0;
        static List<int> aliveHistory = new List<int>();
        static bool isStable = false;

        static void LoadSettings()
        {
            if (File.Exists(settingsFile))
            {
                string json = File.ReadAllText(settingsFile);
                settings = JsonSerializer.Deserialize<Settings>(json);
            }
            else
            {
                settings = new Settings();
                SaveSettings();
            }
        }

        static void SaveSettings()
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsFile, json);
        }

        static void Reset()
        {
            board = new Board(settings.Width, settings.Height, settings.CellSize, settings.LiveDensity);
            generation = 0;
            aliveHistory.Clear();
            isStable = false;
        }

        static void Render()
        {
            Console.Clear();
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    Console.Write(cell.IsAlive ? '*' : ' ');
                }
                Console.Write('\n');
            }
            Console.WriteLine($"\nGeneration: {generation} | Alive cells: {board.GetAliveCount()} | Stable: {isStable}");
        }

        static void AnalyzeAndReport()
        {
            Console.WriteLine("\n=== Analysis Report ===");
            var clusters = board.FindClusters();
            Console.WriteLine($"Total clusters: {clusters.Count}");
            Console.WriteLine($"Total alive cells: {board.GetAliveCount()}");

            var classification = board.ClassifyAllFigures();
            Console.WriteLine("\nClassification:");
            foreach (var kvp in classification)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void SaveState()
        {
            string filename = $"save_gen_{generation}.json";
            board.SaveToFile(filename);
            Console.WriteLine($"State saved to {filename}");
            Thread.Sleep(500);
        }

        static void LoadState()
        {
            Console.Write("Enter filename to load: ");
            string filename = Console.ReadLine();
            if (File.Exists(filename))
            {
                board.LoadFromFile(filename);
                generation = board.Generation;
                Console.WriteLine($"State loaded from {filename}");
                Thread.Sleep(500);
            }
            else
            {
                Console.WriteLine("File not found!");
                Thread.Sleep(1000);
            }
        }

        static void LoadPattern()
        {
            Console.WriteLine("Available patterns:");
            Console.WriteLine("1. Glider (движущаяся)");
            Console.WriteLine("2. Blinker (периодическая)");
            Console.WriteLine("3. Block (стабильная)");
            Console.WriteLine("4. Beehive (стабильная)");
            Console.WriteLine("5. Gosper Glider Gun (ружье)");
            Console.WriteLine("6. LWSS (движущаяся)");
            Console.WriteLine("7. Toad (периодическая)");
            Console.Write("Choose pattern: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    board.LoadPattern(new[] { " * ", "  *", "***" }, 10, 5);
                    break;
                case "2":
                    board.LoadPattern(new[] { "***" }, 20, 10);
                    break;
                case "3":
                    board.LoadPattern(new[] { "**", "**" }, 15, 8);
                    break;
                case "4":
                    board.LoadPattern(new[] { " ** ", "*  *", " ** " }, 20, 8);
                    break;
                case "5":
                    board.LoadPattern(new[] {
                        "........................*",
                        "......................*.*",
                        "............**......**............**",
                        "...........*...*....**............**",
                        "**........*.....*...**",
                        "**........*...*.**....*.*",
                        "..........*.....*.......*",
                        "...........*...*",
                        "............**"
                    }, 5, 5);
                    break;
                case "6":
                    board.LoadPattern(new[] { "  **", " ** ", " ** ", "  **" }, 15, 8);
                    break;
                case "7":
                    board.LoadPattern(new[] { "***", " **" }, 20, 10);
                    break;
                default:
                    Console.WriteLine("Invalid choice!");
                    break;
            }
            generation = 0;
            aliveHistory.Clear();
        }

        static void Main(string[] args)
        {
            Console.Title = "Conway's Game of Life";
            LoadSettings();
            Reset();

            Console.WriteLine("=== Conway's Game of Life ===");
            Console.WriteLine("Controls:");
            Console.WriteLine("  ESC - Exit");
            Console.WriteLine("  R - Reset");
            Console.WriteLine("  S - Save state (JSON)");
            Console.WriteLine("  L - Load state");
            Console.WriteLine("  P - Load pattern");
            Console.WriteLine("  A - Analyze current state");
            Console.WriteLine("  Space - Pause/Resume");
            Console.WriteLine("\nPress any key to start...");
            Console.ReadKey();

            bool running = true;
            bool paused = false;

            while (running)
            {
                if (!paused)
                {
                    Render();

                    if (!isStable && generation > 10)
                    {
                        int recentWindow = Math.Min(10, aliveHistory.Count);
                        if (aliveHistory.Count >= recentWindow)
                        {
                            var recent = aliveHistory.Skip(aliveHistory.Count - recentWindow).ToList();
                            isStable = recent.All(x => x == recent[0]);
                        }
                    }

                    board.Advance();
                    generation++;
                    aliveHistory.Add(board.GetAliveCount());

                    if (generation > settings.MaxGenerations)
                    {
                        Console.WriteLine($"\nReached max generations ({settings.MaxGenerations})");
                        paused = true;
                    }
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Escape:
                            running = false;
                            break;
                        case ConsoleKey.R:
                            Reset();
                            break;
                        case ConsoleKey.S:
                            SaveState();
                            break;
                        case ConsoleKey.L:
                            LoadState();
                            break;
                        case ConsoleKey.P:
                            LoadPattern();
                            break;
                        case ConsoleKey.A:
                            AnalyzeAndReport();
                            break;
                        case ConsoleKey.Spacebar:
                            paused = !paused;
                            if (paused)
                                Console.WriteLine("\nPAUSED");
                            break;
                    }
                }

                Thread.Sleep(settings.SleepMs);
            }
        }
    }
}
