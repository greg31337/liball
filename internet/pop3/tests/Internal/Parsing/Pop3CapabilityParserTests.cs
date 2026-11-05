using System.Text;

namespace pop3.Tests;

public class Pop3CapabilityParserTests
{
    [Test]
    public void ParserPreservesTagCasingAndOrderedParameters()
    {
        Pop3Capability capability = new Pop3CapabilityParser().ParseCapability(
            Encoding.ASCII.GetBytes("sAsL SCRAM-SHA-256 PLAIN"));

        Assert.That(capability.Name, Is.EqualTo("sAsL"));
        Assert.That(capability.Parameters, Is.EqualTo(new[] { "SCRAM-SHA-256", "PLAIN" }));
    }

    [Test]
    public void ParserBuildsTheCompleteAuthorizationCapabilityView()
    {
        byte[][] lines = CreateLines(
            "top",
            "USER",
            "Sasl SCRAM-SHA-256 PLAIN",
            "resp-codes",
            "LOGIN-DELAY 900 user",
            "pipelining",
            "EXPIRE NEVER USER",
            "uidl",
            "IMPLEMENTATION Example POP3",
            "stls",
            "auth-resp-code",
            "UTF8 user",
            "lang",
            "X-Vendor alpha beta");

        Pop3CapabilitySet result = new Pop3CapabilityParser().ParseCapabilities(
            lines,
            Pop3SessionState.Authorization);

        Assert.That(result.Entries, Has.Length.EqualTo(lines.Length));
        Assert.That(result.Entries[0].Name, Is.EqualTo("top"));
        Assert.That(result.Entries[13].Name, Is.EqualTo("X-Vendor"));
        Assert.That(result.Entries[13].Parameters, Is.EqualTo(new[] { "alpha", "beta" }));
        Assert.That(result.SupportsTop, Is.True);
        Assert.That(result.SupportsUser, Is.True);
        Assert.That(result.SaslMechanisms, Is.EqualTo(new[] { "SCRAM-SHA-256", "PLAIN" }));
        Assert.That(result.SupportsResponseCodes, Is.True);
        Assert.That(result.HasLoginDelay, Is.True);
        Assert.That(result.LoginDelaySeconds, Is.EqualTo(900));
        Assert.That(result.LoginDelayPerUser, Is.True);
        Assert.That(result.SupportsPipelining, Is.True);
        Assert.That(result.HasExpire, Is.True);
        Assert.That(result.ExpireNever, Is.True);
        Assert.That(result.ExpireDays, Is.Zero);
        Assert.That(result.ExpirePerUser, Is.True);
        Assert.That(result.SupportsUidl, Is.True);
        Assert.That(result.Implementation, Is.EqualTo("Example POP3"));
        Assert.That(result.SupportsStls, Is.True);
        Assert.That(result.SupportsAuthenticationResponseCode, Is.True);
        Assert.That(result.SupportsUtf8, Is.True);
        Assert.That(result.SupportsUtf8User, Is.True);
        Assert.That(result.SupportsLang, Is.True);
    }

    [Test]
    public void ParserInterpretsTheRfc2449CapabilityExample()
    {
        byte[][] lines = CreateLines(
            "TOP",
            "USER",
            "SASL CRAM-MD5 KERBEROS_V4",
            "RESP-CODES",
            "LOGIN-DELAY 900",
            "PIPELINING",
            "EXPIRE 60",
            "UIDL",
            "IMPLEMENTATION Shlemazle-Plotz-v302");

        Pop3CapabilitySet result = new Pop3CapabilityParser().ParseCapabilities(
            lines,
            Pop3SessionState.Authorization);

        Assert.That(result.Entries, Has.Length.EqualTo(9));
        Assert.That(result.SaslMechanisms, Is.EqualTo(new[] { "CRAM-MD5", "KERBEROS_V4" }));
        Assert.That(result.LoginDelaySeconds, Is.EqualTo(900));
        Assert.That(result.ExpireDays, Is.EqualTo(60));
        Assert.That(result.Implementation, Is.EqualTo("Shlemazle-Plotz-v302"));
        Assert.That(result.SupportsTop, Is.True);
        Assert.That(result.SupportsUser, Is.True);
        Assert.That(result.SupportsResponseCodes, Is.True);
        Assert.That(result.SupportsPipelining, Is.True);
        Assert.That(result.SupportsUidl, Is.True);
    }

