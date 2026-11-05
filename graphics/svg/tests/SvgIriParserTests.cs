namespace svg.Tests;

public class SvgIriParserTests
{
    [Test]
    public void IriParsingPreservesUnicodeRelativeReferencesFragmentsAndOffsets()
    {
        string input = "  images/日本.svg?mode=full#icon  ";
        SvgIriParseResult result = new SvgIriParser().ParseIri(input, 128);

        Assert.Multiple(() =>
        {
            AssertSuccess(result);
            Assert.That(result.Reference.Value, Is.EqualTo("images/日本.svg?mode=full#icon"));
            Assert.That(result.Reference.HasFragment, Is.True);
            Assert.That(result.Reference.Fragment, Is.EqualTo("icon"));
            Assert.That(result.Reference.IsFragmentOnly, Is.False);
            Assert.That(result.Reference.IsUrlFunction, Is.False);
            Assert.That(result.Reference.StartOffset, Is.EqualTo(2));
            Assert.That(result.Reference.EndOffset, Is.EqualTo(input.Length - 2));
            Assert.That(result.Reference.Source, Is.EqualTo(input));
            Assert.That(result.Reference.ToText(), Is.EqualTo("images/日本.svg?mode=full#icon"));
        });
    }

    [Test]
    public void IriAndFragmentParsingValidatePercentEscapesCharactersAndFragmentShape()
    {
        SvgIriParseResult encoded = new SvgIriParser().ParseIri("folder/my%20file.svg", 64);
        SvgIriParseResult badPercent = new SvgIriParser().ParseIri("a%2G", 64);
        SvgIriParseResult space = new SvgIriParser().ParseIri("a b", 64);
        SvgIriParseResult fragment = new SvgIriParser().ParseFragment(" #target ", 64);
        SvgIriParseResult emptyFragment = new SvgIriParser().ParseFragment("#", 64);
        SvgIriParseResult notFragment = new SvgIriParser().ParseFragment("file.svg#x", 64);

        Assert.Multiple(() =>
        {
            AssertSuccess(encoded);
            Assert.That(badPercent.Error, Is.EqualTo(SvgIriParseError.InvalidPercentEscape));
            Assert.That(badPercent.ErrorOffset, Is.EqualTo(1));
            Assert.That(space.Error, Is.EqualTo(SvgIriParseError.InvalidCharacter));
            AssertSuccess(fragment);
            Assert.That(fragment.Reference.Fragment, Is.EqualTo("target"));
            Assert.That(fragment.Reference.IsFragmentOnly, Is.True);
            Assert.That(emptyFragment.Error, Is.EqualTo(SvgIriParseError.InvalidFragment));
            Assert.That(notFragment.Error, Is.EqualTo(SvgIriParseError.InvalidFragment));
        });
    }

    [Test]
    public void UrlFunctionsDecodeQuotedAndUnquotedCssEscapes()
    {
        SvgIriParseResult fragment = new SvgIriParser().ParseUrl(" URL( #gr\\61 dient ) ", 64);
        SvgIriParseResult quoted = new SvgIriParser().ParseUrl("url(\"folder/my file.svg#tile\")", 64);
        SvgIriParseResult escapedClose = new SvgIriParser().ParseUrl("url(foo\\29 bar)", 64);

        Assert.Multiple(() =>
        {
            AssertSuccess(fragment);
            Assert.That(fragment.Reference.Value, Is.EqualTo("#gradient"));
            Assert.That(fragment.Reference.Fragment, Is.EqualTo("gradient"));
            Assert.That(fragment.Reference.IsUrlFunction, Is.True);
            AssertSuccess(quoted);
            Assert.That(quoted.Reference.Value, Is.EqualTo("folder/my file.svg#tile"));
            AssertSuccess(escapedClose);
            Assert.That(escapedClose.Reference.Value, Is.EqualTo("foo)bar"));
        });
    }

    [Test]
    public void UrlFunctionsRejectWrongNamesEmptyValuesMalformedEscapesAndTrailingText()
    {
        SvgIriParseResult wrong = new SvgIriParser().ParseUrl("src(#x)", 32);
        SvgIriParseResult empty = new SvgIriParser().ParseUrl("url()", 32);
        SvgIriParseResult whitespace = new SvgIriParser().ParseUrl("url(foo bar)", 32);
        SvgIriParseResult percent = new SvgIriParser().ParseUrl("url(foo%2G)", 32);
        SvgIriParseResult missing = new SvgIriParser().ParseUrl("url(#x", 32);
        SvgIriParseResult trailing = new SvgIriParser().ParseUrl("url(#x) red", 32);

        Assert.Multiple(() =>
        {
            Assert.That(wrong.Error, Is.EqualTo(SvgIriParseError.InvalidFunction));
            Assert.That(empty.Error, Is.EqualTo(SvgIriParseError.ExpectedValue));
            Assert.That(whitespace.Error, Is.EqualTo(SvgIriParseError.InvalidCharacter));
            Assert.That(percent.Error, Is.EqualTo(SvgIriParseError.InvalidPercentEscape));
            Assert.That(missing.Error, Is.EqualTo(SvgIriParseError.ExpectedClose));
            Assert.That(trailing.Error, Is.EqualTo(SvgIriParseError.TrailingCharacters));
        });
    }

