using websocket;

namespace websocket.Tests;

public class AsciiTokenTests
{
    [Test]
    public void AcceptsEveryTokenCharacter()
    {
        const string token = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&'*+-.^_`|~";

        Assert.Multiple(() =>
        {
            Assert.That(AsciiToken.IsValid(token), Is.True);
            foreach (var character in token)
            {
                Assert.That(AsciiToken.IsCharacter(character), Is.True, $"Character {character}");
            }
        });
    }

    [TestCase("")]
    [TestCase("has space")]
    [TestCase("name:value")]
    [TestCase("name,value")]
    [TestCase("name/value")]
    [TestCase("name=value")]
    [TestCase("name\tvalue")]
    [TestCase("caf\u00e9")]
    public void RejectsEmptyDelimitedWhitespaceOrNonAsciiTokens(string value)
    {
        Assert.That(AsciiToken.IsValid(value), Is.False);
    }

    [Test]
    public void ReadsMaximalTokenFromAnOffset()
    {
        const string source = "ignore websocket/13";

        Assert.Multiple(() =>
        {
            Assert.That(AsciiToken.ReadLength(source, 7), Is.EqualTo(9));
            Assert.That(AsciiToken.ReadLength(source, 16), Is.Zero);
            Assert.That(AsciiToken.ReadLength(source, source.Length), Is.Zero);
            Assert.That(AsciiToken.ReadLength(source, -1), Is.Zero);
            Assert.That(AsciiToken.ReadLength(source, source.Length + 1), Is.Zero);
        });
    }

    [Test]
    public void ComparesAsciiWithoutCultureOrCaseSensitivity()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AsciiToken.EqualsIgnoreCase("WebSocket", "websocket"), Is.True);
            Assert.That(AsciiToken.EqualsIgnoreCase("X-CUSTOM_1", "x-custom_1"), Is.True);
            Assert.That(AsciiToken.EqualsIgnoreCase("websocket", "websockets"), Is.False);
            Assert.That(AsciiToken.EqualsIgnoreCase("websocket", "websockeT!"), Is.False);
            Assert.That(AsciiToken.EqualsIgnoreCase("\u00c9", "\u00e9"), Is.False);
        });
    }
}
