using System.Text;

namespace dns.Tests.Internal.Security;

public class ZoneDigestOperationsTests
{
    [Test]
    public void CalculatesRfc8976SimpleZoneSha384Vector()
    {
        DnsZone zone = ParseZone(
            "example. 86400 IN SOA ns1 admin 2018031900 1800 900 604800 86400\n" +
            "example. 86400 IN NS ns1\n" +
            "example. 86400 IN NS ns2\n" +
            "example. 86400 IN ZONEMD 2018031900 1 1 " +
                "c68090d90a7aed716bc459f9340e3d7c1370d4d24b7e2fc3" +
                "a1ddc0b9a87153b9a9713b3c9ae5cc27777f98b8e730044c\n" +
            "ns1 3600 IN A 203.0.113.63\n" +
            "ns2 3600 IN AAAA 2001:db8::63\n");

        DnsZoneDigestResult result =
            new DnsZoneDigestService().CalculateZoneDigest(zone, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Serial, Is.EqualTo(2018031900));
            Assert.That(result.Digest, Is.EqualTo(Convert.FromHexString(
                "c68090d90a7aed716bc459f9340e3d7c1370d4d24b7e2fc3" +
                "a1ddc0b9a87153b9a9713b3c9ae5cc27777f98b8e730044c")));
            Assert.That(result.IncludedRecordCount, Is.EqualTo(5));
            Assert.That(result.ExcludedRecordCount, Is.EqualTo(1));
            Assert.That(result.RecordData!.Digest, Is.EqualTo(result.Digest));
        });
    }

    [Test]
    public void CanonicalizesAndExcludesRecordsInRfc8976ComplexVector()
    {
        DnsZone zone = ParseZone(
            "example. 86400 IN SOA ns1 admin 2018031900 1800 900 604800 86400\n" +
            "example. 86400 IN NS ns1\n" +
            "example. 86400 IN NS ns2\n" +
            "example. 86400 IN ZONEMD 2018031900 1 1 " +
                "a3b69bad980a3504e1cffcb0fd6397f93848071c93151f55" +
                "2ae2f6b1711d4bd2d8b39808226d7b9db71e34b72077f8fe\n" +
            "ns1 3600 IN A 203.0.113.63\n" +
            "NS2 3600 IN AAAA 2001:db8::63\n" +
            "occluded.sub 7200 IN TXT \"I'm occluded but must be digested\"\n" +
            "sub 7200 IN NS ns1\n" +
            "duplicate 300 IN TXT \"I must be digested just once\"\n" +
            "duplicate 300 IN TXT \"I must be digested just once\"\n" +
            "foo.test. 555 IN TXT \"out-of-zone data must be excluded\"\n" +
            "UPPERCASE 3600 IN TXT \"canonicalize uppercase owner names\"\n" +
            "* 777 IN PTR dont-forget-about-wildcards\n" +
            "mail 3600 IN MX 20 MAIL1\n" +
            "mail 3600 IN MX 10 Mail2.Example.\n" +
            "sortme 3600 IN AAAA 2001:db8::5:61\n" +
            "sortme 3600 IN AAAA 2001:db8::3:62\n" +
            "sortme 3600 IN AAAA 2001:db8::4:63\n" +
            "sortme 3600 IN AAAA 2001:db8::1:65\n" +
            "sortme 3600 IN AAAA 2001:db8::2:64\n" +
            "non-apex 900 IN ZONEMD 2018031900 1 1 " +
                "616c6c6f776564206275742069676e6f7265642e20616c6c" +
                "6f776564206275742069676e6f7265642e20616c6c6f7765\n");

        DnsZoneDigestResult result =
            ZoneDigestOperations.Calculate(zone, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Digest, Is.EqualTo(Convert.FromHexString(
                "a3b69bad980a3504e1cffcb0fd6397f93848071c93151f55" +
                "2ae2f6b1711d4bd2d8b39808226d7b9db71e34b72077f8fe")));
            Assert.That(result.IncludedRecordCount, Is.EqualTo(18));
            Assert.That(result.ExcludedRecordCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void CalculatesBothRfc8976SupportedDigestAlgorithms()
    {
        DnsZone zone = ParseZone(
            "example. 86400 IN SOA ns1 admin 2018031900 1800 900 604800 86400\n" +
            "example. 86400 IN NS ns1.example.\n" +
            "example. 86400 IN NS ns2.example.\n" +
            "example. 86400 IN ZONEMD 2018031900 1 1 " +
                "62e6cf51b02e54b9b5f967d547ce43136792901f9f88e637" +
                "493daaf401c92c279dd10f0edb1c56f8080211f8480ee306\n" +
            "example. 86400 IN ZONEMD 2018031900 1 2 " +
                "08cfa1115c7b948c4163a901270395ea226a930cd2cbcf2f" +
                "a9a5e6eb85f37c8a4e114d884e66f176eab121cb02db7d65" +
                "2e0cc4827e7a3204f166b47e5613fd27\n" +
            "example. 86400 IN ZONEMD 2018031900 1 240 " +
                "e2d523f654b9422a96c5a8f44607bbee\n" +
            "example. 86400 IN ZONEMD 2018031900 241 1 " +
                "e1846540e33a9e4189792d18d5d131f605fc283e\n" +
            "ns1.example. 3600 IN A 203.0.113.63\n" +
            "ns2.example. 86400 IN TXT \"This example has multiple digests\"\n" +
            "NS2.EXAMPLE. 3600 IN AAAA 2001:db8::63\n");

        DnsZoneDigestResult sha384 =
            ZoneDigestOperations.Calculate(zone, 1, 1);
        DnsZoneDigestResult sha512 =
            ZoneDigestOperations.Calculate(zone, 1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(sha384.Digest, Is.EqualTo(Convert.FromHexString(
                "62e6cf51b02e54b9b5f967d547ce43136792901f9f88e637" +
                "493daaf401c92c279dd10f0edb1c56f8080211f8480ee306")));
            Assert.That(sha512.Digest, Is.EqualTo(Convert.FromHexString(
                "08cfa1115c7b948c4163a901270395ea226a930cd2cbcf2f" +
                "a9a5e6eb85f37c8a4e114d884e66f176eab121cb02db7d65" +
                "2e0cc4827e7a3204f166b47e5613fd27")));
            Assert.That(sha384.ExcludedRecordCount, Is.EqualTo(4));
            Assert.That(sha512.ExcludedRecordCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void ExcludesApexRrsigThatCoversZonemd()
    {
        DnsZone zone = ParseZone(
            "example. 300 IN SOA ns hostmaster 9 3600 600 86400 300\n" +
            "example. 300 IN NS ns\n" +
            "ns 300 IN A 192.0.2.53\n");
        DnsZoneDigestResult before =
            ZoneDigestOperations.Calculate(zone, 1, 1);
        AddApexZonemdSignature(zone);

        DnsZoneDigestResult after =
            ZoneDigestOperations.Calculate(zone, 1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(after.Success, Is.True);
            Assert.That(after.Digest, Is.EqualTo(before.Digest));
            Assert.That(after.IncludedRecordCount,
                Is.EqualTo(before.IncludedRecordCount));
            Assert.That(after.ExcludedRecordCount,
                Is.EqualTo(before.ExcludedRecordCount + 1));
        });
    }

    [Test]
    public void VerifiesDigestAndReportsMismatchOrUnsupportedInputs()
    {
        DnsZone zone = ParseZone(
            "example. 300 IN SOA ns hostmaster 9 3600 600 86400 300\n" +
            "example. 300 IN NS ns\n" +
            "ns 300 IN A 192.0.2.53\n");
        DnsZoneDigestService service = new DnsZoneDigestService();
        DnsZoneDigestResult calculated =
            service.CalculateZoneDigest(zone, 1, 1);
        DnsZoneDigestResult verified = service.VerifyZoneDigest(
            zone,
            calculated.RecordData!);
        DnsZonemdData mismatch = Copy(calculated.RecordData!);
        mismatch.Digest[0] = (byte)(mismatch.Digest[0] ^ 1);
        DnsZoneDigestResult different = service.VerifyZoneDigest(
            zone,
            mismatch);
        DnsZonemdData wrongSerial = Copy(calculated.RecordData!);
        wrongSerial.Serial = wrongSerial.Serial + 1;
        DnsZoneDigestResult serialMismatch = service.VerifyZoneDigest(
            zone,
            wrongSerial);
        DnsZonemdData wrongLength = Copy(calculated.RecordData!);
        wrongLength.Digest = [1];
        DnsZoneDigestResult invalidLength = service.VerifyZoneDigest(
            zone,
            wrongLength);
        DnsZoneDigestResult unsupported =
            service.CalculateZoneDigest(zone, 2, 1);

        Assert.Multiple(() =>
        {
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Verified, Is.True);
            Assert.That(verified.Diagnostics, Is.Empty);
            Assert.That(different.Success, Is.True);
            Assert.That(different.Verified, Is.False);
            Assert.That(different.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResponseMismatch));
            Assert.That(serialMismatch.Success, Is.True);
            Assert.That(serialMismatch.Verified, Is.False);
            Assert.That(serialMismatch.Diagnostics[0].Field,
                Is.EqualTo("expectedDigest.serial"));
            Assert.That(invalidLength.Success, Is.False);
            Assert.That(invalidLength.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnsupportedAlgorithm));
        });
    }

    static DnsZone ParseZone(string source)
    {
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        options.Origin = Name("example");
        options.Class = DnsClass.Internet;
        options.ClassValue = 1;
        options.ValidateZone = false;
        DnsZoneLoadResult loaded = new DnsZoneCodec().ParseZone(
            source,
            options);
        Assert.That(loaded.Success, Is.True,
            string.Join("; ", loaded.Diagnostics.Select(item => item.Reason)));
        return loaded.Zone!;
    }

    static void AddApexZonemdSignature(DnsZone zone)
    {
        DnsZoneNode apex = zone.Nodes.Single(node =>
            RecordSetOperations.NamesEqual(node.Name, zone.Apex));
        DnsSignatureData signature = new DnsSignatureData();
        signature.TypeCovered = DnsRecordType.Zonemd;
        signature.TypeCoveredValue = 63;
        DnsRecordData data = new DnsRecordData();
        data.Signature = signature;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = zone.Apex;
        record.RecordType = DnsRecordType.Rrsig;
        record.RecordTypeValue = 46;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 300;
        record.Data = data;
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = zone.Apex;
        set.RecordType = DnsRecordType.Rrsig;
        set.RecordTypeValue = 46;
        set.Class = DnsClass.Internet;
        set.ClassValue = 1;
        set.TimeToLive = 300;
        set.Records.Add(record);
        apex.RecordSets.Add(set);
    }

    static DnsZonemdData Copy(DnsZonemdData source)
    {
        DnsZonemdData result = new DnsZonemdData();
        result.Serial = source.Serial;
        result.Scheme = source.Scheme;
        result.HashAlgorithm = source.HashAlgorithm;
        result.Digest = source.Digest.ToArray();
        return result;
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
}
