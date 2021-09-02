using Gelf.Extensions.Logging;

namespace Liberis.OrchestrationAdapter.Core.Options
{
    public class GelfOptions
    {
        public const string Options = "Logging:GELF";
        public string Host { get; set; }
        public int Port { get; set; }
        public string Source { get; set; } = null;
        public GelfProtocol Protocol { get; set; }
    }
}
