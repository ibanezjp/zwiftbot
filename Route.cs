using System;
using System.Collections.Generic;
using System.Linq;
using F23.StringSimilarity;
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
            return $"{Name} {Distance / 1000}km + {(double)DistanceFromStart / 1000:F1}km ({Gain}m)";
        }
    }

    public class RoutesList : List<Route>
    {
        public Route FindRoute(string name)
        {
            var route = this.SingleOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (route != null)
                return route;

            var longestCommonSubsequence = new LongestCommonSubsequence();

            var distance = double.MaxValue;
            Route currentRoute = null;

            foreach (var tmp in this)
            {
                var currentDistance = longestCommonSubsequence.Distance(tmp.Name, name);
                if (currentDistance < distance)
                {
                    distance = currentDistance;
                    currentRoute = tmp;
                }
            }

            return currentRoute;
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
