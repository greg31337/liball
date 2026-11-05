using System.Text;

namespace pop3.Tests;

public class Pop3CommandParserTests
{
    [Test]
    public void ParserCanonicalizesEveryStandardCommandFromLowerAndMixedCase()
    {
        string[] commandLines = new[]
        {
            "USER alice",
            "PASS secret",
            "APOP alice digest",
            "STAT",
            "LIST",
            "RETR 1",
            "DELE 1",
            "NOOP",
            "RSET",
            "QUIT",
            "TOP 1 0",
            "UIDL",
            "CAPA",
            "STLS",
            "AUTH PLAIN",
            "UTF8",
            "LANG"
        };
        string[] expectedKeywords = new[]
        {
            Pop3Commands.User,
            Pop3Commands.Pass,
            Pop3Commands.Apop,
            Pop3Commands.Stat,
            Pop3Commands.List,
            Pop3Commands.Retr,
            Pop3Commands.Dele,
            Pop3Commands.Noop,
            Pop3Commands.Rset,
            Pop3Commands.Quit,
            Pop3Commands.Top,
            Pop3Commands.Uidl,
            Pop3Commands.Capa,
            Pop3Commands.Stls,
            Pop3Commands.Auth,
            Pop3Commands.Utf8,
            Pop3Commands.Lang
        };
        Pop3CommandParser parser = new Pop3CommandParser();

        int index = 0;
        while (index < commandLines.Length)
        {
            string lower = commandLines[index].ToLowerInvariant();
            string mixed = AlternatingCase(commandLines[index]);

            Assert.That(
                parser.ParseCommand(Encoding.ASCII.GetBytes(lower)).Keyword,
                Is.EqualTo(expectedKeywords[index]),
                commandLines[index] + " lowercase");
            Assert.That(
                parser.ParseCommand(Encoding.ASCII.GetBytes(mixed)).Keyword,
                Is.EqualTo(expectedKeywords[index]),
                commandLines[index] + " mixed case");
            index++;
        }
    }

