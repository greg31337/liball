using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffRawUnknownTagPreserverTests
{
    [Test]
    public void PreserveEntries_PreservesUnknownInlineTagAsTypedRawValue()
    {
        TiffIfdEntry entry = CreateInlineEntry(65000, TiffDataType.Short, 2, new byte[] { 1, 0, 2, 0 });

        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreserver.PreserveEntries(new TiffIfdEntry[] { entry }, new byte[0]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ValueCount(), Is.EqualTo(1));
        Assert.That(result.Values[0].TagId, Is.EqualTo(65000));
        Assert.That(result.Values[0].DataType, Is.EqualTo(TiffDataType.Short));
        Assert.That(result.Values[0].Count, Is.EqualTo(2));
        Assert.That(result.Values[0].IsRaw, Is.True);
        Assert.That(result.Values[0].RawBytes, Is.EqualTo(new byte[] { 1, 0, 2, 0 }));
    }

    [Test]
    public void PreserveEntries_PreservesUnknownExternalTagBytes()
    {
        byte[] fileBytes = new byte[] { 0, 1, 2, 3, 10, 11, 12, 13, 99 };
        TiffIfdEntry entry = CreateExternalEntry(65001, TiffDataType.Byte, 5, 4);

        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreserver.PreserveEntries(new TiffIfdEntry[] { entry }, fileBytes);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ValueCount(), Is.EqualTo(1));
        Assert.That(result.Values[0].Count, Is.EqualTo(5));
        Assert.That(result.Values[0].RawBytes, Is.EqualTo(new byte[] { 10, 11, 12, 13, 99 }));
    }

    [Test]
    public void PreserveDirectory_SkipsKnownTags()
    {
        TiffDirectory directory = TiffDirectory.Create(0, 8, 0, new TiffIfdEntry[] { CreateInlineEntry(TiffTagId.ImageWidth, TiffDataType.Long, 1, new byte[] { 100, 0, 0, 0 }) });

        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreserver.PreserveDirectory(directory, new byte[0]);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ValueCount(), Is.EqualTo(0));
    }

    [Test]
    public void PreserveEntries_SkipsCallerRegisteredCustomTags()
    {
        TiffReadOptions options = new TiffReadOptions();
        options.RegisterTagDefinition(TiffTagDefinition.Create(65002, "PrivateKnown", TiffDataType.Byte, new TiffDataType[] { TiffDataType.Byte }, TiffTagDefinition.AnyCount));
        TiffIfdEntry entry = CreateInlineEntry(65002, TiffDataType.Byte, 4, new byte[] { 1, 2, 3, 4 });

        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreserver.PreserveEntries(new TiffIfdEntry[] { entry }, new byte[0], options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ValueCount(), Is.EqualTo(0));
        Assert.That(TiffRawUnknownTagPreserver.IsUnknownTag(entry, options), Is.False);
    }

    [Test]
    public void PreserveEntries_RespectsPreserveUnknownTagsOption()
    {
        TiffReadOptions options = new TiffReadOptions();
        options.PreserveUnknownTags = false;
        TiffIfdEntry entry = CreateInlineEntry(65003, TiffDataType.Byte, 4, new byte[] { 1, 2, 3, 4 });

        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreserver.PreserveEntries(new TiffIfdEntry[] { entry }, new byte[0], options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ValueCount(), Is.EqualTo(0));
        Assert.That(TiffRawUnknownTagPreserver.ShouldPreserveUnknownTags(options), Is.False);
    }

    [Test]
    public void PreserveEntries_MergesLoadDiagnosticsForInvalidExternalRange()
    {
        TiffIfdEntry entry = CreateExternalEntry(65004, TiffDataType.Byte, 5, 8);

        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreserver.PreserveEntries(new TiffIfdEntry[] { entry }, new byte[] { 0, 1, 2, 3 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ValueCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
    }

    [Test]
    public void PreserveEntries_ReportsInvalidDataType()
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = 65005;
        entry.DataType = TiffDataType.Unknown;
        entry.Count = 0;
        entry.RawCount = 0;
        entry.ValueByteCount = 0;
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];

        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreserver.PreserveEntries(new TiffIfdEntry[] { entry }, new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ValueCount(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(result.Diagnostics.Get(0).TagId, Is.EqualTo(65005));
    }

    [Test]
    public void PreserveDirectory_ReportsNullDirectory()
    {
        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreserver.PreserveDirectory(null!, new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    [Test]
    public void ResultAndAppendCopyMutableValues()
    {
        TiffTagValue value = TiffTagValue.CreateRaw(65006, TiffDataType.Byte, 2, new byte[] { 1, 2 });

        TiffRawUnknownTagPreservationResult result = TiffRawUnknownTagPreservationResult.Create(new TiffTagValue[] { value }, new TiffDiagnostics());
        TiffTagValue[] appended = TiffRawUnknownTagPreserver.AppendValue(new TiffTagValue[0], value);
        value.RawBytes[0] = 9;

        Assert.That(result.Values[0].RawBytes, Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(appended[0].RawBytes, Is.EqualTo(new byte[] { 1, 2 }));
    }

    static TiffIfdEntry CreateInlineEntry(int tagId, TiffDataType dataType, long count, byte[] valueOffsetBytes)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = 0;
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = valueOffsetBytes.Length;
        entry.ValueOffsetBytes = valueOffsetBytes;
        return entry;
    }

    static TiffIfdEntry CreateExternalEntry(int tagId, TiffDataType dataType, long count, ulong offset)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = tagId;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = offset;
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = 4;
        entry.ValueOffsetBytes = new byte[4];
        return entry;
    }
}
