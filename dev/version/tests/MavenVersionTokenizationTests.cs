using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class MavenVersionTokenizationTests
{
    [Test]
    public void ParseTokens_SplitsDotsHyphensAndUnderscores()
    {
        MavenVersionToken[] tokens = MavenVersionParser.ParseTokens("1.0-alpha_2");

        AssertToken(tokens, 0, "1", true, VersionChar.EndOfInput);
        AssertToken(tokens, 1, "0", true, '.');
        AssertToken(tokens, 2, "alpha", false, '-');
        AssertToken(tokens, 3, "2", true, '_');
    }

    [Test]
    public void ParseTokens_SplitsDigitLetterTransitions()
    {
        MavenVersionToken[] tokens = MavenVersionParser.ParseTokens("1.0alpha1");

        AssertToken(tokens, 0, "1", true, VersionChar.EndOfInput);
        AssertToken(tokens, 1, "0", true, '.');
        AssertToken(tokens, 2, "alpha", false, VersionChar.EndOfInput);
        AssertToken(tokens, 3, "1", true, VersionChar.EndOfInput);
    }

    [Test]
    public void ParseTokens_SupportsUnlimitedComponents()
    {
        MavenVersionToken[] tokens = MavenVersionParser.ParseTokens("1.2.3.4.5.6.7.8.9.10");

        Assert.That(tokens.Length, Is.EqualTo(10));
        AssertToken(tokens, 0, "1", true, VersionChar.EndOfInput);
        AssertToken(tokens, 9, "10", true, '.');
    }

    [Test]
    public void ParseTokens_TreatsMixedTextAndNumbersAsSeparateComponents()
    {
        MavenVersionToken[] tokens = MavenVersionParser.ParseTokens("2024rc1Final2");

        AssertToken(tokens, 0, "2024", true, VersionChar.EndOfInput);
        AssertToken(tokens, 1, "rc", false, VersionChar.EndOfInput);
        AssertToken(tokens, 2, "1", true, VersionChar.EndOfInput);
        AssertToken(tokens, 3, "Final", false, VersionChar.EndOfInput);
        AssertToken(tokens, 4, "2", true, VersionChar.EndOfInput);
    }

    [Test]
    public void ParseTokens_SkipsEmptySeparatorRuns()
    {
        MavenVersionToken[] tokens = MavenVersionParser.ParseTokens("1..2--rc");

        Assert.That(tokens.Length, Is.EqualTo(3));
        AssertToken(tokens, 0, "1", true, VersionChar.EndOfInput);
        AssertToken(tokens, 1, "2", true, '.');
        AssertToken(tokens, 2, "rc", false, '-');
    }

    static void AssertToken(MavenVersionToken[] tokens, int index, string text, bool numeric, char separatorBefore)
    {
        Assert.That(tokens[index].GetText(), Is.EqualTo(text));
        Assert.That(tokens[index].IsNumeric(), Is.EqualTo(numeric));
        Assert.That(tokens[index].IsText(), Is.EqualTo(!numeric));
        Assert.That(tokens[index].GetSeparatorBefore(), Is.EqualTo(separatorBefore));
        Assert.That(tokens[index].HasSeparatorBefore(), Is.EqualTo(separatorBefore != VersionChar.EndOfInput));
    }
}
