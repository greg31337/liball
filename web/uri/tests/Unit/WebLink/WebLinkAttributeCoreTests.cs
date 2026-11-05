namespace uri.Tests;

public class WebLinkAttributeCoreTests
{
    [Test]
    public void StrictParserInterpretsStandardAttributesCaseInsensitively()
    {
        const string text =
            "<target>; ANCHOR=\"../context?x=1#part\"; HREFLANG=en; hrefLang=de-CH; MEDIA=\"screen and (color), print\"; TITLE=\"A title\"; TITLE*=UTF-8'en'A%20title; TYPE=\"Application/Problem+JSON\"; X-Custom=\"raw,value\"; X*=encoded";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(link.HasAnchor, Is.True);
            Assert.That(link.Anchor,
                Is.EqualTo("../context?x=1#part"));
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { "en", "de-CH" }));
            Assert.That(link.HasMedia, Is.True);
            Assert.That(link.Media,
                Is.EqualTo("screen and (color), print"));
            Assert.That(link.HasTitle, Is.True);
            Assert.That(link.Title, Is.EqualTo("A title"));
            Assert.That(link.HasTitleExtended, Is.True);
            Assert.That(link.TitleExtended, Is.EqualTo("A title"));
            Assert.That(link.TitleExtendedCharacterSet,
                Is.EqualTo("UTF-8"));
            Assert.That(link.TitleExtendedLanguage, Is.EqualTo("en"));
            Assert.That(link.TitleExtendedRawValue,
                Is.EqualTo("UTF-8'en'A%20title"));
            Assert.That(link.HasType, Is.True);
            Assert.That(link.Type,
                Is.EqualTo("Application/Problem+JSON"));
            Assert.That(link.Parameters.Select(parameter => parameter.Kind),
                Is.EqualTo(new WebLinkParameterKind[]
                {
                    WebLinkParameterKind.Anchor,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.Media,
                    WebLinkParameterKind.Title,
                    WebLinkParameterKind.TitleExtended,
                    WebLinkParameterKind.Type,
                    WebLinkParameterKind.Extension,
                    WebLinkParameterKind.Extension
                }));
            Assert.That(link.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[]
                {
                    "ANCHOR",
                    "HREFLANG",
                    "hrefLang",
                    "MEDIA",
                    "TITLE",
                    "TITLE*",
                    "TYPE",
                    "X-Custom",
                    "X*"
                }));
            Assert.That(link.Parameters.Select(parameter => parameter.RawValue),
                Is.EqualTo(new string[]
                {
                    "../context?x=1#part",
                    "en",
                    "de-CH",
                    "screen and (color), print",
                    "A title",
                    "UTF-8'en'A%20title",
                    "Application/Problem+JSON",
                    "raw,value",
                    "encoded"
                }));
            Assert.That(link.Parameters[8].Extended, Is.True);
            Assert.That(link.Parameters[8].OriginalText,
                Is.EqualTo("; X*=encoded"));
        });
    }

    [TestCase("")]
    [TestCase("#part")]
    [TestCase("../context")]
    [TestCase("//example.test/context")]
    [TestCase("https://example.test/context")]
    public void StrictParserAcceptsEveryAnchorUriReferenceForm(string anchor)
    {
        string text = "<a>; anchor=\"" + anchor + "\"";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links[0].HasAnchor, Is.True);
            Assert.That(result.LinkSet.Links[0].Anchor,
                Is.EqualTo(anchor));
        });
    }

    [TestCase("<a>; anchor=\"a b\"", " ", 1)]
    [TestCase("<a>; anchor=\"caf\u00e9\"", "\u00e9", 1)]
    [TestCase("<a>; anchor=\"http://[bad]\"", "]", 0)]
    public void StrictParserRejectsInvalidAnchorUriReferences(
        string text,
        string marker,
        int expectedLength)
    {
        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertAttributeFailure(
            result,
            UriComponent.LinkContext,
            text.IndexOf(marker, text.IndexOf("anchor=\"") +
                "anchor=\"".Length),
            expectedLength,
            WebLinkAttributeCore.InvalidAnchorMessage);
    }

    [TestCase("anchor")]
    [TestCase("hreflang")]
    [TestCase("media")]
    [TestCase("title")]
    [TestCase("title*")]
    [TestCase("type")]
    public void StrictParserRequiresValuesForStandardAttributes(string name)
    {
        string text = "<a>; " + name;

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertAttributeFailure(
            result,
            UriComponent.LinkParameter,
            text.Length,
            0,
            WebLinkAttributeCore.MissingAttributeValueMessage);
    }

    [TestCase("text/plain")]
    [TestCase("Application/Problem+JSON")]
    [TestCase("1example/2example")]
    [TestCase("x!#$&-^_.+/y")]
    public void StrictParserAcceptsValidMediaTypeHints(string mediaType)
    {
        string text = "<a>; type=\"" + mediaType + "\"";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LinkSet.Links[0].HasType, Is.True);
            Assert.That(result.LinkSet.Links[0].Type,
                Is.EqualTo(mediaType));
        });
    }

    [TestCase("", "", 0)]
    [TestCase("text", "text", 0)]
    [TestCase("/plain", "/", 1)]
    [TestCase("text/", "text/", 0)]
    [TestCase("text/@plain", "@", 1)]
    [TestCase("text/plain/more", "/more", 1)]
    [TestCase("-text/plain", "-", 1)]
    [TestCase("text/-plain", "-plain", 1)]
    public void StrictParserRejectsInvalidMediaTypeHints(
        string mediaType,
        string marker,
        int expectedLength)
    {
        string text = "<a>; type=\"" + mediaType + "\"";
        int expectedOffset = text.IndexOf("type=\"") + "type=\"".Length;
        if (marker.Length > 0)
        {
            expectedOffset = text.IndexOf(marker, expectedOffset);
            if (expectedLength == 0)
            {
                expectedOffset = expectedOffset + marker.Length;
            }
        }

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertAttributeFailure(
            result,
            UriComponent.LinkParameter,
            expectedOffset,
            expectedLength,
            WebLinkAttributeCore.InvalidMediaTypeMessage);
    }

    [Test]
    public void StrictParserEnforcesMediaTypeNameLengthLimits()
    {
        string validName = new string('a', 127);
        string invalidName = validName + "a";
        WebLinkParseResult valid =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                ("<a>; type=\"" + validName + "/plain\"").AsSpan(),
                new WebLinkParseOptions());
        string invalidText = "<a>; type=\"" + invalidName + "/plain\"";
        WebLinkParseResult invalid =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                invalidText.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(valid.Success, Is.True);
        AssertAttributeFailure(
            invalid,
            UriComponent.LinkParameter,
            invalidText.LastIndexOf('"'),
            0,
            WebLinkAttributeCore.InvalidMediaTypeMessage);
    }

    [Test]
    public void RecoveryRetainsInvalidRawAttributesAndAppliesValidOnes()
    {
        const string text =
            "<a>; anchor=\"a b\"; hreflang=en; HREFLANG=de; media=screen; title; type=\"text/@plain\"; custom=x";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(3));
            Assert.That(link.HasAnchor, Is.False);
            Assert.That(link.Anchor, Is.Empty);
            Assert.That(link.HrefLanguages,
                Is.EqualTo(new string[] { "en", "de" }));
            Assert.That(link.HasMedia, Is.True);
            Assert.That(link.Media, Is.EqualTo("screen"));
            Assert.That(link.HasTitle, Is.False);
            Assert.That(link.HasType, Is.False);
            Assert.That(link.Parameters, Has.Length.EqualTo(7));
            Assert.That(link.Parameters.Select(parameter => parameter.Kind),
                Is.EqualTo(new WebLinkParameterKind[]
                {
                    WebLinkParameterKind.Anchor,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.HrefLang,
                    WebLinkParameterKind.Media,
                    WebLinkParameterKind.Title,
                    WebLinkParameterKind.Type,
                    WebLinkParameterKind.Extension
                }));
            Assert.That(result.Diagnostics[0].Range.Offset,
                Is.EqualTo(text.IndexOf(' ' , text.IndexOf("a b"))));
            Assert.That(result.Diagnostics[0].Component,
                Is.EqualTo(UriComponent.LinkContext));
            Assert.That(result.Diagnostics[0].Message,
                Is.EqualTo(
                    WebLinkAttributeCore.RecoveredInvalidAnchorMessage));
            Assert.That(result.Diagnostics[1].Range.Offset,
                Is.EqualTo(text.IndexOf("; type")));
            Assert.That(result.Diagnostics[1].Message,
                Is.EqualTo(
                    WebLinkAttributeCore.RecoveredMissingAttributeValueMessage));
            Assert.That(result.Diagnostics[2].Range.Offset,
                Is.EqualTo(text.IndexOf('@')));
            Assert.That(result.Diagnostics[2].Message,
                Is.EqualTo(
                    WebLinkAttributeCore.RecoveredInvalidMediaTypeMessage));
            Assert.That(result.Diagnostics.All(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Warning), Is.True);
        });
    }

    [Test]
    public void DecodedExtendedTitleAlsoPreservesItsRawValue()
    {
        const string text =
            "<a>; title=Fallback; title*=UTF-8'de'n%C3%A4chstes%20Kapitel";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(link.HasTitle, Is.True);
            Assert.That(link.Title, Is.EqualTo("Fallback"));
            Assert.That(link.HasTitleExtended, Is.True);
            Assert.That(link.TitleExtended,
                Is.EqualTo("nächstes Kapitel"));
            Assert.That(link.TitleExtendedCharacterSet,
                Is.EqualTo("UTF-8"));
            Assert.That(link.TitleExtendedLanguage, Is.EqualTo("de"));
            Assert.That(link.TitleExtendedRawValue,
                Is.EqualTo("UTF-8'de'n%C3%A4chstes%20Kapitel"));
            Assert.That(link.Parameters[1].Value,
                Is.EqualTo("nächstes Kapitel"));
            Assert.That(link.Parameters[1].RawValue,
                Is.EqualTo("UTF-8'de'n%C3%A4chstes%20Kapitel"));
        });
    }

    private static WebLinkParseOptions CreateRecoveryOptions()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Mode = WebLinkParseMode.Recovery;
        return options;
    }

    private static void AssertAttributeFailure(
        WebLinkParseResult result,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.LinkSet.Links, Is.Empty);
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidLinkParameter));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset,
                Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length,
                Is.EqualTo(length));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
            Assert.That(result.Diagnostics,
                Is.EqualTo(new UriDiagnostic[] { result.Diagnostic }));
        });
    }
}
