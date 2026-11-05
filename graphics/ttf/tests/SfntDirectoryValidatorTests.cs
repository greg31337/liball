using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

public class SfntDirectoryValidatorTests
{
    [TestCase(FontOutlineKind.TrueType)]
    [TestCase(FontOutlineKind.Cff1)]
    [TestCase(FontOutlineKind.Cff2)]
    [TestCase(FontOutlineKind.BitmapOnly)]
    public void AcceptsEverySupportedOutlineTableCombination(FontOutlineKind outlineKind)
    {
        SfntValidationResult result = Validate(
            BuildValidFont(outlineKind),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.True);
            Assert.That(result.GetOutlineKind(), Is.EqualTo(outlineKind));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(0));
        });
    }

    [Test]
    public void RejectsTableCountAboveConfiguredLimit()
    {
        byte[] bytes = BuildDirectoryOnlyFont(257);
        FontData data = FontData.Create(bytes);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));

        Assert.Throws<FontLimitException>(() => SfntDirectoryValidator.Validate(
            data,
            directory,
            FontLoadOptions.CreateStrict(),
            0));
    }

    [Test]
    public void RejectsEmptyTableDirectory()
    {
        SfntValidationResult result = Validate(
            BuildDirectoryOnlyFont(0),
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.False);
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.InvalidSfntHeader), Is.True);
        });
    }

    [Test]
    public void DetectsUnsortedTableRecords()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        SwapRecords(font, 0, GetTableCount(font) - 1);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.False);
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.UnsortedTableDirectory), Is.True);
        });
    }

    [Test]
    public void DetectsDuplicateTagsEvenWhenDirectoryIsAlsoUnsorted()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int firstRecord = 12;
        int lastRecord = 12 + (GetTableCount(font) - 1) * 16;
        uint firstTag = BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(firstRecord, 4));
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(lastRecord, 4), firstTag);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.That(HasDiagnostic(result, FontDiagnosticCode.DuplicateTable), Is.True);
    }

    [Test]
    public void DetectsTableRangeOutsideFontDataWithStructuredContext()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int recordOffset = FindRecordOffset(font, FontTags.Name);
        BinaryPrimitives.WriteUInt32BigEndian(
            font.AsSpan(recordOffset + 8, 4),
            (uint)(font.Length + 4));

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible(),
            3);
        FontDiagnostic diagnostic = FindDiagnostic(
            result,
            FontDiagnosticCode.TableOutOfBounds);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.False);
            Assert.That(diagnostic.GetFaceIndex(), Is.EqualTo(3));
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Name));
            Assert.That(diagnostic.GetAbsoluteOffset(), Is.EqualTo(recordOffset + 8));
            Assert.That(diagnostic.GetLogicalPath(), Does.Contain(".range"));
        });
    }

    [Test]
    public void DetectsTableThatOverlapsDirectory()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int recordOffset = FindRecordOffset(font, FontTags.Name);
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(recordOffset + 8, 4), 12);
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(recordOffset + 12, 4), 4);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.That(HasDiagnostic(result, FontDiagnosticCode.TableOutOfBounds), Is.True);
    }

    [Test]
    public void DetectsMisalignedTableOffset()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int recordOffset = FindRecordOffset(font, FontTags.Name);
        uint offset = BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(recordOffset + 8, 4));
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(recordOffset + 8, 4), offset + 1);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.That(HasDiagnostic(result, FontDiagnosticCode.MisalignedTable), Is.True);
    }

    [Test]
    public void DetectsOverlappingTableRanges()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int firstRecord = FindRecordOffset(font, FontTags.Hhea);
        int secondRecord = FindRecordOffset(font, FontTags.Hmtx);
        uint firstOffset = BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(firstRecord + 8, 4));
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(secondRecord + 8, 4), firstOffset);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.That(HasDiagnostic(result, FontDiagnosticCode.OverlappingTables), Is.True);
    }

    [Test]
    public void ReportsNonzeroAlignmentPaddingInCompatibleMode()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int recordOffset = FindRecordOffset(font, FontTags.Name);
        int tableOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(recordOffset + 8, 4));
        int tableLength = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(recordOffset + 12, 4));
        font[tableOffset + tableLength] = 0xA5;

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());
        FontDiagnostic diagnostic = FindDiagnostic(
            result,
            FontDiagnosticCode.NonZeroPadding);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.GetSeverity(), Is.EqualTo(FontDiagnosticSeverity.Warning));
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.NonZeroPadding), Is.True);
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.RepairApplied), Is.True);
        });
    }

    [Test]
    public void NonzeroAlignmentPaddingInvalidatesStrictMode()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int recordOffset = FindRecordOffset(font, FontTags.Name);
        int tableOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(recordOffset + 8, 4));
        int tableLength = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(recordOffset + 12, 4));
        font[tableOffset + tableLength] = 1;

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.False);
            Assert.That(
                FindDiagnostic(result, FontDiagnosticCode.NonZeroPadding).GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Error));
        });
    }

    [Test]
    public void ReportsTableChecksumMismatchUnderCompatiblePolicy()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int recordOffset = FindRecordOffset(font, FontTags.Name);
        uint checksum = BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(recordOffset + 4, 4));
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(recordOffset + 4, 4), checksum + 1);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());
        FontDiagnostic diagnostic = FindDiagnostic(
            result,
            FontDiagnosticCode.TableChecksumMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.True);
            Assert.That(diagnostic.GetSeverity(), Is.EqualTo(FontDiagnosticSeverity.Warning));
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Name));
        });
    }

    [Test]
    public void RequiredChecksumPolicyInvalidatesMismatch()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int recordOffset = FindRecordOffset(font, FontTags.Name);
        font[recordOffset + 7] ^= 1;

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.False);
            Assert.That(
                FindDiagnostic(result, FontDiagnosticCode.TableChecksumMismatch).GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Error));
        });
    }

    [Test]
    public void DetectsInvalidWholeFontChecksumAdjustment()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int headRecord = FindRecordOffset(font, FontTags.Head);
        int headOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(headRecord + 8, 4));
        font[headOffset + 11] ^= 1;

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.TableChecksumMismatch), Is.False);
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.FontChecksumMismatch), Is.True);
        });
    }

    [Test]
    public void IgnoreChecksumPolicySkipsTableAndWholeFontChecks()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        int nameRecord = FindRecordOffset(font, FontTags.Name);
        int headRecord = FindRecordOffset(font, FontTags.Head);
        int headOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(headRecord + 8, 4));
        font[nameRecord + 7] ^= 1;
        font[headOffset + 11] ^= 1;

        SfntValidationResult result = Validate(font, CreateIgnoreChecksumOptions());

        Assert.Multiple(() =>
        {
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.TableChecksumMismatch), Is.False);
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.FontChecksumMismatch), Is.False);
            Assert.That(result.IsValid(), Is.True);
        });
    }

    [Test]
    public void StoredSearchFieldsAreWarningsInCompatibleModeAndErrorsInStrictMode()
    {
        byte[] font = BuildValidFont(FontOutlineKind.TrueType);
        BinaryPrimitives.WriteUInt16BigEndian(font.AsSpan(6, 2), 0);
        SfntValidationResult compatible = Validate(
            font,
            FontLoadOptions.CreateCompatible());
        SfntValidationResult strict = Validate(
            font,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(
                FindDiagnostic(compatible, FontDiagnosticCode.InvalidSearchFields).GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Warning));
            Assert.That(
                FindDiagnostic(compatible, FontDiagnosticCode.RepairApplied).GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Information));
            Assert.That(
                FindDiagnostic(strict, FontDiagnosticCode.InvalidSearchFields).GetSeverity(),
                Is.EqualTo(FontDiagnosticSeverity.Error));
            Assert.That(strict.IsValid(), Is.False);
        });
    }

    [Test]
    public void DetectsMissingCommonRequiredTable()
    {
        byte[] font = BuildTrueTypeFont(FontTags.Post, true);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());
        FontDiagnostic diagnostic = FindDiagnostic(
            result,
            FontDiagnosticCode.MissingRequiredTable);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid(), Is.False);
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Post));
        });
    }

    [Test]
    public void DetectsMissingLocaForTrueTypeOutlines()
    {
        byte[] font = BuildTrueTypeFont(0, false);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(result.GetOutlineKind(), Is.EqualTo(FontOutlineKind.TrueType));
            Assert.That(
                FindDiagnostic(result, FontDiagnosticCode.MissingRequiredTable).GetTableTag(),
                Is.EqualTo(FontTags.Loca));
        });
    }

    [Test]
    public void DetectsMultipleVectorOutlineTechnologies()
    {
        TinySfntBuilder builder = AddCommonTables(new TinySfntBuilder(), 0);
        byte[] font = builder
            .AddTable("glyf", new byte[4])
            .AddTable("loca", new byte[4])
            .AddTable("CFF ", new byte[4])
            .Build();

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(result.GetOutlineKind(), Is.EqualTo(FontOutlineKind.Unknown));
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.InvalidTableCombination), Is.True);
        });
    }

    [Test]
    public void DetectsScalerTypeInconsistentWithOutlineKind()
    {
        byte[] font = BuildValidFont(FontOutlineKind.Cff1);
        BinaryPrimitives.WriteUInt32BigEndian(font.AsSpan(0, 4), 0x00010000U);

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.That(result.GetOutlineKind(), Is.EqualTo(FontOutlineKind.Cff1));
            Assert.That(HasDiagnostic(result, FontDiagnosticCode.InvalidTableCombination), Is.True);
        });
    }

    [Test]
    public void DetectsUnpairedBitmapTables()
    {
        TinySfntBuilder builder = AddCommonTables(new TinySfntBuilder(), 0);
        byte[] font = builder.AddTable("CBDT", new byte[4]).Build();

        SfntValidationResult result = Validate(
            font,
            FontLoadOptions.CreateCompatible());
        FontDiagnostic diagnostic = FindDiagnostic(
            result,
            FontDiagnosticCode.MissingRequiredTable);

        Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Cblc));
    }

    [Test]
    public void RejectsNullArgumentsAndNegativeFaceIndex()
    {
        byte[] bytes = BuildValidFont(FontOutlineKind.TrueType);
        FontData data = FontData.Create(bytes);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));
        FontLoadOptions options = FontLoadOptions.CreateCompatible();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => SfntDirectoryValidator.Validate(
                null!,
                directory,
                options,
                0));
            Assert.Throws<ArgumentNullException>(() => SfntDirectoryValidator.Validate(
                data,
                null!,
                options,
                0));
            Assert.Throws<ArgumentNullException>(() => SfntDirectoryValidator.Validate(
                data,
                directory,
                null!,
                0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SfntDirectoryValidator.Validate(
                data,
                directory,
                options,
                -1));
        });
    }

    [Test]
    public void ValidationResultRejectsDiagnosticIndexOutsideResult()
    {
        SfntValidationResult result = Validate(
            BuildValidFont(FontOutlineKind.TrueType),
            FontLoadOptions.CreateCompatible());

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetDiagnostic(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetDiagnostic(0));
        });
    }

    private static SfntValidationResult Validate(byte[] bytes, FontLoadOptions options)
    {
        return Validate(bytes, options, 0);
    }

    private static SfntValidationResult Validate(
        byte[] bytes,
        FontLoadOptions options,
        int faceIndex)
    {
        FontData data = FontData.Create(bytes);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));
        return SfntDirectoryValidator.Validate(data, directory, options, faceIndex);
    }

    private static byte[] BuildValidFont(FontOutlineKind outlineKind)
    {
        if (outlineKind == FontOutlineKind.TrueType)
        {
            return BuildTrueTypeFont(0, true);
        }

        TinySfntBuilder builder = AddCommonTables(new TinySfntBuilder(), 0);

        if (outlineKind == FontOutlineKind.Cff1)
        {
            return builder
                .SetScalerType(0x4F54544FU)
                .AddTable("CFF ", new byte[4])
                .Build();
        }

        if (outlineKind == FontOutlineKind.Cff2)
        {
            return builder
                .SetScalerType(0x4F54544FU)
                .AddTable("CFF2", new byte[4])
                .Build();
        }

        return builder
            .AddTable("CBDT", new byte[4])
            .AddTable("CBLC", new byte[4])
            .Build();
    }

    private static byte[] BuildTrueTypeFont(int omittedCommonTag, bool includeLoca)
    {
        TinySfntBuilder builder = AddCommonTables(
            new TinySfntBuilder(),
            omittedCommonTag);
        builder.AddTable("glyf", new byte[4]);

        if (includeLoca)
        {
            builder.AddTable("loca", new byte[4]);
        }

        return builder.Build();
    }

    private static TinySfntBuilder AddCommonTables(
        TinySfntBuilder builder,
        int omittedTag)
    {
        AddTableUnlessOmitted(builder, "cmap", FontTags.Cmap, new byte[4], omittedTag);
        AddTableUnlessOmitted(builder, "head", FontTags.Head, new byte[12], omittedTag);
        AddTableUnlessOmitted(builder, "hhea", FontTags.Hhea, new byte[4], omittedTag);
        AddTableUnlessOmitted(builder, "hmtx", FontTags.Hmtx, new byte[4], omittedTag);
        AddTableUnlessOmitted(builder, "maxp", FontTags.Maxp, new byte[4], omittedTag);
        AddTableUnlessOmitted(builder, "name", FontTags.Name, new byte[1], omittedTag);
        AddTableUnlessOmitted(builder, "OS/2", FontTags.Os2, new byte[4], omittedTag);
        AddTableUnlessOmitted(builder, "post", FontTags.Post, new byte[4], omittedTag);
        return builder;
    }

    private static void AddTableUnlessOmitted(
        TinySfntBuilder builder,
        string name,
        int tag,
        byte[] data,
        int omittedTag)
    {
        if (tag != omittedTag)
        {
            builder.AddTable(name, data);
        }
    }

    private static byte[] BuildDirectoryOnlyFont(int tableCount)
    {
        byte[] bytes = new byte[12 + tableCount * 16];
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(0, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(4, 2), (ushort)tableCount);

        for (int index = 0; index < tableCount; index += 1)
        {
            int recordOffset = 12 + index * 16;
            BinaryPrimitives.WriteUInt32BigEndian(
                bytes.AsSpan(recordOffset, 4),
                (uint)(0x20202020 + index));
            BinaryPrimitives.WriteUInt32BigEndian(
                bytes.AsSpan(recordOffset + 8, 4),
                (uint)bytes.Length);
        }

        return bytes;
    }

    private static FontLoadOptions CreateIgnoreChecksumOptions()
    {
        return FontLoadOptions.Create(
            FontLoadMode.Compatible,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.Safe,
            FontChecksumPolicy.Ignore,
            FontLimits.CreateCompatible(),
            CancellationToken.None);
    }

    private static int GetTableCount(byte[] font)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4, 2));
    }

    private static int FindRecordOffset(byte[] font, int tag)
    {
        int tableCount = GetTableCount(font);

        for (int index = 0; index < tableCount; index += 1)
        {
            int recordOffset = 12 + index * 16;
            int actualTag = BinaryPrimitives.ReadInt32BigEndian(font.AsSpan(recordOffset, 4));

            if (actualTag == tag)
            {
                return recordOffset;
            }
        }

        throw new InvalidOperationException("The requested test table was not found.");
    }

    private static void SwapRecords(byte[] font, int firstIndex, int secondIndex)
    {
        int firstOffset = 12 + firstIndex * 16;
        int secondOffset = 12 + secondIndex * 16;
        byte[] temporary = font.AsSpan(firstOffset, 16).ToArray();
        font.AsSpan(secondOffset, 16).CopyTo(font.AsSpan(firstOffset, 16));
        temporary.CopyTo(font, secondOffset);
    }

    private static bool HasDiagnostic(
        SfntValidationResult result,
        FontDiagnosticCode code)
    {
        for (int index = 0; index < result.GetDiagnosticCount(); index += 1)
        {
            if (result.GetDiagnostic(index).GetCode() == code)
            {
                return true;
            }
        }

        return false;
    }

    private static FontDiagnostic FindDiagnostic(
        SfntValidationResult result,
        FontDiagnosticCode code)
    {
        for (int index = 0; index < result.GetDiagnosticCount(); index += 1)
        {
            FontDiagnostic diagnostic = result.GetDiagnostic(index);

            if (diagnostic.GetCode() == code)
            {
                return diagnostic;
            }
        }

        throw new AssertionException($"Diagnostic {code} was not found.");
    }
}
