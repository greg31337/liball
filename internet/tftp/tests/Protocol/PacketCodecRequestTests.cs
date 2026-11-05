using System.Text;
using tftp;

namespace tftp.Tests;

public class PacketCodecRequestTests
{
    [Test]
    public void EncodesReadRequestAndReportsItsLength()
    {
        TftpPacket packet = CreateRequest(TftpOpcode.ReadRequest, "file.txt", "octet");
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] destination = Enumerable.Repeat((byte)170, 32).ToArray();

        int encodedLength = codec.GetEncodedLength(packet);
        int written = codec.Encode(packet, destination.AsSpan());

        byte[] expected = new byte[]
        {
            0, 1,
            102, 105, 108, 101, 46, 116, 120, 116, 0,
            111, 99, 116, 101, 116, 0
        };
        Assert.Multiple(() =>
        {
            Assert.That(encodedLength, Is.EqualTo(expected.Length));
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(destination.AsSpan(0, written).ToArray(), Is.EqualTo(expected));
            Assert.That(destination[written], Is.EqualTo(170));
        });
    }

    [Test]
    public void EncodesWriteRequestOptionsInCallerOrder()
    {
        TftpPacket packet = CreateRequest(TftpOpcode.WriteRequest, "dir/image.bin", "OcTeT");
        packet.Options.Add(new TftpOption { Name = "timeout", Value = "3" });
        packet.Options.Add(new TftpOption { Name = "BLKSIZE", Value = "1024" });
        packet.Options.Add(new TftpOption { Name = "tsize", Value = "4096" });
        TftpPacketCodec codec = new TftpPacketCodec();
        int length = codec.GetEncodedLength(packet);
        byte[] destination = new byte[length];

        int written = codec.Encode(packet, destination.AsSpan());

        byte[] expectedFields = Encoding.ASCII.GetBytes(
            "dir/image.bin\0OcTeT\0timeout\0" +
            "3\0BLKSIZE\0" +
            "1024\0tsize\0" +
            "4096\0");
        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(length));
            Assert.That(destination[0], Is.Zero);
            Assert.That(destination[1], Is.EqualTo(2));
            Assert.That(destination.AsSpan(2).ToArray(), Is.EqualTo(expectedFields));
        });
    }

    [TestCase("netascii")]
    [TestCase("NETASCII")]
    [TestCase("octet")]
    [TestCase("OcTeT")]
    [TestCase("mail")]
    [TestCase("MAIL")]
    public void AcceptsProtocolModesCaseInsensitively(string mode)
    {
        TftpPacket packet = CreateRequest(TftpOpcode.WriteRequest, "target", mode);
        TftpPacketCodec codec = new TftpPacketCodec();

        int length = codec.GetEncodedLength(packet);
        byte[] destination = new byte[length];

        Assert.That(codec.Encode(packet, destination.AsSpan()), Is.EqualTo(length));
        Assert.That(Encoding.ASCII.GetString(destination), Does.EndWith(mode + "\0"));
    }

    [Test]
    public void AcceptsRequestAtMaximumPacketLength()
    {
        TftpPacket packet = CreateRequest(
            TftpOpcode.ReadRequest,
            new string('f', 503),
            TftpProtocolValues.OctetMode);
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] destination = new byte[TftpProtocolValues.MaximumRequestPacketLength];

        int length = codec.GetEncodedLength(packet);
        int written = codec.Encode(packet, destination.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(length, Is.EqualTo(TftpProtocolValues.MaximumRequestPacketLength));
            Assert.That(written, Is.EqualTo(TftpProtocolValues.MaximumRequestPacketLength));
            Assert.That(destination[0], Is.Zero);
            Assert.That(destination[1], Is.EqualTo(1));
            Assert.That(destination[505], Is.Zero);
            Assert.That(destination[511], Is.Zero);
        });
    }

    [Test]
    public void RejectsRequestOverMaximumPacketLength()
    {
        TftpPacket packet = CreateRequest(
            TftpOpcode.ReadRequest,
            new string('f', 504),
            TftpProtocolValues.OctetMode);
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(packet));

        Assert.That(failure.Message, Does.Contain("512-byte"));
    }

    [Test]
    public void RejectsDuplicateOptionNamesCaseInsensitively()
    {
        TftpPacket packet = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        packet.Options.Add(new TftpOption { Name = "blksize", Value = "512" });
        packet.Options.Add(new TftpOption { Name = "BLKSIZE", Value = "1024" });
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(packet));

        Assert.That(failure.Message, Does.Contain("duplicate"));
    }

    [TestCaseSource(nameof(InvalidRequestFields))]
    public void RejectsInvalidRequiredRequestFields(TftpPacket packet, string messagePart)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(packet));

        Assert.That(failure.Message, Does.Contain(messagePart));
    }

    [Test]
    public void RejectsInvalidRequestAndOptionContainers()
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        TftpPacket invalidOpcode = CreateRequest((TftpOpcode)99, "file", "octet");
        TftpPacket nullOptions = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nullOptions.Options = null!;
        TftpPacket nullOption = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nullOption.Options.Add(null!);

        TftpProtocolException nullPacketFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(null!));
        TftpProtocolException opcodeFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(invalidOpcode));
        TftpProtocolException optionsFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(nullOptions));
        TftpProtocolException optionFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(nullOption));

        Assert.Multiple(() =>
        {
            Assert.That(nullPacketFailure.Message, Does.Contain("packet cannot be null"));
            Assert.That(opcodeFailure.Message, Does.Contain("opcode"));
            Assert.That(optionsFailure.Message, Does.Contain("option list cannot be null"));
            Assert.That(optionFailure.Message, Does.Contain("option cannot be null"));
        });
    }

    [Test]
    public void RejectsSmallDestinationWithoutMutation()
    {
        TftpPacket packet = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        TftpPacketCodec codec = new TftpPacketCodec();
        int length = codec.GetEncodedLength(packet);
        byte[] destination = Enumerable.Repeat((byte)170, length - 1).ToArray();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Encode(packet, destination.AsSpan()));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("too small"));
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    [TestCase(TftpOpcode.ReadRequest)]
    [TestCase(TftpOpcode.WriteRequest)]
    public void DecodesRequestsWithoutOptions(TftpOpcode opcode)
    {
        byte[] source = CreateEncodedRequest(opcode, "folder/file.txt\0NeTaScIi\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacket packet = codec.Decode(source.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(packet.Opcode, Is.EqualTo(opcode));
            Assert.That(packet.Filename, Is.EqualTo("folder/file.txt"));
            Assert.That(packet.Mode, Is.EqualTo("NeTaScIi"));
            Assert.That(packet.Options, Is.Empty);
        });
    }

    [Test]
    public void DecodesOptionsInWireOrderAndPreservesCasing()
    {
        byte[] source = CreateEncodedRequest(
            TftpOpcode.WriteRequest,
            "upload.bin\0OCTET\0TIMEOUT\0" +
            "3\0blksize\0" +
            "1024\0TSize\0" +
            "4096\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacket packet = codec.Decode(source.AsSpan(), 0);
        byte[] roundTrip = new byte[codec.GetEncodedLength(packet)];
        int written = codec.Encode(packet, roundTrip.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(packet.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "TIMEOUT", "blksize", "TSize" }));
            Assert.That(packet.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "3", "1024", "4096" }));
            Assert.That(written, Is.EqualTo(source.Length));
            Assert.That(roundTrip, Is.EqualTo(source));
        });
    }

    [TestCase("netascii")]
    [TestCase("NETASCII")]
    [TestCase("octet")]
    [TestCase("OCTET")]
    [TestCase("mail")]
    [TestCase("MAIL")]
    public void DecoderAcceptsProtocolModesCaseInsensitively(string mode)
    {
        byte[] source = CreateEncodedRequest(
            TftpOpcode.WriteRequest,
            "recipient\0" + mode + "\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacket packet = codec.Decode(source.AsSpan(), 0);

        Assert.That(packet.Mode, Is.EqualTo(mode));
    }

    [Test]
    public void DecodesRequestAtMaximumPacketLength()
    {
        byte[] source = CreateEncodedRequest(
            TftpOpcode.ReadRequest,
            new string('f', 503) + "\0octet\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacket packet = codec.Decode(source.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(source, Has.Length.EqualTo(TftpProtocolValues.MaximumRequestPacketLength));
            Assert.That(packet.Filename, Has.Length.EqualTo(503));
            Assert.That(packet.Mode, Is.EqualTo(TftpProtocolValues.OctetMode));
        });
    }

    [Test]
    public void RejectsDecodedRequestOverMaximumPacketLength()
    {
        byte[] source = CreateEncodedRequest(
            TftpOpcode.ReadRequest,
            new string('f', 504) + "\0octet\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("512-byte"));
            Assert.That(failure.HasPacketOffset, Is.True);
            Assert.That(failure.PacketOffset,
                Is.EqualTo(TftpProtocolValues.MaximumRequestPacketLength));
        });
    }

    [Test]
    public void RejectsOddAndDuplicateDecodedOptions()
    {
        byte[] odd = CreateEncodedRequest(
            TftpOpcode.ReadRequest,
            "file\0octet\0timeout\0");
        byte[] duplicate = CreateEncodedRequest(
            TftpOpcode.ReadRequest,
            "file\0octet\0blksize\0" +
            "512\0BLKSIZE\0" +
            "1024\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException oddFailure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(odd.AsSpan(), 0));
        TftpProtocolException duplicateFailure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(duplicate.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(oddFailure.Message, Does.Contain("without a value"));
            Assert.That(oddFailure.PacketOffset, Is.EqualTo(odd.Length));
            Assert.That(duplicateFailure.Message, Does.Contain("duplicate"));
            Assert.That(duplicateFailure.HasPacketOffset, Is.True);
        });
    }

    [TestCaseSource(nameof(InvalidEncodedRequestFields))]
    public void RejectsInvalidDecodedRequestFields(byte[] source, string messagePart)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain(messagePart));
            Assert.That(failure.HasPacketOffset, Is.True);
        });
    }

    private static IEnumerable<TestCaseData> InvalidRequestFields()
    {
        TftpPacket emptyFilename = CreateRequest(TftpOpcode.ReadRequest, "", "octet");
        yield return new TestCaseData(emptyFilename, "empty");

        TftpPacket nullFilename = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nullFilename.Filename = null!;
        yield return new TestCaseData(nullFilename, "null");

        TftpPacket nulFilename = CreateRequest(TftpOpcode.ReadRequest, "a\0b", "octet");
        yield return new TestCaseData(nulFilename, "NUL");

        TftpPacket nonAsciiFilename = CreateRequest(TftpOpcode.ReadRequest, "café", "octet");
        yield return new TestCaseData(nonAsciiFilename, "non-ASCII");

        TftpPacket emptyMode = CreateRequest(TftpOpcode.ReadRequest, "file", "");
        yield return new TestCaseData(emptyMode, "empty");

        TftpPacket nullMode = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nullMode.Mode = null!;
        yield return new TestCaseData(nullMode, "null");

        TftpPacket unknownMode = CreateRequest(TftpOpcode.ReadRequest, "file", "binary");
        yield return new TestCaseData(unknownMode, "not recognized");

        TftpPacket nulMode = CreateRequest(TftpOpcode.ReadRequest, "file", "oc\0tet");
        yield return new TestCaseData(nulMode, "NUL");

        TftpPacket nonAsciiMode = CreateRequest(TftpOpcode.ReadRequest, "file", "octét");
        yield return new TestCaseData(nonAsciiMode, "non-ASCII");

        TftpPacket emptyOptionName = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        emptyOptionName.Options.Add(new TftpOption { Name = "", Value = "1" });
        yield return new TestCaseData(emptyOptionName, "empty");

        TftpPacket nullOptionName = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nullOptionName.Options.Add(new TftpOption { Name = null!, Value = "1" });
        yield return new TestCaseData(nullOptionName, "null");

        TftpPacket nulOptionName = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nulOptionName.Options.Add(new TftpOption { Name = "time\0out", Value = "1" });
        yield return new TestCaseData(nulOptionName, "NUL");

        TftpPacket emptyOptionValue = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        emptyOptionValue.Options.Add(new TftpOption { Name = "timeout", Value = "" });
        yield return new TestCaseData(emptyOptionValue, "empty");

        TftpPacket nullOptionValue = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nullOptionValue.Options.Add(new TftpOption { Name = "timeout", Value = null! });
        yield return new TestCaseData(nullOptionValue, "null");

        TftpPacket nulOptionValue = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nulOptionValue.Options.Add(new TftpOption { Name = "timeout", Value = "1\0x" });
        yield return new TestCaseData(nulOptionValue, "NUL");

        TftpPacket nonAsciiOptionName = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nonAsciiOptionName.Options.Add(new TftpOption { Name = "tímeout", Value = "1" });
        yield return new TestCaseData(nonAsciiOptionName, "non-ASCII");

        TftpPacket nonAsciiOptionValue = CreateRequest(TftpOpcode.ReadRequest, "file", "octet");
        nonAsciiOptionValue.Options.Add(new TftpOption { Name = "timeout", Value = "três" });
        yield return new TestCaseData(nonAsciiOptionValue, "non-ASCII");
    }

    private static IEnumerable<TestCaseData> InvalidEncodedRequestFields()
    {
        yield return new TestCaseData(
            CreateEncodedRequest(TftpOpcode.ReadRequest, "filename"),
            "terminator");
        yield return new TestCaseData(
            CreateEncodedRequest(TftpOpcode.ReadRequest, "\0octet\0"),
            "empty");
        yield return new TestCaseData(
            CreateEncodedRequest(TftpOpcode.ReadRequest, "file\0octet"),
            "terminator");
        yield return new TestCaseData(
            CreateEncodedRequest(TftpOpcode.ReadRequest, "file\0\0"),
            "empty");
        yield return new TestCaseData(
            CreateEncodedRequest(TftpOpcode.ReadRequest, "file\0binary\0"),
            "not recognized");
        yield return new TestCaseData(
            CreateEncodedRequest(TftpOpcode.ReadRequest, "file\0octet\0\0value\0"),
            "empty");
        yield return new TestCaseData(
            CreateEncodedRequest(TftpOpcode.ReadRequest, "file\0octet\0timeout\0\0"),
            "empty");
        yield return new TestCaseData(
            CreateEncodedRequest(
                TftpOpcode.ReadRequest,
                "file\0octet\0timeout\0" +
                "1"),
            "terminator");

        byte[] nonAsciiFilename = CreateEncodedRequest(
            TftpOpcode.ReadRequest,
            "file\0octet\0");
        nonAsciiFilename[2] = 128;
        yield return new TestCaseData(nonAsciiFilename, "non-ASCII");

        byte[] nonAsciiMode = CreateEncodedRequest(
            TftpOpcode.ReadRequest,
            "file\0octet\0");
        nonAsciiMode[7] = 128;
        yield return new TestCaseData(nonAsciiMode, "non-ASCII");

        byte[] nonAsciiOption = CreateEncodedRequest(
            TftpOpcode.ReadRequest,
            "file\0octet\0timeout\0" +
            "1\0");
        nonAsciiOption[13] = 128;
        yield return new TestCaseData(nonAsciiOption, "non-ASCII");

        byte[] nonAsciiOptionValue = CreateEncodedRequest(
            TftpOpcode.ReadRequest,
            "file\0octet\0timeout\0" +
            "1\0");
        nonAsciiOptionValue[21] = 128;
        yield return new TestCaseData(nonAsciiOptionValue, "non-ASCII");
    }

    private static TftpPacket CreateRequest(TftpOpcode opcode, string filename, string mode)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = opcode;
        packet.Filename = filename;
        packet.Mode = mode;
        return packet;
    }

    private static byte[] CreateEncodedRequest(TftpOpcode opcode, string fields)
    {
        byte[] fieldBytes = Encoding.ASCII.GetBytes(fields);
        byte[] source = new byte[TftpProtocolValues.OpcodeLength + fieldBytes.Length];
        source[0] = 0;
        source[1] = (byte)opcode;
        fieldBytes.CopyTo(source, TftpProtocolValues.OpcodeLength);
        return source;
    }
}
