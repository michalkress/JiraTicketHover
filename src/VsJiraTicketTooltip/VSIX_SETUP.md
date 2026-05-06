# VSIX Setup — VsJiraTicketTooltip

Ten dokument opisuje kroki potrzebne do przekształcenia projektu-placeholdera w pełnoprawną
wtyczkę VSIX gotową do załadowania w Visual Studio 2026+.

---

## 1. Wymagane pakiety NuGet

Dodaj do `VsJiraTicketTooltip.csproj`:

```xml
<PackageReference Include="Microsoft.VisualStudio.Extensibility" Version="17.13.*" />
<PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.*" />
<PackageReference Include="Microsoft.VSSDK.BuildTools" Version="17.*" PrivateAssets="all" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.*" />
```

> **Uwaga**: `Microsoft.VisualStudio.Extensibility` 17.13+ jest wymagany dla trybu
> hybrydowego in-proc/out-of-proc (VSSDK-compatible VisualStudio.Extensibility).

---

## 2. Zmiany w `.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <RootNamespace>VsJiraTicketTooltip</RootNamespace>
    <AssemblyName>VsJiraTicketTooltip</AssemblyName>

    <!-- VSIX-specific -->
    <ProjectTypeGuids>{82b43b9b-a64c-4715-b499-d71e9ca2bd60};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids>
    <IncludeAssemblyInVSIXContainer>true</IncludeAssemblyInVSIXContainer>
    <GeneratePkgDefFile>false</GeneratePkgDefFile>
    <UseCodebase>true</UseCodebase>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\VsJiraTicketTooltip.Core\VsJiraTicketTooltip.Core.csproj" />
  </ItemGroup>

</Project>
```

---

## 3. Plik `source.extension.vsixmanifest`

Utwórz `source.extension.vsixmanifest` w katalogu projektu:

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011"
                 xmlns:d="http://schemas.microsoft.com/developer/vsx-schema-design/2011">
  <Metadata>
    <Identity Id="VsJiraTicketTooltip.YourCompany" Version="1.0.0" Language="en-US"
              Publisher="YourCompany" />
    <DisplayName>Jira Ticket Tooltip</DisplayName>
    <Description>Displays Jira ticket title and link in a tooltip when hovering over ticket identifiers in code comments.</Description>
    <Tags>jira, tooltip, editor</Tags>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Community" Version="[17.0,)" />
  </Installation>
  <Dependencies>
    <Dependency Id="Microsoft.Framework.NDP" DisplayName=".NET Framework" Version="[4.7.2,)" />
  </Dependencies>
  <Prerequisites>
    <Prerequisite Id="Microsoft.VisualStudio.Component.CoreEditor" Version="[17.0,)"
                  DisplayName="Visual Studio core editor" />
  </Prerequisites>
  <Assets>
    <Asset Type="Microsoft.VisualStudio.MefComponent" d:Source="Project" d:ProjectName="%CurrentProject%"
           Path="|%CurrentProject%|" />
  </Assets>
</PackageManifest>
```

---

## 4. Odkomentowanie atrybutów MEF

### `Editor/CommentTaggerProvider.cs`

Odkomentuj atrybuty MEF eksportujące provider do Visual Studio:

```csharp
[Export(typeof(IViewTaggerProvider))]
[ContentType("code")]
[TagType(typeof(TextMarkerTag))]
[Name("JiraTicketTagger")]
public class CommentTaggerProvider : IViewTaggerProvider
{
    // ...
}
```

### `Editor/JiraQuickInfoSourceProvider.cs`

Odkomentuj atrybuty MEF eksportujące provider QuickInfo:

```csharp
[Export(typeof(IAsyncQuickInfoSourceProvider))]
[Name("JiraQuickInfoSourceProvider")]
[ContentType("code")]
[Order]
public class JiraQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
{
    // ...
}
```

> **Uwaga**: Atrybuty MEF są zakomentowane w bieżącej wersji, ponieważ projekt nie ma
> zainstalowanego VS SDK. Po dodaniu pakietów NuGet z punktu 1 należy je odkomentować.

---

## 5. Implementacja `ISettingsStore` przez VisualStudio.Extensibility Settings API

Utwórz klasę `VsSettingsStore` implementującą `ISettingsStore`:

