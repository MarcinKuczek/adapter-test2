using Liberis.OrchestrationAdapter.Core.Interfaces;
using MassTransit;
using Liberis.OrchestrationAdapter.Core.Models;

namespace Liberis.OrchestrationAdapter.Application.Consumers
{
    public class ExampleRequestConsumer : AdapterConsumer<ExampleHubRequest, ExampleAdapterResponse>
    {
        public ExampleRequestConsumer(IPublishEndpoint publishEndpoint, IAdapterService<ExampleHubRequest, ExampleAdapterResponse> adapterService) : base(publishEndpoint, adapterService)
        {
        }
    }
}
