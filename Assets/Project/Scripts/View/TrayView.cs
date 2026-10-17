
using System.Collections.Generic;
using UnityEngine;

namespace TrayMatch.View
{
    // The tray under the board. GameState has already decided the outcome of every tap; this view
    // catches up as blocks land. Items sit next to their own color, and three of a color pop and clear.
    public sealed class TrayView : MonoBehaviour
    {
        public const float SlotSize = 0.8f; // world units
        public const float ItemSize = SlotSize * 0.82f;

        const float ClearDelay = 0.18f;
        const float SlideSpeed = 16f;
        const float PopSpeed = 6f;
        const float DangerPulseSpeed = 10f;
        const int BackgroundOrder = 20;
        const int SlotOrder = 21;
        const int ItemOrder = 22;

        static readonly Color DangerColor =
            new Color(224 / 255f, 49 / 255f, 49 / 255f, 1f);

        sealed class Item
        {
            public int Color;
            public Transform Root;
            public float Pos;
            public float Pop;
            public float Clearing = -1f;
        }

        readonly List<Item> _items = new List<Item>();
        readonly List<GameObject> _slots = new List<GameObject>();

        SpriteRenderer _background;
        int _size;
        bool _danger;
        float _time;

        public void Build(int size)
        {
            foreach (var item in _items)
                Destroy(item.Root.gameObject);

            _items.Clear();
            _danger = false;
            SetSize(size);
        }

        // Also for the revive (+1 slot) in step 4c: items stay where they are.
        public void SetSize(int size)
        {
            _size = size;

            if (_background == null)
            {
                _background = new GameObject("Background")
                    .AddComponent<SpriteRenderer>();

                _background.transform.SetParent(transform, false);
                _background.sprite = SpriteFactory.RoundedSquare;
                _background.drawMode = SpriteDrawMode.Sliced;
                _background.sortingOrder = BackgroundOrder;
            }

            _background.size =
                new Vector2(size * SlotSize + 0.27f, SlotSize + 0.27f);

            foreach (var slot in _slots)
                Destroy(slot);

            _slots.Clear();

            for (int i = 0; i < size; i++)
            {
                var slot = new GameObject($"Slot {i}")
                    .AddComponent<SpriteRenderer>();

                slot.transform.SetParent(transform, false);
                slot.transform.localPosition = SlotLocal(i);
                slot.transform.localScale =
                    Vector3.one * (SlotSize * 0.86f);

                slot.sprite = SpriteFactory.RoundedSquare;
                slot.color = Palette.Dot;
                slot.sortingOrder = SlotOrder;

                _slots.Add(slot.gameObject);
            }
        }

        // Pulses red while only one slot is left.
        public void SetDanger(bool danger) => _danger = danger;

        // Where a block of this color would land right now.
        public Vector3 TargetFor(int color) =>
            transform.TransformPoint(SlotLocal(InsertIndex(color)));

        public void Land(int color)
        {
            int index = InsertIndex(color);

            var item = new Item
            {
                Color = color,
                Root = CreateTile(color),
                Pos = index
            };

            item.Root.localPosition = SlotLocal(index);
            item.Root.localScale = Vector3.zero;

            _items.Insert(index, item);

            int same = 0;

            foreach (var it in _items)
                if (it.Color == color && it.Clearing < 0f)
                    same++;

            if (same >= 3)
            {
                foreach (var it in _items)
                    if (it.Color == color && it.Clearing < 0f)
                        it.Clearing = 0f;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _time += dt;

            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];

                it.Pos += (i - it.Pos) * Mathf.Min(1f, dt * SlideSpeed);
                it.Pop = Mathf.Min(1f, it.Pop + dt * PopSpeed);

                float scale = Ease.OutBack(it.Pop);

                if (it.Clearing >= 0f)
                {
                    it.Clearing += dt;
                    scale *= 1f + 0.35f *
                        Mathf.Sin(Mathf.Min(1f, it.Clearing / ClearDelay) * Mathf.PI);
                }

                it.Root.localPosition = SlotLocal(it.Pos);
                it.Root.localScale = Vector3.one * (ItemSize * scale);
            }

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].Clearing > ClearDelay)
                {
                    Destroy(_items[i].Root.gameObject);
                    _items.RemoveAt(i);
                }
            }

            if (_background != null)
            {
                float pulse = 0.5f + 0.5f *
                    Mathf.Sin(_time * DangerPulseSpeed);

                _background.color = _danger
                    ? Color.Lerp(
                        Palette.Board,
                        DangerColor,
                        0.35f + 0.35f * pulse)
                    : Palette.Board;
            }
        }

        // Next to the last item of the same color, otherwise at the end.
        int InsertIndex(int color)
        {
            int last = -1;

            for (int i = 0; i < _items.Count; i++)
                if (_items[i].Color == color && _items[i].Clearing < 0f)
                    last = i;

            return last >= 0 ? last + 1 : _items.Count;
        }

        Vector3 SlotLocal(float index) =>
            new Vector3(
                (index + 0.5f - _size / 2f) * SlotSize,
                0,
                0);

        Transform CreateTile(int color)
        {
            var root = new GameObject($"Item (color {color})").transform;
            root.SetParent(transform, false);

            Color c = Palette.Block(color);

            AddPart(root, "Shadow", Palette.Shade(c, -0.35f), -0.035f, ItemOrder);
            AddPart(root, "Body", c, 0.035f, ItemOrder + 1);

            return root;
        }

        static void AddPart(
            Transform parent,
            string name,
            Color color,
            float y,
            int sortingOrder)
        {
            var sr = new GameObject(name)
                .AddComponent<SpriteRenderer>();

            sr.transform.SetParent(parent, false);
            sr.transform.localPosition = new Vector3(0, y, 0);
            sr.transform.localScale = new Vector3(1f, 0.93f, 1f);
            sr.sprite = SpriteFactory.RoundedSquare;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
        }
    }
}
