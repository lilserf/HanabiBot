using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    class InfoGiverBot : IPlayer
    {
        private InfoTrackerModule m_infoTracker;
        private Random m_random;
        private GameState m_gameState;

        public int PlayerIndex
        {
            get;
            set;
        }

        public InfoGiverBot()
        {
            m_infoTracker = new InfoTrackerModule();
            m_random = new Random();
        }


        public void TileDrawn(int playerIndex, Tile newTile, IEnumerable<Tile> newHand)
        {
            
        }

        public void Update(GameState gameState)
        {
            m_gameState = gameState;

            // TODO: this is stupid
            m_infoTracker.PlayerIndex = PlayerIndex;
            m_infoTracker.Update(m_gameState);
        }

        public PlayerAction TakeTurn()
        {
            var actions = m_infoTracker.GetBestActions();

            return actions.FirstOrDefault();
        }





    }
}
