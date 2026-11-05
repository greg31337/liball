namespace sip.Tests;

public class SipDnsWireCodecTests
{
    [TestCase(SipDnsRecordKind.A, 1)]
    [TestCase(SipDnsRecordKind.Cname, 5)]
    [TestCase(SipDnsRecordKind.Aaaa, 28)]
    [TestCase(SipDnsRecordKind.Srv, 33)]
    [TestCase(SipDnsRecordKind.Naptr, 35)]
    public void CreatesCanonicalBoundedQueries(SipDnsRecordKind kind, int expectedType)
    {
        byte[] query = SipDnsWireCodec.CreateQuery("_SIP._UDP.Example.COM.", kind, 0x1234);

        Assert.Multiple(() =>
        {
            Assert.That(query.Take(12), Is.EqualTo(new byte[]
            {
                0x12, 0x34, 0x01, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            }));
            Assert.That(ReadQuestionName(query), Is.EqualTo("_sip._udp.example.com"));
            Assert.That((query[^4] << 8) | query[^3], Is.EqualTo(expectedType));
            Assert.That((query[^2] << 8) | query[^1], Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsInvalidQueryInputs()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<System.ArgumentException>(() => SipDnsWireCodec.CreateQuery(
                "bad..example", SipDnsRecordKind.A, 1));
            Assert.Throws<System.ArgumentException>(() => SipDnsWireCodec.CreateQuery(
                new string('a', 64) + ".example", SipDnsRecordKind.A, 1));
            Assert.Throws<System.ArgumentException>(() => SipDnsWireCodec.CreateQuery(
                "example.com", SipDnsRecordKind.Unknown, 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => SipDnsWireCodec.CreateQuery(
                "example.com", SipDnsRecordKind.A, 65536));
        });
    }

    [Test]
    public void ParsesNaptrSrvAddressAndCnameRecordsWithCompression()
    {
        List<byte> packet = StartResponse(0x4321, 0x8580, "EXAMPLE.com", 35, 2, 0, 3);
        AddRecord(packet, 35, 300, data =>
        {
            AddUInt16(data, 100);
            AddUInt16(data, 10);
            AddCharacterString(data, "s");
            AddCharacterString(data, "SIP+D2U");
            AddCharacterString(data, "");
            AddLabelAndQuestionPointer(data, "_sip");
        });
        AddRecord(packet, 5, uint.MaxValue, data =>
            AddLabelAndQuestionPointer(data, "alias"));
        AddRecord(packet, 33, 120, data =>
        {
            AddUInt16(data, 4);
            AddUInt16(data, 25);
            AddUInt16(data, 5060);
            AddLabelAndQuestionPointer(data, "server");
        });
        AddRecord(packet, 1, 60, data => data.AddRange(new byte[] { 192, 0, 2, 10 }));
        AddRecord(packet, 28, 90, data => data.AddRange(new byte[]
        {
            0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1
        }));

        SipDnsWireResult result = SipDnsWireCodec.ParseResponse(
            packet.ToArray(), 0x4321, "example.COM.", SipDnsRecordKind.Naptr, 5);

        SipDnsWireRecord naptr = result.Records.Single(record => record.Kind == SipDnsRecordKind.Naptr);
        SipDnsWireRecord cname = result.Records.Single(record => record.Kind == SipDnsRecordKind.Cname);
        SipDnsWireRecord srv = result.Records.Single(record => record.Kind == SipDnsRecordKind.Srv);
        SipDnsWireRecord ipv4 = result.Records.Single(record => record.Kind == SipDnsRecordKind.A);
        SipDnsWireRecord ipv6 = result.Records.Single(record => record.Kind == SipDnsRecordKind.Aaaa);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Authoritative, Is.True);
            Assert.That(result.RecursionAvailable, Is.True);
            Assert.That(result.ResponseCode, Is.Zero);
            Assert.That(result.Records, Has.Length.EqualTo(5));
            Assert.That(naptr.Name, Is.EqualTo("EXAMPLE.com"));
            Assert.That(naptr.NaptrOrder, Is.EqualTo(100));
            Assert.That(naptr.NaptrPreference, Is.EqualTo(10));
            Assert.That(naptr.NaptrFlags, Is.EqualTo("s"));
            Assert.That(naptr.NaptrServices, Is.EqualTo("SIP+D2U"));
            Assert.That(naptr.NaptrRegexp, Is.Empty);
            Assert.That(naptr.NaptrReplacement, Is.EqualTo("_sip.EXAMPLE.com"));
            Assert.That(cname.Target, Is.EqualTo("alias.EXAMPLE.com"));
            Assert.That(cname.TtlSeconds, Is.EqualTo(uint.MaxValue));
            Assert.That(srv.Priority, Is.EqualTo(4));
            Assert.That(srv.Weight, Is.EqualTo(25));
            Assert.That(srv.Port, Is.EqualTo(5060));
            Assert.That(srv.Target, Is.EqualTo("server.EXAMPLE.com"));
            Assert.That(srv.Section, Is.EqualTo(2));
            Assert.That(ipv4.Address, Is.EqualTo("192.0.2.10"));
            Assert.That(ipv6.Address, Is.EqualTo("2001:db8::1"));
        });
    }

    [Test]
    public void ReportsTruncationAndResponseCodesWithoutReadingMissingRecords()
    {
        List<byte> truncated = StartResponse(7, 0x8380, "example.com", 1, 4, 0, 0);
        List<byte> missing = StartResponse(8, 0x81a3, "missing.example", 1, 0, 0, 0);

        SipDnsWireResult truncatedResult = SipDnsWireCodec.ParseResponse(
            truncated.ToArray(), 7, "example.com", SipDnsRecordKind.A, 4);
        SipDnsWireResult missingResult = SipDnsWireCodec.ParseResponse(
            missing.ToArray(), 8, "missing.example", SipDnsRecordKind.A, 4);

        Assert.Multiple(() =>
        {
            Assert.That(truncatedResult.Success, Is.True, truncatedResult.ErrorMessage);
            Assert.That(truncatedResult.Truncated, Is.True);
            Assert.That(truncatedResult.Records, Is.Empty);
            Assert.That(missingResult.Success, Is.True, missingResult.ErrorMessage);
            Assert.That(missingResult.ResponseCode, Is.EqualTo(3));
            Assert.That(missingResult.Records, Is.Empty);
        });
    }

    [Test]
    public void RejectsTransactionAndQuestionMismatches()
    {
        byte[] packet = StartResponse(11, 0x8180, "example.com", 1, 0, 0, 0).ToArray();

        SipDnsWireResult transaction = SipDnsWireCodec.ParseResponse(
            packet, 12, "example.com", SipDnsRecordKind.A, 1);
        SipDnsWireResult name = SipDnsWireCodec.ParseResponse(
            packet, 11, "other.example", SipDnsRecordKind.A, 1);
        SipDnsWireResult type = SipDnsWireCodec.ParseResponse(
            packet, 11, "example.com", SipDnsRecordKind.Aaaa, 1);

        Assert.Multiple(() =>
        {
            Assert.That(transaction.Success, Is.False);
            Assert.That(transaction.Unrelated, Is.True);
            Assert.That(name.Success, Is.False);
            Assert.That(name.Unrelated, Is.True);
            Assert.That(type.Success, Is.False);
            Assert.That(type.Unrelated, Is.True);
        });
    }

    [Test]
    public void RejectsForwardAndSelfReferentialCompressionPointers()
    {
        List<byte> packet = StartResponse(15, 0x8180, "example.com", 1, 1, 0, 0);
        int recordOffset = packet.Count;
        AddRecord(packet, 1, 60, data => data.AddRange(new byte[] { 192, 0, 2, 1 }));
        byte[] selfPointer = packet.ToArray();
        selfPointer[recordOffset] = (byte)(0xc0 | recordOffset >> 8);
        selfPointer[recordOffset + 1] = (byte)recordOffset;
        byte[] forwardPointer = packet.ToArray();
        int forward = recordOffset + 2;
        forwardPointer[recordOffset] = (byte)(0xc0 | forward >> 8);
        forwardPointer[recordOffset + 1] = (byte)forward;

        SipDnsWireResult self = SipDnsWireCodec.ParseResponse(
            selfPointer, 15, "example.com", SipDnsRecordKind.A, 1);
        SipDnsWireResult forwardResult = SipDnsWireCodec.ParseResponse(
            forwardPointer, 15, "example.com", SipDnsRecordKind.A, 1);

        Assert.Multiple(() =>
        {
            Assert.That(self.Success, Is.False);
            Assert.That(self.ErrorMessage, Does.Contain("point backward"));
            Assert.That(forwardResult.Success, Is.False);
            Assert.That(forwardResult.ErrorMessage, Does.Contain("point backward"));
        });
    }

    [Test]
    public void EnforcesRecordAndRdataBounds()
    {
        byte[] tooMany = StartResponse(20, 0x8180, "example.com", 1, 2, 0, 0).ToArray();
        List<byte> malformed = StartResponse(21, 0x8180, "example.com", 1, 1, 0, 0);
        AddPointer(malformed, 12);
        AddUInt16(malformed, 1);
        AddUInt16(malformed, 1);
        AddUInt32(malformed, 60);
        AddUInt16(malformed, 4);
        malformed.AddRange(new byte[] { 192, 0 });

        SipDnsWireResult limited = SipDnsWireCodec.ParseResponse(
            tooMany, 20, "example.com", SipDnsRecordKind.A, 1);
        SipDnsWireResult truncated = SipDnsWireCodec.ParseResponse(
            malformed.ToArray(), 21, "example.com", SipDnsRecordKind.A, 1);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.ErrorMessage, Does.Contain("record limit"));
            Assert.That(truncated.Success, Is.False);
            Assert.That(truncated.ErrorMessage, Does.Contain("truncated"));
        });
    }

    [Test]
    public void RejectsPointersIntoHeadersReservedLabelsAndTrailingBytes()
    {
        List<byte> headerPointer = StartResponse(
            30, 0x8180, "example.com", 1, 1, 0, 0);
        AddPointer(headerPointer, 0);
        AddUInt16(headerPointer, 1);
        AddUInt16(headerPointer, 1);
        AddUInt32(headerPointer, 60);
        AddUInt16(headerPointer, 4);
        headerPointer.AddRange(new byte[] { 192, 0, 2, 1 });

        List<byte> reservedLabel = StartResponse(
            31, 0x8180, "example.com", 1, 1, 0, 0);
        reservedLabel.Add(0x40);
        reservedLabel.Add(0);
        AddUInt16(reservedLabel, 1);
        AddUInt16(reservedLabel, 1);
        AddUInt32(reservedLabel, 60);
        AddUInt16(reservedLabel, 4);
        reservedLabel.AddRange(new byte[] { 192, 0, 2, 1 });

        List<byte> trailing = StartResponse(
            32, 0x8180, "example.com", 1, 0, 0, 0);
        trailing.Add(0);

        SipDnsWireResult header = SipDnsWireCodec.ParseResponse(
            headerPointer.ToArray(), 30, "example.com", SipDnsRecordKind.A, 1);
        SipDnsWireResult label = SipDnsWireCodec.ParseResponse(
            reservedLabel.ToArray(), 31, "example.com", SipDnsRecordKind.A, 1);
        SipDnsWireResult extra = SipDnsWireCodec.ParseResponse(
            trailing.ToArray(), 32, "example.com", SipDnsRecordKind.A, 1);

        Assert.Multiple(() =>
        {
            Assert.That(header.Success, Is.False);
            Assert.That(header.ErrorMessage, Does.Contain("name area"));
            Assert.That(label.Success, Is.False);
            Assert.That(label.ErrorMessage, Does.Contain("invalid length"));
            Assert.That(extra.Success, Is.False);
            Assert.That(extra.ErrorMessage, Does.Contain("trailing bytes"));
        });
    }

    [Test]
    public void BoundsDeepBackwardCompressionPointerGraphs()
    {
        byte[] acceptedPacket = BuildPointerChainResponse(40, 128);
        byte[] rejectedPacket = BuildPointerChainResponse(41, 129);

        SipDnsWireResult accepted = SipDnsWireCodec.ParseResponse(
            acceptedPacket, 40, "example.com", SipDnsRecordKind.A, 128);
        SipDnsWireResult rejected = SipDnsWireCodec.ParseResponse(
            rejectedPacket, 41, "example.com", SipDnsRecordKind.A, 129);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Success, Is.True, accepted.ErrorMessage);
            Assert.That(accepted.Records.Single().Address,
                Is.EqualTo("192.0.2.128"));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorMessage,
                Does.Contain("too many compression pointers"));
        });
    }

    [Test]
    public void RejectsPacketsBeyondTheDnsWireSizeLimit()
    {
        byte[] packet = new byte[65536];

        SipDnsWireResult result = SipDnsWireCodec.ParseResponse(
            packet, 0, "example.com", SipDnsRecordKind.A, 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorOffset, Is.EqualTo(65535));
            Assert.That(result.ErrorMessage, Does.Contain("wire message limit"));
        });
    }

    static byte[] BuildPointerChainResponse(int id, int pointerCount)
    {
        List<byte> packet = StartResponse(
            id, 0x8180, "example.com", 1, pointerCount, 0, 0);
        int pointerTarget = 12;
        int index = 0;
        while (index < pointerCount)
        {
            int ownerOffset = packet.Count;
            AddPointer(packet, pointerTarget);
            bool final = index == pointerCount - 1;
            AddUInt16(packet, final ? 1 : 99);
            AddUInt16(packet, 1);
            AddUInt32(packet, 60);
            AddUInt16(packet, final ? 4 : 0);
            if (final)
            {
                packet.AddRange(new byte[] { 192, 0, 2, (byte)pointerCount });
            }
            pointerTarget = ownerOffset;
            index++;
        }
        return packet.ToArray();
    }

    static List<byte> StartResponse(int id, int flags, string questionName,
        int questionType, int answers, int authorities, int additional)
    {
        List<byte> packet = new List<byte>();
        AddUInt16(packet, id);
        AddUInt16(packet, flags);
        AddUInt16(packet, 1);
        AddUInt16(packet, answers);
        AddUInt16(packet, authorities);
        AddUInt16(packet, additional);
        AddName(packet, questionName);
        AddUInt16(packet, questionType);
        AddUInt16(packet, 1);
        return packet;
    }

    static void AddRecord(List<byte> packet, int type, uint ttl,
        System.Action<List<byte>> writeData)
    {
        AddPointer(packet, 12);
        AddUInt16(packet, type);
        AddUInt16(packet, 1);
        AddUInt32(packet, ttl);
        List<byte> data = new List<byte>();
        writeData(data);
        AddUInt16(packet, data.Count);
        packet.AddRange(data);
    }

    static void AddName(List<byte> bytes, string name)
    {
        foreach (string label in name.TrimEnd('.').Split('.'))
        {
            byte[] text = System.Text.Encoding.ASCII.GetBytes(label);
            bytes.Add((byte)text.Length);
            bytes.AddRange(text);
        }
        bytes.Add(0);
    }

    static void AddLabelAndQuestionPointer(List<byte> bytes, string label)
    {
        byte[] text = System.Text.Encoding.ASCII.GetBytes(label);
        bytes.Add((byte)text.Length);
        bytes.AddRange(text);
        AddPointer(bytes, 12);
    }

    static void AddCharacterString(List<byte> bytes, string text)
    {
        byte[] encoded = System.Text.Encoding.ASCII.GetBytes(text);
        bytes.Add((byte)encoded.Length);
        bytes.AddRange(encoded);
    }

    static void AddPointer(List<byte> bytes, int offset)
    {
        bytes.Add((byte)(0xc0 | offset >> 8));
        bytes.Add((byte)offset);
    }

    static void AddUInt16(List<byte> bytes, int value)
    {
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static void AddUInt32(List<byte> bytes, uint value)
    {
        bytes.Add((byte)(value >> 24));
        bytes.Add((byte)(value >> 16));
        bytes.Add((byte)(value >> 8));
        bytes.Add((byte)value);
    }

    static string ReadQuestionName(byte[] query)
    {
        List<string> labels = new List<string>();
        int offset = 12;
        while (query[offset] != 0)
        {
            int length = query[offset];
            offset++;
            labels.Add(System.Text.Encoding.ASCII.GetString(query, offset, length));
            offset += length;
        }
        return string.Join('.', labels);
    }
}
