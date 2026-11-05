using xml;

namespace xml.Tests;

[TestFixture]
public class XmlLexicalScannerTests
{
    [Test]
    public void TokenBufferAppendsUtf16SpansAndUnicodeScalarsAcrossGrowth()
    {
        XmlTokenBuffer buffer = new XmlTokenBuffer(1);
        buffer.AppendTokenCharacter('<');
        buffer.AppendTokenCharacters("name".AsSpan());
        buffer.AppendTokenScalar(0x1F600);

        Assert.That(buffer.TokenText(), Is.EqualTo("<name\U0001F600"));
        Assert.That(buffer.TokenCharacters().ToString(), Is.EqualTo("<name\U0001F600"));
        Assert.That(buffer.TokenCharacterCount(), Is.EqualTo(7));

        buffer.ClearToken();
        Assert.That(buffer.TokenCharacterCount(), Is.Zero);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.AppendTokenScalar(0xD800));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.AppendTokenScalar(0x110000));
    }

    [Test]
    public void ScannerReadsRequiredWhitespaceAndQualifiedNameWithExactOffsets()
    {
        XmlLexicalScanner scanner = new XmlLexicalScanner(" \t\r\nAlpha:local rest".AsMemory(), XmlVersion.Xml10);

        XmlLexicalToken whitespace = scanner.ScanXmlSpace();
        XmlQualifiedNameToken name = scanner.ScanXmlQualifiedName();

        Assert.That(whitespace.Value, Is.EqualTo(" \t\r\n"));
        Assert.That(whitespace.StartUtf16Offset, Is.Zero);
        Assert.That(whitespace.Utf16Length, Is.EqualTo(4));
        Assert.That(name.Value, Is.EqualTo("Alpha:local"));
        Assert.That(name.Prefix, Is.EqualTo("Alpha"));
        Assert.That(name.LocalName, Is.EqualTo("local"));
        Assert.That(name.StartUtf16Offset, Is.EqualTo(4));
        Assert.That(name.Utf16Length, Is.EqualTo(11));
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.EqualTo(15));
    }

    [Test]
    public void ScannerDistinguishesNameNmtokenAndNcNameProductions()
    {
        XmlLexicalScanner nameScanner = new XmlLexicalScanner(":root.part rest".AsMemory(), XmlVersion.Xml11);
        XmlLexicalScanner tokenScanner = new XmlLexicalScanner("-9.name rest".AsMemory(), XmlVersion.Xml10);
        XmlLexicalScanner ncNameScanner = new XmlLexicalScanner("local:tail".AsMemory(), XmlVersion.Xml10);

        Assert.That(nameScanner.ScanXmlName().Value, Is.EqualTo(":root.part"));
        Assert.That(tokenScanner.ScanXmlNmtoken().Value, Is.EqualTo("-9.name"));
        Assert.That(ncNameScanner.ScanXmlNcName().Value, Is.EqualTo("local"));
        Assert.That(ncNameScanner.CurrentXmlLexicalOffset(), Is.EqualTo(5));
    }

    [Test]
    public void ScannerHandlesSupplementaryNameCharactersAsWholeScalars()
    {
        string supplementary = char.ConvertFromUtf32(0x10000);
        XmlLexicalScanner scanner = new XmlLexicalScanner((supplementary + "-x ").AsMemory(), XmlVersion.Xml10);

        XmlLexicalToken name = scanner.ScanXmlNcName();

        Assert.That(name.Value, Is.EqualTo(supplementary + "-x"));
        Assert.That(name.Utf16Length, Is.EqualTo(4));
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.EqualTo(4));
    }

    [TestCase(":local")]
    [TestCase("prefix:")]
    [TestCase("a:b:c")]
    [TestCase("a:9local")]
    public void ScannerRejectsMalformedQualifiedNamesWithoutAdvancing(string text)
    {
        XmlLexicalScanner scanner = new XmlLexicalScanner(text.AsMemory(), XmlVersion.Xml10);

        Assert.Throws<FormatException>(() => scanner.ScanXmlQualifiedName());
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.Zero);
    }

    [Test]
    public void ScannerRejectsMissingNameAndWhitespaceWithoutAdvancing()
    {
        XmlLexicalScanner nameScanner = new XmlLexicalScanner("9name".AsMemory(), XmlVersion.Xml10);
        XmlLexicalScanner whitespaceScanner = new XmlLexicalScanner("name".AsMemory(), XmlVersion.Xml11);

        Assert.Throws<FormatException>(() => nameScanner.ScanXmlName());
        Assert.Throws<FormatException>(() => whitespaceScanner.ScanXmlSpace());
        Assert.That(nameScanner.CurrentXmlLexicalOffset(), Is.Zero);
        Assert.That(whitespaceScanner.CurrentXmlLexicalOffset(), Is.Zero);
    }

    [Test]
    public void ScannerReadsQuotedAndPublicIdentifierLiterals()
    {
        XmlLexicalScanner literalScanner = new XmlLexicalScanner("\"a'b<>&\U0001F600\"tail".AsMemory(), XmlVersion.Xml10);
        XmlLexicalScanner publicScanner = new XmlLexicalScanner("\"AZaz09 -'()+,./:=?;!*#@$_%\r\n\"tail".AsMemory(), XmlVersion.Xml11);

        XmlQuotedLiteralToken literal = literalScanner.ScanXmlQuotedLiteral();
        XmlQuotedLiteralToken publicIdentifier = publicScanner.ScanXmlPublicIdentifierLiteral();

        Assert.That(literal.Value, Is.EqualTo("a'b<>&\U0001F600"));
        Assert.That(literal.Quote, Is.EqualTo('"'));
        Assert.That(literal.Utf16Length, Is.EqualTo(10));
        Assert.That(publicIdentifier.Value, Is.EqualTo("AZaz09 -'()+,./:=?;!*#@$_%\r\n"));
        Assert.That(publicIdentifier.StartUtf16Offset, Is.Zero);
        Assert.That(publicScanner.CurrentXmlLexicalOffset(), Is.EqualTo(publicIdentifier.Utf16Length));
    }

    [Test]
    public void ScannerEnforcesPublicIdentifierAndDirectCharacterRules()
    {
        Assert.That(XmlLexicalCharacterRules.IsXmlPublicIdentifierCharacter('\''), Is.True);
        Assert.That(XmlLexicalCharacterRules.IsXmlPublicIdentifierCharacter('\t'), Is.False);
        Assert.That(XmlLexicalCharacterRules.IsXmlSpaceCharacter('\t'), Is.True);
        Assert.That(XmlLexicalCharacterRules.IsXmlSpaceCharacter(0x85), Is.False);

        XmlLexicalScanner publicScanner = new XmlLexicalScanner("\"bad\tvalue\"".AsMemory(), XmlVersion.Xml10);
        XmlLexicalScanner xml10Scanner = new XmlLexicalScanner("\"bad\0value\"".AsMemory(), XmlVersion.Xml10);
        XmlLexicalScanner xml11Scanner = new XmlLexicalScanner("\"bad\u0001value\"".AsMemory(), XmlVersion.Xml11);

        Assert.Throws<FormatException>(() => publicScanner.ScanXmlPublicIdentifierLiteral());
        Assert.Throws<FormatException>(() => xml10Scanner.ScanXmlQuotedLiteral());
        Assert.Throws<FormatException>(() => xml11Scanner.ScanXmlQuotedLiteral());
        Assert.That(publicScanner.CurrentXmlLexicalOffset(), Is.Zero);
        Assert.That(xml10Scanner.CurrentXmlLexicalOffset(), Is.Zero);
        Assert.That(xml11Scanner.CurrentXmlLexicalOffset(), Is.Zero);
    }

    [Test]
    public void ScannerReadsDecimalAndHexadecimalCharacterReferences()
    {
        XmlLexicalScanner scanner = new XmlLexicalScanner("&#65;&#x1F600;!".AsMemory(), XmlVersion.Xml10);

        XmlCharacterReference decimalReference = scanner.ScanXmlCharacterReference();
        XmlCharacterReference hexadecimalReference = scanner.ScanXmlCharacterReference();

        Assert.That(decimalReference.ScalarValue, Is.EqualTo('A'));
        Assert.That(decimalReference.ReplacementText, Is.EqualTo("A"));
        Assert.That(decimalReference.SourceCharacterCount, Is.EqualTo(5));
        Assert.That(hexadecimalReference.ScalarValue, Is.EqualTo(0x1F600));
        Assert.That(hexadecimalReference.ReplacementText, Is.EqualTo("\U0001F600"));
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.EqualTo(14));
    }

    [Test]
    public void Xml11ReferenceScannerAllowsRestrictedCharactersOnlyByReference()
    {
        XmlLexicalScanner scanner = new XmlLexicalScanner("&#x1;tail".AsMemory(), XmlVersion.Xml11);

        XmlCharacterReference reference = scanner.ScanXmlCharacterReference();

        Assert.That(reference.ScalarValue, Is.EqualTo(1));
        Assert.That(reference.RestrictedCharacter, Is.True);
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.EqualTo(5));

        XmlLexicalScanner malformed = new XmlLexicalScanner("&#xZZ;".AsMemory(), XmlVersion.Xml11);
        Assert.Throws<FormatException>(() => malformed.ScanXmlCharacterReference());
        Assert.That(malformed.CurrentXmlLexicalOffset(), Is.Zero);
    }

    [Test]
    public void ScannerMatchesAndConsumesDelimitersWithoutPartialAdvancement()
    {
        XmlLexicalScanner scanner = new XmlLexicalScanner("<![CDATA[text".AsMemory(), XmlVersion.Xml10);

        Assert.That(scanner.IsXmlDelimiter("<![CDATA[".AsSpan()), Is.True);
        Assert.That(scanner.ConsumeOptionalXmlDelimiter("<!--".AsSpan()), Is.False);
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.Zero);
        scanner.ConsumeRequiredXmlDelimiter("<![CDATA[".AsSpan());
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.EqualTo(9));
        Assert.That(scanner.IsXmlDelimiter("text".AsSpan()), Is.True);
        Assert.Throws<FormatException>(() => scanner.ConsumeRequiredXmlDelimiter("]]>".AsSpan()));
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.EqualTo(9));
        Assert.Throws<ArgumentException>(() => scanner.IsXmlDelimiter(ReadOnlySpan<char>.Empty));
    }

    [Test]
    public void ScannerResetRejectsOffsetsInsideSurrogatePairs()
    {
        XmlLexicalScanner scanner = new XmlLexicalScanner("A\U0001F600B".AsMemory(), XmlVersion.Xml10);

        scanner.ResetXmlLexicalOffset(1);
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.EqualTo(1));
        Assert.Throws<ArgumentException>(() => scanner.ResetXmlLexicalOffset(2));
        Assert.That(scanner.CurrentXmlLexicalOffset(), Is.EqualTo(1));
        scanner.ResetXmlLexicalOffset(4);
        Assert.That(scanner.IsXmlLexicalEnd(), Is.True);
    }
}
