using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    using TileValue = Tuple<Suit, int>;
    using TileValueList = List<Tuple<Suit, int>>;

    /// <summary>
    /// Struct for tracking finesses
    /// </summary>
    struct Finesse
    {
        // TODO: track whether the finessed tile actually got played or not

        // When we are finessed:
        // 1: Remember what we think we have
        // 2: Play the finessed tile

        // When we are given info to finesse someone else:
        // 1: Remember what we think we have
        // 2: Don't play yet
        // 3: Watch to see if the finessed person plays
        // 3b: Maybe see if they got any other info on that tile
        // 4: If they do, we definitely have the tile we thought
        // 5: If they don't, forget about this finesse

        public Guid InfoTileId;
        public Guid TargetTileId;

        public TileValueList PossibleInfoValues;
        public TileValueList PossibleTargetValues;

        public int GivingPlayer;
        public int TurnGiven;
        public int TargetedPlayer;
        public int TurnTargetShouldAct;

        public Finesse(GameState gs, Guid infoTile, Guid targetTile, TileValueList infoValues, TileValueList targetValues, int turn, int givingPlayer)
        {
            InfoTileId = infoTile;
            TargetTileId = targetTile;
            PossibleInfoValues = infoValues;
            PossibleTargetValues = targetValues;
            TurnGiven = turn;
            GivingPlayer = givingPlayer;
            TargetedPlayer = gs.WhoHas(targetTile);

            int turnDifference = ((TargetedPlayer < GivingPlayer) ? TargetedPlayer + gs.NumPlayers : TargetedPlayer) - GivingPlayer;
            TurnTargetShouldAct = TurnGiven + turnDifference;
        }
    }


    /// <summary>
    /// Bookkeeping helper class that tracks game state and info being given and tries to narrow
    /// down what each player knows about tiles in their hand
    /// 
    /// TODO: This is starting to be the whole bot when it was originally intended as a helper. Oops.
    /// </summary>
    class InfoTrackerModule
    {
        /// <summary>
        /// Dictionary of what is known about every tile in the game by *that tile's owner*
        /// </summary>
        Dictionary<Guid, UnknownTile> m_infoLookup;

        /// <summary>
        /// List of Finesses we're tracking
        /// </summary>
        List<Finesse> m_activeFinesses;

        /// <summary>
        /// Are we doing finesses?
        /// </summary>
        bool m_allowFinesses;

        /// <summary>
        /// Most recent game state received in Update()
        /// </summary>
        GameState m_lastGameState;

        /// <summary>
        /// Our own player index
        /// </summary>
        public int PlayerIndex { get; set; }

        // Strength to add to the newest tile included in an info give
        const int NEWEST_INFO_TILE_STRENGTH = 50;
        // Strength to add to the oldest tile included in an info give (tiles between will lerp between these values)
        const int OLDEST_INFO_TILE_STRENGTH = 10;

        // Strength value required to play a tile when we're not at 2 fuses
        const int SAFE_STRENGTH_TO_PLAY = 15;
        // Strength value required to play a tile when we're at 2 fuses
        const int DANGER_STRENGTH_TO_PLAY = 50;

        // Each playable tile in a possible info give adds this much to the info's fitness
        const int INFO_PLAYABLE_FITNESS = 20;
        // Each unplayable tile in a possible info give adds this much to the info's fitness
        const int INFO_UNPLAYABLE_FITNESS = -10;

        /// <summary>
        /// Constructor
        /// </summary>
        public InfoTrackerModule(bool finesse)
        {
            m_infoLookup = new Dictionary<Guid, UnknownTile>();
            m_activeFinesses = new List<Finesse>();
            m_allowFinesses = finesse;
        }

        /// <summary>
        /// Get a list of all tiles that could be finessed
        /// </summary>
        IEnumerable<Tile> GetPossibleFinesseTargets()
        {
            // TODO: Tiles that already have high play strength shouldn't be targeted!

            List<Tile> playableNewTiles = new List<Tile>();
            foreach (var hand in m_lastGameState.Hands)
            {
                var newestTile = hand.Value.Last();

                if (m_lastGameState.IsPlayable(newestTile))
                {
                    playableNewTiles.Add(newestTile);
                }
            }

            return playableNewTiles;
        }

        /// <summary>
        /// Get a list of player/tile combos that we could give info on in order to finesse something else
        /// </summary>
        IEnumerable<Tuple<int, Tile>> GetSimpleFinesses()
        {
            var playableNewTiles = GetPossibleFinesseTargets();

            // TODO: Don't finesse a tile that's already been finessed
            // TODO: Don't finesse a tile where a copy has already been info'd
            // TODO: Return info about WHAT we're trying to finesse, then tune the info given according to that

            List<Tuple<int, Tile>> finesses = new List<Tuple<int, Tile>>();
            foreach(var tile in playableNewTiles)
            {
                if(tile.Number < 5)
                {
                    int nextNumber = tile.Number + 1;
                    int finessedPlayer = m_lastGameState.WhoHas(tile.UniqueId);

                    foreach(var hand in m_lastGameState.Hands)
                    {
                        // Can't finesse you with a tile in your hand
                        if(hand.Key != finessedPlayer)
                        {
                            var nextTile = hand.Value.Where(t => t.Suit == tile.Suit && t.Number == nextNumber).FirstOrDefault();

                            if(nextTile != null)
                            {
                                finesses.Add(new Tuple<int, Tile>(hand.Key, nextTile));
                            }
                        }
                    }
                }
            }

            return finesses;
        }

        /// <summary>
        /// Get a list of all playable tiles in other players hands
        /// </summary>
        IEnumerable<Tuple<int, Tile>> GetAllPlayableTiles()
        {
            foreach (var hand in m_lastGameState.Hands)
            {
                foreach(var tile in hand.Value)
                {
                    // Is this playable?
                    if(m_lastGameState.IsPlayable(tile))
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
        IEnumerable<Tile> GetEliminatedTiles(int viewpoint)
        {
            var visible = new List<Tile>();

            // Batch up all the visible tiles
            visible.AddRange(m_lastGameState.Discard);
            visible.AddRange(m_lastGameState.Play);
            // Add tiles visible to that player in other hands
            foreach (var hand in m_lastGameState.Hands)
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
                if(g.Key.Number == 1 && g.Count() >= m_lastGameState.NumCopies(g.Key.Suit, g.Key.Number))
                {
                    // Can see all three 1s
                    eliminated.Add(g.Key);
                }
                else if (g.Key.Number == 5 && g.Count() >= m_lastGameState.NumCopies(g.Key.Suit, g.Key.Number))
                {
                    // Can see the single 5
                    eliminated.Add(g.Key);
                }
                else if ((g.Key.Number == 2 || g.Key.Number == 3 || g.Key.Number == 4) && g.Count() >= m_lastGameState.NumCopies(g.Key.Suit, g.Key.Number))
                {
                    // Can see both copies of a 2/3/4
                    eliminated.Add(g.Key);
                }
            }

            return eliminated.OrderBy(t => t.Number).OrderBy(t => t.Suit);
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

            if (m_allowFinesses)
            {
                List<Finesse> deadFinesses = new List<Finesse>();

                // Cull finesses that are over now
                foreach (var finesse in m_activeFinesses)
                {
                    // If both parts of the finesse are dead we can stop tracking this
                    if (m_lastGameState.IsDead(finesse.InfoTileId) && m_lastGameState.IsDead(finesse.TargetTileId))
                    {
                        deadFinesses.Add(finesse);
                    }
                }

                // Remove all the dead finesses
                foreach (var finesse in deadFinesses)
                {
                    m_activeFinesses.Remove(finesse);
                }
            }

            foreach(var hand in gameState.Hands)
            {
                // Look at the gamestate and cross out any tiles that are eliminated
                // from the perspective of that player
                var eliminatedTiles = GetEliminatedTiles(hand.Key);

                foreach(var tile in hand.Value)
                {
                    var lookup = Lookup(tile);
                    lookup.CannotBe(eliminatedTiles, "Eliminated tiles");
                }
            }
            
            // Also go through your hand and cross out any tiles that are eliminated
            foreach (var guid in gameState.YourHand)
            {
                var lookup = Lookup(guid);
                lookup.CannotBe(GetEliminatedTiles(PlayerIndex), "Eliminated tiles");
            }

            if (m_lastGameState.History.Any())
            {
                // Record the last thing that happened
                RecordPlayerTurn(m_lastGameState.History.Last(), m_lastGameState.History.Count());
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
        int GetCurrentPlayStrength()
        {
            if(m_lastGameState.Fuses < 2)
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
        public void RecordPlayerTurn(Turn turn, int turnNumber)
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
                        // Go through the tiles included in the give and record what we now know about them
                        foreach (Guid g in turn.TargetedTiles)
                        {
                            var lookup = Lookup(g);

                            if (turn.Action.InfoType == PlayerAction.PlayerActionInfoType.Suit)
                            {
                                lookup.MustBe((Suit)turn.Action.Info, "Given Info");
                                lookup.GotInfo();
                            }
                            else if (turn.Action.InfoType == PlayerAction.PlayerActionInfoType.Number)
                            {
                                lookup.MustBe(turn.Action.Info, "Given Info");
                                lookup.GotInfo();
                            }

                            // If the targeted tile might be playable at all, remember it
                            if(lookup.IsPossiblyPlayable(m_lastGameState))
                            {
                                tiles.Add(g);
                            }
                        }


                        if (m_allowFinesses)
                        {
                            if (turn.Action.TargetPlayer == PlayerIndex)
                            {
                                // Info was given to you, double check if it might be a finesse
                                var finesseTargets = GetPossibleFinesseTargets();

                                // Ignore any finesse targets that are in the hand of the person giving the info, they can't see those
                                finesseTargets = finesseTargets.Where(t => m_lastGameState.WhoHas(t.UniqueId) != turn.Action.ActingPlayer);

                                // If there are possible finesses
                                if (finesseTargets.Any())
                                {
                                    // Possible values our info could have if they're finessing each target
                                    var infoValueList = finesseTargets.Select(t => new TileValue(t.Suit, t.Number + 1)).ToList();

                                    foreach (var infoTarget in turn.TargetedTiles)
                                    {
                                        foreach (var finesseTarget in finesseTargets)
                                        {
                                            var targetValueList = new TileValueList();
                                            targetValueList.Add(new TileValue(finesseTarget.Suit, finesseTarget.Number));

                                            var finesse = new Finesse(m_lastGameState, infoTarget, finesseTarget.UniqueId, infoValueList, targetValueList, turnNumber, turn.Action.ActingPlayer);
                                            m_activeFinesses.Add(finesse);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Info was given to someone else, see if you're being finessed

                                var actualTiles = turn.TargetedTiles.Select(g => m_lastGameState.AllHands().First(t => t.UniqueId == g));

                                // If this info give is all unplayable tiles, it's gotta be a finesse
                                if (!actualTiles.Any(t => m_lastGameState.IsPlayable(t)))
                                {
                                    var finesseTargets = GetPossibleFinesseTargets();
                                    var missingTargets = new List<Tile>();

                                    foreach (var target in turn.TargetedTiles)
                                    {
                                        // Find this info tile
                                        var infoTile = m_lastGameState.AllHands().Where(t => t.UniqueId == target).First();
                                        // See if the finesse targets list contains a corresponding tile
                                        var finesseTarget = finesseTargets.Where(t => t.Suit == infoTile.Suit && t.Number == infoTile.Number - 1).FirstOrDefault();

                                        var infoValues = new TileValueList();
                                        infoValues.Add(new TileValue(infoTile.Suit, infoTile.Number));

                                        if (finesseTarget != null)
                                        {
                                            var targetValues = new TileValueList();
                                            targetValues.Add(new TileValue(finesseTarget.Suit, finesseTarget.Number));
                                            var finesse = new Finesse(m_lastGameState, infoTile.UniqueId, finesseTarget.UniqueId, infoValues, targetValues, turnNumber, turn.Action.ActingPlayer);
                                            m_activeFinesses.Add(finesse);
                                        }
                                        else
                                        {
                                            // We must have the target!
                                            var myNewest = m_lastGameState.YourHand.Last();
                                            var targetValues = new TileValueList();
                                            targetValues.Add(new TileValue(infoTile.Suit, infoTile.Number - 1));
                                            var finesse = new Finesse(m_lastGameState, infoTile.UniqueId, myNewest, infoValues, targetValues, turnNumber, turn.Action.ActingPlayer);
                                            m_activeFinesses.Add(finesse);
                                        }
                                    }

                                }
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
                                lookup.CannotBe((Suit)turn.Action.Info, "Negative info");
                            }
                            else if (turn.Action.InfoType == PlayerAction.PlayerActionInfoType.Number)
                            {
                                lookup.CannotBe(turn.Action.Info, "Negative info");
                            }
                        }
                    }
                    break;
            }

        }

        /// <summary>
        /// Given a tile we want to give info on, evaluate the relative strengths of suit vs number info for that tile
        /// and return whichever is better
        /// </summary>
        private Tuple<int, PlayerAction> ChooseBestInfoForTile(int playerIndex, Tile tile, string log)
        {
            // Only give info if this tile hasn't had info given on it recently
            var unknownTile = Lookup(tile);
            bool noRecentInfo = (unknownTile.InfoAge == -1 || unknownTile.InfoAge > 20);

            // Only give this info if no other player has already been told to play their copy of this tile
            var otherCopies = m_lastGameState.AllHands().Where(t => t.Same(tile) && t.UniqueId != tile.UniqueId);
            bool otherCopiesInfoAlready = otherCopies.Any(t => Lookup(t).InfoAge >= 0);

            Tuple<int, PlayerAction> bestAction = null;

            if (noRecentInfo && !otherCopiesInfoAlready)
            {
                // Construct an action for giving number info for this tile
                var numAction = PlayerAction.GiveInfo(PlayerIndex, PlayerAction.PlayerActionInfoType.Number, playerIndex, tile.Number, log);
                var numTiles = m_lastGameState.Hands[playerIndex].Where(t => t.Number == tile.Number);
                var numPlayable = numTiles.Count(t => m_lastGameState.IsPlayable(t));
                var numBad = numTiles.Count() - numPlayable;
                var numFitness = (INFO_PLAYABLE_FITNESS * numPlayable + INFO_UNPLAYABLE_FITNESS * numBad);

                // Construct an action for giving suit info for this tile
                var suitAction = PlayerAction.GiveInfo(PlayerIndex, PlayerAction.PlayerActionInfoType.Suit, playerIndex, (int)tile.Suit, log);
                var suitTiles = m_lastGameState.Hands[playerIndex].Where(t => t.Suit == tile.Suit);
                var suitPlayable = suitTiles.Count(t => m_lastGameState.IsPlayable(t));
                var suitBad = suitTiles.Count() - suitPlayable;
                var suitFitness = (INFO_PLAYABLE_FITNESS * suitPlayable + INFO_UNPLAYABLE_FITNESS * suitBad);

                if (numFitness > suitFitness)
                {
                    bestAction = new Tuple<int, PlayerAction>(numFitness, numAction);
                }
                else
                {
                    bestAction = new Tuple<int, PlayerAction>(suitFitness, suitAction);
                }
            }

            return bestAction;
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

                if (m_allowFinesses)
                {
                    // Finesses
                    var finesseTargetsInOurHand = m_activeFinesses.Where(f => m_lastGameState.YourHand.Contains(f.TargetTileId));

                    if (finesseTargetsInOurHand.Any())
                    {
                        var finesse = finesseTargetsInOurHand.First();

                        if (finesse.PossibleTargetValues.All(v => m_lastGameState.IsPlayable(v.Item1, v.Item2)))
                        {
                            var infoString = finesse.PossibleInfoValues.Aggregate("", (s, t) => s += t.Item1 + " " + t.Item2 + ", ");
                            var playAction = PlayerAction.PlayTile(PlayerIndex, finesse.TargetTileId, String.Format("Finessed by {0}", infoString));
                            yield return new Tuple<int, PlayerAction>(80, playAction);
                        }
                    }
                }
                // Other Info

                var lookup = Lookup(g);
                // If this tile has been strongly messages OR we know it's definitely playable
                bool playStrength = lookup.PlayStrength >= GetCurrentPlayStrength();
                bool isUnplayable = lookup.IsUnplayable(m_lastGameState);
                bool isPlayable = lookup.IsDefinitelyPlayable(m_lastGameState);
                if (isPlayable || (playStrength && !isUnplayable))
                {
                    string log = "";
                    if (isPlayable)
                    {
                        log = String.Format("This tile should be playable ({0})", lookup.ToString());
                    }
                    else
                    {
                        log = String.Format("PlayStrength is {0}", lookup.PlayStrength);
                    }

                    if (m_activeFinesses.Where(f => f.InfoTileId == g).Any())
                    {
                        // Check to see if this has play strength because it's the info part of a finesse
                        Finesse finesse = m_activeFinesses.Where(f => f.InfoTileId == g).First();
                        int currTurn = m_lastGameState.History.Count() + 1;

                        // If the target has had a chance to act
                        if (currTurn > finesse.TurnTargetShouldAct)
                        {
                            var target = finesse.PossibleTargetValues.First();
                            // If the target tile of this finesse was played
                            if (m_lastGameState.Play.Where(t => t.UniqueId == finesse.TargetTileId).Any())
                            {
                                // Awesome! This is probably playable!
                                log = String.Format("Was used to finesse the {0} {1} which was played!", target.Item1, target.Item2);
                            }
                            else
                            {
                                // There's been enough time but the finessed person didn't play
                                // Therefore this is probably not a finesse
                                // Remove the finesse from our list and keep assuming this tile is playable
                                m_activeFinesses.Remove(finesse);
                                log = String.Format("Thought this was finessing the {0} {1} but apparently not!", target.Item1, target.Item2);
                            }
                        }
                        else
                        {
                            // Hasn't been long enough for the finesse target to act yet
                            // So delay - skip considering this tile playable for now, but don't change any bookkeeping
                            continue;
                        }

                    }

                    var playAction = PlayerAction.PlayTile(PlayerIndex, g, log);
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
                if (m_allowFinesses)
                {
                    var finesses = GetSimpleFinesses().OrderBy(t => t.Item2.Number);

                    foreach (var combo in finesses)
                    {
                        var info = ChooseBestInfoForTile(combo.Item1, combo.Item2, "Finesse!");
                        if (info != null)
                        {
                            // Add bonus strength for this being a finesse
                            var newInfo = new Tuple<int, PlayerAction>(info.Item1 + 20, info.Item2);
                            yield return newInfo;
                        }
                    }
                }

                // Next see if there's valid info to give
                // This is pretty naive
                var playable = GetAllPlayableTiles().OrderBy(t => t.Item2.Number);

                foreach (var combo in playable)
                {
                    var info = ChooseBestInfoForTile(combo.Item1, combo.Item2, "Playable");
                    if (info != null)
                    {
                        yield return info;
                    }
                }
            }

            ///////////////////////////////////////////////////
            // NOTHING BETTER TO DO DISCARD
            ///////////////////////////////////////////////////

            if (m_lastGameState.Tokens < 8)
            {
                // TODO: account for don't-discard info
                var discardOldestAction = PlayerAction.DiscardTile(PlayerIndex, m_lastGameState.YourHand.First(), "Discard oldest");
                yield return new Tuple<int, PlayerAction>(1, discardOldestAction);
            }
            else
            {
                // Give totally arbitrary info
                // This should maybe try to at least indicate a definitely-unplayable tile or something
                // This logic is currently responsible for most of our 3-Fuse endgames

                var leastPlayable = m_lastGameState.AllHands().OrderBy(t => t.Number).Last();
                var owner = m_lastGameState.WhoHas(leastPlayable.UniqueId);

                var arbitraryInfoAction = PlayerAction.GiveInfo(PlayerIndex, PlayerAction.PlayerActionInfoType.Number, owner, leastPlayable.Number, "Least playable thing I could find");
                yield return new Tuple<int, PlayerAction>(0, arbitraryInfoAction);
            }
        }

        /// <summary>
        /// Get all possible actions we could take, sorted by fitness
        /// But don't return the fitness to the caller, that's internal info
        /// </summary>
        public IEnumerable<PlayerAction> GetBestActions()
        {
            var list = GetAllActions();

            return list.OrderByDescending(o => o.Item1).Select(o => o.Item2);
        }
    }
}
