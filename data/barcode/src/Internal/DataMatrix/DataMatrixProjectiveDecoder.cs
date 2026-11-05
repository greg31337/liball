namespace barcode;

internal class DataMatrixModuleVote
{
    internal bool Value;
    internal double Confidence;
}

internal class DataMatrixGridSampleResult
{
    internal ModuleMatrix Modules = new ModuleMatrix(1, 1);
    internal BarcodeQuadrilateral Location = new BarcodeQuadrilateral();
    internal DataMatrixSymbolInfo SymbolInfo = new DataMatrixSymbolInfo();
    internal double GridOffsetX;
    internal double GridOffsetY;
    internal double VoteConfidence;
    internal int UncertainModuleCount;
}

internal class DataMatrixImageDecodeResult
{
    internal DataMatrixPureDecodeResult Decode = new DataMatrixPureDecodeResult();
    internal DataMatrixFinderEvidence FinderEvidence = new DataMatrixFinderEvidence();
    internal DataMatrixGridSampleResult GridSample = new DataMatrixGridSampleResult();
    internal bool UsedRefinedQuadrilateral;
}

internal static class DataMatrixProjectiveDecoder
{
    internal const double VoteOffset = 0.22;
    internal const double LocalGridOffset = 0.24;
    internal const double UncertainVoteThreshold = 0.67;
    internal const int FinderRefinementAdjustment = 4;

    internal static DataMatrixImageDecodeResult DecodeCandidate(
        ModuleMatrix image,
        DataMatrixFinderEvidence finderEvidence,
        BarcodeDecodeOptions? options)
    {
        DataMatrixImageDecodeResult? result = TryDecodeCandidate(
            image,
            finderEvidence,
            options);
        if (result == null)
        {
            throw new BarcodeDecodingException("The Data Matrix image candidate could not be sampled and decoded.");
        }

        return result;
    }

    internal static DataMatrixImageDecodeResult? TryDecodeCandidate(
        ModuleMatrix image,
        DataMatrixFinderEvidence finderEvidence,
        BarcodeDecodeOptions? options)
    {
        ValidateCandidateInputs(image, finderEvidence);
        BarcodeDecodeOptions decodeOptions = CopyDecodeOptions(options);
        BarcodeQuadrilateral[] locations = CreateLocationAttempts(
            image,
            finderEvidence);
        double[] offsets = new double[]
        {
            0.0,
            -LocalGridOffset,
            LocalGridOffset
        };
        DataMatrixImageDecodeResult? best = null;
        int locationIndex = 0;
        while (locationIndex < locations.Length)
        {
            int verticalIndex = 0;
            while (verticalIndex < offsets.Length)
            {
                int horizontalIndex = 0;
                while (horizontalIndex < offsets.Length)
                {
                    DataMatrixGridSampleResult sample = SampleCandidate(
                        image,
                        finderEvidence.SymbolInfo,
                        finderEvidence.Rotation,
                        locations[locationIndex],
                        offsets[horizontalIndex],
                        offsets[verticalIndex]);
                    DataMatrixPureDecodeResult? decoded = DataMatrixPureDecoder.TryDecode(
                        sample.Modules,
                        decodeOptions);
                    if (decoded != null)
                    {
                        DataMatrixImageDecodeResult candidate = CreateImageDecodeResult(
                            decoded,
                            finderEvidence,
                            sample,
                            locationIndex > 0);
                        if (IsBetterDecode(candidate, best))
                        {
                            best = candidate;
                        }
                    }

                    horizontalIndex = horizontalIndex + 1;
                }

                verticalIndex = verticalIndex + 1;
            }

            locationIndex = locationIndex + 1;
        }

        return best;
    }

