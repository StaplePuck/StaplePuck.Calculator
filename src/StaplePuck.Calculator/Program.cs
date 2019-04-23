using System;

namespace StaplePuck.Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var request = new LeagueRequest { LeagueId = 1 };

            // init league
            // set day state
            // refresh stats
            Updater.UpdateLeague(request);
        }
    }
}