    [Test]
    public void TransactionCapabilityViewUsesCurrentScalarValues()
    {
        Pop3CapabilitySet result = new Pop3CapabilityParser().ParseCapabilities(
            CreateLines("LOGIN-DELAY 60", "EXPIRE 30", "UTF8", "SASL SCRAM-SHA-256"),
            Pop3SessionState.Transaction);

        Assert.That(result.LoginDelaySeconds, Is.EqualTo(60));
        Assert.That(result.LoginDelayPerUser, Is.False);
        Assert.That(result.ExpireNever, Is.False);
        Assert.That(result.ExpireDays, Is.EqualTo(30));
        Assert.That(result.ExpirePerUser, Is.False);
        Assert.That(result.SupportsUtf8, Is.True);
        Assert.That(result.SupportsUtf8User, Is.False);
        Assert.That(result.SupportsStls, Is.False);
        Assert.That(result.SaslMechanisms, Is.EqualTo(new[] { "SCRAM-SHA-256" }));
    }

    [Test]
    public void DuplicateEntriesRemainOrderedWhileInterpretedValuesAreDeterministic()
    {
        Pop3Capability[] entries = new[]
        {
            Capability("SASL", "PLAIN"),
            Capability("x-private", "one"),
            Capability("sasl", "SCRAM-SHA-256", "PLAIN"),
            Capability("LOGIN-DELAY", "30", "USER"),
            Capability("login-delay", "60", "user"),
            Capability("EXPIRE", "NEVER", "USER"),
            Capability("expire", "1", "USER"),
            Capability("IMPLEMENTATION", "first"),
            Capability("implementation", "second"),
            Capability("X-PRIVATE", "two")
        };
        Pop3CapabilityParser parser = new Pop3CapabilityParser();

        Pop3CapabilitySet result = parser.InterpretCapabilities(entries, Pop3SessionState.Authorization);
        Pop3Capability[] privateEntries = parser.FindCapabilities(entries, "X-PrIvAtE");

        Assert.That(result.Entries, Is.SameAs(entries));
        Assert.That(result.SaslMechanisms, Is.EqualTo(new[] { "PLAIN", "SCRAM-SHA-256", "PLAIN" }));
        Assert.That(result.LoginDelaySeconds, Is.EqualTo(30));
        Assert.That(result.ExpireNever, Is.True);
        Assert.That(result.Implementation, Is.EqualTo("first"));
        Assert.That(privateEntries, Is.EqualTo(new[] { entries[1], entries[9] }));
    }

    [TestCaseSource(nameof(FormattingCases))]
    public void FormatterCanonicalizesRegisteredTagsAndPreservesParameters(
        Pop3Capability capability,
        Pop3SessionState state,
        string expected)
    {
        byte[] line = new Pop3CapabilityParser().FormatCapability(capability, state);

        Assert.That(Encoding.ASCII.GetString(line), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(ValidParameterizedCapabilities))]
    public void RegisteredParameterVariationsParseInterpretAndFormat(
        string line,
        Pop3SessionState state,
        string expected)
    {
        Pop3CapabilityParser parser = new Pop3CapabilityParser();
        Pop3Capability capability = parser.ParseCapability(Encoding.ASCII.GetBytes(line));

        Pop3CapabilitySet result = parser.InterpretCapabilities(new[] { capability }, state);
        byte[] formatted = parser.FormatCapability(capability, state);

        Assert.That(result.Entries, Is.EqualTo(new[] { capability }));
        Assert.That(Encoding.ASCII.GetString(formatted), Is.EqualTo(expected));
    }

    [Test]
    public void UnknownCapabilitiesRoundTripWithoutModification()
    {
        Pop3CapabilityParser parser = new Pop3CapabilityParser();
        Pop3Capability parsed = parser.ParseCapability(Encoding.ASCII.GetBytes("X-Custom Value.One second"));
        byte[] formatted = parser.FormatCapability(parsed, Pop3SessionState.Transaction);

        Assert.That(Encoding.ASCII.GetString(formatted), Is.EqualTo("X-Custom Value.One second"));
    }

