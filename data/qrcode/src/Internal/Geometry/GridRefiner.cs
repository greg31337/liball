namespace qrcode;

internal class GridRefiner
{
    internal const int MaximumIterations = 24;

    internal const int MaximumEvaluations = 1 + MaximumIterations * 16;

    // Expected values are supplied by the caller's current pattern/format
    // hypothesis. Unknown values and non-evidence roles never influence the fit.
    // The initial transform maps module cell boundaries into a normalized binary image.
    internal static GridRefinementResult Refine(
        QrRaster raster,
        PerspectiveTransform initial,
        QrMatrix expectedPatterns,
        QrDetectionOptions options)
    {
        BinaryMorphology.ValidateRaster(raster, options);
        if (initial == null)
        {
            throw new QrInputException("The initial grid transform is required.", "initial");
        }

        initial.Invert();
        int[] groupCounts = ValidateEvidence(expectedPatterns, options);
        double[] parameters = new double[8];
        PerspectiveTransform best = CreateTrial(initial, parameters, expectedPatterns.Width, expectedPatterns.Height);
        double bestScore = Score(raster, best, expectedPatterns, groupCounts);
        GridRefinementResult result = new GridRefinementResult
        {
            InitialScore = bestScore,
            Evaluations = 1
        };
        int evidenceCount = groupCounts[0] + groupCounts[1] + groupCounts[2] + groupCounts[3];
        double step = 0.5;
        double bound = global::System.Math.Min(1.5,
            global::System.Math.Min(expectedPatterns.Width, expectedPatterns.Height) * 0.1);
        if (step > bound)
        {
            step = bound;
        }

        // Coordinate descent adjusts translation, scale, shear, and both
        // projective terms. Strict improvement and fixed trial order break ties
        // deterministically; bounds keep the correction close to the initial grid.
        while (options.RefineGrid && evidenceCount > 0 && bestScore < 1.0
            && result.Iterations < MaximumIterations && step >= 0.03125)
        {
            bool improved = false;
            for (int parameter = 0; parameter < parameters.Length; parameter++)
            {
                double original = parameters[parameter];
                double selected = original;
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    double trialValue = original + direction * step;
                    if (global::System.Math.Abs(trialValue) > bound)
                    {
                        continue;
                    }

                    parameters[parameter] = trialValue;
                    PerspectiveTransform trial = CreateTrial(initial, parameters,
                        expectedPatterns.Width, expectedPatterns.Height);
                    double score = Score(raster, trial, expectedPatterns, groupCounts);
                    result.Evaluations++;
                    if (score > bestScore + 1.0E-12)
                    {
                        selected = trialValue;
                        bestScore = score;
                        best = trial;
                        improved = true;
                    }
                }

                parameters[parameter] = selected;
            }

            result.Iterations++;
            if (!improved)
            {
                step /= 2.0;
            }
        }

