﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace StaplePuck.Calculator.Data
{
    public class CalculatedScoreItem
    {
        public int ScoringTypeId { get; set; }
        public int Total { get; set; }
        public int TodaysTotal { get; set; }
        public int Score
        {
            get
            {
                return Convert.ToInt32(Math.Floor(Total * ScoreMultiplyer));
            }
        }

        public int TodaysScore
        {
            get
            {
                return Convert.ToInt32(Math.Floor(TodaysTotal * ScoreMultiplyer));
            }
        }

        [JsonIgnore]
        public float ScoreMultiplyer { get; set; }
    }
}
