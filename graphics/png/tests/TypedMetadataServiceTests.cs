using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class TypedMetadataServiceTests
{
    [Test]
    public void SingletonSetters_AddOrReplaceEveryTypedMetadataFamily()
    {
        IPngMetadataEditor editor = new PngMetadataService();
        PngMetadata metadata = CreateMetadata();

        PngIndexedTransparency indexed = new PngIndexedTransparency();
        indexed.Alpha = new byte[] { 1, 2, 3 };
        metadata = editor.SetIndexedTransparency(metadata, indexed);
        Assert.That(metadata.IndexedTransparency!.Alpha, Is.EqualTo(indexed.Alpha));

        PngGrayscaleTransparency grayscale = new PngGrayscaleTransparency();
        grayscale.Gray = 123;
        metadata = editor.SetGrayscaleTransparency(metadata, grayscale);
        Assert.That(metadata.GrayscaleTransparency!.Gray, Is.EqualTo(123));
        Assert.That(metadata.IndexedTransparency, Is.Null);

        PngTruecolorTransparency truecolor = new PngTruecolorTransparency();
        truecolor.Red = 4;
        truecolor.Green = 5;
        truecolor.Blue = 6;
        metadata = editor.SetTruecolorTransparency(metadata, truecolor);
        Assert.That(metadata.TruecolorTransparency!.Blue, Is.EqualTo(6));
        Assert.That(CountChunks(metadata, PngConstants.ChunkTransparency), Is.EqualTo(1));

        PngChromaticities chromaticities = new PngChromaticities();
        chromaticities.WhitePointX = 31270;
        metadata = editor.SetChromaticities(metadata, chromaticities);
        Assert.That(metadata.Chromaticities!.WhitePointX, Is.EqualTo(31270));

        PngGamma gamma = new PngGamma();
        gamma.Value = 45455;
        metadata = editor.SetGamma(metadata, gamma);
        Assert.That(metadata.Gamma!.Value, Is.EqualTo(45455));

        PngIccProfile icc = new PngIccProfile();
        icc.Name = "Display";
        icc.CompressionMethod = PngIccProfileParser.CompressionMethodDeflate;
        icc.Profile = new byte[] { 8, 6, 7, 5 };
        metadata = editor.SetIccProfile(metadata, icc);
        Assert.That(metadata.IccProfile!.Profile, Is.EqualTo(icc.Profile));

        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = 5;
        metadata = editor.SetGrayscaleSignificantBits(metadata, bits);
        Assert.That(metadata.SignificantBits!.Gray, Is.EqualTo(5));
        bits.Alpha = 6;
        metadata = editor.SetGrayscaleAlphaSignificantBits(metadata, bits);
        Assert.That(metadata.SignificantBits!.Alpha, Is.EqualTo(6));
        bits.Red = 7;
        bits.Green = 6;
        bits.Blue = 5;
        metadata = editor.SetIndexedSignificantBits(metadata, bits);
        Assert.That(metadata.SignificantBits!.Red, Is.EqualTo(7));
        metadata = editor.SetTruecolorSignificantBits(metadata, bits);
        Assert.That(metadata.SignificantBits!.Green, Is.EqualTo(6));
        bits.Alpha = 4;
        metadata = editor.SetTruecolorAlphaSignificantBits(metadata, bits);
        Assert.That(metadata.SignificantBits!.Alpha, Is.EqualTo(4));
        Assert.That(CountChunks(metadata, PngConstants.ChunkSignificantBits), Is.EqualTo(1));

        PngStandardRgb standardRgb = new PngStandardRgb();
        standardRgb.RenderingIntent = 2;
        metadata = editor.SetStandardRgb(metadata, standardRgb);
        Assert.That(metadata.StandardRgb!.RenderingIntent, Is.EqualTo(2));

        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = 1;
        codePoints.TransferCharacteristics = 13;
        codePoints.MatrixCoefficients = 0;
        codePoints.VideoFullRangeFlag = 1;
        metadata = editor.SetCodingIndependentCodePoints(metadata, codePoints);
        Assert.That(metadata.CodingIndependentCodePoints!.TransferCharacteristics, Is.EqualTo(13));
        Assert.That(metadata.ColorMetadata.SourceChunkType, Is.EqualTo(PngConstants.ChunkCodingIndependentCodePoints));

        PngMasteringDisplayColorVolume volume = new PngMasteringDisplayColorVolume();
        volume.MaximumLuminance = 1000;
        metadata = editor.SetMasteringDisplayColorVolume(metadata, volume);
        Assert.That(metadata.MasteringDisplayColorVolume!.MaximumLuminance, Is.EqualTo(1000));

        PngContentLightLevelInformation lightLevels = new PngContentLightLevelInformation();
        lightLevels.MaximumContentLightLevel = 800;
        lightLevels.MaximumFrameAverageLightLevel = 400;
        metadata = editor.SetContentLightLevelInformation(metadata, lightLevels);
        Assert.That(metadata.ContentLightLevelInformation!.MaximumFrameAverageLightLevel, Is.EqualTo(400));

        PngBackgroundColor background = new PngBackgroundColor();
        background.PaletteIndex = 2;
        metadata = editor.SetIndexedBackgroundColor(metadata, background);
        Assert.That(metadata.BackgroundColor!.PaletteIndex, Is.EqualTo(2));
        background.Gray = 10;
        metadata = editor.SetGrayscaleBackgroundColor(metadata, background);
        Assert.That(metadata.BackgroundColor!.Gray, Is.EqualTo(10));
        background.Red = 11;
        background.Green = 12;
        background.Blue = 13;
        metadata = editor.SetTruecolorBackgroundColor(metadata, background);
        Assert.That(metadata.BackgroundColor!.Blue, Is.EqualTo(13));
        Assert.That(CountChunks(metadata, PngConstants.ChunkBackgroundColor), Is.EqualTo(1));

        PngPaletteHistogram histogram = new PngPaletteHistogram();
        histogram.Frequencies = new ushort[] { 10, 20 };
        metadata = editor.SetPaletteHistogram(metadata, histogram);
        Assert.That(metadata.PaletteHistogram!.Frequencies, Is.EqualTo(histogram.Frequencies));

        PngPhysicalPixels physical = new PngPhysicalPixels();
        physical.PixelsPerUnitX = 3780;
        physical.PixelsPerUnitY = 3780;
        physical.UnitSpecifier = PngPhysicalPixelsParser.UnitMetre;
        metadata = editor.SetPhysicalDimensions(metadata, physical);
        Assert.That(metadata.PhysicalPixels!.PixelsPerUnitX, Is.EqualTo(3780));

        PngExifProfile exif = new PngExifProfile();
        exif.Profile = new byte[] { 0x49, 0x49, 0x2A, 0x00 };
        metadata = editor.SetExifProfile(metadata, exif);
        Assert.That(metadata.ExifProfile!.Profile, Is.EqualTo(exif.Profile));

        PngTime time = new PngTime();
        time.Year = 2026;
        time.Month = 8;
        time.Day = 26;
        metadata = editor.SetTime(metadata, time);
        Assert.That(metadata.Time!.Year, Is.EqualTo(2026));

        PngAnimationControl animation = new PngAnimationControl();
        animation.FrameCount = 2;
        animation.PlayCount = 3;
        metadata = editor.SetAnimationControl(metadata, animation);
        Assert.That(metadata.AnimationControl!.FrameCount, Is.EqualTo(2));
        Assert.That(metadata.UnknownChunks, Has.Length.EqualTo(1));
        Assert.That(metadata.UnknownChunks[0].Type, Is.EqualTo("vpAg"));
    }

    [Test]
    public void RepeatableEditors_AddReplaceAndPopulateTypedArrays()
    {
        IPngMetadataEditor editor = new PngMetadataService();
        PngMetadata metadata = CreateMetadata();
        metadata = editor.AddLatin1Text(metadata, CreateLatin1Text("Title", "One"));
        metadata = editor.AddCompressedText(metadata, CreateCompressedText("Comment", "Two"));
        metadata = editor.AddInternationalText(metadata, CreateInternationalText("Description", "Three"));

        Assert.That(metadata.TextEntries, Has.Length.EqualTo(3));
        Assert.That(metadata.Latin1TextEntries, Has.Length.EqualTo(1));
        Assert.That(metadata.CompressedTextEntries, Has.Length.EqualTo(1));
        Assert.That(metadata.InternationalTextEntries, Has.Length.EqualTo(1));

        metadata = editor.ReplaceLatin1Text(metadata, new PngText[]
        {
            CreateLatin1Text("Title", "First"),
            CreateLatin1Text("Author", "Second")
        });
        metadata = editor.ReplaceCompressedText(metadata, new PngCompressedText[]
        {
            CreateCompressedText("Comment", "Replacement")
        });
        metadata = editor.ReplaceInternationalText(metadata, new PngInternationalText[]
        {
            CreateInternationalText("Description", "International")
        });

        Assert.That(metadata.TextEntries, Has.Length.EqualTo(4));
        Assert.That(metadata.Latin1TextEntries, Has.Length.EqualTo(2));
        Assert.That(metadata.CompressedTextEntries[0].Text, Is.EqualTo("Replacement"));
        Assert.That(metadata.InternationalTextEntries[0].Text, Is.EqualTo("International"));
        Assert.That(metadata.TextBytes, Is.EqualTo(35));

        metadata = editor.AddSuggestedPalette(metadata, CreateSuggestedPalette("First", 1));
        metadata = editor.AddSuggestedPalette(metadata, CreateSuggestedPalette("Second", 2));
        Assert.That(metadata.SuggestedPalettes, Has.Length.EqualTo(2));
        metadata = editor.ReplaceSuggestedPalettes(metadata, new PngSuggestedPalette[] { CreateSuggestedPalette("Only", 3) });
        Assert.That(metadata.SuggestedPalettes, Has.Length.EqualTo(1));
        Assert.That(metadata.SuggestedPalettes[0].Name, Is.EqualTo("Only"));

        metadata = editor.AddFrameControl(metadata, CreateFrameControl(0));
        metadata = editor.ReplaceFrameControls(metadata, new PngFrameControl[]
        {
            CreateFrameControl(2),
            CreateFrameControl(4)
        });
        Assert.That(metadata.FrameControls, Has.Length.EqualTo(2));
        Assert.That(metadata.FrameControls[1].SequenceNumber, Is.EqualTo(4));

        long metadataBytesBeforeFrameData = metadata.MetadataBytes;
        metadata = editor.AddFrameData(metadata, CreateFrameData(1, 7));
        Assert.That(metadata.MetadataBytes, Is.EqualTo(metadataBytesBeforeFrameData));
        metadata = editor.ReplaceFrameData(metadata, new PngFrameData[]
        {
            CreateFrameData(3, 8),
            CreateFrameData(5, 9)
        });
        Assert.That(metadata.FrameData, Has.Length.EqualTo(2));
        Assert.That(metadata.FrameData[1].Data, Is.EqualTo(new byte[] { 9 }));
        Assert.That(metadata.MetadataBytes, Is.EqualTo(metadataBytesBeforeFrameData));
    }

    [Test]
    public void TypedRemovers_ClearChunksAndTypedFieldsForEveryFamily()
    {
        IPngMetadataEditor editor = new PngMetadataService();
        PngMetadata metadata = CreateAllFamilies(editor);

        metadata = editor.RemoveTransparency(metadata);
        metadata = editor.RemoveChromaticities(metadata);
        metadata = editor.RemoveGamma(metadata);
        metadata = editor.RemoveIccProfile(metadata);
        metadata = editor.RemoveSignificantBits(metadata);
        metadata = editor.RemoveStandardRgb(metadata);
        metadata = editor.RemoveCodingIndependentCodePoints(metadata);
        metadata = editor.RemoveMasteringDisplayColorVolume(metadata);
        metadata = editor.RemoveContentLightLevelInformation(metadata);
        metadata = editor.RemoveLatin1Text(metadata);
        metadata = editor.RemoveCompressedText(metadata);
        metadata = editor.RemoveInternationalText(metadata);
        metadata = editor.RemoveBackgroundColor(metadata);
        metadata = editor.RemovePaletteHistogram(metadata);
        metadata = editor.RemovePhysicalDimensions(metadata);
        metadata = editor.RemoveSuggestedPalettes(metadata);
        metadata = editor.RemoveExifProfile(metadata);
        metadata = editor.RemoveTime(metadata);
        metadata = editor.RemoveAnimationControl(metadata);
        metadata = editor.RemoveFrameControls(metadata);
        metadata = editor.RemoveFrameData(metadata);

        Assert.That(metadata.Chunks, Has.Length.EqualTo(1));
        Assert.That(metadata.Chunks[0].Type, Is.EqualTo("vpAg"));
        Assert.That(metadata.TextEntries, Is.Empty);
        Assert.That(metadata.SuggestedPalettes, Is.Empty);
        Assert.That(metadata.FrameControls, Is.Empty);
        Assert.That(metadata.FrameData, Is.Empty);
        Assert.That(metadata.ColorMetadata.HasColorMetadata, Is.False);

        metadata = editor.AddLatin1Text(metadata, CreateLatin1Text("Title", "Again"));
        metadata = editor.AddCompressedText(metadata, CreateCompressedText("Comment", "Again"));
        metadata = editor.AddInternationalText(metadata, CreateInternationalText("Description", "Again"));
        metadata = editor.RemoveText(metadata);
        Assert.That(metadata.TextEntries, Is.Empty);
        Assert.That(metadata.TextEntryCount, Is.EqualTo(0));
    }

    [Test]
    public void TypedEditors_RejectNullModelsAndArrays()
    {
        IPngMetadataEditor editor = new PngMetadataService();
        PngMetadata metadata = CreateMetadata();

        Assert.Throws<ArgumentNullException>(() => editor.SetIndexedTransparency(metadata, null!));
        Assert.Throws<ArgumentNullException>(() => editor.SetChromaticities(metadata, null!));
        Assert.Throws<ArgumentNullException>(() => editor.AddInternationalText(metadata, null!));
        Assert.Throws<ArgumentNullException>(() => editor.ReplaceCompressedText(metadata, null!));
        Assert.Throws<ArgumentNullException>(() => editor.ReplaceSuggestedPalettes(metadata, null!));
        Assert.Throws<ArgumentNullException>(() => editor.ReplaceFrameControls(metadata, null!));
        Assert.Throws<ArgumentNullException>(() => editor.ReplaceFrameData(metadata, null!));
    }

    static PngMetadata CreateAllFamilies(IPngMetadataEditor editor)
    {
        PngMetadata metadata = CreateMetadata();
        PngTruecolorTransparency transparency = new PngTruecolorTransparency();
        metadata = editor.SetTruecolorTransparency(metadata, transparency);
        metadata = editor.SetChromaticities(metadata, new PngChromaticities());
        PngGamma gamma = new PngGamma();
        gamma.Value = 45455;
        metadata = editor.SetGamma(metadata, gamma);
        PngIccProfile icc = new PngIccProfile();
        icc.Name = "Profile";
        icc.Profile = new byte[] { 1 };
        metadata = editor.SetIccProfile(metadata, icc);
        metadata = editor.SetTruecolorSignificantBits(metadata, new PngSignificantBits());
        metadata = editor.SetStandardRgb(metadata, new PngStandardRgb());
        metadata = editor.SetCodingIndependentCodePoints(metadata, new PngCodingIndependentCodePoints());
        metadata = editor.SetMasteringDisplayColorVolume(metadata, new PngMasteringDisplayColorVolume());
        metadata = editor.SetContentLightLevelInformation(metadata, new PngContentLightLevelInformation());
        metadata = editor.AddLatin1Text(metadata, CreateLatin1Text("Title", "One"));
        metadata = editor.AddCompressedText(metadata, CreateCompressedText("Comment", "Two"));
        metadata = editor.AddInternationalText(metadata, CreateInternationalText("Description", "Three"));
        metadata = editor.SetTruecolorBackgroundColor(metadata, new PngBackgroundColor());
        PngPaletteHistogram histogram = new PngPaletteHistogram();
        histogram.Frequencies = new ushort[] { 1 };
        metadata = editor.SetPaletteHistogram(metadata, histogram);
        metadata = editor.SetPhysicalDimensions(metadata, new PngPhysicalPixels());
        metadata = editor.AddSuggestedPalette(metadata, CreateSuggestedPalette("Palette", 1));
        PngExifProfile exif = new PngExifProfile();
        exif.Profile = new byte[] { 0x49, 0x49, 0x2A, 0x00 };
        metadata = editor.SetExifProfile(metadata, exif);
        PngTime time = new PngTime();
        time.Month = 1;
        time.Day = 1;
        metadata = editor.SetTime(metadata, time);
        PngAnimationControl animation = new PngAnimationControl();
        animation.FrameCount = 1;
        metadata = editor.SetAnimationControl(metadata, animation);
        metadata = editor.AddFrameControl(metadata, CreateFrameControl(0));
        metadata = editor.AddFrameData(metadata, CreateFrameData(1, 1));
        return metadata;
    }

    static PngMetadata CreateMetadata()
    {
        PngChunk unknown = new PngChunk();
        unknown.Type = "vpAg";
        unknown.Data = new byte[] { 9 };
        unknown.Length = 1;
        PngMetadata metadata = new PngMetadata();
        metadata.Chunks = new PngChunk[] { unknown };
        metadata.UnknownChunks = new PngChunk[] { unknown };
        return metadata;
    }

    static PngText CreateLatin1Text(string keyword, string value)
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

    static PngInternationalText CreateInternationalText(string keyword, string value)
    {
        PngInternationalText text = new PngInternationalText();
        text.Keyword = keyword;
        text.CompressionFlag = PngInternationalTextParser.CompressionFlagCompressed;
        text.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        text.LanguageTag = "en";
        text.TranslatedKeyword = keyword;
        text.Text = value;
        return text;
    }

    static PngSuggestedPalette CreateSuggestedPalette(string name, byte red)
    {
        PngSuggestedPaletteEntry entry = new PngSuggestedPaletteEntry();
        entry.Red = red;
        entry.Alpha = 255;
        PngSuggestedPalette palette = new PngSuggestedPalette();
        palette.Name = name;
        palette.SampleDepth = PngSuggestedPaletteParser.SampleDepth8;
        palette.Entries = new PngSuggestedPaletteEntry[] { entry };
        return palette;
    }

    static PngFrameControl CreateFrameControl(uint sequenceNumber)
    {
        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = sequenceNumber;
        control.Width = 1;
        control.Height = 1;
        control.DelayDenominator = 100;
        control.DisposeOperation = PngApngDisposeOperation.None;
        control.BlendOperation = PngApngBlendOperation.Source;
        return control;
    }

    static PngFrameData CreateFrameData(uint sequenceNumber, byte value)
    {
        PngFrameData frameData = new PngFrameData();
        frameData.SequenceNumber = sequenceNumber;
        frameData.Data = new byte[] { value };
        return frameData;
    }

    static int CountChunks(PngMetadata metadata, string type)
    {
        int count = 0;
        int index = 0;
        while (index < metadata.Chunks.Length)
        {
            if (metadata.Chunks[index].Type == type)
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }
}
