namespace qrcode;

internal class DetectionOrientationAnalyzer
{
    internal const double MinimumScore = 0.8;

    internal const double MinimumMargin = 0.02;

    // Samples must already follow the detector's finder axes. Compare a logical
    // transpose and both polarities through read-only views, never by rewriting
    // the payload. Expected format values belong to the caller's current format
    // hypothesis; unavailable evidence must be Unknown, not guessed light bits.
    // sampledPolarity records the preprocessing pass: raw reflectance uses
    // DarkOnLight; a raster inverted by preprocessing uses LightOnDark.
    internal static void Annotate(
        QrDetection detection,
        QrMatrix expectedPatterns,
        QrPolarity sampledPolarity)
    {
        if (detection == null)
        {
            throw new QrInputException("The detection is required.", "detection");
        }

        QrMatrix? sampled = detection.SampledMatrix;
        if (sampled == null)
        {
            throw new QrInputException("The detection requires sampled modules.", "SampledMatrix");
        }

        if (sampledPolarity != QrPolarity.DarkOnLight && sampledPolarity != QrPolarity.LightOnDark)
        {
            throw new QrInputException("The sampling pass must specify an explicit polarity.", "sampledPolarity");
        }

        ValidateMatrix(sampled, false);
        ValidateMatrix(expectedPatterns, true);
        bool sameDimensions = sampled.Width == expectedPatterns.Width && sampled.Height == expectedPatterns.Height;
        bool transposedDimensions = sampled.Width == expectedPatterns.Height && sampled.Height == expectedPatterns.Width;
        if (!sameDimensions && !transposedDimensions)
        {
            throw new QrInputException("The sampled grid does not match the pattern dimensions.", "expectedPatterns");
        }

        bool geometryMirrored = IsGeometryMirrored(detection.Corners);
        double[] scores = new double[4];
        double best = -1.0;
        for (int trial = 0; trial < scores.Length; trial++)
        {
            bool transpose = trial >= 2;
            bool invert = trial % 2 != 0;
            scores[trial] = (transpose ? transposedDimensions : sameDimensions)
                ? Score(sampled, expectedPatterns, transpose, invert)
                : -1.0;
            if (scores[trial] > best)
            {
                best = scores[trial];
            }
        }

        bool normalPossible = false;
        bool mirroredPossible = false;
        bool ordinaryPolarityPossible = false;
        bool invertedPolarityPossible = false;
        for (int trial = 0; trial < scores.Length; trial++)
        {
            if (scores[trial] < MinimumScore || best - scores[trial] >= MinimumMargin)
            {
                continue;
            }

            if ((trial >= 2) != geometryMirrored)
            {
                mirroredPossible = true;
            }
            else
            {
                normalPossible = true;
            }

            if (trial % 2 == 0)
            {
                ordinaryPolarityPossible = true;
            }
            else
            {
                invertedPolarityPossible = true;
            }
        }

        // Update only orientation metadata after all validation succeeds.
        // Ambiguous evidence must not become a fabricated positive estimate.
        detection.HasMirrorEstimate = normalPossible != mirroredPossible;
        detection.IsMirrored = detection.HasMirrorEstimate && mirroredPossible;
        detection.Polarity = QrPolarity.Automatic;
        if (ordinaryPolarityPossible != invertedPolarityPossible)
        {
            detection.Polarity = sampledPolarity;
            if (invertedPolarityPossible)
            {
                detection.Polarity = sampledPolarity == QrPolarity.DarkOnLight
                    ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight;
            }
        }
    }

