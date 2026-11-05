using System;

namespace avif;

internal static class ColorMatrixCoefficients
{
    static readonly ColorMatrix ictcpLms = new ColorMatrix(new double[]
    {
        1688.0 / 4096, 2146.0 / 4096, 262.0 / 4096,
        683.0 / 4096, 2951.0 / 4096, 462.0 / 4096,
        99.0 / 4096, 309.0 / 4096, 3688.0 / 4096
    });

    static readonly ColorMatrix iptC2Lms = new ColorMatrix(new double[]
    {
        1747.0 / 4096, 2169.0 / 4096, 180.0 / 4096,
        673.0 / 4096, 3029.0 / 4096, 394.0 / 4096,
        50.0 / 4096, 207.0 / 4096, 3839.0 / 4096
    });

    internal static bool IsSupported(
        AvifMatrixCoefficients matrix,
        AvifColorPrimaries primaries,
        AvifTransferCharacteristics transfer)
    {
        if (!TransferFunctions.IsSupported(transfer))
        {
            return false;
        }
        if (matrix == AvifMatrixCoefficients.Identity
            || matrix == AvifMatrixCoefficients.YCgCo
            || matrix == AvifMatrixCoefficients.Smpte2085
            || matrix == AvifMatrixCoefficients.ICtCp
            || matrix == AvifMatrixCoefficients.IptC2)
        {
            return true;
        }
        return GetWeights(matrix, primaries) != null;
    }

    internal static ColorVector? LinearRgbToComponents(
        ColorVector linearRgb,
        AvifMatrixCoefficients matrix,
        AvifColorPrimaries primaries,
        AvifTransferCharacteristics transfer)
    {
        if (linearRgb == null
            || !IsSupported(matrix, primaries, transfer))
        {
            return null;
        }
        if (matrix == AvifMatrixCoefficients.ICtCp)
        {
            return LinearRgbToOpponent(
                linearRgb, transfer, ictcpLms, GetIctcpOpponent(transfer));
        }
        if (matrix == AvifMatrixCoefficients.IptC2)
        {
            return LinearRgbToOpponent(
                linearRgb, transfer, iptC2Lms, GetIptC2Opponent());
        }

        ColorVector encoded = FromLinear(linearRgb, transfer);
        if (matrix == AvifMatrixCoefficients.Identity)
        {
            return new ColorVector(
                encoded.Second, encoded.Third, encoded.First);
        }
        if (matrix == AvifMatrixCoefficients.YCgCo)
        {
            double y = (encoded.First + 2 * encoded.Second
                + encoded.Third) / 4;
            double cg = (-encoded.First + 2 * encoded.Second
                - encoded.Third) / 4;
            double co = (encoded.First - encoded.Third) / 2;
            return new ColorVector(y, cg, co);
        }
        if (matrix == AvifMatrixCoefficients.Smpte2085)
        {
            double y = encoded.Second;
            return new ColorVector(
                y,
                (0.986566 * encoded.Third - y) / 2,
                (encoded.First - 0.991902 * y) / 2);
        }

        ColorVector? weights = GetWeights(matrix, primaries);
        if (matrix == AvifMatrixCoefficients.Bt2020ConstantLuminance
            || matrix == AvifMatrixCoefficients.ChromaDerivedConstantLuminance)
        {
            return EncodeConstantLuminance(
                linearRgb, encoded, weights, transfer);
        }
        return EncodeNonConstantLuminance(encoded, weights);
    }

