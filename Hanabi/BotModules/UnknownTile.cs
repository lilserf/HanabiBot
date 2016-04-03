using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    /// <summary>
    /// Bookkeeping object for a Tile that tracks all the possible values it might have
    /// </summary>
    class UnknownTile
    {
        /// <summary>
        /// Guid of the tile
        /// </summary>
        Guid UniqueId { get; set; }

        /// <summary>
        /// List of all the possible tiles this could be
        /// </summary>
        public List<Tuple<Suit, int>> Possible;

        /// <summary>
        /// Record why we know what we know
        /// </summary>
        public List<string> Reasons;

        /// <summary>
        /// How strong a signal to play this tile have we gotten?
        /// </summary>
        public int PlayStrength = 0;

        /// <summary>
        /// How strong a signal to not discard this tile have we gotten?
        /// </summary>
        public int DontDiscardStrength = 0;

        /// <summary>
        /// How long ago was I given info on this tile?
        /// -1 means I haven't gotten info
        /// </summary>
        public int InfoAge = -1;

        /// <summary>
        /// Constructor
        /// </summary>
        public UnknownTile(Guid uniqueId)
        {
            UniqueId = uniqueId;

            Possible = new List<Tuple<Suit, int>>();
            Reasons = new List<string>();

            // Loop through all known suits
            foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
            {
                // Add all the tile values in each suit
                for (int num = 1; num <= 5; num++)
                {
                    Possible.Add(new Tuple<Suit, int>(s, num));
                }
            }

        }

        // We got info on this tile!
        public void GotInfo()
        {
            InfoAge = 0;
        }

        // Increase the age of our info
        public void AgeInfo()
        {
            if (InfoAge >= 0)
            {
                InfoAge++;
            }
        }

        private void SanityCheck()
        {
            Debug.Assert(Possible.Any());
        }

        /// <summary>
        /// Set the possibilities for this tile to the combinations in the list IF
        /// that's consistent with what was already possible for this tile
        /// </summary>
        /// <param name="list"></param>
        public void MightBe(IEnumerable<Tuple<Suit, int>> list, string reason = "")
        {
            int count = Possible.Count();
            var overlap = Possible.Where(e => list.Contains(e));

            if(overlap.Any())
            {
                Possible = overlap.ToList();
                if(count != Possible.Count())
                Reasons.Add("MightBe(list) - " + reason + " - " + ToString());
            }
            SanityCheck();
        }

        /// <summary>
        /// Directly set the possibilities for this tile to the provided list
        /// </summary>
        public void MustBe(IEnumerable<Tuple<Suit, int>> list, string reason = "")
        {
            int count = Possible.Count();
            Possible = list.ToList();
            if (count != Possible.Count())
            {
                Reasons.Add("MustBe(list) - " + reason + " - " + ToString());
            }
            SanityCheck();
        }

        /// <summary>
        /// This tile MUST BE a given suit
        /// </summary>
        public void MustBe(Suit suit, string reason = "")
        {
            int count = Possible.Count();
            Possible = Possible.Where(t => t.Item1 == suit).ToList();
            if (count != Possible.Count())
            {
                Reasons.Add("MustBe(" + suit + ") - " + reason + " - " + ToString());
            }
            SanityCheck();
        }

        /// <summary>
        /// This tile MUST BE a given number
        /// </summary>
        public void MustBe(int number, string reason = "")
        {
            int count = Possible.Count();
            Possible = Possible.Where(t => t.Item2 == number).ToList();
            if (count != Possible.Count())
            {
                Reasons.Add("MustBe(" + number + ") - " + reason + " - " + ToString());
            }
            SanityCheck();
        }

        /// <summary>
        /// This tile MUST BE a given suit and number
        /// </summary>
        public void MustBe(Suit suit, int number, string reason = "")
        {
            int count = Possible.Count();
            Possible = Possible.Where(t => t.Item1 == suit && t.Item2 == number).ToList();
            if (count != Possible.Count())
            {
                Reasons.Add("MustBe(" + suit + " " + number + ") - " + reason + " - " + ToString());
            }
            SanityCheck();
        }

        /// <summary>
        /// This tile CANNOT BE a given suit
        /// </summary>
        public void CannotBe(Suit suit, string reason = "")
        {
            int count = Possible.Count();
            Possible = Possible.Where(t => t.Item1 != suit).ToList();
            if (count != Possible.Count())
            {
                Reasons.Add("CannotBe(" + suit + ") - " + reason + " - " + ToString());
            }
            SanityCheck();
        }

        /// <summary>
        /// This tile CANNOT BE a given number
        /// </summary>
        public void CannotBe(int number, string reason = "")
        {
            int count = Possible.Count();
            Possible = Possible.Where(t => t.Item2 != number).ToList();
            if (count != Possible.Count())
            {
                Reasons.Add("CannotBe(" + number + ") - " + reason + " - " + ToString());
            }
            SanityCheck();
        }

        /// <summary>
        /// This tile CANNOT BE a given suit and number
        /// </summary>
        public void CannotBe(Suit suit, int number, string reason = "")
        {
            int count = Possible.Count();
            Possible = Possible.Where(t => t.Item1 != suit || t.Item2 != number).ToList();
            if (count != Possible.Count())
            {
                Reasons.Add("CannotBe(" + suit + " " + number + ") - " + reason + " - " + ToString());
            }
            SanityCheck();
        }

        /// <summary>
        /// This tile CANNOT BE any of the provided tiles
        /// </summary>
        public void CannotBe(IEnumerable<Tile> list, string reason = "")
        {
            foreach (var t in list)
            {
                CannotBe(t.Suit, t.Number, reason);
            }
            SanityCheck();
        }

        /// <summary>
        /// Do we KNOW this tile is the given number?
        /// </summary>
        public bool IsKnown(int number)
        {
            return !Possible.Any(t => t.Item2 != number);
        }

        /// <summary>
        /// Do we KNOW this tile is the given suit?
        /// </summary>
        public bool IsKnown(Suit suit)
        {
            return !Possible.Any(t => t.Item1 != suit);
        }

        /// <summary>
        /// Are any of our possible values playable?
        /// </summary>
        public bool IsPossiblyPlayable(GameState gs)
        {
            return Possible.Any(t => gs.NextPlay[t.Item1] == t.Item2);
        }

        /// <summary>
        /// Are ALL of our possible values playable?
        /// </summary>
        public bool IsDefinitelyPlayable(GameState gs)
        {
            return Possible.All(t => gs.NextPlay[t.Item1] == t.Item2);
        }

        /// <summary>
        /// Are ALL of our possible values unplayable?
        /// </summary>
        public bool IsUnplayable(GameState gs)
        {
            return Possible.All(t => gs.NextPlay[t.Item1] != t.Item2);
        }

        /// <summary>
        /// Is this tile dead?
        /// </summary>
        public bool IsDead(GameState gs)
        {
            // Have all our possibilities already been played?
            bool alreadyplayed = Possible.All(t => gs.NextPlay[t.Item1] > t.Item2);

            // TODO: tiles that are dead because the suit is broken before them

            return alreadyplayed;
        }

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString()
        {
            var num = Possible.Count();
            if(num <= 5)
            {
                return Possible.Aggregate("", (s, p) => s += String.Format("{0}{1} ", p.Item1, p.Item2));
            }
            else
            {
                return num + " possibilities";
            }
        }
    }
}
