using TrayMatch.Core;
using UnityEngine;

namespace TrayMatch.View
{
    // One block on the board: shadow + body + arrow. Animations come in step 4b.
    public sealed class BlockView : MonoBehaviour
    {
        public const float Size = 0.86f; // fraction of a cell, as in the prototype

        public int Index { get; private set; }

        public static BlockView Create(Transform parent, BlockState block, Vector3 localPosition, int sortingOrder)
        {
            var go = new GameObject($"Block {block.Index} ({block.X},{block.Y})");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var view = go.AddComponent<BlockView>();
            view.Index = block.Index;

            // The body is slightly shorter than wide and sits on a darker "shadow" copy,
            // which gives the prototype its 3D look.
            Color color = Palette.Block(block.Color);
            var bodyScale = new Vector3(Size, Size * 0.93f, 1);

            AddPart(go.transform, "Shadow", SpriteFactory.RoundedSquare,
                new Vector3(0, -0.035f * Size, 0), bodyScale,
                Palette.Shade(color, -0.35f), sortingOrder);

            AddPart(go.transform, "Body", SpriteFactory.RoundedSquare,
                new Vector3(0, 0.035f * Size, 0), bodyScale,
                color, sortingOrder + 1);

            var arrow = AddPart(go.transform, "Arrow", SpriteFactory.Arrow,
                Vector3.zero, new Vector3(Size, Size, 1), Color.white,
                sortingOrder + 2);

            arrow.transform.localRotation = Quaternion.Euler(0, 0, -90f * (int)block.Dir);

            return view;
        }

        static SpriteRenderer AddPart(Transform parent, string name, Sprite sprite,
            Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            return sr;
        }
    }
}