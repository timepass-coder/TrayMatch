using System.Collections.Generic;

namespace TrayMatch.Core
{
    // All of the game's levels in one file, written by TrayMatch → Bake Levels.
    public sealed class LevelPack
    {
        public const int CurrentFormat = 1;
        public int Format = CurrentFormat;
        public List<Level> Levels = new List<Level>();
    }
}