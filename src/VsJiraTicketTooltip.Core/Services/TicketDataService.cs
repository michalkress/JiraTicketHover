using Microsoft.Extensions.Logging;
using VsJiraTicketTooltip.Core.Exceptions;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Models;

namespace VsJiraTicketTooltip.Core.Services;

/// <summary>
/// Fasada łącząca cache z rejestrem providerów. Jedyny punkt wejścia dla warstwy edytora
/// do pobierania danych ticketów.
/// </summary>
public sealed class TicketDataService : ITicketDataService
{
    private readonly ITicketCache _cache;
    private readonly IProviderRegistry _registry;
    private readonly ILogger<TicketDataService> _logger;

    public TicketDataService(
        ITicketCache cache,
        IProviderRegistry registry,
        ILogger<TicketDataService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TicketDataResult> GetTicketDataAsync(
        string ticketKey,
        CancellationToken cancellationToken)
    {
        // 1. Sprawdź cache
        if (_cache.TryGet(ticketKey, out var cachedData) && cachedData is not null)
        {
            _logger.LogDebug("Cache hit for ticket {TicketKey}", ticketKey);
            return new TicketDataResult.Success(cachedData);
        }

        // 2. Pobierz aktywnego providera
        ITicketProvider provider;
        try
        {
            provider = _registry.GetActiveProvider();
        }
        catch (ProviderNotConfiguredException ex)
        {
            _logger.LogWarning(ex, "No active provider configured when fetching ticket {TicketKey}", ticketKey);
            return new TicketDataResult.ProviderNotConfigured();
        }

        // 3. Wywołaj providera i mapuj wyjątki
        try
        {
            var data = await provider.FetchAsync(ticketKey, cancellationToken).ConfigureAwait(false);

            _cache.Set(ticketKey, data);
            _logger.LogDebug("Fetched and cached ticket {TicketKey}", ticketKey);

            return new TicketDataResult.Success(data);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogInformation(ex, "Ticket {TicketKey} not found", ticketKey);
            return new TicketDataResult.NotFound(ticketKey);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized when fetching ticket {TicketKey}", ticketKey);
            return new TicketDataResult.Unauthorized();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timed out for ticket {TicketKey}", ticketKey);
            return new TicketDataResult.Timeout(ticketKey);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error when fetching ticket {TicketKey}", ticketKey);
            return new TicketDataResult.ServiceError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when fetching ticket {TicketKey}", ticketKey);
            return new TicketDataResult.ServiceError(ex.Message);
        }
    }
}
