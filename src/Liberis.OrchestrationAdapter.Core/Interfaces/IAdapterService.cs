using System.Threading.Tasks;

namespace Liberis.OrchestrationAdapter.Core.Interfaces
{
    public interface IAdapterService<TRequest, TResponse>
    {
        Task<TResponse> Process(TRequest request);
    }
}
