namespace qrcode;

internal class MaskEvaluator
{
    internal const int AdjacentRunPenalty = 3;
    internal const int BlockPenalty = 3;
    internal const int FinderLikePenalty = 40;
    internal const int BalancePenalty = 10;

    internal static void ApplyModel2Mask(
        BitMatrixBuffer matrix,
        BitMatrixBuffer reserved,
        int mask)
    {
        ValidateModel2Matrix(matrix, "matrix");
        if (reserved == null)
        {
            throw new QrInputException("The reserved-module map cannot be null.", "reserved");
        }

        if (reserved.GetWidth() != matrix.GetWidth()
            || reserved.GetHeight() != matrix.GetHeight())
        {
            throw new QrInputException(
                "The reserved-module map dimensions must match the module matrix.",
                "reserved");
        }

        Model2MaskTable.IsInverted(mask, 0, 0);
        for (int row = 0; row < matrix.GetHeight(); row++)
        {
            for (int column = 0; column < matrix.GetWidth(); column++)
            {
                if (!reserved.GetModule(row, column)
                    && Model2MaskTable.IsInverted(mask, row, column))
                {
                    matrix.ToggleModule(row, column);
                }
            }
        }
    }

    internal static int CalculateModel2Penalty(BitMatrixBuffer matrix)
    {
        ValidateModel2Matrix(matrix, "matrix");
        return CalculateAdjacentPenalty(matrix)
            + CalculateBlockPenalty(matrix)
            + CalculateFinderLikePenalty(matrix)
            + CalculateBalancePenalty(matrix);
    }

    internal static int CalculateAdjacentPenalty(BitMatrixBuffer matrix)
    {
        ValidateModel2Matrix(matrix, "matrix");
        int penalty = 0;
        int dimension = matrix.GetWidth();
        for (int row = 0; row < dimension; row++)
        {
            bool color = matrix.GetModule(row, 0);
            int runLength = 1;
            for (int column = 1; column < dimension; column++)
            {
                bool nextColor = matrix.GetModule(row, column);
                if (nextColor == color)
                {
                    runLength++;
                    if (runLength == 5)
                    {
                        penalty += AdjacentRunPenalty;
                    }
                    else if (runLength > 5)
                    {
                        penalty++;
                    }
                }
                else
                {
                    color = nextColor;
                    runLength = 1;
                }
            }
        }

        for (int column = 0; column < dimension; column++)
        {
            bool color = matrix.GetModule(0, column);
            int runLength = 1;
            for (int row = 1; row < dimension; row++)
            {
                bool nextColor = matrix.GetModule(row, column);
                if (nextColor == color)
                {
                    runLength++;
                    if (runLength == 5)
                    {
                        penalty += AdjacentRunPenalty;
                    }
                    else if (runLength > 5)
                    {
                        penalty++;
                    }
                }
                else
                {
                    color = nextColor;
                    runLength = 1;
                }
            }
        }

        return penalty;
    }

    internal static int CalculateBlockPenalty(BitMatrixBuffer matrix)
    {
        ValidateModel2Matrix(matrix, "matrix");
        int penalty = 0;
        for (int row = 0; row < matrix.GetHeight() - 1; row++)
        {
            for (int column = 0; column < matrix.GetWidth() - 1; column++)
            {
                bool color = matrix.GetModule(row, column);
                if (matrix.GetModule(row, column + 1) == color
                    && matrix.GetModule(row + 1, column) == color
                    && matrix.GetModule(row + 1, column + 1) == color)
                {
                    penalty += BlockPenalty;
                }
            }
        }

        return penalty;
    }

    internal static int CalculateFinderLikePenalty(BitMatrixBuffer matrix)
    {
        ValidateModel2Matrix(matrix, "matrix");
        int penalty = 0;
        int dimension = matrix.GetWidth();
        for (int row = 0; row < dimension; row++)
        {
            int[] history = new int[7];
            bool color = false;
            int runLength = 0;
            for (int column = 0; column < dimension; column++)
            {
                bool nextColor = matrix.GetModule(row, column);
                if (nextColor == color)
                {
                    runLength++;
                }
                else
                {
                    AddFinderRun(runLength, history, dimension);
                    if (!color)
                    {
                        penalty += CountFinderPatterns(history) * FinderLikePenalty;
                    }

                    color = nextColor;
                    runLength = 1;
                }
            }

            penalty += TerminateFinderLine(
                color,
                runLength,
                history,
                dimension) * FinderLikePenalty;
        }

        for (int column = 0; column < dimension; column++)
        {
            int[] history = new int[7];
            bool color = false;
            int runLength = 0;
            for (int row = 0; row < dimension; row++)
            {
                bool nextColor = matrix.GetModule(row, column);
                if (nextColor == color)
                {
                    runLength++;
                }
                else
                {
                    AddFinderRun(runLength, history, dimension);
                    if (!color)
                    {
                        penalty += CountFinderPatterns(history) * FinderLikePenalty;
                    }

                    color = nextColor;
                    runLength = 1;
                }
            }

            penalty += TerminateFinderLine(
                color,
                runLength,
                history,
                dimension) * FinderLikePenalty;
        }

        return penalty;
    }

    internal static int CalculateBalancePenalty(BitMatrixBuffer matrix)
    {
        ValidateModel2Matrix(matrix, "matrix");
        int darkCount = 0;
        int totalCount = matrix.GetWidth() * matrix.GetHeight();
        for (int row = 0; row < matrix.GetHeight(); row++)
        {
            for (int column = 0; column < matrix.GetWidth(); column++)
            {
                if (matrix.GetModule(row, column))
                {
                    darkCount++;
                }
            }
        }

        long deviation = (long)darkCount * 20 - (long)totalCount * 10;
        if (deviation < 0)
        {
            deviation = -deviation;
        }

        return (int)(deviation / totalCount) * BalancePenalty;
    }

