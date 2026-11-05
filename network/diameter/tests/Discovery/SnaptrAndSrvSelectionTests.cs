namespace diameter.Tests;

public class SnaptrAndSrvSelectionTests
{
    [Test]
    public void ExtendedApplicationRecordsTakePrecedenceAndMatchOpaqueProtocols()
    {
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Tcp,
            DiameterSecurityMode.Tls);
        List<DiameterNaptrRecord> records = new List<DiameterNaptrRecord>();
        records.Add(DiscoveryTestData.CreateNaptrRecord(
            1, 1, "S", "aaa:diameter.tls.tcp", "legacy.example", 60));
        records.Add(DiscoveryTestData.CreateNaptrRecord(
            30, 20, "s", "AAA+AP4:DIAMETER.SCTP:DIAMETER.TLS.TCP",
            "second.example", 60));
        records.Add(DiscoveryTestData.CreateNaptrRecord(
            20, 30, "A", "aaa+ap4:diameter.tls.tcp", "first.example", 60));
        records.Add(DiscoveryTestData.CreateNaptrRecord(
            10, 10, "S", "aaa+ap1:diameter.tls.tcp", "other.example", 60));

        List<DiameterNaptrRecord> selected =
            SnaptrMatcher.SelectCompatibleNaptrRecords(records, request);