    [Test]
    public void ParserCanonicalizesKeywordsAndPreservesOrderedArguments()
    {
        Pop3CommandParser parser = new Pop3CommandParser();

        Pop3Command command = parser.ParseCommand(Encoding.ASCII.GetBytes("rEtR 42 token-._~"));

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Retr));
        Assert.That(command.Arguments, Is.EqualTo(new[] { "42", "token-._~" }));
        Assert.That(command.ContainsSensitiveData, Is.False);
        Assert.That(command.ExpectsMultilineResponse, Is.True);
    }

    [Test]
    public void ParserAcceptsAndCanonicalizesExtensionKeywords()
    {
        Pop3Command command = new Pop3CommandParser().ParseCommand(Encoding.ASCII.GetBytes("x-a value"));

        Assert.That(command.Keyword, Is.EqualTo("X-A"));
        Assert.That(command.Arguments, Is.EqualTo(new[] { "value" }));
        Assert.That(command.ContainsSensitiveData, Is.False);
        Assert.That(command.ExpectsMultilineResponse, Is.False);
    }

    [Test]
    public void ParserAndFormatterAcceptAllVisibleAsciiArgumentOctets()
    {
        char[] characters = new char[94];
        int value = 33;
        while (value <= 126)
        {
            characters[value - 33] = (char)value;
            value++;
        }
        string argument = new string(characters);
        Pop3CommandParser parser = new Pop3CommandParser();
        Pop3Command parsed = parser.ParseCommand(
            Encoding.ASCII.GetBytes("X01 " + argument));
        Pop3Command formatted = new Pop3Command();
        formatted.Keyword = "x01";
        formatted.Arguments = new[] { argument };

        Assert.That(parsed.Arguments, Is.EqualTo(new[] { argument }));
        Assert.That(
            Encoding.ASCII.GetString(parser.FormatCommand(formatted)),
            Is.EqualTo("X01 " + argument));
    }

    [TestCase("pass secret", true)]
    [TestCase("apop alice digest", true)]
    [TestCase("auth plain payload", true)]
    [TestCase("user alice", false)]
    [TestCase("stat", false)]
    public void ParserClassifiesSensitiveCommands(string text, bool expectedSensitive)
    {
        Pop3Command command = new Pop3CommandParser().ParseCommand(Encoding.ASCII.GetBytes(text));

        Assert.That(command.ContainsSensitiveData, Is.EqualTo(expectedSensitive));
    }

    [TestCase("RETR 1", true)]
    [TestCase("TOP 1 0", true)]
    [TestCase("LIST", true)]
    [TestCase("LIST 1", false)]
    [TestCase("UIDL", true)]
    [TestCase("UIDL 1", false)]
    [TestCase("CAPA", true)]
    [TestCase("LANG", true)]
    [TestCase("LANG en", false)]
    [TestCase("NOOP", false)]
    public void ParserClassifiesKnownMultilineResponses(string text, bool expectedMultiline)
    {
        Pop3Command command = new Pop3CommandParser().ParseCommand(Encoding.ASCII.GetBytes(text));

        Assert.That(command.ExpectsMultilineResponse, Is.EqualTo(expectedMultiline));
    }

    [Test]
    [TestCase(248, 254, true)]
    [TestCase(249, 255, true)]
    [TestCase(250, 256, false)]
    public void ParserEnforcesEveryCommandWireBoundary(
        int argumentLength,
        int wireLength,
        bool accepted)
    {
        Pop3CommandParser parser = new Pop3CommandParser();
        byte[] line = Encoding.ASCII.GetBytes(
            "X01 " + new string('a', argumentLength));

        Assert.That(line.Length + 2, Is.EqualTo(wireLength));
        if (accepted)
        {
            Pop3Command command = parser.ParseCommand(line);
            Assert.That(command.Arguments[0], Has.Length.EqualTo(argumentLength));
        }
        else
        {
            Assert.Throws<InvalidDataException>(() => parser.ParseCommand(line));
        }
    }

    [TestCase("")]
    [TestCase("AB")]
    [TestCase("ABCDE")]
    [TestCase(" STAT")]
    [TestCase("STAT ")]
    [TestCase("STAT  1")]
    [TestCase("STAT\t1")]
    public void ParserRejectsInvalidKeywordLengthsAndSpacing(string text)
    {
        Pop3CommandParser parser = new Pop3CommandParser();

        Assert.Throws<InvalidDataException>(() => parser.ParseCommand(Encoding.ASCII.GetBytes(text)));
    }

    [Test]
    public void ParserRejectsControlDeleteAndNonAsciiOctets()
    {
        Pop3CommandParser parser = new Pop3CommandParser();

        Assert.Throws<InvalidDataException>(() => parser.ParseCommand(new byte[] { 85, 83, 69, 82, 32, 0 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseCommand(new byte[] { 85, 83, 69, 82, 32, 127 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseCommand(new byte[] { 85, 83, 69, 82, 32, 128 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseCommand(new byte[] { 83, 84, 65, 84, 13, 10 }));
    }

    [Test]
    public void ParserRejectsEveryControlOctetInKeywordsAndArguments()
    {
        Pop3CommandParser parser = new Pop3CommandParser();
        int value = 0;
        while (value <= 31)
        {
            AssertInvalidParserOctet(parser, value);
            value++;
        }
        AssertInvalidParserOctet(parser, 127);
    }

    [Test]
    public void ServerParserStrictlyDecodesAdvertisedUtf8Credentials()
    {
        Pop3CommandParser parser = new Pop3CommandParser();
        byte[] valid = Encoding.UTF8.GetBytes("uSeR álîçé😀");
        byte[] malformed = new byte[]
        {
            85, 83, 69, 82, 32, 195, 40
        };

        Pop3Command command = parser.ParseServerCommand(valid, true);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.User));
        Assert.That(command.Arguments, Is.EqualTo(new[] { "álîçé😀" }));
        Assert.Throws<InvalidDataException>(() =>
            parser.ParseServerCommand(valid, false));
        Assert.Throws<InvalidDataException>(() =>
            parser.ParseServerCommand(malformed, true));
    }

    [TestCase("USER", "álîçé😀", "")]
    [TestCase("PASS", "päss🔑", "")]
    [TestCase("APOP", "郵便", "digest")]
    public void ServerParserAllowsUtf8OnlyInTheFirstCredentialArgument(
        string keyword,
        string firstArgument,
        string secondArgument)
    {
        string text = keyword + " " + firstArgument;
        if (secondArgument.Length > 0)
        {
            text += " " + secondArgument;
        }

        Pop3Command command = new Pop3CommandParser().ParseServerCommand(
            Encoding.UTF8.GetBytes(text),
            true);

        Assert.That(command.Keyword, Is.EqualTo(keyword));
        Assert.That(command.Arguments[0], Is.EqualTo(firstArgument));
        if (secondArgument.Length > 0)
        {
            Assert.That(command.Arguments[1], Is.EqualTo(secondArgument));
        }
    }

    [TestCase("USER alice é")]
    [TestCase("PASS secret é")]
    [TestCase("APOP alice é")]
    public void ServerParserRejectsUtf8InLaterCredentialArguments(string text)
    {
        Assert.Throws<InvalidDataException>(() =>
            new Pop3CommandParser().ParseServerCommand(
                Encoding.UTF8.GetBytes(text),
                true));
    }

    [TestCase("AUTH PLAIN")]
    [TestCase("LIST 1")]
    public void ServerParserKeepsUnrelatedArgumentsAsciiOnly(string commandText)
    {
        byte[] prefix = Encoding.ASCII.GetBytes(
            commandText.Substring(0, commandText.Length - 1));
        byte[] utf8 = Encoding.UTF8.GetBytes("é");
        byte[] line = new byte[prefix.Length + utf8.Length];
        Array.Copy(prefix, line, prefix.Length);
        Array.Copy(utf8, 0, line, prefix.Length, utf8.Length);

        Assert.Throws<InvalidDataException>(() =>
            new Pop3CommandParser().ParseServerCommand(line, true));
    }

    [Test]
    public void FormatterEmitsCanonicalKeywordAndSingleSpaceTokens()
    {
        Pop3Command command = new Pop3Command();
        command.Keyword = "uIdL";
        command.Arguments = new[] { "17", "token" };

        byte[] line = new Pop3CommandParser().FormatCommand(command);

        Assert.That(Encoding.ASCII.GetString(line), Is.EqualTo("UIDL 17 token"));
    }

    [Test]
    [TestCase(248, 254, true)]
    [TestCase(249, 255, true)]
    [TestCase(250, 256, false)]
    public void FormatterEnforcesEveryCommandWireBoundary(
        int argumentLength,
        int wireLength,
        bool accepted)
    {
        Pop3CommandParser parser = new Pop3CommandParser();
        Pop3Command command = new Pop3Command();
        command.Keyword = "X01";
        command.Arguments = new[] { new string('a', argumentLength) };

        if (accepted)
        {
            Assert.That(parser.FormatCommand(command),
                Has.Length.EqualTo(wireLength - 2));
        }
        else
        {
            Assert.Throws<InvalidDataException>(() =>
                parser.FormatCommand(command));
        }
    }

    [Test]
    public void FormatterRejectsInvalidObjectTokens()
    {
        Pop3CommandParser parser = new Pop3CommandParser();
        Pop3Command shortKeyword = new Pop3Command();
        Pop3Command emptyArgument = new Pop3Command();
        Pop3Command spacedArgument = new Pop3Command();
        Pop3Command nonAscii = new Pop3Command();
        Pop3Command nullArguments = new Pop3Command();
        Pop3Command nullKeyword = new Pop3Command();
        Pop3Command nullArgument = new Pop3Command();
        Pop3Command longKeyword = new Pop3Command();
        shortKeyword.Keyword = "NO";
        emptyArgument.Keyword = "NOOP";
        emptyArgument.Arguments = new[] { "" };
        spacedArgument.Keyword = "USER";
        spacedArgument.Arguments = new[] { "two words" };
        nonAscii.Keyword = "USER";
        nonAscii.Arguments = new[] { "café" };
        nullArguments.Keyword = "NOOP";
        nullArguments.Arguments = null!;
        nullKeyword.Keyword = null!;
        nullArgument.Keyword = "USER";
        nullArgument.Arguments = new string[] { null! };
        longKeyword.Keyword = "ABCDE";

        Assert.Throws<ArgumentNullException>(() => parser.FormatCommand(null!));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(shortKeyword));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(emptyArgument));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(spacedArgument));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(nonAscii));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(nullArguments));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(nullKeyword));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(nullArgument));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(longKeyword));
    }

    [Test]
    public void FormatterRejectsEveryControlCharacterInKeywordsAndArguments()
    {
        Pop3CommandParser parser = new Pop3CommandParser();
        int value = 0;
        while (value <= 32)
        {
            AssertInvalidFormatterCharacter(parser, value);
            value++;
        }
        AssertInvalidFormatterCharacter(parser, 127);
        AssertInvalidFormatterCharacter(parser, 128);
        AssertInvalidFormatterCharacter(parser, 255);
    }

    [Test]
    public void FormattedCommandsRoundTripThroughTheParser()
    {
        Pop3Command source = new Pop3Command();
        Pop3CommandParser parser = new Pop3CommandParser();
        source.Keyword = "top";
        source.Arguments = new[] { "3", "0" };

        Pop3Command parsed = parser.ParseCommand(parser.FormatCommand(source));

        Assert.That(parsed.Keyword, Is.EqualTo(Pop3Commands.Top));
        Assert.That(parsed.Arguments, Is.EqualTo(new[] { "3", "0" }));
        Assert.That(parsed.ExpectsMultilineResponse, Is.True);
        Assert.That(source.Keyword, Is.EqualTo("top"));
        Assert.That(source.Arguments, Is.EqualTo(new[] { "3", "0" }));
    }

    static string AlternatingCase(string text)
    {
        char[] characters = text.ToLowerInvariant().ToCharArray();
        int index = 0;
        while (index < characters.Length && characters[index] != ' ')
        {
            if (index % 2 == 0)
            {
                characters[index] = char.ToUpperInvariant(characters[index]);
            }
            index++;
        }
        return new string(characters);
    }

    static void AssertInvalidParserOctet(Pop3CommandParser parser, int value)
    {
        byte octet = (byte)value;
        byte[] keyword = new byte[] { 88, octet, 49 };
        byte[] argument = new byte[] { 88, 48, 49, 32, 97, octet, 98 };

        Assert.Throws<InvalidDataException>(() => parser.ParseCommand(keyword),
            "keyword control 0x" + value.ToString("X2"));
        Assert.Throws<InvalidDataException>(() => parser.ParseCommand(argument),
            "argument control 0x" + value.ToString("X2"));
    }

    static void AssertInvalidFormatterCharacter(
        Pop3CommandParser parser,
        int value)
    {
        char character = (char)value;
        Pop3Command keyword = new Pop3Command();
        keyword.Keyword = "X" + character + "1";
        Pop3Command argument = new Pop3Command();
        argument.Keyword = "X01";
        argument.Arguments = new[] { "a" + character + "b" };

        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(keyword),
            "keyword character U+" + value.ToString("X4"));
        Assert.Throws<InvalidDataException>(() => parser.FormatCommand(argument),
            "argument character U+" + value.ToString("X4"));
    }
}
