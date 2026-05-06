using System.Collections.Concurrent;
using VsJiraTicketTooltip.Core.Exceptions;
using VsJiraTicketTooltip.Core.Interfaces;

namespace VsJiraTicketTooltip.Core.Providers;

/// <summary>
/// Thread-safe rejestr providerów ticketów.
/// Przechowuje mapowanie nazwa → <see cref="ITicketProvider"/> i udostępnia aktywnego providera.
/// </summary>
public class ProviderRegistry : IProviderRegistry
{
    private readonly ConcurrentDictionary<string, ITicketProvider> _providers = new();
    private volatile string? _activeProviderName;
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Register(ITicketProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        _providers[provider.ProviderName] = provider;

        // Jeśli to pierwszy zarejestrowany provider, ustaw go automatycznie jako aktywny.
        // Używamy _lock, aby operacja "sprawdź czy pierwszy + ustaw aktywny" była atomowa.
        if (_activeProviderName is null)
        {
            lock (_lock)
            {
                if (_activeProviderName is null)
                {
                    _activeProviderName = provider.ProviderName;
                }
            }
        }
    }

    /// <inheritdoc />
    public ITicketProvider GetActiveProvider()
    {
        var name = _activeProviderName;

        if (name is null || !_providers.TryGetValue(name, out var provider))
        {
            throw new ProviderNotConfiguredException();
        }

        return provider;
    }

    /// <inheritdoc />
    public void SetActiveProvider(string providerName)
    {
        ArgumentNullException.ThrowIfNull(providerName);

        if (!_providers.ContainsKey(providerName))
        {
            throw new ProviderNotConfiguredException(
                $"Provider '{providerName}' is not registered. Register it first before setting it as active.");
        }

        _activeProviderName = providerName;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetRegisteredProviderNames()
    {
        return _providers.Keys.ToList().AsReadOnly();
    }
}
