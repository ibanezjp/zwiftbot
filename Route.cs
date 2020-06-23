using System;
using Newtonsoft.Json;

namespace CoreBot
{
    public class Route
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Name { get; set; }
        public int Distance { get; set; }
        public int Gain { get; set; }
        public string World { get; set; }
        public int DistanceFromStart { get; set; }
        public int MinLevel { get; set; }
        public bool EventOnly { get; set; }
        public bool HasAward { get; set; }
        public Sports AllowedSports { get; set; }
        public DateTime? Completed { get; set; }

        public override string ToString()
        {
            return $"{Name} {Distance / 1000}Kms";
        }
    }

    [Flags]
    public enum Sports
    {
        None = 0,
        Running = 1,
        Cycling = 2
    }
}
