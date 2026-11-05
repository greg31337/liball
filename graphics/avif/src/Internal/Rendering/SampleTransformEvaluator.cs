using System;
using System.Buffers.Binary;
using System.Numerics;

namespace avif;

internal class SampleTransformEvaluationResult
{
    internal AvifResultCode Code;
    internal long Value;

    internal SampleTransformEvaluationResult(
        AvifResultCode code,
        long value)
    {
        Code = code;
        Value = value;
    }
}

internal static class SampleTransformEvaluator
{
    internal static SampleTransformEvaluationResult Evaluate(
        AvifSampleTransform transform,
        long[] inputSamples)
    {
        if (transform == null || inputSamples == null)
        {
            return new SampleTransformEvaluationResult(
                AvifResultCode.InvalidArgument,
                0);
        }
        AvifResultCode transformCode = transform.GetCode();
        if (transformCode != AvifResultCode.Success)
        {
            return new SampleTransformEvaluationResult(transformCode, 0);
        }
        if (inputSamples.Length != transform.GetInputCount())
        {
            return new SampleTransformEvaluationResult(
                AvifResultCode.InvalidArgument,
                0);
        }

        int bitDepth = transform.GetIntermediateBitDepth();
        long maximum = GetMaximum(bitDepth);
        int sampleIndex = 0;
        while (sampleIndex < inputSamples.Length)
        {
            if (inputSamples[sampleIndex] < 0
                || inputSamples[sampleIndex] > maximum)
            {
                return new SampleTransformEvaluationResult(
                    AvifResultCode.NonConformingData,
                    0);
            }
            sampleIndex++;
        }

        long[] stack = new long[transform.GetTokenCount()];
        int stackDepth = 0;
        int tokenIndex = 0;
        while (tokenIndex < transform.GetTokenCount())
        {
            AvifSampleTransformToken? token = transform.GetToken(tokenIndex);
            if ((token ?? throw new InvalidOperationException("Invalid internal state.")).IsConstant())
            {
                stack[stackDepth] = token.GetConstant();
                stackDepth++;
            }
            else if (token.IsInputSample())
            {
                stack[stackDepth] = inputSamples[token.GetInputIndex()];
                stackDepth++;
            }
            else if (token.IsUnaryOperator())
            {
                stack[stackDepth - 1] = EvaluateUnary(
                    token.GetOperator(),
                    stack[stackDepth - 1],
                    bitDepth);
            }
            else
            {
                long right = stack[stackDepth - 1];
                long left = stack[stackDepth - 2];
                stackDepth--;
                stack[stackDepth - 1] = EvaluateBinary(
                    token.GetOperator(),
                    left,
                    right,
                    bitDepth);
            }
            tokenIndex++;
        }
        return new SampleTransformEvaluationResult(
            AvifResultCode.Success,
            stack[0]);
    }

    internal static AvifImage Render(
        AvifSampleTransform transform,
        AvifImage[] inputs,
        int outputBitDepth,
        AvifSecurityLimits limits)
    {
        if (transform == null || inputs == null || limits == null)
        {
            return AvifImage.FromCode(AvifResultCode.InvalidArgument);
        }
        AvifResultCode transformCode = transform.GetCode();
        if (transformCode != AvifResultCode.Success)
        {
            return AvifImage.FromCode(transformCode);
        }
        if (inputs.Length != transform.GetInputCount()
            || !IsOutputBitDepth(outputBitDepth))
        {
            return AvifImage.FromCode(AvifResultCode.InvalidArgument);
        }
        AvifResultCode compatibility = ValidateInputs(inputs);
        if (compatibility != AvifResultCode.Success)
        {
            return AvifImage.FromCode(compatibility);
        }

        AvifImage first = inputs[0];
        AvifImage output = AvifImage.Create(
            first.GetWidth(),
            first.GetHeight(),
            outputBitDepth,
            first.GetChromaFormat(),
            first.GetRange(),
            first.GetChromaSamplePosition(),
            first.GetAlphaMode(),
            1,
            limits);
        if (output.GetCode() != AvifResultCode.Success)
        {
            return output;
        }
        AvifResultCode metadataCode = output.SetColorInfo(first.GetColorInfo());
        if (metadataCode == AvifResultCode.Success)
        {
            metadataCode = output.SetIccProfile(first.GetIccProfile());
        }
        if (metadataCode != AvifResultCode.Success)
        {
            output.Close();
            return AvifImage.FromCode(metadataCode);
        }

        long[] samples = new long[inputs.Length];
        int planeIndex = 0;
        while (planeIndex < 4)
        {
            AvifPlaneKind kind = (AvifPlaneKind)planeIndex;
            AvifPlane? destination = output.GetPlane(kind);
            if (destination != null)
            {
                AvifResultCode planeCode = RenderPlane(
                    transform,
                    inputs,
                    samples,
                    destination,
                    kind,
                    outputBitDepth,
                    first.GetRange());
                if (planeCode != AvifResultCode.Success)
                {
                    output.Close();
                    return AvifImage.FromCode(planeCode);
                }
            }
            planeIndex++;
        }
        return output;
    }

