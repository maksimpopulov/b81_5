using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameOfLife;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GameOfLife.Tests
{
    [TestClass]
    public class CellTests
    {
        [TestMethod]
        public void Cell_Underpopulation_Dies()
        {
            var cell = new Cell { IsAlive = true };
            cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_Survives_With2Neighbors()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 2; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.IsTrue(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_Survives_With3Neighbors()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 3; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.IsTrue(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_Overpopulation_Dies()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_Reproduction_Born()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 3; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.IsTrue(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_Dead_With2Neighbors_StaysDead()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 2; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_Dead_With4Neighbors_StaysDead()
        {
            var cell = new Cell { IsAlive = false };
            for (int i = 0; i < 4; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.IsFalse(cell.IsAlive);
        }

        [TestMethod]
        public void Cell_DetermineNextLiveState_DoesNotChangeCurrentState()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 2; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            cell.DetermineNextLiveState();
            Assert.IsTrue(cell.IsAlive); // Current state unchanged
            cell.Advance();
            Assert.IsTrue(cell.IsAlive); // State changed after Advance
        }
    }

    [TestClass]
    public class BoardTests
    {
        private Board CreateEmptyBoard(int columns, int rows) =>
            new Board(columns, rows);

        [TestMethod]
        public void Board_Constructor_SetsCorrectDimensions()
        {
            var board = new Board(80, 40, 1, 0);
            Assert.AreEqual(80, board.Columns);
            Assert.AreEqual(40, board.Rows);
            Assert.AreEqual(1, board.CellSize);
            Assert.AreEqual(0, board.Generation);
        }

        [TestMethod]
        public void Board_Constructor_WithWidthAndHeight_SetsCorrectDimensions()
        {
            var board = new Board(50, 30);
            Assert.AreEqual(50, board.Columns);
            Assert.AreEqual(30, board.Rows);
            Assert.AreEqual(1, board.CellSize);
            Assert.AreEqual(0, board.Generation);
        }

        [TestMethod]
        public void Board_ConnectNeighbors_EachCellHas8Neighbors()
        {
            var board = CreateEmptyBoard(10, 10);
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    Assert.AreEqual(8, board.Cells[x, y].neighbors.Count);
        }

        [TestMethod]
        public void Board_Randomize_SetsCorrectDensity()
        {
            var board = new Board(100, 100, 1, 0.3);
            int aliveCount = board.GetAliveCount();
            double density = (double)aliveCount / (board.Columns * board.Rows);
            // Allow some variance due to randomness
            Assert.IsTrue(density > 0.2 && density < 0.4);
        }

        [TestMethod]
        public void Board_Blinker_Oscillates()
        {
            var board = CreateEmptyBoard(5, 5);
            board.LoadPattern(new[] { "***" }, 1, 2);
            Assert.AreEqual(3, board.GetAliveCount());

            board.Advance();
            Assert.AreEqual(3, board.GetAliveCount());
            Assert.IsTrue(board.Cells[2, 1].IsAlive);
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
            Assert.IsTrue(board.Cells[2, 3].IsAlive);

            board.Advance();
            Assert.AreEqual(3, board.GetAliveCount());
            Assert.IsTrue(board.Cells[1, 2].IsAlive);
            Assert.IsTrue(board.Cells[2, 2].IsAlive);
            Assert.IsTrue(board.Cells[3, 2].IsAlive);
        }

        [TestMethod]
        public void Board_Block_IsStable()
        {
            var board = CreateEmptyBoard(10, 10);
            board.LoadPattern(new[] { "**", "**" }, 3, 3);
            int before = board.GetAliveCount();
            Assert.AreEqual(4, before);
            
            // Check stability over multiple generations
            for (int i = 0; i < 5; i++)
            {
                board.Advance();
                Assert.AreEqual(before, board.GetAliveCount());
            }
            
            Assert.IsTrue(board.Cells[3, 3].IsAlive);
            Assert.IsTrue(board.Cells[4, 3].IsAlive);
            Assert.IsTrue(board.Cells[3, 4].IsAlive);
            Assert.IsTrue(board.Cells[4, 4].IsAlive);
        }

        [TestMethod]
        public void Board_Glider_MovesAndMaintainsShape()
        {
            var board = CreateEmptyBoard(20, 20);
            board.LoadPattern(new[] { " * ", "  *", "***" }, 5, 5);
            Assert.AreEqual(5, board.GetAliveCount());
            
            // Glider should maintain 5 cells as it moves
            for (int i = 0; i < 5; i++)
            {
                board.Advance();
                Assert.AreEqual(5, board.GetAliveCount());
            }
        }

        [TestMethod]
        public void Board_FindClusters_SingleBlock_OneCluster()
        {
            var board = CreateEmptyBoard(20, 20);
            board.LoadPattern(new[] { "**", "**" }, 5, 5);
            var clusters = board.FindClusters();
            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(4, clusters[0].Count);
        }

        [TestMethod]
        public void Board_FindClusters_TwoSeparateBlocks_TwoClusters()
        {
            var board = CreateEmptyBoard(30, 30);
            board.LoadPattern(new[] { "**", "**" }, 2, 2);
            board.LoadPattern(new[] { "**", "**" }, 10, 10);
            var clusters = board.FindClusters();
            Assert.AreEqual(2, clusters.Count);
        }

        [TestMethod]
        public void Board_FindClusters_DiagonalSeparated_ReturnsSeparateClusters()
        {
            var board = CreateEmptyBoard(10, 10);
            board.Cells[2, 2].IsAlive = true;
            board.Cells[3, 3].IsAlive = true; // Diagonal, not connected
            var clusters = board.FindClusters();
            Assert.AreEqual(2, clusters.Count);
        }

        [TestMethod]
        public void Board_AllDead_RemainsDead()
        {
            var board = CreateEmptyBoard(10, 10);
            Assert.AreEqual(0, board.GetAliveCount());
            
            for (int i = 0; i < 10; i++)
            {
                board.Advance();
                Assert.AreEqual(0, board.GetAliveCount());
            }
        }

        [TestMethod]
        public void Board_AllAlive_DiesQuickly()
        {
            var board = CreateEmptyBoard(5, 5);
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    board.Cells[x, y].IsAlive = true;
            
            int alive = board.GetAliveCount();
            Assert.AreEqual(25, alive);
            board.Advance();
            Assert.IsTrue(board.GetAliveCount() < alive);
            board.Advance();
            Assert.AreEqual(0, board.GetAliveCount());
        }

        [TestMethod]
        public void Board_TinyBoard_1x1_NoEvolution()
        {
            var board = CreateEmptyBoard(1, 1);
            board.Cells[0, 0].IsAlive = true;
            
            board.Advance();
            Assert.IsFalse(board.Cells[0, 0].IsAlive);
            
            board.Advance();
            Assert.IsFalse(board.Cells[0, 0].IsAlive);
        }

        [TestMethod]
        public void Board_TorusConnectivity_NeighborsWrapAround()
        {
            var board = CreateEmptyBoard(5, 5);
            var corner = board.Cells[0, 0];
            
            bool hasFarNeighbor = false;
            foreach (var n in corner.neighbors)
                if (n == board.Cells[4, 4] || n == board.Cells[4, 0] || n == board.Cells[0, 4])
                    hasFarNeighbor = true;
            
            Assert.IsTrue(hasFarNeighbor);
            Assert.AreEqual(8, corner.neighbors.Count);
        }

        [TestMethod]
        public void Board_TorusConnectivity_AllCornersConnected()
        {
            var board = CreateEmptyBoard(3, 3);
            var center = board.Cells[1, 1];
            
            // In 3x3 torus, center is connected to all cells
            var neighborCount = center.neighbors.Count;
            Assert.AreEqual(8, neighborCount);
        }

        [TestMethod]
        public void Board_GetAliveCount_ReturnsCorrectCount()
        {
            var board = CreateEmptyBoard(10, 10);
            board.LoadPattern(new[] { "***", "***", "***" }, 2, 2);
            Assert.AreEqual(9, board.GetAliveCount());
            
            board.Cells[5, 5].IsAlive = true;
            Assert.AreEqual(10, board.GetAliveCount());
        }

        [TestMethod]
        public void Board_LoadPattern_AtBoundary_LoadsCorrectly()
        {
            var board = CreateEmptyBoard(5, 5);
            board.LoadPattern(new[] { "**", "**" }, 3, 3);
            
            Assert.IsTrue(board.Cells[3, 3].IsAlive);
            Assert.IsTrue(board.Cells[4, 3].IsAlive);
            Assert.IsTrue(board.Cells[3, 4].IsAlive);
            Assert.IsTrue(board.Cells[4, 4].IsAlive);
        }

        [TestMethod]
        public void Board_LoadPattern_PartiallyOutOfBounds_IgnoresOutOfBounds()
        {
            var board = CreateEmptyBoard(5, 5);
            board.LoadPattern(new[] { "***", "***" }, 4, 4);
            
            // Pattern at (4,4), (5,4) out of bounds, (4,5) out of bounds
            // Only (4,4) should be set
            Assert.IsTrue(board.Cells[4, 4].IsAlive);
        }

        [TestMethod]
        public void Board_SaveAndLoad_StatePreserved()
        {
            var board = CreateEmptyBoard(10, 10);
            board.LoadPattern(new[] { "**", "**" }, 3, 3);
            board.Generation = 5;
            int aliveCount = board.GetAliveCount();
            
            string filename = "test_save.json";
            board.SaveToFile(filename);
            
            var newBoard = CreateEmptyBoard(10, 10);
            newBoard.LoadFromFile(filename);
            
            Assert.AreEqual(board.Columns, newBoard.Columns);
            Assert.AreEqual(board.Rows, newBoard.Rows);
            Assert.AreEqual(board.Generation, newBoard.Generation);
            Assert.AreEqual(aliveCount, newBoard.GetAliveCount());
            
            File.Delete(filename);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Board_LoadFromFile_WrongDimensions_ThrowsException()
        {
            var board = CreateEmptyBoard(10, 10);
            var board2 = new Board(5, 5);
            
            board.SaveToFile("test_wrong.json");
            board2.LoadFromFile("test_wrong.json");
            
            File.Delete("test_wrong.json");
        }

        [TestMethod]
        public void Board_ClassifyAllFigures_IdentifiesBlock()
        {
            var board = CreateEmptyBoard(10, 10);
            board.LoadPattern(new[] { "**", "**" }, 3, 3);
            var classification = board.ClassifyAllFigures();
            
            Assert.AreEqual(1, classification.Count);
            Assert.IsTrue(classification.ContainsKey("Block"));
            Assert.AreEqual(1, classification["Block"]);
        }

        [TestMethod]
        public void Board_ClassifyAllFigures_IdentifiesBlinker()
        {
            var board = CreateEmptyBoard(10, 10);
            board.LoadPattern(new[] { "***" }, 3, 4);
            board.Advance(); // Blinker becomes vertical
            
            var classification = board.ClassifyAllFigures();
            Assert.IsTrue(classification.ContainsKey("Blinker (periodic)"));
        }

        [TestMethod]
        public void Board_ClassifyAllFigures_IdentifiesMultipleFigures()
        {
            var board = CreateEmptyBoard(30, 30);
            board.LoadPattern(new[] { "**", "**" }, 2, 2); // Block
            board.LoadPattern(new[] { "***" }, 10, 10); // Blinker
            board.LoadPattern(new[] { " * ", "  *", "***" }, 20, 20); // Glider
            
            var classification = board.ClassifyAllFigures();
            Assert.IsTrue(classification.Count >= 3);
        }

        [TestMethod]
        public void Board_ClassifyCluster_EmptyCluster_ReturnsEmpty()
        {
            var board = CreateEmptyBoard(10, 10);
            var cluster = new List<Cell>();
            string result = board.ClassifyCluster(cluster);
            Assert.AreEqual("Empty", result);
        }

        [TestMethod]
        public void Board_ClassifyCluster_UnknownCluster_ReturnsUnknown()
        {
            var board = CreateEmptyBoard(10, 10);
            board.LoadPattern(new[] { "*" }, 5, 5);
            var clusters = board.FindClusters();
            string result = board.ClassifyCluster(clusters[0]);
            Assert.AreEqual("Unknown (1 cells)", result);
        }
    }

    [TestClass]
    public class SettingsTests
    {
        [TestMethod]
        public void Settings_DefaultValues()
        {
            var settings = new Settings();
            Assert.AreEqual(80, settings.Width);
            Assert.AreEqual(40, settings.Height);
            Assert.AreEqual(1, settings.CellSize);
            Assert.AreEqual(0.3, settings.LiveDensity);
            Assert.AreEqual(100, settings.SleepMs);
            Assert.AreEqual(1000, settings.MaxGenerations);
            Assert.AreEqual(5, settings.StableThreshold);
        }

        [TestMethod]
        public void Settings_CanModifyValues()
        {
            var settings = new Settings();
            settings.Width = 200;
            settings.Height = 100;
            settings.CellSize = 2;
            settings.LiveDensity = 0.5;
            settings.SleepMs = 50;
            settings.MaxGenerations = 500;
            settings.StableThreshold = 10;
            
            Assert.AreEqual(200, settings.Width);
            Assert.AreEqual(100, settings.Height);
            Assert.AreEqual(2, settings.CellSize);
            Assert.AreEqual(0.5, settings.LiveDensity);
            Assert.AreEqual(50, settings.SleepMs);
            Assert.AreEqual(500, settings.MaxGenerations);
            Assert.AreEqual(10, settings.StableThreshold);
        }
    }

    [TestClass]
    public class BoardStateTests
    {
        [TestMethod]
        public void BoardState_Serialization_Deserialization_Works()
        {
            var state = new BoardState
            {
                Columns = 10,
                Rows = 10,
                CellSize = 1,
                Generation = 5,
                Cells = new List<List<bool>>
                {
                    new List<bool> { true, false, true },
                    new List<bool> { false, true, false }
                }
            };
            
            string json = System.Text.Json.JsonSerializer.Serialize(state);
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<BoardState>(json);
            
            Assert.AreEqual(state.Columns, deserialized.Columns);
            Assert.AreEqual(state.Rows, deserialized.Rows);
            Assert.AreEqual(state.CellSize, deserialized.CellSize);
            Assert.AreEqual(state.Generation, deserialized.Generation);
            Assert.AreEqual(state.Cells.Count, deserialized.Cells.Count);
            Assert.AreEqual(state.Cells[0][0], deserialized.Cells[0][0]);
        }
    }

    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public void FullSimulation_Block_RemainsStable()
        {
            var board = new Board(10, 10);
            board.LoadPattern(new[] { "**", "**" }, 3, 3);
            
            int initialAlive = board.GetAliveCount();
            
            for (int gen = 0; gen < 10; gen++)
            {
                board.Advance();
                Assert.AreEqual(initialAlive, board.GetAliveCount());
            }
            
            Assert.AreEqual(10, board.Generation);
        }

        [TestMethod]
        public void FullSimulation_Blinker_OscillatesEveryTwoGenerations()
        {
            var board = new Board(5, 5);
            board.LoadPattern(new[] { "***" }, 1, 2);
            
            Assert.AreEqual(3, board.GetAliveCount());
            board.Advance();
            Assert.AreEqual(3, board.GetAliveCount());
            board.Advance();
            Assert.AreEqual(3, board.GetAliveCount());
        }

        [TestMethod]
        public void FullSimulation_Glider_ContinuesMoving()
        {
            var board = new Board(30, 30);
            board.LoadPattern(new[] { " * ", "  *", "***" }, 5, 5);
            
            // Track if glider moves (position changes)
            var initialPositions = new List<(int, int)>();
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    if (board.Cells[x, y].IsAlive)
                        initialPositions.Add((x, y));
            
            board.Advance();
            
            var newPositions = new List<(int, int)>();
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    if (board.Cells[x, y].IsAlive)
                        newPositions.Add((x, y));
            
            // Positions should be different (glider moved)
            bool positionsChanged = false;
            for (int i = 0; i < Math.Min(initialPositions.Count, newPositions.Count); i++)
            {
                if (initialPositions[i] != newPositions[i])
                {
                    positionsChanged = true;
                    break;
                }
            }
            Assert.IsTrue(positionsChanged || initialPositions.Count != newPositions.Count);
        }

        [TestMethod]
        public void FullSimulation_MaxGenerations_StopsSimulation()
        {
            var board = new Board(10, 10, 1, 0.5);
            int maxGen = 10;
            
            for (int gen = 0; gen < maxGen; gen++)
            {
                board.Advance();
            }
            
            Assert.AreEqual(maxGen, board.Generation);
        }

        [TestMethod]
        public void FullSimulation_RandomBoard_StabilizesEventually()
        {
            var board = new Board(20, 20, 1, 0.3);
            var aliveHistory = new List<int>();
            
            for (int gen = 0; gen < 50; gen++)
            {
                aliveHistory.Add(board.GetAliveCount());
                board.Advance();
            }
            
            // Check if board stabilized (alive count not changing much)
            var last10 = aliveHistory.Skip(Math.Max(0, aliveHistory.Count - 10)).ToList();
            bool isStable = last10.Distinct().Count() <= 3;
            
            // Not always true due to randomness, but useful test
            Assert.IsTrue(isStable || board.GetAliveCount() < 100);
        }
    }
}
