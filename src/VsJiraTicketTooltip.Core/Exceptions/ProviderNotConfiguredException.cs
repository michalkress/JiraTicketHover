namespace VsJiraTicketTooltip.Core.Exceptions;

/// <summary>
/// Wyjątek rzucany przez <c>IProviderRegistry.GetActiveProvider()</c> gdy żaden provider
/// nie jest zarejestrowany lub żaden nie jest wybrany jako aktywny.
/// </summary>
public class ProviderNotConfiguredException : InvalidOperationException
{
    public ProviderNotConfiguredException()
        : base("No ticket provider is configured or active. Register a provider and set it as active.")
    {
    }

    public ProviderNotConfiguredException(string message)
        : base(message)
    {
    }

    public ProviderNotConfiguredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
