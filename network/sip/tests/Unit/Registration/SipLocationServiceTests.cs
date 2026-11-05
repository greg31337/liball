namespace sip.Tests;

public class SipLocationServiceTests
{
    const string InstanceOne =
        "<urn:uuid:00000000-0000-1000-8000-000000000001>";

    [Test]
    public void StoresOrdersAndReplacesBindingsWithinAnAddressOfRecord()
    {
        SipLocationService service = new SipLocationService();
        SipUri addressOfRecord = ParseUri("sip:alice@example.com");
        service.UpdateLocation(Binding(addressOfRecord, "low.example.com",
            "low-call", 1, 5000, 200));
        service.UpdateLocation(Binding(addressOfRecord, "z.example.com",
            "z-call", 2, 3000, 800));
        SipLocationResult added = service.UpdateLocation(Binding(addressOfRecord,
            "a.example.com", "a-call", 3, 3000, 800));

        SipRegistrationBinding replacement = Binding(addressOfRecord,
            "z.example.com", "z-replacement", 4, 7000, 900);
        SipLocationResult replaced = service.UpdateLocation(replacement);

        Assert.Multiple(() =>
        {
            Assert.That(added.Success, Is.True);
            Assert.That(added.Bindings.Select(ContactHost), Is.EqualTo(
                new string[] { "a.example.com", "z.example.com", "low.example.com" }));
            Assert.That(replaced.Bindings, Has.Length.EqualTo(3));
            Assert.That(replaced.Bindings.Select(ContactHost), Is.EqualTo(
                new string[] { "z.example.com", "a.example.com", "low.example.com" }));
            Assert.That(replaced.Bindings[0].CallId, Is.EqualTo("z-replacement"));
            Assert.That(replaced.Bindings[0].CSeq, Is.EqualTo(4));
        });
    }

    [Test]
    public void PreservesRoutingMetadataAndReturnsDeepSnapshots()
    {
        SipLocationService service = new SipLocationService();
        SipRegistrationBinding source = Binding(ParseUri("sip:alice@example.com"),
            "device.example.com", "metadata-call", 42, 9000, 750);
        source.InstanceId = InstanceOne;
        source.RegistrationId = 4;
        source.Contact!.InstanceId = InstanceOne;
        source.Contact.RegistrationId = 4;
        source.Contact.PublicGruu =
            "sip:alice@example.com;gr=00000000-0000-1000-8000-000000000001";
        source.Contact.TemporaryGruu = "sip:temporary@example.com;gr";
        source.Path = new SipRouteValue[] { Route("path.example.com") };
        source.ServiceRoute = new SipRouteValue[] { Route("service.example.com") };
        source.ReceivedAddress = Endpoint("198.51.100.20", 62000);
        source.Flow = Flow("flow-one", 4, 8500, "connection-one");

        SipLocationResult updated = service.UpdateLocation(source);
        source.Contact.Address!.Uri!.Host = "mutated-input.example.com";
        source.Path[0].Address!.Uri!.Host = "mutated-path.example.com";
        source.ReceivedAddress.Host = "203.0.113.99";
        source.Flow.Id = "mutated-flow";
        updated.Bindings[0].Contact!.PublicGruu = "mutated-gruu";
        updated.Bindings[0].ServiceRoute[0].Address!.Uri!.Host =
            "mutated-service.example.com";
        updated.Bindings[0].Flow!.Connection!.Id = "mutated-connection";

        SipLocationResult queried = service.QueryLocation(Query(
            ParseUri("SIP:alice@EXAMPLE.COM"), -1));

        Assert.Multiple(() =>
        {
            Assert.That(queried.Success, Is.True);
            Assert.That(queried.Bindings, Has.Length.EqualTo(1));
            SipRegistrationBinding stored = queried.Bindings[0];
            Assert.That(ContactHost(stored), Is.EqualTo("device.example.com"));
            Assert.That(stored.CallId, Is.EqualTo("metadata-call"));
            Assert.That(stored.CSeq, Is.EqualTo(42));
            Assert.That(stored.Contact!.PublicGruu, Does.Contain("gr="));
            Assert.That(stored.Contact.TemporaryGruu, Is.EqualTo(
                "sip:temporary@example.com;gr"));
            Assert.That(stored.Path[0].Address!.Uri!.Host,
                Is.EqualTo("path.example.com"));
            Assert.That(stored.ServiceRoute[0].Address!.Uri!.Host,
                Is.EqualTo("service.example.com"));
            Assert.That(stored.ReceivedAddress!.Host, Is.EqualTo("198.51.100.20"));
            Assert.That(stored.Flow!.Id, Is.EqualTo("flow-one"));
            Assert.That(stored.Flow.Connection!.Id, Is.EqualTo("connection-one"));
            Assert.That(stored.Flow.ExpiresAtMilliseconds, Is.EqualTo(8500));
        });
    }

