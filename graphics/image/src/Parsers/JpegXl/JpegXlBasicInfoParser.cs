using System.Collections.Generic;

namespace image;

internal class JpegXlBasicInfoParser
{
    internal static void Parse(ImageMetadataDocument document, JpegXlBitReader bits, JpegXlCodestreamHeader header)
    {
        int start = bits.BitPosition;
        bool allDefault = bits.ReadBits(1) != 0;
        header.MetadataAllDefault = allDefault;
        if (allDefault)
        {
            Finish(bits, header, start);
            return;
        }

        bool extraFields = bits.ReadBits(1) != 0;
        if (extraFields)
        {
            header.Orientation = bits.ReadBits(3) + 1;
            header.HaveIntrinsicSize = bits.ReadBits(1) != 0;
            if (header.HaveIntrinsicSize)
            {
                ReadSize(bits, header, true);
            }

            header.HavePreview = bits.ReadBits(1) != 0;
            if (header.HavePreview)
            {
                ReadPreviewSize(bits, header);
            }

            header.HaveAnimation = bits.ReadBits(1) != 0;
            if (header.HaveAnimation)
            {
                ReadAnimation(bits, header);
            }
        }

        ReadBitDepth(bits, header);
        header.Modular16BitBufferSufficient = bits.ReadBits(1) != 0;
        header.ExtraChannelCount = ReadU32(bits, 0, 0, 0, 1, 4, 2, 12, 1);
        if (header.ExtraChannelCount > 4096)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jxl.codestream.extra-channel.limit", "JPEG XL extra channel count exceeded the supported metadata limit.", "Codestream", header.SourceOffset, header.ExtraChannelCount);
            bits.Valid = false;
        }

        uint channelIndex = 0;
        while (channelIndex < header.ExtraChannelCount && bits.Valid)
        {
            JpegXlExtraChannel channel = ReadExtraChannel(bits);
            if (channel.Type == 0 && !header.HasAlpha)
            {
                header.HasAlpha = true;
                header.AlphaBitsPerSample = channel.BitsPerSample;
                header.AlphaPremultiplied = channel.AlphaPremultiplied;
            }

            channelIndex = channelIndex + 1;
        }

        header.XybEncoded = bits.ReadBits(1) != 0;
        ReadColorEncoding(bits, header);
        if (extraFields)
        {
            SkipToneMapping(bits);
        }

        if (!bits.Valid)
        {
            header.MetadataBitLength = bits.BitPosition - start;
            header.MetadataByteLength = (header.MetadataBitLength + 7) / 8;
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jxl.codestream.metadata.truncated", "JPEG XL codestream basic metadata was truncated or invalid.", "Codestream", header.SourceOffset, bits.Data.Length);
            return;
        }

