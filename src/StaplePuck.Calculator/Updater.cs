using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using StaplePuck.Core.Auth;
using StaplePuck.Core.Stats;
using StaplePuck.Core.Client;
using Microsoft.Extensions.Options;

namespace StaplePuck.Calculator
{
    public class Updater
    {
        private readonly Settings _settings;
        private readonly StatsProvider _statsProvider;

        public Updater(IOptions<Settings> options, StatsProvider statsProvider)
        {
            _settings = options.Value;
            _statsProvider = statsProvider;
        }

        public static Updater Init()
        {
            return null;
        }

        public static void UpdateLeague(LeagueRequest request)
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var configuration = builder.Build();

            var serviceProvider = new ServiceCollection()
                .AddOptions()
                .AddSingleton<StatsProvider>()
                .AddAuth0Client(configuration)
                .AddStaplePuckClient(configuration)
                .BuildServiceProvider();

            var provider = serviceProvider.GetService<StatsProvider>();

            var league = provider.GetLeagueStats(request.LeagueId).Result;
            var playerScores = provider.GeneratePlayerScores(league).Result;
            var teamScores = provider.GenerateTeamScores(league, playerScores).Result;

            var leagueScore = new Data.LeagueScore
            {
                Id = request.LeagueId,
                PlayerCalculatedScores = playerScores,
                FantasyTeams = teamScores
            };
            var client = serviceProvider.GetService<IStaplePuckClient>();
            var result = client.UpdateAsync("updateLeagueScores", leagueScore, "league").Result;

            //var playerScores = provider.GetScoresForDateAsync(request.GameDateId).Result;
            //var teamStates = provider.GetTeamsStatesAsync(request.SeasonId).Result;
        }

    }
}
