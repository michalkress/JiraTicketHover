// Ten plik jest placeholderem dla projektu głównej wtyczki VSIX.
//
// Pełna implementacja wymaga:
// - Visual Studio SDK (Microsoft.VSSDK.BuildTools)
// - VisualStudio.Extensibility 17.13+ (Microsoft.VisualStudio.Extensibility)
// - Microsoft.VisualStudio.SDK (dla IAsyncQuickInfoSource, ITextViewTaggerProvider)
// - source.extension.vsixmanifest
//
// Komponenty do zaimplementowania w kolejnych zadaniach:
// - CommentTaggerProvider / CommentTagger (in-proc MEF, ITextViewTaggerProvider)
// - JiraQuickInfoSourceProvider / JiraQuickInfoSource (in-proc MEF, IAsyncQuickInfoSourceProvider)
// - ExtensionSettings (VisualStudio.Extensibility Settings API)
// - SettingsObserver
// - Punkt wejścia Extension class z konfiguracją DI

namespace VsJiraTicketTooltip;
