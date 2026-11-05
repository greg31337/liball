namespace qrcode;

internal class MatrixTransformer
{
    internal static QrMatrix Rotate(QrMatrix source, int quarterTurnsClockwise)
    {
        ValidateMatrix(source);
        if (quarterTurnsClockwise < 0 || quarterTurnsClockwise > 3)
        {
            throw new QrInputException(
                "The clockwise quarter-turn count must be between zero and three.",
                "quarterTurnsClockwise");
        }

        int width = source.Width;
        int height = source.Height;
        if (quarterTurnsClockwise == 1 || quarterTurnsClockwise == 3)
        {
            width = source.Height;
            height = source.Width;
        }

        QrMatrix result = CreateOutputMatrix(source, width, height);
        for (int row = 0; row < source.Height; row++)
        {
            for (int column = 0; column < source.Width; column++)
            {
                int destinationRow = row;
                int destinationColumn = column;
                if (quarterTurnsClockwise == 1)
                {
                    destinationRow = column;
                    destinationColumn = source.Height - 1 - row;
                }
                else if (quarterTurnsClockwise == 2)
                {
                    destinationRow = source.Height - 1 - row;
                    destinationColumn = source.Width - 1 - column;
                }
                else if (quarterTurnsClockwise == 3)
                {
                    destinationRow = source.Width - 1 - column;
                    destinationColumn = row;
                }

                int sourceIndex = row * source.Width + column;
                int destinationIndex = destinationRow * result.Width + destinationColumn;
                CopyModule(source, sourceIndex, result, destinationIndex);
            }
        }

        SetRotatedQuietZoneExtents(result, source, quarterTurnsClockwise);
        return result;
    }

    internal static QrMatrix Mirror(QrMatrix source)
    {
        ValidateMatrix(source);
        QrMatrix result = CreateOutputMatrix(source, source.Height, source.Width);
        for (int row = 0; row < source.Height; row++)
        {
            for (int column = 0; column < source.Width; column++)
            {
                int sourceIndex = row * source.Width + column;
                int destinationIndex = column * result.Width + row;
                CopyModule(source, sourceIndex, result, destinationIndex);
            }
        }

        result.QuietZoneLeft = source.QuietZoneTop;
        result.QuietZoneTop = source.QuietZoneLeft;
        result.QuietZoneRight = source.QuietZoneBottom;
        result.QuietZoneBottom = source.QuietZoneRight;
        return result;
    }

    internal static QrMatrix InvertPolarity(QrMatrix source)
    {
        ValidateMatrix(source);
        QrMatrix result = CreateOutputMatrix(source, source.Width, source.Height);
        CopyQuietZoneExtents(source, result);
        for (int index = 0; index < source.Values.Length; index++)
        {
            QrModuleValue value = source.Values[index];
            if (value == QrModuleValue.Light)
            {
                value = QrModuleValue.Dark;
            }
            else if (value == QrModuleValue.Dark)
            {
                value = QrModuleValue.Light;
            }

            result.Values[index] = value;
            if (source.Roles != null && result.Roles != null)
            {
                result.Roles[index] = source.Roles[index];
            }

            if (source.Confidence != null && result.Confidence != null)
            {
                result.Confidence[index] = source.Confidence[index];
            }
        }

        return result;
    }

