
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorLexerTests
{
    [Test]
    public void TokenizesARepresentativeSelectorWithSemanticValues()
    {
        HtmlSelectorLexer lexer = new HtmlSelectorLexer(
            @"div#main.item[data-x=""a\26 b""]:nth-child(2n+1), *");

        HtmlSelectorToken[] tokens = lexer.Tokenize();

        Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(new[]
        {
            HtmlSelectorTokenKind.Identifier,
            HtmlSelectorTokenKind.Hash,
            HtmlSelectorTokenKind.Delimiter,
            HtmlSelectorTokenKind.Identifier,
            HtmlSelectorTokenKind.Delimiter,
            HtmlSelectorTokenKind.Identifier,
            HtmlSelectorTokenKind.Delimiter,
            HtmlSelectorTokenKind.String,
            HtmlSelectorTokenKind.Delimiter,
            HtmlSelectorTokenKind.Delimiter,
            HtmlSelectorTokenKind.Function,
            HtmlSelectorTokenKind.Dimension,
            HtmlSelectorTokenKind.Number,
            HtmlSelectorTokenKind.Delimiter,
            HtmlSelectorTokenKind.Delimiter,
            HtmlSelectorTokenKind.Whitespace,
            HtmlSelectorTokenKind.Delimiter,
            HtmlSelectorTokenKind.End
        }));

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0].GetValue(), Is.EqualTo("div"));
            Assert.That(tokens[1].GetValue(), Is.EqualTo("main"));
            Assert.That(tokens[1].IsIdHash(), Is.True);
            Assert.That(tokens[3].GetValue(), Is.EqualTo("item"));
            Assert.That(tokens[7].GetValue(), Is.EqualTo("a&b"));
            Assert.That(tokens[10].GetValue(), Is.EqualTo("nth-child"));
            Assert.That(tokens[10].GetRepresentation(),
                Is.EqualTo("nth-child("));
            Assert.That(tokens[11].GetNumber(), Is.EqualTo(2));
            Assert.That(tokens[11].GetUnit(), Is.EqualTo("n"));
            Assert.That(tokens[11].IsInteger(), Is.True);
            Assert.That(tokens[12].GetNumber(), Is.EqualTo(1));
            Assert.That(tokens[12].GetRepresentation(), Is.EqualTo("+1"));
            Assert.That(tokens[16].GetValue(), Is.EqualTo("*"));
            Assert.That(lexer.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void IdentifierAndHashEscapesDecodeCssCodePoints()
    {
        HtmlSelectorLexer lexer = new HtmlSelectorLexer(
            @"#\69 d .\66 oo\+bar .\1F600 x .\0 y #123");

        HtmlSelectorToken[] tokens = lexer.Tokenize();

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0].GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.Hash));
            Assert.That(tokens[0].GetValue(), Is.EqualTo("id"));
            Assert.That(tokens[0].IsIdHash(), Is.True);
            Assert.That(tokens[3].GetValue(), Is.EqualTo("foo+bar"));
            Assert.That(tokens[6].GetValue(), Is.EqualTo("😀x"));
            Assert.That(tokens[9].GetValue(), Is.EqualTo("�y"));
            Assert.That(tokens[11].GetValue(), Is.EqualTo("123"));
            Assert.That(tokens[11].IsIdHash(), Is.False);
            Assert.That(lexer.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void PreprocessingReplacesInvalidScalarsAndPreservesAstralNames()
    {
        HtmlSelectorLexer lexer = new HtmlSelectorLexer(
            "\0name \uD800bad 😀good");

        HtmlSelectorToken[] tokens = lexer.Tokenize();

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0].GetValue(), Is.EqualTo("�name"));
            Assert.That(tokens[2].GetValue(), Is.EqualTo("�bad"));
            Assert.That(tokens[4].GetValue(), Is.EqualTo("😀good"));
            Assert.That(tokens[4].GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(6));
            Assert.That(lexer.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void NumbersPercentagesAndDimensionsFollowCssNumberBoundaries()
    {
        HtmlSelectorLexer lexer = new HtmlSelectorLexer(
            "-12 +.5 6.02e23 10% 2n -3.5PX . 1e+");

        HtmlSelectorToken[] tokens = lexer.Tokenize();
        HtmlSelectorToken[] values = tokens.Where(token =>
            token.GetKind() != HtmlSelectorTokenKind.Whitespace
                && token.GetKind() != HtmlSelectorTokenKind.End).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(values.Select(token => token.GetKind()), Is.EqualTo(
                new[]
                {
                    HtmlSelectorTokenKind.Number,
                    HtmlSelectorTokenKind.Number,
                    HtmlSelectorTokenKind.Number,
                    HtmlSelectorTokenKind.Percentage,
                    HtmlSelectorTokenKind.Dimension,
                    HtmlSelectorTokenKind.Dimension,
                    HtmlSelectorTokenKind.Delimiter,
                    HtmlSelectorTokenKind.Dimension,
                    HtmlSelectorTokenKind.Delimiter
                }));
            Assert.That(values[0].GetNumber(), Is.EqualTo(-12));
            Assert.That(values[0].IsInteger(), Is.True);
            Assert.That(values[1].GetNumber(), Is.EqualTo(0.5));
            Assert.That(values[1].IsInteger(), Is.False);
            Assert.That(values[2].GetNumber(), Is.EqualTo(6.02e23));
            Assert.That(values[3].GetNumber(), Is.EqualTo(10));
            Assert.That(values[4].GetNumber(), Is.EqualTo(2));
            Assert.That(values[4].GetUnit(), Is.EqualTo("n"));
            Assert.That(values[5].GetNumber(), Is.EqualTo(-3.5));
            Assert.That(values[5].GetUnit(), Is.EqualTo("PX"));
            Assert.That(values[7].GetRepresentation(), Is.EqualTo("1e"));
            Assert.That(values[7].GetUnit(), Is.EqualTo("e"));
            Assert.That(values[8].GetValue(), Is.EqualTo("+"));
        });
    }

    [Test]
    public void StringsRecoverFromLineContinuationsNewlinesAndEndOfInput()
    {
        HtmlSelectorLexer continued = new HtmlSelectorLexer(
            "\"a\\\r\nb\"");
        HtmlSelectorToken continuedToken = continued.Read();

        Assert.Multiple(() =>
        {
            Assert.That(continuedToken.GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.String));
            Assert.That(continuedToken.GetValue(), Is.EqualTo("ab"));
            Assert.That(continuedToken.GetSourceSpan()
                .GetNormalizedCharacterLength(), Is.EqualTo(6));
            Assert.That(continuedToken.GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(7));
            Assert.That(continued.GetDiagnostics(), Is.Empty);
        });

        HtmlSelectorLexer newline = new HtmlSelectorLexer("'bad\nnext");
        HtmlSelectorToken[] newlineTokens = newline.Tokenize();
        Assert.Multiple(() =>
        {
            Assert.That(newlineTokens[0].GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.BadString));
            Assert.That(newlineTokens[0].GetValue(), Is.EqualTo("bad"));
            Assert.That(newlineTokens[1].GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.Whitespace));
            Assert.That(newlineTokens[2].GetValue(), Is.EqualTo("next"));
            Assert.That(newline.GetDiagnostics(), Has.Length.EqualTo(1));
        });

        HtmlSelectorLexer ended = new HtmlSelectorLexer("'end");
        Assert.Multiple(() =>
        {
            Assert.That(ended.Read().GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.String));
            Assert.That(ended.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CommentsAreRemovedAndLexicalErrorsCarryExactSpans()
    {
        HtmlSelectorLexer lexer = new HtmlSelectorLexer("a/**/b/*oops");

        HtmlSelectorToken[] tokens = lexer.Tokenize();
        HtmlDiagnostic[] diagnostics = lexer.GetDiagnostics();
        diagnostics[0] = null;
        HtmlDiagnostic diagnostic = lexer.GetDiagnostics()[0];

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.GetKind()), Is.EqualTo(
                new[]
                {
                    HtmlSelectorTokenKind.Identifier,
                    HtmlSelectorTokenKind.Identifier,
                    HtmlSelectorTokenKind.End
                }));
            Assert.That(tokens[0].GetValue(), Is.EqualTo("a"));
            Assert.That(tokens[1].GetValue(), Is.EqualTo("b"));
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorInvalidSyntax));
            Assert.That(diagnostic.GetSeverity(),
                Is.EqualTo(HtmlDiagnosticSeverity.Error));
            Assert.That(diagnostic.GetSpan().GetStart().GetCharacterOffset(),
                Is.EqualTo(6));
            Assert.That(diagnostic.GetSpan().GetEnd().GetCharacterOffset(),
                Is.EqualTo(12));
        });
    }

    [Test]
    public void InvalidEscapesRecoverAsDelimitersAndReportTheirLocation()
    {
        HtmlSelectorLexer newline = new HtmlSelectorLexer("\\\r\nnext");
        HtmlSelectorToken[] tokens = newline.Tokenize();
        HtmlDiagnostic diagnostic = newline.GetDiagnostics()[0];

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0].GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.Delimiter));
            Assert.That(tokens[0].GetValue(), Is.EqualTo("\\"));
            Assert.That(tokens[1].GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.Whitespace));
            Assert.That(tokens[2].GetValue(), Is.EqualTo("next"));
            Assert.That(diagnostic.GetSpan().GetStart().GetLine(),
                Is.EqualTo(1));
            Assert.That(diagnostic.GetSpan().GetStart().GetColumn(),
                Is.EqualTo(1));
            Assert.That(diagnostic.GetSpan().GetCharacterLength(),
                Is.EqualTo(1));
            Assert.That(tokens[1].GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(2));
        });

        HtmlSelectorLexer ended = new HtmlSelectorLexer("name\\");
        HtmlSelectorToken token = ended.Read();
        Assert.Multiple(() =>
        {
            Assert.That(token.GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.Identifier));
            Assert.That(token.GetValue(), Is.EqualTo("name�"));
            Assert.That(ended.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void SelectorPunctuationRemainsOrderedDelimiterTokens()
    {
        HtmlSelectorLexer lexer = new HtmlSelectorLexer(
            ". > + ~ , [ ] ( ) : = * | ^ $ !");

        HtmlSelectorToken[] tokens = lexer.Tokenize();
        string delimiters = string.Concat(tokens.Where(token =>
            token.GetKind() == HtmlSelectorTokenKind.Delimiter)
            .Select(token => token.GetValue()));

        Assert.Multiple(() =>
        {
            Assert.That(delimiters, Is.EqualTo(".>+~,[]():=*|^$!"));
            Assert.That(tokens.Last().GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.End));
            Assert.That(lexer.Read().GetKind(),
                Is.EqualTo(HtmlSelectorTokenKind.End));
            Assert.That(lexer.GetDiagnostics(), Is.Empty);
        });
    }
}
