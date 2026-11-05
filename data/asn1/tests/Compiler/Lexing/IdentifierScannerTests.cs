using asn1;

namespace asn1.Tests;

public class IdentifierScannerTests
{
    [Test]
    public void ScannerRecognizesReservedWordsWithExactCaseAndEquivalentHyphens()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IdentifierScanner scanner = CreateScanner(
            "reserved.asn",
            "BEGIN begin ABSTRACT\u2011SYNTAX GeneralizedTime generalizedTime",
            true,
            diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.ReservedWord, "BEGIN", 0, 5);
        AssertTrivia(scanner.ScanNext(), " ");
        AssertToken(scanner.ScanNext(), AsnTokenKind.Identifier, "begin", 6, 11);
        AssertTrivia(scanner.ScanNext(), " ");
        AssertToken(scanner.ScanNext(), AsnTokenKind.ReservedWord, "ABSTRACT\u2011SYNTAX", 12, 27);
        AssertTrivia(scanner.ScanNext(), " ");
        AssertToken(scanner.ScanNext(), AsnTokenKind.ReservedWord, "GeneralizedTime", 28, 43);
        AssertTrivia(scanner.ScanNext(), " ");
        AssertToken(scanner.ScanNext(), AsnTokenKind.Identifier, "generalizedTime", 44, 59);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(IdentifierScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerReportsEveryContextuallyValidReferenceCategory()
    {
        AssertResolvedKind("TypeName", AsnTokenKind.TypeReference);
        AssertResolvedKind("valueName", AsnTokenKind.ValueReference);
        AssertResolvedKind("ModuleName", AsnTokenKind.ModuleReference);
        AssertResolvedKind("OBJECT-CLASS1", AsnTokenKind.ObjectClassReference);
        AssertResolvedKind("objectName", AsnTokenKind.ObjectReference);
        AssertResolvedKind("ObjectSet", AsnTokenKind.ObjectSetReference);
        AssertResolvedKind("&Type", AsnTokenKind.TypeFieldReference);
        AssertResolvedKind("&value", AsnTokenKind.ValueFieldReference);
        AssertResolvedKind("&ValueSet", AsnTokenKind.ValueSetFieldReference);
        AssertResolvedKind("&object", AsnTokenKind.ObjectFieldReference);
        AssertResolvedKind("&ObjectSet", AsnTokenKind.ObjectSetFieldReference);
        AssertResolvedKind("XER", AsnTokenKind.EncodingReference);
        AssertResolvedKind("identifier", AsnTokenKind.Identifier);
    }

    [Test]
    public void DefaultKindsAndCandidatesRetainLexicalAmbiguity()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IdentifierScanner scanner = CreateScanner(
            "categories.asn",
            "lower MixedName UPPER XER &lower &Upper",
            true,
            diagnostics);

        IdentifierScannerResult lower = NextToken(scanner);
        Assert.That(lower.Token?.Kind, Is.EqualTo(AsnTokenKind.Identifier));
        Assert.That(lower.CandidateKinds, Is.EqualTo(new[]
        {
            AsnTokenKind.Identifier,
            AsnTokenKind.ValueReference,
            AsnTokenKind.ObjectReference
        }));

        SkipTrivia(scanner);
        IdentifierScannerResult mixed = NextToken(scanner);
        Assert.That(mixed.Token?.Kind, Is.EqualTo(AsnTokenKind.TypeReference));
        Assert.That(mixed.CandidateKinds, Is.EqualTo(new[]
        {
            AsnTokenKind.TypeReference,
            AsnTokenKind.ModuleReference,
            AsnTokenKind.ObjectSetReference
        }));

        SkipTrivia(scanner);
        IdentifierScannerResult upper = NextToken(scanner);
        Assert.That(upper.Token?.Kind, Is.EqualTo(AsnTokenKind.ObjectClassReference));
        Assert.That(upper.CanBeKind(AsnTokenKind.ModuleReference), Is.True);
        Assert.That(upper.CanBeKind(AsnTokenKind.EncodingReference), Is.False);

        SkipTrivia(scanner);
        IdentifierScannerResult encoding = NextToken(scanner);
        Assert.That(encoding.Token?.Kind, Is.EqualTo(AsnTokenKind.ObjectClassReference));
        Assert.That(encoding.CanBeKind(AsnTokenKind.EncodingReference), Is.True);

        SkipTrivia(scanner);
        IdentifierScannerResult lowerField = NextToken(scanner);
        Assert.That(lowerField.Token?.Kind, Is.EqualTo(AsnTokenKind.ValueFieldReference));
        Assert.That(lowerField.CandidateKinds, Is.EqualTo(new[]
        {
            AsnTokenKind.ValueFieldReference,
            AsnTokenKind.ObjectFieldReference
        }));

        SkipTrivia(scanner);
        IdentifierScannerResult upperField = NextToken(scanner);
        Assert.That(upperField.Token?.Kind, Is.EqualTo(AsnTokenKind.TypeFieldReference));
        Assert.That(upperField.CandidateKinds, Is.EqualTo(new[]
        {
            AsnTokenKind.TypeFieldReference,
            AsnTokenKind.ValueSetFieldReference,
            AsnTokenKind.ObjectSetFieldReference
        }));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerDiagnosesTrailingAndAdjacentEquivalentHyphens()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IdentifierScanner scanner = CreateScanner("invalid-names.asn", "bad- Bad-\u2011Name", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "bad-", 0, 4);
        AssertTrivia(scanner.ScanNext(), " ");
        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "Bad-\u2011Name", 5, 14);

