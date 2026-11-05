using System.Collections.Generic;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class PublicCodecEndToEndTests
{
    [TestCaseSource(nameof(CreateCombinationCases))]
    public void PublicCodec_RoundTripsEveryValidColorDepthAndInterlaceCombination(PngColorType colorType, PngBitDepth bitDepth, PngPixelFormat format, PngInterlaceMethod interlace)
    {
        IPngImageFactory factory = new PngImageFactory();
        IPngEncoder encoder = new PngCodec();
        IPngDecoder decoder = new PngCodec();
        byte[] pixels = CreatePixels(format, 9, 5);
        PngImage image = factory.CreateImageWithData(9, 5, format, pixels);
        if (colorType == PngColorType.IndexedColor)
        {
            image.Metadata.Palette = CreatePalette(GetPaletteSize(bitDepth));
        }

        PngEncoderOptions options = CreateExplicitOptions(colorType, bitDepth, interlace);
        byte[] encoded = encoder.EncodeToBytes(image, options);
        PngValidationResult validation = new PngValidationService().ValidateBytes(encoded);
        PngIdentifyResult identified = new PngIdentifierService().IdentifyBytes(encoded);
        PngDecoderOptions decodeOptions = new PngDecoderOptions();
        decodeOptions.PreferredPixelFormat = PngPixelFormat.Unknown;
        PngImage decoded = decoder.DecodeBytes(encoded, decodeOptions);
        byte[] reencoded = encoder.EncodeToBytes(decoded, CreateExplicitOptions(colorType, bitDepth, interlace));
        PngImage decodedAgain = decoder.DecodeBytes(reencoded, decodeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(validation.IsValid, Is.True);
            Assert.That(identified.IsValid, Is.True);
            Assert.That(identified.Info.ColorType, Is.EqualTo(colorType));
            Assert.That(identified.Info.BitDepth, Is.EqualTo(bitDepth));
            Assert.That(identified.Info.InterlaceMethod, Is.EqualTo(interlace));
            Assert.That(decoded.Pixels.Format, Is.EqualTo(format));
            Assert.That(decoded.Pixels.Stride, Is.EqualTo(GetRowBytes(format, 9)));
            Assert.That(decoded.Pixels.Data, Is.EqualTo(pixels));
            Assert.That(decodedAgain.Pixels.Format, Is.EqualTo(format));
            Assert.That(decodedAgain.Pixels.Data, Is.EqualTo(pixels));
        });
    }

    [TestCaseSource(nameof(CreateFilterCases))]
    public void PublicCodec_RoundTripsEveryFilterStrategyInBothInterlaceModes(PngRowFilterStrategy strategy, PngFilterType fixedFilter, PngInterlaceMethod interlace)
    {
        byte[] pixels = CreatePixels(PngPixelFormat.Rgba8, 9, 5);
        PngImage image = new PngImageFactory().CreateImageWithData(9, 5, PngPixelFormat.Rgba8, pixels);
        PngEncoderOptions options = CreateExplicitOptions(PngColorType.TruecolorAlpha, PngBitDepth.Eight, interlace);
        options.RowFilterStrategy = strategy;
        options.UseFixedFilter = strategy == PngRowFilterStrategy.Fixed;
        options.FixedFilterType = fixedFilter;

        byte[] encoded = new PngCodec().EncodeToBytes(image, options);
        PngDecoderOptions decodeOptions = new PngDecoderOptions();
        decodeOptions.PreferredPixelFormat = PngPixelFormat.Unknown;
        PngImage decoded = new PngCodec().DecodeBytes(encoded, decodeOptions);

        Assert.That(new PngValidationService().ValidateBytes(encoded).IsValid, Is.True);
        Assert.That(decoded.Info.InterlaceMethod, Is.EqualTo(interlace));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(pixels));
        if (strategy == PngRowFilterStrategy.Fixed && interlace == PngInterlaceMethod.None)
        {
            AssertEveryNonInterlacedRowUsesFilter(encoded, fixedFilter, 9 * 4, 5);
        }
    }

    [Test]
    public void PublicDecoder_ProducesEveryReorderedRequestedOutputFormat()
    {
        PngImage image = new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[] { 10, 20, 30, 40 });
        byte[] encoded = new PngCodec().EncodeToBytes(image, CreateExplicitOptions(PngColorType.TruecolorAlpha, PngBitDepth.Eight, PngInterlaceMethod.None));
        PngPixelFormat[] formats = new PngPixelFormat[]
        {
            PngPixelFormat.Bgr8,
            PngPixelFormat.Bgr16,
            PngPixelFormat.Bgra8,
            PngPixelFormat.Bgra16,
            PngPixelFormat.Argb8,
            PngPixelFormat.Argb16
        };
        byte[][] expected = new byte[][]
        {
            new byte[] { 30, 20, 10 },
            new byte[] { 30, 30, 20, 20, 10, 10 },
            new byte[] { 30, 20, 10, 40 },
            new byte[] { 30, 30, 20, 20, 10, 10, 40, 40 },
            new byte[] { 40, 10, 20, 30 },
            new byte[] { 40, 40, 10, 10, 20, 20, 30, 30 }
        };

        int index = 0;
        while (index < formats.Length)
        {
            PngDecoderOptions options = new PngDecoderOptions();
            options.PreferredPixelFormat = formats[index];
            PngImage decoded = new PngCodec().DecodeBytes(encoded, options);
            Assert.That(decoded.Pixels.Format, Is.EqualTo(formats[index]));
            Assert.That(decoded.Pixels.Data, Is.EqualTo(expected[index]));
            index += 1;
        }
    }

    [Test]
    public void PublicCodec_AutomaticAnalysisAppliesEachLosslessReductionEndToEnd()
    {
        AssertAutomaticEncoding(
            new PngImageFactory().CreateImageWithData(4, 1, PngPixelFormat.Rgba8, new byte[] { 0, 0, 0, 255, 255, 255, 255, 255, 0, 0, 0, 255, 255, 255, 255, 255 }),
            PngColorType.Grayscale,
            PngBitDepth.One,
            PngPixelFormat.Rgba8,
            new byte[] { 0, 0, 0, 255, 255, 255, 255, 255, 0, 0, 0, 255, 255, 255, 255, 255 });

        AssertAutomaticEncoding(
            new PngImageFactory().CreateImageWithData(4, 1, PngPixelFormat.Grayscale8, new byte[] { 0, 85, 170, 255 }),
            PngColorType.Grayscale,
            PngBitDepth.Two,
            PngPixelFormat.Grayscale8,
            new byte[] { 0, 85, 170, 255 });

        byte[] sixteenBit = new byte[512];
        int sample = 0;
        while (sample < 256)
        {
            sixteenBit[sample * 2] = (byte)sample;
            sixteenBit[sample * 2 + 1] = (byte)sample;
            sample += 1;
        }

        AssertAutomaticEncoding(
            new PngImageFactory().CreateImageWithData(256, 1, PngPixelFormat.Grayscale16, sixteenBit),
            PngColorType.Grayscale,
            PngBitDepth.Eight,
            PngPixelFormat.Grayscale16,
            sixteenBit);

        PngImage indexed = new PngImageFactory().CreateImageWithData(4, 1, PngPixelFormat.Indexed8, new byte[] { 0, 1, 2, 1 });
        indexed.Metadata.Palette = CreatePalette(6);
        byte[] indexedEncoded = new PngCodec().EncodeToBytes(indexed, new PngEncoderOptions());
        PngIdentifyResult indexedInfo = new PngIdentifierService().IdentifyBytes(indexedEncoded);
        PngDecoderOptions indexedDecodeOptions = new PngDecoderOptions();
        indexedDecodeOptions.PreferredPixelFormat = PngPixelFormat.Rgb8;
        PngImage indexedDecoded = new PngCodec().DecodeBytes(indexedEncoded, indexedDecodeOptions);
        Assert.That(indexedInfo.Info.ColorType, Is.EqualTo(PngColorType.IndexedColor));
        Assert.That(indexedInfo.Info.BitDepth, Is.EqualTo(PngBitDepth.Two));
        Assert.That(indexedInfo.Metadata.Palette.Entries, Has.Length.EqualTo(3));
        Assert.That(indexedDecoded.Pixels.Data, Is.EqualTo(new byte[] { 0, 0, 0, 53, 97, 193, 106, 194, 130, 53, 97, 193 }));
    }

    [TestCase(PngMetadataPolicy.Preserve, true, true, true)]
    [TestCase(PngMetadataPolicy.Strip, false, false, false)]
    [TestCase(PngMetadataPolicy.KeepSelected, true, false, true)]
    [TestCase(PngMetadataPolicy.RemoveSelected, true, false, true)]
    public void PublicCodec_AppliesEveryMetadataPolicyDuringRoundTrip(PngMetadataPolicy policy, bool hasGamma, bool hasPhysicalPixels, bool hasText)
    {
        PngImage image = new PngImageFactory().CreateImageWithData(1, 1, PngPixelFormat.Rgba8, new byte[] { 1, 2, 3, 4 });
        image.Metadata.Chunks = new PngChunk[]
        {
            CreateChunk(PngConstants.ChunkGamma, new byte[] { 0, 0, 177, 143 }),
            CreateChunk(PngConstants.ChunkPhysicalPixels, new byte[] { 0, 0, 14, 196, 0, 0, 14, 196, 1 }),
            CreateChunk(PngConstants.ChunkText, new byte[] { 75, 0, 86 })
        };
        PngEncoderOptions options = CreateExplicitOptions(PngColorType.TruecolorAlpha, PngBitDepth.Eight, PngInterlaceMethod.None);
        options.MetadataPolicy = policy;
        options.MetadataChunkTypesToKeep = new string[] { PngConstants.ChunkGamma, PngConstants.ChunkText };
        options.MetadataChunkTypesToRemove = new string[] { PngConstants.ChunkPhysicalPixels };

        byte[] encoded = new PngCodec().EncodeToBytes(image, options);
        PngImage decoded = new PngCodec().DecodeBytes(encoded, new PngDecoderOptions());

        Assert.That(HasChunk(decoded.Metadata.Chunks, PngConstants.ChunkGamma), Is.EqualTo(hasGamma));
        Assert.That(HasChunk(decoded.Metadata.Chunks, PngConstants.ChunkPhysicalPixels), Is.EqualTo(hasPhysicalPixels));
        Assert.That(HasChunk(decoded.Metadata.Chunks, PngConstants.ChunkText), Is.EqualTo(hasText));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
    }

    [Test]
    public void PublicCodec_DecodesPinnedGoldenAndProducesEquivalentCanonicalEncoding()
    {
        byte[] golden = Convert.FromHexString("89504E470D0A1A0A0000000D49484452000000010000000108000000003A7E9B550000000A4944415478016360000000020001737501180000000049454E44AE426082");
        IPngValidator validator = new PngValidationService();
        IPngDecoder decoder = new PngCodec();
        IPngEncoder encoder = new PngCodec();
        PngDecoderOptions decodeOptions = new PngDecoderOptions();
        decodeOptions.PreferredPixelFormat = PngPixelFormat.Unknown;

        PngImage decoded = decoder.DecodeBytes(golden, decodeOptions);
        PngEncoderOptions encodeOptions = CreateExplicitOptions(PngColorType.Grayscale, PngBitDepth.Eight, PngInterlaceMethod.None);
        encodeOptions.UseFixedFilter = true;
        encodeOptions.RowFilterStrategy = PngRowFilterStrategy.Fixed;
        encodeOptions.FixedFilterType = PngFilterType.None;
        encodeOptions.CompressionLevel = 0;
        byte[] encoded = encoder.EncodeToBytes(decoded, encodeOptions);
        PngImage decodedAgain = decoder.DecodeBytes(encoded, decodeOptions);
        PngChunkListResult chunks = new PngChunkService().ReadChunks(encoded);
        byte[] filtered = PngZlibDecompressor.Decompress(PngIdatConcatenator.Concatenate(chunks));

        Assert.That(validator.ValidateBytes(golden).IsValid, Is.True);
        Assert.That(validator.ValidateBytes(encoded).IsValid, Is.True);
        Assert.That(decoded.Info.ColorType, Is.EqualTo(PngColorType.Grayscale));
        Assert.That(decoded.Info.BitDepth, Is.EqualTo(PngBitDepth.Eight));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(new byte[] { 0 }));
        Assert.That(decodedAgain.Pixels.Data, Is.EqualTo(new byte[] { 0 }));
        Assert.That(chunks.Chunks.Select(chunk => chunk.Type), Is.EqualTo(new string[] { PngConstants.ChunkIHDR, PngConstants.ChunkIDAT, PngConstants.ChunkIEND }));
        Assert.That(chunks.Chunks[0].Data, Is.EqualTo(golden.AsSpan(16, 13).ToArray()));
        Assert.That(filtered, Is.EqualTo(new byte[] { 0, 0 }));
    }

    static IEnumerable<TestCaseData> CreateCombinationCases()
    {
        PngInterlaceMethod[] interlaceMethods = new PngInterlaceMethod[] { PngInterlaceMethod.None, PngInterlaceMethod.Adam7 };
        object[][] combinations = new object[][]
        {
            new object[] { PngColorType.Grayscale, PngBitDepth.One, PngPixelFormat.Grayscale1 },
            new object[] { PngColorType.Grayscale, PngBitDepth.Two, PngPixelFormat.Grayscale2 },
            new object[] { PngColorType.Grayscale, PngBitDepth.Four, PngPixelFormat.Grayscale4 },
            new object[] { PngColorType.Grayscale, PngBitDepth.Eight, PngPixelFormat.Grayscale8 },
            new object[] { PngColorType.Grayscale, PngBitDepth.Sixteen, PngPixelFormat.Grayscale16 },
            new object[] { PngColorType.Truecolor, PngBitDepth.Eight, PngPixelFormat.Rgb8 },
            new object[] { PngColorType.Truecolor, PngBitDepth.Sixteen, PngPixelFormat.Rgb16 },
            new object[] { PngColorType.IndexedColor, PngBitDepth.One, PngPixelFormat.Indexed1 },
            new object[] { PngColorType.IndexedColor, PngBitDepth.Two, PngPixelFormat.Indexed2 },
            new object[] { PngColorType.IndexedColor, PngBitDepth.Four, PngPixelFormat.Indexed4 },
            new object[] { PngColorType.IndexedColor, PngBitDepth.Eight, PngPixelFormat.Indexed8 },
            new object[] { PngColorType.GrayscaleAlpha, PngBitDepth.Eight, PngPixelFormat.GrayscaleAlpha8 },
            new object[] { PngColorType.GrayscaleAlpha, PngBitDepth.Sixteen, PngPixelFormat.GrayscaleAlpha16 },
            new object[] { PngColorType.TruecolorAlpha, PngBitDepth.Eight, PngPixelFormat.Rgba8 },
            new object[] { PngColorType.TruecolorAlpha, PngBitDepth.Sixteen, PngPixelFormat.Rgba16 }
        };

        int combinationIndex = 0;
        while (combinationIndex < combinations.Length)
        {
            int interlaceIndex = 0;
            while (interlaceIndex < interlaceMethods.Length)
            {
                object[] combination = combinations[combinationIndex];
                yield return new TestCaseData(combination[0], combination[1], combination[2], interlaceMethods[interlaceIndex])
                    .SetName(combination[0] + " " + combination[1] + " " + interlaceMethods[interlaceIndex]);
                interlaceIndex += 1;
            }

            combinationIndex += 1;
        }
    }

    static IEnumerable<TestCaseData> CreateFilterCases()
    {
        PngInterlaceMethod[] interlaceMethods = new PngInterlaceMethod[] { PngInterlaceMethod.None, PngInterlaceMethod.Adam7 };
        object[][] filters = new object[][]
        {
            new object[] { PngRowFilterStrategy.Fixed, PngFilterType.None },
            new object[] { PngRowFilterStrategy.Fixed, PngFilterType.Sub },
            new object[] { PngRowFilterStrategy.Fixed, PngFilterType.Up },
            new object[] { PngRowFilterStrategy.Fixed, PngFilterType.Average },
            new object[] { PngRowFilterStrategy.Fixed, PngFilterType.Paeth },
            new object[] { PngRowFilterStrategy.Fast, PngFilterType.None },
            new object[] { PngRowFilterStrategy.Adaptive, PngFilterType.None },
            new object[] { PngRowFilterStrategy.Exhaustive, PngFilterType.None }
        };

        int filterIndex = 0;
        while (filterIndex < filters.Length)
        {
            int interlaceIndex = 0;
            while (interlaceIndex < interlaceMethods.Length)
            {
                yield return new TestCaseData(filters[filterIndex][0], filters[filterIndex][1], interlaceMethods[interlaceIndex])
                    .SetName(filters[filterIndex][0] + " " + filters[filterIndex][1] + " " + interlaceMethods[interlaceIndex]);
                interlaceIndex += 1;
            }

            filterIndex += 1;
        }
    }

    static PngEncoderOptions CreateExplicitOptions(PngColorType colorType, PngBitDepth bitDepth, PngInterlaceMethod interlace)
    {
        PngEncoderOptions options = new PngEncoderOptions();
        options.ColorType = colorType;
        options.BitDepth = bitDepth;
        options.InterlaceMethod = interlace;
        options.UseAutomaticColorType = false;
        options.UseAutomaticBitDepth = false;
        options.RowFilterStrategy = PngRowFilterStrategy.Adaptive;
        return options;
    }

    static byte[] CreatePixels(PngPixelFormat format, int width, int height)
    {
        int rowBytes = GetRowBytes(format, width);
        byte[] data = new byte[rowBytes * height];
        int bits = GetPackedBits(format);
        if (bits > 0)
        {
            int maximum = (1 << bits) - 1;
            int y = 0;
            while (y < height)
            {
                byte[] samples = new byte[width];
                int x = 0;
                while (x < width)
                {
                    samples[x] = (byte)((x + y * 3) & maximum);
                    x += 1;
                }

                byte[] row = PngPackedSampleWriter.Pack(samples, 0, samples.Length, GetBitDepth(bits));
                Array.Copy(row, 0, data, y * rowBytes, row.Length);
                y += 1;
            }

            return data;
        }

        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)((index * 37 + 11) & 255);
            index += 1;
        }

        if (format == PngPixelFormat.Indexed8)
        {
            index = 0;
            while (index < data.Length)
            {
                data[index] = (byte)(index & 3);
                index += 1;
            }
        }

        return data;
    }

    static int GetRowBytes(PngPixelFormat format, int width)
    {
        return (int)PngSizeCalculator.CalculateFrameRowBytes(width, format);
    }

    static int GetPackedBits(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Indexed1:
                return 1;
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Indexed2:
                return 2;
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Indexed4:
                return 4;
            default:
                return 0;
        }
    }

    static PngBitDepth GetBitDepth(int bits)
    {
        switch (bits)
        {
            case 1:
                return PngBitDepth.One;
            case 2:
                return PngBitDepth.Two;
            default:
                return PngBitDepth.Four;
        }
    }

    static int GetPaletteSize(PngBitDepth bitDepth)
    {
        if (bitDepth == PngBitDepth.Eight)
        {
            return 4;
        }

        return 1 << (int)bitDepth;
    }

    static PngPalette CreatePalette(int count)
    {
        PngPalette palette = new PngPalette();
        palette.Entries = new PngPaletteEntry[count];
        int index = 0;
        while (index < count)
        {
            PngPaletteEntry entry = new PngPaletteEntry();
            entry.Red = (byte)(index * 53);
            entry.Green = (byte)(index * 97);
            entry.Blue = (byte)(index * 193);
            palette.Entries[index] = entry;
            index += 1;
        }

        return palette;
    }

    static void AssertAutomaticEncoding(PngImage image, PngColorType colorType, PngBitDepth bitDepth, PngPixelFormat requestedFormat, byte[] expectedPixels)
    {
        IPngEncoder encoder = new PngCodec();
        IPngDecoder decoder = new PngCodec();
        byte[] encoded = encoder.EncodeToBytes(image, new PngEncoderOptions());
        PngIdentifyResult identified = new PngIdentifierService().IdentifyBytes(encoded);
        PngDecoderOptions decodeOptions = new PngDecoderOptions();
        decodeOptions.PreferredPixelFormat = requestedFormat;
        PngImage decoded = decoder.DecodeBytes(encoded, decodeOptions);

        Assert.That(identified.Info.ColorType, Is.EqualTo(colorType));
        Assert.That(identified.Info.BitDepth, Is.EqualTo(bitDepth));
        Assert.That(decoded.Pixels.Format, Is.EqualTo(requestedFormat));
        Assert.That(decoded.Pixels.Data, Is.EqualTo(expectedPixels));
    }

    static PngChunk CreateChunk(string type, byte[] data)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = type;
        chunk.Data = data;
        return chunk;
    }

    static bool HasChunk(PngChunk[] chunks, string type)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == type)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    static void AssertEveryNonInterlacedRowUsesFilter(byte[] encoded, PngFilterType filter, int rowBytes, int height)
    {
        PngChunkListResult chunks = new PngChunkService().ReadChunks(encoded);
        byte[] compressed = PngIdatConcatenator.Concatenate(chunks);
        byte[] filtered = PngZlibDecompressor.Decompress(compressed);
        int row = 0;
        while (row < height)
        {
            Assert.That(filtered[row * (rowBytes + 1)], Is.EqualTo((byte)filter));
            row += 1;
        }
    }
}