```csharp
using Microsoft.VisualStudio.Extensibility.Settings;
using VsJiraTicketTooltip.Core.Settings;

public class VsSettingsStore : ISettingsStore
{
    private readonly ISettingsManager _settingsManager;

    public event EventHandler<ExtensionSettings>? SettingsChanged;

    public VsSettingsStore(ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
        // Subskrybuj zdarzenia zmiany ustawień z VS Settings API
        _settingsManager.SettingChanged += OnVsSettingChanged;
    }

    public ExtensionSettings Load()
    {
        return new ExtensionSettings
        {
            IsEnabled        = _settingsManager.GetValueOrDefault(SettingKeys.IsEnabled, true),
            JiraInstanceUrl  = _settingsManager.GetValueOrDefault(SettingKeys.JiraInstanceUrl, string.Empty),
            OAuthClientId    = _settingsManager.GetValueOrDefault(SettingKeys.OAuthClientId, string.Empty),
            ActiveProvider   = _settingsManager.GetValueOrDefault(SettingKeys.ActiveProvider, "Jira"),
        };
    }

    public void Save(ExtensionSettings settings)
    {
        _settingsManager.SetValue(SettingKeys.IsEnabled, settings.IsEnabled);
        _settingsManager.SetValue(SettingKeys.JiraInstanceUrl, settings.JiraInstanceUrl);
        _settingsManager.SetValue(SettingKeys.OAuthClientId, settings.OAuthClientId);
        _settingsManager.SetValue(SettingKeys.ActiveProvider, settings.ActiveProvider);
    }

    private void OnVsSettingChanged(object? sender, SettingChangedEventArgs e)
    {
        SettingsChanged?.Invoke(this, Load());
    }
}
```

Klucze ustawień zdefiniuj w osobnej klasie statycznej:

```csharp
internal static class SettingKeys
{
    public const string IsEnabled       = "VsJiraTicketTooltip.IsEnabled";
    public const string JiraInstanceUrl = "VsJiraTicketTooltip.JiraInstanceUrl";
    public const string OAuthClientId   = "VsJiraTicketTooltip.OAuthClientId";
    public const string ActiveProvider  = "VsJiraTicketTooltip.ActiveProvider";
}
```

---

## 6. Klasa `Extension` dziedzicząca po `Microsoft.VisualStudio.Extensibility.Extension`

Utwórz klasę `JiraTicketTooltipExtension` jako punkt wejścia wtyczki:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using VsJiraTicketTooltip.Core.Settings;

[VisualStudioContribution]
public class JiraTicketTooltipExtension : Extension
{
    /// <inheritdoc />
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
            id: "VsJiraTicketTooltip.YourCompany",
            version: ExtensionAssemblyVersion,
            publisherName: "YourCompany",
            displayName: "Jira Ticket Tooltip",
            description: "Displays Jira ticket title and link in a tooltip when hovering over ticket identifiers in code comments."),
    };

    /// <inheritdoc />
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);

        // Wczytaj ustawienia początkowe i utwórz composition root
        var initialSettings = new ExtensionSettings(); // zastąp odczytem z VS Settings API
        var compositionRoot = new ExtensionCompositionRoot(initialSettings);

        // Zarejestruj serwisy w DI
        serviceCollection.AddSingleton(compositionRoot.TicketDataService);
        serviceCollection.AddSingleton(compositionRoot.SettingsObserver);
        serviceCollection.AddSingleton(compositionRoot.ProviderRegistry);

        // Zarejestruj connector łączący ISettingsStore z SettingsObserver
        serviceCollection.AddSingleton<ISettingsStore, VsSettingsStore>();
        serviceCollection.AddSingleton<ExtensionSettingsConnector>();
    }
}
```

> **Uwaga**: `[VisualStudioContribution]` jest atrybutem z `Microsoft.VisualStudio.Extensibility`
> rejestrującym klasę jako punkt wejścia wtyczki. Klasa musi dziedziczyć po `Extension`
> i być w assembly oznaczonym jako MEF component w `source.extension.vsixmanifest`.

---

## Kolejność kroków wdrożenia

1. Zainstaluj pakiety NuGet (punkt 1)
2. Zaktualizuj `.csproj` (punkt 2)
3. Utwórz `source.extension.vsixmanifest` (punkt 3)
4. Odkomentuj atrybuty MEF (punkt 4)
5. Zaimplementuj `VsSettingsStore` (punkt 5)
6. Utwórz klasę `JiraTicketTooltipExtension` (punkt 6)
7. Uruchom `dotnet build` i napraw ewentualne błędy kompilacji
8. Przetestuj w Visual Studio Experimental Instance (`F5` z projektem VSIX)
