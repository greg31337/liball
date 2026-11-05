using asn1;

namespace asn1.Tests;

public class PunctuationScannerTests
{
    [TestCase("{", AsnTokenKind.LeftBrace)]
    [TestCase("}", AsnTokenKind.RightBrace)]
    [TestCase("(", AsnTokenKind.LeftParenthesis)]
    [TestCase(")", AsnTokenKind.RightParenthesis)]
    [TestCase("[", AsnTokenKind.LeftBracket)]
    [TestCase("]", AsnTokenKind.RightBracket)]
    [TestCase("<", AsnTokenKind.LessThan)]
    [TestCase(">", AsnTokenKind.GreaterThan)]
    [TestCase(",", AsnTokenKind.Comma)]
    [TestCase(";", AsnTokenKind.Semicolon)]
    [TestCase(":", AsnTokenKind.Colon)]
    [TestCase(".", AsnTokenKind.Dot)]
    [TestCase("!", AsnTokenKind.ExclamationMark)]
    [TestCase("@", AsnTokenKind.AtSign)]
    [TestCase("|", AsnTokenKind.VerticalBar)]
    [TestCase("^", AsnTokenKind.Caret)]
    [TestCase("&", AsnTokenKind.Ampersand)]
    [TestCase("+", AsnTokenKind.Plus)]
    [TestCase("-", AsnTokenKind.Minus)]
    [TestCase("*", AsnTokenKind.Asterisk)]
    [TestCase("/", AsnTokenKind.Slash)]
    [TestCase("=", AsnTokenKind.Equals)]
    public void ScannerRecognizesEverySingleCharacterPunctuation(string text, AsnTokenKind expectedKind)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        PunctuationScanner scanner = CreateScanner("single.asn", text, true, diagnostics);

