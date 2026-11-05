namespace icc;

/// <summary>
/// Provides colour-space conversion, matrix, encoding, and colour-difference operations.
/// </summary>
public interface IIccColorMath
{
    /// <summary>
    /// Converts an XYZ colour to Lab relative to the supplied XYZ white point.
    /// </summary>
    /// <param name="xyz">The three XYZ components.</param>
    /// <param name="whitePoint">The three XYZ white-point components.</param>
    /// <returns>The three Lab components.</returns>
    double[] ConvertXyzToLab(double[] xyz, double[] whitePoint);

    /// <summary>
    /// Converts a Lab colour to XYZ relative to the supplied XYZ white point.
    /// </summary>
    /// <param name="lab">The three Lab components.</param>
    /// <param name="whitePoint">The three XYZ white-point components.</param>
    /// <returns>The three XYZ components.</returns>
    double[] ConvertLabToXyz(double[] lab, double[] whitePoint);

    /// <summary>
    /// Converts an XYZ colour to xyY.
    /// </summary>
    /// <param name="xyz">The three XYZ components.</param>
    /// <returns>The three xyY components.</returns>
    double[] ConvertXyzToXyY(double[] xyz);

    /// <summary>
    /// Converts an xyY colour to XYZ.
    /// </summary>
    /// <param name="xyY">The three xyY components.</param>
    /// <returns>The three XYZ components.</returns>
    double[] ConvertXyYToXyz(double[] xyY);

    /// <summary>
    /// Converts a Lab colour to cylindrical LCh coordinates.
    /// </summary>
    /// <param name="lab">The three Lab components.</param>
    /// <returns>The three LCh components.</returns>
    double[] ConvertLabToLch(double[] lab);

    /// <summary>
    /// Converts cylindrical LCh coordinates to Lab.
    /// </summary>
    /// <param name="lch">The three LCh components.</param>
    /// <returns>The three Lab components.</returns>
    double[] ConvertLchToLab(double[] lch);

    /// <summary>
    /// Encodes actual PCS values as generation-specific unsigned 16-bit components.
    /// </summary>
    /// <remarks>Values outside the integer PCS range are clipped component by component before quantization.</remarks>
    /// <param name="pcsValues">The three actual PCSXYZ or PCSLAB values.</param>
    /// <param name="pcsKind">The PCS colour-space family.</param>
    /// <param name="integerEncoding">The version 2 or version 4 integer encoding.</param>
    /// <returns>The three encoded integer components.</returns>
    ushort[] EncodePcsUInt16(double[] pcsValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding);

    /// <summary>
    /// Decodes generation-specific unsigned 16-bit components to actual PCS values.
    /// </summary>
    /// <param name="encodedValues">The three encoded integer components.</param>
    /// <param name="pcsKind">The PCS colour-space family.</param>
    /// <param name="integerEncoding">The version 2 or version 4 integer encoding.</param>
    /// <returns>The three actual PCSXYZ or PCSLAB values.</returns>
    double[] DecodePcsUInt16(ushort[] encodedValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding);

    /// <summary>
    /// Encodes actual PCS values directly as floating-point components without clipping.
    /// </summary>
    /// <remarks>Every component must be representable as a finite float32 value.</remarks>
    /// <param name="pcsValues">The three actual PCSXYZ or PCSLAB values.</param>
    /// <param name="pcsKind">The PCS colour-space family.</param>
    /// <returns>The three float32 PCS components.</returns>
    float[] EncodePcsFloat32(double[] pcsValues, IccPcsKind pcsKind);

    /// <summary>
    /// Decodes direct floating-point PCS components.
    /// </summary>
    /// <param name="encodedValues">The three float32 PCS components.</param>
    /// <param name="pcsKind">The PCS colour-space family.</param>
    /// <returns>The three actual PCSXYZ or PCSLAB values.</returns>
    double[] DecodePcsFloat32(float[] encodedValues, IccPcsKind pcsKind);

    /// <summary>
    /// Normalizes actual PCS values to the zero-to-one representation of an integer encoding.
    /// </summary>
    /// <remarks>Values outside the integer PCS range are clipped component by component.</remarks>
    /// <param name="pcsValues">The three actual PCSXYZ or PCSLAB values.</param>
    /// <param name="pcsKind">The PCS colour-space family.</param>
    /// <param name="integerEncoding">The version 2 or version 4 integer encoding.</param>
    /// <returns>The three normalized components.</returns>
    double[] NormalizePcs(double[] pcsValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding);

    /// <summary>
    /// Converts normalized integer-encoding components to actual PCS values.
    /// </summary>
    /// <param name="normalizedValues">The three normalized components.</param>
    /// <param name="pcsKind">The PCS colour-space family.</param>
    /// <param name="integerEncoding">The version 2 or version 4 integer encoding.</param>
    /// <returns>The three actual PCSXYZ or PCSLAB values.</returns>
    double[] DenormalizePcs(double[] normalizedValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding);

