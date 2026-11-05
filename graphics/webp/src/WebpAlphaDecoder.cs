using System;

namespace webp;

internal static class WebpAlphaDecoder
{
    internal static WebpAlphaHeader ParseHeader(byte headerByte)
    {
        WebpAlphaHeader header = new WebpAlphaHeader();
        header.RawHeader = headerByte;
        header.CompressionMethod = (byte)(headerByte & WebpConstants.AlphaCompressionMask);
        header.FilterMethod = (byte)((headerByte & WebpConstants.AlphaFilterMask) >> WebpConstants.AlphaFilterShift);
        header.Preprocessing = (byte)((headerByte & WebpConstants.AlphaPreprocessingMask) >> WebpConstants.AlphaPreprocessingShift);
        header.ReservedBits = (byte)(headerByte & WebpConstants.AlphaReservedMask);
        header.HasReservedBits = header.ReservedBits != 0;
        return header;
    }

    internal static WebpAlphaDecodeResult Decode(byte[] payload, int width, int height, WebpStrictness strictness)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Decode(payload.AsSpan(), width, height, strictness);
    }

    internal static WebpAlphaDecodeResult Decode(byte[] payload, int width, int height, WebpStrictness strictness, Func<byte[], int, int, WebpPixelBuffer> losslessDecoder)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(losslessDecoder);
        return Decode(payload.AsSpan(), width, height, strictness, losslessDecoder);
    }

    internal static WebpAlphaDecodeResult Decode(ReadOnlySpan<byte> payload, int width, int height, WebpStrictness strictness)
    {
        return DecodeCore(payload, width, height, strictness, null);
    }

    internal static WebpAlphaDecodeResult Decode(ReadOnlySpan<byte> payload, int width, int height, WebpStrictness strictness, Func<byte[], int, int, WebpPixelBuffer> losslessDecoder)
    {
        ArgumentNullException.ThrowIfNull(losslessDecoder);
        return DecodeCore(payload, width, height, strictness, losslessDecoder);
    }

    internal static WebpAlphaDecodeResult DecodeCore(ReadOnlySpan<byte> payload, int width, int height, WebpStrictness strictness, Func<byte[], int, int, WebpPixelBuffer>? losslessDecoder)
    {
        ValidateDimensions(width, height);

        WebpAlphaDecodeResult result = new WebpAlphaDecodeResult();
        if (payload.Length == 0)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "ALPH payload must contain a one-byte header");
            Finish(result);
            return result;
        }

        result.Header = ParseHeader(payload[0]);
        ValidateHeader(result, strictness);
        DecodeAlpha(result, payload, width, height, losslessDecoder);
        Finish(result);
        return result;
    }

    internal static void DecodeAlpha(WebpAlphaDecodeResult result, ReadOnlySpan<byte> payload, int width, int height, Func<byte[], int, int, WebpPixelBuffer>? losslessDecoder)
    {
        if (result.HasErrors())
        {
            return;
        }

        if (result.Header.CompressionMethod == WebpConstants.AlphaCompressionNone)
        {
            DecodeUncompressedAlpha(result, payload, width, height);
        }
        else
        {
            DecodeCompressedAlpha(result, payload, width, height, losslessDecoder);
        }
    }

    internal static void DecodeUncompressedAlpha(WebpAlphaDecodeResult result, ReadOnlySpan<byte> payload, int width, int height)
    {
        if (result.HasErrors())
        {
            return;
        }

        long alphaSampleCount = (long)width * (long)height;
        long expectedLength = alphaSampleCount + 1L;
        if (payload.Length != expectedLength)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 1, "uncompressed ALPH payload length must be width * height + 1 bytes");
            return;
        }

        byte[] filteredAlpha = new byte[(int)alphaSampleCount];
        int index = 0;
        while (index < filteredAlpha.Length)
        {
            filteredAlpha[index] = payload[index + 1];
            index = index + 1;
        }

        result.Alpha = WebpAlphaFilter.Unfilter(filteredAlpha, width, height, result.Header.FilterMethod);
    }

    internal static void DecodeCompressedAlpha(WebpAlphaDecodeResult result, ReadOnlySpan<byte> payload, int width, int height, Func<byte[], int, int, WebpPixelBuffer>? losslessDecoder)
    {
        if (losslessDecoder == null)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 1, "compressed ALPH payload decode requires a VP8L alpha stream decoder");
            return;
        }

        if (payload.Length == 1)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 1, "compressed ALPH payload must include a lossless image stream");
            return;
        }

        byte[] imageStream = CopyAlphaBitstream(payload);
        WebpPixelBuffer argb = losslessDecoder(imageStream, width, height);
        ArgumentNullException.ThrowIfNull(argb);
        WebpPixelBufferHelper.ValidateRgbaBuffer(argb);
        ValidateDecodedDimensions(argb, width, height);

        byte[] filteredAlpha = ExtractGreenChannel(argb);
        result.Alpha = WebpAlphaFilter.Unfilter(filteredAlpha, width, height, result.Header.FilterMethod);
    }

    internal static byte[] CopyAlphaBitstream(ReadOnlySpan<byte> payload)
    {
        byte[] imageStream = new byte[payload.Length - 1];
        int index = 0;
        while (index < imageStream.Length)
        {
            imageStream[index] = payload[index + 1];
            index = index + 1;
        }

        return imageStream;
    }

    internal static byte[] ExtractGreenChannel(WebpPixelBuffer argb)
    {
        byte[] green = new byte[argb.Width * argb.Height];
        int row = 0;
        while (row < argb.Height)
        {
            int rowOffset = row * argb.Stride;
            int column = 0;
            while (column < argb.Width)
            {
                int pixel = rowOffset + column * 4;
                int alphaIndex = row * argb.Width + column;
                green[alphaIndex] = argb.Rgba[pixel + 1];
                column = column + 1;
            }

            row = row + 1;
        }

        return green;
    }

    internal static void ValidateHeader(WebpAlphaDecodeResult result, WebpStrictness strictness)
    {
        if (result.Header.HasReservedBits)
        {
            AddReservedDiagnostic(result, strictness, 0, "ALPH reserved header bits are set");
        }

        if (result.Header.CompressionMethod != WebpConstants.AlphaCompressionNone && result.Header.CompressionMethod != WebpConstants.AlphaCompressionLossless)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, 0, "ALPH compression method must be 0 or 1");
        }

    }

    internal static void ValidateDecodedDimensions(WebpPixelBuffer argb, int width, int height)
    {
        if (argb.Width != width || argb.Height != height)
        {
            throw new ArgumentException("decoded VP8L alpha stream dimensions must match the implicit ALPH dimensions", "argb");
        }
    }

    internal static void AddReservedDiagnostic(WebpAlphaDecodeResult result, WebpStrictness strictness, long offset, string message)
    {
        if (strictness == WebpStrictness.Strict)
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Error, offset, message);
        }
        else
        {
            AddDiagnostic(result, WebpDiagnosticSeverity.Warning, offset, message);
        }
    }

    internal static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException("height");
        }

        long alphaSampleCount = (long)width * (long)height;
        if (alphaSampleCount > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("height");
        }
    }

    internal static void AddDiagnostic(WebpAlphaDecodeResult result, WebpDiagnosticSeverity severity, long offset, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(severity, offset, WebpConstants.FourCcAlphaString, message));
    }

    internal static void Finish(WebpAlphaDecodeResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
