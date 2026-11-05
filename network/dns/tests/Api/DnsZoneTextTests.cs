namespace dns.Tests.Api;

public class DnsZoneTextTests
{
    [Test]
    public void CanonicalOutputIsLowercaseAbsoluteAndOrderIndependent()
    {
        DnsZone first = ParseAbsoluteZone(
            "Z.Example. 300 IN A 192.0.2.9\n" +
            "A.Example. 300 IN AAAA 2001:db8::1\n" +
            "a.example. 300 IN A 192.0.2.20\n" +
            "a.example. 300 IN A 192.0.2.10\n" +
            "a.example. 300 IN MX 10 mx10.example.\n" +
            "a.example. 300 IN MX 2 mx2.example.\n" +
            "Example. 300 IN NS Ns.Example.\n");
        DnsZone second = ParseAbsoluteZone(
            "EXAMPLE. 300 IN NS NS.EXAMPLE.\n" +
            "A.EXAMPLE. 300 IN A 192.0.2.10\n" +
            "A.EXAMPLE. 300 IN A 192.0.2.20\n" +
            "a.example. 300 IN MX 2 MX2.EXAMPLE.\n" +
            "a.example. 300 IN MX 10 MX10.EXAMPLE.\n" +
            "a.example. 300 IN AAAA 2001:db8::1\n" +
            "z.example. 300 IN A 192.0.2.9\n");
        DnsTextOptions options = new DnsTextOptions();
        options.Canonical = true;
        options.Origin = ParseName("example.");

        DnsTextResult firstText = new DnsZoneCodec().FormatZone(
            first, options);
        DnsTextResult secondText = new DnsZoneCodec().FormatZone(
            second, options);

        Assert.Multiple(() =>
        {
            Assert.That(firstText.Success, Is.True);
            Assert.That(firstText.Text, Is.EqualTo(secondText.Text));
            Assert.That(firstText.Text, Is.EqualTo(
                "example. 300 IN NS ns.example.\n" +
                "a.example. 300 IN A 192.0.2.10\n" +
                "a.example. 300 IN A 192.0.2.20\n" +
                "a.example. 300 IN MX 2 mx2.example.\n" +
                "a.example. 300 IN MX 10 mx10.example.\n" +
                "a.example. 300 IN AAAA 2001:db8::1\n" +
                "z.example. 300 IN A 192.0.2.9"));
            Assert.That(firstText.Text, Does.Not.Contain("$ORIGIN"));
            Assert.That(Format(first.Nodes[0].Name),
                Is.EqualTo("Z.Example."));
        });
    }

    [Test]
    public void PresentationOutputIsDeterministicByDefault()
    {
        DnsZone first = ParseAbsoluteZone(
            "z.example. 60 IN A 192.0.2.2\n" +
            "a.example. 60 IN AAAA 2001:db8::1\n" +
            "a.example. 60 IN A 192.0.2.1\n");
        DnsZone second = ParseAbsoluteZone(
            "a.example. 60 IN A 192.0.2.1\n" +
            "a.example. 60 IN AAAA 2001:db8::1\n" +
            "z.example. 60 IN A 192.0.2.2\n");

        DnsTextResult firstText = new DnsZoneCodec().FormatZone(
            first, new DnsTextOptions());
        DnsTextResult secondText = new DnsZoneCodec().FormatZone(
            second, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(firstText.Success, Is.True);
            Assert.That(firstText.Text, Is.EqualTo(secondText.Text));
            Assert.That(firstText.Text, Is.EqualTo(
                "$TTL 60\n" +
                "a.example. 60 IN A 192.0.2.1\n" +
                "a.example. 60 IN AAAA 2001:db8::1\n" +
                "z.example. 60 IN A 192.0.2.2"));
        });
    }

    [Test]
    public void PreservationModeRetainsModelTraversalOrder()
    {
        DnsZone zone = ParseAbsoluteZone(
            "z.example. 60 IN A 192.0.2.2\n" +
            "a.example. 60 IN A 192.0.2.1\n");
        DnsTextOptions options = new DnsTextOptions();
        options.PreserveInputFormatting = true;

        DnsTextResult result = new DnsZoneCodec().FormatZone(zone, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(
                "$TTL 60\n" +
                "z.example. 60 IN A 192.0.2.2\n" +
                "a.example. 60 IN A 192.0.2.1"));
            Assert.That(result.Zone, Is.SameAs(zone));
        });
    }

    [Test]
    public void OriginAndDefaultTimeToLiveMakePresentationSelfContained()
    {
        DnsZoneLoadOptions loadOptions = new DnsZoneLoadOptions();
        loadOptions.Origin = ParseName("Example.");
        loadOptions.HasDefaultTimeToLive = true;
        loadOptions.DefaultTimeToLive = 300;
        loadOptions.ValidateZone = false;
        DnsZoneLoadResult loaded = new DnsZoneCodec().ParseZone(
            "@ SOA Ns Hostmaster 7 1h 10m 1d 5m\n" +
            "@ NS Ns\n" +
            "www A 192.0.2.1\n" +
            "mail MX 10 MailHost\n",
            loadOptions);
        DnsTextOptions textOptions = new DnsTextOptions();
        textOptions.Origin = ParseName("Example.");
        textOptions.PreserveInputFormatting = true;

        DnsTextResult formatted = new DnsZoneCodec().FormatZone(
            loaded.Zone!, textOptions);
        DnsZoneLoadOptions freshOptions = new DnsZoneLoadOptions();
        freshOptions.ValidateZone = false;
        DnsZoneLoadResult reparsed = new DnsZoneCodec().ParseZone(
            formatted.Text, freshOptions);

        Assert.Multiple(() =>
        {
            Assert.That(formatted.Success, Is.True);
            Assert.That(formatted.Text, Does.StartWith(
                "$ORIGIN Example.\n$TTL 300\n"));
            Assert.That(formatted.Text,
                Does.Contain("@ 300 IN SOA Ns Hostmaster"));
            Assert.That(formatted.Text,
                Does.Contain("mail 300 IN MX 10 MailHost"));
            Assert.That(reparsed.Success, Is.True,
                FailureReason(reparsed));
            Assert.That(reparsed.RecordsLoaded, Is.EqualTo(4));
            Assert.That(reparsed.Zone!.HasDefaultTimeToLive, Is.True);
            Assert.That(reparsed.Zone.DefaultTimeToLive, Is.EqualTo(300));
            Assert.That(Format(reparsed.Zone.Nodes[1].Name),
                Is.EqualTo("www.Example."));
        });
    }

    [Test]
    public void SharedChildNodesAreSerializedOnlyOnce()
    {
        DnsZone zone = ParseAbsoluteZone(
            "example. 60 IN NS ns.example.\n" +
            "ns.example. 60 IN A 192.0.2.1\n");
        zone.Nodes[0].Children.Add(zone.Nodes[1]);

        DnsTextResult result = new DnsZoneCodec().FormatZone(
            zone, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text.Split('\n'), Has.Length.EqualTo(3));
            Assert.That(result.Text.Split("192.0.2.1"), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void InvalidTextLimitsReturnDiagnostics()
    {
        DnsTextOptions options = new DnsTextOptions();
        options.MaximumLineLength = 0;

        DnsTextResult result = new DnsZoneCodec().FormatZone(
            new DnsZone(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("options"));
        });
    }

    static DnsZone ParseAbsoluteZone(string source)
    {
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        options.ValidateZone = false;
        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, options);
        Assert.That(result.Success, Is.True, FailureReason(result));
        return result.Zone!;
    }

    static DnsName ParseName(string text)
    {
        return new DnsNameService().ParseName(text, null);
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