    [Test]
    public void FiltersRemovesAndExpiresBindingsAtExactDeadlines()
    {
        SipLocationService service = new SipLocationService();
        SipUri alice = ParseUri("sip:alice@example.com");
        SipRegistrationBinding first = Binding(alice, "one.example.com",
            "one", 1, 100, 500);
        AddOutboundIdentity(first, InstanceOne, 1);
        SipRegistrationBinding second = Binding(alice, "two.example.com",
            "two", 2, 200, 600);
        AddOutboundIdentity(second, InstanceOne, 2);
        service.UpdateLocation(first);
        service.UpdateLocation(second);
        service.UpdateLocation(Binding(ParseUri("sip:bob@example.com"),
            "bob.example.com", "bob", 1, 300, 700));

        SipLocationQuery activeQuery = Query(alice, 100);
        SipLocationResult active = service.QueryLocation(activeQuery);
        SipLocationQuery flowQuery = Query(alice, -1);
        flowQuery.InstanceId = InstanceOne;
        flowQuery.RegistrationId = 2;
        SipLocationResult selected = service.QueryLocation(flowQuery);
        SipLocationResult removed = service.RemoveLocation(flowQuery);
        SipLocationResult expired = service.ExpireLocations(300);
        SipLocationResult empty = service.QueryLocation(Query(alice, -1));

        Assert.Multiple(() =>
        {
            Assert.That(active.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "two.example.com" }));
            Assert.That(selected.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "two.example.com" }));
            Assert.That(removed.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "one.example.com" }));
            Assert.That(expired.Success, Is.True);
            Assert.That(expired.Bindings, Is.Empty);
            Assert.That(expired.ExpiredBindings.Select(ContactHost),
                Is.EqualTo(new string[] {
                    "one.example.com", "bob.example.com" }));
            Assert.That(empty.Bindings, Is.Empty);
        });
    }

    [Test]
    public void InvalidUpdateLeavesExistingAddressOfRecordUnchanged()
    {
        SipLocationService service = new SipLocationService();
        SipUri addressOfRecord = ParseUri("sip:alice@example.com");
        service.UpdateLocation(Binding(addressOfRecord, "device.example.com",
            "original", 7, 5000, 500));
        SipRegistrationBinding invalid = Binding(addressOfRecord,
            "device.example.com", "invalid", 8, 6000, 900);
        invalid.QualityThousandths = 1001;

        SipLocationResult rejected = service.UpdateLocation(invalid);
        SipLocationResult queried = service.QueryLocation(Query(addressOfRecord, -1));

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorMessage, Does.Contain("quality"));
            Assert.That(queried.Bindings, Has.Length.EqualTo(1));
            Assert.That(queried.Bindings[0].CallId, Is.EqualTo("original"));
            Assert.That(queried.Bindings[0].CSeq, Is.EqualTo(7));
        });
    }

    [Test]
    public void ReplacesACompleteAddressOfRecordAtomically()
    {
        SipLocationService service = new SipLocationService();
        SipUri alice = ParseUri("sip:alice@example.com");
        service.UpdateLocation(Binding(alice, "old.example.com",
            "old", 1, 5000, 500));
        service.UpdateLocation(Binding(ParseUri("sip:bob@example.com"),
            "bob.example.com", "bob", 1, 5000, 500));
        SipLocationUpdate update = new SipLocationUpdate();
        update.AddressOfRecord = alice;
        update.Bindings = new SipRegistrationBinding[]
        {
            Binding(alice, "new-one.example.com", "new", 2, 6000, 800),
            Binding(alice, "new-two.example.com", "new", 2, 6000, 700)
        };

        SipLocationResult replaced = service.ReplaceLocations(update);
        update.Bindings[0].Contact!.Address!.Uri!.Host = "mutated.example.com";
        SipLocationResult aliceResult = service.QueryLocation(Query(alice, -1));
        SipLocationResult bobResult = service.QueryLocation(Query(
            ParseUri("sip:bob@example.com"), -1));

        Assert.Multiple(() =>
        {
            Assert.That(replaced.Success, Is.True);
            Assert.That(aliceResult.Bindings.Select(ContactHost), Is.EqualTo(
                new string[] { "new-one.example.com", "new-two.example.com" }));
            Assert.That(bobResult.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "bob.example.com" }));
        });
    }

    [Test]
    public void ConcurrentUpdatesToOneAddressOfRecordDoNotLoseBindings()
    {
        SipLocationService service = new SipLocationService();
        SipUri addressOfRecord = ParseUri("sip:alice@example.com");

        System.Threading.Tasks.Parallel.For(0, 32, index =>
        {
            string host = "device-" + index.ToString("D2") + ".example.com";
            service.UpdateLocation(Binding(addressOfRecord, host,
                "call-" + index.ToString(), index, 10000, 500));
        });

        SipLocationResult result = service.QueryLocation(Query(addressOfRecord, -1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bindings, Has.Length.EqualTo(32));
            Assert.That(result.Bindings.Select(ContactHost), Is.EqualTo(
                Enumerable.Range(0, 32).Select(index =>
                    "device-" + index.ToString("D2") + ".example.com")));
        });
    }

    static SipRegistrationBinding Binding(SipUri addressOfRecord, string host,
        string callId, int cseq, long expiresAt, int quality)
    {
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.AddressOfRecord = addressOfRecord;
        binding.Contact = new SipContactValue();
        binding.Contact.Address = new SipNameAddress();
        binding.Contact.Address.Uri = ParseUri("sip:alice@" + host);
        binding.CallId = callId;
        binding.CSeq = cseq;
        binding.ExpiresAtMilliseconds = expiresAt;
        binding.QualityThousandths = quality;
        return binding;
    }

    static void AddOutboundIdentity(SipRegistrationBinding binding,
        string instanceId, int registrationId)
    {
        binding.InstanceId = instanceId;
        binding.RegistrationId = registrationId;
        binding.Contact!.InstanceId = instanceId;
        binding.Contact.RegistrationId = registrationId;
    }

    static SipLocationQuery Query(SipUri addressOfRecord, long nowMilliseconds)
    {
        SipLocationQuery query = new SipLocationQuery();
        query.AddressOfRecord = addressOfRecord;
        query.NowMilliseconds = nowMilliseconds;
        return query;
    }

    static SipRouteValue Route(string host)
    {
        SipRouteValue route = new SipRouteValue();
        route.Address = new SipNameAddress();
        route.Address.Uri = ParseUri("sip:" + host + ";lr");
        route.LooseRouting = true;
        return route;
    }

    static SipEndpointAddress Endpoint(string host, int port)
    {
        SipEndpointAddress endpoint = new SipEndpointAddress();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.Transport = SipTransportKind.Tcp;
        return endpoint;
    }

    static SipFlow Flow(string id, int registrationId, long expiresAt,
        string connectionId)
    {
        SipFlow flow = new SipFlow();
        flow.Id = id;
        flow.InstanceId = InstanceOne;
        flow.RegistrationId = registrationId;
        flow.ExpiresAtMilliseconds = expiresAt;
        flow.Connection = new SipConnectionInfo();
        flow.Connection.Id = connectionId;
        return flow;
    }

    static SipUri ParseUri(string text)
    {
        SipParseResult result = new SipUriCodec().ParseUri(
            System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete),
            result.ErrorMessage);
        return result.Uri!;
    }

    static string ContactHost(SipRegistrationBinding binding)
    {
        return binding.Contact!.Address!.Uri!.Host!;
    }
}
