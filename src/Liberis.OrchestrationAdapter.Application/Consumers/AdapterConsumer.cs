using Liberis.OrchestrationAdapter.Core.Interfaces;
using Liberis.OrchestrationAdapter.Messages.V1;
using Liberis.OrchestrationHub.Messages.V1;
using MassTransit;
using System.Threading.Tasks;

namespace Liberis.OrchestrationAdapter.Application.Consumers
{
    public class AdapterConsumer<TRequest, TResponse> : IConsumer<HubRequest<TRequest>> where TRequest : class
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IAdapterService<TRequest, TResponse> _adapterService;
        public AdapterConsumer(IPublishEndpoint publishEndpoint, IAdapterService<TRequest, TResponse> adapterService)
        {
            _publishEndpoint = publishEndpoint;
            _adapterService = adapterService;
        }

        public async Task Consume(ConsumeContext<HubRequest<TRequest>> context)
        {
            HubRequest<TRequest> hubRequest = context.Message;
            var response = await _adapterService.Process(hubRequest.Request);
            var adapterResponse = new AdapterResponse<TResponse>
            {
                RequestId = hubRequest.RequestId,
                AdapterName = hubRequest.AdapterName,
                Response = response
            };

            await _publishEndpoint.Publish(adapterResponse);
        }
    }
}
