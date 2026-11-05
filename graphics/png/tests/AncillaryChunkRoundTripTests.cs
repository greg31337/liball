using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class AncillaryChunkRoundTripTests
{
    [TestCaseSource(nameof(CreateAncillaryChunks))]
    public void AncillaryChunk_WriterRawReaderAndParserRoundTrip(PngChunk chunk)
    {
        byte[] raw = PngRawChunkWriter.WriteToBytes(chunk);

        PngChunk readChunk = PngRawChunkReader.Read(raw, 0);

        Assert.That(readChunk.Type, Is.EqualTo(chunk.Type));
        Assert.That(readChunk.Data, Is.EqualTo(chunk.Data));
        Assert.That(readChunk.Length, Is.EqualTo((uint)chunk.Data.Length));
        Assert.That(readChunk.IsKnown, Is.True);
        Assert.That(readChunk.IsCritical, Is.False);
        Assert.That(readChunk.IsReservedBitValid, Is.True);
        Assert.That(readChunk.Crc, Is.EqualTo(PngCrc32.CalculateChunkCrc(readChunk.Type, readChunk.Data)));
        Assert.DoesNotThrow(() => ParseAncillaryChunk(readChunk));
    }

    static IEnumerable<TestCaseData> CreateAncillaryChunks()
    {
        yield return CreateCase("tRNS truecolor", PngTransparencyWriter.WriteTruecolorChunk(CreateTruecolorTransparency()));
        yield return CreateCase("cHRM", PngChromaticitiesWriter.WriteChunk(CreateChromaticities()));
        yield return CreateCase("gAMA", PngGammaWriter.WriteChunk(CreateGamma()));
        yield return CreateCase("iCCP", PngIccProfileWriter.WriteChunk(CreateIccProfile()));
        yield return CreateCase("sBIT truecolor-alpha", PngSignificantBitsWriter.WriteTruecolorAlphaChunk(CreateSignificantBits()));
        yield return CreateCase("sRGB", PngStandardRgbWriter.WriteChunk(CreateStandardRgb()));
        yield return CreateCase("cICP", PngCodingIndependentCodePointsWriter.WriteChunk(CreateCodingIndependentCodePoints()));
        yield return CreateCase("mDCV", PngMasteringDisplayColorVolumeWriter.WriteChunk(CreateMasteringDisplayColorVolume()));
        yield return CreateCase("cLLI", PngContentLightLevelInformationWriter.WriteChunk(CreateContentLightLevelInformation()));
        yield return CreateCase("tEXt", PngTextWriter.WriteChunk(CreateText()));
        yield return CreateCase("zTXt", PngCompressedTextWriter.WriteChunk(CreateCompressedText()));
        yield return CreateCase("iTXt", PngInternationalTextWriter.WriteChunk(CreateInternationalText()));
        yield return CreateCase("bKGD truecolor", PngBackgroundColorWriter.WriteTruecolorChunk(CreateBackgroundColor()));
        yield return CreateCase("hIST", PngPaletteHistogramWriter.WriteChunk(CreatePaletteHistogram()));
        yield return CreateCase("pHYs", PngPhysicalPixelsWriter.WriteChunk(CreatePhysicalPixels()));
        yield return CreateCase("sPLT", PngSuggestedPaletteWriter.WriteChunk(CreateSuggestedPalette()));
        yield return CreateCase("eXIf", PngExifProfileWriter.WriteChunk(CreateExifProfile()));
        yield return CreateCase("tIME", PngTimeWriter.WriteChunk(CreateTime()));
        yield return CreateCase("acTL", PngAnimationControlWriter.WriteChunk(CreateAnimationControl()));
        yield return CreateCase("fcTL", PngFrameControlWriter.WriteChunk(CreateFrameControl()));
        yield return CreateCase("fdAT", PngFrameDataWriter.WriteChunk(CreateFrameData()));
    }

    static TestCaseData CreateCase(string name, PngChunk chunk)
    {
        return new TestCaseData(chunk).SetName(name);
    }

    static void ParseAncillaryChunk(PngChunk chunk)
    {
        switch (chunk.Type)
        {
            case PngConstants.ChunkTransparency:
                PngTransparencyParser.ParseTruecolor(chunk);
                return;
            case PngConstants.ChunkChromaticities:
                PngChromaticitiesParser.Parse(chunk);
                return;
            case PngConstants.ChunkGamma:
                PngGammaParser.Parse(chunk);
                return;
            case PngConstants.ChunkIccProfile:
                PngIccProfileParser.Parse(chunk);
                return;
            case PngConstants.ChunkSignificantBits:
                PngSignificantBitsParser.ParseTruecolorAlpha(chunk);
                return;
            case PngConstants.ChunkStandardRgb:
                PngStandardRgbParser.Parse(chunk);
                return;
            case PngConstants.ChunkCodingIndependentCodePoints:
                PngCodingIndependentCodePointsParser.Parse(chunk);
                return;
            case PngConstants.ChunkMasteringDisplayColorVolume:
                PngMasteringDisplayColorVolumeParser.Parse(chunk);
                return;
            case PngConstants.ChunkContentLightLevelInformation:
                PngContentLightLevelInformationParser.Parse(chunk);
                return;
            case PngConstants.ChunkText:
                PngTextParser.Parse(chunk);
                return;
            case PngConstants.ChunkCompressedText:
                PngCompressedTextParser.Parse(chunk);
                return;
            case PngConstants.ChunkInternationalText:
                PngInternationalTextParser.Parse(chunk);
                return;
            case PngConstants.ChunkBackgroundColor:
                PngBackgroundColorParser.ParseTruecolor(chunk);
                return;
            case PngConstants.ChunkPaletteHistogram:
                PngPaletteHistogramParser.Parse(chunk);
                return;
            case PngConstants.ChunkPhysicalPixels:
                PngPhysicalPixelsParser.Parse(chunk);
                return;
            case PngConstants.ChunkSuggestedPalette:
                PngSuggestedPaletteParser.Parse(chunk);
                return;
            case PngConstants.ChunkExif:
                PngExifProfileParser.Parse(chunk);
                return;
            case PngConstants.ChunkTime:
                PngTimeParser.Parse(chunk);
                return;
            case PngConstants.ChunkAnimationControl:
                PngAnimationControlParser.Parse(chunk);
                return;
            case PngConstants.ChunkFrameControl:
                PngFrameControlParser.Parse(chunk);
                return;
            case PngConstants.ChunkFrameData:
                PngFrameDataParser.Parse(chunk);
                return;
            default:
                throw new InvalidOperationException("Unsupported ancillary chunk type.");
        }
    }

    static PngTruecolorTransparency CreateTruecolorTransparency()
    {
        PngTruecolorTransparency transparency = new PngTruecolorTransparency();
        transparency.Red = 1;
        transparency.Green = 2;
        transparency.Blue = 3;
        return transparency;
    }

    static PngChromaticities CreateChromaticities()
    {
        PngChromaticities chromaticities = new PngChromaticities();
        chromaticities.WhitePointX = 1;
        chromaticities.WhitePointY = 2;
        chromaticities.RedX = 3;
        chromaticities.RedY = 4;
        chromaticities.GreenX = 5;
        chromaticities.GreenY = 6;
        chromaticities.BlueX = 7;
        chromaticities.BlueY = 8;
        return chromaticities;
    }

    static PngGamma CreateGamma()
    {
        PngGamma gamma = new PngGamma();
        gamma.Value = 45455;
        return gamma;
    }

    static PngIccProfile CreateIccProfile()
    {
        PngIccProfile profile = new PngIccProfile();
        profile.Name = "Display";
        profile.CompressionMethod = PngIccProfileParser.CompressionMethodDeflate;
        profile.Profile = new byte[] { 1, 2, 3, 4, 5 };
        return profile;
    }

    static PngSignificantBits CreateSignificantBits()
    {
        PngSignificantBits bits = new PngSignificantBits();
        bits.Gray = 3;
        bits.Red = 5;
        bits.Green = 6;
        bits.Blue = 7;
        bits.Alpha = 8;
        return bits;
    }

    static PngStandardRgb CreateStandardRgb()
    {
        PngStandardRgb standardRgb = new PngStandardRgb();
        standardRgb.RenderingIntent = 1;
        return standardRgb;
    }

    static PngCodingIndependentCodePoints CreateCodingIndependentCodePoints()
    {
        PngCodingIndependentCodePoints codePoints = new PngCodingIndependentCodePoints();
        codePoints.ColorPrimaries = 9;
        codePoints.TransferCharacteristics = 16;
        codePoints.MatrixCoefficients = 9;
        codePoints.VideoFullRangeFlag = 1;
        return codePoints;
    }

    static PngMasteringDisplayColorVolume CreateMasteringDisplayColorVolume()
    {
        PngMasteringDisplayColorVolume volume = new PngMasteringDisplayColorVolume();
        volume.RedX = 1;
        volume.RedY = 2;
        volume.GreenX = 3;
        volume.GreenY = 4;
        volume.BlueX = 5;
        volume.BlueY = 6;
        volume.WhitePointX = 7;
        volume.WhitePointY = 8;
        volume.MaximumLuminance = 1000;
        volume.MinimumLuminance = 1;
        return volume;
    }

    static PngContentLightLevelInformation CreateContentLightLevelInformation()
    {
        PngContentLightLevelInformation lightLevels = new PngContentLightLevelInformation();
        lightLevels.MaximumContentLightLevel = 1000;
        lightLevels.MaximumFrameAverageLightLevel = 400;
        return lightLevels;
    }

    static PngText CreateText()
    {
        PngText text = new PngText();
        text.Keyword = "Comment";
        text.Text = "Line";
        return text;
    }

    static PngCompressedText CreateCompressedText()
    {
        PngCompressedText text = new PngCompressedText();
        text.Keyword = "Comment";
        text.CompressionMethod = PngCompressedTextParser.CompressionMethodDeflate;
        text.Text = "Compressed";
        return text;
    }

    static PngInternationalText CreateInternationalText()
    {
        PngInternationalText text = new PngInternationalText();
        text.Keyword = "Comment";
        text.CompressionFlag = PngInternationalTextParser.CompressionFlagCompressed;
        text.CompressionMethod = PngInternationalTextParser.CompressionMethodDeflate;
        text.LanguageTag = "en";
        text.TranslatedKeyword = "Comment";
        text.Text = "Snow";
        return text;
    }

    static PngBackgroundColor CreateBackgroundColor()
    {
        PngBackgroundColor background = new PngBackgroundColor();
        background.Red = 1;
        background.Green = 2;
        background.Blue = 3;
        return background;
    }

    static PngPaletteHistogram CreatePaletteHistogram()
    {
        PngPaletteHistogram histogram = new PngPaletteHistogram();
        histogram.Frequencies = new ushort[] { 1, 2, 3 };
        return histogram;
    }

    static PngPhysicalPixels CreatePhysicalPixels()
    {
        PngPhysicalPixels physicalPixels = new PngPhysicalPixels();
        physicalPixels.PixelsPerUnitX = 2835;
        physicalPixels.PixelsPerUnitY = 2835;
        physicalPixels.UnitSpecifier = PngPhysicalPixelsParser.UnitMetre;
        return physicalPixels;
    }

    static PngSuggestedPalette CreateSuggestedPalette()
    {
        PngSuggestedPaletteEntry entry = new PngSuggestedPaletteEntry();
        entry.Red = 1;
        entry.Green = 2;
        entry.Blue = 3;
        entry.Alpha = 255;
        entry.Frequency = 4;

        PngSuggestedPalette palette = new PngSuggestedPalette();
        palette.Name = "Web";
        palette.SampleDepth = PngSuggestedPaletteParser.SampleDepth8;
        palette.Entries = new PngSuggestedPaletteEntry[] { entry };
        return palette;
    }

    static PngExifProfile CreateExifProfile()
    {
        PngExifProfile profile = new PngExifProfile();
        profile.Profile = new byte[] { 0x49, 0x49, 0x2A, 0x00 };
        return profile;
    }

    static PngTime CreateTime()
    {
        PngTime time = new PngTime();
        time.Year = 2024;
        time.Month = 8;
        time.Day = 4;
        time.Hour = 23;
        time.Minute = 59;
        time.Second = 60;
        return time;
    }

    static PngAnimationControl CreateAnimationControl()
    {
        PngAnimationControl control = new PngAnimationControl();
        control.FrameCount = 3;
        control.PlayCount = 5;
        return control;
    }

    static PngFrameControl CreateFrameControl()
    {
        PngFrameControl control = new PngFrameControl();
        control.SequenceNumber = 1;
        control.Width = 2;
        control.Height = 3;
        control.XOffset = 4;
        control.YOffset = 5;
        control.DelayNumerator = 6;
        control.DelayDenominator = 7;
        control.DisposeOperation = PngApngDisposeOperation.Background;
        control.BlendOperation = PngApngBlendOperation.Over;
        return control;
    }

    static PngFrameData CreateFrameData()
    {
        PngFrameData frameData = new PngFrameData();
        frameData.SequenceNumber = 2;
        frameData.Data = new byte[] { 1, 2, 3 };
        return frameData;
    }
}
