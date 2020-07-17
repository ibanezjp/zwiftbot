using Newtonsoft.Json;

namespace ZwiftBot
{
    public class User
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Name { get; set; }
        public RoutesList CompletedRoutes { get; set; }
        public RoutesList PendingRoutes { get; set; }
    }
}
