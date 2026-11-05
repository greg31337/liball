namespace sip.Tests;

public class Rfc4475ConformanceTests
{
    static string[] ValidParserMessages =
    [
        "wsinv", "intmeth", "esc01", "escnull", "esc02", "lwsdisp", "longreq",
        "dblreq", "semiuri", "transports", "mpart01", "unreason", "noreason"
    ];

    static string[] InvalidParserMessages =
    [
        "badinv01", "clerr", "ncl", "scalar02", "scalarlg", "quotbal", "ltgtruri",
        "lwsruri", "lwsstart", "trws", "escruri", "baddate", "regbadct", "badaspec",
        "baddn", "badvers", "mismatch01", "mismatch02", "bigcode"
    ];

    static object[] SemanticMessages =
    [
        new object[] { "badbranch", true, SipParseStatus.Complete },
        new object[] { "insuf", true, SipParseStatus.Error },
        new object[] { "unkscm", true, SipParseStatus.Complete },
        new object[] { "novelsc", true, SipParseStatus.Complete },
        new object[] { "unksm2", false, SipParseStatus.Complete },
        new object[] { "bext01", true, SipParseStatus.Complete },
        new object[] { "invut", true, SipParseStatus.Complete },
        new object[] { "regaut01", true, SipParseStatus.Complete },
        new object[] { "multi01", true, SipParseStatus.Error },
        new object[] { "mcl01", true, SipParseStatus.Error },
        new object[] { "bcast", true, SipParseStatus.Complete },
        new object[] { "zeromf", true, SipParseStatus.Complete },
        new object[] { "cparam01", true, SipParseStatus.Complete },
        new object[] { "cparam02", true, SipParseStatus.Complete },
        new object[] { "regescrt", true, SipParseStatus.Complete },
        new object[] { "sdp01", true, SipParseStatus.Complete },
        new object[] { "inv2543", false, SipParseStatus.Complete }
    ];

    [TestCaseSource(nameof(ValidParserMessages))]
    public void AcceptsAndCanonicallyRoundTripsValidParserMessages(string name)
    {
        SipMessageCodec codec = new SipMessageCodec();
        SipParseResult parsed = codec.ParseDatagramMessage(
            RfcTortureFixture.Read4475(name), Options(true, true));

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.AllowHeaderFolding = true;
        SipWriteResult formatted = codec.SerializeMessage(parsed.Message!, writeOptions);
        Assert.That(formatted.Success, Is.True, formatted.ErrorMessage);

        SipParseResult reparsed = codec.ParseDatagramMessage(formatted.Bytes, Options(false, true));
        Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete), reparsed.ErrorMessage);
    }

    [TestCaseSource(nameof(InvalidParserMessages))]
    public void RejectsInvalidParserMessagesInStrictMode(string name)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            RfcTortureFixture.Read4475(name), Options(false, true));

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error), name);
        Assert.That(result.ErrorOffset, Is.GreaterThanOrEqualTo(0), name);
    }

    [TestCaseSource(nameof(SemanticMessages))]
    public void AppliesTransactionApplicationAndCompatibilityOutcomes(
        string name, bool strict, SipParseStatus expected)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            RfcTortureFixture.Read4475(name), Options(true, strict));

        Assert.That(result.Status, Is.EqualTo(expected), result.ErrorMessage);
    }

    [Test]
    public void DecodesEscapedUriComponentsWithoutRecursiveUnescaping()
    {
        SipMessage message = Parse("esc01", true).Message!;
        SipContactValue contact = message.ContactValues.Single();

        Assert.Multiple(() =>
        {
            Assert.That(message.RequestUri!.User, Is.EqualTo("sips:user@example.com"));
            Assert.That(message.To!.Uri!.User, Is.EqualTo("user"));
            Assert.That(message.From!.Uri!.User, Is.EqualTo("I have spaces"));
            Assert.That(contact.Address!.Uri!.User, Is.EqualTo("caller"));
            Assert.That(contact.Address.Uri.Parameters.Single(value => value.Name == "name").Value,
                Is.EqualTo("value%41"));
        });
    }

    [Test]
    public void PreservesInputsThatDriveRoutingOutcomes()
    {
        SipMessage unknownScheme = Parse("unkscm", true).Message!;
        SipMessage zeroHops = Parse("zeromf", true).Message!;
        SipMessage broadcast = Parse("bcast", true).Message!;

        Assert.Multiple(() =>
        {
            Assert.That(unknownScheme.RequestUri!.Scheme, Is.EqualTo(SipUriScheme.Absolute));
            Assert.That(unknownScheme.RequestUri.SchemeToken, Is.EqualTo("nobodyKnowsThisScheme"));
            Assert.That(unknownScheme.To!.Uri!.Scheme, Is.EqualTo(SipUriScheme.Sip));
            Assert.That(HeaderValue(zeroHops, "Max-Forwards"), Is.EqualTo("0"));
            Assert.That(broadcast.ViaValues, Has.Length.EqualTo(2));
            Assert.That(broadcast.ViaValues[1].Host, Is.EqualTo("255.255.255.255"));
        });
    }

    [Test]
    public void DistinguishesContactParametersFromUriParameters()
    {
        SipContactValue contactParameter = Parse("cparam01", true).Message!.ContactValues.Single();
        SipContactValue uriParameter = Parse("cparam02", true).Message!.ContactValues.Single();
        SipContactValue escapedRoute = Parse("regescrt", true).Message!.ContactValues.Single();

        Assert.Multiple(() =>
        {
            Assert.That(contactParameter.Address!.Parameters.Select(value => value.Name),
                Does.Contain("unknownparam"));
            Assert.That(contactParameter.Address.Uri!.Parameters, Is.Empty);
            Assert.That(uriParameter.Address!.Parameters, Is.Empty);
            Assert.That(uriParameter.Address.Uri!.Parameters.Select(value => value.Name),
                Does.Contain("unknownparam"));
            Assert.That(escapedRoute.Address!.Uri!.Headers.Single().Name, Is.EqualTo("Route"));
            Assert.That(escapedRoute.Address.Uri.Headers.Single().Value,
                Is.EqualTo("<sip:sip.example.com>"));
        });
    }

    [Test]
    public void ReportsSemanticErrorsWithoutTurningThemIntoParserCrashes()
    {
        SipMessageCodec codec = new SipMessageCodec();
        SipParseResult unusualSchemes = codec.ParseDatagramMessage(
            RfcTortureFixture.Read4475("unksm2"), Options(true, false));
        SipParseResult missingHeaders = codec.ParseDatagramMessage(
            RfcTortureFixture.Read4475("insuf"), Options(true, true));

        Assert.That(unusualSchemes.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(codec.ValidateMessage(unusualSchemes.Message!), Is.Not.Empty);
        Assert.That(missingHeaders.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(missingHeaders.ValidationIssues.Select(value => value.Code),
            Does.Contain("header-missing"));
    }

    static SipParseResult Parse(string name, bool strict)
    {
        return new SipMessageCodec().ParseDatagramMessage(
            RfcTortureFixture.Read4475(name), Options(true, strict));
    }

    static SipParseOptions Options(bool compatibleWhitespace, bool strict)
    {
        SipParseOptions options = new SipParseOptions();
        options.Strict = strict;
        options.AllowHeaderFolding = true;
        options.AllowCompatibleWhitespace = compatibleWhitespace;
        return options;
    }

    static string? HeaderValue(SipMessage message, string name)
    {
        return message.Headers.Single(value => value.Name!.Equals(
            name, StringComparison.OrdinalIgnoreCase)).Value;
    }
}
