using asn1;

namespace asn1.Tests;

public class OerLengthCodecTests
{
    [TestCase(0L, new byte[] { 0x00 })]
    [TestCase(127L, new byte[] { 0x7f })]
    [TestCase(128L, new byte[] { 0x81, 0x80 })]
    [TestCase(255L, new byte[] { 0x81, 0xff })]
    [TestCase(256L, new byte[] { 0x82, 0x01, 0x00 })]
    [TestCase(65535L, new byte[] { 0x82, 0xff, 0xff })]
    [TestCase(65536L, new byte[] { 0x83, 0x01, 0x00, 0x00 })]
    public void WriterUsesShortestShortAndLongForms(
        long length,
        byte[] expected)
    {
        OerLengthResult result = Codec().WriteLength(length);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.Length, Is.EqualTo(length));
            Assert.That(result.Data, Is.EqualTo(expected));
        });
    }

    [Test]
    public void WriterSupportsTheMaximumSignedLength()
    {
        OerLengthResult result = Codec().WriteLength(long.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.EqualTo(new byte[]
            {
                0x88,
                0x7f,
                0xff,
                0xff,
                0xff,
                0xff,
                0xff,
                0xff,
                0xff
            }));
        });
    }

    [Test]
    public void ReaderConsumesOnlyOneCompleteDeterminant()
    {
        OerLengthResult shortForm = Codec().ConsumeLength(
            [0x7f, 0xaa],
            0,
            2,
            true,
            false);
        OerLengthResult longForm = Codec().ConsumeLength(
            [0x83, 0x01, 0x00, 0x00, 0xaa],
            0,
            5,
            true,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(shortForm.Success, Is.True);
            Assert.That(shortForm.Length, Is.EqualTo(127));
            Assert.That(shortForm.ConsumedCount, Is.EqualTo(1));
            Assert.That(shortForm.TotalConsumedCount, Is.EqualTo(1));
            Assert.That(longForm.Success, Is.True);
            Assert.That(longForm.Length, Is.EqualTo(65536));
            Assert.That(longForm.ConsumedCount, Is.EqualTo(4));
            Assert.That(longForm.TotalConsumedCount, Is.EqualTo(4));
            Assert.That(longForm.Minimal, Is.True);
        });
    }

    [Test]
    public void ReaderContinuesAcrossEveryLongFormOctet()
    {
        OerLengthCodec codec = Codec();
        OerLengthResult header = codec.ConsumeLength(
            [0x83],
            0,
            1,
            false,
            false);
        OerLengthResult first = codec.ConsumeLength(
            [0x01],
            0,
            1,
            false,
            false);
        OerLengthResult second = codec.ConsumeLength(
            [0x00],
            0,
            1,
            false,
            false);
        OerLengthResult done = codec.ConsumeLength(
            [0x00, 0xaa],
            0,
            2,
            true,
            false);
        OerLengthResult terminal = codec.ConsumeLength(
            [0x01],
            0,
            1,
            true,
            false);
        codec.ResetLength();
        OerLengthResult reset = codec.ConsumeLength(
            [0x05],
            0,
            1,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(header.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(first.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(second.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(header.TotalConsumedCount, Is.EqualTo(1));
            Assert.That(first.TotalConsumedCount, Is.EqualTo(2));
            Assert.That(second.TotalConsumedCount, Is.EqualTo(3));
            Assert.That(done.Success, Is.True);
            Assert.That(done.Length, Is.EqualTo(65536));
            Assert.That(done.ConsumedCount, Is.EqualTo(1));
            Assert.That(done.TotalConsumedCount, Is.EqualTo(4));
            Assert.That(terminal.ErrorCode, Is.EqualTo("ASNO0008"));
            Assert.That(reset.Success, Is.True);
            Assert.That(reset.Length, Is.EqualTo(5));
        });
    }

    [Test]
    public void CompletionRejectsTruncatedLongFormAtNextOctet()
    {
        OerLengthCodec codec = Codec();
        OerLengthResult waiting = codec.ConsumeLength(
            [0x82, 0x01],
            0,
            2,
            false,
            true);
        OerLengthResult completed = codec.CompleteLength(true);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(completed.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(completed.ErrorCode, Is.EqualTo("ASNO0003"));
            Assert.That(completed.HasViolationOffset, Is.True);
            Assert.That(completed.ViolationOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void CoerRejectsNonminimalFormsAcceptedByBasicOer()
    {
        OerLengthResult basicSmall = Codec().ConsumeLength(
            [0x81, 0x7f],
            0,
            2,
            true,
            false);
        OerLengthResult canonicalSmall = Codec().ConsumeLength(
            [0x81, 0x7f],
            0,
            2,
            true,
            true);
        OerLengthResult basicLeading = Codec().ConsumeLength(
            [0x82, 0x00, 0x80],
            0,
            3,
            true,
            false);
        OerLengthResult canonicalLeading = Codec().ConsumeLength(
            [0x82, 0x00, 0x80],
            0,
            3,
            true,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(basicSmall.Success, Is.True);
            Assert.That(basicSmall.Length, Is.EqualTo(127));
            Assert.That(basicSmall.Minimal, Is.False);
            Assert.That(canonicalSmall.ErrorCode, Is.EqualTo("ASNO0004"));
            Assert.That(canonicalSmall.ViolationOffset, Is.Zero);
            Assert.That(basicLeading.Success, Is.True);
            Assert.That(basicLeading.Length, Is.EqualTo(128));
            Assert.That(basicLeading.Minimal, Is.False);
            Assert.That(canonicalLeading.ErrorCode, Is.EqualTo("ASNO0004"));
            Assert.That(canonicalLeading.ViolationOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReservedEmptyLongFormAndOverflowAreRejected()
    {
        OerLengthResult empty = Codec().ConsumeLength(
            [0x80],
            0,
            1,
            true,
            false);
        OerLengthResult overflow = Codec().ConsumeLength(
            [0x88, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            0,
            9,
            true,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(empty.ErrorCode, Is.EqualTo("ASNO0002"));
            Assert.That(empty.ViolationOffset, Is.Zero);
            Assert.That(overflow.ErrorCode, Is.EqualTo("ASNO0005"));
            Assert.That(overflow.ViolationOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void LengthAndAllocationLimitsFailBeforeValueBuffering()
    {
        OerLengthCodec lengthCodec = new OerLengthCodec(new AsnResourceLimits
        {
            MaximumLengthOctets = 2,
            MaximumTotalAllocationBytes = 100
        });
        OerLengthResult readLengthLimit = lengthCodec.ConsumeLength(
            [0x82],
            0,
            1,
            false,
            false);
        OerLengthResult writeLengthLimit = new OerLengthCodec(
            new AsnResourceLimits
            {
                MaximumLengthOctets = 2,
                MaximumTotalAllocationBytes = 100
            }).WriteLength(256);
        OerLengthResult allocationLimit = new OerLengthCodec(
            new AsnResourceLimits
            {
                MaximumLengthOctets = 9,
                MaximumTotalAllocationBytes = 1
            }).ConsumeLength(
                [0x82],
                0,
                1,
                false,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(readLengthLimit.ErrorCode, Is.EqualTo("ASNR0003"));
            Assert.That(readLengthLimit.LimitExceeded, Is.True);
            Assert.That(readLengthLimit.ViolationOffset, Is.Zero);
            Assert.That(writeLengthLimit.ErrorCode, Is.EqualTo("ASNR0003"));
            Assert.That(writeLengthLimit.LimitExceeded, Is.True);
            Assert.That(allocationLimit.ErrorCode, Is.EqualTo("ASNR0005"));
            Assert.That(allocationLimit.LimitExceeded, Is.True);
            Assert.That(allocationLimit.ViolationOffset, Is.Zero);
        });
    }

    [Test]
    public void NegativeLengthsAreRejectedWithoutOutput()
    {
        OerLengthResult result = Codec().WriteLength(-1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Data, Is.Empty);
            Assert.That(result.ErrorCode, Is.EqualTo("ASNO0001"));
        });
    }

    private static OerLengthCodec Codec()
    {
        return new OerLengthCodec(new AsnResourceLimits
        {
            MaximumLengthOctets = 9
        });
    }
}
