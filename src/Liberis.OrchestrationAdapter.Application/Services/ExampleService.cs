using System.Net.Http;
using System.Threading.Tasks;
using Liberis.Common.Services;
using Liberis.OrchestrationAdapter.Core.Interfaces;
using Liberis.OrchestrationAdapter.Core.Models;

namespace Liberis.OrchestrationAdapter.Application.Services
{
    public class ExampleService : ExternalServiceBase, IAdapterService<ExampleHubRequest, ExampleAdapterResponse>
    {
        public ExampleService(IHttpClientFactory httpClientFactory) : base(httpClientFactory.CreateClient())
        {
        }
            
        public async Task<ExampleAdapterResponse> Process(ExampleHubRequest request)
        {
            return await GetAsync<ExampleAdapterResponse>("http://localhost:3000/hub-1-external-1");
        }
    }
}