    static AvifResultCode ValidateInputs(AvifImage[] inputs)
    {
        if (inputs.Length == 0 || inputs[0] == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifImage first = inputs[0];
        AvifResultCode firstCode = first.GetCode();
        if (firstCode != AvifResultCode.Success)
        {
            return firstCode;
        }
        int index = 1;
        while (index < inputs.Length)
        {
            AvifImage image = inputs[index];
            if (image == null)
            {
                return AvifResultCode.InvalidArgument;
            }
            AvifResultCode imageCode = image.GetCode();
            if (imageCode != AvifResultCode.Success)
            {
                return imageCode;
            }
            if (!AreCompatible(first, image))
            {
                return AvifResultCode.NonConformingData;
            }
            index++;
        }
        return AvifResultCode.Success;
    }

    static bool AreCompatible(AvifImage first, AvifImage second)
    {
        if (first.GetWidth() != second.GetWidth()
            || first.GetHeight() != second.GetHeight()
            || first.GetChromaFormat() != second.GetChromaFormat()
            || first.GetChromaSamplePosition() != second.GetChromaSamplePosition()
            || first.GetRange() != second.GetRange()
            || first.GetAlphaMode() != second.GetAlphaMode())
        {
            return false;
        }
        AvifColorInfo? firstColor = first.GetColorInfo();
        AvifColorInfo? secondColor = second.GetColorInfo();
        if ((firstColor ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries != (secondColor ?? throw new InvalidOperationException("Invalid internal state.")).ColorPrimaries
            || firstColor.TransferCharacteristics
                != secondColor.TransferCharacteristics
            || firstColor.MatrixCoefficients != secondColor.MatrixCoefficients
            || firstColor.Range != secondColor.Range)
        {
            return false;
        }
        return IccMatches(first.GetIccProfile(), second.GetIccProfile());
    }

    static bool IccMatches(AvifIccProfile? first, AvifIccProfile? second)
    {
        if (first == null || second == null)
        {
            return first == null && second == null;
        }
        byte[] firstData = first.GetData();
        byte[] secondData = second.GetData();
        if (firstData.Length != secondData.Length)
        {
            return false;
        }
        int index = 0;
        while (index < firstData.Length)
        {
            if (firstData[index] != secondData[index])
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static AvifResultCode RenderPlane(
        AvifSampleTransform transform,
        AvifImage[] inputs,
        long[] samples,
        AvifPlane destination,
        AvifPlaneKind kind,
        int outputBitDepth,
        AvifRange range)
    {
        int y = 0;
        while (y < destination.GetHeight())
        {
            Span<byte> destinationRow = destination.GetRowSpan(y);
            int x = 0;
            while (x < destination.GetWidth())
            {
                int inputIndex = 0;
                while (inputIndex < inputs.Length)
                {
                    samples[inputIndex] = ReadSample(
                        inputs[inputIndex].GetPlane(kind),
                        x,
                        y,
                        inputs[inputIndex].GetBitDepth());
                    inputIndex++;
                }
                SampleTransformEvaluationResult evaluated = Evaluate(
                    transform,
                    samples);
                if (evaluated.Code != AvifResultCode.Success)
                {
                    return evaluated.Code;
                }
                long clamped = ClampOutput(
                    evaluated.Value,
                    outputBitDepth,
                    range,
                    kind);
                WriteSample(destinationRow, x, outputBitDepth, clamped);
                x++;
            }
            y++;
        }
        return AvifResultCode.Success;
    }

    static long ReadSample(
        AvifPlane? plane,
        int x,
        int y,
        int bitDepth)
    {
        Span<byte> row = (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(y);
        if (bitDepth <= 8)
        {
            return row[x];
        }
        return BinaryPrimitives.ReadUInt16LittleEndian(
            row.Slice(x * 2, 2));
    }

    static void WriteSample(
        Span<byte> row,
        int x,
        int bitDepth,
        long value)
    {
        if (bitDepth <= 8)
        {
            row[x] = (byte)value;
            return;
        }
        BinaryPrimitives.WriteUInt16LittleEndian(
            row.Slice(x * 2, 2),
            (ushort)value);
    }

    static long ClampOutput(
        long value,
        int bitDepth,
        AvifRange range,
        AvifPlaneKind kind)
    {
        long minimum = 0;
        long maximum = (1L << bitDepth) - 1;
        if (range == AvifRange.Limited && kind != AvifPlaneKind.Alpha)
        {
            int shift = bitDepth - 8;
            minimum = 16L << shift;
            maximum = (kind == AvifPlaneKind.Y ? 235L : 240L) << shift;
        }
        if (value < minimum)
        {
            return minimum;
        }
        if (value > maximum)
        {
            return maximum;
        }
        return value;
    }

    static long EvaluateUnary(
        AvifSampleTransformOperator operation,
        long value,
        int bitDepth)
    {
        if (operation == AvifSampleTransformOperator.Negate)
        {
            return Clamp(-(BigInteger)value, bitDepth);
        }
        if (operation == AvifSampleTransformOperator.AbsoluteValue)
        {
            BigInteger absolute = value < 0
                ? -(BigInteger)value
                : (BigInteger)value;
            return Clamp(absolute, bitDepth);
        }
        if (operation == AvifSampleTransformOperator.BitwiseNot)
        {
            return ~value;
        }
        if (value <= 0)
        {
            return 0;
        }
        ulong remaining = (ulong)value;
        long position = -1;
        while (remaining != 0)
        {
            remaining >>= 1;
            position++;
        }
        return position;
    }

    static long EvaluateBinary(
        AvifSampleTransformOperator operation,
        long left,
        long right,
        int bitDepth)
    {
        if (operation == AvifSampleTransformOperator.Add)
        {
            return Clamp((BigInteger)left + right, bitDepth);
        }
        if (operation == AvifSampleTransformOperator.Subtract)
        {
            return Clamp((BigInteger)left - right, bitDepth);
        }
        if (operation == AvifSampleTransformOperator.Multiply)
        {
            return Clamp((BigInteger)left * right, bitDepth);
        }
        if (operation == AvifSampleTransformOperator.Divide)
        {
            if (right == 0)
            {
                return left;
            }
            return Clamp((BigInteger)left / right, bitDepth);
        }
        if (operation == AvifSampleTransformOperator.BitwiseAnd)
        {
            return left & right;
        }
        if (operation == AvifSampleTransformOperator.BitwiseOr)
        {
            return left | right;
        }
        if (operation == AvifSampleTransformOperator.BitwiseXor)
        {
            return left ^ right;
        }
        if (operation == AvifSampleTransformOperator.Power)
        {
            return Power(left, right, bitDepth);
        }
        if (operation == AvifSampleTransformOperator.Minimum)
        {
            return left <= right ? left : right;
        }
        return left >= right ? left : right;
    }

    static long Power(long left, long right, int bitDepth)
    {
        if (left == 0)
        {
            return 0;
        }
        if (right < 0)
        {
            if (left == 1)
            {
                return 1;
            }
            if (left == -1)
            {
                return (right & 1) == 0 ? 1 : -1;
            }
            return 0;
        }

        ulong exponent = (ulong)right;
        BigInteger boundary = (BigInteger.One << (bitDepth - 1)) + 1;
        BigInteger result = BigInteger.One;
        BigInteger factor = left;
        while (exponent != 0)
        {
            if ((exponent & 1) != 0)
            {
                result *= factor;
                if (BigInteger.Abs(result) > boundary)
                {
                    return Clamp(result, bitDepth);
                }
            }
            exponent >>= 1;
            if (exponent != 0)
            {
                factor *= factor;
                if (BigInteger.Abs(factor) > boundary)
                {
                    factor = boundary;
                }
            }
        }
        return Clamp(result, bitDepth);
    }

    static long Clamp(BigInteger value, int bitDepth)
    {
        BigInteger minimum = -(BigInteger.One << (bitDepth - 1));
        BigInteger maximum = (BigInteger.One << (bitDepth - 1)) - 1;
        if (value < minimum)
        {
            return bitDepth == 64 ? long.MinValue : -(1L << (bitDepth - 1));
        }
        if (value > maximum)
        {
            return GetMaximum(bitDepth);
        }
        return (long)value;
    }

    static long GetMaximum(int bitDepth)
    {
        return bitDepth == 64
            ? long.MaxValue
            : (1L << (bitDepth - 1)) - 1;
    }

    static bool IsOutputBitDepth(int value)
    {
        return value == 8 || value == 10 || value == 12 || value == 16;
    }
}
