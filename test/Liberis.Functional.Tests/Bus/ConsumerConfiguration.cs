namespace Liberis.OrchestrationAdapter.Functional.Tests.Bus
{
    public class ConsumerConfiguration
    {
        public string ExchangeName { get; set; }
        public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Fanout;
        public string RoutingKey { get; set; } = "";
        public bool Durable { get; set; } = false;
    }
}
