namespace barcode;

internal class ImageValidationState
{
    internal BarcodeValidationMode Mode;
    internal string Standard = "Barcode image quality";
    internal string? Profile;
    internal bool RecoveryDiagnosticObserved;
    internal global::System.Collections.Generic.List<BarcodeValidationIssue> Issues;
    internal global::System.Collections.Generic.List<BarcodeQualityMetric> Metrics;
    internal global::System.Collections.Generic.List<BarcodeDiagnostic> Diagnostics;

    internal ImageValidationState(BarcodeValidationMode mode)
    {
        Mode = mode;
        Issues = new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        Metrics = new global::System.Collections.Generic.List<BarcodeQualityMetric>();
        Diagnostics = new global::System.Collections.Generic.List<BarcodeDiagnostic>();
    }
}

internal static class ImageQualityValidator
{
    internal const double MinimumContrast = 0.30;
    internal const double MinimumModulation = 0.25;
    internal const double MinimumModuleConsistency = 0.75;
    internal const double MaximumSkewDegrees = 5.0;
    internal const double MaximumPerspectiveDistortion = 0.15;
    internal const double MaximumAxialNonuniformity = 0.15;
    internal const double MinimumFixedPatternIntegrity = 0.50;
    internal const double MinimumEccMargin = 0.25;
    internal const double MinimumDecodeConfidence = 0.40;
    internal const int MaximumDecodeDiagnostics = 32;

