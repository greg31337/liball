namespace qrcode;

internal class ModuleClassifier
{
    internal static QrModuleRole[] ClassifyModules(
        QrMatrix matrix,
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex)
    {
        ValidateMatrix(matrix);
        int encodingWidth = matrix.Width - matrix.QuietZoneLeft - matrix.QuietZoneRight;
        int encodingHeight = matrix.Height - matrix.QuietZoneTop - matrix.QuietZoneBottom;
        QrModuleRole[] encodingRoles = CreateEncodingRoleMap(
            symbolKind,
            version,
            errorCorrectionIndex);
        int expectedWidth = GetEncodingWidth(symbolKind, version);
        int expectedHeight = GetEncodingHeight(symbolKind, version);
        if (encodingWidth != expectedWidth || encodingHeight != expectedHeight)
        {
            throw new QrInputException(
                "The matrix encoding region does not match the selected symbol profile.",
                "matrix");
        }

        int area = CheckedDimensions.CalculateArea(
            matrix.Width,
            matrix.Height,
            QrLimits.DefaultMaximumImagePixels);
        CheckedDimensions.CalculateAllocationBytes(
            area,
            4,
            QrLimits.DefaultMaximumAllocationBytes);
        QrModuleRole[] result = new QrModuleRole[area];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = QrModuleRole.QuietZone;
        }

        for (int row = 0; row < encodingHeight; row++)
        {
            int sourceOffset = row * encodingWidth;
            int destinationOffset = (row + matrix.QuietZoneTop) * matrix.Width
                + matrix.QuietZoneLeft;
            for (int column = 0; column < encodingWidth; column++)
            {
                result[destinationOffset + column] = encodingRoles[sourceOffset + column];
            }
        }