    /// <summary>
    /// Converts a media-relative colorimetric or spectral PCS vector to ICC-absolute values.
    /// </summary>
    /// <remarks>The conversion is not clipped, so scene-referred and extended-range values are preserved.</remarks>
    /// <param name="mediaRelativePcs">The media-relative PCS coordinate vector.</param>
    /// <param name="relativeWhite">The media or spectral relative-white vector.</param>
    /// <param name="absoluteWhite">The PCS illuminant or spectral absolute-white vector.</param>
    /// <returns>The ICC-absolute PCS coordinate vector.</returns>
    double[] ConvertMediaRelativeToIccAbsolute(double[] mediaRelativePcs, double[] relativeWhite, double[] absoluteWhite);

    /// <summary>
    /// Converts an ICC-absolute colorimetric or spectral PCS vector to media-relative values.
    /// </summary>
    /// <remarks>The conversion is not clipped, so scene-referred and extended-range values are preserved.</remarks>
    /// <param name="absolutePcs">The ICC-absolute PCS coordinate vector.</param>
    /// <param name="relativeWhite">The media or spectral relative-white vector.</param>
    /// <param name="absoluteWhite">The PCS illuminant or spectral absolute-white vector.</param>
    /// <returns>The media-relative PCS coordinate vector.</returns>
    double[] ConvertIccAbsoluteToMediaRelative(double[] absolutePcs, double[] relativeWhite, double[] absoluteWhite);

    /// <summary>
    /// Calculates a colour difference between two Lab colours.
    /// </summary>
    /// <param name="firstLab">The first Lab colour.</param>
    /// <param name="secondLab">The second Lab colour.</param>
    /// <param name="formula">The requested difference formula.</param>
    /// <returns>The colour-difference value.</returns>
    double CalculateColorDifference(double[] firstLab, double[] secondLab, IccColorDifferenceFormula formula);

    /// <summary>
    /// Finds the first named colour whose root, composed prefix/root/suffix, or prepared display name exactly matches the supplied name.
    /// </summary>
    /// <param name="colorSet">The named-colour collection.</param>
    /// <param name="name">The case-sensitive name to find.</param>
    /// <returns>An independent copy of the matching colour, or <c>null</c> when no colour matches.</returns>
    IccNamedColor? FindNamedColorByExactName(IccNamedColorSet colorSet, string name);

    /// <summary>
    /// Finds the first named colour after Unicode compatibility, case, and whitespace normalization.
    /// </summary>
    /// <param name="colorSet">The named-colour collection.</param>
    /// <param name="name">The name to normalize and find.</param>
    /// <returns>An independent copy of the matching colour, or <c>null</c> when no colour matches.</returns>
    IccNamedColor? FindNamedColorByNormalizedName(IccNamedColorSet colorSet, string name);

    /// <summary>
    /// Evaluates a named colour at a normalized tint and prepares its available colour representations.
    /// </summary>
    /// <param name="colorSet">The containing collection and optional shared zero-tint endpoint.</param>
    /// <param name="color">The named colour to evaluate.</param>
    /// <param name="tint">The normalized tint from zero through one.</param>
    /// <returns>An independent human-oriented colour description.</returns>
    IccColorDescription EvaluateNamedColorTint(IccNamedColorSet colorSet, IccNamedColor color, double tint);

    /// <summary>
    /// Selects or linearly interpolates a named colour's PCS coordinates at a normalized tint.
    /// </summary>
    /// <param name="colorSet">The containing collection and optional shared zero-tint endpoint.</param>
    /// <param name="color">The named colour to evaluate.</param>
    /// <param name="tint">The normalized tint from zero through one.</param>
    /// <returns>An independent PCS coordinate vector.</returns>
    IccColorVector SelectNamedColorPcs(IccNamedColorSet colorSet, IccNamedColor color, double tint);

    /// <summary>
    /// Selects or linearly interpolates a named colour's device coordinates at a normalized tint.
    /// </summary>
    /// <param name="colorSet">The containing collection and optional shared zero-tint endpoint.</param>
    /// <param name="color">The named colour to evaluate.</param>
    /// <param name="tint">The normalized tint from zero through one.</param>
    /// <returns>An independent device coordinate vector.</returns>
    IccColorVector SelectNamedColorDevice(IccNamedColorSet colorSet, IccNamedColor color, double tint);

    /// <summary>
    /// Finds the full-tint named colour with the smallest CIEDE2000 difference from a Lab query.
    /// </summary>
    /// <param name="colorSet">The named-colour collection.</param>
    /// <param name="lab">The query Lab coordinates.</param>
    /// <returns>An independent copy of the nearest colour, or <c>null</c> for an empty collection.</returns>
    IccNamedColor? FindNearestNamedColorByLab(IccNamedColorSet colorSet, double[] lab);

    /// <summary>
    /// Multiplies two dense row-major matrices.
    /// </summary>
    /// <param name="left">The left row-major matrix.</param>
    /// <param name="leftRows">The left matrix row count.</param>
    /// <param name="leftColumns">The left matrix column count.</param>
    /// <param name="right">The right row-major matrix.</param>
    /// <param name="rightColumns">The right matrix column count.</param>
    /// <returns>The row-major matrix product.</returns>
    double[] MultiplyMatrices(double[] left, int leftRows, int leftColumns, double[] right, int rightColumns);

    /// <summary>
    /// Inverts a dense square row-major matrix.
    /// </summary>
    /// <param name="matrix">The square row-major matrix.</param>
    /// <param name="size">The matrix row and column count.</param>
    /// <returns>The inverse row-major matrix.</returns>
    double[] InvertMatrix(double[] matrix, int size);
}