        AssertToken(scanner.ScanNext(), expectedKind, text, 0, 1);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerUsesLongestMatchesForAssignmentDotsAndVersionBrackets()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        PunctuationScanner scanner = CreateScanner("compound.asn", "::= ... .. . [[ ]]", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Assignment, "::=", 0, 3);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Ellipsis, "...", 4, 7);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Range, "..", 8, 10);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Dot, ".", 11, 12);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.LeftVersionBracket, "[[", 13, 15);
        SkipTrivia(scanner);
        AssertToken(scanner.ScanNext(), AsnTokenKind.RightVersionBracket, "]]", 16, 18);
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AdjacentAmbiguousSequencesArePartitionedDeterministically()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        PunctuationScanner scanner = CreateScanner("adjacent.asn", ".... ..... :::= [[[ ]]]", true, diagnostics);

        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Ellipsis, "...");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Dot, ".");
        SkipTrivia(scanner);
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Ellipsis, "...");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Range, "..");
        SkipTrivia(scanner);
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Colon, ":");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Assignment, "::=");
        SkipTrivia(scanner);
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.LeftVersionBracket, "[[");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.LeftBracket, "[");
        SkipTrivia(scanner);
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.RightVersionBracket, "]]");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.RightBracket, "]");
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void AtNotationAndEncodingPrefixSymbolsRemainSeparateLexicalItems()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        PunctuationScanner scanner = CreateScanner("contexts.asn", "[XER:Type] @.component", true, diagnostics);

        AssertTokenText(scanner.ScanNext(), AsnTokenKind.LeftBracket, "[");
        AssertNonPunctuation(scanner.ScanNext(), 'X');
        AssertNonPunctuation(scanner.ScanNext(), 'E');
        AssertNonPunctuation(scanner.ScanNext(), 'R');
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Colon, ":");
        AssertNonPunctuation(scanner.ScanNext(), 'T');
        AssertNonPunctuation(scanner.ScanNext(), 'y');
        AssertNonPunctuation(scanner.ScanNext(), 'p');
        AssertNonPunctuation(scanner.ScanNext(), 'e');
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.RightBracket, "]");
        SkipTrivia(scanner);
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.AtSign, "@");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Dot, ".");
        AssertNonPunctuation(scanner.ScanNext(), 'c');
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CommentsTakePrecedenceOverSlashAndHyphenPunctuation()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        PunctuationScanner scanner = CreateScanner("comments.asn", "--one--/ /*two*/-", true, diagnostics);

        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Comment, "--one--");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Slash, "/");
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Whitespace, " ");
        AssertTrivia(scanner.ScanNext(), AsnTokenKind.Comment, "/*two*/");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Minus, "-");
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompoundPunctuationResumesAtEveryLookaheadBoundary()
    {
        SourceCursor cursor = new SourceCursor("chunks.asn");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        PunctuationScanner scanner = new PunctuationScanner(new TriviaScanner(cursor, diagnostics), diagnostics);

        cursor.SupplyChunk(":", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.NeedMoreData));
        cursor.SupplyChunk(":", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.NeedMoreData));
        cursor.SupplyChunk("=.", false);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Assignment, "::=", 0, 3);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.NeedMoreData));
        cursor.SupplyChunk(".", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.NeedMoreData));
        cursor.SupplyChunk(".[", false);
        AssertToken(scanner.ScanNext(), AsnTokenKind.Ellipsis, "...", 3, 6);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.NeedMoreData));
        cursor.SupplyChunk("[", true);
        AssertToken(scanner.ScanNext(), AsnTokenKind.LeftVersionBracket, "[[", 6, 8);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerPassesOtherTokenStartsAndDiagnosesUnsupportedCharacters()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        PunctuationScanner scanner = CreateScanner("invalid.asn", "A1\"' _\u2011", true, diagnostics);

        AssertNonPunctuation(scanner.ScanNext(), 'A');
        AssertNonPunctuation(scanner.ScanNext(), '1');
        AssertNonPunctuation(scanner.ScanNext(), '"');
        AssertNonPunctuation(scanner.ScanNext(), '\'');
        SkipTrivia(scanner);
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Invalid, "_");
        AssertTokenText(scanner.ScanNext(), AsnTokenKind.Invalid, "\u2011");

        AsnDiagnostic[] results = diagnostics.GetDiagnostics();
        Assert.That(results, Has.Length.EqualTo(2));
        Assert.That(results[0].Code, Is.EqualTo("ASNL0014"));
        Assert.That(results[0].Span?.Start.Offset, Is.EqualTo(5));
        Assert.That(results[1].Code, Is.EqualTo("ASNL0014"));
        Assert.That(results[1].Span?.Start.Offset, Is.EqualTo(6));
    }

    private static PunctuationScanner CreateScanner(
        string sourceName,
        string text,
        bool isFinal,
        DiagnosticBag diagnostics)
    {
        SourceCursor cursor = new SourceCursor(sourceName);
        cursor.SupplyChunk(text, isFinal);
        return new PunctuationScanner(new TriviaScanner(cursor, diagnostics), diagnostics);
    }

    private static void SkipTrivia(PunctuationScanner scanner)
    {
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(PunctuationScannerStatus.Trivia));
    }

    private static void AssertToken(
        PunctuationScannerResult result,
        AsnTokenKind kind,
        string text,
        int startOffset,
        int endOffset)
    {
        AssertTokenText(result, kind, text);
        Assert.That(result.Token?.Span?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Token?.Span?.End.Offset, Is.EqualTo(endOffset));
    }

    private static void AssertTokenText(
        PunctuationScannerResult result,
        AsnTokenKind kind,
        string text)
    {
        Assert.That(result.Status, Is.EqualTo(PunctuationScannerStatus.Token));
        Assert.That(result.Token?.Kind, Is.EqualTo(kind));
        Assert.That(result.Token?.Text, Is.EqualTo(text));
    }

    private static void AssertTrivia(
        PunctuationScannerResult result,
        AsnTokenKind kind,
        string text)
    {
        Assert.That(result.Status, Is.EqualTo(PunctuationScannerStatus.Trivia));
        Assert.That(result.Token?.Kind, Is.EqualTo(kind));
        Assert.That(result.Token?.Text, Is.EqualTo(text));
    }

    private static void AssertNonPunctuation(PunctuationScannerResult result, int scalarValue)
    {
        Assert.That(result.Status, Is.EqualTo(PunctuationScannerStatus.NonPunctuation));
        Assert.That(result.Input?.ScalarValue, Is.EqualTo(scalarValue));
    }
}