    [Test]
    public void ParserAndFormatterEnforceThe511512513OctetCapabilityBoundaries()
    {
        Pop3CapabilityParser parser = new Pop3CapabilityParser();
        string shortName = "X" + new string('A', 508);
        string exactName = "X" + new string('A', 509);
        string longName = "X" + new string('A', 510);
        Pop3Capability shortCapability = Capability(shortName);
        Pop3Capability exact = Capability(exactName);
        Pop3Capability tooLong = Capability(longName);

        Assert.That(parser.ParseCapability(Encoding.ASCII.GetBytes(shortName)).Name, Is.EqualTo(shortName));
        Assert.That(parser.FormatCapability(shortCapability, Pop3SessionState.Authorization), Has.Length.EqualTo(509));
        Assert.That(parser.ParseCapability(Encoding.ASCII.GetBytes(exactName)).Name, Is.EqualTo(exactName));
        Assert.That(parser.FormatCapability(exact, Pop3SessionState.Authorization), Has.Length.EqualTo(510));
        Assert.Throws<InvalidDataException>(() => parser.ParseCapability(Encoding.ASCII.GetBytes(longName)));
        Assert.Throws<InvalidDataException>(() => parser.FormatCapability(tooLong, Pop3SessionState.Authorization));
    }

    [TestCase("")]
    [TestCase(" TOP")]
    [TestCase("TOP ")]
    [TestCase("TOP  extra")]
    [TestCase(".")]
    [TestCase("X.BAD")]
    [TestCase("TOP\textra")]
    public void ParserRejectsMalformedCapabilityGrammar(string line)
    {
        Assert.Throws<InvalidDataException>(() => new Pop3CapabilityParser().ParseCapability(
            Encoding.ASCII.GetBytes(line)));
    }

