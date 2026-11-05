using System.Buffers.Binary;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicTagApiTests
{
    const int CustomBase = 64000;
    const int MutationTag = 64800;
    const int RemovedTag = 64801;
    const int OrderedFirstTag = 64810;
    const int OrderedSecondTag = 64811;
    const int ValidatedTag = 64820;
    const int NestedPointerPlaceholder = 64830;
    const int DateTimeOriginal = 36867;
    const int GpsAltitude = 6;

    [Test]
    public void BigTiff_PublicTagAccessRoundTripsEveryFieldType()
    {
        TiffTagValue[] expected = CreateAllFieldTypeValues();
        TiffWriteOptions writeOptions = CreateBigTiffOptions(expected);
        byte[] bytes = CreateFile(writeOptions, expected);
        TiffReadOptions readOptions = new TiffReadOptions();
        RegisterDefinitions(readOptions.TagRegistry, expected);
        TiffImage image = TiffApi.Factory.OpenReadBytes(bytes, readOptions).GetImage(0);

        TiffDataType[] actualTypes = expected.Select(value => image.GetTagValue(value.TagId)!.DataType).ToArray();

        Assert.That(actualTypes, Is.EqualTo(expected.Select(value => value.DataType).ToArray()));
        Assert.That(image.GetUnsignedTagScalar(CustomBase), Is.EqualTo(7));
        Assert.That(image.GetAsciiTagArray(CustomBase + 1), Is.EqualTo(new string[] { "one", "a longer second value" }));
        Assert.That(image.GetUnsignedTagArray(CustomBase + 2), Is.EqualTo(new ulong[] { 3, 4 }));
        Assert.That(image.GetUnsignedTagScalar(CustomBase + 3), Is.EqualTo(4000000000));
        Assert.That(image.GetRationalTagArray(CustomBase + 4).Select(value => value.Numerator), Is.EqualTo(new long[] { 1, 3 }));
        Assert.That(image.GetRationalTagArray(CustomBase + 4).Select(value => value.Denominator), Is.EqualTo(new long[] { 2, 4 }));
        Assert.That(image.GetSignedTagScalar(CustomBase + 5), Is.EqualTo(-2));
        Assert.That(image.GetTagValue(CustomBase + 6)!.RawBytes, Is.EqualTo(new byte[] { 9, 8, 7 }));
        Assert.That(image.GetSignedTagArray(CustomBase + 7), Is.EqualTo(new long[] { -3, 4 }));
        Assert.That(image.GetSignedTagScalar(CustomBase + 8), Is.EqualTo(-100000));
        Assert.That(image.GetRationalTagScalar(CustomBase + 9).Numerator, Is.EqualTo(-1));
        Assert.That(image.GetRationalTagScalar(CustomBase + 9).Denominator, Is.EqualTo(2));
        Assert.That(image.GetFloatingTagArray(CustomBase + 10), Is.EqualTo(new double[] { 1.5, -2.5 }).Within(0.00001));
        Assert.That(image.GetFloatingTagScalar(CustomBase + 11), Is.EqualTo(3.25));
        Assert.That(image.GetUnsignedTagArray(CustomBase + 12), Is.EqualTo(new ulong[] { 12, 34 }));
        Assert.That(image.GetUnsignedTagScalar(CustomBase + 13), Is.EqualTo(5000000000));
        Assert.That(image.GetSignedTagArray(CustomBase + 14), Is.EqualTo(new long[] { -5000000000, 5000000000 }));
        Assert.That(image.GetUnsignedTagScalar(CustomBase + 15), Is.EqualTo(6000000000));
        Assert.That(image.GetSuccess(), Is.True);
    }

    [Test]
    public void InlineExternalAsciiAndRationalValues_PreserveEncodingAndLazyState()
    {
        TiffTagValue[] values = CreateAllFieldTypeValues();
        byte[] bytes = CreateFile(CreateBigTiffOptions(values), values);
        TiffImage image = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions()).GetImage(0);
        TiffIfdEntry inlineEntry = image.GetTag(CustomBase)!;
        TiffIfdEntry externalEntry = image.GetTag(CustomBase + 1)!;

        Assert.That(inlineEntry.ValueByteCount, Is.LessThanOrEqualTo(inlineEntry.ValueSlotByteCount));
        Assert.That(externalEntry.ValueByteCount, Is.GreaterThan(externalEntry.ValueSlotByteCount));
        Assert.That(image.GetIsTagValueLoaded(CustomBase + 1), Is.False);

        byte[] asciiBytes = image.GetRawTagBytes(CustomBase + 1);
        TiffRational[] rationals = image.GetRationalTagArray(CustomBase + 4);

        Assert.That(image.GetIsTagValueLoaded(CustomBase + 1), Is.True);
        Assert.That(asciiBytes[^1], Is.EqualTo((byte)0));
        Assert.That(asciiBytes.Count(value => value == 0), Is.EqualTo(2));
        Assert.That(rationals[0].Numerator, Is.EqualTo(1));
        Assert.That(rationals[0].Denominator, Is.EqualTo(2));
        Assert.That(rationals[1].Numerator, Is.EqualTo(3));
        Assert.That(rationals[1].Denominator, Is.EqualTo(4));
    }

    [Test]
    public void DefaultsCustomUnknownAndMutation_AreVisibleThroughPublicImageAccess()
    {
        TiffTagValue initial = TiffApi.Data.CreateAsciiTag(MutationTag, "initial");
        TiffTagValue removed = TiffApi.Data.CreateUnsignedScalarTag(RemovedTag, TiffDataType.Long, 9);
        TiffWriteOptions options = new TiffWriteOptions();
        options.TagRegistry.RegisterKnown(MutationTag, "Mutation", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        options.TagRegistry.RegisterKnown(RemovedTag, "Removed", TiffDataType.Long, 1);
        TiffWriter writer = TiffApi.Factory.CreateWriter(options);
        int imageIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(Combine(CreateRequiredTags(), new TiffTagValue[] { initial, removed })));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1 });

        Assert.That(writer.WriteTag(imageIndex, TiffApi.Data.CreateAsciiTag(MutationTag, "replaced")).HasErrors(), Is.False);
        Assert.That(writer.UpdateMetadata(imageIndex, new TiffTagValue[] { TiffApi.Data.CreateAsciiTag(MutationTag, "updated") }).HasErrors(), Is.False);
        Assert.That(writer.RemoveMetadata(imageIndex, new int[] { RemovedTag }).HasErrors(), Is.False);
        Assert.That(writer.Flush().HasErrors(), Is.False);
        TiffImage image = TiffApi.Factory.OpenReadBytes(writer.GetFileBytes(), new TiffReadOptions()).GetImage(0);

        TiffTagDefaultResult compression = image.GetTagDefaulted(TiffTagId.Compression);
        TiffTagDefaultResult orientation = image.GetTagDefaulted(TiffTagId.Orientation);
        TiffTagValue detached = image.GetTagValue(MutationTag)!;
        detached.Text = "changed by caller";

        Assert.That(compression.Found, Is.True);
        Assert.That(compression.UsedDefault, Is.True);
        Assert.That(compression.Value.UnsignedValues, Is.EqualTo(new ulong[] { (ulong)TiffCompression.None }));
        Assert.That(orientation.Found, Is.True);
        Assert.That(orientation.UsedDefault, Is.True);
        Assert.That(orientation.Value.UnsignedValues, Is.EqualTo(new ulong[] { (ulong)TiffOrientation.TopLeft }));
        Assert.That(image.GetAsciiTagScalar(MutationTag), Is.EqualTo("updated"));
        Assert.That(image.GetTagValue(RemovedTag), Is.Null);
        Assert.That(image.GetTagValue(MutationTag)!.Text, Is.EqualTo("updated"));
    }

    [Test]
    public void OriginalDirectoryOrder_IsPreservedForTypedAndRawTagAccess()
    {
        TiffTagValue first = TiffApi.Data.CreateUnsignedScalarTag(OrderedFirstTag, TiffDataType.Byte, 17);
        TiffTagValue second = TiffApi.Data.CreateUnsignedScalarTag(OrderedSecondTag, TiffDataType.Byte, 29);
        TiffTagValue[] custom = new TiffTagValue[] { first, second };
        byte[] bytes = CreateFile(CreateBigTiffOptions(custom), custom);
        TiffFile initialFile = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions());
        TiffDirectory directory = initialFile.GetDirectory(0);
        int firstIndex = FindEntryIndex(directory, OrderedFirstTag);
        int secondIndex = FindEntryIndex(directory, OrderedSecondTag);
        initialFile.Close();
        SwapEntries(bytes, directory, firstIndex, secondIndex, TiffFileKind.BigTiff);

        TiffImage image = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions()).GetImage(0);
        TiffIfdEntry[] entries = image.GetTags();
        TiffTagValue[] values = image.GetTagValues();
        byte[][] rawValues = image.GetRawTagBytesInOriginalOrder();
        int swappedFirstIndex = Array.FindIndex(entries, entry => entry.TagId == OrderedFirstTag);
        int swappedSecondIndex = Array.FindIndex(entries, entry => entry.TagId == OrderedSecondTag);

        Assert.That(swappedSecondIndex, Is.EqualTo(firstIndex));
        Assert.That(swappedFirstIndex, Is.EqualTo(secondIndex));
        Assert.That(values[swappedSecondIndex].TagId, Is.EqualTo(OrderedSecondTag));
        Assert.That(values[swappedFirstIndex].TagId, Is.EqualTo(OrderedFirstTag));
        Assert.That(rawValues[swappedSecondIndex], Is.EqualTo(new byte[] { 29 }));
        Assert.That(rawValues[swappedFirstIndex], Is.EqualTo(new byte[] { 17 }));
    }

    [Test]
    public void DuplicateWrongTypeAndWrongCountTags_ProduceStructuredPublicDiagnostics()
    {
        TiffTagValue validated = TiffApi.Data.CreateUnsignedScalarTag(ValidatedTag, TiffDataType.Long, 5);
        TiffWriteOptions options = CreateBigTiffOptions(new TiffTagValue[] { validated });
        options.TagRegistry.RegisterKnown(ValidatedTag, "Validated", TiffDataType.Long, 1);
        byte[] original = CreateFile(options, new TiffTagValue[] { validated });
        TiffFile fixture = TiffApi.Factory.OpenReadBytes(original, new TiffReadOptions());
        TiffDirectory directory = fixture.GetDirectory(0);
        int validatedIndex = FindEntryIndex(directory, ValidatedTag);
        int lengthIndex = FindEntryIndex(directory, TiffTagId.ImageLength);
        fixture.Close();

        byte[] duplicateBytes = original.ToArray();
        WriteEntryTagId(duplicateBytes, directory, lengthIndex, TiffTagId.ImageWidth, TiffFileKind.BigTiff, TiffByteOrder.LittleEndian);
        byte[] wrongTypeBytes = original.ToArray();
        WriteEntryDataType(wrongTypeBytes, directory, validatedIndex, TiffDataType.Ascii, TiffFileKind.BigTiff, TiffByteOrder.LittleEndian);
        byte[] wrongCountBytes = original.ToArray();
        WriteEntryCount(wrongCountBytes, directory, validatedIndex, 2, TiffFileKind.BigTiff, TiffByteOrder.LittleEndian);
        TiffReadOptions validationOptions = new TiffReadOptions();
        validationOptions.TagRegistry.RegisterKnown(ValidatedTag, "Validated", TiffDataType.Long, 1);

        TiffValidationResult duplicate = TiffApi.Validation.ValidateBytes(duplicateBytes, validationOptions);
        TiffValidationResult wrongType = TiffApi.Validation.ValidateBytes(wrongTypeBytes, validationOptions);
        TiffValidationResult wrongCount = TiffApi.Validation.ValidateBytes(wrongCountBytes, validationOptions);

        Assert.That(duplicate.Success, Is.False);
        Assert.That(duplicate.Diagnostics.ContainsCode(TiffErrorCode.DuplicateTag), Is.True);
        Assert.That(duplicate.Diagnostics.ToArray().Any(item => item.Code == TiffErrorCode.DuplicateTag && item.TagId == TiffTagId.ImageWidth), Is.True);
        Assert.That(wrongType.Success, Is.False);
        Assert.That(wrongType.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagType), Is.True);
        Assert.That(wrongType.Diagnostics.ToArray().Any(item => item.Code == TiffErrorCode.InvalidTagType && item.TagId == ValidatedTag), Is.True);
        Assert.That(wrongCount.Success, Is.False);
        Assert.That(wrongCount.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
        Assert.That(wrongCount.Diagnostics.ToArray().Any(item => item.Code == TiffErrorCode.InvalidTagCount && item.TagId == ValidatedTag), Is.True);
    }

    [TestCase(TiffFileKind.Classic)]
    [TestCase(TiffFileKind.BigTiff)]
    public void PublicSubImageWriter_ProducesRecursivelyNavigableNestedSubIfds(TiffFileKind fileKind)
    {
        TiffDataType pointerType = fileKind == TiffFileKind.Classic ? TiffDataType.Long : TiffDataType.Long8;
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = fileKind;
        options.TagRegistry.RegisterKnown(NestedPointerPlaceholder, "Nested", pointerType, 1);
        TiffWriter writer = TiffApi.Factory.CreateWriter(options);
        int parentIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(CreateRequiredTags()));
        TiffTagValue placeholder = TiffApi.Data.CreateUnsignedScalarTag(NestedPointerPlaceholder, pointerType, 0);
        TiffImageDefinition childDefinition = TiffApi.Data.CreateImageDefinition(Combine(CreateRequiredTags(), new TiffTagValue[] { placeholder }));
        TiffApi.Data.SetRawStrip(childDefinition, 0, new byte[] { 2 });
        writer.AddSubImage(parentIndex, childDefinition);
        writer.WriteRawStrip(parentIndex, 0, new byte[] { 1 });
        Assert.That(writer.Flush().HasErrors(), Is.False);
        byte[] bytes = writer.GetFileBytes();

        TiffImage firstImage = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions()).GetImage(0);
        TiffDirectory childDirectory = firstImage.ReadSubIfds().SubIfds[0].Directories[0];
        ulong nestedOffset = AppendEmptyDirectory(ref bytes, fileKind);
        PatchPointerEntry(bytes, childDirectory, NestedPointerPlaceholder, TiffTagId.SubIFDs, nestedOffset, fileKind, TiffByteOrder.LittleEndian);

        TiffImage image = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions()).GetImage(0);
        TiffSubIfdReadResult result = image.ReadSubIfds();

        Assert.That(result.Success, Is.True);
        Assert.That(image.GetSubImageCount(), Is.EqualTo(2));
        Assert.That(result.SubIfds, Has.Length.EqualTo(2));
        Assert.That(result.SubIfds[0].Depth, Is.EqualTo(1));
        Assert.That(result.SubIfds[1].Depth, Is.EqualTo(2));
        Assert.That(result.SubIfds[1].Offset, Is.EqualTo(nestedOffset));
        Assert.That(result.SubIfds[1].Directories, Has.Length.EqualTo(1));
    }

    [TestCase(TiffFileKind.Classic)]
    [TestCase(TiffFileKind.BigTiff)]
    public void ExifGpsAndNestedInteroperabilityPointers_AreTraversedThroughPublicImageApi(TiffFileKind fileKind)
    {
        TiffDataType pointerType = fileKind == TiffFileKind.Classic ? TiffDataType.Long : TiffDataType.Ifd8;
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = fileKind;
        TiffWriter writer = TiffApi.Factory.CreateWriter(options);
        int imageIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(CreateRequiredTags()));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1 });
        TiffExifMetadata exif = new TiffExifMetadata();
        exif.HasExif = true;
        exif.Values = new TiffTagValue[]
        {
            TiffApi.Data.CreateAsciiTag(DateTimeOriginal, "2026:08:27 12:00:00"),
            TiffApi.Data.CreateUnsignedScalarTag(NestedPointerPlaceholder, pointerType, 0)
        };
        TiffGpsMetadata gps = new TiffGpsMetadata();
        gps.HasGps = true;
        gps.Values = new TiffTagValue[] { TiffApi.Data.CreateRationalScalarTag(GpsAltitude, TiffDataType.Rational, 1234, 10) };
        Assert.That(writer.WriteExif(imageIndex, exif).HasErrors(), Is.False);
        Assert.That(writer.WriteGps(imageIndex, gps).HasErrors(), Is.False);
        Assert.That(writer.Flush().HasErrors(), Is.False);
        byte[] bytes = writer.GetFileBytes();

        TiffImage initialImage = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions()).GetImage(0);
        TiffIfdPointer exifPointer = initialImage.ReadIfdPointers().Pointers.Single(pointer => pointer.TagId == TiffTagId.ExifIfd);
        TiffDirectory exifDirectory = exifPointer.Directories[0];
        ulong interoperabilityOffset = AppendEmptyDirectory(ref bytes, fileKind);
        PatchPointerEntry(bytes, exifDirectory, NestedPointerPlaceholder, TiffTagId.InteroperabilityIfd, interoperabilityOffset, fileKind, TiffByteOrder.LittleEndian);

        TiffImage image = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions()).GetImage(0);
        TiffIfdPointerReadResult pointers = image.ReadIfdPointers();
        TiffExifReadResult exifResult = image.ReadExif();
        TiffGpsReadResult gpsResult = image.ReadGps();

        Assert.That(pointers.Success, Is.True);
        Assert.That(pointers.Pointers.Select(pointer => pointer.TagId), Is.EquivalentTo(new int[] { TiffTagId.ExifIfd, TiffTagId.GpsIfd, TiffTagId.InteroperabilityIfd }));
        Assert.That(pointers.Pointers.Single(pointer => pointer.TagId == TiffTagId.InteroperabilityIfd).Depth, Is.EqualTo(2));
        Assert.That(pointers.Pointers.Single(pointer => pointer.TagId == TiffTagId.InteroperabilityIfd).Offset, Is.EqualTo(interoperabilityOffset));
        Assert.That(exifResult.Success, Is.True);
        Assert.That(exifResult.Metadata.InteroperabilityPointers, Has.Length.EqualTo(1));
        Assert.That(gpsResult.Success, Is.True);
        Assert.That(gpsResult.Metadata.Values[0].RationalValues[0].Numerator, Is.EqualTo(1234));
    }

    static TiffTagValue[] CreateAllFieldTypeValues()
    {
        return new TiffTagValue[]
        {
            TiffApi.Data.CreateUnsignedScalarTag(CustomBase, TiffDataType.Byte, 7),
            TiffApi.Data.CreateAsciiArrayTag(CustomBase + 1, new string[] { "one", "a longer second value" }),
            TiffApi.Data.CreateUnsignedArrayTag(CustomBase + 2, TiffDataType.Short, new ulong[] { 3, 4 }),
            TiffApi.Data.CreateUnsignedScalarTag(CustomBase + 3, TiffDataType.Long, 4000000000),
            TiffApi.Data.CreateRationalArrayTag(CustomBase + 4, TiffDataType.Rational, new TiffRational[] { CreateRational(1, 2), CreateRational(3, 4) }),
            TiffApi.Data.CreateSignedScalarTag(CustomBase + 5, TiffDataType.SByte, -2),
            TiffApi.Data.CreateRawTag(CustomBase + 6, TiffDataType.Undefined, 3, new byte[] { 9, 8, 7 }),
            TiffApi.Data.CreateSignedArrayTag(CustomBase + 7, TiffDataType.SShort, new long[] { -3, 4 }),
            TiffApi.Data.CreateSignedScalarTag(CustomBase + 8, TiffDataType.SLong, -100000),
            TiffApi.Data.CreateRationalScalarTag(CustomBase + 9, TiffDataType.SRational, -1, 2),
            TiffApi.Data.CreateFloatingArrayTag(CustomBase + 10, TiffDataType.Float, new double[] { 1.5, -2.5 }),
            TiffApi.Data.CreateFloatingScalarTag(CustomBase + 11, TiffDataType.Double, 3.25),
            TiffApi.Data.CreateUnsignedArrayTag(CustomBase + 12, TiffDataType.Ifd, new ulong[] { 12, 34 }),
            TiffApi.Data.CreateUnsignedScalarTag(CustomBase + 13, TiffDataType.Long8, 5000000000),
            TiffApi.Data.CreateSignedArrayTag(CustomBase + 14, TiffDataType.SLong8, new long[] { -5000000000, 5000000000 }),
            TiffApi.Data.CreateUnsignedScalarTag(CustomBase + 15, TiffDataType.Ifd8, 6000000000)
        };
    }

    static TiffRational CreateRational(long numerator, long denominator)
    {
        TiffRational value = new TiffRational();
        value.Numerator = numerator;
        value.Denominator = denominator;
        return value;
    }

    static TiffWriteOptions CreateBigTiffOptions(TiffTagValue[] customValues)
    {
        TiffWriteOptions options = new TiffWriteOptions();
        options.FileKind = TiffFileKind.BigTiff;
        RegisterDefinitions(options.TagRegistry, customValues);
        return options;
    }

    static void RegisterDefinitions(TiffTagRegistry registry, TiffTagValue[] values)
    {
        foreach (TiffTagValue value in values)
        {
            registry.RegisterKnown(value.TagId, "Custom" + value.TagId, value.DataType, TiffTagDefinition.AnyCount);
        }
    }

    static byte[] CreateFile(TiffWriteOptions options, TiffTagValue[] customValues)
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(options);
        int imageIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(Combine(CreateRequiredTags(), customValues)));
        Assert.That(writer.WriteRawStrip(imageIndex, 0, new byte[] { 1 }).Success, Is.True);
        Assert.That(writer.Flush().HasErrors(), Is.False);
        return writer.GetFileBytes();
    }

    static TiffTagValue[] CreateRequiredTags()
    {
        return new TiffTagValue[]
        {
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, 1),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 1)
        };
    }

    static TiffTagValue[] Combine(TiffTagValue[] first, TiffTagValue[] second)
    {
        return first.Concat(second).ToArray();
    }

    static int FindEntryIndex(TiffDirectory directory, int tagId)
    {
        int index = Array.FindIndex(directory.Entries, entry => entry.TagId == tagId);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        return index;
    }

    static int EntryOffset(TiffDirectory directory, int entryIndex, TiffFileKind fileKind)
    {
        int countByteCount = fileKind == TiffFileKind.Classic ? 2 : 8;
        int entryByteCount = fileKind == TiffFileKind.Classic ? 12 : 20;
        return (int)directory.Offset + countByteCount + (entryIndex * entryByteCount);
    }

    static void SwapEntries(byte[] bytes, TiffDirectory directory, int firstIndex, int secondIndex, TiffFileKind fileKind)
    {
        int entryByteCount = fileKind == TiffFileKind.Classic ? 12 : 20;
        int firstOffset = EntryOffset(directory, firstIndex, fileKind);
        int secondOffset = EntryOffset(directory, secondIndex, fileKind);
        byte[] first = bytes.AsSpan(firstOffset, entryByteCount).ToArray();
        bytes.AsSpan(secondOffset, entryByteCount).CopyTo(bytes.AsSpan(firstOffset, entryByteCount));
        first.CopyTo(bytes, secondOffset);
    }

    static void WriteEntryTagId(byte[] bytes, TiffDirectory directory, int entryIndex, int tagId, TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        WriteUInt16(bytes.AsSpan(EntryOffset(directory, entryIndex, fileKind), 2), (ushort)tagId, byteOrder);
    }

    static void WriteEntryDataType(byte[] bytes, TiffDirectory directory, int entryIndex, TiffDataType dataType, TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        WriteUInt16(bytes.AsSpan(EntryOffset(directory, entryIndex, fileKind) + 2, 2), (ushort)dataType, byteOrder);
    }

    static void WriteEntryCount(byte[] bytes, TiffDirectory directory, int entryIndex, ulong count, TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        int offset = EntryOffset(directory, entryIndex, fileKind) + 4;
        if (fileKind == TiffFileKind.Classic)
        {
            WriteUInt32(bytes.AsSpan(offset, 4), (uint)count, byteOrder);
        }
        else
        {
            WriteUInt64(bytes.AsSpan(offset, 8), count, byteOrder);
        }
    }

    static ulong AppendEmptyDirectory(ref byte[] bytes, TiffFileKind fileKind)
    {
        ulong offset = (ulong)bytes.Length;
        int byteCount = fileKind == TiffFileKind.Classic ? 6 : 16;
        Array.Resize(ref bytes, bytes.Length + byteCount);
        return offset;
    }

    static void PatchPointerEntry(byte[] bytes, TiffDirectory directory, int oldTagId, int newTagId, ulong pointerOffset, TiffFileKind fileKind, TiffByteOrder byteOrder)
    {
        int entryIndex = FindEntryIndex(directory, oldTagId);
        int offset = EntryOffset(directory, entryIndex, fileKind);
        WriteUInt16(bytes.AsSpan(offset, 2), (ushort)newTagId, byteOrder);
        if (fileKind == TiffFileKind.Classic)
        {
            WriteUInt32(bytes.AsSpan(offset + 8, 4), (uint)pointerOffset, byteOrder);
        }
        else
        {
            WriteUInt64(bytes.AsSpan(offset + 12, 8), pointerOffset, byteOrder);
        }
    }

    static void WriteUInt16(Span<byte> destination, ushort value, TiffByteOrder byteOrder)
    {
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination, value);
        }
    }

    static void WriteUInt32(Span<byte> destination, uint value, TiffByteOrder byteOrder)
    {
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination, value);
        }
    }

    static void WriteUInt64(Span<byte> destination, ulong value, TiffByteOrder byteOrder)
    {
        if (byteOrder == TiffByteOrder.LittleEndian)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination, value);
        }
        else
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination, value);
        }
    }
}
