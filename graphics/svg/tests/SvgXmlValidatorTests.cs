namespace svg.Tests;

public class SvgXmlValidatorTests
{
    [Test]
    public void XmlNamesAcceptTheCompleteUnicodeNameCategories()
    {
        string input = "<n:À名\U00010000 xmlns:n='http://www.w3.org/2000/svg' attr\u00B7name='ok' combining\u0301='yes'/>";

        SvgLoadResult result = Load(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.XmlTokens, Has.Count.EqualTo(1));
            Assert.That(result.XmlTokens[0].QualifiedName, Is.EqualTo("n:À名\U00010000"));
            Assert.That(result.XmlTokens[0].Attributes[1].QualifiedName, Is.EqualTo("attr\u00B7name"));
            Assert.That(result.XmlTokens[0].Attributes[2].QualifiedName, Is.EqualTo("combining\u0301"));
        });
    }

    [TestCase("<1svg/>", SvgXmlValidationError.InvalidName, 1)]
    [TestCase("<sv$g/>", SvgXmlValidationError.InvalidName, 3)]
    [TestCase("<svg bad$name='x'/>", SvgXmlValidationError.InvalidName, 8)]
    [TestCase("</9svg>", SvgXmlValidationError.InvalidName, 2)]
    [TestCase("<?9pi?>", SvgXmlValidationError.InvalidName, 2)]
    [TestCase("<!DOCTYPE 9svg>", SvgXmlValidationError.InvalidName, 10)]
    public void InvalidXmlNamesReportTheirExactCharacter(string input, SvgXmlValidationError expectedError, int expectedOffset)
    {
        SvgXmlTokenizeResult tokenized = new SvgXmlTokenizer().Tokenize(input, 32, 256);
        SvgXmlValidationResult validation = new SvgXmlValidator().Validate(input, tokenized.Tokens, 128);

        Assert.Multiple(() =>
        {
            Assert.That(tokenized.Success, Is.True);
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Error, Is.EqualTo(expectedError));
            Assert.That(validation.ErrorOffset, Is.EqualTo(expectedOffset));
        });
    }

    [Test]
    public void XmlCharactersAcceptValidPlanesAndRejectForbiddenCodePointsAndUnpairedSurrogates()
    {
        SvgLoadResult valid = Load("<svg>\t\n\r\u0020\uD7FF\uE000\uFFFD\U00010000\U0010FFFF</svg>");
        SvgLoadResult control = Load("<svg>ok\u0001</svg>");
        SvgLoadResult nonCharacter = Load("<svg>\uFFFE</svg>");
        SvgLoadResult highSurrogate = Load("<svg>\uD800</svg>");
        SvgLoadResult lowSurrogate = Load("<svg>\uDC00</svg>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(control.Success, Is.False);
            Assert.That(control.Diagnostics[0].Source.StartOffset, Is.EqualTo(7));
            Assert.That(nonCharacter.Success, Is.False);
            Assert.That(nonCharacter.Diagnostics[0].Source.StartOffset, Is.EqualTo(5));
            Assert.That(highSurrogate.Success, Is.False);
            Assert.That(highSurrogate.Diagnostics[0].Source.StartOffset, Is.EqualTo(5));
            Assert.That(lowSurrogate.Success, Is.False);
            Assert.That(lowSurrogate.Diagnostics[0].Source.StartOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void PredefinedAndNumericReferencesDecodeWithoutChangingRawSource()
    {
        string input = "<svg a=\"x&#xA;\r\n\t&amp;&#65;&#x1F600;&lt;&gt;&apos;&quot;\">a&amp;&#65;&#x1F600;&lt;&gt;&apos;&quot;\r\nb</svg>";

        SvgLoadResult result = Load(input);
        SvgXmlAttributeToken attribute = result.XmlTokens[0].Attributes[0];
        string normalizedText = string.Concat(result.XmlTokens
            .Where(token => token.Kind == SvgXmlTokenKind.Text || token.Kind == SvgXmlTokenKind.Reference)
            .Select(token => token.NormalizedValue));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(string.Concat(result.XmlTokens.Select(token => token.RawText)), Is.EqualTo(input));
            Assert.That(attribute.Value, Is.EqualTo("x&#xA;\r\n\t&amp;&#65;&#x1F600;&lt;&gt;&apos;&quot;"));
            Assert.That(attribute.NormalizedValue, Is.EqualTo("x\n  &A😀<>'\""));
            Assert.That(attribute.References.Select(reference => reference.DecodedValue), Is.EqualTo(new[] { "\n", "&", "A", "😀", "<", ">", "'", "\"" }));
            Assert.That(normalizedText, Is.EqualTo("a&A😀<>'\"\nb"));
        });
    }

    [TestCase("<svg>&unknown;</svg>", SvgXmlValidationError.InvalidReference, 6)]
    [TestCase("<svg>&#;</svg>", SvgXmlValidationError.InvalidReference, 7)]
    [TestCase("<svg>&#x;</svg>", SvgXmlValidationError.InvalidReference, 8)]
    [TestCase("<svg>&#X41;</svg>", SvgXmlValidationError.InvalidReference, 7)]
    [TestCase("<svg>&#12a;</svg>", SvgXmlValidationError.InvalidReference, 9)]
    [TestCase("<svg>&#0;</svg>", SvgXmlValidationError.InvalidReference, 7)]
    [TestCase("<svg>&#xD800;</svg>", SvgXmlValidationError.InvalidReference, 8)]
    [TestCase("<svg>&#x110000;</svg>", SvgXmlValidationError.InvalidReference, 13)]
    public void InvalidReferencesAreRejectedAtStableOffsets(string input, SvgXmlValidationError expectedError, int expectedOffset)
    {
        SvgXmlTokenizeResult tokenized = new SvgXmlTokenizer().Tokenize(input, 32, 256);
        SvgXmlValidationResult validation = new SvgXmlValidator().Validate(input, tokenized.Tokens, 128);

        Assert.Multiple(() =>
        {
            Assert.That(tokenized.Success, Is.True);
            Assert.That(validation.Error, Is.EqualTo(expectedError));
            Assert.That(validation.ErrorOffset, Is.EqualTo(expectedOffset));
        });
    }

    [Test]
    public void DeclaredInternalAndExternalEntitiesRemainUnexpanded()
    {
        string internalSource = "<!DOCTYPE svg [<!ENTITY local 'boom'>]><svg>&local;</svg>";
        string externalSource = "<!DOCTYPE svg [<!ENTITY ext SYSTEM 'file:///secret'>]><svg>&ext;</svg>";
        SvgLoadOptions options = new SvgLoadOptions();
        options.DtdPolicy = SvgDtdPolicy.PreserveOnly;

        SvgLoadResult internalEntity = new SvgLoader().LoadWithStringAndSvgLoadOptions(internalSource, options);
        SvgLoadResult externalEntity = new SvgLoader().LoadWithStringAndSvgLoadOptions(externalSource, options);

        Assert.Multiple(() =>
        {
            Assert.That(internalEntity.Success, Is.False);
            Assert.That(internalEntity.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(internalEntity.Diagnostics[0].Message, Does.Contain("not expanded"));
            Assert.That(externalEntity.Success, Is.False);
            Assert.That(externalEntity.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(externalEntity.Diagnostics[0].Message, Does.Contain("not expanded"));
        });
    }

    [Test]
    public void LiteralLineEndingsNormalizeAcrossXmlContentKinds()
    {
        string input = "<svg><!--a\r\nb\r--><?work a\r\nb?><![CDATA[x\r\ny\r]]></svg>";

        SvgLoadResult result = Load(input);
        SvgXmlToken comment = result.XmlTokens.Single(token => token.Kind == SvgXmlTokenKind.Comment);
        SvgXmlToken instruction = result.XmlTokens.Single(token => token.Kind == SvgXmlTokenKind.ProcessingInstruction);
        SvgXmlToken cdata = result.XmlTokens.Single(token => token.Kind == SvgXmlTokenKind.CData);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(comment.Value, Is.EqualTo("a\r\nb\r"));
            Assert.That(comment.NormalizedValue, Is.EqualTo("a\nb\n"));
            Assert.That(instruction.NormalizedValue, Is.EqualTo("a\nb"));
            Assert.That(cdata.NormalizedValue, Is.EqualTo("x\ny\n"));
            Assert.That(string.Concat(result.XmlTokens.Select(token => token.RawText)), Is.EqualTo(input));
        });
    }

    [TestCase("<!--a--b-->", SvgXmlValidationError.InvalidComment, 5)]
    [TestCase("<!--a--->", SvgXmlValidationError.InvalidComment, 5)]
    [TestCase("a]]>b", SvgXmlValidationError.InvalidCharacterData, 1)]
    [TestCase("<?XmL data?>", SvgXmlValidationError.InvalidProcessingInstruction, 2)]
    [TestCase("<!DOCTYPE >", SvgXmlValidationError.InvalidDoctype, 10)]
    public void OtherWellFormednessRulesProduceSpecificFailures(string input, SvgXmlValidationError expectedError, int expectedOffset)
    {
        SvgXmlTokenizeResult tokenized = new SvgXmlTokenizer().Tokenize(input, 32, 256);
        SvgXmlValidationResult validation = new SvgXmlValidator().Validate(input, tokenized.Tokens, 128);

        Assert.Multiple(() =>
        {
            Assert.That(tokenized.Success, Is.True);
            Assert.That(validation.Error, Is.EqualTo(expectedError));
            Assert.That(validation.ErrorOffset, Is.EqualTo(expectedOffset));
        });
    }

    [Test]
    public void XmlDeclarationGrammarAcceptsOnlyVersionFirstAndOrderedOptionalValues()
    {
        SvgLoadResult valid = Load("<?xml version='1.0' encoding='UTF-8' standalone='yes'?><svg/>");
        SvgLoadResult missingVersion = Load("<?xml encoding='UTF-8'?><svg/>");
        SvgLoadResult wrongVersion = Load("<?xml version='1.1'?><svg/>");
        SvgLoadResult badEncoding = Load("<?xml version='1.0' encoding='9bad'?><svg/>");
        SvgLoadResult badStandalone = Load("<?xml version='1.0' standalone='maybe'?><svg/>");
        SvgLoadResult misplaced = Load("\n<?xml version='1.0'?><svg/>");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Success, Is.True);
            Assert.That(valid.XmlTokens[0].Attributes.Select(attribute => attribute.NormalizedValue), Is.EqualTo(new[] { "1.0", "UTF-8", "yes" }));
            Assert.That(missingVersion.Success, Is.False);
            Assert.That(wrongVersion.Success, Is.False);
            Assert.That(badEncoding.Success, Is.False);
            Assert.That(badStandalone.Success, Is.False);
            Assert.That(misplaced.Success, Is.False);
            Assert.That(misplaced.Diagnostics[0].Source.StartOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void NameLimitsAndLocatedValidationFailuresMapToLoaderDiagnostics()
    {
        SvgLoadOptions limitedOptions = new SvgLoadOptions();
        limitedOptions.BaseUri = "memory:names";
        limitedOptions.Limits.Tree.MaxNameCharacters = 3;
        SvgLoadResult limited = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svgx/>", limitedOptions);

        SvgLoadResult located = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg>\r\nok\u0001</svg>", new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(limited.Diagnostics[0].Source.Source.Uri, Is.EqualTo("memory:names"));
            Assert.That(limited.Diagnostics[0].Source.StartOffset, Is.EqualTo(4));
            Assert.That(located.Success, Is.False);
            Assert.That(located.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(located.Diagnostics[0].Source.StartOffset, Is.EqualTo(9));
            Assert.That(located.Diagnostics[0].Source.StartLine, Is.EqualTo(2));
            Assert.That(located.Diagnostics[0].Source.StartColumn, Is.EqualTo(3));
        });
    }

    [Test]
    public void ValidatorRejectsInvalidArgumentsWithoutMutatingTokens()
    {
        List<SvgXmlToken> tokens = new List<SvgXmlToken>();

        SvgXmlValidationResult nullSource = new SvgXmlValidator().Validate(null, tokens, 10);
        SvgXmlValidationResult nullTokens = new SvgXmlValidator().Validate("", null, 10);
        SvgXmlValidationResult invalidLimit = new SvgXmlValidator().Validate("", tokens, 0);

        Assert.Multiple(() =>
        {
            Assert.That(nullSource.Error, Is.EqualTo(SvgXmlValidationError.InvalidArgument));
            Assert.That(nullTokens.Error, Is.EqualTo(SvgXmlValidationError.InvalidArgument));
            Assert.That(invalidLimit.Error, Is.EqualTo(SvgXmlValidationError.InvalidArgument));
            Assert.That(tokens, Is.Empty);
        });
    }

    static SvgLoadResult Load(string input)
    {
        return new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
    }
}
