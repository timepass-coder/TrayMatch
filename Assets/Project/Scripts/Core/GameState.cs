using System;
using System.Collections.Generic;

namespace TrayMatch.Core
{
    public enum GameStatus
    {
        Playing,
        Won,
        Lost,
    }

    public sealed class BlockState
    {
        public readonly int Index;
        public readonly int X;
        public readonly int Y;
        public readonly Direction Dir;
        public readonly int Color;
        public bool OnBoard { get; internal set; } = true;

        internal BlockState(int index, BlockData data)
        {
            Index = index;
            X = data.X;
            Y = data.Y;
            Dir = data.Dir;
            Color = data.Color;
        }
    }

    // The game rules. Pure logic: no Unity, no time, no rendering.
    // Mirrors findTrain() / tap() in prototype/web/index.html.
    public sealed class GameState
    {
        public const int MatchCount = 3;

        static readonly IReadOnlyList<GameEvent> NoEvents = new GameEvent[0];

        public readonly int Width;
        public readonly int Height;

        readonly List<BlockState> _blocks = new List<BlockState>();
        readonly List<int> _tray = new List<int>(); // colors, in arrival order
        readonly int[] _grid; // block index per cell, -1 = empty

        public IReadOnlyList<BlockState> Blocks => _blocks;
        public IReadOnlyList<int> Tray => _tray;
        public int TraySize { get; private set; }
        public int MovesLeft { get; private set; }
        public int BlocksOnBoard { get; private set; }
        public GameStatus Status { get; private set; } = GameStatus.Playing;
        public LoseReason? LoseReason { get; private set; }
        public bool Revived { get; private set; }

        // "+1 Slot & Continue": only after a tray-full loss, once per level.
        public bool CanRevive => Status == GameStatus.Lost &&
            LoseReason == Core.LoseReason.TrayFull && !Revived;

        public GameState(Level level)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (level.Width <= 0 || level.Height <= 0)
                throw new ArgumentException("Level size must be positive.");
            if (level.TraySize <= 0)
                throw new ArgumentException("TraySize must be positive.");
            if (level.MoveLimit <= 0)
                throw new ArgumentException("MoveLimit must be positive.");

            Width = level.Width;
            Height = level.Height;
            TraySize = level.TraySize;
            MovesLeft = level.MoveLimit;
            _grid = new int[Width * Height];

            for (int i = 0; i < _grid.Length; i++) _grid[i] = -1;

            foreach (var data in level.Blocks)
            {
                if (!InBounds(data.X, data.Y))
                    throw new ArgumentException($"Block at ({data.X},{data.Y}) is outside the board.");
                if (_grid[data.Y * Width + data.X] >= 0)
                    throw new ArgumentException($"Two blocks at ({data.X},{data.Y}).");
                if (data.Color < 0)
                    throw new ArgumentException($"Block at ({data.X},{data.Y}) has a negative color.");

                _grid[data.Y * Width + data.X] = _blocks.Count;
                _blocks.Add(new BlockState(_blocks.Count, data));
            }

            BlocksOnBoard = _blocks.Count;
        }

        public bool InBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height;

        // Index of the block on the board at (x, y), or -1.
        public int BlockAt(int x, int y) =>
            InBounds(x, y) ? _grid[y * Width + x] : -1;

        // The tapped block leaves together with every block AHEAD of it in its line that has the
        // same color and direction (empty cells between them are fine; any other block stops it).
        // Blocks behind the tapped one stay. If anything else is in front, the train bumps and
        // nothing changes (no move used).
        public IReadOnlyList<GameEvent> Tap(int x, int y)
        {
            if (Status != GameStatus.Playing) return NoEvents;
            int tapped = BlockAt(x, y);
            if (tapped < 0) return NoEvents;

            var b = _blocks[tapped];
            var train = new List<int> { tapped };
            int dx = b.Dir.Dx(), dy = b.Dir.Dy();
            int cx = b.X + dx, cy = b.Y + dy, gap = 0;

            while (InBounds(cx, cy))
            {
                int other = _grid[cy * Width + cx];
                if (other >= 0)
                {
                    var o = _blocks[other];
                    if (o.Color != b.Color || o.Dir != b.Dir)
                        return new GameEvent[] { new BumpedEvent(train, other, gap) };

                    train.Add(other);
                    gap = 0;
                }
                else
                {
                    gap++;
                }

                cx += dx;
                cy += dy;
            }

            foreach (int i in train)
            {
                var m = _blocks[i];
                m.OnBoard = false;
                _grid[m.Y * Width + m.X] = -1;
            }

            BlocksOnBoard -= train.Count;
            MovesLeft--;
            train.Reverse(); // front-most first

            var events = new List<GameEvent> { new LeftEvent(train) };
            for (int i = 0; i < train.Count; i++) _tray.Add(b.Color);
            events.Add(new TrayAddedEvent(b.Color, train.Count));

            while (CountInTray(b.Color) >= MatchCount)
            {
                for (int n = 0; n < MatchCount; n++) _tray.Remove(b.Color);
                events.Add(new TripleClearedEvent(b.Color));
            }

            if (BlocksOnBoard == 0 && _tray.Count == 0)
                Finish(events, GameStatus.Won, null);
            else if (_tray.Count >= TraySize)
                Finish(events, GameStatus.Lost, Core.LoseReason.TrayFull);
            else if (MovesLeft <= 0)
                Finish(events, GameStatus.Lost, Core.LoseReason.OutOfMoves);

            return events;
        }

        public IReadOnlyList<GameEvent> Revive()
        {
            if (!CanRevive) return NoEvents;
            Revived = true;
            TraySize++;
            var events = new List<GameEvent> { new RevivedEvent(TraySize) };
            // The tray filled on the last move: there is nothing left to play.
            if (MovesLeft <= 0)
                Finish(events, GameStatus.Lost, Core.LoseReason.OutOfMoves);
            else
            {
                Status = GameStatus.Playing;
                LoseReason = null;
            }

            return events;
        }

        int CountInTray(int color)
        {
            int n = 0;
            foreach (int c in _tray)
                if (c == color) n++;
            return n;
        }

        void Finish(List<GameEvent> events, GameStatus status, LoseReason? reason)
        {
            Status = status;
            LoseReason = reason;
            events.Add(status == GameStatus.Won
                ? (GameEvent)new WonEvent()
                : new LostEvent(reason.Value));
        }
    }
}
