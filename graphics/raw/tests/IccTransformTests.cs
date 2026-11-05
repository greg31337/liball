using raw;

namespace raw.Tests;

public class IccTransformTests
{
    [Test]
    public void MatrixTrcInputOutputAndProfileToProfileTransformsRoundTrip()
    {
        RawIccToneCurve gamma = new RawIccToneCurve(RawIccCurveKind.Gamma, 0, 2.0, null, null);
        RawIccProfile profile = Profile(RawMatrixMath.Identity(), gamma, gamma, gamma, Array.Empty<RawIccTag>(), D50(), D50());
        RawRgbImage input = IccImage(new float[] { 0.75F, 0.25F, 0.5F, 0.4F }, true);
        RawIccTransformOptions options = new RawIccTransformOptions();

        RawRgbImage xyz = RawRenderer.ConvertIccToXyz(input, profile, options);
        RawRgbImage output = RawRenderer.ConvertXyzToIcc(xyz, profile, options);
        RawRgbImage direct = RawRenderer.ConvertIcc(input, profile, profile, options);

        Assert.Multiple(() =>
        {
            Assert.That(xyz.FloatingSamples, Is.EqualTo(new float[] { 0.25F, 0.0625F, 0.5625F, 0.4F }).Within(0.000001F));
            Assert.That(xyz.ColorSpace, Is.EqualTo(RawColorSpace.XyzD50));
            Assert.That(xyz.TransferFunction, Is.EqualTo(RawTransferFunction.Linear));
            Assert.That(output.FloatingSamples, Is.EqualTo(new float[] { 0.5F, 0.25F, 0.75F, 0.4F }).Within(0.000001F));
            Assert.That(direct.FloatingSamples, Is.EqualTo(output.FloatingSamples).Within(0.000001F));
            Assert.That(output.ColorSpace, Is.EqualTo(RawColorSpace.IccRgb));
            Assert.That(output.TransferFunction, Is.EqualTo(RawTransferFunction.Icc));
            Assert.That(output.Channels[3].Kind, Is.EqualTo(RawChannelKind.Alpha));
            Assert.That(output.Validate(new RawLimits()).IsValid(), Is.True);
        });
    }

    [Test]
    public void IdentityGammaSampledAndParametricCurvesEvaluateAndInvert()
    {
        RawIccToneCurve identity = new RawIccToneCurve(RawIccCurveKind.Identity, 0, 1.0, null, null);
        RawIccToneCurve gamma = new RawIccToneCurve(RawIccCurveKind.Gamma, 0, 2.0, null, null);
        RawIccToneCurve sampled = new RawIccToneCurve(RawIccCurveKind.Sampled, 0, 1.0, new ushort[] { 0, 16384, 65535 }, null);
        RawIccToneCurve srgb = new RawIccToneCurve(RawIccCurveKind.Parametric, 4, 2.4, null, new double[] { 2.4, 1.0 / 1.055, 0.055 / 1.055, 1.0 / 12.92, 0.04045, 0.0, 0.0 });

        RawIccTransformProcessor.RequireCurve(identity, true);
        RawIccTransformProcessor.RequireCurve(gamma, true);
        RawIccTransformProcessor.RequireCurve(sampled, true);
        RawIccTransformProcessor.RequireCurve(srgb, true);

        Assert.Multiple(() =>
        {
            Assert.That(RawIccTransformProcessor.EvaluateCurve(identity, 0.3), Is.EqualTo(0.3).Within(0.00000001));
            Assert.That(RawIccTransformProcessor.EvaluateCurve(gamma, 0.5), Is.EqualTo(0.25).Within(0.00000001));
            Assert.That(RawIccTransformProcessor.InvertCurve(gamma, 0.25), Is.EqualTo(0.5).Within(0.00000001));
            Assert.That(RawIccTransformProcessor.EvaluateCurve(sampled, 0.25), Is.EqualTo(0.1250019).Within(0.00001));
            Assert.That(RawIccTransformProcessor.EvaluateCurve(srgb, 0.5), Is.EqualTo(0.21404114).Within(0.0000001));
            Assert.That(RawIccTransformProcessor.InvertCurve(srgb, 0.21404114), Is.EqualTo(0.5).Within(0.000001));
        });
    }

