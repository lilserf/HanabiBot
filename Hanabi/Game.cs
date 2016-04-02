using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{

 

    public class Game
    {
        // Numbers for the tiles in each suit
        public static int[] TileNumsFull = { 1, 1, 1, 2, 2, 3, 3, 4, 4, 5 };

        // Number of tiles per player
        static Dictionary<int, int> TilesPerPlayer;

        // Draw pile
        private Queue<Tile> m_draw;
        // Discard pile
        private Queue<Tile> m_discard;
        // Played tiles
        private Queue<Tile> m_play;
        // Turn history
        private List<Turn> m_history;
        // Next valid play in each suit
        private Dictionary<Suit, int> m_nextPlay;
        // Players
        private Dictionary<int, IPlayer> m_players;
        // Current player index
        private int m_currPlayerIndex = 0;
        // Dictionary mapping player indices to hands
        private Dictionary<int, List<Tile>> m_hands;
        // Number of fuses
        int m_fuses = 0;
        // Number of tokens
        int m_tokens = 8;
        // Print extra debug info
        public bool LogEnabled = false;
        // Log to the console?
        public bool LogToConsole = false;
        // Game ID
        private int m_gameId;
        // Log file writer
        private StreamWriter m_logFile;
        // Filename
        private string m_fileName;

        static Game()
        {
            TilesPerPlayer = new Dictionary<int, int>()
            {
                { 2, 5 },
                { 3, 5 },
                { 4, 4 },
                { 5, 4 }
            };
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Game(string baseDir, int gameId)
        {
            m_gameId = gameId;

            m_fileName = String.Format("{0}\\{1:d5}.txt", baseDir, m_gameId);
            Directory.CreateDirectory(Path.GetDirectoryName(m_fileName));
            m_logFile = new StreamWriter(m_fileName);
            SetupNewGame();
        }

        /// <summary>
        /// Clear everything up and set up for a new game
        /// </summary>
        private void SetupNewGame()
        {
            // Set up all our lists
            m_draw = new Queue<Tile>();
            m_discard = new Queue<Tile>();
            m_play = new Queue<Tile>();
            m_history = new List<Turn>();
            m_nextPlay = new Dictionary<Suit, int>();
            m_players = new Dictionary<int, IPlayer>();
            m_currPlayerIndex = 0;
            m_hands = new Dictionary<int, List<Tile>>();

            // Build a pool of all tiles
            List<Tile> allTiles = new List<Tile>();

            // Loop through all known suits
            foreach(Suit s in System.Enum.GetValues(typeof(Suit)))
            {
                // Start each suit at 1
                m_nextPlay[s] = 1;

                // Add all the tiles we need in each suit
                foreach(int num in TileNumsFull)
                {
                    allTiles.Add(new Tile(s, num));
                }
            }

            // Now pull tiles from the pool and queue them up to be drawn
            Random r = new Random();
            while(allTiles.Any())
            {
                int index = r.Next(allTiles.Count);
                Tile t = allTiles[index];
                allTiles.RemoveAt(index);
                m_draw.Enqueue(t);
            }

        }

        /// <summary>
        /// Add an IPlayer to the game
        /// </summary>
        public void AddPlayer(IPlayer player)
        {
            int newPlayerIndex = m_players.Count;
            player.PlayerIndex = newPlayerIndex;
            m_players.Add(newPlayerIndex, player);
            m_hands.Add(newPlayerIndex, new List<Tile>());
        }

        /// <summary>
        /// Run the game and return the points scored
        /// </summary>
        public GameOutcome RunGame()
        {
            // Deal tiles
            int numTiles = TilesPerPlayer[m_players.Count];

            // Draw initial tiles
            foreach(var player in m_players)
            {
                for(int i=0; i < numTiles; i++)
                {
                    DrawToHand(m_hands[player.Key]);
                }
            }

            var possibleGameOver = GameOver();
            while (possibleGameOver == GameOutcome.GameEndReason.NotEnded)
            {
                Log("\n=========================================");
                LogGameState();
                ProcessTurn();
                possibleGameOver = GameOver();
            }

            var points = m_nextPlay.Values.Aggregate(0, (t, i) => t += i - 1);
            Log("GAME OVER!");
            Log(points + " points!");

            m_logFile.Close();
            var newFileName = Path.Combine(Path.GetDirectoryName(m_fileName), Path.GetFileNameWithoutExtension(m_fileName)+"_"+points+"_"+possibleGameOver+Path.GetExtension(m_fileName));
            File.Move(m_fileName, newFileName);
            return new GameOutcome { Points = points, EndReason = possibleGameOver };
        }

        private void Log(String message)
        {
            if(LogEnabled)
            {
                if (LogToConsole)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    m_logFile.WriteLine(message);
                }
            }
        }

        /// <summary>
        /// Print a snapshot of the game state
        /// </summary>
        private void LogGameState()
        {
            string output = "";
            foreach(var pile in m_nextPlay)
            {
                output += "[" + pile.Key + " " + (pile.Value-1) + "] ";
            }
            Log(output);
            Log("Fuses: "+m_fuses);
            Log("Tokens: "+m_tokens);

            foreach(var hand in m_hands)
            {
                output = "";
                foreach(var tile in hand.Value)
                {
                    output += "("+tile+") ";
                }
                Log("Player "+hand.Key+": "+output);
            }
        }

        /// <summary>
        /// Check whether the game is now over
        /// </summary>
        /// <returns></returns>
        private GameOutcome.GameEndReason GameOver()
        {
            // If the draw pile is empty
            if(m_draw.Count == 0)
            {
                // TODO: handle that last round
                return GameOutcome.GameEndReason.EmptyDrawPile;
            }

            // If all piles are perfect
            if(m_nextPlay.All(p => p.Value == 6))
            {
                return GameOutcome.GameEndReason.PerfectGame;
            }

            // If we've blown 3 fuses
            if(m_fuses >= 3)
            {
                return GameOutcome.GameEndReason.Fuses;
            }

            return GameOutcome.GameEndReason.NotEnded;
        }

        /// <summary>
        /// Draw a tile to the provided hand
        /// </summary>
        private void DrawToHand(List<Tile> hand)
        {
            hand.Add(m_draw.Dequeue());
        }

        /// <summary>
        /// Helper - move to the next player
        /// </summary>
        private void NextPlayer()
        {
            m_currPlayerIndex++;
            m_currPlayerIndex %= m_players.Count;
        }

        /// <summary>
        /// Helper - get the gamestate from the perspective of a given player
        /// </summary>
        /// <param name="viewpoint">Player index to view the game as</param>
        /// <returns>Gamestate collection from that viewpoint</returns>
        private GameState GetGameState(int viewpoint)
        {
            var yourHand = new List<Guid>();
            var hands = new Dictionary<int, IEnumerable<Tile>>();

            foreach(var player in m_players)
            {
                if(player.Key != viewpoint)
                {
                    hands.Add(player.Key, m_hands[player.Key]);
                }
                else
                {
                    yourHand.AddRange(m_hands[player.Key].Select(t => t.UniqueId));
                }
            }

            return new GameState(viewpoint, m_tokens, m_draw.ToList(), m_discard.ToList(), m_play.ToList(), yourHand, hands, m_nextPlay, m_fuses, m_history);
        }

        /// <summary>
        /// Process the current player's turn and advance to the next player
        /// </summary>
        private void ProcessTurn()
        {
            foreach(var p in m_players)
            {
                var gameState = GetGameState(p.Key);
                p.Value.Update(gameState);
            }

            IPlayer currPlayer = m_players[m_currPlayerIndex];

            // Give player the game state and receive an action choice
            PlayerAction action = currPlayer.TakeTurn();

            // Do the action
            ExecuteAction(action);

            // Move to the next player
            NextPlayer();
        }

        private void AttemptPlayTile(Tile tile)
        {
            if(m_nextPlay[tile.Suit] == tile.Number)
            {
                m_nextPlay[tile.Suit]++;
                m_play.Enqueue(tile);
                // Get a token back on 5s
                if(tile.Number == 5)
                {
                    m_tokens++;
                }
                Log("Tile [" + tile.Suit + " " + tile.Number + "] played successfully!");
            }
            else
            {
                m_fuses++;
                Log("Tile [" + tile.Suit + " " + tile.Number + "] invalid!");
                DiscardTile(tile);
            }
        }

        private void DiscardTile(Tile tile)
        {
            m_discard.Enqueue(tile);
            Log("Tile [" + tile.Suit + " " + tile.Number + "] discarded!");
        }

        private void ExecuteAction(PlayerAction action)
        {
            if(action.ActionType == PlayerAction.PlayerActionType.Invalid)
            {
                LogGameState();
                throw new InvalidOperationException("Tried to play a null action!");
            }

            IPlayer currPlayer = m_players[m_currPlayerIndex];
            List<Tile> currHand = m_hands[m_currPlayerIndex];

            Turn thisTurn = new Turn();
            thisTurn.Action = action;

            switch(action.ActionType)
            {
                case PlayerAction.PlayerActionType.Discard:
                    {
                        if (m_tokens == 8)
                        {
                            throw new InvalidOperationException("Can't discard with 8 tokens");
                        }
                        Tile toDiscard = currHand.First(t => t.UniqueId == action.TileId);
                        currHand.Remove(toDiscard);
                        DiscardTile(toDiscard);
                        DrawToHand(currHand);
                        m_tokens++;
                    }
                    break;
                case PlayerAction.PlayerActionType.Play:
                    {
                        Tile toPlay = currHand.First(t => t.UniqueId == action.TileId);
                        currHand.Remove(toPlay);
                        AttemptPlayTile(toPlay);
                        DrawToHand(currHand);
                    }
                    break;
                case PlayerAction.PlayerActionType.Info:
                    {
                        if (m_tokens == 0)
                        {
                            throw new InvalidOperationException("Can't give info with 0 tokens");
                        }
                        m_tokens--;

                        var targetHand = m_hands[action.TargetPlayer];
                        
                        thisTurn.TargetedTiles = new List<Guid>();
                        if(action.InfoType == PlayerAction.PlayerActionInfoType.Number)
                        {
                            thisTurn.TargetedTiles.AddRange(targetHand.Where(t => t.Number == action.Info).Select(t => t.UniqueId));
                        }
                        else if(action.InfoType == PlayerAction.PlayerActionInfoType.Suit)
                        {
                            thisTurn.TargetedTiles.AddRange(targetHand.Where(t => t.Suit == (Suit)action.Info).Select(t => t.UniqueId));
                        }
                    }
                    break;
            }

            m_history.Add(thisTurn);
            String tiles = "";
            if(thisTurn.TargetedTiles != null && thisTurn.TargetedTiles.Any())
            {
                tiles += " at " + thisTurn.TargetedTiles.Aggregate("", (s, g) => s += g + " ");
            }
            Log("\nPLAYER " + currPlayer.PlayerIndex + ": \"" + thisTurn.Action + tiles + ".\"");

        }
    }
}