    [Test]
    public void ParserRejectsControlAndNonAsciiOctets()
    {
        Pop3CapabilityParser parser = new Pop3CapabilityParser();

        Assert.Throws<InvalidDataException>(() => parser.ParseCapability(new byte[] { 84, 79, 80, 0 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseCapability(new byte[] { 84, 79, 80, 32, 127 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseCapability(new byte[] { 88, 32, 195, 169 }));
    }

    [TestCaseSource(nameof(InvalidRegisteredCapabilities))]
    public void RegisteredCapabilityValidationRejectsInvalidParametersAndStates(
        Pop3Capability capability,
        Pop3SessionState state)
    {
        Pop3CapabilityParser parser = new Pop3CapabilityParser();

        Assert.Throws<InvalidDataException>(() => parser.InterpretCapabilities(new[] { capability }, state));
        Assert.Throws<InvalidDataException>(() => parser.FormatCapability(capability, state));
    }

    [Test]
    public void ParserRejectsInvalidObjectsAndSessionStates()
    {
        Pop3CapabilityParser parser = new Pop3CapabilityParser();
        Pop3Capability nullName = Capability("TOP");
        Pop3Capability nullParameters = Capability("TOP");
        Pop3Capability nullParameter = Capability("SASL", "PLAIN");
        nullName.Name = null!;
        nullParameters.Parameters = null!;
        nullParameter.Parameters[0] = null!;

        Assert.Throws<ArgumentNullException>(() => parser.ParseCapabilities(null!, Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => parser.ParseCapabilities(
            new byte[][] { null! }, Pop3SessionState.Authorization));
        Assert.Throws<ArgumentNullException>(() => parser.InterpretCapabilities(null!, Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => parser.InterpretCapabilities(
            new Pop3Capability[] { null! }, Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => parser.FormatCapability(null!, Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => parser.FormatCapability(nullName, Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => parser.FormatCapability(nullParameters, Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => parser.FormatCapability(nullParameter, Pop3SessionState.Authorization));
        Assert.Throws<ArgumentNullException>(() => parser.FindCapabilities(null!, "TOP"));
        Assert.Throws<ArgumentNullException>(() => parser.FindCapabilities(Array.Empty<Pop3Capability>(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.InterpretCapabilities(
            Array.Empty<Pop3Capability>(), Pop3SessionState.Closed));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.FormatCapability(
            Capability("TOP"), Pop3SessionState.Disconnected));
    }

    static IEnumerable<TestCaseData> FormattingCases()
    {
        yield return new TestCaseData(Capability("top"), Pop3SessionState.Authorization, "TOP");
        yield return new TestCaseData(Capability("uSeR"), Pop3SessionState.Transaction, "USER");
        yield return new TestCaseData(Capability("sAsL", "PLAIN", "SCRAM-SHA-256"), Pop3SessionState.Transaction, "SASL PLAIN SCRAM-SHA-256");
        yield return new TestCaseData(Capability("resp-codes"), Pop3SessionState.Authorization, "RESP-CODES");
        yield return new TestCaseData(Capability("login-delay", "300", "user"), Pop3SessionState.Authorization, "LOGIN-DELAY 300 user");
        yield return new TestCaseData(Capability("pIpElInInG"), Pop3SessionState.Transaction, "PIPELINING");
        yield return new TestCaseData(Capability("expire", "never"), Pop3SessionState.Transaction, "EXPIRE never");
        yield return new TestCaseData(Capability("uIdL"), Pop3SessionState.Authorization, "UIDL");
        yield return new TestCaseData(Capability("implementation", "Example", "POP3"), Pop3SessionState.Authorization, "IMPLEMENTATION Example POP3");
        yield return new TestCaseData(Capability("sTlS"), Pop3SessionState.Authorization, "STLS");
        yield return new TestCaseData(Capability("auth-resp-code"), Pop3SessionState.Transaction, "AUTH-RESP-CODE");
        yield return new TestCaseData(Capability("utf8", "user"), Pop3SessionState.Transaction, "UTF8 user");
        yield return new TestCaseData(Capability("lAnG"), Pop3SessionState.Authorization, "LANG");
        yield return new TestCaseData(Capability("X-Vendor", "MiXeD"), Pop3SessionState.Transaction, "X-Vendor MiXeD");
    }

    static IEnumerable<TestCaseData> ValidParameterizedCapabilities()
    {
        yield return new TestCaseData("sasl plain", Pop3SessionState.Authorization, "SASL plain");
        yield return new TestCaseData("SASL PLAIN DIGEST-MD5 GSSAPI ANONYMOUS", Pop3SessionState.Transaction, "SASL PLAIN DIGEST-MD5 GSSAPI ANONYMOUS");
        yield return new TestCaseData("LOGIN-DELAY 0", Pop3SessionState.Authorization, "LOGIN-DELAY 0");
        yield return new TestCaseData("LOGIN-DELAY 2147483647 UsEr", Pop3SessionState.Authorization, "LOGIN-DELAY 2147483647 UsEr");
        yield return new TestCaseData("LOGIN-DELAY 60", Pop3SessionState.Transaction, "LOGIN-DELAY 60");
        yield return new TestCaseData("EXPIRE 0", Pop3SessionState.Authorization, "EXPIRE 0");
        yield return new TestCaseData("EXPIRE 2147483647", Pop3SessionState.Transaction, "EXPIRE 2147483647");
        yield return new TestCaseData("EXPIRE NeVeR uSeR", Pop3SessionState.Authorization, "EXPIRE NeVeR uSeR");
        yield return new TestCaseData("IMPLEMENTATION Example", Pop3SessionState.Authorization, "IMPLEMENTATION Example");
        yield return new TestCaseData("IMPLEMENTATION Example POP3 Server", Pop3SessionState.Transaction, "IMPLEMENTATION Example POP3 Server");
        yield return new TestCaseData("UTF8", Pop3SessionState.Authorization, "UTF8");
        yield return new TestCaseData("UTF8 UsEr", Pop3SessionState.Transaction, "UTF8 UsEr");
    }

    static IEnumerable<TestCaseData> InvalidRegisteredCapabilities()
    {
        yield return new TestCaseData(Capability("TOP", "extra"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("USER", "extra"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("RESP-CODES", "extra"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("PIPELINING", "extra"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("UIDL", "extra"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("STLS", "extra"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("AUTH-RESP-CODE", "extra"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("LANG", "extra"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("SASL"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("SASL", "TOO-LONG-MECHANISM-123"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("SASL", "PLAIN!"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("LOGIN-DELAY"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("LOGIN-DELAY", "-1"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("LOGIN-DELAY", "2147483648"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("LOGIN-DELAY", "30", "GLOBAL"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("LOGIN-DELAY", "30", "USER"), Pop3SessionState.Transaction);
        yield return new TestCaseData(Capability("EXPIRE"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("EXPIRE", "FOREVER"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("EXPIRE", "NEVER", "USER"), Pop3SessionState.Transaction);
        yield return new TestCaseData(Capability("IMPLEMENTATION"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("UTF8", "OTHER"), Pop3SessionState.Authorization);
        yield return new TestCaseData(Capability("STLS"), Pop3SessionState.Transaction);
    }

    static Pop3Capability Capability(string name, params string[] parameters)
    {
        Pop3Capability capability = new Pop3Capability();
        capability.Name = name;
        capability.Parameters = parameters;
        return capability;
    }

    static byte[][] CreateLines(params string[] lines)
    {
        byte[][] result = new byte[lines.Length][];
        int index = 0;
        while (index < lines.Length)
        {
            result[index] = Encoding.ASCII.GetBytes(lines[index]);
            index++;
        }
        return result;
    }
}