        matrix.Roles = result;
        return result;
    }

    internal static QrModuleRole[] CreateEncodingRoleMap(
        QrSymbolKind symbolKind,
        int version,
        int errorCorrectionIndex)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return CreateModel2RoleMap(version, errorCorrectionIndex);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return CreateMicroRoleMap(version, errorCorrectionIndex);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return CreateRectangularMicroRoleMap(version, errorCorrectionIndex);
        }

        if (symbolKind == QrSymbolKind.LegacyModel1)
        {
            throw new QrUnsupportedFeatureException(
                "Legacy Model 1 role classification requires the decode-only matrix parser.",
                QrFailureReason.InvalidInput,
                "LegacyModel1RoleClassification",
                version);
        }

        throw new QrInputException("The symbol kind is not defined.", "symbolKind");
    }

    internal static QrModuleRole[] CreateModel2RoleMap(
        int version,
        int errorCorrectionIndex)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        int dataBitCount = EccBlockTable.GetDataCodewordCount(
            version,
            errorCorrectionIndex) * 8;
        int codewordBitCount = Model2VersionTable.GetTotalCodewordCount(version) * 8;
        QrModuleRole[] roles = new QrModuleRole[dimension * dimension];
        for (int finderIndex = 0; finderIndex < 3; finderIndex++)
        {
            DrawFinderRoles(
                roles,
                dimension,
                dimension,
                Model2PlacementTable.GetFinderCenterRow(version, finderIndex),
                Model2PlacementTable.GetFinderCenterColumn(version, finderIndex));
        }

        for (int index = 8; index < dimension - 8; index++)
        {
            SetRole(roles, dimension, Model2PlacementTable.TimingPatternIndex, index, QrModuleRole.Timing);
            SetRole(roles, dimension, index, Model2PlacementTable.TimingPatternIndex, QrModuleRole.Timing);
        }

        int[] alignmentPositions = AlignmentPositionTable.GetPositions(version);
        for (int rowIndex = 0; rowIndex < alignmentPositions.Length; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < alignmentPositions.Length; columnIndex++)
            {
                bool overlapsFinder = rowIndex == 0
                    && (columnIndex == 0 || columnIndex == alignmentPositions.Length - 1)
                    || rowIndex == alignmentPositions.Length - 1 && columnIndex == 0;
                if (!overlapsFinder)
                {
                    DrawSquareRole(
                        roles,
                        dimension,
                        alignmentPositions[rowIndex],
                        alignmentPositions[columnIndex],
                        2,
                        QrModuleRole.Alignment);
                }
            }
        }

        for (int bitIndex = 0; bitIndex < 15; bitIndex++)
        {
            SetRole(
                roles,
                dimension,
                Model2PlacementTable.GetFormatPrimaryRow(bitIndex),
                Model2PlacementTable.GetFormatPrimaryColumn(bitIndex),
                QrModuleRole.Format);
            SetRole(
                roles,
                dimension,
                Model2PlacementTable.GetFormatSecondaryRow(version, bitIndex),
                Model2PlacementTable.GetFormatSecondaryColumn(version, bitIndex),
                QrModuleRole.Format);
        }

        SetRole(
            roles,
            dimension,
            Model2PlacementTable.GetDarkModuleRow(version),
            Model2PlacementTable.GetDarkModuleColumn(version),
            QrModuleRole.DarkModule);
        if (version >= 7)
        {
            for (int bitIndex = 0; bitIndex < 18; bitIndex++)
            {
                SetRole(
                    roles,
                    dimension,
                    Model2PlacementTable.GetVersionPrimaryRow(version, bitIndex),
                    Model2PlacementTable.GetVersionPrimaryColumn(version, bitIndex),
                    QrModuleRole.Version);
                SetRole(
                    roles,
                    dimension,
                    Model2PlacementTable.GetVersionSecondaryRow(version, bitIndex),
                    Model2PlacementTable.GetVersionSecondaryColumn(version, bitIndex),
                    QrModuleRole.Version);
            }
        }

        AssignModel2PayloadRoles(
            roles,
            version,
            dataBitCount,
            codewordBitCount);
        return roles;
    }

    internal static QrModuleRole[] CreateMicroRoleMap(
        int version,
        int errorCorrectionIndex)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        int dataBitCount = MicroVersionTable.GetDataBitCapacity(
            version,
            errorCorrectionIndex);
        int codewordBitCount = dataBitCount
            + MicroVersionTable.GetEccCodewordCount(version, errorCorrectionIndex) * 8;
        QrModuleRole[] roles = new QrModuleRole[dimension * dimension];
        DrawFinderRoles(
            roles,
            dimension,
            dimension,
            MicroPlacementTable.GetFinderCenterRow(version),
            MicroPlacementTable.GetFinderCenterColumn(version));
        for (int index = MicroPlacementTable.TimingStartIndex;
            index < dimension;
            index++)
        {
            SetRole(roles, dimension, 0, index, QrModuleRole.Timing);
            SetRole(roles, dimension, index, 0, QrModuleRole.Timing);
        }

        for (int bitIndex = 0; bitIndex <= 7; bitIndex++)
        {
            SetRole(
                roles,
                dimension,
                MicroPlacementTable.GetFormatVerticalRow(bitIndex),
                MicroPlacementTable.GetFormatVerticalColumn(bitIndex),
                QrModuleRole.Format);
        }

        for (int bitIndex = 7; bitIndex < 15; bitIndex++)
        {
            SetRole(
                roles,
                dimension,
                MicroPlacementTable.GetFormatHorizontalRow(bitIndex),
                MicroPlacementTable.GetFormatHorizontalColumn(bitIndex),
                QrModuleRole.Format);
        }

        AssignMicroPayloadRoles(
            roles,
            version,
            dataBitCount,
            codewordBitCount);
        return roles;
    }

    internal static QrModuleRole[] CreateRectangularMicroRoleMap(
        int versionIndex,
        int errorCorrectionIndex)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        int dataBitCount = RectangularMicroVersionTable.GetDataBitCapacity(
            versionIndex,
            errorCorrectionIndex);
        int codewordBitCount = RectangularMicroVersionTable.GetTotalCodewordCount(
            versionIndex) * 8;
        QrModuleRole[] roles = new QrModuleRole[width * height];
        DrawFinderRoles(
            roles,
            width,
            height,
            RectangularMicroPlacementTable.GetFinderCenterRow(versionIndex),
            RectangularMicroPlacementTable.GetFinderCenterColumn(versionIndex));
        DrawSquareRole(
            roles,
            width,
            RectangularMicroPlacementTable.GetSubfinderCenterRow(versionIndex),
            RectangularMicroPlacementTable.GetSubfinderCenterColumn(versionIndex),
            2,
            QrModuleRole.Finder);
        DrawRectangularMicroCornerRoles(roles, versionIndex);

        int[] alignmentColumns = RectangularMicroAlignmentTable.GetCenterColumns(width);
        for (int centerIndex = 0; centerIndex < alignmentColumns.Length; centerIndex++)
        {
            int centerColumn = alignmentColumns[centerIndex];
            DrawRectangularMicroAlignmentRole(
                roles,
                width,
                height,
                centerColumn);
        }

        DrawRectangularMicroTimingRoles(
            roles,
            versionIndex,
            alignmentColumns);
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            SetRole(
                roles,
                width,
                RectangularMicroPlacementTable.GetFinderSideFormatRow(bitIndex),
                RectangularMicroPlacementTable.GetFinderSideFormatColumn(bitIndex),
                QrModuleRole.Format);
            SetRole(
                roles,
                width,
                RectangularMicroPlacementTable.GetSubfinderSideFormatRow(
                    versionIndex,
                    bitIndex),
                RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(
                    versionIndex,
                    bitIndex),
                QrModuleRole.Format);
        }

        AssignRectangularMicroPayloadRoles(
            roles,
            versionIndex,
            dataBitCount,
            codewordBitCount);
        return roles;
    }

    static int GetEncodingWidth(QrSymbolKind symbolKind, int version)
    {
        if (symbolKind == QrSymbolKind.Model2)
        {
            return Model2VersionTable.GetDimension(version);
        }

        if (symbolKind == QrSymbolKind.Micro)
        {
            return MicroVersionTable.GetDimension(version);
        }

        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.GetWidth(version);
        }

        throw new QrInputException("The symbol kind is not classifiable.", "symbolKind");
    }

    static int GetEncodingHeight(QrSymbolKind symbolKind, int version)
    {
        if (symbolKind == QrSymbolKind.rMQR)
        {
            return RectangularMicroVersionTable.GetHeight(version);
        }

        return GetEncodingWidth(symbolKind, version);
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

    static void DrawFinderRoles(
        QrModuleRole[] roles,
        int width,
        int height,
        int centerRow,
        int centerColumn)
    {
        for (int rowOffset = -4; rowOffset <= 4; rowOffset++)
        {
            for (int columnOffset = -4; columnOffset <= 4; columnOffset++)
            {
                int row = centerRow + rowOffset;
                int column = centerColumn + columnOffset;
                if (row >= 0 && row < height && column >= 0 && column < width)
                {
                    int rowDistance = AbsoluteValue(rowOffset);
                    int columnDistance = AbsoluteValue(columnOffset);
                    int distance = rowDistance;
                    if (columnDistance > distance)
                    {
                        distance = columnDistance;
                    }

                    QrModuleRole role = QrModuleRole.Finder;
                    if (distance == 4)
                    {
                        role = QrModuleRole.Separator;
                    }

                    SetRole(roles, width, row, column, role);
                }
            }
        }
    }

    static void DrawSquareRole(
        QrModuleRole[] roles,
        int width,
        int centerRow,
        int centerColumn,
        int radius,
        QrModuleRole role)
    {
        for (int rowOffset = -radius; rowOffset <= radius; rowOffset++)
        {
            for (int columnOffset = -radius; columnOffset <= radius; columnOffset++)
            {
                SetRole(
                    roles,
                    width,
                    centerRow + rowOffset,
                    centerColumn + columnOffset,
                    role);
            }
        }
    }

    static void DrawRectangularMicroCornerRoles(
        QrModuleRole[] roles,
        int versionIndex)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        for (int column = 0; column < 3; column++)
        {
            SetRole(roles, width, height - 1, column, QrModuleRole.Finder);
        }

        if (height >= 11)
        {
            SetRole(roles, width, height - 2, 0, QrModuleRole.Finder);
            SetRole(roles, width, height - 2, 1, QrModuleRole.Finder);
        }

        SetRole(roles, width, 0, width - 1, QrModuleRole.Finder);
        SetRole(roles, width, 0, width - 2, QrModuleRole.Finder);
        SetRole(roles, width, 1, width - 1, QrModuleRole.Finder);
        SetRole(roles, width, 1, width - 2, QrModuleRole.Finder);
    }

    static void DrawRectangularMicroAlignmentRole(
        QrModuleRole[] roles,
        int width,
        int height,
        int centerColumn)
    {
        for (int rowOffset = 0; rowOffset < 3; rowOffset++)
        {
            for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
            {
                SetRole(
                    roles,
                    width,
                    rowOffset,
                    centerColumn + columnOffset,
                    QrModuleRole.Alignment);
                SetRole(
                    roles,
                    width,
                    height - 1 - rowOffset,
                    centerColumn + columnOffset,
                    QrModuleRole.Alignment);
            }
        }
    }

    static void DrawRectangularMicroTimingRoles(
        QrModuleRole[] roles,
        int versionIndex,
        int[] alignmentColumns)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        for (int column = 0; column < width; column++)
        {
            SetRoleWhenUnknown(roles, width, 0, column, QrModuleRole.Timing);
            SetRoleWhenUnknown(roles, width, height - 1, column, QrModuleRole.Timing);
        }

        for (int timingIndex = -1; timingIndex <= alignmentColumns.Length; timingIndex++)
        {
            int column = 0;
            if (timingIndex >= 0 && timingIndex < alignmentColumns.Length)
            {
                column = alignmentColumns[timingIndex];
            }
            else if (timingIndex == alignmentColumns.Length)
            {
                column = width - 1;
            }

            for (int row = 0; row < height; row++)
            {
                SetRoleWhenUnknown(roles, width, row, column, QrModuleRole.Timing);
            }
        }
    }

    static void AssignModel2PayloadRoles(
        QrModuleRole[] roles,
        int version,
        int dataBitCount,
        int codewordBitCount)
    {
        int placementIndex = 0;
        int dimension = Model2VersionTable.GetDimension(version);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateModel2ReservedMap(version);
        int pairCount = Model2PlacementTable.GetDataColumnPairCount(version);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = Model2PlacementTable.GetDataColumnPairRight(version, pairIndex);
            bool upward = Model2PlacementTable.IsDataColumnPairUpward(version, pairIndex);
            for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
            {
                int row = upward ? dimension - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (!reserved.GetModule(row, column))
                    {
                        SetPayloadRole(
                            roles,
                            dimension,
                            row,
                            column,
                            placementIndex,
                            dataBitCount,
                            codewordBitCount);
                        placementIndex++;
                    }
                }
            }
        }
    }

    static void AssignMicroPayloadRoles(
        QrModuleRole[] roles,
        int version,
        int dataBitCount,
        int codewordBitCount)
    {
        int placementIndex = 0;
        int dimension = MicroVersionTable.GetDimension(version);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateMicroReservedMap(version);
        int pairCount = MicroPlacementTable.GetDataColumnPairCount(version);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = MicroPlacementTable.GetDataColumnPairRight(version, pairIndex);
            bool upward = MicroPlacementTable.IsDataColumnPairUpward(version, pairIndex);
            for (int rowIndex = 0; rowIndex < dimension; rowIndex++)
            {
                int row = upward ? dimension - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (!reserved.GetModule(row, column))
                    {
                        SetPayloadRole(
                            roles,
                            dimension,
                            row,
                            column,
                            placementIndex,
                            dataBitCount,
                            codewordBitCount);
                        placementIndex++;
                    }
                }
            }
        }
    }

    static void AssignRectangularMicroPayloadRoles(
        QrModuleRole[] roles,
        int versionIndex,
        int dataBitCount,
        int codewordBitCount)
    {
        int placementIndex = 0;
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        BitMatrixBuffer reserved = FunctionPatternBuilder.CreateRectangularMicroReservedMap(
            versionIndex);
        int pairCount = RectangularMicroPlacementTable.GetDataColumnPairCount(versionIndex);
        for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            int rightColumn = RectangularMicroPlacementTable.GetDataColumnPairRight(
                versionIndex,
                pairIndex);
            bool upward = RectangularMicroPlacementTable.IsDataColumnPairUpward(
                versionIndex,
                pairIndex);
            for (int rowIndex = 0; rowIndex < height; rowIndex++)
            {
                int row = upward ? height - 1 - rowIndex : rowIndex;
                for (int columnOffset = 0; columnOffset < 2; columnOffset++)
                {
                    int column = rightColumn - columnOffset;
                    if (!reserved.GetModule(row, column))
                    {
                        SetPayloadRole(
                            roles,
                            width,
                            row,
                            column,
                            placementIndex,
                            dataBitCount,
                            codewordBitCount);
                        placementIndex++;
                    }
                }
            }
        }
    }

    static void SetPayloadRole(
        QrModuleRole[] roles,
        int width,
        int row,
        int column,
        int placementIndex,
        int dataBitCount,
        int codewordBitCount)
    {
        QrModuleRole role = QrModuleRole.Remainder;
        if (placementIndex < dataBitCount)
        {
            role = QrModuleRole.Data;
        }
        else if (placementIndex < codewordBitCount)
        {
            role = QrModuleRole.ErrorCorrection;
        }

        SetRole(roles, width, row, column, role);
    }

    static void SetRoleWhenUnknown(
        QrModuleRole[] roles,
        int width,
        int row,
        int column,
        QrModuleRole role)
    {
        int index = row * width + column;
        if (roles[index] == QrModuleRole.Unknown)
        {
            roles[index] = role;
        }
    }

    static void SetRole(
        QrModuleRole[] roles,
        int width,
        int row,
        int column,
        QrModuleRole role)
    {
        roles[row * width + column] = role;
    }

    static int AbsoluteValue(int value)
    {
        if (value < 0)
        {
            return -value;
        }

        return value;
    }
}
