using System;

namespace StaplePuck.Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var request = new LeagueRequest { LeagueId = 5, Initialize = true };

            Updater.UpdateLeague(request).Wait();

            //var updater = Updater.Init();
            //updater.Update();
        }
    }
}
