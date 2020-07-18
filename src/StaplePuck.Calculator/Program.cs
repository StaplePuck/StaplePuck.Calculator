using System;

namespace StaplePuck.Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var request = new LeagueRequest { LeagueId = 1 };

            //Updater.UpdateLeague(request);

            var updater = Updater.Init();
            updater.Update();
        }
    }
}
