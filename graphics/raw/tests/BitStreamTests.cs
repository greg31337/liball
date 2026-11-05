using raw;

namespace raw.Tests;

public class BitStreamTests
{
    [Test]
    public void MsbReaderPeeksAndConsumesAcrossByteBoundaries()
    {
        MsbBitReader reader = CreateMsbReader(new byte[] { 0xAB, 0xCD, 0xEF });

        Assert.Multiple(() =>
        {
            Assert.That(reader.PeekBits(4), Is.EqualTo(0xA));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(0));
            Assert.That(reader.PeekBits(12), Is.EqualTo(0xABC));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(24));
        });

        reader.ConsumeBits(4);

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xBC));
            Assert.That(reader.ReadBits(4), Is.EqualTo(0xD));
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xEF));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(24));
            Assert.That(reader.IsAtEnd(), Is.True);
            Assert.DoesNotThrow(() => reader.RequireEnd());
        });
    }

    [Test]
    public void LsbReaderPeeksAndConsumesAcrossByteBoundaries()
    {
        LsbBitReader reader = CreateLsbReader(new byte[] { 0xAB, 0xCD, 0xEF });

        Assert.Multiple(() =>
        {
            Assert.That(reader.PeekBits(4), Is.EqualTo(0xB));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(0));
            Assert.That(reader.PeekBits(12), Is.EqualTo(0xDAB));
        });

        reader.ConsumeBits(4);

        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xDA));
            Assert.That(reader.ReadBits(4), Is.EqualTo(0xC));
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xEF));
            Assert.That(reader.IsAtEnd(), Is.True);
        });
    }

    [Test]
    public void ZeroAndThirtyTwoBitOperationsWorkAtBothApiBoundaries()
    {
        byte[] source = new byte[] { 0x89, 0xAB, 0xCD, 0xEF };
        MsbBitReader msbReader = CreateMsbReader(source);
        LsbBitReader lsbReader = CreateLsbReader(source);
        byte[] msbBytes = new byte[4];
        byte[] lsbBytes = new byte[4];
        MsbBitWriter msbWriter = new MsbBitWriter(msbBytes);
        LsbBitWriter lsbWriter = new LsbBitWriter(lsbBytes);

        Assert.Multiple(() =>
        {
            Assert.That(msbReader.PeekBits(0), Is.EqualTo(0));
            Assert.That(lsbReader.ReadBits(0), Is.EqualTo(0));
            Assert.That(msbReader.GetBitPosition(), Is.EqualTo(0));
            Assert.That(lsbReader.GetBitPosition(), Is.EqualTo(0));
            Assert.That(msbReader.ReadBits(32), Is.EqualTo(0x89ABCDEFU));
            Assert.That(lsbReader.ReadBits(32), Is.EqualTo(0xEFCDAB89U));
        });

        msbWriter.WriteBits(0, 0);
        lsbWriter.WriteBits(0, 0);
        msbWriter.WriteBits(0x89ABCDEFU, 32);
        lsbWriter.WriteBits(0xEFCDAB89U, 32);

        Assert.Multiple(() =>
        {
            Assert.That(msbBytes, Is.EqualTo(source));
            Assert.That(lsbBytes, Is.EqualTo(source));
            Assert.That(msbWriter.GetBitPosition(), Is.EqualTo(32));
            Assert.That(lsbWriter.GetBitPosition(), Is.EqualTo(32));
        });
    }

    [Test]
    public void TruncatedPeekRetainsEveryAvailableBit()
    {
        MsbBitReader reader = CreateMsbReader(new byte[] { 0xAB });

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => reader.PeekBits(12));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That(exception.Offset, Is.EqualTo(1));
            Assert.That(reader.GetBitPosition(), Is.EqualTo(0));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(8));
            Assert.That(reader.ReadBits(8), Is.EqualTo(0xAB));
            Assert.That(reader.IsAtEnd(), Is.True);
        });
    }

    [Test]
    public void ReadersValidateOnePaddingDuringByteAlignment()
    {
        MsbBitReader msb = CreateMsbReader(new byte[] { 0xBF });
        LsbBitReader lsb = CreateLsbReader(new byte[] { 0xFD });

        Assert.Multiple(() =>
        {
            Assert.That(msb.ReadBits(3), Is.EqualTo(5));
            Assert.That(lsb.ReadBits(3), Is.EqualTo(5));
        });

        msb.AlignToByteWithExpectedPadding(1);
        lsb.AlignToByteWithExpectedPadding(1);

        Assert.Multiple(() =>
        {
            Assert.That(msb.GetBitPosition(), Is.EqualTo(8));
            Assert.That(lsb.GetBitPosition(), Is.EqualTo(8));
            Assert.That(msb.IsAtEnd(), Is.True);
            Assert.That(lsb.IsAtEnd(), Is.True);
        });
    }

    [Test]
    public void PaddingMismatchDoesNotConsumeBits()
    {
        MsbBitReader msb = CreateMsbReader(new byte[] { 0xA0 });
        LsbBitReader lsb = CreateLsbReader(new byte[] { 0x05 });
        msb.ReadBits(3);
        lsb.ReadBits(3);

        RawFormatException? msbError = Assert.Throws<RawFormatException>(() => msb.AlignToByteWithExpectedPadding(1));
        RawFormatException? lsbError = Assert.Throws<RawFormatException>(() => lsb.AlignToByteWithExpectedPadding(1));

        Assert.Multiple(() =>
        {
            Assert.That((msbError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((lsbError ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That(msb.GetBitPosition(), Is.EqualTo(3));
            Assert.That(lsb.GetBitPosition(), Is.EqualTo(3));
        });

        msb.AlignToByteWithExpectedPadding(0);
        lsb.AlignToByteWithExpectedPadding(0);

        Assert.Multiple(() =>
        {
            Assert.That(msb.IsAtEnd(), Is.True);
            Assert.That(lsb.IsAtEnd(), Is.True);
        });
    }

    [Test]
    public void ReadersValidatePaddingAcrossTheEntireTail()
    {
        MsbBitReader msb = CreateMsbReader(new byte[] { 0xA0, 0x00, 0x00 });
        LsbBitReader lsb = CreateLsbReader(new byte[] { 0x05, 0x00, 0x00 });
        msb.ReadBits(3);
        lsb.ReadBits(3);

        msb.ValidatePaddingToEnd(0);
        lsb.ValidatePaddingToEnd(0);

        Assert.Multiple(() =>
        {
            Assert.That(msb.GetBitPosition(), Is.EqualTo(24));
            Assert.That(lsb.GetBitPosition(), Is.EqualTo(24));
            Assert.DoesNotThrow(() => msb.RequireEnd());
            Assert.DoesNotThrow(() => lsb.RequireEnd());
        });
    }

    [Test]
    public void RequireEndRejectsUnreadBufferedOrInputBits()
    {
        MsbBitReader reader = CreateMsbReader(new byte[] { 0x12, 0x34 });
        reader.PeekBits(4);

        RawFormatException? exception = Assert.Throws<RawFormatException>(() => reader.RequireEnd());

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That(reader.GetRemainingBitCount(), Is.EqualTo(16));
        });

        reader.ReadBits(16);
        Assert.DoesNotThrow(() => reader.RequireEnd());
    }

    [Test]
    public void WritersProduceKnownMsbAndLsbByteSequences()
    {
        byte[] msbBytes = new byte[2];
        byte[] lsbBytes = new byte[2];
        MsbBitWriter msb = new MsbBitWriter(msbBytes);
        LsbBitWriter lsb = new LsbBitWriter(lsbBytes);

        msb.WriteBits(0xA, 4);
        msb.WriteBits(0xBC, 8);
        msb.WriteBits(0xD, 4);
        lsb.WriteBits(0xB, 4);
        lsb.WriteBits(0xDA, 8);
        lsb.WriteBits(0xC, 4);

        Assert.Multiple(() =>
        {
            Assert.That(msb.Finish(0), Is.EqualTo(2));
            Assert.That(lsb.Finish(0), Is.EqualTo(2));
            Assert.That(msbBytes, Is.EqualTo(new byte[] { 0xAB, 0xCD }));
            Assert.That(lsbBytes, Is.EqualTo(new byte[] { 0xAB, 0xCD }));
            Assert.That(msb.GetBitPosition(), Is.EqualTo(16));
            Assert.That(lsb.GetBitPosition(), Is.EqualTo(16));
            Assert.That(msb.GetRemainingBitCapacity(), Is.EqualTo(0));
            Assert.That(lsb.GetRemainingBitCapacity(), Is.EqualTo(0));
        });
    }

    [Test]
    public void WritersSupportZeroAndOneBytePadding()
    {
        byte[] msbZeroBytes = new byte[1];
        byte[] msbOneBytes = new byte[1];
        byte[] lsbZeroBytes = new byte[1];
        byte[] lsbOneBytes = new byte[1];
        MsbBitWriter msbZero = new MsbBitWriter(msbZeroBytes);
        MsbBitWriter msbOne = new MsbBitWriter(msbOneBytes);
        LsbBitWriter lsbZero = new LsbBitWriter(lsbZeroBytes);
        LsbBitWriter lsbOne = new LsbBitWriter(lsbOneBytes);

        msbZero.WriteBits(5, 3);
        msbOne.WriteBits(5, 3);
        lsbZero.WriteBits(5, 3);
        lsbOne.WriteBits(5, 3);
        msbZero.Finish(0);
        msbOne.Finish(1);
        lsbZero.Finish(0);
        lsbOne.Finish(1);

        Assert.Multiple(() =>
        {
            Assert.That(msbZeroBytes[0], Is.EqualTo(0xA0));
            Assert.That(msbOneBytes[0], Is.EqualTo(0xBF));
            Assert.That(lsbZeroBytes[0], Is.EqualTo(0x05));
            Assert.That(lsbOneBytes[0], Is.EqualTo(0xFD));
        });
    }

    [Test]
    public void WritersPreflightValueWidthAndBoundedCapacity()
    {
        byte[] bytes = new byte[] { 0xAA };
        MsbBitWriter writer = new MsbBitWriter(bytes);

        RawFormatException? value = Assert.Throws<RawFormatException>(() => writer.WriteBits(8, 3));

        writer.WriteBits(0xAB, 8);
        RawFormatException? capacity = Assert.Throws<RawFormatException>(() => writer.WriteBits(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That((value ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((capacity ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidLength));
            Assert.That(writer.GetBitPosition(), Is.EqualTo(8));
            Assert.That(writer.GetBytesWritten(), Is.EqualTo(1));
            Assert.That(bytes[0], Is.EqualTo(0xAB));
        });
    }

    [Test]
    public void StreamWritersRespectBoundsAndLeaveCallerStreamsOpen()
    {
        MemoryStream msbStream = new MemoryStream();
        MemoryStream lsbStream = new MemoryStream();
        MsbBitWriter msb = MsbBitWriter.CreateStream(msbStream, 1);
        LsbBitWriter lsb = LsbBitWriter.CreateStream(lsbStream, 1);
        msb.WriteBits(5, 3);
        lsb.WriteBits(5, 3);

        msb.Finish(1);
        lsb.Finish(1);

        Assert.Multiple(() =>
        {
            Assert.That(msbStream.ToArray(), Is.EqualTo(new byte[] { 0xBF }));
            Assert.That(lsbStream.ToArray(), Is.EqualTo(new byte[] { 0xFD }));
            Assert.That(msbStream.CanWrite, Is.True);
            Assert.That(lsbStream.CanWrite, Is.True);
        });
    }

    [Test]
    public void InvalidCountsAndPaddingBitsAreRejected()
    {
        MsbBitReader reader = CreateMsbReader(new byte[] { 0x00 });
        LsbBitWriter writer = new LsbBitWriter(new byte[1]);

        RawFormatException? negative = Assert.Throws<RawFormatException>(() => reader.ReadBits(-1));
        RawFormatException? tooLarge = Assert.Throws<RawFormatException>(() => reader.PeekBits(33));
        RawFormatException? readPadding = Assert.Throws<RawFormatException>(() => reader.AlignToByteWithExpectedPadding(2));
        RawFormatException? writePadding = Assert.Throws<RawFormatException>(() => writer.Finish(-1));

        Assert.Multiple(() =>
        {
            Assert.That((negative ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((tooLarge ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((readPadding ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That((writePadding ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
        });
    }

    static MsbBitReader CreateMsbReader(byte[] bytes)
    {
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(bytes), 0, bytes.Length, new RawLimits());
        return new MsbBitReader(input);
    }

    static LsbBitReader CreateLsbReader(byte[] bytes)
    {
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(bytes), 0, bytes.Length, new RawLimits());
        return new LsbBitReader(input);
    }
}
