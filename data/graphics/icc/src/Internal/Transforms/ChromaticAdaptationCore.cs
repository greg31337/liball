namespace icc;

internal static class ChromaticAdaptationCore
{
    internal const double WhiteComparisonTolerance = 1e-14;

    internal static double[] BuildBradfordMatrix(double[] sourceWhitePoint, double[] destinationWhitePoint)
    {
        double[] sourceWhite = NormalizeWhitePoint(sourceWhitePoint, "source XYZ white point");
        double[] destinationWhite = NormalizeWhitePoint(destinationWhitePoint, "destination XYZ white point");
        if (AreSameWhiteChromaticity(sourceWhite, destinationWhite))
        {
            return MatrixMath.CreateIdentityMatrix(3);
        }

        double[] bradford = CreateBradfordMatrix();
        double[] inverseBradford = MatrixMath.InvertMatrix3x3(bradford);
        double[] sourceCone = MatrixMath.ApplyMatrix3x3(bradford, sourceWhite);
        double[] destinationCone = MatrixMath.ApplyMatrix3x3(bradford, destinationWhite);
        ValidateConeResponses(sourceCone, "source");
        ValidateConeResponses(destinationCone, "destination");
        double[] coneScaling = new double[]
        {
            destinationCone[0] / sourceCone[0], 0.0, 0.0,
            0.0, destinationCone[1] / sourceCone[1], 0.0,
            0.0, 0.0, destinationCone[2] / sourceCone[2]
        };
        double[] scaledBradford = MatrixMath.MultiplyMatrices3x3(coneScaling, bradford);
        return MatrixMath.MultiplyMatrices3x3(inverseBradford, scaledBradford);
    }

    internal static double[] ApplyAdaptationMatrix(double[] matrix, double[] xyz)
    {
        return MatrixMath.ApplyMatrix3x3(matrix, xyz);
    }

    internal static double[] InvertAdaptationMatrix(double[] matrix)
    {
        return MatrixMath.InvertMatrix3x3(matrix);
    }

    internal static double[] BuildToD50Matrix(double[] sourceWhitePoint)
    {
        return BuildBradfordMatrix(sourceWhitePoint, CreateD50WhitePoint());
    }

    internal static double[] BuildFromD50Matrix(double[] destinationWhitePoint)
    {
        return BuildBradfordMatrix(CreateD50WhitePoint(), destinationWhitePoint);
    }

    internal static double[] AdaptToD50(double[] xyz, double[] sourceWhitePoint)
    {
        return ApplyAdaptationMatrix(BuildToD50Matrix(sourceWhitePoint), xyz);
    }

    internal static double[] AdaptFromD50(double[] xyz, double[] destinationWhitePoint)
    {
        return ApplyAdaptationMatrix(BuildFromD50Matrix(destinationWhitePoint), xyz);
    }

    internal static double[] ConvertMediaWhiteToPcsAdoptedWhite(double[] mediaWhitePoint, double[] chromaticAdaptationMatrix)
    {
        return ApplyAdaptationMatrix(chromaticAdaptationMatrix, mediaWhitePoint);
    }

    internal static double[] ConvertPcsAdoptedWhiteToMediaWhite(double[] pcsWhitePoint, double[] chromaticAdaptationMatrix)
    {
        double[] inverse = InvertAdaptationMatrix(chromaticAdaptationMatrix);
        return ApplyAdaptationMatrix(inverse, pcsWhitePoint);
    }

    internal static double[] CreateBradfordMatrix()
    {
        return new double[]
        {
            IccStandardColorimetry.Bradford00,
            IccStandardColorimetry.Bradford01,
            IccStandardColorimetry.Bradford02,
            IccStandardColorimetry.Bradford10,
            IccStandardColorimetry.Bradford11,
            IccStandardColorimetry.Bradford12,
            IccStandardColorimetry.Bradford20,
            IccStandardColorimetry.Bradford21,
            IccStandardColorimetry.Bradford22
        };
    }

    internal static double[] CreateD50WhitePoint()
    {
        return new double[]
        {
            IccStandardColorimetry.D50X,
            IccStandardColorimetry.D50Y,
            IccStandardColorimetry.D50Z
        };
    }

    internal static double[] NormalizeWhitePoint(double[] whitePoint, string description)
    {
        ColorMathCore.ValidateWhitePoint(whitePoint);
        double[] normalized = new double[]
        {
            whitePoint[0] / whitePoint[1],
            1.0,
            whitePoint[2] / whitePoint[1]
        };
        ColorMathCore.ValidateFiniteResult(normalized, description + " normalization");
        return normalized;
    }

    internal static bool AreSameWhiteChromaticity(double[] first, double[] second)
    {
        int index = 0;
        while (index < 3)
        {
            double difference = System.Math.Abs(first[index] - second[index]);
            double scale = System.Math.Max(1.0, System.Math.Max(System.Math.Abs(first[index]), System.Math.Abs(second[index])));
            if (difference > WhiteComparisonTolerance * scale)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void ValidateConeResponses(double[] responses, string description)
    {
        int index = 0;
        while (index < responses.Length)
        {
            if (!System.Double.IsFinite(responses[index]) || responses[index] <= 0.0)
            {
                throw new IccTransformException($"The {description} XYZ white point does not produce positive finite Bradford cone responses.");
            }

            index = index + 1;
        }
    }
}
