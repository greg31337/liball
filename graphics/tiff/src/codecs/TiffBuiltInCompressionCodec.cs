using System;

namespace tiff;

internal class TiffBuiltInCompressionCodec : ITiffCompressionCodec
{
    public bool CanDecode(TiffCompression compression)
    {
        return IsSupportedCompression(compression);
    }

    public bool CanEncode(TiffCompression compression)
    {
        return IsSupportedCompression(compression);
    }

    internal static bool IsSupportedCompression(TiffCompression compression)
    {
        if (compression == TiffCompression.None || compression == TiffCompression.PackBits)
        {
            return true;
        }

        if (compression == TiffCompression.Deflate || compression == TiffCompression.AdobeDeflate || compression == TiffCompression.Lzw)
        {
            return true;
        }

        if (compression == TiffCompression.CcittRle || compression == TiffCompression.CcittGroup3 || compression == TiffCompression.CcittGroup4)
        {
            return true;
        }

        return false;
    }

    public TiffCodecResult Decode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        if (context.Info.Compression == TiffCompression.None)
        {
            return TiffNoneCodec.Decode(context, source);
        }

        if (context.Info.Compression == TiffCompression.PackBits)
        {
            return TiffPackBitsCodec.Decode(context, source);
        }

        if (context.Info.Compression == TiffCompression.Deflate || context.Info.Compression == TiffCompression.AdobeDeflate)
        {
            return TiffDeflateCodec.Decode(context, source);
        }

        if (context.Info.Compression == TiffCompression.Lzw)
        {
            return TiffLzwCodec.Decode(context, source);
        }

        if (context.Info.Compression == TiffCompression.CcittRle)
        {
            return TiffCcittModifiedHuffmanCodec.Decode(context, source);
        }

        if (context.Info.Compression == TiffCompression.CcittGroup3)
        {
            return TiffCcittGroup3Codec.Decode(context, source);
        }

        if (context.Info.Compression == TiffCompression.CcittGroup4)
        {
            return TiffCcittGroup4Codec.Decode(context, source);
        }

        return TiffCodec.CreateUnsupportedCompressionResult(context, source, "decode");
    }

    public TiffCodecResult Encode(TiffCodecContext context, ReadOnlySpan<byte> source)
    {
        if (context.Info.Compression == TiffCompression.None)
        {
            return TiffNoneCodec.Encode(context, source);
        }

        if (context.Info.Compression == TiffCompression.PackBits)
        {
            return TiffPackBitsCodec.Encode(context, source);
        }

        if (context.Info.Compression == TiffCompression.Deflate || context.Info.Compression == TiffCompression.AdobeDeflate)
        {
            return TiffDeflateCodec.Encode(context, source);
        }

        if (context.Info.Compression == TiffCompression.Lzw)
        {
            return TiffLzwCodec.Encode(context, source);
        }

        if (context.Info.Compression == TiffCompression.CcittRle)
        {
            return TiffCcittModifiedHuffmanCodec.Encode(context, source);
        }

        if (context.Info.Compression == TiffCompression.CcittGroup3)
        {
            return TiffCcittGroup3Codec.Encode(context, source);
        }

        if (context.Info.Compression == TiffCompression.CcittGroup4)
        {
            return TiffCcittGroup4Codec.Encode(context, source);
        }

        return TiffCodec.CreateUnsupportedCompressionResult(context, source, "encode");
    }
}
