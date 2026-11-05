using System.Text;
using tftp;

namespace tftp.Tests;

public class PacketCodecOptionTests
{
    [Test]
    public void EncodesOptionsInCallerOrderWithoutApplyingPolicy()
    {
        TftpPacket packet = CreateOptionAcknowledgement();
        packet.Options.Add(new TftpOption { Name = "TIMEOUT", Value = "not-a-number" });
        packet.Options.Add(new TftpOption { Name = "vendor-window", Value = "enabled" });
        TftpPacketCodec codec = new TftpPacketCodec();
        int length = codec.GetEncodedLength(packet);
        byte[] destination = Enumerable.Repeat((byte)170, length + 1).ToArray();

        int written = codec.Encode(packet, destination.AsSpan());

        byte[] expectedFields = Encoding.ASCII.GetBytes(
            "TIMEOUT\0not-a-number\0vendor-window\0enabled\0");
        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(length));
            Assert.That(destination[0], Is.Zero);
            Assert.That(destination[1], Is.EqualTo(6));
            Assert.That(destination.AsSpan(2, expectedFields.Length).ToArray(),
                Is.EqualTo(expectedFields));
            Assert.That(destination[written], Is.EqualTo(170));
        });
    }

    [Test]
    public void DecodesOptionsInWireOrderAndPreservesCasing()
    {
        byte[] source = CreateEncodedOptionAcknowledgement(
            "TSize\04096\0BLKSIZE\01024\0custom\0value\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpPacket packet = codec.Decode(source.AsSpan(), 0);
        byte[] roundTrip = new byte[codec.GetEncodedLength(packet)];
        int written = codec.Encode(packet, roundTrip.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(packet.Opcode, Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(packet.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "TSize", "BLKSIZE", "custom" }));
            Assert.That(packet.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "4096", "1024", "value" }));
            Assert.That(written, Is.EqualTo(source.Length));
            Assert.That(roundTrip, Is.EqualTo(source));
        });
    }

    [Test]
    public void RoundTripsMaximumLengthOptionAcknowledgement()
    {
        TftpPacket packet = CreateOptionAcknowledgement();
        packet.Options.Add(new TftpOption
        {
            Name = "x",
            Value = new string('v', TftpProtocolValues.MaximumRequestPacketLength - 5)
        });
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];

        int written = codec.Encode(packet, encoded.AsSpan());
        TftpPacket decoded = codec.Decode(encoded.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(TftpProtocolValues.MaximumRequestPacketLength));
            Assert.That(decoded.Options, Has.Count.EqualTo(1));
            Assert.That(decoded.Options[0].Name, Is.EqualTo("x"));
            Assert.That(decoded.Options[0].Value, Is.EqualTo(packet.Options[0].Value));
        });
    }

    [Test]
    public void RejectsOptionAcknowledgementAboveMaximumLength()
    {
        TftpPacket packet = CreateOptionAcknowledgement();
        packet.Options.Add(new TftpOption
        {
            Name = "x",
            Value = new string('v', TftpProtocolValues.MaximumRequestPacketLength - 4)
        });
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = CreateEncodedOptionAcknowledgement(
            "x\0" + new string('v', TftpProtocolValues.MaximumRequestPacketLength - 4) + "\0");

        TftpProtocolException encodeFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(packet));
        TftpProtocolException decodeFailure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(encoded.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Has.Length.EqualTo(TftpProtocolValues.MaximumRequestPacketLength + 1));
            Assert.That(encodeFailure.Message, Does.Contain("512-byte"));
            Assert.That(decodeFailure.Message, Does.Contain("512-byte"));
            Assert.That(decodeFailure.PacketOffset,
                Is.EqualTo(TftpProtocolValues.MaximumRequestPacketLength));
        });
    }

    [Test]
    public void RejectsEmptyAndNullOptionContainers()
    {
        TftpPacket empty = CreateOptionAcknowledgement();
        TftpPacket nullOptions = CreateOptionAcknowledgement();
        nullOptions.Options = null!;
        TftpPacket nullOption = CreateOptionAcknowledgement();
        nullOption.Options.Add(null!);
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException emptyFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(empty));
        TftpProtocolException nullOptionsFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(nullOptions));
        TftpProtocolException nullOptionFailure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(nullOption));

        Assert.Multiple(() =>
        {
            Assert.That(emptyFailure.Message, Does.Contain("at least one"));
            Assert.That(nullOptionsFailure.Message, Does.Contain("list cannot be null"));
            Assert.That(nullOptionFailure.Message, Does.Contain("option cannot be null"));
        });
    }

    [TestCaseSource(nameof(InvalidOptionsForEncoding))]
    public void RejectsInvalidOptionsForEncoding(TftpOption option, string messagePart)
    {
        TftpPacket packet = CreateOptionAcknowledgement();
        packet.Options.Add(option);
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(packet));

        Assert.That(failure.Message, Does.Contain(messagePart));
    }

    [Test]
    public void RejectsDuplicateOptionNamesCaseInsensitivelyForEncoding()
    {
        TftpPacket packet = CreateOptionAcknowledgement();
        packet.Options.Add(new TftpOption { Name = "blksize", Value = "512" });
        packet.Options.Add(new TftpOption { Name = "BLKSIZE", Value = "256" });
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.GetEncodedLength(packet));

        Assert.That(failure.Message, Does.Contain("duplicate"));
    }

    [TestCaseSource(nameof(MalformedOptionAcknowledgements))]
    public void RejectsMalformedOptionAcknowledgements(
        byte[] source,
        string messagePart,
        int failureOffset)
    {
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain(messagePart));
            Assert.That(failure.HasPacketOffset, Is.True);
            Assert.That(failure.PacketOffset, Is.EqualTo(failureOffset));
        });
    }

    [Test]
    public void RejectsDuplicateDecodedOptionNamesCaseInsensitively()
    {
        byte[] source = CreateEncodedOptionAcknowledgement(
            "timeout\01\0TIMEOUT\02\0");
        TftpPacketCodec codec = new TftpPacketCodec();

        TftpProtocolException failure = Assert.Throws<TftpProtocolException>(
            () => codec.Decode(source.AsSpan(), 0));

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("duplicate"));
            Assert.That(failure.PacketOffset, Is.EqualTo(12));
        });
    }

    [Test]
    public void RejectsSmallDestinationWithoutMutation()
    {
        TftpPacket packet = CreateOptionAcknowledgement();
        packet.Options.Add(new TftpOption { Name = "timeout", Value = "3" });
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

    private static TftpPacket CreateOptionAcknowledgement()
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        return packet;
    }

    private static byte[] CreateEncodedOptionAcknowledgement(string fields)
    {
        byte[] fieldBytes = Encoding.ASCII.GetBytes(fields);
        byte[] source = new byte[fieldBytes.Length + 2];
        source[1] = 6;
        fieldBytes.CopyTo(source, 2);
        return source;
    }

    private static IEnumerable<TestCaseData> InvalidOptionsForEncoding()
    {
        yield return new TestCaseData(new TftpOption { Name = "", Value = "1" }, "empty");
        yield return new TestCaseData(new TftpOption { Name = null!, Value = "1" }, "null");
        yield return new TestCaseData(new TftpOption { Name = "time\0out", Value = "1" }, "NUL");
        yield return new TestCaseData(new TftpOption { Name = "t\u00edmeout", Value = "1" }, "non-ASCII");
        yield return new TestCaseData(new TftpOption { Name = "timeout", Value = "" }, "empty");
        yield return new TestCaseData(new TftpOption { Name = "timeout", Value = null! }, "null");
        yield return new TestCaseData(new TftpOption { Name = "timeout", Value = "1\0x" }, "NUL");
        yield return new TestCaseData(new TftpOption { Name = "timeout", Value = "tr\u00eas" }, "non-ASCII");
    }

    private static IEnumerable<TestCaseData> MalformedOptionAcknowledgements()
    {
        yield return new TestCaseData(new byte[] { 0, 6 }, "at least one", 2);
        yield return new TestCaseData(CreateEncodedOptionAcknowledgement("timeout"), "terminator", 2);
        yield return new TestCaseData(CreateEncodedOptionAcknowledgement("timeout\0"), "without a value", 10);
        yield return new TestCaseData(CreateEncodedOptionAcknowledgement("timeout\03"), "terminator", 10);
        yield return new TestCaseData(CreateEncodedOptionAcknowledgement("\03\0"), "empty", 2);
        yield return new TestCaseData(CreateEncodedOptionAcknowledgement("timeout\0\0"), "empty", 10);
        yield return new TestCaseData(
            new byte[] { 0, 6, 116, 105, 109, 101, 128, 0, 51, 0 },
            "non-ASCII",
            6);
        yield return new TestCaseData(
            new byte[] { 0, 6, 116, 105, 109, 101, 0, 51, 128, 0 },
            "non-ASCII",
            8);
    }
}
