namespace uri.Tests;

public class WebLinkLanguageTagCoreTests
{
    [TestCase("en")]
    [TestCase("de-CH")]
    [TestCase("zh-cmn-Hans-CN")]
    [TestCase("sl-rozaj-biske-1994")]
    [TestCase("en-Latn-US-u-ca-gregory-x-private")]
    [TestCase("x-private-12345678")]
    [TestCase("en-0-abc")]
    [TestCase("en-001")]
    [TestCase("abcd")]
    [TestCase("abcdefgh")]
    [TestCase("zz")]
    [TestCase("EN-lAtN-us")]
    public void StrictParserAcceptsWellFormedLanguageTagCategories(
        string language)
    {
        string text = "<a>; hreflang=\"" + language + "\"";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        WebLinkParameter parameter = link.Parameters[0];
        Assert.Multiple(() =>
        {
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { language }));
            Assert.That(parameter.Kind,
                Is.EqualTo(WebLinkParameterKind.HrefLang));
            Assert.That(parameter.Value, Is.EqualTo(language));
            Assert.That(parameter.RawValue, Is.EqualTo(language));
            Assert.That(parameter.OriginalText,
                Is.EqualTo("; hreflang=\"" + language + "\""));
            Assert.That(parameter.Ignored, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void EveryGrandfatheredTagIsAcceptedCaseInsensitively()
    {
        int index = 0;
        while (index < WebLinkLanguageTagCore.GrandfatheredTags.Length)
        {
            string tag =
                WebLinkLanguageTagCore.GrandfatheredTags[index].ToUpperInvariant();
            WebLinkLanguageTagValidationResult result =
                WebLinkLanguageTagCore.ValidateTag(tag);

            Assert.That(result.Valid, Is.True, tag);
            index = index + 1;
        }
    }

    [TestCase("", 0, 0)]
    [TestCase("e", 0, 1)]
    [TestCase("123", 0, 3)]
    [TestCase("abcdefghi", 0, 9)]
    [TestCase("en-", 3, 0)]
    [TestCase("-en", 0, 1)]
    [TestCase("en--US", 3, 1)]
    [TestCase("en_US", 2, 1)]
    [TestCase("en-12", 3, 2)]
    [TestCase("x", 1, 0)]
    [TestCase("en-x", 4, 0)]
    [TestCase("x-abcdefghi", 2, 9)]
    [TestCase("en-a", 4, 0)]
    [TestCase("en-a-x-private", 4, 0)]
    [TestCase("sl-rozaj-ROZAJ", 9, 5)]
    [TestCase("en-a-foo-A-bar", 9, 1)]
    [TestCase("en-US-US", 6, 2)]
    [TestCase("en US", 2, 1)]
    [TestCase("d\u00e9", 1, 1)]
    public void StrictParserRejectsInvalidLanguageTagsAtExactOffsets(
        string language,
        int relativeOffset,
        int expectedLength)
    {
        string text = "<a>; hreflang=\"" + language + "\"";
        int valueOffset = text.IndexOf('"') + 1;

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertLanguageFailure(
            result,
            valueOffset + relativeOffset,
            expectedLength);
    }

    [Test]
    public void RecoveryOmitsInvalidHrefLangValuesButPreservesRawParameters()
    {
        const string text =
            "<a>; hreflang=en; HREFLANG=\"bad tag\"; hreflang=de-CH";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLanguageTag));
            Assert.That(result.Diagnostics[0].Severity,
                Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(text.IndexOf(' ', text.IndexOf("bad tag"))));
            Assert.That(result.Diagnostics[0].Range.Length,
                Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Message,
                Is.EqualTo(
                    WebLinkLanguageTagCore.RecoveredInvalidLanguageTagMessage));
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { "en", "de-CH" }));
            Assert.That(link.Parameters, Has.Length.EqualTo(3));
            Assert.That(link.Parameters[1].Value,
                Is.EqualTo("bad tag"));
            Assert.That(link.Parameters[1].RawValue,
                Is.EqualTo("bad tag"));
            Assert.That(link.Parameters[1].OriginalText,
                Is.EqualTo("; HREFLANG=\"bad tag\""));
            Assert.That(link.Parameters[1].Ignored, Is.False);
        });
    }

    [Test]
    public void ValidExtendedLanguageRetainsOriginalCaseAndRawText()
    {
        const string text =
            "<a>; title*=UTF-8'ZH-hAnt-cn'good%20title";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        WebLink link = result.LinkSet.Links[0];
        WebLinkParameter parameter = link.Parameters[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(link.HasTitleExtended, Is.True);
            Assert.That(link.TitleExtended, Is.EqualTo("good title"));
            Assert.That(link.TitleExtendedLanguage,
                Is.EqualTo("ZH-hAnt-cn"));
            Assert.That(parameter.Language, Is.EqualTo("ZH-hAnt-cn"));
            Assert.That(parameter.Value, Is.EqualTo("good title"));
            Assert.That(parameter.RawValue,
                Is.EqualTo("UTF-8'ZH-hAnt-cn'good%20title"));
            Assert.That(parameter.OriginalText, Is.EqualTo(
                "; title*=UTF-8'ZH-hAnt-cn'good%20title"));
        });
    }

    [Test]
    public void InvalidExtendedLanguageFailsStrictParsingAtItsSourceRange()
    {
        const string text =
            "<a>; title*=UTF-8'en_US'good%20title";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertLanguageFailure(result, text.IndexOf('_'), 1);
    }

    [Test]
    public void RecoveryDecodesButDoesNotApplyTitleWithInvalidLanguage()
    {
        const string text =
            "<a>; title=fallback; title*=UTF-8'en_US'good%20title";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        WebLinkParameter parameter = link.Parameters[1];
        Assert.Multiple(() =>
        {
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLanguageTag));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(text.IndexOf('_')));
            Assert.That(link.HasTitle, Is.True);
            Assert.That(link.Title, Is.EqualTo("fallback"));
            Assert.That(link.HasTitleExtended, Is.False);
            Assert.That(link.TitleExtended, Is.Empty);
            Assert.That(parameter.Value, Is.EqualTo("good title"));
            Assert.That(parameter.RawValue,
                Is.EqualTo("UTF-8'en_US'good%20title"));
            Assert.That(parameter.CharacterSet, Is.EqualTo("UTF-8"));
            Assert.That(parameter.Language, Is.EqualTo("en_US"));
            Assert.That(parameter.OriginalText,
                Is.EqualTo("; title*=UTF-8'en_US'good%20title"));
        });
    }

    [Test]
    public void EmptyExtendedLanguageRemainsValid()
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                "<a>; title*=UTF-8''value".AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links[0].TitleExtended,
                Is.EqualTo("value"));
            Assert.That(result.LinkSet.Links[0].TitleExtendedLanguage,
                Is.Empty);
        });
    }

    [Test]
    public void ExtendedValueEncoderRejectsInvalidLanguageMetadata()
    {
        PercentEncodeResult result =
            WebLinkExtendedValueCore.EncodeUtf8Value(
                "value",
                "en_US",
                100);

        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLanguageTag));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.LanguageTag));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(2));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
        });
    }

    private static WebLinkParseOptions CreateRecoveryOptions()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Mode = WebLinkParseMode.Recovery;
        return options;
    }

    private static void AssertLanguageFailure(
        WebLinkParseResult result,
        int offset,
        int length)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLanguageTag));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.LanguageTag));
            Assert.That(result.Diagnostic.Range.Offset,
                Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length,
                Is.EqualTo(length));
            Assert.That(result.Diagnostic.Message,
                Is.EqualTo(WebLinkLanguageTagCore.InvalidLanguageTagMessage));
        });
    }
}