        AsnDiagnostic[] results = diagnostics.GetDiagnostics();
        Assert.That(results, Has.Length.EqualTo(2));
        Assert.That(results[0].Code, Is.EqualTo("ASNL0003"));
        Assert.That(results[0].Message, Is.EqualTo("A reference or identifier cannot end with a hyphen."));
        Assert.That(results[0].Span?.Start.Offset, Is.Zero);
        Assert.That(results[0].Span?.End.Offset, Is.EqualTo(4));
        Assert.That(results[1].Code, Is.EqualTo("ASNL0002"));
        Assert.That(results[1].Message, Is.EqualTo("A reference or identifier cannot contain adjacent hyphens."));
        Assert.That(results[1].Span?.Start.Offset, Is.EqualTo(5));
        Assert.That(results[1].Span?.End.Offset, Is.EqualTo(14));
    }

    [Test]
    public void ScannerPreservesCommentsTriviaAndNonIdentifierLookahead()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IdentifierScanner scanner = CreateScanner("lookahead.asn", "value--note-- Type & 9", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Identifier, "value", 0, 5);
        IdentifierScannerResult comment = scanner.ScanNext();
        Assert.That(comment.Status, Is.EqualTo(IdentifierScannerStatus.Trivia));
        Assert.That(comment.Token?.Kind, Is.EqualTo(AsnTokenKind.Comment));
        Assert.That(comment.Token?.Text, Is.EqualTo("--note--"));
        AssertTrivia(scanner.ScanNext(), " ");
        AssertToken(scanner.ScanNext(), AsnTokenKind.TypeReference, "Type", 14, 18);
        AssertTrivia(scanner.ScanNext(), " ");
        AssertNonIdentifier(scanner.ScanNext(), '&', 19, 20);
        AssertTrivia(scanner.ScanNext(), " ");
        AssertNonIdentifier(scanner.ScanNext(), '9', 21, 22);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(IdentifierScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void FieldReferenceResumesAcrossAmpersandNameAndDelimiterChunks()
    {
        SourceCursor cursor = new SourceCursor("chunks.asn");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IdentifierScanner scanner = new IdentifierScanner(new TriviaScanner(cursor, diagnostics), diagnostics);

        cursor.SupplyChunk("&", false);
        Assert.That(
            scanner.ScanNextForKind(AsnTokenKind.ValueSetFieldReference).Status,
            Is.EqualTo(IdentifierScannerStatus.NeedMoreData));

        cursor.SupplyChunk("Value", false);
        Assert.That(
            scanner.ScanNextForKind(AsnTokenKind.ValueSetFieldReference).Status,
            Is.EqualTo(IdentifierScannerStatus.NeedMoreData));

        cursor.SupplyChunk("Set", false);
        Assert.That(
            scanner.ScanNextForKind(AsnTokenKind.ValueSetFieldReference).Status,
            Is.EqualTo(IdentifierScannerStatus.NeedMoreData));

        cursor.SupplyChunk(" X", true);
        IdentifierScannerResult field = scanner.ScanNextForKind(AsnTokenKind.ValueSetFieldReference);
        AssertToken(field, AsnTokenKind.ValueSetFieldReference, "&ValueSet", 0, 9);
        AssertTrivia(scanner.ScanNext(), " ");
        AssertToken(scanner.ScanNext(), AsnTokenKind.ObjectClassReference, "X", 10, 11);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(IdentifierScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static IdentifierScanner CreateScanner(
        string sourceName,
        string text,
        bool isFinal,
        DiagnosticBag diagnostics)
    {
        SourceCursor cursor = new SourceCursor(sourceName);
        cursor.SupplyChunk(text, isFinal);
        return new IdentifierScanner(new TriviaScanner(cursor, diagnostics), diagnostics);
    }

    private static void AssertResolvedKind(string text, AsnTokenKind requestedKind)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IdentifierScanner scanner = CreateScanner("resolved.asn", text, true, diagnostics);

        IdentifierScannerResult result = scanner.ScanNextForKind(requestedKind);

        AssertToken(result, requestedKind, text, 0, text.Length);
        Assert.That(result.CanBeKind(requestedKind), Is.True);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(IdentifierScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    private static IdentifierScannerResult NextToken(IdentifierScanner scanner)
    {
        IdentifierScannerResult result = scanner.ScanNext();
        Assert.That(result.Status, Is.EqualTo(IdentifierScannerStatus.Token));
        Assert.That(result.Token, Is.Not.Null);
        return result;
    }

    private static void SkipTrivia(IdentifierScanner scanner)
    {
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(IdentifierScannerStatus.Trivia));
    }

    private static void AssertToken(
        IdentifierScannerResult result,
        AsnTokenKind kind,
        string text,
        int startOffset,
        int endOffset)
    {
        Assert.That(result.Status, Is.EqualTo(IdentifierScannerStatus.Token));
        Assert.That(result.Token, Is.Not.Null);
        Assert.That(result.Token?.Kind, Is.EqualTo(kind));
        Assert.That(result.Token?.Text, Is.EqualTo(text));
        Assert.That(result.Token?.Span?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Token?.Span?.End.Offset, Is.EqualTo(endOffset));
    }

    private static void AssertTrivia(IdentifierScannerResult result, string text)
    {
        Assert.That(result.Status, Is.EqualTo(IdentifierScannerStatus.Trivia));
        Assert.That(result.Token?.Kind, Is.EqualTo(AsnTokenKind.Whitespace));
        Assert.That(result.Token?.Text, Is.EqualTo(text));
    }

    private static void AssertNonIdentifier(
        IdentifierScannerResult result,
        int scalarValue,
        int startOffset,
        int endOffset)
    {
        Assert.That(result.Status, Is.EqualTo(IdentifierScannerStatus.NonIdentifier));
        Assert.That(result.Input, Is.Not.Null);
        Assert.That(result.Input?.ScalarValue, Is.EqualTo(scalarValue));
        Assert.That(result.Input?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Input?.End.Offset, Is.EqualTo(endOffset));
    }
}
