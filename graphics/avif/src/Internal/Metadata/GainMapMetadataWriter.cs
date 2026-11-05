using System;
using System.Buffers.Binary;

namespace avif;

internal class GainMapEncodedFraction
{
    internal int SignedNumerator;
    internal uint UnsignedNumerator;
    internal uint Denominator;

    internal GainMapEncodedFraction(
        int signedNumerator,
        uint unsignedNumerator,
        uint denominator)
    {
        SignedNumerator = signedNumerator;
        UnsignedNumerator = unsignedNumerator;
        Denominator = denominator;
    }
}

internal static class GainMapMetadataWriter
{
    internal static byte[] Write(
        bool multichannel,
        bool useBaseColorSpace,
        double baseHeadroom,
        double alternateHeadroom,
        double[] minimum,
        double[] maximum,
        double[] gamma,
        double[] baseOffset,
        double[] alternateOffset)
    {
        if (!ValidateArrays(
            minimum, maximum, gamma, baseOffset, alternateOffset))
        {
            return System.Array.Empty<byte>();
        }

        GainMapEncodedFraction? baseHeadroomFraction = EncodeUnsigned(
            baseHeadroom);
        GainMapEncodedFraction? alternateHeadroomFraction = EncodeUnsigned(
            alternateHeadroom);
        if (baseHeadroomFraction == null
            || alternateHeadroomFraction == null)
        {
            return System.Array.Empty<byte>();
        }

        int channelCount = multichannel ? 3 : 1;
        GainMapEncodedFraction?[] encoded = new GainMapEncodedFraction[
            channelCount * 5];
        int channel = 0;
        while (channel < channelCount)
        {
            int offset = channel * 5;
            encoded[offset] = EncodeSigned(minimum[channel]);
            encoded[offset + 1] = EncodeSigned(maximum[channel]);
            encoded[offset + 2] = EncodeUnsigned(gamma[channel]);
            encoded[offset + 3] = EncodeSigned(baseOffset[channel]);
            encoded[offset + 4] = EncodeSigned(alternateOffset[channel]);
            if (encoded[offset] == null || encoded[offset + 1] == null
                || encoded[offset + 2] == null
                || encoded[offset + 3] == null
                || encoded[offset + 4] == null)
            {
                return System.Array.Empty<byte>();
            }
            channel++;
        }

        byte[] payload = new byte[22 + channelCount * 40];
        payload[5] = (byte)((multichannel ? 128 : 0)
            | (useBaseColorSpace ? 64 : 0));
        int position = 6;
        WriteUnsigned(payload, position, baseHeadroomFraction);
        position += 8;
        WriteUnsigned(payload, position, alternateHeadroomFraction);
        position += 8;
        channel = 0;
        while (channel < channelCount)
        {
            int offset = channel * 5;
            WriteSigned(payload, position, encoded[offset]);
            position += 8;
            WriteSigned(payload, position, encoded[offset + 1]);
            position += 8;
            WriteUnsigned(payload, position, encoded[offset + 2]);
            position += 8;
            WriteSigned(payload, position, encoded[offset + 3]);
            position += 8;
            WriteSigned(payload, position, encoded[offset + 4]);
            position += 8;
            channel++;
        }
        return payload;
    }

    static bool ValidateArrays(
        double[] minimum,
        double[] maximum,
        double[] gamma,
        double[] baseOffset,
        double[] alternateOffset)
    {
        if (minimum == null || maximum == null || gamma == null
            || baseOffset == null || alternateOffset == null
            || minimum.Length < 3 || maximum.Length < 3
            || gamma.Length < 3 || baseOffset.Length < 3
            || alternateOffset.Length < 3)
        {
            return false;
        }
        int channel = 0;
        while (channel < 3)
        {
            if (!IsFinite(minimum[channel]) || !IsFinite(maximum[channel])
                || !IsFinite(gamma[channel]) || gamma[channel] <= 0
                || !IsFinite(baseOffset[channel])
                || !IsFinite(alternateOffset[channel])
                || maximum[channel] < minimum[channel])
            {
                return false;
            }
            channel++;
        }
        return true;
    }

    static GainMapEncodedFraction? EncodeSigned(double value)
    {
        if (!IsFinite(value))
        {
            return null;
        }
        const long precision = 1000000;
        double scaled = System.Math.Round(value * precision);
        if (scaled < int.MinValue || scaled > int.MaxValue)
        {
            return null;
        }
        long numerator = (long)scaled;
        long divisor = GreatestCommonDivisor(
            System.Math.Abs(numerator), precision);
        return new GainMapEncodedFraction(
            (int)(numerator / divisor),
            0,
            (uint)(precision / divisor));
    }

    static GainMapEncodedFraction? EncodeUnsigned(double value)
    {
        if (!IsFinite(value) || value < 0)
        {
            return null;
        }
        const ulong precision = 1000000;
        double scaled = System.Math.Round(value * precision);
        if (scaled < 0 || scaled > uint.MaxValue)
        {
            return null;
        }
        ulong numerator = (ulong)scaled;
        ulong divisor = GreatestCommonDivisorUnsigned(numerator, precision);
        return new GainMapEncodedFraction(
            0,
            (uint)(numerator / divisor),
            (uint)(precision / divisor));
    }

    static long GreatestCommonDivisor(long first, long second)
    {
        while (second != 0)
        {
            long remainder = first % second;
            first = second;
            second = remainder;
        }
        return first == 0 ? 1 : first;
    }

    static ulong GreatestCommonDivisorUnsigned(ulong first, ulong second)
    {
        while (second != 0)
        {
            ulong remainder = first % second;
            first = second;
            second = remainder;
        }
        return first == 0 ? 1 : first;
    }

    static void WriteSigned(
        byte[] payload,
        int position,
        GainMapEncodedFraction? value)
    {
        BinaryPrimitives.WriteInt32BigEndian(
            payload.AsSpan(position, 4), (value ?? throw new InvalidOperationException("Invalid internal state.")).SignedNumerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(position + 4, 4), value.Denominator);
    }

    static void WriteUnsigned(
        byte[] payload,
        int position,
        GainMapEncodedFraction? value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(position, 4), (value ?? throw new InvalidOperationException("Invalid internal state.")).UnsignedNumerator);
        BinaryPrimitives.WriteUInt32BigEndian(
            payload.AsSpan(position + 4, 4), value.Denominator);
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
