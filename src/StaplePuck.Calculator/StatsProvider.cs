﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StaplePuck.Core;
using StaplePuck.Core.Client;
using StaplePuck.Core.Fantasy;

namespace StaplePuck.Calculator
{
    public class StatsProvider
    {
        private readonly IStaplePuckClient _staplePuckClient;
        private const string LeagueStatsQuery = @"query my($leagueId: String) {
  leagues (id: $leagueId) {
    id
    fantasyTeams {
      id
      fantasyTeamPlayers {
        playerId
      }
    }
    scoringRules {
      scoringTypeId
      positionTypeId
      pointsPerScore
    }
    season {
      externalId
      gameDates {
        gameDate {
          id        
          playersStatsOnDate {
            playerId
            playerScores {
              total
              scoringTypeId
            }
          }
        }
      }
      playerSeasons {
        playerId
        positionTypeId
      }
    }
  }
}";

        public StatsProvider(IStaplePuckClient client)
        {
            _staplePuckClient = client;
        }

        public async Task<League> GetLeagueStats(int leagueId)
        {
            var variables = new ExpandoObject() as IDictionary<string, object>;
            variables.Add("leagueId", leagueId.ToString());

            var result = await _staplePuckClient.GetAsync<League>(LeagueStatsQuery, variables);
            
            if (result.Length == 0)
            {
                return null;
            }

            return result[0];
        }

        public async Task<IEnumerable<Data.CalculatedScore>> GeneratePlayerScores(League league)
        {
            var todaysId = DateExtensions.TodaysDateId();

            var multiplyers = new Dictionary<ScoreRuleKey, int>(new ScoreRuleKeyComparer());
            foreach (var item in league.ScoringRules)
            {
                var key = new ScoreRuleKey { TypeId = item.ScoringTypeId, PositionId = item.PositionTypeId };
                multiplyers.Add(key, item.PointsPerScore);
            }
            var playerScores = new Dictionary<int, Data.CalculatedScore>();
            foreach (var date in league.Season.GameDates)
            {
                foreach (var player in date.GameDate.PlayersStatsOnDate)
                {
                    var playerInfo = league.Season.PlayerSeasons.FirstOrDefault(x => x.PlayerId == player.PlayerId);
                    if (playerInfo == null)
                    {
                        Console.WriteLine($"Warning unable to find player position for {player.Id}");
                        continue;
                    }
                    Data.CalculatedScore score;
                    if (!playerScores.TryGetValue(player.PlayerId, out score))
                    {
                        score = new Data.CalculatedScore
                        {
                            PlayerId = player.PlayerId,
                            LeagueId = league.Id
                        };
                        playerScores.Add(player.PlayerId, score);
                    }
                    foreach (var si in player.PlayerScores)
                    {
                        int multiplyer = 0;

                        if (!multiplyers.TryGetValue(new ScoreRuleKey { PositionId = playerInfo.PositionTypeId, TypeId = si.ScoringTypeId }, out multiplyer))
                        {
                            if (!multiplyers.TryGetValue(new ScoreRuleKey { PositionId = 1, TypeId = si.ScoringTypeId }, out multiplyer))
                            {
                                Console.WriteLine($"Warning unable to find scoring value for position: {playerInfo.PositionType} and scoring type {si.ScoringTypeId} ");
                            }
                        }
                        var scoringItem = new Data.CalculatedScoreItem
                        {
                            Total = si.Total,
                            ScoringTypeId = si.ScoringTypeId,
                            ScoreMultiplyer = multiplyer
                        };
                        if (todaysId == date.GameDate.Id)
                        {
                            scoringItem.TodaysTotal = si.Total;
                        }
                        score.Scoring.Add(scoringItem);
                    }
                }
            }

            await Task.CompletedTask;
            return playerScores.Values;
        }

        public async Task<IEnumerable<Data.FantasyTeamScore>> GenerateTeamScores(League league,  IEnumerable<Data.CalculatedScore> scores)
        {
            var teamList = new List<Data.FantasyTeamScore>();
            foreach (var team in league.FantasyTeams)
            {
                var teamScore = new Data.FantasyTeamScore { Id = team.Id };

                foreach (var player in team.FantasyTeamPlayers)
                {
                    var scoreItem = scores.FirstOrDefault(x => x.PlayerId == player.PlayerId);
                    if (scoreItem == null)
                    {
                        // Player hasn't scored yet. bummer
                        continue;
                    }

                    teamScore.Score += scoreItem.Score;
                    teamScore.TodaysScore += scoreItem.TodaysScore;
                }

                teamList.Add(teamScore);
            }

            int rank = 1;
            int lastScore = -1;
            int position = 1;
            foreach (var team in teamList.OrderByDescending(x => x.Score))
            {
                if (lastScore != team.Score)
                {
                    lastScore = team.Score;
                    rank = position;
                }

                team.Rank = rank;
                position++;
            }
            await Task.CompletedTask;
            return teamList;
        }

        private class ScoreRuleKey
        {
            public int TypeId { get; set; }
            public int PositionId { get; set; }
        }

        private class ScoreRuleKeyComparer : IEqualityComparer<ScoreRuleKey>
        { 
            public bool Equals(ScoreRuleKey x, ScoreRuleKey y)
            {
                return x.PositionId == y.PositionId && x.TypeId == y.TypeId;
            }

            public int GetHashCode(ScoreRuleKey obj)
            {
                return obj.TypeId.GetHashCode() ^ obj.PositionId.GetHashCode();
            }
        }
    }
}
