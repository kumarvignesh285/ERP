using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ERP.ViewModels;

namespace ERP.Interfaces;

public interface IAIChatService
{
    Task<AIChatResponseDto> ProcessMessageAsync(AIChatRequestDto request, ClaimsPrincipal user, CancellationToken ct = default);
    List<AIChatSuggestionDto> GetSuggestedQuestions(ClaimsPrincipal user);
    Task ClearConversationHistoryAsync(string conversationId, ClaimsPrincipal user);
}