    internal static QrMatrix Crop(
        QrMatrix source,
        int left,
        int top,
        int width,
        int height)
    {
        ValidateMatrix(source);
        if (left < 0)
        {
            throw new QrInputException("The crop left edge cannot be negative.", "left");
        }

        if (top < 0)
        {
            throw new QrInputException("The crop top edge cannot be negative.", "top");
        }

        if (width <= 0)
        {
            throw new QrInputException("The crop width must be positive.", "width");
        }

        if (height <= 0)
        {
            throw new QrInputException("The crop height must be positive.", "height");
        }

        long right = (long)left + width;
        long bottom = (long)top + height;
        if (right > source.Width || bottom > source.Height)
        {
            throw new QrInputException(
                "The crop rectangle extends beyond the matrix.",
                "width");
        }

        int encodingRight = source.Width - source.QuietZoneRight;
        int encodingBottom = source.Height - source.QuietZoneBottom;
        if (left >= encodingRight
            || right <= source.QuietZoneLeft
            || top >= encodingBottom
            || bottom <= source.QuietZoneTop)
        {
            throw new QrInputException(
                "The crop must retain a nonempty portion of the encoding region.",
                "width");
        }

        QrMatrix result = CreateOutputMatrix(source, width, height);
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int sourceIndex = (row + top) * source.Width + column + left;
                int destinationIndex = row * width + column;
                CopyModule(source, sourceIndex, result, destinationIndex);
            }
        }

        if (left < source.QuietZoneLeft)
        {
            result.QuietZoneLeft = source.QuietZoneLeft - left;
        }

        if (top < source.QuietZoneTop)
        {
            result.QuietZoneTop = source.QuietZoneTop - top;
        }

        if (right > encodingRight)
        {
            result.QuietZoneRight = (int)right - encodingRight;
        }

        if (bottom > encodingBottom)
        {
            result.QuietZoneBottom = (int)bottom - encodingBottom;
        }

        return result;
    }

    internal static QrMatrix Scale(QrMatrix source, int scale)
    {
        ValidateMatrix(source);
        if (scale <= 0)
        {
            throw new QrInputException("The matrix scale must be positive.", "scale");
        }

        int width = CheckedDimensions.CalculateProduct(
            source.Width,
            scale,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumWidth");
        int height = CheckedDimensions.CalculateProduct(
            source.Height,
            scale,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumHeight");
        QrMatrix result = CreateOutputMatrix(source, width, height);
        result.QuietZoneLeft = source.QuietZoneLeft * scale;
        result.QuietZoneTop = source.QuietZoneTop * scale;
        result.QuietZoneRight = source.QuietZoneRight * scale;
        result.QuietZoneBottom = source.QuietZoneBottom * scale;
        for (int sourceRow = 0; sourceRow < source.Height; sourceRow++)
        {
            for (int sourceColumn = 0; sourceColumn < source.Width; sourceColumn++)
            {
                int sourceIndex = sourceRow * source.Width + sourceColumn;
                for (int rowOffset = 0; rowOffset < scale; rowOffset++)
                {
                    int destinationRow = sourceRow * scale + rowOffset;
                    for (int columnOffset = 0; columnOffset < scale; columnOffset++)
                    {
                        int destinationColumn = sourceColumn * scale + columnOffset;
                        int destinationIndex = destinationRow * width + destinationColumn;
                        CopyModule(source, sourceIndex, result, destinationIndex);
                    }
                }
            }
        }

        return result;
    }

    internal static int GetStandardQuietZoneWidth(QrSymbolKind symbolKind)
    {
        switch (symbolKind)
        {
            case QrSymbolKind.Model2:
                return QrLimits.Model2QuietZoneModules;
            case QrSymbolKind.Micro:
                return QrLimits.MicroQuietZoneModules;
            case QrSymbolKind.rMQR:
                return QrLimits.RectangularMicroQuietZoneModules;
            case QrSymbolKind.LegacyModel1:
                return QrLimits.LegacyModel1QuietZoneModules;
            default:
                throw new QrInputException(
                    "The symbol kind does not have a defined quiet zone.",
                    "symbolKind");
        }
    }

    internal static QrMatrix AddQuietZone(
        QrMatrix source,
        QrSymbolKind symbolKind,
        int quietZoneWidth)
    {
        ValidateMatrix(source);
        if (quietZoneWidth < -1)
        {
            throw new QrInputException(
                "The quiet-zone width must be nonnegative or -1 for the profile default.",
                "quietZoneWidth");
        }

        int resolvedWidth = quietZoneWidth;
        if (resolvedWidth == -1)
        {
            resolvedWidth = GetStandardQuietZoneWidth(symbolKind);
        }
        else
        {
            ValidateSymbolKind(symbolKind);
        }

        int encodingWidth = GetEncodingRegionWidth(source);
        int encodingHeight = GetEncodingRegionHeight(source);
        int renderedWidth = CheckedDimensions.CalculateSum(
            encodingWidth,
            resolvedWidth,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumWidth");
        renderedWidth = CheckedDimensions.CalculateSum(
            renderedWidth,
            resolvedWidth,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumWidth");
        int renderedHeight = CheckedDimensions.CalculateSum(
            encodingHeight,
            resolvedWidth,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumHeight");
        renderedHeight = CheckedDimensions.CalculateSum(
            renderedHeight,
            resolvedWidth,
            QrLimits.DefaultMaximumImageDimension,
            "MaximumHeight");
        int renderedArea = CheckedDimensions.CalculateArea(
            renderedWidth,
            renderedHeight,
            QrLimits.DefaultMaximumImagePixels);
        ValidateOutputAllocation(source, renderedArea);

        QrMatrix result = new QrMatrix
        {
            Width = renderedWidth,
            Height = renderedHeight,
            Values = new QrModuleValue[renderedArea],
            QuietZoneLeft = resolvedWidth,
            QuietZoneTop = resolvedWidth,
            QuietZoneRight = resolvedWidth,
            QuietZoneBottom = resolvedWidth
        };

        if (source.Roles != null)
        {
            result.Roles = new QrModuleRole[renderedArea];
        }

        if (source.Confidence != null)
        {
            result.Confidence = new byte[renderedArea];
        }

        InitializeQuietZone(result);
        CopyEncodingRegion(source, result, resolvedWidth, resolvedWidth);
        return result;
    }

    internal static QrMatrix RemoveQuietZone(QrMatrix source)
    {
        ValidateMatrix(source);
        int encodingWidth = GetEncodingRegionWidth(source);
        int encodingHeight = GetEncodingRegionHeight(source);
        int encodingArea = CheckedDimensions.CalculateArea(
            encodingWidth,
            encodingHeight,
            QrLimits.DefaultMaximumImagePixels);
        ValidateOutputAllocation(source, encodingArea);

        QrMatrix result = new QrMatrix
        {
            Width = encodingWidth,
            Height = encodingHeight,
            Values = new QrModuleValue[encodingArea]
        };

        if (source.Roles != null)
        {
            result.Roles = new QrModuleRole[encodingArea];
        }

        if (source.Confidence != null)
        {
            result.Confidence = new byte[encodingArea];
        }

        CopyEncodingRegion(source, result, 0, 0);
        return result;
    }

    internal static int GetEncodingRegionWidth(QrMatrix matrix)
    {
        ValidateMatrix(matrix);
        return matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
    }

    internal static int GetEncodingRegionHeight(QrMatrix matrix)
    {
        ValidateMatrix(matrix);
        return matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
    }

    static void ValidateSymbolKind(QrSymbolKind symbolKind)
    {
        int value = (int)symbolKind;
        if (value < (int)QrSymbolKind.Model2
            || value > (int)QrSymbolKind.LegacyModel1)
        {
            throw new QrInputException(
                "The symbol kind does not have a defined quiet zone.",
                "symbolKind");
        }
    }

    static void ValidateMatrix(QrMatrix matrix)
    {
        if (matrix == null)
        {
            throw new QrInputException("The matrix is required.", "matrix");
        }

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
        if (horizontalQuietZone >= matrix.Width
            || verticalQuietZone >= matrix.Height)
        {
            throw new QrInputException(
                "The quiet-zone extents must leave a nonempty encoding region.",
                "matrix");
        }
    }

    static void ValidateOutputAllocation(QrMatrix source, int area)
    {
        int bytesPerModule = 4;
        if (source.Roles != null)
        {
            bytesPerModule += 4;
        }

        if (source.Confidence != null)
        {
            bytesPerModule++;
        }

        CheckedDimensions.CalculateAllocationBytes(
            area,
            bytesPerModule,
            QrLimits.DefaultMaximumAllocationBytes);
    }

    static QrMatrix CreateOutputMatrix(QrMatrix source, int width, int height)
    {
        int area = CheckedDimensions.CalculateArea(
            width,
            height,
            QrLimits.DefaultMaximumImagePixels);
        ValidateOutputAllocation(source, area);
        QrMatrix result = new QrMatrix
        {
            Width = width,
            Height = height,
            Values = new QrModuleValue[area]
        };
        if (source.Roles != null)
        {
            result.Roles = new QrModuleRole[area];
        }

        if (source.Confidence != null)
        {
            result.Confidence = new byte[area];
        }

        return result;
    }

    static void CopyModule(
        QrMatrix source,
        int sourceIndex,
        QrMatrix destination,
        int destinationIndex)
    {
        destination.Values[destinationIndex] = source.Values[sourceIndex];
        if (source.Roles != null && destination.Roles != null)
        {
            destination.Roles[destinationIndex] = source.Roles[sourceIndex];
        }

        if (source.Confidence != null && destination.Confidence != null)
        {
            destination.Confidence[destinationIndex] = source.Confidence[sourceIndex];
        }
    }

    static void SetRotatedQuietZoneExtents(
        QrMatrix result,
        QrMatrix source,
        int quarterTurnsClockwise)
    {
        if (quarterTurnsClockwise == 0)
        {
            CopyQuietZoneExtents(source, result);
        }
        else if (quarterTurnsClockwise == 1)
        {
            result.QuietZoneLeft = source.QuietZoneBottom;
            result.QuietZoneTop = source.QuietZoneLeft;
            result.QuietZoneRight = source.QuietZoneTop;
            result.QuietZoneBottom = source.QuietZoneRight;
        }
        else if (quarterTurnsClockwise == 2)
        {
            result.QuietZoneLeft = source.QuietZoneRight;
            result.QuietZoneTop = source.QuietZoneBottom;
            result.QuietZoneRight = source.QuietZoneLeft;
            result.QuietZoneBottom = source.QuietZoneTop;
        }
        else
        {
            result.QuietZoneLeft = source.QuietZoneTop;
            result.QuietZoneTop = source.QuietZoneRight;
            result.QuietZoneRight = source.QuietZoneBottom;
            result.QuietZoneBottom = source.QuietZoneLeft;
        }
    }

    static void CopyQuietZoneExtents(QrMatrix source, QrMatrix destination)
    {
        destination.QuietZoneLeft = source.QuietZoneLeft;
        destination.QuietZoneTop = source.QuietZoneTop;
        destination.QuietZoneRight = source.QuietZoneRight;
        destination.QuietZoneBottom = source.QuietZoneBottom;
    }

    static void InitializeQuietZone(QrMatrix matrix)
    {
        for (int index = 0; index < matrix.Values.Length; index++)
        {
            matrix.Values[index] = QrModuleValue.Light;
            if (matrix.Roles != null)
            {
                matrix.Roles[index] = QrModuleRole.QuietZone;
            }

            if (matrix.Confidence != null)
            {
                matrix.Confidence[index] = 255;
            }
        }
    }

    static void CopyEncodingRegion(
        QrMatrix source,
        QrMatrix destination,
        int destinationLeft,
        int destinationTop)
    {
        int encodingWidth = source.Width - source.QuietZoneLeft - source.QuietZoneRight;
        int encodingHeight = source.Height - source.QuietZoneTop - source.QuietZoneBottom;
        for (int row = 0; row < encodingHeight; row++)
        {
            int sourceRow = row + source.QuietZoneTop;
            int destinationRow = row + destinationTop;
            for (int column = 0; column < encodingWidth; column++)
            {
                int sourceIndex = sourceRow * source.Width + column + source.QuietZoneLeft;
                int destinationIndex = destinationRow * destination.Width + column + destinationLeft;
                destination.Values[destinationIndex] = source.Values[sourceIndex];
                if (source.Roles != null && destination.Roles != null)
                {
                    destination.Roles[destinationIndex] = source.Roles[sourceIndex];
                }

                if (source.Confidence != null && destination.Confidence != null)
                {
                    destination.Confidence[destinationIndex] = source.Confidence[sourceIndex];
                }
            }
        }
    }
}
