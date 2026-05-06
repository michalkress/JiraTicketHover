using FluentAssertions;
using NUnit.Framework;
using VsJiraTicketTooltip.Core.Editor;

namespace VsJiraTicketTooltip.Tests.Editor;

[TestFixture]
public class TicketIdentifierScannerTests
{
    // -----------------------------------------------------------------------
    // Podstawowe przypadki dopasowania
    // -----------------------------------------------------------------------

    [Test]
    public void Scan_WhenCommentHasSingleTicket_ReturnsOneMatch()
    {
        // Validates: Requirements 1.1, 1.2
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("// Fix for ABC-123");

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("ABC-123");
    }

    [Test]
    public void Scan_WhenCommentHasMultipleTickets_ReturnsAllMatches()
    {
        // Validates: Requirements 1.1, 1.5
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("// ABC-123 and PROJ-456");

        result.Should().HaveCount(2);
        result.Select(m => m.Key).Should().BeEquivalentTo(new[] { "ABC-123", "PROJ-456" });
    }

    [Test]
    public void Scan_WhenNoTickets_ReturnsEmptyList()
    {
        // Validates: Requirements 1.1
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("// No tickets here");

        result.Should().BeEmpty();
    }

    [Test]
    public void Scan_WhenEmptyString_ReturnsEmptyList()
    {
        // Validates: Requirements 1.1
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers(string.Empty);

        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Pozycja identyfikatora w tekście
    // -----------------------------------------------------------------------

    [Test]
    public void Scan_WhenTicketAtStartOfString_ReturnsMatch()
    {
        // Validates: Requirements 1.2
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("ABC-123 fix");

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("ABC-123");
    }

    [Test]
    public void Scan_WhenTicketAtEndOfString_ReturnsMatch()
    {
        // Validates: Requirements 1.2
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("fix for ABC-123");

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("ABC-123");
    }

    // -----------------------------------------------------------------------
    // Negatywne przypadki — wzorce które NIE powinny pasować
    // -----------------------------------------------------------------------

    [Test]
    public void Scan_WhenLowercasePattern_DoesNotMatch()
    {
        // Validates: Requirements 1.1 — wzorzec wymaga wielkich liter [A-Z]+
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("abc-123");

        result.Should().BeEmpty();
    }

    [Test]
    public void Scan_WhenPartialPattern_DoesNotMatch()
    {
        // Validates: Requirements 1.1 — "ABC-" bez cyfr nie pasuje do [A-Z]+-[0-9]+
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("ABC-");

        result.Should().BeEmpty();
    }

    [Test]
    public void Scan_WhenTicketEmbeddedInWord_DoesNotMatch()
    {
        // Validates: Requirements 1.1 — \b word boundary: "XABC-123Y" nie pasuje
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("XABC-123Y");

        result.Should().BeEmpty();
    }

    // -----------------------------------------------------------------------
    // Weryfikacja pozycji i długości dopasowania
    // -----------------------------------------------------------------------

    [Test]
    public void Scan_VerifiesCorrectStartPosition()
    {
        // Validates: Requirements 1.2
        // "// Fix for ABC-123" — "ABC-123" zaczyna się na indeksie 11
        const string text = "// Fix for ABC-123";
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers(text);

        result.Should().HaveCount(1);
        var match = result[0];
        match.Start.Should().Be(text.IndexOf("ABC-123", StringComparison.Ordinal));
        text.Substring(match.Start, match.Length).Should().Be("ABC-123");
    }

    [Test]
    public void Scan_VerifiesCorrectLength()
    {
        // Validates: Requirements 1.2
        // "ABC-123" ma długość 7, "PROJ-4567" ma długość 9
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("// ABC-123 and PROJ-4567");

        result.Should().HaveCount(2);
        result[0].Length.Should().Be("ABC-123".Length);
        result[1].Length.Should().Be("PROJ-4567".Length);
    }

    // -----------------------------------------------------------------------
    // Dodatkowe przypadki brzegowe
    // -----------------------------------------------------------------------

    [Test]
    public void Scan_WhenNullString_ReturnsEmptyList()
    {
        // Validates: Requirements 1.1 — null traktowany jak brak tekstu
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers(null!);

        result.Should().BeEmpty();
    }

    [Test]
    public void Scan_WhenMixedCasePrefix_DoesNotMatch()
    {
        // Validates: Requirements 1.1 — "Abc-123" nie pasuje (wymaga wyłącznie [A-Z]+)
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers("Abc-123");

        result.Should().BeEmpty();
    }

    [Test]
    public void Scan_WhenMultipleTicketsInSingleComment_ReturnsCorrectPositions()
    {
        // Validates: Requirements 1.5 — wiele identyfikatorów, każdy z poprawną pozycją
        const string text = "// ABC-1 and DEF-22 and GHI-333";
        var result = TicketIdentifierScanner.ScanForTicketIdentifiers(text);

        result.Should().HaveCount(3);

        foreach (var match in result)
        {
            text.Substring(match.Start, match.Length).Should().Be(match.Key);
        }
    }
}
