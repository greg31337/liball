namespace barcode;

internal static class ImageInputValidator
{
    internal static string? FindImageError(BarcodeImage? image)
    {
        if (image == null || image.Pixels == null)
        {
            return "The raw image and its pixel buffer must not be null.";
        }

        if (!IsFiniteNonNegative(image.DpiX) ||
            !IsFiniteNonNegative(image.DpiY))
        {
            return "The raw image resolution must be finite and non-negative.";
        }

        if (image.Width <= 0 || image.Height <= 0)
        {
            return "The raw image dimensions must be greater than zero.";
        }

        long sampleCount = (long)image.Width * image.Height;
        if (sampleCount > DecodingContext.DefaultMaximumModules)
        {
            return "The raw image dimensions exceed the supported size.";
        }

        if (image.Pixels.Length > DecodingContext.DefaultMaximumBytes)
        {
            return "The raw image pixel buffer exceeds the supported size.";
        }

        int bytesPerPixel = ResolveBytesPerPixel(image.PixelFormat);
        if (bytesPerPixel == 0)
        {
            return "The raw image pixel format is invalid.";
        }

        long rowBytes = (long)image.Width * bytesPerPixel;
        if (rowBytes > int.MaxValue)
        {
            return "The raw image row exceeds the supported size.";
        }

        long absoluteStride = image.Stride < 0
            ? -(long)image.Stride
            : image.Stride;
        if (image.Height > 1 && absoluteStride < rowBytes)
        {
            return "The raw image stride is shorter than a pixel row.";
        }

        int row = 0;
        while (row < image.Height)
        {
            long start = (long)image.Offset + ((long)row * image.Stride);
            long end = start + rowBytes;
            if (start < 0L || end < start || end > image.Pixels.Length)
            {
                return "The raw image rows exceed the pixel buffer.";
            }

            row = row + 1;
        }

        return null;
    }

    internal static string? FindDetectionOptionsError(
        BarcodeDetectionOptions? options,
        BarcodeImage image)
    {
        if (options == null)
        {
            return null;
        }

        string? commonError = FindCommonOptionsError(
            options.AllowedFormats,
            options.RegionOfInterest,
            image,
            options.Effort,
            options.BinarizationMode,
            options.LocalizationMode,
            options.Polarity,
            options.MirrorMode,
            options.MaximumDiagnostics);
        if (commonError != null)
        {
            return commonError;
        }

        if (options.MaximumCandidates < 0 ||
            options.ExpectedCandidateCount < 0)
        {
            return "Detection candidate limits must not be negative.";
        }

        return null;
    }

    internal static string? FindSymbolError(BarcodeSymbol? symbol)
    {
        if (symbol == null)
        {
            return "A pure barcode symbol is required.";
        }

        if (!FormatDispatcher.IsSupportedFormat(symbol.Format))
        {
            return "The pure barcode symbol format is unsupported.";
        }

        if (symbol.WidthModules <= 0 || symbol.HeightModules <= 0)
        {
            return "Pure symbol dimensions must be greater than zero.";
        }

        long moduleCount = (long)symbol.WidthModules * symbol.HeightModules;
        if (moduleCount > DecodingContext.DefaultMaximumModules)
        {
            return "Pure symbol dimensions exceed the supported size.";
        }

        if (symbol.Format == BarcodeFormat.Itf14 && symbol.Modules == null)
        {
            if (symbol.Codewords == null ||
                symbol.Codewords.Length != Itf14Profile.CompleteDigitCount)
            {
                return "Pure ITF-14 symbols require fourteen digit codewords.";
            }

            char[] digits = new char[symbol.Codewords.Length];
            int codewordIndex = 0;
            while (codewordIndex < symbol.Codewords.Length)
            {
                int codeword = symbol.Codewords[codewordIndex];
                if (codeword < 0 || codeword > 9)
                {
                    return "Pure ITF-14 codewords must contain decimal digits.";
                }

                digits[codewordIndex] = (char)('0' + codeword);
                codewordIndex = codewordIndex + 1;
            }

            if (!Gs1Modulo10.VerifyCompleteDigits(new string(digits)))
            {
                return "Pure ITF-14 codewords have an invalid check digit.";
            }

            return null;
        }

        if (symbol.Modules == null || symbol.Modules.Length != moduleCount)
        {
            return "Pure symbol module length does not match its dimensions.";
        }

        return null;
    }

