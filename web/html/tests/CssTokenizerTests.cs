
namespace UniversalHtml.Tests;

[TestFixture]
public class CssTokenizerTests
{
    [Test]
    public void FullTokenizationAndActiveStateHonorCancellation()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        CssTokenizerState state = new CssTokenizerState(
            "color:red;width:2px", 100, source.Token);
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                CssTokenizer.Tokenize("color:red",
                    HtmlLimits.CreateDefault(), source.Token));
            Assert.Throws<OperationCanceledException>(() => state.Run());
        });
    }

    [Test]
    public void RemovesCommentsAndCollapsesWhitespaceRunsIntoTokens()
    {
        CssTokenizeResult result = CssTokenizer.Tokenize(
            "alpha/**/beta /* gap */ \t\r\ngamma/*unfinished");
        CssToken[] tokens = result.GetTokens();

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(
                new[]
                {
                    CssTokenKind.Identifier,
                    CssTokenKind.Identifier,
                    CssTokenKind.Whitespace,
                    CssTokenKind.Whitespace,
                    CssTokenKind.Identifier,
                    CssTokenKind.End
                }));
            Assert.That(tokens[0].GetValue(), Is.EqualTo("alpha"));
            Assert.That(tokens[1].GetValue(), Is.EqualTo("beta"));
            Assert.That(tokens[4].GetValue(), Is.EqualTo("gamma"));
            Assert.That(result.GetErrorCount(), Is.EqualTo(1));
            Assert.That(result.IsSuccess(), Is.True);
        });
    }

    [Test]
    public void DecodesEscapesInIdentifiersHashesAndAtKeywords()
    {
        CssTokenizeResult result = CssTokenizer.Tokenize(
            @"--custom \63 olor #\69 d #123 @m\65 dia \1F600 face \0 bad");
        CssToken[] tokens = NonWhitespace(result);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(
                new[]
                {
                    CssTokenKind.Identifier,
                    CssTokenKind.Identifier,
                    CssTokenKind.Hash,
                    CssTokenKind.Hash,
                    CssTokenKind.AtKeyword,
                    CssTokenKind.Identifier,
                    CssTokenKind.Identifier,
                    CssTokenKind.End
                }));
            Assert.That(tokens[0].GetValue(), Is.EqualTo("--custom"));
            Assert.That(tokens[1].GetValue(), Is.EqualTo("color"));
            Assert.That(tokens[2].GetValue(), Is.EqualTo("id"));
            Assert.That(tokens[2].GetHashType(), Is.EqualTo(CssHashType.Id));
            Assert.That(tokens[3].GetValue(), Is.EqualTo("123"));
            Assert.That(tokens[3].GetHashType(),
                Is.EqualTo(CssHashType.Unrestricted));
            Assert.That(tokens[4].GetValue(), Is.EqualTo("media"));
            Assert.That(tokens[5].GetValue(), Is.EqualTo("😀face"));
            Assert.That(tokens[6].GetValue(), Is.EqualTo("�bad"));
            Assert.That(result.GetErrorCount(), Is.Zero);
        });
    }

    [Test]
    public void StringsHandleEscapesContinuationsAndNewlineRecovery()
    {
        CssTokenizeResult result = CssTokenizer.Tokenize(
            "\"a\\26 b\" 'line\\\r\ncontinued' 'bad\nnext");
        CssToken[] tokens = NonWhitespace(result);

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0].GetKind(), Is.EqualTo(CssTokenKind.String));
            Assert.That(tokens[0].GetValue(), Is.EqualTo("a&b"));
            Assert.That(tokens[1].GetKind(), Is.EqualTo(CssTokenKind.String));
            Assert.That(tokens[1].GetValue(), Is.EqualTo("linecontinued"));
            Assert.That(tokens[2].GetKind(),
                Is.EqualTo(CssTokenKind.BadString));
            Assert.That(tokens[2].GetValue(), Is.EqualTo("bad"));
            Assert.That(tokens[3].GetKind(),
                Is.EqualTo(CssTokenKind.Identifier));
            Assert.That(tokens[3].GetValue(), Is.EqualTo("next"));
            Assert.That(result.GetErrorCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void NumbersPercentagesAndDimensionsPreserveNumericMetadata()
    {
        CssToken[] tokens = NonWhitespace(CssTokenizer.Tokenize(
            "-12 +.5 6.02e23 10% 2px -3.5\\50 X 1e+"));

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(
                new[]
                {
                    CssTokenKind.Number,
                    CssTokenKind.Number,
                    CssTokenKind.Number,
                    CssTokenKind.Percentage,
                    CssTokenKind.Dimension,
                    CssTokenKind.Dimension,
                    CssTokenKind.Dimension,
                    CssTokenKind.Delimiter,
                    CssTokenKind.End
                }));
            Assert.That(tokens[0].GetNumber(), Is.EqualTo(-12));
            Assert.That(tokens[0].GetNumericType(),
                Is.EqualTo(CssNumericType.Integer));
            Assert.That(tokens[1].GetNumber(), Is.EqualTo(0.5));
            Assert.That(tokens[1].GetNumericType(),
                Is.EqualTo(CssNumericType.Number));
            Assert.That(tokens[2].GetNumber(), Is.EqualTo(6.02e23));
            Assert.That(tokens[3].GetNumberRepresentation(),
                Is.EqualTo("10"));
            Assert.That(tokens[4].GetUnit(), Is.EqualTo("px"));
            Assert.That(tokens[5].GetUnit(), Is.EqualTo("PX"));
            Assert.That(tokens[6].GetNumberRepresentation(),
                Is.EqualTo("1"));
            Assert.That(tokens[6].GetUnit(), Is.EqualTo("e"));
            Assert.That(tokens[7].GetValue(), Is.EqualTo("+"));
        });
    }

    [Test]
    public void EmitsDedicatedPunctuationAndLegacyCommentBoundaryTokens()
    {
        CssToken[] tokens = NonWhitespace(CssTokenizer.Tokenize(
            ":;,[](){} <!-- --> . @1"));

        Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(new[]
        {
            CssTokenKind.Colon,
            CssTokenKind.Semicolon,
            CssTokenKind.Comma,
            CssTokenKind.OpenSquare,
            CssTokenKind.CloseSquare,
            CssTokenKind.OpenParenthesis,
            CssTokenKind.CloseParenthesis,
            CssTokenKind.OpenCurly,
            CssTokenKind.CloseCurly,
            CssTokenKind.Cdo,
            CssTokenKind.Cdc,
            CssTokenKind.Delimiter,
            CssTokenKind.Delimiter,
            CssTokenKind.Number,
            CssTokenKind.End
        }));
    }

    [Test]
    public void DistinguishesFunctionsQuotedUrlsAndUnquotedUrls()
    {
        CssToken[] tokens = NonWhitespace(CssTokenizer.Tokenize(
            @"rgb(1) url(icon\2E svg) URL(   ""quoted.svg"" )"));

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(
                new[]
                {
                    CssTokenKind.Function,
                    CssTokenKind.Number,
                    CssTokenKind.CloseParenthesis,
                    CssTokenKind.Url,
                    CssTokenKind.Function,
                    CssTokenKind.String,
                    CssTokenKind.CloseParenthesis,
                    CssTokenKind.End
                }));
            Assert.That(tokens[0].GetValue(), Is.EqualTo("rgb"));
            Assert.That(tokens[3].GetValue(), Is.EqualTo("icon.svg"));
            Assert.That(tokens[3].GetRepresentation(),
                Is.EqualTo(@"url(icon\2E svg)"));
            Assert.That(tokens[4].GetValue(), Is.EqualTo("URL"));
            Assert.That(tokens[4].GetRepresentation(), Is.EqualTo("URL(  "));
            Assert.That(tokens[5].GetValue(), Is.EqualTo("quoted.svg"));
        });
    }

    [Test]
    public void BadUrlsConsumeTheirRemnantsAndResumeAfterClosingParenthesis()
    {
        CssTokenizeResult result = CssTokenizer.Tokenize(
            "url(bad\"part) safe");
        CssToken[] tokens = NonWhitespace(result);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(
                new[]
                {
                    CssTokenKind.BadUrl,
                    CssTokenKind.Identifier,
                    CssTokenKind.End
                }));
            Assert.That(tokens[1].GetValue(), Is.EqualTo("safe"));
            Assert.That(result.GetErrorCount(), Is.EqualTo(1));
            Assert.That(result.IsSuccess(), Is.True);
        });
    }

    [Test]
    public void PreprocessingNormalizesScalarsAndPreservesOriginalSpans()
    {
        CssTokenizeResult result = CssTokenizer.Tokenize(
            "\0name\r\n\uD800bad 😀good");
        CssToken[] tokens = result.GetTokens();

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0].GetValue(), Is.EqualTo("�name"));
            Assert.That(tokens[1].GetKind(),
                Is.EqualTo(CssTokenKind.Whitespace));
            Assert.That(tokens[1].GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(2));
            Assert.That(tokens[1].GetSourceSpan()
                .GetNormalizedCharacterLength(), Is.EqualTo(1));
            Assert.That(tokens[2].GetValue(), Is.EqualTo("�bad"));
            Assert.That(tokens[4].GetValue(), Is.EqualTo("😀good"));
            Assert.That(tokens[4].GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(6));
        });
    }

    [Test]
    public void StopsBeforeExceedingTheConfiguredTokenLimit()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumCssTokens(2);

        CssTokenizeResult result = CssTokenizer.Tokenize("one two three",
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetTokens(), Has.Length.EqualTo(2));
            Assert.That(result.GetTokens()[0].GetKind(),
                Is.EqualTo(CssTokenKind.Identifier));
            Assert.That(result.GetTokens()[1].GetKind(),
                Is.EqualTo(CssTokenKind.Whitespace));
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
        });
    }

    [Test]
    public void RejectsInputBeyondTheConfiguredCharacterLimitBeforeScanning()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(3);

        CssTokenizeResult result = CssTokenizer.Tokenize("four", limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetTokens(), Is.Empty);
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetErrorCount(), Is.Zero);
        });
    }

    [Test]
    public void ResultOwnsItsTokenArrayAndRejectsInvalidArguments()
    {
        CssTokenizeResult result = CssTokenizer.Tokenize("name");
        CssToken[] first = result.GetTokens();
        first[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.GetTokens()[0].GetValue(), Is.EqualTo("name"));
            Assert.That(() => CssTokenizer.Tokenize(null),
                Throws.ArgumentNullException);
            Assert.That(() => CssTokenizer.Tokenize("name", (HtmlLimits)null),
                Throws.ArgumentNullException);
        });
    }

    static CssToken[] NonWhitespace(CssTokenizeResult result)
    {
        return result.GetTokens().Where(token =>
            token.GetKind() != CssTokenKind.Whitespace).ToArray();
    }
}
