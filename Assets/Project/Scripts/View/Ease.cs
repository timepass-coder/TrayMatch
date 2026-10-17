
using UnityEngine;

namespace TrayMatch.View
{
    // Easing curves, same formulas as the web prototype. k goes from 0 to 1.
    public static class Ease
    {
        public static float OutCubic(float k) => 1f - Mathf.Pow(1f - k, 3f);

        public static float InOutCubic(float k) =>
            k < 0.5f ? 4f * k * k * k : 1f - Mathf.Pow(-2f * k + 2f, 3f) / 2f;

        // Overshoots slightly past 1 before settling: a "pop".
        public static float OutBack(float k)
        {
            const float c = 1.7f;
            return 1f + (c + 1f) * Mathf.Pow(k - 1f, 3f) + c * Mathf.Pow(k - 1f, 2f);
        }
    }
}
