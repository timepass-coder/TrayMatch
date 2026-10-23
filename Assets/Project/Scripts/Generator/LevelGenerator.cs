using System;
using System.Collections.Generic;
using TrayMatch.Core;

namespace TrayMatch.Generator
{
    public sealed class GeneratedLevel
    {
        public readonly Level Level;
        public readonly IReadOnlyList<int[]> Solution; // Id is 0; the caller numbers the level
        public readonly int Seed;                     // Winning taps as [x, y], in order
        public readonly int Attempt;                  // RNG seed of the attempt that worked

        public GeneratedLevel(Level level, IReadOnlyList<int[]> solution, int seed, int attempt)
        {
            Level = level;
            Solution = solution;
            Seed = seed;
            Attempt = attempt;
        }
    }

    // Builds a level from a recipe and a seed (pipeline: docs/ARCHITECTURE.md §5).
    // A 1:1 port of generate() / buildLevel() in prototype/web/index.html with trains ON, including
    // the order of RNG calls: Recipe.Prototype(L) + Recipe.PrototypeSeed(L) give the browser's level L.
    public static class LevelGenerator
    {
        // Stamped into each baked level. Bump it when a code change can change generated levels.
        public const string Version = "1.0";
        public const int MaxAttempts = 30;
        public const int AttemptSeedStep = 1000003;

        static readonly Dictionary<string, IElementPass> Passes = new Dictionary<string, IElementPass>
        {
            ["wall"] = new NoOpPass(),
            ["ice"] = new NoOpPass(),
        };

        // Tries seed, seed + AttemptSeedStep, ... and returns the first level that verifies,
        // or null if all MaxAttempts attempts fail.
        public static GeneratedLevel Generate(Recipe recipe, int seed)
        {
            Validate(recipe);

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                int s = unchecked(seed + attempt * AttemptSeedStep);
                var level = new Builder(recipe, s).Build(out var solution);
                if (level != null) return new GeneratedLevel(level, solution, s, attempt);
            }

            return null;
        }

        static void Validate(Recipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (recipe.Width <= 0 || recipe.Height <= 0)
                throw new ArgumentException("Recipe board size must be positive.");
            if (recipe.Colors <= 0)
                throw new ArgumentException("Recipe needs at least one color.");
            if (recipe.TraySize < GameState.MatchCount)
                throw new ArgumentException($"TraySize must be at least {GameState.MatchCount}.");
            if (recipe.Slack < 0)
                throw new ArgumentException("Slack can't be negative.");
            if (recipe.Elements == null)
                throw new ArgumentException("Recipe.Elements can't be null (use an empty list).");

            foreach (var e in recipe.Elements)
                if (e?.Type == null || !Passes.ContainsKey(e.Type))
                    throw new ArgumentException($"Unknown element type: {e?.Type ?? "null"}.");
        }

        sealed class Piece
        {
            public readonly List<(int X, int Y)> Cells; // front first
            public readonly Direction Dir;
            public int Color;

            public Piece(List<(int X, int Y)> cells, Direction dir)
            {
                Cells = cells;
                Dir = dir;
            }
        }

        // One attempt (buildLevel() in the prototype).
        sealed class Builder
        {
            const int Empty = -1;
            const int Wall = -2;
            const int CandidatesPerPiece = 24;
            const int MaxPlacementFails = 200;

            readonly Recipe _recipe;
            readonly Mulberry32 _rng;
            readonly GenerationContext _context;
            readonly int _w;
            readonly int _h;
            readonly int[] _occ; // piece index per cell, or Empty / Wall
            readonly List<Piece> _pieces = new List<Piece>();

            public Builder(Recipe recipe, int seed)
            {
                _recipe = recipe;
                _rng = new Mulberry32(seed);
                _context = new GenerationContext(recipe, _rng);
                _w = recipe.Width;
                _h = recipe.Height;
                _occ = new int[_w * _h];
            }