    [Test]
    public void FunctionValueParsingHandlesEscapedNamesNestedCallsQuotesAndSourceOffsets()
    {
        string input = "  f\\6f o(calc(1 + fn(2)), \"x)\")  ";
        SvgFunctionValueParseResult result = new SvgFunctionValueParser().Parse(input, 32, 128);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Function.Name, Is.EqualTo("foo"));
            Assert.That(result.Function.Content, Is.EqualTo("calc(1 + fn(2)), \"x)\""));
            Assert.That(result.Function.StartOffset, Is.EqualTo(2));
            Assert.That(result.Function.ContentOffset, Is.EqualTo(input.IndexOf('(') + 1));
            Assert.That(result.Function.EndOffset, Is.EqualTo(input.Length - 2));
            Assert.That(result.Function.ToText(), Is.EqualTo("foo(calc(1 + fn(2)), \"x)\")"));
        });
    }

    [Test]
    public void FunctionValueParsingReportsNameContentCloseAndTrailingFailures()
    {
        SvgFunctionValueParseResult digit = new SvgFunctionValueParser().Parse("1bad(x)", 16, 16);
        SvgFunctionValueParseResult loneHyphen = new SvgFunctionValueParser().Parse("-(x)", 16, 16);
        SvgFunctionValueParseResult unicode = new SvgFunctionValueParser().Parse("滤镜(x)", 16, 16);
        SvgFunctionValueParseResult noOpen = new SvgFunctionValueParser().Parse("name x", 16, 16);
        SvgFunctionValueParseResult noClose = new SvgFunctionValueParser().Parse("name(x", 16, 16);
        SvgFunctionValueParseResult nameLimit = new SvgFunctionValueParser().Parse("longname(x)", 4, 16);
        SvgFunctionValueParseResult contentLimit = new SvgFunctionValueParser().Parse("name(12345)", 16, 4);
        SvgFunctionValueParseResult trailing = new SvgFunctionValueParser().Parse("name(x) y", 16, 16);

        Assert.Multiple(() =>
        {
            Assert.That(digit.Error, Is.EqualTo(SvgIriParseError.InvalidFunction));
            Assert.That(loneHyphen.Error, Is.EqualTo(SvgIriParseError.InvalidFunction));
            Assert.That(unicode.Success, Is.True);
            Assert.That(noOpen.Error, Is.EqualTo(SvgIriParseError.InvalidFunction));
            Assert.That(noClose.Error, Is.EqualTo(SvgIriParseError.ExpectedClose));
            Assert.That(nameLimit.Error, Is.EqualTo(SvgIriParseError.TokenTooLong));
            Assert.That(contentLimit.Error, Is.EqualTo(SvgIriParseError.TokenTooLong));
            Assert.That(trailing.Error, Is.EqualTo(SvgIriParseError.TrailingCharacters));
        });
    }

    [Test]
    public void MediaTypesParseTokensQuotedParametersEscapesAndCanonicalText()
    {
        string input = " Application/SVG+XML ; Charset = utf-8 ; profile=\"print mode\" ; note=\"a\\\"b\" ";
        SvgMediaTypeParseResult result = new SvgMediaTypeParser().Parse(input, 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.MediaType.Type, Is.EqualTo("application"));
            Assert.That(result.MediaType.Subtype, Is.EqualTo("svg+xml"));
            Assert.That(result.MediaType.Parameters, Has.Count.EqualTo(3));
            Assert.That(result.MediaType.Parameters[0].Name, Is.EqualTo("charset"));
            Assert.That(result.MediaType.Parameters[0].Value, Is.EqualTo("utf-8"));
            Assert.That(result.MediaType.Parameters[1].Value, Is.EqualTo("print mode"));
            Assert.That(result.MediaType.Parameters[2].Value, Is.EqualTo("a\"b"));
            Assert.That(result.MediaType.Parameters[0].StartOffset, Is.EqualTo(input.IndexOf(';')));
            Assert.That(result.MediaType.ToText(), Is.EqualTo("application/svg+xml; charset=utf-8; profile=\"print mode\"; note=\"a\\\"b\""));
        });
    }

    [Test]
    public void MediaTypesRejectMissingPartsBadParametersAndBounds()
    {
        SvgMediaTypeParseResult noSubtype = new SvgMediaTypeParser().Parse("image/", 2, 16);
        SvgMediaTypeParseResult noSlash = new SvgMediaTypeParser().Parse("image", 2, 16);
        SvgMediaTypeParseResult noEquals = new SvgMediaTypeParser().Parse("image/png; charset", 2, 16);
        SvgMediaTypeParseResult unclosed = new SvgMediaTypeParser().Parse("text/plain; note=\"x", 2, 16);
        SvgMediaTypeParseResult parameterLimit = new SvgMediaTypeParser().Parse("text/plain;a=1;b=2", 1, 16);
        SvgMediaTypeParseResult tokenLimit = new SvgMediaTypeParser().Parse("application/json", 2, 4);

        Assert.Multiple(() =>
        {
            Assert.That(noSubtype.Error, Is.EqualTo(SvgIriParseError.InvalidMediaType));
            Assert.That(noSlash.Error, Is.EqualTo(SvgIriParseError.InvalidMediaType));
            Assert.That(noEquals.Error, Is.EqualTo(SvgIriParseError.InvalidMediaType));
            Assert.That(unclosed.Error, Is.EqualTo(SvgIriParseError.ExpectedClose));
            Assert.That(parameterLimit.Error, Is.EqualTo(SvgIriParseError.ItemLimitExceeded));
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgIriParseError.TokenTooLong));
        });
    }

    [Test]
    public void LanguageListsNormalizeTagsAndPreserveEverySourceSpan()
    {
        string input = " en-US, FR , zh-Hant-TW ";
        SvgTextTokenListParseResult result = new SvgLanguageListParser().Parse(input, 5, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Items.Select(item => item.Value), Is.EqualTo(new[] { "en-us", "fr", "zh-hant-tw" }));
            Assert.That(result.Items[0].Source, Is.EqualTo("en-US"));
            Assert.That(result.Items[0].StartOffset, Is.EqualTo(1));
            Assert.That(result.Items[0].EndOffset, Is.EqualTo(6));
            Assert.That(result.Items[2].StartOffset, Is.EqualTo(input.IndexOf("zh", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void LanguageListsRejectMissingCommasMalformedTagsAndLimitsWithPrefixes()
    {
        SvgTextTokenListParseResult missingComma = new SvgLanguageListParser().Parse("en fr", 4, 16);
        SvgTextTokenListParseResult underscore = new SvgLanguageListParser().Parse("en_US", 4, 16);
        SvgTextTokenListParseResult empty = new SvgLanguageListParser().Parse("en,,fr", 4, 16);
        SvgTextTokenListParseResult itemLimit = new SvgLanguageListParser().Parse("en,fr,de", 2, 16);
        SvgTextTokenListParseResult tokenLimit = new SvgLanguageListParser().Parse("abcdefghij", 4, 5);

        Assert.Multiple(() =>
        {
            Assert.That(missingComma.Error, Is.EqualTo(SvgIriParseError.InvalidSeparator));
            Assert.That(missingComma.Items, Has.Count.EqualTo(1));
            Assert.That(underscore.Error, Is.EqualTo(SvgIriParseError.InvalidLanguageTag));
            Assert.That(empty.Error, Is.EqualTo(SvgIriParseError.InvalidLanguageTag));
            Assert.That(itemLimit.Error, Is.EqualTo(SvgIriParseError.ItemLimitExceeded));
            Assert.That(itemLimit.Items, Has.Count.EqualTo(2));
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgIriParseError.TokenTooLong));
        });
    }

    [Test]
    public void FeatureStringsParseWhitespaceListsAndRetainSourceOffsets()
    {
        string first = "http://www.w3.org/TR/SVG11/feature#Shape";
        string second = "http://example.test/ext%20feature";
        string input = " " + first + "\n" + second + " ";
        SvgTextTokenListParseResult result = new SvgFeatureStringParser().Parse(input, 4, 128);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Items, Has.Count.EqualTo(2));
            Assert.That(result.Items[0].Value, Is.EqualTo(first));
            Assert.That(result.Items[0].StartOffset, Is.EqualTo(1));
            Assert.That(result.Items[1].StartOffset, Is.EqualTo(input.IndexOf(second, StringComparison.Ordinal)));
        });
    }

    [Test]
    public void FeatureStringsAndIriParsersEnforceInvalidInputAndConfiguredBounds()
    {
        SvgTextTokenListParseResult badPercent = new SvgFeatureStringParser().Parse("http://x/%zz", 2, 32);
        SvgTextTokenListParseResult itemLimit = new SvgFeatureStringParser().Parse("one two", 1, 32);
        SvgTextTokenListParseResult tokenLimit = new SvgFeatureStringParser().Parse("feature", 2, 3);
        SvgIriParseResult nullIri = new SvgIriParser().ParseIri(null, 10);
        SvgIriParseResult emptyIri = new SvgIriParser().ParseIri(" ", 10);
        SvgIriParseResult iriLimit = new SvgIriParser().ParseIri("abcdef", 3);

        Assert.Multiple(() =>
        {
            Assert.That(badPercent.Error, Is.EqualTo(SvgIriParseError.InvalidFeatureString));
            Assert.That(itemLimit.Error, Is.EqualTo(SvgIriParseError.ItemLimitExceeded));
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgIriParseError.TokenTooLong));
            Assert.That(nullIri.Error, Is.EqualTo(SvgIriParseError.InvalidArgument));
            Assert.That(emptyIri.Error, Is.EqualTo(SvgIriParseError.ExpectedValue));
            Assert.That(iriLimit.Error, Is.EqualTo(SvgIriParseError.TokenTooLong));
        });
    }

    static void AssertSuccess(SvgIriParseResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgIriParseError.None));
        Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        Assert.That(result.Reference, Is.Not.Null);
        Assert.That(result.Reference.IsValid, Is.True);
    }
}
