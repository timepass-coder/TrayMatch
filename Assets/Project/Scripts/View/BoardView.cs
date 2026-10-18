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
        GameState _state;

        public void Build(GameState state)
        {
            Clear();
            _state = state;

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
            var view = BlockView.Create(
                transform,
                block,
                CellToLocal(block.X, block.Y),
                BlockOrder
            );

            view.gameObject
                .AddComponent<BlockMotion>()
                .Init(block.Dir, (block.X + block.Y) * 0.025f);

            _blocks[block.Index] = view;
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
            
        public void LaunchToTray(IReadOnlyList<int> indices, TrayView tray)
        {
            float maxDistance = 0f;

            foreach (var i in indices)
                maxDistance = Mathf.Max(
                    maxDistance,
                    DistanceToEdge(_state.Blocks[i]));

            for (int n = 0; n < indices.Count; n++)
            {
                int i = indices[n];

                if (!_blocks.TryGetValue(i, out var view))
                    continue;

                int color = _state.Blocks[i].Color;
                _blocks.Remove(i);

                view.GetComponent<BlockMotion>().Launch(
                    DistanceToEdge(_state.Blocks[i]),
                    maxDistance,
                    n * 0.04f,
                    () => tray.TargetFor(color),
                    () => tray.Land(color));
            }
        }

        // The blocks slide forward `steps` cells, hit a blocker, and come back.
        public void Bump(IReadOnlyList<int> indices, int blocker, int steps)
        {
            float distance = steps + 0.18f;

            foreach (int i in indices)
            {
                if (!_blocks.TryGetValue(i, out var view))
                    continue;

                view.GetComponent<BlockMotion>().Bump(distance);
            }

            if (_blocks.TryGetValue(blocker, out var hit))
                hit.GetComponent<BlockMotion>().Hit(
                    _state.Blocks[indices[0]].Dir,
                    BlockMotion.BumpOutTime(distance));
        }

        // Cells between the block and the board edge in its direction, plus a small overshoot.
        float DistanceToEdge(BlockState b)
        {
            int cells = b.Dir switch
            {
                Direction.Up    => b.Y,
                Direction.Right => _width - 1 - b.X,
                Direction.Down  => _height - 1 - b.Y,
                _               => b.X
            };

            return cells + 0.8f;
        }


        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            _blocks.Clear();
        }
    }
}