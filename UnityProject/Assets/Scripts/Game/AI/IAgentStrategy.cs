using System.Collections.Generic;
using StockPicker.Game.Core;

namespace StockPicker.Game.AI
{
    public interface IAgentStrategy
    {
        void DecideTrades(GameSession session, int playerIndex, List<TradeOrder> outOrders);
    }
}
