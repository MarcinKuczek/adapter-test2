namespace Liberis.OrchestrationAdapter.Core.Options
{
    public class ApiOptions
    {
        public const string Example = "Example";
        public string Uri { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public double CacheLifeTimeMinutes { get; set; }
    }
}
