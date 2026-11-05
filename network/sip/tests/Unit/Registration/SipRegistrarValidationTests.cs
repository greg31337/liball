namespace sip.Tests;

public class SipRegistrarValidationTests
{
    [Test]
    public void RegistersMultipleContactsUsingExpiryPrecedenceAndBounds()
    {
        SipLocationService location = new SipLocationService();
        SipRegistrar registrar = new SipRegistrar();
        registrar.ConfigureRegistrarLocationService(location);
        registrar.ExpireRegistrarBindings(1000);
        SipRegistrarOptions options = Options();
        options.DefaultExpiresSeconds = 180;
        options.MaxExpiresSeconds = 240;
        SipMessage request = Register("sip:alice@example.com",
            "sip:alice@example.com", "registration-call", 1,
            "Expires: 300\r\n",
            "Contact: <sip:alice@one.example.com>;expires=120;q=0.5\r\n"
            + "Contact: <sip:alice@two.example.com>;q=0.9\r\n");

        SipRegistrationSnapshot snapshot =
            registrar.ProcessRegistration(request, options);
        SipLocationResult stored = location.QueryLocation(
            LocationQuery("sip:alice@example.com", -1));

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.State, Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(snapshot.Bindings.Select(ContactHost), Is.EqualTo(
                new string[] { "two.example.com", "one.example.com" }));
            Assert.That(snapshot.Bindings[0].ExpiresAtMilliseconds,
                Is.EqualTo(241000));
            Assert.That(snapshot.Bindings[1].ExpiresAtMilliseconds,
                Is.EqualTo(121000));
            Assert.That(snapshot.ExpiresAtMilliseconds, Is.EqualTo(121000));
            Assert.That(snapshot.CallId, Is.EqualTo("registration-call"));
            Assert.That(snapshot.CSeq, Is.EqualTo(1));
            Assert.That(stored.Bindings, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void RemovesOneContactAndSupportsWildcardUnregistration()
    {
        SipRegistrar registrar = new SipRegistrar();
        SipRegistrarOptions options = Options();
        registrar.ProcessRegistration(Register("sip:alice@example.com",
            "sip:alice@example.com", "remove-call", 1, "",
            "Contact: <sip:alice@one.example.com>;expires=120\r\n"
            + "Contact: <sip:alice@two.example.com>;expires=120\r\n"), options);

        SipRegistrationSnapshot oneRemaining = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "remove-call", 2, "",
                "Contact: <sip:alice@one.example.com>;expires=0\r\n"), options);
        SipRegistrationSnapshot removedAll = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "remove-call", 3, "Expires: 0\r\n", "Contact: *\r\n"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(oneRemaining.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(oneRemaining.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "two.example.com" }));
            Assert.That(removedAll.State,
                Is.EqualTo(SipRegistrationState.Terminated));
            Assert.That(removedAll.Bindings, Is.Empty);
        });
    }

    [Test]
    public void EnforcesCallIdCSeqOrderingAndRetransmissionIdempotence()
    {
        TrackingLocationService location = new TrackingLocationService();
        SipRegistrar registrar = new SipRegistrar();
        registrar.ConfigureRegistrarLocationService(location);
        SipRegistrarOptions options = Options();
        SipMessage original = Register("sip:alice@example.com",
            "sip:alice@example.com", "ordered-call", 10, "",
            "Contact: <sip:alice@original.example.com>;expires=120\r\n");
        registrar.ProcessRegistration(original, options);

        SipRegistrationSnapshot retransmission =
            registrar.ProcessRegistration(original, options);
        SipRegistrationSnapshot sameSequenceChanged =
            registrar.ProcessRegistration(Register("sip:alice@example.com",
                "sip:alice@example.com", "ordered-call", 10, "",
                "Contact: <sip:alice@changed.example.com>;expires=120\r\n"),
                options);
        SipRegistrationSnapshot older = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "ordered-call", 9, "",
                "Contact: <sip:alice@older.example.com>;expires=120\r\n"),
            options);
        SipRegistrationSnapshot newCall = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "new-call", 1, "",
                "Contact: <sip:alice@original.example.com>;expires=180\r\n"),
            options);

        Assert.Multiple(() =>
        {
            Assert.That(retransmission.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "original.example.com" }));
            Assert.That(sameSequenceChanged.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "original.example.com" }));
            Assert.That(location.ReplaceCalls, Is.EqualTo(2));
            Assert.That(older.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(older.ErrorMessage, Does.Contain("older"));
            Assert.That(newCall.State, Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(newCall.Bindings[0].CallId, Is.EqualTo("new-call"));
            Assert.That(newCall.Bindings[0].CSeq, Is.EqualTo(1));
        });
    }

    [Test]
    public void AppliesDomainThirdPartyAndAddressOfRecordPolicy()
    {
        SipRegistrarOptions options = Options();
        SipRegistrationSnapshot wrongDomain = new SipRegistrar()
            .ProcessRegistration(Register("sip:alice@elsewhere.example",
                "sip:alice@elsewhere.example", "domain-call", 1, "",
                "Contact: <sip:alice@device.example.com>\r\n"), options);
        SipRegistrationSnapshot thirdParty = new SipRegistrar()
            .ProcessRegistration(Register("sip:alice@example.com",
                "sip:bob@example.com", "third-party-call", 1, "",
                "Contact: <sip:alice@device.example.com>\r\n"), options);
        options.AllowThirdPartyRegistration = true;
        SipRegistrationSnapshot allowed = new SipRegistrar()
            .ProcessRegistration(Register("sip:alice@example.com",
                "sip:bob@example.com", "allowed-call", 1, "",
                "Contact: <sip:alice@device.example.com>\r\n"), options);
        SipMessage taggedRequest = Register("sip:alice@example.com",
            "sip:alice@example.com", "tagged-call", 1, "",
            "Contact: <sip:alice@device.example.com>\r\n");
        SipUriParameter tag = new SipUriParameter();
        tag.Name = "tag";
        tag.Value = "unexpected";
        tag.HasValue = true;
        taggedRequest.To!.Parameters = new SipUriParameter[] { tag };
        SipRegistrationSnapshot tagged = new SipRegistrar()
            .ProcessRegistration(taggedRequest, options);

        Assert.Multiple(() =>
        {
            Assert.That(wrongDomain.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(wrongDomain.ErrorMessage, Does.Contain("does not serve"));
            Assert.That(thirdParty.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(thirdParty.ErrorMessage, Does.Contain("Third-party"));
            Assert.That(allowed.State, Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(tagged.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(tagged.ErrorMessage, Does.Contain("tag"));
        });
    }

    [Test]
    public void RejectsContactPolicyFailuresWithoutPartialChanges()
    {
        TrackingLocationService location = new TrackingLocationService();
        SipRegistrar registrar = new SipRegistrar();
        registrar.ConfigureRegistrarLocationService(location);
        SipRegistrarOptions options = Options();
        registrar.ProcessRegistration(Register("sip:alice@example.com",
            "sip:alice@example.com", "atomic-call", 1, "",
            "Contact: <sip:alice@original.example.com>;expires=120\r\n"),
            options);

        SipRegistrationSnapshot shortExpiry = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "atomic-call", 2, "",
                "Contact: <sip:alice@replacement.example.com>;expires=120\r\n"
                + "Contact: <sip:alice@too-short.example.com>;expires=30\r\n"),
            options);
        SipRegistrationSnapshot wildcard = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "atomic-call", 3, "Expires: 120\r\n", "Contact: *\r\n"),
            options);
        options.MaxBindingCount = 1;
        SipRegistrationSnapshot capacity = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "atomic-call", 4, "",
                "Contact: <sip:alice@original.example.com>;expires=120\r\n"
                + "Contact: <sip:alice@second.example.com>;expires=120\r\n"),
            options);
        SipRegistrationSnapshot duplicate = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "atomic-call", 5, "",
                "Contact: <sip:alice@duplicate.example.com>;expires=120\r\n"
                + "Contact: <sip:alice@duplicate.example.com>;expires=180\r\n"),
            options);
        SipRegistrationSnapshot query = registrar.ProcessRegistration(
            Register("sip:alice@example.com", "sip:alice@example.com",
                "query-call", 1, "", ""), options);

        Assert.Multiple(() =>
        {
            Assert.That(shortExpiry.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(shortExpiry.ErrorMessage, Does.Contain("minimum"));
            Assert.That(wildcard.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(wildcard.ErrorMessage, Does.Contain("wildcard"));
            Assert.That(capacity.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(capacity.ErrorMessage, Does.Contain("capacity"));
            Assert.That(duplicate.State, Is.EqualTo(SipRegistrationState.Failed));
            Assert.That(duplicate.ErrorMessage, Does.Contain("duplicate"));
            Assert.That(location.ReplaceCalls, Is.EqualTo(1));
            Assert.That(query.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "original.example.com" }));
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

    static SipMessage Register(string to, string from, string callId, int cseq,
        string additionalHeaders, string contacts)
    {
        string text = "REGISTER sip:example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-registrar\r\n"
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

    static SipLocationQuery LocationQuery(string addressOfRecord,
        long nowMilliseconds)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(addressOfRecord),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        SipLocationQuery query = new SipLocationQuery();
        query.AddressOfRecord = parsed.Uri;
        query.NowMilliseconds = nowMilliseconds;
        return query;
    }

    static string ContactHost(SipRegistrationBinding binding)
    {
        return binding.Contact!.Address!.Uri!.Host!;
    }
}

class TrackingLocationService : ISipLocationService
{
    internal SipLocationService Inner = new SipLocationService();
    internal int ReplaceCalls;

    public void ConfigureLocationStorage(ISipLocationStorage storage)
    {
        Inner.ConfigureLocationStorage(storage);
    }

    public SipLocationResult QueryLocation(SipLocationQuery query)
    {
        return Inner.QueryLocation(query);
    }

    public SipLocationResult UpdateLocation(SipRegistrationBinding binding)
    {
        return Inner.UpdateLocation(binding);
    }

    public SipLocationResult ReplaceLocations(SipLocationUpdate update)
    {
        ReplaceCalls++;
        return Inner.ReplaceLocations(update);
    }

    public SipLocationResult RemoveLocation(SipLocationQuery query)
    {
        return Inner.RemoveLocation(query);
    }

    public SipLocationResult ExpireLocations(long nowMilliseconds)
    {
        return Inner.ExpireLocations(nowMilliseconds);
    }
}
