using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Hanabi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Yes, it's stupid that this is a WPF app but I'm not using the WPF part
    /// Eventually I want to show stuff in the WPF window but I just haven't bothered yet
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // How many games to play
            int numGames = 1000;

            // Remember how many times we scored each score
            Dictionary<int, int> Scores = new Dictionary<int,int>();
            // Remember how many times the game ended for each reason
            Dictionary<GameOutcome.GameEndReason, int> EndReasons = new Dictionary<GameOutcome.GameEndReason, int>();

            // TODO: use the windows temp folder
            String folder = "d:\\temp\\hanabi\\"+DateTime.Now.ToString("yyyy_MM_dd_H_mm_ss");

            // Looooop
            for (int i = 0; i < numGames; i++)
            {
                // Just create a new game every time for no risk of pollution
                Game game = new Game(folder, i);
                game.LogEnabled = true;
                game.LogToConsole = false;

                InfoGiverBotOptions opts = new InfoGiverBotOptions(true);
                // Add 5 copies of our only current bot
                game.AddPlayer(new InfoGiverBot(opts));
                game.AddPlayer(new InfoGiverBot(opts));
                game.AddPlayer(new InfoGiverBot(opts));
                game.AddPlayer(new InfoGiverBot(opts));
                game.AddPlayer(new InfoGiverBot(opts));

                //try
                {
                    // Run this game and get the outcome!
                    var outcome = game.RunGame();

                    // Add this score to our bookkeeping
                    if(Scores.ContainsKey(outcome.Points))
                    {
                        Scores[outcome.Points]++;
                    }
                    else
                    {
                        Scores[outcome.Points] = 1;
                    }
                    
                    // Add this game end reason to our bookkeeping
                    if(EndReasons.ContainsKey(outcome.EndReason))
                    {
                        EndReasons[outcome.EndReason]++;
                    }
                    else
                    {
                        EndReasons[outcome.EndReason] = 1;
                    }
                    
                }
                //catch(Exception ex)
                //{
                //    // A bot tried to do something disallowed and the game crapped out
                //    Console.WriteLine("Game "+i+": "+ex.Message);
                //}

                if (game.LogToConsole) break;
            }

            // Count how many games we played
            int count = Scores.Values.Aggregate(0, (t, c) => t += c);
            // Total scores we achieved
            int total = Scores.Aggregate(0, (t, kvp) => t += kvp.Key * kvp.Value);
            // Minimum score we got
            int min = Scores.Min(kvp => kvp.Key);
            // Maximum score we got
            int max = Scores.Max(kvp => kvp.Key);
            
            // Average score
            float mean = (float)total / count;

            // Build an ordered list of scores to find the median
            List<int> scoreList = new List<int>();
            foreach(var entry in Scores.OrderBy(k => k.Key))
            {
                for (int i = 0; i < entry.Value; i++)
                {
                    scoreList.Add(entry.Key);
                }
            }
            int median = scoreList.ElementAt(count / 2);

            // Finally, write out the stats for this game, in pastable-into-Excel format
            Console.WriteLine("GAMES\t" + count);
            Console.WriteLine();
            for (int i = 0; i <= 30; i++)
            {
                Console.Write(i + "\t");
                if(Scores.ContainsKey(i))
                {
                    Console.Write(Scores[i]);
                }
                Console.WriteLine();
            }
            Console.WriteLine("MEAN\t"+mean);
            Console.WriteLine("MEDIAN\t"+median);
            Console.WriteLine("MIN\t"+min);
            Console.WriteLine("MAX\t"+max);
            Console.WriteLine();
            foreach(var endReason in EndReasons)
            {
                Console.WriteLine(endReason.Key+"\t"+endReason.Value);
            }
        }
    }
}