    [Test]
    public void Lut16UsesIntentTagsTetrahedralInterpolationAndFallback()
    {
        ushort[] tetrahedral = new ushort[24];
        tetrahedral[21] = ushort.MaxValue;
        tetrahedral[22] = ushort.MaxValue;
        tetrahedral[23] = ushort.MaxValue;
        ushort[] relative = Constant16(8192, 8192, 8192);
        RawIccTag perceptualTag = new RawIccTag("A2B0", 0, 0, "mft2", Lut16(tetrahedral));
        RawIccTag relativeTag = new RawIccTag("A2B1", 0, 0, "mft2", Lut16(relative));
        RawIccProfile profile = Profile(Array.Empty<double>(), null, null, null, new RawIccTag[] { perceptualTag, relativeTag }, D50(), D50());
        RawRgbImage input = IccImage(new float[] { 0.75F, 0.5F, 0.25F }, false);
        RawIccTransformOptions perceptual = Options(RawIccRenderingIntent.Perceptual);
        RawIccTransformOptions mediaRelative = Options(RawIccRenderingIntent.MediaRelativeColorimetric);
        RawIccTransformOptions saturation = Options(RawIccRenderingIntent.Saturation);
        RawIccTransformOptions absolute = Options(RawIccRenderingIntent.IccAbsoluteColorimetric);

        RawRgbImage perceptualResult = RawRenderer.ConvertIccToXyz(input, profile, perceptual);
        RawRgbImage relativeResult = RawRenderer.ConvertIccToXyz(input, profile, mediaRelative);
        RawRgbImage saturationResult = RawRenderer.ConvertIccToXyz(input, profile, saturation);
        RawRgbImage absoluteResult = RawRenderer.ConvertIccToXyz(input, profile, absolute);

        Assert.Multiple(() =>
        {
            Assert.That(perceptualResult.FloatingSamples, Is.EqualTo(new float[] { 0.499992F, 0.499992F, 0.499992F }).Within(0.00001F));
            Assert.That(relativeResult.FloatingSamples, Is.EqualTo(new float[] { 0.25F, 0.25F, 0.25F }).Within(0.00001F));
            Assert.That(saturationResult.FloatingSamples, Is.EqualTo(perceptualResult.FloatingSamples).Within(0.00001F));
            Assert.That(absoluteResult.FloatingSamples, Is.EqualTo(relativeResult.FloatingSamples).Within(0.00001F));
        });
    }

    [Test]
    public void Lut16AndLut8OutputProfilesProduceEncodedRgb()
    {
        RawIccTag lut16Tag = new RawIccTag("B2A1", 0, 0, "mft2", Lut16(Constant16(13107, 26214, 39321)));
        RawIccTag lut8Tag = new RawIccTag("B2A0", 0, 0, "mft1", Lut8(64, 128, 192));
        RawIccTag multiProcessTag = new RawIccTag("B2A1", 0, 0, "mBA ", MultiProcess("mBA ", Constant16(13107, 26214, 39321), false));
        RawIccProfile lut16Profile = Profile(Array.Empty<double>(), null, null, null, new RawIccTag[] { lut16Tag }, D50(), D50());
        RawIccProfile lut8Profile = Profile(Array.Empty<double>(), null, null, null, new RawIccTag[] { lut8Tag }, D50(), D50());
        RawIccProfile multiProcessProfile = Profile(Array.Empty<double>(), null, null, null, new RawIccTag[] { multiProcessTag }, D50(), D50());
        RawRgbImage xyz = XyzImage(new float[] { 0.2F, 0.3F, 0.4F, 0.65F });

        RawRgbImage sixteen = RawRenderer.ConvertXyzToIcc(xyz, lut16Profile, Options(RawIccRenderingIntent.MediaRelativeColorimetric));
        RawRgbImage eight = RawRenderer.ConvertXyzToIcc(xyz, lut8Profile, Options(RawIccRenderingIntent.Perceptual));
        RawRgbImage multiProcess = RawRenderer.ConvertXyzToIcc(xyz, multiProcessProfile, Options(RawIccRenderingIntent.MediaRelativeColorimetric));

        Assert.Multiple(() =>
        {
            Assert.That(sixteen.FloatingSamples, Is.EqualTo(new float[] { 0.2F, 0.4F, 0.6F, 0.65F }).Within(0.00001F));
            Assert.That(eight.FloatingSamples, Is.EqualTo(new float[] { 64F / 255F, 128F / 255F, 192F / 255F, 0.65F }).Within(0.00001F));
            Assert.That(multiProcess.FloatingSamples, Is.EqualTo(sixteen.FloatingSamples).Within(0.00001F));
            Assert.That(sixteen.Orientation, Is.EqualTo(RawOrientation.MirrorHorizontal));
            Assert.That(sixteen.OrientationApplied, Is.True);
        });
    }

