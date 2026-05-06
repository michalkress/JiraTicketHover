using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using VsJiraTicketTooltip.Core.Exceptions;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Models;
using VsJiraTicketTooltip.Core.Services;

namespace VsJiraTicketTooltip.Tests.Services;

[TestFixture]
public class TicketDataServiceTests
{
    private ITicketCache _cache = null!;
    private IProviderRegistry _registry = null!;
    private ITicketProvider _provider = null!;
    private TicketDataService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _cache = Substitute.For<ITicketCache>();
        _registry = Substitute.For<IProviderRegistry>();
        _provider = Substitute.For<ITicketProvider>();
        _registry.GetActiveProvider().Returns(_provider);
        _service = new TicketDataService(_cache, _registry, NullLogger<TicketDataService>.Instance);
    }

    [Test]
    public async Task GetTicketDataAsync_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var ticketData = new TicketData("ABC-123", "Cache Hit Title", "https://jira.example.com/ABC-123");
        _cache.TryGet("ABC-123", out Arg.Any<TicketData?>())
            .Returns(x =>
            {
                x[1] = ticketData;
                return true;
            });

        // Act
        var result = await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<TicketDataResult.Success>()
            .Which.Data.Should().Be(ticketData);

        // Provider nie powinien być wywoływany
        await _provider.DidNotReceive().FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetTicketDataAsync_WhenCacheMiss_FetchesFromProvider()
    {
        // Arrange
        var ticketData = new TicketData("ABC-123", "Provider Title", "https://jira.example.com/ABC-123");
        _cache.TryGet("ABC-123", out Arg.Any<TicketData?>()).Returns(false);
        _provider.FetchAsync("ABC-123", Arg.Any<CancellationToken>()).Returns(ticketData);

        // Act
        var result = await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<TicketDataResult.Success>()
            .Which.Data.Should().Be(ticketData);

        await _provider.Received(1).FetchAsync("ABC-123", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetTicketDataAsync_WhenProviderNotConfigured_ReturnsProviderNotConfigured()
    {
        // Arrange
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<TicketData?>()).Returns(false);
        _registry.GetActiveProvider().Throws(new ProviderNotConfiguredException());

        // Act
        var result = await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<TicketDataResult.ProviderNotConfigured>();
    }

    [Test]
    public async Task GetTicketDataAsync_WhenProviderReturnsData_StoresInCache()
    {
        // Arrange
        var ticketData = new TicketData("ABC-123", "Title", "https://jira.example.com/ABC-123");
        _cache.TryGet("ABC-123", out Arg.Any<TicketData?>()).Returns(false);
        _provider.FetchAsync("ABC-123", Arg.Any<CancellationToken>()).Returns(ticketData);

        // Act
        await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert — cache.Set powinien być wywołany z poprawnymi danymi
        _cache.Received(1).Set("ABC-123", ticketData);
    }

    [Test]
    public async Task GetTicketDataAsync_WhenProviderThrowsKeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<TicketData?>()).Returns(false);
        _provider.FetchAsync("ABC-123", Arg.Any<CancellationToken>())
            .Throws(new KeyNotFoundException("Ticket not found"));

        // Act
        var result = await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<TicketDataResult.NotFound>()
            .Which.Key.Should().Be("ABC-123");
    }

    [Test]
    public async Task GetTicketDataAsync_WhenProviderThrowsUnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<TicketData?>()).Returns(false);
        _provider.FetchAsync("ABC-123", Arg.Any<CancellationToken>())
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<TicketDataResult.Unauthorized>();
    }

    [Test]
    public async Task GetTicketDataAsync_WhenProviderThrowsOperationCanceledException_ReturnsTimeout()
    {
        // Arrange
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<TicketData?>()).Returns(false);
        _provider.FetchAsync("ABC-123", Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException("Request timed out"));

        // Act
        var result = await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<TicketDataResult.Timeout>()
            .Which.Key.Should().Be("ABC-123");
    }

    [Test]
    public async Task GetTicketDataAsync_WhenProviderThrowsHttpRequestException_ReturnsServiceError()
    {
        // Arrange
        const string errorMessage = "Connection refused";
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<TicketData?>()).Returns(false);
        _provider.FetchAsync("ABC-123", Arg.Any<CancellationToken>())
            .Throws(new HttpRequestException(errorMessage));

        // Act
        var result = await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<TicketDataResult.ServiceError>()
            .Which.Message.Should().Be(errorMessage);
    }

    [Test]
    public async Task GetTicketDataAsync_WhenProviderThrowsGenericException_ReturnsServiceError()
    {
        // Arrange
        const string errorMessage = "Unexpected internal error";
        _cache.TryGet(Arg.Any<string>(), out Arg.Any<TicketData?>()).Returns(false);
        _provider.FetchAsync("ABC-123", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(errorMessage));

        // Act
        var result = await _service.GetTicketDataAsync("ABC-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<TicketDataResult.ServiceError>()
            .Which.Message.Should().Be(errorMessage);
    }
}