        result.Transform = best;
        result.FinalScore = bestScore;
        result.Improved = bestScore > result.InitialScore + 1.0E-12;
        result.Matrix = GridSampler.Sample(raster, best,
            expectedPatterns.Width, expectedPatterns.Height, options);
        return result;
    }

    internal static int[] ValidateEvidence(QrMatrix expected, QrDetectionOptions options)
    {
        if (expected == null)
        {
            throw new QrInputException("The pattern evidence is required.", "expectedPatterns");
        }

        // Symbol grids are bounded separately from image pixels so each search
        // has a predictable amount of work, including adversarial evidence maps.
        CheckedDimensions.ValidateDimensionBounds(expected.Width, expected.Height,
            global::System.Math.Min(options.MaximumImageWidth, QrLimits.Model2MaximumDimension),
            global::System.Math.Min(options.MaximumImageHeight, QrLimits.Model2MaximumDimension),
            options.MaximumImagePixels);
        int area = CheckedDimensions.CalculateArea(expected.Width, expected.Height, options.MaximumImagePixels);
        if (expected.Values == null || expected.Values.Length != area)
        {
            throw new QrInputException("The evidence values must match the grid dimensions.", "Values");
        }

        QrModuleRole[]? roles = expected.Roles;
        if (roles == null || roles.Length != area)
        {
            throw new QrInputException("The evidence requires one role per module.", "Roles");
        }

        int[] counts = new int[4];
        for (int index = 0; index < area; index++)
        {
            int value = (int)expected.Values[index];
            int role = (int)roles[index];
            if (value < (int)QrModuleValue.Unknown || value > (int)QrModuleValue.Dark
                || role < (int)QrModuleRole.Unknown || role > (int)QrModuleRole.Remainder)
            {
                throw new QrInputException("The evidence contains an undefined value or role.", "expectedPatterns");
            }

            int group = GetGroup(roles[index]);
            if (group >= 0 && expected.Values[index] != QrModuleValue.Unknown)
            {
                counts[group]++;
            }
        }

        return counts;
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

    static double Score(QrRaster raster, PerspectiveTransform transform,
        QrMatrix expected, int[] counts)
    {
        QrModuleRole[]? roles = expected.Roles;
        if (roles == null)
        {
            return 0.0;
        }

        int[] matches = new int[4];
        QrPoint mapped = new QrPoint();
        for (int index = 0; index < expected.Values.Length; index++)
        {
            int group = GetGroup(roles[index]);
            QrModuleValue value = expected.Values[index];
            if (group < 0 || value == QrModuleValue.Unknown)
            {
                continue;
            }

            // Always probe the interior as well as the center when scoring.
            // This provides evidence of off-center grids even in center-only output mode.
            for (int sample = 0; sample < 5; sample++)
            {
                double x = index % expected.Width + 0.5;
                double y = index / expected.Width + 0.5;
                if (sample == 1)
                {
                    x -= 0.25;
                }
                else if (sample == 2)
                {
                    x += 0.25;
                }
                else if (sample == 3)
                {
                    y -= 0.25;
                }
                else if (sample == 4)
                {
                    y += 0.25;
                }

                if (transform.TryMapPoint(x, y, mapped)
                    && mapped.X >= 0.0 && mapped.Y >= 0.0
                    && mapped.X < raster.Width && mapped.Y < raster.Height)
                {
                    bool dark = raster.Pixels[(int)mapped.Y * raster.Stride + (int)mapped.X] == 0;
                    if (dark == (value == QrModuleValue.Dark))
                    {
                        matches[group]++;
                    }
                }
            }
        }

        double score = 0.0;
        int groups = 0;
        for (int group = 0; group < counts.Length; group++)
        {
            if (counts[group] > 0)
            {
                // Equal weight per available evidence family prevents the many
                // finder cells from overwhelming timing, alignment, or format.
                // Missing/out-of-image probes stay in the denominator.
                score += (double)matches[group] / (counts[group] * 5);
                groups++;
            }
        }

        return groups == 0 ? 0.0 : score / groups;
    }

    static PerspectiveTransform CreateTrial(PerspectiveTransform initial,
        double[] parameters, int width, int height)
    {
        double maximumDimension = global::System.Math.Max(width, height);
        double a = 1.0 + parameters[2] / width;
        double b = parameters[4] / height;
        double c = parameters[0];
        double d = parameters[5] / width;
        double e = 1.0 + parameters[3] / height;
        double f = parameters[1];
        double g = parameters[6] / (maximumDimension * width);
        double h = parameters[7] / (maximumDimension * height);
        // Compose a small correction in module space with the initial image
        // homography. Parameter bounds keep this correction nonsingular.
        return new PerspectiveTransform
        {
            M00 = initial.M00 * a + initial.M01 * d + initial.M02 * g,
            M01 = initial.M00 * b + initial.M01 * e + initial.M02 * h,
            M02 = initial.M00 * c + initial.M01 * f + initial.M02,
            M10 = initial.M10 * a + initial.M11 * d + initial.M12 * g,
            M11 = initial.M10 * b + initial.M11 * e + initial.M12 * h,
            M12 = initial.M10 * c + initial.M11 * f + initial.M12,
            M20 = initial.M20 * a + initial.M21 * d + initial.M22 * g,
            M21 = initial.M20 * b + initial.M21 * e + initial.M22 * h,
            M22 = initial.M20 * c + initial.M21 * f + initial.M22
        };
    }
}
