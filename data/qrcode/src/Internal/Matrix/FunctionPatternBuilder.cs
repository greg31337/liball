namespace qrcode;

internal class FunctionPatternBuilder
{
    internal static BitMatrixBuffer CreateModel2FunctionPatterns(
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        int formatCode = FormatInformationCodec.CreateModel2Code(
            errorCorrectionIndex,
            mask);
        BitMatrixBuffer modules = new BitMatrixBuffer(dimension, dimension);
        BitMatrixBuffer reserved = new BitMatrixBuffer(dimension, dimension);
        DrawModel2Patterns(version, formatCode, modules, reserved);
        return modules;
    }

    internal static BitMatrixBuffer CreateModel2ReservedMap(int version)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        BitMatrixBuffer modules = new BitMatrixBuffer(dimension, dimension);
        BitMatrixBuffer reserved = new BitMatrixBuffer(dimension, dimension);
        DrawModel2Patterns(version, 0, modules, reserved);
        return reserved;
    }

    internal static BitMatrixBuffer CreateMicroFunctionPatterns(
        int version,
        int errorCorrectionIndex,
        int mask)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        int formatCode = FormatInformationCodec.CreateMicroCode(
            version,
            errorCorrectionIndex,
            mask);
        BitMatrixBuffer modules = new BitMatrixBuffer(dimension, dimension);
        BitMatrixBuffer reserved = new BitMatrixBuffer(dimension, dimension);
        DrawMicroPatterns(version, formatCode, modules, reserved);
        return modules;
    }

    internal static BitMatrixBuffer CreateMicroReservedMap(int version)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        BitMatrixBuffer modules = new BitMatrixBuffer(dimension, dimension);
        BitMatrixBuffer reserved = new BitMatrixBuffer(dimension, dimension);
        DrawMicroPatterns(version, 0, modules, reserved);
        return reserved;
    }

    internal static BitMatrixBuffer CreateRectangularMicroFunctionPatterns(
        int versionIndex,
        int errorCorrectionIndex)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        int finderCode = FormatInformationCodec.CreateRectangularMicroFinderCode(
            versionIndex,
            errorCorrectionIndex);
        int subfinderCode = FormatInformationCodec.CreateRectangularMicroSubfinderCode(
            versionIndex,
            errorCorrectionIndex);
        BitMatrixBuffer modules = new BitMatrixBuffer(width, height);
        BitMatrixBuffer reserved = new BitMatrixBuffer(width, height);
        DrawRectangularMicroPatterns(
            versionIndex,
            finderCode,
            subfinderCode,
            modules,
            reserved);
        return modules;
    }

    internal static BitMatrixBuffer CreateRectangularMicroReservedMap(int versionIndex)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        BitMatrixBuffer modules = new BitMatrixBuffer(width, height);
        BitMatrixBuffer reserved = new BitMatrixBuffer(width, height);
        DrawRectangularMicroPatterns(versionIndex, 0, 0, modules, reserved);
        return reserved;
    }

    static void DrawModel2Patterns(
        int version,
        int formatCode,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int dimension = Model2VersionTable.GetDimension(version);
        for (int finderIndex = 0; finderIndex < 3; finderIndex++)
        {
            int centerRow = Model2PlacementTable.GetFinderCenterRow(
                version,
                finderIndex);
            int centerColumn = Model2PlacementTable.GetFinderCenterColumn(
                version,
                finderIndex);
            DrawFinderPattern(
                centerRow,
                centerColumn,
                dimension,
                modules,
                reserved);
        }

        DrawTimingPatterns(dimension, modules, reserved);
        DrawAlignmentPatterns(version, modules, reserved);
        DrawFormatPatterns(version, formatCode, modules, reserved);
        SetFunctionModule(
            modules,
            reserved,
            Model2PlacementTable.GetDarkModuleRow(version),
            Model2PlacementTable.GetDarkModuleColumn(version),
            true);
        if (version >= 7)
        {
            DrawVersionPatterns(version, modules, reserved);
        }
    }

    static void DrawMicroPatterns(
        int version,
        int formatCode,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int dimension = MicroVersionTable.GetDimension(version);
        DrawFinderPattern(
            MicroPlacementTable.GetFinderCenterRow(version),
            MicroPlacementTable.GetFinderCenterColumn(version),
            dimension,
            modules,
            reserved);
        for (int index = MicroPlacementTable.TimingStartIndex;
            index < dimension;
            index++)
        {
            bool dark = MicroPlacementTable.GetTimingModuleValue(version, index);
            SetFunctionModule(modules, reserved, 0, index, dark);
            SetFunctionModule(modules, reserved, index, 0, dark);
        }

        for (int bitIndex = 0; bitIndex <= 7; bitIndex++)
        {
            bool dark = ((formatCode >> bitIndex) & 1) != 0;
            SetFunctionModule(
                modules,
                reserved,
                MicroPlacementTable.GetFormatVerticalRow(bitIndex),
                MicroPlacementTable.GetFormatVerticalColumn(bitIndex),
                dark);
        }

        for (int bitIndex = 7; bitIndex < 15; bitIndex++)
        {
            bool dark = ((formatCode >> bitIndex) & 1) != 0;
            SetFunctionModule(
                modules,
                reserved,
                MicroPlacementTable.GetFormatHorizontalRow(bitIndex),
                MicroPlacementTable.GetFormatHorizontalColumn(bitIndex),
                dark);
        }
    }

    static void DrawRectangularMicroPatterns(
        int versionIndex,
        int finderCode,
        int subfinderCode,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        DrawRectangularMicroFinder(
            versionIndex,
            modules,
            reserved);
        DrawRectangularMicroSubfinder(
            versionIndex,
            modules,
            reserved);
        DrawRectangularMicroCorners(versionIndex, modules, reserved);
        DrawRectangularMicroAlignmentPatterns(versionIndex, modules, reserved);
        DrawRectangularMicroTimingPatterns(versionIndex, modules, reserved);
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            SetFunctionModule(
                modules,
                reserved,
                RectangularMicroPlacementTable.GetFinderSideFormatRow(bitIndex),
                RectangularMicroPlacementTable.GetFinderSideFormatColumn(bitIndex),
                ((finderCode >> bitIndex) & 1) != 0);
            SetFunctionModule(
                modules,
                reserved,
                RectangularMicroPlacementTable.GetSubfinderSideFormatRow(
                    versionIndex,
                    bitIndex),
                RectangularMicroPlacementTable.GetSubfinderSideFormatColumn(
                    versionIndex,
                    bitIndex),
                ((subfinderCode >> bitIndex) & 1) != 0);
        }
    }

    static void DrawRectangularMicroSubfinder(
        int versionIndex,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int centerRow = RectangularMicroPlacementTable.GetSubfinderCenterRow(versionIndex);
        int centerColumn = RectangularMicroPlacementTable.GetSubfinderCenterColumn(versionIndex);
        for (int rowOffset = -2; rowOffset <= 2; rowOffset++)
        {
            for (int columnOffset = -2; columnOffset <= 2; columnOffset++)
            {
                bool dark = rowOffset == -2
                    || rowOffset == 2
                    || columnOffset == -2
                    || columnOffset == 2
                    || rowOffset == 0 && columnOffset == 0;
                SetFunctionModule(
                    modules,
                    reserved,
                    centerRow + rowOffset,
                    centerColumn + columnOffset,
                    dark);
            }
        }
    }

    static void DrawRectangularMicroFinder(
        int versionIndex,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int centerRow = RectangularMicroPlacementTable.GetFinderCenterRow(versionIndex);
        int centerColumn = RectangularMicroPlacementTable.GetFinderCenterColumn(versionIndex);
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        for (int rowOffset = -4; rowOffset <= 4; rowOffset++)
        {
            for (int columnOffset = -4; columnOffset <= 4; columnOffset++)
            {
                int row = centerRow + rowOffset;
                int column = centerColumn + columnOffset;
                if (row < 0 || row >= height || column < 0 || column >= width)
                {
                    continue;
                }

                int rowDistance = rowOffset;
                if (rowDistance < 0)
                {
                    rowDistance = -rowDistance;
                }

                int columnDistance = columnOffset;
                if (columnDistance < 0)
                {
                    columnDistance = -columnDistance;
                }

                int distance = rowDistance;
                if (columnDistance > distance)
                {
                    distance = columnDistance;
                }

                bool dark = distance != 2 && distance != 4;
                SetFunctionModule(modules, reserved, row, column, dark);
            }
        }
    }

    static void DrawRectangularMicroCorners(
        int versionIndex,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        for (int column = 0; column < 3; column++)
        {
            SetFunctionModule(modules, reserved, height - 1, column, true);
        }

        if (height >= 11)
        {
            SetFunctionModule(modules, reserved, height - 2, 0, true);
            SetFunctionModule(modules, reserved, height - 2, 1, false);
        }

        SetFunctionModule(modules, reserved, 0, width - 1, true);
        SetFunctionModule(modules, reserved, 0, width - 2, true);
        SetFunctionModule(modules, reserved, 1, width - 1, true);
        SetFunctionModule(modules, reserved, 1, width - 2, false);
    }

    static void DrawRectangularMicroAlignmentPatterns(
        int versionIndex,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        int[] columns = RectangularMicroAlignmentTable.GetCenterColumns(width);
        for (int centerIndex = 0; centerIndex < columns.Length; centerIndex++)
        {
            int centerColumn = columns[centerIndex];
            for (int rowOffset = 0; rowOffset < 3; rowOffset++)
            {
                for (int columnOffset = -1; columnOffset <= 1; columnOffset++)
                {
                    bool dark = rowOffset == 0
                        || rowOffset == 2
                        || columnOffset == -1
                        || columnOffset == 1;
                    SetFunctionModule(
                        modules,
                        reserved,
                        rowOffset,
                        centerColumn + columnOffset,
                        dark);
                    SetFunctionModule(
                        modules,
                        reserved,
                        height - 1 - rowOffset,
                        centerColumn + columnOffset,
                        dark);
                }
            }
        }
    }

    static void DrawRectangularMicroTimingPatterns(
        int versionIndex,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int width = RectangularMicroVersionTable.GetWidth(versionIndex);
        int height = RectangularMicroVersionTable.GetHeight(versionIndex);
        for (int column = 0; column < width; column++)
        {
            bool dark = RectangularMicroPlacementTable.GetHorizontalTimingModuleValue(
                versionIndex,
                column);
            if (!reserved.GetModule(0, column))
            {
                SetFunctionModule(modules, reserved, 0, column, dark);
            }

            if (!reserved.GetModule(height - 1, column))
            {
                SetFunctionModule(modules, reserved, height - 1, column, dark);
            }
        }

        int[] alignmentColumns = RectangularMicroAlignmentTable.GetCenterColumns(width);
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
                if (!reserved.GetModule(row, column))
                {
                    bool dark = RectangularMicroPlacementTable.GetVerticalTimingModuleValue(
                        versionIndex,
                        row);
                    SetFunctionModule(modules, reserved, row, column, dark);
                }
            }
        }
    }

    static void DrawFinderPattern(
        int centerRow,
        int centerColumn,
        int dimension,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        for (int rowOffset = -4; rowOffset <= 4; rowOffset++)
        {
            for (int columnOffset = -4; columnOffset <= 4; columnOffset++)
            {
                int row = centerRow + rowOffset;
                int column = centerColumn + columnOffset;
                if (row < 0 || row >= dimension || column < 0 || column >= dimension)
                {
                    continue;
                }

                int rowDistance = rowOffset;
                if (rowDistance < 0)
                {
                    rowDistance = -rowDistance;
                }

                int columnDistance = columnOffset;
                if (columnDistance < 0)
                {
                    columnDistance = -columnDistance;
                }

                int distance = rowDistance;
                if (columnDistance > distance)
                {
                    distance = columnDistance;
                }

                bool dark = distance != 2 && distance != 4;
                SetFunctionModule(modules, reserved, row, column, dark);
            }
        }
    }

    static void DrawTimingPatterns(
        int dimension,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        for (int index = 8; index < dimension - 8; index++)
        {
            bool dark = index % 2 == 0;
            SetFunctionModule(
                modules,
                reserved,
                Model2PlacementTable.TimingPatternIndex,
                index,
                dark);
            SetFunctionModule(
                modules,
                reserved,
                index,
                Model2PlacementTable.TimingPatternIndex,
                dark);
        }
    }

    static void DrawAlignmentPatterns(
        int version,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int[] positions = AlignmentPositionTable.GetPositions(version);
        for (int rowIndex = 0; rowIndex < positions.Length; rowIndex++)
        {
            for (int columnIndex = 0; columnIndex < positions.Length; columnIndex++)
            {
                bool overlapsFinder = rowIndex == 0
                    && (columnIndex == 0 || columnIndex == positions.Length - 1)
                    || rowIndex == positions.Length - 1 && columnIndex == 0;
                if (overlapsFinder)
                {
                    continue;
                }

                DrawAlignmentPattern(
                    positions[rowIndex],
                    positions[columnIndex],
                    modules,
                    reserved);
            }
        }
    }

    static void DrawAlignmentPattern(
        int centerRow,
        int centerColumn,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        for (int rowOffset = -2; rowOffset <= 2; rowOffset++)
        {
            for (int columnOffset = -2; columnOffset <= 2; columnOffset++)
            {
                bool dark = rowOffset == -2
                    || rowOffset == 2
                    || columnOffset == -2
                    || columnOffset == 2
                    || rowOffset == 0 && columnOffset == 0;
                SetFunctionModule(
                    modules,
                    reserved,
                    centerRow + rowOffset,
                    centerColumn + columnOffset,
                    dark);
            }
        }
    }

    static void DrawFormatPatterns(
        int version,
        int formatCode,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        for (int bitIndex = 0; bitIndex < 15; bitIndex++)
        {
            bool dark = ((formatCode >> bitIndex) & 1) != 0;
            SetFunctionModule(
                modules,
                reserved,
                Model2PlacementTable.GetFormatPrimaryRow(bitIndex),
                Model2PlacementTable.GetFormatPrimaryColumn(bitIndex),
                dark);
            SetFunctionModule(
                modules,
                reserved,
                Model2PlacementTable.GetFormatSecondaryRow(version, bitIndex),
                Model2PlacementTable.GetFormatSecondaryColumn(version, bitIndex),
                dark);
        }
    }

    static void DrawVersionPatterns(
        int version,
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved)
    {
        int versionCode = VersionInformationCodec.CreateCode(version);
        for (int bitIndex = 0; bitIndex < 18; bitIndex++)
        {
            bool dark = ((versionCode >> bitIndex) & 1) != 0;
            SetFunctionModule(
                modules,
                reserved,
                Model2PlacementTable.GetVersionPrimaryRow(version, bitIndex),
                Model2PlacementTable.GetVersionPrimaryColumn(version, bitIndex),
                dark);
            SetFunctionModule(
                modules,
                reserved,
                Model2PlacementTable.GetVersionSecondaryRow(version, bitIndex),
                Model2PlacementTable.GetVersionSecondaryColumn(version, bitIndex),
                dark);
        }
    }

    static void SetFunctionModule(
        BitMatrixBuffer modules,
        BitMatrixBuffer reserved,
        int row,
        int column,
        bool dark)
    {
        modules.SetModule(row, column, dark);
        reserved.SetModule(row, column, true);
    }
}
