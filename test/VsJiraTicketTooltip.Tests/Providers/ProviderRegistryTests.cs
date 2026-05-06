using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using VsJiraTicketTooltip.Core.Exceptions;
using VsJiraTicketTooltip.Core.Interfaces;
using VsJiraTicketTooltip.Core.Providers;

namespace VsJiraTicketTooltip.Tests.Providers;

[TestFixture]
public class ProviderRegistryTests
{
    private ProviderRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        _registry = new ProviderRegistry();
    }

    private static ITicketProvider CreateProvider(string name)
    {
        var provider = Substitute.For<ITicketProvider>();
        provider.ProviderName.Returns(name);
        return provider;
    }

    [Test]
    public void Register_FirstProvider_SetsAsActive()
    {
        // Arrange
        var provider = CreateProvider("Jira");

        // Act
        _registry.Register(provider);

        // Assert
        _registry.GetActiveProvider().Should().BeSameAs(provider);
    }

    [Test]
    public void Register_SecondProvider_DoesNotChangeActive()
    {
        // Arrange
        var first = CreateProvider("Jira");
        var second = CreateProvider("GitHub");

        // Act
        _registry.Register(first);
        _registry.Register(second);

        // Assert
        _registry.GetActiveProvider().Should().BeSameAs(first);
    }

    [Test]
    public void Register_ExistingProviderName_ReplacesProvider()
    {
        // Arrange
        var original = CreateProvider("Jira");
        var replacement = CreateProvider("Jira");

        _registry.Register(original);

        // Act
        _registry.Register(replacement);

        // Assert — aktywny provider powinien być nową instancją
        _registry.GetActiveProvider().Should().BeSameAs(replacement);
    }

    [Test]
    public void GetActiveProvider_WhenNoProviderRegistered_ThrowsProviderNotConfiguredException()
    {
        // Act
        var act = () => _registry.GetActiveProvider();

        // Assert
        act.Should().Throw<ProviderNotConfiguredException>();
    }

    [Test]
    public void GetActiveProvider_WhenProviderRegistered_ReturnsActiveProvider()
    {
        // Arrange
        var provider = CreateProvider("Jira");
        _registry.Register(provider);

        // Act
        var result = _registry.GetActiveProvider();

        // Assert
        result.Should().BeSameAs(provider);
    }

    [Test]
    public void SetActiveProvider_WhenProviderExists_ChangesActiveProvider()
    {
        // Arrange
        var jira = CreateProvider("Jira");
        var github = CreateProvider("GitHub");
        _registry.Register(jira);
        _registry.Register(github);

        // Act
        _registry.SetActiveProvider("GitHub");

        // Assert
        _registry.GetActiveProvider().Should().BeSameAs(github);
    }

    [Test]
    public void SetActiveProvider_WhenProviderNotExists_ThrowsProviderNotConfiguredException()
    {
        // Arrange
        _registry.Register(CreateProvider("Jira"));

        // Act
        var act = () => _registry.SetActiveProvider("NonExistent");

        // Assert
        act.Should().Throw<ProviderNotConfiguredException>();
    }

    [Test]
    public void GetRegisteredProviderNames_ReturnsAllRegisteredNames()
    {
        // Arrange
        _registry.Register(CreateProvider("Jira"));
        _registry.Register(CreateProvider("GitHub"));
        _registry.Register(CreateProvider("AzureDevOps"));

        // Act
        var names = _registry.GetRegisteredProviderNames();

        // Assert
        names.Should().BeEquivalentTo(new[] { "Jira", "GitHub", "AzureDevOps" });
    }

    [Test]
    public void GetRegisteredProviderNames_WhenEmpty_ReturnsEmptyList()
    {
        // Act
        var names = _registry.GetRegisteredProviderNames();

        // Assert
        names.Should().BeEmpty();
    }
}
