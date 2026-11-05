using System.Text;

namespace dns.Tests.Internal.Zones;

public class ZoneIndexOperationsTests
{
    [Test]
    public void BuildsCanonicalReversedNameAndRrsetIndexes()
    {
        DnsZoneNode apex = Node("example", Set("example", 6));
        DnsZoneNode a = Node(
            "a.example",
            Set("a.example", 28),
            Set("a.example", 1));
        DnsZoneNode wildcard = Node("*.a.example", Set("*.a.example", 1));
        DnsZoneNode x = Node("x.a.example", Set("x.a.example", 16));
        DnsZoneNode delegation = Node(
            "deleg.example", Set("deleg.example", 2));
        delegation.IsDelegation = true;
        DnsZoneNode glue = Node(
            "glue.deleg.example", Set("glue.deleg.example", 1));
        DnsZoneNode empty = Node("empty.example");
        empty.IsEmptyNonTerminal = true;
        DnsZoneNode z = Node("z.example", Set("z.example", 1));
        a.Children.Add(x);
        DnsZone zone = Zone(
            "example",
            1,
            z,
            x,
            apex,
            wildcard,
            a,
            delegation,
            glue,
            empty);

        DnsZoneIndex index = ZoneIndexOperations.BuildZone(zone);
        DnsResourceRecordSet? address = ZoneIndexOperations.FindRecordSet(
            index,
            Name("A", "EXAMPLE"),
            DnsRecordType.A,
            0,
            DnsClass.Internet,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(index.Nodes.Select(item => Text(item.Node.Name)),
                Is.EqualTo(new[]
                {
                    "example.",
                    "a.example.",
                    "*.a.example.",
                    "x.a.example.",
                    "deleg.example.",
                    "glue.deleg.example.",
                    "empty.example.",
                    "z.example."
                }));
            Assert.That(index.RecordSets, Has.Count.EqualTo(8));
            Assert.That(address, Is.SameAs(a.RecordSets[1]));
            Assert.That(ZoneIndexOperations.FindRecordSet(
                index,
                Name("a", "example"),
                DnsRecordType.Mx,
                15,
                DnsClass.Internet,
                1), Is.Null);
            Assert.That(index.Nodes[1].Key.Labels.Select(TextLabel),
                Is.EqualTo(new[] { "example", "a" }));
        });
    }

    [Test]
    public void FindsExactAncestorsClosestEncloserWildcardAndDelegation()
    {
        DnsZoneNode apex = Node("example");
        DnsZoneNode a = Node("a.example");
        DnsZoneNode x = Node("x.a.example");
        DnsZoneNode wildcard = Node("*.a.example");
        DnsZoneNode delegation = Node("deleg.example");
        delegation.IsDelegation = true;
        DnsZoneNode glue = Node("glue.deleg.example");
        DnsZoneIndex index = ZoneIndexOperations.BuildZone(Zone(
            "example", 1, apex, a, x, wildcard, delegation, glue));

        DnsZoneNode? exact = ZoneIndexOperations.FindExact(
            index, Name("X", "A", "EXAMPLE"));
        DnsZoneNode? strictAncestor = ZoneIndexOperations.FindAncestor(
            index, Name("deep", "x", "a", "example"));
        DnsZoneNode? closest = ZoneIndexOperations.FindClosestEncloser(
            index, Name("missing", "a", "example"));
        DnsZoneNode? foundWildcard = ZoneIndexOperations.FindWildcard(
            index, Name("missing", "a", "example"));
        DnsZoneNode? existingWildcard = ZoneIndexOperations.FindWildcard(
            index, Name("a", "example"));
        DnsZoneNode? cut = ZoneIndexOperations.FindDelegation(
            index, Name("host", "glue", "deleg", "example"));
        DnsZoneNode? outside = ZoneIndexOperations.FindClosestEncloser(
            index, Name("outside", "test"));

        Assert.Multiple(() =>
        {
            Assert.That(exact, Is.SameAs(x));
            Assert.That(strictAncestor, Is.SameAs(x));
            Assert.That(closest, Is.SameAs(a));
            Assert.That(foundWildcard, Is.SameAs(wildcard));
            Assert.That(existingWildcard, Is.Null);
            Assert.That(cut, Is.SameAs(delegation));
            Assert.That(outside, Is.Null);
        });
    }

    [Test]
    public void FindsCircularCanonicalPredecessorsAndSuccessors()
    {
        DnsZoneNode apex = Node("example");
        DnsZoneNode a = Node("a.example");
        DnsZoneNode empty = Node("empty.example");
        DnsZoneNode z = Node("z.example");
        DnsZoneIndex index = ZoneIndexOperations.BuildZone(
            Zone("example", 1, empty, z, apex, a));

        Assert.Multiple(() =>
        {
            Assert.That(ZoneIndexOperations.FindPredecessor(
                index, Name("example")), Is.SameAs(z));
            Assert.That(ZoneIndexOperations.FindSuccessor(
                index, Name("z", "example")), Is.SameAs(apex));
            Assert.That(ZoneIndexOperations.FindPredecessor(
                index, Name("m", "example")), Is.SameAs(empty));
            Assert.That(ZoneIndexOperations.FindSuccessor(
                index, Name("m", "example")), Is.SameAs(z));
            Assert.That(ZoneIndexOperations.FindPredecessor(
                ZoneIndexOperations.BuildZone(Zone("example", 1)),
                Name("example")), Is.Null);
        });
    }

    [Test]
    public void FindsLongestHostedZoneBySuffixAndClass()
    {
        DnsZone root = Zone(string.Empty, 1, Node(string.Empty));
        DnsZone example = Zone("example", 1, Node("example"));
        DnsZone child = Zone("a.example", 1, Node("a.example"));
        DnsZone chaosChild = Zone("a.example", 3, Node("a.example"));
        DnsHostedZoneIndex index = ZoneIndexOperations.BuildHosted(
            new List<DnsZone> { example, root, chaosChild, child });

        Assert.Multiple(() =>
        {
            Assert.That(ZoneIndexOperations.FindLongestZone(
                index,
                Name("host", "A", "EXAMPLE"),
                DnsClass.Internet,
                0), Is.SameAs(child));
            Assert.That(ZoneIndexOperations.FindLongestZone(
                index,
                Name("host", "a", "example"),
                DnsClass.Chaos,
                0), Is.SameAs(chaosChild));
            Assert.That(ZoneIndexOperations.FindLongestZone(
                index,
                Name("other", "test"),
                DnsClass.Internet,
                1), Is.SameAs(root));
            Assert.That(ZoneIndexOperations.FindLongestZone(
                index,
                Name("other", "test"),
                DnsClass.Hesiod,
                4), Is.Null);
        });
    }

    [Test]
    public void RejectsDuplicateNodeRrsetAndHostedZoneKeys()
    {
        DnsZone duplicateNodes = Zone(
            "example", 1, Node("www.example"), Node("WWW.EXAMPLE"));
        DnsZoneNode duplicateSetNode = Node(
            "example", Set("example", 1), Set("EXAMPLE", 1));
        DnsZone duplicateSets = Zone("example", 1, duplicateSetNode);
        DnsZone first = Zone("example", 1, Node("example"));
        DnsZone second = Zone("EXAMPLE", 1, Node("example"));

        Assert.Multiple(() =>
        {
            Assert.That(() => ZoneIndexOperations.BuildZone(duplicateNodes),
                Throws.ArgumentException);
            Assert.That(() => ZoneIndexOperations.BuildZone(duplicateSets),
                Throws.ArgumentException);
            Assert.That(() => ZoneIndexOperations.BuildHosted(
                new List<DnsZone> { first, second }),
                Throws.ArgumentException);
        });
    }

    static DnsZone Zone(
        string apex,
        int classValue,
        params DnsZoneNode[] nodes)
    {
        DnsZone result = new DnsZone();
        result.Apex = Parse(apex);
        result.Class = (DnsClass)classValue;
        result.ClassValue = classValue;
        result.Nodes.AddRange(nodes);
        return result;
    }

    static DnsZoneNode Node(
        string name,
        params DnsResourceRecordSet[] sets)
    {
        DnsZoneNode result = new DnsZoneNode();
        result.Name = Parse(name);
        result.RecordSets.AddRange(sets);
        return result;
    }

    static DnsResourceRecordSet Set(string name, int typeValue)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = Parse(name);
        result.RecordType = (DnsRecordType)typeValue;
        result.RecordTypeValue = typeValue;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        return result;
    }

    static DnsName Parse(string text)
    {
        if (text.Length == 0)
        {
            return Name();
        }

        return Name(text.Split('.'));
    }

    static DnsName Name(params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        foreach (string label in labels)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(label));
        }

        return result;
    }

    static string Text(DnsName name)
    {
        if (name.Labels.Count == 0)
        {
            return ".";
        }

        return string.Join(".", name.Labels.Select(TextLabel)) + ".";
    }

    static string TextLabel(byte[] label)
    {
        return Encoding.ASCII.GetString(label);
    }
}
