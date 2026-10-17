using System.Collections.Generic;

namespace TrayMatch.Core
{
    public enum LoseReason
    {
        TrayFull,
        OutOfMoves,
    }

    // What happened as a result of a player action, in order. The view plays these as
    // animations; it never decides rules itself. Blocks are referred to by their index
    // in GameState.Blocks.
    public abstract class GameEvent { }

    // The tapped block (and its train) hit Blocker after Steps empty cells and stayed.
    // No move is used.
    public sealed class BumpedEvent : GameEvent
    {
        public readonly IReadOnlyList<int> Blocks;
        public readonly int Blocker;
        public readonly int Steps;

        public BumpedEvent(IReadOnlyList<int> blocks, int blocker, int steps)
        {
            Blocks = blocks;
            Blocker = blocker;
            Steps = steps;
        }
    }

    // These blocks left the board in one move, front-most block first.
    public sealed class LeftEvent : GameEvent
    {
        public readonly IReadOnlyList<int> Blocks;

        public LeftEvent(IReadOnlyList<int> blocks)
        {
            Blocks = blocks;
        }
    }

    public sealed class TrayAddedEvent : GameEvent
    {
        public readonly int Color;
        public readonly int Count;

        public TrayAddedEvent(int color, int count)
        {
            Color = color;
            Count = count;
        }
    }

    public sealed class TripleClearedEvent : GameEvent
    {
        public readonly int Color;

        public TripleClearedEvent(int color)
        {
            Color = color;
        }
    }

    public sealed class WonEvent : GameEvent { }

    public sealed class LostEvent : GameEvent
    {
        public readonly LoseReason Reason;

        public LostEvent(LoseReason reason)
        {
            Reason = reason;
        }
    }

    public sealed class RevivedEvent : GameEvent
    {
        public readonly int TraySize;

        public RevivedEvent(int traySize)
        {
            TraySize = traySize;
        }
    }
}