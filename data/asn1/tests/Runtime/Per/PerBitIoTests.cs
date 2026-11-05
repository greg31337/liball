using asn1;

namespace asn1.Tests;

public class PerBitIoTests
{
    [Test]
    public void WriterAndReaderUseMsbFirstOrderAcrossArbitraryBitChunks()
    {
        PerBitWriter writer = CreateWriter();
        PerBitWriteResult firstWrite = writer.WriteBits(5, 3);
        PerBitWriteResult secondWrite = writer.WriteInputBits([0x6e], 0, 8);
        PerBitWriteResult aligned = writer.AlignOutput();
        PerBitWriteResult output = writer.GetOutput();

        PerBitReader reader = CreateReader();
        PerBitReadResult firstChunk = reader.AppendInputBits(
            [0xa0],
            0,
            3,
            false);
        PerBitReadResult waiting = reader.ReadBits(4);
        PerBitReadResult secondChunk = reader.AppendInputBits(
            [0x6e],
            0,
            8,
            false);
        PerBitReadResult firstField = reader.ReadBits(4);
        PerBitReadResult secondField = reader.ReadBits(7);
        PerBitReadResult paddingChunk = reader.AppendInputBits(
            [0x00],
            0,
            5,
            true);
        PerBitReadResult inputAligned = reader.AlignInput(true);

        Assert.Multiple(() =>
        {
            Assert.That(firstWrite.Success, Is.True);
            Assert.That(secondWrite.Success, Is.True);
            Assert.That(aligned.Success, Is.True);
            Assert.That(aligned.PaddedBitCount, Is.EqualTo(5));
            Assert.That(writer.GetWrittenBitCount(), Is.EqualTo(16));
            Assert.That(output.Data, Is.EqualTo(new byte[] { 0xad, 0xc0 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(16));
            Assert.That(firstChunk.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(secondChunk.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(paddingChunk.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(waiting.ConsumedBitCount, Is.Zero);
            Assert.That(reader.GetBitOffset(), Is.EqualTo(16));
            Assert.That(firstField.Value,
                Is.EqualTo(new System.Numerics.BigInteger(10)));
            Assert.That(secondField.Value,
                Is.EqualTo(new System.Numerics.BigInteger(110)));
            Assert.That(inputAligned.ConsumedBitCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void MultiOctetIntegersHaveStableNetworkBitOrder()
    {
        PerBitWriter writer = CreateWriter();
        writer.WriteBits(new System.Numerics.BigInteger(0x0102), 16);
        writer.WriteBits(new System.Numerics.BigInteger(3), 2);
        PerBitWriteResult output = writer.GetOutput();
        PerBitReader reader = CreateReader();
        reader.AppendInputBits(output.Data, 0, 18, true);
        PerBitReadResult integer = reader.ReadBits(16);
        PerBitReadResult suffix = reader.ReadBits(2);

        Assert.Multiple(() =>
        {
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0x01, 0x02, 0xc0 }));
            Assert.That(output.WrittenBitCount, Is.EqualTo(18));
            Assert.That(integer.Value,
                Is.EqualTo(new System.Numerics.BigInteger(0x0102)));
            Assert.That(suffix.Value,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
        });
    }

    [Test]
    public void AlignmentSupportsOctetTransfersAndRejectsWrongPositions()
    {
        PerBitWriter misalignedWriter = CreateWriter();
        misalignedWriter.WriteBit(true);
        PerBitWriteResult writeError = misalignedWriter.WriteAlignedOctets(
            [0x12],
            0,
            1);

        PerBitWriter writer = CreateWriter();
        writer.WriteBit(true);
        writer.AlignOutput();
        PerBitWriteResult octets = writer.WriteAlignedOctets(
            [0x12, 0x34],
            0,
            2);
        PerBitWriteResult output = writer.GetOutput();

        PerBitReader misalignedReader = CreateReader();
        misalignedReader.AppendInputBits(output.Data, 0, 24, true);
        misalignedReader.ReadBit();
        PerBitReadResult readError = misalignedReader.ReadAlignedOctets(2);

        PerBitReader reader = CreateReader();
        reader.AppendInputBits(output.Data, 0, 24, true);
        PerBitReadResult flag = reader.ReadBit();
        PerBitReadResult padding = reader.AlignInput(true);
        PerBitReadResult readOctets = reader.ReadAlignedOctets(2);

        Assert.Multiple(() =>
        {
            Assert.That(writeError.Success, Is.False);
            Assert.That(writeError.ErrorCode, Is.EqualTo("ASNP0002"));
            Assert.That(octets.Success, Is.True);
            Assert.That(output.Data,
                Is.EqualTo(new byte[] { 0x80, 0x12, 0x34 }));
            Assert.That(readError.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(readError.ErrorCode, Is.EqualTo("ASNP0005"));
            Assert.That(flag.Value, Is.EqualTo(System.Numerics.BigInteger.One));
            Assert.That(padding.ConsumedBitCount, Is.EqualTo(7));
            Assert.That(readOctets.Data,
                Is.EqualTo(new byte[] { 0x12, 0x34 }));
            Assert.That(reader.GetAvailableBitCount(), Is.Zero);
        });
    }

    [Test]
    public void ReaderReportsTheExactNonZeroPaddingBit()
    {
        PerBitReader strict = CreateReader();
        strict.AppendInputBits([0xa5], 0, 8, true);
        PerBitReadResult value = strict.ReadBits(3);
        PerBitReadResult padding = strict.AlignInput(true);

        PerBitReader permissive = CreateReader();
        permissive.AppendInputBits([0xa5], 0, 8, true);
        permissive.ReadBits(3);
        PerBitReadResult ignored = permissive.AlignInput(false);

        Assert.Multiple(() =>
        {
            Assert.That(value.Value,
                Is.EqualTo(new System.Numerics.BigInteger(5)));
            Assert.That(padding.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(padding.ErrorCode, Is.EqualTo("ASNP0007"));
            Assert.That(padding.HasViolationBitOffset, Is.True);
            Assert.That(padding.ViolationBitOffset, Is.EqualTo(5));
            Assert.That(strict.GetBitOffset(), Is.EqualTo(3));
            Assert.That(ignored.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(ignored.ConsumedBitCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void FinalInputAndTrailingBitsHaveStableFailureStates()
    {
        PerBitReader truncated = CreateReader();
        truncated.AppendInputBits([0xe0], 0, 3, true);
        PerBitReadResult missing = truncated.ReadBits(4);

        PerBitReader trailing = CreateReader();
        trailing.AppendInputBits([0xff, 0x80], 0, 9, true);
        trailing.ReadBit();
        PerBitReadResult completion = trailing.CompleteInput(true);

        PerBitReader final = CreateReader();
        final.AppendInputBits([], 0, 0, true);
        PerBitReadResult afterFinal = final.AppendInputBits(
            [0x00],
            0,
            1,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(missing.ErrorCode, Is.EqualTo("ASNP0004"));
            Assert.That(missing.ViolationBitOffset, Is.EqualTo(3));
            Assert.That(completion.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(completion.ErrorCode, Is.EqualTo("ASNP0008"));
            Assert.That(afterFinal.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(afterFinal.ErrorCode, Is.EqualTo("ASNP0003"));
        });
    }

    [Test]
    public void ResourceFailuresDoNotPartiallyAdvanceBitState()
    {
        AsnResourceLimits byteLimits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 2,
            MaximumTotalAllocationBytes = 16
        };
        PerBitWriter writer = new PerBitWriter(byteLimits);
        PerBitWriteResult writeLimit = writer.WriteInputBits(
            [0xff, 0xff, 0x80],
            0,
            17);
        PerBitReader reader = new PerBitReader(byteLimits);
        PerBitReadResult readLimit = reader.AppendInputBits(
            [0xff, 0xff, 0x80],
            0,
            17,
            false);

        AsnResourceLimits integerLimits = new AsnResourceLimits
        {
            MaximumIntegerBits = 8
        };
        PerBitWriter integerWriter = new PerBitWriter(integerLimits);
        PerBitWriteResult integerWrite = integerWriter.WriteBits(1, 9);
        PerBitReader integerReader = new PerBitReader(integerLimits);
        integerReader.AppendInputBits([0x00, 0x00], 0, 16, true);
        PerBitReadResult integerRead = integerReader.ReadBits(9);

        AsnResourceLimits allocationLimits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 8,
            MaximumTotalAllocationBytes = 3
        };
        PerBitWriter allocationWriter = new PerBitWriter(allocationLimits);
        PerBitWriteResult allocationWrite = allocationWriter.WriteBit(true);
        PerBitReader allocationReader = new PerBitReader(allocationLimits);
        PerBitReadResult allocationRead = allocationReader.AppendInputBits(
            [0x80],
            0,
            1,
            false);

        int failedWriterBits = writer.GetWrittenBitCount();
        writer.ResetWriter();
        PerBitWriteResult resetWrite = writer.WriteBits(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(writeLimit.Success, Is.False);
            Assert.That(writeLimit.ErrorCode, Is.EqualTo("ASNR0006"));
            Assert.That(failedWriterBits, Is.Zero);
            Assert.That(readLimit.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(readLimit.ErrorCode, Is.EqualTo("ASNR0006"));
            Assert.That(reader.GetBitOffset(), Is.Zero);
            Assert.That(reader.GetAvailableBitCount(), Is.Zero);
            Assert.That(integerWrite.Success, Is.False);
            Assert.That(integerWrite.ErrorCode, Is.EqualTo("ASNR0010"));
            Assert.That(integerWriter.GetWrittenBitCount(), Is.Zero);
            Assert.That(integerRead.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(integerRead.ErrorCode, Is.EqualTo("ASNR0010"));
            Assert.That(integerReader.GetBitOffset(), Is.Zero);
            Assert.That(allocationWrite.Success, Is.False);
            Assert.That(allocationWrite.ErrorCode, Is.EqualTo("ASNR0005"));
            Assert.That(allocationRead.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(allocationRead.ErrorCode, Is.EqualTo("ASNR0005"));
            Assert.That(resetWrite.Success, Is.True);
        });
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
