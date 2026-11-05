namespace avif.Tests;

public class BitIoTests
{
    [Test]
    public void MsbFirstBitsCrossByteBoundariesExactly()
    {
        byte[] buffer = new byte[2];
        BitWriter writer = BitWriter.Create(buffer.Length);

        writer.WriteBits(buffer, 5, 3);
        writer.WriteBits(buffer, 73, 7);
        writer.WriteBits(buffer, 41, 6);

        Assert.That(buffer, Is.EqualTo(new byte[] { 178, 105 }));

        BitReader reader = BitReader.Create(buffer.Length);
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadBits(buffer, 3), Is.EqualTo(5));
            Assert.That(reader.ReadBits(buffer, 7), Is.EqualTo(73));
            Assert.That(reader.ReadBits(buffer, 6), Is.EqualTo(41));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.RemainingBits(), Is.Zero);
        });
    }

    [Test]
    public void SignedBitValuesRoundTripAtRepresentableBoundaries()
    {
        byte[] buffer = new byte[10];
        BitWriter writer = BitWriter.Create(buffer.Length);

        writer.WriteSignedBits(buffer, -16, 5);
        writer.WriteSignedBits(buffer, 15, 5);
        writer.WriteSignedBits(buffer, -1, 5);
        writer.WriteSignedBits(buffer, long.MinValue, 64);

        BitReader reader = new BitReader(buffer.Length, writer.BitPosition);
        Assert.Multiple(() =>
        {
            Assert.That(reader.ReadSignedBits(buffer, 5), Is.EqualTo(-16));
            Assert.That(reader.ReadSignedBits(buffer, 5), Is.EqualTo(15));
            Assert.That(reader.ReadSignedBits(buffer, 5), Is.EqualTo(-1));
            Assert.That(reader.ReadSignedBits(buffer, 64), Is.EqualTo(long.MinValue));
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void OutOfRangeSignedValueIsRejectedAtomically()
    {
        byte[] destination = [204];
        BitWriter writer = BitWriter.Create(destination.Length);

        AvifResultCode code = writer.WriteSignedBits(destination, 16, 5);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(writer.BitPosition, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 204 }));
        });
    }

    [Test]
    public void InvalidRawBitArgumentsAreRejectedAtomically()
    {
        byte[] destination = [204];
        BitWriter valueWriter = BitWriter.Create(destination.Length);
        AvifResultCode valueCode = valueWriter.WriteBits(destination, 2, 1);

        byte[] source = [255];
        BitReader widthReader = BitReader.Create(source.Length);
        ulong value = widthReader.ReadBits(source, 65);

        Assert.Multiple(() =>
        {
            Assert.That(valueCode, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(valueWriter.BitPosition, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 204 }));
            Assert.That(value, Is.Zero);
            Assert.That(widthReader.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(widthReader.BitPosition, Is.Zero);
        });
    }

    [Test]
    public void BitBoundsFailuresAreAtomicAndSticky()
    {
        byte[] source = [255];
        byte[] destination = [170];
        BitReader reader = new BitReader(source.Length, 7);
        BitWriter writer = new BitWriter(destination.Length, 7);

        ulong readValue = reader.ReadBits(source, 8);
        int laterBit = reader.ReadBit(source);
        AvifResultCode writeCode = writer.WriteBits(destination, 255, 8);
        AvifResultCode laterWrite = writer.WriteBit(destination, 1);

        Assert.Multiple(() =>
        {
            Assert.That(readValue, Is.Zero);
            Assert.That(laterBit, Is.Zero);
            Assert.That(reader.BitPosition, Is.Zero);
            Assert.That(reader.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(writeCode, Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(laterWrite, Is.EqualTo(AvifResultCode.BufferTooSmall));
            Assert.That(writer.BitPosition, Is.Zero);
            Assert.That(destination, Is.EqualTo(new byte[] { 170 }));
        });
    }

    [Test]
    public void ByteAlignmentCanRequireZeroPadding()
    {
        byte[] buffer = new byte[1];
        BitWriter writer = BitWriter.Create(buffer.Length);
        writer.WriteBits(buffer, 5, 3);
        writer.AlignToByte(buffer, 0);

        BitReader reader = BitReader.Create(buffer.Length);
        ulong prefix = reader.ReadBits(buffer, 3);
        AvifResultCode code = reader.AlignToByte(buffer, true);

        Assert.Multiple(() =>
        {
            Assert.That(buffer, Is.EqualTo(new byte[] { 160 }));
            Assert.That(prefix, Is.EqualTo(5));
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(reader.IsByteAligned(), Is.True);
        });
    }

    [Test]
    public void InvalidAlignmentPaddingDoesNotAdvance()
    {
        byte[] source = [175];
        BitReader reader = BitReader.Create(source.Length);
        reader.ReadBits(source, 3);

        AvifResultCode code = reader.AlignToByte(source, true);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(reader.BitPosition, Is.EqualTo(3));
        });
    }

    [Test]
    public void TrailingBitsWriteAndValidateAtAlignedAndUnalignedPositions()
    {
        byte[] unaligned = new byte[1];
        BitWriter unalignedWriter = BitWriter.Create(unaligned.Length);
        unalignedWriter.WriteBits(unaligned, 5, 3);
        unalignedWriter.WriteTrailingBits(unaligned);

        byte[] aligned = new byte[1];
        BitWriter alignedWriter = BitWriter.Create(aligned.Length);
        alignedWriter.WriteTrailingBits(aligned);

        BitReader unalignedReader = BitReader.Create(unaligned.Length);
        unalignedReader.ReadBits(unaligned, 3);
        AvifResultCode unalignedCode = unalignedReader.ReadTrailingBits(unaligned);
        BitReader alignedReader = BitReader.Create(aligned.Length);
        AvifResultCode alignedCode = alignedReader.ReadTrailingBits(aligned);

        Assert.Multiple(() =>
        {
            Assert.That(unaligned, Is.EqualTo(new byte[] { 176 }));
            Assert.That(aligned, Is.EqualTo(new byte[] { 128 }));
            Assert.That(unalignedCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(unalignedReader.BitPosition, Is.EqualTo(8));
            Assert.That(alignedCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(alignedReader.BitPosition, Is.EqualTo(8));
        });
    }

    [Test]
    public void InvalidTrailingBitsDoNotAdvance()
    {
        byte[] missingOne = [160];
        byte[] nonzeroPadding = [184];
        BitReader missingOneReader = BitReader.Create(missingOne.Length);
        BitReader nonzeroPaddingReader = BitReader.Create(nonzeroPadding.Length);
        missingOneReader.ReadBits(missingOne, 3);
        nonzeroPaddingReader.ReadBits(nonzeroPadding, 3);

        AvifResultCode missingOneCode = missingOneReader.ReadTrailingBits(missingOne);
        AvifResultCode nonzeroPaddingCode = nonzeroPaddingReader.ReadTrailingBits(nonzeroPadding);

        Assert.Multiple(() =>
        {
            Assert.That(missingOneCode, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(missingOneReader.BitPosition, Is.EqualTo(3));
            Assert.That(nonzeroPaddingCode, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(nonzeroPaddingReader.BitPosition, Is.EqualTo(3));
        });
    }

    [Test]
    public void TrailingBitsRejectAdditionalDeclaredPayload()
    {
        byte[] source = [128, 0];
        BitReader reader = BitReader.Create(source.Length);

        AvifResultCode code = reader.ReadTrailingBits(source);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidData));
            Assert.That(reader.BitPosition, Is.Zero);
        });
    }
}
