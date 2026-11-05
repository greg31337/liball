using icc;

namespace icc.Tests;

public class TagTableCodecTests
{
    [Test]
    public void TableOrderSharingAndPaddingArePreservedIndependentlyOfPayloadOrder()
    {
        byte[] source = CreateProfile(5, 188, 3);
        WriteEntry(source, 5, 0, IccTagSignatures.ProfileDescription, 180, 8);
        WriteEntry(source, 5, 1, IccTagSignatures.Copyright, 168, 9);
        WriteEntry(source, 5, 2, IccTagSignatures.MediaWhitePoint, 168, 9);
        source[5 + 168] = 1;
        source[5 + 176] = 5;
        source[5 + 177] = 0xa1;
        source[5 + 178] = 0xa2;
        source[5 + 179] = 0xa3;
        source[5 + 180] = 6;
        source[5 + 187] = 13;
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Conforming;
        options.ResourceLimits.MaximumTotalTagBytes = 17;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 5, 1000L, options);
        source[5 + 177] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(profile.TagTableEntries, Has.Length.EqualTo(3));
            Assert.That(profile.Tags, Has.Length.EqualTo(3));
            Assert.That(profile.TagTableEntries[0].Signature, Is.EqualTo(IccTagSignatures.ProfileDescription));
            Assert.That(profile.TagTableEntries[1].Signature, Is.EqualTo(IccTagSignatures.Copyright));
            Assert.That(profile.TagTableEntries[2].Signature, Is.EqualTo(IccTagSignatures.MediaWhitePoint));
            Assert.That(profile.TagTableEntries[0].OriginalIndex, Is.Zero);
            Assert.That(profile.TagTableEntries[1].OriginalIndex, Is.EqualTo(1));
            Assert.That(profile.TagTableEntries[2].OriginalIndex, Is.EqualTo(2));
            Assert.That(profile.TagTableEntries[0].Offset, Is.EqualTo(1185));
            Assert.That(profile.TagTableEntries[1].Offset, Is.EqualTo(1173));
            Assert.That(profile.TagTableEntries[2].Offset, Is.EqualTo(1173));
            Assert.That(profile.TagTableEntries[0].Size, Is.EqualTo(8));
            Assert.That(profile.TagTableEntries[1].Size, Is.EqualTo(9));
            Assert.That(profile.TagTableEntries[2].Size, Is.EqualTo(9));
            Assert.That(profile.TagTableEntries[0].SharedDataId, Is.EqualTo(-1));
            Assert.That(profile.TagTableEntries[1].SharedDataId, Is.Zero);
            Assert.That(profile.TagTableEntries[2].SharedDataId, Is.Zero);
            Assert.That(profile.Tags[0].Signature, Is.EqualTo(IccTagSignatures.ProfileDescription));
            Assert.That(profile.Tags[1].Signature, Is.EqualTo(IccTagSignatures.Copyright));
            Assert.That(profile.Tags[2].Signature, Is.EqualTo(IccTagSignatures.MediaWhitePoint));
            Assert.That(profile.Tags[1].TableEntry, Is.SameAs(profile.TagTableEntries[1]));
            Assert.That(profile.Tags[1].PaddingBytes, Is.EqualTo(new byte[] { 0xa1, 0xa2, 0xa3 }));
            Assert.That(profile.Tags[2].PaddingBytes, Is.EqualTo(new byte[] { 0xa1, 0xa2, 0xa3 }));
            Assert.That(profile.Tags[1].PaddingBytes, Is.Not.SameAs(profile.Tags[2].PaddingBytes));
            Assert.That(profile.Tags[0].PaddingBytes, Is.Empty);
            Assert.That(profile.Tags[0].IsDecodeAttempted, Is.False);
            Assert.That(profile.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Unknown));
            Assert.That(profile.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(profile.ValidationIssues[0].RuleId, Is.EqualTo(IccValidationRuleIds.TagTablePadding));
            Assert.That(profile.ValidationIssues[0].Severity, Is.EqualTo(IccValidationSeverity.Warning));
        });
    }

    [Test]
    public void UnsignedTagCountIsRejectedBeforeAllocation()
    {
        byte[] source = CreateProfile(0, 132, uint.MaxValue);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 200L, new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("tag count"));
            Assert.That(exception.Message, Does.Contain("4294967295"));
            Assert.That(exception.Message, Does.Contain("byte offset 328"));
        });
    }

    [Test]
    public void TagCountCannotExceedConfiguredLimit()
    {
        byte[] source = CreateProfile(0, 156, 2);
        IccReadOptions options = new IccReadOptions();
        options.ResourceLimits.MaximumTagCount = 1;

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, options))!;

        Assert.That(exception.Message, Does.Contain("tag count 2").And.Contain("configured maximum of 1"));
    }

    [Test]
    public void TagTableEntriesMustFitInsideDeclaredProfile()
    {
        byte[] source = CreateProfile(0, 132, 1);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, new IccReadOptions()))!;

        Assert.That(exception.Message, Does.Contain("requires 16 bytes").And.Contain("extending beyond the 132-byte profile"));
    }

    [Test]
    public void NearMaximumTagTableMathFailsWithAProfileFormatError()
    {
        byte[] source = CreateProfile(0, 132, 178956970);
        IccReadOptions options = new IccReadOptions();
        options.ResourceLimits.MaximumTagCount = int.MaxValue;

        Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, options));
    }

    [Test]
    public void DuplicateTagSignaturesAreRejected()
    {
        byte[] source = CreateProfile(0, 172, 2);
        WriteEntry(source, 0, 0, IccTagSignatures.Copyright, 156, 8);
        WriteEntry(source, 0, 1, IccTagSignatures.Copyright, 164, 8);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, new IccReadOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("entries 0 and 1"));
            Assert.That(exception.Message, Does.Contain("duplicate signature"));
            Assert.That(exception.Message, Does.Contain("0x63707274"));
        });
    }

    [Test]
    public void PayloadOffsetMustBeFourByteAligned()
    {
        byte[] source = CreateProfile(0, 148, 1);
        WriteEntry(source, 0, 0, IccTagSignatures.Copyright, 145, 3);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, new IccReadOptions()))!;

        Assert.That(exception.Message, Does.Contain("unaligned payload offset 145").And.Contain("four-byte boundary"));
    }

    [Test]
    public void PayloadCannotStartInsideTagTable()
    {
        byte[] source = CreateProfile(0, 148, 1);
        WriteEntry(source, 0, 0, IccTagSignatures.Copyright, 140, 8);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, new IccReadOptions()))!;

        Assert.That(exception.Message, Does.Contain("profile offset 140").And.Contain("tag table ending at offset 144"));
    }

    [Test]
    public void PayloadRangeMustRemainInsideDeclaredProfile()
    {
        byte[] source = CreateProfile(0, 148, 1);
        WriteEntry(source, 0, 0, IccTagSignatures.Copyright, 144, 8);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 0L, new IccReadOptions()))!;

        Assert.That(exception.Message, Does.Contain("offsets 144 through 152").And.Contain("declared profile size 148"));
    }

    [Test]
    public void PartiallyOverlappingPayloadsAreRejected()
    {
        byte[] source = CreateProfile(0, 168, 2);
        WriteEntry(source, 0, 0, IccTagSignatures.Copyright, 156, 8);
        WriteEntry(source, 0, 1, IccTagSignatures.ProfileDescription, 160, 8);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 500L, new IccReadOptions()))!;

        Assert.That(exception.Message, Does.Contain("entry 1").And.Contain("overlaps").And.Contain("byte offset 660"));
    }

    [TestCase(148u, 156, "before")]
    [TestCase(144u, 156, "after")]
    public void GapsBeforeOrAfterPayloadsAreRejected(uint payloadOffset, int declaredSize, string position)
    {
        byte[] source = CreateProfile(0, declaredSize, 1);
        WriteEntry(source, 0, 0, IccTagSignatures.Copyright, payloadOffset, 8);

        IccProfileFormatException exception = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(source, 0, 900L, new IccReadOptions()))!;

        Assert.That(exception.Message, Does.Contain("4-byte gap").And.Contain(position));
    }

    [Test]
    public void UniquePayloadTotalAndIndividualSizeAreBounded()
    {
        byte[] totalSource = CreateProfile(0, 172, 2);
        WriteEntry(totalSource, 0, 0, IccTagSignatures.Copyright, 156, 8);
        WriteEntry(totalSource, 0, 1, IccTagSignatures.ProfileDescription, 164, 8);
        IccReadOptions totalOptions = new IccReadOptions();
        totalOptions.ResourceLimits.MaximumTotalTagBytes = 15;
        byte[] individualSource = CreateProfile(0, 152, 1);
        WriteEntry(individualSource, 0, 0, IccTagSignatures.Copyright, 144, 8);
        IccReadOptions individualOptions = new IccReadOptions();
        individualOptions.ResourceLimits.MaximumTagBytes = 7;

        IccProfileFormatException totalException = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(totalSource, 0, 0L, totalOptions))!;
        IccProfileFormatException individualException = Assert.Throws<IccProfileFormatException>(() => ProfileCodec.ReadEnvelope(individualSource, 0, 0L, individualOptions))!;

        Assert.Multiple(() =>
        {
            Assert.That(totalException.Message, Does.Contain("total 16 bytes").And.Contain("maximum of 15"));
            Assert.That(individualException.Message, Does.Contain("payload size 8").And.Contain("maximum of 7"));
        });
    }

    private static byte[] CreateProfile(int prefixSize, int declaredSize, uint tagCount)
    {
        byte[] source = new byte[prefixSize + declaredSize];
        WriteUInt32(source, prefixSize, (uint)declaredSize);
        WriteUInt32(source, prefixSize + 8, 0x04400000);
        WriteUInt32(source, prefixSize + 12, IccProfileClassSignatures.Display);
        WriteUInt32(source, prefixSize + 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(source, prefixSize + 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(source, prefixSize + 36, IccConstants.ProfileFileSignature);
        WriteUInt32(source, prefixSize + 128, tagCount);
        return source;
    }

    private static void WriteEntry(byte[] destination, int profileOffset, int entryIndex, uint signature, uint payloadOffset, uint payloadSize)
    {
        int offset = profileOffset + 132 + (entryIndex * 12);
        WriteUInt32(destination, offset, signature);
        WriteUInt32(destination, offset + 4, payloadOffset);
        WriteUInt32(destination, offset + 8, payloadSize);
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
