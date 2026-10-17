using System;
using System.Linq;
using NUnit.Framework;
using TrayMatch.Core;

namespace TrayMatch.Core.Tests
{
    public class GameStateTests
    {
        const int Red = 0, Blue = 1, Green = 2;

        static GameState Game(int width, int height, int traySize, int moves, params BlockData[] blocks)
        {
            var level = new Level { Width = width, Height = height, TraySize = traySize, MoveLimit = moves };
            level.Blocks.AddRange(blocks);
            return new GameState(level);
        }

        static BlockData B(int x, int y, Direction dir, int color) => new BlockData(x, y, dir, color);

        static T Single<T>(System.Collections.Generic.IReadOnlyList<GameEvent> events) where T : GameEvent
        {
            var found = events.OfType<T>().ToList();
            Assert.AreEqual(1, found.Count, $"expected exactly one {typeof(T).Name}");
            return found[0];
        }

        // ---------- Basic tap ----------

        [Test]
        public void TapOnEmptyCell_DoesNothing()
        {
            var g = Game(3, 1, 5, 5, B(0, 0, Direction.Right, Red));
            Assert.AreEqual(0, g.Tap(2, 0).Count);
            Assert.AreEqual(0, g.Tap(-1, 0).Count);
            Assert.AreEqual(5, g.MovesLeft);
        }

        [Test]
        public void FreeBlock_Leaves_GoesToTray_UsesOneMove()
        {
            var g = Game(3, 1, 5, 5, B(0, 0, Direction.Right, Red));
            var events = g.Tap(0, 0);
            CollectionAssert.AreEqual(new[] { 0 }, Single<LeftEvent>(events).Blocks);
            Assert.AreEqual(1, Single<TrayAddedEvent>(events).Count);
            Assert.AreEqual(4, g.MovesLeft);
            Assert.AreEqual(-1, g.BlockAt(0, 0));
            CollectionAssert.AreEqual(new[] { Red }, g.Tray);
        }

        [Test]
        public void BlockedBlock_Bumps_NothingChanges_NoMoveUsed()
        {
            // red -> . blue(up)
            var g = Game(3, 1, 5, 5, B(0, 0, Direction.Right, Red), B(2, 0, Direction.Up, Blue));
            var bump = Single<BumpedEvent>(g.Tap(0, 0));
            CollectionAssert.AreEqual(new[] { 0 }, bump.Blocks);
            Assert.AreEqual(1, bump.Blocker);
            Assert.AreEqual(1, bump.Steps);
            Assert.AreEqual(5, g.MovesLeft);
            Assert.AreEqual(0, g.BlockAt(0, 0));
            Assert.AreEqual(0, g.Tray.Count);
        }

        // ---------- Trains ----------

        // Three red blocks in a row pointing right: x=0 (back), x=1 (middle), x=2 (front).
        [TestCase(2, 1)] // tap front -> 1 leaves
        [TestCase(1, 2)] // tap middle -> 2 leave
        [TestCase(0, 3)] // tap back -> all 3 leave
        public void RowOfThree_TappedBlockAndBlocksAheadLeave(int tapX, int expectedLeaving)
        {
            var g = Game(5, 1, 5, 5,
                B(0, 0, Direction.Right, Red),
                B(1, 0, Direction.Right, Red),
                B(2, 0, Direction.Right, Red));

            var left = Single<LeftEvent>(g.Tap(tapX, 0));
            Assert.AreEqual(expectedLeaving, left.Blocks.Count);
            Assert.AreEqual(3 - expectedLeaving, g.BlocksOnBoard);
            Assert.AreEqual(4, g.MovesLeft, "a train costs one move");
        }

        [Test]
        public void Train_LeftEvent_ListsFrontMostBlockFirst()
        {
            var g = Game(5, 1, 5, 5,
                B(0, 0, Direction.Right, Red),
                B(1, 0, Direction.Right, Red),
                B(2, 0, Direction.Right, Red));

            CollectionAssert.AreEqual(new[] { 2, 1, 0 }, Single<LeftEvent>(g.Tap(0, 0)).Blocks);
        }

        [Test]
        public void Train_EmptyCellsBetweenBlocksAreAllowed()
        {
            // red -> . red -> . . red ->
            var g = Game(7, 1, 5, 5,
                B(0, 0, Direction.Right, Red),
                B(2, 0, Direction.Right, Red),
                B(5, 0, Direction.Right, Red));

            Assert.AreEqual(3, Single<LeftEvent>(g.Tap(0, 0)).Blocks.Count);
        }

