namespace TrayMatch.Generator
{
    // Seeded random numbers, bit-for-bit the same as mulberry32() in prototype/web/index.html,
    // so the same seed gives the same level in Unity and in the browser.
    public sealed class Mulberry32
    {
        uint _state;

        public Mulberry32(int seed)
        {
            _state = unchecked((uint)seed);
        }

        // A number in [0, 1), like the prototype's rng().
        public double Next()
        {
            unchecked
            {
                _state += 0x6D2B79F5;
                uint t = _state;
                t = (t ^ (t >> 15)) * (t | 1);
                t = (t + (t ^ (t >> 7)) * (t | 61)) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        // An integer in [0, n), like the prototype's rng() % n.
        public int Next(int n) => (int)(Next() * n);
    }
}