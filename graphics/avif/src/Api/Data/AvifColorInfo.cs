namespace avif;

/// <summary>
/// Describes the H.273 color interpretation and integer range of image samples.
/// </summary>
public class AvifColorInfo
{
    /// <summary>The color-primary chromaticities.</summary>
    public AvifColorPrimaries ColorPrimaries;

    /// <summary>The nonlinear transfer characteristic.</summary>
    public AvifTransferCharacteristics TransferCharacteristics;

    /// <summary>The relationship between luma/chroma and color components.</summary>
    public AvifMatrixCoefficients MatrixCoefficients;

    /// <summary>The encoded integer sample range.</summary>
    public AvifRange Range;

    /// <summary>Creates unspecified H.273 signaling with limited-range samples.</summary>
    /// <returns>The default color information.</returns>
    internal static AvifColorInfo Create()
    {
        return new AvifColorInfo(
            AvifColorPrimaries.Unspecified,
            AvifTransferCharacteristics.Unspecified,
            AvifMatrixCoefficients.Unspecified,
            AvifRange.Limited);
    }

    /// <summary>Creates explicit H.273 color signaling.</summary>
    /// <param name="colorPrimaries">The color-primary chromaticities.</param>
    /// <param name="transferCharacteristics">The nonlinear transfer characteristic.</param>
    /// <param name="matrixCoefficients">The luma/chroma matrix coefficients.</param>
    /// <param name="range">The encoded integer sample range.</param>
    public AvifColorInfo(
        AvifColorPrimaries colorPrimaries,
        AvifTransferCharacteristics transferCharacteristics,
        AvifMatrixCoefficients matrixCoefficients,
        AvifRange range)
    {
        ColorPrimaries = colorPrimaries;
        TransferCharacteristics = transferCharacteristics;
        MatrixCoefficients = matrixCoefficients;
        Range = range;
    }

    /// <summary>Creates explicit H.273 color signaling.</summary>
    /// <returns>The initialized color information.</returns>
    internal static AvifColorInfo Create(
        AvifColorPrimaries colorPrimaries,
        AvifTransferCharacteristics transferCharacteristics,
        AvifMatrixCoefficients matrixCoefficients,
        AvifRange range)
    {
        return new AvifColorInfo(
            colorPrimaries,
            transferCharacteristics,
            matrixCoefficients,
            range);
    }
}