        [Test]
        public void Train_DifferentColorInBetween_BlocksTheTrain()
        {
            // red -> blue(up) red ->
            var g = Game(4, 1, 5, 5,
                B(0, 0, Direction.Right, Red),
                B(1, 0, Direction.Up, Blue),
                B(2, 0, Direction.Right, Red));

            var bump = Single<BumpedEvent>(g.Tap(0, 0));
            Assert.AreEqual(1, bump.Blocker);
            Assert.AreEqual(0, bump.Steps);
        }

        [Test]
        public void Train_SameColorButDifferentDirection_IsABlocker()
        {
            // red -> red(up)
            var g = Game(3, 1, 5, 5,
                B(0, 0, Direction.Right, Red),
                B(1, 0, Direction.Up, Red));

            Assert.AreEqual(1, Single<BumpedEvent>(g.Tap(0, 0)).Blocker);
        }

        [Test]
        public void Train_BlockedAtTheFront_WholeTrainBumps()
        {
            // red -> red -> . blue(up)
            var g = Game(4, 1, 5, 5,
                B(0, 0, Direction.Right, Red),
                B(1, 0, Direction.Right, Red),
                B(3, 0, Direction.Up, Blue));

            var bump = Single<BumpedEvent>(g.Tap(0, 0));
            CollectionAssert.AreEqual(new[] { 0, 1 }, bump.Blocks);
            Assert.AreEqual(2, bump.Blocker);
            Assert.AreEqual(1, bump.Steps, "empty cells between the front block and the blocker");
            Assert.AreEqual(5, g.MovesLeft);
        }

        [Test]
        public void Train_WorksVertically()
        {
            // column x=0, all pointing up: y=0 (front), y=1, y=2 (back)
            var g = Game(1, 3, 5, 5,
                B(0, 0, Direction.Up, Green),
                B(0, 1, Direction.Up, Green),
                B(0, 2, Direction.Up, Green));

            Assert.AreEqual(3, Single<LeftEvent>(g.Tap(0, 2)).Blocks.Count);
        }

        // ---------- Tray ----------

        [Test]
        public void ThreeOfAColor_ClearFromTray()
        {
            var g = Game(3, 3, 5, 5,
                B(0, 0, Direction.Up, Red),
                B(1, 0, Direction.Up, Red),
                B(2, 0, Direction.Up, Red),
                B(0, 2, Direction.Down, Blue));

            g.Tap(0, 0);
            g.Tap(1, 0);
            var events = g.Tap(2, 0);

            Assert.AreEqual(Red, Single<TripleClearedEvent>(events).Color);
            Assert.AreEqual(0, g.Tray.Count);
        }

        [Test]
        public void TrainOfFour_ClearsOneTriple_LeavesOneInTray()
        {
            var g = Game(5, 2, 5, 5,
                B(0, 0, Direction.Right, Red),
                B(1, 0, Direction.Right, Red),
                B(2, 0, Direction.Right, Red),
                B(3, 0, Direction.Right, Red),
                B(0, 1, Direction.Left, Blue));

            var events = g.Tap(0, 0);

            Assert.AreEqual(4, Single<TrayAddedEvent>(events).Count);
            Assert.AreEqual(1, events.OfType<TripleClearedEvent>().Count());
            CollectionAssert.AreEqual(new[] { Red }, g.Tray);
        }

        [Test]
        public void TripleOnTheLastFreeSlot_ClearsInsteadOfLosing()
        {
            // tray of 3: red, red, then the third red clears
            var g = Game(3, 2, 3, 5,
                B(0, 0, Direction.Up, Red),
                B(1, 0, Direction.Up, Red),
                B(2, 0, Direction.Up, Red),
                B(0, 1, Direction.Down, Blue));

            g.Tap(0, 0);
            g.Tap(1, 0);
            g.Tap(2, 0);

            Assert.AreEqual(GameStatus.Playing, g.Status);
        }

        // ---------- Win / lose ----------

        [Test]
        public void TrayFull_Loses()
        {
            // tray of 2: red, blue -> full, no triple
            var g = Game(3, 1, 2, 5,
                B(0, 0, Direction.Up, Red),
                B(1, 0, Direction.Up, Blue),
                B(2, 0, Direction.Up, Green));

            g.Tap(0, 0);
            var events = g.Tap(1, 0);

            Assert.AreEqual(LoseReason.TrayFull, Single<LostEvent>(events).Reason);
            Assert.AreEqual(GameStatus.Lost, g.Status);
            Assert.AreEqual(LoseReason.TrayFull, g.LoseReason);
        }

