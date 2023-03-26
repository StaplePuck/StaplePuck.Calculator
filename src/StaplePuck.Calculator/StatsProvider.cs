using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        private const string LeagueStatsQuery = @"query my($leagueId: ID) {
  leagues (id: $leagueId) {
    id
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
    }
  }
}";

        private const string LeagueStatsInfoQuery = @"query my($leagueId: ID) {
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
      scoringWeight
    }
  }
}";

        private const string LeagueStatsSeasonsQuery = @"query my($leagueId: ID) {
  leagues (id: $leagueId) {
    id
    season {
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

        public async Task<League?> GetLeagueStats(int leagueId)
        {
            var variables = new ExpandoObject() as IDictionary<string, object>;
            variables.Add("leagueId", leagueId);

            var result = await _staplePuckClient.GetAsync<LeagueResponse>(LeagueStatsQuery, variables);
            var result2 = await _staplePuckClient.GetAsync<LeagueResponse>(LeagueStatsInfoQuery, variables);
            var resultSeason = await _staplePuckClient.GetAsync<LeagueResponse>(LeagueStatsSeasonsQuery, variables);

            if (result.Leagues.Length == 0)
            {
                return null;
            }

            var league = result.Leagues[0];
            league.FantasyTeams = result2.Leagues[0].FantasyTeams;
            league.ScoringRules = result2.Leagues[0].ScoringRules;
            if (league.Season != null && resultSeason.Leagues[0]?.Season?.PlayerSeasons != null)
            {
                league.Season.PlayerSeasons = resultSeason.Leagues[0].Season!.PlayerSeasons;
            }
            return league;
        }

        public async Task<IEnumerable<Data.CalculatedScore>> GeneratePlayerScores(League league, bool init)
        {
            var todaysId = DateExtensions.TodaysDateId();

            var multiplyers = new Dictionary<ScoreRuleKey, double>(new ScoreRuleKeyComparer());
            foreach (var item in league.ScoringRules)
            {
                var key = new ScoreRuleKey { TypeId = item.ScoringTypeId, PositionId = item.PositionTypeId };
                multiplyers.Add(key, item.ScoringWeight);
            }
            var playerScores = new Dictionary<int, Data.CalculatedScore>();
            if (league.Season == null)
            {
                return playerScores.Values;
            }
            foreach (var date in league.Season.GameDates)
            {
                if (date.GameDate == null)
                {
                    continue;
                }
                foreach (var player in date.GameDate.PlayersStatsOnDate)
                {
                    var playerInfo = league.Season.PlayerSeasons.FirstOrDefault(x => x.PlayerId == player.PlayerId);
                    if (playerInfo == null)
                    {
                        Console.WriteLine($"Warning unable to find player position for {player.PlayerId}");
                        continue;
                    }
                    Data.CalculatedScore? score;
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
                        double multiplyer = 0;

                        if (!multiplyers.TryGetValue(new ScoreRuleKey { PositionId = playerInfo.PositionTypeId, TypeId = si.ScoringTypeId }, out multiplyer))
                        {
                            if (!multiplyers.TryGetValue(new ScoreRuleKey { PositionId = 1, TypeId = si.ScoringTypeId }, out multiplyer))
                            {
                                Console.WriteLine($"Warning unable to find scoring value for position: {playerInfo.PositionType} and scoring type {si.ScoringTypeId} ");
                            }
                        }
                        Data.CalculatedScoreItem? scoringItem = score.Scoring.FirstOrDefault(x => x.ScoringTypeId == si.ScoringTypeId);
                        if (scoringItem == null)
                        {
                            scoringItem = new Data.CalculatedScoreItem
                            {
                                ScoringTypeId = si.ScoringTypeId,
                                ScoreMultiplyer = multiplyer
                            };
                            score.Scoring.Add(scoringItem);
                        }

                        scoringItem.Total += si.Total;
                        if (todaysId == date.GameDate.Id)
                        {
                            scoringItem.TodaysTotal = si.Total;
                        }
                    }
                }
            }

            if (init)
            {
                foreach (var player in league.Season.PlayerSeasons)
                {
                    Data.CalculatedScore? score;
                    if (!playerScores.TryGetValue(player.PlayerId, out score))
                    {
                        score = new Data.CalculatedScore
                        {
                            PlayerId = player.PlayerId,
                            LeagueId = league.Id
                        };
                        playerScores.Add(player.PlayerId, score);
                    }

                    // calculate total count
                    score.NumberOfSelectedByTeams = league.FantasyTeams.Count(x => x.FantasyTeamPlayers.Any(p => p.PlayerId == player.PlayerId));
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
            public bool Equals(ScoreRuleKey? x, ScoreRuleKey? y)
            {
                if (x == null || y == null)
                {
                    return x == y;
                }
                return x.PositionId == y.PositionId && x.TypeId == y.TypeId;
            }

            public int GetHashCode(ScoreRuleKey obj)
            {
                return obj.TypeId.GetHashCode() ^ obj.PositionId.GetHashCode();
            }
        }
    }
}
