using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
 
    public struct PlayerAction
    {
        public const int INVALID = -1;

        public enum PlayerActionType
        {
            Invalid,
            Play,
            Discard,
            Info
        }

        public enum PlayerActionInfoType
        {
            Invalid,
            Suit,
            Number
        }

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

        public static PlayerAction PlayTile(int actingPlayer, Guid tileId)
        {
            PlayerAction pa = new PlayerAction();
            pa.ActingPlayer = actingPlayer;
            pa.ActionType = PlayerActionType.Play;
            pa.TileId = tileId;
            pa.TargetPlayer = INVALID;
            pa.InfoType = PlayerActionInfoType.Invalid;
            pa.Info = INVALID;
            return pa;
        }

        public static PlayerAction DiscardTile(int actingPlayer, Guid tileId)
        {
            PlayerAction pa = new PlayerAction();
            pa.ActingPlayer = actingPlayer;
            pa.ActionType = PlayerActionType.Discard;
            pa.TileId = tileId;
            pa.TargetPlayer = INVALID;
            pa.InfoType = PlayerActionInfoType.Invalid;
            pa.Info = INVALID;
            return pa;
        }

        public static PlayerAction GiveInfo(int actingPlayer, PlayerActionInfoType infoType, int targetPlayer, int info)
        {
            PlayerAction pa = new PlayerAction();
            pa.ActingPlayer = actingPlayer;
            pa.ActionType = PlayerActionType.Info;
            pa.TileId = Guid.Empty;
            pa.TargetPlayer = targetPlayer;
            pa.InfoType = infoType;
            pa.Info = info;
            return pa;
        }

        public override string ToString()
        {
            switch(ActionType)
            {
                case PlayerActionType.Play:
                    {
                        return "Play tile " + TileId;
                    }
                case PlayerActionType.Discard:
                    {
                        return "Discard tile " + TileId;
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
                        return "Player " + TargetPlayer + " has " + desc;
                    }
                default:
                    return "PlayerAction.ToString Error!";
            }
        }
    }

    /*
    public class PlayerActionInfoSuit : IPlayerAction
    {
        public readonly int TargetPlayerIndex;
        public readonly Suit Suit;
        public readonly IEnumerable<Guid> Tiles;

        public PlayerActionInfoSuit(int playerIndex, Suit suit, IEnumerable<Guid> tiles)
        {
            TargetPlayerIndex = playerIndex;
            Suit = suit;
            Tiles = tiles;
        }

        public override string ToString()
        {
            return "Player " + TargetPlayerIndex + " has " + Suit + " at tiles " + Tiles.Aggregate("", (s, i) => s += i + " ");
        }
    }

    public class PlayerActionInfoNumber : IPlayerAction
    {
        public readonly int TargetPlayerIndex;
        public readonly int Number;
        public readonly IEnumerable<Guid> Tiles;

        public PlayerActionInfoNumber(int playerIndex, int number, IEnumerable<Guid> tiles)
        {
            TargetPlayerIndex = playerIndex;
            Number = number;
            Tiles = tiles;
        }

        public override string ToString()
        {
            return "Player " + TargetPlayerIndex + " has " + Number + " at tiles " + Tiles.Aggregate("", (s, i) => s += i + " ");
        }
    }

    public class PlayerActionDiscard : IPlayerAction
    {
        public Guid TileId { get; set; }

        public PlayerActionDiscard(Guid tileId)
        {
            TileId = tileId;
        }

        public override string ToString()
        {
            return "Discard tile " + TileId;
        }
    }

    public class PlayerActionPlay : IPlayerAction
    {
        public Guid TileId { get; set; }

        public PlayerActionPlay(Guid tileId)
        {
            TileId = tileId;
        }

        public override string ToString()
        {
            return "Play tile " + TileId;
        }
    }

    */

    public interface IPlayer
    {
        int PlayerIndex { get; set; }

        void Update(GameState gameState);

        PlayerAction TakeTurn();
    }
}