    [Test]
    public void V4AToBPipelineAppliesCurvesClutMatrixAndFinalCurves()
    {
        ushort[] tetrahedral = new ushort[24];
        tetrahedral[21] = ushort.MaxValue;
        tetrahedral[22] = ushort.MaxValue;
        tetrahedral[23] = ushort.MaxValue;
        RawIccTag tag = new RawIccTag("A2B0", 0, 0, "mAB ", MultiProcess("mAB ", tetrahedral, true));
        RawIccProfile profile = Profile(Array.Empty<double>(), null, null, null, new RawIccTag[] { tag }, D50(), D50());

        RawRgbImage converted = RawRenderer.ConvertIccToXyz(IccImage(new float[] { 0.75F, 0.5F, 0.25F }, false), profile, Options(RawIccRenderingIntent.Perceptual));

        Assert.That(converted.FloatingSamples, Is.EqualTo(new float[] { 0.225003F, 0.124998F, 0.124998F }).Within(0.00001F));
    }

    [Test]
    public void AbsoluteColorimetricIntentUsesMediaAndPcsWhitePoints()
    {
        RawIccToneCurve identity = new RawIccToneCurve(RawIccCurveKind.Identity, 0, 1.0, null, null);
        RawIccProfile profile = Profile(RawMatrixMath.Identity(), identity, identity, identity, Array.Empty<RawIccTag>(), new double[] { 0.5, 0.75, 0.25 }, new double[] { 1.0, 1.0, 1.0 });
        RawIccTransformOptions options = Options(RawIccRenderingIntent.IccAbsoluteColorimetric);
        RawRgbImage input = IccImage(new float[] { 0.2F, 0.4F, 0.8F }, false);

        RawRgbImage absolute = RawRenderer.ConvertIccToXyz(input, profile, options);
        RawRgbImage restored = RawRenderer.ConvertXyzToIcc(absolute, profile, options);

        Assert.Multiple(() =>
        {
            Assert.That(absolute.FloatingSamples, Is.EqualTo(new float[] { 0.1F, 0.3F, 0.2F }).Within(0.000001F));
            Assert.That(restored.FloatingSamples, Is.EqualTo(new float[] { 0.2F, 0.4F, 0.8F }).Within(0.000001F));
        });
    }

