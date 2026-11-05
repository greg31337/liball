namespace avif;

internal static class TransferFunctions
{
    const double BtAlpha = 1.099296826809442;
    const double BtBeta = 0.018053968510807;
    const double Smpte240Alpha = 1.111572195921731;
    const double Smpte240Beta = 0.022821585529445;

    internal static bool IsSupported(AvifTransferCharacteristics transfer)
    {
        switch (transfer)
        {
            case AvifTransferCharacteristics.Bt709:
            case AvifTransferCharacteristics.Gamma22:
            case AvifTransferCharacteristics.Gamma28:
            case AvifTransferCharacteristics.Bt601:
            case AvifTransferCharacteristics.Smpte240:
            case AvifTransferCharacteristics.Linear:
            case AvifTransferCharacteristics.Logarithmic100:
            case AvifTransferCharacteristics.Logarithmic316:
            case AvifTransferCharacteristics.Iec61966_2_4:
            case AvifTransferCharacteristics.Bt1361:
            case AvifTransferCharacteristics.Srgb:
            case AvifTransferCharacteristics.Bt2020TenBit:
            case AvifTransferCharacteristics.Bt2020TwelveBit:
            case AvifTransferCharacteristics.Smpte2084:
            case AvifTransferCharacteristics.Smpte428:
            case AvifTransferCharacteristics.Hlg:
                return true;
            default:
                return false;
        }
    }

    internal static double ToLinear(
        double value,
        AvifTransferCharacteristics transfer)
    {
        switch (transfer)
        {
            case AvifTransferCharacteristics.Bt709:
            case AvifTransferCharacteristics.Bt601:
                return DecodeBt709(value, BtAlpha, BtBeta);
            case AvifTransferCharacteristics.Gamma22:
                return SignedPower(value, 2.2);
            case AvifTransferCharacteristics.Gamma28:
                return SignedPower(value, 2.8);
            case AvifTransferCharacteristics.Smpte240:
                return DecodeSmpte240(value);
            case AvifTransferCharacteristics.Linear:
                return value;
            case AvifTransferCharacteristics.Logarithmic100:
                return value <= 0
                    ? 0
                    : System.Math.Pow(10, 2 * (value - 1));
            case AvifTransferCharacteristics.Logarithmic316:
                return value <= 0
                    ? 0
                    : System.Math.Pow(10, 2.5 * (value - 1));
            case AvifTransferCharacteristics.Iec61966_2_4:
                return DecodeExtendedBt709(value);
            case AvifTransferCharacteristics.Bt1361:
                return DecodeBt1361(value);
            case AvifTransferCharacteristics.Srgb:
                return DecodeSrgb(value);
            case AvifTransferCharacteristics.Bt2020TenBit:
            case AvifTransferCharacteristics.Bt2020TwelveBit:
                return DecodeBt709(value, BtAlpha, BtBeta);
            case AvifTransferCharacteristics.Smpte2084:
                return DecodePq(value);
            case AvifTransferCharacteristics.Smpte428:
                return value <= 0
                    ? 0
                    : System.Math.Pow(value, 2.6) * 52.37 / 48;
            case AvifTransferCharacteristics.Hlg:
                return DecodeHlg(value);
            default:
                return double.NaN;
        }
    }

    internal static double FromLinear(
        double value,
        AvifTransferCharacteristics transfer)
    {
        switch (transfer)
        {
            case AvifTransferCharacteristics.Bt709:
            case AvifTransferCharacteristics.Bt601:
                return EncodeBt709(value, BtAlpha, BtBeta);
            case AvifTransferCharacteristics.Gamma22:
                return SignedPower(value, 1 / 2.2);
            case AvifTransferCharacteristics.Gamma28:
                return SignedPower(value, 1 / 2.8);
            case AvifTransferCharacteristics.Smpte240:
                return EncodeSmpte240(value);
            case AvifTransferCharacteristics.Linear:
                return value;
            case AvifTransferCharacteristics.Logarithmic100:
                return value < 0.01
                    ? 0
                    : 1 + System.Math.Log10(value) / 2;
            case AvifTransferCharacteristics.Logarithmic316:
                return value < System.Math.Sqrt(10) / 1000
                    ? 0
                    : 1 + System.Math.Log10(value) / 2.5;
            case AvifTransferCharacteristics.Iec61966_2_4:
                return EncodeExtendedBt709(value);
            case AvifTransferCharacteristics.Bt1361:
                return EncodeBt1361(value);
            case AvifTransferCharacteristics.Srgb:
                return EncodeSrgb(value);
            case AvifTransferCharacteristics.Bt2020TenBit:
            case AvifTransferCharacteristics.Bt2020TwelveBit:
                return EncodeBt709(value, BtAlpha, BtBeta);
            case AvifTransferCharacteristics.Smpte2084:
                return EncodePq(value);
            case AvifTransferCharacteristics.Smpte428:
                return value <= 0
                    ? 0
                    : System.Math.Pow(value * 48 / 52.37, 1 / 2.6);
            case AvifTransferCharacteristics.Hlg:
                return EncodeHlg(value);
            default:
                return double.NaN;
        }
    }

