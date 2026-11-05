namespace sip.Tests;

public class SipContactRoutingTests
{
    const string InstanceOne = "<urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6>";

    [Test]
    public void ContactParserRequiresCanonicalUuidInstanceIdentifiers()
    {
        SipHeaderParseResult valid = ParseContactHeader(
            "<sip:a@example.com>;+sip.instance=\"<URN:UUID:F81D4FAE-7DEC-11D0-A765-00A0C91E6BF6>\"");
        SipHeaderParseResult invalidHex = ParseContactHeader(
            "<sip:a@example.com>;+sip.instance=\"<urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bfg>\"");
        SipHeaderParseResult invalidShape = ParseContactHeader(
            "<sip:a@example.com>;+sip.instance=\"<urn:uuid:f81d4fae7dec11d0a76500a0c91e6bf6>\"");

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(invalidHex.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(invalidShape.Status, Is.EqualTo(SipParseStatus.Error));
        });
    }

    [Test]
    public void NormalizesWildcardAndOutboundRegistrationSemantics()
    {
        SipMessage wildcard = ParseRegister("Contact: *\r\nExpires: 0\r\n");
        SipContactValue[] wildcardResult = SipContactRouting.NormalizeRegisterContacts(
            wildcard, 0, 3600);

        SipMessage orphanRequest = ParseRegister(
            "Contact: <sip:a@device.example.com>;reg-id=3;expires=120\r\n");
        SipContactValue[] orphanResult = SipContactRouting.NormalizeRegisterContacts(
            orphanRequest, -1, 3600);

        SipMessage competing = ParseRegister(
            "Contact: <sip:a@one.example.com>;reg-id=1;+sip.instance=\"" + InstanceOne + "\", "
            + "<sip:a@two.example.com>;reg-id=2;+sip.instance=\"" + InstanceOne + "\"\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(wildcardResult.Single().Wildcard, Is.True);
            Assert.That(wildcardResult[0], Is.Not.SameAs(wildcard.ContactValues[0]));
            Assert.Throws<SipProtocolException>(() => SipContactRouting.NormalizeRegisterContacts(
                wildcard, -1, 3600));
            Assert.That(orphanResult[0].RegistrationId, Is.EqualTo(-1));
            Assert.That(orphanResult[0].Address!.Parameters.Select(parameter => parameter.Name),
                Does.Not.Contain("reg-id"));
            Assert.That(orphanRequest.ContactValues[0].RegistrationId, Is.EqualTo(3));
            Assert.Throws<SipProtocolException>(() => SipContactRouting.NormalizeRegisterContacts(
                competing, -1, 3600));
        });
    }

    [Test]
    public void ComparesUriBindingsAndOutboundInstanceBindings()
    {
        SipContactValue uriLeft = ParseContact(
            "<sip:%61lice@EXAMPLE.com;transport=TCP;foo=bar>;q=0.1;expires=10");
        SipContactValue uriRight = ParseContact(
            "<sip:alice@example.COM;foo=bar;Transport=tcp>;q=0.9;expires=600");
        SipContactValue outboundLeft = ParseContact(
            "<sip:a@old.example.com>;reg-id=7;+sip.instance=\"" + InstanceOne + "\"");
        SipContactValue outboundRight = ParseContact(
            "<sip:a@new.example.com>;reg-id=7;+sip.instance=\"" + InstanceOne.ToUpperInvariant() + "\"");
        SipContactValue otherFlow = ParseContact(
            "<sip:a@new.example.com>;reg-id=8;+sip.instance=\"" + InstanceOne + "\"");
        SipContactValue wildcard = ParseContact("*");

        Assert.Multiple(() =>
        {
            Assert.That(SipContactRouting.ContactsMatch(uriLeft, uriRight), Is.True);
            Assert.That(SipContactRouting.ContactsMatch(outboundLeft, outboundRight), Is.True);
            Assert.That(SipContactRouting.ContactsMatch(outboundLeft, otherFlow), Is.False);
            Assert.That(SipContactRouting.ContactsMatch(wildcard, wildcard), Is.True);
            Assert.That(SipContactRouting.ContactsMatch(wildcard, uriLeft), Is.False);
        });
    }

    [Test]
    public void OrdersContactsByQualityThenEffectiveExpiry()
    {
        SipContactValue[] contacts = ParseContactValues(
            "<sip:a@low.example.com>;q=0.5;expires=300, "
            + "<sip:a@short.example.com>;q=0.9;expires=60, "
            + "<sip:a@long.example.com>;q=0.9;expires=600, "
            + "<sip:a@default.example.com>");

        SipContactValue[] ordered = SipContactRouting.OrderContacts(contacts, 120, 3600);
        ordered[0].Address!.Uri!.Host = "changed.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(ordered.Select(ContactHost), Is.EqualTo(new string[]
            {
                "changed.example.com", "long.example.com", "short.example.com", "low.example.com"
            }));
            Assert.That(contacts[3].Address!.Uri!.Host, Is.EqualTo("default.example.com"));
            Assert.That(SipContactRouting.GetEffectiveExpires(contacts[3], 120, 3600), Is.EqualTo(120));
            Assert.That(SipContactRouting.GetEffectiveExpires(contacts[0], 120, 3600), Is.EqualTo(300));
            Assert.That(SipContactRouting.GetEffectiveExpires(contacts[3], -1, 3600), Is.EqualTo(3600));
        });
    }

    [Test]
    public void PreservesContactOrderForEqualPreferencesAndHonorsZeroExpiry()
    {
        SipContactValue[] contacts = ParseContactValues(
            "<sip:a@first.example.com>;q=0.5;expires=60, "
            + "<sip:a@second.example.com>;q=0.5;expires=60, "
            + "<sip:a@removed.example.com>;q=0.5;expires=0");

        SipContactValue[] ordered =
            SipContactRouting.OrderContacts(contacts, 120, 3600);

        Assert.Multiple(() =>
        {
            Assert.That(ordered.Select(ContactHost), Is.EqualTo(
                new string[]
                {
                    "first.example.com", "second.example.com", "removed.example.com"
                }));
            Assert.That(SipContactRouting.GetEffectiveExpires(
                contacts[2], 120, 3600), Is.Zero);
            Assert.That(ordered[0], Is.Not.SameAs(contacts[0]));
        });
    }

    [Test]
    public void AppliesOnlySuccessfulTargetRefreshMessages()
    {
        SipDialogSnapshot dialog = new SipDialogSnapshot();
        dialog.RemoteTarget = ParseUri("sip:bob@old.example.com");
        dialog.RouteSet = ParseRouteValues("<sip:route.example.com;lr>");
        SipRouteValue[] originalRoutes = dialog.RouteSet;

        bool inviteUpdated = SipContactRouting.ApplyTargetRefresh(dialog,
            ParseRequest("INVITE", "Contact: <sip:bob@invite.example.com>\r\n"));
        bool ackIgnored = SipContactRouting.ApplyTargetRefresh(dialog,
            ParseRequest("ACK", "Contact: <sip:bob@ack.example.com>\r\n"));
        bool failedIgnored = SipContactRouting.ApplyTargetRefresh(dialog,
            ParseResponse(488, "UPDATE", "Contact: <sip:bob@failed.example.com>\r\n"));
        bool updateSucceeded = SipContactRouting.ApplyTargetRefresh(dialog,
            ParseResponse(200, "UPDATE", "Contact: <sip:bob@update.example.com>\r\n"));

        SipDialogSnapshot secure = new SipDialogSnapshot();
        secure.Secure = true;
        secure.RemoteTarget = ParseUri("sips:bob@secure.example.com");

        Assert.Multiple(() =>
        {
            Assert.That(inviteUpdated, Is.True);
            Assert.That(ackIgnored, Is.False);
            Assert.That(failedIgnored, Is.False);
            Assert.That(updateSucceeded, Is.True);
            Assert.That(dialog.RemoteTarget!.Host, Is.EqualTo("update.example.com"));
            Assert.That(dialog.RouteSet, Is.SameAs(originalRoutes));
            Assert.Throws<SipProtocolException>(() => SipContactRouting.ApplyTargetRefresh(secure,
                ParseRequest("NOTIFY", "Contact: <sip:bob@insecure.example.com>\r\n")));
            Assert.That(secure.RemoteTarget!.Host, Is.EqualTo("secure.example.com"));
        });
    }

    [Test]
    public void SelectsPreferredGruuAndValidatesGruuSemantics()
    {
        SipContactValue contact = ParseContact(
            "<sip:a@device.example.com>;pub-gruu=\"sips:a@example.com;gr=public\";"
            + "temp-gruu=\"sip:temporary@example.com;gr\"");
        SipUri temporary = SipContactRouting.SelectGruu(contact, true, false);
        SipUri publicGruu = SipContactRouting.SelectGruu(contact, false, true);
        SipContactValue plain = ParseContact("<sip:a@plain.example.com>");
        SipUri fallback = SipContactRouting.SelectGruu(plain, true, false);
        SipContactValue invalidGruu = ParseContact(
            "<sip:a@device.example.com>;pub-gruu=\"sip:a@example.com\"");

        Assert.Multiple(() =>
        {
            Assert.That(temporary.Host, Is.EqualTo("example.com"));
            Assert.That(temporary.User, Is.EqualTo("temporary"));
            Assert.That(publicGruu.Scheme, Is.EqualTo(SipUriScheme.Sips));
            Assert.That(fallback.Host, Is.EqualTo("plain.example.com"));
            Assert.Throws<ArgumentException>(() => SipContactRouting.SelectGruu(
                invalidGruu, false, false));
            Assert.Throws<SipProtocolException>(() => SipContactRouting.SelectGruu(
                contact, true, true));
        });
    }

    [Test]
    public void CreatesRegistrationBindingWithObservedAndFlowMetadata()
    {
        SipContactValue contact = ParseContact(
            "<sip:a@device.example.com;ob>;q=0.750;expires=90;reg-id=4;"
            + "+sip.instance=\"" + InstanceOne + "\"");
        SipUri addressOfRecord = ParseUri("sip:a@example.com");
        SipEndpointAddress observed = Endpoint("198.51.100.10", 62000, SipTransportKind.Tcp);
        SipFlow flow = new SipFlow();
        flow.Id = "flow-4";
        flow.InstanceId = InstanceOne;
        flow.RegistrationId = 4;

        SipRegistrationBinding binding = SipContactRouting.CreateRegistrationBinding(
            addressOfRecord, contact, "call-id", 22, -1, 3600, 1000, observed, flow);
        contact.Address!.Uri!.Host = "changed.example.com";
        observed.Host = "changed-address";
        flow.Id = "changed-flow";

        SipFlow wrongFlow = new SipFlow();
        wrongFlow.InstanceId = InstanceOne;
        wrongFlow.RegistrationId = 5;

        Assert.Multiple(() =>
        {
            Assert.That(binding.Contact!.Address!.Uri!.Host, Is.EqualTo("device.example.com"));
            Assert.That(binding.ExpiresAtMilliseconds, Is.EqualTo(91000));
            Assert.That(binding.QualityThousandths, Is.EqualTo(750));
            Assert.That(binding.ReceivedAddress!.Host, Is.EqualTo("198.51.100.10"));
            Assert.That(binding.Flow!.Id, Is.EqualTo("flow-4"));
            Assert.That(binding.InstanceId, Is.EqualTo(InstanceOne));
            Assert.That(binding.RegistrationId, Is.EqualTo(4));
            Assert.Throws<ArgumentException>(() => SipContactRouting.CreateRegistrationBinding(
                addressOfRecord, binding.Contact, "call-id", 23, -1, 3600, 2000,
                binding.ReceivedAddress, wrongFlow));
        });
    }

    static SipMessage ParseRegister(string contactHeaders)
    {
        return ParseMessage("REGISTER sip:example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-contact\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:a@example.com>;tag=from-tag\r\n"
            + "To: <sip:a@example.com>\r\n"
            + "Call-ID: contact-call@example.com\r\n"
            + "CSeq: 1 REGISTER\r\n"
            + contactHeaders
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage ParseRequest(string method, string contactHeader)
    {
        return ParseMessage(method + " sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-refresh\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:a@example.com>;tag=from-tag\r\n"
            + "To: <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\n"
            + "CSeq: 2 " + method + "\r\n"
            + contactHeader
            + "Content-Length: 0\r\n\r\n");
    }

    static SipMessage ParseResponse(int status, string method, string contactHeader)
    {
        return ParseMessage("SIP/2.0 " + status.ToString() + " Result\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-refresh\r\n"
            + "From: <sip:a@example.com>;tag=from-tag\r\n"
            + "To: <sip:bob@example.com>;tag=to-tag\r\n"
            + "Call-ID: refresh-call@example.com\r\n"
            + "CSeq: 2 " + method + "\r\n"
            + contactHeader
            + "Content-Length: 0\r\n\r\n");
    }

    static SipContactValue ParseContact(string value)
    {
        return ParseContactHeader(value).ContactValues.Single();
    }

    static SipContactValue[] ParseContactValues(string value)
    {
        return ParseContactHeader(value).ContactValues;
    }

    static SipHeaderParseResult ParseContactHeader(string value)
    {
        return new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.ASCII.GetBytes("Contact: " + value + "\r\n"),
            new SipParseOptions());
    }

    static SipRouteValue[] ParseRouteValues(string value)
    {
        SipHeaderParseResult result = new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.ASCII.GetBytes("Route: " + value + "\r\n"),
            new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.RouteValues;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Uri!;
    }

    static SipMessage ParseMessage(string text)
    {
        SipParseResult result = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
        return result.Message!;
    }

    static SipEndpointAddress Endpoint(string host, int port, SipTransportKind transport)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = transport;
        return endpoint;
    }

    static string ContactHost(SipContactValue contact)
    {
        return contact.Address!.Uri!.Host!;
    }
}
