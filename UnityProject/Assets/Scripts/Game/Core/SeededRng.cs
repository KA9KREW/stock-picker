namespace StockPicker.Game.Core
{
    /// <summary>
    /// Deterministic RNG (xorshift64*) for reproducible rolls.
    /// </summary>
    public struct SeededRng
    {
        private ulong _state;

        public ulong State
        {
            get => _state;
            set => _state = value == 0 ? 0x9E3779B97F4A7C15UL : value;
        }

        public SeededRng(int seed)
        {
            _state = (ulong)(uint)seed;
            if (_state == 0) _state = 0x9E3779B97F4A7C15UL;
        }

        public uint NextUInt()
        {
            var x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return (uint)(x * 0x2545F4914F6CDD1DUL >> 32);
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;
            return (int)(NextUInt() % (uint)maxExclusive);
        }

        public int NextRange(int minInclusive, int maxExclusive) =>
            minInclusive + NextInt(maxExclusive - minInclusive);
    }
}
