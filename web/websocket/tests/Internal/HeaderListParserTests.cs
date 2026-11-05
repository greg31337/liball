using websocket;

namespace websocket.Tests;

public class HeaderListParserTests
{
    [TestCase("", new string[0])]
    [TestCase(" \t ", new string[0])]
    [TestCase("websocket", new[] { "websocket" })]
    [TestCase("keep-alive, Upgrade", new[] { "keep-alive", "Upgrade" })]
    [TestCase("\tfoo \t,\t BAR,baz\t", new[] { "foo", "BAR", "baz" })]
    [TestCase(",foo,, bar,", new[] { "foo", "bar" })]
    [TestCase(", , \t,", new string[0])]
    public void ParsesOrderedTokensAndIgnoresEmptyMembers(string source, string[] expected)
    {
        var result = HeaderListParser.ParseTokenList(source, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Elements, Is.EqualTo(expected));
        });
    }

    [TestCase("foo bar")]
    [TestCase("foo;bar")]
    [TestCase("foo/bar")]
    [TestCase("foo=bar")]
    [TestCase("\"foo\"")]
    [TestCase("foo\r,bar")]
    [TestCase("foo\n,bar")]
    [TestCase("foo, caf\u00e9")]
    public void RejectsMalformedTokenLists(string source)
    {
        var result = HeaderListParser.ParseTokenList(source, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Elements, Is.Empty);
        });
    }

    [Test]
    public void EnforcesElementLimitWithoutReturningPartialElements()
    {
        var exact = HeaderListParser.ParseTokenList("one, two", 2);
        var exceeded = HeaderListParser.ParseTokenList("one, two, three", 2);
        var invalidLimit = HeaderListParser.ParseTokenList("", -1);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(exact.Elements, Is.EqualTo(new[] { "one", "two" }));
            Assert.That(exceeded.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(exceeded.Elements, Is.Empty);
            Assert.That(invalidLimit.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(invalidLimit.Elements, Is.Empty);
        });
    }

    [Test]
    public void ParsesTokensContainingEveryAllowedTokenCharacter()
    {
        const string allTokenCharacters =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&'*+-.^_`|~";

        var result = HeaderListParser.ParseTokenList(
            allTokenCharacters + ", Second",
            2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Elements, Is.EqualTo(new[] { allTokenCharacters, "Second" }));
        });
    }
}
