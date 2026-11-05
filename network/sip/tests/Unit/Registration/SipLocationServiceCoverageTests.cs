namespace sip.Tests;

public class SipLocationServiceCoverageTests
{
    const string InstanceOne =
        "<urn:uuid:00000000-0000-1000-8000-000000000011>";
    const string InstanceTwo =
        "<urn:uuid:00000000-0000-1000-8000-000000000022>";

    [Test]
    public void RetransmittedMetadataDoesNotDuplicateAContact()
    {
        SipLocationService service = new SipLocationService();
        SipUri addressOfRecord = Uri("sip:alice@example.com");
        SipRegistrationBinding initial = Binding(addressOfRecord,
            "device.example.com", "call-one", 42, 5000, 700);

        service.UpdateLocation(initial);
        SipLocationResult replay = service.UpdateLocation(
            Binding(addressOfRecord, "device.example.com",
                "call-one", 42, 5000, 700));
        SipLocationResult newDialog = service.UpdateLocation(
            Binding(addressOfRecord, "device.example.com",
                "call-two", 1, 6000, 800));

        Assert.Multiple(() =>
        {
            Assert.That(replay.Success, Is.True);
            Assert.That(replay.Bindings, Has.Length.EqualTo(1));
            Assert.That(replay.Bindings[0].CallId,
                Is.EqualTo("call-one"));
            Assert.That(replay.Bindings[0].CSeq, Is.EqualTo(42));
            Assert.That(newDialog.Bindings, Has.Length.EqualTo(1));
            Assert.That(newDialog.Bindings[0].CallId,
                Is.EqualTo("call-two"));
            Assert.That(newDialog.Bindings[0].CSeq, Is.EqualTo(1));
            Assert.That(newDialog.Bindings[0].QualityThousandths,
                Is.EqualTo(800));
        });
    }

