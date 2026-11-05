using raw;

namespace raw.Tests;

public class PublicRenderPipelineTests
{
    [Test]
    public void DocumentAndSensorOperationsShareTheCompleteCropAwarePipeline()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor = RawDescriptor.CreateRgb(3, 2, 8, 8, RawByteOrder.LittleEndian);
        descriptor.Orientation = RawOrientation.MirrorHorizontal;
        byte[] source = new byte[]
        {
            16, 32, 48, 64, 80, 96, 112, 128, 144,
            160, 176, 192, 208, 224, 240, 32, 64, 96
        };
        RawDocument document = codec.Open(source, descriptor, new RawOpenOptions());
        RawSensorImage sensor = codec.Decode(document, new RawDecodeOptions());
        RawRenderOptions options = CameraOptions();
        options.UserCrop = new RawArea(RawAreaKind.UserCrop, 1, 0, 2, 2);
        options.WhiteBalanceMode = RawWhiteBalanceMode.UserMultipliers;
        options.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.UserMultipliers, new double[] { 1.25, 1.0, 0.75 }, 0.0, 0.0);
        options.HighlightMode = RawHighlightMode.Blend;
        options.ExposureCompensation = 0.5;
        options.ShadowScale = 1.1;
        options.SaturationProtection = 0.25;
        options.DenoiseMode = RawDenoiseMode.MedianChroma;
        options.DenoiseStrength = 0.25;
        options.FalseColorSuppression = 0.2;
        options.CaptureSharpening = 0.1;
        options.ToneCurve = new double[] { 0.0, 0.4, 1.0 };
        options.Contrast = 1.1;
        options.GamutMode = RawGamutMode.Compress;
        options.OutputWidth = 1;
        options.OutputHeight = 2;
        options.OutputBitDepth = 16;
        options.ApplyOrientation = true;

        RawRgbImage fromDocument = codec.Render(document, options);
        RawRgbImage fromSensor = codec.RenderSensorImage(sensor, options);

        Assert.Multiple(() =>
        {
            Assert.That(fromDocument.UShortSamples, Is.EqualTo(fromSensor.UShortSamples));
            Assert.That(fromDocument.Width, Is.EqualTo(1));
            Assert.That(fromDocument.Height, Is.EqualTo(2));
            Assert.That(fromDocument.ChannelCount, Is.EqualTo(3));
            Assert.That(fromDocument.OrientationApplied, Is.True);
            Assert.That(fromDocument.ColorSpace, Is.EqualTo(RawColorSpace.RawCamera));
            Assert.That(fromDocument.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(fromDocument.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(sensor.UserCrop, Is.Null);
            Assert.That(sensor.CropModel, Is.Null);
            Assert.That(sensor.IntegerSamples, Is.EqualTo(source.Select(value => (ushort)(value * 257))));
        });
    }

    [Test]
    public void DocumentRenderDemosaicsBayerAndRejectsUnavailableSelectionsAndCrops()
    {
        IRawCodec codec = new RawCodecService();
        RawDescriptor descriptor = RawDescriptor.CreateBayer(4, 4, 8, 8, RawByteOrder.LittleEndian, RawCfaLayout.Rggb);
        byte[] source = new byte[]
        {
            16, 32, 48, 64,
            80, 96, 112, 128,
            144, 160, 176, 192,
            208, 224, 240, 255
        };
        RawDocument document = codec.Open(source, descriptor, new RawOpenOptions());
        RawRenderOptions options = CameraOptions();
        options.DemosaicMode = RawDemosaicMode.Bilinear;
        options.UserCrop = new RawArea(RawAreaKind.UserCrop, 1, 1, 2, 2);

        RawRgbImage rendered = codec.Render(document, options);

        RawRenderOptions missingImage = CameraOptions();
        missingImage.DemosaicMode = RawDemosaicMode.Bilinear;
        missingImage.ImageIndex = 1;
        RawRenderOptions missingPlane = CameraOptions();
        missingPlane.DemosaicMode = RawDemosaicMode.Bilinear;
        missingPlane.PlaneIndex = 1;
        RawRenderOptions outsideCrop = CameraOptions();
        outsideCrop.DemosaicMode = RawDemosaicMode.Bilinear;
        outsideCrop.UserCrop = new RawArea(RawAreaKind.UserCrop, 3, 3, 2, 2);
        RawRenderOptions wrongCropKind = CameraOptions();
        wrongCropKind.DemosaicMode = RawDemosaicMode.Bilinear;
        wrongCropKind.UserCrop = new RawArea(RawAreaKind.Active, 0, 0, 2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(rendered.Width, Is.EqualTo(2));
            Assert.That(rendered.Height, Is.EqualTo(2));
            Assert.That(rendered.ChannelCount, Is.EqualTo(3));
            Assert.That(rendered.ByteSamples, Has.Length.EqualTo(12));
            Assert.Throws<RawFormatException>(() => codec.Render(document, missingImage));
            Assert.Throws<RawFormatException>(() => codec.Render(document, missingPlane));
            Assert.Throws<RawFormatException>(() => codec.Render(document, outsideCrop));
            Assert.Throws<RawFormatException>(() => codec.Render(document, wrongCropKind));
        });
    }

    private static RawRenderOptions CameraOptions()
    {
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.None;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.ColorSpace = RawColorSpace.RawCamera;
        options.TransferFunction = RawTransferFunction.Linear;
        options.OutputBitDepth = 8;
        options.ApplyOrientation = false;
        return options;
    }
}
