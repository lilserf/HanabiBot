using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    /// <summary>
    /// Bot that uses the InfoTrackerModule to manage its info
    /// </summary>
    class InfoGiverBot : IPlayer
    {
        /// <summary>
        /// Info tracker module
        /// </summary>
        private InfoTrackerModule m_infoTracker;

        /// <summary>
        /// What's our player index?
        /// </summary>
        public int PlayerIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public InfoGiverBot()
        {
            m_infoTracker = new InfoTrackerModule();
        }

        /// <summary>
        /// Update the bot with the new game state for this turn
        /// </summary>
        /// <param name="gameState"></param>
        public void Update(GameState gameState)
        {
            // TODO: this is stupid
            m_infoTracker.PlayerIndex = PlayerIndex;
            m_infoTracker.Update(gameState);
        }

        /// <summary>
        /// Okay bot, take your turn - what action should you do?
        /// </summary>
        /// <returns></returns>
        public PlayerAction TakeTurn()
        {
            var actions = m_infoTracker.GetBestActions();
            // Just trust what InfoTrackerModule thought was best
            return actions.FirstOrDefault();
        }





    }
}