    internal static string? FindDecodeOptionsError(
        BarcodeDecodeOptions? options,
        BarcodeImage? image)
    {
        if (options == null)
        {
            return null;
        }

        string? commonError = FindCommonOptionsError(
            options.AllowedFormats,
            options.RegionOfInterest,
            image,
            options.Effort,
            options.BinarizationMode,
            options.LocalizationMode,
            options.Polarity,
            options.MirrorMode,
            options.MaximumDiagnostics);
        if (commonError != null)
        {
            return commonError;
        }

        if (options.MaximumResults < 0 || options.ExpectedResultCount < 0)
        {
            return "Decode result limits must not be negative.";
        }

        if (!IsChecksumModeValid(options.ChecksumMode))
        {
            return "The decode checksum mode is invalid.";
        }

        if (options.AllowedLengths != null)
        {
            int lengthIndex = 0;
            while (lengthIndex < options.AllowedLengths.Length)
            {
                if (options.AllowedLengths[lengthIndex] < 0)
                {
                    return "Allowed decode lengths must not be negative.";
                }

                lengthIndex = lengthIndex + 1;
            }
        }

        if (options.Rotations != null)
        {
            if (options.Rotations.Length == 0)
            {
                return "Requested decode rotations must not be empty.";
            }

            int rotationIndex = 0;
            while (rotationIndex < options.Rotations.Length)
            {
                BarcodeRotation rotation = options.Rotations[rotationIndex];
                if (rotation < BarcodeRotation.None ||
                    rotation > BarcodeRotation.Clockwise270)
                {
                    return "A requested decode rotation is invalid.";
                }

                rotationIndex = rotationIndex + 1;
            }
        }

        return null;
    }

    static string? FindCommonOptionsError(
        BarcodeFormat[]? formats,
        BarcodeRectangle? region,
        BarcodeImage? image,
        BarcodeEffort effort,
        BarcodeBinarizationMode binarization,
        BarcodeLocalizationMode localization,
        BarcodePolarity polarity,
        BarcodeMirrorMode mirror,
        int maximumDiagnostics)
    {
        if (formats != null)
        {
            int formatIndex = 0;
            while (formatIndex < formats.Length)
            {
                BarcodeFormat format = formats[formatIndex];
                if (format != BarcodeFormat.Automatic &&
                    !FormatDispatcher.IsSupportedFormat(format))
                {
                    return "A requested barcode format is unsupported.";
                }

                formatIndex = formatIndex + 1;
            }
        }

        if (effort < BarcodeEffort.Fast || effort > BarcodeEffort.Thorough)
        {
            return "The requested image-processing effort is invalid.";
        }

        if (binarization < BarcodeBinarizationMode.Global ||
            binarization > BarcodeBinarizationMode.Automatic)
        {
            return "The requested binarization mode is invalid.";
        }

        if (localization < BarcodeLocalizationMode.Direct ||
            localization > BarcodeLocalizationMode.Automatic)
        {
            return "The requested localization mode is invalid.";
        }

        if (polarity < BarcodePolarity.DarkOnLight ||
            polarity > BarcodePolarity.Automatic)
        {
            return "The requested barcode polarity is invalid.";
        }

        if (mirror < BarcodeMirrorMode.Normal || mirror > BarcodeMirrorMode.Both)
        {
            return "The requested mirror mode is invalid.";
        }

        if (maximumDiagnostics < 0)
        {
            return "The diagnostic limit must not be negative.";
        }

        if (region != null)
        {
            if (image == null)
            {
                return "An image is required when a region of interest is supplied.";
            }

            string? regionError = FindRegionError(
                region,
                image.Width,
                image.Height);
            if (regionError != null)
            {
                return regionError;
            }
        }

        return null;
    }

    static string? FindRegionError(
        BarcodeRectangle region,
        int width,
        int height)
    {
        if (!IsFinite(region.X) || !IsFinite(region.Y) ||
            !IsFinite(region.Width) || !IsFinite(region.Height))
        {
            return "The image region of interest must be finite.";
        }

        if (region.Width <= 0.0 || region.Height <= 0.0)
        {
            return "The image region of interest must have positive dimensions.";
        }

        double right = region.X + region.Width;
        double bottom = region.Y + region.Height;
        if (region.X < 0.0 || region.Y < 0.0 ||
            right > width || bottom > height)
        {
            return "The image region of interest exceeds the source image.";
        }

        return null;
    }

    static int ResolveBytesPerPixel(BarcodePixelFormat format)
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

    static bool IsChecksumModeValid(BarcodeChecksumMode mode)
    {
        return mode >= BarcodeChecksumMode.None &&
            mode <= BarcodeChecksumMode.VerifyStrip;
    }

    static bool IsFiniteNonNegative(double value)
    {
        return IsFinite(value) && value >= 0.0;
    }

    static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