            public Level Build(out List<int[]> solution)
            {
                solution = null;

                // 1 Layout
                foreach (var e in _recipe.Elements) Passes[e.Type].Layout(_context, e);
                for (int i = 0; i < _occ.Length; i++)
                    _occ[i] = _context.Blocked[i] ? Wall : Empty;

                // 2 Placement, then trim to a multiple of 3
                int count = PlacePieces();
                while (count % GameState.MatchCount != 0)
                {
                    // Removing any piece never breaks solvability.
                    int r = count % GameState.MatchCount;
                    int idx = _pieces.FindIndex(p => p.Cells.Count == r);
                    if (idx < 0) idx = 0;
                    count -= _pieces[idx].Cells.Count;
                    _pieces.RemoveAt(idx);
                }

                // 3 Colors
                if (!AssignColors(count)) return null;

                var blocks = new List<BlockData>(count);
                var pieceBlocks = new List<List<int>>(_pieces.Count); // block indices per piece
                foreach (var pc in _pieces)
                {
                    var indices = new List<int>(pc.Cells.Count);
                    foreach (var (x, y) in pc.Cells)
                    {
                        indices.Add(blocks.Count);
                        blocks.Add(new BlockData(x, y, pc.Dir, pc.Color));
                    }

                    pieceBlocks.Add(indices);
                }

                // 4 Element passes
                _context.Blocks = blocks;
                foreach (var e in _recipe.Elements) Passes[e.Type].Decorate(_context, e);

                // 5 Verify
                var level = new Level
                {
                    Width = _w,
                    Height = _h,
                    TraySize = _recipe.TraySize,
                    MoveLimit = Math.Max(count, 1), // every move removes a block, so this is never short
                    Blocks = blocks,
                };

                solution = Replay(level, pieceBlocks);
                if (solution == null) return null;

                level.MoveLimit = solution.Count + _recipe.Slack;
                return level;
            }

            // Pieces (a single block, or a train of 2–3 blocks in a line) are placed in reverse
            // removal order. Each new piece needs a clear exit in front of its front block among
            // already-placed pieces, so reverse placement order is always a valid solution.
            // Returns the number of blocks placed.
            int PlacePieces()
            {
                int target = (int)Math.Floor(_w * _h * _recipe.Density + 0.5); // JS Math.round
                int count = 0, fails = 0;

                while (count < target && fails < MaxPlacementFails)
                {
                    Piece best = null;
                    double bestScore = -1;
                    int k = _rng.Next() < _recipe.TrainChance
                        ? (_rng.Next() < _recipe.TrainOfTwoChance ? 2 : 3)
                        : 1;

                    for (int i = 0; i < CandidatesPerPiece; i++)
                    {
                        int x = _rng.Next(_w), y = _rng.Next(_h);
                        if (_occ[y * _w + x] != Empty) continue;

                        var d = (Direction)_rng.Next(4);
                        var cells = k == 1 ? new List<(int X, int Y)> { (x, y) } : Line(x, y, d, k);
                        if (cells == null || !ExitClear(x, y, d)) continue;

                        // Prefer pieces that sit in the exit path of many placed blocks.
                        int blocking = 0;
                        foreach (var (cx, cy) in cells) blocking += BlockedBy(cx, cy);
                        double score = blocking * 2 + _rng.Next() * 3;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = new Piece(cells, d);
                        }
                    }

                    if (best == null)
                    {
                        fails++;
                        continue;
                    }

                    foreach (var (cx, cy) in best.Cells)
                        _occ[cy * _w + cx] = _pieces.Count;

                    _pieces.Add(best);
                    count += best.Cells.Count;
                }

                return count;
            }

