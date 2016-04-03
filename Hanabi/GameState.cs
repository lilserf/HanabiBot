using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    /// <summary>
    /// A turn consists of a chosen PlayerAction and a list of tiles it targets (or null if it's not an Info action)
    /// </summary>
    public struct Turn
    {
        public PlayerAction Action;
        public List<Guid> TargetedTiles;
    }

    /// <summary>
    /// A GameOutcome consists of the points you scored plus the reason the game ended
    /// </summary>
    public struct GameOutcome
    {
        public enum GameEndReason
        {
            Fuses,
            EmptyDrawPile,
            PerfectGame,
            NotEnded
        }

        public int Points;
        public GameEndReason EndReason;
    }

    /// <summary>
    /// Player-facing depiction of the game state from a specific viewpoint
    /// </summary>
    public struct GameState
    {
        /// <summary>
        /// Which player sees this game state?
        /// </summary>
        public int Viewpoint;
        /// <summary>
        /// Draw pile
        /// </summary>
        public readonly IEnumerable<Tile> Draw;
        /// <summary>
        /// Discard pile
        /// </summary>
        public readonly IEnumerable<Tile> Discard;
        /// <summary>
        /// Play "pile" - all tiles that have been played
        /// </summary>
        public readonly IEnumerable<Tile> Play;

        /// <summary>
        /// History of what happened on each turn
        /// </summary>
        public readonly IEnumerable<Turn> History;

        /// <summary>
        /// List of just the Guids of tiles in your hand
        /// </summary>
        public readonly IEnumerable<Guid> YourHand;
        /// <summary>
        /// Dictionary containing lists of actual Tiles for the other players hands
        /// </summary>
        public readonly IDictionary<int, IEnumerable<Tile>> Hands;
        
        /// <summary>
        /// What's the next valid play number for each suit?
        /// </summary>
        public readonly IDictionary<Suit, int> NextPlay;

        /// <summary>
        /// How many fuses are blown
        /// </summary>
        public readonly int Fuses;
        /// <summary>
        /// How many tokens are available
        /// </summary>
        public readonly int Tokens;

        /// <summary>
        /// Constructor
        /// </summary>
        public GameState(int playerViewpoint, int tokens, IEnumerable<Tile> draw, IEnumerable<Tile> discard, IEnumerable<Tile> play, IEnumerable<Guid> yourHand, IDictionary<int, IEnumerable<Tile>> hands, IDictionary<Suit, int> nextPlays, int fuses, IEnumerable<Turn> history)
        {
            Viewpoint = playerViewpoint;
            Draw = draw;
            Discard = discard;
            Play = play;
            YourHand = yourHand;
            Hands = hands;
            Fuses = fuses;
            Tokens = tokens;
            NextPlay = nextPlays;
            History = history;
        }

        /// <summary>
        /// Is the given tile playable?
        /// </summary>
        public bool IsPlayable(Suit s, int n)
        {
            return NextPlay[s] == n;
        }

        /// <summary>
        /// Is the given tile playable?
        /// </summary>
        public bool IsPlayable(Tile t)
        {
            return IsPlayable(t.Suit, t.Number);
        }

        /// <summary>
        /// Get a single list of all tiles in the other players' hands
        /// </summary>
        public IEnumerable<Tile> AllHands()
        {
            return Hands.Values.Aggregate(new List<Tile>(), (l, h) => { l.AddRange(h); return l; });
        }

        
        /// <summary>
        /// Return the player index of the player holding the tile with this ID, or -1 if nobody's holding that tile
        /// </summary>
        public int WhoHas(Guid tileId)
        {
            if(YourHand.Contains(tileId))
            {
                return Viewpoint;
            }
            else
            {
                foreach(var hand in Hands)
                {
                    if(hand.Value.Any(t => t.UniqueId == tileId))
                    {
                        return hand.Key;
                    }
                }
            }

            return -1;
        }
    }
}
