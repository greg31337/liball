using asn1;

namespace asn1.Tests;

public class XmlValueScannerTests
{
    [Test]
    public void ScannerRecognizesSimpleNestedAndSelfClosingXmlValues()
    {
        DiagnosticBag simpleDiagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        XmlValueScanner simple = CreateScanner("simple-xml.asn", "<INTEGER>42</INTEGER>", true, simpleDiagnostics);
        AssertToken(simple.ScanNext(), AsnTokenKind.XmlValue, "<INTEGER>42</INTEGER>", 0, 21);
        Assert.That(simple.ScanNext().Status, Is.EqualTo(XmlValueScannerStatus.EndOfInput));
        Assert.That(simpleDiagnostics.GetDiagnostics(), Is.Empty);

        string nestedText = "<Root xmlns=\"urn:x\"><Child a='&amp;'/><Text>&#x41;</Text></Root>";
        DiagnosticBag nestedDiagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        XmlValueScanner nested = CreateScanner("nested-xml.asn", nestedText, true, nestedDiagnostics);
        AssertToken(nested.ScanNext(), AsnTokenKind.XmlValue, nestedText, 0, nestedText.Length);
        Assert.That(nestedDiagnostics.GetDiagnostics(), Is.Empty);

        DiagnosticBag emptyDiagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        XmlValueScanner empty = CreateScanner("empty-xml.asn", "<NULL/>", true, emptyDiagnostics);
        AssertToken(empty.ScanNext(), AsnTokenKind.XmlValue, "<NULL/>", 0, 7);
        Assert.That(emptyDiagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerRollsBackOrdinaryLessThanSyntaxWithoutLosingInput()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        XmlValueScanner scanner = CreateScanner("less-than.asn", "< Type", true, diagnostics);

        AssertNonXml(scanner.ScanNext(), '<', 0, 1);
        XmlValueScannerResult whitespace = scanner.ScanNext();
        Assert.That(whitespace.Status, Is.EqualTo(XmlValueScannerStatus.Trivia));
        Assert.That(whitespace.Token?.Text, Is.EqualTo(" "));
        AssertNonXml(scanner.ScanNext(), 'T', 2, 3);
        AssertNonXml(scanner.ScanNext(), 'y', 3, 4);
        AssertNonXml(scanner.ScanNext(), 'p', 4, 5);
        AssertNonXml(scanner.ScanNext(), 'e', 5, 6);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(XmlValueScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerDiagnosesInvalidXmlCharactersEscapesAndRawDelimiters()
    {
        string text = "<Text>a>b &bogus;\u0001</Text>";
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        XmlValueScanner scanner = CreateScanner("invalid-xml.asn", text, true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, text, 0, text.Length);
        Assert.That(diagnostics.GetDiagnostics(), Has.Length.EqualTo(1));
        Assert.That(diagnostics.GetDiagnostics()[0].Code, Is.EqualTo("ASNL0013"));
        Assert.That(diagnostics.GetDiagnostics()[0].Span?.Start.Offset, Is.Zero);
        Assert.That(diagnostics.GetDiagnostics()[0].Span?.End.Offset, Is.EqualTo(text.Length));
    }

    [Test]
    public void XmlValueScanningResumesAcrossTagsAttributesEntitiesAndText()
    {
        SourceCursor cursor = new SourceCursor("xml-chunks.asn");
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        XmlValueScanner scanner = new XmlValueScanner(new TriviaScanner(cursor, diagnostics), diagnostics);

        cursor.SupplyChunk("<Root a=\"x", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(XmlValueScannerStatus.NeedMoreData));
        cursor.SupplyChunk("\"><Child>&am", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(XmlValueScannerStatus.NeedMoreData));
        cursor.SupplyChunk("p;</Child></Ro", false);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(XmlValueScannerStatus.NeedMoreData));
        cursor.SupplyChunk("ot>Q", true);

        string expected = "<Root a=\"x\"><Child>&amp;</Child></Root>";
        AssertToken(scanner.ScanNext(), AsnTokenKind.XmlValue, expected, 0, expected.Length);
        AssertNonXml(scanner.ScanNext(), 'Q', expected.Length, expected.Length + 1);
        Assert.That(scanner.ScanNext().Status, Is.EqualTo(XmlValueScannerStatus.EndOfInput));
        Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ScannerReturnsAnInvalidTokenForCommittedUnterminatedXml()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        XmlValueScanner scanner = CreateScanner("unterminated-xml.asn", "<Root><Child/>", true, diagnostics);

        AssertToken(scanner.ScanNext(), AsnTokenKind.Invalid, "<Root><Child/>", 0, 14);
        Assert.That(diagnostics.GetDiagnostics().Select(result => result.Code), Does.Contain("ASNL0012"));
    }

    private static XmlValueScanner CreateScanner(
        string sourceName,
        string text,
        bool isFinal,
        DiagnosticBag diagnostics)
    {
        SourceCursor cursor = new SourceCursor(sourceName);
        cursor.SupplyChunk(text, isFinal);
        return new XmlValueScanner(new TriviaScanner(cursor, diagnostics), diagnostics);
    }

    private static void AssertToken(
        XmlValueScannerResult result,
        AsnTokenKind kind,
        string text,
        int startOffset,
        int endOffset)
    {
        Assert.That(result.Status, Is.EqualTo(XmlValueScannerStatus.Token));
        Assert.That(result.Token?.Kind, Is.EqualTo(kind));
        Assert.That(result.Token?.Text, Is.EqualTo(text));
        Assert.That(result.Token?.Span?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Token?.Span?.End.Offset, Is.EqualTo(endOffset));
    }

    private static void AssertNonXml(
        XmlValueScannerResult result,
        int scalarValue,
        int startOffset,
        int endOffset)
    {
        Assert.That(result.Status, Is.EqualTo(XmlValueScannerStatus.NonXmlValue));
        Assert.That(result.Input?.ScalarValue, Is.EqualTo(scalarValue));
        Assert.That(result.Input?.Start.Offset, Is.EqualTo(startOffset));
        Assert.That(result.Input?.End.Offset, Is.EqualTo(endOffset));
    }
}
