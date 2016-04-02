using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    class DummyPlayer : IPlayer
    {
        public int PlayerIndex { get; set; }

        private GameState m_gameState;

        public void Update(GameState gameState)
        {
            m_gameState = gameState;
        }

        public PlayerAction TakeTurn()
        {
            return PlayerAction.PlayTile(PlayerIndex, m_gameState.YourHand.First());
        }

    }
}
