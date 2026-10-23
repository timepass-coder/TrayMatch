using System.Collections.Generic;

namespace TrayMatch.Core
{
    // Extra data a tool stores with a level, so it can be rebuilt or debugged (docs/ARCHITECTURE.md §4).
    public sealed class LevelMeta
    {
        public string Generator;     // LevelGenerator.Version
        public string Role;          // RecipeBook role, e.g. "intro", "hard"
        public int Seed;             // RecipeBook seed
        public int Attempt;          // which retry of that seed worked
        public List<int[]> Solution; // winning taps as [x, y]
    }
}