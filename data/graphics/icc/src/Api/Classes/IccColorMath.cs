namespace icc;

/// <summary>Provides colour conversion, difference, and matrix operations.</summary>
public class IccColorMath : IIccColorMath
{
    internal ServiceAvailability Core;

    /// <summary>Initializes a colour-math service.</summary>
    public IccColorMath()
    {
        Core = new ServiceAvailability("Colour mathematics");
    }

    /// <summary>Converts XYZ to Lab.</summary>
    /// <param name="xyz">The XYZ values.</param>
    /// <param name="whitePoint">The white point.</param>
    /// <returns>The Lab values.</returns>
    public double[] ConvertXyzToLab(double[] xyz, double[] whitePoint)
    {
        return ColorMathCore.ConvertXyzToLab(xyz, whitePoint);
    }

    /// <summary>Converts Lab to XYZ.</summary>
    /// <param name="lab">The Lab values.</param>
    /// <param name="whitePoint">The white point.</param>
    /// <returns>The XYZ values.</returns>
    public double[] ConvertLabToXyz(double[] lab, double[] whitePoint)
    {
        return ColorMathCore.ConvertLabToXyz(lab, whitePoint);
    }

    /// <summary>Converts XYZ to xyY.</summary>
    /// <param name="xyz">The XYZ values.</param>
    /// <returns>The xyY values.</returns>
    public double[] ConvertXyzToXyY(double[] xyz)
    {
        return ColorMathCore.ConvertXyzToXyY(xyz);
    }

    /// <summary>Converts xyY to XYZ.</summary>
    /// <param name="xyY">The xyY values.</param>
    /// <returns>The XYZ values.</returns>
    public double[] ConvertXyYToXyz(double[] xyY)
    {
        return ColorMathCore.ConvertXyYToXyz(xyY);
    }

    /// <summary>Converts Lab to LCh.</summary>
    /// <param name="lab">The Lab values.</param>
    /// <returns>The LCh values.</returns>
    public double[] ConvertLabToLch(double[] lab)
    {
        return ColorMathCore.ConvertLabToLch(lab);
    }

    /// <summary>Converts LCh to Lab.</summary>
    /// <param name="lch">The LCh values.</param>
    /// <returns>The Lab values.</returns>
    public double[] ConvertLchToLab(double[] lch)
    {
        return ColorMathCore.ConvertLchToLab(lch);
    }

    /// <summary>Encodes PCS values as unsigned 16-bit components.</summary>
    /// <param name="pcsValues">The actual PCS values.</param>
    /// <param name="pcsKind">The PCS family.</param>
    /// <param name="integerEncoding">The integer encoding.</param>
    /// <returns>The encoded components.</returns>
    public ushort[] EncodePcsUInt16(double[] pcsValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        return PcsConverter.EncodePcsUInt16(pcsValues, pcsKind, integerEncoding);
    }

    /// <summary>Decodes unsigned 16-bit PCS components.</summary>
    /// <param name="encodedValues">The encoded components.</param>
    /// <param name="pcsKind">The PCS family.</param>
    /// <param name="integerEncoding">The integer encoding.</param>
    /// <returns>The actual PCS values.</returns>
    public double[] DecodePcsUInt16(ushort[] encodedValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        return PcsConverter.DecodePcsUInt16(encodedValues, pcsKind, integerEncoding);
    }

    /// <summary>Encodes direct float32 PCS components without clipping.</summary>
    /// <param name="pcsValues">The actual PCS values.</param>
    /// <param name="pcsKind">The PCS family.</param>
    /// <returns>The float32 PCS components.</returns>
    public float[] EncodePcsFloat32(double[] pcsValues, IccPcsKind pcsKind)
    {
        return PcsConverter.EncodePcsFloat32(pcsValues, pcsKind);
    }

    /// <summary>Decodes direct float32 PCS components.</summary>
    /// <param name="encodedValues">The float32 PCS components.</param>
    /// <param name="pcsKind">The PCS family.</param>
    /// <returns>The actual PCS values.</returns>
    public double[] DecodePcsFloat32(float[] encodedValues, IccPcsKind pcsKind)
    {
        return PcsConverter.DecodePcsFloat32(encodedValues, pcsKind);
    }

    /// <summary>Normalizes actual PCS values for an integer encoding.</summary>
    /// <param name="pcsValues">The actual PCS values.</param>
    /// <param name="pcsKind">The PCS family.</param>
    /// <param name="integerEncoding">The integer encoding.</param>
    /// <returns>The normalized values.</returns>
    public double[] NormalizePcs(double[] pcsValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        return PcsConverter.NormalizePcs(pcsValues, pcsKind, integerEncoding);
    }

    /// <summary>Converts normalized integer-encoding components to actual PCS values.</summary>
    /// <param name="normalizedValues">The normalized values.</param>
    /// <param name="pcsKind">The PCS family.</param>
    /// <param name="integerEncoding">The integer encoding.</param>
    /// <returns>The actual PCS values.</returns>
    public double[] DenormalizePcs(double[] normalizedValues, IccPcsKind pcsKind, IccPcsEncodingKind integerEncoding)
    {
        return PcsConverter.DenormalizePcs(normalizedValues, pcsKind, integerEncoding);
    }

