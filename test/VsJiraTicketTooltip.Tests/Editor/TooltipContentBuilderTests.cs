using FluentAssertions;
using NUnit.Framework;
using VsJiraTicketTooltip.Core.Editor;
using VsJiraTicketTooltip.Core.Models;

namespace VsJiraTicketTooltip.Tests.Editor;

[TestFixture]
public class TooltipContentBuilderTests
{
    // -----------------------------------------------------------------------
    // Success — dane ticketu dostępne
    // -----------------------------------------------------------------------

    [Test]
    public void Build_WhenSuccess_ReturnsTicketKeyTitleAndUrl()
    {
        // Validates: Requirements 2.2
        var data = new TicketData("ABC-123", "Fix login bug", "https://jira.example.com/browse/ABC-123");
        var result = new TicketDataResult.Success(data);

        var content = TooltipContentBuilder.Build("ABC-123", result);

        content.TicketKey.Should().Be("ABC-123");
        content.Title.Should().Be("Fix login bug");
        content.Url.Should().Be("https://jira.example.com/browse/ABC-123");
    }

    [Test]
    public void Build_WhenSuccess_IsNotError()
    {
        // Validates: Requirements 2.2
        var data = new TicketData("PROJ-456", "Add dark mode", "https://jira.example.com/browse/PROJ-456");
        var result = new TicketDataResult.Success(data);

        var content = TooltipContentBuilder.Build("PROJ-456", result);

        content.IsError.Should().BeFalse();
        content.ErrorMessage.Should().BeEmpty();
    }

    [Test]
    public void Build_WhenSuccess_UrlMatchesTicketDataUrl()
    {
        // Validates: Requirements 2.2
        const string expectedUrl = "https://jira.mycompany.com/browse/TEAM-999";
        var data = new TicketData("TEAM-999", "Some task", expectedUrl);
        var result = new TicketDataResult.Success(data);

        var content = TooltipContentBuilder.Build("TEAM-999", result);

        content.Url.Should().Be(expectedUrl);
    }

    // -----------------------------------------------------------------------
    // NotFound — ticket nie istnieje
    // -----------------------------------------------------------------------

    [Test]
    public void Build_WhenNotFound_IsError_WithNotFoundMessage()
    {
        // Validates: Requirements 2.5
        var result = new TicketDataResult.NotFound("ABC-123");

        var content = TooltipContentBuilder.Build("ABC-123", result);

        content.IsError.Should().BeTrue();
        content.ErrorMessage.Should().Be("Ticket not found");
    }

    [Test]
    public void Build_WhenNotFound_ContainsTicketKey()
    {
        // Validates: Requirements 2.5
        var result = new TicketDataResult.NotFound("XYZ-789");

        var content = TooltipContentBuilder.Build("XYZ-789", result);

        content.TicketKey.Should().Be("XYZ-789");
    }

    [Test]
    public void Build_WhenError_UrlIsNull()
    {
        // Validates: Requirements 2.5 — błędy nie mają URL
        var result = new TicketDataResult.NotFound("ABC-123");

        var content = TooltipContentBuilder.Build("ABC-123", result);

        content.Url.Should().BeNull();
        content.Title.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // Unauthorized — brak autoryzacji
    // -----------------------------------------------------------------------

    [Test]
    public void Build_WhenUnauthorized_IsError_WithAuthMessage()
    {
        // Validates: Requirements 2.5
        var result = new TicketDataResult.Unauthorized();

        var content = TooltipContentBuilder.Build("ABC-123", result);

        content.IsError.Should().BeTrue();
        content.ErrorMessage.Should().Contain("re-authorize");
        content.ErrorMessage.Should().Contain("Tools → Options");
    }

    // -----------------------------------------------------------------------
    // Timeout — przekroczenie czasu
    // -----------------------------------------------------------------------

    [Test]
    public void Build_WhenTimeout_IsError_WithTimeoutMessage()
    {
        // Validates: Requirements 2.5
        var result = new TicketDataResult.Timeout("ABC-123");

        var content = TooltipContentBuilder.Build("ABC-123", result);

        content.IsError.Should().BeTrue();
        content.ErrorMessage.Should().Be("Request timed out");
    }

    // -----------------------------------------------------------------------
    // ServiceError — błąd serwisu
    // -----------------------------------------------------------------------

    [Test]
    public void Build_WhenServiceError_IsError_ContainsErrorMessage()
    {
        // Validates: Requirements 2.5
        var result = new TicketDataResult.ServiceError("HTTP 503 Service Unavailable");

        var content = TooltipContentBuilder.Build("ABC-123", result);

        content.IsError.Should().BeTrue();
        content.ErrorMessage.Should().Contain("Service unavailable");
        content.ErrorMessage.Should().Contain("HTTP 503 Service Unavailable");
    }

    // -----------------------------------------------------------------------
    // ProviderNotConfigured — brak skonfigurowanego providera
    // -----------------------------------------------------------------------

    [Test]
    public void Build_WhenProviderNotConfigured_IsError_WithConfigMessage()
    {
        // Validates: Requirements 2.5
        var result = new TicketDataResult.ProviderNotConfigured();

        var content = TooltipContentBuilder.Build("ABC-123", result);

        content.IsError.Should().BeTrue();
        content.ErrorMessage.Should().Contain("No provider configured");
        content.ErrorMessage.Should().Contain("Tools → Options");
    }
}
