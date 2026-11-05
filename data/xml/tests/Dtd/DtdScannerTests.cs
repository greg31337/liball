using xml;

namespace xml.Tests;

[TestFixture]
public class DtdScannerTests
{
    [Test]
    public void TokenizesMarkupDeclarationsNamesNmtokensAndSymbols()
    {
        string text = "<!ELEMENT root (child|#PCDATA)*>\n<!ATTLIST root id ID #REQUIRED> .token 12";
        DtdScanner scanner = new DtdScanner(text.AsMemory(), XmlVersion.Xml10);

        List<DtdToken> tokens = ScanAllTokens(scanner);

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new DtdTokenKind[]
        {
            DtdTokenKind.MarkupDeclarationOpen,
            DtdTokenKind.Name,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Name,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Symbol,
            DtdTokenKind.Name,
            DtdTokenKind.Symbol,
            DtdTokenKind.Symbol,
            DtdTokenKind.Name,
            DtdTokenKind.Symbol,
            DtdTokenKind.Symbol,
            DtdTokenKind.DeclarationClose,
            DtdTokenKind.Whitespace,
            DtdTokenKind.MarkupDeclarationOpen,
            DtdTokenKind.Name,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Name,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Name,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Name,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Symbol,
            DtdTokenKind.Name,
            DtdTokenKind.DeclarationClose,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Nmtoken,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Nmtoken,
            DtdTokenKind.EndOfInput,
        }));
        Assert.That(tokens[1].Name, Is.EqualTo("ELEMENT"));
        Assert.That(tokens[23].Value, Is.EqualTo("#"));
        Assert.That(tokens[27].Value, Is.EqualTo(".token"));
        Assert.That(tokens[29].Value, Is.EqualTo("12"));
        Assert.That(string.Concat(tokens.Where(token => token.Kind != DtdTokenKind.EndOfInput).Select(token => token.Lexeme)), Is.EqualTo(text));
        AssertSequentialOffsets(tokens);
    }

    [Test]
    public void SeparatesWhitespaceAndParameterEntityReferences()
    {
        string text = " \n%shared;\t<!ENTITY % local 'value'>";
        List<DtdToken> tokens = ScanAllTokens(new DtdScanner(text.AsMemory(), XmlVersion.Xml10));

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new DtdTokenKind[]
        {
            DtdTokenKind.Whitespace,
            DtdTokenKind.ParameterEntityReference,
            DtdTokenKind.Whitespace,
            DtdTokenKind.MarkupDeclarationOpen,
            DtdTokenKind.Name,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Symbol,
            DtdTokenKind.Whitespace,
            DtdTokenKind.Name,
            DtdTokenKind.Whitespace,
            DtdTokenKind.QuotedLiteral,
            DtdTokenKind.DeclarationClose,
            DtdTokenKind.EndOfInput,
        }));
        Assert.That(tokens[0].Value, Is.EqualTo(" \n"));
        Assert.That(tokens[1].Name, Is.EqualTo("shared"));
        Assert.That(tokens[1].Lexeme, Is.EqualTo("%shared;"));
        Assert.That(tokens[6].Value, Is.EqualTo("%"));
        Assert.That(tokens[10].Value, Is.EqualTo("value"));
        Assert.That(tokens[10].Quote, Is.EqualTo('\''));
    }

    [Test]
    public void ClassifiesPublicSystemAndOrdinaryQuotedLiterals()
    {
        string text = "<!ENTITY one SYSTEM 'one.dtd'><!ENTITY two PUBLIC \"-//Example//DTD Sample//EN\" 'two.dtd'><!ENTITY SYSTEM 'plain'>";
        List<DtdToken> tokens = ScanAllTokens(new DtdScanner(text.AsMemory(), XmlVersion.Xml10));

        List<DtdToken> literals = tokens.Where(token => token.Quote != '\0').ToList();
        Assert.That(literals.Select(token => token.Kind), Is.EqualTo(new DtdTokenKind[]
        {
            DtdTokenKind.SystemLiteral,
            DtdTokenKind.PublicIdentifierLiteral,
            DtdTokenKind.SystemLiteral,
            DtdTokenKind.QuotedLiteral,
        }));
        Assert.That(literals.Select(token => token.Value), Is.EqualTo(new string[]
        {
            "one.dtd",
            "-//Example//DTD Sample//EN",
            "two.dtd",
            "plain",
        }));
        Assert.That(literals[1].Quote, Is.EqualTo('"'));
    }

    [Test]
    public void TokenizesCommentsAndProcessingInstructionsAsSingleUnits()
    {
        string text = "<!-- dtd > [ ] --><?target data > [ ]?><?empty?><?xml encoding='UTF-8'?>";
        List<DtdToken> tokens = ScanAllTokens(new DtdScanner(text.AsMemory(), XmlVersion.Xml10));

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new DtdTokenKind[]
        {
            DtdTokenKind.Comment,
            DtdTokenKind.ProcessingInstruction,
            DtdTokenKind.ProcessingInstruction,
            DtdTokenKind.ProcessingInstruction,
            DtdTokenKind.EndOfInput,
        }));
        Assert.That(tokens[0].Value, Is.EqualTo(" dtd > [ ] "));
        Assert.That(tokens[1].Name, Is.EqualTo("target"));
        Assert.That(tokens[1].Value, Is.EqualTo("data > [ ]"));
        Assert.That(tokens[1].HasNameValueSeparator, Is.True);
        Assert.That(tokens[2].Name, Is.EqualTo("empty"));
        Assert.That(tokens[2].Value, Is.Empty);
        Assert.That(tokens[2].HasNameValueSeparator, Is.False);
        Assert.That(tokens[3].Name, Is.EqualTo("xml"));
        Assert.That(tokens[3].Value, Is.EqualTo("encoding='UTF-8'"));
    }

    [Test]
    public void PreservesConditionalSectionDelimiterBoundaries()
    {
        string text = "<![ INCLUDE [ <!ENTITY x 'y'> ]]><![]]>";
        List<DtdToken> tokens = ScanAllTokens(new DtdScanner(text.AsMemory(), XmlVersion.Xml10));

        Assert.That(tokens.Count(token => token.Kind == DtdTokenKind.ConditionalSectionOpen), Is.EqualTo(2));
        Assert.That(tokens.Count(token => token.Kind == DtdTokenKind.ConditionalSectionClose), Is.EqualTo(2));
        Assert.That(tokens.First(token => token.Kind == DtdTokenKind.ConditionalSectionOpen).Lexeme, Is.EqualTo("<!["));
        Assert.That(tokens.First(token => token.Kind == DtdTokenKind.ConditionalSectionClose).Lexeme, Is.EqualTo("]]>"));
        Assert.That(string.Concat(tokens.Where(token => token.Kind != DtdTokenKind.EndOfInput).Select(token => token.Lexeme)), Is.EqualTo(text));
    }

    [Test]
    public void SupportsUnicodeNamesAndSupplementarySymbols()
    {
        string text = "元素 𐐀 \U000F0000";
        List<DtdToken> tokens = ScanAllTokens(new DtdScanner(text.AsMemory(), XmlVersion.Xml11));

        Assert.That(tokens[0].Kind, Is.EqualTo(DtdTokenKind.Name));
        Assert.That(tokens[0].Name, Is.EqualTo("元素"));
        Assert.That(tokens[2].Kind, Is.EqualTo(DtdTokenKind.Name));
        Assert.That(tokens[2].Name, Is.EqualTo("𐐀"));
        Assert.That(tokens[4].Kind, Is.EqualTo(DtdTokenKind.Symbol));
        Assert.That(tokens[4].Value, Is.EqualTo("\U000F0000"));
        Assert.That(tokens[4].Utf16Length, Is.EqualTo(2));
        AssertSequentialOffsets(tokens);
    }

    [TestCase("%missing")]
    [TestCase("<!ENTITY x PUBLIC 'bad[id' 'x'>")]
    [TestCase("'unterminated")]
    [TestCase("<!--unterminated")]
    [TestCase("<!--bad--comment-->")]
    [TestCase("<?pi unterminated")]
    [TestCase("<?pi?data?>")]
    public void RejectsMalformedLexicalConstructs(string text)
    {
        DtdScanner scanner = new DtdScanner(text.AsMemory(), XmlVersion.Xml10);

        Assert.Throws<FormatException>(() => ScanAllTokens(scanner));
    }

    [Test]
    public void RejectsIllegalDirectCharactersForTheSelectedXmlVersion()
    {
        Assert.Throws<FormatException>(() => ScanAllTokens(new DtdScanner("\0".AsMemory(), XmlVersion.Xml10)));
        Assert.Throws<FormatException>(() => ScanAllTokens(new DtdScanner("\u0001".AsMemory(), XmlVersion.Xml11)));
    }

    [Test]
    public void ResetReplaysTokensAndEndOfInputIsStable()
    {
        DtdScanner scanner = new DtdScanner("%part;".AsMemory(), XmlVersion.Xml10);
        List<DtdToken> first = ScanAllTokens(scanner);

        DtdToken repeatedEnd = scanner.ScanNextDtdToken();
        Assert.That(repeatedEnd.Kind, Is.EqualTo(DtdTokenKind.EndOfInput));
        Assert.That(repeatedEnd.StartUtf16Offset, Is.EqualTo(6));

        scanner.ResetDtdScanner();
        List<DtdToken> second = ScanAllTokens(scanner);

        Assert.That(second.Select(token => token.Kind), Is.EqualTo(first.Select(token => token.Kind)));
        Assert.That(second.Select(token => token.Lexeme), Is.EqualTo(first.Select(token => token.Lexeme)));
        Assert.That(scanner.CurrentDtdScannerOffset(), Is.EqualTo(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DtdScanner("".AsMemory(), (XmlVersion)99));
    }

    private static List<DtdToken> ScanAllTokens(DtdScanner scanner)
    {
        List<DtdToken> tokens = new List<DtdToken>();
        while (true)
        {
            DtdToken token = scanner.ScanNextDtdToken();
            tokens.Add(token);
            if (token.Kind == DtdTokenKind.EndOfInput)
            {
                return tokens;
            }
        }
    }

    private static void AssertSequentialOffsets(List<DtdToken> tokens)
    {
        int offset = 0;
        foreach (DtdToken token in tokens)
        {
            Assert.That(token.StartUtf16Offset, Is.EqualTo(offset));
            Assert.That(token.Utf16Length, Is.EqualTo(token.Lexeme.Length));
            offset = offset + token.Utf16Length;
        }
    }
}
