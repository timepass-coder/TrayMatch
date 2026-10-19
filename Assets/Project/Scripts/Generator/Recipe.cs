using System;
using System.Collections.Generic;

namespace TrayMatch.Generator
{
    // Settings for one element pass (docs/ARCHITECTURE.md 65), e.g. { Type = "ice", Chance = 0.12, Max = 6 }.
    public sealed class ElementSettings
    {
        public string Type;
        public double Chance;
        public int Max;
    }

    // Everything the generator needs to build a level, except the seed. Data, not code.
    public sealed class Recipe
    {
        // Board
        public int Width;
        public int Height;
        public double Density; // share of cells filled with blocks before trimming

        // Pieces
        public double TrainChance = 0.25;       // chance a placement step builds a train instead of a single block
        public double TrainOfTwoChance = 0.6;   // a train has 2 blocks with this chance, otherwise 3
        public double TrainGapChance = 0.3;     // chance of an empty cell between two train blocks

        // Colors
        public int Colors;       // how many colors are in play
        public int Peak;         // max tray load on the guaranteed solution
        public double OpenProb;  // how often the solution interleaves colors (higher = harder)

        // Moves
        public int TraySize = 5;
        public int Slack; // extra moves on top of the solution's move count (lower = tighter)

        public List<ElementSettings> Elements = new List<ElementSettings>();

        // The recipe the web prototype uses for level L (levelParams() in prototype/web/index.html).
        public static Recipe Prototype(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));

            switch (level)
            {
                case 1: return Make(5, 5, 0.5, 3, 2, 0.3, 3);
                case 2: return Make(6, 7, 0.55, 4, 3, 0.55, 2);
                case 3: return Make(7, 8, 0.6, 5, 3, 0.8, 1);
            }

            int s = Math.Min(7 + (level - 3) / 3, 9);
            return Make(s, Math.Min(s + 1, 11), 0.62, 6, 3, 0.85, 1);
        }

        // The seed the prototype uses for level L. Retry n uses seed + n * LevelGenerator.AttemptSeedStep.
        public static int PrototypeSeed(int level) => unchecked(level * 7919 + 12345);

        static Recipe Make(int w, int h, double density, int colors, int peak, double openProb, int slack) =>
            new Recipe
            {
                Width = w,
                Height = h,
                Density = density,
                Colors = colors,
                Peak = peak,
                OpenProb = openProb,
                Slack = slack,
            };
    }
}