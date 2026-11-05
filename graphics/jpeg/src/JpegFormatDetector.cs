namespace jpeg;

internal class JpegFormatDetector
{
    internal JpegFormatFamily Detect(JpegByteReader reader)
    {
        byte[] data = reader.PeekBytes(64);
        return DetectBytes(data);
    }

    internal JpegFormatFamily DetectBytes(byte[] data)
    {
        if (Matches(data, 0, new byte[] { 0xFF, 0xD8 }))
        {
            return JpegFormatFamily.ClassicJpeg;
        }

        if (Matches(data, 0, new byte[] { 0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A }))
        {
            return JpegFormatFamily.Jpeg2000;
        }

        if (Matches(data, 0, new byte[] { 0xFF, 0x4F, 0xFF, 0x51 }))
        {
            return JpegFormatFamily.Jpeg2000;
        }

        if (Matches(data, 0, new byte[] { 0x49, 0x49, 0xBC, 0x01 }))
        {
            return JpegFormatFamily.JpegXr;
        }

        if (Matches(data, 0, new byte[] { 0x57, 0x4D, 0x50, 0x48, 0x4F, 0x54, 0x4F, 0x00 }))
        {
            return JpegFormatFamily.JpegXr;
        }

        if (Matches(data, 0, new byte[] { 0x00, 0x00, 0x00, 0x0C, 0x4A, 0x58, 0x4C, 0x20, 0x0D, 0x0A, 0x87, 0x0A }))
        {
            return JpegFormatFamily.JpegXl;
        }

        if (Matches(data, 0, new byte[] { 0xFF, 0x0A }))
        {
            return JpegFormatFamily.JpegXl;
        }

        if (IsHeif(data))
        {
            return JpegFormatFamily.Heif;
        }

        return JpegFormatFamily.Unknown;
    }

    internal bool IsHeif(byte[] data)
    {
        if (!MatchesFourCc(data, 4, "ftyp"))
        {
            return false;
        }

        if (IsHeifBrand(data, 8))
        {
            return true;
        }

        int offset = 16;
        while (offset + 4 <= data.Length)
        {
            if (IsHeifBrand(data, offset))
            {
                return true;
            }

            offset = offset + 4;
        }

        return false;
    }

    internal bool IsHeifBrand(byte[] data, int offset)
    {
        return MatchesFourCc(data, offset, "mif1")
            || MatchesFourCc(data, offset, "msf1")
            || MatchesFourCc(data, offset, "heif")
            || MatchesFourCc(data, offset, "heic")
            || MatchesFourCc(data, offset, "heix")
            || MatchesFourCc(data, offset, "hevc")
            || MatchesFourCc(data, offset, "hevx")
            || MatchesFourCc(data, offset, "heim")
            || MatchesFourCc(data, offset, "heis")
            || MatchesFourCc(data, offset, "hevm")
            || MatchesFourCc(data, offset, "hevs")
            || MatchesFourCc(data, offset, "avif")
            || MatchesFourCc(data, offset, "avis");
    }

    internal bool MatchesFourCc(byte[] data, int offset, string value)
    {
        if (offset < 0 || offset + 4 > data.Length)
        {
            return false;
        }

        int index = 0;
        while (index < 4)
        {
            if (data[offset + index] != (byte)value[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool Matches(byte[] data, int offset, byte[] signature)
    {
        if (offset < 0 || signature.Length > data.Length - offset)
        {
            return false;
        }

        int index = 0;
        while (index < signature.Length)
        {
            if (data[offset + index] != signature[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
