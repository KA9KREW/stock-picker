namespace StockPicker.Game.Core
{
    public static class DiceService
    {
        private static readonly int[] Magnitudes = { 5, 10, 20 };

        public static DiceRoll Roll(ref SeededRng rng)
        {
            var c = (CommodityId)rng.NextInt(6);
            var m = (MovementKind)rng.NextInt(3);
            var cents = Magnitudes[rng.NextInt(3)];
            return new DiceRoll(c, m, cents);
        }
    }
}
