using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    /// <summary>
    /// Bookkeeping helper class that tracks game state and info being given and tries to narrow
    /// down what each player knows about tiles in their hand
    /// </summary>
    class InfoTrackerModule
    {
        /// <summary>
        /// Dictionary of what is known about every tile in the game by that tile's owner
        /// </summary>
        Dictionary<Guid, UnknownTile> m_infoLookup;


        /// <summary>
        /// Most recent game state
        /// </summary>
        GameState m_lastGameState;

        public int PlayerIndex { get; set; }

        // Strength to add to the newest tile included in an info give
        const int NEWEST_INFO_TILE_STRENGTH = 50;
        // Strength to add to the oldest tile included in an info give (tiles between will lerp between these values)
        const int OLDEST_INFO_TILE_STRENGTH = 10;

        // Strength value required to play a tile when we're not at 2 fuses
        const int SAFE_STRENGTH_TO_PLAY = 15;
        // Strength value required to play a tile when we're at 2 fuses
        const int DANGER_STRENGTH_TO_PLAY = 50;

        
        const int INFO_PLAYABLE_FITNESS = 20;
        const int INFO_UNPLAYABLE_FITNESS = -10;

        /// <summary>
        /// Constructor
        /// </summary>
        public InfoTrackerModule()
        {
            m_infoLookup = new Dictionary<Guid, UnknownTile>();
        }

        /// <summary>
        /// Get a list of all playable tiles in other players hands
        /// </summary>
        IEnumerable<Tuple<int, Tile>> GetAllPlayableTiles(GameState gs)
        {
            foreach(var hand in gs.Hands)
            {
                foreach(var tile in hand.Value)
                {
                    // Is this playable?
                    if(gs.IsPlayable(tile))
                    {
                        yield return new Tuple<int, Tile>(hand.Key, tile);
                    }
                }
            }
        }

        /// <summary>
        /// Get a list of all tiles that, from a given perspective, can be seen fully
        /// (and thus can't be in that player's hand)
        /// </summary>
        IEnumerable<Tile> GetEliminatedTiles(GameState gs, int viewpoint)
        {
            var visible = new List<Tile>();

            // Batch up all the visible tiles
            visible.AddRange(gs.Discard);
            visible.AddRange(gs.Play);
            foreach(var hand in gs.Hands)
            {
                if(hand.Key != viewpoint)
                {
                    visible.AddRange(hand.Value);
                }
            }

            var eliminated = new List<Tile>();

            // Group the visible tiles by tile (all red 1s together, etc)
            var groups = visible.GroupBy(t => t, new TileComparer());

            foreach(var g in groups)
            {
                if(g.Key.Number == 1 && g.Count() >= 3)
                {
                    // Can see all three 1s
                    eliminated.Add(g.Key);
                }
                else if(g.Key.Number == 5 && g.Count() >= 1)
                {
                    // Can see the single 5
                    eliminated.Add(g.Key);
                }
                else if (g.Count() >= 2)
                {
                    // Can see both copies of a 2/3/4
                    eliminated.Add(g.Key);
                }
            }

            return eliminated;
        }

        /// <summary>
        /// Get the bookkeeping object for a tile
        /// </summary>
        UnknownTile Lookup(Tile t)
        {
            return Lookup(t.UniqueId);
        }

        /// <summary>
        /// Get the bookkeeping object for a tile by guid
        /// </summary>
        UnknownTile Lookup(Guid g)
        {
            if (!m_infoLookup.ContainsKey(g))
            {
                m_infoLookup[g] = new UnknownTile(g);
            }
            return m_infoLookup[g];
        }

        /// <summary>
        /// Update the game state - called when it changes, more or less
        /// </summary>
        public void Update(GameState gameState)
        {
            m_lastGameState = gameState;

            foreach(var hand in gameState.Hands)
            {
                // Look at the gamestate and cross out any tiles that are eliminated
                // from the perspective of that player
                var eliminatedTiles = GetEliminatedTiles(gameState, hand.Key);

                foreach(var tile in hand.Value)
                {
                    var lookup = Lookup(tile);
                    lookup.CannotBe(eliminatedTiles);
                }
            }
            
            // Also go through your hand and cross out any tiles that are eliminated
            foreach (var guid in gameState.YourHand)
            {
                var lookup = Lookup(guid);
                lookup.CannotBe(GetEliminatedTiles(gameState, PlayerIndex));
            }

            if (m_lastGameState.History.Any())
            {
                // Record the last thing that happened
                RecordPlayerTurn(m_lastGameState.History.Last());
            }
        }

        /// <summary>
        /// Get the guids of the tiles in a given player's hand
        /// </summary>
        public IEnumerable<Guid> TilesInHand(int playerIndex)
        {
            if(playerIndex == m_lastGameState.Viewpoint)
            {
                return m_lastGameState.YourHand;
            }
            else
            {
                return m_lastGameState.Hands.Where(h => h.Key == playerIndex).First().Value.Select(t => t.UniqueId);
            }
        }

        /// <summary>
        /// What is the current strength required to actually play a tile?
        /// </summary>
        /// <returns></returns>
        int GetCurrentPlayStrength()
        {
            if(m_lastGameState.Tokens < 2)
            {
                return SAFE_STRENGTH_TO_PLAY;
            }
            else
            {
                return DANGER_STRENGTH_TO_PLAY;
            }
        }

        /// <summary>
        /// Linear interpolate from start to end
        /// </summary>
        int Lerp(int start, int end, float percent)
        {
            return (int)Math.Floor(start + percent * (end - start));
        }

        /// <summary>
        /// Given a set of tiles that we got info on, modify their play strength 
        /// Currently we lerp from MAX_STRENGTH to MIN_STRENGTH from the newest to oldest tiles
        /// So the strongest play signal is for the newest tiles
        /// </summary>
        private void ModifyPlayStrength(int playerIndex, IEnumerable<Guid> tiles)
        {
            int numTiles = tiles.Count();

            var hand = TilesInHand(playerIndex);

            var ordered = tiles.Where(t => hand.Contains(t)).Reverse();

            if(numTiles == 1)
            {
                Lookup(ordered.First()).PlayStrength += NEWEST_INFO_TILE_STRENGTH;
            }
            else
            {
                float step = 1.0f / (numTiles-1);
                float percent = 0.0f;
                foreach(var g in ordered)
                {
                    var lookup = Lookup(g);
                    var value = Lerp(NEWEST_INFO_TILE_STRENGTH, OLDEST_INFO_TILE_STRENGTH, percent);
                    lookup.PlayStrength += value;
                    percent += step;
                }
            }
        }

        /// <summary>
        /// A player took an action!
        /// </summary>
        public void RecordPlayerTurn(Turn turn)
        {
            // Increase the age of info
            foreach(var tile in m_infoLookup.Values)
            {
                tile.AgeInfo();
            }

            switch(turn.Action.ActionType)
            {
                case PlayerAction.PlayerActionType.Info:
                    {
                        List<Guid> tiles = new List<Guid>();
                        foreach (Guid g in turn.TargetedTiles)
                        {
                            var lookup = Lookup(g);

                            if (turn.Action.InfoType == PlayerAction.PlayerActionInfoType.Suit)
                            {
                                lookup.MustBe((Suit)turn.Action.Info);
                                lookup.GotInfo();
                            }
                            else if (turn.Action.InfoType == PlayerAction.PlayerActionInfoType.Number)
                            {
                                lookup.MustBe(turn.Action.Info);
                                lookup.GotInfo();
                            }

                            // If the targeted tile might be playable at all, remember it
                            if(lookup.IsPossiblyPlayable(m_lastGameState))
                            {
                                tiles.Add(g);
                            }
                        }

                        if (tiles.Any())
                        {
                            // Modify our strength value for all targeted tiles that could possibly be playable
                            ModifyPlayStrength(turn.Action.TargetPlayer, tiles);
                        }

                        // Now go through all other tiles in that hand and mark the negative info we got
                        foreach (Guid g in TilesInHand(turn.Action.TargetPlayer).Except(turn.TargetedTiles))
                        {
                            var lookup = Lookup(g);

                            if (turn.Action.InfoType == PlayerAction.PlayerActionInfoType.Suit)
                            {
                                lookup.CannotBe((Suit)turn.Action.Info);
                            }
                            else if (turn.Action.InfoType == PlayerAction.PlayerActionInfoType.Number)
                            {
                                lookup.CannotBe(turn.Action.Info);
                            }
                        }
                    }
                    break;
            }

        }

        private bool AlreadyKnows(Guid tileId, int number)
        {
            return Lookup(tileId).IsKnown(number);
        }

        private bool AlreadyKnows(Guid tileId, Suit suit)
        {
            return Lookup(tileId).IsKnown(suit);
        }

        /// <summary>
        /// Get all the reasonable actions along with their "strength"
        /// </summary>
        private IEnumerable<Tuple<int, PlayerAction>> GetAllActions()
        {
            ///////////////////////////////////////////////////
            // PLAY ACTIONS
            ///////////////////////////////////////////////////
            foreach(Guid g in TilesInHand(PlayerIndex))
            {
                var lookup = Lookup(g);
                if (lookup.PlayStrength >= GetCurrentPlayStrength())
                {
                    var playAction = PlayerAction.PlayTile(PlayerIndex, g);
                    yield return new Tuple<int, PlayerAction>(lookup.PlayStrength, playAction);
                }
            }
            
            ///////////////////////////////////////////////////
            // PURPOSEFUL DISCARD ACTIONS
            ///////////////////////////////////////////////////

            // Next see if we need to discard
            // TODO

            ///////////////////////////////////////////////////
            // INFO ACTIONS
            ///////////////////////////////////////////////////

            // Only give info if we have a token
            // TODO: don't wait until 0 tokens, maybe
            if (m_lastGameState.Tokens > 0)
            {
                // Next see if there's valid info to give
                // This is very naive
                var playable = GetAllPlayableTiles(m_lastGameState).OrderBy(t => t.Item2.Number);

                foreach (var combo in playable)
                {
                    // TODO: Only give this info if no other player has already been told to play their copy of this tile
                    // Only give info if this tile hasn't had info given on it recently
                    var unknownTile = Lookup(combo.Item2.UniqueId);

                    var otherCopies = m_lastGameState.AllHands().Where(t => t.Same(combo.Item2) && t.UniqueId != combo.Item2.UniqueId);

                    bool otherCopiesInfoAlready = otherCopies.Any(t => Lookup(t).InfoAge >= 0);
                    bool noRecentInfo = (unknownTile.InfoAge == -1 || unknownTile.InfoAge > 20);

                    if (noRecentInfo && !otherCopiesInfoAlready)
                    {
                        var playerIndex = combo.Item1;
                        var numAction = PlayerAction.GiveInfo(PlayerIndex, PlayerAction.PlayerActionInfoType.Number, playerIndex, combo.Item2.Number);
                        var numTiles = m_lastGameState.Hands[playerIndex].Where(t => t.Number == combo.Item2.Number);
                        var numPlayable = numTiles.Count(t => m_lastGameState.IsPlayable(t));
                        var numBad = numTiles.Count() - numPlayable;
                        var numStrength = (INFO_PLAYABLE_FITNESS * numPlayable + INFO_UNPLAYABLE_FITNESS * numBad);

                        var suitAction = PlayerAction.GiveInfo(PlayerIndex, PlayerAction.PlayerActionInfoType.Suit, playerIndex, (int)combo.Item2.Suit);
                        var suitTiles = m_lastGameState.Hands[playerIndex].Where(t => t.Suit == combo.Item2.Suit);
                        var suitPlayable = suitTiles.Count(t => m_lastGameState.IsPlayable(t));
                        var suitBad = suitTiles.Count() - suitPlayable;
                        var suitStrength = (INFO_PLAYABLE_FITNESS * suitPlayable + INFO_UNPLAYABLE_FITNESS * suitBad);

                        // TODO: what strength should info on a playable tile be?
                        if(numStrength > suitStrength)
                        {
                            yield return new Tuple<int, PlayerAction>(numStrength, numAction);
                        }
                        else
                        {
                            yield return new Tuple<int, PlayerAction>(suitStrength, suitAction);
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////
            // NOTHING BETTER TO DO DISCARD
            ///////////////////////////////////////////////////

            if (m_lastGameState.Tokens < 8)
            {
                // TODO: account for don't-discard info
                var discardOldestAction = PlayerAction.DiscardTile(PlayerIndex, m_lastGameState.YourHand.First());
                yield return new Tuple<int, PlayerAction>(1, discardOldestAction);
            }
            else
            {
                // Give totally arbitrary info
                var arbitraryInfoAction = PlayerAction.GiveInfo(PlayerIndex, PlayerAction.PlayerActionInfoType.Suit, m_lastGameState.Hands.Keys.First(), (int)m_lastGameState.Hands.Values.First().First().Suit);
                yield return new Tuple<int, PlayerAction>(0, arbitraryInfoAction);
            }
        }

        public IEnumerable<PlayerAction> GetBestActions()
        {
            var list = GetAllActions();

            return list.OrderByDescending(o => o.Item1).Select(o => o.Item2);
        }
    }
}
