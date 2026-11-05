using System.Net;

namespace diameter.Tests;

public class DnsMessageCodecTests
{
    [Test]
    public void QueryEncodingProducesExactRecursiveDnsQuestion()
    {
        byte[] encoded = DnsMessageCodec.EncodeDnsQuery(
            "_diameter._tcp.example.",
            DnsMessageCodec.SrvRecordType,
            0x1234);

        byte[] expected = new byte[]
        {
            0x12, 0x34, 0x01, 0x00,
            0x00, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x09, 0x5f, 0x64, 0x69, 0x61, 0x6d, 0x65, 0x74, 0x65, 0x72,
            0x04, 0x5f, 0x74, 0x63, 0x70,
            0x07, 0x65, 0x78, 0x61, 0x6d, 0x70, 0x6c, 0x65,
            0x00, 0x00, 0x21, 0x00, 0x01
        };

        Assert.That(encoded, Is.EqualTo(expected));
    }

    [Test]
    public void ResponseDecodingHandlesCompressedNamesAndEverySupportedRecord()
    {
        DateTimeOffset receivedAt = new DateTimeOffset(
            2026, 8, 29, 18, 0, 0, TimeSpan.Zero);
        byte[] query = DnsMessageCodec.EncodeDnsQuery(
            "example",
            DnsMessageCodec.NaptrRecordType,
            0x4321);
        byte[] naptr = DnsTestMessages.BuildRecord(
            DnsMessageCodec.NaptrRecordType,
            60,
            DnsTestMessages.BuildNaptrData(
                10,
                20,
                "S",
                "AAA+D2T",
                "",
                "peer.example"));
        byte[] srv = DnsTestMessages.BuildRecord(
            DnsMessageCodec.SrvRecordType,
            45,
            DnsTestMessages.BuildSrvData(1, 10, 3868, "peer.example"));
        byte[] ipv4 = DnsTestMessages.BuildRecord(
            DnsMessageCodec.Ipv4RecordType,
            30,
            new byte[] { 192, 0, 2, 10 });
        byte[] ipv6 = DnsTestMessages.BuildRecord(
            DnsMessageCodec.Ipv6RecordType,
            15,
            IPAddress.Parse("2001:db8::10").GetAddressBytes());
        byte[] response = DnsTestMessages.BuildResponse(
            query,
            0x8580,
            naptr,
            srv,
            ipv4,
            ipv6);

        DiameterDnsQueryResult result = DnsMessageCodec.DecodeDnsResponse(
            response,
            0x4321,
            "EXAMPLE.",
            DnsMessageCodec.NaptrRecordType,
            receivedAt);

        Assert.Multiple(() =>
        {
            Assert.That(result.QueryName, Is.EqualTo("EXAMPLE"));
            Assert.That(result.RecordType, Is.EqualTo(DnsMessageCodec.NaptrRecordType));
            Assert.That(result.ResponseCode, Is.Zero);
            Assert.That(result.Successful, Is.True);
            Assert.That(result.Authoritative, Is.True);
            Assert.That(result.RecursionAvailable, Is.True);
            Assert.That(result.UsedTcpFallback, Is.False);
            Assert.That(result.Code, Is.EqualTo("dns-success"));
            Assert.That(result.NaptrRecords, Has.Count.EqualTo(1));
            Assert.That(result.SrvRecords, Has.Count.EqualTo(1));
            Assert.That(result.AddressRecords, Has.Count.EqualTo(2));

            Assert.That(result.NaptrRecords[0].Name, Is.EqualTo("example"));
            Assert.That(result.NaptrRecords[0].Order, Is.EqualTo(10));
            Assert.That(result.NaptrRecords[0].Preference, Is.EqualTo(20));
            Assert.That(result.NaptrRecords[0].Flags, Is.EqualTo("S"));
            Assert.That(result.NaptrRecords[0].Service, Is.EqualTo("AAA+D2T"));
            Assert.That(result.NaptrRecords[0].RegularExpression, Is.Empty);
            Assert.That(result.NaptrRecords[0].Replacement, Is.EqualTo("peer.example"));
            Assert.That(result.NaptrRecords[0].TimeToLiveSeconds, Is.EqualTo(60U));
            Assert.That(result.NaptrRecords[0].ExpiresAt,
                Is.EqualTo(receivedAt.AddSeconds(60)));

            Assert.That(result.SrvRecords[0].Name, Is.EqualTo("example"));
            Assert.That(result.SrvRecords[0].Priority, Is.EqualTo(1));
            Assert.That(result.SrvRecords[0].Weight, Is.EqualTo(10));
            Assert.That(result.SrvRecords[0].Port, Is.EqualTo(3868));
            Assert.That(result.SrvRecords[0].Target, Is.EqualTo("peer.example"));
            Assert.That(result.SrvRecords[0].TimeToLiveSeconds, Is.EqualTo(45U));
            Assert.That(result.SrvRecords[0].ExpiresAt,
                Is.EqualTo(receivedAt.AddSeconds(45)));

            Assert.That(result.AddressRecords[0].Address,
                Is.EqualTo(IPAddress.Parse("192.0.2.10")));
            Assert.That(result.AddressRecords[0].ExpiresAt,
                Is.EqualTo(receivedAt.AddSeconds(30)));
            Assert.That(result.AddressRecords[1].Address,
                Is.EqualTo(IPAddress.Parse("2001:db8::10")));
            Assert.That(result.AddressRecords[1].ExpiresAt,
                Is.EqualTo(receivedAt.AddSeconds(15)));
        });
    }

