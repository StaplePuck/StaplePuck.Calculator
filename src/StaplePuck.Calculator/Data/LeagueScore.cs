using System;
using System.Collections.Generic;
using System.Text;

namespace StaplePuck.Calculator.Data
{
    public class LeagueScore
    {
        public int Id { get; set; }
        public IEnumerable<FantasyTeamScore> FantasyTeams { get; set; }
        public IEnumerable<CalculatedScore> PlayerCalculatedScores { get; set; }
    }
}
