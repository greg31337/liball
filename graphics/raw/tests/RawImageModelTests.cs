using raw;

namespace raw.Tests;

public class RawImageModelTests
{
    [Test]
    public void AreasPlanesAndCfaImageInfoRetainNonDestructiveGeometry()
    {
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.RawImage, "IFD0/Strip0", 64, 128, 0, 0, 0, RawCompression.None, true, true);
        RawImageInfo info = CreateCfaInfo(4, 3, 12, payload);

        Assert.Multiple(() =>
        {
            Assert.That(info.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(info.StoredArea.Contains(3, 2), Is.True);
            Assert.That(info.StoredArea.Contains(4, 2), Is.False);
            Assert.That((info.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Contains(1, 1), Is.True);
            Assert.That(info.Planes[0].GetRequiredSampleCount(), Is.EqualTo(12));
            Assert.That((info.Cfa ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetColor(2, 2), Is.EqualTo(RawCfaColor.Red));
            Assert.That(info.GetChannelCount(), Is.EqualTo(1));
        });

        (info.ActiveArea ?? throw new System.InvalidOperationException("A required value is unavailable.")).Kind = RawAreaKind.Masked;
        info.Planes[0].RowStrideSamples = 3;

        Assert.Multiple(() =>
        {
            Assert.That(((info.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "ImageInfo.ActiveArea")), Is.True);
            Assert.That(((info.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Plane.Geometry")), Is.True);
            Assert.Throws<RawFormatException>(() => info.Planes[0].GetRequiredSampleCount());
            Assert.That(new RawArea(RawAreaKind.UserCrop, 3, 2, 2, 2).Validate(4, 3).IsValid(), Is.False);
        });
    }

    [Test]
    public void PayloadMapFindsKindsCountsAliasedBytesAndRejectsInvalidRanges()
    {
        RawPayloadBlock rawBlock = new RawPayloadBlock(RawPayloadKind.RawImage, "Raw/Strip0", 10, 20, 0, 0, 0, RawCompression.None, true, true);
        RawPayloadBlock thumbnailBlock = new RawPayloadBlock(RawPayloadKind.Thumbnail, "IFD1/JPEG", 40, 8, -1, -1, 0, RawCompression.BaselineJpeg, true, true);
        RawPayloadBlock alias = new RawPayloadBlock(RawPayloadKind.Opaque, "Alias", 15, 5, -1, -1, -1, RawCompression.Unknown, false, true);
        RawPayloadMap map = new RawPayloadMap(64, new RawPayloadBlock[] { rawBlock, thumbnailBlock, alias });

        Assert.Multiple(() =>
        {
            Assert.That(map.Validate().IsValid(), Is.True);
            Assert.That(map.Find(RawPayloadKind.Thumbnail), Is.EqualTo(new RawPayloadBlock[] { thumbnailBlock }));
            Assert.That(map.Find(RawPayloadKind.Metadata), Is.Empty);
            Assert.That(map.GetMappedByteCount(), Is.EqualTo(33));
            Assert.That(rawBlock.Contains(12, 4), Is.True);
            Assert.That(rawBlock.Contains(29, 2), Is.False);
        });

        thumbnailBlock.Offset = 60;
        Assert.Multiple(() =>
        {
            Assert.That(((map.Validate().Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.InvalidOffset)), Is.True);
            Assert.Throws<RawFormatException>(() => map.GetMappedByteCount());
        });
    }

    [Test]
    public void BlackLevelWhiteBalanceAndColorProfilesExposeCheckedCalibration()
    {
        RawBlackLevel black = new RawBlackLevel(
            2,
            2,
            2,
            new double[] { 10, 20, 30, 40, 50, 60, 70, 80 },
            new double[] { 1, 2, 3 },
            new double[] { 0.1, 0.2, 0.3, 0.4 });
        RawWhiteBalance whiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.Camera, new double[] { 2.0, 1.0, 1.5 }, 5200, 0.1);
        RawColorProfile profile = CreateColorProfile();

        Assert.Multiple(() =>
        {
            Assert.That(black.Validate(4, 3, 2).IsValid(), Is.True);
            Assert.That(black.GetValue(3, 2, 1), Is.EqualTo(43.4).Within(0.000001));
            Assert.Throws<RawFormatException>(() => RawBlackLevel.CreateScalar(64, 0));
            Assert.That(whiteBalance.Validate(3).IsValid(), Is.True);
            Assert.That(whiteBalance.GetNormalizedMultipliers(), Is.EqualTo(new double[] { 2.0, 1.0, 1.5 }));
            Assert.That(profile.Validate().IsValid(), Is.True);
            Assert.That(profile.HasMatrixProfile(), Is.True);
            Assert.That(profile.HasIccProfile(), Is.False);
        });

        black.Values[0] = double.NaN;
        whiteBalance.Multipliers[1] = 0;
        profile.ColorMatrix = new double[] { 1, 2 };

        Assert.Multiple(() =>
        {
            Assert.That(black.Validate(4, 3, 2).IsValid(), Is.False);
            Assert.Throws<RawFormatException>(() => black.GetValue(0, 0, 0));
            Assert.That(whiteBalance.Validate(3).IsValid(), Is.False);
            Assert.Throws<RawFormatException>(() => whiteBalance.GetNormalizedMultipliers());
            Assert.That(profile.Validate().IsValid(), Is.False);
        });
    }

    [Test]
    public void StoredImagesSelectTheDeclaredNumericBufferAndIgnoreRowPaddingValues()
    {
        RawImageInfo unsignedInfo = CreateRgbInfo(2, 2, 12, RawSampleKind.UnsignedInteger);
        uint[] unsignedSamples = new uint[] { 0, 1, 2, 3, 4, 4095, uint.MaxValue, uint.MaxValue, 5, 6, 7, 8, 9, 10 };
        RawStoredImage unsigned = new RawStoredImage(unsignedInfo, unsignedSamples, null, null, 8, false, null);
        RawImageInfo signedInfo = CreateGrayInfo(2, 1, 4, RawSampleKind.SignedInteger);
        RawStoredImage signed = new RawStoredImage(signedInfo, null, new int[] { -8, 7 }, null, 2, true, null);
        RawImageInfo floatInfo = CreateGrayInfo(2, 1, 32, RawSampleKind.Single);
        RawStoredImage floating = new RawStoredImage(floatInfo, null, null, new float[] { float.NaN, float.PositiveInfinity }, 2, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(unsigned.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(signed.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(floating.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(unsigned.GetSamples(), Is.SameAs(unsignedSamples));
            Assert.That(unsigned.OwnsSamples, Is.False);
            Assert.That(signed.OwnsSamples, Is.True);
        });

        unsignedSamples[8] = 4096;
        RawStoredImage wrongKind = new RawStoredImage(unsignedInfo, null, new int[14], null, 8, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(((unsigned.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Code == RawDiagnosticCode.InvalidCodecData)), Is.True);
            Assert.That(((wrongKind.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "StoredImage.SampleKind")), Is.True);
        });
    }

    [Test]
    public void SensorImageCombinesNormalizedSamplesAreasAndCalibrationWithoutApplyingThem()
    {
        RawImageInfo info = CreateCfaInfo(4, 3, 12, null);
        ushort[] samples = new ushort[14];
        RawArea userCrop = new RawArea(RawAreaKind.UserCrop, 1, 1, 2, 2);
        RawArea opticalBlack = new RawArea(RawAreaKind.OpticalBlack, 0, 0, 4, 1);
        RawBlackLevel black = RawBlackLevel.CreateScalar(64, 1);
        RawWhiteBalance whiteBalance = new RawWhiteBalance(RawWhiteBalanceMode.Camera, new double[] { 2.0 }, 5000, 0);
        RawSensorImage image = new RawSensorImage(info, samples, null, 5, true, null, userCrop, new RawArea[] { opticalBlack }, black, new double[] { 4095 }, whiteBalance, new RawColorProfile[] { CreateColorProfile() }, null);

        Assert.Multiple(() =>
        {
            Assert.That(image.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(image.GetSamples(), Is.SameAs(samples));
            Assert.That(image.UserCrop, Is.SameAs(userCrop));
            Assert.That(image.MaskedAreas.Single(), Is.SameAs(opticalBlack));
            Assert.That((image.BlackLevel ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetValue(3, 2, 0), Is.EqualTo(64));
        });

        image.FloatingSamples = new float[14];
        image.UserCrop = new RawArea(RawAreaKind.UserCrop, 3, 2, 2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(((image.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "SensorImage.Samples")), Is.True);
            Assert.That(((image.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "Area.Geometry")), Is.True);
        });
    }

    [Test]
    public void RenderedImagesSupportByteWordAndFloatBuffersWithExplicitOwnership()
    {
        RawChannelDescription[] channels = RgbChannels();
        RawRgbImage bytes = new RawRgbImage(2, 2, 3, 8, new byte[14], null, null, channels, RawColorSpace.Srgb, RawTransferFunction.Srgb, RawOrientation.Normal, true, true, null);
        RawRgbImage words = new RawRgbImage(2, 1, 3, 6, null, new ushort[6], null, channels, RawColorSpace.Rec2020, RawTransferFunction.Linear, RawOrientation.Normal, false, false, null);
        RawRgbImage floats = new RawRgbImage(1, 1, 3, 3, null, null, new float[3], channels, RawColorSpace.AcesCg, RawTransferFunction.Linear, RawOrientation.Rotate90Clockwise, false, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(bytes.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(words.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(floats.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(bytes.GetBitsPerChannel(), Is.EqualTo(8));
            Assert.That(words.GetBitsPerChannel(), Is.EqualTo(16));
            Assert.That(floats.GetBitsPerChannel(), Is.EqualTo(32));
            Assert.That(bytes.GetSamples(), Is.SameAs(bytes.ByteSamples));
            Assert.That(words.OwnsSamples, Is.False);
        });

        bytes.UShortSamples = new ushort[14];
        Assert.That(((bytes.Validate(new RawLimits()).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "RgbImage.Samples")), Is.True);
    }

    [Test]
    public void UncompressedThumbnailsDecodeWithoutCopyingAndOpaqueDataIsExplicitlyUnsupported()
    {
        byte[] rgbBytes = new byte[] { 1, 2, 3, 4, 5, 6 };
        RawPayloadBlock payload = new RawPayloadBlock(RawPayloadKind.Thumbnail, "IFD1/RGB", 100, 6, -1, -1, 0, RawCompression.None, true, true);
        RawThumbnail rgb = new RawThumbnail(0, RawThumbnailKind.Rgb8, 2, 1, RawOrientation.MirrorHorizontal, "application/octet-stream", rgbBytes, payload, null);
        RawThumbnail gray = new RawThumbnail(1, RawThumbnailKind.Gray8, 2, 1, RawOrientation.Normal, "application/octet-stream", new byte[] { 7, 8 }, null, null);
        RawThumbnail opaque = new RawThumbnail(2, RawThumbnailKind.Opaque, 1, 1, RawOrientation.Normal, "image/x-private", new byte[] { 9 }, null, null);

        RawRgbImage decodedRgb = rgb.Decode(new RawLimits(), CancellationToken.None);
        RawRgbImage decodedGray = gray.Decode(new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(rgb.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(decodedRgb.ByteSamples, Is.SameAs(rgbBytes));
            Assert.That(decodedRgb.ChannelCount, Is.EqualTo(3));
            Assert.That(decodedRgb.Orientation, Is.EqualTo(RawOrientation.MirrorHorizontal));
            Assert.That(decodedRgb.OrientationApplied, Is.False);
            Assert.That(decodedRgb.OwnsSamples, Is.False);
            Assert.That(decodedGray.ByteSamples, Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(decodedGray.ChannelCount, Is.EqualTo(1));
            Assert.Throws<RawUnsupportedException>(() => opaque.Decode(new RawLimits(), CancellationToken.None));
        });
    }

    [Test]
    public void BaselineJpegThumbnailDecodesToPublicImageAndChecksDeclaredDimensions()
    {
        byte[] jpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAYEBQYFBAYGBQYHBwYIChAKCgkJChQODwwQFxQYGBcUFhYaHSUfGhsjHBYWICwgIyYnKSopGR8tMC0oMCUoKSj/wAALCAAIAAgBAREA/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/9oACAEBAAA/AE/4Z6/6c/8Ax2v/2Q==");
        RawThumbnail thumbnail = new RawThumbnail(0, RawThumbnailKind.Jpeg, 8, 8, RawOrientation.Normal, "image/jpeg", jpeg, null, null);

        RawRgbImage decoded = thumbnail.Decode(new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(decoded.Width, Is.EqualTo(8));
            Assert.That(decoded.Height, Is.EqualTo(8));
            Assert.That(decoded.ChannelCount, Is.EqualTo(1));
            Assert.That(decoded.ByteSamples.Length, Is.EqualTo(64));
            Assert.That(decoded.Channels.Single().Kind, Is.EqualTo(RawChannelKind.Gray));
            Assert.That(decoded.OwnsSamples, Is.True);
        });

        thumbnail.Width = 7;
        Assert.Throws<RawFormatException>(() => thumbnail.Decode(new RawLimits(), CancellationToken.None));
    }

    [Test]
    public void ColorJpegThumbnailConvertsNativeYcbcrPlanesToRgb()
    {
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = 2;
        image.Height = 1;
        image.Precision = 8;
        image.Planes.Add(CreateJpegPlane(1, new byte[] { 128, 128 }));
        image.Planes.Add(CreateJpegPlane(2, new byte[] { 128, 128 }));
        image.Planes.Add(CreateJpegPlane(3, new byte[] { 128, 128 }));
        byte[] jpeg = BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(100, 1, 1, 0), new RawLimits(), CancellationToken.None);
        RawThumbnail thumbnail = new RawThumbnail(0, RawThumbnailKind.Jpeg, 2, 1, RawOrientation.Normal, "image/jpeg", jpeg, null, null);

        RawRgbImage decoded = thumbnail.Decode(new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(decoded.ChannelCount, Is.EqualTo(3));
            Assert.That(decoded.Channels.Select(channel => channel.Kind), Is.EqualTo(new RawChannelKind[] { RawChannelKind.Red, RawChannelKind.Green, RawChannelKind.Blue }));
            Assert.That(decoded.ByteSamples, Is.All.InRange((byte)126, (byte)130));
        });
    }

    [Test]
    public void ModelValidationRejectsMismatchedCollectionsEnumsAndLimits()
    {
        RawImageInfo info = CreateRgbInfo(2, 2, 8, RawSampleKind.UnsignedInteger);
        info.Planes = Array.Empty<RawPlaneInfo>();
        RawLimits limits = new RawLimits();
        limits.MaxWidth = 1;
        RawDiagnostic[] invalidDiagnostics = new RawDiagnostic[1];
        ((System.Array)invalidDiagnostics).SetValue(null, 0);
        RawThumbnail thumbnail = new RawThumbnail(-1, (RawThumbnailKind)99, 2, 2, (RawOrientation)99, null, null, null, invalidDiagnostics);
        RawColorProfile invalidProfile = new RawColorProfile(string.Empty, RawColorIlluminant.Custom, 0, null, null, null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(((info.Validate(limits).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "ImageInfo.Planes")), Is.True);
            Assert.That(((info.Validate(limits).Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "ImageInfo.Geometry")), Is.True);
            Assert.That(thumbnail.Validate(new RawLimits()).IsValid(), Is.False);
            Assert.Throws<RawFormatException>(() => thumbnail.Decode(new RawLimits(), CancellationToken.None));
            Assert.That(((invalidProfile.Validate().Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Any(message => message.Context == "ColorProfile.Data")), Is.True);
        });
    }

    private static RawImageInfo CreateCfaInfo(int width, int height, int bitDepth, RawPayloadBlock? payload)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, width, height, bitDepth, width) };
        return new RawImageInfo(
            0,
            RawImageKind.Raw,
            width,
            height,
            bitDepth,
            RawSampleKind.UnsignedInteger,
            RawPhotometricInterpretation.ColorFilterArray,
            RawCompression.None,
            RawOrientation.Normal,
            channels,
            planes,
            RawCfaPattern.CreateBayer(RawCfaLayout.Rggb),
            new RawArea(RawAreaKind.StoredImage, 0, 0, width, height),
            new RawArea(RawAreaKind.Active, 0, 0, width, height),
            new RawArea(RawAreaKind.DefaultCrop, 0, 0, width, height),
            payload);
    }

    private static RawImageInfo CreateRgbInfo(int width, int height, int bitDepth, RawSampleKind sampleKind)
    {
        RawChannelDescription[] channels = RgbChannels();
        RawPlaneInfo[] planes = new RawPlaneInfo[]
        {
            new RawPlaneInfo(0, "R", RawChannelKind.Red, width, height, bitDepth, width),
            new RawPlaneInfo(1, "G", RawChannelKind.Green, width, height, bitDepth, width),
            new RawPlaneInfo(2, "B", RawChannelKind.Blue, width, height, bitDepth, width)
        };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, sampleKind, RawPhotometricInterpretation.Rgb, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }

    private static RawImageInfo CreateGrayInfo(int width, int height, int bitDepth, RawSampleKind sampleKind)
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "Gray", RawChannelKind.Gray, width, height, bitDepth, width) };
        return new RawImageInfo(0, RawImageKind.Raw, width, height, bitDepth, sampleKind, RawPhotometricInterpretation.BlackIsZero, RawCompression.None, RawOrientation.Normal, channels, planes, null, new RawArea(RawAreaKind.StoredImage, 0, 0, width, height), null, null, null);
    }

    private static RawColorProfile CreateColorProfile()
    {
        return new RawColorProfile(
            "Camera D65",
            RawColorIlluminant.D65,
            6504,
            new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 },
            null,
            null,
            null,
            new double[] { 0.95047, 1.0, 1.08883 },
            null);
    }

    private static BaselineJpegPlane CreateJpegPlane(byte identifier, byte[] samples)
    {
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = identifier;
        plane.Width = 2;
        plane.Height = 1;
        plane.HorizontalSampling = 1;
        plane.VerticalSampling = 1;
        plane.Samples = samples;
        return plane;
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
