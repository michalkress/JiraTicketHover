using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Jira;

namespace VsJiraTicketTooltip.Tests.Jira;

[TestFixture]
public class JiraOAuthServiceTests
{
    private ICredentialStore _credentialStore = null!;
    private FakeHttpMessageHandler _httpHandler = null!;
    private HttpClient _httpClient = null!;
    private JiraOAuthService _service = null!;

    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";
    private const string RedirectUri = "http://localhost:9089/callback";

    [SetUp]
    public void SetUp()
    {
        _credentialStore = Substitute.For<ICredentialStore>();
        _httpHandler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);
        _service = new JiraOAuthService(ClientId, ClientSecret, _credentialStore, _httpClient, RedirectUri);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    [Test]
    public void GetAuthorizationUrl_ReturnsValidUrl_WithStateParameter()
    {
        // Act
        var (url, state) = _service.GetAuthorizationUrl();

        // Assert
        url.Should().NotBeNullOrEmpty();
        url.Should().StartWith("https://auth.atlassian.com/authorize?");
        url.Should().Contain($"client_id={ClientId}");
        url.Should().Contain("audience=api.atlassian.com");
        url.Should().Contain($"state={state}");
        url.Should().Contain("response_type=code");
        url.Should().Contain("scope=read%3ajira-work+offline_access");

        state.Should().NotBeNullOrEmpty();
        state.Length.Should().BeGreaterThan(20); // Kryptograficznie losowy string
    }

    [Test]
    public void GenerateState_ReturnsDifferentValuesEachTime()
    {
        // Act
        var state1 = _service.GenerateState();
        var state2 = _service.GenerateState();

        // Assert
        state1.Should().NotBeNullOrEmpty();
        state2.Should().NotBeNullOrEmpty();
        state1.Should().NotBe(state2);
    }

    [Test]
    public async Task ExchangeCodeForTokenAsync_WhenSuccess_StoresTokensInCredentialStore()
    {
        // Arrange
        var tokenResponse = new
        {
            access_token = "test-access-token",
            refresh_token = "test-refresh-token",
            expires_in = 3600,
            token_type = "Bearer",
            scope = "read:jira-work offline_access"
        };

        var accessibleResourcesResponse = new[]
        {
            new { id = "test-cloud-id", name = "Test Site" }
        };

        _httpHandler.AddResponse(
            "https://auth.atlassian.com/oauth/token",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        _httpHandler.AddResponse(
            "https://api.atlassian.com/oauth/token/accessible-resources",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(accessibleResourcesResponse));

        // Act
        await _service.ExchangeCodeForTokenAsync("test-code");

        // Assert
        _credentialStore.Received(1).Save(
            "VsJiraTicketTooltip/AccessToken",
            "jira_oauth",
            "test-access-token");

        _credentialStore.Received(1).Save(
            "VsJiraTicketTooltip/RefreshToken",
            "jira_oauth",
            "test-refresh-token");

        _credentialStore.Received(1).Save(
            Arg.Is<string>(s => s == "VsJiraTicketTooltip/TokenExpiry"),
            "jira_oauth",
            Arg.Any<string>());

        _credentialStore.Received(1).Save(
            "VsJiraTicketTooltip/CloudId",
            "jira_oauth",
            "test-cloud-id");
    }

    [Test]
    public void ExchangeCodeForTokenAsync_WhenHttpFails_ThrowsException()
    {
        // Arrange
        _httpHandler.AddResponse(
            "https://auth.atlassian.com/oauth/token",
            HttpStatusCode.BadRequest,
            "{\"error\":\"invalid_grant\"}");

        // Act
        Func<Task> act = async () => await _service.ExchangeCodeForTokenAsync("invalid-code");

        // Assert
        act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*400*");
    }

    [Test]
    public async Task RefreshAccessTokenAsync_WhenTokenValid_ReturnsTrueWithoutHttpCall()
    {
        // Arrange — token ważny (wygasa za 10 minut)
        var expiry = DateTime.UtcNow.AddMinutes(10);
        _credentialStore.TryLoad("VsJiraTicketTooltip/TokenExpiry", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = expiry.ToString("O");
                return true;
            });

        // Act
        var result = await _service.RefreshAccessTokenAsync();

        // Assert
        result.Should().BeTrue(); // Token był ważny, nie trzeba było odświeżać
        _httpHandler.RequestCount.Should().Be(0); // Brak wywołań HTTP
    }

    [Test]
    public async Task RefreshAccessTokenAsync_WhenTokenExpired_CallsRefreshEndpoint()
    {
        // Arrange — token wygasły (wygasł 1 minutę temu)
        var expiry = DateTime.UtcNow.AddMinutes(-1);
        _credentialStore.TryLoad("VsJiraTicketTooltip/TokenExpiry", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = expiry.ToString("O");
                return true;
            });

