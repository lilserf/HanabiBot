using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
 
    /// <summary>
    /// Struct that describes an action a player can take
    /// </summary>
    public struct PlayerAction
    {
        // Misc constant used for invalid fields
        public const int INVALID = -1;

        // Type of action being taken
        public enum PlayerActionType
        {
            Invalid,
            Play,
            Discard,
            Info
        }

        // Type of info being given
        public enum PlayerActionInfoType
        {
            Invalid,
            Suit,
            Number
        }

        // String describing this action in the player's terms
        public string LogString;

        // Acting player
        public int ActingPlayer;

        // Type of action
        public PlayerActionType ActionType;

        // Tile involved (if PlayerActionType.Play or .Discard)
        public Guid TileId;

        // Player involved (if PlayerActionType.Info)
        public int TargetPlayer;

        // Type of info (if PlayerActionType.Info)
        public PlayerActionInfoType InfoType;

        // Suit or number of tile (if PlayerActionType.Info)
        public int Info;

        /// <summary>
        /// Static helper - construct a PlayerAction where player A plays tile B
        /// </summary>
        public static PlayerAction PlayTile(int actingPlayer, Guid tileId, string log)
        {
            PlayerAction pa = new PlayerAction();
            pa.ActingPlayer = actingPlayer;
            pa.ActionType = PlayerActionType.Play;
            pa.TileId = tileId;
            pa.TargetPlayer = INVALID;
            pa.InfoType = PlayerActionInfoType.Invalid;
            pa.Info = INVALID;
            pa.LogString = log;
            return pa;
        }

        /// <summary>
        /// Static helper - construct a PlayerAction where player A discards tile B
        /// </summary>
        public static PlayerAction DiscardTile(int actingPlayer, Guid tileId, string log)
        {
            PlayerAction pa = new PlayerAction();
            pa.ActingPlayer = actingPlayer;
            pa.ActionType = PlayerActionType.Discard;
            pa.TileId = tileId;
            pa.TargetPlayer = INVALID;
            pa.InfoType = PlayerActionInfoType.Invalid;
            pa.Info = INVALID;
            pa.LogString = log;
            return pa;
        }

        /// <summary>
        /// Static helper - construct a PlayerAction where player A gives player B info C of type D
        /// </summary>
        public static PlayerAction GiveInfo(int actingPlayer, PlayerActionInfoType infoType, int targetPlayer, int info, string log)
        {
            PlayerAction pa = new PlayerAction();
            pa.ActingPlayer = actingPlayer;
            pa.ActionType = PlayerActionType.Info;
            pa.TileId = Guid.Empty;
            pa.TargetPlayer = targetPlayer;
            pa.InfoType = infoType;
            pa.Info = info;
            pa.LogString = log;
            return pa;
        }

        /// <summary>
        /// Describe this action
        /// </summary>
        public override string ToString()
        {
            switch(ActionType)
            {
                case PlayerActionType.Play:
                    {
                        return String.Format("Play tile {0} ({1})", TileId, LogString);
                    }
                case PlayerActionType.Discard:
                    {
                        return String.Format("Discard tile {0} ({1})", TileId, LogString);
                    }
                case PlayerActionType.Info:
                    {
                        String desc = "";
                        if(InfoType == PlayerActionInfoType.Suit)
                        {
                            desc = System.Enum.GetName(typeof(Suit), (Suit)Info);
                        }
                        else
                        {
                            desc = "" + Info;
                        }

                        return String.Format("Player {0} has {1} ({2})", TargetPlayer, desc, LogString);
                    }
                default:
                    return String.Format("PlayerAction.ToString Error! ({0})", LogString);
            }
        }
    }

    /// <summary>
    /// Interface for a Player
    /// </summary>
    public interface IPlayer
    {
        /// <summary>
        /// Player Index for this player
        /// </summary>
        int PlayerIndex { get; set; }

        /// <summary>
        /// Called each turn with the newest game state
        /// </summary>
        void Update(GameState gameState);

        /// <summary>
        /// This bot needs to take a turn now
        /// </summary>
        PlayerAction TakeTurn();
    }
}
