using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    /// <summary>
    /// Suits in Hanabi!
    /// </summary>
    public enum Suit
    {
        Red,
        Green,
        Blue,
        Yellow,
        White,
        Rainbow
    }

    /// <summary>
    /// Represents a fully visible tile you can see
    /// </summary>
    public class Tile
    {
        // Suit (red/green/blue/etc)
        public Suit Suit { get; private set; }
        // Number on the tile
        public int Number { get; private set; }
        // Unique ID of this tile so bots can say "that one"
        public Guid UniqueId { get; private set; }

        // Constructor
        public Tile(Suit s, int n)
        {
            Suit = s;
            Number = n;
            UniqueId = Guid.NewGuid();
        }

        /// <summary>
        /// String representation of this tile
        /// </summary>
        public override string ToString()
        {
            return System.Enum.GetName(typeof(Suit), Suit) + " " + Number;
        }

        /// <summary>
        /// Is this functionally the same tile as another (not actually the same tile, but same suit & number)?
        /// </summary>
        public bool Same(Tile other)
        {
            return other.Suit == this.Suit && other.Number == this.Number;
        }
    }

    /// <summary>
    /// Used when grouping Tiles - compare whether they're copies of each other, not literally the same tile
    /// </summary>
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
