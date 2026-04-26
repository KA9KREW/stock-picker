namespace StockPicker.Game.Core
{
    public enum MovementKind : byte
    {
        Up = 0,
        Down = 1,
        Dividend = 2
    }

    /// <summary>
    /// Three-dice outcome: commodity, direction/dividend, magnitude in cents.
    /// </summary>
    public readonly struct DiceRoll
    {
        public readonly CommodityId Commodity;
        public readonly MovementKind Movement;
        public readonly int Cents;

        public DiceRoll(CommodityId commodity, MovementKind movement, int cents)
        {
            Commodity = commodity;
            Movement = movement;
            Cents = cents;
        }

        public override string ToString() =>
            $"{Commodity} {Movement} {Cents}c";
    }
}
