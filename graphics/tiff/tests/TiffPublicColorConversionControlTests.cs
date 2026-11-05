using System;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicColorConversionControlTests
{
    [Test]
    public void ConvertSamplesToRgba8_ExposesSupportedColorFamilies()
    {
        ITiffCodecService service = new TiffCodecService();
        TiffImageInfo bilevel = CreateInfo(TiffPhotometric.BlackIsZero, 1, 1, TiffSampleFormat.UnsignedInteger);
        TiffImageInfo subByte = CreateInfo(TiffPhotometric.BlackIsZero, 1, 4, TiffSampleFormat.UnsignedInteger);
        TiffImageInfo floatingGray = CreateInfo(TiffPhotometric.BlackIsZero, 1, 32, TiffSampleFormat.IeeeFloatingPoint);
        TiffImageInfo palette = CreateInfo(TiffPhotometric.Palette, 1, 1, TiffSampleFormat.UnsignedInteger);
        palette.ColorMap = new ushort[] { 0, 65535, 0, 32768, 0, 0 };

        AssertConversionSucceeds(service, bilevel, new double[] { 1 });
        AssertConversionSucceeds(service, subByte, new double[] { 8 });
        AssertConversionSucceeds(service, floatingGray, new double[] { 0.5d });
        AssertConversionSucceeds(service, palette, new double[] { 1 });
        AssertConversionSucceeds(service, CreateInfo(TiffPhotometric.Rgb, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 255, 128, 0 });
        AssertConversionSucceeds(service, CreateInfo(TiffPhotometric.Cmyk, 4, 8, TiffSampleFormat.UnsignedInteger), new double[] { 0, 255, 255, 0 });
        AssertConversionSucceeds(service, CreateInfo(TiffPhotometric.YCbCr, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 128, 128, 128 });
        AssertConversionSucceeds(service, CreateInfo(TiffPhotometric.CieLab, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 255, 128, 128 });
        AssertConversionSucceeds(service, CreateInfo(TiffPhotometric.IccLab, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 255, 128, 128 });
        AssertConversionSucceeds(service, CreateInfo(TiffPhotometric.ItuLab, 3, 8, TiffSampleFormat.UnsignedInteger), new double[] { 255, 128, 96 });
        AssertConversionSucceeds(service, CreateInfo(TiffPhotometric.LogL, 1, 16, TiffSampleFormat.UnsignedInteger), new double[] { 15872 });
        AssertConversionSucceeds(service, CreateInfo(TiffPhotometric.LogLuv, 1, 32, TiffSampleFormat.UnsignedInteger), new double[] { 0 });
    }

    [Test]
    public void ConvertSamplesToRgba8And16_ControlsCfaConversion()
    {
        ITiffCodecService service = new TiffCodecService();
        TiffImageInfo info = CreateInfo(TiffPhotometric.ColorFilterArray, 1, 8, TiffSampleFormat.UnsignedInteger);
        info.Width = 2;
        info.Length = 2;
        info.CFARepeatPatternDimensions = new int[] { 2, 2 };
        info.CFAPattern = new byte[] { 0, 1, 1, 2 };
        double[] samples = new double[] { 255, 128, 128, 0 };

        TiffRgba8ReadResult bilinear = service.ConvertSamplesToRgba8(info, samples, new TiffRgbaReadOptions());
        TiffRgba16ReadResult bilinear16 = service.ConvertSamplesToRgba16(info, samples, new TiffRgbaReadOptions());
        TiffRgbaReadOptions grayscaleOptions = new TiffRgbaReadOptions();
        grayscaleOptions.CfaConversionMode = TiffCfaConversionMode.Grayscale;
        TiffRgba8ReadResult grayscale = service.ConvertSamplesToRgba8(info, samples, grayscaleOptions);
        TiffRgbaReadOptions rejectOptions = new TiffRgbaReadOptions();
        rejectOptions.CfaConversionMode = TiffCfaConversionMode.Reject;
        TiffRgba8ReadResult rejected = service.ConvertSamplesToRgba8(info, samples, rejectOptions);
        TiffRgbaReadOptions ignoreOptions = new TiffRgbaReadOptions();
        ignoreOptions.CfaConversionMode = TiffCfaConversionMode.Reject;
        ignoreOptions.UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.Ignore;
        TiffRgba8ReadResult ignored = service.ConvertSamplesToRgba8(info, samples, ignoreOptions);
        info.CFAPattern = new byte[] { 0 };
        TiffRgba8ReadResult invalidPattern = service.ConvertSamplesToRgba8(info, samples, new TiffRgbaReadOptions());

        Assert.That(bilinear.Success, Is.True);
        Assert.That(bilinear.Bytes, Is.EqualTo(new byte[]
        {
            255, 128, 0, 255,
            255, 128, 0, 255,
            255, 128, 0, 255,
            255, 128, 0, 255
        }));
        Assert.That(bilinear16.Success, Is.True);
        Assert.That(bilinear16.Samples, Is.EqualTo(new ushort[]
        {
            65535, 32896, 0, 65535,
            65535, 32896, 0, 65535,
            65535, 32896, 0, 65535,
            65535, 32896, 0, 65535
        }));
        Assert.That(grayscale.Success, Is.True);
        Assert.That(grayscale.Bytes, Is.EqualTo(new byte[]
        {
            255, 255, 255, 255,
            128, 128, 128, 255,
            128, 128, 128, 255,
            0, 0, 0, 255
        }));
        Assert.That(rejected.Success, Is.False);
        Assert.That(rejected.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
        Assert.That(ignored.Success, Is.True);
        Assert.That(ignored.Bytes, Is.EqualTo(grayscale.Bytes));
        Assert.That(ignored.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
        Assert.That(invalidPattern.Success, Is.False);
        Assert.That(invalidPattern.Diagnostics.ContainsCode(TiffErrorCode.InvalidTagCount), Is.True);
    }

    [Test]
    public void ConvertSamplesToRgba8_ControlsUnspecifiedAlphaAndPremultiplication()
    {
        ITiffCodecService service = new TiffCodecService();
        TiffImageInfo info = CreateInfo(TiffPhotometric.Rgb, 4, 8, TiffSampleFormat.UnsignedInteger);
        info.ExtraSamples = new TiffExtraSample[] { TiffExtraSample.Unspecified };
        double[] samples = new double[] { 10, 20, 30, 64 };
        TiffRgbaReadOptions ignoreOptions = new TiffRgbaReadOptions();
        ignoreOptions.UseUnspecifiedExtraSampleAsAlpha = false;
        TiffRgbaReadOptions associatedOptions = new TiffRgbaReadOptions();
        associatedOptions.AlphaAssociation = TiffExtraSample.AssociatedAlpha;

        TiffRgba8ReadResult included = service.ConvertSamplesToRgba8(info, samples, new TiffRgbaReadOptions());
        TiffRgba8ReadResult ignored = service.ConvertSamplesToRgba8(info, samples, ignoreOptions);
        TiffRgba8ReadResult associated = service.ConvertSamplesToRgba8(info, samples, associatedOptions);

        Assert.That(included.Success, Is.True);
        Assert.That(included.Bytes, Is.EqualTo(new byte[] { 10, 20, 30, 64 }));
        Assert.That(ignored.Success, Is.True);
        Assert.That(ignored.Bytes, Is.EqualTo(new byte[] { 10, 20, 30, 255 }));
        Assert.That(associated.Success, Is.True);
        Assert.That(associated.Bytes, Is.EqualTo(new byte[] { 3, 5, 8, 64 }));
    }

    [Test]
    public void ReadRgba8_ControlsPredictorReversal()
    {
        TiffWriterImageConfiguration configuration = CreateGrayWriterConfiguration(2, 1, 8);
        configuration.Predictor = 2;
        ITiffImage image = WriteSourceImage(configuration, new byte[] { 10, 20 });
        TiffRgbaReadOptions encodedOptions = new TiffRgbaReadOptions();
        encodedOptions.ApplyPredictor = false;

        TiffRgba8ReadResult decoded = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba8ReadResult encoded = image.ReadRgba8(encodedOptions);

        Assert.That(decoded.Success, Is.True);
        Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 10, 10, 10, 255, 20, 20, 20, 255 }));
        Assert.That(encoded.Success, Is.True);
        Assert.That(encoded.Bytes, Is.EqualTo(new byte[] { 10, 10, 10, 255, 10, 10, 10, 255 }));
    }

    [Test]
    public void ReadRgba16_ControlsSampleByteOrderConversion()
    {
        TiffWriterImageConfiguration configuration = CreateGrayWriterConfiguration(1, 1, 16);
        configuration.ByteOrder = TiffByteOrder.LittleEndian;
        if (BitConverter.IsLittleEndian)
        {
            configuration.ByteOrder = TiffByteOrder.BigEndian;
        }

        byte[] source = new byte[] { 1, 2 };
        if (BitConverter.IsLittleEndian)
        {
            source = new byte[] { 2, 1 };
        }

        ITiffImage image = WriteSourceImage(configuration, source);
        TiffRgbaReadOptions rawOrderOptions = new TiffRgbaReadOptions();
        rawOrderOptions.ApplyByteOrderConversion = false;

        TiffRgba16ReadResult converted = image.ReadRgba16(new TiffRgbaReadOptions());
        TiffRgba16ReadResult rawOrder = image.ReadRgba16(rawOrderOptions);

        Assert.That(converted.Success, Is.True);
        Assert.That(converted.Samples, Is.EqualTo(new ushort[] { 258, 258, 258, 65535 }));
        Assert.That(rawOrder.Success, Is.True);
        Assert.That(rawOrder.Samples, Is.EqualTo(new ushort[] { 513, 513, 513, 65535 }));
    }

    static void AssertConversionSucceeds(ITiffCodecService service, TiffImageInfo info, double[] samples)
    {
        TiffRgba8ReadResult result = service.ConvertSamplesToRgba8(info, samples, new TiffRgbaReadOptions());
        Assert.That(result.Success, Is.True, result.Diagnostics.ToString());
        Assert.That(result.Bytes.Length, Is.EqualTo(4));
    }

    static TiffImageInfo CreateInfo(TiffPhotometric photometric, int samplesPerPixel, int bitsPerSample, TiffSampleFormat sampleFormat)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.Width = 1;
        info.Length = 1;
        info.SamplesPerPixel = samplesPerPixel;
        info.BitsPerSample = new int[samplesPerPixel];
        info.SampleFormats = new TiffSampleFormat[samplesPerPixel];
        info.Photometric = photometric;
        int sampleIndex = 0;
        while (sampleIndex < samplesPerPixel)
        {
            info.BitsPerSample[sampleIndex] = bitsPerSample;
            info.SampleFormats[sampleIndex] = sampleFormat;
            sampleIndex = sampleIndex + 1;
        }

        return info;
    }

    static TiffWriterImageConfiguration CreateGrayWriterConfiguration(int width, int length, int bitsPerSample)
    {
        TiffWriterImageConfiguration configuration = new TiffWriterImageConfiguration();
        configuration.Width = width;
        configuration.Length = length;
        configuration.PixelFormat.BitsPerSample = new int[] { bitsPerSample };
        configuration.Layout.Kind = TiffWriterLayoutKind.Strips;
        configuration.Layout.RowsPerStrip = length;
        return configuration;
    }

    static ITiffImage WriteSourceImage(TiffWriterImageConfiguration configuration, byte[] source)
    {
        TiffWriter writer = new TiffFactory().CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddConfiguredImage(configuration);
        TiffDiagnostics write = writer.WriteSourceRaster(imageIndex, source);
        TiffDiagnostics flush = writer.Flush();
        Assert.That(write.Count(), Is.EqualTo(0));
        Assert.That(flush.Count(), Is.EqualTo(0));
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        Assert.That(file.Success, Is.True);
        return file.GetImage(0);
    }
}
