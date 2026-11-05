namespace uri.Tests;

public class WebLinkExtendedValueCoreTests
{
    [TestCase(
        "UTF-8'en'%C2%A3%20rates",
        "£ rates",
        "UTF-8",
        "en")]
    [TestCase(
        "utf-8''%c2%a3%20and%20%e2%82%ac%20rates",
        "£ and € rates",
        "utf-8",
        "")]
    [TestCase(
        "UtF-8'de'n%C3%A4chstes%20Kapitel",
        "nächstes Kapitel",
        "UtF-8",
        "de")]
    [TestCase(
        "UTF-8''%F0%9F%98%80",
        "😀",
        "UTF-8",
        "")]
    [TestCase(
        "UTF-8'en'AZaz09!#$&+-.^_`|~",
        "AZaz09!#$&+-.^_`|~",
        "UTF-8",
        "en")]
    [TestCase("UTF-8''", "", "UTF-8", "")]
    public void StrictParserDecodesUtf8ExtendedTitles(
        string rawValue,
        string decodedValue,
        string charset,
        string language)
    {
        string text = "<a>; title*=" + rawValue;

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        Assert.That(result.Success, Is.True);
        WebLink link = result.LinkSet.Links[0];
        WebLinkParameter parameter = link.Parameters[0];
        Assert.Multiple(() =>
        {
            Assert.That(link.HasTitleExtended, Is.True);
            Assert.That(link.TitleExtended, Is.EqualTo(decodedValue));
            Assert.That(link.TitleExtendedCharacterSet,
                Is.EqualTo(charset));
            Assert.That(link.TitleExtendedLanguage,
                Is.EqualTo(language));
            Assert.That(link.TitleExtendedRawValue,
                Is.EqualTo(rawValue));
            Assert.That(parameter.Kind,
                Is.EqualTo(WebLinkParameterKind.TitleExtended));
            Assert.That(parameter.Extended, Is.True);
            Assert.That(parameter.Value, Is.EqualTo(decodedValue));
            Assert.That(parameter.RawValue, Is.EqualTo(rawValue));
            Assert.That(parameter.CharacterSet, Is.EqualTo(charset));
            Assert.That(parameter.Language, Is.EqualTo(language));
            Assert.That(parameter.OriginalText,
                Is.EqualTo("; title*=" + rawValue));
        });
    }

