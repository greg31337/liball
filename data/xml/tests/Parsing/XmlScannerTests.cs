using xml;

namespace xml.Tests;

[TestFixture]
public class XmlScannerTests
{
    [Test]
    public void ScannerTokenizesCompleteDocumentAndPreservesEveryLexeme()
    {
        string text = "<?xml version='1.0'?><root a='v&amp;&#65;'>text&amp;&#x1F600;<!--c--><![CDATA[x<y]]><?pi data?></root>";
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        List<XmlToken> tokens = ScanAllTokens(scanner);

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new XmlTokenKind[]
        {
            XmlTokenKind.XmlDeclaration,
            XmlTokenKind.StartTagOpen,
            XmlTokenKind.Name,
            XmlTokenKind.Whitespace,
            XmlTokenKind.Name,
            XmlTokenKind.Equals,
            XmlTokenKind.QuotedValue,
            XmlTokenKind.TagClose,
            XmlTokenKind.Text,
            XmlTokenKind.EntityReference,
            XmlTokenKind.CharacterReference,
            XmlTokenKind.Comment,
            XmlTokenKind.CData,
            XmlTokenKind.ProcessingInstruction,
            XmlTokenKind.EndTagOpen,
            XmlTokenKind.Name,
            XmlTokenKind.TagClose,
            XmlTokenKind.EndOfInput,
        }));
        Assert.That(string.Concat(tokens.Where(token => token.Kind != XmlTokenKind.EndOfInput).Select(token => token.Lexeme)), Is.EqualTo(text));
        AssertSequentialOffsets(tokens);

        Assert.That(tokens[0].Declaration, Is.Not.Null);
        Assert.That(tokens[0].Declaration!.Version, Is.EqualTo(XmlVersion.Xml10));
        Assert.That(tokens[2].Name, Is.EqualTo("root"));
        Assert.That(tokens[6].Value, Is.EqualTo("v&amp;&#65;"));
        Assert.That(tokens[6].Quote, Is.EqualTo('\''));
        Assert.That(tokens[9].Name, Is.EqualTo("amp"));
        Assert.That(tokens[10].ScalarValue, Is.EqualTo(0x1F600));
        Assert.That(tokens[10].Value, Is.EqualTo("\U0001F600"));
        Assert.That(tokens[11].Value, Is.EqualTo("c"));
        Assert.That(tokens[12].Value, Is.EqualTo("x<y"));
        Assert.That(tokens[13].Name, Is.EqualTo("pi"));
        Assert.That(tokens[13].Value, Is.EqualTo("data"));
        Assert.That(tokens[13].HasNameValueSeparator, Is.True);
    }

    [Test]
    public void ScannerTokenizesEmptyElementsAndMarkupWhitespace()
    {
        XmlScanner scanner = new XmlScanner("<p:root a = \"value\" />".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        List<XmlToken> tokens = ScanAllTokens(scanner);

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new XmlTokenKind[]
        {
            XmlTokenKind.StartTagOpen,
            XmlTokenKind.Name,
            XmlTokenKind.Whitespace,
            XmlTokenKind.Name,
            XmlTokenKind.Whitespace,
            XmlTokenKind.Equals,
            XmlTokenKind.Whitespace,
            XmlTokenKind.QuotedValue,
            XmlTokenKind.Whitespace,
            XmlTokenKind.EmptyElementClose,
            XmlTokenKind.EndOfInput,
        }));
        Assert.That(tokens[1].Name, Is.EqualTo("p:root"));
        Assert.That(tokens[7].Value, Is.EqualTo("value"));
        Assert.That(tokens[7].Quote, Is.EqualTo('"'));
    }

    [Test]
    public void ScannerResumesThroughDoctypeInternalSubsetTokens()
    {
        string text = "<!DOCTYPE root [<!ELEMENT root (#PCDATA)><!--ok--><?inside value?>]><root/>";
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        List<XmlToken> tokens = ScanAllTokens(scanner);

        Assert.That(tokens[0].Kind, Is.EqualTo(XmlTokenKind.MarkupDeclarationOpen));
        Assert.That(tokens.Any(token => token.Kind == XmlTokenKind.Comment && token.Value == "ok"), Is.True);
        Assert.That(tokens.Any(token => token.Kind == XmlTokenKind.ProcessingInstruction && token.Name == "inside" && token.Value == "value"), Is.True);
        Assert.That(tokens.Count(token => token.Kind == XmlTokenKind.MarkupDeclarationOpen), Is.EqualTo(1));
        Assert.That(tokens.Count(token => token.Kind == XmlTokenKind.TagClose), Is.EqualTo(1));
        Assert.That(tokens.Any(token => token.Kind == XmlTokenKind.EmptyElementClose), Is.True);
        Assert.That(string.Concat(tokens.Where(token => token.Kind != XmlTokenKind.EndOfInput).Select(token => token.Lexeme)), Is.EqualTo(text));
        Assert.That(scanner.CurrentXmlScannerState(), Is.EqualTo(XmlScannerState.EndOfInput));
    }

    [Test]
    public void Xml11DeclarationChangesFollowingCharacterAndReferenceRules()
    {
        XmlScanner scanner = new XmlScanner("<?xml version='1.1'?><r>&#x1;\u0001</r>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        Assert.That(scanner.ScanNextXmlToken().Kind, Is.EqualTo(XmlTokenKind.XmlDeclaration));
        Assert.That(scanner.CurrentXmlScannerVersion(), Is.EqualTo(XmlVersion.Xml11));
        Assert.That(scanner.ScanNextXmlToken().Kind, Is.EqualTo(XmlTokenKind.StartTagOpen));
        Assert.That(scanner.ScanNextXmlToken().Kind, Is.EqualTo(XmlTokenKind.Name));
        Assert.That(scanner.ScanNextXmlToken().Kind, Is.EqualTo(XmlTokenKind.TagClose));
        XmlToken reference = scanner.ScanNextXmlToken();
        Assert.That(reference.Kind, Is.EqualTo(XmlTokenKind.CharacterReference));
        Assert.That(reference.ScalarValue, Is.EqualTo(1));
        Assert.That(reference.RestrictedCharacter, Is.True);
        Assert.Throws<FormatException>(() => scanner.ScanNextXmlToken());
    }

    [Test]
    public void ExternalEntityDeclarationProducesTextDeclarationToken()
    {
        XmlScanner scanner = new XmlScanner("<?xml version='1.1' encoding='UTF-8'?>entity".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.ExternalParsedEntity);

        XmlToken declaration = scanner.ScanNextXmlToken();
        XmlToken content = scanner.ScanNextXmlToken();

        Assert.That(declaration.Kind, Is.EqualTo(XmlTokenKind.TextDeclaration));
        Assert.That(declaration.Declaration, Is.Not.Null);
        Assert.That(declaration.Declaration!.IsTextDeclaration, Is.True);
        Assert.That(declaration.Declaration.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(scanner.CurrentXmlScannerVersion(), Is.EqualTo(XmlVersion.Xml11));
        Assert.That(content.Kind, Is.EqualTo(XmlTokenKind.Text));
        Assert.That(content.Value, Is.EqualTo("entity"));
    }

    [Test]
    public void ScannerResetReplaysTokensAndEndOfInputIsStable()
    {
        XmlScanner scanner = new XmlScanner("<n/>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        List<XmlToken> first = ScanAllTokens(scanner);

        XmlToken repeatedEnd = scanner.ScanNextXmlToken();
        Assert.That(repeatedEnd.Kind, Is.EqualTo(XmlTokenKind.EndOfInput));
        Assert.That(repeatedEnd.StartUtf16Offset, Is.EqualTo(4));

        scanner.ResetXmlScanner();
        List<XmlToken> second = ScanAllTokens(scanner);

        Assert.That(second.Select(token => token.Kind), Is.EqualTo(first.Select(token => token.Kind)));
        Assert.That(second.Select(token => token.Lexeme), Is.EqualTo(first.Select(token => token.Lexeme)));
        Assert.That(scanner.CurrentXmlScannerOffset(), Is.EqualTo(4));
    }

    [Test]
    public void ScannerKeepsReferenceBoundariesSeparateFromText()
    {
        XmlScanner scanner = new XmlScanner("left&amp;middle&#65;right".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        List<XmlToken> tokens = ScanAllTokens(scanner);

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new XmlTokenKind[]
        {
            XmlTokenKind.Text,
            XmlTokenKind.EntityReference,
            XmlTokenKind.Text,
            XmlTokenKind.CharacterReference,
            XmlTokenKind.Text,
            XmlTokenKind.EndOfInput,
        }));
        Assert.That(tokens.Select(token => token.Value), Is.EqualTo(new string?[] { "left", null, "middle", "A", "right", null }));
    }

    [TestCase("&name")]
    [TestCase("<!--unterminated")]
    [TestCase("<![CDATA[unterminated")]
    [TestCase("<?pi unterminated")]
    [TestCase("<?xml version='1.0'")]
    [TestCase("<root")]
    [TestCase("<root a='unterminated")]
    [TestCase("<root @>")]
    [TestCase("<!DOCTYPE root [<!ELEMENT root EMPTY>")]
    public void ScannerRejectsIncompleteOrUnexpectedTokenSequences(string text)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        Assert.Throws<FormatException>(() => ScanAllTokens(scanner));
    }

    [Test]
    public void ScannerRejectsMisplacedDeclarationAndIllegalDirectCharacters()
    {
        XmlScanner misplaced = new XmlScanner("x<?xml version='1.0'?>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        Assert.That(misplaced.ScanNextXmlToken().Value, Is.EqualTo("x"));
        Assert.Throws<FormatException>(() => misplaced.ScanNextXmlToken());

        XmlScanner xml10 = new XmlScanner("A\0B".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        Assert.Throws<FormatException>(() => xml10.ScanNextXmlToken());

        XmlScanner xml11 = new XmlScanner("A\u007FB".AsMemory(), XmlVersion.Xml11, XmlEncodingEntityKind.Document);
        Assert.Throws<FormatException>(() => xml11.ScanNextXmlToken());
    }

    [Test]
    public void ConstructorRejectsInvalidVersionAndEntityKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlScanner(ReadOnlyMemory<char>.Empty, (XmlVersion)2, XmlEncodingEntityKind.Document));
        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlScanner(ReadOnlyMemory<char>.Empty, XmlVersion.Xml10, (XmlEncodingEntityKind)2));
    }

    private static List<XmlToken> ScanAllTokens(XmlScanner scanner)
    {
        List<XmlToken> tokens = [];
        while (true)
        {
            XmlToken token = scanner.ScanNextXmlToken();
            tokens.Add(token);
            if (token.Kind == XmlTokenKind.EndOfInput)
            {
                return tokens;
            }
        }
    }

    private static void AssertSequentialOffsets(List<XmlToken> tokens)
    {
        int offset = 0;
        foreach (XmlToken token in tokens)
        {
            Assert.That(token.StartUtf16Offset, Is.EqualTo(offset), token.Kind.ToString());
            Assert.That(token.Utf16Length, Is.EqualTo(token.Lexeme.Length), token.Kind.ToString());
            offset = offset + token.Utf16Length;
        }
    }
}