    internal static ColorVector? ComponentsToLinearRgb(
        ColorVector? components,
        AvifMatrixCoefficients matrix,
        AvifColorPrimaries primaries,
        AvifTransferCharacteristics transfer)
    {
        if (components == null
            || !IsSupported(matrix, primaries, transfer))
        {
            return null;
        }
        if (matrix == AvifMatrixCoefficients.ICtCp)
        {
            return OpponentToLinearRgb(
                components, transfer, ictcpLms, GetIctcpOpponent(transfer));
        }
        if (matrix == AvifMatrixCoefficients.IptC2)
        {
            return OpponentToLinearRgb(
                components, transfer, iptC2Lms, GetIptC2Opponent());
        }

        ColorVector encoded;
        if (matrix == AvifMatrixCoefficients.Identity)
        {
            encoded = new ColorVector(
                components.Third, components.First, components.Second);
        }
        else if (matrix == AvifMatrixCoefficients.YCgCo)
        {
            double temporary = components.First - components.Second;
            encoded = new ColorVector(
                temporary + components.Third,
                components.First + components.Second,
                temporary - components.Third);
        }
        else if (matrix == AvifMatrixCoefficients.Smpte2085)
        {
            encoded = new ColorVector(
                2 * components.Third + 0.991902 * components.First,
                components.First,
                (2 * components.Second + components.First) / 0.986566);
        }
        else
        {
            ColorVector? weights = GetWeights(matrix, primaries);
            if (matrix == AvifMatrixCoefficients.Bt2020ConstantLuminance
                || matrix == AvifMatrixCoefficients.ChromaDerivedConstantLuminance)
            {
                return DecodeConstantLuminance(
                    components, weights, transfer);
            }
            encoded = DecodeNonConstantLuminance(components, weights);
        }
        return ToLinear(encoded, transfer);
    }

    static ColorVector EncodeNonConstantLuminance(
        ColorVector encoded,
        ColorVector? weights)
    {
        double y = (weights ?? throw new InvalidOperationException("Invalid internal state.")).First * encoded.First
            + weights.Second * encoded.Second
            + weights.Third * encoded.Third;
        return new ColorVector(
            y,
            0.5 * (encoded.Third - y) / (1 - weights.Third),
            0.5 * (encoded.First - y) / (1 - weights.First));
    }

    static ColorVector DecodeNonConstantLuminance(
        ColorVector components,
        ColorVector? weights)
    {
        double red = components.First
            + 2 * (1 - (weights ?? throw new InvalidOperationException("Invalid internal state.")).First) * components.Third;
        double blue = components.First
            + 2 * (1 - weights.Third) * components.Second;
        double green = (components.First
            - weights.First * red
            - weights.Third * blue) / weights.Second;
        return new ColorVector(red, green, blue);
    }

    static ColorVector EncodeConstantLuminance(
        ColorVector linear,
        ColorVector encoded,
        ColorVector? weights,
        AvifTransferCharacteristics transfer)
    {
        double linearY = (weights ?? throw new InvalidOperationException("Invalid internal state.")).First * linear.First
            + weights.Second * linear.Second
            + weights.Third * linear.Third;
        double encodedY = TransferFunctions.FromLinear(linearY, transfer);
        double negativeBlue = TransferFunctions.FromLinear(
            1 - weights.Third, transfer);
        double positiveBlue = 1 - TransferFunctions.FromLinear(
            weights.Third, transfer);
        double negativeRed = TransferFunctions.FromLinear(
            1 - weights.First, transfer);
        double positiveRed = 1 - TransferFunctions.FromLinear(
            weights.First, transfer);
        double blueDifference = encoded.Third - encodedY;
        double redDifference = encoded.First - encodedY;
        double blue = blueDifference <= 0
            ? blueDifference / (2 * negativeBlue)
            : blueDifference / (2 * positiveBlue);
        double red = redDifference <= 0
            ? redDifference / (2 * negativeRed)
            : redDifference / (2 * positiveRed);
        return new ColorVector(encodedY, blue, red);
    }

    static ColorVector DecodeConstantLuminance(
        ColorVector components,
        ColorVector? weights,
        AvifTransferCharacteristics transfer)
    {
        double negativeBlue = TransferFunctions.FromLinear(
            1 - (weights ?? throw new InvalidOperationException("Invalid internal state.")).Third, transfer);
        double positiveBlue = 1 - TransferFunctions.FromLinear(
            weights.Third, transfer);
        double negativeRed = TransferFunctions.FromLinear(
            1 - weights.First, transfer);
        double positiveRed = 1 - TransferFunctions.FromLinear(
            weights.First, transfer);
        double encodedBlue = components.First + components.Second
            * 2 * (components.Second <= 0 ? negativeBlue : positiveBlue);
        double encodedRed = components.First + components.Third
            * 2 * (components.Third <= 0 ? negativeRed : positiveRed);
        double linearY = TransferFunctions.ToLinear(
            components.First, transfer);
        double linearBlue = TransferFunctions.ToLinear(encodedBlue, transfer);
        double linearRed = TransferFunctions.ToLinear(encodedRed, transfer);
        double linearGreen = (linearY
            - weights.First * linearRed
            - weights.Third * linearBlue) / weights.Second;
        return new ColorVector(linearRed, linearGreen, linearBlue);
    }

