using System;
using System.Collections.Generic;

namespace TrayMatch.Core
{
    // Plain data for one level, as loaded from a baked level file.
    // Public fields + [Serializable] so Unity's JsonUtility can read it later.
    [Serializable]
    public sealed class BlockData
    {
        public int X;
        public int Y;
        public Direction Dir;
        public int Color; // palette index

        public BlockData() { }

        public BlockData(int x, int y, Direction dir, int color)
        {
            X = x;
            Y = y;
            Dir = dir;
            Color = color;
        }
    }

    [Serializable]
    public sealed class Level
    {
        public int Id;
        public int Width;
        public int Height;
        public int TraySize = 5;
        public int MoveLimit;
        public List<BlockData> Blocks = new List<BlockData>();
    }
}