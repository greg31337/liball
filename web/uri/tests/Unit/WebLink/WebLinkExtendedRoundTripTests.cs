namespace uri.Tests;

public class WebLinkExtendedRoundTripTests
{
    [Test]
    public void Utf8TitleAndLanguageMetadataSurvivePublicRoundTrip()
    {
        const string fieldValue =
            "<https://example.test/resource>; " +
            "title=\"fallback title\"; " +
            "title*=uTf-8'ZH-hAnt-cn'%e6%a8%99%e9%a1%8c%20" +
            "%f0%9f%98%80; " +
            "hreflang=zh-Hant-CN; hreflang=x-private";
        const string expected =
            "<https://example.test/resource>; " +
            "title=\"fallback title\"; " +
            "title*=UTF-8'ZH-hAnt-cn'%E6%A8%99%E9%A1%8C%20" +
            "%F0%9F%98%80; " +
            "hreflang=zh-Hant-CN; hreflang=x-private";
        IWebLinkParser parser = new WebLinkParser();
        IWebLinkComposer composer = new WebLinkComposer();

        WebLinkSet parsed = parser.ParseLinkFieldValues(
            new string[] { fieldValue },
            new WebLinkParseOptions());
        string composed = composer.ComposeLinkFieldValue(
            parsed,
            new OperationLimits());
        WebLinkSet reparsed = parser.ParseLinkFieldValues(
            new string[] { composed },
            new WebLinkParseOptions());

        WebLink first = parsed.Links[0];
        WebLink second = reparsed.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(first.HasTitle, Is.True);
            Assert.That(first.Title, Is.EqualTo("fallback title"));
            Assert.That(first.HasTitleExtended, Is.True);
            Assert.That(first.TitleExtended, Is.EqualTo("標題 😀"));
            Assert.That(first.TitleExtendedCharacterSet,
                Is.EqualTo("uTf-8"));
            Assert.That(first.TitleExtendedLanguage,
                Is.EqualTo("ZH-hAnt-cn"));
            Assert.That(first.HrefLanguages,
                Is.EqualTo(new string[] { "zh-Hant-CN", "x-private" }));
            Assert.That(composed, Is.EqualTo(expected));
            Assert.That(second.Title, Is.EqualTo(first.Title));
            Assert.That(second.TitleExtended,
                Is.EqualTo(first.TitleExtended));
            Assert.That(second.TitleExtendedCharacterSet,
                Is.EqualTo("UTF-8"));
            Assert.That(second.TitleExtendedLanguage,
                Is.EqualTo(first.TitleExtendedLanguage));
            Assert.That(second.HrefLanguages,
                Is.EqualTo(first.HrefLanguages));
        });
    }

    [TestCase("UTF-8'en'bad%")]
    [TestCase("UTF-8'en'bad%2")]
    [TestCase("UTF-8'en'bad%GG")]
    public void PercentErrorsFailStrictModeAndUseFallbackInRecovery(
        string rawValue)
    {
        string fieldValue =
            "<target>; title=fallback; title*=" + rawValue;

        WebLinkParseResult strict = Parse(fieldValue, false);
        WebLinkParseResult recovery = Parse(fieldValue, true);

        Assert.That(strict.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidExtendedParameter));
            Assert.That(strict.Diagnostic.Component,
                Is.EqualTo(UriComponent.ExtendedParameter));
            Assert.That(recovery.Success, Is.True);
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(recovery.Diagnostics[0].Code,
                Is.EqualTo(UriDiagnosticCode.InvalidExtendedParameter));
            Assert.That(recovery.LinkSet.Links[0].Title,
                Is.EqualTo("fallback"));
            Assert.That(recovery.LinkSet.Links[0].HasTitleExtended,
                Is.False);
            Assert.That(recovery.LinkSet.Links[0].Parameters[1].RawValue,
                Is.EqualTo(rawValue));
        });
    }

    [TestCase(
        "UTF-8'en'%C3%28",
        UriDiagnosticCode.InvalidExtendedParameter)]
    [TestCase(
        "UTF-8'en'%C0%AF",
        UriDiagnosticCode.InvalidExtendedParameter)]
    [TestCase(
        "ISO-8859-1'en'%A3%20rates",
        UriDiagnosticCode.UnsupportedCharacterSet)]
    public void InvalidUtf8AndUnsupportedCharsetsRecoverToPlainTitle(
        string rawValue,
        UriDiagnosticCode expectedCode)
    {
        string fieldValue =
            "<target>; title=plain; title*=" + rawValue;

        WebLinkParseResult strict = Parse(fieldValue, false);
        WebLinkParseResult recovery = Parse(fieldValue, true);

        Assert.That(strict.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostic!.Code, Is.EqualTo(expectedCode));
            Assert.That(recovery.Success, Is.True);
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(recovery.Diagnostics[0].Code,
                Is.EqualTo(expectedCode));
            Assert.That(recovery.LinkSet.Links[0].HasTitle, Is.True);
            Assert.That(recovery.LinkSet.Links[0].Title,
                Is.EqualTo("plain"));
            Assert.That(recovery.LinkSet.Links[0].HasTitleExtended,
                Is.False);
            Assert.That(recovery.LinkSet.Links[0].Parameters[1].RawValue,
                Is.EqualTo(rawValue));
        });
    }

    [Test]
    public void InvalidExtendedLanguageRecoversWithoutReplacingFallback()
    {
        const string fieldValue =
            "<target>; title=fallback; " +
            "title*=UTF-8'en_US'preferred%20title; " +
            "hreflang=de-CH";

        WebLinkParseResult strict = Parse(fieldValue, false);
        WebLinkParseResult recovery = Parse(fieldValue, true);

        Assert.That(strict.Diagnostic, Is.Not.Null);
        WebLink link = recovery.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLanguageTag));
            Assert.That(strict.Diagnostic.Component,
                Is.EqualTo(UriComponent.LanguageTag));
            Assert.That(recovery.Success, Is.True);
            Assert.That(recovery.Recovered, Is.True);
            Assert.That(recovery.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(recovery.Diagnostics[0].Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLanguageTag));
            Assert.That(link.Title, Is.EqualTo("fallback"));
            Assert.That(link.HasTitleExtended, Is.False);
            Assert.That(link.Parameters[1].Value,
                Is.EqualTo("preferred title"));
            Assert.That(link.Parameters[1].Language,
                Is.EqualTo("en_US"));
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { "de-CH" }));
        });
    }

    private static WebLinkParseResult Parse(
        string fieldValue,
        bool recovery)
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        if (recovery)
        {
            options.Mode = WebLinkParseMode.Recovery;
        }

        IWebLinkParser parser = new WebLinkParser();
        return parser.TryParseLinkFieldValueSpan(
            fieldValue.AsSpan(),
            options);
    }
}
