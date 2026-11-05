using System;

namespace tiff;

internal class TiffNoneCodec
{
    internal static TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] bytes = DecodeBytes(context, source, diagnostics);
        return TiffCodecResult.Create(bytes, diagnostics);
    }

    internal static TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        byte[] bytes = EncodeBytes(context, source, diagnostics);
        return TiffCodecResult.Create(bytes, diagnostics);
    }

    internal static byte[] DecodeBytes(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateDecodeContext(context, source, diagnostics))
        {
            return new byte[0];
        }

        if (context.ExpectedDecodedByteCount != (ulong)source.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Uncompressed source byte count does not match the expected decoded byte count.");
            return new byte[0];
        }

        return TiffIfdEntry.CopyBytes(source);
    }

    internal static byte[] EncodeBytes(TiffCodecContext context, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (!TiffCodec.ValidateEncodeContext(context, diagnostics))
        {
            return new byte[0];
        }

        if (context.ExpectedDecodedByteCount != (ulong)source.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Uncompressed decoded byte count does not match the expected byte count.");
            return new byte[0];
        }

        return TiffIfdEntry.CopyBytes(source);
    }
}
