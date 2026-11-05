using System;

namespace avif;

internal class ColorConversionContext
{
    internal AvifColorPrimaries Primaries;
    internal AvifTransferCharacteristics Transfer;
    internal AvifMatrixCoefficients Matrix;

    internal ColorConversionContext(AvifColorInfo? information)
    {
        Matrix = ResolveMatrix((information ?? throw new InvalidOperationException("Invalid internal state.")).MatrixCoefficients);
        Primaries = ResolvePrimaries(information.ColorPrimaries, Matrix);
        Transfer = ResolveTransfer(information.TransferCharacteristics, Matrix);
    }

    internal bool IsSupported()
    {
        return ColorMatrixCoefficients.IsSupported(
            Matrix, Primaries, Transfer);
    }

    internal static bool UsesDirectComponents(
        AvifMatrixCoefficients matrix)
    {
        return matrix == AvifMatrixCoefficients.Identity;
    }

    static AvifMatrixCoefficients ResolveMatrix(
        AvifMatrixCoefficients matrix)
    {
        if (matrix == AvifMatrixCoefficients.Unspecified)
        {
            return AvifMatrixCoefficients.Bt601;
        }
        return matrix;
    }

    static AvifColorPrimaries ResolvePrimaries(
        AvifColorPrimaries primaries,
        AvifMatrixCoefficients matrix)
    {
        if (ColorTables.IsSupported(primaries))
        {
            return primaries;
        }
        if (matrix == AvifMatrixCoefficients.Bt2020NonConstantLuminance
            || matrix == AvifMatrixCoefficients.Bt2020ConstantLuminance
            || matrix == AvifMatrixCoefficients.ICtCp
            || matrix == AvifMatrixCoefficients.IptC2)
        {
            return AvifColorPrimaries.Bt2020;
        }
        return AvifColorPrimaries.Bt709;
    }

    static AvifTransferCharacteristics ResolveTransfer(
        AvifTransferCharacteristics transfer,
        AvifMatrixCoefficients matrix)
    {
        if (TransferFunctions.IsSupported(transfer))
        {
            return transfer;
        }
        if (matrix == AvifMatrixCoefficients.ICtCp)
        {
            return AvifTransferCharacteristics.Smpte2084;
        }
        if (matrix == AvifMatrixCoefficients.Identity)
        {
            return AvifTransferCharacteristics.Srgb;
        }
        return AvifTransferCharacteristics.Bt709;
    }
}