    [Test]
    public void NameCompressionCanComposeLabelsWithEarlierSuffixes()
    {
        byte[] message = new byte[]
        {
            0x07, (byte)'e', (byte)'x', (byte)'a', (byte)'m', (byte)'p', (byte)'l', (byte)'e', 0,
            0x04, (byte)'p', (byte)'e', (byte)'e', (byte)'r', 0xc0, 0x00
        };

        DnsNameResult result = DnsMessageCodec.ReadDnsName(message, 9);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("peer.example"));
            Assert.That(result.NextOffset, Is.EqualTo(message.Length));
        });
    }

    [Test]
    public void NameErrorResponseIsAValidNegativeOutcome()
    {
        byte[] query = DnsMessageCodec.EncodeDnsQuery(
            "missing.example",
            DnsMessageCodec.Ipv4RecordType,
            7);
        byte[] response = DnsTestMessages.BuildResponse(query, 0x8183);

        DiameterDnsQueryResult result = DnsMessageCodec.DecodeDnsResponse(
            response,
            7,
            "missing.example",
            DnsMessageCodec.Ipv4RecordType,
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.False);
            Assert.That(result.ResponseCode, Is.EqualTo(3));
            Assert.That(result.Code, Is.EqualTo("dns-name-error"));
            Assert.That(result.AddressRecords, Is.Empty);
        });
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(5)]
    public void OtherDnsResponseCodesHaveStableErrorOutcome(int responseCode)
    {
        byte[] query = DnsMessageCodec.EncodeDnsQuery(
            "error.example",
            DnsMessageCodec.Ipv4RecordType,
            15);
        byte[] response = DnsTestMessages.BuildResponse(
            query,
            0x8180 | responseCode);

        DiameterDnsQueryResult result = DnsMessageCodec.DecodeDnsResponse(
            response,
            15,
            "error.example",
            DnsMessageCodec.Ipv4RecordType,
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.False);
            Assert.That(result.ResponseCode, Is.EqualTo(responseCode));
            Assert.That(result.Code, Is.EqualTo("dns-response-error"));
        });
    }

    [Test]
    public void NameErrorReadsRfc2308NegativeLifetimeFromAuthoritySoa()
    {
        byte[] query = DnsMessageCodec.EncodeDnsQuery(
            "missing.example",
            DnsMessageCodec.Ipv4RecordType,
            17);
        List<byte> soaData = new List<byte>();
        soaData.AddRange(DnsTestMessages.EncodeName("ns.example"));
        soaData.AddRange(DnsTestMessages.EncodeName("hostmaster.example"));
        DnsTestMessages.AddUInt32(soaData, 1);
        DnsTestMessages.AddUInt32(soaData, 3600);
        DnsTestMessages.AddUInt32(soaData, 600);
        DnsTestMessages.AddUInt32(soaData, 86400);
        DnsTestMessages.AddUInt32(soaData, 120);
        byte[] soa = DnsTestMessages.BuildRecord(
            DnsMessageCodec.SoaRecordType,
            300,
            soaData.ToArray());
        byte[] response = DnsTestMessages.BuildResponse(query, 0x8583, soa);
        response[6] = 0;
        response[7] = 0;
        response[8] = 0;
        response[9] = 1;

        DiameterDnsQueryResult result = DnsMessageCodec.DecodeDnsResponse(
            response,
            17,
            "missing.example",
            DnsMessageCodec.Ipv4RecordType,
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Successful, Is.False);
            Assert.That(result.ResponseCode, Is.EqualTo(3));
            Assert.That(result.NegativeTimeToLiveSeconds, Is.EqualTo(120U));
        });
    }

    [Test]
    public void TruncationIsDetectedBeforeRecordDecoding()
    {
        byte[] query = DnsMessageCodec.EncodeDnsQuery(
            "example",
            DnsMessageCodec.Ipv4RecordType,
            8);
        byte[] response = DnsTestMessages.BuildResponse(query, 0x8380);

        DnsResponseHeader header = DnsMessageCodec.ReadDnsResponseHeader(
            response,
            8,
            "example",
            DnsMessageCodec.Ipv4RecordType);

        Assert.Multiple(() =>
        {
            Assert.That((header.Flags & 512) != 0, Is.True);
            Assert.Throws<DiameterCodecException>(() =>
                DnsMessageCodec.DecodeDnsResponse(
                    response,
                    8,
                    "example",
                    DnsMessageCodec.Ipv4RecordType,
                    DateTimeOffset.UtcNow));
        });
    }

    [Test]
    public void CorrelationRejectsWrongIdentifiersQuestionsAndTypes()
    {
        byte[] query = DnsMessageCodec.EncodeDnsQuery(
            "example",
            DnsMessageCodec.Ipv4RecordType,
            9);
        byte[] response = DnsTestMessages.BuildResponse(query, 0x8180);

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterCodecException>(() =>
                DnsMessageCodec.ReadDnsResponseHeader(
                    response,
                    10,
                    "example",
                    DnsMessageCodec.Ipv4RecordType));
            Assert.Throws<DiameterCodecException>(() =>
                DnsMessageCodec.ReadDnsResponseHeader(
                    response,
                    9,
                    "other.example",
                    DnsMessageCodec.Ipv4RecordType));
            Assert.Throws<DiameterCodecException>(() =>
                DnsMessageCodec.ReadDnsResponseHeader(
                    response,
                    9,
                    "example",
                    DnsMessageCodec.Ipv6RecordType));
        });
    }

    [Test]
    public void DefensiveLimitsRejectMalformedNamesCountsAndRecordLengths()
    {
        byte[] pointerLoop = new byte[]
        {
            0, 1, 0x81, 0x80,
            0, 1, 0, 0,
            0, 0, 0, 0,
            0xc0, 0x0c, 0, 1, 0, 1
        };

        byte[] query = DnsMessageCodec.EncodeDnsQuery(
            "example",
            DnsMessageCodec.Ipv4RecordType,
            11);
        byte[] excessiveCount = DnsTestMessages.BuildResponse(query, 0x8180);
        excessiveCount[6] = 2;
        excessiveCount[7] = 1;

        byte[] shortAddress = DnsTestMessages.BuildRecord(
            DnsMessageCodec.Ipv4RecordType,
            30,
            new byte[] { 192, 0, 2 });
        byte[] shortAddressResponse = DnsTestMessages.BuildResponse(
            query,
            0x8180,
            shortAddress);

        string longLabel = new string('a', 64) + ".example";

        Assert.Multiple(() =>
        {
            Assert.Throws<DiameterCodecException>(() =>
                DnsMessageCodec.ReadDnsResponseHeader(
                    pointerLoop,
                    1,
                    "example",
                    DnsMessageCodec.Ipv4RecordType));
            Assert.Throws<DiameterCodecException>(() =>
                DnsMessageCodec.ReadDnsResponseHeader(
                    excessiveCount,
                    11,
                    "example",
                    DnsMessageCodec.Ipv4RecordType));
            Assert.Throws<DiameterCodecException>(() =>
                DnsMessageCodec.DecodeDnsResponse(
                    shortAddressResponse,
                    11,
                    "example",
                    DnsMessageCodec.Ipv4RecordType,
                    DateTimeOffset.UtcNow));
            Assert.Throws<ArgumentException>(() =>
                DnsMessageCodec.EncodeDnsQuery(
                    longLabel,
                    DnsMessageCodec.Ipv4RecordType,
                    12));
        });
    }

    [Test]
    public void QueryResultDefaultsOwnTheirRecordCollections()
    {
        DiameterDnsQueryResult first = new DiameterDnsQueryResult();
        DiameterDnsQueryResult second = new DiameterDnsQueryResult();
        first.NaptrRecords.Add(new DiameterNaptrRecord());
        first.SrvRecords.Add(new DiameterSrvRecord());
        first.AddressRecords.Add(new DiameterAddressRecord());

        Assert.Multiple(() =>
        {
            Assert.That(second.QueryName, Is.Empty);
            Assert.That(second.RecordType, Is.Zero);
            Assert.That(second.ResponseCode, Is.Zero);
            Assert.That(second.Successful, Is.False);
            Assert.That(second.Authoritative, Is.False);
            Assert.That(second.RecursionAvailable, Is.False);
            Assert.That(second.UsedTcpFallback, Is.False);
            Assert.That(second.NegativeTimeToLiveSeconds, Is.Zero);
            Assert.That(second.NaptrRecords, Is.Empty);
            Assert.That(second.SrvRecords, Is.Empty);
            Assert.That(second.AddressRecords, Is.Empty);
            Assert.That(second.Code, Is.Empty);
            Assert.That(second.Message, Is.Empty);
            Assert.That(first.NaptrRecords, Is.Not.SameAs(second.NaptrRecords));
            Assert.That(first.SrvRecords, Is.Not.SameAs(second.SrvRecords));
            Assert.That(first.AddressRecords, Is.Not.SameAs(second.AddressRecords));
        });
    }
}
