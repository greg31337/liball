using System.Buffers.Binary;

namespace avif.Tests;

public class SampleTransformTests
{
    [Test]
    public void ParserReadsTokensConstantsAndIgnoresReservedHeaderBits()
    {
        byte[] payload = CreatePayload(
            32,
            new int[] { 0, 1, 130, 2, 128 },
            new long[] { 256, 0, 0, 0, 0 });
        payload[0] |= 60;

        AvifSampleTransform transform = AvifSampleTransform.Create(payload, 2);

        Assert.Multiple(() =>
        {
            Assert.That(transform.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(transform.GetInputCount(), Is.EqualTo(2));
            Assert.That(transform.GetIntermediateBitDepth(), Is.EqualTo(32));
            Assert.That(transform.GetTokenCount(), Is.EqualTo(5));
            Assert.That((transform.GetToken(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsConstant(), Is.True);
            Assert.That((transform.GetToken(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetEncodedValue(), Is.Zero);
            Assert.That((transform.GetToken(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetConstant(), Is.EqualTo(256));
            Assert.That((transform.GetToken(1) ?? throw new InvalidOperationException("Invalid internal state.")).IsInputSample(), Is.True);
            Assert.That((transform.GetToken(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetInputIndex(), Is.Zero);
            Assert.That((transform.GetToken(2) ?? throw new InvalidOperationException("Invalid internal state.")).IsOperator(), Is.True);
            Assert.That((transform.GetToken(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetEncodedValue(), Is.EqualTo(130));
            Assert.That((transform.GetToken(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetOperator(),
                Is.EqualTo(AvifSampleTransformOperator.Multiply));
            Assert.That((transform.GetToken(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetOperator(),
                Is.EqualTo(AvifSampleTransformOperator.Add));
        });
    }

    [Test]
    public void ParserRejectsUnsupportedAndMalformedExpressions()
    {
        byte[] version = CreatePayload(
            16,
            new int[] { 1 },
            new long[] { 0 });
        version[0] |= 64;
        byte[] extra = CreatePayload(
            16,
            new int[] { 1 },
            new long[] { 0 });
        Array.Resize(ref extra, extra.Length + 1);

        Assert.Multiple(() =>
        {
            Assert.That(AvifSampleTransform.Create(version, 1).GetCode(),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(AvifSampleTransform.Create(
                new byte[] { 1, 1, 33 }, 1).GetCode(),
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(AvifSampleTransform.Create(
                new byte[] { 1, 0 }, 1).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(AvifSampleTransform.Create(
                new byte[] { 1, 1, 2 }, 1).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(AvifSampleTransform.Create(
                new byte[] { 1, 1, 128 }, 1).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(AvifSampleTransform.Create(
                new byte[] { 1, 2, 1, 1 }, 1).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(AvifSampleTransform.Create(
                new byte[] { 2, 1, 0, 0 }, 1).GetCode(),
                Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(AvifSampleTransform.Create(extra, 1).GetCode(),
                Is.EqualTo(AvifResultCode.InvalidItem));
        });
    }

    [TestCase(8, -128L, 127L)]
    [TestCase(16, -32768L, 32767L)]
    [TestCase(32, -2147483648L, 2147483647L)]
    [TestCase(64, long.MinValue, long.MaxValue)]
    public void ArithmeticSaturatesAtEveryIntermediateWidth(
        int bitDepth,
        long minimum,
        long maximum)
    {
        long overflow = Evaluate(
            bitDepth,
            new int[] { 0, 0, 128 },
            new long[] { maximum, 1, 0 });
        long underflow = Evaluate(
            bitDepth,
            new int[] { 0, 0, 129 },
            new long[] { minimum, 1, 0 });
        long negatedMinimum = Evaluate(
            bitDepth,
            new int[] { 0, 64 },
            new long[] { minimum, 0 });
        long absoluteMinimum = Evaluate(
            bitDepth,
            new int[] { 0, 65 },
            new long[] { minimum, 0 });

        Assert.Multiple(() =>
        {
            Assert.That(overflow, Is.EqualTo(maximum));
            Assert.That(underflow, Is.EqualTo(minimum));
            Assert.That(negatedMinimum, Is.EqualTo(maximum));
            Assert.That(absoluteMinimum, Is.EqualTo(maximum));
        });
    }

    [Test]
    public void EveryDefinedOperatorUsesNormativePostfixSemantics()
    {
        Assert.Multiple(() =>
        {
            Assert.That(EvaluateUnary(16, 66, 85), Is.EqualTo(-86));
            Assert.That(EvaluateUnary(16, 67, 64), Is.EqualTo(6));
            Assert.That(EvaluateBinary(16, 128, 12, 5), Is.EqualTo(17));
            Assert.That(EvaluateBinary(16, 129, 12, 5), Is.EqualTo(7));
            Assert.That(EvaluateBinary(16, 130, 12, 5), Is.EqualTo(60));
            Assert.That(EvaluateBinary(16, 131, -12, 5), Is.EqualTo(-2));
            Assert.That(EvaluateBinary(16, 131, 12, 0), Is.EqualTo(12));
            Assert.That(EvaluateBinary(16, 132, 6, 3), Is.EqualTo(2));
            Assert.That(EvaluateBinary(16, 133, 6, 3), Is.EqualTo(7));
            Assert.That(EvaluateBinary(16, 134, 6, 3), Is.EqualTo(5));
            Assert.That(EvaluateBinary(16, 135, 3, 4), Is.EqualTo(81));
            Assert.That(EvaluateBinary(16, 135, 2, -2), Is.Zero);
            Assert.That(EvaluateBinary(16, 135, 0, 0), Is.Zero);
            Assert.That(EvaluateBinary(16, 136, 12, 5), Is.EqualTo(5));
            Assert.That(EvaluateBinary(16, 137, 12, 5), Is.EqualTo(12));
        });
    }

    [Test]
    public void StandardBitDepthExtensionRecipesProduceExpectedSamples()
    {
        AvifSampleTransform suffix =
            AvifSampleTransform.CreateSuffixBitDepthExtension();
        AvifSampleTransform residual =
            AvifSampleTransform.CreateResidualBitDepthExtension();
        long[] suffixOutput = new long[1];
        long[] residualOutput = new long[1];

        AvifResultCode suffixCode = suffix.Evaluate(
            new long[] { 18, 52 }, suffixOutput);
        AvifResultCode residualCode = residual.Evaluate(
            new long[] { 4090, 100 }, residualOutput);

        Assert.Multiple(() =>
        {
            Assert.That(suffixCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(suffixOutput[0], Is.EqualTo(4660));
            Assert.That(suffix.GetIntermediateBitDepth(), Is.EqualTo(32));
            Assert.That(suffix.GetTokenCount(), Is.EqualTo(5));
            Assert.That(residualCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(residualOutput[0], Is.EqualTo(65412));
            Assert.That(residual.GetTokenCount(), Is.EqualTo(7));
        });
    }

    [Test]
    public void SuffixRecipeReconstructsSixteenBitPlanarImages()
    {
        AvifImage high = CreateMonochromeImage(
            8, AvifRange.Full, new ushort[] { 18, 255 });
        AvifImage low = CreateMonochromeImage(
            8, AvifRange.Full, new ushort[] { 52, 255 });
        AvifSampleTransform recipe =
            AvifSampleTransform.CreateSuffixBitDepthExtension();
        AvifSampleTransform transform = new AvifSampleTransform(
            recipe.GetData(),
            new AvifImage[] { high, low },
            16);

        AvifImage output = transform.Render(
            new AvifImage[] { high, low }, 16);

        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(transform.GetInputImage(0), Is.SameAs(high));
            Assert.That(transform.GetInputImage(1), Is.SameAs(low));
            Assert.That(transform.GetInputImage(2), Is.Null);
            Assert.That(output.GetBitDepth(), Is.EqualTo(16));
            Assert.That(ReadSample(output, 0), Is.EqualTo(4660));
            Assert.That(ReadSample(output, 1), Is.EqualTo(65535));
        });

        output.Close();
        high.Close();
        low.Close();
    }

    [Test]
    public void ResidualRecipeAcceptsMixedInputDepthsAndRejectsLayoutMismatch()
    {
        AvifImage baseImage = CreateMonochromeImage(
            12, AvifRange.Full, new ushort[] { 4090 });
        AvifImage residual = CreateMonochromeImage(
            8, AvifRange.Full, new ushort[] { 100 });
        AvifImage wrongSize = AvifImage.Create(
            2, 1, 8, AvifChromaFormat.Monochrome, false);
        AvifSampleTransform transform =
            AvifSampleTransform.CreateResidualBitDepthExtension();

        AvifImage output = transform.Render(
            new AvifImage[] { baseImage, residual }, 16);
        AvifImage mismatched = transform.Render(
            new AvifImage[] { baseImage, wrongSize }, 16);

        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(output, 0), Is.EqualTo(65412));
            Assert.That(mismatched.GetCode(),
                Is.EqualTo(AvifResultCode.NonConformingData));
        });

        output.Close();
        mismatched.Close();
        baseImage.Close();
        residual.Close();
        wrongSize.Close();
    }

    [Test]
    public void ExplicitRenderLimitsRejectOutputWithoutChangingInputs()
    {
        AvifImage high = CreateMonochromeImage(
            8, AvifRange.Full, new ushort[] { 18, 19 });
        AvifImage low = CreateMonochromeImage(
            8, AvifRange.Full, new ushort[] { 52, 53 });
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxWidth = 1;
        AvifSampleTransform transform =
            AvifSampleTransform.CreateSuffixBitDepthExtension();

        AvifImage output = transform.RenderWithLimits(
            new AvifImage[] { high, low }, 16, limits);

        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(),
                Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That((high.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 18, 19 }));
            Assert.That((low.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).ToArray(),
                Is.EqualTo(new byte[] { 52, 53 }));
        });

        output.Close();
        high.Close();
        low.Close();
    }

    [Test]
    public void RenderingClampsToLimitedOutputRangeAndChecksIntermediateFit()
    {
        AvifSampleTransform zero = AvifSampleTransform.Create(
            CreatePayload(32, new int[] { 0 }, new long[] { 0 }), 1);
        AvifImage limited = CreateMonochromeImage(
            8, AvifRange.Limited, new ushort[] { 16 });
        AvifImage output = zero.Render(new AvifImage[] { limited }, 16);

        AvifSampleTransform narrow = AvifSampleTransform.Create(
            CreatePayload(8, new int[] { 1 }, new long[] { 0 }), 1);
        long[] unchanged = new long[] { 99 };
        AvifResultCode narrowCode = narrow.Evaluate(
            new long[] { 255 }, unchanged);

        Assert.Multiple(() =>
        {
            Assert.That(output.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(output, 0), Is.EqualTo(4096));
            Assert.That(narrowCode,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(unchanged[0], Is.EqualTo(99));
        });

        output.Close();
        limited.Close();
    }

    private static long EvaluateUnary(int bitDepth, int operation, long value)
    {
        return Evaluate(
            bitDepth,
            new int[] { 0, operation },
            new long[] { value, 0 });
    }

    private static long EvaluateBinary(
        int bitDepth,
        int operation,
        long left,
        long right)
    {
        return Evaluate(
            bitDepth,
            new int[] { 0, 0, operation },
            new long[] { left, right, 0 });
    }

    private static long Evaluate(
        int bitDepth,
        int[] tokenValues,
        long[] constants)
    {
        AvifSampleTransform transform = AvifSampleTransform.Create(
            CreatePayload(bitDepth, tokenValues, constants), 1);
        long[] output = new long[1];
        Assert.That(transform.Evaluate(new long[] { 0 }, output),
            Is.EqualTo(AvifResultCode.Success));
        return output[0];
    }

    private static byte[] CreatePayload(
        int bitDepth,
        int[] tokenValues,
        long[] constants)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add((byte)GetDepthCode(bitDepth));
        bytes.Add((byte)tokenValues.Length);
        int index = 0;
        while (index < tokenValues.Length)
        {
            bytes.Add((byte)tokenValues[index]);
            if (tokenValues[index] == 0)
            {
                AppendConstant(bytes, constants[index], bitDepth);
            }
            index++;
        }
        return bytes.ToArray();
    }

    private static void AppendConstant(
        List<byte> bytes,
        long value,
        int bitDepth)
    {
        byte[] encoded = new byte[bitDepth >> 3];
        if (bitDepth == 8)
        {
            encoded[0] = unchecked((byte)(sbyte)value);
        }
        else if (bitDepth == 16)
        {
            BinaryPrimitives.WriteInt16BigEndian(encoded, (short)value);
        }
        else if (bitDepth == 32)
        {
            BinaryPrimitives.WriteInt32BigEndian(encoded, (int)value);
        }
        else
        {
            BinaryPrimitives.WriteInt64BigEndian(encoded, value);
        }
        bytes.AddRange(encoded);
    }

    private static int GetDepthCode(int bitDepth)
    {
        if (bitDepth == 8)
        {
            return 0;
        }
        if (bitDepth == 16)
        {
            return 1;
        }
        if (bitDepth == 32)
        {
            return 2;
        }
        return 3;
    }

    private static AvifImage CreateMonochromeImage(
        int bitDepth,
        AvifRange range,
        ushort[] samples)
    {
        AvifImage image = AvifImage.Create(
            samples.Length,
            1,
            bitDepth,
            AvifChromaFormat.Monochrome,
            range,
            AvifChromaSamplePosition.Unknown,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        Span<byte> row = (image.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0);
        int index = 0;
        while (index < samples.Length)
        {
            if (bitDepth <= 8)
            {
                row[index] = (byte)samples[index];
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(
                    row.Slice(index * 2, 2), samples[index]);
            }
            index++;
        }
        return image;
    }

    private static ushort ReadSample(AvifImage image, int x)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(
            (image.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(0).Slice(x * 2, 2));
    }
}
