using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TrayMatch.Core
{
    // Plain data for one level, as loaded from a baked level file.
    // Public fields + [Serializable] so Unity's JsonUtility can read it later.
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

    public sealed class Level
    {
        public const int CurrentFormat =1;
        public int Format = CurrentFormat;
        public int Id;
        public int Width;
        public int Height;
        public int TraySize = 5;
        public int MoveLimit;
        public List<BlockData> Blocks = new List<BlockData>();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LevelMeta Meta; // where the game came from; the game oesnt need it to play
    }
}