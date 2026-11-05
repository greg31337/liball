namespace dns.Tests.Internal.Wire;

public class WirePrimitiveExhaustiveTests
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(6)]
    public void EveryIntegerWidthRoundTripsBothBoundariesInAnExactRegion(
        int width)
    {
        ulong maximum = Maximum(width);
        ulong[] values = [0, maximum];

        foreach (ulong value in values)
        {
            byte[] destination = FilledBytes(width + 2, 0xa5);
            WireWriter writer = new WireWriter(destination, 1, width);

            WireWriteResult written = WriteUnsigned(writer, width, value);
            WireReader reader = new WireReader(destination, 1, width);
            WireReadResult read = ReadUnsigned(reader, width);

            Assert.Multiple(() =>
            {
                Assert.That(written.Success, Is.True,
                    $"write width {width}, value {value}");
                Assert.That(written.BytesWritten, Is.EqualTo(width));
                Assert.That(written.StartOffset, Is.EqualTo(1));
                Assert.That(written.NextOffset, Is.EqualTo(width + 1));
                Assert.That(writer.WrittenLength(), Is.EqualTo(width));
                Assert.That(writer.RemainingLength(), Is.Zero);
                Assert.That(writer.CopyWrittenBytes(), Has.Length.EqualTo(width));
                Assert.That(destination[0], Is.EqualTo(0xa5));
                Assert.That(destination[width + 1], Is.EqualTo(0xa5));
                Assert.That(read.Success, Is.True,
                    $"read width {width}, value {value}");
                Assert.That(read.UnsignedValue, Is.EqualTo(value));
                Assert.That(read.StartOffset, Is.EqualTo(1));
                Assert.That(read.NextOffset, Is.EqualTo(width + 1));
                Assert.That(reader.ConsumedLength(), Is.EqualTo(width));
                Assert.That(reader.RemainingLength(), Is.Zero);
            });
        }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(6)]
    public void EveryTruncatedIntegerLengthFailsWithoutConsumption(int width)
    {
        int available = 0;
        while (available < width)
        {
            byte[] source = FilledBytes(width + 2, 0xff);
            WireReader reader = new WireReader(source, 1, available);

            WireReadResult result = ReadUnsigned(reader, width);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False,
                    $"width {width}, available {available}");
                Assert.That(result.StartOffset, Is.EqualTo(1));
                Assert.That(result.NextOffset, Is.EqualTo(1));
                Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
                Assert.That(result.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
                Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(1));
                Assert.That(result.Diagnostics[0].Field,
                    Is.EqualTo(IntegerField(width)));
                Assert.That(reader.ReadOffset(), Is.EqualTo(1));
                Assert.That(reader.ConsumedLength(), Is.Zero);
                Assert.That(reader.RemainingLength(), Is.EqualTo(available));
            });

            available = available + 1;
        }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(6)]
    public void EveryUndersizedIntegerDestinationFailsAtomically(int width)
    {
        int capacity = 0;
        while (capacity < width)
        {
            byte[] destination = FilledBytes(width + 2, 0xa5);
            WireWriter buffered = new WireWriter(destination, 1, capacity);
            WireWriter counting = new WireWriter(null, 0, capacity);

            WireWriteResult bufferedResult =
                WriteUnsigned(buffered, width, Maximum(width));
            WireWriteResult countingResult =
                WriteUnsigned(counting, width, Maximum(width));

            Assert.Multiple(() =>
            {
                Assert.That(bufferedResult.Success, Is.False,
                    $"buffered width {width}, capacity {capacity}");
                Assert.That(countingResult.Success, Is.False,
                    $"counting width {width}, capacity {capacity}");
                Assert.That(bufferedResult.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
                Assert.That(countingResult.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
                Assert.That(bufferedResult.Diagnostics[0].Field,
                    Is.EqualTo(IntegerField(width)));
                Assert.That(buffered.WriteOffset(), Is.EqualTo(1));
                Assert.That(buffered.WrittenLength(), Is.Zero);
                Assert.That(buffered.RemainingLength(), Is.EqualTo(capacity));
                Assert.That(counting.WrittenLength(), Is.Zero);
                Assert.That(counting.RemainingLength(), Is.EqualTo(capacity));
                Assert.That(destination, Is.All.EqualTo(0xa5));
            });

            capacity = capacity + 1;
        }
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(6)]
    public void EveryIntegerWidthRejectsItsFirstOverflowValue(int width)
    {
        byte[] destination = FilledBytes(width + 2, 0xa5);
        WireWriter writer = new WireWriter(destination, 1, width);

        WireWriteResult result =
            WriteUnsigned(writer, width, Maximum(width) + 1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.StartOffset, Is.EqualTo(1));
            Assert.That(result.NextOffset, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ValueOutOfRange));
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo(IntegerField(width)));
            Assert.That(result.Diagnostics[0].Offset, Is.EqualTo(1));
            Assert.That(writer.WrittenLength(), Is.Zero);
            Assert.That(destination, Is.All.EqualTo(0xa5));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(255)]
    public void ByteArraysHonorExactAndOneShortRegions(int length)
    {
        byte[] payload = SequentialBytes(length);
        byte[] destination = FilledBytes(length + 2, 0xa5);
        WireWriter writer = new WireWriter(destination, 1, length);

        WireWriteResult written = writer.WriteBytes(payload);
        WireReader reader = new WireReader(destination, 1, length);
        WireReadResult read = reader.ReadBytes(length);
        WireReadResult pastEnd = reader.ReadBytes(1);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.BytesWritten, Is.EqualTo(length));
            Assert.That(read.Success, Is.True);
            Assert.That(read.Bytes, Is.EqualTo(payload));
            Assert.That(reader.ConsumedLength(), Is.EqualTo(length));
            Assert.That(pastEnd.Success, Is.False);
            Assert.That(reader.ConsumedLength(), Is.EqualTo(length));
            Assert.That(destination[0], Is.EqualTo(0xa5));
            Assert.That(destination[length + 1], Is.EqualTo(0xa5));
        });

        if (length > 0)
        {
            byte[] shortDestination = FilledBytes(length, 0xa5);
            WireWriter shortWriter =
                new WireWriter(shortDestination, 0, length - 1);
            WireWriteResult shortWrite = shortWriter.WriteBytes(payload);
            WireReader shortReader = new WireReader(payload, 0, length - 1);
            WireReadResult shortRead = shortReader.ReadBytes(length);

            Assert.Multiple(() =>
            {
                Assert.That(shortWrite.Success, Is.False);
                Assert.That(shortWrite.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
                Assert.That(shortWriter.WrittenLength(), Is.Zero);
                Assert.That(shortDestination, Is.All.EqualTo(0xa5));
                Assert.That(shortRead.Success, Is.False);
                Assert.That(shortRead.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
                Assert.That(shortReader.ConsumedLength(), Is.Zero);
            });
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(255)]
    public void CharacterStringsHonorPayloadAndBufferBoundaries(int length)
    {
        byte[] payload = SequentialBytes(length);
        int encodedLength = length + 1;
        byte[] destination = FilledBytes(encodedLength + 2, 0xa5);
        WireWriter writer = new WireWriter(destination, 1, encodedLength);

        WireWriteResult written = writer.WriteCharacterString(payload);
        WireReader reader = new WireReader(destination, 1, encodedLength);
        WireReadResult read = reader.ReadCharacterString();

        byte[] shortDestination = FilledBytes(encodedLength + 1, 0xa5);
        WireWriter shortWriter =
            new WireWriter(shortDestination, 1, encodedLength - 1);
        WireWriteResult shortWrite = shortWriter.WriteCharacterString(payload);
        WireReader shortReader =
            new WireReader(destination, 1, encodedLength - 1);
        WireReadResult shortRead = shortReader.ReadCharacterString();

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.BytesWritten, Is.EqualTo(encodedLength));
            Assert.That(read.Success, Is.True);
            Assert.That(read.UnsignedValue, Is.EqualTo((ulong)length));
            Assert.That(read.Bytes, Is.EqualTo(payload));
            Assert.That(reader.ConsumedLength(), Is.EqualTo(encodedLength));
            Assert.That(shortWrite.Success, Is.False);
            Assert.That(shortWrite.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
            Assert.That(shortWriter.WrittenLength(), Is.Zero);
            Assert.That(shortDestination, Is.All.EqualTo(0xa5));
            Assert.That(shortRead.Success, Is.False);
            Assert.That(shortRead.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
            Assert.That(shortReader.ConsumedLength(), Is.Zero);
        });
    }

    [TestCase(4)]
    [TestCase(16)]
    public void AddressesRejectEveryTruncatedInputAndDestination(int width)
    {
        byte[] address = SequentialBytes(width);
        int available = 0;
        while (available < width)
        {
            WireReader reader = new WireReader(address, 0, available);
            WireReadResult read = ReadAddress(reader, width);
            byte[] destination = FilledBytes(width, 0xa5);
            WireWriter writer = new WireWriter(destination, 0, available);
            WireWriteResult written = WriteAddress(writer, width, address);

            Assert.Multiple(() =>
            {
                Assert.That(read.Success, Is.False,
                    $"read width {width}, available {available}");
                Assert.That(read.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.UnexpectedEnd));
                Assert.That(reader.ConsumedLength(), Is.Zero);
                Assert.That(written.Success, Is.False,
                    $"write width {width}, capacity {available}");
                Assert.That(written.Diagnostics[0].Code,
                    Is.EqualTo(DnsDiagnosticCode.ResourceLimitExceeded));
                Assert.That(writer.WrittenLength(), Is.Zero);
                Assert.That(destination, Is.All.EqualTo(0xa5));
            });

            available = available + 1;
        }
    }

    [Test]
    public void PrimitiveRegionPredicatesHandleZeroExactNegativeAndExcessiveCounts()
    {
        WireReader reader = new WireReader([1, 2, 3], 1, 2);
        WireWriter writer = new WireWriter(new byte[3], 1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(reader.CanRead(0), Is.True);
            Assert.That(reader.CanRead(2), Is.True);
            Assert.That(reader.CanRead(-1), Is.False);
            Assert.That(reader.CanRead(3), Is.False);
            Assert.That(reader.CanRead(int.MaxValue), Is.False);
            Assert.That(writer.CanWrite(0), Is.True);
            Assert.That(writer.CanWrite(2), Is.True);
            Assert.That(writer.CanWrite(-1), Is.False);
            Assert.That(writer.CanWrite(3), Is.False);
            Assert.That(writer.CanWrite(int.MaxValue), Is.False);
        });
    }

    static WireReadResult ReadUnsigned(WireReader reader, int width)
    {
        switch (width)
        {
            case 1:
                return reader.ReadUnsigned8();
            case 2:
                return reader.ReadUnsigned16();
            case 4:
                return reader.ReadUnsigned32();
            case 6:
                return reader.ReadUnsigned48();
            default:
                throw new ArgumentOutOfRangeException(nameof(width));
        }
    }

    static WireWriteResult WriteUnsigned(
        WireWriter writer,
        int width,
        ulong value)
    {
        switch (width)
        {
            case 1:
                return writer.WriteUnsigned8(value);
            case 2:
                return writer.WriteUnsigned16(value);
            case 4:
                return writer.WriteUnsigned32(value);
            case 6:
                return writer.WriteUnsigned48(value);
            default:
                throw new ArgumentOutOfRangeException(nameof(width));
        }
    }

    static WireReadResult ReadAddress(WireReader reader, int width)
    {
        if (width == 4)
        {
            return reader.ReadIpv4Address();
        }

        return reader.ReadIpv6Address();
    }

    static WireWriteResult WriteAddress(
        WireWriter writer,
        int width,
        byte[] address)
    {
        if (width == 4)
        {
            return writer.WriteIpv4Address(address);
        }

        return writer.WriteIpv6Address(address);
    }

    static ulong Maximum(int width)
    {
        switch (width)
        {
            case 1:
                return 0xff;
            case 2:
                return 0xffff;
            case 4:
                return 0xffffffff;
            case 6:
                return 0xffffffffffff;
            default:
                throw new ArgumentOutOfRangeException(nameof(width));
        }
    }

    static string IntegerField(int width)
    {
        return "unsigned" + (width * 8);
    }

    static byte[] FilledBytes(int length, byte value)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = value;
            index = index + 1;
        }

        return bytes;
    }

    static byte[] SequentialBytes(int length)
    {
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)index;
            index = index + 1;
        }

        return bytes;
    }
}
