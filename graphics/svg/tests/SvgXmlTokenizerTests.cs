namespace svg.Tests;

public class SvgXmlTokenizerTests
{
    [Test]
    public void CompleteXmlSyntaxIsPartitionedIntoSourcePreservingTokens()
    {
        string input = "<?xml version=\"1.0\" encoding='UTF-8'?>\n" +
            "<!DOCTYPE svg [<!ENTITY logo \"mark\">]>\n" +
            "<?editor keep?>\n" +
            "<svg:svg xmlns:svg=\"http://www.w3.org/2000/svg\" data-x='a&amp;&#65;'>\n" +
            "text &logo;\n<!--note--><![CDATA[<raw>&logo;]]><g id=\"x\"/>\n" +
            "</svg:svg>";

        SvgXmlTokenizeResult result = new SvgXmlTokenizer().Tokenize(input, 128, 1024);
        List<SvgXmlToken> markup = result.Tokens.Where(token => token.Kind != SvgXmlTokenKind.Text).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(string.Concat(result.Tokens.Select(token => token.RawText)), Is.EqualTo(input));
            Assert.That(markup.Select(token => token.Kind), Is.EqualTo(new[]
            {
                SvgXmlTokenKind.Declaration,
                SvgXmlTokenKind.Doctype,
                SvgXmlTokenKind.ProcessingInstruction,
                SvgXmlTokenKind.StartElement,
                SvgXmlTokenKind.Reference,
                SvgXmlTokenKind.Comment,
                SvgXmlTokenKind.CData,
                SvgXmlTokenKind.EmptyElement,
                SvgXmlTokenKind.EndElement
            }));
            Assert.That(markup[0].Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "version", "encoding" }));
            Assert.That(markup[0].Attributes[0].Quote, Is.EqualTo('"'));
            Assert.That(markup[0].Attributes[1].Quote, Is.EqualTo('\''));
            Assert.That(markup[1].Value, Is.EqualTo(" svg [<!ENTITY logo \"mark\">]"));
            Assert.That(markup[2].QualifiedName, Is.EqualTo("editor"));
            Assert.That(markup[2].Value, Is.EqualTo("keep"));
            Assert.That(markup[3].QualifiedName, Is.EqualTo("svg:svg"));
            Assert.That(markup[3].Prefix, Is.EqualTo("svg"));
            Assert.That(markup[3].LocalName, Is.EqualTo("svg"));
            Assert.That(markup[3].Attributes, Has.Count.EqualTo(2));
            Assert.That(markup[3].Attributes[1].References.Select(reference => reference.RawText), Is.EqualTo(new[] { "&amp;", "&#65;" }));
            Assert.That(markup[4].Reference.Value, Is.EqualTo("logo"));
            Assert.That(markup[5].Value, Is.EqualTo("note"));
            Assert.That(markup[6].Value, Is.EqualTo("<raw>&logo;"));
            Assert.That(markup[7].QualifiedName, Is.EqualTo("g"));
            Assert.That(markup[8].QualifiedName, Is.EqualTo("svg:svg"));
        });
    }

    [Test]
    public void AttributeAndReferenceTokensRetainExactOffsetsQuotesAndLines()
    {
        string input = "<svg\r\n  xml:lang = 'en&amp;US'\n  plain=\"x\"/>";

        SvgXmlTokenizeResult result = new SvgXmlTokenizer().Tokenize(input, 16, 128);
        SvgXmlToken element = result.Tokens[0];
        SvgXmlAttributeToken language = element.Attributes[0];
        SvgXmlReferenceToken reference = language.References[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(element.Kind, Is.EqualTo(SvgXmlTokenKind.EmptyElement));
            Assert.That(element.StartLine, Is.EqualTo(1));
            Assert.That(element.EndLine, Is.EqualTo(3));
            Assert.That(language.RawText, Is.EqualTo("xml:lang = 'en&amp;US'"));
            Assert.That(language.QualifiedName, Is.EqualTo("xml:lang"));
            Assert.That(language.Prefix, Is.EqualTo("xml"));
            Assert.That(language.LocalName, Is.EqualTo("lang"));
            Assert.That(language.Value, Is.EqualTo("en&amp;US"));
            Assert.That(language.StartOffset, Is.EqualTo(input.IndexOf("xml:lang", StringComparison.Ordinal)));
            Assert.That(language.StartLine, Is.EqualTo(2));
            Assert.That(language.StartColumn, Is.EqualTo(3));
            Assert.That(reference.RawText, Is.EqualTo("&amp;"));
            Assert.That(reference.StartOffset, Is.EqualTo(input.IndexOf("&amp;", StringComparison.Ordinal)));
            Assert.That(reference.StartLine, Is.EqualTo(2));
            Assert.That(input.Substring(reference.StartOffset, reference.EndOffset - reference.StartOffset), Is.EqualTo(reference.RawText));
        });
    }

    [Test]
    public void TextReferencesAreSeparateWhileCdataAndDoctypeContentRemainUnexpanded()
    {
        string input = "<!DOCTYPE svg [<!ENTITY x 'expanded'>]><svg>a&amp;b&#x20;c&x;<![CDATA[&amp;&x;]]></svg>";

        SvgXmlTokenizeResult result = new SvgXmlTokenizer().Tokenize(input, 32, 256);
        List<SvgXmlToken> references = result.Tokens.Where(token => token.Kind == SvgXmlTokenKind.Reference).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(references.Select(token => token.Value), Is.EqualTo(new[] { "amp", "#x20", "x" }));
            Assert.That(result.Tokens.Single(token => token.Kind == SvgXmlTokenKind.CData).Value, Is.EqualTo("&amp;&x;"));
            Assert.That(result.Tokens.Single(token => token.Kind == SvgXmlTokenKind.Doctype).RawText, Does.Contain("<!ENTITY x 'expanded'>"));
            Assert.That(result.Tokens.SelectMany(token => token.Attributes), Is.Empty);
        });
    }

    [TestCase("<", SvgXmlTokenizeError.ExpectedName, 1)]
    [TestCase("<svg", SvgXmlTokenizeError.UnexpectedEnd, 4)]
    [TestCase("<svg a=1>", SvgXmlTokenizeError.InvalidAttribute, 7)]
    [TestCase("<svg a='x>", SvgXmlTokenizeError.UnexpectedEnd, 10)]
    [TestCase("<svg a='<'/>", SvgXmlTokenizeError.InvalidAttribute, 8)]
    [TestCase("<svg a='&bad'/>", SvgXmlTokenizeError.InvalidReference, 12)]
    [TestCase("text & bad;", SvgXmlTokenizeError.InvalidReference, 6)]
    [TestCase("<!--open", SvgXmlTokenizeError.UnexpectedEnd, 8)]
    [TestCase("<![CDATA[open", SvgXmlTokenizeError.UnexpectedEnd, 13)]
    [TestCase("<?target", SvgXmlTokenizeError.UnexpectedEnd, 8)]
    [TestCase("<!DOCTYPE svg [", SvgXmlTokenizeError.UnexpectedEnd, 15)]
    [TestCase("</svg extra>", SvgXmlTokenizeError.InvalidMarkup, 6)]
    public void MalformedLexicalConstructsReportExactOffsets(string input, SvgXmlTokenizeError error, int offset)
    {
        SvgXmlTokenizeResult result = new SvgXmlTokenizer().Tokenize(input, 32, 128);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.ErrorOffset, Is.EqualTo(offset));
            Assert.That(result.ErrorLine, Is.GreaterThan(0));
            Assert.That(result.ErrorColumn, Is.GreaterThan(0));
        });
    }

    [Test]
    public void DeclarationAndElementAttributesRequireWhitespaceAndQuotes()
    {
        SvgXmlTokenizeResult declaration = new SvgXmlTokenizer().Tokenize("<?xml version='1.0'encoding='UTF-8'?>", 16, 128);
        SvgXmlTokenizeResult element = new SvgXmlTokenizer().Tokenize("<svg a='1'b='2'/>", 16, 128);
        SvgXmlTokenizeResult unquoted = new SvgXmlTokenizer().Tokenize("<svg a=value/>", 16, 128);

        Assert.Multiple(() =>
        {
            Assert.That(declaration.Error, Is.EqualTo(SvgXmlTokenizeError.InvalidAttribute));
            Assert.That(declaration.ErrorOffset, Is.EqualTo(19));
            Assert.That(element.Error, Is.EqualTo(SvgXmlTokenizeError.InvalidAttribute));
            Assert.That(element.ErrorOffset, Is.EqualTo(10));
            Assert.That(unquoted.Error, Is.EqualTo(SvgXmlTokenizeError.InvalidAttribute));
            Assert.That(unquoted.ErrorOffset, Is.EqualTo(7));
        });
    }

    [Test]
    public void TokenAndCharacterBoundsFailBeforeUnboundedSnapshotsAreCreated()
    {
        SvgXmlTokenizeResult tokenLimit = new SvgXmlTokenizer().Tokenize("<svg a='1'/>", 1, 128);
        SvgXmlTokenizeResult tagLength = new SvgXmlTokenizer().Tokenize("<svg/>", 8, 5);
        SvgXmlTokenizeResult textLength = new SvgXmlTokenizer().Tokenize("abcdef", 8, 5);
        SvgXmlTokenizeResult referenceLength = new SvgXmlTokenizer().Tokenize("&abcdef;", 8, 5);

        Assert.Multiple(() =>
        {
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgXmlTokenizeError.TokenLimitExceeded));
            Assert.That(tokenLimit.ErrorOffset, Is.EqualTo(5));
            Assert.That(tagLength.Error, Is.EqualTo(SvgXmlTokenizeError.TokenTooLong));
            Assert.That(tagLength.ErrorOffset, Is.EqualTo(5));
            Assert.That(textLength.ErrorOffset, Is.EqualTo(5));
            Assert.That(referenceLength.ErrorOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void FailureLocationsAccountForMixedXmlNewlines()
    {
        string input = "<svg>\r\n<g>\r<bad a=1>";

        SvgXmlTokenizeResult result = new SvgXmlTokenizer().Tokenize(input, 16, 128);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error, Is.EqualTo(SvgXmlTokenizeError.InvalidAttribute));
            Assert.That(result.ErrorOffset, Is.EqualTo(input.IndexOf('1')));
            Assert.That(result.ErrorLine, Is.EqualTo(3));
            Assert.That(result.ErrorColumn, Is.EqualTo(8));
        });
    }

    [Test]
    public void LoaderAttachesTokensAndMapsMalformedAndBoundedFailuresToDiagnostics()
    {
        SvgLoadOptions sourceOptions = new SvgLoadOptions();
        sourceOptions.BaseUri = "memory:document";
        SvgLoadResult loaded = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(System.Text.Encoding.UTF8.GetBytes("<svg a='1'/>"), sourceOptions);

        SvgLoadResult malformed = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg a=1>", sourceOptions);
        SvgLoadOptions limitedOptions = new SvgLoadOptions();
        limitedOptions.Limits.Input.MaxXmlTokenCharacters = 5;
        SvgLoadResult limited = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg/>", limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.Success, Is.True);
            Assert.That(loaded.XmlTokens, Has.Count.EqualTo(1));
            Assert.That(loaded.XmlTokens[0].Attributes, Has.Count.EqualTo(1));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(malformed.Diagnostics[0].Source.Source.Uri, Is.EqualTo("memory:document"));
            Assert.That(malformed.Diagnostics[0].Source.StartOffset, Is.EqualTo(7));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void EmptyDecodedSourcesProduceAnEmptySuccessfulTokenStream()
    {
        SvgXmlTokenizeResult result = new SvgXmlTokenizer().Tokenize("", 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Tokens, Is.Empty);
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }
}
