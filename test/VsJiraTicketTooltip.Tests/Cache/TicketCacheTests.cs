using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using VsJiraTicketTooltip.Core.Cache;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Models;

namespace VsJiraTicketTooltip.Tests.Cache;

[TestFixture]
public class TicketCacheTests
{
    private ISystemClock _clock = null!;
    private TicketCache _cache = null!;

    [SetUp]
    public void SetUp()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTime.UtcNow);
        _cache = new TicketCache(_clock);
    }

    [Test]
    public void TryGet_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        var data = new TicketData("ABC-123", "Test Title", "https://jira.example.com/ABC-123");
        _cache.Set("ABC-123", data);

        // Act
        var result = _cache.TryGet("ABC-123", out var retrieved);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().Be(data);
    }

    [Test]
    public void TryGet_WhenKeyNotExists_ReturnsFalse()
    {
        // Act
        var result = _cache.TryGet("NONEXISTENT-999", out var retrieved);

        // Assert
        result.Should().BeFalse();
        retrieved.Should().BeNull();
    }

    [Test]
    public void TryGet_WhenEntryExpired_ReturnsFalse()
    {
        // Arrange
        var storedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(storedAt);
        _cache.Set("ABC-123", new TicketData("ABC-123", "Title", "https://jira.example.com/ABC-123"));

        // Symuluj upływ czasu powyżej TTL (301 sekund)
        _clock.UtcNow.Returns(storedAt.AddSeconds(TicketCache.TtlSeconds + 1));

        // Act
        var result = _cache.TryGet("ABC-123", out var retrieved);

        // Assert
        result.Should().BeFalse();
        retrieved.Should().BeNull();
    }

    [Test]
    public void TryGet_WhenEntryNotExpired_ReturnsTrue()
    {
        // Arrange
        var storedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(storedAt);
        var data = new TicketData("ABC-123", "Title", "https://jira.example.com/ABC-123");
        _cache.Set("ABC-123", data);

        // Symuluj upływ czasu poniżej TTL (299 sekund)
        _clock.UtcNow.Returns(storedAt.AddSeconds(TicketCache.TtlSeconds - 1));

        // Act
        var result = _cache.TryGet("ABC-123", out var retrieved);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().Be(data);
    }

    [Test]
    public void Set_WhenCacheExceedsCapacity_EvictsLruEntry()
    {
        // Arrange — wypełnij cache do maksimum
        for (int i = 0; i < TicketCache.MaxCapacity; i++)
        {
            var key = $"PROJ-{i}";
            _cache.Set(key, new TicketData(key, $"Title {i}", $"https://jira.example.com/{key}"));
        }

        // Upewnij się, że pierwszy wpis istnieje przed przekroczeniem limitu
        _cache.TryGet("PROJ-0", out _).Should().BeTrue();

        // Dodaj jeszcze jeden wpis — powinien wypchnąć LRU
        // Po TryGet("PROJ-0") węzeł PROJ-0 jest MRU, więc LRU to PROJ-1
        _cache.Set("PROJ-NEW", new TicketData("PROJ-NEW", "New Title", "https://jira.example.com/PROJ-NEW"));

        // Assert — PROJ-1 powinien zostać usunięty (był LRU po tym jak PROJ-0 stał się MRU)
        _cache.TryGet("PROJ-1", out _).Should().BeFalse();

        // Nowy wpis powinien być dostępny
        _cache.TryGet("PROJ-NEW", out _).Should().BeTrue();

        // PROJ-0 powinien nadal być dostępny (był MRU)
        _cache.TryGet("PROJ-0", out _).Should().BeTrue();
    }

    [Test]
    public void Set_WhenKeyAlreadyExists_UpdatesEntry()
    {
        // Arrange
        var original = new TicketData("ABC-123", "Original Title", "https://jira.example.com/ABC-123");
        var updated = new TicketData("ABC-123", "Updated Title", "https://jira.example.com/ABC-123");
        _cache.Set("ABC-123", original);

        // Act
        _cache.Set("ABC-123", updated);
        var result = _cache.TryGet("ABC-123", out var retrieved);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().Be(updated);
        retrieved!.Title.Should().Be("Updated Title");
    }

    [Test]
    public void Invalidate_WhenKeyExists_RemovesEntry()
    {
        // Arrange
        var data = new TicketData("ABC-123", "Title", "https://jira.example.com/ABC-123");
        _cache.Set("ABC-123", data);

        // Act
        _cache.Invalidate("ABC-123");
        var result = _cache.TryGet("ABC-123", out var retrieved);

        // Assert
        result.Should().BeFalse();
        retrieved.Should().BeNull();
    }

    [Test]
    public void Invalidate_WhenKeyNotExists_DoesNotThrow()
    {
        // Act & Assert — nie powinno rzucić wyjątku
        var act = () => _cache.Invalidate("NONEXISTENT-999");
        act.Should().NotThrow();
    }

    [Test]
    public void Clear_RemovesAllEntries()
    {
        // Arrange — dodaj kilka wpisów
        _cache.Set("ABC-1", new TicketData("ABC-1", "Title 1", "https://jira.example.com/ABC-1"));
        _cache.Set("ABC-2", new TicketData("ABC-2", "Title 2", "https://jira.example.com/ABC-2"));
        _cache.Set("ABC-3", new TicketData("ABC-3", "Title 3", "https://jira.example.com/ABC-3"));

        // Act
        _cache.Clear();

        // Assert — wszystkie wpisy powinny być usunięte
        _cache.TryGet("ABC-1", out _).Should().BeFalse();
        _cache.TryGet("ABC-2", out _).Should().BeFalse();
        _cache.TryGet("ABC-3", out _).Should().BeFalse();
    }
}
