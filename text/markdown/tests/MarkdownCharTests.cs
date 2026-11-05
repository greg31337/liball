using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownCharTests
{
    [Test]
    public void IsWhitespace_FollowsCommonMarkUnicodeWhitespaceDefinition()
    {
        Assert.That(MarkdownChar.IsWhitespace(' '), Is.True);
        Assert.That(MarkdownChar.IsWhitespace('\t'), Is.True);
        Assert.That(MarkdownChar.IsWhitespace('\n'), Is.True);
        Assert.That(MarkdownChar.IsWhitespace('\r'), Is.True);
        Assert.That(MarkdownChar.IsWhitespace('\f'), Is.True);
        Assert.That(MarkdownChar.IsWhitespace('\u00a0'), Is.True);

        Assert.That(MarkdownChar.IsWhitespace('\v'), Is.False);
        Assert.That(MarkdownChar.IsWhitespace('a'), Is.False);
    }

    [Test]
    public void IsLineEnding_OnlyMatchesLfAndCr()
    {
        Assert.That(MarkdownChar.IsLineEnding('\n'), Is.True);
        Assert.That(MarkdownChar.IsLineEnding('\r'), Is.True);
        Assert.That(MarkdownChar.IsLineEnding('\f'), Is.False);
        Assert.That(MarkdownChar.IsLineEnding(' '), Is.False);
    }

    [Test]
    public void GetLineEndingLength_DetectsLfCrAndCrLf()
    {
        Assert.That(MarkdownChar.GetLineEndingLength("\nnext", 0), Is.EqualTo(1));
        Assert.That(MarkdownChar.GetLineEndingLength("\rnext", 0), Is.EqualTo(1));
        Assert.That(MarkdownChar.GetLineEndingLength("\r\nnext", 0), Is.EqualTo(2));
        Assert.That(MarkdownChar.GetLineEndingLength("text", 0), Is.EqualTo(0));
        Assert.That(MarkdownChar.GetLineEndingLength("text", 4), Is.EqualTo(0));
    }

    [Test]
    public void GetLineEndingLength_RejectsInvalidArguments()
    {
        Assert.That(() => MarkdownChar.GetLineEndingLength(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownChar.GetLineEndingLength("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownChar.GetLineEndingLength("", 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void IsAsciiPunctuation_MatchesCommonMarkAsciiRanges()
    {
        Assert.That(MarkdownChar.IsAsciiPunctuation('!'), Is.True);
        Assert.That(MarkdownChar.IsAsciiPunctuation('/'), Is.True);
        Assert.That(MarkdownChar.IsAsciiPunctuation(':'), Is.True);
        Assert.That(MarkdownChar.IsAsciiPunctuation('@'), Is.True);
        Assert.That(MarkdownChar.IsAsciiPunctuation('['), Is.True);
        Assert.That(MarkdownChar.IsAsciiPunctuation('`'), Is.True);
        Assert.That(MarkdownChar.IsAsciiPunctuation('{'), Is.True);
        Assert.That(MarkdownChar.IsAsciiPunctuation('~'), Is.True);

        Assert.That(MarkdownChar.IsAsciiPunctuation(' '), Is.False);
        Assert.That(MarkdownChar.IsAsciiPunctuation('0'), Is.False);
        Assert.That(MarkdownChar.IsAsciiPunctuation('A'), Is.False);
        Assert.That(MarkdownChar.IsAsciiPunctuation('\u007f'), Is.False);
    }

    [Test]
    public void IsPunctuation_MatchesUnicodePunctuationAndSymbols()
    {
        Assert.That(MarkdownChar.IsPunctuation('!'), Is.True);
        Assert.That(MarkdownChar.IsPunctuation('\u2014'), Is.True);
        Assert.That(MarkdownChar.IsPunctuation('\u20ac'), Is.True);
        Assert.That(MarkdownChar.IsPunctuation('a'), Is.False);
        Assert.That(MarkdownChar.IsPunctuation(' '), Is.False);
    }

    [Test]
    public void BasicHelpers_ClassifySpacesTabsAndControls()
    {
        Assert.That(MarkdownChar.IsSpace(' '), Is.True);
        Assert.That(MarkdownChar.IsSpace('\t'), Is.False);
        Assert.That(MarkdownChar.IsTab('\t'), Is.True);
        Assert.That(MarkdownChar.IsTab(' '), Is.False);
        Assert.That(MarkdownChar.IsSpaceOrTab(' '), Is.True);
        Assert.That(MarkdownChar.IsSpaceOrTab('\t'), Is.True);
        Assert.That(MarkdownChar.IsSpaceOrTab('x'), Is.False);
        Assert.That(MarkdownChar.IsAsciiControl('\u0000'), Is.True);
        Assert.That(MarkdownChar.IsAsciiControl('\u001f'), Is.True);
        Assert.That(MarkdownChar.IsAsciiControl('\u007f'), Is.True);
        Assert.That(MarkdownChar.IsAsciiControl(' '), Is.False);
    }
}
