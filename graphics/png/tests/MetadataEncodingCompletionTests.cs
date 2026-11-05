using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class MetadataEncodingCompletionTests
{
    [Test]
    public void Encode_MaterializesTypedMetadataAndUsesColorPrecedence()
    {
        PngImage image = CreateRgbaImage();
        image.Metadata.CodingIndependentCodePoints = CreateCodePoints();
        image.Metadata.IccProfile = CreateIccProfile();
        image.Metadata.StandardRgb = CreateStandardRgb();
        image.Metadata.Chromaticities = CreateChromaticities();
        image.Metadata.Gamma = CreateGamma(45455);
        image.Metadata.SignificantBits = CreateSignificantBits();
        image.Metadata.PhysicalPixels = CreatePhysicalPixels();
        image.Metadata.Time = CreateTime();
        image.Metadata.Latin1TextEntries = new PngText[] { CreateText("Title", "Typed metadata") };

        byte[] encoded = Png.EncodeToBytes(image, CreateOptions());
        PngChunk[] chunks = Png.ReadChunks(encoded).Chunks;
        PngImage decoded = Png.DecodeBytes(encoded);

        Assert.That(CountChunks(chunks, PngConstants.ChunkCodingIndependentCodePoints), Is.EqualTo(1));
        Assert.That(HasChunk(chunks, PngConstants.ChunkIccProfile), Is.False);
        Assert.That(HasChunk(chunks, PngConstants.ChunkStandardRgb), Is.False);
        Assert.That(HasChunk(chunks, PngConstants.ChunkChromaticities), Is.False);
        Assert.That(HasChunk(chunks, PngConstants.ChunkGamma), Is.False);
        Assert.That(HasChunk(chunks, PngConstants.ChunkSignificantBits), Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkPhysicalPixels), Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkTime), Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkText), Is.True);
        Assert.That(decoded.Metadata.ColorMetadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));
        Assert.That(decoded.Metadata.PhysicalPixels!.PixelsPerUnitX, Is.EqualTo(3780));
        Assert.That(decoded.Metadata.Latin1TextEntries[0].Text, Is.EqualTo("Typed metadata"));
    }

    [Test]
    public void Encode_AppliesMetadataSelectionBeforeTypedColorPrecedence()
    {
        PngImage image = CreateRgbaImage();
        image.Metadata.CodingIndependentCodePoints = CreateCodePoints();
        image.Metadata.Gamma = CreateGamma(45455);
        image.Metadata.PhysicalPixels = CreatePhysicalPixels();
        PngEncoderOptions options = CreateOptions();
        options.MetadataPolicy = PngMetadataPolicy.KeepSelected;
        options.MetadataChunkTypesToKeep = new string[] { PngConstants.ChunkGamma };

        PngChunk[] chunks = Png.ReadChunks(Png.EncodeToBytes(image, options)).Chunks;

        Assert.That(HasChunk(chunks, PngConstants.ChunkCodingIndependentCodePoints), Is.False);
        Assert.That(HasChunk(chunks, PngConstants.ChunkGamma), Is.True);
        Assert.That(HasChunk(chunks, PngConstants.ChunkPhysicalPixels), Is.False);
    }

    [Test]
    public void Encode_CanonicalizesMetadataAndRetainsUnknownChunkPlacement()
    {
        PngImage image = CreateRgbaImage();
        PngChunk beforeUnknown = CreateUnknownChunk("vpAg", 1, PngChunkPlacement.BeforePalette);
        PngChunk afterUnknown = CreateUnknownChunk("vpBg", 2, PngChunkPlacement.AfterImageData);
        image.Metadata.Chunks = new PngChunk[]
        {
            afterUnknown,
            PngTextWriter.WriteChunk(CreateText("Title", "Last")),
            PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels()),
            PngGammaWriter.WriteChunk(CreateGamma(45455)),
            PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodePoints()),
            beforeUnknown
        };

        PngChunk[] chunks = Png.ReadChunks(Png.EncodeToBytes(image, CreateOptions())).Chunks;
        int idat = FindChunk(chunks, PngConstants.ChunkIDAT, 0);

        Assert.That(FindChunk(chunks, PngConstants.ChunkCodingIndependentCodePoints, 0), Is.LessThan(FindChunk(chunks, PngConstants.ChunkGamma, 0)));
        Assert.That(FindChunk(chunks, PngConstants.ChunkGamma, 0), Is.LessThan(FindChunk(chunks, "vpAg", 0)));
        Assert.That(FindChunk(chunks, "vpAg", 0), Is.LessThan(FindChunk(chunks, PngConstants.ChunkPhysicalPixels, 0)));
        Assert.That(FindChunk(chunks, PngConstants.ChunkPhysicalPixels, 0), Is.LessThan(idat));
        Assert.That(FindChunk(chunks, PngConstants.ChunkTime, 0), Is.EqualTo(-1));
        Assert.That(FindChunk(chunks, PngConstants.ChunkText, 0), Is.GreaterThan(idat));
        Assert.That(FindChunk(chunks, "vpBg", 0), Is.GreaterThan(idat));
    }

    [Test]
    public void Encode_UsesUnknownChunkSubsetWithoutDuplicatingChunkListEntries()
    {
        PngImage image = CreateRgbaImage();
        PngChunk shared = CreateUnknownChunk("vpAg", 1, PngChunkPlacement.BeforeImageData);
        PngChunk unsafeChunk = CreateUnknownChunk("vpBG", 2, PngChunkPlacement.AfterImageData);
        image.Metadata.Chunks = new PngChunk[] { shared };
        image.Metadata.UnknownChunks = new PngChunk[] { shared, unsafeChunk };

        PngChunk[] changedChunks = Png.ReadChunks(Png.EncodeToBytes(image, CreateOptions())).Chunks;
        PngEncoderOptions unchanged = CreateOptions();
        unchanged.PixelDataChanged = false;
        PngChunk[] unchangedChunks = Png.ReadChunks(Png.EncodeToBytes(image, unchanged)).Chunks;

        Assert.That(CountChunks(changedChunks, "vpAg"), Is.EqualTo(1));
        Assert.That(HasChunk(changedChunks, "vpBG"), Is.False);
        Assert.That(CountChunks(unchangedChunks, "vpAg"), Is.EqualTo(1));
        Assert.That(HasChunk(unchangedChunks, "vpBG"), Is.True);
    }

    [Test]
    public void Encode_PreservesOptionalPaletteForTruecolorOutput()
    {
        PngImage image = CreateRgbaImage();
        PngPaletteEntry entry = new PngPaletteEntry();
        entry.Red = 90;
        entry.Green = 80;
        entry.Blue = 70;
        image.Metadata.Palette.Entries = new PngPaletteEntry[] { entry };
        PngEncoderOptions preserve = CreateOptions();
        PngEncoderOptions strip = CreateOptions();
        strip.MetadataPolicy = PngMetadataPolicy.Strip;

        PngChunk[] preservedChunks = Png.ReadChunks(Png.EncodeToBytes(image, preserve)).Chunks;
        PngChunk[] strippedChunks = Png.ReadChunks(Png.EncodeToBytes(image, strip)).Chunks;

        Assert.That(FindChunk(preservedChunks, PngConstants.ChunkPLTE, 0), Is.GreaterThan(FindChunk(preservedChunks, PngConstants.ChunkIHDR, 0)));
        Assert.That(FindChunk(preservedChunks, PngConstants.ChunkPLTE, 0), Is.LessThan(FindChunk(preservedChunks, PngConstants.ChunkIDAT, 0)));
        Assert.That(HasChunk(strippedChunks, PngConstants.ChunkPLTE), Is.False);
    }

    [Test]
    public void Encode_AppliesForcedAndAutomaticTextCompressionChoices()
    {
        PngImage image = CreateRgbaImage();
        image.Metadata.Chunks = new PngChunk[]
        {
            PngTextWriter.WriteChunk(CreateText("Short", "tiny")),
            PngTextWriter.WriteChunk(CreateText("Long", "0123456789")),
            PngInternationalTextWriter.WriteChunk(CreateInternationalText("International text", false))
        };
        PngEncoderOptions automatic = CreateOptions();
        automatic.TextCompressionMode = PngTextCompressionMode.Automatic;
        automatic.TextCompressionThreshold = 8;
        PngEncoderOptions compressed = CreateOptions();
        compressed.TextCompressionMode = PngTextCompressionMode.Compressed;
        PngEncoderOptions uncompressed = CreateOptions();
        uncompressed.TextCompressionMode = PngTextCompressionMode.Uncompressed;

        PngChunk[] automaticChunks = Png.ReadChunks(Png.EncodeToBytes(image, automatic)).Chunks;
        PngChunk[] compressedChunks = Png.ReadChunks(Png.EncodeToBytes(image, compressed)).Chunks;
        PngChunk[] uncompressedChunks = Png.ReadChunks(Png.EncodeToBytes(image, uncompressed)).Chunks;

        Assert.That(CountChunks(automaticChunks, PngConstants.ChunkText), Is.EqualTo(1));
        Assert.That(CountChunks(automaticChunks, PngConstants.ChunkCompressedText), Is.EqualTo(1));
        Assert.That(PngInternationalTextParser.Parse(FindRequiredChunk(automaticChunks, PngConstants.ChunkInternationalText)).CompressionFlag, Is.EqualTo(PngInternationalTextParser.CompressionFlagCompressed));
        Assert.That(CountChunks(compressedChunks, PngConstants.ChunkText), Is.EqualTo(0));
        Assert.That(CountChunks(compressedChunks, PngConstants.ChunkCompressedText), Is.EqualTo(2));
        Assert.That(PngInternationalTextParser.Parse(FindRequiredChunk(compressedChunks, PngConstants.ChunkInternationalText)).CompressionFlag, Is.EqualTo(PngInternationalTextParser.CompressionFlagCompressed));
        Assert.That(CountChunks(uncompressedChunks, PngConstants.ChunkText), Is.EqualTo(2));
        Assert.That(CountChunks(uncompressedChunks, PngConstants.ChunkCompressedText), Is.EqualTo(0));
        Assert.That(PngInternationalTextParser.Parse(FindRequiredChunk(uncompressedChunks, PngConstants.ChunkInternationalText)).CompressionFlag, Is.EqualTo(PngInternationalTextParser.CompressionFlagUncompressed));
    }

    [Test]
    public void Encode_ProducesStableMetadataEditorRoundTrips()
    {
        PngImage image = CreateRgbaImage();
        image.Metadata.Chunks = new PngChunk[]
        {
            PngGammaWriter.WriteChunk(CreateGamma(45455)),
            PngTextWriter.WriteChunk(CreateText("Title", "Original"))
        };
        PngEncoderOptions options = CreateOptions();
        PngImage decoded = Png.DecodeBytes(Png.EncodeToBytes(image, options));
        PngMetadataService service = new PngMetadataService();
        decoded.Metadata = service.SetGamma(decoded.Metadata, CreateGamma(50000));
        decoded.Metadata = service.AddCompressedText(decoded.Metadata, CreateCompressedText("Comment", "Edited"));

        byte[] edited = Png.EncodeToBytes(decoded, options);
        PngImage decodedAgain = Png.DecodeBytes(edited);
        byte[] reencoded = Png.EncodeToBytes(decodedAgain, options);

        Assert.That(reencoded, Is.EqualTo(edited));
        Assert.That(decodedAgain.Metadata.Gamma!.Value, Is.EqualTo(50000));
        Assert.That(decodedAgain.Metadata.CompressedTextEntries[0].Text, Is.EqualTo("Edited"));
    }

    [Test]
    public void Encode_RegeneratesAnimationChunksInsteadOfCopyingStaleMetadata()
    {
        PngImage image = CreateRgbaImage();
        image.Animation.FrameCount = 1;
        image.Animation.PlayCount = 3;
        image.Animation.Frames = new PngFrame[] { CreateDefaultFrame() };
        PngAnimationControl staleControl = new PngAnimationControl();
        staleControl.FrameCount = 9;
        staleControl.PlayCount = 99;
        PngFrameData staleData = new PngFrameData();
        staleData.SequenceNumber = 50;
        staleData.Data = new byte[] { 1, 2, 3 };
        image.Metadata.Chunks = new PngChunk[]
        {
            PngAnimationControlWriter.WriteChunk(staleControl),
            PngFrameDataWriter.WriteChunk(staleData)
        };
        PngEncoderOptions options = CreateOptions();
        options.EncodeAnimation = true;

        PngChunk[] chunks = Png.ReadChunks(Png.EncodeToBytes(image, options)).Chunks;
        PngAnimationControl actualControl = PngAnimationControlParser.Parse(FindRequiredChunk(chunks, PngConstants.ChunkAnimationControl));

        Assert.That(CountChunks(chunks, PngConstants.ChunkAnimationControl), Is.EqualTo(1));
        Assert.That(CountChunks(chunks, PngConstants.ChunkFrameControl), Is.EqualTo(1));
        Assert.That(CountChunks(chunks, PngConstants.ChunkFrameData), Is.EqualTo(0));
        Assert.That(actualControl.FrameCount, Is.EqualTo(1));
        Assert.That(actualControl.PlayCount, Is.EqualTo(3));
    }

    static PngImage CreateRgbaImage()
    {
        return Png.CreateImage(1, 1, PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 255 });
    }

    static PngEncoderOptions CreateOptions()
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.ColorType = PngColorType.TruecolorAlpha;
        options.BitDepth = PngBitDepth.Eight;
        options.UseFixedFilter = true;
        options.FixedFilterType = PngFilterType.None;
        return options;
    }

    static PngCodingIndependentCodePoints CreateCodePoints()
    {
        PngCodingIndependentCodePoints value = new PngCodingIndependentCodePoints();
        value.ColorPrimaries = 1;
        value.TransferCharacteristics = 13;
        value.MatrixCoefficients = 0;
        value.VideoFullRangeFlag = 1;
        return value;
    }

    static PngIccProfile CreateIccProfile()
    {
        PngIccProfile value = new PngIccProfile();
        value.Name = "Profile";
        value.CompressionMethod = PngIccProfileParser.CompressionMethodDeflate;
        value.Profile = new byte[] { 1, 2, 3, 4 };
        return value;
    }

    static PngStandardRgb CreateStandardRgb()
    {
        PngStandardRgb value = new PngStandardRgb();
        value.RenderingIntent = 0;
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

    static PngGamma CreateGamma(uint value)
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = value;
        return gamma;
    }

    static PngSignificantBits CreateSignificantBits()
    {
        PngSignificantBits bits = new PngSignificantBits();
        bits.Red = 8;
        bits.Green = 8;
        bits.Blue = 8;
        bits.Alpha = 8;
        return bits;
    }

    static PngPhysicalPixels CreatePhysicalPixels()
    {
        PngPhysicalPixels value = new PngPhysicalPixels();
        value.PixelsPerUnitX = 3780;
        value.PixelsPerUnitY = 3780;
        value.UnitSpecifier = 1;
        return value;
    }

    static PngTime CreateTime()
    {
        PngTime value = new PngTime();
        value.Year = 2026;
        value.Month = 8;
        value.Day = 26;
        value.Hour = 12;
        value.Minute = 30;
        value.Second = 0;
        return value;
    }

    static PngText CreateText(string keyword, string value)
    {
        PngText text = new PngText();
        text.Keyword = keyword;
        text.Text = value;
        return text;
    }

    static PngCompressedText CreateCompressedText(string keyword, string value)
    {
        PngCompressedText text = new PngCompressedText();
        text.Keyword = keyword;
        text.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
        text.Text = value;
        return text;
    }

    static PngInternationalText CreateInternationalText(string value, bool compressed)
    {
        PngInternationalText text = new PngInternationalText();
        text.Keyword = "Comment";
        text.CompressionFlag = compressed ? PngInternationalTextParser.CompressionFlagCompressed : PngInternationalTextParser.CompressionFlagUncompressed;
        text.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        text.LanguageTag = "en";
        text.TranslatedKeyword = "Comment";
        text.Text = value;
        return text;
    }

    static PngFrame CreateDefaultFrame()
    {
        PngFrame frame = new PngFrame();
        frame.Width = 1;
        frame.Height = 1;
        frame.Pixels = CreateRgbaImage().Pixels;
        frame.DelayNumerator = 1;
        frame.DelayDenominator = 10;
        frame.BlendOperation = PngApngBlendOperation.Source;
        frame.DisposeOperation = PngApngDisposeOperation.None;
        frame.UsesDefaultImage = true;
        return frame;
    }

    static PngChunk CreateUnknownChunk(string type, byte value, PngChunkPlacement placement)
    {
        PngChunk chunk = PngUnknownChunkPreservation.CreateChunk(type, new byte[] { value }, 0);
        chunk.Placement = placement;
        chunk.PreservationState = PngChunkPreservationState.Preserved;
        return chunk;
    }

    static bool HasChunk(PngChunk[] chunks, string type)
    {
        return FindChunk(chunks, type, 0) >= 0;
    }

    static int CountChunks(PngChunk[] chunks, string type)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    static int FindChunk(PngChunk[] chunks, string type, int occurrence)
    {
        int seen = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                if (seen == occurrence)
                {
                    return index;
                }

                seen += 1;
            }

            index += 1;
        }

        return -1;
    }

    static PngChunk FindRequiredChunk(PngChunk[] chunks, string type)
    {
        int index = FindChunk(chunks, type, 0);
        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        return chunks[index];
    }
}
