namespace qrcode;

internal class DimensionEstimator
{
    internal static DimensionEstimate EstimateSquare(
        double horizontalDistance,
        double verticalDistance,
        double horizontalModuleSize,
        double verticalModuleSize,
        bool allowModel2,
        bool allowLegacyModel1)
    {
        DimensionEstimate failure = new DimensionEstimate();
        if (!IsFinite(horizontalDistance)
            || !IsFinite(verticalDistance)
            || !IsFinite(horizontalModuleSize)
            || !IsFinite(verticalModuleSize)
            || horizontalDistance <= 0.0
            || verticalDistance <= 0.0
            || horizontalModuleSize <= 0.0
            || verticalModuleSize <= 0.0
            || (!allowModel2 && !allowLegacyModel1))
        {
            return failure;
        }

        double horizontalDimension = horizontalDistance / horizontalModuleSize + 7.0;
        double verticalDimension = verticalDistance / verticalModuleSize + 7.0;
        double averageDimension = (horizontalDimension + verticalDimension) / 2.0;
        int maximumVersion = LegacyModel1Table.MaximumVersion;
        if (allowModel2)
        {
            maximumVersion = Model2VersionTable.MaximumVersion;
        }

        double minimumAcceptedDimension = Model2VersionTable.GetDimension(1) - 2.5;
        double maximumAcceptedDimension = Model2VersionTable.GetDimension(maximumVersion) + 2.5;
        if (averageDimension < minimumAcceptedDimension
            || averageDimension > maximumAcceptedDimension)
        {
            return failure;
        }

        int version = (int)((averageDimension - 17.0) / 4.0 + 0.5);
        if (version < 1)
        {
            version = 1;
        }

        if (version > maximumVersion)
        {
            version = maximumVersion;
        }

        int dimension = Model2VersionTable.GetDimension(version);
        double axisDifference = global::System.Math.Abs(
            horizontalDimension - verticalDimension);
        double maximumAxisDifference = dimension * 0.15;
        if (maximumAxisDifference < 4.0)
        {
            maximumAxisDifference = 4.0;
        }

        double horizontalError = global::System.Math.Abs(horizontalDimension - dimension);
        double verticalError = global::System.Math.Abs(verticalDimension - dimension);
        double averageError = (horizontalError + verticalError) / 2.0;
        if (axisDifference > maximumAxisDifference || averageError > 4.0)
        {
            return failure;
        }

        bool model2Possible = allowModel2;
        bool legacyPossible = allowLegacyModel1
            && version <= LegacyModel1Table.MaximumVersion;
        if (!model2Possible && !legacyPossible)
        {
            return failure;
        }

        double dimensionScore = 1.0 - averageError / 4.0;
        double axisScore = 1.0 - axisDifference / maximumAxisDifference;
        if (dimensionScore < 0.0)
        {
            dimensionScore = 0.0;
        }

        if (axisScore < 0.0)
        {
            axisScore = 0.0;
        }

        return new DimensionEstimate
        {
            Success = true,
            Dimension = dimension,
            Version = version,
            Model2Possible = model2Possible,
            LegacyModel1Possible = legacyPossible,
            HorizontalDimension = horizontalDimension,
            VerticalDimension = verticalDimension,
            Confidence = (dimensionScore + axisScore) / 2.0
        };
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
