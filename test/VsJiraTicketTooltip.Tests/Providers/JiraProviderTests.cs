using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Providers;

namespace VsJiraTicketTooltip.Tests.Providers;

[TestFixture]
public class JiraProviderTests
{
    private const string JiraInstanceUrl = "https://mycompany.atlassian.net";
    private const string CloudId = "test-cloud-id-123";
    private const string TicketKey = "ABC-123";

    private IJiraOAuthService _oauthService = null!;
    private JiraProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _oauthService = Substitute.For<IJiraOAuthService>();
        _oauthService.GetCloudId().Returns(CloudId);

        _provider = new JiraProvider(_oauthService, JiraInstanceUrl, NullLogger<JiraProvider>.Instance);
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    // -------------------------------------------------------------------------
    // Test 1: HTTP 200 — sukces
    // -------------------------------------------------------------------------

    [Test]
    public async Task FetchAsync_WhenSuccess_ReturnsTicketData()
    {
        // Arrange
        const string json = """{"fields":{"summary":"Test Title"}}""";
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.OK, json));

        // Act
        var result = await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        result.Key.Should().Be(TicketKey);
        result.Title.Should().Be("Test Title");
        result.Url.Should().Be($"{JiraInstanceUrl}/browse/{TicketKey}");
    }

    [Test]
    public async Task FetchAsync_WhenSuccess_CallsEnsureValidTokenFirst()
    {
        // Arrange
        const string json = """{"fields":{"summary":"Some Title"}}""";
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.OK, json));

        // Act
        await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        await _oauthService.Received(1).EnsureValidTokenAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FetchAsync_WhenSuccess_BuildsCorrectApiUrl()
    {
        // Arrange
        const string json = """{"fields":{"summary":"Title"}}""";
        string? capturedUrl = null;
        _oauthService
            .CallJiraApiAsync(Arg.Do<string>(url => capturedUrl = url), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.OK, json));

        // Act
        await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        capturedUrl.Should().Be(
            $"https://api.atlassian.com/ex/jira/{CloudId}/rest/api/3/issue/{TicketKey}?fields=summary");
    }

    // -------------------------------------------------------------------------
    // Test 2: HTTP 404 — ticket nie istnieje
    // -------------------------------------------------------------------------

    [Test]
    public async Task FetchAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.NotFound, "{}"));

        // Act
        var act = async () => await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{TicketKey}*");
    }

    // -------------------------------------------------------------------------
    // Test 3: HTTP 401 → refresh → HTTP 200 (retry sukces)
    // -------------------------------------------------------------------------

    [Test]
    public async Task FetchAsync_WhenUnauthorized_RefreshesAndRetries()
    {
        // Arrange
        const string json = """{"fields":{"summary":"Retried Title"}}""";

        int callCount = 0;
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return callCount == 1
                    ? CreateJsonResponse(HttpStatusCode.Unauthorized, "{}")
                    : CreateJsonResponse(HttpStatusCode.OK, json);
            });

        // Act
        var result = await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert — retry nastąpił (2 wywołania API)
        callCount.Should().Be(2);
        await _oauthService.Received(1).RefreshAccessTokenAsync(Arg.Any<CancellationToken>());
        result.Title.Should().Be("Retried Title");
    }

    // -------------------------------------------------------------------------
    // Test 4: HTTP 401 → refresh → HTTP 401 (retry też nieudany)
    // -------------------------------------------------------------------------

    [Test]
    public async Task FetchAsync_WhenUnauthorizedAfterRetry_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.Unauthorized, "{}"));

        // Act
        var act = async () => await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _oauthService.Received(1).RefreshAccessTokenAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FetchAsync_WhenForbiddenAfterRetry_ThrowsUnauthorizedAccessException()
    {
        // Arrange — HTTP 403 → refresh → HTTP 403
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.Forbidden, "{}"));

        // Act
        var act = async () => await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // -------------------------------------------------------------------------
    // Test 5: HTTP 500 — błąd serwera
    // -------------------------------------------------------------------------

    [Test]
    public async Task FetchAsync_WhenServerError_ThrowsHttpRequestException()
    {
        // Arrange
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.InternalServerError, "Internal Server Error"));

        // Act
        var act = async () => await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task FetchAsync_WhenServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.ServiceUnavailable, "Service Unavailable"));

        // Act
        var act = async () => await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // -------------------------------------------------------------------------
    // Test 6: Anulowanie CancellationToken
    // -------------------------------------------------------------------------

    [Test]
    public async Task FetchAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<HttpResponseMessage>(_ => throw new OperationCanceledException());

        // Act
        var act = async () => await _provider.FetchAsync(TicketKey, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task FetchAsync_WhenEnsureValidTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _oauthService
            .EnsureValidTokenAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new OperationCanceledException());

        // Act
        var act = async () => await _provider.FetchAsync(TicketKey, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // -------------------------------------------------------------------------
    // Dodatkowe testy
    // -------------------------------------------------------------------------

    [Test]
    public void ProviderName_ReturnsJira()
    {
        _provider.ProviderName.Should().Be("Jira");
    }

    [Test]
    public async Task FetchAsync_WhenCloudIdIsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        _oauthService.GetCloudId().Returns((string?)null);

        // Act
        var act = async () => await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task FetchAsync_WhenSuccess_TicketUrlUsesJiraInstanceUrlNotApiUrl()
    {
        // Arrange — URL ticketu powinien wskazywać na UI Jiry, nie na API
        const string json = """{"fields":{"summary":"Title"}}""";
        _oauthService
            .CallJiraApiAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(CreateJsonResponse(HttpStatusCode.OK, json));

        // Act
        var result = await _provider.FetchAsync(TicketKey, CancellationToken.None);

        // Assert
        result.Url.Should().StartWith(JiraInstanceUrl);
        result.Url.Should().NotContain("api.atlassian.com");
        result.Url.Should().Be($"{JiraInstanceUrl}/browse/{TicketKey}");
    }
}
