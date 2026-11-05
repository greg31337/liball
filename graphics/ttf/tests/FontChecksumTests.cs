using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

public class FontChecksumTests
{
    [Test]
    public void EmptyDataHasZeroChecksum()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.CalculateTable(ReadOnlySpan<byte>.Empty), Is.EqualTo(0U));
            Assert.That(FontChecksum.CalculateFont(ReadOnlySpan<byte>.Empty), Is.EqualTo(0U));
        });
    }

    [TestCase(new byte[] { 0x01 }, 0x01000000U)]
    [TestCase(new byte[] { 0x01, 0x02 }, 0x01020000U)]
    [TestCase(new byte[] { 0x01, 0x02, 0x03 }, 0x01020300U)]
    [TestCase(new byte[] { 0x01, 0x02, 0x03, 0x04 }, 0x01020304U)]
    [TestCase(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, 0x06020304U)]
    public void TableChecksumTreatsTailAsBigEndianZeroPaddedWord(byte[] data, uint expected)
    {
        Assert.That(FontChecksum.CalculateTable(data), Is.EqualTo(expected));
    }

    [Test]
    public void ChecksumWrapsAtUnsignedThirtyTwoBits()
    {
        byte[] data = new byte[]
        {
            0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF
        };

        Assert.That(FontChecksum.CalculateTable(data), Is.EqualTo(0xFFFFFFFEU));
    }

    [Test]
    public void WholeFontChecksumUsesSamePaddedUnsignedSum()
    {
        byte[] data = new byte[]
        {
            0x12, 0x34, 0x56, 0x78,
            0x90, 0xAB, 0xCD
        };

        uint expected = 0xA2E02378U;

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.CalculateFont(data), Is.EqualTo(expected));
            Assert.That(FontChecksum.CalculateTable(data), Is.EqualTo(expected));
        });
    }

    [Test]
    public void HeadTableChecksumTreatsAdjustmentAsZero()
    {
        byte[] head = new byte[]
        {
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x02,
            0xDE, 0xAD, 0xBE, 0xEF,
            0x00, 0x00, 0x00, 0x03
        };

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.CalculateTable(head), Is.EqualTo(0xDEADBEF5U));
            Assert.That(FontChecksum.CalculateHeadTable(head), Is.EqualTo(6U));
        });
    }

    [Test]
    public void HeadChecksumDoesNotMutateInput()
    {
        byte[] head = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(8, 4), 0x12345678U);
        byte[] before = head.ToArray();

        FontChecksum.CalculateHeadTable(head);

        Assert.That(head, Is.EqualTo(before));
    }

    [Test]
    public void CalculatesAndVerifiesWholeFontAdjustment()
    {
        byte[] font = new byte[20];
        int headOffset = 4;
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(0, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(4, 4), (uint)FontTags.Head);
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(8, 4), 0x11223344U);
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(12, 4), 0xDEADBEEFU);
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(16, 4), 0x55667788U);

        uint zeroedChecksum = 0xCEEF0C30U;
        uint adjustment = FontChecksum.WholeFontChecksum - zeroedChecksum;

        Assert.Multiple(() =>
        {
            Assert.That(
                FontChecksum.CalculateFontWithZeroedHeadAdjustment(font, headOffset),
                Is.EqualTo(zeroedChecksum));
            Assert.That(
                FontChecksum.CalculateChecksumAdjustment(font, headOffset),
                Is.EqualTo(adjustment));
        });

        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(headOffset + 8, 4), adjustment);

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.CalculateFont(font), Is.EqualTo(FontChecksum.WholeFontChecksum));
            Assert.That(FontChecksum.VerifyFont(font), Is.True);
        });
    }

    [Test]
    public void ExistingHeadAdjustmentIsIgnoredWhenCalculatingReplacement()
    {
        byte[] first = new byte[16];
        byte[] second = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(first.AsSpan(8, 4), 0x11111111U);
        BinaryPrimitives.WriteUInt32BigEndian(second.AsSpan(8, 4), 0xEEEEEEEEU);

        Assert.That(
            FontChecksum.CalculateChecksumAdjustment(first, 0),
            Is.EqualTo(FontChecksum.CalculateChecksumAdjustment(second, 0)));
    }

    [Test]
    public void ChecksumAdjustmentSubtractionWrapsModuloThirtyTwoBits()
    {
        byte[] font = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(0, 4), 0xFFFFFFFFU);

        uint adjustment = FontChecksum.CalculateChecksumAdjustment(font, 0);

        Assert.That(adjustment, Is.EqualTo(0xB1B0AFBBU));

        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(8, 4), adjustment);

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.CalculateFont(font), Is.EqualTo(FontChecksum.WholeFontChecksum));
            Assert.That(FontChecksum.VerifyFont(font), Is.True);
        });
    }

    [Test]
    public void VerificationHelpersDistinguishMatchesAndMismatches()
    {
        byte[] table = new byte[] { 1, 2, 3, 4, 5 };
        byte[] head = new byte[12];
        BinaryPrimitives.WriteUInt32BigEndian(head.AsSpan(8, 4), 0xFFFFFFFFU);
        uint tableChecksum = FontChecksum.CalculateTable(table);
        uint headChecksum = FontChecksum.CalculateHeadTable(head);

        Assert.Multiple(() =>
        {
            Assert.That(FontChecksum.VerifyTable(table, tableChecksum), Is.True);
            Assert.That(FontChecksum.VerifyTable(table, tableChecksum + 1), Is.False);
            Assert.That(FontChecksum.VerifyHeadTable(head, headChecksum), Is.True);
            Assert.That(FontChecksum.VerifyHeadTable(head, headChecksum + 1), Is.False);
            Assert.That(FontChecksum.VerifyFont(new byte[4]), Is.False);
        });
    }

    [Test]
    public void HeadChecksumRejectsTruncatedAdjustmentField()
    {
        Assert.Throws<FontFormatException>(() => FontChecksum.CalculateHeadTable(new byte[11]));
    }

    [TestCase(-1)]
    [TestCase(1)]
    [TestCase(int.MaxValue)]
    public void WholeFontHeadHandlingRejectsOutOfBoundsOffset(int headOffset)
    {
        byte[] font = new byte[12];

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() =>
                FontChecksum.CalculateFontWithZeroedHeadAdjustment(font, headOffset));
            Assert.Throws<FontFormatException>(() =>
                FontChecksum.CalculateChecksumAdjustment(font, headOffset));
        });
    }

    [Test]
    public void WholeFontHeadHandlingAcceptsFieldAtFinalBoundary()
    {
        byte[] font = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(12, 4), 0xFFFFFFFFU);

        Assert.That(
            FontChecksum.CalculateFontWithZeroedHeadAdjustment(font, 4),
            Is.EqualTo(0U));
    }
}
