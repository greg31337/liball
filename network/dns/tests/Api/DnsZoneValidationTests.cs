namespace dns.Tests.Api;

public class DnsZoneValidationTests
{
    [Test]
    public void ValidatesAuthoritativeDataDelegationGlueAndSerialWraparound()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 0 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n" +
            "www 300 A 192.0.2.2\n" +
            "alias 300 CNAME www\n" +
            "branch 300 DNAME target.example.net.\n" +
            "child 300 NS ns.child\n" +
            "ns.child 300 AAAA 2001:db8::53\n");
        DnsZoneJournalEntry entry = new DnsZoneJournalEntry();
        entry.Sequence = 1;
        entry.FromSerial = uint.MaxValue;
        entry.ToSerial = 0;
        entry.Transaction.Committed = true;
        entry.Transaction.HasExpectedSerial = true;
        entry.Transaction.ExpectedSerial = uint.MaxValue;
        entry.Transaction.HasResultingSerial = true;
        entry.Transaction.ResultingSerial = 0;
        zone.JournalEntries.Add(entry);

        DnsZoneValidationResult result =
            new DnsZoneCodec().ValidateZone(zone);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.HasApexSoa, Is.True);
            Assert.That(result.HasApexNameServer, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(FindNode(zone, "child.example.").IsDelegation,
                Is.True);
        });
    }

    [Test]
    public void ParseZoneRunsRequestedStructuralValidation()
    {
        DnsZoneLoadOptions options = CreateOptions(true);
        string source =
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n";

        DnsZoneLoadResult result =
            new DnsZoneCodec().ParseZone(source, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(result.Validation, Is.Not.Null);
            Assert.That(result.Validation!.Valid, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void RejectsMissingApexNameServerDuringRequestedValidation()
    {
        DnsZoneLoadOptions options = CreateOptions(true);

        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Zone, Is.Not.Null);
            Assert.That(result.Validation, Is.Not.Null);
            Assert.That(result.Validation!.HasApexSoa, Is.True);
            Assert.That(result.Validation.HasApexNameServer, Is.False);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("at least one NS")),
                Is.True);
        });
    }

    [Test]
    public void EnforcesApexSoaCardinalityAndPlacement()
    {
        DnsZone missing = ParseWithoutValidation(
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n");
        DnsZone duplicate = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 SOA ns hostmaster 2 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n");
        DnsZone nonApex = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n" +
            "child 300 SOA ns hostmaster 2 3600 600 86400 300\n");

        DnsZoneValidationResult missingResult =
            new DnsZoneCodec().ValidateZone(missing);
        DnsZoneValidationResult duplicateResult =
            new DnsZoneCodec().ValidateZone(duplicate);
        DnsZoneValidationResult nonApexResult =
            new DnsZoneCodec().ValidateZone(nonApex);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.HasApexSoa, Is.False);
            Assert.That(missingResult.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("exactly one SOA")),
                Is.True);
            Assert.That(duplicateResult.HasApexSoa, Is.False);
            Assert.That(duplicateResult.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("exactly one SOA")),
                Is.True);
            Assert.That(nonApexResult.HasApexSoa, Is.True);
            Assert.That(nonApexResult.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("only at the zone apex")),
                Is.True);
        });
    }

    [Test]
    public void ReportsOwnerAndRrsetTimeToLiveMismatchesOnTheRecord()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n");
        DnsResourceRecordSet set =
            FindNode(zone, "ns.example.").RecordSets[0];
        DnsResourceRecord record = set.Records[0];
        record.Name = ParseName("other.example.");
        record.TimeToLive = 301;

        DnsZoneValidationResult result =
            new DnsZoneCodec().ValidateZone(zone);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Record == record &&
                    diagnostic.Reason.Contains("owner")),
                Is.True);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Record == record &&
                    diagnostic.Reason.Contains("same TTL")),
                Is.True);
        });
    }

    [Test]
    public void RejectsCnameCoexistingWithOrdinaryData()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n" +
            "alias 300 CNAME ns\n" +
            "alias 300 A 192.0.2.9\n");

        DnsZoneValidationResult result =
            new DnsZoneCodec().ValidateZone(zone);

        Assert.That(result.Diagnostics.Any(diagnostic =>
                diagnostic.Reason.Contains("CNAME cannot coexist")),
            Is.True);
    }

    [Test]
    public void RejectsDataBelowDnameAndDnameAtADelegation()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n" +
            "branch 300 DNAME target.example.net.\n" +
            "branch 300 NS ns.branch\n" +
            "ns.branch 300 A 192.0.2.53\n" +
            "host.branch 300 A 192.0.2.54\n");

        DnsZoneValidationResult result =
            new DnsZoneCodec().ValidateZone(zone);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("DNAME and NS")),
                Is.True);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("below a DNAME")),
                Is.True);
        });
    }

    [Test]
    public void RejectsMissingGlueAndNonGlueDataBelowDelegation()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n" +
            "child 300 NS ns.child\n" +
            "host.child 300 TXT \"not glue\"\n");

        DnsZoneValidationResult result =
            new DnsZoneCodec().ValidateZone(zone);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("requires A or AAAA glue")),
                Is.True);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("required address glue")),
                Is.True);
        });
    }

    [Test]
    public void PermitsExternalNameServersAndDnssecDataAtDelegationCuts()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n" +
            "child 300 NS ns.external.test.\n" +
            "child 300 DS 12345 8 2 A1B2C3D4\n" +
            "child 300 RRSIG DS 8 2 300 20300101000000 " +
                "20200101000000 42 example. AQI=\n" +
            "child 300 NSEC next NS DS RRSIG NSEC\n");

        DnsZoneValidationResult result =
            new DnsZoneCodec().ValidateZone(zone);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True,
                string.Join("; ", result.Diagnostics.Select(
                    diagnostic => diagnostic.Reason)));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(FindNode(zone, "child.example.").IsDelegation,
                Is.True);
        });
    }

    [Test]
    public void RejectsOwnersOutsideTheZoneApex()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 1 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n" +
            "outside.test. 300 TXT \"foreign\"\n");

        DnsZoneValidationResult result =
            new DnsZoneCodec().ValidateZone(zone);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Name != null &&
                    FormatName(diagnostic.Name) == "outside.test." &&
                    diagnostic.Reason.Contains("at or below")),
                Is.True);
        });
    }

    [Test]
    public void ReportsVerifiedStaleAndMismatchedZonemdStates()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 9 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "@ 300 ZONEMD 9 1 1 " +
                "000000000000000000000000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000\n" +
            "ns 300 A 192.0.2.1\n");
        DnsZoneValidationResult structural =
            new DnsZoneCodec().ValidateZone(zone);
        DnsZoneDigestService service = new DnsZoneDigestService();
        DnsZoneDigestResult calculated =
            service.CalculateZoneDigest(zone, 1, 1);
        DnsResourceRecordSet digestSet = FindNode(zone, "example.")
            .RecordSets.Single(set => set.RecordType == DnsRecordType.Zonemd);
        DnsZonemdData published = digestSet.Records[0].Data!.Zonemd!;
        published.Digest = calculated.Digest.ToArray();
        DnsZoneDigestResult verified =
            service.VerifyZoneDigest(zone, published);
        published.Serial = 10;
        DnsZoneDigestResult stale =
            service.VerifyZoneDigest(zone, published);
        published.Serial = 9;
        published.Digest[0] = (byte)(published.Digest[0] ^ 1);
        DnsZoneDigestResult mismatched =
            service.VerifyZoneDigest(zone, published);

        Assert.Multiple(() =>
        {
            Assert.That(structural.Valid, Is.True);
            Assert.That(calculated.Success, Is.True);
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Verified, Is.True);
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Verified, Is.False);
            Assert.That(stale.Diagnostics[0].Field,
                Is.EqualTo("expectedDigest.serial"));
            Assert.That(mismatched.Success, Is.True);
            Assert.That(mismatched.Verified, Is.False);
            Assert.That(mismatched.Diagnostics[0].Field,
                Is.EqualTo("expectedDigest.digest"));
        });
    }

    [Test]
    public void RejectsUndefinedSerialAdvanceAndJournalInconsistency()
    {
        DnsZone zone = ParseWithoutValidation(
            "@ 300 SOA ns hostmaster 2147483648 3600 600 86400 300\n" +
            "@ 300 NS ns\n" +
            "ns 300 A 192.0.2.1\n");
        DnsZoneJournalEntry first = new DnsZoneJournalEntry();
        first.Sequence = 2;
        first.FromSerial = 0;
        first.ToSerial = 2147483648;
        first.Transaction.Committed = false;
        zone.JournalEntries.Add(first);
        DnsZoneJournalEntry second = new DnsZoneJournalEntry();
        second.Sequence = 1;
        second.FromSerial = 7;
        second.ToSerial = 2147483648;
        second.Transaction.Committed = true;
        zone.JournalEntries.Add(second);

        DnsZoneValidationResult result =
            new DnsZoneCodec().ValidateZone(zone);

        Assert.Multiple(() =>
        {
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("RFC 1982")),
                Is.True);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("sequence values")),
                Is.True);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("continuous chain")),
                Is.True);
            Assert.That(result.Diagnostics.Any(diagnostic =>
                    diagnostic.Reason.Contains("committed transactions")),
                Is.True);
        });
    }

    static DnsZone ParseWithoutValidation(string source)
    {
        DnsZoneLoadResult result = new DnsZoneCodec().ParseZone(
            source, CreateOptions(false));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Zone, Is.Not.Null);
        return result.Zone!;
    }

    static DnsZoneLoadOptions CreateOptions(bool validateZone)
    {
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        options.Origin = ParseName("example.");
        options.Class = DnsClass.Internet;
        options.ClassValue = 1;
        options.ValidateZone = validateZone;
        return options;
    }

    static DnsZoneNode FindNode(DnsZone zone, string name)
    {
        DnsName expected = ParseName(name);
        DnsNameService service = new DnsNameService();
        return zone.Nodes.Single(node =>
            service.NamesEqual(node.Name, expected));
    }

    static DnsName ParseName(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    static string FormatName(DnsName name)
    {
        return new DnsNameService().FormatName(
            name, new DnsTextOptions());
    }
}
