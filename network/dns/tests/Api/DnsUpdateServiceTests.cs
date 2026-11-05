using System.Text;

namespace dns.Tests.Api;

public class DnsUpdateServiceTests
{
    [Test]
    public void AppliesEveryMutationAtomicallyAfterAllPrerequisitesSucceed()
    {
        MemoryDnsZoneStore store = StoreWithZone(10);
        DnsUpdateRequest request = Request();
        request.Prerequisites.Add(Prerequisite(
            DnsUpdatePrerequisiteKind.NameInUse,
            "multi.example",
            DnsRecordType.Reserved));
        request.Prerequisites.Add(Prerequisite(
            DnsUpdatePrerequisiteKind.NameNotInUse,
            "missing.example",
            DnsRecordType.Reserved));
        request.Prerequisites.Add(Prerequisite(
            DnsUpdatePrerequisiteKind.RecordSetExists,
            "multi.example",
            DnsRecordType.A));
        DnsUpdatePrerequisite values = Prerequisite(
            DnsUpdatePrerequisiteKind.RecordSetExistsWithValues,
            "multi.example",
            DnsRecordType.A);
        values.Records.Add(AddressRecord("multi.example", 2, 0));
        values.Records.Add(AddressRecord("multi.example", 1, 0));
        request.Prerequisites.Add(values);
        request.Prerequisites.Add(Prerequisite(
            DnsUpdatePrerequisiteKind.RecordSetDoesNotExist,
            "multi.example",
            DnsRecordType.Aaaa));

        DnsUpdateChange add = Change(
            DnsUpdateChangeKind.Add,
            "new.example",
            DnsRecordType.A,
            300);
        add.Records.Add(AddressRecord("new.example", 9, 300));
        DnsUpdateChange deleteRecord = Change(
            DnsUpdateChangeKind.DeleteRecord,
            "multi.example",
            DnsRecordType.A,
            0);
        deleteRecord.Records.Add(AddressRecord(
            "multi.example",
            1,
            700));
        request.Changes.Add(add);
        request.Changes.Add(deleteRecord);
        request.Changes.Add(Change(
            DnsUpdateChangeKind.DeleteRecordSet,
            "set.example",
            DnsRecordType.Aaaa,
            0));
        request.Changes.Add(Change(
            DnsUpdateChangeKind.DeleteName,
            "name.example",
            DnsRecordType.Reserved,
            0));

        DnsUpdateResult result = new DnsUpdateService(store).ApplyUpdate(
            request);
        DnsZone updated = ReadZone(store);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.True);
            Assert.That(result.ResponseCode, Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(result.Response, Is.Not.Null);
            Assert.That(result.Response!.Header.Opcode,
                Is.EqualTo(DnsOpcode.Update));
            Assert.That(result.Response.Header.Flags,
                Is.EqualTo(DnsHeaderFlag.Response));
            Assert.That(result.Transaction, Is.Not.Null);
            Assert.That(result.Transaction!.Atomic, Is.True);
            Assert.That(result.Transaction.Committed, Is.True);
            Assert.That(result.Transaction.Changes, Has.Count.EqualTo(4));
            Assert.That(result.HasResultingSerial, Is.True);
            Assert.That(result.ResultingSerial, Is.EqualTo(11));
            Assert.That(ReadSerial(updated), Is.EqualTo(11));
            Assert.That(AddressValues(updated, "new.example"),
                Is.EqualTo(new byte[] { 9 }));
            Assert.That(AddressValues(updated, "multi.example"),
                Is.EqualTo(new byte[] { 2 }));
            Assert.That(FindNode(updated, "set.example"), Is.Null);
            Assert.That(FindNode(updated, "name.example"), Is.Null);
            Assert.That(store.ReadZoneJournal(
                Parse("example"),
                DnsClass.Internet,
                1,
                10), Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RejectsStaleSerialThenJournalsAutomaticSerialWraparound()
    {
        MemoryDnsZoneStore store = StoreWithZone(uint.MaxValue);
        DnsUpdateRequest stale = Request();
        stale.HasExpectedSerial = true;
        stale.ExpectedSerial = uint.MaxValue - 1;
        DnsUpdateChange rejectedAdd = Change(
            DnsUpdateChangeKind.Add,
            "stale.example",
            DnsRecordType.A,
            300);
        rejectedAdd.Records.Add(AddressRecord("stale.example", 7, 300));
        stale.Changes.Add(rejectedAdd);
        DnsUpdateRequest current = Request();
        current.HasExpectedSerial = true;
        current.ExpectedSerial = uint.MaxValue;
        DnsUpdateChange acceptedAdd = Change(
            DnsUpdateChangeKind.Add,
            "wrapped.example",
            DnsRecordType.A,
            300);
        acceptedAdd.Records.Add(AddressRecord("wrapped.example", 8, 300));
        current.Changes.Add(acceptedAdd);
        DnsUpdateService service = new DnsUpdateService(store);

        DnsUpdateResult rejected = service.ApplyUpdate(stale);
        DnsUpdateResult accepted = service.ApplyUpdate(current);
        DnsZone updated = ReadZone(store);
        List<DnsZoneJournalEntry> journal = store.ReadZoneJournal(
            Parse("example"),
            DnsClass.Internet,
            1,
            uint.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Applied, Is.False);
            Assert.That(rejected.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(rejected.Transaction, Is.Null);
            Assert.That(FindNode(updated, "stale.example"), Is.Null);
            Assert.That(accepted.Applied, Is.True);
            Assert.That(accepted.HasResultingSerial, Is.True);
            Assert.That(accepted.ResultingSerial, Is.Zero);
            Assert.That(ReadSerial(updated), Is.Zero);
            Assert.That(AddressValues(updated, "wrapped.example"),
                Is.EqualTo(new byte[] { 8 }));
            Assert.That(journal, Has.Count.EqualTo(1));
            Assert.That(journal[0].FromSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(journal[0].ToSerial, Is.Zero);
        });
    }

    [TestCase(
        DnsUpdatePrerequisiteKind.NameInUse,
        "missing.example",
        DnsRecordType.Reserved,
        0,
        DnsResponseCode.NameError)]
    [TestCase(
        DnsUpdatePrerequisiteKind.NameNotInUse,
        "multi.example",
        DnsRecordType.Reserved,
        0,
        DnsResponseCode.NameExists)]
    [TestCase(
        DnsUpdatePrerequisiteKind.RecordSetExists,
        "multi.example",
        DnsRecordType.Aaaa,
        0,
        DnsResponseCode.RecordSetDoesNotExist)]
    [TestCase(
        DnsUpdatePrerequisiteKind.RecordSetExistsWithValues,
        "multi.example",
        DnsRecordType.A,
        9,
        DnsResponseCode.RecordSetDoesNotExist)]
    [TestCase(
        DnsUpdatePrerequisiteKind.RecordSetDoesNotExist,
        "multi.example",
        DnsRecordType.A,
        0,
        DnsResponseCode.RecordSetExists)]
    public void MapsFailedPrerequisitesToRfc2136ResponseCodes(
        DnsUpdatePrerequisiteKind kind,
        string owner,
        DnsRecordType recordType,
        byte value,
        DnsResponseCode expectedCode)
    {
        MemoryDnsZoneStore store = StoreWithZone(20);
        DnsUpdateRequest request = Request();
        DnsUpdatePrerequisite prerequisite = Prerequisite(
            kind,
            owner,
            recordType);
        if (kind == DnsUpdatePrerequisiteKind.RecordSetExistsWithValues)
        {
            prerequisite.Records.Add(AddressRecord(owner, value, 0));
        }

        request.Prerequisites.Add(prerequisite);
        DnsUpdateResult result = new DnsUpdateService(store).ApplyUpdate(
            request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.False);
            Assert.That(result.ResponseCode, Is.EqualTo(expectedCode));
            Assert.That(result.Response!.Header.ResponseCode,
                Is.EqualTo(expectedCode));
            Assert.That(result.Transaction, Is.Null);
            Assert.That(ReadSerial(ReadZone(store)), Is.EqualTo(20));
            Assert.That(store.ReadZoneJournal(
                Parse("example"),
                DnsClass.Internet,
                1,
                20), Is.Empty);
        });
    }

