using System;

namespace StockPicker.Game.Core
{
    [Serializable]
    public struct PlayerPortfolio
    {
        public string DisplayName;
        public bool IsHuman;
        public int CashCents;
        public int[] SharesByCommodity;

        public static PlayerPortfolio CreateNew(string name, bool isHuman, int startingCash)
        {
            var p = new PlayerPortfolio
            {
                DisplayName = name,
                IsHuman = isHuman,
                CashCents = startingCash,
                SharesByCommodity = new int[6]
            };
            return p;
        }

        public int NetWorthCents(ReadOnlySpan<int> pricesCents)
        {
            long total = CashCents;
            for (var i = 0; i < 6; i++)
                total += (long)SharesByCommodity[i] * pricesCents[i];
            return (int)Math.Min(int.MaxValue, total);
        }
    }
}