    [Test]
    public void InvalidProfilesImagesLutsOptionsAndCancellationAreRejected()
    {
        RawIccToneCurve identity = new RawIccToneCurve(RawIccCurveKind.Identity, 0, 1.0, null, null);
        RawIccProfile matrix = Profile(RawMatrixMath.Identity(), identity, identity, identity, Array.Empty<RawIccTag>(), D50(), D50());
        RawIccProfile wrongPcs = Profile(RawMatrixMath.Identity(), identity, identity, identity, Array.Empty<RawIccTag>(), D50(), D50());
        (wrongPcs.Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).ProfileConnectionSpace = "Lab ";
        RawIccTag unsupportedTag = new RawIccTag("A2B0", 0, 0, "mpet", new byte[32]);
        RawIccProfile unsupported = Profile(Array.Empty<double>(), null, null, null, new RawIccTag[] { unsupportedTag }, D50(), D50());
        byte[] truncatedBytes = Lut16(Constant16(0, 0, 0));
        Array.Resize(ref truncatedBytes, truncatedBytes.Length - 2);
        RawIccTag truncatedTag = new RawIccTag("A2B0", 0, 0, "mft2", truncatedBytes);
        RawIccProfile truncated = Profile(Array.Empty<double>(), null, null, null, new RawIccTag[] { truncatedTag }, D50(), D50());
        RawRgbImage input = IccImage(new float[] { 0.2F, 0.3F, 0.4F }, false);
        RawRgbImage wrongImage = XyzImage(new float[] { 0.2F, 0.3F, 0.4F });
        RawRgbImage nonFinite = IccImage(new float[] { float.NaN, 0.3F, 0.4F }, false);
        RawIccTransformOptions invalid = Options((RawIccRenderingIntent)99);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        RawIccTransformOptions cancelled = new RawIccTransformOptions();
        cancelled.CancellationToken = cancellation.Token;

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Validate().IsValid(), Is.False);
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertIccToXyz(input, wrongPcs, new RawIccTransformOptions()));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertIccToXyz(input, unsupported, Options(RawIccRenderingIntent.Perceptual)));
            Assert.Throws<RawFormatException>(() => RawRenderer.ConvertIccToXyz(input, truncated, Options(RawIccRenderingIntent.Perceptual)));
            Assert.Throws<RawUnsupportedException>(() => RawRenderer.ConvertIccToXyz(wrongImage, matrix, new RawIccTransformOptions()));
            Assert.Throws<RawFormatException>(() => RawRenderer.ConvertIccToXyz(nonFinite, matrix, new RawIccTransformOptions()));
            Assert.Throws<RawFormatException>(() => RawRenderer.ConvertIccToXyz(input, matrix, invalid));
            Assert.Throws<OperationCanceledException>(() => RawRenderer.ConvertIccToXyz(input, matrix, cancelled));
        });
    }

    private static RawIccTransformOptions Options(RawIccRenderingIntent intent)
    {
        RawIccTransformOptions options = new RawIccTransformOptions();
        options.RenderingIntent = intent;
        return options;
    }

    private static RawIccProfile Profile(double[] matrix, RawIccToneCurve? red, RawIccToneCurve? green, RawIccToneCurve? blue, RawIccTag[] tags, double[] whitePoint, double[] pcsWhite)
    {
        RawIccHeader header = new RawIccHeader(132, "TEST", 4, 4, 0, "mntr", "RGB ", "XYZ ", 2026, 8, 20, 12, 0, 0, true, "APPL", 0, "TEST", "RGB ", 0, 1, pcsWhite, "TEST");
        return new RawIccProfile(new byte[132], header, tags, "Synthetic", "Synthetic", new byte[16], new byte[16], string.Empty, string.Empty, false, false, true, matrix, whitePoint, red, green, blue, -1, string.Empty);
    }

    private static RawRgbImage IccImage(float[] samples, bool bgr)
    {
        int channelCount = samples.Length == 4 ? 4 : 3;
        RawChannelDescription[] channels;
        if (bgr)
        {
            channels = new RawChannelDescription[]
            {
                new RawChannelDescription("B", RawChannelKind.Blue),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            };
        }
        else
        {
            channels = channelCount == 4 ? new RawChannelDescription[]
            {
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("B", RawChannelKind.Blue),
                new RawChannelDescription("A", RawChannelKind.Alpha)
            } : new RawChannelDescription[]
            {
                new RawChannelDescription("R", RawChannelKind.Red),
                new RawChannelDescription("G", RawChannelKind.Green),
                new RawChannelDescription("B", RawChannelKind.Blue)
            };
        }
        return new RawRgbImage(1, 1, channelCount, channelCount, null, null, samples, channels, RawColorSpace.IccRgb, RawTransferFunction.Icc, RawOrientation.Normal, false, true, null);
    }

    private static RawRgbImage XyzImage(float[] samples)
    {
        int channelCount = samples.Length == 4 ? 4 : 3;
        RawChannelDescription[] channels = channelCount == 4 ? new RawChannelDescription[]
        {
            new RawChannelDescription("X", RawChannelKind.Custom),
            new RawChannelDescription("Y", RawChannelKind.Custom),
            new RawChannelDescription("Z", RawChannelKind.Custom),
            new RawChannelDescription("A", RawChannelKind.Alpha)
        } : new RawChannelDescription[]
        {
            new RawChannelDescription("X", RawChannelKind.Custom),
            new RawChannelDescription("Y", RawChannelKind.Custom),
            new RawChannelDescription("Z", RawChannelKind.Custom)
        };
        return new RawRgbImage(1, 1, channelCount, channelCount, null, null, samples, channels, RawColorSpace.XyzD50, RawTransferFunction.Linear, RawOrientation.MirrorHorizontal, true, true, null);
    }

    private static ushort[] Constant16(ushort red, ushort green, ushort blue)
    {
        ushort[] values = new ushort[24];
        int index = 0;
        while (index < 8)
        {
            values[index * 3] = red;
            values[index * 3 + 1] = green;
            values[index * 3 + 2] = blue;
            index++;
        }
        return values;
    }

    private static byte[] Lut16(ushort[] clut)
    {
        byte[] bytes = new byte[124];
        WriteSignature(bytes, 0, "mft2");
        bytes[8] = 3;
        bytes[9] = 3;
        bytes[10] = 2;
        WriteFixed(bytes, 12, 1.0);
        WriteFixed(bytes, 28, 1.0);
        WriteFixed(bytes, 44, 1.0);
        WriteUInt16(bytes, 48, 2);
        WriteUInt16(bytes, 50, 2);
        int position = 52;
        int channel = 0;
        while (channel < 3)
        {
            WriteUInt16(bytes, position, 0);
            WriteUInt16(bytes, position + 2, ushort.MaxValue);
            position += 4;
            channel++;
        }
        int index = 0;
        while (index < clut.Length)
        {
            WriteUInt16(bytes, position, clut[index]);
            position += 2;
            index++;
        }
        channel = 0;
        while (channel < 3)
        {
            WriteUInt16(bytes, position, 0);
            WriteUInt16(bytes, position + 2, ushort.MaxValue);
            position += 4;
            channel++;
        }
        return bytes;
    }

    private static byte[] Lut8(byte red, byte green, byte blue)
    {
        byte[] bytes = new byte[1608];
        WriteSignature(bytes, 0, "mft1");
        bytes[8] = 3;
        bytes[9] = 3;
        bytes[10] = 2;
        WriteFixed(bytes, 12, 1.0);
        WriteFixed(bytes, 28, 1.0);
        WriteFixed(bytes, 44, 1.0);
        int position = 48;
        int channel = 0;
        while (channel < 3)
        {
            int entry = 0;
            while (entry < 256)
            {
                bytes[position++] = (byte)entry;
                entry++;
            }
            channel++;
        }
        int vertex = 0;
        while (vertex < 8)
        {
            bytes[position++] = red;
            bytes[position++] = green;
            bytes[position++] = blue;
            vertex++;
        }
        channel = 0;
        while (channel < 3)
        {
            int entry = 0;
            while (entry < 256)
            {
                bytes[position++] = (byte)entry;
                entry++;
            }
            channel++;
        }
        return bytes;
    }

    private static byte[] MultiProcess(string signature, ushort[] clut, bool complex)
    {
        int clutOffset = complex ? 116 : 32;
        int length = complex ? 232 : 100;
        byte[] bytes = new byte[length];
        WriteSignature(bytes, 0, signature);
        bytes[8] = 3;
        bytes[9] = 3;
        WriteUInt32(bytes, 24, (uint)clutOffset);
        if (complex)
        {
            WriteUInt32(bytes, 12, 32);
            WriteUInt32(bytes, 16, 68);
            WriteUInt32(bytes, 28, 184);
            WriteIdentityCurve(bytes, 32);
            WriteIdentityCurve(bytes, 44);
            WriteIdentityCurve(bytes, 56);
            WriteFixed(bytes, 68, 1.0);
            WriteFixed(bytes, 84, 1.0);
            WriteFixed(bytes, 100, 1.0);
            WriteFixed(bytes, 104, 0.05);
            WriteGammaCurve(bytes, 184, 2.0);
            WriteGammaCurve(bytes, 200, 2.0);
            WriteGammaCurve(bytes, 216, 2.0);
        }
        bytes[clutOffset] = 2;
        bytes[clutOffset + 1] = 2;
        bytes[clutOffset + 2] = 2;
        bytes[clutOffset + 16] = 2;
        int position = clutOffset + 20;
        int index = 0;
        while (index < clut.Length)
        {
            WriteUInt16(bytes, position, clut[index]);
            position += 2;
            index++;
        }
        return bytes;
    }

    private static void WriteIdentityCurve(byte[] bytes, int offset)
    {
        WriteSignature(bytes, offset, "curv");
        WriteUInt32(bytes, offset + 8, 0);
    }

    private static void WriteGammaCurve(byte[] bytes, int offset, double gamma)
    {
        WriteSignature(bytes, offset, "curv");
        WriteUInt32(bytes, offset + 8, 1);
        WriteUInt16(bytes, offset + 12, (ushort)Math.Round(gamma * 256.0));
    }

    private static double[] D50()
    {
        return new double[] { 0.9642, 1.0, 0.8249 };
    }

    private static void WriteSignature(byte[] bytes, int offset, string signature)
    {
        int index = 0;
        while (index < 4)
        {
            bytes[offset + index] = (byte)signature[index];
            index++;
        }
    }

    private static void WriteUInt16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static void WriteFixed(byte[] bytes, int offset, double value)
    {
        WriteUInt32(bytes, offset, unchecked((uint)(int)Math.Round(value * 65536.0)));
    }
}