    /// <summary>Converts media-relative PCS values to ICC-absolute values.</summary>
    /// <param name="mediaRelativePcs">The media-relative PCS values.</param>
    /// <param name="relativeWhite">The relative-white values.</param>
    /// <param name="absoluteWhite">The absolute-white values.</param>
    /// <returns>The ICC-absolute PCS values.</returns>
    public double[] ConvertMediaRelativeToIccAbsolute(double[] mediaRelativePcs, double[] relativeWhite, double[] absoluteWhite)
    {
        return PcsConverter.ConvertMediaRelativeToIccAbsolute(mediaRelativePcs, relativeWhite, absoluteWhite);
    }

    /// <summary>Converts ICC-absolute PCS values to media-relative values.</summary>
    /// <param name="absolutePcs">The ICC-absolute PCS values.</param>
    /// <param name="relativeWhite">The relative-white values.</param>
    /// <param name="absoluteWhite">The absolute-white values.</param>
    /// <returns>The media-relative PCS values.</returns>
    public double[] ConvertIccAbsoluteToMediaRelative(double[] absolutePcs, double[] relativeWhite, double[] absoluteWhite)
    {
        return PcsConverter.ConvertIccAbsoluteToMediaRelative(absolutePcs, relativeWhite, absoluteWhite);
    }

    /// <summary>Calculates a colour difference.</summary>
    /// <param name="firstLab">The first Lab values.</param>
    /// <param name="secondLab">The second Lab values.</param>
    /// <param name="formula">The difference formula.</param>
    /// <returns>The colour difference.</returns>
    public double CalculateColorDifference(double[] firstLab, double[] secondLab, IccColorDifferenceFormula formula)
    {
        return ColorDifferenceMath.CalculateColorDifference(firstLab, secondLab, formula);
    }

    /// <summary>Finds an exact named-colour name.</summary>
    /// <param name="colorSet">The named-colour collection.</param>
    /// <param name="name">The case-sensitive name.</param>
    /// <returns>An independent matching colour, or <c>null</c>.</returns>
    public IccNamedColor? FindNamedColorByExactName(IccNamedColorSet colorSet, string name)
    {
        return NamedColorMath.FindByExactName(colorSet, name);
    }

    /// <summary>Finds a normalized named-colour name.</summary>
    /// <param name="colorSet">The named-colour collection.</param>
    /// <param name="name">The name to normalize.</param>
    /// <returns>An independent matching colour, or <c>null</c>.</returns>
    public IccNamedColor? FindNamedColorByNormalizedName(IccNamedColorSet colorSet, string name)
    {
        return NamedColorMath.FindByNormalizedName(colorSet, name);
    }

    /// <summary>Evaluates a named colour at a normalized tint.</summary>
    /// <param name="colorSet">The containing collection.</param>
    /// <param name="color">The named colour.</param>
    /// <param name="tint">The normalized tint.</param>
    /// <returns>The evaluated colour description.</returns>
    public IccColorDescription EvaluateNamedColorTint(IccNamedColorSet colorSet, IccNamedColor color, double tint)
    {
        return NamedColorMath.EvaluateTint(colorSet, color, tint);
    }

    /// <summary>Selects named-colour PCS coordinates at a normalized tint.</summary>
    /// <param name="colorSet">The containing collection.</param>
    /// <param name="color">The named colour.</param>
    /// <param name="tint">The normalized tint.</param>
    /// <returns>The selected PCS coordinates.</returns>
    public IccColorVector SelectNamedColorPcs(IccNamedColorSet colorSet, IccNamedColor color, double tint)
    {
        return NamedColorMath.SelectPcs(colorSet, color, tint);
    }

    /// <summary>Selects named-colour device coordinates at a normalized tint.</summary>
    /// <param name="colorSet">The containing collection.</param>
    /// <param name="color">The named colour.</param>
    /// <param name="tint">The normalized tint.</param>
    /// <returns>The selected device coordinates.</returns>
    public IccColorVector SelectNamedColorDevice(IccNamedColorSet colorSet, IccNamedColor color, double tint)
    {
        return NamedColorMath.SelectDevice(colorSet, color, tint);
    }

    /// <summary>Finds the nearest full-tint named colour using CIEDE2000.</summary>
    /// <param name="colorSet">The named-colour collection.</param>
    /// <param name="lab">The query Lab coordinates.</param>
    /// <returns>An independent nearest colour, or <c>null</c>.</returns>
    public IccNamedColor? FindNearestNamedColorByLab(IccNamedColorSet colorSet, double[] lab)
    {
        return NamedColorMath.FindNearestByLab(colorSet, lab);
    }

    /// <summary>Multiplies dense matrices.</summary>
    /// <param name="left">The left matrix.</param>
    /// <param name="leftRows">The left row count.</param>
    /// <param name="leftColumns">The left column count.</param>
    /// <param name="right">The right matrix.</param>
    /// <param name="rightColumns">The right column count.</param>
    /// <returns>The matrix product.</returns>
    public double[] MultiplyMatrices(double[] left, int leftRows, int leftColumns, double[] right, int rightColumns)
    {
        return MatrixMath.MultiplyMatrices(left, leftRows, leftColumns, right, rightColumns);
    }

    /// <summary>Inverts a dense square matrix.</summary>
    /// <param name="matrix">The matrix values.</param>
    /// <param name="size">The matrix size.</param>
    /// <returns>The inverse matrix.</returns>
    public double[] InvertMatrix(double[] matrix, int size)
    {
        return MatrixMath.InvertMatrix(matrix, size);
    }
}
