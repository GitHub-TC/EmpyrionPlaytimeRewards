using EmpyrionNetAPIDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace EmpyrionPlaytimeRewards
{
    [Serializable]
    public class Configuration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; set; } = LogLevel.Message;
        public string ChatCommandPrefix { get; set; } = "/\\";
        public int XpRewardPeriodInMinutes { get; set; } = 1;
        public int XpPerPeriod { get; set; } = 100;
        public int PlayerMaxXp { get; set; } = 500000;
    }
}