    [Test]
    public void InvalidAtomicReplacementAndWildcardRemovalPreserveOtherAors()
    {
        SipLocationService service = new SipLocationService();
        SipUri alice = Uri("sip:alice@example.com");
        SipUri bob = Uri("sip:bob@example.com");
        service.UpdateLocation(Binding(alice, "old.example.com",
            "alice-call", 1, 5000, 500));
        service.UpdateLocation(Binding(bob, "bob.example.com",
            "bob-call", 1, 5000, 600));
        SipLocationUpdate invalid = new SipLocationUpdate();
        invalid.AddressOfRecord = alice;
        invalid.Bindings =
        [
            Binding(alice, "new.example.com",
                "alice-call", 2, 6000, 900),
            Binding(alice, "invalid.example.com",
                "alice-call", 2, 6000, 1001)
        ];

        SipLocationResult rejected = service.ReplaceLocations(invalid);
        SipLocationResult unchanged = service.QueryLocation(Query(alice));
        SipLocationResult removed = service.RemoveLocation(Query(alice));
        SipLocationResult aliceAfter = service.QueryLocation(Query(alice));
        SipLocationResult bobAfter = service.QueryLocation(Query(bob));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(unchanged.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "old.example.com" }));
            Assert.That(removed.Success, Is.True);
            Assert.That(removed.Bindings, Is.Empty);
            Assert.That(aliceAfter.Bindings, Is.Empty);
            Assert.That(bobAfter.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "bob.example.com" }));
        });
    }

    [Test]
    public void RegistrarBackedLocationCreatesUniqueGruusForEachInstance()
    {
        SipLocationService location = new SipLocationService();
        SipRegistrar registrar = new SipRegistrar();
        registrar.ConfigureRegistrarLocationService(location);
        SipRegistrarOptions options = new SipRegistrarOptions();
        options.RequireAuthentication = false;
        options.MinExpiresSeconds = 60;
        options.DefaultExpiresSeconds = 120;
        options.MaxExpiresSeconds = 600;
        options.Domains = ["example.com"];
        string contacts = "Contact: <sip:alice@one.example.com>"
            + ";expires=120;+sip.instance=\"" + InstanceOne + "\"\r\n"
            + "Contact: <sip:alice@two.example.com>"
            + ";expires=120;+sip.instance=\"" + InstanceTwo + "\"\r\n";

        SipRegistrationSnapshot registered = registrar.ProcessRegistration(
            Register("gruu-call", 1,
                "Supported: gruu\r\n" + contacts), options);
        SipLocationResult stored = location.QueryLocation(
            Query(Uri("sip:alice@example.com")));

        Assert.Multiple(() =>
        {
            Assert.That(registered.State,
                Is.EqualTo(SipRegistrationState.Registered));
            Assert.That(stored.Bindings, Has.Length.EqualTo(2));
            Assert.That(stored.Bindings[0].Contact!.PublicGruu,
                Is.Not.Null.And.Not.Empty);
            Assert.That(stored.Bindings[1].Contact!.PublicGruu,
                Is.Not.Null.And.Not.Empty);
            Assert.That(stored.Bindings[0].Contact!.PublicGruu,
                Is.Not.EqualTo(stored.Bindings[1].Contact!.PublicGruu));
            Assert.That(stored.Bindings[0].Contact!.TemporaryGruu,
                Is.Not.EqualTo(stored.Bindings[1].Contact!.TemporaryGruu));
            Assert.That(registered.Response!.ContactValues
                .Select(value => value.PublicGruu),
                Is.EquivalentTo(stored.Bindings
                    .Select(value => value.Contact!.PublicGruu)));
        });
    }

    [Test]
    public void MalformedCustomStorageResultsFailWithoutEscaping()
    {
        SipLocationService service = new SipLocationService();
        service.ConfigureLocationStorage(
            new MalformedLocationStorage());

        SipLocationResult queried = service.QueryLocation(
            Query(Uri("sip:alice@example.com")));
        SipLocationResult expired = service.ExpireLocations(1000);

        Assert.Multiple(() =>
        {
            Assert.That(queried.Success, Is.False);
            Assert.That(queried.Bindings, Is.Empty);
            Assert.That(queried.ErrorMessage,
                Does.Contain("invalid"));
            Assert.That(expired.Success, Is.False);
            Assert.That(expired.Bindings, Is.Empty);
            Assert.That(expired.ErrorMessage,
                Does.Contain("invalid"));
        });
    }

    static SipRegistrationBinding Binding(SipUri addressOfRecord,
        string host, string callId, int cseq, long expires, int quality)
    {
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.AddressOfRecord = addressOfRecord;
        binding.Contact = new SipContactValue();
        binding.Contact.Address = new SipNameAddress();
        binding.Contact.Address.Uri = Uri("sip:alice@" + host);
        binding.CallId = callId;
        binding.CSeq = cseq;
        binding.ExpiresAtMilliseconds = expires;
        binding.QualityThousandths = quality;
        return binding;
    }

    static SipLocationQuery Query(SipUri addressOfRecord)
    {
        SipLocationQuery query = new SipLocationQuery();
        query.AddressOfRecord = addressOfRecord;
        return query;
    }

    static SipMessage Register(string callId, int cseq, string headers)
    {
        string text = "REGISTER sip:example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com"
            + ";branch=z9hG4bK-location\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from-tag\r\n"
            + "To: <sip:alice@example.com>\r\n"
            + "Call-ID: " + callId + "\r\n"
            + "CSeq: " + cseq + " REGISTER\r\n"
            + headers + "Content-Length: 0\r\n\r\n";
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.ASCII.GetBytes(text),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static SipUri Uri(string text)
    {
        SipParseResult parsed = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Uri!;
    }

    static string ContactHost(SipRegistrationBinding binding)
    {
        return binding.Contact!.Address!.Uri!.Host!;
    }
}

class MalformedLocationStorage : ISipLocationStorage
{
    public SipLocationResult QueryStoredLocations(SipLocationQuery query)
    {
        SipLocationResult result = new SipLocationResult();
        result.Success = true;
        result.Bindings = null!;
        return result;
    }

    public SipLocationResult UpdateStoredLocation(
        SipRegistrationBinding binding)
    {
        return InvalidResult();
    }

    public SipLocationResult ReplaceStoredLocations(
        SipLocationUpdate update)
    {
        return InvalidResult();
    }

    public SipLocationResult RemoveStoredLocations(SipLocationQuery query)
    {
        return InvalidResult();
    }

    public SipLocationResult ExpireStoredLocations(long nowMilliseconds)
    {
        return InvalidResult();
    }

    static SipLocationResult InvalidResult()
    {
        SipLocationResult result = new SipLocationResult();
        result.Success = true;
        result.Bindings = null!;
        return result;
    }
}
