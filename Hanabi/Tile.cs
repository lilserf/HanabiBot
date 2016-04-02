using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    public enum Suit
    {
        Red,
        Green,
        Blue,
        Yellow,
        White,
        Rainbow
    }

    public class Tile
    {
        const int FIRST = 0;
        const int RED = 0;
        const int GREEN = 1;
        const int BLUE = 2;
        const int YELLOW = 3;
        const int WHITE = 4;
        const int LAST_STANDARD = 4;
        const int RAINBOW = 5;
        const int LAST_RAINBOW = 4;

        public Suit Suit { get; private set; }
        public int Number { get; private set; }
        public Guid UniqueId { get; private set; }

        public Tile(Suit s, int n)
        {
            Suit = s;
            Number = n;
            UniqueId = Guid.NewGuid();
        }

        public override string ToString()
        {
            return System.Enum.GetName(typeof(Suit), Suit) + " " + Number;
        }

        public bool Same(Tile other)
        {
            return other.Suit == this.Suit && other.Number == this.Number;
        }
    }

    public class TileComparer : IEqualityComparer<Tile>
    {
        bool IEqualityComparer<Tile>.Equals(Tile x, Tile y)
        {
            return x.Same(y);
        }

        int IEqualityComparer<Tile>.GetHashCode(Tile obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}
