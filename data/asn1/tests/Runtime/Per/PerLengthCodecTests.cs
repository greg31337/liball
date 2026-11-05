using asn1;

namespace asn1.Tests;

public class PerLengthCodecTests
{
    [Test]
    public void UnconstrainedComponentsUseStandardBoundaryForms()
    {
        int[] lengths = [0, 127, 128, 16383, 16384, 65536, 65537];
        byte[][] expected =
        [
            [0x00],
            [0x7f],
            [0x80, 0x80],
            [0xbf, 0xff],
            [0xc1],
            [0xc4],
            [0xc4]
        ];
        int[] fragments = [0, 127, 128, 16383, 16384, 65536, 65536];
        bool[] more = [false, false, false, false, true, true, true];

        int index = 0;
        while (index < lengths.Length)
        {
            PerLengthCodec codec = CreateCodec();
            PerBitWriter writer = CreateWriter();
            PerLengthResult written = codec.WriteLengthComponent(
                writer,
                lengths[index],
                false);
            PerBitWriteResult output = writer.GetOutput();
            PerBitReader reader = CreateReader();
            reader.AppendInputBits(
                output.Data,
                0,
                output.WrittenBitCount,
                true);
            PerLengthResult read = codec.ReadLengthComponent(reader, false);

            Assert.That(output.Data, Is.EqualTo(expected[index]));
            Assert.That(written.FragmentLength,
                Is.EqualTo(fragments[index]));
            Assert.That(written.MoreFragments, Is.EqualTo(more[index]));
            Assert.That(read.FragmentLength, Is.EqualTo(fragments[index]));
            Assert.That(read.MoreFragments, Is.EqualTo(more[index]));
            index = index + 1;
        }
    }

