using KromicFlow.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace KromicFlow.Infrastructure.Services;

public sealed class OAuthStateService(IMemoryCache cache) : IOAuthStateService
{
    private static readonly TimeSpan StateExpiration = TimeSpan.FromMinutes(10);

    public string GenerateState()
    {
        var state = Guid.NewGuid().ToString("N");
        cache.Set(state, true, StateExpiration);
        return state;
    }

    public bool ValidateState(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return false;

        return cache.TryGetValue(state, out _);
    }
}