    internal static void ApplyMicroMask(
        BitMatrixBuffer matrix,
        BitMatrixBuffer reserved,
        int mask)
    {
        ValidateMicroMatrix(matrix, "matrix");
        if (reserved == null)
        {
            throw new QrInputException("The reserved-module map cannot be null.", "reserved");
        }

        if (reserved.GetWidth() != matrix.GetWidth()
            || reserved.GetHeight() != matrix.GetHeight())
        {
            throw new QrInputException(
                "The reserved-module map dimensions must match the module matrix.",
                "reserved");
        }

        MicroMaskTable.IsInverted(mask, 0, 0);
        for (int row = 0; row < matrix.GetHeight(); row++)
        {
            for (int column = 0; column < matrix.GetWidth(); column++)
            {
                if (!reserved.GetModule(row, column)
                    && MicroMaskTable.IsInverted(mask, row, column))
                {
                    matrix.ToggleModule(row, column);
                }
            }
        }
    }

    internal static int CalculateMicroScore(BitMatrixBuffer matrix)
    {
        ValidateMicroMatrix(matrix, "matrix");
        int lastRow = matrix.GetHeight() - 1;
        int lastColumn = matrix.GetWidth() - 1;
        int rightEdgeDarkCount = 0;
        int bottomEdgeDarkCount = 0;
        for (int index = 1; index < matrix.GetWidth(); index++)
        {
            if (matrix.GetModule(index, lastColumn))
            {
                rightEdgeDarkCount++;
            }

            if (matrix.GetModule(lastRow, index))
            {
                bottomEdgeDarkCount++;
            }
        }

        int smallerCount = rightEdgeDarkCount;
        int largerCount = bottomEdgeDarkCount;
        if (smallerCount > largerCount)
        {
            smallerCount = bottomEdgeDarkCount;
            largerCount = rightEdgeDarkCount;
        }

        return smallerCount * 16 + largerCount;
    }

    internal static void ApplyRectangularMicroMask(
        BitMatrixBuffer matrix,
        BitMatrixBuffer reserved)
    {
        ValidateRectangularMicroMatrix(matrix, "matrix");
        if (reserved == null)
        {
            throw new QrInputException("The reserved-module map cannot be null.", "reserved");
        }

        if (reserved.GetWidth() != matrix.GetWidth()
            || reserved.GetHeight() != matrix.GetHeight())
        {
            throw new QrInputException(
                "The reserved-module map dimensions must match the module matrix.",
                "reserved");
        }

        for (int row = 0; row < matrix.GetHeight(); row++)
        {
            for (int column = 0; column < matrix.GetWidth(); column++)
            {
                if (!reserved.GetModule(row, column)
                    && RectangularMicroMaskTable.IsInverted(row, column))
                {
                    matrix.ToggleModule(row, column);
                }
            }
        }
    }

    static void AddFinderRun(
        int runLength,
        int[] history,
        int dimension)
    {
        if (history[0] == 0)
        {
            runLength += dimension;
        }

        for (int index = history.Length - 1; index > 0; index--)
        {
            history[index] = history[index - 1];
        }

        history[0] = runLength;
    }

    static int CountFinderPatterns(int[] history)
    {
        int unit = history[1];
        bool core = unit > 0
            && history[2] == unit
            && history[3] == unit * 3
            && history[4] == unit
            && history[5] == unit;
        if (!core)
        {
            return 0;
        }

        int count = 0;
        if (history[0] >= unit * 4 && history[6] >= unit)
        {
            count++;
        }

        if (history[6] >= unit * 4 && history[0] >= unit)
        {
            count++;
        }

        return count;
    }

    static int TerminateFinderLine(
        bool color,
        int runLength,
        int[] history,
        int dimension)
    {
        if (color)
        {
            AddFinderRun(runLength, history, dimension);
            runLength = 0;
        }

        runLength += dimension;
        AddFinderRun(runLength, history, dimension);
        return CountFinderPatterns(history);
    }

    static void ValidateModel2Matrix(BitMatrixBuffer matrix, string inputName)
    {
        if (matrix == null)
        {
            throw new QrInputException("The module matrix cannot be null.", inputName);
        }

        int dimension = matrix.GetWidth();
        if (matrix.GetHeight() != dimension
            || dimension < Model2VersionTable.GetDimension(Model2VersionTable.MinimumVersion)
            || dimension > Model2VersionTable.GetDimension(Model2VersionTable.MaximumVersion)
            || (dimension - 17) % 4 != 0)
        {
            throw new QrInputException(
                "The module matrix dimensions are not valid for Model 2.",
                inputName);
        }
    }

    static void ValidateMicroMatrix(BitMatrixBuffer matrix, string inputName)
    {
        if (matrix == null)
        {
            throw new QrInputException("The module matrix cannot be null.", inputName);
        }

        int dimension = matrix.GetWidth();
        if (matrix.GetHeight() != dimension
            || dimension < MicroVersionTable.GetDimension(MicroVersionTable.MinimumVersion)
            || dimension > MicroVersionTable.GetDimension(MicroVersionTable.MaximumVersion)
            || (dimension - 9) % 2 != 0)
        {
            throw new QrInputException(
                "The module matrix dimensions are not valid for Micro QR.",
                inputName);
        }
    }

    static void ValidateRectangularMicroMatrix(
        BitMatrixBuffer matrix,
        string inputName)
    {
        if (matrix == null)
        {
            throw new QrInputException("The module matrix cannot be null.", inputName);
        }

        RectangularMicroVersionTable.GetVersionIndex(
            matrix.GetHeight(),
            matrix.GetWidth());
    }
}
