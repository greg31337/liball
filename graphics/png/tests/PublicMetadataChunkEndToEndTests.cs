using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicMetadataChunkEndToEndTests
{
    [TestCaseSource(nameof(CreateEveryStandardChunkCase))]
    public void PublicChunkService_RoundTripsEveryStandardChunk(PngChunk source)
    {
        IPngChunkCodec chunks = new PngChunkService();
        byte[] framed = new byte[source.Data.Length + 12];

        int written = chunks.WriteChunkSpan(framed, source);
        PngChunk decoded = chunks.ReadChunkSpan(framed);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(framed.Length));
            Assert.That(decoded.Type, Is.EqualTo(source.Type));
            Assert.That(decoded.Data, Is.EqualTo(source.Data));
            Assert.That(decoded.Length, Is.EqualTo((uint)source.Data.Length));
            Assert.That(decoded.Crc, Is.EqualTo(new PngCrcCalculator().CalculateChunkFromName(source.Type, source.Data)));
            Assert.That(decoded.IsKnown, Is.True);
        });
    }

    [Test]
    public void PublicCodec_IdentifyDecodeAndReencodeEveryCompatibleNonPaletteMetadataFamily()
    {
        PngImage source = CreateMetadataImage();
        PngEncoderOptions options = CreateRgbaOptions();
        PngCodec codec = new PngCodec();
        PngIdentifierService identifier = new PngIdentifierService();

        byte[] encoded = codec.EncodeToBytes(source, options);
        PngIdentifyResult identified = identifier.IdentifyBytes(encoded);
        PngImage decoded = codec.DecodeBytes(encoded, CreateDecodeOptions());
        byte[] reencoded = codec.EncodeToBytes(decoded, options);
        PngImage decodedAgain = codec.DecodeBytes(reencoded, CreateDecodeOptions());

        PngValidationResult validation = new PngValidationService().ValidateBytes(encoded);
        Assert.That(validation.IsValid, Is.True, string.Join(", ", validation.Errors.Select(error => error.Code + ":" + error.ChunkType)));
        Assert.That(identified.IsValid, Is.True);
        AssertCompleteMetadata(identified.Metadata);
        AssertCompleteMetadata(decoded.Metadata);
        AssertCompleteMetadata(decodedAgain.Metadata);
        Assert.That(reencoded, Is.EqualTo(encoded));

        PngDecoderOptions exactTextLimits = CreateDecodeOptions();
        exactTextLimits.Limits.MaxTextBytes = identified.Metadata.TextBytes;
        exactTextLimits.Limits.MaxTextEntries = 3;
        PngDecoderOptions lowTextBytes = CreateDecodeOptions();
        lowTextBytes.Limits.MaxTextBytes = identified.Metadata.TextBytes - 1;
        PngDecoderOptions lowTextEntries = CreateDecodeOptions();
        lowTextEntries.Limits.MaxTextEntries = 2;

        Assert.That(identifier.IdentifyBytesWithOptions(encoded, exactTextLimits).Metadata.TextEntryCount, Is.EqualTo(3));
        Assert.Throws<OverflowException>(() => identifier.IdentifyBytesWithOptions(encoded, lowTextBytes));
        Assert.Throws<OverflowException>(() => identifier.IdentifyBytesWithOptions(encoded, lowTextEntries));
    }

    [Test]
    public void PublicCodec_RoundTripsIndexedPaletteTransparencyBackgroundAndHistogram()
    {
        PngPalette palette = CreatePalette();
        PngImage image = new PngImageFactory().CreateIndexedImage(3, 1, PngPixelFormat.Indexed2, 1, new byte[] { 0x18 }, palette);
        PngMetadataService metadataService = new PngMetadataService();
        PngIndexedTransparency transparency = new PngIndexedTransparency();
        transparency.Alpha = new byte[] { 255, 128, 0 };
        PngBackgroundColor background = new PngBackgroundColor();
        background.PaletteIndex = 1;
        PngPaletteHistogram histogram = new PngPaletteHistogram();
        histogram.Frequencies = new ushort[] { 10, 20, 30 };
        image.Metadata = metadataService.SetIndexedTransparency(image.Metadata, transparency);
        image.Metadata = metadataService.SetIndexedBackgroundColor(image.Metadata, background);
        image.Metadata = metadataService.SetPaletteHistogram(image.Metadata, histogram);

        PngEncoderOptions options = CreateIndexedOptions();
        byte[] encoded = new PngCodec().EncodeToBytes(image, options);
        PngIdentifyResult identified = new PngIdentifierService().IdentifyBytes(encoded);
        PngImage decoded = new PngCodec().DecodeBytes(encoded, CreateUnknownFormatDecodeOptions());
        byte[] reencoded = new PngCodec().EncodeToBytes(decoded, options);

        Assert.Multiple(() =>
        {
            Assert.That(identified.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
            Assert.That(identified.Info.BitDepth, Is.EqualTo(PngBitDepth.Two));
            Assert.That(identified.Metadata.Palette.Entries, Has.Length.EqualTo(3));
            Assert.That(identified.Metadata.IndexedTransparency!.Alpha, Is.EqualTo(new byte[] { 255, 128, 0 }));
            Assert.That(identified.Metadata.BackgroundColor!.PaletteIndex, Is.EqualTo(1));
            Assert.That(identified.Metadata.PaletteHistogram!.Frequencies, Is.EqualTo(new ushort[] { 10, 20, 30 }));
            Assert.That(decoded.Pixels.Format, Is.EqualTo(PngPixelFormat.Indexed2));
            Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 0x18 }));
            Assert.That(reencoded, Is.EqualTo(encoded));
            Assert.That(HasChunk(encoded, PngConstants.ChunkPLTE), Is.True);
            Assert.That(HasChunk(encoded, PngConstants.ChunkTransparency), Is.True);
            Assert.That(HasChunk(encoded, PngConstants.ChunkBackgroundColor), Is.True);
            Assert.That(HasChunk(encoded, PngConstants.ChunkPaletteHistogram), Is.True);
        });
    }

    [TestCase(1, "cICP")]
    [TestCase(2, "iCCP")]
    [TestCase(3, "sRGB")]
    [TestCase(4, "cHRM/gAMA")]
    public void PublicColorMetadataService_UsesDocumentedPrecedence(int lowestIncludedTier, string expectedSource)
    {
        PngMetadata metadata = new PngMetadata();
        List<PngChunk> chunks = new List<PngChunk>();
        if (lowestIncludedTier <= 4)
        {
            chunks.Add(PngChromaticitiesWriter.WriteChunk(CreateChromaticities()));
            chunks.Add(PngGammaWriter.WriteChunk(CreateGamma()));
        }

        if (lowestIncludedTier <= 3)
        {
            chunks.Add(PngStandardRgbWriter.WriteChunk(CreateStandardRgb()));
        }

        if (lowestIncludedTier <= 2)
        {
            chunks.Add(PngIccProfileWriter.WriteChunk(CreateIccProfile()));
        }

        if (lowestIncludedTier <= 1)
        {
            chunks.Add(PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodePoints()));
        }

        metadata.Chunks = chunks.ToArray();

        PngColorMetadata result = new PngColorMetadataService().ResolveColorMetadata(metadata);

        Assert.That(result.HasColorMetadata, Is.True);
        Assert.That(result.SourceChunkType, Is.EqualTo(expectedSource));
        Assert.That(result.Precedence, Is.EqualTo(lowestIncludedTier));
    }

    [Test]
    public void PublicEncoder_AppliesSelectedAndUnknownChunkPoliciesEndToEnd()
    {
        PngImage image = CreatePolicyImage();

        PngEncoderOptions changed = CreateRgbaOptions();
        changed.PixelDataChanged = true;
        PngEncoderOptions unchanged = CreateRgbaOptions();
        unchanged.PixelDataChanged = false;
        PngEncoderOptions keepGamma = CreateRgbaOptions();
        keepGamma.MetadataPolicy = PngMetadataPolicy.KeepSelected;
        keepGamma.MetadataChunkTypesToKeep = new string[] { PngConstants.ChunkGamma };
        PngEncoderOptions removePhysical = CreateRgbaOptions();
        removePhysical.MetadataPolicy = PngMetadataPolicy.RemoveSelected;
        removePhysical.MetadataChunkTypesToRemove = new string[] { PngConstants.ChunkPhysicalPixels };
        removePhysical.PixelDataChanged = false;
        PngEncoderOptions replaceUnknown = CreateRgbaOptions();
        replaceUnknown.UnknownChunkDisposition = PngChunkDisposition.Replace;
        replaceUnknown.UnknownChunkReplacements = new PngUnknownChunkReplacement[]
        {
            CreateReplacement("vpAg", new byte[] { 9 }),
            CreateReplacement("vpAG", new byte[] { 8 })
        };

        PngChunk[] changedChunks = ReadChunks(new PngCodec().EncodeToBytes(image, changed));
        PngChunk[] unchangedChunks = ReadChunks(new PngCodec().EncodeToBytes(image, unchanged));
        PngChunk[] keptChunks = ReadChunks(new PngCodec().EncodeToBytes(image, keepGamma));
        PngChunk[] removedChunks = ReadChunks(new PngCodec().EncodeToBytes(image, removePhysical));
        PngChunk[] replacedChunks = ReadChunks(new PngCodec().EncodeToBytes(image, replaceUnknown));

        Assert.That(HasChunk(changedChunks, "vpAg"), Is.True);
        Assert.That(HasChunk(changedChunks, "vpAG"), Is.False);
        Assert.That(HasChunk(unchangedChunks, "vpAg"), Is.True);
        Assert.That(HasChunk(unchangedChunks, "vpAG"), Is.True);
        Assert.That(ChunkTypes(keptChunks), Does.Contain(PngConstants.ChunkGamma));
        Assert.That(HasChunk(keptChunks, PngConstants.ChunkPhysicalPixels), Is.False);
        Assert.That(HasChunk(keptChunks, PngConstants.ChunkText), Is.False);
        Assert.That(HasChunk(removedChunks, PngConstants.ChunkGamma), Is.True);
        Assert.That(HasChunk(removedChunks, PngConstants.ChunkPhysicalPixels), Is.False);
        Assert.That(HasChunk(removedChunks, PngConstants.ChunkText), Is.True);
        Assert.That(FindRequiredChunk(replacedChunks, "vpAg").Data, Is.EqualTo(new byte[] { 9 }));
        Assert.That(FindRequiredChunk(replacedChunks, "vpAG").Data, Is.EqualTo(new byte[] { 8 }));
    }

    [Test]
    public void PublicValidationAndRepair_HandleAncillaryCrcAndTrailingDataEndToEnd()
    {
        PngEncoderOptions encodeOptions = CreateRgbaOptions();
        encodeOptions.PixelDataChanged = false;
        byte[] encoded = new PngCodec().EncodeToBytes(CreatePolicyImage(), encodeOptions);
        byte[] corrupted = Append(CorruptChunkCrc(encoded, "vpAg"), new byte[] { 7, 8, 9 });
        PngValidationService validator = new PngValidationService();

        PngValidationResult invalid = validator.ValidateBytes(corrupted);
        PngRepairOptions repairOptions = new PngRepairOptions();
        repairOptions.RepairAncillaryCrc = true;
        repairOptions.RemoveTrailingData = true;
        PngRepairResult repaired = new PngRepairService().RepairBytes(corrupted, repairOptions);
        PngImage decoded = new PngCodec().DecodeBytes(repaired.Data, CreateDecodeOptions());

        Assert.That(HasError(invalid, "PNG_CHUNK_CRC_MISMATCH"), Is.True);
        Assert.That(HasError(invalid, "PNG_TRAILING_DATA"), Is.True);
        Assert.That(repaired.Report.Changed, Is.True);
        Assert.That(repaired.Report.Actions.Select(action => action.Code), Does.Contain("PNG_REPAIR_ANCILLARY_CRC_RECALCULATED"));
        Assert.That(repaired.Report.Actions.Select(action => action.Code), Does.Contain("PNG_REPAIR_TRAILING_DATA_REMOVED"));
        Assert.That(repaired.Report.AfterValidation.IsValid, Is.True);
        Assert.That(decoded.Metadata.UnknownChunks.Any(chunk => chunk.Type == "vpAg"), Is.True);

        byte[] withTrailingData = Append(encoded, new byte[] { 1, 2 });
        PngDecoderOptions ignoreTrailing = CreateDecodeOptions();
        ignoreTrailing.IgnoreTrailingBytes = true;
        Assert.Throws<InvalidDataException>(() => new PngCodec().DecodeBytes(withTrailingData, CreateDecodeOptions()));
        Assert.DoesNotThrow(() => new PngCodec().DecodeBytes(withTrailingData, ignoreTrailing));
    }

    [TestCaseSource(nameof(CreateMalformedPngCase))]
    public void PublicValidation_ReportsMalformedInputsDeterministically(byte[] malformed)
    {
        PngValidationService validator = new PngValidationService();

        PngValidationResult first = validator.ValidateBytes(malformed);
        PngValidationResult second = validator.ValidateBytes(malformed);

        Assert.That(first.IsValid, Is.False);
        Assert.That(first.Errors, Is.Not.Empty);
        Assert.That(DescribeErrors(second.Errors), Is.EqualTo(DescribeErrors(first.Errors)));
        Assert.That(DescribeWarnings(second.Warnings), Is.EqualTo(DescribeWarnings(first.Warnings)));
    }

    static IEnumerable<TestCaseData> CreateEveryStandardChunkCase()
    {
        yield return StandardChunkCase(PngIhdrWriter.WriteChunk(CreateIhdr()));
        yield return StandardChunkCase(PngPlteWriter.WriteChunk(CreatePalette()));
        yield return StandardChunkCase(CreateChunk(PngConstants.ChunkIDAT, new byte[] { 1, 2, 3 }));
        yield return StandardChunkCase(PngIendWriter.WriteChunk());
        yield return StandardChunkCase(PngTransparencyWriter.WriteTruecolorChunk(CreateTruecolorTransparency()));
        yield return StandardChunkCase(PngChromaticitiesWriter.WriteChunk(CreateChromaticities()));
        yield return StandardChunkCase(PngGammaWriter.WriteChunk(CreateGamma()));
        yield return StandardChunkCase(PngIccProfileWriter.WriteChunk(CreateIccProfile()));
        yield return StandardChunkCase(PngSignificantBitsWriter.WriteTruecolorAlphaChunk(CreateSignificantBits()));
        yield return StandardChunkCase(PngStandardRgbWriter.WriteChunk(CreateStandardRgb()));
        yield return StandardChunkCase(PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodePoints()));
        yield return StandardChunkCase(PngMasteringDisplayColorVolumeWriter.WriteChunk(CreateMasteringDisplay()));
        yield return StandardChunkCase(PngContentLightLevelInformationWriter.WriteChunk(CreateLightLevels()));
        yield return StandardChunkCase(PngTextWriter.WriteChunk(CreateLatinText()));
        yield return StandardChunkCase(PngCompressedTextWriter.WriteChunk(CreateCompressedText()));
        yield return StandardChunkCase(PngInternationalTextWriter.WriteChunk(CreateInternationalText()));
        yield return StandardChunkCase(PngBackgroundColorWriter.WriteTruecolorChunk(CreateTruecolorBackground()));
        yield return StandardChunkCase(PngPaletteHistogramWriter.WriteChunk(CreateHistogram()));
        yield return StandardChunkCase(PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels()));
        yield return StandardChunkCase(PngSuggestedPaletteWriter.WriteChunk(CreateSuggestedPalette()));
        yield return StandardChunkCase(PngExifProfileWriter.WriteChunk(CreateExifProfile()));
        yield return StandardChunkCase(PngTimeWriter.WriteChunk(CreateTime()));
        yield return StandardChunkCase(PngAnimationControlWriter.WriteChunk(CreateAnimationControl()));
        yield return StandardChunkCase(PngFrameControlWriter.WriteChunk(CreateFrameControl()));
        yield return StandardChunkCase(PngFrameDataWriter.WriteChunk(CreateFrameData()));
    }

    static TestCaseData StandardChunkCase(PngChunk chunk)
    {
        return new TestCaseData(chunk).SetName("Public chunk " + chunk.Type);
    }

    static IEnumerable<TestCaseData> CreateMalformedPngCase()
    {
        byte[] valid = new PngCodec().EncodeToBytes(new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[] { 1, 2, 3, 4 }), CreateRgbaOptions());
        yield return new TestCaseData(new byte[] { 1, 2, 3 }).SetName("Short signature");
        yield return new TestCaseData(valid.AsSpan(0, valid.Length - 2).ToArray()).SetName("Truncated IEND");

        PngChunk[] validChunks = ReadChunks(valid);
        PngChunk[] duplicateHeader = new PngChunk[validChunks.Length + 1];
        duplicateHeader[0] = validChunks[0];
        Array.Copy(validChunks, 0, duplicateHeader, 1, validChunks.Length);
        yield return new TestCaseData(new PngChunkService().WriteChunks(duplicateHeader)).SetName("Duplicate IHDR");

        PngChunk[] malformedTime = new PngChunk[validChunks.Length + 1];
        malformedTime[0] = validChunks[0];
        malformedTime[1] = CreateChunk(PngConstants.ChunkTime, new byte[] { 1 });
        Array.Copy(validChunks, 1, malformedTime, 2, validChunks.Length - 1);
        yield return new TestCaseData(new PngChunkService().WriteChunks(malformedTime)).SetName("Malformed tIME");
    }

    static PngImage CreateMetadataImage()
    {
        PngImage image = new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 40 });
        image.Metadata.Chunks = new PngChunk[]
        {
            PngChromaticitiesWriter.WriteChunk(CreateChromaticities()),
            PngGammaWriter.WriteChunk(CreateGamma()),
            PngSignificantBitsWriter.WriteTruecolorAlphaChunk(CreateSignificantBits()),
            PngMasteringDisplayColorVolumeWriter.WriteChunk(CreateMasteringDisplay()),
            PngContentLightLevelInformationWriter.WriteChunk(CreateLightLevels()),
            PngTextWriter.WriteChunk(CreateLatinText()),
            PngCompressedTextWriter.WriteChunk(CreateCompressedText()),
            PngInternationalTextWriter.WriteChunk(CreateInternationalText()),
            PngBackgroundColorWriter.WriteTruecolorChunk(CreateTruecolorBackground()),
            PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels()),
            PngSuggestedPaletteWriter.WriteChunk(CreateSuggestedPalette()),
            PngExifProfileWriter.WriteChunk(CreateExifProfile()),
            PngTimeWriter.WriteChunk(CreateTime()),
            CreateChunk("vpAg", new byte[] { 5, 6 }),
            CreateChunk("vpAG", new byte[] { 7, 8 })
        };
        return image;
    }

    static void AssertCompleteMetadata(PngMetadata metadata)
    {
        Assert.Multiple(() =>
        {
            Assert.That(metadata.Chromaticities, Is.Not.Null);
            Assert.That(metadata.Gamma!.Value, Is.EqualTo(45455));
            Assert.That(metadata.SignificantBits!.Alpha, Is.EqualTo(8));
            Assert.That(metadata.MasteringDisplayColorVolume!.MaximumLuminance, Is.EqualTo(1000));
            Assert.That(metadata.ContentLightLevelInformation!.MaximumContentLightLevel, Is.EqualTo(1000));
            Assert.That(metadata.Latin1TextEntries.Single().Text, Is.EqualTo("Café"));
            Assert.That(metadata.CompressedTextEntries.Single().Text, Is.EqualTo("compressed"));
            Assert.That(metadata.InternationalTextEntries.Single().Text, Is.EqualTo("雪"));
            Assert.That(metadata.BackgroundColor!.Blue, Is.EqualTo(3));
            Assert.That(metadata.PhysicalPixels!.PixelsPerUnitX, Is.EqualTo(3780));
            Assert.That(metadata.SuggestedPalettes.Single().Name, Is.EqualTo("Web"));
            Assert.That(metadata.ExifProfile!.Profile, Is.EqualTo(new byte[] { 0x49, 0x49, 0x2A, 0x00 }));
            Assert.That(metadata.Time!.Second, Is.EqualTo(60));
            Assert.That(metadata.TextEntryCount, Is.EqualTo(3));
            Assert.That(metadata.UnknownChunks.Select(chunk => chunk.Type), Is.EqualTo(new string[] { "vpAg", "vpAG" }));
            Assert.That(metadata.ColorMetadata.SourceChunkType, Is.EqualTo("cHRM/gAMA"));
        });
    }

    static PngImage CreatePolicyImage()
    {
        PngImage image = new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[] { 1, 2, 3, 4 });
        image.Metadata.Chunks = new PngChunk[]
        {
            PngGammaWriter.WriteChunk(CreateGamma()),
            PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels()),
            PngTextWriter.WriteChunk(CreateLatinText()),
            CreateChunk("vpAg", new byte[] { 1 }),
            CreateChunk("vpAG", new byte[] { 2 })
        };
        return image;
    }

    static PngEncoderOptions CreateRgbaOptions()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.UseAutomaticBitDepth = false;
        options.BitDepth = PngBitDepth.Eight;
        options.UseFixedFilter = true;
        options.RowFilterStrategy = PngRowFilterStrategy.Fixed;
        options.FixedFilterType = PngFilterType.None;
        options.PixelDataChanged = false;
        return options;
    }

    static PngEncoderOptions CreateIndexedOptions()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.ColorType = PngColorType.IndexedColor;
        options.UseAutomaticBitDepth = false;
        options.BitDepth = PngBitDepth.Two;
        options.AllowPaletteReduction = false;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        return options;
    }

    static PngDecoderOptions CreateDecodeOptions()
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Rgba8;
        return options;
    }

    static PngDecoderOptions CreateUnknownFormatDecodeOptions()
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.PreferredPixelFormat = PngPixelFormat.Unknown;
        return options;
    }

    static PngIhdr CreateIhdr()
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = 1;
        ihdr.Height = 1;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }

    static PngPalette CreatePalette()
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[]
        {
            CreatePaletteEntry(10, 20, 30),
            CreatePaletteEntry(40, 50, 60),
            CreatePaletteEntry(70, 80, 90)
        };
        return palette;
    }

    static PngPaletteEntry CreatePaletteEntry(int red, int green, int blue)
    {
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = (byte)red;
        entry.Green = (byte)green;
        entry.Blue = (byte)blue;
        return entry;
    }

    static PngTruecolorTransparency CreateTruecolorTransparency()
    {
        PngTruecolorTransparency value = new PngTruecolorTransparency();
        value.Red = 1;
        value.Green = 2;
        value.Blue = 3;
        return value;
    }

    static PngChromaticities CreateChromaticities()
    {
        PngChromaticities value = new PngChromaticities();
        value.WhitePointX = 31270;
        value.WhitePointY = 32900;
        value.RedX = 64000;
        value.RedY = 33000;
        value.GreenX = 30000;
        value.GreenY = 60000;
        value.BlueX = 15000;
        value.BlueY = 6000;
        return value;
    }

    static PngGamma CreateGamma()
    {
        PngGamma value = new PngGamma();
        value.Value = 45455;
        return value;
    }

    static PngIccProfile CreateIccProfile()
    {
        PngIccProfile value = new PngIccProfile();
        value.Name = "Display";
        value.Profile = new byte[] { 1, 3, 5, 7 };
        return value;
    }

    static PngSignificantBits CreateSignificantBits()
    {
        PngSignificantBits value = new PngSignificantBits();
        value.Red = 8;
        value.Green = 8;
        value.Blue = 8;
        value.Alpha = 8;
        return value;
    }

    static PngStandardRgb CreateStandardRgb()
    {
        PngStandardRgb value = new PngStandardRgb();
        value.RenderingIntent = 1;
        return value;
    }

    static PngCodingIndependentCodePoints CreateCodePoints()
    {
        PngCodingIndependentCodePoints value = new PngCodingIndependentCodePoints();
        value.ColorPrimaries = 9;
        value.TransferCharacteristics = 16;
        value.MatrixCoefficients = 0;
        value.VideoFullRangeFlag = 1;
        return value;
    }

    static PngMasteringDisplayColorVolume CreateMasteringDisplay()
    {
        PngMasteringDisplayColorVolume value = new PngMasteringDisplayColorVolume();
        value.RedX = 1;
        value.RedY = 2;
        value.GreenX = 3;
        value.GreenY = 4;
        value.BlueX = 5;
        value.BlueY = 6;
        value.WhitePointX = 7;
        value.WhitePointY = 8;
        value.MaximumLuminance = 1000;
        value.MinimumLuminance = 1;
        return value;
    }

    static PngContentLightLevelInformation CreateLightLevels()
    {
        PngContentLightLevelInformation value = new PngContentLightLevelInformation();
        value.MaximumContentLightLevel = 1000;
        value.MaximumFrameAverageLightLevel = 400;
        return value;
    }

    static PngText CreateLatinText()
    {
        PngText value = new PngText();
        value.Keyword = "Title";
        value.Text = "Café";
        return value;
    }

    static PngCompressedText CreateCompressedText()
    {
        PngCompressedText value = new PngCompressedText();
        value.Keyword = "Comment";
        value.Text = "compressed";
        return value;
    }

    static PngInternationalText CreateInternationalText()
    {
        PngInternationalText value = new PngInternationalText();
        value.Keyword = "Description";
        value.CompressionFlag = 1;
        value.LanguageTag = "ja";
        value.TranslatedKeyword = "説明";
        value.Text = "雪";
        return value;
    }

    static PngBackgroundColor CreateTruecolorBackground()
    {
        PngBackgroundColor value = new PngBackgroundColor();
        value.Red = 1;
        value.Green = 2;
        value.Blue = 3;
        return value;
    }

    static PngPaletteHistogram CreateHistogram()
    {
        PngPaletteHistogram value = new PngPaletteHistogram();
        value.Frequencies = new ushort[] { 10, 20, 30 };
        return value;
    }

    static PngPhysicalPixels CreatePhysicalPixels()
    {
        PngPhysicalPixels value = new PngPhysicalPixels();
        value.PixelsPerUnitX = 3780;
        value.PixelsPerUnitY = 3780;
        value.UnitSpecifier = 1;
        return value;
    }

    static PngSuggestedPalette CreateSuggestedPalette()
    {
        PngSuggestedPaletteEntry entry = new PngSuggestedPaletteEntry();
        entry.Red = 1;
        entry.Green = 2;
        entry.Blue = 3;
        entry.Alpha = 255;
        entry.Frequency = 4;
        PngSuggestedPalette value = new PngSuggestedPalette();
        value.Name = "Web";
        value.SampleDepth = 8;
        value.Entries = new PngSuggestedPaletteEntry[] { entry };
        return value;
    }

    static PngExifProfile CreateExifProfile()
    {
        PngExifProfile value = new PngExifProfile();
        value.Profile = new byte[] { 0x49, 0x49, 0x2A, 0x00 };
        return value;
    }

    static PngTime CreateTime()
    {
        PngTime value = new PngTime();
        value.Year = 2024;
        value.Month = 8;
        value.Day = 4;
        value.Hour = 23;
        value.Minute = 59;
        value.Second = 60;
        return value;
    }

    static PngAnimationControl CreateAnimationControl()
    {
        PngAnimationControl value = new PngAnimationControl();
        value.FrameCount = 1;
        value.PlayCount = 0;
        return value;
    }

    static PngFrameControl CreateFrameControl()
    {
        PngFrameControl value = new PngFrameControl();
        value.Width = 1;
        value.Height = 1;
        value.DelayNumerator = 1;
        value.DelayDenominator = 100;
        return value;
    }

    static PngFrameData CreateFrameData()
    {
        PngFrameData value = new PngFrameData();
        value.SequenceNumber = 1;
        value.Data = new byte[] { 1 };
        return value;
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        chunk.Length = (uint)data.Length;
        chunk.IsKnown = PngKnownChunkTypes.IsKnown(type);
        chunk.IsCritical = !PngChunkTypeFlags.IsAncillary(type);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(type);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(type);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(type);
        return chunk;
    }

    static PngUnknownChunkReplacement CreateReplacement(string type, byte[] data)
    {
        PngUnknownChunkReplacement replacement = new PngUnknownChunkReplacement();
        replacement.Type = type;
        replacement.Data = data;
        return replacement;
    }

    static PngChunk[] ReadChunks(byte[] data)
    {
        return new PngChunkService().ReadChunks(data).Chunks;
    }

    static bool HasChunk(byte[] data, string type)
    {
        return HasChunk(ReadChunks(data), type);
    }

    static bool HasChunk(PngChunk[] chunks, string type)
    {
        return chunks.Any(chunk => chunk.Type == type);
    }

    static PngChunk FindRequiredChunk(PngChunk[] chunks, string type)
    {
        return chunks.Single(chunk => chunk.Type == type);
    }

    static string[] ChunkTypes(PngChunk[] chunks)
    {
        return chunks.Select(chunk => chunk.Type).ToArray();
    }

    static bool HasError(PngValidationResult result, string code)
    {
        return result.Errors.Any(error => error.Code == code);
    }

    static string[] DescribeErrors(PngError[] errors)
    {
        return errors.Select(error => error.Code + "|" + error.ChunkType + "|" + error.Offset + "|" + error.Message).ToArray();
    }

    static string[] DescribeWarnings(PngWarning[] warnings)
    {
        return warnings.Select(warning => warning.Code + "|" + warning.ChunkType + "|" + warning.Offset + "|" + warning.Message).ToArray();
    }

    static byte[] CorruptChunkCrc(byte[] data, string type)
    {
        byte[] result = data.ToArray();
        int offset = PngConstants.SignatureLength;
        while (offset <= result.Length - 12)
        {
            int length = ReadInt32(result, offset);
            string currentType = System.Text.Encoding.ASCII.GetString(result, offset + 4, 4);
            if (currentType == type)
            {
                result[offset + 8 + length] ^= 0xFF;
                return result;
            }

            offset += length + 12;
        }

        throw new InvalidOperationException("Chunk was not found.");
    }

    static int ReadInt32(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    static byte[] Append(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
