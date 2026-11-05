namespace qrcode;

internal class MatrixInformationSupport
{
    internal static void ReadBit(QrMatrix matrix, MatrixInformationCopy copy, int row, int column, int bit)
    {
        int index = (row + matrix.QuietZoneTop) * matrix.Width + column + matrix.QuietZoneLeft;
        QrModuleValue value = matrix.Values[index];
        if (value == QrModuleValue.Unknown || matrix.Confidence != null && matrix.Confidence[index] == 0)
        {
            copy.MissingBitCount++;
        }
        else
        {
            copy.KnownBitMask |= 1 << bit;
            if (value == QrModuleValue.Dark)
            {
                copy.ReceivedCode |= 1 << bit;
            }
        }
    }

    internal static void CorrectCopy(MatrixInformationCopy copy, int bitCount, int first, int last, global::System.Func<int, int> getCode)
    {
        // Use the conservative BCH budget 2 * errors + erasures <= 6. An
        // unknown module is an erasure, never an assumed light bit. No candidates
        // outside the guaranteed correction budget are returned as decoded data.
        if (copy.MissingBitCount > 6)
        {
            return;
        }

        int bestData = -1;
        int bestCode = -1;
        int bestDistance = bitCount + 1;
        bool tied = false;
        for (int data = first; data <= last; data++)
        {
            int code = getCode(data);
            int distance = BchCodec.CountDifferences(copy.ReceivedCode, code & copy.KnownBitMask, bitCount);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestData = data;
                bestCode = code;
                tied = false;
            }
            else if (distance == bestDistance)
            {
                tied = true;
            }
        }

        if (!tied && bestDistance * 2 + copy.MissingBitCount <= 6)
        {
            copy.Success = true;
            copy.Data = bestData;
            copy.CorrectedCode = bestCode;
            copy.CorrectedErrorCount = bestDistance;
        }
    }

    internal static int ChooseRequestedCopy(MatrixInformationCopy[] copies, int requested)
    {
        if (requested < -1 || requested >= copies.Length)
        {
            throw new QrInputException("The metadata copy index is invalid.", "requested");
        }

        if (requested == -1) { return ChooseCopy(copies); }
        return copies[requested].Success ? requested : -1;
    }

    internal static int ChooseCopy(MatrixInformationCopy[] copies)
    {
        if (!copies[0].Success)
        {
            return copies[1].Success ? 1 : -1;
        }

        if (!copies[1].Success)
        {
            return 0;
        }

        int firstCost = copies[0].CorrectedErrorCount * 2 + copies[0].MissingBitCount;
        int secondCost = copies[1].CorrectedErrorCount * 2 + copies[1].MissingBitCount;
        if (firstCost == secondCost && copies[0].Data != copies[1].Data)
        {
            return -1;
        }

        // Deterministic primary-copy preference only when corrected data agrees
        // or that copy has stronger evidence. Conflicting alternatives remain in
        // the result for future recovery; dimension agreement never overwrites BCH data.
        return firstCost <= secondCost ? 0 : 1;
    }

    internal static bool CopiesAgree(MatrixInformationCopy[] copies)
    {
        return copies[0].Success && copies[1].Success && copies[0].Data == copies[1].Data;
    }

    internal static void ValidateInput(QrMatrix matrix, QrDecodeOptions options)
    {
        if (matrix == null || options == null)
        {
            throw new QrInputException("Matrix and decode options are required.", matrix == null ? "matrix" : "options");
        }

        CheckedDimensions.ValidateDimensionBounds(matrix.Width, matrix.Height,
            options.MaximumMatrixDimension, options.MaximumMatrixDimension, int.MaxValue);
        if (options.MaximumAllocationBytes <= 0)
        {
            throw new QrInputException("The allocation limit must be positive.", "MaximumAllocationBytes");
        }

        // Bounded result/copy storage; input pixels are inspected without copying.
        CheckedDimensions.CalculateAllocationBytes(1, 1024, options.MaximumAllocationBytes);
        int area = CheckedDimensions.CalculateArea(matrix.Width, matrix.Height, int.MaxValue);
        if (matrix.Values == null || matrix.Values.Length != area
            || matrix.Confidence != null && matrix.Confidence.Length != area
            || matrix.Roles != null && matrix.Roles.Length != area)
        {
            throw new QrInputException("Matrix buffers must match the declared dimensions.", "matrix");
        }

        if (matrix.QuietZoneLeft < 0 || matrix.QuietZoneRight < 0 || matrix.QuietZoneTop < 0 || matrix.QuietZoneBottom < 0
            || (long)matrix.QuietZoneLeft + matrix.QuietZoneRight >= matrix.Width
            || (long)matrix.QuietZoneTop + matrix.QuietZoneBottom >= matrix.Height)
        {
            throw new QrInputException("Quiet-zone extents must leave a positive encoding region.", "matrix");
        }

        foreach (QrModuleValue value in matrix.Values)
        {
            if (value != QrModuleValue.Unknown && value != QrModuleValue.Light && value != QrModuleValue.Dark)
            {
                throw new QrInputException("The matrix contains an undefined module value.", "Values");
            }
        }
    }
}
