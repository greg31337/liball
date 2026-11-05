using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicRasterApiTests
{
    [Test]
    public void ConvertSamplesToDouble_CoversPlannedBitDepthAndSampleFormatMatrix()
    {
        int[] integerBits = new int[] { 1, 2, 4, 8, 16, 32, 64 };
        int index = 0;
        while (index < integerBits.Length)
        {
            int bits = integerBits[index];
            byte[] maximum = CreateIntegerBytes(bits, true);
            AssertDoubleConversion(bits, TiffSampleFormat.UnsignedInteger, maximum, CalculateUnsignedMaximum(bits));
            AssertDoubleConversion(bits, TiffSampleFormat.SignedInteger, maximum, -1d);
            index = index + 1;
        }

        AssertDoubleConversion(8, TiffSampleFormat.Undefined, new byte[] { 173 }, 173d);
        AssertDoubleConversion(32, TiffSampleFormat.IeeeFloatingPoint, BitConverter.GetBytes(1.5f), 1.5d);
        AssertDoubleConversion(64, TiffSampleFormat.IeeeFloatingPoint, BitConverter.GetBytes(1.5d), 1.5d);

        AssertUnsupportedSampleFormat(TiffSampleFormat.Unknown);
        AssertUnsupportedSampleFormat(TiffSampleFormat.ComplexInteger);
        AssertUnsupportedSampleFormat(TiffSampleFormat.ComplexIeeeFloatingPoint);
    }

    [Test]
    public void ConvertSamplesToRgba8And16_CoversPlannedNumericRasterMatrix()
    {
        int[] unsignedBits = new int[] { 1, 2, 4, 8, 16, 32, 64 };
        int index = 0;
        while (index < unsignedBits.Length)
        {
            TiffImageInfo info = CreateInfo(1, 1, TiffPhotometric.BlackIsZero, new int[] { unsignedBits[index] }, new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger });
            AssertGrayConversion(info, CalculateUnsignedMaximum(unsignedBits[index]), 255, 65535);
            index = index + 1;
        }

        AssertGrayConversion(CreateInfo(1, 1, TiffPhotometric.BlackIsZero, new int[] { 8 }, new TiffSampleFormat[] { TiffSampleFormat.SignedInteger }), 0d, 128, 32896);
        AssertGrayConversion(CreateInfo(1, 1, TiffPhotometric.BlackIsZero, new int[] { 8 }, new TiffSampleFormat[] { TiffSampleFormat.Undefined }), 128d, 128, 32896);
        AssertGrayConversion(CreateInfo(1, 1, TiffPhotometric.BlackIsZero, new int[] { 32 }, new TiffSampleFormat[] { TiffSampleFormat.IeeeFloatingPoint }), 0.5d, 128, 32768);
        AssertGrayConversion(CreateInfo(1, 1, TiffPhotometric.BlackIsZero, new int[] { 64 }, new TiffSampleFormat[] { TiffSampleFormat.IeeeFloatingPoint }), 0.5d, 128, 32768);

        TiffImageInfo mixed = CreateInfo(1, 1, TiffPhotometric.Rgb, new int[] { 8, 16, 32 }, new TiffSampleFormat[]
        {
            TiffSampleFormat.UnsignedInteger,
            TiffSampleFormat.UnsignedInteger,
            TiffSampleFormat.UnsignedInteger
        });
        TiffRgba8ReadResult mixed8 = TiffApi.Codec.ConvertSamplesToRgba8(mixed, new double[] { 255d, 32768d, uint.MaxValue }, new TiffRgbaReadOptions());
        TiffRgba16ReadResult mixed16 = TiffApi.Codec.ConvertSamplesToRgba16(mixed, new double[] { 255d, 32768d, uint.MaxValue }, new TiffRgbaReadOptions());

        Assert.That(mixed8.Success, Is.True, mixed8.Diagnostics.ToString());
        Assert.That(mixed8.Bytes, Is.EqualTo(new byte[] { 255, 128, 255, 255 }));
        Assert.That(mixed16.Success, Is.True, mixed16.Diagnostics.ToString());
        Assert.That(mixed16.Samples, Is.EqualTo(new ushort[] { 65535, 32768, 65535, 65535 }));
    }

    [Test]
    public void ConvertSamplesToRgba8And16_AppliesDeclaredIntegerAndFloatingRanges()
    {
        TiffRgbaReadOptions options = new TiffRgbaReadOptions();
        options.ApplyMinMaxScaling = true;
        TiffImageInfo integerInfo = CreateInfo(3, 1, TiffPhotometric.BlackIsZero, new int[] { 8 }, new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger });
        integerInfo.MinSampleValues = new double[] { 10d };
        integerInfo.MaxSampleValues = new double[] { 110d };
        TiffImageInfo floatingInfo = CreateInfo(3, 1, TiffPhotometric.BlackIsZero, new int[] { 32 }, new TiffSampleFormat[] { TiffSampleFormat.IeeeFloatingPoint });
        floatingInfo.SMinSampleValues = new double[] { -2d };
        floatingInfo.SMaxSampleValues = new double[] { 2d };

        TiffRgba8ReadResult integer8 = TiffApi.Codec.ConvertSamplesToRgba8(integerInfo, new double[] { 10d, 60d, 110d }, options);
        TiffRgba16ReadResult integer16 = TiffApi.Codec.ConvertSamplesToRgba16(integerInfo, new double[] { 10d, 60d, 110d }, options);
        TiffRgba8ReadResult floating8 = TiffApi.Codec.ConvertSamplesToRgba8(floatingInfo, new double[] { -2d, 0d, 2d }, options);
        TiffRgba16ReadResult floating16 = TiffApi.Codec.ConvertSamplesToRgba16(floatingInfo, new double[] { -2d, 0d, 2d }, options);

        Assert.That(integer8.Bytes, Is.EqualTo(CreateGray8Ramp()));
        Assert.That(integer16.Samples, Is.EqualTo(CreateIntegerGray16Ramp()));
        Assert.That(floating8.Bytes, Is.EqualTo(CreateGray8Ramp()));
        Assert.That(floating16.Samples, Is.EqualTo(CreateGray16Ramp()));
    }

    [Test]
    public void ConvertSamplesToRgba8And16_CoversEveryPlannedPhotometricFamily()
    {
        TiffImageInfo palette = CreateInfo(1, 1, TiffPhotometric.Palette, new int[] { 1 }, new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger });
        palette.ColorMap = new ushort[] { 0, 65535, 0, 32768, 0, 0 };

        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.WhiteIsZero, new int[] { 1 }, CreateUnsignedFormats(1)), new double[] { 0d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.BlackIsZero, new int[] { 8 }, CreateUnsignedFormats(1)), new double[] { 128d });
        AssertPhotometricConversion(palette, new double[] { 1d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.Rgb, new int[] { 8, 8, 8 }, CreateUnsignedFormats(3)), new double[] { 255d, 128d, 0d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.Cmyk, new int[] { 8, 8, 8, 8 }, CreateUnsignedFormats(4)), new double[] { 0d, 255d, 255d, 0d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.YCbCr, new int[] { 8, 8, 8 }, CreateUnsignedFormats(3)), new double[] { 128d, 128d, 128d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.CieLab, new int[] { 8, 8, 8 }, CreateUnsignedFormats(3)), new double[] { 255d, 128d, 128d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.IccLab, new int[] { 8, 8, 8 }, CreateUnsignedFormats(3)), new double[] { 255d, 128d, 128d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.ItuLab, new int[] { 8, 8, 8 }, CreateUnsignedFormats(3)), new double[] { 255d, 128d, 96d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.LogL, new int[] { 16 }, CreateUnsignedFormats(1)), new double[] { 15872d });
        AssertPhotometricConversion(CreateInfo(1, 1, TiffPhotometric.LogLuv, new int[] { 32 }, CreateUnsignedFormats(1)), new double[] { 0d });

        TiffImageInfo cfa = CreateInfo(2, 2, TiffPhotometric.ColorFilterArray, new int[] { 8 }, CreateUnsignedFormats(1));
        cfa.CFARepeatPatternDimensions = new int[] { 2, 2 };
        cfa.CFAPattern = new byte[] { 0, 1, 1, 2 };
        AssertPhotometricConversion(cfa, new double[] { 255d, 128d, 128d, 0d });

        AssertUnsupportedPhotometric(TiffPhotometric.TransparencyMask);
        AssertUnsupportedPhotometric(TiffPhotometric.LinearRaw);
    }

    [TestCase(TiffOrientation.TopLeft, new byte[] { 1, 2, 3, 4, 5, 6 }, 3, 2)]
    [TestCase(TiffOrientation.TopRight, new byte[] { 3, 2, 1, 6, 5, 4 }, 3, 2)]
    [TestCase(TiffOrientation.BottomRight, new byte[] { 6, 5, 4, 3, 2, 1 }, 3, 2)]
    [TestCase(TiffOrientation.BottomLeft, new byte[] { 4, 5, 6, 1, 2, 3 }, 3, 2)]
    [TestCase(TiffOrientation.LeftTop, new byte[] { 1, 4, 2, 5, 3, 6 }, 2, 3)]
    [TestCase(TiffOrientation.RightTop, new byte[] { 4, 1, 5, 2, 6, 3 }, 2, 3)]
    [TestCase(TiffOrientation.RightBottom, new byte[] { 6, 3, 5, 2, 4, 1 }, 2, 3)]
    [TestCase(TiffOrientation.LeftBottom, new byte[] { 3, 6, 2, 5, 1, 4 }, 2, 3)]
    public void ApplyOrientation_CoversEveryOrientationThroughPublicService(TiffOrientation orientation, byte[] expected, int expectedWidth, int expectedLength)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 3;
        info.Length = 2;
        info.Orientation = orientation;
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffOrientationTransformResult result = TiffApi.Codec.ApplyOrientation(info, new byte[] { 1, 2, 3, 4, 5, 6 }, 1, diagnostics);

        Assert.That(result.Success, Is.True, diagnostics.ToString());
        Assert.That(result.Width, Is.EqualTo(expectedWidth));
        Assert.That(result.Length, Is.EqualTo(expectedLength));
        Assert.That(result.Bytes, Is.EqualTo(expected));
    }

    [Test]
    public void ReadRgba8And16_ExpandPackedTwoBitPaletteThroughPublicImageApi()
    {
        TiffImageDefinition definition = TiffApi.Data.CreateImageDefinition(CreatePaletteTags());
        TiffApi.Data.SetRawStrip(definition, 0, new byte[] { 27 });
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        writer.AddImage(definition);
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);

        TiffRgba8ReadResult rgba8 = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba16ReadResult rgba16 = image.ReadRgba16(new TiffRgbaReadOptions());

        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(rgba8.Success, Is.True, rgba8.Diagnostics.ToString());
        Assert.That(rgba8.Bytes, Is.EqualTo(new byte[]
        {
            0, 0, 0, 255,
            255, 0, 0, 255,
            0, 255, 0, 255,
            255, 255, 255, 255
        }));
        Assert.That(rgba16.Success, Is.True, rgba16.Diagnostics.ToString());
        Assert.That(rgba16.Samples, Is.EqualTo(new ushort[]
        {
            0, 0, 0, 65535,
            65535, 0, 0, 65535,
            0, 65535, 0, 65535,
            65535, 65535, 65535, 65535
        }));
    }

    [Test]
    public void ReadRgba8And16_CombineSeparatePlanesBigEndianPredictorAndAlpha()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 2;
        configuration.Length = 1;
        configuration.PixelFormat.SamplesPerPixel = 4;
        configuration.PixelFormat.BitsPerSample = new int[] { 16 };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.UnsignedInteger };
        configuration.PixelFormat.Photometric = TiffPhotometric.Rgb;
        configuration.PixelFormat.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.UnassociatedAlpha };
        configuration.Layout.PlanarConfiguration = TiffPlanarConfiguration.Separate;
        configuration.Compression = TiffCompression.Deflate;
        configuration.Predictor = 2;
        configuration.ByteOrder = TiffByteOrder.BigEndian;
        ushort[] sourceSamples = new ushort[]
        {
            65535, 32768, 16384, 40000,
            1000, 2000, 3000, 4000
        };
        byte[] source = new byte[sourceSamples.Length * 2];
        Buffer.BlockCopy(sourceSamples, 0, source, 0, source.Length);
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, source);
        TiffDiagnostics flush = writer.Flush();
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        ITiffImage image = file.GetImage(0);
        TiffRgba8ReadResult rgba8 = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba16ReadResult rgba16 = image.ReadRgba16(new TiffRgbaReadOptions());

        Assert.That(write.HasErrors(), Is.False, write.ToString());
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(file.GetHeader().ByteOrder, Is.EqualTo(TiffByteOrder.BigEndian));
        Assert.That(image.GetInfo().PlanarConfiguration, Is.EqualTo(TiffPlanarConfiguration.Separate));
        Assert.That(image.GetInfo().Predictor, Is.EqualTo(2));
        Assert.That(rgba8.Success, Is.True, rgba8.Diagnostics.ToString());
        Assert.That(rgba8.Bytes, Is.EqualTo(new byte[] { 255, 128, 64, 156, 4, 8, 12, 16 }));
        Assert.That(rgba16.Success, Is.True, rgba16.Diagnostics.ToString());
        Assert.That(rgba16.Samples, Is.EqualTo(sourceSamples));
    }

    [Test]
    public void ReadRgba8And16_ApplyFloatingPointPredictorThroughPublicImageApi()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 2;
        configuration.Length = 1;
        configuration.PixelFormat.BitsPerSample = new int[] { 32 };
        configuration.PixelFormat.SampleFormats = new TiffSampleFormat[] { TiffSampleFormat.IeeeFloatingPoint };
        configuration.Compression = TiffCompression.Deflate;
        configuration.Predictor = 3;
        configuration.ByteOrder = TiffByteOrder.BigEndian;
        float[] sourceSamples = new float[] { 0.25f, 0.75f };
        byte[] source = new byte[sourceSamples.Length * 4];
        Buffer.BlockCopy(sourceSamples, 0, source, 0, source.Length);
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);

        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, source);
        TiffDiagnostics flush = writer.Flush();
        ITiffImage image = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
        TiffRgba8ReadResult rgba8 = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba16ReadResult rgba16 = image.ReadRgba16(new TiffRgbaReadOptions());

        Assert.That(write.HasErrors(), Is.False, write.ToString());
        Assert.That(flush.HasErrors(), Is.False, flush.ToString());
        Assert.That(image.GetInfo().Predictor, Is.EqualTo(3));
        Assert.That(rgba8.Success, Is.True, rgba8.Diagnostics.ToString());
        Assert.That(rgba8.Bytes, Is.EqualTo(new byte[] { 64, 64, 64, 255, 191, 191, 191, 255 }));
        Assert.That(rgba16.Success, Is.True, rgba16.Diagnostics.ToString());
        Assert.That(rgba16.Samples, Is.EqualTo(new ushort[] { 16384, 16384, 16384, 65535, 49151, 49151, 49151, 65535 }));
    }

    [Test]
    public void PublicRasterMethods_ReportShortBuffersAndArithmeticOverflowWithoutWriting()
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = 2;
        configuration.Length = 1;
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        Assert.That(writer.WriteSourceRaster(imageIndex, new byte[] { 10, 20 }).HasErrors(), Is.False);
        Assert.That(writer.Flush().HasErrors(), Is.False);
        ITiffImage image = TiffFile.OpenRead(writer.GetFileBytes()).GetImage(0);
        byte[] rasterDestination = new byte[] { 91 };
        byte[] regionDestination = new byte[] { 92 };
        TiffRegionReadOptions regionOptions = new TiffRegionReadOptions();
        regionOptions.Region.Width = 2;
        regionOptions.Region.Length = 1;

        TiffDiagnostics raster = image.ReadSourceRasterToBuffer(rasterDestination);
        TiffDiagnostics region = image.ReadRegionToBuffer(regionOptions, regionDestination);
        TiffDiagnostics rgba8 = writer.WriteRgba8(imageIndex, new byte[7]);
        TiffDiagnostics rgba16 = writer.WriteRgba16(imageIndex, new ushort[7]);
        TiffImageInfo overflowInfo = new TiffImageInfo();
        overflowInfo.Width = ulong.MaxValue;
        overflowInfo.Length = 2;
        overflowInfo.Orientation = TiffOrientation.TopLeft;
        TiffDiagnostics overflowDiagnostics = new TiffDiagnostics();
        TiffOrientationTransformResult overflow = TiffApi.Codec.ApplyOrientation(overflowInfo, new byte[0], 2, overflowDiagnostics);

        Assert.That(raster.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(region.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(rgba8.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(rgba16.ContainsCode(TiffErrorCode.InvalidByteRange), Is.True);
        Assert.That(rasterDestination, Is.EqualTo(new byte[] { 91 }));
        Assert.That(regionDestination, Is.EqualTo(new byte[] { 92 }));
        Assert.That(overflow.Success, Is.False);
        Assert.That(overflowDiagnostics.ContainsCode(TiffErrorCode.IntegerOverflow), Is.True);
    }

    static void AssertDoubleConversion(int bits, TiffSampleFormat format, byte[] source, double expected)
    {
        TiffCodecContext context = CreateSampleContext(bits, format, source.Length);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] samples = TiffApi.Codec.ConvertSamplesToDouble(context, source, diagnostics);

        Assert.That(diagnostics.HasErrors(), Is.False, diagnostics.ToString());
        Assert.That(samples, Is.EqualTo(new double[] { expected }));
    }

    static void AssertUnsupportedSampleFormat(TiffSampleFormat format)
    {
        TiffCodecContext context = CreateSampleContext(8, format, 1);
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        double[] samples = TiffApi.Codec.ConvertSamplesToDouble(context, new byte[] { 0 }, diagnostics);

        Assert.That(samples, Is.Empty);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidTagValue), Is.True);
    }

    static void AssertPhotometricConversion(TiffImageInfo info, double[] samples)
    {
        TiffRgba8ReadResult rgba8 = TiffApi.Codec.ConvertSamplesToRgba8(info, samples, new TiffRgbaReadOptions());
        TiffRgba16ReadResult rgba16 = TiffApi.Codec.ConvertSamplesToRgba16(info, samples, new TiffRgbaReadOptions());
        int pixelCount = (int)(info.Width * info.Length);

        Assert.That(rgba8.Success, Is.True, info.Photometric + ": " + rgba8.Diagnostics);
        Assert.That(rgba8.Bytes.Length, Is.EqualTo(pixelCount * 4));
        Assert.That(rgba16.Success, Is.True, info.Photometric + ": " + rgba16.Diagnostics);
        Assert.That(rgba16.Samples.Length, Is.EqualTo(pixelCount * 4));
    }

    static void AssertUnsupportedPhotometric(TiffPhotometric photometric)
    {
        TiffImageInfo info = CreateInfo(1, 1, photometric, new int[] { 8 }, CreateUnsignedFormats(1));

        TiffRgba8ReadResult rgba8 = TiffApi.Codec.ConvertSamplesToRgba8(info, new double[] { 0d }, new TiffRgbaReadOptions());
        TiffRgba16ReadResult rgba16 = TiffApi.Codec.ConvertSamplesToRgba16(info, new double[] { 0d }, new TiffRgbaReadOptions());

        Assert.That(rgba8.Success, Is.False);
        Assert.That(rgba8.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
        Assert.That(rgba16.Success, Is.False);
        Assert.That(rgba16.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    static TiffCodecContext CreateSampleContext(int bits, TiffSampleFormat format, int byteCount)
    {
        TiffCodecContext context = new TiffCodecContext();
        context.Info.Width = 1;
        context.Info.Length = 1;
        context.Info.RowsPerStrip = 1;
        context.Info.SamplesPerPixel = 1;
        context.Info.BitsPerSample = new int[] { bits };
        context.Info.SampleFormats = new TiffSampleFormat[] { format };
        context.ExpectedDecodedByteCount = (ulong)byteCount;
        return context;
    }

    static void AssertGrayConversion(TiffImageInfo info, double sample, byte expected8, ushort expected16)
    {
        TiffRgba8ReadResult rgba8 = TiffApi.Codec.ConvertSamplesToRgba8(info, new double[] { sample }, new TiffRgbaReadOptions());
        TiffRgba16ReadResult rgba16 = TiffApi.Codec.ConvertSamplesToRgba16(info, new double[] { sample }, new TiffRgbaReadOptions());

        Assert.That(rgba8.Success, Is.True, rgba8.Diagnostics.ToString());
        Assert.That(rgba8.Bytes, Is.EqualTo(new byte[] { expected8, expected8, expected8, 255 }));
        Assert.That(rgba16.Success, Is.True, rgba16.Diagnostics.ToString());
        Assert.That(rgba16.Samples, Is.EqualTo(new ushort[] { expected16, expected16, expected16, 65535 }));
    }

    static TiffImageInfo CreateInfo(ulong width, ulong length, TiffPhotometric photometric, int[] bits, TiffSampleFormat[] formats)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = width;
        info.Length = length;
        info.SamplesPerPixel = bits.Length;
        info.BitsPerSample = bits;
        info.SampleFormats = formats;
        info.Photometric = photometric;
        return info;
    }

    static TiffSampleFormat[] CreateUnsignedFormats(int count)
    {
        TiffSampleFormat[] formats = new TiffSampleFormat[count];
        int index = 0;
        while (index < formats.Length)
        {
            formats[index] = TiffSampleFormat.UnsignedInteger;
            index = index + 1;
        }

        return formats;
    }

    static byte[] CreateIntegerBytes(int bits, bool allBitsSet)
    {
        int byteCount = bits < 8 ? 1 : bits / 8;
        byte[] bytes = new byte[byteCount];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = allBitsSet ? (byte)255 : (byte)0;
            index = index + 1;
        }

        if (bits < 8 && allBitsSet)
        {
            bytes[0] = (byte)((1 << bits) - 1);
        }

        return bytes;
    }

    static double CalculateUnsignedMaximum(int bits)
    {
        if (bits == 64)
        {
            return (double)ulong.MaxValue;
        }

        return (double)((1UL << bits) - 1UL);
    }

    static byte[] CreateGray8Ramp()
    {
        return new byte[]
        {
            0, 0, 0, 255,
            128, 128, 128, 255,
            255, 255, 255, 255
        };
    }

    static ushort[] CreateGray16Ramp()
    {
        return new ushort[]
        {
            0, 0, 0, 65535,
            32768, 32768, 32768, 65535,
            65535, 65535, 65535, 65535
        };
    }

    static ushort[] CreateIntegerGray16Ramp()
    {
        return new ushort[]
        {
            0, 0, 0, 65535,
            32896, 32896, 32896, 65535,
            65535, 65535, 65535, 65535
        };
    }

    static TiffTagValue[] CreatePaletteTags()
    {
        List<TiffTagValue> tags = new List<TiffTagValue>();
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, 4));
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, 1));
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 2));
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.None));
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.Palette));
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.SamplesPerPixel, TiffDataType.Short, 1));
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 1));
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PlanarConfiguration, TiffDataType.Short, (ulong)TiffPlanarConfiguration.Contiguous));
        tags.Add(TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.SampleFormat, TiffDataType.Short, (ulong)TiffSampleFormat.UnsignedInteger));
        tags.Add(TiffApi.Data.CreateUnsignedArrayTag(TiffTagId.ColorMap, TiffDataType.Short, new ulong[]
        {
            0, 65535, 0, 65535,
            0, 0, 65535, 65535,
            0, 0, 0, 65535
        }));
        return tags.ToArray();
    }
}