        Assert.Multiple(() =>
        {
            Assert.That(SnaptrMatcher.HasExtendedDiameterService(records), Is.True);
            Assert.That(selected, Has.Count.EqualTo(2));
            Assert.That(selected[0].Replacement, Is.EqualTo("first.example"));
            Assert.That(selected[1].Replacement, Is.EqualTo("second.example"));
            Assert.That(SnaptrMatcher.CreateDiameterApplicationService(request.Application),
                Is.EqualTo("aaa+ap4"));
            Assert.That(SnaptrMatcher.CreateDiameterProtocolTag(request),
                Is.EqualTo("diameter.tls.tcp"));
        });
    }

    [Test]
    public void LegacyApplicationRecordsApplyOnlyWithoutExtendedRecords()
    {
        DiameterDiscoveryRequest request = DiscoveryTestData.CreateDiscoveryRequest(
            DiameterApplicationIds.CreditControl,
            DiameterTransportKind.Sctp,
            DiameterSecurityMode.None);
        List<DiameterNaptrRecord> records = new List<DiameterNaptrRecord>();
        records.Add(DiscoveryTestData.CreateNaptrRecord(
            1, 1, "S", "aaa:diameter.sctp", "legacy.example", 60));
        records.Add(DiscoveryTestData.CreateNaptrRecord(
            1, 2, "S", "aaa+ap04:diameter.sctp", "malformed.example", 60));

        List<DiameterNaptrRecord> selected =
            SnaptrMatcher.SelectCompatibleNaptrRecords(records, request);

        Assert.Multiple(() =>
        {
            Assert.That(SnaptrMatcher.HasExtendedDiameterService(records), Is.False);
            Assert.That(selected, Has.Count.EqualTo(1));
            Assert.That(selected[0].Replacement, Is.EqualTo("legacy.example"));
            Assert.That(SnaptrMatcher.IsValidExtendedDiameterService("aaa+ap0"), Is.True);
            Assert.That(SnaptrMatcher.IsValidExtendedDiameterService("aaa+ap04"), Is.False);
            Assert.That(SnaptrMatcher.IsValidExtendedDiameterService("aaa+ap4294967295"), Is.True);
            Assert.That(SnaptrMatcher.IsValidExtendedDiameterService("aaa+ap4294967296"), Is.False);
        });
    }

    [Test]
    public void TransportAndSecurityMapToRegisteredProtocolAndSrvNames()
    {
        DiameterDiscoveryRequest tcp = DiscoveryTestData.CreateDiscoveryRequest(
            4, DiameterTransportKind.Tcp, DiameterSecurityMode.None);
        DiameterDiscoveryRequest tls = DiscoveryTestData.CreateDiscoveryRequest(
            4, DiameterTransportKind.Tcp, DiameterSecurityMode.Tls);
        DiameterDiscoveryRequest sctp = DiscoveryTestData.CreateDiscoveryRequest(
            4, DiameterTransportKind.Sctp, DiameterSecurityMode.None);
        DiameterDiscoveryRequest dtls = DiscoveryTestData.CreateDiscoveryRequest(
            4, DiameterTransportKind.Sctp, DiameterSecurityMode.Dtls);

        Assert.Multiple(() =>
        {
            Assert.That(SnaptrMatcher.CreateDiameterProtocolTag(tcp),
                Is.EqualTo("diameter.tcp"));
            Assert.That(SnaptrMatcher.CreateDiameterProtocolTag(tls),
                Is.EqualTo("diameter.tls.tcp"));
            Assert.That(SnaptrMatcher.CreateDiameterProtocolTag(sctp),
                Is.EqualTo("diameter.sctp"));
            Assert.That(SnaptrMatcher.CreateDiameterProtocolTag(dtls),
                Is.EqualTo("diameter.dtls.sctp"));
            Assert.That(SnaptrMatcher.CreateDiameterSrvFallbackName(tcp),
                Is.EqualTo("_diameter._tcp.example"));
            Assert.That(SnaptrMatcher.CreateDiameterSrvFallbackName(tls),
                Is.EqualTo("_diameters._tcp.example"));
            Assert.That(SnaptrMatcher.CreateDiameterSrvFallbackName(sctp),
                Is.EqualTo("_diameter._sctp.example"));
            Assert.That(SnaptrMatcher.CreateDiameterSrvFallbackName(dtls),
                Is.EqualTo("_diameters._sctp.example"));
        });
    }

    [Test]
    public void SrvSelectionHonorsPriorityThenRfcWeightedDrawing()
    {
        DiameterSrvRecord zero = DiscoveryTestData.CreateSrvRecord(
            0, 0, 3868, "zero.example", 60);
        DiameterSrvRecord light = DiscoveryTestData.CreateSrvRecord(
            0, 1, 3868, "light.example", 60);
        DiameterSrvRecord heavy = DiscoveryTestData.CreateSrvRecord(
            0, 3, 3868, "heavy.example", 60);
        DiameterSrvRecord later = DiscoveryTestData.CreateSrvRecord(
            1, 100, 3868, "later.example", 60);
        DiameterSrvRecord unavailable = DiscoveryTestData.CreateSrvRecord(
            0, 100, 3868, ".", 60);
        SequenceDiscoveryRandom random = new SequenceDiscoveryRandom(0, 4, 0, 0);
        SrvSelector selector = new SrvSelector(random);

        List<DiameterSrvRecord> ordered = selector.OrderSrvRecords(
            new List<DiameterSrvRecord>
            {
                heavy,
                later,
                unavailable,
                light,
                zero
            });

        Assert.That(ordered, Is.EqualTo(new[] { zero, light, heavy, later }));
    }

    [Test]
    public void SrvSelectionFiltersInvalidRecordsAndValidatesWeightedDraws()
    {
        DiameterSrvRecord light = DiscoveryTestData.CreateSrvRecord(
            0, 1, 3868, "light.example", 60);
        DiameterSrvRecord heavy = DiscoveryTestData.CreateSrvRecord(
            0, 3, 3868, "heavy.example", 60);
        DiameterSrvRecord invalidPriority = DiscoveryTestData.CreateSrvRecord(
            -1, 1, 3868, "priority.example", 60);
        DiameterSrvRecord invalidWeight = DiscoveryTestData.CreateSrvRecord(
            0, -1, 3868, "weight.example", 60);
        DiameterSrvRecord invalidPort = DiscoveryTestData.CreateSrvRecord(
            0, 1, 0, "port.example", 60);
        DiameterSrvRecord unavailable = DiscoveryTestData.CreateSrvRecord(
            0, 1, 3868, ".", 60);
        List<DiameterSrvRecord> records = new List<DiameterSrvRecord>
        {
            invalidPriority,
            light,
            invalidWeight,
            invalidPort,
            heavy,
            unavailable
        };
        SrvSelector selector = new SrvSelector(new SequenceDiscoveryRandom(1, 0));

        List<DiameterSrvRecord> ordered = selector.OrderSrvRecords(records);
        SrvSelector invalidSelector = new SrvSelector(
            new SequenceDiscoveryRandom(5));

        Assert.Multiple(() =>
        {
            Assert.That(ordered, Is.EqualTo(new[] { light, heavy }));
            Assert.Throws<DiameterPeerException>(() =>
                invalidSelector.OrderSrvRecords(
                    new List<DiameterSrvRecord> { light, heavy }));
        });
    }
}
