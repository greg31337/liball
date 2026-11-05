namespace icc;

internal static class ColorSpaceChannelCount
{
    internal static int Decode(uint signature)
    {
        switch (signature)
        {
            case IccColorSpaceSignatures.Gray:
            case IccColorSpaceSignatures.OneColor:
                return 1;
            case IccColorSpaceSignatures.TwoColor:
                return 2;
            case IccColorSpaceSignatures.Xyz:
            case IccColorSpaceSignatures.Lab:
            case IccColorSpaceSignatures.Luv:
            case IccColorSpaceSignatures.YCbCr:
            case IccColorSpaceSignatures.Yxy:
            case IccColorSpaceSignatures.Rgb:
            case IccColorSpaceSignatures.Hsv:
            case IccColorSpaceSignatures.Hls:
            case IccColorSpaceSignatures.Cmy:
            case IccColorSpaceSignatures.ThreeColor:
                return 3;
            case IccColorSpaceSignatures.Cmyk:
            case IccColorSpaceSignatures.FourColor:
                return 4;
            case IccColorSpaceSignatures.FiveColor:
                return 5;
            case IccColorSpaceSignatures.SixColor:
                return 6;
            case IccColorSpaceSignatures.SevenColor:
                return 7;
            case IccColorSpaceSignatures.EightColor:
                return 8;
            case IccColorSpaceSignatures.NineColor:
                return 9;
            case IccColorSpaceSignatures.TenColor:
                return 10;
            case IccColorSpaceSignatures.ElevenColor:
                return 11;
            case IccColorSpaceSignatures.TwelveColor:
                return 12;
            case IccColorSpaceSignatures.ThirteenColor:
                return 13;
            case IccColorSpaceSignatures.FourteenColor:
                return 14;
            case IccColorSpaceSignatures.FifteenColor:
                return 15;
            default:
                break;
        }

        uint prefix = signature & 0xffff0000;
        uint encodedCount = signature & 0x0000ffff;
        if ((prefix == IccColorSpaceSignatures.ExtendedDevicePrefix
            || prefix == IccColorSpaceSignatures.ReflectanceSpectralPrefix
            || prefix == IccColorSpaceSignatures.TransmissionSpectralPrefix
            || prefix == IccColorSpaceSignatures.RadiantSpectralPrefix
            || prefix == IccColorSpaceSignatures.BiSpectralPrefix
            || prefix == IccColorSpaceSignatures.SparseMatrixSpectralPrefix)
            && encodedCount <= int.MaxValue)
        {
            return (int)encodedCount;
        }

        if ((signature & 0xffffff00) == 0x4d434800)
        {
            uint digit = signature & 0xff;
            if (digit >= 0x31 && digit <= 0x39)
            {
                return (int)(digit - 0x30);
            }

            if (digit >= 0x41 && digit <= 0x46)
            {
                return (int)(digit - 0x37);
            }
        }

        return 0;
    }
}
