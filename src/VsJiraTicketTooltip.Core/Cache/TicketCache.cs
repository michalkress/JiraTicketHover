using System.Collections.Concurrent;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Models;

namespace VsJiraTicketTooltip.Core.Cache;

/// <summary>
/// Thread-safe implementacja LRU cache z TTL dla danych ticketów.
/// Maksymalnie <see cref="MaxCapacity"/> wpisów, TTL <see cref="TtlSeconds"/> sekund.
/// </summary>
public sealed class TicketCache : ITicketCache
{
    /// <summary>Maksymalna liczba wpisów w cache.</summary>
    public const int MaxCapacity = 500;

    /// <summary>Czas życia wpisu w sekundach.</summary>
    public const int TtlSeconds = 300;

    private readonly ISystemClock _clock;
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();
    private readonly LinkedList<string> _lruList = new();
    private readonly object _lruLock = new();

    /// <summary>
    /// Inicjalizuje nową instancję <see cref="TicketCache"/>.
    /// </summary>
    /// <param name="clock">Abstrakcja zegara systemowego (wstrzykiwana dla testowalności TTL).</param>
    public TicketCache(ISystemClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public bool TryGet(string key, out TicketData? data)
    {
        if (!_store.TryGetValue(key, out var entry))
        {
            data = null;
            return false;
        }

        // Sprawdź TTL
        if (_clock.UtcNow - entry.StoredAt > TimeSpan.FromSeconds(TtlSeconds))
        {
            // Wpis wygasł — usuń go
            lock (_lruLock)
            {
                if (_store.TryRemove(key, out var expiredEntry))
                {
                    _lruList.Remove(expiredEntry.LruNode);
                }
            }

            data = null;
            return false;
        }

        // Przesuń węzeł na koniec listy (most recently used)
        lock (_lruLock)
        {
            // Sprawdź ponownie po uzyskaniu locka — wpis mógł zostać usunięty
            if (!_store.ContainsKey(key))
            {
                data = null;
                return false;
            }

            _lruList.Remove(entry.LruNode);
            _lruList.AddLast(entry.LruNode);
        }

        data = entry.Data;
        return true;
    }

    /// <inheritdoc />
    public void Set(string key, TicketData data)
    {
        lock (_lruLock)
        {
            if (_store.TryGetValue(key, out var existing))
            {
                // Klucz już istnieje — usuń stary węzeł LRU i zaktualizuj wpis
                _lruList.Remove(existing.LruNode);
                _store.TryRemove(key, out _);
            }
            else if (_store.Count >= MaxCapacity)
            {
                // Cache pełny — usuń najdawniej używany wpis (pierwszy węzeł listy)
                var lruKey = _lruList.First?.Value;
                if (lruKey != null)
                {
                    _lruList.RemoveFirst();
                    _store.TryRemove(lruKey, out _);
                }
            }

            // Dodaj nowy węzeł na koniec listy i utwórz CacheEntry
            var node = _lruList.AddLast(key);
            var newEntry = new CacheEntry(data, _clock.UtcNow, node);
            _store[key] = newEntry;
        }
    }

    /// <inheritdoc />
    public void Invalidate(string key)
    {
        lock (_lruLock)
        {
            if (_store.TryRemove(key, out var entry))
            {
                _lruList.Remove(entry.LruNode);
            }
            // Jeśli klucz nie istnieje — ignoruj
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lruLock)
        {
            _store.Clear();
            _lruList.Clear();
        }
    }
}