    [Test]
    public void RollsBackEveryStagedMutationWhenTheFinalZoneIsInvalid()
    {
        MemoryDnsZoneStore store = StoreWithZone(30);
        DnsUpdateRequest request = Request();
        DnsUpdateChange add = Change(
            DnsUpdateChangeKind.Add,
            "staged.example",
            DnsRecordType.A,
            300);
        add.Records.Add(AddressRecord("staged.example", 7, 300));
        request.Changes.Add(add);
        DnsUpdateChange conflicting = Change(
            DnsUpdateChangeKind.Add,
            "multi.example",
            DnsRecordType.Cname,
            300);
        conflicting.Records.Add(NameRecord(
            "multi.example",
            DnsRecordType.Cname,
            "target.example",
            300));
        request.Changes.Add(conflicting);

        DnsUpdateResult result = new DnsUpdateService(store).ApplyUpdate(
            request);
        DnsZone unchanged = ReadZone(store);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.False);
            Assert.That(result.ResponseCode, Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(result.Transaction, Is.Not.Null);
            Assert.That(result.Transaction!.Committed, Is.False);
            Assert.That(ReadSerial(unchanged), Is.EqualTo(30));
            Assert.That(FindNode(unchanged, "staged.example"), Is.Null);
            Assert.That(FindNode(unchanged, "example"), Is.Not.Null);
            Assert.That(store.ReadZoneJournal(
                Parse("example"),
                DnsClass.Internet,
                1,
                30), Is.Empty);
        });
    }

    [Test]
    public void MapsMalformedOutsideZoneAndUnhostedRequests()
    {
        MemoryDnsZoneStore store = StoreWithZone(40);
        DnsUpdateRequest malformed = Request();
        malformed.Prerequisites.Add(Prerequisite(
            DnsUpdatePrerequisiteKind.RecordSetExists,
            "multi.example",
            DnsRecordType.Any));
        DnsUpdateRequest outside = Request();
        outside.Prerequisites.Add(Prerequisite(
            DnsUpdatePrerequisiteKind.NameInUse,
            "www.other",
            DnsRecordType.Reserved));
        DnsUpdateRequest unhosted = Request();
        unhosted.Zone = Parse("other");

        DnsUpdateService service = new DnsUpdateService(store);
        DnsUpdateResult malformedResult = service.ApplyUpdate(malformed);
        DnsUpdateResult outsideResult = service.ApplyUpdate(outside);
        DnsUpdateResult unhostedResult = service.ApplyUpdate(unhosted);

        Assert.Multiple(() =>
        {
            Assert.That(malformedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.FormatError));
            Assert.That(outsideResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.NotZone));
            Assert.That(unhostedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.NotAuthoritative));
            Assert.That(malformedResult.Applied, Is.False);
            Assert.That(outsideResult.Applied, Is.False);
            Assert.That(unhostedResult.Applied, Is.False);
            Assert.That(ReadSerial(ReadZone(store)), Is.EqualTo(40));
        });
    }

    [Test]
    public void AuthenticatesUpdateSelectsForwardSoaSerialAndSignsResponse()
    {
        MemoryDnsZoneStore store = StoreWithZone(50);
        DnsTsigKey key = Key("update-key");
        DnsClockValue now = TransportTime.ReadClock();
        DnsUpdateRequest request = Request();
        request.Authorization.RequireTsigForUpdates = true;
        request.Authorization.AllowedTsigKeyNames.Add(Parse("update-key"));
        DnsUpdateChange add = Change(
            DnsUpdateChangeKind.Add,
            "signed.example",
            DnsRecordType.A,
            300);
        add.Records.Add(AddressRecord("signed.example", 8, 300));
        DnsUpdateChange soa = Change(
            DnsUpdateChangeKind.Add,
            "example",
            DnsRecordType.Soa,
            300);
        soa.Records.Add(SoaSet(75).Records[0]);
        request.Changes.Add(add);
        request.Changes.Add(soa);
        SignRequest(request, key, now);

        DnsUpdateResult result = new DnsUpdateService(store).ApplyUpdate(
            request);
        DnsTsigResult responseAuthentication =
            new DnsTsigService().VerifyTsig(
                result.Response!,
                key,
                result.Tsig!.Mac,
                TransportTime.ReadClock());
        List<DnsZoneJournalEntry> journal = store.ReadZoneJournal(
            Parse("example"),
            DnsClass.Internet,
            1,
            50);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.True);
            Assert.That(result.ResponseCode, Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(result.Tsig, Is.Not.Null);
            Assert.That(result.Tsig!.Authenticated, Is.True);
            Assert.That(result.Tsig.KeyName, Is.Not.Null);
            Assert.That(Text(result.Tsig.KeyName!), Is.EqualTo("update-key."));
            Assert.That(result.Response!.Additionals.Last().RecordType,
                Is.EqualTo(DnsRecordType.Tsig));
            Assert.That(responseAuthentication.Success, Is.True);
            Assert.That(responseAuthentication.Authenticated, Is.True);
            Assert.That(result.Transaction!.HasResultingSerial, Is.True);
            Assert.That(result.Transaction.ResultingSerial, Is.EqualTo(75));
            Assert.That(result.ResultingSerial, Is.EqualTo(75));
            Assert.That(ReadSerial(ReadZone(store)), Is.EqualTo(75));
            Assert.That(AddressValues(ReadZone(store), "signed.example"),
                Is.EqualTo(new byte[] { 8 }));
            Assert.That(journal, Has.Count.EqualTo(1));
            Assert.That(journal[0].FromSerial, Is.EqualTo(50));
            Assert.That(journal[0].ToSerial, Is.EqualTo(75));
            Assert.That(journal[0].Transaction.HasResultingSerial, Is.True);
            Assert.That(journal[0].RecordedAtUnixMilliseconds,
                Is.GreaterThan(0));
        });
    }

    [Test]
    public void RejectsBadSignaturesUnsignedRequirementsAndDisallowedKeys()
    {
        DnsTsigKey key = Key("update-key");
        DnsClockValue now = TransportTime.ReadClock();
        MemoryDnsZoneStore tamperedStore = StoreWithZone(60);
        DnsUpdateRequest tampered = SignedAddRequest(key, now, "bad.example");
        tampered.Message!.Authorities[0].Data!.Address!.Address[3] = 99;
        DnsUpdateResult badSignature =
            new DnsUpdateService(tamperedStore).ApplyUpdate(tampered);

        MemoryDnsZoneStore unsignedStore = StoreWithZone(60);
        DnsUpdateRequest unsigned = Request();
        unsigned.Authorization.RequireTsigForUpdates = true;
        DnsUpdateResult missing =
            new DnsUpdateService(unsignedStore).ApplyUpdate(unsigned);

        MemoryDnsZoneStore deniedStore = StoreWithZone(60);
        DnsUpdateRequest denied = SignedAddRequest(
            key,
            now,
            "denied.example");
        denied.Authorization.AllowedTsigKeyNames.Add(Parse("other-key"));
        DnsUpdateResult disallowed =
            new DnsUpdateService(deniedStore).ApplyUpdate(denied);

        Assert.Multiple(() =>
        {
            Assert.That(badSignature.ResponseCode,
                Is.EqualTo(DnsResponseCode.NotAuthoritative));
            Assert.That(badSignature.Tsig!.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
            Assert.That(badSignature.Applied, Is.False);
            Assert.That(ReadSerial(ReadZone(tamperedStore)), Is.EqualTo(60));
            Assert.That(missing.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(missing.Tsig, Is.Null);
            Assert.That(missing.Applied, Is.False);
            Assert.That(disallowed.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(disallowed.Tsig!.Authenticated, Is.True);
            Assert.That(disallowed.Response!.Additionals.Last().RecordType,
                Is.EqualTo(DnsRecordType.Tsig));
            Assert.That(disallowed.Applied, Is.False);
            Assert.That(ReadSerial(ReadZone(deniedStore)), Is.EqualTo(60));
        });
    }

    [Test]
    public void IgnoresStaleAndDeletedSoaWhileProtectingApexIdentity()
    {
        MemoryDnsZoneStore store = StoreWithZone(100);
        DnsUpdateRequest request = Request();
        DnsUpdateChange staleSoa = Change(
            DnsUpdateChangeKind.Add,
            "example",
            DnsRecordType.Soa,
            300);
        staleSoa.Records.Add(SoaSet(90).Records[0]);
        DnsUpdateChange deleteSoa = Change(
            DnsUpdateChangeKind.DeleteRecordSet,
            "example",
            DnsRecordType.Soa,
            0);
        DnsUpdateChange add = Change(
            DnsUpdateChangeKind.Add,
            "ordinary.example",
            DnsRecordType.A,
            300);
        add.Records.Add(AddressRecord("ordinary.example", 7, 300));
        request.Changes.Add(staleSoa);
        request.Changes.Add(deleteSoa);
        request.Changes.Add(Change(
            DnsUpdateChangeKind.DeleteName,
            "example",
            DnsRecordType.Reserved,
            0));
        request.Changes.Add(add);

        DnsUpdateResult result = new DnsUpdateService(store).ApplyUpdate(
            request);
        DnsZone updated = ReadZone(store);
        DnsZoneNode apex = FindNode(updated, "example")!;
        List<DnsZoneJournalEntry> journal = store.ReadZoneJournal(
            Parse("example"),
            DnsClass.Internet,
            1,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Applied, Is.True);
            Assert.That(result.ResultingSerial, Is.EqualTo(101));
            Assert.That(apex.RecordSets.Any(
                set => set.RecordType == DnsRecordType.Soa), Is.True);
            Assert.That(apex.RecordSets.Any(
                set => set.RecordType == DnsRecordType.Ns), Is.True);
            Assert.That(apex.RecordSets.Any(
                set => set.RecordType == DnsRecordType.A), Is.False);
            Assert.That(AddressValues(updated, "ordinary.example"),
                Is.EqualTo(new byte[] { 7 }));
            Assert.That(result.Transaction!.HasResultingSerial, Is.False);
            Assert.That(result.Transaction.Changes, Has.Count.EqualTo(2));
            Assert.That(journal, Has.Count.EqualTo(1));
            Assert.That(journal[0].ToSerial, Is.EqualTo(101));
        });
    }

    [Test]
    public void RejectsMalformedAuthorizationPolicyWithoutCommitting()
    {
        MemoryDnsZoneStore store = StoreWithZone(110);
        DnsUpdateRequest request = Request();
        request.Authorization.RateLimitWindowMilliseconds = 0;

        DnsUpdateResult result = new DnsUpdateService(store).ApplyUpdate(
            request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Applied, Is.False);
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.ServerFailure));
            Assert.That(result.Transaction, Is.Null);
            Assert.That(ReadSerial(ReadZone(store)), Is.EqualTo(110));
            Assert.That(store.ReadZoneJournal(
                Parse("example"),
                DnsClass.Internet,
                1,
                110), Is.Empty);
        });
    }

    static MemoryDnsZoneStore StoreWithZone(uint serial)
    {
        MemoryDnsZoneStore result = new MemoryDnsZoneStore();
        DnsZone zone = new DnsZone();
        zone.Apex = Parse("example");
        zone.Class = DnsClass.Internet;
        zone.ClassValue = 1;
        DnsZoneNode apex = new DnsZoneNode();
        apex.Name = Parse("example");
        apex.RecordSets.Add(SoaSet(serial));
        apex.RecordSets.Add(NameSet(
            "example",
            DnsRecordType.Ns,
            "ns.outside"));
        apex.RecordSets.Add(AddressSet("example", 3, DnsRecordType.A));
        zone.Nodes.Add(apex);
        zone.Nodes.Add(AddressNode("multi.example", 1, 2));
        zone.Nodes.Add(AddressNode("set.example", 4, 5, DnsRecordType.Aaaa));
        zone.Nodes.Add(AddressNode("name.example", 6));
        Assert.That(result.AddZone(zone).Valid, Is.True);
        return result;
    }

    static DnsZone ReadZone(MemoryDnsZoneStore store)
    {
        return store.ReadZone(
            Parse("example"),
            DnsClass.Internet,
            1)!;
    }

    static DnsUpdateRequest Request()
    {
        DnsUpdateRequest result = new DnsUpdateRequest();
        result.Zone = Parse("example");
        result.Authorization.AllowUpdates = true;
        result.Authorization.RequireTsigForUpdates = false;
        return result;
    }

    static DnsUpdateRequest SignedAddRequest(
        DnsTsigKey key,
        DnsClockValue now,
        string owner)
    {
        DnsUpdateRequest result = Request();
        result.Authorization.RequireTsigForUpdates = true;
        DnsUpdateChange add = Change(
            DnsUpdateChangeKind.Add,
            owner,
            DnsRecordType.A,
            300);
        add.Records.Add(AddressRecord(owner, 5, 300));
        result.Changes.Add(add);
        SignRequest(result, key, now);
        return result;
    }

    static void SignRequest(
        DnsUpdateRequest request,
        DnsTsigKey key,
        DnsClockValue now)
    {
        DnsUpdateMessageResult built =
            UpdateMessageOperations.BuildUpdateMessage(request, 0x1234);
        DnsTsigResult signed = new DnsTsigService().SignTsig(
            built.Message!,
            key,
            [],
            now);
        Assert.That(signed.Success, Is.True);
        request.Message = signed.Message;
        request.TsigKey = key;
    }

    static DnsTsigKey Key(string name)
    {
        DnsTsigKey result = new DnsTsigKey();
        result.Name = Parse(name);
        result.AlgorithmName = Parse("hmac-sha256");
        result.Secret = Encoding.ASCII.GetBytes("update-test-secret");
        return result;
    }

    static DnsUpdatePrerequisite Prerequisite(
        DnsUpdatePrerequisiteKind kind,
        string name,
        DnsRecordType recordType)
    {
        DnsUpdatePrerequisite result = new DnsUpdatePrerequisite();
        result.Kind = kind;
        result.Name = Parse(name);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        return result;
    }

    static DnsUpdateChange Change(
        DnsUpdateChangeKind kind,
        string name,
        DnsRecordType recordType,
        uint timeToLive)
    {
        DnsUpdateChange result = new DnsUpdateChange();
        result.Kind = kind;
        result.Name = Parse(name);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.TimeToLive = timeToLive;
        return result;
    }

    static DnsZoneNode AddressNode(
        string name,
        byte first,
        byte second = 0,
        DnsRecordType recordType = DnsRecordType.A)
    {
        DnsZoneNode result = new DnsZoneNode();
        result.Name = Parse(name);
        DnsResourceRecordSet set = AddressSet(
            name,
            first,
            recordType);
        if (second != 0)
        {
            set.Records.Add(AddressRecord(name, second, 300, recordType));
        }

        result.RecordSets.Add(set);
        return result;
    }

    static DnsResourceRecordSet AddressSet(
        string name,
        byte value,
        DnsRecordType recordType)
    {
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = Parse(name);
        set.RecordType = recordType;
        set.RecordTypeValue = (int)recordType;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 300;
        set.Records.Add(AddressRecord(name, value, 300, recordType));
        return set;
    }

    static DnsResourceRecord AddressRecord(
        string name,
        byte value,
        uint timeToLive,
        DnsRecordType recordType = DnsRecordType.A)
    {
        DnsResourceRecord result = BaseRecord(name, recordType, timeToLive);
        result.Data = new DnsRecordData();
        result.Data.Address = new DnsAddressData();
        if (recordType == DnsRecordType.Aaaa)
        {
            result.Data.Address.Address = new byte[16];
            result.Data.Address.Address[15] = value;
        }
        else
        {
            result.Data.Address.Address = [192, 0, 2, value];
        }

        return result;
    }

    static DnsResourceRecordSet SoaSet(uint serial)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = Parse("example");
        result.RecordType = DnsRecordType.Soa;
        result.RecordTypeValue = 6;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 300;
        DnsResourceRecord record = BaseRecord(
            "example",
            DnsRecordType.Soa,
            300);
        record.Data = new DnsRecordData();
        record.Data.Soa = new DnsSoaData();
        record.Data.Soa.PrimaryNameServer = Parse("ns.outside");
        record.Data.Soa.ResponsibleMailbox = Parse("hostmaster.outside");
        record.Data.Soa.Serial = serial;
        record.Data.Soa.Refresh = 3600;
        record.Data.Soa.Retry = 600;
        record.Data.Soa.Expire = 86400;
        record.Data.Soa.Minimum = 300;
        result.Records.Add(record);
        return result;
    }

    static DnsResourceRecordSet NameSet(
        string owner,
        DnsRecordType recordType,
        string target)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = Parse(owner);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 300;
        DnsResourceRecord record = BaseRecord(owner, recordType, 300);
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = Parse(target);
        result.Records.Add(record);
        return result;
    }

    static DnsResourceRecord NameRecord(
        string owner,
        DnsRecordType recordType,
        string target,
        uint timeToLive)
    {
        DnsResourceRecord result = BaseRecord(
            owner,
            recordType,
            timeToLive);
        result.Data = new DnsRecordData();
        result.Data.Name = new DnsNameData();
        result.Data.Name.Name = Parse(target);
        return result;
    }

    static DnsResourceRecord BaseRecord(
        string name,
        DnsRecordType recordType,
        uint timeToLive)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Parse(name);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = timeToLive;
        return result;
    }

    static DnsZoneNode? FindNode(DnsZone zone, string name)
    {
        string expected = Text(Parse(name));
        return zone.Nodes.SingleOrDefault(
            node => Text(node.Name) == expected);
    }

    static byte[] AddressValues(DnsZone zone, string name)
    {
        DnsZoneNode node = FindNode(zone, name)!;
        return node.RecordSets[0].Records.Select(
            record => record.Data!.Address!.Address[^1]).ToArray();
    }

    static uint ReadSerial(DnsZone zone)
    {
        DnsZoneNode apex = FindNode(zone, "example")!;
        DnsResourceRecordSet soa = apex.RecordSets.Single(
            set => set.RecordType == DnsRecordType.Soa);
        return soa.Records[0].Data!.Soa!.Serial;
    }

    static DnsName Parse(string text)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        string[] labels = text.Split('.');
        foreach (string label in labels)
        {
            if (label.Length > 0)
            {
                result.Labels.Add(Encoding.ASCII.GetBytes(label));
            }
        }

        return result;
    }

    static string Text(DnsName name)
    {
        return string.Join(
            ".",
            name.Labels.Select(Encoding.ASCII.GetString)) + ".";
    }
}