        _credentialStore.TryLoad("VsJiraTicketTooltip/RefreshToken", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = "test-refresh-token";
                return true;
            });

        // Symuluj istniejący CloudId — zapobiega dodatkowemu wywołaniu HTTP
        _credentialStore.TryLoad("VsJiraTicketTooltip/CloudId", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = "existing-cloud-id";
                return true;
            });

        var tokenResponse = new
        {
            access_token = "new-access-token",
            refresh_token = "new-refresh-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        _httpHandler.AddResponse(
            "https://auth.atlassian.com/oauth/token",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        // Act
        var result = await _service.RefreshAccessTokenAsync();

        // Assert
        result.Should().BeFalse(); // Token został odświeżony
        _httpHandler.RequestCount.Should().Be(1); // Jedno wywołanie HTTP (tylko refresh)
        _credentialStore.Received().Save(
            "VsJiraTicketTooltip/AccessToken",
            "jira_oauth",
            "new-access-token");
    }

    [Test]
    public void RefreshAccessTokenAsync_WhenNoRefreshToken_ThrowsException()
    {
        // Arrange — brak refresh tokenu
        var expiry = DateTime.UtcNow.AddMinutes(-1);
        _credentialStore.TryLoad("VsJiraTicketTooltip/TokenExpiry", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = expiry.ToString("O");
                return true;
            });

        _credentialStore.TryLoad("VsJiraTicketTooltip/RefreshToken", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _service.RefreshAccessTokenAsync();

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No refresh token available*");
    }

    [Test]
    public async Task EnsureValidTokenAsync_WhenTokenValid_DoesNotRefresh()
    {
        // Arrange — token ważny
        var expiry = DateTime.UtcNow.AddMinutes(10);
        _credentialStore.TryLoad("VsJiraTicketTooltip/AccessToken", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = "valid-access-token";
                return true;
            });

        _credentialStore.TryLoad("VsJiraTicketTooltip/TokenExpiry", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = expiry.ToString("O");
                return true;
            });

        // Act
        await _service.EnsureValidTokenAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(0); // Brak wywołań HTTP
    }

    [Test]
    public async Task EnsureValidTokenAsync_WhenTokenExpired_RefreshesToken()
    {
        // Arrange — token wygasły
        var expiry = DateTime.UtcNow.AddMinutes(-1);
        _credentialStore.TryLoad("VsJiraTicketTooltip/AccessToken", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = "expired-access-token";
                return true;
            });

        _credentialStore.TryLoad("VsJiraTicketTooltip/TokenExpiry", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = expiry.ToString("O");
                return true;
            });

        _credentialStore.TryLoad("VsJiraTicketTooltip/RefreshToken", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = "test-refresh-token";
                return true;
            });

        // Symuluj istniejący CloudId — zapobiega dodatkowemu wywołaniu HTTP
        _credentialStore.TryLoad("VsJiraTicketTooltip/CloudId", out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(x =>
            {
                x[1] = "jira_oauth";
                x[2] = "existing-cloud-id";
                return true;
            });

        var tokenResponse = new
        {
            access_token = "refreshed-access-token",
            refresh_token = "refreshed-refresh-token",
            expires_in = 3600,
            token_type = "Bearer"
        };

        _httpHandler.AddResponse(
            "https://auth.atlassian.com/oauth/token",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(tokenResponse));

        // Act
        await _service.EnsureValidTokenAsync();

        // Assert
        _httpHandler.RequestCount.Should().Be(1); // Jedno wywołanie HTTP (tylko refresh)
        _credentialStore.Received().Save(
            "VsJiraTicketTooltip/AccessToken",
            "jira_oauth",
            "refreshed-access-token");
    }

    /// <summary>
    /// Fake HttpMessageHandler do mockowania odpowiedzi HTTP w testach.
    /// </summary>
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode StatusCode, string Content)> _responses = new();
        public int RequestCount { get; private set; }

        public void AddResponse(string url, HttpStatusCode statusCode, string content)
        {
            _responses[url] = (statusCode, content);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;

            var requestUrl = request.RequestUri?.GetLeftPart(UriPartial.Path) ?? string.Empty;

            if (_responses.TryGetValue(requestUrl, out var response))
            {
                return Task.FromResult(new HttpResponseMessage(response.StatusCode)
                {
                    Content = new StringContent(response.Content, Encoding.UTF8, "application/json")
                });
            }

            // Domyślna odpowiedź 404
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"error\":\"not_found\"}", Encoding.UTF8, "application/json")
            });
        }
    }
}
