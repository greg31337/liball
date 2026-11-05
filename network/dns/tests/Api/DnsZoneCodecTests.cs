namespace dns.Tests.Api;

public class DnsZoneCodecTests
{
    [Test]
    public void ParsesCommentsQuotesParenthesesAndOmittedOwnersTogether()
    {
        DnsZoneLoadOptions options = CreateOptions(null, false, 0);
        string source = "$ORIGIN sub.example. ; relative names use this origin\n" +
            "$TTL 300\n" +
            "@ IN SOA (\n" +
            "  ns ; comments are ignored inside parentheses\n" +
            "  hostmaster 1 1h 10m 1d 5m )\n" +
            "text TXT \"semi; colon\" \"a\\\"b\\000\" ; trailing comment\n" +
            "    AAAA 2001:db8::1\n";

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, FailureReason(result));
            Assert.That(result.RecordsLoaded, Is.EqualTo(3));
            Assert.That(Format(result.Zone!.Apex),
                Is.EqualTo("sub.example."));
            Assert.That(result.Zone.Nodes, Has.Count.EqualTo(2));
            Assert.That(Format(result.Zone.Nodes[1].Name),
                Is.EqualTo("text.sub.example."));
            Assert.That(result.Zone.Nodes[1].RecordSets,
                Has.Count.EqualTo(2));
            Assert.That(result.Zone.Nodes[1].RecordSets[0].Records[0]
                    .Data!.CharacterStrings!.Strings[0],
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "semi; colon")));
            Assert.That(result.Zone.Nodes[1].RecordSets[0].Records[0]
                    .Data!.CharacterStrings!.Strings[1],
                Is.EqualTo(new byte[]
                    { (byte)'a', (byte)'"', (byte)'b', 0 }));
            Assert.That(result.Zone.Nodes[1].RecordSets[1].RecordType,
                Is.EqualTo(DnsRecordType.Aaaa));
        });
    }

    [Test]
    public void ParsesDefaultsOwnerInheritanceAndRelativeNames()
    {
        DnsZoneLoadOptions options = CreateOptions("example.", true, 300);
        string source = "@ SOA ns hostmaster 1 1h 10m 1d 5m\n" +
            "@ NS ns\n" +
            "www A 192.0.2.1\n" +
            "    AAAA 2001:db8::1\n" +
            "mail 1h30m MX 10 mx\n";

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.RecordsLoaded, Is.EqualTo(5));
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(Format(result.Zone!.Apex), Is.EqualTo("example."));
            Assert.That(result.Zone.Nodes, Has.Count.EqualTo(3));
            Assert.That(result.Zone.Nodes[0].RecordSets,
                Has.Count.EqualTo(2));
            Assert.That(result.Zone.Nodes[1].RecordSets,
                Has.Count.EqualTo(2));
            Assert.That(Format(result.Zone.Nodes[1].Name),
                Is.EqualTo("www.example."));
            Assert.That(result.Zone.Nodes[1].RecordSets[0].TimeToLive,
                Is.EqualTo(300));
            Assert.That(result.Zone.Nodes[1].RecordSets[1].Records[0].Class,
                Is.EqualTo(DnsClass.Internet));
            Assert.That(result.Zone.Nodes[2].RecordSets[0].TimeToLive,
                Is.EqualTo(5400));
            Assert.That(Format(result.Zone.Nodes[2].RecordSets[0].Records[0]
                    .Data!.PreferenceName!.Name),
                Is.EqualTo("mx.example."));
            Assert.That(result.Zone.Nodes[0].RecordSets[0].Records[0]
                    .Data!.Soa!.Refresh,
                Is.EqualTo(3600));
        });
    }

    [Test]
    public void AcceptsClassAndCompoundTimeToLiveInEitherOrder()
    {
        DnsZoneLoadOptions options = CreateOptions("example.", false, 0);
        string source = "first 1h CH TXT \"one\"\n" +
            "second HS 2h15m TXT \"two\"\n";

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Zone!.Nodes[0].RecordSets[0].ClassValue,
                Is.EqualTo(3));
            Assert.That(result.Zone.Nodes[0].RecordSets[0].TimeToLive,
                Is.EqualTo(3600));
            Assert.That(result.Zone.Nodes[1].RecordSets[0].ClassValue,
                Is.EqualTo(4));
            Assert.That(result.Zone.Nodes[1].RecordSets[0].TimeToLive,
                Is.EqualTo(8100));
        });
    }

    [Test]
    public void GroupsInheritedOwnerRecordsAndNormalizesSetTimeToLive()
    {
        DnsZoneLoadOptions options = CreateOptions("example.", false, 0);
        string source = "www 2h IN A 192.0.2.1\n" +
            "    1h A 192.0.2.2\n";

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Zone!.Nodes, Has.Count.EqualTo(1));
            Assert.That(result.Zone.Nodes[0].RecordSets,
                Has.Count.EqualTo(1));
            Assert.That(result.Zone.Nodes[0].RecordSets[0].Records,
                Has.Count.EqualTo(2));
            Assert.That(result.Zone.Nodes[0].RecordSets[0].TimeToLive,
                Is.EqualTo(3600));
            Assert.That(result.Zone.Nodes[0].RecordSets[0].Records[0]
                    .TimeToLive,
                Is.EqualTo(7200));
            Assert.That(result.Zone.Nodes[0].RecordSets[0].Records[1]
                    .TimeToLive,
                Is.EqualTo(3600));
        });
    }

    [Test]
    public void InfersApexFromAbsoluteSoaWithoutInitialOrigin()
    {
        DnsZoneLoadOptions options = CreateOptions(null, false, 0);
        string source = "example. 60 IN A 192.0.2.1\n" +
            "zone.example. 300 IN SOA ns.zone.example. " +
            "hostmaster.zone.example. 7 1h 10m 1d 5m\n";

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(Format(result.Zone!.Apex),
                Is.EqualTo("zone.example."));
            Assert.That(result.RecordsLoaded, Is.EqualTo(2));
        });
    }

    [TestCase("    60 IN A 192.0.2.1", "omitted owner", 5)]
    [TestCase("www IN A 192.0.2.1", "requires a TTL", 8)]
    public void RejectsMissingInheritedFieldsWithLocations(
        string source,
        string reason,
        int column)
    {
        DnsZoneLoadOptions options = CreateOptions("example.", false, 0);

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Zone, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Reason,
                Does.Contain(reason).IgnoreCase);
            Assert.That(result.Diagnostics[0].Line, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Column, Is.EqualTo(column));
        });
    }

    [Test]
    public void RejectsRelativeOwnersWithoutAnOrigin()
    {
        DnsZoneLoadOptions options = CreateOptions(null, true, 60);

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            "www A 192.0.2.1", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidZone));
            Assert.That(result.Diagnostics[0].Reason,
                Does.Contain("absolute DNS name"));
            Assert.That(result.Diagnostics[0].Line, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Column, Is.EqualTo(1));
        });
    }

    static DnsZoneLoadOptions CreateOptions(
        string? origin,
        bool hasDefaultTimeToLive,
        uint defaultTimeToLive)
    {
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        if (origin != null)
            options.Origin = new DnsNameService().ParseName(origin, null);
        options.HasDefaultTimeToLive = hasDefaultTimeToLive;
        options.DefaultTimeToLive = defaultTimeToLive;
        options.ValidateZone = false;
        return options;
    }

    static string Format(DnsName name)
    {
        return new DnsNameService().FormatName(name, new DnsTextOptions());
    }

    static string FailureReason(DnsZoneLoadResult result)
    {
        if (result.Diagnostics.Count == 0) return string.Empty;
        return result.Diagnostics[0].Reason;
    }
}
