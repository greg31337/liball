namespace svg.Tests;

public class SvgCssTokenizerTests
{
    [Test]
    public void StructuralTokensCommentsAndRawSourceArePreserved()
    {
        string input = "/*lead*/ @media #main #123 .item { color: rgb(1,2,3); }";

        SvgCssTokenizeResult result = Tokenize(input);
        List<SvgCssToken> significant = result.Tokens.Where(token => token.Kind != SvgCssTokenKind.Whitespace).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(string.Concat(result.Tokens.Select(token => token.RawText)), Is.EqualTo(input));
            Assert.That(significant.Select(token => token.Kind), Is.EqualTo(new[]
            {
                SvgCssTokenKind.Comment,
                SvgCssTokenKind.AtKeyword,
                SvgCssTokenKind.Hash,
                SvgCssTokenKind.Hash,
                SvgCssTokenKind.Delimiter,
                SvgCssTokenKind.Ident,
                SvgCssTokenKind.OpenCurly,
                SvgCssTokenKind.Ident,
                SvgCssTokenKind.Colon,
                SvgCssTokenKind.Function,
                SvgCssTokenKind.Number,
                SvgCssTokenKind.Comma,
                SvgCssTokenKind.Number,
                SvgCssTokenKind.Comma,
                SvgCssTokenKind.Number,
                SvgCssTokenKind.CloseParenthesis,
                SvgCssTokenKind.Semicolon,
                SvgCssTokenKind.CloseCurly
            }));
            Assert.That(significant[0].Value, Is.EqualTo("lead"));
            Assert.That(significant[1].Value, Is.EqualTo("media"));
            Assert.That(significant[2].Value, Is.EqualTo("main"));
            Assert.That(significant[2].HashType, Is.EqualTo(SvgCssHashType.Id));
            Assert.That(significant[3].HashType, Is.EqualTo(SvgCssHashType.Unrestricted));
            Assert.That(significant[9].Value, Is.EqualTo("rgb"));
        });
    }

    [Test]
    public void NumbersDimensionsAndPercentagesExposeRepresentationsAndValues()
    {
        string input = "-1.5e2px +.25 50% 12 1e3 6\\70 x";

        SvgCssTokenizeResult result = Tokenize(input);
        List<SvgCssToken> values = result.Tokens.Where(token => token.Kind != SvgCssTokenKind.Whitespace).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(token => token.Kind), Is.EqualTo(new[]
            {
                SvgCssTokenKind.Dimension,
                SvgCssTokenKind.Number,
                SvgCssTokenKind.Percentage,
                SvgCssTokenKind.Number,
                SvgCssTokenKind.Number,
                SvgCssTokenKind.Dimension
            }));
            Assert.That(values[0].Value, Is.EqualTo("-1.5e2"));
            Assert.That(values[0].NumberValue, Is.EqualTo(-150));
            Assert.That(values[0].NumberType, Is.EqualTo(SvgCssNumberType.Number));
            Assert.That(values[0].Unit, Is.EqualTo("px"));
            Assert.That(values[1].NumberValue, Is.EqualTo(0.25));
            Assert.That(values[2].NumberValue, Is.EqualTo(50));
            Assert.That(values[3].NumberType, Is.EqualTo(SvgCssNumberType.Integer));
            Assert.That(values[4].NumberValue, Is.EqualTo(1000));
            Assert.That(values[4].NumberType, Is.EqualTo(SvgCssNumberType.Number));
            Assert.That(values[5].RawText, Is.EqualTo("6\\70 x"));
            Assert.That(values[5].Unit, Is.EqualTo("px"));
        });
    }

    [Test]
    public void EscapesDecodeInIdentifiersAtKeywordsHashesAndPreprocessedNulls()
    {
        string input = "\\66 oo @\\6d edia #\\31 23 a\0b";

        SvgCssTokenizeResult result = Tokenize(input);
        List<SvgCssToken> values = result.Tokens.Where(token => token.Kind != SvgCssTokenKind.Whitespace).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParseErrorCount, Is.EqualTo(1));
            Assert.That(values.Select(token => token.Kind), Is.EqualTo(new[]
            {
                SvgCssTokenKind.Ident,
                SvgCssTokenKind.AtKeyword,
                SvgCssTokenKind.Hash,
                SvgCssTokenKind.Ident
            }));
            Assert.That(values.Select(token => token.Value), Is.EqualTo(new[] { "foo", "media", "123", "a\uFFFDb" }));
            Assert.That(values[2].HashType, Is.EqualTo(SvgCssHashType.Id));
            Assert.That(string.Concat(result.Tokens.Select(token => token.RawText)), Is.EqualTo(input));
        });
    }

    [Test]
    public void StringsDecodeEscapesAndRecoverAtNewlines()
    {
        string input = "\"a\\41 b\" 'line\\\r\nnext' \"bad\n";

        SvgCssTokenizeResult result = Tokenize(input);
        List<SvgCssToken> values = result.Tokens.Where(token => token.Kind != SvgCssTokenKind.Whitespace).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParseErrorCount, Is.EqualTo(1));
            Assert.That(values.Select(token => token.Kind), Is.EqualTo(new[]
            {
                SvgCssTokenKind.String,
                SvgCssTokenKind.String,
                SvgCssTokenKind.BadString
            }));
            Assert.That(values[0].Value, Is.EqualTo("aAb"));
            Assert.That(values[1].Value, Is.EqualTo("linenext"));
            Assert.That(values[1].RawText, Is.EqualTo("'line\\\r\nnext'"));
            Assert.That(values[2].RawText, Is.EqualTo("\"bad"));
            Assert.That(values[2].Value, Is.EqualTo("bad"));
            Assert.That(string.Concat(result.Tokens.Select(token => token.RawText)), Is.EqualTo(input));
        });
    }

    [Test]
    public void UrlTokensDistinguishQuotedFunctionsAndRecoverMalformedValues()
    {
        string input = "url(icon\\2e svg) URL( spaced ) url(  \"quoted.svg\") url(bad\"x) url(open(x)";

        SvgCssTokenizeResult result = Tokenize(input);
        List<SvgCssToken> values = result.Tokens.Where(token => token.Kind != SvgCssTokenKind.Whitespace).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParseErrorCount, Is.EqualTo(2));
            Assert.That(values.Select(token => token.Kind), Is.EqualTo(new[]
            {
                SvgCssTokenKind.Url,
                SvgCssTokenKind.Url,
                SvgCssTokenKind.Function,
                SvgCssTokenKind.String,
                SvgCssTokenKind.CloseParenthesis,
                SvgCssTokenKind.BadUrl,
                SvgCssTokenKind.BadUrl
            }));
            Assert.That(values[0].Value, Is.EqualTo("icon.svg"));
            Assert.That(values[1].Value, Is.EqualTo("spaced"));
            Assert.That(values[2].Value, Is.EqualTo("url"));
            Assert.That(values[2].RawText, Is.EqualTo("url( "));
            Assert.That(values[3].Value, Is.EqualTo("quoted.svg"));
            Assert.That(values[5].RawText, Is.EqualTo("url(bad\"x)"));
            Assert.That(values[6].RawText, Is.EqualTo("url(open(x)"));
            Assert.That(string.Concat(result.Tokens.Select(token => token.RawText)), Is.EqualTo(input));
        });
    }

    [Test]
    public void UnicodeRangesCdoCdcAndBlockDelimitersAreTagged()
    {
        string input = "U+26 u+4?? U+00A0-00FF <!-- --> []{}()";

        SvgCssTokenizeResult result = Tokenize(input);
        List<SvgCssToken> values = result.Tokens.Where(token => token.Kind != SvgCssTokenKind.Whitespace).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(token => token.Kind), Is.EqualTo(new[]
            {
                SvgCssTokenKind.UnicodeRange,
                SvgCssTokenKind.UnicodeRange,
                SvgCssTokenKind.UnicodeRange,
                SvgCssTokenKind.Cdo,
                SvgCssTokenKind.Cdc,
                SvgCssTokenKind.OpenSquare,
                SvgCssTokenKind.CloseSquare,
                SvgCssTokenKind.OpenCurly,
                SvgCssTokenKind.CloseCurly,
                SvgCssTokenKind.OpenParenthesis,
                SvgCssTokenKind.CloseParenthesis
            }));
            Assert.That(values[0].UnicodeRangeStart, Is.EqualTo(0x26));
            Assert.That(values[0].UnicodeRangeEnd, Is.EqualTo(0x26));
            Assert.That(values[1].UnicodeRangeStart, Is.EqualTo(0x400));
            Assert.That(values[1].UnicodeRangeEnd, Is.EqualTo(0x4ff));
            Assert.That(values[2].UnicodeRangeStart, Is.EqualTo(0x00a0));
            Assert.That(values[2].UnicodeRangeEnd, Is.EqualTo(0x00ff));
            Assert.That(string.Concat(result.Tokens.Select(token => token.RawText)), Is.EqualTo(input));
        });
    }

    [Test]
    public void BadTokensAndUnclosedCommentsRemainRecoverableSourceTokens()
    {
        string input = "url(foo\\\nbar) \"x\n/*open";

        SvgCssTokenizeResult result = Tokenize(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ParseErrorCount, Is.EqualTo(3));
            Assert.That(result.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                SvgCssTokenKind.BadUrl,
                SvgCssTokenKind.Whitespace,
                SvgCssTokenKind.BadString,
                SvgCssTokenKind.Whitespace,
                SvgCssTokenKind.Comment
            }));
            Assert.That(result.Tokens[4].Value, Is.EqualTo("open"));
            Assert.That(string.Concat(result.Tokens.Select(token => token.RawText)), Is.EqualTo(input));
        });
    }

    [Test]
    public void OffsetsAndLinesTrackCrLfAndFormFeedWithoutLosingRawText()
    {
        string input = "a\r\n/*b*/\fc";

        SvgCssTokenizeResult result = Tokenize(input);

        Assert.That(result.Tokens, Has.Count.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(result.Tokens[1].RawText, Is.EqualTo("\r\n"));
            Assert.That(result.Tokens[1].StartLine, Is.EqualTo(1));
            Assert.That(result.Tokens[1].StartColumn, Is.EqualTo(2));
            Assert.That(result.Tokens[1].EndLine, Is.EqualTo(2));
            Assert.That(result.Tokens[1].EndColumn, Is.EqualTo(1));
            Assert.That(result.Tokens[2].StartLine, Is.EqualTo(2));
            Assert.That(result.Tokens[3].RawText, Is.EqualTo("\f"));
            Assert.That(result.Tokens[3].EndLine, Is.EqualTo(3));
            Assert.That(result.Tokens[4].StartLine, Is.EqualTo(3));
            Assert.That(result.Tokens[4].StartColumn, Is.EqualTo(1));
        });

        int next = 0;
        foreach (SvgCssToken token in result.Tokens)
        {
            Assert.That(token.StartOffset, Is.EqualTo(next));
            Assert.That(input.Substring(token.StartOffset, token.EndOffset - token.StartOffset), Is.EqualTo(token.RawText));
            next = token.EndOffset;
        }
        Assert.That(next, Is.EqualTo(input.Length));
    }

    [Test]
    public void InvalidArgumentsCharacterBoundsAndTokenBoundsFailDeterministically()
    {
        SvgCssTokenizeResult nullInput = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(null, 4, 4);
        SvgCssTokenizeResult nullLimits = new SvgCssTokenizer().TokenizeWithValueAndLimits("a", null);
        SvgCssTokenizeResult characters = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters("a\r\nb", 8, 3);
        SvgCssTokenizeResult tokens = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters("a b", 2, 16);

        Assert.Multiple(() =>
        {
            Assert.That(nullInput.Error, Is.EqualTo(SvgCssTokenizeError.InvalidArgument));
            Assert.That(nullLimits.Error, Is.EqualTo(SvgCssTokenizeError.InvalidArgument));
            Assert.That(characters.Error, Is.EqualTo(SvgCssTokenizeError.CharacterLimitExceeded));
            Assert.That(characters.ErrorOffset, Is.EqualTo(3));
            Assert.That(characters.ErrorLine, Is.EqualTo(2));
            Assert.That(characters.ErrorColumn, Is.EqualTo(1));
            Assert.That(tokens.Error, Is.EqualTo(SvgCssTokenizeError.TokenLimitExceeded));
            Assert.That(tokens.ErrorOffset, Is.EqualTo(2));
            Assert.That(string.Concat(tokens.Tokens.Select(token => token.RawText)), Is.EqualTo("a "));
        });
    }

    static SvgCssTokenizeResult Tokenize(string input)
    {
        return new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(input, 256, 4096);
    }
}
