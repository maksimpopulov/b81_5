using Microsoft.VisualStudio.TestTools.UnitTesting;
using GameOfLife;  // Изменено с cli_life на GameOfLife
using System.Collections.Generic;

namespace GameOfLife.Tests  // Изменено пространство имен
{
    [TestClass]
    public class UnitTest1
    {
        private Board CreateEmptyBoard(int columns, int rows) =>
            new Board(columns, rows);

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
        public void Board_Constructor_SetsCorrectDimensions()
        {
            var board = new Board(80, 40, 1, 0);
            Assert.AreEqual(80, board.Columns);
            Assert.AreEqual(40, board.Rows);
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
            board.Advance();
            Assert.AreEqual(before, board.GetAliveCount());
            Assert.IsTrue(board.Cells[3, 3].IsAlive);
            Assert.IsTrue(board.Cells[4, 3].IsAlive);
            Assert.IsTrue(board.Cells[3, 4].IsAlive);
            Assert.IsTrue(board.Cells[4, 4].IsAlive);
        }

        [TestMethod]
        public void Board_Glider_Moves()
        {
            var board = CreateEmptyBoard(20, 20);
            board.LoadPattern(new[] { " * ", "  *", "***" }, 5, 5);
            Assert.AreEqual(5, board.GetAliveCount());
            board.Advance();
            Assert.AreEqual(5, board.GetAliveCount());
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
        public void Board_AllDead_RemainsDead()
        {
            var board = CreateEmptyBoard(10, 10);
            Assert.AreEqual(0, board.GetAliveCount());
            board.Advance();
            Assert.AreEqual(0, board.GetAliveCount());
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
        public void Settings_DefaultValues()
        {
            var settings = new Settings();
            Assert.AreEqual(80, settings.Width);
            Assert.AreEqual(40, settings.Height);
            Assert.AreEqual(1, settings.CellSize);
            Assert.AreEqual(0.3, settings.LiveDensity);
            Assert.AreEqual(100, settings.SleepMs);
        }
    }
}
