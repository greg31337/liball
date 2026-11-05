using System;

namespace webp;

internal class WebpVp8LBitReader
{
    internal WebpBitReader Reader;
    internal WebpVp8LHeader Header;

    internal WebpVp8LBitReader(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        Reader = WebpBitReader.FromBytes(data, WebpBitOrder.LeastSignificantBitFirst);
        Header = new WebpVp8LHeader();
    }

    internal static WebpVp8LBitReader FromState(byte[] data, WebpVp8LHeader header, int bitPosition)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(header);
        ValidateBitPosition(data, bitPosition);

        WebpVp8LBitReader result = new WebpVp8LBitReader(data);
        result.Reader.BitPosition = bitPosition;
        result.Header = header;
        return result;
    }

    internal static WebpVp8LHeaderParseResult ParseHeader(byte[] payload, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return ParseHeader(payload.AsSpan(), strictness);
    }

    internal static WebpVp8LHeaderParseResult ParseHeader(ReadOnlySpan<byte> payload, WebpStrictness strictness)
    {
        WebpVp8LHeaderParseResult result = new WebpVp8LHeaderParseResult();
        if (payload.Length < WebpConstants.Vp8LHeaderSize)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "VP8L payload is too short for a lossless header");
            Finish(result);
            return result;
        }

        byte[] data = payload.ToArray();
        WebpVp8LBitReader reader = new WebpVp8LBitReader(data);
        WebpVp8LHeader header = result.Header;
        header.Signature = (byte)reader.ReadBits(8);
        header.Width = (int)reader.ReadBits(14) + 1;
        header.Height = (int)reader.ReadBits(14) + 1;
        header.HasAlpha = reader.ReadBits(1) != 0;
        header.Version = (int)reader.ReadBits(3);

        if (header.Signature != WebpConstants.Vp8LSignature)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "VP8L signature is invalid");
        }

        if (header.Version != WebpConstants.Vp8LVersion)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 4, "VP8L version must be zero");
        }

        if (!result.HasErrors())
        {
            result.Reader = FromState(data, header, WebpConstants.Vp8LHeaderSize * 8);
        }

        Finish(result);
        return result;
    }

    internal int BitsRemaining()
    {
        return Reader.BitsRemaining();
    }

    internal bool HasBits(int bitCount)
    {
        return Reader.HasBits(bitCount);
    }

    internal int BitPosition()
    {
        return Reader.BitPosition;
    }

    internal int BytePosition()
    {
        return Reader.BytePosition();
    }

    internal bool IsByteAligned()
    {
        return Reader.IsByteAligned();
    }

    internal void AlignToByte()
    {
        Reader.AlignToByte();
    }

    internal int ReadBit()
    {
        return Reader.ReadBit();
    }

    internal uint ReadBits(int bitCount)
    {
        return Reader.ReadBits(bitCount);
    }

    internal static void ValidateBitPosition(byte[] data, int bitPosition)
    {
        if (bitPosition < 0)
        {
            throw new ArgumentOutOfRangeException("bitPosition");
        }

        if (bitPosition > data.Length * 8)
        {
            throw new ArgumentOutOfRangeException("bitPosition");
        }
    }

    internal static void AddDiagnostic(WebpVp8LHeaderParseResult result, WebpDiagnosticSeverity severity, long offset, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, WebpConstants.FourCcVp8LString, message));
    }

    internal static void Finish(WebpVp8LHeaderParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
