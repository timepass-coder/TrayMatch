using System;
using UnityEngine;

namespace TrayMatch.View
{
    // Placeholder art generated at startup, so the project needs no imported sprites yet.
    // All sprites are white (tint them with SpriteRenderer.color) and 1 world unit wide.
    public static class SpriteFactory
    {
        const int Size = 128;
        const int Supersample = 4;
        const float CornerRadius = 0.22f; // fraction of the size, as in the prototype

        static Sprite _roundedSquare, _arrow, _circle;

        // Has 9-slice borders, so it can also be drawn stretched (SpriteDrawMode.Sliced).
        public static Sprite RoundedSquare => _roundedSquare != null ? _roundedSquare
            : (_roundedSquare = Create("RoundedSquare", RoundedSquareCoverage, Mathf.CeilToInt(CornerRadius * Size) + 2));

        // Points up; rotate the transform for other directions.
        public static Sprite Arrow => _arrow != null ? _arrow : (_arrow = Create("Arrow", ArrowCoverage, 0));

        public static Sprite Circle => _circle != null ? _circle : (_circle = Create("Circle", CircleCoverage, 0));

        static Sprite Create(string name, Func<float, float, bool> inside, int border)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            var pixels = new Color32[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int hits = 0;

                    for (int sy = 0; sy < Supersample; sy++)
                        for (int sx = 0; sx < Supersample; sx++)
                        {
                            // u, v in [-0.5, 0.5], v pointing up
                            float u = (x + (sx + 0.5f) / Supersample) / Size - 0.5f;
                            float v = (y + (sy + 0.5f) / Supersample) / Size - 0.5f;

                            if (inside(u, v)) hits++;
                        }

                    pixels[y * Size + x] = new Color32(
                        255, 255, 255,
                        (byte)(255 * hits / (Supersample * Supersample))
                    );
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, Size, Size),
                new Vector2(0.5f, 0.5f),
                Size,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border)
            );

            sprite.name = name;
            return sprite;
        }

        static bool RoundedSquareCoverage(float u, float v)
        {
            float qx = Mathf.Abs(u) - 0.5f + CornerRadius;
            float qy = Mathf.Abs(v) - 0.5f + CornerRadius;
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            float dist = outside + Mathf.Min(Mathf.Max(qx, qy), 0) - CornerRadius;

            return dist <= 0;
        }

        // Same shape as the prototype's arrow: a round-capped shaft plus a triangular head.
        static bool ArrowCoverage(float u, float v)
        {
            const float shaftBottom = -0.21f, shaftTop = 0.07f, halfWidth = 0.055f;

            float cy = Mathf.Clamp(v, shaftBottom, shaftTop);

            if (u * u + (v - cy) * (v - cy) <= halfWidth * halfWidth)
                return true;

            return InTriangle(
                u, v,
                new Vector2(-0.2f, 0.05f),
                new Vector2(0f, 0.30f),
                new Vector2(0.2f, 0.05f)
            );
        }

        static bool CircleCoverage(float u, float v) => u * u + v * v <= 0.25f;

        static bool InTriangle(float px, float py, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(px, py, a, b);
            float d2 = Cross(px, py, b, c);
            float d3 = Cross(px, py, c, a);

            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;

            return !(hasNeg && hasPos);
        }

        static float Cross(float px, float py, Vector2 a, Vector2 b) =>
            (px - b.x) * (a.y - b.y) - (a.x - b.x) * (py - b.y);
    }
}