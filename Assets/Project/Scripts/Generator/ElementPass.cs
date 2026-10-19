using System.Collections.Generic;
using TrayMatch.Core;

namespace TrayMatch.Generator
{
    // What an element pass can see and change while a level is built.
    public sealed class GenerationContext
    {
        public readonly Recipe Recipe;
        public readonly Mulberry32 Rng;
        public readonly int Width;
        public readonly int Height;

        // Layout: cells no piece may use or pass through (walls). Index = y * Width + x.
        public readonly bool[] Blocked;

        // Decorate: the colored blocks, in level order.
        public List<BlockData> Blocks;

        internal GenerationContext(Recipe recipe, Mulberry32 rng)
        {
            Recipe = recipe;
            Rng = rng;
            Width = recipe.Width;
            Height = recipe.Height;
            Blocked = new bool[Width * Height];
        }
    }

    // The generator part of an element (docs/ARCHITECTURE.md §5, "What a new element needs").
    // Passes run in recipe order; a pass that doesn't use the RNG leaves the level unchanged.
    public interface IElementPass
    {
        // Stage 1, before pieces are placed.
        void Layout(GenerationContext context, ElementSettings settings);

        // Stage 4, after colors, before the solution is verified. Changes must keep the
        // solution valid (e.g. ice N <= blocks removed before it).
        void Decorate(GenerationContext context, ElementSettings settings);
    }

    // Placeholder for elements whose rules aren't built yet (walls, ice: V1 item 4).
    sealed class NoOpPass : IElementPass
    {
        public void Layout(GenerationContext context, ElementSettings settings) { }

        public void Decorate(GenerationContext context, ElementSettings settings) { }
    }
}