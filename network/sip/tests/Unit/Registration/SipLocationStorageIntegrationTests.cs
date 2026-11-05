namespace sip.Tests;

public class SipLocationStorageIntegrationTests
{
    [Test]
    public void ConfiguredStorageCanBeSharedAcrossServiceInstances()
    {
        SharedLocationStorage storage = new SharedLocationStorage();
        SipLocationService writer = new SipLocationService();
        SipLocationService reader = new SipLocationService();
        writer.ConfigureLocationStorage(storage);
        reader.ConfigureLocationStorage(storage);
        SipRegistrationBinding source = Binding("sip:alice@example.com",
            "device.example.com", 5000, 700);

        SipLocationResult written = writer.UpdateLocation(source);
        source.Contact!.Address!.Uri!.Host = "mutated-input.example.com";
        SipLocationResult read = reader.QueryLocation(
            Query("SIP:alice@EXAMPLE.COM", -1));
        read.Bindings[0].Contact!.Address!.Uri!.Host =
            "mutated-output.example.com";
        SipLocationResult reread = writer.QueryLocation(
            Query("sip:alice@example.com", -1));
        SipLocationResult removed = reader.RemoveLocation(
            Query("sip:alice@example.com", -1));
        SipLocationQuery afterRemoveQuery =
            Query("sip:alice@example.com", -1);
        SipLocationResult afterRemove = writer.QueryLocation(afterRemoveQuery);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(read.Bindings, Has.Length.EqualTo(1));
            Assert.That(ContactHost(read.Bindings[0]),
                Is.EqualTo("mutated-output.example.com"));
            Assert.That(ContactHost(reread.Bindings[0]),
                Is.EqualTo("device.example.com"));
            Assert.That(removed.Bindings, Is.Empty);
            Assert.That(afterRemove.Bindings, Is.Empty);
            Assert.That(storage.UpdateCalls, Is.EqualTo(1));
            Assert.That(storage.QueryCalls, Is.EqualTo(3));
            Assert.That(storage.RemoveCalls, Is.EqualTo(1));
            Assert.That(storage.LastUpdatedBinding, Is.Not.SameAs(source));
            Assert.That(storage.LastQuery, Is.Not.Null);
            Assert.That(storage.LastQuery, Is.Not.SameAs(afterRemoveQuery));
        });
    }

    [Test]
    public void StorageResultsAreFilteredOrderedAndDetached()
    {
        SharedLocationStorage storage = new SharedLocationStorage();
        storage.Bindings.Add(Binding("sip:alice@example.com",
            "low.example.com", 5000, 200));
        storage.Bindings.Add(Binding("sip:bob@example.com",
            "other.example.com", 5000, 1000));
        storage.Bindings.Add(Binding("sip:alice@example.com",
            "high.example.com", 4000, 900));
        SipLocationService service = new SipLocationService();
        service.ConfigureLocationStorage(storage);
        SipLocationQuery query = Query("sip:alice@example.com", 1000);

        SipLocationResult result = service.QueryLocation(query);
        storage.Bindings[2].Contact!.Address!.Uri!.Host =
            "mutated-storage.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(result.Bindings.Select(ContactHost), Is.EqualTo(
                new string[] { "high.example.com", "low.example.com" }));
            Assert.That(storage.LastQuery, Is.Not.SameAs(query));
            Assert.That(storage.LastQuery!.AddressOfRecord,
                Is.Not.SameAs(query.AddressOfRecord));
            Assert.That(storage.LastQuery.NowMilliseconds, Is.EqualTo(1000));
            Assert.That(ContactHost(result.Bindings[0]),
                Is.EqualTo("high.example.com"));
        });
    }

    [Test]
    public void StorageExpiryAndFailuresArePropagatedSafely()
    {
        SharedLocationStorage storage = new SharedLocationStorage();
        storage.Bindings.Add(Binding("sip:alice@example.com",
            "expired.example.com", 100, 500));
        storage.Bindings.Add(Binding("sip:bob@example.com",
            "active.example.com", 101, 500));
        SipLocationService service = new SipLocationService();
        service.ConfigureLocationStorage(storage);

        SipLocationResult expired = service.ExpireLocations(100);
        storage.FailQueries = true;
        SipLocationResult failed = service.QueryLocation(
            Query("sip:bob@example.com", -1));
        System.ArgumentNullException? missing =
            Assert.Throws<System.ArgumentNullException>(() =>
                new SipLocationService().ConfigureLocationStorage(null!));

        Assert.Multiple(() =>
        {
            Assert.That(expired.Success, Is.True);
            Assert.That(expired.Bindings.Select(ContactHost),
                Is.EqualTo(new string[] { "active.example.com" }));
            Assert.That(storage.ExpireCalls, Is.EqualTo(1));
            Assert.That(storage.LastExpiry, Is.EqualTo(100));
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Bindings, Is.Empty);
            Assert.That(failed.ErrorMessage, Is.EqualTo("storage unavailable"));
            Assert.That(missing!.ParamName, Is.EqualTo("storage"));
        });
    }

    [Test]
    public void AtomicReplacementDelegatesToConfiguredStorage()
    {
        SharedLocationStorage storage = new SharedLocationStorage();
        SipLocationService service = new SipLocationService();
        service.ConfigureLocationStorage(storage);
        SipLocationUpdate update = new SipLocationUpdate();
        update.AddressOfRecord = ParseUri("sip:alice@example.com");
        update.Bindings = new SipRegistrationBinding[]
        {
            Binding("sip:alice@example.com", "one.example.com", 5000, 500),
            Binding("sip:alice@example.com", "two.example.com", 5000, 600)
        };

        SipLocationResult result = service.ReplaceLocations(update);
        update.Bindings[1].Contact!.Address!.Uri!.Host = "mutated.example.com";

        Assert.Multiple(() =>
        {
            Assert.That(storage.ReplaceCalls, Is.EqualTo(1));
            Assert.That(result.Bindings.Select(ContactHost), Is.EqualTo(
                new string[] { "two.example.com", "one.example.com" }));
            Assert.That(ContactHost(storage.Bindings[1]),
                Is.EqualTo("two.example.com"));
        });
    }

    static SipRegistrationBinding Binding(string addressOfRecord, string host,
        long expiresAt, int quality)
    {
        SipRegistrationBinding binding = new SipRegistrationBinding();
        binding.AddressOfRecord = ParseUri(addressOfRecord);
        binding.Contact = new SipContactValue();
        binding.Contact.Address = new SipNameAddress();
        binding.Contact.Address.Uri = ParseUri("sip:alice@" + host);
        binding.CallId = host + "-call";
        binding.CSeq = 1;
        binding.ExpiresAtMilliseconds = expiresAt;
        binding.QualityThousandths = quality;
        return binding;
    }

    static SipLocationQuery Query(string addressOfRecord, long nowMilliseconds)
    {
        SipLocationQuery query = new SipLocationQuery();
        query.AddressOfRecord = ParseUri(addressOfRecord);
        query.NowMilliseconds = nowMilliseconds;
        return query;
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

class SharedLocationStorage : ISipLocationStorage
{
    internal System.Collections.Generic.List<SipRegistrationBinding> Bindings =
        new System.Collections.Generic.List<SipRegistrationBinding>();
    internal SipLocationQuery? LastQuery;
    internal SipRegistrationBinding? LastUpdatedBinding;
    internal long LastExpiry = -1;
    internal int QueryCalls;
    internal int UpdateCalls;
    internal int ReplaceCalls;
    internal int RemoveCalls;
    internal int ExpireCalls;
    internal bool FailQueries;

    public SipLocationResult QueryStoredLocations(SipLocationQuery query)
    {
        QueryCalls++;
        LastQuery = query;
        if (FailQueries)
        {
            SipLocationResult failed = new SipLocationResult();
            failed.ErrorMessage = "storage unavailable";
            return failed;
        }
        return Current();
    }

    public SipLocationResult UpdateStoredLocation(SipRegistrationBinding binding)
    {
        UpdateCalls++;
        LastUpdatedBinding = binding;
        Bindings.Clear();
        Bindings.Add(binding);
        return Current();
    }

    public SipLocationResult ReplaceStoredLocations(SipLocationUpdate update)
    {
        ReplaceCalls++;
        Bindings.Clear();
        Bindings.AddRange(update.Bindings);
        return Current();
    }

    public SipLocationResult RemoveStoredLocations(SipLocationQuery query)
    {
        RemoveCalls++;
        LastQuery = query;
        Bindings.Clear();
        return Current();
    }

    public SipLocationResult ExpireStoredLocations(long nowMilliseconds)
    {
        ExpireCalls++;
        LastExpiry = nowMilliseconds;
        int index = Bindings.Count - 1;
        while (index >= 0)
        {
            if (Bindings[index].ExpiresAtMilliseconds <= nowMilliseconds)
            {
                Bindings.RemoveAt(index);
            }
            index--;
        }
        return Current();
    }

    SipLocationResult Current()
    {
        SipLocationResult result = new SipLocationResult();
        result.Success = true;
        result.Bindings = Bindings.ToArray();
        return result;
    }
}
