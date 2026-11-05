using System.Text;

namespace pop3.Tests;

public class Pop3RfcTranscriptParserTests
{
    [Test]
    public void Rfc1939BaseSessionTranscriptParsesCommandsAndMailboxData()
    {
        Pop3CommandParser commandParser = new Pop3CommandParser();
        Pop3Command[] commands = new[]
        {
            commandParser.ParseCommand(Ascii(
                "APOP mrose c4c9334bac560ecc979e58001b3e22fb")),
            commandParser.ParseCommand(Ascii("STAT")),
            commandParser.ParseCommand(Ascii("LIST")),
            commandParser.ParseCommand(Ascii("RETR 1")),
            commandParser.ParseCommand(Ascii("DELE 1")),
            commandParser.ParseCommand(Ascii("QUIT"))
        };
        Pop3Response greeting = new Pop3ResponseParser().ParseResponse(
            Ascii("+OK POP3 server ready <1896.697170952@dbc.mtview.ca.us>"),
            true,
            4096);
        Pop3MailboxStatus status = new Pop3ListParser().ParseMailboxStatus(
            Ascii("2 320"));
        Pop3MessageInfo[] messages = new Pop3ListParser().ParseListEntries(
            Lines("1 120", "2 200"),
            status);
        Pop3UniqueId[] uniqueIds = new Pop3UidlParser().ParseUniqueIds(
            Lines(
                "1 whqtswO00WBw418f9t5JxYwZ",
                "2 QhdPYR:00WBw1Ph7x7"),
            messages);

        Assert.That(commands.Select(command => command.Keyword), Is.EqualTo(new[]
        {
            "APOP", "STAT", "LIST", "RETR", "DELE", "QUIT"
        }));
        Assert.That(commands[0].Arguments, Is.EqualTo(new[]
        {
            "mrose", "c4c9334bac560ecc979e58001b3e22fb"
        }));
        Assert.That(greeting.GreetingTimestamp,
            Is.EqualTo("<1896.697170952@dbc.mtview.ca.us>"));
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(320));
        Assert.That(messages.Select(message => message.OctetCount),
            Is.EqualTo(new long[] { 120, 200 }));
        Assert.That(uniqueIds.Select(uniqueId => uniqueId.Value), Is.EqualTo(new[]
        {
            "whqtswO00WBw418f9t5JxYwZ",
            "QhdPYR:00WBw1Ph7x7"
        }));
    }

    [Test]
    public void Rfc2449CapaTranscriptParsesEveryAdvertisedCapability()
    {
        Pop3CapabilitySet capabilities = new Pop3CapabilityParser().ParseCapabilities(
            Lines(
                "TOP",
                "USER",
                "SASL CRAM-MD5 KERBEROS_V4",
                "RESP-CODES",
                "LOGIN-DELAY 900",
                "PIPELINING",
                "EXPIRE 60",
                "UIDL",
                "IMPLEMENTATION Shlemazle-Plotz-v302"),
            Pop3SessionState.Authorization);

        Assert.That(capabilities.SupportsTop, Is.True);
        Assert.That(capabilities.SupportsUser, Is.True);
        Assert.That(capabilities.SaslMechanisms,
            Is.EqualTo(new[] { "CRAM-MD5", "KERBEROS_V4" }));
        Assert.That(capabilities.SupportsResponseCodes, Is.True);
        Assert.That(capabilities.LoginDelaySeconds, Is.EqualTo(900));
        Assert.That(capabilities.SupportsPipelining, Is.True);
        Assert.That(capabilities.ExpireDays, Is.EqualTo(60));
        Assert.That(capabilities.SupportsUidl, Is.True);
        Assert.That(capabilities.Implementation,
            Is.EqualTo("Shlemazle-Plotz-v302"));
    }

    [Test]
    public void Rfc2595StlsTranscriptParsesUpgradeCommandAndResponse()
    {
        Pop3Command command = new Pop3CommandParser().ParseCommand(Ascii("STLS"));
        Pop3Response response = new Pop3ResponseParser().ParseResponse(
            Ascii("+OK Begin TLS negotiation"),
            false,
            4096);
        Pop3CapabilitySet capabilities = new Pop3CapabilityParser().ParseCapabilities(
            Lines("STLS"),
            Pop3SessionState.Authorization);

        Assert.That(command.Keyword, Is.EqualTo("STLS"));
        Assert.That(command.Arguments, Is.Empty);
        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(response.Text, Is.EqualTo("Begin TLS negotiation"));
        Assert.That(capabilities.SupportsStls, Is.True);
    }

    [TestCase("-ERR [SYS/TEMP] mailbox locked", Pop3ResponseCodes.SystemTemporary, Pop3AuthenticationStatus.TemporaryFailure)]
    [TestCase("-ERR [SYS/PERM] mailbox corrupt", Pop3ResponseCodes.SystemPermanent, Pop3AuthenticationStatus.PermanentFailure)]
    [TestCase("-ERR [AUTH] invalid password", Pop3ResponseCodes.Authentication, Pop3AuthenticationStatus.Rejected)]
    [TestCase("-ERR [IN-USE] another session is active", Pop3ResponseCodes.InUse, Pop3AuthenticationStatus.InUse)]
    [TestCase("-ERR [LOGIN-DELAY] retry later", Pop3ResponseCodes.LoginDelay, Pop3AuthenticationStatus.LoginDelay)]
    public void Rfc3206AndRfc2449ResponseCodesRemainMachineReadable(
        string line,
        string expectedCode,
        Pop3AuthenticationStatus expectedStatus)
    {
        Pop3Response response = new Pop3ResponseParser().ParseResponse(
            Ascii(line),
            false,
            4096);
        Pop3ResponseCodeParser parser = new Pop3ResponseCodeParser();

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(parser.GetRegisteredResponseCode(response.ResponseCodeLevels),
            Is.EqualTo(expectedCode));
        Assert.That(parser.ClassifyAuthenticationStatus(response.ResponseCodeLevels),
            Is.EqualTo(expectedStatus));
    }

    [Test]
    public void Rfc5034AuthPlainTranscriptsParseInitialAndChallengeForms()
    {
        Pop3CommandParser commandParser = new Pop3CommandParser();
        Pop3Command initial = commandParser.ParseCommand(
            Ascii("AUTH PLAIN dGVzdAB0ZXN0AHRlc3Q="));
        Pop3Command challenge = commandParser.ParseCommand(Ascii("AUTH PLAIN"));
        Pop3Response emptyChallenge = new Pop3ResponseParser().ParseResponse(
            Ascii("+ "),
            false,
            4096);
        byte[] credentials = new Pop3Base64().Decode(initial.Arguments[1]);

        Assert.That(initial.Arguments, Is.EqualTo(new[]
        {
            "PLAIN", "dGVzdAB0ZXN0AHRlc3Q="
        }));
        Assert.That(challenge.Arguments, Is.EqualTo(new[] { "PLAIN" }));
        Assert.That(emptyChallenge.Kind,
            Is.EqualTo(Pop3ResponseKind.SaslContinuation));
        Assert.That(emptyChallenge.Text, Is.Empty);
        Assert.That(credentials, Is.EqualTo(new byte[]
        {
            116, 101, 115, 116, 0,
            116, 101, 115, 116, 0,
            116, 101, 115, 116
        }));
    }

    [Test]
    public void Rfc6856Utf8AndLangTranscriptParsesCapabilitiesCommandsAndLanguages()
    {
        Pop3CapabilitySet capabilities = new Pop3CapabilityParser().ParseCapabilities(
            Lines("UTF8 USER", "LANG"),
            Pop3SessionState.Authorization);
        Pop3CommandParser commandParser = new Pop3CommandParser();
        Pop3Command[] commands = new[]
        {
            commandParser.ParseCommand(Ascii("UTF8")),
            commandParser.ParseCommand(Ascii("LANG")),
            commandParser.ParseCommand(Ascii("LANG es")),
            commandParser.ParseCommand(Ascii("LANG *"))
        };
        Pop3Language[] languages = new Pop3LanguageParser().ParseLanguages(
            Lines(
                "en English",
                "en-boont English Boontling dialect",
                "de Deutsch",
                "it Italiano",
                "es Espanol",
                "sv Svenska"));

        Assert.That(capabilities.SupportsUtf8, Is.True);
        Assert.That(capabilities.SupportsUtf8User, Is.True);
        Assert.That(capabilities.SupportsLang, Is.True);
        Assert.That(commands.Select(command => command.Keyword),
            Is.EqualTo(new[] { "UTF8", "LANG", "LANG", "LANG" }));
        Assert.That(commands[2].Arguments, Is.EqualTo(new[] { "es" }));
        Assert.That(commands[3].Arguments, Is.EqualTo(new[] { "*" }));
        Assert.That(languages.Select(language => language.Tag), Is.EqualTo(new[]
        {
            "en", "en-boont", "de", "it", "es", "sv"
        }));
        Assert.That(languages[1].Description,
            Is.EqualTo("English Boontling dialect"));
    }

    static byte[] Ascii(string text)
    {
        return Encoding.ASCII.GetBytes(text);
    }

    static byte[][] Lines(params string[] values)
    {
        return values.Select(Ascii).ToArray();
    }
}
