using asn1;

namespace asn1.Tests;

public class TriviaScannerTests
{
    [Test]
    public void ScannerRetainsEveryX680WhitespaceCharacterAndExactPositions()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner scanner = CreateScanner("whitespace.asn", "\t \u00A0\r\n\v\fX", true, diagnostics);

        TriviaScannerResult trivia = scanner.ScanNext();
        TriviaScannerResult x = scanner.ScanNext();

        AssertTrivia(trivia, AsnTokenKind.Whitespace, "\t \u00A0\r\n\v\f", 0, 7);
        Assert.That(trivia.Token?.Span?.Start.Line, Is.EqualTo(1));
        Assert.That(trivia.Token?.Span?.Start.Column, Is.EqualTo(1));
        Assert.That(trivia.Token?.Span?.End.Line, Is.EqualTo(4));
        Assert.That(trivia.Token?.Span?.End.Column, Is.EqualTo(1));
        AssertNonTrivia(x, 'X', "X", 7, 8);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void HyphenCommentsEndAtTheNextPairOrNewlineAndPreserveAdjacency()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner scanner = CreateScanner("hyphen.asn", "A--one--B--two\r\nC", true, diagnostics);

        AssertNonTrivia(scanner.ScanNext(), 'A', "A", 0, 1);
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Comment, "--one--", 1, 8);
        AssertNonTrivia(scanner.ScanNext(), 'B', "B", 8, 9);
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Comment, "--two", 9, 14);
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Whitespace, "\r\n", 14, 16);
        AssertNonTrivia(scanner.ScanNext(), 'C', "C", 16, 17);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(TriviaScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CommentMarkersAreInertInsideTheOtherCommentForm()
    {
        DiagnosticBag lineDiagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner lineScanner = CreateScanner("line.asn", "-- /* text */\nX", true, lineDiagnostics);

        AssertTrivia(lineScanner.ScanNext(), AsnTokenKind.Comment, "-- /* text */", 0, 13);
        AssertTrivia(lineScanner.ScanNext(), AsnTokenKind.Whitespace, "\n", 13, 14);
        AssertNonTrivia(lineScanner.ScanNext(), 'X', "X", 14, 15);

        DiagnosticBag blockDiagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner blockScanner = CreateScanner("block.asn", "/* -- text -- */X", true, blockDiagnostics);

        AssertTrivia(blockScanner.ScanNext(), AsnTokenKind.Comment, "/* -- text -- */", 0, 16);
        AssertNonTrivia(blockScanner.ScanNext(), 'X', "X", 16, 17);
        Assert.That(lineDiagnostics.GetDiagnostics(), Is.Empty);
        Assert.That(blockDiagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void MultipleLineCommentsNestAndRetainLineBreaks()
    {
        string text = "/* outer\n/* inner */ tail */X";
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner scanner = CreateScanner("nested.asn", text, true, diagnostics);

        TriviaScannerResult comment = scanner.ScanNext();
        TriviaScannerResult x = scanner.ScanNext();

        AssertTrivia(comment, AsnTokenKind.Comment, "/* outer\n/* inner */ tail */", 0, 28);
        Assert.That(comment.Token?.Span?.End.Line, Is.EqualTo(2));
        Assert.That(comment.Token?.Span?.End.Column, Is.EqualTo(20));
        AssertNonTrivia(x, 'X', "X", 28, 29);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void TriviaScanningResumesAcrossWhitespaceAndCommentDelimiterBoundaries()
    {
        SourceCursor cursor = new SourceCursor("chunks.asn");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner scanner = new TriviaScanner(cursor, diagnostics);

        cursor.SupplyChunk(" \t-", false);
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Whitespace, " \t", 0, 2);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(TriviaScannerStatus.NeedMoreData));

        cursor.SupplyChunk("-a-", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(TriviaScannerStatus.NeedMoreData));

        cursor.SupplyChunk("-/*x", false);
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Comment, "--a--", 2, 7);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(TriviaScannerStatus.NeedMoreData));

        cursor.SupplyChunk("/*y*/z*/Q", true);
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Comment, "/*x/*y*/z*/", 7, 18);
        AssertNonTrivia(scanner.ScanNext(), 'Q', "Q", 18, 19);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(TriviaScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void WhitespaceRunRemainsOpenAcrossAChunkBoundary()
    {
        SourceCursor cursor = new SourceCursor("whitespace-chunks.asn");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner scanner = new TriviaScanner(cursor, diagnostics);

        cursor.SupplyChunk(" \t", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(TriviaScannerStatus.NeedMoreData));

        cursor.SupplyChunk("\u00A0A", true);
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Whitespace, " \t\u00A0", 0, 3);
        AssertNonTrivia(scanner.ScanNext(), 'A', "A", 3, 4);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(TriviaScannerStatus.EndOfInput));
    }

    [Test]
    public void UnterminatedMultipleLineCommentIsRetainedAndDiagnosedOnce()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner scanner = CreateScanner("unterminated.asn", "/* open", true, diagnostics);

        TriviaScannerResult comment = scanner.ScanNext();
        TriviaScannerResult end = scanner.ScanNext();
        AsnDiagnostic[] results = diagnostics.GetDiagnostics();

        AssertTrivia(comment, AsnTokenKind.Comment, "/* open", 0, 7);
        Assert.That(end.Status, Is.EqualTo(TriviaScannerStatus.EndOfInput));
        Assert.That(results, Has.Length.EqualTo(1));
        Assert.That(results[0].Severity, Is.EqualTo(AsnDiagnosticSeverity.Error));
        Assert.That(results[0].Category, Is.EqualTo(AsnDiagnosticCategory.Lexical));
        Assert.That(results[0].Code, Is.EqualTo("ASNL0001"));
        Assert.That(results[0].Message, Is.EqualTo("Unterminated multiple-line comment."));
        Assert.That(results[0].Span?.Start.Offset, Is.Zero);
        Assert.That(results[0].Span?.End.Offset, Is.EqualTo(7));
    }

    [Test]
    public void NonCommentHyphensAndSlashesAreReturnedWithoutLosingLookahead()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        TriviaScanner scanner = CreateScanner("ordinary.asn", "/A-*B", true, diagnostics);

        AssertNonTrivia(scanner.ScanNext(), '/', "/", 0, 1);
        AssertNonTrivia(scanner.ScanNext(), 'A', "A", 1, 2);
        AssertNonTrivia(scanner.ScanNext(), '-', "-", 2, 3);
        AssertNonTrivia(scanner.ScanNext(), '*', "*", 3, 4);
        AssertNonTrivia(scanner.ScanNext(), 'B', "B", 4, 5);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(TriviaScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static TriviaScanner CreateScanner(string sourceName, string text, bool isFinal, DiagnosticBag diagnostics)
    {
        SourceCursor cursor = new SourceCursor(sourceName);
        cursor.SupplyChunk(text, isFinal);
        return new TriviaScanner(cursor, diagnostics);
    }

    private static void AssertTrivia(TriviaScannerResult result, AsnTokenKind kind, string text, int startOffset, int endOffset)
    {
        Assert.That(result.Status, Is.EqualTo(TriviaScannerStatus.Trivia));
        Assert.That(result.Token, Is.Not.Null);
        Assert.That(result.Token?.Kind, Is.EqualTo(kind));
        Assert.That(result.Token?.Text, Is.EqualTo(text));
        Assert.That(result.Token?.Span?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Token?.Span?.End.Offset, Is.EqualTo(endOffset));
    }

    private static void AssertNonTrivia(TriviaScannerResult result, int scalarValue, string text, int startOffset, int endOffset)
    {
        Assert.That(result.Status, Is.EqualTo(TriviaScannerStatus.NonTrivia));
        Assert.That(result.Input, Is.Not.Null);
        Assert.That(result.Input?.ScalarValue, Is.EqualTo(scalarValue));
        Assert.That(result.Input?.Text, Is.EqualTo(text));
        Assert.That(result.Input?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Input?.End.Offset, Is.EqualTo(endOffset));
    }
}
