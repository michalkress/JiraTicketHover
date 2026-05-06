namespace VsJiraTicketTooltip.Core.Models;

/// <summary>
/// Wewnętrzny wpis cache przechowujący dane ticketu wraz z metadanymi LRU i TTL.
/// Używany wyłącznie przez implementację <c>TicketCache</c>.
/// </summary>
internal record CacheEntry(
    TicketData Data,
    DateTime StoredAt,
    LinkedListNode<string> LruNode
);