    static double EncodeBt709(double value, double alpha, double beta)
    {
        return value < beta
            ? 4.5 * value
            : alpha * System.Math.Pow(value, 0.45) - (alpha - 1);
    }

    static double DecodeBt709(double value, double alpha, double beta)
    {
        double threshold = 4.5 * beta;
        return value < threshold
            ? value / 4.5
            : System.Math.Pow(
                (value + alpha - 1) / alpha, 1 / 0.45);
    }

    static double EncodeSmpte240(double value)
    {
        return value < Smpte240Beta
            ? 4 * value
            : Smpte240Alpha * System.Math.Pow(value, 0.45)
                - (Smpte240Alpha - 1);
    }

    static double DecodeSmpte240(double value)
    {
        return value < 4 * Smpte240Beta
            ? value / 4
            : System.Math.Pow(
                (value + Smpte240Alpha - 1) / Smpte240Alpha,
                1 / 0.45);
    }

    static double EncodeExtendedBt709(double value)
    {
        double magnitude = System.Math.Abs(value);
        double encoded = magnitude < BtBeta
            ? 4.5 * magnitude
            : BtAlpha * System.Math.Pow(magnitude, 0.45)
                - (BtAlpha - 1);
        return value < 0 ? -encoded : encoded;
    }

    static double DecodeExtendedBt709(double value)
    {
        double magnitude = System.Math.Abs(value);
        double decoded = magnitude < 4.5 * BtBeta
            ? magnitude / 4.5
            : System.Math.Pow(
                (magnitude + BtAlpha - 1) / BtAlpha, 1 / 0.45);
        return value < 0 ? -decoded : decoded;
    }

    static double EncodeBt1361(double value)
    {
        if (value < -BtBeta / 4)
        {
            return -(BtAlpha * System.Math.Pow(-4 * value, 0.45)
                - (BtAlpha - 1)) / 4;
        }
        if (value < BtBeta)
        {
            return 4.5 * value;
        }
        return BtAlpha * System.Math.Pow(value, 0.45)
            - (BtAlpha - 1);
    }

    static double DecodeBt1361(double value)
    {
        if (value < -4.5 * BtBeta / 4)
        {
            return -System.Math.Pow(
                (-4 * value + BtAlpha - 1) / BtAlpha, 1 / 0.45) / 4;
        }
        if (value < 4.5 * BtBeta)
        {
            return value / 4.5;
        }
        return System.Math.Pow(
            (value + BtAlpha - 1) / BtAlpha, 1 / 0.45);
    }

    static double EncodeSrgb(double value)
    {
        double magnitude = System.Math.Abs(value);
        double encoded = magnitude <= 0.0031308
            ? 12.92 * magnitude
            : 1.055 * System.Math.Pow(magnitude, 1 / 2.4) - 0.055;
        return value < 0 ? -encoded : encoded;
    }

    static double DecodeSrgb(double value)
    {
        double magnitude = System.Math.Abs(value);
        double decoded = magnitude <= 0.04045
            ? magnitude / 12.92
            : System.Math.Pow((magnitude + 0.055) / 1.055, 2.4);
        return value < 0 ? -decoded : decoded;
    }

    static double EncodePq(double value)
    {
        double luminance = Clamp01(value);
        double m1 = 2610.0 / 16384;
        double m2 = 2523.0 / 32;
        double c1 = 3424.0 / 4096;
        double c2 = 2413.0 / 128;
        double c3 = 2392.0 / 128;
        double powered = System.Math.Pow(luminance, m1);
        return System.Math.Pow(
            (c1 + c2 * powered) / (1 + c3 * powered), m2);
    }

    static double DecodePq(double value)
    {
        double encoded = Clamp01(value);
        double m1 = 2610.0 / 16384;
        double m2 = 2523.0 / 32;
        double c1 = 3424.0 / 4096;
        double c2 = 2413.0 / 128;
        double c3 = 2392.0 / 128;
        double powered = System.Math.Pow(encoded, 1 / m2);
        double numerator = System.Math.Max(powered - c1, 0);
        double denominator = c2 - c3 * powered;
        return denominator <= 0
            ? 1
            : System.Math.Pow(numerator / denominator, 1 / m1);
    }

    static double EncodeHlg(double value)
    {
        double linear = value < 0 ? 0 : value;
        double a = 0.17883277;
        double b = 1 - 4 * a;
        double c = 0.55991073;
        return linear <= 1.0 / 12
            ? System.Math.Sqrt(3 * linear)
            : a * System.Math.Log(12 * linear - b) + c;
    }

    static double DecodeHlg(double value)
    {
        double encoded = value < 0 ? 0 : value;
        double a = 0.17883277;
        double b = 1 - 4 * a;
        double c = 0.55991073;
        return encoded <= 0.5
            ? encoded * encoded / 3
            : (System.Math.Exp((encoded - c) / a) + b) / 12;
    }

    static double SignedPower(double value, double power)
    {
        return value < 0
            ? -System.Math.Pow(-value, power)
            : System.Math.Pow(value, power);
    }

    static double Clamp01(double value)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value > 1)
        {
            return 1;
        }
        return value;
    }
}
