using asn1;

namespace asn1.Tests;

public class LiteralScannerTests
{
    [Test]
    public void ScannerRecognizesIntegersRealsExponentsAndRangeLookahead()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = CreateScanner("numbers.asn", "0 42 1.25 6E-3 7. 8e+9 9..10", true, diagnostics);

        LiteralScannerResult zero = scanner.ScanNext();
        AssertToken(zero, AsnTokenKind.IntegerLiteral, "0", 0, 1);
        Assert.That(zero.CanBeKind(AsnTokenKind.RealLiteral), Is.True);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.IntegerLiteral, "42", 2, 4);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.RealLiteral, "1.25", 5, 9);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.RealLiteral, "6E-3", 10, 14);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.RealLiteral, "7.", 15, 17);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.RealLiteral, "8e+9", 18, 22);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.IntegerLiteral, "9", 23, 24);
        AssertNonLiteral(scanner.ScanNext(), '.', 24, 25);
        AssertNonLiteral(scanner.ScanNext(), '.', 25, 26);
        AssertToken(scanner.ScanNext(), AsnTokenKind.IntegerLiteral, "10", 26, 28);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(LiteralScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerDiagnosesLeadingZerosIncompleteExponentsAndDigitSeparators()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = CreateScanner("invalid-numbers.asn", "01 0.0 1e+ 2_3", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "01", 0, 2);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "0.0", 3, 6);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "1e+", 7, 10);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "2_", 11, 13);
        AssertToken(scanner.ScanNext(), AsnTokenKind.IntegerLiteral, "3", 13, 14);

        AsnDiagnostic[] results = diagnostics.GetDiagnostics();
        Assert.That(results, Has.Length.EqualTo(4));
        Assert.That(results.Select(result => result.Code), Is.EqualTo(new[]
        {
            "ASNL0004",
            "ASNL0004",
            "ASNL0005",
            "ASNL0004"
        }));
    }

    [Test]
    public void ScannerRecognizesBinaryAndHexadecimalStringsWithWhitespace()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = CreateScanner("radix.asn", "'01 10'B '\nAF\t09'H ''B ''H", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.BinaryStringLiteral, "'01 10'B", 0, 8);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.HexStringLiteral, "'\nAF\t09'H", 9, 18);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.BinaryStringLiteral, "''B", 19, 22);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.HexStringLiteral, "''H", 23, 26);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerDiagnosesRadixContentSuffixAndTerminationErrors()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = CreateScanner("invalid-radix.asn", "'02'B 'af'H '01'X '01", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "'02'B", 0, 5);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "'af'H", 6, 11);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "'01'X", 12, 17);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "'01", 18, 21);

        Assert.That(
            diagnostics.GetDiagnostics().Select(result => result.Code),
            Is.EqualTo(new[] { "ASNL0006", "ASNL0006", "ASNL0008", "ASNL0007" }));
    }

    [Test]
    public void ScannerRetainsQuotedStringsDoubledQuotesCommentsAndLines()
    {
        string text = "\"\" \"a\"\"b\" \"line\n  next\" \"/*--*/\"";
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = CreateScanner("strings.asn", text, true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.CharacterStringLiteral, "\"\"", 0, 2);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.CharacterStringLiteral, "\"a\"\"b\"", 3, 9);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.CharacterStringLiteral, "\"line\n  next\"", 10, 23);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.CharacterStringLiteral, "\"/*--*/\"", 24, 32);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerDiagnosesInvalidAndUnterminatedQuotedStrings()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = CreateScanner("invalid-strings.asn", "\"a\u0001b\" \"open", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "\"a\u0001b\"", 0, 5);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "\"open", 6, 11);

        Assert.That(
            diagnostics.GetDiagnostics().Select(result => result.Code),
            Is.EqualTo(new[] { "ASNL0009", "ASNL0010" }));
    }

    [Test]
    public void ScannerRecognizesTupleAndQuadrupleAndRollsBackOtherBraces()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = CreateScanner(
            "tuples.asn",
            "{1, 2} {0,/* gap */1,2,3} {1,2,3}",
            true,
            diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.TupleLiteral, "{1, 2}", 0, 6);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.QuadrupleLiteral, "{0,/* gap */1,2,3}", 7, 25);
        SkipTrivia(scanner);
        AssertNonLiteral(scanner.ScanNext(), '{', 26, 27);
        AssertToken(scanner.ScanNext(), AsnTokenKind.IntegerLiteral, "1", 27, 28);
        AssertNonLiteral(scanner.ScanNext(), ',', 28, 29);
        AssertToken(scanner.ScanNext(), AsnTokenKind.IntegerLiteral, "2", 29, 30);
        AssertNonLiteral(scanner.ScanNext(), ',', 30, 31);
        AssertToken(scanner.ScanNext(), AsnTokenKind.IntegerLiteral, "3", 31, 32);
        AssertNonLiteral(scanner.ScanNext(), '}', 32, 33);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void TupleComponentsUseTheNumberLeadingZeroRule()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = CreateScanner("tuple-zero.asn", "{01,2}", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "{01,2}", 0, 6);
        Assert.That(diagnostics.GetDiagnostics(), Has.Length.EqualTo(1));
        Assert.That(diagnostics.GetDiagnostics()[0].Code, Is.EqualTo("ASNL0011"));
    }

    [Test]
    public void LiteralScanningResumesAcrossExponentQuoteAndTupleBoundaries()
    {
        SourceCursor cursor = new SourceCursor("chunks.asn");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        LiteralScanner scanner = new LiteralScanner(new TriviaScanner(cursor, diagnostics), diagnostics);

        cursor.SupplyChunk("1.25E-", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(LiteralScannerStatus.NeedMoreData));
        cursor.SupplyChunk("3 ", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(LiteralScannerStatus.NeedMoreData));
        cursor.SupplyChunk("'01'", false);
        AssertToken(scanner.ScanNext(), AsnTokenKind.RealLiteral, "1.25E-3", 0, 7);
        SkipTrivia(scanner);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(LiteralScannerStatus.NeedMoreData));
        cursor.SupplyChunk("B {1,", false);
        AssertToken(scanner.ScanNext(), AsnTokenKind.BinaryStringLiteral, "'01'B", 8, 13);
        SkipTrivia(scanner);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(LiteralScannerStatus.NeedMoreData));

        cursor.SupplyChunk("2}", true);
        AssertToken(scanner.ScanNext(), AsnTokenKind.TupleLiteral, "{1,2}", 14, 19);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(LiteralScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static LiteralScanner CreateScanner(
        string sourceName,
        string text,
        bool isFinal,
        DiagnosticBag diagnostics)
    {
        SourceCursor cursor = new SourceCursor(sourceName);
        cursor.SupplyChunk(text, isFinal);
        return new LiteralScanner(new TriviaScanner(cursor, diagnostics), diagnostics);
    }

    private static void SkipTrivia(LiteralScanner scanner)
    {
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(LiteralScannerStatus.Trivia));
    }

    private static void AssertToken(
        LiteralScannerResult result,
        AsnTokenKind kind,
        string text,
        int startOffset,
        int endOffset)
    {
        Assert.That(result.Status, Is.EqualTo(LiteralScannerStatus.Token));
        Assert.That(result.Token?.Kind, Is.EqualTo(kind));
        Assert.That(result.Token?.Text, Is.EqualTo(text));
        Assert.That(result.Token?.Span?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Token?.Span?.End.Offset, Is.EqualTo(endOffset));
    }

    private static void AssertNonLiteral(
        LiteralScannerResult result,
        int scalarValue,
        int startOffset,
        int endOffset)
    {
        Assert.That(result.Status, Is.EqualTo(LiteralScannerStatus.NonLiteral));
        Assert.That(result.Input?.ScalarValue, Is.EqualTo(scalarValue));
        Assert.That(result.Input?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Input?.End.Offset, Is.EqualTo(endOffset));
    }
}
