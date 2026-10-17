using System.Collections.Generic;
using TrayMatch.Core;
using UnityEngine;

namespace TrayMatch.View
{
    // Draws the board and its blocks. 1 cell = 1 world unit; the board is centered on this
    // transform. Grid Y grows downward (row 0 on top), world Y grows upward.
    public sealed class BoardView : MonoBehaviour
    {
        const int BackgroundOrder = 0;
        const int DotOrder = 1;
        const int BlockOrder = 10;

        readonly Dictionary<int, BlockView> _blocks = new Dictionary<int, BlockView>();
        int _width, _height;

        public void Build(GameState state)
        {
            Clear();
            _width = state.Width;
            _height = state.Height;

            var bg = new GameObject("Background").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(transform, false);
            bg.sprite = SpriteFactory.RoundedSquare;
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(_width + 0.27f, _height + 0.27f);
            bg.color = Palette.Board;
            bg.sortingOrder = BackgroundOrder;

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    var dot = new GameObject($"Dot ({x},{y})").AddComponent<SpriteRenderer>();
                    dot.transform.SetParent(transform, false);
                    dot.transform.localPosition = CellToLocal(x, y);
                    dot.transform.localScale = Vector3.one * 0.12f;
                    dot.sprite = SpriteFactory.Circle;
                    dot.color = Palette.Dot;
                    dot.sortingOrder = DotOrder;
                }
            }

            foreach (var block in state.Blocks)
            {
                _blocks[block.Index] = BlockView.Create(
                    transform, block, CellToLocal(block.X, block.Y), BlockOrder);
            }
        }

        public Vector3 CellToLocal(int x, int y) =>
            new Vector3(x + 0.5f - _width / 2f, _height / 2f - y - 0.5f, 0);

        public bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            Vector3 p = transform.InverseTransformPoint(world);
            x = Mathf.FloorToInt(p.x + _width / 2f);
            y = Mathf.FloorToInt(_height / 2f - p.y);

            return x >= 0 && y >= 0 && x < _width && y < _height;
        }

        // Step 4a: blocks simply disappear. Step 4b replaces this with the slide-out animation.
        public void RemoveBlocks(IReadOnlyList<int> indices)
        {
            foreach (int i in indices)
            {
                if (_blocks.TryGetValue(i, out var view))
                {
                    Destroy(view.gameObject);
                    _blocks.Remove(i);
                }
            }
        }

        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _blocks.Clear();
        }
    }
}