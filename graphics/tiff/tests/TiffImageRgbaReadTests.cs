using System.Collections.Generic;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffImageRgbaReadTests
{
    [Test]
    public void ReadRgba8And16_ConvertRgbSourceThroughImageApi()
    {
        ITiffImage image = CreateImage(2, 1, new ulong[] { 8, 8, 8 }, TiffPhotometric.Rgb, new byte[] { 1, 2, 3, 4, 5, 6 }, new TiffExtraSample[0], TiffOrientation.TopLeft, 1, new ulong[0], new ulong[0]);

        TiffRgba8ReadResult rgba8 = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba16ReadResult rgba16 = image.ReadRgba16(new TiffRgbaReadOptions());

        Assert.That(rgba8.Success, Is.True);
        Assert.That(rgba8.Width, Is.EqualTo(2));
        Assert.That(rgba8.Length, Is.EqualTo(1));
        Assert.That(rgba8.RowByteCount, Is.EqualTo(8));
        Assert.That(rgba8.Bytes, Is.EqualTo(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }));
        Assert.That(rgba16.Success, Is.True);
        Assert.That(rgba16.RowByteCount, Is.EqualTo(16));
        Assert.That(rgba16.Samples, Is.EqualTo(new ushort[] { 257, 514, 771, 65535, 1028, 1285, 1542, 65535 }));
    }

    [Test]
    public void ReadRgba8_ControlsOrientation()
    {
        ITiffImage image = CreateImage(2, 1, new ulong[] { 8 }, TiffPhotometric.BlackIsZero, new byte[] { 10, 20 }, new TiffExtraSample[0], TiffOrientation.TopRight, 1, new ulong[0], new ulong[0]);
        TiffRgbaReadOptions sourceOrderOptions = new TiffRgbaReadOptions();
        sourceOrderOptions.ApplyOrientation = false;

        TiffRgba8ReadResult oriented = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba8ReadResult sourceOrder = image.ReadRgba8(sourceOrderOptions);

        Assert.That(oriented.Success, Is.True);
        Assert.That(oriented.Bytes, Is.EqualTo(new byte[] { 20, 20, 20, 255, 10, 10, 10, 255 }));
        Assert.That(sourceOrder.Success, Is.True);
        Assert.That(sourceOrder.Bytes, Is.EqualTo(new byte[] { 10, 10, 10, 255, 20, 20, 20, 255 }));
    }

    [Test]
    public void ReadRgba8_ControlsOutputAlphaAssociation()
    {
        ITiffImage image = CreateImage(1, 1, new ulong[] { 8, 8, 8, 8 }, TiffPhotometric.Rgb, new byte[] { 64, 32, 16, 128 }, new TiffExtraSample[] { TiffExtraSample.AssociatedAlpha }, TiffOrientation.TopLeft, 1, new ulong[0], new ulong[0]);
        TiffRgbaReadOptions associatedOptions = new TiffRgbaReadOptions();
        associatedOptions.AlphaAssociation = TiffExtraSample.AssociatedAlpha;

        TiffRgba8ReadResult unassociated = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba8ReadResult associated = image.ReadRgba8(associatedOptions);
        TiffRgba16ReadResult associated16 = image.ReadRgba16(associatedOptions);

        Assert.That(unassociated.Success, Is.True);
        Assert.That(unassociated.Bytes, Is.EqualTo(new byte[] { 128, 64, 32, 128 }));
        Assert.That(associated.Success, Is.True);
        Assert.That(associated.Bytes, Is.EqualTo(new byte[] { 64, 32, 16, 128 }));
        Assert.That(associated16.Success, Is.True);
        Assert.That(associated16.Samples, Is.EqualTo(new ushort[] { 16448, 8224, 4112, 32896 }));
    }

    [Test]
    public void ReadRgba8_ControlsDeclaredMinMaxScaling()
    {
        ITiffImage image = CreateImage(3, 1, new ulong[] { 8 }, TiffPhotometric.BlackIsZero, new byte[] { 10, 60, 110 }, new TiffExtraSample[0], TiffOrientation.TopLeft, 1, new ulong[] { 10 }, new ulong[] { 110 });
        TiffRgbaReadOptions scaledOptions = new TiffRgbaReadOptions();
        scaledOptions.ApplyMinMaxScaling = true;

        TiffRgba8ReadResult nativeRange = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba8ReadResult declaredRange = image.ReadRgba8(scaledOptions);

        Assert.That(nativeRange.Success, Is.True);
        Assert.That(nativeRange.Bytes, Is.EqualTo(new byte[] { 10, 10, 10, 255, 60, 60, 60, 255, 110, 110, 110, 255 }));
        Assert.That(declaredRange.Success, Is.True);
        Assert.That(declaredRange.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 255, 128, 128, 128, 255, 255, 255, 255, 255 }));
    }

    [Test]
    public void ReadRgba8_ControlsFillOrderForPackedSamples()
    {
        ITiffImage image = CreateImage(8, 1, new ulong[] { 1 }, TiffPhotometric.BlackIsZero, new byte[] { 1 }, new TiffExtraSample[0], TiffOrientation.TopLeft, 2, new ulong[0], new ulong[0]);
        TiffRgbaReadOptions ignoredOptions = new TiffRgbaReadOptions();
        ignoredOptions.ApplyFillOrder = false;

        TiffRgba8ReadResult applied = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba8ReadResult ignored = image.ReadRgba8(ignoredOptions);

        Assert.That(applied.Success, Is.True);
        Assert.That(applied.Bytes[0], Is.EqualTo(255));
        Assert.That(applied.Bytes[28], Is.EqualTo(0));
        Assert.That(ignored.Success, Is.True);
        Assert.That(ignored.Bytes[0], Is.EqualTo(0));
        Assert.That(ignored.Bytes[28], Is.EqualTo(255));
    }

    [Test]
    public void ReadRgba8_ControlsPhotometricConversion()
    {
        ITiffImage image = CreateImage(1, 1, new ulong[] { 8 }, TiffPhotometric.WhiteIsZero, new byte[] { 0 }, new TiffExtraSample[0], TiffOrientation.TopLeft, 1, new ulong[0], new ulong[0]);
        TiffRgbaReadOptions directOptions = new TiffRgbaReadOptions();
        directOptions.ApplyPhotometricConversion = false;

        TiffRgba8ReadResult converted = image.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba8ReadResult direct = image.ReadRgba8(directOptions);

        Assert.That(converted.Success, Is.True);
        Assert.That(converted.Bytes, Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
        Assert.That(direct.Success, Is.True);
        Assert.That(direct.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 255 }));
    }

    [Test]
    public void ReadRgba8_HandlesUnsupportedPhotometricAccordingToPolicy()
    {
        ITiffImage errorImage = CreateImage(1, 1, new ulong[] { 1 }, TiffPhotometric.TransparencyMask, new byte[] { 128 }, new TiffExtraSample[0], TiffOrientation.TopLeft, 1, new ulong[0], new ulong[0]);
        ITiffImage skipImage = CreateImage(1, 1, new ulong[] { 1 }, TiffPhotometric.TransparencyMask, new byte[] { 128 }, new TiffExtraSample[0], TiffOrientation.TopLeft, 1, new ulong[0], new ulong[0]);
        ITiffImage ignoreImage = CreateImage(1, 1, new ulong[] { 1 }, TiffPhotometric.TransparencyMask, new byte[] { 128 }, new TiffExtraSample[0], TiffOrientation.TopLeft, 1, new ulong[0], new ulong[0]);
        TiffRgbaReadOptions skipOptions = new TiffRgbaReadOptions();
        skipOptions.UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.Skip;
        TiffRgbaReadOptions ignoreOptions = new TiffRgbaReadOptions();
        ignoreOptions.UnsupportedFeaturePolicy = TiffUnsupportedFeaturePolicy.Ignore;

        TiffRgba8ReadResult error = errorImage.ReadRgba8(new TiffRgbaReadOptions());
        TiffRgba8ReadResult skipped = skipImage.ReadRgba8(skipOptions);
        TiffRgba8ReadResult ignored = ignoreImage.ReadRgba8(ignoreOptions);

        Assert.That(error.Success, Is.False);
        Assert.That(error.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
        Assert.That(skipped.Success, Is.True);
        Assert.That(skipped.Bytes, Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
        Assert.That(skipped.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
        Assert.That(ignored.Success, Is.True);
        Assert.That(ignored.Bytes, Is.EqualTo(new byte[] { 255, 255, 255, 255 }));
        Assert.That(ignored.Diagnostics.ContainsCode(TiffErrorCode.UnsupportedPhotometric), Is.True);
    }

    static ITiffImage CreateImage(ulong width, ulong length, ulong[] bitsPerSample, TiffPhotometric photometric, byte[] pixels, TiffExtraSample[] extraSamples, TiffOrientation orientation, int fillOrder, ulong[] minimumValues, ulong[] maximumValues)
    {
        List<TiffTagValue> tags = new List<TiffTagValue>();
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, width));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, length));
        tags.Add(TiffTagValue.CreateUnsignedArray(TiffTagId.BitsPerSample, TiffDataType.Short, bitsPerSample));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.None));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)photometric));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.FillOrder, TiffDataType.Short, (ulong)fillOrder));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.SamplesPerPixel, TiffDataType.Short, (ulong)bitsPerSample.Length));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, length));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.PlanarConfiguration, TiffDataType.Short, (ulong)TiffPlanarConfiguration.Contiguous));
        tags.Add(TiffTagValue.CreateUnsignedScalar(TiffTagId.Orientation, TiffDataType.Short, (ulong)orientation));
        ulong[] sampleFormats = new ulong[bitsPerSample.Length];
        int sampleIndex = 0;
        while (sampleIndex < sampleFormats.Length)
        {
            sampleFormats[sampleIndex] = (ulong)TiffSampleFormat.UnsignedInteger;
            sampleIndex = sampleIndex + 1;
        }

        tags.Add(TiffTagValue.CreateUnsignedArray(TiffTagId.SampleFormat, TiffDataType.Short, sampleFormats));
        if (extraSamples.Length > 0)
        {
            ulong[] values = new ulong[extraSamples.Length];
            int index = 0;
            while (index < values.Length)
            {
                values[index] = (ulong)extraSamples[index];
                index = index + 1;
            }

            tags.Add(TiffTagValue.CreateUnsignedArray(TiffTagId.ExtraSamples, TiffDataType.Short, values));
        }

        if (minimumValues.Length > 0)
        {
            tags.Add(TiffTagValue.CreateUnsignedArray(TiffTagId.MinSampleValue, TiffDataType.Short, minimumValues));
        }

        if (maximumValues.Length > 0)
        {
            tags.Add(TiffTagValue.CreateUnsignedArray(TiffTagId.MaxSampleValue, TiffDataType.Short, maximumValues));
        }

        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(tags.ToArray()));
        writer.WriteRawStrip(imageIndex, 0, pixels);
        TiffDiagnostics diagnostics = writer.Flush();
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
        TiffFile file = TiffFile.OpenRead(writer.GetFileBytes());
        Assert.That(file.Success, Is.True);
        return file.GetImage(0);
    }
}