    [Test]
    public void AsteriskExtensionNamesAreNotAutomaticallyDecoded()
    {
        const string rawValue = "not-an-ext-value%GG";
        const string text = "<a>; example*=not-an-ext-value%GG";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        WebLinkParameter parameter = result.LinkSet.Links[0].Parameters[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(parameter.Kind,
                Is.EqualTo(WebLinkParameterKind.Extension));
            Assert.That(parameter.Extended, Is.True);
            Assert.That(parameter.Value, Is.EqualTo(rawValue));
            Assert.That(parameter.RawValue, Is.EqualTo(rawValue));
            Assert.That(parameter.CharacterSet, Is.Empty);
            Assert.That(parameter.Language, Is.Empty);
        });
    }

    [TestCase("UTF-8", "UTF-8", 0)]
    [TestCase("''value", "''", 0)]
    [TestCase("UTF-8'en", "UTF-8'en", 0)]
    [TestCase("UTF.8''value", ".", 1)]
    [TestCase("UTF-8''a*b", "*", 1)]
    [TestCase("UTF-8''value%", "%", 1)]
    [TestCase("UTF-8''value%2", "%2", 1)]
    [TestCase("UTF-8''value%GG", "G", 1)]
    [TestCase("UTF-8''a'b", "'a'b", 1)]
    public void StrictParserRejectsMalformedExtendedValueSyntax(
        string rawValue,
        string marker,
        int expectedLength)
    {
        string text = "<a>; title*=" + rawValue;
        int valueOffset = text.IndexOf(rawValue);
        int expectedOffset;
        if (expectedLength == 0)
        {
            expectedOffset = valueOffset + rawValue.Length;
            if (rawValue.Length > 0 && rawValue[0] == '\'')
            {
                expectedOffset = valueOffset;
            }
        }
        else
        {
            expectedOffset = text.IndexOf(marker, valueOffset);
            if (marker == "'a'b")
            {
                expectedOffset = text.LastIndexOf('\'');
            }
        }

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertExtendedFailure(
            result,
            UriDiagnosticCode.InvalidExtendedParameter,
            expectedOffset,
            expectedLength,
            WebLinkExtendedValueCore.MalformedExtendedValueMessage);
    }

    [Test]
    public void StrictParserRejectsQuotedExtendedValues()
    {
        const string rawValue = "UTF-8''value";
        const string text = "<a>; title*=\"UTF-8''value\"";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertExtendedFailure(
            result,
            UriDiagnosticCode.InvalidExtendedParameter,
            text.IndexOf(rawValue),
            rawValue.Length,
            WebLinkExtendedValueCore.MalformedExtendedValueMessage);
    }

    [Test]
    public void StrictParserReportsUnsupportedCharsetAndRetainsItsRange()
    {
        const string text =
            "<a>; title*=ISO-8859-1'en'%A3%20rates";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertExtendedFailure(
            result,
            UriDiagnosticCode.UnsupportedCharacterSet,
            text.IndexOf("ISO-8859-1"),
            "ISO-8859-1".Length,
            WebLinkExtendedValueCore.UnsupportedCharacterSetMessage);
    }

    [TestCase("UTF-8''%C3%28", "%28", 3)]
    [TestCase("UTF-8''%C0%AF", "%C0", 3)]
    [TestCase("UTF-8''%F0%9F%98", "%F0%9F%98", 0)]
    public void StrictParserRejectsInvalidUtf8Octets(
        string rawValue,
        string marker,
        int expectedLength)
    {
        string text = "<a>; title*=" + rawValue;
        int expectedOffset = text.IndexOf(marker);
        if (expectedLength == 0)
        {
            expectedOffset = expectedOffset + marker.Length;
        }

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        AssertExtendedFailure(
            result,
            UriDiagnosticCode.InvalidExtendedParameter,
            expectedOffset,
            expectedLength,
            WebLinkExtendedValueCore.InvalidExtendedUtf8Message);
    }

    [Test]
    public void RecoveryKeepsMalformedRawValuesAndFallsBackToPlainTitles()
    {
        const string text =
            "<a>; title=fallback-a; title*=ISO-8859-1'en'%A3, <b>; title=fallback-b; title*=UTF-8'de'bad%GG, <c>; title*=UTF-8'en'good%20title";

        WebLinkParseResult result =
            WebLinkRecoveryParserCore.ParseRecoveryFieldValueSpan(
                text.AsSpan(),
                CreateRecoveryOptions());

        Assert.That(result.Success, Is.True);
        WebLink first = result.LinkSet.Links[0];
        WebLink second = result.LinkSet.Links[1];
        WebLink third = result.LinkSet.Links[2];
        Assert.Multiple(() =>
        {
            Assert.That(result.Recovered, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(2));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(UriDiagnosticCode.UnsupportedCharacterSet));
            Assert.That(result.Diagnostics[0].Message,
                Is.EqualTo(
                    WebLinkExtendedValueCore.RecoveredUnsupportedCharacterSetMessage));
            Assert.That(result.Diagnostics[1].Code,
                Is.EqualTo(UriDiagnosticCode.InvalidExtendedParameter));
            Assert.That(result.Diagnostics[1].Message,
                Is.EqualTo(
                    WebLinkExtendedValueCore.RecoveredMalformedExtendedValueMessage));
            Assert.That(first.Title, Is.EqualTo("fallback-a"));
            Assert.That(first.HasTitleExtended, Is.False);
            Assert.That(first.Parameters[1].Value,
                Is.EqualTo("ISO-8859-1'en'%A3"));
            Assert.That(first.Parameters[1].RawValue,
                Is.EqualTo("ISO-8859-1'en'%A3"));
            Assert.That(first.Parameters[1].CharacterSet,
                Is.EqualTo("ISO-8859-1"));
            Assert.That(first.Parameters[1].Language,
                Is.EqualTo("en"));
            Assert.That(second.Title, Is.EqualTo("fallback-b"));
            Assert.That(second.HasTitleExtended, Is.False);
            Assert.That(second.Parameters[1].RawValue,
                Is.EqualTo("UTF-8'de'bad%GG"));
            Assert.That(second.Parameters[1].CharacterSet,
                Is.EqualTo("UTF-8"));
            Assert.That(second.Parameters[1].Language,
                Is.EqualTo("de"));
            Assert.That(third.HasTitleExtended, Is.True);
            Assert.That(third.TitleExtended,
                Is.EqualTo("good title"));
        });
    }

    [Test]
    public void IgnoredExtendedTitleDuplicatesAreNotDecodedOrValidated()
    {
        const string text =
            "<a>; title*=UTF-8''good; TITLE*=ISO-8859-1''%A3";

        WebLinkParseResult result =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());

        WebLink link = result.LinkSet.Links[0];
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(UriDiagnosticCode.DuplicateParameter));
            Assert.That(link.TitleExtended, Is.EqualTo("good"));
            Assert.That(link.Parameters[1].Ignored, Is.True);
            Assert.That(link.Parameters[1].Value,
                Is.EqualTo("ISO-8859-1''%A3"));
            Assert.That(link.Parameters[1].RawValue,
                Is.EqualTo("ISO-8859-1''%A3"));
            Assert.That(link.Parameters[1].CharacterSet, Is.Empty);
        });
    }

    [TestCase(
        "£ rates",
        "en",
        "UTF-8'en'%C2%A3%20rates")]
    [TestCase(
        "€ and 😀",
        "",
        "UTF-8''%E2%82%AC%20and%20%F0%9F%98%80")]
    [TestCase(
        "AZaz09!#$&+-.^_`|~",
        "de",
        "UTF-8'de'AZaz09!#$&+-.^_`|~")]
    public void EncoderProducesUtf8AttrCharExtendedValues(
        string value,
        string language,
        string expected)
    {
        PercentEncodeResult result =
            WebLinkExtendedValueCore.EncodeUtf8Value(
                value,
                language,
                expected.Length);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void EncoderReportsOutputLimitAndInvalidUnicode()
    {
        const string expected = "UTF-8'en'%C2%A3";
        PercentEncodeResult limited =
            WebLinkExtendedValueCore.EncodeUtf8Value(
                "£",
                "en",
                expected.Length - 1);
        PercentEncodeResult invalid =
            WebLinkExtendedValueCore.EncodeUtf8Value(
                "\ud800",
                "",
                100);

        Assert.That(limited.Diagnostic, Is.Not.Null);
        Assert.That(invalid.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(limited.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.LimitExceeded));
            Assert.That(limited.Diagnostic.Component,
                Is.EqualTo(UriComponent.Output));
            Assert.That(limited.Diagnostic.Range.Offset,
                Is.EqualTo(expected.Length - 1));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(invalid.Diagnostic.Code,
                Is.EqualTo(UriDiagnosticCode.InvalidUnicode));
            Assert.That(invalid.Diagnostic.Component,
                Is.EqualTo(UriComponent.ExtendedParameter));
            Assert.That(invalid.Diagnostic.Range.Offset, Is.Zero);
            Assert.That(invalid.Diagnostic.Range.Length, Is.EqualTo(1));
        });
    }

    private static WebLinkParseOptions CreateRecoveryOptions()
    {
        WebLinkParseOptions options = new WebLinkParseOptions();
        options.Mode = WebLinkParseMode.Recovery;
        return options;
    }

    private static void AssertExtendedFailure(
        WebLinkParseResult result,
        UriDiagnosticCode code,
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
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity,
                Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.ExtendedParameter));
            Assert.That(result.Diagnostic.Range.Offset,
                Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length,
                Is.EqualTo(length));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
        });
    }
}
