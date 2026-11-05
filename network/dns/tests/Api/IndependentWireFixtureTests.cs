using System.Text;

namespace dns.Tests.Api;

public class IndependentWireFixtureTests
{
    [Test]
    public void DnspythonNameAndRecordFixturesAreByteExact()
    {
        byte[] nameWire = Hex(
            "054d69586544074578616d706c6500");
        byte[] svcbWire = Hex(
            "000103737663076578616d706c6500" +
            "000100060268320268330003000201bb00040004c0000201");
        byte[] dnskeyWire = Hex("0101030801020304");
        IDnsNameService names = new DnsNameService();
        IDnsRecordDataCodec records = new DnsRecordDataCodec();

        DnsDecodeResult decodedName = names.DecodeName(
            nameWire,
            0,
            new DnsDecodeOptions());
        DnsEncodeResult encodedName = names.EncodeName(
            decodedName.Name!,
            new DnsEncodeOptions());
        DnsDecodeResult decodedSvcb = records.DecodeRecordData(
            DnsRecordType.Svcb,
            (int)DnsRecordType.Svcb,
            svcbWire,
            0,
            svcbWire.Length,
            new DnsDecodeOptions());
        DnsEncodeResult encodedSvcb = records.EncodeRecordData(
            DnsRecordType.Svcb,
            (int)DnsRecordType.Svcb,
            decodedSvcb.RecordData!,
            new DnsEncodeOptions());
        DnsDecodeResult decodedDnskey = records.DecodeRecordData(
            DnsRecordType.Dnskey,
            (int)DnsRecordType.Dnskey,
            dnskeyWire,
            0,
            dnskeyWire.Length,
            new DnsDecodeOptions());
        DnsEncodeResult encodedDnskey = records.EncodeRecordData(
            DnsRecordType.Dnskey,
            (int)DnsRecordType.Dnskey,
            decodedDnskey.RecordData!,
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encodedName.Bytes, Is.EqualTo(nameWire));
            Assert.That(decodedName.Name!.Labels[0],
                Is.EqualTo(Encoding.ASCII.GetBytes("MiXeD")));
            Assert.That(encodedSvcb.Bytes, Is.EqualTo(svcbWire));
            Assert.That(decodedSvcb.RecordData!.Svcb!.Priority,
                Is.EqualTo(1));
            Assert.That(decodedSvcb.RecordData.Svcb.Parameters,
                Has.Count.EqualTo(3));
            Assert.That(encodedDnskey.Bytes, Is.EqualTo(dnskeyWire));
            Assert.That(decodedDnskey.RecordData!.Key!.Flags,
                Is.EqualTo(257));
            Assert.That(decodedDnskey.RecordData.Key.PublicKey,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    [Test]
    public void DnspythonEdnsMessageFixtureIsByteExact()
    {
        byte[] wire = Hex(
            "123401000001000000000001" +
            "03777777076578616d706c650000010001" +
            "00002904d000008000000c000a00080102030405060708");
        IDnsMessageCodec codec = new DnsMessageCodec();

        DnsDecodeResult decoded = codec.DecodeMessage(
            wire,
            new DnsDecodeOptions());
        DnsEncodeResult encoded = codec.EncodeMessage(
            decoded.Message!,
            new DnsEncodeOptions());
        DnsResourceRecord opt = decoded.Message!.Additionals.Single();

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Bytes, Is.EqualTo(wire));
            Assert.That(decoded.Message.Header.Identifier,
                Is.EqualTo(0x1234));
            Assert.That(opt.RecordType, Is.EqualTo(DnsRecordType.Opt));
            Assert.That(opt.Data!.Edns!.UdpPayloadSize, Is.EqualTo(1232));
            Assert.That(opt.Data.Edns.Flags,
                Is.EqualTo(DnsEdnsFlag.DnssecAnswerOk));
            Assert.That(opt.Data.Edns.Options.Single().Cookie!.ClientCookie,
                Is.EqualTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
        });
    }

    [Test]
    public void DnspythonAxfrMessageFixtureIsByteExact()
    {
        byte[] wire = Hex(
            "424285000001000400000000" +
            "076578616d706c650000fc0001" +
            "c00c000600010000012c0026026e73c00c" +
            "0a686f73746d6173746572c00c78a3f175" +
            "00000e1000000258000151800000012c" +
            "c00c000200010000012c0002c025" +
            "c025000100010000012c0004c0000235" +
            "c00c000600010000012c0018c025c02a" +
            "78a3f17500000e1000000258000151800000012c");
        IDnsMessageCodec codec = new DnsMessageCodec();

        DnsDecodeResult decoded = codec.DecodeMessage(
            wire,
            new DnsDecodeOptions());
        DnsEncodeResult encoded = codec.EncodeMessage(
            decoded.Message!,
            new DnsEncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Bytes, Is.EqualTo(wire));
            Assert.That(decoded.Message!.Questions.Single().RecordType,
                Is.EqualTo(DnsRecordType.Axfr));
            Assert.That(decoded.Message.Answers, Has.Count.EqualTo(4));
            Assert.That(decoded.Message.Answers.First().RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(decoded.Message.Answers.Last().RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(decoded.Message.Answers.First().Data!.Soa!.Serial,
                Is.EqualTo(2024010101));
        });
    }

    [Test]
    public void DnspythonTsigFixtureAuthenticatesAndIsByteExact()
    {
        byte[] wire = Hex(
            "515101000001000000000001" +
            "067369676e6564076578616d706c650000010001" +
            "036b6579c01300fa00ff00000000003d" +
            "0b686d61632d7368613235360000006553f100012c0020" +
            "69569d436a8bd6f5996a613a4035a66d" +
            "466f2f6d37f2d62acbaead0faca38e6a" +
            "515100000000");
        IDnsMessageCodec codec = new DnsMessageCodec();
        DnsDecodeResult decoded = codec.DecodeMessage(
            wire,
            new DnsDecodeOptions());
        DnsEncodeResult encoded = codec.EncodeMessage(
            decoded.Message!,
            new DnsEncodeOptions());
        DnsTsigKey key = new DnsTsigKey();
        key.Name = new DnsNameService().ParseName("key.example.", null);
        key.AlgorithmName = new DnsNameService().ParseName(
            "hmac-sha256.",
            null);
        key.Secret = Encoding.ASCII.GetBytes(
            "independent-fixture-secret");
        key.Fudge = 300;
        DnsClockValue now = new DnsClockValue();
        now.UnixTimeMilliseconds = 1700000000000;

        DnsTsigResult verified = new DnsTsigService().VerifyTsig(
            decoded.Message!,
            key,
            [],
            now);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Bytes, Is.EqualTo(wire));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Authenticated, Is.True);
            Assert.That(verified.Error, Is.EqualTo(DnsTsigError.NoError));
            Assert.That(verified.Mac, Is.EqualTo(Hex(
                "69569d436a8bd6f5996a613a4035a66d" +
                "466f2f6d37f2d62acbaead0faca38e6a")));
        });
    }

    static byte[] Hex(string text)
    {
        return Convert.FromHexString(text);
    }
}
