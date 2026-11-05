using System.Text;

namespace dns.Tests.Internal.Zones;

public class MemoryDnsZoneStoreTests
{
    [Test]
    public void AddsReadsMatchesAndDeletesDetachedZoneSnapshots()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        DnsZone example = Zone("example", 10, AddressNode("www.example", 1));
        DnsZone child = Zone("sub.example", 30, AddressNode("host.sub.example", 2));

        DnsZoneValidationResult addedExample = store.AddZone(example);
        DnsZoneValidationResult addedChild = store.AddZone(child);
        DnsZone replacementSource = Zone(
            "sub.example", 31, AddressNode("new.sub.example", 3));
        DnsZoneValidationResult replacedChild =
            store.ReplaceZone(replacementSource);
        replacementSource.Nodes.Clear();
        example.Nodes.Clear();
        DnsZone? firstRead = store.ReadZone(
            Name("example"), DnsClass.Internet, 0);
        firstRead!.Apex.Labels[0][0] = (byte)'x';
        firstRead.Nodes.Clear();
        DnsZone? secondRead = store.ReadZone(
            Name("EXAMPLE"), DnsClass.Internet, 1);
        DnsZone? best = store.FindBestZone(
            Name("deep", "host", "sub", "example"),
            DnsClass.Internet,
            0);
        DnsZoneValidationResult duplicate = store.AddZone(
            Zone("example", 40));

