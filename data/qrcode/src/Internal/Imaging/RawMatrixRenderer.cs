namespace qrcode;

internal class RawMatrixRenderer
{
    internal static QrRaster RenderSymbol(QrSymbol symbol, QrRenderOptions options)
    {
        if (symbol == null)
        {
            throw new QrInputException("The symbol is required.", "symbol");
        }

        if (symbol.Matrix == null)
        {
            throw new QrInputException("The symbol matrix is required.", "Matrix");
        }

        ValidateOptions(options);
        if (options.RenderRoleMap)
        {
            return RoleMapRenderer.RenderRoleMap(symbol, options);
        }

        ValidateMatrix(symbol.Matrix);
        int profileQuietZone = MatrixTransformer.GetStandardQuietZoneWidth(symbol.SymbolKind);
        int quietZone = ResolveQuietZone(options, profileQuietZone);
        int encodingWidth = symbol.Matrix.Width
            - symbol.Matrix.QuietZoneLeft
            - symbol.Matrix.QuietZoneRight;
        int encodingHeight = symbol.Matrix.Height
            - symbol.Matrix.QuietZoneTop
            - symbol.Matrix.QuietZoneBottom;
        ValidateEncodingValues(symbol.Matrix, encodingWidth, encodingHeight);

        int moduleWidth = CheckedDimensions.CalculateSum(
            encodingWidth,
            quietZone,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumWidth");
        moduleWidth = CheckedDimensions.CalculateSum(
            moduleWidth,
            quietZone,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumWidth");
        int moduleHeight = CheckedDimensions.CalculateSum(
            encodingHeight,
            quietZone,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumHeight");
        moduleHeight = CheckedDimensions.CalculateSum(
            moduleHeight,
            quietZone,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumHeight");
        int pixelWidth = CheckedDimensions.CalculateProduct(
            moduleWidth,
            options.Scale,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumWidth");
        int pixelHeight = CheckedDimensions.CalculateProduct(
            moduleHeight,
            options.Scale,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumHeight");
        int pixelCount = CheckedDimensions.CalculateArea(
            pixelWidth,
            pixelHeight,
            QrLimits.DefaultMaximumImagePixels);
        CheckedDimensions.CalculateAllocationBytes(
            pixelCount,
            1,
            QrLimits.DefaultMaximumAllocationBytes);

        QrRaster raster = new QrRaster
        {
            Pixels = new byte[pixelCount],
            Width = pixelWidth,
            Height = pixelHeight,
            Stride = pixelWidth
        };
        byte darkValue = options.DarkValue;
        byte lightValue = options.LightValue;
        if (options.Invert)
        {
            darkValue = options.LightValue;
            lightValue = options.DarkValue;
        }

        for (int moduleRow = 0; moduleRow < moduleHeight; moduleRow++)
        {
            int encodingRow = moduleRow - quietZone;
            for (int moduleColumn = 0; moduleColumn < moduleWidth; moduleColumn++)
            {
                int encodingColumn = moduleColumn - quietZone;
                byte pixelValue = lightValue;
                if (encodingRow >= 0
                    && encodingRow < encodingHeight
                    && encodingColumn >= 0
                    && encodingColumn < encodingWidth)
                {
                    int sourceRow = encodingRow + symbol.Matrix.QuietZoneTop;
                    int sourceColumn = encodingColumn + symbol.Matrix.QuietZoneLeft;
                    int sourceIndex = sourceRow * symbol.Matrix.Width + sourceColumn;
                    if (symbol.Matrix.Values[sourceIndex] == QrModuleValue.Dark)
                    {
                        pixelValue = darkValue;
                    }
                }

                for (int rowOffset = 0; rowOffset < options.Scale; rowOffset++)
                {
                    int pixelRow = moduleRow * options.Scale + rowOffset;
                    for (int columnOffset = 0; columnOffset < options.Scale; columnOffset++)
                    {
                        int pixelColumn = moduleColumn * options.Scale + columnOffset;
                        raster.Pixels[pixelRow * raster.Stride + pixelColumn] = pixelValue;
                    }
                }
            }
        }

        return raster;
    }

    static int ResolveQuietZone(QrRenderOptions options, int profileQuietZone)
    {
        if (!options.IncludeQuietZone)
        {
            return 0;
        }

        if (options.QuietZoneModules == -1)
        {
            return profileQuietZone;
        }

        return options.QuietZoneModules;
    }

    static void ValidateOptions(QrRenderOptions options)
    {
        if (options == null)
        {
            throw new QrInputException("The render options are required.", "options");
        }

        if (options.Scale <= 0)
        {
            throw new QrInputException("The render scale must be positive.", "Scale");
        }

        if (options.QuietZoneModules < -1)
        {
            throw new QrInputException(
                "The quiet-zone width must be nonnegative or -1 for the profile default.",
                "QuietZoneModules");
        }
    }

    static void ValidateMatrix(QrMatrix matrix)
    {
        CheckedDimensions.ValidateDimensionBounds(
            matrix.Width,
            matrix.Height,
            QrLimits.DefaultMaximumImageDimension,
            QrLimits.DefaultMaximumImageDimension,
            QrLimits.DefaultMaximumImagePixels);
        int area = CheckedDimensions.CalculateArea(
            matrix.Width,
            matrix.Height,
            QrLimits.DefaultMaximumImagePixels);
        if (matrix.Values == null || matrix.Values.Length != area)
        {
            throw new QrInputException(
                "The module values must match the declared matrix dimensions.",
                "Values");
        }

        if (matrix.Roles != null && matrix.Roles.Length != area)
        {
            throw new QrInputException(
                "The module roles must match the declared matrix dimensions.",
                "Roles");
        }

        if (matrix.Confidence != null && matrix.Confidence.Length != area)
        {
            throw new QrInputException(
                "The module confidence values must match the declared matrix dimensions.",
                "Confidence");
        }

        if (matrix.QuietZoneLeft < 0
            || matrix.QuietZoneTop < 0
            || matrix.QuietZoneRight < 0
            || matrix.QuietZoneBottom < 0)
        {
            throw new QrInputException(
                "Quiet-zone extents cannot be negative.",
                "matrix");
        }

        long horizontalQuietZone = (long)matrix.QuietZoneLeft + matrix.QuietZoneRight;
        long verticalQuietZone = (long)matrix.QuietZoneTop + matrix.QuietZoneBottom;
        if (horizontalQuietZone >= matrix.Width || verticalQuietZone >= matrix.Height)
        {
            throw new QrInputException(
                "The quiet-zone extents must leave a nonempty encoding region.",
                "matrix");
        }
    }

    static void ValidateEncodingValues(
        QrMatrix matrix,
        int encodingWidth,
        int encodingHeight)
    {
        for (int row = 0; row < encodingHeight; row++)
        {
            int sourceRow = row + matrix.QuietZoneTop;
            for (int column = 0; column < encodingWidth; column++)
            {
                int sourceColumn = column + matrix.QuietZoneLeft;
                QrModuleValue value = matrix.Values[sourceRow * matrix.Width + sourceColumn];
                if (value != QrModuleValue.Light && value != QrModuleValue.Dark)
                {
                    throw new QrInputException(
                        "Every encoding-region module must be light or dark.",
                        "Values");
                }
            }
        }
    }
}
