using System.Text;

namespace dns.Tests.Internal.Security;

public class CanonicalDnsDataTests
{
    [Test]
    public void EncodesCanonicalNamesInOwnersAndTypedRecordData()
    {
        DnsEncodeResult name = CanonicalDnsData.EncodeCanonicalName(
            Name("WWW", "ExAmPlE"));
        DnsResourceRecord cname = new DnsResourceRecord();
        cname.Name = Name("Alias", "Example");
        cname.RecordType = DnsRecordType.Cname;
        cname.RecordTypeValue = 5;
        cname.Class = DnsClass.Internet;
        cname.ClassValue = 1;
        cname.Data = new DnsRecordData();
        cname.Data.Name = new DnsNameData();
        cname.Data.Name.Name = Name("TaRgEt", "ExAmPlE");

        DnsEncodeResult recordData =
            CanonicalDnsData.EncodeCanonicalRecordData(cname);

        Assert.Multiple(() =>
        {
            Assert.That(name.Success, Is.True);
            Assert.That(name.Bytes, Is.EqualTo(new byte[]
            {
                3, 119, 119, 119,
                7, 101, 120, 97, 109, 112, 108, 101,
                0
            }));
            Assert.That(recordData.Success, Is.True);
            Assert.That(recordData.Bytes, Is.EqualTo(new byte[]
            {
                6, 116, 97, 114, 103, 101, 116,
                7, 101, 120, 97, 109, 112, 108, 101,
                0
            }));
        });
    }

    [Test]
    public void ReconstructsWildcardOwnerSortsAndDeduplicatesCanonicalRrset()
    {
        DnsResourceRecordSet set = Set(
            AddressRecord("Foo.Wild.Example", 12, [192, 0, 2, 2]),
            AddressRecord("foo.wild.example", 13, [192, 0, 2, 1]),
            AddressRecord("FOO.WILD.EXAMPLE", 14, [192, 0, 2, 1]));
        DnsSignatureData signature = Signature(
            2,
            3600,
            Name("Example"));

        DnsCanonicalRecordSetResult result =
            CanonicalDnsData.CanonicalizeRecordSet(set, signature);
        DnsDecodeResult decoded = ResourceRecordCodec.DecodeResourceRecord(
            result.Records[0].WireData,
            0,
            new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(Text(result.Owner!), Is.EqualTo("*.wild.example."));
            Assert.That(result.Records, Has.Count.EqualTo(2));
            Assert.That(result.Records[0].RecordData,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(result.Records[1].RecordData,
                Is.EqualTo(new byte[] { 192, 0, 2, 2 }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(Text(decoded.Record!.Name),
                Is.EqualTo("*.wild.example."));
            Assert.That(decoded.Record.TimeToLive, Is.EqualTo(3600));
            Assert.That(decoded.Record.RawData,
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
        });
    }

    [Test]
    public void BuildsExactRrsigCoveredBytesWithoutTheSignatureField()
    {
        DnsResourceRecordSet set = Set(
            AddressRecord("WWW.Example", 5, [192, 0, 2, 1]));
        DnsSignatureData signature = Signature(
            2,
            3600,
            Name("EXAMPLE"));
        signature.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        signature.AlgorithmValue = 8;
        signature.SignatureExpiration = 0x01020304;
        signature.SignatureInception = 0x05060708;
        signature.KeyTag = 0x1234;
        signature.Signature = [9, 9, 9];

        DnsEncodeResult result = CanonicalDnsData.BuildRrsigSignedData(
            set,
            signature);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes, Is.EqualTo(new byte[]
            {
                0, 1, 8, 2,
                0, 0, 14, 16,
                1, 2, 3, 4,
                5, 6, 7, 8,
                18, 52,
                7, 101, 120, 97, 109, 112, 108, 101, 0,
                3, 119, 119, 119,
                7, 101, 120, 97, 109, 112, 108, 101, 0,
                0, 1, 0, 1,
                0, 0, 14, 16,
                0, 4, 192, 0, 2, 1
            }));
            Assert.That(result.Bytes, Does.Not.Contain((byte)9));
        });
    }

    [Test]
    public void RejectsImpossibleLabelsAndRecordsOutsideTheRrset()
    {
        DnsCanonicalNameResult labels =
            CanonicalDnsData.ReconstructWildcardOwner(
                Name("example"),
                2);
        DnsResourceRecordSet set = Set(
            AddressRecord("www.example", 30, [192, 0, 2, 1]));
        set.Records[0].Name = Name("other.example");
        DnsCanonicalRecordSetResult membership =
            CanonicalDnsData.CanonicalizeRecordSet(
                set,
                Signature(2, 30, Name("example")));

        Assert.Multiple(() =>
        {
            Assert.That(labels.Success, Is.False);
            Assert.That(labels.Diagnostics[0].Field,
                Is.EqualTo("rrsig.labels"));
            Assert.That(membership.Success, Is.False);
            Assert.That(membership.Diagnostics[0].Field,
                Is.EqualTo("recordSet.records"));
        });
    }

    static DnsSignatureData Signature(
        int labels,
        uint originalTimeToLive,
        DnsName signerName)
    {
        DnsSignatureData result = new DnsSignatureData();
        result.TypeCovered = DnsRecordType.A;
        result.TypeCoveredValue = 1;
        result.Algorithm = DnsDnssecAlgorithm.RsaSha256;
        result.AlgorithmValue = 8;
        result.Labels = labels;
        result.OriginalTimeToLive = originalTimeToLive;
        result.SignerName = signerName;
        return result;
    }

    static DnsResourceRecordSet Set(params DnsResourceRecord[] records)
    {
        DnsResourceRecordSet result = new DnsResourceRecordSet();
        result.Name = records[0].Name;
        result.RecordType = DnsRecordType.A;
        result.RecordTypeValue = 1;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = records[0].TimeToLive;
        result.Records.AddRange(records);
        return result;
    }

    static DnsResourceRecord AddressRecord(
        string owner,
        uint timeToLive,
        byte[] address)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Parse(owner);
        result.RecordType = DnsRecordType.A;
        result.RecordTypeValue = 1;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = timeToLive;
        result.Data = new DnsRecordData();
        result.Data.Address = new DnsAddressData();
        result.Data.Address.Address = address;
        return result;
    }

    static DnsName Name(params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        int index = 0;
        while (index < labels.Length)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(labels[index]));
            index = index + 1;
        }

        return result;
    }

    static DnsName Parse(string text)
    {
        string[] labels = text.Split('.');
        return Name(labels);
    }

    static string Text(DnsName name)
    {
        if (name.Labels.Count == 0)
        {
            return ".";
        }

        string result = string.Empty;
        int index = 0;
        while (index < name.Labels.Count)
        {
            if (index > 0)
            {
                result = result + ".";
            }

            result = result + Encoding.ASCII.GetString(name.Labels[index]);
            index = index + 1;
        }

        return result + ".";
    }
}