        Assert.Multiple(() =>
        {
            Assert.That(addedExample.Valid, Is.True);
            Assert.That(addedChild.Valid, Is.True);
            Assert.That(replacedChild.Valid, Is.True);
            Assert.That(secondRead, Is.Not.Null);
            Assert.That(secondRead!.Nodes, Is.Not.Empty);
            Assert.That(Text(secondRead.Apex), Is.EqualTo("example."));
            Assert.That(ReadSerial(secondRead), Is.EqualTo(10));
            Assert.That(best, Is.Not.Null);
            Assert.That(Text(best!.Apex), Is.EqualTo("sub.example."));
            Assert.That(ReadSerial(best), Is.EqualTo(31));
            Assert.That(store.FindExactNode(
                best, Name("new", "sub", "example")), Is.Not.Null);
            Assert.That(duplicate.Valid, Is.False);
            Assert.That(store.DeleteZone(
                Name("sub", "example"), DnsClass.Internet, 1), Is.True);
            Assert.That(store.DeleteZone(
                Name("sub", "example"), DnsClass.Internet, 1), Is.False);
            Assert.That(store.FindBestZone(
                Name("host", "sub", "example"),
                DnsClass.Internet,
                1)!.Apex.Labels, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ExposesIndexedNodeLookupsThroughDetachedSnapshots()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        DnsZoneNode branch = EmptyNode("branch.example");
        DnsZoneNode wildcard = AddressNode("*.branch.example", 5);
        DnsZoneNode delegation = DelegationNode("deleg.example");
        DnsZone zone = Zone(
            "example",
            1,
            branch,
            wildcard,
            delegation,
            AddressNode("z.example", 6));
        Assert.That(store.AddZone(zone).Valid, Is.True);
        DnsZone snapshot = store.ReadZone(
            Name("example"), DnsClass.Internet, 1)!;

        DnsZoneNode? exact = store.FindExactNode(
            snapshot, Name("BRANCH", "EXAMPLE"));
        DnsZoneNode? closest = store.FindClosestNode(
            snapshot, Name("missing", "branch", "example"));
        DnsZoneNode? wildcardResult = store.FindWildcardNode(
            snapshot, Name("missing", "branch", "example"));
        DnsZoneNode? cut = store.FindDelegationNode(
            snapshot, Name("host", "deleg", "example"));
        DnsZoneNode? preceding = store.FindPrecedingNode(
            snapshot, Name("m", "example"));
        DnsZoneNode? following = store.FindFollowingNode(
            snapshot, Name("m", "example"));

        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.Not.Null);
            Assert.That(exact!.IsEmptyNonTerminal, Is.True);
            Assert.That(closest, Is.SameAs(exact));
            Assert.That(Text(wildcardResult!.Name),
                Is.EqualTo("*.branch.example."));
            Assert.That(Text(cut!.Name), Is.EqualTo("deleg.example."));
            Assert.That(Text(preceding!.Name), Is.EqualTo("deleg.example."));
            Assert.That(Text(following!.Name), Is.EqualTo("z.example."));
        });
    }

    [Test]
    public void AppliesEveryMutationAtomicallyAndCreatesDetachedJournalEntries()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        DnsZone zone = Zone(
            "example",
            100,
            AddressNode("replace.example", 1),
            TextNode("records.example", 3, 4),
            AddressNode("set.example", 5, DnsRecordType.Aaaa),
            AddressNode("name.example", 6));
        Assert.That(store.AddZone(zone).Valid, Is.True);
        DnsZoneTransaction transaction = new DnsZoneTransaction();
        transaction.Identifier = 77;
        transaction.HasExpectedSerial = true;
        transaction.ExpectedSerial = 100;
        transaction.HasResultingSerial = true;
        transaction.ResultingSerial = 200;
        transaction.Changes.Add(AddChange("api.example", DnsRecordType.A, 9));
        transaction.Changes.Add(ReplaceChange(
            "replace.example", DnsRecordType.A, 10));
        transaction.Changes.Add(DeleteRecordChange(
            "records.example", DnsRecordType.Txt, 3));
        transaction.Changes.Add(DeleteSetChange(
            "set.example", DnsRecordType.Aaaa));
        transaction.Changes.Add(DeleteNameChange("name.example"));

        DnsZoneValidationResult applied = store.ApplyZoneTransaction(
            Name("example"),
            DnsClass.Internet,
            0,
            transaction);
        DnsZone snapshot = store.ReadZone(
            Name("example"), DnsClass.Internet, 1)!;
        List<DnsZoneJournalEntry> journal = store.ReadZoneJournal(
            Name("example"), DnsClass.Internet, 1, 100);

        Assert.Multiple(() =>
        {
            Assert.That(applied.Valid, Is.True);
            Assert.That(transaction.Committed, Is.True);
            Assert.That(ReadSerial(snapshot), Is.EqualTo(200));
            Assert.That(store.FindExactNode(
                snapshot, Name("api", "example")), Is.Not.Null);
            Assert.That(ReadFirstByte(store.FindExactNode(
                snapshot, Name("replace", "example"))!), Is.EqualTo(10));
            Assert.That(store.FindExactNode(
                snapshot, Name("records", "example"))!
                .RecordSets[0].Records, Has.Count.EqualTo(1));
            Assert.That(ReadFirstByte(store.FindExactNode(
                snapshot, Name("records", "example"))!), Is.EqualTo(4));
            Assert.That(store.FindExactNode(
                snapshot, Name("set", "example")), Is.Null);
            Assert.That(store.FindExactNode(
                snapshot, Name("name", "example")), Is.Null);
            Assert.That(journal, Has.Count.EqualTo(1));
            Assert.That(journal[0].FromSerial, Is.EqualTo(100));
            Assert.That(journal[0].ToSerial, Is.EqualTo(200));
            Assert.That(journal[0].Transaction.Identifier, Is.EqualTo(77));
            Assert.That(journal[0].Transaction.Committed, Is.True);
        });

        journal[0].Transaction.Committed = false;
        journal[0].Transaction.Changes.Clear();
        List<DnsZoneJournalEntry> reread = store.ReadZoneJournal(
            Name("example"), DnsClass.Internet, 1, 100);
        Assert.Multiple(() =>
        {
            Assert.That(reread[0].Transaction.Committed, Is.True);
            Assert.That(reread[0].Transaction.Changes, Has.Count.EqualTo(5));
        });
    }

    [Test]
    public void AdvancesSerialByDefaultAndReturnsContinuousJournalSuffixes()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        Assert.That(store.AddZone(Zone("example", uint.MaxValue)).Valid,
            Is.True);
        DnsZoneTransaction first = new DnsZoneTransaction();
        first.HasExpectedSerial = true;
        first.ExpectedSerial = uint.MaxValue;
        first.Changes.Add(AddChange("one.example", DnsRecordType.A, 1));
        DnsZoneTransaction second = new DnsZoneTransaction();
        second.HasExpectedSerial = true;
        second.ExpectedSerial = 0;
        second.Changes.Add(AddChange("two.example", DnsRecordType.A, 2));

        DnsZoneValidationResult firstResult = store.ApplyZoneTransaction(
            Name("example"), DnsClass.Internet, 1, first);
        DnsZoneValidationResult secondResult = store.ApplyZoneTransaction(
            Name("example"), DnsClass.Internet, 1, second);
        List<DnsZoneJournalEntry> all = store.ReadZoneJournal(
            Name("example"), DnsClass.Internet, 1, uint.MaxValue);
        List<DnsZoneJournalEntry> suffix = store.ReadZoneJournal(
            Name("example"), DnsClass.Internet, 1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Valid, Is.True);
            Assert.That(secondResult.Valid, Is.True);
            Assert.That(ReadSerial(store.ReadZone(
                Name("example"), DnsClass.Internet, 1)!), Is.EqualTo(1));
            Assert.That(all.Select(item => item.FromSerial),
                Is.EqualTo(new uint[] { uint.MaxValue, 0 }));
            Assert.That(all.Select(item => item.ToSerial),
                Is.EqualTo(new uint[] { 0, 1 }));
            Assert.That(all[1].Sequence, Is.GreaterThan(all[0].Sequence));
            Assert.That(suffix, Has.Count.EqualTo(1));
            Assert.That(suffix[0].FromSerial, Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsInvalidReplacementAndRollsBackFailedTransactions()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        Assert.That(store.AddZone(Zone("example", 50)).Valid, Is.True);
        DnsZone invalidReplacement = Zone("example", 60);
        DnsZoneNode invalidApex = invalidReplacement.Nodes.Single(
            item => Text(item.Name) == "example.");
        invalidApex.RecordSets.RemoveAll(
            set => set.RecordTypeValue == (int)DnsRecordType.Ns);
        DnsZoneValidationResult replacement =
            store.ReplaceZone(invalidReplacement);
        DnsZoneValidationResult missingReplacement =
            store.ReplaceZone(Zone("other", 1));
        DnsZoneTransaction wrongSerial = new DnsZoneTransaction();
        wrongSerial.HasExpectedSerial = true;
        wrongSerial.ExpectedSerial = 49;
        wrongSerial.Changes.Add(AddChange(
            "wrong.example", DnsRecordType.A, 1));
        DnsZoneValidationResult rejectedSerial = store.ApplyZoneTransaction(
            Name("example"), DnsClass.Internet, 1, wrongSerial);
        DnsZoneTransaction invalidMutation = new DnsZoneTransaction();
        invalidMutation.HasExpectedSerial = true;
        invalidMutation.ExpectedSerial = 50;
        invalidMutation.Changes.Add(AddChange(
            "staged.example", DnsRecordType.A, 2));
        invalidMutation.Changes.Add(DeleteNameChange("example"));
        DnsZoneValidationResult rejectedMutation = store.ApplyZoneTransaction(
            Name("example"), DnsClass.Internet, 1, invalidMutation);
        DnsZone snapshot = store.ReadZone(
            Name("example"), DnsClass.Internet, 1)!;

        Assert.Multiple(() =>
        {
            Assert.That(replacement.Valid, Is.False);
            Assert.That(missingReplacement.Valid, Is.False);
            Assert.That(rejectedSerial.Valid, Is.False);
            Assert.That(rejectedMutation.Valid, Is.False);
            Assert.That(wrongSerial.Committed, Is.False);
            Assert.That(invalidMutation.Committed, Is.False);
            Assert.That(ReadSerial(snapshot), Is.EqualTo(50));
            Assert.That(store.FindExactNode(
                snapshot, Name("wrong", "example")), Is.Null);
            Assert.That(store.FindExactNode(
                snapshot, Name("staged", "example")), Is.Null);
            Assert.That(store.ReadZoneJournal(
                Name("example"), DnsClass.Internet, 1, 50), Is.Empty);
        });
    }

    [Test]
    public void ConcurrentReadsReturnIndependentStableSnapshots()
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        Assert.That(store.AddZone(Zone(
            "example",
            70,
            AddressNode("host.example", 7))).Valid, Is.True);
        System.Collections.Concurrent.ConcurrentBag<uint> serials = new();

        Parallel.For(0, 64, index =>
        {
            DnsZone snapshot = store.ReadZone(
                Name("example"), DnsClass.Internet, 1)!;
            DnsZone best = store.FindBestZone(
                Name("deep", "host", "example"),
                DnsClass.Internet,
                1)!;
            DnsZoneNode? host = store.FindExactNode(
                snapshot,
                Name("host", "example"));
            if (host == null || ReadFirstByte(host) != 7)
            {
                throw new InvalidOperationException(
                    "A concurrent zone snapshot was incomplete.");
            }

            serials.Add(ReadSerial(snapshot));
            snapshot.Nodes.Clear();
            best.Apex.Labels.Clear();
        });

        DnsZone retained = store.ReadZone(
            Name("example"), DnsClass.Internet, 1)!;
        Assert.Multiple(() =>
        {
            Assert.That(serials, Has.Count.EqualTo(64));
            Assert.That(serials, Is.All.EqualTo(70));
            Assert.That(Text(retained.Apex), Is.EqualTo("example."));
            Assert.That(store.FindExactNode(
                retained,
                Name("host", "example")), Is.Not.Null);
        });
    }

    static DnsZone Zone(
        string apexText,
        uint serial,
        params DnsZoneNode[] additionalNodes)
    {
        DnsZone zone = new DnsZone();
        zone.Apex = Parse(apexText);
        zone.Class = DnsClass.Internet;
        zone.ClassValue = 1;
        DnsZoneNode apex = new DnsZoneNode();
        apex.Name = Parse(apexText);
        apex.RecordSets.Add(SoaSet(apexText, serial));
        apex.RecordSets.Add(NameServerSet(apexText));
        zone.Nodes.Add(apex);
        zone.Nodes.AddRange(additionalNodes);
        return zone;
    }

    static DnsZoneNode EmptyNode(string name)
    {
        DnsZoneNode result = new DnsZoneNode();
        result.Name = Parse(name);
        result.IsEmptyNonTerminal = true;
        return result;
    }

    static DnsZoneNode AddressNode(
        string name,
        byte value,
        DnsRecordType recordType = DnsRecordType.A)
    {
        DnsZoneNode result = new DnsZoneNode();
        result.Name = Parse(name);
        result.RecordSets.Add(RawSet(name, recordType, value));
        return result;
    }

    static DnsZoneNode TextNode(
        string name,
        byte first,
        byte second)
    {
        DnsZoneNode result = new DnsZoneNode();
        result.Name = Parse(name);
        DnsResourceRecordSet set = RawSet(name, DnsRecordType.Txt, first);
        set.Records.Add(RawRecord(name, DnsRecordType.Txt, second));
        result.RecordSets.Add(set);
        return result;
    }

    static DnsZoneNode DelegationNode(string name)
    {
        DnsZoneNode result = new DnsZoneNode();
        result.Name = Parse(name);
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = Parse(name);
        set.RecordType = DnsRecordType.Ns;
        set.RecordTypeValue = 2;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 300;
        DnsResourceRecord record = BaseRecord(name, DnsRecordType.Ns);
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = Name("ns", "outside");
        set.Records.Add(record);
        result.RecordSets.Add(set);
        return result;
    }

    static DnsResourceRecordSet SoaSet(string name, uint serial)
    {
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = Parse(name);
        set.RecordType = DnsRecordType.Soa;
        set.RecordTypeValue = 6;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 300;
        DnsResourceRecord record = BaseRecord(name, DnsRecordType.Soa);
        record.Data = new DnsRecordData();
        record.Data.Soa = new DnsSoaData();
        record.Data.Soa.PrimaryNameServer = Name("ns", "outside");
        record.Data.Soa.ResponsibleMailbox = Name("hostmaster", "outside");
        record.Data.Soa.Serial = serial;
        record.Data.Soa.Refresh = 3600;
        record.Data.Soa.Retry = 600;
        record.Data.Soa.Expire = 86400;
        record.Data.Soa.Minimum = 300;
        set.Records.Add(record);
        return set;
    }

    static DnsResourceRecordSet NameServerSet(string name)
    {
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = Parse(name);
        set.RecordType = DnsRecordType.Ns;
        set.RecordTypeValue = 2;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 300;
        DnsResourceRecord record = BaseRecord(name, DnsRecordType.Ns);
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = Name("ns", "outside");
        set.Records.Add(record);
        return set;
    }

    static DnsResourceRecordSet RawSet(
        string name,
        DnsRecordType recordType,
        byte value)
    {
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = Parse(name);
        set.RecordType = recordType;
        set.RecordTypeValue = (int)recordType;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 300;
        set.Records.Add(RawRecord(name, recordType, value));
        return set;
    }

    static DnsResourceRecord RawRecord(
        string name,
        DnsRecordType recordType,
        byte value)
    {
        DnsResourceRecord result = BaseRecord(name, recordType);
        result.RawData = [value];
        result.UseRawData = true;
        return result;
    }

    static DnsResourceRecord BaseRecord(
        string name,
        DnsRecordType recordType)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Parse(name);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = 300;
        return result;
    }

    static DnsZoneChange AddChange(
        string name,
        DnsRecordType recordType,
        byte value)
    {
        DnsZoneChange result = Change(name, recordType);
        result.Kind = DnsZoneChangeKind.Add;
        result.Record = RawRecord(name, recordType, value);
        return result;
    }

    static DnsZoneChange ReplaceChange(
        string name,
        DnsRecordType recordType,
        byte value)
    {
        DnsZoneChange result = Change(name, recordType);
        result.Kind = DnsZoneChangeKind.ReplaceRecordSet;
        result.RecordSet = RawSet(name, recordType, value);
        return result;
    }

    static DnsZoneChange DeleteRecordChange(
        string name,
        DnsRecordType recordType,
        byte value)
    {
        DnsZoneChange result = Change(name, recordType);
        result.Kind = DnsZoneChangeKind.DeleteRecord;
        result.Record = RawRecord(name, recordType, value);
        return result;
    }

    static DnsZoneChange DeleteSetChange(
        string name,
        DnsRecordType recordType)
    {
        DnsZoneChange result = Change(name, recordType);
        result.Kind = DnsZoneChangeKind.DeleteRecordSet;
        return result;
    }

    static DnsZoneChange DeleteNameChange(string name)
    {
        DnsZoneChange result = Change(name, DnsRecordType.Reserved);
        result.Kind = DnsZoneChangeKind.DeleteName;
        return result;
    }

    static DnsZoneChange Change(string name, DnsRecordType recordType)
    {
        DnsZoneChange result = new DnsZoneChange();
        result.Name = Parse(name);
        result.RecordType = recordType;
        result.RecordTypeValue = (int)recordType;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        return result;
    }

    static uint ReadSerial(DnsZone zone)
    {
        DnsZoneNode apex = zone.Nodes.Single(
            node => Text(node.Name) == Text(zone.Apex));
        DnsResourceRecordSet soa = apex.RecordSets.Single(
            set => set.RecordTypeValue == 6);
        return soa.Records[0].Data!.Soa!.Serial;
    }

    static byte ReadFirstByte(DnsZoneNode node)
    {
        return node.RecordSets[0].Records[0].RawData[0];
    }

    static DnsName Parse(string text)
    {
        return Name(text.Split('.'));
    }

    static DnsName Name(params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
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
        if (name.Labels.Count == 0)
        {
            return ".";
        }

        return string.Join(
            ".",
            name.Labels.Select(Encoding.ASCII.GetString)) + ".";
    }
}
