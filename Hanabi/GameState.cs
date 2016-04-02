using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    public struct Turn
    {
        public PlayerAction Action;
        public List<Guid> TargetedTiles;
    }

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

    public struct GameState
    {
        public int Viewpoint;
        public readonly IEnumerable<Tile> Draw;
        public readonly IEnumerable<Tile> Discard;
        public readonly IEnumerable<Tile> Play;
        public readonly IEnumerable<Turn> History;

        public readonly IEnumerable<Guid> YourHand;
        public readonly IDictionary<int, IEnumerable<Tile>> Hands;
        public readonly IDictionary<Suit, int> NextPlay;

        public readonly int Fuses;
        public readonly int Tokens;

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

        public bool IsPlayable(Tile t)
        {
            return NextPlay[t.Suit] == t.Number;
        }

        public IEnumerable<Tile> AllHands()
        {
            return Hands.Values.Aggregate(new List<Tile>(), (l, h) => { l.AddRange(h); return l; });
        }
    }
}