    internal static DataMatrixGridSampleResult SampleCandidate(
        ModuleMatrix image,
        DataMatrixSymbolInfo symbol,
        BarcodeRotation observedRotation,
        BarcodeQuadrilateral location,
        double gridOffsetX,
        double gridOffsetY)
    {
        if (image == null)
        {
            throw new BarcodeValidationException("Data Matrix sampling image must not be null.");
        }

        if (symbol == null)
        {
            throw new BarcodeValidationException("Data Matrix sampling symbol information must not be null.");
        }

        if (!IsValidRotation(observedRotation))
        {
            throw new BarcodeValidationException("Data Matrix sampling rotation is invalid.");
        }

        if (symbol.SymbolColumns <= 0 || symbol.SymbolRows <= 0)
        {
            throw new BarcodeValidationException("Data Matrix sampling dimensions must be positive.");
        }

        GeometryMath.ValidateQuadrilateral(location);
        GeometryMath.ValidateCoordinate(gridOffsetX);
        GeometryMath.ValidateCoordinate(gridOffsetY);
        if (global::System.Math.Abs(gridOffsetX) > 0.5
            || global::System.Math.Abs(gridOffsetY) > 0.5)
        {
            throw new BarcodeValidationException("Data Matrix local grid offsets must remain within half a module.");
        }

        int outputWidth = observedRotation == BarcodeRotation.None
            || observedRotation == BarcodeRotation.Clockwise180
            ? symbol.SymbolColumns
            : symbol.SymbolRows;
        int outputHeight = observedRotation == BarcodeRotation.None
            || observedRotation == BarcodeRotation.Clockwise180
            ? symbol.SymbolRows
            : symbol.SymbolColumns;
        BarcodeQuadrilateral destination = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(outputWidth, 0.0),
            GeometryMath.CreatePoint(outputWidth, outputHeight),
            GeometryMath.CreatePoint(0.0, outputHeight));
        PerspectiveTransform transform = PerspectiveTransform.CreateQuadrilateralMapping(
            destination,
            location);
        ModuleMatrix sampled = new ModuleMatrix(outputWidth, outputHeight);
        double confidenceTotal = 0.0;
        int uncertainCount = 0;
        int row = 0;
        while (row < outputHeight)
        {
            int column = 0;
            while (column < outputWidth)
            {
                DataMatrixModuleVote vote = VoteModule(
                    image,
                    transform,
                    column + 0.5 + gridOffsetX,
                    row + 0.5 + gridOffsetY);
                sampled.SetModule(column, row, vote.Value);
                confidenceTotal = confidenceTotal + vote.Confidence;
                if (vote.Confidence < UncertainVoteThreshold)
                {
                    uncertainCount = uncertainCount + 1;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        DataMatrixGridSampleResult result = new DataMatrixGridSampleResult();
        result.Modules = sampled;
        result.Location = CopyLocation(location);
        result.SymbolInfo = symbol.CopySymbolInfo();
        result.GridOffsetX = gridOffsetX;
        result.GridOffsetY = gridOffsetY;
        result.VoteConfidence = confidenceTotal / (double)(outputWidth * outputHeight);
        result.UncertainModuleCount = uncertainCount;
        return result;
    }

    static DataMatrixModuleVote VoteModule(
        ModuleMatrix image,
        PerspectiveTransform transform,
        double gridX,
        double gridY)
    {
        int darkCount = 0;
        int validCount = 0;
        int sampleRow = -1;
        while (sampleRow <= 1)
        {
            int sampleColumn = -1;
            while (sampleColumn <= 1)
            {
                BarcodePoint destination = GeometryMath.CreatePoint(
                    gridX + (sampleColumn * VoteOffset),
                    gridY + (sampleRow * VoteOffset));
                BarcodePoint source = transform.TransformPoint(destination);
                int sourceColumn = (int)global::System.Math.Round(source.X);
                int sourceRow = (int)global::System.Math.Round(source.Y);
                if (sourceColumn >= 0
                    && sourceColumn < image.Width
                    && sourceRow >= 0
                    && sourceRow < image.Height)
                {
                    validCount = validCount + 1;
                    if (image.GetModule(sourceColumn, sourceRow))
                    {
                        darkCount = darkCount + 1;
                    }
                }

                sampleColumn = sampleColumn + 1;
            }

            sampleRow = sampleRow + 1;
        }

        DataMatrixModuleVote vote = new DataMatrixModuleVote();
        if (validCount == 0)
        {
            return vote;
        }

        int lightCount = validCount - darkCount;
        int winningCount = darkCount > lightCount ? darkCount : lightCount;
        vote.Value = darkCount > lightCount;
        vote.Confidence = (double)winningCount / (double)validCount;
        return vote;
    }

    static BarcodeQuadrilateral[] CreateLocationAttempts(
        ModuleMatrix image,
        DataMatrixFinderEvidence finderEvidence)
    {
        BarcodeQuadrilateral original = CopyLocation(finderEvidence.Location);
        global::System.Collections.Generic.List<BarcodeQuadrilateral> attempts =
            new global::System.Collections.Generic.List<BarcodeQuadrilateral>();
        AddLocationAttempt(attempts, original);
        DataMatrixFinderEvidence? refined = DataMatrixImageFinder.RefineQuadrilateral(
            image,
            finderEvidence.Location,
            finderEvidence.Polarity,
            FinderRefinementAdjustment);
        if (refined != null)
        {
            AddLocationAttempt(attempts, refined.Location);
        }

        int baseCount = attempts.Count;
        int index = 0;
        double[] adjustments = new double[]
        {
            0.0,
            0.25,
            0.50,
            -0.25
        };
        while (index < baseCount)
        {
            BarcodeQuadrilateral location = attempts[index];
            int verticalIndex = 0;
            while (verticalIndex < adjustments.Length)
            {
                int horizontalIndex = 0;
                while (horizontalIndex < adjustments.Length)
                {
                    if (adjustments[horizontalIndex] != 0.0
                        || adjustments[verticalIndex] != 0.0)
                    {
                        AddLocationAttempt(
                            attempts,
                            ExpandLocation(
                                location,
                                finderEvidence.SymbolInfo,
                                finderEvidence.Rotation,
                                adjustments[horizontalIndex],
                                adjustments[verticalIndex]));
                    }

                    horizontalIndex = horizontalIndex + 1;
                }

                verticalIndex = verticalIndex + 1;
            }

            index = index + 1;
        }

        return attempts.ToArray();
    }

    static BarcodeQuadrilateral ExpandLocation(
        BarcodeQuadrilateral source,
        DataMatrixSymbolInfo symbol,
        BarcodeRotation rotation,
        double horizontalModuleAmount,
        double verticalModuleAmount)
    {
        int columns = rotation == BarcodeRotation.None
            || rotation == BarcodeRotation.Clockwise180
            ? symbol.SymbolColumns
            : symbol.SymbolRows;
        int rows = rotation == BarcodeRotation.None
            || rotation == BarcodeRotation.Clockwise180
            ? symbol.SymbolRows
            : symbol.SymbolColumns;
        BarcodePoint topLeft = GeometryMath.RequirePoint(source.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(source.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(source.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(source.BottomLeft);
        double horizontalFraction = horizontalModuleAmount / (double)columns;
        double verticalFraction = verticalModuleAmount / (double)rows;
        BarcodePoint expandedTopLeft = ExpandCorner(
            topLeft,
            topRight,
            bottomLeft,
            horizontalFraction,
            verticalFraction);
        BarcodePoint expandedTopRight = ExpandCorner(
            topRight,
            topLeft,
            bottomRight,
            horizontalFraction,
            verticalFraction);
        BarcodePoint expandedBottomRight = ExpandCorner(
            bottomRight,
            bottomLeft,
            topRight,
            horizontalFraction,
            verticalFraction);
        BarcodePoint expandedBottomLeft = ExpandCorner(
            bottomLeft,
            bottomRight,
            topLeft,
            horizontalFraction,
            verticalFraction);
        return GeometryMath.CreateQuadrilateral(
            expandedTopLeft,
            expandedTopRight,
            expandedBottomRight,
            expandedBottomLeft);
    }

    static BarcodePoint ExpandCorner(
        BarcodePoint corner,
        BarcodePoint horizontalNeighbour,
        BarcodePoint verticalNeighbour,
        double horizontalFraction,
        double verticalFraction)
    {
        double x = corner.X
            + ((corner.X - horizontalNeighbour.X) * horizontalFraction)
            + ((corner.X - verticalNeighbour.X) * verticalFraction);
        double y = corner.Y
            + ((corner.Y - horizontalNeighbour.Y) * horizontalFraction)
            + ((corner.Y - verticalNeighbour.Y) * verticalFraction);
        return GeometryMath.CreatePoint(x, y);
    }

    static void AddLocationAttempt(
        global::System.Collections.Generic.List<BarcodeQuadrilateral> attempts,
        BarcodeQuadrilateral candidate)
    {
        int index = 0;
        while (index < attempts.Count)
        {
            if (LocationsAreEquivalent(attempts[index], candidate))
            {
                return;
            }

            index = index + 1;
        }

        attempts.Add(CopyLocation(candidate));
    }

    static DataMatrixImageDecodeResult CreateImageDecodeResult(
        DataMatrixPureDecodeResult decoded,
        DataMatrixFinderEvidence finderEvidence,
        DataMatrixGridSampleResult sample,
        bool usedRefinement)
    {
        decoded.Result.Location = CopyLocation(sample.Location);
        double topAngle = CalculateTopEdgeAngle(sample.Location);
        decoded.Result.OrientationDegrees = NormalizeDegrees(
            topAngle + RotationDegrees(decoded.Result.Rotation));
        decoded.Result.SkewDegrees = CalculateSkewDegrees(sample.Location);
        if (decoded.Result.Metadata != null)
        {
            decoded.Result.Metadata.Confidence = (finderEvidence.Confidence * 0.35)
                + (sample.VoteConfidence * 0.35)
                + (decoded.Result.Metadata.Confidence * 0.30);
        }

        DataMatrixImageDecodeResult result = new DataMatrixImageDecodeResult();
        result.Decode = decoded;
        result.FinderEvidence = finderEvidence;
        result.GridSample = sample;
        result.UsedRefinedQuadrilateral = usedRefinement;
        return result;
    }

    static bool IsBetterDecode(
        DataMatrixImageDecodeResult candidate,
        DataMatrixImageDecodeResult? current)
    {
        if (current == null)
        {
            return true;
        }

        BarcodeDecodeMetadata? candidateMetadata = candidate.Decode.Result.Metadata;
        BarcodeDecodeMetadata? currentMetadata = current.Decode.Result.Metadata;
        int candidateCorrections = candidateMetadata == null
            ? int.MaxValue
            : candidateMetadata.CorrectedErrorCount;
        int currentCorrections = currentMetadata == null
            ? int.MaxValue
            : currentMetadata.CorrectedErrorCount;
        if (candidateCorrections != currentCorrections)
        {
            return candidateCorrections < currentCorrections;
        }

        if (candidate.GridSample.UncertainModuleCount
            != current.GridSample.UncertainModuleCount)
        {
            return candidate.GridSample.UncertainModuleCount
                < current.GridSample.UncertainModuleCount;
        }

        return candidate.GridSample.VoteConfidence > current.GridSample.VoteConfidence;
    }

    static BarcodeDecodeOptions CopyDecodeOptions(BarcodeDecodeOptions? source)
    {
        BarcodeDecodeOptions copy = new BarcodeDecodeOptions();
        if (source == null)
        {
            copy.Polarity = BarcodePolarity.Automatic;
            copy.MirrorMode = BarcodeMirrorMode.Both;
            return copy;
        }

        copy.AllowedFormats = CopyFormats(source.AllowedFormats);
        copy.AllowedLengths = CopyIntegers(source.AllowedLengths);
        copy.RegionOfInterest = ResultCopier.CopyRectangle(source.RegionOfInterest);
        copy.MaximumResults = source.MaximumResults;
        copy.ExpectedResultCount = source.ExpectedResultCount;
        copy.PureSymbol = source.PureSymbol;
        copy.ChecksumMode = source.ChecksumMode;
        copy.ParseGs1 = source.ParseGs1;
        copy.CharacterEncoding = source.CharacterEncoding;
        copy.Polarity = source.Polarity;
        copy.MirrorMode = source.MirrorMode;
        copy.Rotations = CopyRotations(source.Rotations);
        copy.Effort = source.Effort;
        copy.BinarizationMode = source.BinarizationMode;
        copy.LocalizationMode = source.LocalizationMode;
        copy.MaximumDiagnostics = source.MaximumDiagnostics;
        return copy;
    }

    static BarcodeFormat[]? CopyFormats(BarcodeFormat[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeFormat[] copy = new BarcodeFormat[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static int[]? CopyIntegers(int[]? source)
    {
        if (source == null)
        {
            return null;
        }

        int[] copy = new int[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static BarcodeRotation[]? CopyRotations(BarcodeRotation[]? source)
    {
        if (source == null)
        {
            return null;
        }

        BarcodeRotation[] copy = new BarcodeRotation[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index = index + 1;
        }

        return copy;
    }

    static BarcodeQuadrilateral CopyLocation(BarcodeQuadrilateral source)
    {
        return GeometryMath.CreateQuadrilateral(
            GeometryMath.RequirePoint(source.TopLeft),
            GeometryMath.RequirePoint(source.TopRight),
            GeometryMath.RequirePoint(source.BottomRight),
            GeometryMath.RequirePoint(source.BottomLeft));
    }

    static bool LocationsAreEquivalent(
        BarcodeQuadrilateral first,
        BarcodeQuadrilateral second)
    {
        BarcodePoint firstTopLeft = GeometryMath.RequirePoint(first.TopLeft);
        BarcodePoint secondTopLeft = GeometryMath.RequirePoint(second.TopLeft);
        BarcodePoint firstTopRight = GeometryMath.RequirePoint(first.TopRight);
        BarcodePoint secondTopRight = GeometryMath.RequirePoint(second.TopRight);
        BarcodePoint firstBottomRight = GeometryMath.RequirePoint(first.BottomRight);
        BarcodePoint secondBottomRight = GeometryMath.RequirePoint(second.BottomRight);
        BarcodePoint firstBottomLeft = GeometryMath.RequirePoint(first.BottomLeft);
        BarcodePoint secondBottomLeft = GeometryMath.RequirePoint(second.BottomLeft);
        return GeometryMath.DistanceBetweenPoints(firstTopLeft, secondTopLeft) <= GeometryMath.Epsilon
            && GeometryMath.DistanceBetweenPoints(firstTopRight, secondTopRight) <= GeometryMath.Epsilon
            && GeometryMath.DistanceBetweenPoints(firstBottomRight, secondBottomRight) <= GeometryMath.Epsilon
            && GeometryMath.DistanceBetweenPoints(firstBottomLeft, secondBottomLeft) <= GeometryMath.Epsilon;
    }

    static double CalculateTopEdgeAngle(BarcodeQuadrilateral location)
    {
        BarcodePoint topLeft = GeometryMath.RequirePoint(location.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(location.TopRight);
        double radians = global::System.Math.Atan2(
            topRight.Y - topLeft.Y,
            topRight.X - topLeft.X);
        return radians * 180.0 / global::System.Math.PI;
    }

    static double CalculateSkewDegrees(BarcodeQuadrilateral location)
    {
        BarcodePoint topLeft = GeometryMath.RequirePoint(location.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(location.TopRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(location.BottomLeft);
        double topAngle = global::System.Math.Atan2(
            topRight.Y - topLeft.Y,
            topRight.X - topLeft.X);
        double leftAngle = global::System.Math.Atan2(
            bottomLeft.Y - topLeft.Y,
            bottomLeft.X - topLeft.X);
        double difference = (leftAngle - topAngle) * 180.0 / global::System.Math.PI;
        while (difference < 0.0)
        {
            difference = difference + 360.0;
        }

        while (difference >= 360.0)
        {
            difference = difference - 360.0;
        }

        double skew = difference - 90.0;
        return global::System.Math.Abs(skew);
    }

    static double RotationDegrees(BarcodeRotation rotation)
    {
        switch (rotation)
        {
            case BarcodeRotation.None:
                return 0.0;
            case BarcodeRotation.Clockwise90:
                return 90.0;
            case BarcodeRotation.Clockwise180:
                return 180.0;
            default:
                return 270.0;
        }
    }

    static double NormalizeDegrees(double degrees)
    {
        while (degrees < 0.0)
        {
            degrees = degrees + 360.0;
        }

        while (degrees >= 360.0)
        {
            degrees = degrees - 360.0;
        }

        return degrees;
    }

    static void ValidateCandidateInputs(
        ModuleMatrix image,
        DataMatrixFinderEvidence finderEvidence)
    {
        if (image == null)
        {
            throw new BarcodeValidationException("Data Matrix projective decode image must not be null.");
        }

        if (finderEvidence == null)
        {
            throw new BarcodeValidationException("Data Matrix finder evidence must not be null.");
        }

        GeometryMath.ValidateQuadrilateral(finderEvidence.Location);
        if (finderEvidence.SymbolInfo == null
            || finderEvidence.SymbolInfo.Version < 1
            || finderEvidence.SymbolInfo.Version > DataMatrixSymbolInfoTable.SymbolCount)
        {
            throw new BarcodeValidationException("Data Matrix finder evidence has invalid symbol information.");
        }

        if (!IsValidRotation(finderEvidence.Rotation))
        {
            throw new BarcodeValidationException("Data Matrix finder evidence has an invalid rotation.");
        }
    }

    static bool IsValidRotation(BarcodeRotation rotation)
    {
        return rotation == BarcodeRotation.None
            || rotation == BarcodeRotation.Clockwise90
            || rotation == BarcodeRotation.Clockwise180
            || rotation == BarcodeRotation.Clockwise270;
    }
}
