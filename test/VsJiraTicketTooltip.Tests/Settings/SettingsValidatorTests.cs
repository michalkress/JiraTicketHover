using FluentAssertions;
using NUnit.Framework;
using VsJiraTicketTooltip.Core.Settings;

namespace VsJiraTicketTooltip.Tests.Settings;

[TestFixture]
public class SettingsValidatorTests
{
    #region ValidateJiraInstanceUrl

    [TestCase("https://mycompany.atlassian.net", true)]
    [TestCase("https://jira.example.com", true)]
    [TestCase("https://jira.example.com/", true)]
    [TestCase("http://jira.example.com", false)]   // HTTP — odrzucony
    [TestCase("", false)]                          // pusty string
    [TestCase(null, false)]                        // null
    [TestCase("not-a-url", false)]                 // brak schematu
    [TestCase("ftp://jira.example.com", false)]    // zły schemat
    [TestCase("https://", false)]                  // brak hosta
    [TestCase("https:// ", false)]                 // whitespace jako host
    public void ValidateJiraInstanceUrl_ReturnsExpectedResult(string? url, bool expected)
    {
        var result = SettingsValidator.ValidateJiraInstanceUrl(url);

        result.Should().Be(expected, because: $"URL '{url}' should return {expected}");
    }

    #endregion

    #region Validate(ExtensionSettings)

    [Test]
    public void Validate_WhenAllValid_ReturnsEmptyList()
    {
        var settings = new ExtensionSettings
        {
            JiraInstanceUrl = "https://mycompany.atlassian.net",
            OAuthClientId = "my-client-id"
        };

        var errors = SettingsValidator.Validate(settings);

        errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_WhenUrlInvalid_ReturnsError()
    {
        var settings = new ExtensionSettings
        {
            JiraInstanceUrl = "http://not-https.example.com",
            OAuthClientId = "my-client-id"
        };

        var errors = SettingsValidator.Validate(settings);

        errors.Should().ContainSingle()
            .Which.Should().Contain("Jira Instance URL");
    }

    [Test]
    public void Validate_WhenClientIdEmpty_ReturnsError()
    {
        var settings = new ExtensionSettings
        {
            JiraInstanceUrl = "https://mycompany.atlassian.net",
            OAuthClientId = ""
        };

        var errors = SettingsValidator.Validate(settings);

        errors.Should().ContainSingle()
            .Which.Should().Contain("OAuth Client ID");
    }

    [Test]
    public void Validate_WhenMultipleErrors_ReturnsAllErrors()
    {
        var settings = new ExtensionSettings
        {
            JiraInstanceUrl = "not-a-url",
            OAuthClientId = "   "
        };

        var errors = SettingsValidator.Validate(settings);

        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.Contains("Jira Instance URL"));
        errors.Should().Contain(e => e.Contains("OAuth Client ID"));
    }

    #endregion
}
