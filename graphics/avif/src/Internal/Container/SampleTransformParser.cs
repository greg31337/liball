using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif;

internal class SampleTransformParseResult
{
    internal AvifResultCode Code;
    internal int IntermediateBitDepth;
    internal List<AvifSampleTransformToken> Tokens;

    internal SampleTransformParseResult(AvifResultCode code)
    {
        Code = code;
        Tokens = new List<AvifSampleTransformToken>();
    }
}

internal static class SampleTransformParser
{
    internal static SampleTransformParseResult Parse(
        byte[] payload,
        int inputCount)
    {
        if (payload == null || inputCount < 1 || inputCount > 32)
        {
            return new SampleTransformParseResult(
                AvifResultCode.InvalidArgument);
        }
        if (payload.Length < 2)
        {
            return new SampleTransformParseResult(
                AvifResultCode.TruncatedData);
        }

        int version = payload[0] >> 6;
        if (version != 0)
        {
            return new SampleTransformParseResult(
                AvifResultCode.UnsupportedFeature);
        }
        int depthCode = payload[0] & 3;
        int intermediateBitDepth = 8 << depthCode;
        int tokenCount = payload[1];
        if (tokenCount == 0)
        {
            return new SampleTransformParseResult(
                AvifResultCode.InvalidItem);
        }

        SampleTransformParseResult result = new SampleTransformParseResult(
            AvifResultCode.Success);
        result.IntermediateBitDepth = intermediateBitDepth;
        int offset = 2;
        int stackDepth = 0;
        int tokenIndex = 0;
        while (tokenIndex < tokenCount)
        {
            if (offset >= payload.Length)
            {
                return new SampleTransformParseResult(
                    AvifResultCode.TruncatedData);
            }
            int tokenValue = payload[offset];
            offset++;
            if (tokenValue == 0)
            {
                int constantBytes = intermediateBitDepth >> 3;
                if (offset > payload.Length - constantBytes)
                {
                    return new SampleTransformParseResult(
                        AvifResultCode.TruncatedData);
                }
                long constant = ReadConstant(
                    payload,
                    offset,
                    intermediateBitDepth);
                offset += constantBytes;
                result.Tokens.Add(new AvifSampleTransformToken(
                    tokenValue,
                    0,
                    constant,
                    -1,
                    default(AvifSampleTransformOperator)));
                stackDepth++;
            }
            else if (tokenValue <= 32)
            {
                if (tokenValue > inputCount)
                {
                    return new SampleTransformParseResult(
                        AvifResultCode.InvalidItem);
                }
                result.Tokens.Add(new AvifSampleTransformToken(
                    tokenValue,
                    1,
                    0,
                    tokenValue - 1,
                    default(AvifSampleTransformOperator)));
                stackDepth++;
            }
            else if (tokenValue >= 64 && tokenValue <= 67)
            {
                if (stackDepth < 1)
                {
                    return new SampleTransformParseResult(
                        AvifResultCode.InvalidItem);
                }
                result.Tokens.Add(CreateOperator(tokenValue));
            }
            else if (tokenValue >= 128 && tokenValue <= 137)
            {
                if (stackDepth < 2)
                {
                    return new SampleTransformParseResult(
                        AvifResultCode.InvalidItem);
                }
                result.Tokens.Add(CreateOperator(tokenValue));
                stackDepth--;
            }
            else
            {
                return new SampleTransformParseResult(
                    AvifResultCode.UnsupportedFeature);
            }
            tokenIndex++;
        }

        if (stackDepth != 1 || offset != payload.Length)
        {
            return new SampleTransformParseResult(
                AvifResultCode.InvalidItem);
        }
        return result;
    }

    static long ReadConstant(
        byte[] payload,
        int offset,
        int bitDepth)
    {
        if (bitDepth == 8)
        {
            return IntegerConversion.ToInt8(payload[offset]);
        }
        if (bitDepth == 16)
        {
            return BinaryPrimitives.ReadInt16BigEndian(
                payload.AsSpan(offset, 2));
        }
        if (bitDepth == 32)
        {
            return BinaryPrimitives.ReadInt32BigEndian(
                payload.AsSpan(offset, 4));
        }
        return BinaryPrimitives.ReadInt64BigEndian(
            payload.AsSpan(offset, 8));
    }

    static AvifSampleTransformToken CreateOperator(int tokenValue)
    {
        return new AvifSampleTransformToken(
            tokenValue,
            2,
            0,
            -1,
            (AvifSampleTransformOperator)tokenValue);
    }
}
