using raw;

namespace raw.Tests;

public class PublicProcessingSurfaceTests
{
    [Test]
    public void PublicPreprocessingCropAndHistogramOperationsUseEachInputDomain()
    {
        IRawCodec codec = new RawCodecService();
        RawImageInfo info = GrayInfo();
        RawStoredImage stored = new RawStoredImage(info, new uint[] { 10, 20, 30, 40, 10, 50, 60, 70 }, null, null, 4, true, null);
        RawArea optical = new RawArea(RawAreaKind.OpticalBlack, 0, 0, 1, 2);
        RawArea userCrop = new RawArea(RawAreaKind.UserCrop, 2, 0, 2, 2);
        RawNormalizationOptions opticalOptions = new RawNormalizationOptions();
        opticalOptions.MaskedAreas = new RawArea[] { optical };
        opticalOptions.UseOpticalBlack = true;
        opticalOptions.WhiteLevels = new double[] { 110 };
        opticalOptions.UserCrop = userCrop;

        RawBlackLevel estimate = codec.EstimateOpticalBlackLevel(stored, opticalOptions);
        RawSensorImage fromStored = codec.PreprocessStoredImage(stored, opticalOptions);
        RawSensorImage decoded = new RawSensorImage(info, new ushort[8], null, 4, true, stored, null, new RawArea[] { optical }, RawBlackLevel.CreateScalar(10, 1), new double[] { 110 }, null, null, null);
        RawNormalizationOptions sensorOptions = new RawNormalizationOptions();
        sensorOptions.UserCrop = userCrop;
        RawSensorImage fromSensor = codec.PreprocessSensorImage(decoded, sensorOptions);
        RawCropModel crops = codec.ResolveCrop(info, userCrop);
        RawHistogramOptions histogramOptions = new RawHistogramOptions();
        histogramOptions.HistogramBinCount = 4;
        RawHistogramResult storedHistogram = codec.HistogramStoredImage(stored, histogramOptions);
        RawHistogramResult sensorHistogram = codec.HistogramSensorImage(fromSensor, histogramOptions);

        Assert.Multiple(() =>
        {
            Assert.That(estimate.Values, Is.EqualTo(new double[] { 10 }));
            Assert.That(fromStored.FloatingSamples[2], Is.EqualTo(0.2F).Within(0.000001F));
            Assert.That(fromSensor.FloatingSamples, Is.EqualTo(fromStored.FloatingSamples).Within(0.000001F));
            Assert.That(fromSensor.StoredImage, Is.SameAs(stored));
            Assert.That((crops.SelectedArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).X, Is.EqualTo(2));
            Assert.That(crops.SelectedArea.Width, Is.EqualTo(2));
            Assert.That(storedHistogram.Channels[0].Minimum, Is.EqualTo(10));
            Assert.That(storedHistogram.Channels[0].Maximum, Is.EqualTo(70));
            Assert.That(storedHistogram.Channels[0].SampleCount, Is.EqualTo(8));
            Assert.That(sensorHistogram.Area.X, Is.EqualTo(2));
            Assert.That(sensorHistogram.Area.Width, Is.EqualTo(2));
            Assert.That(sensorHistogram.Channels[0].SampleCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void PublicPreprocessingHonorsLinearizationBlackWhiteAndRetentionToggles()
    {
        IRawCodec codec = new RawCodecService();
        RawImageInfo info = GrayInfo();
        RawStoredImage stored = new RawStoredImage(info, new uint[] { 10, 20, 30, 40, 50, 60, 70, 80 }, null, null, 4, true, null);
        ushort[] table = Enumerable.Range(0, 256).Select(value => (ushort)(value * 2)).ToArray();
        RawNormalizationOptions enabled = new RawNormalizationOptions();
        enabled.LinearizationTable = table;
        enabled.BlackLevel = RawBlackLevel.CreateScalar(20, 1);
        enabled.WhiteLevels = new double[] { 220 };
        enabled.PreserveOriginalValues = true;
        RawNormalizationOptions disabled = new RawNormalizationOptions();
        disabled.LinearizationTable = table;
        disabled.BlackLevel = RawBlackLevel.CreateScalar(20, 1);
        disabled.WhiteLevels = new double[] { 220 };
        disabled.ApplyLinearization = false;
        disabled.SubtractBlackLevel = false;
        disabled.ScaleWhiteLevel = false;
        disabled.PreserveOriginalValues = false;

        RawSensorImage transformed = codec.PreprocessStoredImage(stored, enabled);
        RawSensorImage identityDomain = codec.PreprocessStoredImage(stored, disabled);

        Assert.Multiple(() =>
        {
            Assert.That(transformed.FloatingSamples[0], Is.Zero);
            Assert.That(transformed.FloatingSamples[1], Is.EqualTo(0.1F).Within(0.000001F));
            Assert.That(identityDomain.FloatingSamples[0], Is.EqualTo(10.0F / 255.0F).Within(0.000001F));
            Assert.That(identityDomain.FloatingSamples[1], Is.EqualTo(20.0F / 255.0F).Within(0.000001F));
            Assert.That(transformed.StoredImage, Is.SameAs(stored));
            Assert.That(identityDomain.StoredImage, Is.Null);
            Assert.That(stored.UnsignedSamples, Is.EqualTo(new uint[] { 10, 20, 30, 40, 50, 60, 70, 80 }));
        });
    }

    [Test]
    public void PublicBuiltInStagesComposeThroughFinalOutputAndRenderedHistogram()
    {
        IRawCodec codec = new RawCodecService();
        RawSensorImage sensor = BayerSensor();
        RawRenderOptions reconstructOptions = LinearCameraOptions();
        reconstructOptions.DemosaicMode = RawDemosaicMode.Bilinear;
        RawRgbImage reconstructed = codec.ReconstructSensorImage(sensor, reconstructOptions);

        RawRenderOptions whiteBalanceOptions = LinearCameraOptions();
        whiteBalanceOptions.WhiteBalanceMode = RawWhiteBalanceMode.UserMultipliers;
        whiteBalanceOptions.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.UserMultipliers, new double[] { 1.25, 1.0, 0.75 }, 0.0, 0.0);
        RawRgbImage balanced = codec.ApplyWhiteBalance(reconstructed, null, whiteBalanceOptions);

        RawRenderOptions detailOptions = LinearCameraOptions();
        detailOptions.CaptureSharpening = 0.1;
        RawRgbImage detailed = codec.EnhanceDetails(balanced, null, detailOptions);
        RawRenderOptions exposureOptions = LinearCameraOptions();
        exposureOptions.ExposureCompensation = 0.5;
        exposureOptions.HighlightMode = RawHighlightMode.Blend;
        RawRgbImage exposed = codec.ApplyExposureAndHighlights(detailed, exposureOptions);

        RawRenderOptions xyzOptions = LinearCameraOptions();
        xyzOptions.ColorSpace = RawColorSpace.XyzD65;
        RawRgbImage xyz = codec.ConvertCameraToXyz(exposed, new RawColorProfile[] { IdentityColorProfile() }, 6504.0, xyzOptions);
        RawRenderOptions outputColorOptions = LinearCameraOptions();
        outputColorOptions.ColorSpace = RawColorSpace.Srgb;
        RawRgbImage outputColor = codec.ConvertOutputColor(xyz, outputColorOptions);
        RawRenderOptions toneOptions = LinearCameraOptions();
        toneOptions.ColorSpace = RawColorSpace.Srgb;
        toneOptions.TransferFunction = RawTransferFunction.Srgb;
        toneOptions.ToneCurve = new double[] { 0.0, 0.4, 1.0 };
        toneOptions.Contrast = 1.1;
        toneOptions.GamutMode = RawGamutMode.Compress;
        RawRgbImage toned = codec.ApplyToneAndGamut(outputColor, toneOptions);
        RawRenderOptions outputOptions = LinearCameraOptions();
        outputOptions.ColorSpace = RawColorSpace.Srgb;
        outputOptions.TransferFunction = RawTransferFunction.Srgb;
        outputOptions.OutputBitDepth = 8;
        outputOptions.OutputWidth = 2;
        outputOptions.OutputHeight = 2;
        outputOptions.ApplyOrientation = true;
        RawRgbImage output = codec.CreateOutputImage(toned, new RawArea(RawAreaKind.UserCrop, 1, 1, 2, 2), outputOptions);
        RawHistogramResult histogram = codec.HistogramRenderedImage(output, new RawHistogramOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reconstructed.Width, Is.EqualTo(4));
            Assert.That(reconstructed.ChannelCount, Is.EqualTo(3));
            Assert.That(balanced.FloatingSamples[0], Is.GreaterThan(reconstructed.FloatingSamples[0]));
            Assert.That(exposed.FloatingSamples, Is.Not.EqualTo(detailed.FloatingSamples));
            Assert.That(xyz.ColorSpace, Is.EqualTo(RawColorSpace.XyzD65));
            Assert.That(outputColor.ColorSpace, Is.EqualTo(RawColorSpace.Srgb));
            Assert.That(toned.TransferFunction, Is.EqualTo(RawTransferFunction.Srgb));
            Assert.That(output.Width, Is.EqualTo(2));
            Assert.That(output.Height, Is.EqualTo(2));
            Assert.That(output.ByteSamples, Has.Length.EqualTo(12));
            Assert.That(output.OrientationApplied, Is.True);
            Assert.That(histogram.Channels, Has.Length.EqualTo(3));
            Assert.That(histogram.Channels.Select(channel => channel.SampleCount), Is.All.EqualTo(4));
        });
    }

    [TestCase(RawIccRenderingIntent.Perceptual)]
    [TestCase(RawIccRenderingIntent.MediaRelativeColorimetric)]
    [TestCase(RawIccRenderingIntent.Saturation)]
    [TestCase(RawIccRenderingIntent.IccAbsoluteColorimetric)]
    public void PublicIccToAndFromXyzOperationsRoundTripEveryIntent(RawIccRenderingIntent intent)
    {
        IRawCodec codec = new RawCodecService();
        RawIccToneCurve gamma = new RawIccToneCurve(RawIccCurveKind.Gamma, 0, 2.0, null, null);
        RawIccProfile profile = IccProfile(gamma);
        RawRgbImage input = new RawRgbImage(1, 1, 3, 3, null, null, new float[] { 0.5F, 0.25F, 0.75F }, RgbChannels(), RawColorSpace.IccRgb, RawTransferFunction.Icc, RawOrientation.Normal, true, true, null);
        RawIccTransformOptions options = new RawIccTransformOptions();
        options.RenderingIntent = intent;

        RawRgbImage xyz = codec.ConvertIccToXyz(input, profile, options);
        RawRgbImage output = codec.ConvertXyzToIcc(xyz, profile, options);
        RawRgbImage direct = codec.ConvertIcc(input, profile, profile, options);

        Assert.Multiple(() =>
        {
            Assert.That(xyz.ColorSpace, Is.EqualTo(RawColorSpace.XyzD50));
            Assert.That(xyz.FloatingSamples, Is.EqualTo(new float[] { 0.25F, 0.0625F, 0.5625F }).Within(0.000001F));
            Assert.That(output.FloatingSamples, Is.EqualTo(input.FloatingSamples).Within(0.000001F));
            Assert.That(direct.FloatingSamples, Is.EqualTo(output.FloatingSamples).Within(0.000001F));
            Assert.That(output.ColorSpace, Is.EqualTo(RawColorSpace.IccRgb));
        });
    }

    [TestCase(RawDemosaicMode.None)]
    [TestCase(RawDemosaicMode.NearestNeighbor)]
    [TestCase(RawDemosaicMode.Bilinear)]
    [TestCase(RawDemosaicMode.Vng)]
    [TestCase(RawDemosaicMode.Ahd)]
    public void PublicReconstructionCoversEveryDemosaicMode(RawDemosaicMode mode)
    {
        IRawCodec codec = new RawCodecService();
        RawRenderOptions options = LinearCameraOptions();
        options.DemosaicMode = mode;

        RawRgbImage result = codec.ReconstructSensorImage(BayerSensor(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.ChannelCount, Is.EqualTo(mode == RawDemosaicMode.None ? 1 : 3));
            Assert.That(result.FloatingSamples, Is.All.Matches<float>(value => float.IsFinite(value)));
            Assert.That(result.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [TestCase(RawWhiteBalanceMode.None)]
    [TestCase(RawWhiteBalanceMode.Camera)]
    [TestCase(RawWhiteBalanceMode.Daylight)]
    [TestCase(RawWhiteBalanceMode.Auto)]
    [TestCase(RawWhiteBalanceMode.UserMultipliers)]
    [TestCase(RawWhiteBalanceMode.TemperatureAndTint)]
    public void PublicWhiteBalanceCoversEveryMode(RawWhiteBalanceMode mode)
    {
        IRawCodec codec = new RawCodecService();
        RawRgbImage input = CameraRgbImage();
        float[] original = input.FloatingSamples.ToArray();
        RawRenderOptions options = LinearCameraOptions();
        options.WhiteBalanceMode = mode;
        RawWhiteBalance? camera = null;
        if (mode == RawWhiteBalanceMode.Camera)
        {
            camera = new RawWhiteBalance(RawWhiteBalanceMode.Camera, new double[] { 2.0, 1.0, 1.5 }, 0.0, 0.0);
        }
        else if (mode == RawWhiteBalanceMode.Daylight)
        {
            options.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.Daylight, new double[] { 1.5, 1.0, 1.25 }, 6500.0, 0.0);
        }
        else if (mode == RawWhiteBalanceMode.UserMultipliers)
        {
            options.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.UserMultipliers, new double[] { 1.25, 1.0, 0.75 }, 0.0, 0.0);
        }
        else if (mode == RawWhiteBalanceMode.TemperatureAndTint)
        {
            options.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.TemperatureAndTint, null, 5000.0, 0.25);
        }

        RawRgbImage result = codec.ApplyWhiteBalance(input, camera, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.FloatingSamples, Is.All.Matches<float>(value => float.IsFinite(value)));
            Assert.That(result.FloatingSamples, Is.Not.SameAs(input.FloatingSamples));
            Assert.That(input.FloatingSamples, Is.EqualTo(original));
        });
    }

    [TestCase(RawColorSpace.XyzD50, RawTransferFunction.Linear)]
    [TestCase(RawColorSpace.XyzD65, RawTransferFunction.Linear)]
    [TestCase(RawColorSpace.Srgb, RawTransferFunction.Linear)]
    [TestCase(RawColorSpace.Srgb, RawTransferFunction.Srgb)]
    [TestCase(RawColorSpace.Srgb, RawTransferFunction.Gamma)]
    [TestCase(RawColorSpace.DisplayP3, RawTransferFunction.Linear)]
    [TestCase(RawColorSpace.DisplayP3, RawTransferFunction.Srgb)]
    [TestCase(RawColorSpace.DisplayP3, RawTransferFunction.Gamma)]
    [TestCase(RawColorSpace.AdobeRgb, RawTransferFunction.Linear)]
    [TestCase(RawColorSpace.AdobeRgb, RawTransferFunction.Gamma)]
    [TestCase(RawColorSpace.ProPhotoRgb, RawTransferFunction.Linear)]
    [TestCase(RawColorSpace.ProPhotoRgb, RawTransferFunction.ProPhotoRgb)]
    [TestCase(RawColorSpace.ProPhotoRgb, RawTransferFunction.Gamma)]
    [TestCase(RawColorSpace.Rec2020, RawTransferFunction.Linear)]
    [TestCase(RawColorSpace.Rec2020, RawTransferFunction.Rec2020)]
    [TestCase(RawColorSpace.Rec2020, RawTransferFunction.Gamma)]
    [TestCase(RawColorSpace.AcesCg, RawTransferFunction.Linear)]
    public void PublicOutputColorCoversEveryBuiltInSpaceAndTransfer(RawColorSpace colorSpace, RawTransferFunction transfer)
    {
        IRawCodec codec = new RawCodecService();
        RawRenderOptions options = LinearCameraOptions();
        options.ColorSpace = colorSpace;
        options.TransferFunction = transfer;
        options.Gamma = 2.2;

        RawRgbImage result = codec.ConvertOutputColor(XyzImage(), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.ColorSpace, Is.EqualTo(colorSpace));
            Assert.That(result.TransferFunction, Is.EqualTo(transfer));
            Assert.That(result.FloatingSamples, Is.All.Matches<float>(value => float.IsFinite(value)));
        });
    }

    [Test]
    public void PublicHighlightDetailAndGamutStagesCoverEveryMode()
    {
        IRawCodec codec = new RawCodecService();
        RawRgbImage input = CameraRgbImage();
        foreach (RawHighlightMode mode in new RawHighlightMode[] { RawHighlightMode.Clip, RawHighlightMode.Blend, RawHighlightMode.Reconstruct })
        {
            RawRenderOptions options = LinearCameraOptions();
            options.HighlightMode = mode;
            options.ExposureCompensation = 1.0;
            RawRgbImage result = codec.ApplyExposureAndHighlights(input, options);
            Assert.That(result.FloatingSamples, Is.All.Matches<float>(value => float.IsFinite(value)), mode.ToString());
        }
        foreach (RawDenoiseMode mode in new RawDenoiseMode[] { RawDenoiseMode.None, RawDenoiseMode.HotPixel, RawDenoiseMode.MedianChroma, RawDenoiseMode.WaveletLike })
        {
            RawRenderOptions options = LinearCameraOptions();
            options.DenoiseMode = mode;
            options.DenoiseStrength = mode == RawDenoiseMode.None ? 0.0 : 0.5;
            options.FalseColorSuppression = 0.25;
            options.CaptureSharpening = 0.1;
            RawRgbImage result = codec.EnhanceDetails(input, null, options);
            Assert.That(result.FloatingSamples, Is.All.Matches<float>(value => float.IsFinite(value)), mode.ToString());
        }
        foreach (RawGamutMode mode in new RawGamutMode[] { RawGamutMode.None, RawGamutMode.Clip, RawGamutMode.Compress })
        {
            RawRenderOptions options = LinearCameraOptions();
            options.GamutMode = mode;
            RawRgbImage result = codec.ApplyToneAndGamut(input, options);
            Assert.That(result.FloatingSamples, Is.All.Matches<float>(value => float.IsFinite(value)), mode.ToString());
        }
    }

    [Test]
    public void PublicOutputCoversEveryOrientationResamplerDepthAndRgbaChannelOrder()
    {
        IRawCodec codec = new RawCodecService();
        RawRgbImage input = BgraImage();
        foreach (RawOrientation orientation in new RawOrientation[]
        {
            RawOrientation.Normal, RawOrientation.MirrorHorizontal, RawOrientation.Rotate180, RawOrientation.MirrorVertical,
            RawOrientation.Transpose, RawOrientation.Rotate90Clockwise, RawOrientation.Transverse, RawOrientation.Rotate270Clockwise
        })
        {
            foreach (RawResampler resampler in new RawResampler[] { RawResampler.NearestNeighbor, RawResampler.Bilinear, RawResampler.Bicubic, RawResampler.Lanczos })
            {
                foreach (int depth in new int[] { 8, 16, 32 })
                {
                    input.Orientation = orientation;
                    RawRenderOptions options = LinearCameraOptions();
                    options.Resampler = resampler;
                    options.OutputBitDepth = depth;
                    options.OutputWidth = 3;
                    options.OutputHeight = 2;
                    options.ApplyOrientation = true;
                    RawRgbImage result = codec.CreateOutputImage(input, null, options);

                    Assert.Multiple(() =>
                    {
                        Assert.That(result.Width, Is.EqualTo(3), $"{orientation} {resampler} {depth}");
                        Assert.That(result.Height, Is.EqualTo(2), $"{orientation} {resampler} {depth}");
                        Assert.That(result.Channels.Select(channel => channel.Kind), Is.EqualTo(new RawChannelKind[] { RawChannelKind.Red, RawChannelKind.Green, RawChannelKind.Blue, RawChannelKind.Alpha }));
                        Assert.That(result.ByteSamples.Length != 0, Is.EqualTo(depth == 8));
                        Assert.That(result.UShortSamples.Length != 0, Is.EqualTo(depth == 16));
                        Assert.That(result.FloatingSamples.Length != 0, Is.EqualTo(depth == 32));
                    });
                }
            }
        }

        RawChannelDescription[] grayChannels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawRgbImage gray = new RawRgbImage(2, 1, 1, 2, null, null, new float[] { 0.25F, 0.75F }, grayChannels, RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
        foreach (int depth in new int[] { 8, 16, 32 })
        {
            RawRenderOptions options = LinearCameraOptions();
            options.OutputBitDepth = depth;
            RawRgbImage result = codec.CreateOutputImage(gray, null, options);
            Assert.Multiple(() =>
            {
                Assert.That(result.ChannelCount, Is.EqualTo(1));
                Assert.That(result.Channels.Single().Kind, Is.EqualTo(RawChannelKind.Gray));
                Assert.That(result.ByteSamples.Length != 0, Is.EqualTo(depth == 8));
                Assert.That(result.UShortSamples.Length != 0, Is.EqualTo(depth == 16));
                Assert.That(result.FloatingSamples.Length != 0, Is.EqualTo(depth == 32));
            });
        }
    }

    [Test]
    public void PublicAssembledRenderMatchesDocumentedManualStageOrder()
    {
        IRawCodec codec = new RawCodecService();
        RawSensorImage sensor = BayerSensor();
        RawRenderOptions options = LinearCameraOptions();
        options.DemosaicMode = RawDemosaicMode.Bilinear;
        options.WhiteBalanceMode = RawWhiteBalanceMode.UserMultipliers;
        options.WhiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.UserMultipliers, new double[] { 1.2, 1.0, 0.8 }, 0.0, 0.0);
        options.DenoiseMode = RawDenoiseMode.MedianChroma;
        options.DenoiseStrength = 0.2;
        options.CaptureSharpening = 0.1;
        options.ExposureCompensation = 0.25;
        options.HighlightMode = RawHighlightMode.Blend;
        options.ToneCurve = new double[] { 0.0, 0.45, 1.0 };
        options.Contrast = 1.05;
        options.GamutMode = RawGamutMode.Clip;

        RawRgbImage reconstructed = codec.ReconstructSensorImage(sensor, options);
        RawRgbImage balanced = codec.ApplyWhiteBalance(reconstructed, sensor.WhiteBalance, options);
        RawRgbImage detailed = codec.EnhanceDetails(balanced, null, options);
        RawRgbImage exposed = codec.ApplyExposureAndHighlights(detailed, options);
        RawRgbImage colored = codec.ConvertOutputColor(exposed, options);
        RawRgbImage toned = codec.ApplyToneAndGamut(colored, options);
        RawRgbImage manual = codec.CreateOutputImage(toned, null, options);
        RawRgbImage assembled = codec.RenderSensorImage(sensor, options);

        Assert.That(assembled.FloatingSamples, Is.EqualTo(manual.FloatingSamples).Within(0.000001F));
    }

    private static RawImageInfo GrayInfo()
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, 4, 2, 8, 4) };
        return new RawImageInfo(0, RawImageKind.Raw, 4, 2, 8, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, 4, 2), new RawArea(RawAreaKind.Active, 1, 0, 3, 2), new RawArea(RawAreaKind.DefaultCrop, 1, 0, 3, 2), null);
    }

    private static RawSensorImage BayerSensor()
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, 4, 4, 16, 4) };
        RawImageInfo info = new RawImageInfo(0, RawImageKind.Raw, 4, 4, 16, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.MirrorHorizontal, channels, planes, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb), new RawArea(RawAreaKind.StoredImage, 0, 0, 4, 4), null, null, null);
        return new RawSensorImage(info, null, new float[] { 0.1F, 0.2F, 0.3F, 0.4F, 0.25F, 0.35F, 0.45F, 0.55F, 0.4F, 0.5F, 0.6F, 0.7F, 0.55F, 0.65F, 0.75F, 0.85F }, 4, true, null, null, null, null, null, null, null, null);
    }

    private static RawRgbImage CameraRgbImage()
    {
        return new RawRgbImage(3, 3, 3, 9, null, null, new float[]
        {
            0.1F, 0.2F, 0.3F, 0.2F, 0.3F, 0.4F, 0.3F, 0.4F, 0.5F,
            0.2F, 0.3F, 0.4F, 1.5F, 0.5F, 0.25F, 0.4F, 0.5F, 0.6F,
            0.3F, 0.4F, 0.5F, 0.4F, 0.5F, 0.6F, 0.5F, 0.6F, 0.7F
        }, RgbChannels(), RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }

    private static RawRgbImage XyzImage()
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("X", RawChannelKind.Custom),
            new RawChannelDescription("Y", RawChannelKind.Custom),
            new RawChannelDescription("Z", RawChannelKind.Custom)
        };
        return new RawRgbImage(1, 1, 3, 3, null, null, new float[] { 0.25F, 0.4F, 0.2F }, channels, RawColorSpace.XyzD65, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }

    private static RawRgbImage BgraImage()
    {
        RawChannelDescription[] channels = new RawChannelDescription[]
        {
            new RawChannelDescription("B", RawChannelKind.Blue),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("A", RawChannelKind.Alpha)
        };
        return new RawRgbImage(2, 2, 4, 8, null, null, new float[]
        {
            0.1F, 0.2F, 0.3F, 1.0F, 0.2F, 0.3F, 0.4F, 0.9F,
            0.3F, 0.4F, 0.5F, 0.8F, 0.4F, 0.5F, 0.6F, 0.7F
        }, channels, RawColorSpace.RawCamera, RawTransferFunction.Linear, RawOrientation.Normal, false, true, null);
    }

    private static RawRenderOptions LinearCameraOptions()
    {
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.None;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.ColorSpace = RawColorSpace.RawCamera;
        options.TransferFunction = RawTransferFunction.Linear;
        options.OutputBitDepth = 32;
        options.ApplyOrientation = false;
        return options;
    }

    private static RawColorProfile IdentityColorProfile()
    {
        double[] identity = new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
        return new RawColorProfile("Identity D65", RawColorIlluminant.D65, 0.0, identity, null, null, null, RawChromaticAdaptation.WhitePoint(RawColorIlluminant.D65, 0.0), null);
    }

    private static RawIccProfile IccProfile(RawIccToneCurve curve)
    {
        double[] d50 = new double[] { 0.9642, 1.0, 0.8249 };
        RawIccHeader header = new RawIccHeader(132, "TEST", 4, 4, 0, "mntr", "RGB ", "XYZ ", 2026, 8, 20, 12, 0, 0, true, "APPL", 0, "TEST", "RGB ", 0, 1, d50, "TEST");
        return new RawIccProfile(new byte[132], header, System.Array.Empty<RawIccTag>(), "Synthetic", "Synthetic", new byte[16], new byte[16], string.Empty, string.Empty, false, false, true, new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 }, d50, curve, curve, curve, -1, string.Empty);
    }

    private static RawChannelDescription[] RgbChannels()
    {
        return new RawChannelDescription[]
        {
            new RawChannelDescription("R", RawChannelKind.Red),
            new RawChannelDescription("G", RawChannelKind.Green),
            new RawChannelDescription("B", RawChannelKind.Blue)
        };
    }
}