        Finish(bits, header, start);
    }

    internal static void Finish(JpegXlBitReader bits, JpegXlCodestreamHeader header, int start)
    {
        if (!bits.Valid)
        {
            return;
        }

        header.MetadataValid = true;
        header.MetadataBitLength = bits.BitPosition - start;
        header.MetadataByteLength = (header.MetadataBitLength + 7) / 8;
    }

    internal static void ReadSize(JpegXlBitReader bits, JpegXlCodestreamHeader header, bool intrinsic)
    {
        bool small = bits.ReadBits(1) != 0;
        uint height = JpegXlBoxTraverser.ReadSizeValue(bits, small);
        int ratio = (int)bits.ReadBits(3);
        uint width = ratio == 0 ? JpegXlBoxTraverser.ReadSizeValue(bits, small) : JpegXlBoxTraverser.WidthFromRatio(height, ratio);
        if (intrinsic)
        {
            header.IntrinsicWidth = width;
            header.IntrinsicHeight = height;
        }
    }

    internal static void ReadPreviewSize(JpegXlBitReader bits, JpegXlCodestreamHeader header)
    {
        bool div8 = bits.ReadBits(1) != 0;
        uint height;
        if (div8)
        {
            height = ReadU32(bits, 0, 16, 0, 32, 5, 1, 9, 33) * 8;
        }
        else
        {
            height = ReadU32(bits, 6, 1, 8, 65, 10, 321, 12, 1345);
        }

        int ratio = (int)bits.ReadBits(3);
        uint width;
        if (ratio != 0)
        {
            width = JpegXlBoxTraverser.WidthFromRatio(height, ratio);
        }
        else if (div8)
        {
            width = ReadU32(bits, 0, 16, 0, 32, 5, 1, 9, 33) * 8;
        }
        else
        {
            width = ReadU32(bits, 6, 1, 8, 65, 10, 321, 12, 1345);
        }

        header.PreviewWidth = width;
        header.PreviewHeight = height;
    }

    internal static void ReadAnimation(JpegXlBitReader bits, JpegXlCodestreamHeader header)
    {
        header.TicksPerSecondNumerator = ReadU32(bits, 0, 100, 0, 1000, 10, 1, 30, 1);
        header.TicksPerSecondDenominator = ReadU32(bits, 0, 1, 0, 1001, 8, 1, 10, 1);
        header.LoopCount = ReadU32(bits, 0, 0, 3, 0, 16, 0, 32, 0);
        header.HaveTimecodes = bits.ReadBits(1) != 0;
    }

    internal static void ReadBitDepth(JpegXlBitReader bits, JpegXlCodestreamHeader header)
    {
        header.FloatingPointSamples = bits.ReadBits(1) != 0;
        if (header.FloatingPointSamples)
        {
            header.BitsPerSample = ReadU32(bits, 0, 32, 0, 16, 0, 24, 6, 1);
            header.ExponentBitsPerSample = bits.ReadBits(4) + 1;
        }
        else
        {
            header.BitsPerSample = ReadU32(bits, 0, 8, 0, 10, 0, 12, 6, 1);
            header.ExponentBitsPerSample = 0;
        }
    }

    internal static JpegXlExtraChannel ReadExtraChannel(JpegXlBitReader bits)
    {
        JpegXlExtraChannel channel = new JpegXlExtraChannel();
        bool allDefault = bits.ReadBits(1) != 0;
        if (allDefault)
        {
            return channel;
        }

        channel.Type = ReadEnum(bits);
        bool floatingPoint = bits.ReadBits(1) != 0;
        if (floatingPoint)
        {
            channel.BitsPerSample = ReadU32(bits, 0, 32, 0, 16, 0, 24, 6, 1);
            channel.ExponentBitsPerSample = bits.ReadBits(4) + 1;
        }
        else
        {
            channel.BitsPerSample = ReadU32(bits, 0, 8, 0, 10, 0, 12, 6, 1);
        }

        channel.DimensionShift = ReadU32(bits, 0, 0, 0, 3, 0, 4, 3, 1);
        uint nameLength = ReadU32(bits, 0, 0, 4, 0, 5, 16, 10, 48);
        if (nameLength > 1071)
        {
            bits.Valid = false;
            return channel;
        }

        List<byte> name = new List<byte>();
        uint index = 0;
        while (index < nameLength && bits.Valid)
        {
            name.Add((byte)bits.ReadBits(8));
            index = index + 1;
        }

        channel.Name = MetadataStringReader.DecodeUtf8WithLatin1Fallback(name.ToArray(), null, "Codestream");
        if (channel.Type == 0)
        {
            channel.AlphaPremultiplied = bits.ReadBits(1) != 0;
        }
        else if (channel.Type == 2)
        {
            bits.ReadBits(16);
            bits.ReadBits(16);
            bits.ReadBits(16);
            bits.ReadBits(16);
        }
        else if (channel.Type == 5)
        {
            ReadU32(bits, 0, 1, 2, 0, 4, 3, 8, 19);
        }

        return channel;
    }

    internal static void ReadColorEncoding(JpegXlBitReader bits, JpegXlCodestreamHeader header)
    {
        bool allDefault = bits.ReadBits(1) != 0;
        if (allDefault)
        {
            return;
        }

        header.WantIcc = bits.ReadBits(1) != 0;
        header.ColorSpace = ReadEnum(bits);
        if (header.WantIcc)
        {
            return;
        }

        if (header.ColorSpace != 2)
        {
            header.WhitePoint = ReadEnum(bits);
            if (header.WhitePoint == 2)
            {
                SkipCustomXy(bits);
            }
        }

        if (header.ColorSpace != 1 && header.ColorSpace != 2)
        {
            header.Primaries = ReadEnum(bits);
            if (header.Primaries == 2)
            {
                SkipCustomXy(bits);
                SkipCustomXy(bits);
                SkipCustomXy(bits);
            }
        }

        if (header.ColorSpace != 2)
        {
            header.HaveGamma = bits.ReadBits(1) != 0;
            if (header.HaveGamma)
            {
                header.Gamma = bits.ReadBits(24);
            }
            else
            {
                header.TransferFunction = ReadEnum(bits);
            }
        }

        header.RenderingIntent = ReadEnum(bits);
    }

    internal static void SkipCustomXy(JpegXlBitReader bits)
    {
        ReadU32(bits, 19, 0, 19, 524288, 20, 1048576, 21, 2097152);
        ReadU32(bits, 19, 0, 19, 524288, 20, 1048576, 21, 2097152);
    }

    internal static void SkipToneMapping(JpegXlBitReader bits)
    {
        bool allDefault = bits.ReadBits(1) != 0;
        if (!allDefault)
        {
            bits.ReadBits(16);
            bits.ReadBits(16);
            bits.ReadBits(1);
            bits.ReadBits(16);
        }
    }

    internal static uint ReadEnum(JpegXlBitReader bits)
    {
        return ReadU32(bits, 0, 0, 0, 1, 4, 2, 6, 18);
    }

    internal static uint ReadU32(JpegXlBitReader bits, int bits0, uint offset0, int bits1, uint offset1, int bits2, uint offset2, int bits3, uint offset3)
    {
        uint selector = bits.ReadBits(2);
        if (selector == 0)
        {
            return bits0 == 0 ? offset0 : bits.ReadBits(bits0) + offset0;
        }

        if (selector == 1)
        {
            return bits1 == 0 ? offset1 : bits.ReadBits(bits1) + offset1;
        }

        if (selector == 2)
        {
            return bits2 == 0 ? offset2 : bits.ReadBits(bits2) + offset2;
        }

        return bits3 == 0 ? offset3 : bits.ReadBits(bits3) + offset3;
    }
}
