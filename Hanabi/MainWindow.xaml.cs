using System;
using System.Collections.Generic;
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
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            int numGames = 1000;

            Dictionary<int, int> Scores = new Dictionary<int,int>();
            Dictionary<GameOutcome.GameEndReason, int> EndReasons = new Dictionary<GameOutcome.GameEndReason, int>();

            // TODO: use the windows temp folder
            String folder = "d:\\temp\\hanabi\\"+DateTime.Now.ToString("yyyy_MM_dd_H_mm_ss");

            for (int i = 0; i < numGames; i++)
            {
                Game game = new Game(folder, i);
                game.LogEnabled = true;

                game.AddPlayer(new InfoGiverBot());
                game.AddPlayer(new InfoGiverBot());
                game.AddPlayer(new InfoGiverBot());
                game.AddPlayer(new InfoGiverBot());
                game.AddPlayer(new InfoGiverBot());

                try
                {
                    var outcome = game.RunGame();
                    if(Scores.ContainsKey(outcome.Points))
                    {
                        Scores[outcome.Points]++;
                    }
                    else
                    {
                        Scores[outcome.Points] = 1;
                    }
                    
                    if(EndReasons.ContainsKey(outcome.EndReason))
                    {
                        EndReasons[outcome.EndReason]++;
                    }
                    else
                    {
                        EndReasons[outcome.EndReason] = 1;
                    }
                    
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Game "+i+": "+ex.Message);
                    if(game.LogToConsole)
                    {
                        throw ex;
                    }
                }
            }

            int count = Scores.Values.Aggregate(0, (t, c) => t += c);
            int total = Scores.Aggregate(0, (t, kvp) => t += kvp.Key * kvp.Value);
            int min = Scores.Min(kvp => kvp.Key);
            int max = Scores.Max(kvp => kvp.Key);
            float mean = (float)total / count;

            List<int> scoreList = new List<int>();
            foreach(var entry in Scores.OrderBy(k => k.Key))
            {
                for (int i = 0; i < entry.Value; i++)
                {
                    scoreList.Add(entry.Key);
                }
            }

            int median = scoreList.ElementAt(count / 2);

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
