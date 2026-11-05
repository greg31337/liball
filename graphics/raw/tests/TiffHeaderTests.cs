using raw;

namespace raw.Tests;

public class TiffHeaderTests
{
    [Test]
    public void ClassicLittleEndianHeaderParsesAndLeavesTrailingBytes()
    {
        byte[] source = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x78, 0x56, 0x34, 0x12, 0xAA, 0xBB };
        BoundedInput input = CreateInput(source);

        TiffHeader header = TiffHeader.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(header.ByteOrder, Is.EqualTo(RawByteOrder.LittleEndian));
            Assert.That(header.BigTiff, Is.False);
            Assert.That(header.Version, Is.EqualTo(42));
            Assert.That(header.OffsetSize, Is.EqualTo(4));
            Assert.That(header.FirstIfdOffset, Is.EqualTo(0x12345678UL));
            Assert.That(header.BaseOffset, Is.EqualTo(0));
            Assert.That(header.HeaderSize, Is.EqualTo(8));
            Assert.That(input.GetPosition(), Is.EqualTo(8));
            Assert.That(input.GetRemaining(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ClassicBigEndianHeaderParses()
    {
        byte[] source = new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x12, 0x34, 0x56, 0x78 };

        TiffHeader header = TiffHeader.Parse(CreateInput(source));

        Assert.Multiple(() =>
        {
            Assert.That(header.ByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
            Assert.That(header.BigTiff, Is.False);
            Assert.That(header.Version, Is.EqualTo(42));
            Assert.That(header.OffsetSize, Is.EqualTo(4));
            Assert.That(header.FirstIfdOffset, Is.EqualTo(0x12345678UL));
            Assert.That(header.HeaderSize, Is.EqualTo(8));
        });
    }

    [Test]
    public void BigTiffLittleEndianHeaderParsesExactUnsignedOffset()
    {
        byte[] source = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00, 0x08, 0x00, 0x00, 0x00,
            0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0xF1
        };

        TiffHeader header = TiffHeader.Parse(CreateInput(source));

        Assert.Multiple(() =>
        {
            Assert.That(header.ByteOrder, Is.EqualTo(RawByteOrder.LittleEndian));
            Assert.That(header.BigTiff, Is.True);
            Assert.That(header.Version, Is.EqualTo(43));
            Assert.That(header.OffsetSize, Is.EqualTo(8));
            Assert.That(header.FirstIfdOffset, Is.EqualTo(0xF123456789ABCDEFUL));
            Assert.That(header.HeaderSize, Is.EqualTo(16));
        });
    }

    [Test]
    public void BigTiffBigEndianHeaderParses()
    {
        byte[] source = new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B, 0x00, 0x08, 0x00, 0x00,
            0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF
        };

        TiffHeader header = TiffHeader.Parse(CreateInput(source));

        Assert.Multiple(() =>
        {
            Assert.That(header.ByteOrder, Is.EqualTo(RawByteOrder.BigEndian));
            Assert.That(header.BigTiff, Is.True);
            Assert.That(header.FirstIfdOffset, Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(header.HeaderSize, Is.EqualTo(16));
        });
    }

    [Test]
    public void EmbeddedHeaderRetainsBaseAndResolvesRelativeIfdOffset()
    {
        byte[] source = new byte[]
        {
            0xAA, 0xBB, 0xCC,
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,
            0xDD, 0xEE
        };
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(source), 3, 10, new RawLimits());

        TiffHeader header = TiffHeader.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(header.BaseOffset, Is.EqualTo(3));
            Assert.That(header.FirstIfdOffset, Is.EqualTo(8));
            Assert.That(header.GetFirstIfdAbsoluteOffset(), Is.EqualTo(11));
            Assert.That(input.GetAbsolutePosition(), Is.EqualTo(11));
            Assert.That(input.GetRemaining(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ZeroIfdOffsetRemainsTheNullIfdPointer()
    {
        byte[] embedded = new byte[] { 0xAA, 0xBB, 0x49, 0x49, 0x2A, 0x00, 0x00, 0x00, 0x00, 0x00 };
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(embedded), 2, 8, new RawLimits());

        TiffHeader header = TiffHeader.Parse(input);

        Assert.Multiple(() =>
        {
            Assert.That(header.FirstIfdOffset, Is.EqualTo(0));
            Assert.That(header.GetFirstIfdAbsoluteOffset(), Is.EqualTo(0));
        });
    }

    [Test]
    public void InvalidByteOrderSignaturesAreRejectedAtHeaderBase()
    {
        byte[][] signatures = new byte[][]
        {
            new byte[] { 0x49, 0x4D },
            new byte[] { 0x4D, 0x49 },
            new byte[] { 0x00, 0x00 }
        };

        foreach (byte[] signature in signatures)
        {
            byte[] source = new byte[] { 0xAA, signature[0], signature[1] };
            BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(source), 1, 2, new RawLimits());

            RawFormatException? exception = Assert.Throws<RawFormatException>(() => TiffHeader.Parse(input));

            Assert.Multiple(() =>
            {
                Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidSignature));
                Assert.That(exception.Offset, Is.EqualTo(1));
            });
        }
    }

    [Test]
    public void UnsupportedVersionsAreRejectedInBothByteOrders()
    {
        byte[][] sources = new byte[][]
        {
            new byte[] { 0x49, 0x49, 0x2C, 0x00 },
            new byte[] { 0x4D, 0x4D, 0x00, 0x2C }
        };

        foreach (byte[] source in sources)
        {
            RawFormatException? exception = Assert.Throws<RawFormatException>(() => TiffHeader.Parse(CreateInput(source)));

            Assert.Multiple(() =>
            {
                Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidSignature));
                Assert.That(exception.Offset, Is.EqualTo(2));
            });
        }
    }

    [Test]
    public void BigTiffRequiresEightByteOffsetsAndZeroReservedField()
    {
        byte[] invalidOffsetSize = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00, 0x04, 0x00, 0x00, 0x00,
            0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        byte[] invalidReserved = new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B, 0x00, 0x08, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10
        };

        RawFormatException? offsetSize = Assert.Throws<RawFormatException>(() => TiffHeader.Parse(CreateInput(invalidOffsetSize)));
        RawFormatException? reserved = Assert.Throws<RawFormatException>(() => TiffHeader.Parse(CreateInput(invalidReserved)));

        Assert.Multiple(() =>
        {
            Assert.That((offsetSize ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
            Assert.That(offsetSize.Offset, Is.EqualTo(4));
            Assert.That((reserved ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidContainer));
            Assert.That(reserved.Offset, Is.EqualTo(6));
        });
    }

    [Test]
    public void EveryClassicHeaderTruncationIsRejected()
    {
        byte[] complete = new byte[] { 0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00 };

        for (int length = 0; length < complete.Length; length++)
        {
            byte[] truncated = complete.Take(length).ToArray();
            RawFormatException? exception = Assert.Throws<RawFormatException>(() => TiffHeader.Parse(CreateInput(truncated)));

            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput), $"length {length}");
        }
    }

    [Test]
    public void EveryBigTiffHeaderTruncationIsRejected()
    {
        byte[] complete = new byte[]
        {
            0x4D, 0x4D, 0x00, 0x2B, 0x00, 0x08, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10
        };

        for (int length = 0; length < complete.Length; length++)
        {
            byte[] truncated = complete.Take(length).ToArray();
            RawFormatException? exception = Assert.Throws<RawFormatException>(() => TiffHeader.Parse(CreateInput(truncated)));

            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput), $"length {length}");
        }
    }

    [Test]
    public void UnsignedBigTiffOffsetOverflowIsDeferredUntilAbsoluteResolution()
    {
        byte[] source = new byte[]
        {
            0x49, 0x49, 0x2B, 0x00, 0x08, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };
        TiffHeader header = TiffHeader.Parse(CreateInput(source));

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => header.GetFirstIfdAbsoluteOffset());

        Assert.Multiple(() =>
        {
            Assert.That(header.FirstIfdOffset, Is.EqualTo(ulong.MaxValue));
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ArithmeticOverflow));
        });
    }

    [Test]
    public void NullInputIsRejected()
    {
        RawFormatException? exception = Assert.Throws<RawFormatException>(() => TiffHeader.Parse(null));

        Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
    }

    static BoundedInput CreateInput(byte[] source)
    {
        return new BoundedInput(new ReadOnlyMemory<byte>(source), 0, source.Length, new RawLimits());
    }
}
