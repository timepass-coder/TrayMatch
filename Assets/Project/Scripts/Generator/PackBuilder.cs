using System;
using System.Collections.Generic;
using TrayMatch.Core;

namespace TrayMatch.Generator
{
    // What TrayMatch → Bake Levels does, without the Unity parts.
    public static class PackBuilder
    {
        // Every RecipeBook level, with its meta.
        public static LevelPack Build()
        {
            var pack = new LevelPack();
            for (int id = 1; id <= RecipeBook.Count; id++)
            {
                var g = RecipeBook.Generate(id);
                if (g == null)
                    throw new InvalidOperationException(
                        $"Level {id}: no level after {LevelGenerator.MaxAttempts} attempts. Change its recipe or seed.");

                g.Level.Meta = new LevelMeta
                {
                    Generator = LevelGenerator.Version,
                    Role = RecipeBook.RoleFor(id),
                    Seed = RecipeBook.SeedFor(id),
                    Attempt = g.Attempt,
                    Solution = new List<int[]>(g.Solution),
                };
                pack.Levels.Add(g.Level);
            }

            return pack;
        }

        // IDs of levels in oldPack that would play differently in newPack
        // (board, tray, moves or blocks changed) or are missing from it.
        // Meta changes don't count. Sorted.
        public static List<int> ChangedLevels(LevelPack oldPack, LevelPack newPack)
        {
            var next = new Dictionary<int, string>();
            foreach (var level in newPack.Levels)
                next[level.Id] = PlayData(level);

            var changed = new List<int>();
            foreach (var level in oldPack.Levels)
                if (!next.TryGetValue(level.Id, out string data) || data != PlayData(level))
                    changed.Add(level.Id);

            changed.Sort();
            return changed;
        }

        static string PlayData(Level level) =>
            LevelSerializer.ToJson(new Level
            {
                Format = level.Format,
                Id = level.Id,
                Width = level.Width,
                Height = level.Height,
                TraySize = level.TraySize,
                MoveLimit = level.MoveLimit,
                Blocks = level.Blocks,
            });
    }
}
