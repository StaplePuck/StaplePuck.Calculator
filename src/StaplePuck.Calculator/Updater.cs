﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        private readonly IStaplePuckClient _client;

        public Updater(IOptions<Settings> options, StatsProvider statsProvider, IStaplePuckClient client)
        {
            _settings = options.Value;
            _statsProvider = statsProvider;
            _client = client;
        }

        public static Updater Init()
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var configuration = builder.Build();

            var serviceProvider = new ServiceCollection()
                .AddOptions()
                .Configure<Settings>(configuration.GetSection("Settings"))
                .AddSingleton<StatsProvider>()
                .AddSingleton<Updater>()
                .AddAuth0Client(configuration)
                .AddStaplePuckClient(configuration)
                .BuildServiceProvider();

            return serviceProvider.GetRequiredService<Updater>();
        }

        public static async Task UpdateLeague(LeagueRequest request)
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

            var provider = serviceProvider.GetRequiredService<StatsProvider>();

            var league = await provider.GetLeagueStats(request.LeagueId);
            if (league == null)
            {
                Console.Error.WriteLine("Failed to get league stats");
                return;
            }
            var playerScores = await provider.GeneratePlayerScores(league, request.Initialize);
            var teamScores = await provider.GenerateTeamScores(league, playerScores);

            var leagueScore = new Data.LeagueScore
            {
                Id = request.LeagueId,
                PlayerCalculatedScores = playerScores,
                FantasyTeams = teamScores
            };
            var client = serviceProvider.GetRequiredService<IStaplePuckClient>();
            var result = await client.UpdateAsync("updateLeagueScores", leagueScore, "league");
            if (result == null)
            {
                Console.Error.WriteLine("Null result");
            }
            else if (!result.Success)
            {
                Console.Error.WriteLine($"Failed to update. Message {result.Message}");
            }
        }

        public void Update()
        {
            bool done = false;
            var previousDateId = string.Empty;

            while (!done)
            {
                try
                {
                    Console.Out.WriteLine($"Updating league {_settings.LeagueId}");
                    var league = _statsProvider.GetLeagueStats(_settings.LeagueId).Result;
                    if (league == null)
                    {
                        Console.Error.WriteLine("Failed to get league stats");
                        return;
                    }
                    var playerScores = _statsProvider.GeneratePlayerScores(league, false).Result;
                    var teamScores = _statsProvider.GenerateTeamScores(league, playerScores).Result;

                    var leagueScore = new Data.LeagueScore
                    {
                        Id = _settings.LeagueId,
                        PlayerCalculatedScores = playerScores,
                        FantasyTeams = teamScores
                    };

                    Console.Out.WriteLine("Updating calcuations");
                    var result = _client.UpdateAsync("updateLeagueScores", leagueScore, "league").Result;
                    if (result == null)
                    {
                        Console.Error.WriteLine("Null result");
                    }
                    else if (!result.Success)
                    {
                        Console.Error.WriteLine($"Failed to update. Message {result.Message}");
                    }
                    Console.Out.WriteLine("Finished updating league");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Update failed. {e.Message}. {e.StackTrace}");
                }
                if (!_settings.Continuous)
                {
                    done = true;
                }
                else
                {
                    Task.Delay(_settings.Delay).Wait();
                }
            }
        }
    }
}
