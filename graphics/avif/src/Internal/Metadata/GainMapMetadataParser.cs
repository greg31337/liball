using System;
using System.Buffers.Binary;

namespace avif;

internal class GainMapRational
{
    internal long Numerator;
    internal ulong Denominator;

    internal GainMapRational(long numerator, ulong denominator)
    {
        Numerator = numerator;
        Denominator = denominator;
    }

    internal double GetValue()
    {
        return Numerator / (double)Denominator;
    }
}

internal class ParsedGainMapMetadata
{
    internal AvifResultCode Code;
    internal int Version;
    internal int MinimumVersion;
    internal int WriterVersion;
    internal bool Multichannel;
    internal bool UseBaseColorSpace;
    internal GainMapRational? BaseHeadroom;
    internal GainMapRational? AlternateHeadroom;
    internal GainMapRational[] Minimum;
    internal GainMapRational[] Maximum;
    internal GainMapRational[] Gamma;
    internal GainMapRational[] BaseOffset;
    internal GainMapRational[] AlternateOffset;

    internal ParsedGainMapMetadata(AvifResultCode code)
    {
        Code = code;
        Minimum = new GainMapRational[3];
        Maximum = new GainMapRational[3];
        Gamma = new GainMapRational[3];
        BaseOffset = new GainMapRational[3];
        AlternateOffset = new GainMapRational[3];
    }
}

internal static class GainMapMetadataParser
{
    internal static ParsedGainMapMetadata Parse(byte[] payload)
    {
        if (payload == null)
        {
            return new ParsedGainMapMetadata(AvifResultCode.InvalidArgument);
        }
        if (payload.Length < 6)
        {
            return new ParsedGainMapMetadata(AvifResultCode.TruncatedData);
        }

        ParsedGainMapMetadata parsed = new ParsedGainMapMetadata(
            AvifResultCode.Success);
        parsed.Version = payload[0];
        if (parsed.Version != 0)
        {
            return new ParsedGainMapMetadata(
                AvifResultCode.UnsupportedFeature);
        }
        parsed.MinimumVersion = BinaryPrimitives.ReadUInt16BigEndian(
            payload.AsSpan(1, 2));
        parsed.WriterVersion = BinaryPrimitives.ReadUInt16BigEndian(
            payload.AsSpan(3, 2));
        if (parsed.MinimumVersion > 0)
        {
            return new ParsedGainMapMetadata(
                AvifResultCode.UnsupportedFeature);
        }
        if (parsed.WriterVersion < parsed.MinimumVersion)
        {
            return new ParsedGainMapMetadata(AvifResultCode.InvalidMetadata);
        }

        int flags = payload[5];
        parsed.Multichannel = (flags & 128) != 0;
        parsed.UseBaseColorSpace = (flags & 64) != 0;
        int channelCount = parsed.Multichannel ? 3 : 1;
        int requiredLength = 22 + channelCount * 40;
        if (payload.Length < requiredLength)
        {
            return new ParsedGainMapMetadata(AvifResultCode.TruncatedData);
        }
        if (parsed.WriterVersion == 0 && payload.Length != requiredLength)
        {
            return new ParsedGainMapMetadata(AvifResultCode.InvalidMetadata);
        }

        int offset = 6;
        parsed.BaseHeadroom = ReadUnsigned(payload, offset);
        offset += 8;
        parsed.AlternateHeadroom = ReadUnsigned(payload, offset);
        offset += 8;
        int channel = 0;
        while (channel < channelCount)
        {
            parsed.Minimum[channel] = ReadSigned(payload, offset);
            offset += 8;
            parsed.Maximum[channel] = ReadSigned(payload, offset);
            offset += 8;
            parsed.Gamma[channel] = ReadUnsigned(payload, offset);
            offset += 8;
            parsed.BaseOffset[channel] = ReadSigned(payload, offset);
            offset += 8;
            parsed.AlternateOffset[channel] = ReadSigned(payload, offset);
            offset += 8;
            channel++;
        }
        channel = channelCount;
        while (channel < 3)
        {
            parsed.Minimum[channel] = parsed.Minimum[0];
            parsed.Maximum[channel] = parsed.Maximum[0];
            parsed.Gamma[channel] = parsed.Gamma[0];
            parsed.BaseOffset[channel] = parsed.BaseOffset[0];
            parsed.AlternateOffset[channel] = parsed.AlternateOffset[0];
            channel++;
        }

        if (!IsValid(parsed))
        {
            return new ParsedGainMapMetadata(AvifResultCode.InvalidMetadata);
        }
        return parsed;
    }

    static GainMapRational ReadSigned(byte[] payload, int offset)
    {
        return new GainMapRational(
            BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(offset, 4)),
            BinaryPrimitives.ReadUInt32BigEndian(
                payload.AsSpan(offset + 4, 4)));
    }

    static GainMapRational ReadUnsigned(byte[] payload, int offset)
    {
        return new GainMapRational(
            BinaryPrimitives.ReadUInt32BigEndian(payload.AsSpan(offset, 4)),
            BinaryPrimitives.ReadUInt32BigEndian(
                payload.AsSpan(offset + 4, 4)));
    }

    static bool IsValid(ParsedGainMapMetadata parsed)
    {
        if ((parsed.BaseHeadroom ?? throw new InvalidOperationException("Invalid internal state.")).Denominator == 0
            || (parsed.AlternateHeadroom ?? throw new InvalidOperationException("Invalid internal state.")).Denominator == 0)
        {
            return false;
        }
        int channel = 0;
        while (channel < 3)
        {
            if (parsed.Minimum[channel].Denominator == 0
                || parsed.Maximum[channel].Denominator == 0
                || parsed.Gamma[channel].Denominator == 0
                || parsed.Gamma[channel].Numerator == 0
                || parsed.BaseOffset[channel].Denominator == 0
                || parsed.AlternateOffset[channel].Denominator == 0
                || IsLess(
                    parsed.Maximum[channel], parsed.Minimum[channel]))
            {
                return false;
            }
            channel++;
        }
        return true;
    }

    static bool IsLess(GainMapRational first, GainMapRational second)
    {
        long left = first.Numerator * (long)second.Denominator;
        long right = second.Numerator * (long)first.Denominator;
        return left < right;
    }
}