    internal static BarcodeValidationReport ValidateImageRequest(
        BarcodeValidationRequest request)
    {
        if (request == null)
        {
            throw new BarcodeValidationException(
                "A barcode image validation request is required.");
        }

        if (request.Mode != BarcodeValidationMode.Strict &&
            request.Mode != BarcodeValidationMode.Tolerant)
        {
            throw new BarcodeValidationException(
                "Barcode validation mode is invalid.");
        }

        ImageValidationState state = new ImageValidationState(request.Mode);
        BarcodeImage? image = request.Image;
        if (image == null)
        {
            AddImageIssue(
                state,
                "IMAGE_MISSING",
                "A raw barcode image is required for image validation.",
                "Image",
                "a barcode image",
                "null");
            return CreateImageReport(state, request.Mode);
        }

        ValidateImageEnvelope(state, image);
        ValidateRequestedImageFormat(state, request.Format);
        if (state.Issues.Count != 0)
        {
            return CreateImageReport(state, request.Mode);
        }

        ManagedGrayImage gray = ImageIngestion.ConvertToGray8(image);
        ImageHistogramAnalysis histogram = HistogramAnalyzer.Analyze(gray);
        double imageContrast = (double)histogram.Contrast / 255.0;
        AddImageMetric(
            state,
            "image_contrast",
            imageContrast,
            MinimumContrast,
            imageContrast >= MinimumContrast,
            "Global luminance range normalized to zero through one.");
        if (imageContrast < MinimumContrast)
        {
            AddImageIssue(
                state,
                "IMAGE_CONTRAST_INSUFFICIENT",
                "The image does not provide sufficient luminance contrast.",
                "Image.Pixels",
                MinimumContrast.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                imageContrast.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        BarcodeDecodeAttempt attempt = DecodeValidationImage(
            image,
            request.Format);
        CopyDecodeDiagnostics(state, attempt.Diagnostics);
        BarcodeDecodeResult[] results = attempt.Results == null
            ? new BarcodeDecodeResult[0]
            : attempt.Results;
        if (!attempt.Success || results.Length == 0)
        {
            ValidateUndecodedTruncation(state, gray, histogram);
            AddImageIssue(
                state,
                "IMAGE_NOT_DECODED",
                "The image did not contain a decodable requested barcode.",
                "Image.Pixels",
                "a decodable barcode",
                attempt.FailureCode == null ? "no result" : attempt.FailureCode);
            return CreateImageReport(state, request.Mode);
        }

        BarcodeDecodeResult result = results[0];
        ResolveImageStandard(state, result.Format);
        if (request.Format != BarcodeFormat.Automatic &&
            result.Format != request.Format)
        {
            AddImageIssue(
                state,
                "IMAGE_FORMAT_CONFLICT",
                "The decoded image format conflicts with the requested format.",
                "Format",
                request.Format.ToString(),
                result.Format.ToString());
        }

        AnalyzeDecodedImage(state, gray, histogram, result);
        return CreateImageReport(state, request.Mode);
    }

    static void ValidateImageEnvelope(
        ImageValidationState state,
        BarcodeImage image)
    {
        if (image.Pixels == null)
        {
            AddImageIssue(
                state,
                "IMAGE_PIXELS_MISSING",
                "The raw image pixel buffer is required.",
                "Image.Pixels",
                "a pixel buffer",
                "null");
            return;
        }

        if (image.Width <= 0 || image.Height <= 0)
        {
            AddImageIssue(
                state,
                "IMAGE_DIMENSIONS_INVALID",
                "Image dimensions must be positive.",
                "Image.WidthHeight",
                "positive dimensions",
                $"{image.Width}x{image.Height}");
        }

        long moduleCount = (long)image.Width * image.Height;
        if (moduleCount > DecodingContext.DefaultMaximumModules)
        {
            AddImageIssue(
                state,
                "IMAGE_CAPACITY_EXCEEDED",
                "The image dimensions exceed the supported sample capacity.",
                "Image.WidthHeight",
                $"at most {DecodingContext.DefaultMaximumModules} samples",
                moduleCount.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (image.Pixels.Length > DecodingContext.DefaultMaximumBytes)
        {
            AddImageIssue(
                state,
                "IMAGE_CAPACITY_EXCEEDED",
                "The raw image pixel buffer exceeds the supported byte capacity.",
                "Image.Pixels",
                $"at most {DecodingContext.DefaultMaximumBytes} bytes",
                image.Pixels.Length.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!IsFiniteImageValue(image.DpiX) || image.DpiX < 0.0 ||
            !IsFiniteImageValue(image.DpiY) || image.DpiY < 0.0)
        {
            AddImageIssue(
                state,
                "IMAGE_DPI_INVALID",
                "Image resolution must be finite and non-negative.",
                "Image.DpiXDpiY",
                "finite non-negative resolution",
                $"{image.DpiX}x{image.DpiY}");
        }

        int bytesPerPixel = ResolveValidationBytesPerPixel(image.PixelFormat);
        if (bytesPerPixel == 0)
        {
            AddImageIssue(
                state,
                "IMAGE_PIXEL_FORMAT_INVALID",
                "The raw image pixel format is invalid.",
                "Image.PixelFormat",
                "a supported raw pixel format",
                ((int)image.PixelFormat).ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        long rowBytes = (long)image.Width * bytesPerPixel;
        long absoluteStride = image.Stride < 0
            ? -(long)image.Stride
            : image.Stride;
        if (rowBytes > int.MaxValue ||
            (image.Height > 1 && absoluteStride < rowBytes))
        {
            AddImageIssue(
                state,
                "IMAGE_STRIDE_INVALID",
                "The image stride is shorter than a complete pixel row.",
                "Image.Stride",
                rowBytes.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                image.Stride.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        int row = 0;
        while (row < image.Height)
        {
            long start = (long)image.Offset + ((long)row * image.Stride);
            long end = start + rowBytes;
            if (start < 0L || end < start || end > image.Pixels.Length)
            {
                AddImageIssue(
                    state,
                    "IMAGE_BUFFER_BOUNDS_INVALID",
                    "An image row falls outside the supplied pixel buffer.",
                    "Image.OffsetStride",
                    "every row inside Image.Pixels",
                    row.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
                return;
            }

            row = row + 1;
        }
    }

    static int ResolveValidationBytesPerPixel(BarcodePixelFormat format)
    {
        switch (format)
        {
            case BarcodePixelFormat.Gray8:
                return 1;
            case BarcodePixelFormat.Rgb24:
            case BarcodePixelFormat.Bgr24:
                return 3;
            case BarcodePixelFormat.Rgba32:
            case BarcodePixelFormat.Bgra32:
                return 4;
            default:
                return 0;
        }
    }

    static bool IsFiniteImageValue(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    static void ValidateRequestedImageFormat(
        ImageValidationState state,
        BarcodeFormat format)
    {
        if (format != BarcodeFormat.Automatic &&
            !FormatDispatcher.IsSupportedFormat(format))
        {
            AddImageIssue(
                state,
                "IMAGE_FORMAT_UNSUPPORTED",
                "The requested barcode image format is unsupported.",
                "Format",
                "Automatic or a supported barcode format",
                ((int)format).ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static BarcodeDecodeAttempt DecodeValidationImage(
        BarcodeImage image,
        BarcodeFormat format)
    {
        BarcodeDecodeOptions options = new BarcodeDecodeOptions();
        if (format != BarcodeFormat.Automatic)
        {
            options.AllowedFormats = new BarcodeFormat[] { format };
        }

        options.MaximumResults = 1;
        options.ExpectedResultCount = 1;
        options.Polarity = BarcodePolarity.Automatic;
        options.MirrorMode = BarcodeMirrorMode.Both;
        options.Effort = BarcodeEffort.Balanced;
        options.BinarizationMode = BarcodeBinarizationMode.Automatic;
        options.LocalizationMode = BarcodeLocalizationMode.Automatic;
        options.MaximumDiagnostics = MaximumDecodeDiagnostics;
        BarcodeDecodeRequest decode = new BarcodeDecodeRequest();
        decode.Image = image;
        decode.Options = options;
        return BarcodeDecodePipeline.TryDecode(decode);
    }

    static void CopyDecodeDiagnostics(
        ImageValidationState state,
        BarcodeDiagnostic[]? source)
    {
        if (source == null)
        {
            return;
        }

        int index = 0;
        while (index < source.Length)
        {
            string? code = source[index].Code;
            if (code == DiagnosticCodes.PreprocessSmoothed ||
                code == DiagnosticCodes.PreprocessSharpened ||
                code == DiagnosticCodes.PreprocessNormalized ||
                code == DiagnosticCodes.ErrorCorrection)
            {
                state.RecoveryDiagnosticObserved = true;
            }

            state.Diagnostics.Add(ResultCopier.CopyDiagnostic(source[index]));
            index = index + 1;
        }
    }

    internal static void AnalyzeDecodedImage(
        ImageValidationState state,
        ManagedGrayImage image,
        ImageHistogramAnalysis histogram,
        BarcodeDecodeResult result)
    {
        BarcodeQuadrilateral? location = result.Location;
        if (location == null)
        {
            AddImageIssue(
                state,
                "IMAGE_BOUNDS_MISSING",
                "The decoded barcode does not contain image-space bounds.",
                "Image.Location",
                "a detected quadrilateral",
                "null");
            return;
        }

        BarcodeRectangle bounds = GeometryMath.CalculateQuadrilateralBounds(location);
        double coverage = bounds.Width * bounds.Height /
            ((double)image.Width * image.Height);
        bool boundsValid = bounds.X >= 0.0 && bounds.Y >= 0.0 &&
            bounds.Width > 0.0 && bounds.Height > 0.0 &&
            bounds.X + bounds.Width <= image.Width &&
            bounds.Y + bounds.Height <= image.Height;
        AddImageMetric(
            state,
            "detected_bounds_coverage",
            coverage,
            1.0,
            boundsValid,
            "Fraction of the source image covered by the detected symbol bounds.");
        if (!boundsValid)
        {
            AddImageIssue(
                state,
                "IMAGE_BOUNDS_INVALID",
                "The detected barcode bounds fall outside the source image.",
                "Image.Location",
                "bounds inside the source image",
                $"{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}");
            return;
        }

        double moduleSize = EstimateImageModuleSize(
            image,
            bounds,
            histogram.OtsuThreshold,
            result.Polarity);
        if (moduleSize < 1.0)
        {
            moduleSize = 1.0;
        }

        double moduleConsistency = CalculateModuleConsistency(
            image,
            bounds,
            histogram.OtsuThreshold,
            result.Polarity,
            moduleSize);
        AddImageMetric(
            state,
            "module_size_consistency",
            moduleConsistency,
            MinimumModuleConsistency,
            moduleConsistency >= MinimumModuleConsistency,
            "Run widths compared with integral multiples of the estimated module size.");
        if (moduleConsistency < MinimumModuleConsistency)
        {
            AddImageIssue(
                state,
                "IMAGE_MODULE_SIZE_INCONSISTENT",
                "Observed module runs are not sufficiently consistent.",
                "Image.Pixels",
                MinimumModuleConsistency.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                moduleConsistency.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double quietZone = CalculateQuietZoneClearance(
            image,
            bounds,
            histogram.OtsuThreshold,
            result.Polarity,
            result.Dimension,
            moduleSize);
        double requiredQuietZone = ResolveRequiredQuietZone(result);
        AddImageMetric(
            state,
            "quiet_zone_clearance_modules",
            quietZone,
            requiredQuietZone,
            quietZone >= requiredQuietZone,
            "Minimum clear image margin around the detected symbol in estimated modules.");
        if (quietZone < requiredQuietZone)
        {
            AddImageIssue(
                state,
                "IMAGE_QUIET_ZONE_INFRINGEMENT",
                "The detected symbol does not have the required clear margin.",
                "Image.Location",
                requiredQuietZone.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                quietZone.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double truncationMargin = CalculateTruncationMargin(
            image,
            bounds,
            result.Dimension,
            moduleSize);
        AddImageMetric(
            state,
            "truncation_margin_modules",
            truncationMargin,
            requiredQuietZone,
            truncationMargin >= requiredQuietZone,
            "Minimum available source-image margin around the detected symbol.");
        if (truncationMargin < requiredQuietZone)
        {
            AddImageIssue(
                state,
                "IMAGE_TRUNCATED",
                "The source image does not contain sufficient margin around the symbol.",
                "Image.Location",
                requiredQuietZone.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                truncationMargin.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double skew = global::System.Math.Abs(result.SkewDegrees);
        AddImageMetric(
            state,
            "skew_degrees",
            skew,
            MaximumSkewDegrees,
            skew <= MaximumSkewDegrees,
            "Absolute estimated in-plane skew angle.");
        if (skew > MaximumSkewDegrees)
        {
            AddImageIssue(
                state,
                "IMAGE_SKEW_EXCESSIVE",
                "The decoded symbol exceeds the supported skew threshold.",
                "Image.Location",
                MaximumSkewDegrees.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                skew.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double perspective = CalculatePerspectiveDistortion(location);
        AddImageMetric(
            state,
            "perspective_distortion",
            perspective,
            MaximumPerspectiveDistortion,
            perspective <= MaximumPerspectiveDistortion,
            "Maximum relative difference between opposite quadrilateral edges.");
        if (perspective > MaximumPerspectiveDistortion)
        {
            AddImageIssue(
                state,
                "IMAGE_PERSPECTIVE_EXCESSIVE",
                "The decoded symbol has excessive perspective distortion.",
                "Image.Location",
                MaximumPerspectiveDistortion.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                perspective.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double axial = CalculateAxialNonuniformity(location);
        AddImageMetric(
            state,
            "axial_grid_nonuniformity_proxy",
            axial,
            MaximumAxialNonuniformity,
            axial <= MaximumAxialNonuniformity,
            "Opposite-edge disagreement used as an axial or grid nonuniformity proxy.");
        if (axial > MaximumAxialNonuniformity)
        {
            AddImageIssue(
                state,
                "IMAGE_AXIAL_NONUNIFORMITY",
                "The decoded symbol has excessive axial or grid nonuniformity.",
                "Image.Location",
                MaximumAxialNonuniformity.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                axial.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double symbolContrast = CalculateBoundsContrast(image, bounds);
        AddImageMetric(
            state,
            "symbol_contrast",
            symbolContrast,
            MinimumContrast,
            symbolContrast >= MinimumContrast,
            "Luminance range inside the detected symbol bounds.");
        if (symbolContrast < MinimumContrast)
        {
            AddImageIssue(
                state,
                "IMAGE_SYMBOL_CONTRAST_INSUFFICIENT",
                "The detected symbol has insufficient local contrast.",
                "Image.Location",
                MinimumContrast.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                symbolContrast.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double modulation = symbolContrast * moduleConsistency;
        AddImageMetric(
            state,
            "modulation_proxy",
            modulation,
            MinimumModulation,
            modulation >= MinimumModulation,
            "Local contrast reduced by observed module-width inconsistency.");
        if (modulation < MinimumModulation)
        {
            AddImageIssue(
                state,
                "IMAGE_MODULATION_INSUFFICIENT",
                "The symbol modulation proxy is below the diagnostic threshold.",
                "Image.Pixels",
                MinimumModulation.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                modulation.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        AnalyzeDecodeIntegrity(state, result);
    }

    static double EstimateImageModuleSize(
        ManagedGrayImage image,
        BarcodeRectangle bounds,
        int threshold,
        BarcodePolarity polarity)
    {
        int left = ClampImageCoordinate(
            (int)global::System.Math.Floor(bounds.X),
            image.Width);
        int right = ClampImageCoordinate(
            (int)global::System.Math.Ceiling(bounds.X + bounds.Width) - 1,
            image.Width);
        int top = ClampImageCoordinate(
            (int)global::System.Math.Floor(bounds.Y),
            image.Height);
        int bottom = ClampImageCoordinate(
            (int)global::System.Math.Ceiling(bounds.Y + bounds.Height) - 1,
            image.Height);
        int middleY = (top + bottom) / 2;
        int middleX = (left + right) / 2;
        int horizontal = FindMinimumImageRun(
            image,
            left,
            right,
            middleY,
            true,
            threshold,
            polarity);
        int vertical = FindMinimumImageRun(
            image,
            top,
            bottom,
            middleX,
            false,
            threshold,
            polarity);
        if (horizontal == 0)
        {
            return vertical;
        }

        if (vertical == 0)
        {
            return horizontal;
        }

        return horizontal < vertical ? horizontal : vertical;
    }

    static int FindMinimumImageRun(
        ManagedGrayImage image,
        int start,
        int end,
        int fixedCoordinate,
        bool horizontal,
        int threshold,
        BarcodePolarity polarity)
    {
        int minimum = int.MaxValue;
        int runStart = start;
        bool current = ReadImageDark(
            image,
            horizontal ? start : fixedCoordinate,
            horizontal ? fixedCoordinate : start,
            threshold,
            polarity);
        int position = start + 1;
        while (position <= end)
        {
            bool value = ReadImageDark(
                image,
                horizontal ? position : fixedCoordinate,
                horizontal ? fixedCoordinate : position,
                threshold,
                polarity);
            if (value != current)
            {
                int length = position - runStart;
                if (length < minimum)
                {
                    minimum = length;
                }

                current = value;
                runStart = position;
            }

            position = position + 1;
        }

        int finalLength = end - runStart + 1;
        if (finalLength < minimum)
        {
            minimum = finalLength;
        }

        return minimum == int.MaxValue ? 0 : minimum;
    }

    static double CalculateModuleConsistency(
        ManagedGrayImage image,
        BarcodeRectangle bounds,
        int threshold,
        BarcodePolarity polarity,
        double moduleSize)
    {
        int left = ClampImageCoordinate(
            (int)global::System.Math.Floor(bounds.X),
            image.Width);
        int right = ClampImageCoordinate(
            (int)global::System.Math.Ceiling(bounds.X + bounds.Width) - 1,
            image.Width);
        int middleY = ClampImageCoordinate(
            (int)global::System.Math.Floor(bounds.Y + (bounds.Height * 0.5)),
            image.Height);
        int runStart = left;
        bool current = ReadImageDark(
            image,
            left,
            middleY,
            threshold,
            polarity);
        double error = 0.0;
        int count = 0;
        int position = left + 1;
        while (position <= right)
        {
            bool value = ReadImageDark(
                image,
                position,
                middleY,
                threshold,
                polarity);
            if (value != current)
            {
                error = error + CalculateImageRunError(
                    position - runStart,
                    moduleSize);
                count = count + 1;
                current = value;
                runStart = position;
            }

            position = position + 1;
        }

        error = error + CalculateImageRunError(
            right - runStart + 1,
            moduleSize);
        count = count + 1;
        if (count == 0)
        {
            return 0.0;
        }

        double score = 1.0 - (error / count);
        return ClampImageScore(score);
    }

    static double CalculateImageRunError(int length, double moduleSize)
    {
        double multiple = global::System.Math.Floor(
            ((double)length / moduleSize) + 0.5);
        if (multiple < 1.0)
        {
            multiple = 1.0;
        }

        double expected = multiple * moduleSize;
        return global::System.Math.Abs(length - expected) / moduleSize;
    }

    static double CalculateQuietZoneClearance(
        ManagedGrayImage image,
        BarcodeRectangle bounds,
        int threshold,
        BarcodePolarity polarity,
        BarcodeDimension dimension,
        double moduleSize)
    {
        int left = ClampImageCoordinate(
            (int)global::System.Math.Floor(bounds.X),
            image.Width);
        int right = ClampImageCoordinate(
            (int)global::System.Math.Ceiling(bounds.X + bounds.Width) - 1,
            image.Width);
        int top = ClampImageCoordinate(
            (int)global::System.Math.Floor(bounds.Y),
            image.Height);
        int bottom = ClampImageCoordinate(
            (int)global::System.Math.Ceiling(bounds.Y + bounds.Height) - 1,
            image.Height);
        int middleX = (left + right) / 2;
        int middleY = (top + bottom) / 2;
        int leftClear = CountClearImagePixels(
            image,
            left - 1,
            middleY,
            -1,
            0,
            threshold,
            polarity);
        int rightClear = CountClearImagePixels(
            image,
            right + 1,
            middleY,
            1,
            0,
            threshold,
            polarity);
        int minimum = leftClear < rightClear ? leftClear : rightClear;
        if (dimension != BarcodeDimension.Linear)
        {
            int topClear = CountClearImagePixels(
                image,
                middleX,
                top - 1,
                0,
                -1,
                threshold,
                polarity);
            int bottomClear = CountClearImagePixels(
                image,
                middleX,
                bottom + 1,
                0,
                1,
                threshold,
                polarity);
            if (topClear < minimum)
            {
                minimum = topClear;
            }

            if (bottomClear < minimum)
            {
                minimum = bottomClear;
            }
        }

        return minimum / moduleSize;
    }

    static int CountClearImagePixels(
        ManagedGrayImage image,
        int x,
        int y,
        int stepX,
        int stepY,
        int threshold,
        BarcodePolarity polarity)
    {
        int count = 0;
        while (x >= 0 && x < image.Width && y >= 0 && y < image.Height)
        {
            if (ReadImageDark(image, x, y, threshold, polarity))
            {
                break;
            }

            count = count + 1;
            x = x + stepX;
            y = y + stepY;
        }

        return count;
    }

    static bool ReadImageDark(
        ManagedGrayImage image,
        int x,
        int y,
        int threshold,
        BarcodePolarity polarity)
    {
        int value = image.ReadSample(x, y);
        if (polarity == BarcodePolarity.LightOnDark)
        {
            return value > threshold;
        }

        return value <= threshold;
    }

    static double ResolveRequiredQuietZone(BarcodeDecodeResult result)
    {
        if (result.Dimension == BarcodeDimension.Linear)
        {
            return 10.0;
        }

        if (result.Dimension == BarcodeDimension.StackedLinear)
        {
            return 2.0;
        }

        return 1.0;
    }

    static double CalculateTruncationMargin(
        ManagedGrayImage image,
        BarcodeRectangle bounds,
        BarcodeDimension dimension,
        double moduleSize)
    {
        double left = bounds.X;
        double right = image.Width - (bounds.X + bounds.Width);
        double minimum = left < right ? left : right;
        if (dimension != BarcodeDimension.Linear)
        {
            double top = bounds.Y;
            double bottom = image.Height - (bounds.Y + bounds.Height);
            if (top < minimum)
            {
                minimum = top;
            }

            if (bottom < minimum)
            {
                minimum = bottom;
            }
        }

        return minimum / moduleSize;
    }

    static double CalculatePerspectiveDistortion(BarcodeQuadrilateral location)
    {
        BarcodePoint topLeft = GeometryMath.RequirePoint(location.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(location.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(location.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(location.BottomLeft);
        double top = GeometryMath.DistanceBetweenPoints(topLeft, topRight);
        double bottom = GeometryMath.DistanceBetweenPoints(bottomLeft, bottomRight);
        double left = GeometryMath.DistanceBetweenPoints(topLeft, bottomLeft);
        double right = GeometryMath.DistanceBetweenPoints(topRight, bottomRight);
        double horizontal = CalculateOppositeEdgeDifference(top, bottom);
        double vertical = CalculateOppositeEdgeDifference(left, right);
        return horizontal > vertical ? horizontal : vertical;
    }

    static double CalculateAxialNonuniformity(BarcodeQuadrilateral location)
    {
        BarcodePoint topLeft = GeometryMath.RequirePoint(location.TopLeft);
        BarcodePoint topRight = GeometryMath.RequirePoint(location.TopRight);
        BarcodePoint bottomRight = GeometryMath.RequirePoint(location.BottomRight);
        BarcodePoint bottomLeft = GeometryMath.RequirePoint(location.BottomLeft);
        double firstDiagonal = GeometryMath.DistanceBetweenPoints(
            topLeft,
            bottomRight);
        double secondDiagonal = GeometryMath.DistanceBetweenPoints(
            topRight,
            bottomLeft);
        return CalculateOppositeEdgeDifference(
            firstDiagonal,
            secondDiagonal);
    }

    static double CalculateOppositeEdgeDifference(double first, double second)
    {
        double maximum = first > second ? first : second;
        if (maximum <= GeometryMath.Epsilon)
        {
            return 1.0;
        }

        return global::System.Math.Abs(first - second) / maximum;
    }

    static double CalculateBoundsContrast(
        ManagedGrayImage image,
        BarcodeRectangle bounds)
    {
        int left = ClampImageCoordinate(
            (int)global::System.Math.Floor(bounds.X),
            image.Width);
        int right = ClampImageCoordinate(
            (int)global::System.Math.Ceiling(bounds.X + bounds.Width) - 1,
            image.Width);
        int top = ClampImageCoordinate(
            (int)global::System.Math.Floor(bounds.Y),
            image.Height);
        int bottom = ClampImageCoordinate(
            (int)global::System.Math.Ceiling(bounds.Y + bounds.Height) - 1,
            image.Height);
        int minimum = 255;
        int maximum = 0;
        int y = top;
        while (y <= bottom)
        {
            int x = left;
            while (x <= right)
            {
                int value = image.ReadSample(x, y);
                if (value < minimum)
                {
                    minimum = value;
                }

                if (value > maximum)
                {
                    maximum = value;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return (double)(maximum - minimum) / 255.0;
    }

    static int ClampImageCoordinate(int value, int limit)
    {
        if (value < 0)
        {
            return 0;
        }

        if (value >= limit)
        {
            return limit - 1;
        }

        return value;
    }

    static void AnalyzeDecodeIntegrity(
        ImageValidationState state,
        BarcodeDecodeResult result)
    {
        BarcodeDecodeMetadata? metadata = result.Metadata;
        bool correctionApplied = metadata != null &&
            (metadata.ErrorCorrectionApplied ||
             metadata.CorrectedErrorCount > 0 ||
             metadata.ErasureCount > 0);
        bool orientationRecovered = result.MirrorMode == BarcodeMirrorMode.Mirrored ||
            result.Polarity == BarcodePolarity.LightOnDark;
        if (correctionApplied || orientationRecovered ||
            state.RecoveryDiagnosticObserved)
        {
            AddImageIssue(
                state,
                "IMAGE_RECOVERY_APPLIED",
                "The decoder used a recovery operation to obtain the image result.",
                "Image.Pixels",
                "a result decoded without recovery",
                ResolveImageRecoveryDescription(
                    correctionApplied,
                    orientationRecovered,
                    state.RecoveryDiagnosticObserved));
        }

        double confidence = metadata == null
            ? 0.0
            : ClampImageScore(metadata.Confidence);
        AddImageMetric(
            state,
            "decode_confidence",
            confidence,
            MinimumDecodeConfidence,
            confidence >= MinimumDecodeConfidence,
            "Heuristic confidence reported by the image decoder.");
        if (confidence < MinimumDecodeConfidence)
        {
            AddImageIssue(
                state,
                "IMAGE_DECODE_CONFIDENCE_LOW",
                "The decoded image confidence is below the diagnostic threshold.",
                "Image.Pixels",
                MinimumDecodeConfidence.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                confidence.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double integrity = CalculateFixedPatternIntegrity(result, metadata);
        AddImageMetric(
            state,
            "fixed_pattern_integrity",
            integrity,
            MinimumFixedPatternIntegrity,
            integrity >= MinimumFixedPatternIntegrity,
            "Checksum, correction burden, and decode confidence integrity proxy.");
        if (integrity < MinimumFixedPatternIntegrity)
        {
            AddImageIssue(
                state,
                "IMAGE_FIXED_PATTERN_DAMAGED",
                "The symbol fixed-pattern integrity proxy is below the diagnostic threshold.",
                "Image.Pixels",
                MinimumFixedPatternIntegrity.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                integrity.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        double eccMargin = CalculateUnusedEccMargin(metadata);
        AddImageMetric(
            state,
            "unused_ecc_margin",
            eccMargin,
            MinimumEccMargin,
            eccMargin >= MinimumEccMargin,
            "Approximate unused checksum or error-correction capacity.");
        if (eccMargin < MinimumEccMargin)
        {
            AddImageIssue(
                state,
                "IMAGE_ECC_MARGIN_LOW",
                "The estimated unused error-correction margin is too low.",
                "Image.Pixels",
                MinimumEccMargin.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                eccMargin.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static string ResolveImageRecoveryDescription(
        bool correctionApplied,
        bool orientationRecovered,
        bool diagnosticObserved)
    {
        if (correctionApplied)
        {
            return "checksum or error-correction recovery";
        }

        if (orientationRecovered)
        {
            return "polarity or mirror recovery";
        }

        return diagnosticObserved
            ? "preprocessing recovery"
            : "recovery";
    }

    static double CalculateFixedPatternIntegrity(
        BarcodeDecodeResult result,
        BarcodeDecodeMetadata? metadata)
    {
        if (metadata == null)
        {
            return 0.5;
        }

        if (metadata.HasChecksum)
        {
            return metadata.ChecksumValid ? 1.0 : 0.0;
        }

        if (result.Dimension == BarcodeDimension.Matrix ||
            result.Dimension == BarcodeDimension.StackedLinear)
        {
            return CalculateUnusedEccMargin(metadata);
        }

        return ClampImageScore(metadata.Confidence);
    }

    static double CalculateUnusedEccMargin(BarcodeDecodeMetadata? metadata)
    {
        if (metadata == null)
        {
            return 0.5;
        }

        if (metadata.HasChecksum)
        {
            return metadata.ChecksumValid ? 1.0 : 0.0;
        }

        int total = metadata.Codewords == null
            ? 0
            : metadata.Codewords.Length;
        if (total <= 0)
        {
            return ClampImageScore(metadata.Confidence);
        }

        int burden = (metadata.CorrectedErrorCount * 2) +
            metadata.ErasureCount;
        return ClampImageScore(1.0 - ((double)burden / total));
    }

    static void ValidateUndecodedTruncation(
        ImageValidationState state,
        ManagedGrayImage image,
        ImageHistogramAnalysis histogram)
    {
        bool darkOnEdge = false;
        int x = 0;
        while (x < image.Width)
        {
            if (image.ReadSample(x, 0) <= histogram.OtsuThreshold ||
                image.ReadSample(x, image.Height - 1) <= histogram.OtsuThreshold)
            {
                darkOnEdge = true;
            }

            x = x + 1;
        }

        int y = 0;
        while (y < image.Height)
        {
            if (image.ReadSample(0, y) <= histogram.OtsuThreshold ||
                image.ReadSample(image.Width - 1, y) <= histogram.OtsuThreshold)
            {
                darkOnEdge = true;
            }

            y = y + 1;
        }

        if (darkOnEdge)
        {
            AddImageIssue(
                state,
                "IMAGE_TRUNCATED",
                "Dark symbol evidence reaches the source-image boundary.",
                "Image.Pixels",
                "a clear image boundary",
                "dark boundary samples");
        }
    }

    static double ClampImageScore(double value)
    {
        if (!IsFiniteImageValue(value) || value < 0.0)
        {
            return 0.0;
        }

        return value > 1.0 ? 1.0 : value;
    }

    static void ResolveImageStandard(
        ImageValidationState state,
        BarcodeFormat format)
    {
        if (format == BarcodeFormat.Code128 || format == BarcodeFormat.Gs1128)
        {
            state.Standard = BarcodeStandards.Code128;
        }
        else if (format == BarcodeFormat.Ean8 || format == BarcodeFormat.Ean13 ||
            format == BarcodeFormat.UpcA || format == BarcodeFormat.UpcE ||
            format == BarcodeFormat.EanUpcAddon2 ||
            format == BarcodeFormat.EanUpcAddon5)
        {
            state.Standard = BarcodeStandards.EanUpc;
        }
        else if (format == BarcodeFormat.Code39)
        {
            state.Standard = BarcodeStandards.Code39;
        }
        else if (format == BarcodeFormat.Itf || format == BarcodeFormat.Itf14)
        {
            state.Standard = BarcodeStandards.Itf;
        }
        else if (format == BarcodeFormat.Codabar)
        {
            state.Standard = BarcodeStandards.Codabar;
        }
        else if (format == BarcodeFormat.DataMatrix ||
            format == BarcodeFormat.Gs1DataMatrix)
        {
            state.Standard = BarcodeStandards.DataMatrix;
        }
        else if (format == BarcodeFormat.Pdf417 ||
            format == BarcodeFormat.CompactPdf417 ||
            format == BarcodeFormat.MacroPdf417)
        {
            state.Standard = BarcodeStandards.Pdf417;
        }

        if (format == BarcodeFormat.Gs1128 ||
            format == BarcodeFormat.Gs1DataMatrix ||
            format == BarcodeFormat.Itf14)
        {
            state.Profile = BarcodeStandards.Gs1GeneralSpecifications;
        }
    }

    static void AddImageMetric(
        ImageValidationState state,
        string name,
        double value,
        double threshold,
        bool meetsThreshold,
        string interpretation)
    {
        BarcodeQualityMetric metric = new BarcodeQualityMetric();
        metric.Name = name;
        metric.Value = value;
        metric.Threshold = threshold;
        metric.MeetsThreshold = meetsThreshold;
        metric.IsHeuristic = true;
        metric.Interpretation = interpretation;
        state.Metrics.Add(metric);

        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        diagnostic.Stage = "ImageValidation";
        diagnostic.Code = ConvertMetricNameToCode(name);
        diagnostic.Message = interpretation;
        diagnostic.HasNumericEvidence = true;
        diagnostic.Value = value;
        diagnostic.Threshold = threshold;
        state.Diagnostics.Add(diagnostic);
    }

    static string ConvertMetricNameToCode(string name)
    {
        char[] characters = new char[name.Length];
        int index = 0;
        while (index < name.Length)
        {
            char value = name[index];
            characters[index] = value >= 'a' && value <= 'z'
                ? (char)(value - 32)
                : value;
            index = index + 1;
        }

        return new string(characters);
    }

    static void AddImageIssue(
        ImageValidationState state,
        string code,
        string message,
        string field,
        string? expected,
        string? observed)
    {
        string standard = state.Profile == null
            ? state.Standard
            : state.Profile;
        state.Issues.Add(ValidationIssueFactory.Create(
            ValidationModePolicy.ResolveSeverity(state.Mode, code),
            code,
            message,
            standard,
            field,
            -1,
            expected,
            observed));
    }

    static BarcodeValidationReport CreateImageReport(
        ImageValidationState state,
        BarcodeValidationMode mode)
    {
        BarcodeValidationIssue[] issues =
            new BarcodeValidationIssue[state.Issues.Count];
        int issueIndex = 0;
        while (issueIndex < issues.Length)
        {
            issues[issueIndex] = ValidationIssueFactory.CopyWithOffset(
                state.Issues[issueIndex],
                state.Issues[issueIndex].Offset);
            issueIndex = issueIndex + 1;
        }

        BarcodeQualityMetric[] metrics =
            new BarcodeQualityMetric[state.Metrics.Count];
        int metricIndex = 0;
        while (metricIndex < metrics.Length)
        {
            BarcodeQualityMetric source = state.Metrics[metricIndex];
            BarcodeQualityMetric copy = new BarcodeQualityMetric();
            copy.Name = source.Name;
            copy.Value = source.Value;
            copy.Threshold = source.Threshold;
            copy.MeetsThreshold = source.MeetsThreshold;
            copy.IsHeuristic = source.IsHeuristic;
            copy.Interpretation = source.Interpretation;
            metrics[metricIndex] = copy;
            metricIndex = metricIndex + 1;
        }

        BarcodeDiagnostic[] diagnostics =
            new BarcodeDiagnostic[state.Diagnostics.Count];
        int diagnosticIndex = 0;
        while (diagnosticIndex < diagnostics.Length)
        {
            diagnostics[diagnosticIndex] = ResultCopier.CopyDiagnostic(
                state.Diagnostics[diagnosticIndex]);
            diagnosticIndex = diagnosticIndex + 1;
        }

        BarcodeValidationReport report = new BarcodeValidationReport();
        report.IsConformant = ValidationModePolicy.IsConformant(issues);
        report.Mode = mode;
        report.Standard = state.Standard;
        report.Profile = state.Profile;
        report.Issues = issues;
        report.Metrics = metrics;
        report.Diagnostics = diagnostics;
        return report;
    }
}
