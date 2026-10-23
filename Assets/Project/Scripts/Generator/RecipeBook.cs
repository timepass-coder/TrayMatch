using System;

namespace TrayMatch.Generator
{
    // The game's levels: level 1 is RecipeFor(1) built with SeedFor(1). Changing a row changes that
    // level. Levels 1–10 = episode 1, core only (docs/GAME_DESIGN.md §4): 1 intro, 2–4 easy, 5 hard,
    // 6 relief, 7–9 mix, 10 finale. Levels 1, 2 and 5 are prototype levels 1, 2 and 3. Level 1 must
    // stay as it is: the tutorial is built on it.
    public static class RecipeBook
    {
        static readonly (string Role, int W, int H, double Density, int Colors, int Peak, double OpenProb, int Slack, int Seed)[] Rows =
        {
            ("intro", 5, 5, 0.5, 3, 2, 0.3, 3, 20264),
            ("easy",  6, 7, 0.55, 4, 3, 0.55, 2, 28183),
            ("easy",  6, 7, 0.55, 4, 3, 0.6, 2, 3003),
            ("easy",  7, 8, 0.6,  5, 3, 0.7,  2, 4004),
            ("hard",  7, 8, 0.6,  5, 3, 0.8,  1, 36102),
            ("relief", 6, 7, 0.55, 4, 3, 0.5,  3, 6006),
            ("mix",   7, 8, 0.6,  5, 3, 0.75, 2, 7007),
            ("mix",   7, 8, 0.62, 6, 3, 0.8,  2, 8008),
            ("mix",   8, 9, 0.6,  5, 3, 0.8,  2, 9009),
            ("hard",  8, 9, 0.62, 6, 3, 0.85, 1, 10010),
        };

        public static int Count => Rows.Length;

        public static string RoleFor(int level) => Row(level).Role;

        public static int SeedFor(int level) => Row(level).Seed;

        // A new Recipe each call, so callers can't change the book by accident.
        public static Recipe RecipeFor(int level)
        {
            var r = Row(level);
            return new Recipe
            {
                Width = r.W,
                Height = r.H,
                Density = r.Density,
                Colors = r.Colors,
                Peak = r.Peak,
                OpenProb = r.OpenProb,
                Slack = r.Slack,
            };
        }

        // Level L with Id = L, or null if the generator found no level (the tests make sure it does).
        public static GeneratedLevel Generate(int level)
        {
            var g = LevelGenerator.Generate(RecipeFor(level), SeedFor(level));
            if (g != null) g.Level.Id = level;
            return g;
        }

        static (string Role, int W, int H, double Density, int Colors, int Peak, double OpenProb, int Slack, int Seed) Row(int level)
        {
            if (level < 1 || level > Rows.Length)
                throw new ArgumentOutOfRangeException(nameof(level), $"Levels are 1–{Rows.Length}.");
            return Rows[level - 1];
        }
    }
}