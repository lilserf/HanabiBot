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
        /// How many players are there?
        /// </summary>
        public readonly int NumPlayers;

        /// <summary>
        /// Constructor
        /// </summary>
        public GameState(int playerViewpoint, int tokens, IEnumerable<Tile> discard, IEnumerable<Tile> play, IEnumerable<Guid> yourHand, IDictionary<int, IEnumerable<Tile>> hands, IDictionary<Suit, int> nextPlays, int fuses, IEnumerable<Turn> history)
        {
            Viewpoint = playerViewpoint;
            Discard = discard;
            Play = play;
            YourHand = yourHand;
            Hands = hands;
            Fuses = fuses;
            Tokens = tokens;
            NextPlay = nextPlays;
            History = history;
            NumPlayers = hands.Count() + 1;
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
        /// Try to get a Tile from its ID. Returns the Tile if it's visible or null if we can't see that tile
        /// </summary>
        /// <param name="tileId"></param>
        /// <returns></returns>
        public Tile TryGetTile(Guid tileId)
        {
            return AllHands().Concat(Discard).Concat(Play).Where(t => t.UniqueId == tileId).FirstOrDefault();
        }

        /// <summary>
        /// Is this tile dead? "Alive" means it might still need to be played. "Dead" means it's not needed (if it's in a hand) or already played/discarded.
        /// </summary>
        public bool IsDead(Guid tileId)
        {
            Tile t = TryGetTile(tileId);

            if(t != null)
            {
                return IsDead(t.Suit, t.Number);
            }

            return false;
        }

        /// <summary>
        /// Is the tile with this suit and number dead?
        /// </summary>
        public bool IsDead(Suit s, int n)
        {
            bool dead = false;
            if(NextPlay[s] > n)
            {
                dead = true;
            }
            else
            {
                for(int i = n; i > 1; i--)
                {
                    if(Discard.Where(t => t.Suit == s && t.Number == i).Count() == NumCopies(s, i))
                    {
                        dead = true;
                        break;
                    }
                }
            }

            return dead;
        }
        
        /// <summary>
        /// How many copies exist with this suit and number?
        /// </summary>
        public int NumCopies(Suit s, int n)
        {
            if (n == 5) return 1;
            else if (n == 1) return 3;
            else return 2;
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
