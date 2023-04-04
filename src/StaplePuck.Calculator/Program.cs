using System;

namespace StaplePuck.Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var request = new LeagueRequest { LeagueId = 12, Initialize = false };

            Updater.UpdateLeague(request).Wait();

            //var updater = Updater.Init();
            //updater.Update();
        }
    }
}