    static ColorVector LinearRgbToOpponent(
        ColorVector linearRgb,
        AvifTransferCharacteristics transfer,
        ColorMatrix rgbToLms,
        ColorMatrix opponent)
    {
        ColorVector linearLms = rgbToLms.Transform(linearRgb);
        ColorVector encodedLms = FromLinear(linearLms, transfer);
        return opponent.Transform(encodedLms);
    }

    static ColorVector? OpponentToLinearRgb(
        ColorVector components,
        AvifTransferCharacteristics transfer,
        ColorMatrix rgbToLms,
        ColorMatrix opponent)
    {
        ColorMatrix? opponentInverse = opponent.Invert();
        ColorMatrix? lmsInverse = rgbToLms.Invert();
        if (opponentInverse == null || lmsInverse == null)
        {
            return null;
        }
        ColorVector encodedLms = opponentInverse.Transform(components);
        ColorVector linearLms = ToLinear(encodedLms, transfer);
        return lmsInverse.Transform(linearLms);
    }

    static ColorMatrix GetIctcpOpponent(
        AvifTransferCharacteristics transfer)
    {
        if (transfer == AvifTransferCharacteristics.Hlg)
        {
            return new ColorMatrix(new double[]
            {
                0.5, 0.5, 0,
                3625.0 / 4096, -7465.0 / 4096, 3840.0 / 4096,
                9500.0 / 4096, -9212.0 / 4096, -288.0 / 4096
            });
        }
        return new ColorMatrix(new double[]
        {
            0.5, 0.5, 0,
            6610.0 / 4096, -13613.0 / 4096, 7003.0 / 4096,
            17933.0 / 4096, -17390.0 / 4096, -543.0 / 4096
        });
    }

    static ColorMatrix GetIptC2Opponent()
    {
        return new ColorMatrix(new double[]
        {
            1638.0 / 4096, 1638.0 / 4096, 820.0 / 4096,
            18248.0 / 4096, -19870.0 / 4096, 1622.0 / 4096,
            3300.0 / 4096, 1463.0 / 4096, -4763.0 / 4096
        });
    }

    static ColorVector FromLinear(
        ColorVector value,
        AvifTransferCharacteristics transfer)
    {
        return new ColorVector(
            TransferFunctions.FromLinear(value.First, transfer),
            TransferFunctions.FromLinear(value.Second, transfer),
            TransferFunctions.FromLinear(value.Third, transfer));
    }

    static ColorVector ToLinear(
        ColorVector value,
        AvifTransferCharacteristics transfer)
    {
        return new ColorVector(
            TransferFunctions.ToLinear(value.First, transfer),
            TransferFunctions.ToLinear(value.Second, transfer),
            TransferFunctions.ToLinear(value.Third, transfer));
    }

    static ColorVector? GetWeights(
        AvifMatrixCoefficients matrix,
        AvifColorPrimaries primaries)
    {
        switch (matrix)
        {
            case AvifMatrixCoefficients.Bt709:
                return new ColorVector(0.2126, 0.7152, 0.0722);
            case AvifMatrixCoefficients.Fcc:
                return new ColorVector(0.30, 0.59, 0.11);
            case AvifMatrixCoefficients.Bt470Bg:
            case AvifMatrixCoefficients.Bt601:
                return new ColorVector(0.299, 0.587, 0.114);
            case AvifMatrixCoefficients.Smpte240:
                return new ColorVector(0.212, 0.701, 0.087);
            case AvifMatrixCoefficients.Bt2020NonConstantLuminance:
            case AvifMatrixCoefficients.Bt2020ConstantLuminance:
                return new ColorVector(0.2627, 0.6780, 0.0593);
            case AvifMatrixCoefficients.ChromaDerivedNonConstantLuminance:
            case AvifMatrixCoefficients.ChromaDerivedConstantLuminance:
                ColorMatrix? rgbToXyz = ColorTables.GetRgbToXyz(primaries);
                return rgbToXyz == null
                    ? null
                    : new ColorVector(
                        rgbToXyz.Values[3],
                        rgbToXyz.Values[4],
                        rgbToXyz.Values[5]);
            default:
                return null;
        }
    }
}