            bool Inside(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;

            bool ExitClear(int x, int y, Direction d)
            {
                int dx = d.Dx(), dy = d.Dy();
                for (x += dx, y += dy; Inside(x, y); x += dx, y += dy)
                    if (_occ[y * _w + x] != Empty) return false;
                return true;
            }

            // How many placed blocks have (x, y) in their exit path.
            int BlockedBy(int x, int y)
            {
                int n = 0;
                foreach (var pc in _pieces)
                {
                    int dx = pc.Dir.Dx(), dy = pc.Dir.Dy();
                    foreach (var (bx, by) in pc.Cells)
                    {
                        int rx = x - bx, ry = y - by;
                        if (dx == 0 ? rx == 0 && ry * dy > 0 : ry == 0 && rx * dx > 0) n++;
                    }
                }

                return n;
            }

            // Train cells, front first, extending backwards from (x, y). Gap cells must be empty too.
            List<(int X, int Y)> Line(int x, int y, Direction d, int k)
            {
                int dx = d.Dx(), dy = d.Dy();
                var cells = new List<(int X, int Y)> { (x, y) };
                while (cells.Count < k)
                {
                    int step = _rng.Next() < _recipe.TrainGapChance ? 2 : 1;
                    for (int s = 0; s < step; s++)
                    {
                        x -= dx;
                        y -= dy;
                        if (!Inside(x, y) || _occ[y * _w + x] != Empty) return null;
                    }

                    cells.Add((x, y));
                }

                return cells;
            }

            // Colors are assigned along the solution (pieces in reverse placement order), keeping
            // the tray load within the recipe's peak. False if a train can't get a color.
            bool AssignColors(int count)
            {
                int cap = _recipe.TraySize;
                // Colors with an unfinished triple, in the order they were opened; the prototype
                // keeps them in a Map (insertion order), and the RNG picks depend on that order.
                var open = new List<int>();
                var inTray = new int[_recipe.Colors];
                int load = 0, remaining = count;

                for (int pi = _pieces.Count - 1; pi >= 0; pi--)
                {
                    var pc = _pieces[pi];
                    int k = pc.Cells.Count;
                    int need = 0;
                    foreach (int c in open) need += 3 - inTray[c];
                    var unused = new List<int>();
                    for (int c = 0; c < _recipe.Colors; c++)
                        if (!open.Contains(c)) unused.Add(c);

                    int col;
                    if (k == 1)
                    {
                        bool canOpen = unused.Count > 0 && load + 1 <= Math.Min(_recipe.Peak, cap - 2) &&
                                       need + 3 <= remaining;
                        var conts = open.FindAll(c => inTray[c] == 2 || load + 1 <= cap - 1);
                        if (canOpen && (conts.Count == 0 || _rng.Next() < _recipe.OpenProb))
                        {
                            col = unused[_rng.Next(unused.Count)];
                            open.Add(col);
                        }
                        else
                        {
                            col = conts.Count > 0 ? conts[_rng.Next(conts.Count)] : open[0];
                        }
                    }
                    else if (k == 3)
                    {
                        var pool = unused;
                        if (pool.Count == 0)
                            for (int c = 0; c < _recipe.Colors; c++) pool.Add(c);
                        col = pool[_rng.Next(pool.Count)]; // instant triple
                    }
                    else
                    {
                        var ones = open.FindAll(c => inTray[c] == 1);
                        var twos = open.FindAll(c => inTray[c] == 2);
                        if (ones.Count > 0)
                        {
                            col = ones[_rng.Next(ones.Count)]; // completes a triple
                        }
                        else if (unused.Count > 0 && load + 2 <= cap - 2 && need + 3 <= remaining)
                        {
                            col = unused[_rng.Next(unused.Count)];
                            open.Add(col);
                        }
                        else if (twos.Count > 0)
                        {
                            col = twos[_rng.Next(twos.Count)]; // 2+2: triple clears, 1 stays
                        }
                        else
                        {
                            return false;
                        }
                    }

                    int n = inTray[col] + k;
                    load += k;
                    if (n >= 3)
                    {
                        n -= 3;
                        load -= 3;
                    }

                    inTray[col] = n;
                    if (n == 0) open.Remove(col);
                    else if (!open.Contains(col)) open.Add(col);
                    pc.Color = col;
                    remaining -= k;
                }

                return true;
            }

            // Plays the guaranteed solution through the real rules: pieces in removal order,
            // tapping the back block of each piece so a whole train goes in one move. Separate
            // pieces that happen to line up with the same color also form trains; the rules
            // catch any bump or tray overflow they cause. Null if the solution doesn't win.
            static List<int[]> Replay(Level level, List<List<int>> pieceBlocks)
            {
                var state = new GameState(level);
                var taps = new List<int[]>();

                for (int i = pieceBlocks.Count - 1; i >= 0; i--)
                {
                    for (;;)
                    {
                        int back = -1;
                        foreach (int b in pieceBlocks[i])
                            if (state.Blocks[b].OnBoard) back = b;

                        if (back < 0) break;

                        var block = state.Blocks[back];
                        state.Tap(block.X, block.Y);
                        taps.Add(new[] { block.X, block.Y });
                        if (block.OnBoard || state.Status == GameStatus.Lost) return null;
                    }
                }

                return state.Status == GameStatus.Won ? taps : null;
            }
        }
    }
}