        [Test]
        public void OutOfMoves_Loses()
        {
            var g = Game(3, 1, 5, 1,
                B(0, 0, Direction.Up, Red),
                B(1, 0, Direction.Up, Red),
                B(2, 0, Direction.Up, Red));

            var events = g.Tap(0, 0);

            Assert.AreEqual(LoseReason.OutOfMoves, Single<LostEvent>(events).Reason);
            Assert.AreEqual(0, g.MovesLeft);
        }

        [Test]
        public void ClearingEverythingOnTheLastMove_Wins()
        {
            // one move, a train of 3 reds clears the whole board and the tray
            var g = Game(3, 1, 5, 1,
                B(0, 0, Direction.Right, Red),
                B(1, 0, Direction.Right, Red),
                B(2, 0, Direction.Right, Red));

            var events = g.Tap(0, 0);

            Single<WonEvent>(events);
            Assert.AreEqual(GameStatus.Won, g.Status);
            Assert.AreEqual(0, events.OfType<LostEvent>().Count());
        }

        [Test]
        public void TapsAreIgnoredAfterTheGameEnds()
        {
            var g = Game(3, 1, 5, 1,
                B(0, 0, Direction.Up, Red),
                B(1, 0, Direction.Up, Red),
                B(2, 0, Direction.Up, Red));

            g.Tap(0, 0);

            Assert.AreEqual(0, g.Tap(1, 0).Count);
            Assert.AreEqual(2, g.BlocksOnBoard);
        }

        // ---------- Revive ----------

        [Test]
        public void ReviveAfterTrayFull_AddsSlot_ContinuesPlaying_OnlyOnce()
        {
            var g = Game(4, 1, 2, 5,
                B(0, 0, Direction.Up, Red),
                B(1, 0, Direction.Up, Blue),
                B(2, 0, Direction.Up, Green),
                B(3, 0, Direction.Up, Green));

            g.Tap(0, 0);
            g.Tap(1, 0);
            Assert.IsTrue(g.CanRevive);

            Assert.AreEqual(3, Single<RevivedEvent>(g.Revive()).TraySize);
            Assert.AreEqual(GameStatus.Playing, g.Status);
            Assert.IsNull(g.LoseReason);

            g.Tap(2, 0); // tray: red, blue, green -> full again
            Assert.AreEqual(GameStatus.Lost, g.Status);
            Assert.IsFalse(g.CanRevive);
            Assert.AreEqual(0, g.Revive().Count);
        }

        [Test]
        public void CannotReviveAfterOutOfMoves()
        {
            var g = Game(3, 1, 5, 1,
                B(0, 0, Direction.Up, Red),
                B(1, 0, Direction.Up, Red),
                B(2, 0, Direction.Up, Red));

            g.Tap(0, 0);

            Assert.IsFalse(g.CanRevive);
            Assert.AreEqual(0, g.Revive().Count);
        }

        [Test]
        public void ReviveWhenTrayFilledOnTheLastMove_EndsOutOfMoves()
        {
            // tray of 1, one move: the first tap fills the tray AND uses the last move
            var g = Game(2, 1, 1, 1,
                B(0, 0, Direction.Up, Red),
                B(1, 0, Direction.Up, Red));

            g.Tap(0, 0);
            Assert.AreEqual(LoseReason.TrayFull, g.LoseReason);

            var events = g.Revive();

            Single<RevivedEvent>(events);
            Assert.AreEqual(LoseReason.OutOfMoves, Single<LostEvent>(events).Reason);
            Assert.AreEqual(GameStatus.Lost, g.Status);
        }

        // ---------- Level validation ----------

        [Test]
        public void InvalidLevels_AreRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                Game(2, 2, 5, 5, B(2, 0, Direction.Up, Red)));

            Assert.Throws<ArgumentException>(() =>
                Game(2, 2, 5, 5,
                    B(0, 0, Direction.Up, Red),
                    B(0, 0, Direction.Left, Blue)));

            Assert.Throws<ArgumentException>(() =>
                Game(2, 2, 5, 0, B(0, 0, Direction.Up, Red)));

            Assert.Throws<ArgumentException>(() =>
                Game(2, 2, 5, 5, B(0, 0, Direction.Up, -1)));
        }
    }
}