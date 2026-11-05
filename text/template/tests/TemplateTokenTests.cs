using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateTokenTests
{
    [Test]
    public void TokenKindFacts_ClassifiesTagDelimiters()
    {
        Assert.That(TemplateTokenKindFacts.IsTagStart(TemplateTokenKind.OutputStartDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagStart(TemplateTokenKind.StatementStartDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagStart(TemplateTokenKind.CommentStartDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagStart(TemplateTokenKind.RawBlockStartDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagStart(TemplateTokenKind.Text), Is.False);

        Assert.That(TemplateTokenKindFacts.IsTagEnd(TemplateTokenKind.OutputEndDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagEnd(TemplateTokenKind.StatementEndDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagEnd(TemplateTokenKind.CommentEndDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagEnd(TemplateTokenKind.RawBlockEndDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagEnd(TemplateTokenKind.Identifier), Is.False);
    }

    [Test]
    public void TokenKindFacts_ClassifiesTrimMarkersLiteralsAndPunctuationDelimiters()
    {
        Assert.That(TemplateTokenKindFacts.IsTrimMarker(TemplateTokenKind.LeftTrimMarker), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTrimMarker(TemplateTokenKind.RightTrimMarker), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTrimMarker(TemplateTokenKind.Operator), Is.False);

        Assert.That(TemplateTokenKindFacts.IsLiteral(TemplateTokenKind.StringLiteral), Is.True);
        Assert.That(TemplateTokenKindFacts.IsLiteral(TemplateTokenKind.NumberLiteral), Is.True);
        Assert.That(TemplateTokenKindFacts.IsLiteral(TemplateTokenKind.BooleanLiteral), Is.True);
        Assert.That(TemplateTokenKindFacts.IsLiteral(TemplateTokenKind.NullLiteral), Is.True);
        Assert.That(TemplateTokenKindFacts.IsLiteral(TemplateTokenKind.EmptyLiteral), Is.True);
        Assert.That(TemplateTokenKindFacts.IsLiteral(TemplateTokenKind.Identifier), Is.False);

        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.Dot), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.Comma), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.Colon), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.Semicolon), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.Pipe), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.OpenParen), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.CloseParen), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.OpenBracket), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.CloseBracket), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.OpenBrace), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.CloseBrace), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.Equals), Is.True);
        Assert.That(TemplateTokenKindFacts.IsDelimiter(TemplateTokenKind.EndOfFile), Is.False);
    }

    [Test]
    public void TokenKindFacts_ClassifiesTagContent()
    {
        Assert.That(TemplateTokenKindFacts.IsTagContent(TemplateTokenKind.Identifier), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagContent(TemplateTokenKind.StringLiteral), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagContent(TemplateTokenKind.Operator), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagContent(TemplateTokenKind.LeftTrimMarker), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagContent(TemplateTokenKind.OutputEndDelimiter), Is.True);
        Assert.That(TemplateTokenKindFacts.IsTagContent(TemplateTokenKind.Text), Is.False);
        Assert.That(TemplateTokenKindFacts.IsTagContent(TemplateTokenKind.EndOfFile), Is.False);
    }

    [Test]
    public void TokenKindFacts_ReturnsStableDisplayNames()
    {
        Assert.That(TemplateTokenKindFacts.GetDisplayName(TemplateTokenKind.Text), Is.EqualTo("text"));
        Assert.That(TemplateTokenKindFacts.GetDisplayName(TemplateTokenKind.OutputStartDelimiter), Is.EqualTo("output start delimiter"));
        Assert.That(TemplateTokenKindFacts.GetDisplayName(TemplateTokenKind.RawBlockEndDelimiter), Is.EqualTo("raw block end delimiter"));
        Assert.That(TemplateTokenKindFacts.GetDisplayName(TemplateTokenKind.LeftTrimMarker), Is.EqualTo("left trim marker"));
        Assert.That(TemplateTokenKindFacts.GetDisplayName(TemplateTokenKind.StringLiteral), Is.EqualTo("string literal"));
        Assert.That(TemplateTokenKindFacts.GetDisplayName(TemplateTokenKind.Operator), Is.EqualTo("operator"));
        Assert.That(TemplateTokenKindFacts.GetDisplayName(TemplateTokenKind.EndOfFile), Is.EqualTo("end of file"));
        Assert.That(TemplateTokenKindFacts.GetDisplayName((TemplateTokenKind)999), Is.EqualTo("unknown"));
    }

    [Test]
    public void TemplateToken_StoresKindSpanAndText()
    {
        TemplateSource source = TemplateSource.FromString("Hello {{ name }}");
        TemplateSourceSpan span = source.CreateSpan(6, 2);
        TemplateToken token = new TemplateToken(TemplateTokenKind.OutputStartDelimiter, span, "{{");

        Assert.That(token.Kind, Is.EqualTo(TemplateTokenKind.OutputStartDelimiter));
        Assert.That(token.Span, Is.SameAs(span));
        Assert.That(token.Text, Is.EqualTo("{{"));
        Assert.That(token.IsEndOfFile(), Is.False);
        Assert.That(token.IsText(), Is.False);
        Assert.That(token.IsDelimiter(), Is.True);
        Assert.That(token.IsTrimMarker(), Is.False);
        Assert.That(token.IsLiteral(), Is.False);
        Assert.That(token.TrimLeft, Is.False);
        Assert.That(token.TrimRight, Is.False);
        Assert.That(token.StandaloneLine, Is.False);
    }

    [Test]
    public void TemplateToken_ClassifiesTextLiteralTrimAndEof()
    {
        TemplateSource source = TemplateSource.FromString("x");
        TemplateToken text = new TemplateToken(TemplateTokenKind.Text, source.CreateSpan(0, 1), "x");
        TemplateToken literal = new TemplateToken(TemplateTokenKind.NumberLiteral, source.CreateSpan(0, 1), "1");
        TemplateToken trim = new TemplateToken(TemplateTokenKind.LeftTrimMarker, source.CreateSpan(0, 1), "-");
        TemplateToken eof = new TemplateToken(TemplateTokenKind.EndOfFile, source.CreateSpan(1, 0), "");

        Assert.That(text.IsText(), Is.True);
        Assert.That(literal.IsLiteral(), Is.True);
        Assert.That(trim.IsTrimMarker(), Is.True);
        Assert.That(eof.IsEndOfFile(), Is.True);

        TemplateToken commentText = new TemplateToken(TemplateTokenKind.CommentText, source.CreateSpan(0, 1), "x");
        TemplateToken rawText = new TemplateToken(TemplateTokenKind.RawText, source.CreateSpan(0, 1), "x");
        Assert.That(commentText.IsText(), Is.True);
        Assert.That(rawText.IsText(), Is.True);
    }

    [Test]
    public void TemplateToken_RejectsMissingSpanOrText()
    {
        TemplateSource source = TemplateSource.FromString("x");

        Assert.Throws<ArgumentNullException>(() => new TemplateToken(TemplateTokenKind.Text, null!, "x"));
        Assert.Throws<ArgumentNullException>(() => new TemplateToken(TemplateTokenKind.Text, source.CreateSpan(0, 1), null!));
    }
}
