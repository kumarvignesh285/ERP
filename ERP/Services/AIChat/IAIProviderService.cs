using System.Threading;
using System.Threading.Tasks;

namespace ERP.Services.AIChat;

public interface IAIProviderService
{
    string ProviderName { get; }
    bool IsAvailable { get; }
    Task<AIProviderResponse> ProcessQueryAsync(AIProviderRequest request, CancellationToken ct = default);
}
