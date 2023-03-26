using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StaplePuck.Calculator.Data
{
    public class CalculatedScore
    {
        public int PlayerId { get; set; }
        public int LeagueId { get; set; }
        public List<CalculatedScoreItem> Scoring { get; set; }
        public int NumberOfSelectedByTeams { get; set; }
        public int GameState { get; set; }
        public int Score
        {
            get
            {
                return Scoring.Sum(x => Convert.ToInt32(Math.Floor(x.Total * x.ScoreMultiplyer)));
            }
        }
        public int TodaysScore
        {
            get
            {
                return Scoring.Sum(x => x.TodaysScore);
            }
        }

        public CalculatedScore()
        {
            NumberOfSelectedByTeams = -2;
            GameState = -2;
            Scoring = new List<CalculatedScoreItem>();
        }
    }
}