    static double Score(QrMatrix sampled, QrMatrix expected, bool transpose, bool invert)
    {
        QrModuleRole[]? roles = expected.Roles;
        if (roles == null)
        {
            return 0.0;
        }

        int[] counts = new int[4];
        int[] matches = new int[4];
        for (int index = 0; index < expected.Values.Length; index++)
        {
            int group = GetGroup(roles[index]);
            QrModuleValue expectedValue = expected.Values[index];
            if (group < 0 || expectedValue == QrModuleValue.Unknown)
            {
                continue;
            }

            counts[group]++;
            int row = index / expected.Width;
            int column = index % expected.Width;
            int sampleIndex = transpose ? column * sampled.Width + row : row * sampled.Width + column;
            QrModuleValue value = sampled.Values[sampleIndex];
            if (value == QrModuleValue.Unknown)
            {
                continue;
            }

            bool dark = value == QrModuleValue.Dark;
            if (invert)
            {
                dark = !dark;
            }

            if (dark == (expectedValue == QrModuleValue.Dark))
            {
                matches[group]++;
            }
        }

        double score = 0.0;
        int groups = 0;
        for (int group = 0; group < counts.Length; group++)
        {
            if (counts[group] > 0)
            {
                score += (double)matches[group] / counts[group];
                groups++;
            }
        }

        return groups == 0 ? 0.0 : score / groups;
    }

    static int GetGroup(QrModuleRole role)
    {
        if (role == QrModuleRole.Finder)
        {
            return 0;
        }

        if (role == QrModuleRole.Timing)
        {
            return 1;
        }

        if (role == QrModuleRole.Alignment)
        {
            return 2;
        }

        if (role == QrModuleRole.Format)
        {
            return 3;
        }

        return -1;
    }

    static bool IsGeometryMirrored(QrQuadrilateral? corners)
    {
        if (corners == null)
        {
            return false;
        }

        QrQuadrilateral unit = new QrQuadrilateral
        {
            TopLeft = new QrPoint(), TopRight = new QrPoint { X = 1 },
            BottomRight = new QrPoint { X = 1, Y = 1 }, BottomLeft = new QrPoint { Y = 1 }
        };
        // Validate finite, convex geometry before inspecting the winding.
        PerspectiveTransform.Create(unit, corners);
        QrPoint? origin = corners.TopLeft;
        QrPoint? right = corners.TopRight;
        QrPoint? bottom = corners.BottomLeft;
        if (origin == null || right == null || bottom == null)
        {
            return false;
        }

        double ax = right.X - origin.X;
        double ay = right.Y - origin.Y;
        double bx = bottom.X - origin.X;
        double by = bottom.Y - origin.Y;
        double scale = global::System.Math.Max(global::System.Math.Max(global::System.Math.Abs(ax), global::System.Math.Abs(ay)),
            global::System.Math.Max(global::System.Math.Abs(bx), global::System.Math.Abs(by)));
        return ax / scale * (by / scale) - ay / scale * (bx / scale) < 0.0;
    }

    static void ValidateMatrix(QrMatrix matrix, bool requireRoles)
    {
        if (matrix == null)
        {
            throw new QrInputException("The matrix is required.", "matrix");
        }

        CheckedDimensions.ValidateDimensionBounds(matrix.Width, matrix.Height,
            QrLimits.Model2MaximumDimension, QrLimits.Model2MaximumDimension,
            QrLimits.Model2MaximumDimension * QrLimits.Model2MaximumDimension);
        int count = matrix.Width * matrix.Height;
        if (matrix.Values == null || matrix.Values.Length != count)
        {
            throw new QrInputException("The module values must match the grid dimensions.", "Values");
        }

        QrModuleRole[]? roles = matrix.Roles;
        if (requireRoles && roles == null || roles != null && roles.Length != count)
        {
            throw new QrInputException("The pattern evidence requires one role per module.", "Roles");
        }

        for (int index = 0; index < count; index++)
        {
            int value = (int)matrix.Values[index];
            if (value < (int)QrModuleValue.Unknown || value > (int)QrModuleValue.Dark)
            {
                throw new QrInputException("The module value is not defined.", "Values");
            }

            if (roles != null && ((int)roles[index] < (int)QrModuleRole.Unknown
                || (int)roles[index] > (int)QrModuleRole.Remainder))
            {
                throw new QrInputException("The module role is not defined.", "Roles");
            }
        }
    }
}
