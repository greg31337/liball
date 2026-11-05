namespace sip.Tests;

public class SipRegistrarResponseTests
{
    const string Instance =
        "<urn:uuid:00000000-0000-1000-8000-000000000009>";

    [Test]
    public void SuccessResponseContainsCompleteContactsDateAndCapabilities()
    {
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrarOptions options = Options();
        SipRegistrationSnapshot snapshot = registrar.ProcessRegistration(
            Register("response-call", 1, "",
                "Contact: <sip:alice@one.example.com>;expires=120;q=0.4\r\n"
                + "Contact: <sip:alice@two.example.com>;expires=180;q=0.8\r\n"),
            options);
        SipMessage response = snapshot.Response!;
        SipWriteResult serialized = new SipMessageCodec().SerializeMessage(
            response, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(serialized.Success, Is.True, serialized.ErrorMessage);
            Assert.That(response.ReasonPhrase, Is.EqualTo(SipReasonPhrases.Ok));
            Assert.That(response.ContactValues.Select(ContactHost), Is.EqualTo(
                new string[] { "two.example.com", "one.example.com" }));
            Assert.That(response.ContactValues[0].ExpiresSeconds, Is.EqualTo(180));
            Assert.That(response.ContactValues[1].ExpiresSeconds, Is.EqualTo(120));
            Assert.That(FindHeader(response, SipHeaderNames.Contact), Is.Not.Null);
            Assert.That(FindHeader(response, SipHeaderNames.Date), Is.Not.Null);
            Assert.That(Header(response, SipHeaderNames.Allow), Is.EqualTo("REGISTER"));
            Assert.That(Header(response, SipHeaderNames.Supported),
                Is.EqualTo("path, outbound, gruu"));
            Assert.That(response.To!.Parameters.Any(parameter =>
                parameter.Name!.Equals("tag",
                    System.StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    public void FailureResponsesDistinguishProtocolPolicyAndExtensionErrors()
    {
        SipRegistrarOptions options = Options();
        SipRegistrationSnapshot tooBrief = new SipRegistrar().ProcessRegistration(
            Register("brief-call", 1, "",
                "Contact: <sip:alice@device.example.com>;expires=30\r\n"),
            options);
        SipRegistrationSnapshot forbidden = new SipRegistrar().ProcessRegistration(
            RegisterFor("sip:alice@elsewhere.example",
                "sip:alice@elsewhere.example", "forbidden-call", 1, "",
                "Contact: <sip:alice@device.example.com>\r\n"), options);
        SipRegistrationSnapshot unsupported = new SipRegistrar().ProcessRegistration(
            Register("extension-call", 1, "Require: imaginary-feature\r\n",
                "Contact: <sip:alice@device.example.com>\r\n"), options);
        SipRegistrationSnapshot malformed = new SipRegistrar().ProcessRegistration(
            Register("wildcard-call", 1, "Expires: 120\r\n", "Contact: *\r\n"),
            options);
        SipMessage wrongMethod = Register("method-call", 1, "", "");
        wrongMethod.Method = SipMethod.Options;
        wrongMethod.MethodToken = SipMethods.Options;
        wrongMethod.CSeq!.Method = SipMethod.Options;
        wrongMethod.CSeq.MethodToken = SipMethods.Options;
        SipRegistrationSnapshot method = new SipRegistrar()
            .ProcessRegistration(wrongMethod, options);
        SipMessage unsupportedContact = Register("uri-call", 1, "",
            "Contact: <sip:alice@device.example.com>\r\n");
        unsupportedContact.ContactValues[0].Address!.Uri =
            ParseUri("tel:+12025550123");
        SipRegistrationSnapshot uri = new SipRegistrar()
            .ProcessRegistration(unsupportedContact, options);

        Assert.Multiple(() =>
        {
            Assert.That(tooBrief.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.IntervalTooBrief));
            Assert.That(Header(tooBrief.Response, SipHeaderNames.MinExpires),
                Is.EqualTo("60"));
            Assert.That(forbidden.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Forbidden));
            Assert.That(unsupported.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.BadExtension));
            Assert.That(Header(unsupported.Response, SipHeaderNames.Unsupported),
                Is.EqualTo("imaginary-feature"));
            Assert.That(malformed.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(FindHeader(malformed.Response, SipHeaderNames.Date),
                Is.Not.Null);
            Assert.That(method.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.MethodNotAllowed));
            Assert.That(Header(method.Response, SipHeaderNames.Allow),
                Is.EqualTo(SipMethods.Register));
            Assert.That(uri.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.UnsupportedUriScheme));
        });
    }

    [Test]
    public void PathOutboundAndGruuMetadataAreStoredAndReturned()
    {
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrarOptions options = Options();
        string features = "Supported: path, outbound, gruu\r\n"
            + "Path: <sip:edge.example.com;lr;ob>\r\n";
        string contact = "Contact: <sip:alice@device.example.com;ob>"
            + ";expires=120;reg-id=1;+sip.instance=\"" + Instance + "\"\r\n";

        SipRegistrationSnapshot initial = registrar.ProcessRegistration(
            Register("feature-call", 1, features, contact), options);
        SipRegistrationSnapshot refreshed = registrar.ProcessRegistration(
            Register("feature-call", 2, features, contact), options);
        SipContactValue initialContact = initial.Bindings[0].Contact!;
        SipContactValue refreshedContact = refreshed.Bindings[0].Contact!;

        Assert.Multiple(() =>
        {
            Assert.That(initial.State, Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(initial.Bindings[0].Path, Has.Length.EqualTo(1));
            Assert.That(initial.Bindings[0].Path[0].Address!.Uri!.Host,
                Is.EqualTo("edge.example.com"));
            Assert.That(initialContact.PublicGruu,
                Does.StartWith("sip:alice@example.com;gr="));
            Assert.That(initialContact.TemporaryGruu,
                Does.Contain(";gr"));
            Assert.That(refreshedContact.PublicGruu,
                Is.EqualTo(initialContact.PublicGruu));
            Assert.That(refreshedContact.TemporaryGruu,
                Is.Not.EqualTo(initialContact.TemporaryGruu));
            Assert.That(Header(refreshed.Response!, SipHeaderNames.Require),
                Is.EqualTo(SipOptionTags.Outbound));
            Assert.That(Header(refreshed.Response!, SipHeaderNames.Path),
                Is.EqualTo("<sip:edge.example.com;lr;ob>"));
            Assert.That(refreshed.Response!.ContactValues[0].PublicGruu,
                Is.EqualTo(refreshedContact.PublicGruu));
        });
    }

    [Test]
    public void ConfiguredDigestAuthenticationChallengesAndReturnsAuthenticationInfo()
    {
        RegistrarCredentialProvider provider = new RegistrarCredentialProvider();
        SipCredential credential = new SipCredential();
        credential.UserName = "alice";
        credential.Secret = System.Text.Encoding.UTF8.GetBytes("password");
        credential.Realm = "example.com";
        credential.Scope = SipCredentialScope.Origin;
        provider.Credential = credential;
        SipAuthenticationServer server = new SipAuthenticationServer(provider);
        SipAuthenticationClient client = new SipAuthenticationClient(provider);
        SipRegistrar registrar = new SipRegistrar();
        SipNonceOptions nonce = NonceOptions();
        registrar.ConfigureRegistrarAuthenticationServer(server, nonce);
        SipRegistrarOptions options = Options();
        options.RequireAuthentication = true;
        SipMessage request = Register("authentication-call", 1, "",
            "Contact: <sip:alice@device.example.com>;expires=120\r\n");

        SipRegistrationSnapshot challenged =
            registrar.ProcessRegistration(request, options);
        SipAuthenticationRequest authorization = new SipAuthenticationRequest();
        authorization.Message = request;
        authorization.Target = request.RequestUri;
        authorization.Scope = SipCredentialScope.Origin;
        authorization.Challenges = ReadChallenges(challenged.Response!);
        SipAuthenticationResult authorized = client.AuthorizeRequest(authorization);
        SipRegistrationSnapshot registered = registrar.ProcessRegistration(
            authorized.Message!, options);

        Assert.Multiple(() =>
        {
            Assert.That(challenged.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Unauthorized));
            Assert.That(FindHeader(challenged.Response,
                SipHeaderNames.WwwAuthenticate), Is.Not.Null);
            Assert.That(registered.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(registered.Response!.StatusCode,
                Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(FindHeader(registered.Response,
                SipHeaderNames.AuthenticationInfo), Is.Not.Null);
            Assert.That(provider.Lookups, Is.EqualTo(2));
        });
    }

    static SipRegistrarOptions Options()
    {
        SipRegistrarOptions options = new SipRegistrarOptions();
        options.RequireAuthentication = false;
        options.DefaultExpiresSeconds = 120;
        options.MinExpiresSeconds = 60;
        options.MaxExpiresSeconds = 600;
        options.MaxBindingCount = 10;
        options.Domains = new string[] { "example.com" };
        return options;
    }

    static SipNonceOptions NonceOptions()
    {
        SipNonceOptions options = new SipNonceOptions();
        options.Realm = "example.com";
        options.CurrentKey = new byte[32];
        int index = 0;
        while (index < options.CurrentKey.Length)
        {
            options.CurrentKey[index] = (byte)(index + 1);
            index++;
        }
        return options;
    }

    static SipMessage Register(string callId, int cseq,
        string additionalHeaders, string contacts)
    {
        return RegisterFor("sip:alice@example.com", "sip:alice@example.com",
            callId, cseq, additionalHeaders, contacts);
    }

    static SipMessage RegisterFor(string to, string from, string callId,
        int cseq, string additionalHeaders, string contacts)
    {
        string text = "REGISTER sip:example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-response\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <" + from + ">;tag=from-tag\r\n"
            + "To: <" + to + ">\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq.ToString() + " REGISTER\r\n"
            + additionalHeaders + contacts
            + "Content-Length: 0\r\n\r\n";
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipDigestChallenge[] ReadChallenges(SipMessage response)
    {
        System.Collections.Generic.List<SipDigestChallenge> challenges =
            new System.Collections.Generic.List<SipDigestChallenge>();
        int index = 0;
        while (index < response.Headers.Length)
        {
            SipHeader header = response.Headers[index];
            if (header.Name!.Equals(SipHeaderNames.WwwAuthenticate,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                SipHeaderParseResult parsed = new SipHeaderCodec().ParseHeader(
                    System.Text.Encoding.ASCII.GetBytes(header.Name + ": "
                        + header.Value + "\r\n"), new SipParseOptions());
                challenges.Add(parsed.DigestChallenge!);
            }
            index++;
        }
        return challenges.ToArray();
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }

    static SipHeader? FindHeader(SipMessage message, string name)
    {
        return message.Headers.FirstOrDefault(header => header.Name!.Equals(
            name, System.StringComparison.OrdinalIgnoreCase));
    }

    static string? Header(SipMessage message, string name)
    {
        return FindHeader(message, name)?.Value;
    }

    static string ContactHost(SipContactValue contact)
    {
        return contact.Address!.Uri!.Host!;
    }
}

class RegistrarCredentialProvider : ISipCredentialProvider
{
    internal SipCredential Credential = new SipCredential();
    internal int Lookups;

    public SipCredential GetCredential(SipAuthenticationRequest request)
    {
        Lookups++;
        return Credential;
    }
}
