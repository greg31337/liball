namespace qrcode;

internal class RoleMapRenderer
{
    internal static QrRaster RenderRoleMap(QrSymbol symbol, QrRenderOptions options)
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
        QrMatrix matrix = symbol.Matrix;
        int encodingWidth = MatrixTransformer.GetEncodingRegionWidth(matrix);
        int encodingHeight = MatrixTransformer.GetEncodingRegionHeight(matrix);
        if (matrix.Roles == null)
        {
            throw new QrInputException(
                "Module roles are required for role-map rendering.",
                "Roles");
        }

        QrModuleRole[] roles = matrix.Roles;
        ValidateEncodingCells(matrix, roles, encodingWidth, encodingHeight);
        int profileQuietZone = MatrixTransformer.GetStandardQuietZoneWidth(symbol.SymbolKind);
        int quietZone = ResolveQuietZone(options, profileQuietZone);
        int[] damagedIndices = CreateSortedDamageIndices(symbol, matrix.Values.Length);

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

        int damageOffset = 0;
        for (int moduleRow = 0; moduleRow < moduleHeight; moduleRow++)
        {
            int encodingRow = moduleRow - quietZone;
            for (int moduleColumn = 0; moduleColumn < moduleWidth; moduleColumn++)
            {
                int encodingColumn = moduleColumn - quietZone;
                QrModuleRole role = QrModuleRole.QuietZone;
                QrModuleValue value = QrModuleValue.Light;
                bool damaged = false;
                if (encodingRow >= 0
                    && encodingRow < encodingHeight
                    && encodingColumn >= 0
                    && encodingColumn < encodingWidth)
                {
                    int sourceRow = encodingRow + matrix.QuietZoneTop;
                    int sourceColumn = encodingColumn + matrix.QuietZoneLeft;
                    int sourceIndex = sourceRow * matrix.Width + sourceColumn;
                    while (damageOffset < damagedIndices.Length
                        && damagedIndices[damageOffset] < sourceIndex)
                    {
                        damageOffset++;
                    }

                    if (damageOffset < damagedIndices.Length
                        && damagedIndices[damageOffset] == sourceIndex)
                    {
                        damaged = true;
                    }

                    role = roles[sourceIndex];
                    value = matrix.Values[sourceIndex];
                }

                byte pixelValue = GetRoleShade(
                    role,
                    value,
                    damaged,
                    options.HighlightRole,
                    darkValue,
                    lightValue);
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

        int highlightRole = (int)options.HighlightRole;
        if (highlightRole < (int)QrModuleRole.Unknown
            || highlightRole > (int)QrModuleRole.Remainder)
        {
            throw new QrInputException(
                "The highlighted module role is not defined.",
                "HighlightRole");
        }
    }

    static void ValidateEncodingCells(
        QrMatrix matrix,
        QrModuleRole[] roles,
        int encodingWidth,
        int encodingHeight)
    {
        for (int row = 0; row < encodingHeight; row++)
        {
            int sourceRow = row + matrix.QuietZoneTop;
            for (int column = 0; column < encodingWidth; column++)
            {
                int sourceColumn = column + matrix.QuietZoneLeft;
                int sourceIndex = sourceRow * matrix.Width + sourceColumn;
                int value = (int)matrix.Values[sourceIndex];
                if (value < (int)QrModuleValue.Unknown
                    || value > (int)QrModuleValue.Dark)
                {
                    throw new QrInputException(
                        "An encoding-region module value is not defined.",
                        "Values");
                }

                int role = (int)roles[sourceIndex];
                if (role < (int)QrModuleRole.Unknown
                    || role > (int)QrModuleRole.Remainder)
                {
                    throw new QrInputException(
                        "An encoding-region module role is not defined.",
                        "Roles");
                }
            }
        }
    }

    static int[] CreateSortedDamageIndices(QrSymbol symbol, int moduleCount)
    {
        if (symbol.Validation == null)
        {
            return new int[0];
        }

        if (symbol.Validation.DamagedModuleIndices == null)
        {
            throw new QrInputException(
                "The damaged module indices are required.",
                "DamagedModuleIndices");
        }

        if (symbol.Validation.DamagedRoles == null
            || symbol.Validation.DamagedRoles.Length
                != symbol.Validation.DamagedModuleIndices.Length)
        {
            throw new QrInputException(
                "Damaged roles must match the damaged module indices.",
                "DamagedRoles");
        }

        CheckedDimensions.CalculateAllocationBytes(
            symbol.Validation.DamagedModuleIndices.Length,
            4,
            QrLimits.DefaultMaximumAllocationBytes);
        int[] sortedIndices = (int[])symbol.Validation.DamagedModuleIndices.Clone();
        for (int index = 0; index < sortedIndices.Length; index++)
        {
            if (sortedIndices[index] < 0 || sortedIndices[index] >= moduleCount)
            {
                throw new QrInputException(
                    "A damaged module index is outside the matrix.",
                    "DamagedModuleIndices");
            }

            int role = (int)symbol.Validation.DamagedRoles[index];
            if (role < (int)QrModuleRole.Unknown
                || role > (int)QrModuleRole.Remainder)
            {
                throw new QrInputException(
                    "A damaged module role is not defined.",
                    "DamagedRoles");
            }
        }

        global::System.Array.Sort(sortedIndices);
        return sortedIndices;
    }

    static byte GetRoleShade(
        QrModuleRole role,
        QrModuleValue value,
        bool damaged,
        QrModuleRole highlightRole,
        byte darkValue,
        byte lightValue)
    {
        int shadeIndex = 6;
        if (damaged)
        {
            shadeIndex = 5;
        }
        else if (value == QrModuleValue.Unknown)
        {
            shadeIndex = 5;
        }
        else if (role == QrModuleRole.Unknown)
        {
            shadeIndex = 6;
        }
        else if (highlightRole != QrModuleRole.Unknown && role == highlightRole)
        {
            shadeIndex = 0;
        }
        else if (role == QrModuleRole.QuietZone)
        {
            shadeIndex = 7;
        }
        else if ((int)role >= (int)QrModuleRole.Finder
            && (int)role <= (int)QrModuleRole.DarkModule)
        {
            shadeIndex = 1;
        }
        else if (role == QrModuleRole.Data)
        {
            shadeIndex = 2;
        }
        else if (role == QrModuleRole.ErrorCorrection)
        {
            shadeIndex = 3;
        }
        else if (role == QrModuleRole.Remainder)
        {
            shadeIndex = 4;
        }

        return GetCategoryShade(shadeIndex, darkValue, lightValue);
    }

    static byte GetCategoryShade(int shadeIndex, byte darkValue, byte lightValue)
    {
        int darkWeight = 7 - shadeIndex;
        int weightedValue = darkValue * darkWeight + lightValue * shadeIndex;
        return (byte)((weightedValue + 3) / 7);
    }
}