    [Test]
    public void ConstrainedAndFixedSizesUseTheirExactWidths()
    {
        PerLengthCodec codec = CreateCodec();
        PerBitWriter fixedWriter = CreateWriter();
        PerLengthResult fixedSize = codec.WriteConstrainedLength(
            fixedWriter,
            12,
            12,
            12,
            false);

        PerBitWriter unalignedWriter = CreateWriter();
        PerLengthResult unaligned = codec.WriteConstrainedLength(
            unalignedWriter,
            4,
            3,
            5,
            false);
        PerBitWriteResult unalignedOutput = unalignedWriter.GetOutput();
        PerBitReader unalignedReader = CreateReader();
        unalignedReader.AppendInputBits([0x40], 0, 2, true);
        PerLengthResult unalignedRead = codec.ReadConstrainedLength(
            unalignedReader,
            3,
            5,
            false);

        PerBitWriter alignedWriter = CreateWriter();
        alignedWriter.WriteBits(5, 3);
        PerLengthResult aligned = codec.WriteConstrainedLength(
            alignedWriter,
            265,
            10,
            265,
            true);
        PerBitWriteResult alignedOutput = alignedWriter.GetOutput();
        PerBitReader alignedReader = CreateReader();
        alignedReader.AppendInputBits(alignedOutput.Data, 0, 16, true);
        alignedReader.ReadBits(3);
        PerLengthResult alignedRead = codec.ReadConstrainedLength(
            alignedReader,
            10,
            265,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(fixedSize.DeterminantOmitted, Is.True);
            Assert.That(fixedWriter.GetWrittenBitCount(), Is.Zero);
            Assert.That(unaligned.BitCount, Is.EqualTo(2));
            Assert.That(unalignedOutput.Data,
                Is.EqualTo(new byte[] { 0x40 }));
            Assert.That(unalignedRead.Length, Is.EqualTo(4));
            Assert.That(aligned.PaddingBitCount, Is.EqualTo(5));
            Assert.That(alignedOutput.Data,
                Is.EqualTo(new byte[] { 0xa0, 0xff }));
            Assert.That(alignedRead.Length, Is.EqualTo(265));
            Assert.That(alignedRead.PaddingBitCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void NormallySmallLengthsUseSevenBitsOrAFlaggedLength()
    {
        PerLengthCodec codec = CreateCodec();
        PerBitWriter oneWriter = CreateWriter();
        codec.WriteNormallySmallLength(oneWriter, 1, false);
        PerBitWriteResult one = oneWriter.GetOutput();
        PerBitWriter sixtyFourWriter = CreateWriter();
        codec.WriteNormallySmallLength(sixtyFourWriter, 64, false);
        PerBitWriteResult sixtyFour = sixtyFourWriter.GetOutput();
        PerBitWriter unalignedWriter = CreateWriter();
        PerLengthResult unaligned = codec.WriteNormallySmallLength(
            unalignedWriter,
            65,
            false);
        PerBitWriteResult unalignedOutput = unalignedWriter.GetOutput();
        PerBitWriter alignedWriter = CreateWriter();
        PerLengthResult aligned = codec.WriteNormallySmallLength(
            alignedWriter,
            65,
            true);
        PerBitWriteResult alignedOutput = alignedWriter.GetOutput();

        PerBitReader reader = CreateReader();
        reader.AppendInputBits(alignedOutput.Data, 0, 16, true);
        PerLengthResult decoded = codec.ReadNormallySmallLength(reader, true);

        Assert.Multiple(() =>
        {
            Assert.That(one.Data, Is.EqualTo(new byte[] { 0x00 }));
            Assert.That(one.WrittenBitCount, Is.EqualTo(7));
            Assert.That(sixtyFour.Data, Is.EqualTo(new byte[] { 0x7e }));
            Assert.That(unaligned.BitCount, Is.EqualTo(9));
            Assert.That(unalignedOutput.Data,
                Is.EqualTo(new byte[] { 0xa0, 0x80 }));
            Assert.That(aligned.PaddingBitCount, Is.EqualTo(7));
            Assert.That(alignedOutput.Data,
                Is.EqualTo(new byte[] { 0x80, 0x41 }));
            Assert.That(decoded.Length, Is.EqualTo(65));
            Assert.That(decoded.NormallySmall, Is.True);
        });
    }

    [Test]
    public void FragmentedOctetsRoundTripAtEveryStandardBoundary()
    {
        int[] lengths = [0, 127, 128, 16383, 16384, 65536, 65537];
        int index = 0;
        while (index < lengths.Length)
        {
            byte[] source = CreateData(lengths[index]);
            PerLengthCodec codec = CreateCodec();
            PerBitWriter writer = CreateWriter();
            PerLengthResult written = codec.WriteFragmentedOctets(
                writer,
                source,
                0,
                source.Length,
                false);
            PerBitWriteResult output = writer.GetOutput();
            PerBitReader reader = CreateReader();
            reader.AppendInputBits(
                output.Data,
                0,
                output.WrittenBitCount,
                true);
            PerLengthResult read = codec.ReadFragmentedOctets(reader, false);

            Assert.That(written.Success, Is.True);
            Assert.That(read.Success, Is.True);
            Assert.That(read.Length, Is.EqualTo(source.Length));
            Assert.That(read.Data, Is.EqualTo(source));
            Assert.That(reader.GetAvailableBitCount(), Is.Zero);
            index = index + 1;
        }
    }

    [Test]
    public void FragmentationInterleavesMaximumChunksAndFinalRemainder()
    {
        byte[] source = CreateData(65537);
        PerLengthCodec codec = CreateCodec();
        PerBitWriter writer = CreateWriter();
        writer.WriteBits(5, 3);
        PerLengthResult written = codec.WriteFragmentedOctets(
            writer,
            source,
            0,
            source.Length,
            true);
        PerBitWriteResult output = writer.GetOutput();

        Assert.Multiple(() =>
        {
            Assert.That(written.PaddingBitCount, Is.EqualTo(5));
            Assert.That(output.Data[0], Is.EqualTo(0xa0));
            Assert.That(output.Data[1], Is.EqualTo(0xc4));
            Assert.That(output.Data[65538], Is.EqualTo(0x01));
            Assert.That(output.Data[65539], Is.EqualTo(source[65536]));
        });

        PerBitReader reader = CreateReader();
        reader.AppendInputBits(
            output.Data,
            0,
            output.WrittenBitCount,
            true);
        reader.ReadBits(3);
        PerLengthResult read = codec.ReadFragmentedOctets(reader, true);
        Assert.That(read.Data, Is.EqualTo(source));
    }

    [Test]
    public void FragmentedReaderResumesWithoutConsumingPartialPayload()
    {
        PerLengthCodec codec = CreateCodec();
        PerBitReader reader = CreateReader();
        reader.AppendInputBits([0x03, 0x10, 0x20], 0, 24, false);
        PerLengthResult waiting = codec.ReadFragmentedOctets(reader, false);
        int waitingOffset = reader.GetBitOffset();
        reader.AppendInputBits([0x30], 0, 8, true);
        PerLengthResult decoded = codec.ReadFragmentedOctets(reader, false);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(waitingOffset, Is.Zero);
            Assert.That(decoded.Data,
                Is.EqualTo(new byte[] { 0x10, 0x20, 0x30 }));
            Assert.That(decoded.BitCount, Is.EqualTo(32));
        });
    }

    [Test]
    public void MalformedLengthFormsAndFragmentSequencesAreRejected()
    {
        PerLengthCodec codec = CreateCodec();
        PerBitReader nonMinimalReader = CreateReader();
        nonMinimalReader.AppendInputBits([0x80, 0x7f], 0, 16, true);
        PerLengthResult nonMinimal =
            codec.ReadLengthComponent(nonMinimalReader, false);

        PerBitReader nonZeroPaddingReader = CreateReader();
        nonZeroPaddingReader.AppendInputBits([0xbf, 0x01], 0, 16, true);
        nonZeroPaddingReader.ReadBits(3);
        PerLengthResult nonZeroPadding =
            codec.ReadLengthComponent(nonZeroPaddingReader, true);

        PerBitReader largeSmallReader = CreateReader();
        largeSmallReader.AppendInputBits([0x80, 0x40], 0, 16, true);
        PerLengthResult largeSmall =
            codec.ReadNormallySmallLength(largeSmallReader, true);

        byte[] malformed = new byte[32771];
        malformed[0] = 0xc1;
        malformed[16385] = 0xc1;
        malformed[32770] = 0x00;
        PerBitReader malformedReader = CreateReader();
        malformedReader.AppendInputBits(
            malformed,
            0,
            malformed.Length * 8,
            true);
        PerLengthResult fragments =
            codec.ReadFragmentedOctets(malformedReader, false);

        Assert.Multiple(() =>
        {
            Assert.That(nonMinimal.ErrorCode, Is.EqualTo("ASNP0036"));
            Assert.That(nonZeroPadding.ErrorCode, Is.EqualTo("ASNP0007"));
            Assert.That(largeSmall.ErrorCode, Is.EqualTo("ASNP0034"));
            Assert.That(fragments.ErrorCode, Is.EqualTo("ASNP0038"));
        });
    }

    [Test]
    public void ReassemblyChecksByteAndAllocationLimitsBeforeCopying()
    {
        byte[] encoded = new byte[21];
        encoded[0] = 20;
        PerBitReader byteReader = CreateReader();
        byteReader.AppendInputBits(encoded, 0, encoded.Length * 8, true);
        AsnResourceLimits byteLimits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 10,
            MaximumTotalAllocationBytes = 100
        };
        PerLengthCodec byteCodec = new PerLengthCodec(byteLimits);
        PerLengthResult byteLimit =
            byteCodec.ReadFragmentedOctets(byteReader, false);

        PerBitReader allocationReader = CreateReader();
        allocationReader.AppendInputBits(
            encoded,
            0,
            encoded.Length * 8,
            true);
        AsnResourceLimits allocationLimits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 100,
            MaximumTotalAllocationBytes = 10
        };
        PerLengthCodec allocationCodec =
            new PerLengthCodec(allocationLimits);
        PerLengthResult allocationLimit =
            allocationCodec.ReadFragmentedOctets(allocationReader, false);

        Assert.Multiple(() =>
        {
            Assert.That(byteLimit.ErrorCode, Is.EqualTo("ASNR0006"));
            Assert.That(byteLimit.LimitExceeded, Is.True);
            Assert.That(allocationLimit.ErrorCode, Is.EqualTo("ASNR0005"));
            Assert.That(allocationLimit.LimitExceeded, Is.True);
        });
    }

    private static byte[] CreateData(int count)
    {
        byte[] data = new byte[count];
        int index = 0;
        while (index < count)
        {
            data[index] = (byte)(index * 37 + 11);
            index = index + 1;
        }

        return data;
    }

    private static PerLengthCodec CreateCodec()
    {
        return new PerLengthCodec(new AsnResourceLimits());
    }

    private static PerBitWriter CreateWriter()
    {
        return new PerBitWriter(new AsnResourceLimits());
    }

    private static PerBitReader CreateReader()
    {
        return new PerBitReader(new AsnResourceLimits());
    }
}
