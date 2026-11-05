namespace png;

internal class PngFilterEncoder
{
    internal static byte[] EncodeNone(byte[] rowData)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        return EncodeNone(rowData, 0, rowData.Length);
    }

    internal static byte[] EncodeNone(byte[] rowData, int offset, int rowDataBytes)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        if (offset < 0 || offset > rowData.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > rowData.Length - offset)
        {
            throw new ArgumentOutOfRangeException("rowDataBytes");
        }

        byte[] filteredRow = new byte[rowDataBytes + 1];
        filteredRow[0] = PngConstants.FilterTypeNone;

        int index = 0;
        while (index < rowDataBytes)
        {
            filteredRow[index + 1] = rowData[offset + index];
            index += 1;
        }

        return filteredRow;
    }

    internal static byte[] EncodeSub(byte[] rowData, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        return EncodeSub(rowData, 0, rowData.Length, bytesPerPixel);
    }

    internal static byte[] EncodeSub(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        if (offset < 0 || offset > rowData.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > rowData.Length - offset)
        {
            throw new ArgumentOutOfRangeException("rowDataBytes");
        }

        if (bytesPerPixel <= 0)
        {
            throw new ArgumentOutOfRangeException("bytesPerPixel");
        }

        byte[] filteredRow = new byte[rowDataBytes + 1];
        filteredRow[0] = PngConstants.FilterTypeSub;

        int index = 0;
        while (index < rowDataBytes)
        {
            int left = 0;
            if (index >= bytesPerPixel)
            {
                left = rowData[offset + index - bytesPerPixel];
            }

            filteredRow[index + 1] = (byte)((rowData[offset + index] - left) & 255);
            index += 1;
        }

        return filteredRow;
    }

    internal static byte[] EncodeUp(byte[] rowData)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        return EncodeUpWithPrevious(rowData, 0, rowData.Length, new byte[0], false);
    }

    internal static byte[] EncodeUp(byte[] rowData, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodeUpWithPrevious(rowData, 0, rowData.Length, previousRow, true);
    }

    internal static byte[] EncodeUp(byte[] rowData, int offset, int rowDataBytes)
    {
        return EncodeUpWithPrevious(rowData, offset, rowDataBytes, new byte[0], false);
    }

    internal static byte[] EncodeUp(byte[] rowData, int offset, int rowDataBytes, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodeUpWithPrevious(rowData, offset, rowDataBytes, previousRow, true);
    }

    internal static byte[] EncodeUpWithPrevious(byte[] rowData, int offset, int rowDataBytes, byte[] previousRow, bool hasPreviousRow)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        if (offset < 0 || offset > rowData.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > rowData.Length - offset)
        {
            throw new ArgumentOutOfRangeException("rowDataBytes");
        }

        if (hasPreviousRow && previousRow.Length < rowDataBytes)
        {
            throw new ArgumentOutOfRangeException("previousRow");
        }

        byte[] filteredRow = new byte[rowDataBytes + 1];
        filteredRow[0] = PngConstants.FilterTypeUp;

        int index = 0;
        while (index < rowDataBytes)
        {
            int up = 0;
            if (hasPreviousRow)
            {
                up = previousRow[index];
            }

            filteredRow[index + 1] = (byte)((rowData[offset + index] - up) & 255);
            index += 1;
        }

        return filteredRow;
    }

    internal static byte[] EncodeAverage(byte[] rowData, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        return EncodeAverageWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] EncodeAverage(byte[] rowData, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodeAverageWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, previousRow, true);
    }

    internal static byte[] EncodeAverage(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel)
    {
        return EncodeAverageWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] EncodeAverage(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodeAverageWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, true);
    }

    internal static byte[] EncodeAverageWithPrevious(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        if (offset < 0 || offset > rowData.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > rowData.Length - offset)
        {
            throw new ArgumentOutOfRangeException("rowDataBytes");
        }

        if (bytesPerPixel <= 0)
        {
            throw new ArgumentOutOfRangeException("bytesPerPixel");
        }

        if (hasPreviousRow && previousRow.Length < rowDataBytes)
        {
            throw new ArgumentOutOfRangeException("previousRow");
        }

        byte[] filteredRow = new byte[rowDataBytes + 1];
        filteredRow[0] = PngConstants.FilterTypeAverage;

        int index = 0;
        while (index < rowDataBytes)
        {
            int left = 0;
            if (index >= bytesPerPixel)
            {
                left = rowData[offset + index - bytesPerPixel];
            }

            int up = 0;
            if (hasPreviousRow)
            {
                up = previousRow[index];
            }

            filteredRow[index + 1] = (byte)((rowData[offset + index] - ((left + up) / 2)) & 255);
            index += 1;
        }

        return filteredRow;
    }

    internal static byte[] EncodePaeth(byte[] rowData, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        return EncodePaethWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] EncodePaeth(byte[] rowData, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodePaethWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, previousRow, true);
    }

    internal static byte[] EncodePaeth(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel)
    {
        return EncodePaethWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] EncodePaeth(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodePaethWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, true);
    }

    internal static byte[] EncodePaethWithPrevious(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        if (offset < 0 || offset > rowData.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > rowData.Length - offset)
        {
            throw new ArgumentOutOfRangeException("rowDataBytes");
        }

        if (bytesPerPixel <= 0)
        {
            throw new ArgumentOutOfRangeException("bytesPerPixel");
        }

        if (hasPreviousRow && previousRow.Length < rowDataBytes)
        {
            throw new ArgumentOutOfRangeException("previousRow");
        }

        byte[] filteredRow = new byte[rowDataBytes + 1];
        filteredRow[0] = PngConstants.FilterTypePaeth;

        int index = 0;
        while (index < rowDataBytes)
        {
            int left = 0;
            int upperLeft = 0;
            if (index >= bytesPerPixel)
            {
                left = rowData[offset + index - bytesPerPixel];
                if (hasPreviousRow)
                {
                    upperLeft = previousRow[index - bytesPerPixel];
                }
            }

            int up = 0;
            if (hasPreviousRow)
            {
                up = previousRow[index];
            }

            int predictor = PaethPredictor(left, up, upperLeft);
            filteredRow[index + 1] = (byte)((rowData[offset + index] - predictor) & 255);
            index += 1;
        }

        return filteredRow;
    }

    internal static int PaethPredictor(int left, int up, int upperLeft)
    {
        int estimate = left + up - upperLeft;
        int leftDistance = estimate - left;
        if (leftDistance < 0)
        {
            leftDistance = -leftDistance;
        }

        int upDistance = estimate - up;
        if (upDistance < 0)
        {
            upDistance = -upDistance;
        }

        int upperLeftDistance = estimate - upperLeft;
        if (upperLeftDistance < 0)
        {
            upperLeftDistance = -upperLeftDistance;
        }

        if (leftDistance <= upDistance && leftDistance <= upperLeftDistance)
        {
            return left;
        }

        if (upDistance <= upperLeftDistance)
        {
            return up;
        }

        return upperLeft;
    }

    internal static byte[] EncodeFixed(byte[] rowData, int bytesPerPixel, PngFilterType filterType)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        return EncodeFixedWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, new byte[0], false, filterType);
    }

    internal static byte[] EncodeFixed(byte[] rowData, int bytesPerPixel, byte[] previousRow, PngFilterType filterType)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodeFixedWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, previousRow, true, filterType);
    }

    internal static byte[] EncodeFixed(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, PngFilterType filterType)
    {
        return EncodeFixedWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, new byte[0], false, filterType);
    }

    internal static byte[] EncodeFixed(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, PngFilterType filterType)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodeFixedWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, true, filterType);
    }

    internal static byte[] EncodeFixed(byte[] rowData, int bytesPerPixel, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(options);

        return EncodeFixedWithOptions(rowData, 0, rowData.Length, bytesPerPixel, new byte[0], false, options);
    }

    internal static byte[] EncodeFixed(byte[] rowData, int bytesPerPixel, byte[] previousRow, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);
        ArgumentNullException.ThrowIfNull(options);

        return EncodeFixedWithOptions(rowData, 0, rowData.Length, bytesPerPixel, previousRow, true, options);
    }

    internal static byte[] EncodeFixed(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return EncodeFixedWithOptions(rowData, offset, rowDataBytes, bytesPerPixel, new byte[0], false, options);
    }

    internal static byte[] EncodeFixed(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(previousRow);
        ArgumentNullException.ThrowIfNull(options);

        return EncodeFixedWithOptions(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, true, options);
    }

    internal static byte[] EncodeFixedWithOptions(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow, PngEncoderOptions options)
    {
        if (!options.UseFixedFilter)
        {
            throw new InvalidOperationException("PngEncoderOptions.UseFixedFilter must be true to encode with a fixed filter.");
        }

        return EncodeFixedWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, options.FixedFilterType);
    }

    internal static byte[] EncodeFixedWithPrevious(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow, PngFilterType filterType)
    {
        ValidateFixedFilterType(filterType);

        if (filterType == PngFilterType.None)
        {
            return EncodeNone(rowData, offset, rowDataBytes);
        }

        if (filterType == PngFilterType.Sub)
        {
            return EncodeSub(rowData, offset, rowDataBytes, bytesPerPixel);
        }

        if (filterType == PngFilterType.Up)
        {
            if (hasPreviousRow)
            {
                return EncodeUp(rowData, offset, rowDataBytes, previousRow);
            }

            return EncodeUp(rowData, offset, rowDataBytes);
        }

        if (filterType == PngFilterType.Average)
        {
            if (hasPreviousRow)
            {
                return EncodeAverage(rowData, offset, rowDataBytes, bytesPerPixel, previousRow);
            }

            return EncodeAverage(rowData, offset, rowDataBytes, bytesPerPixel);
        }

        if (hasPreviousRow)
        {
            return EncodePaeth(rowData, offset, rowDataBytes, bytesPerPixel, previousRow);
        }

        return EncodePaeth(rowData, offset, rowDataBytes, bytesPerPixel);
    }

    internal static void ValidateFixedFilterType(PngFilterType filterType)
    {
        if (!PngRowFilterValidator.IsValidFilterType((int)filterType))
        {
            throw new ArgumentOutOfRangeException("filterType");
        }
    }

    internal static long CalculateRowScore(byte[] filteredRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        long score = 0;
        int index = 1;
        while (index < filteredRow.Length)
        {
            int value = filteredRow[index];
            if (value >= 128)
            {
                value = 256 - value;
            }

            score += value;
            index += 1;
        }

        return score;
    }

    internal static byte[] EncodeAdaptive(byte[] rowData, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        return EncodeAdaptiveWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] EncodeAdaptive(byte[] rowData, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodeAdaptiveWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, previousRow, true);
    }

    internal static byte[] EncodeAdaptive(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel)
    {
        return EncodeAdaptiveWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] EncodeAdaptive(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return EncodeAdaptiveWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, true);
    }

    internal static byte[] EncodeRow(byte[] rowData, int bytesPerPixel, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(options);

        return EncodeRowWithOptions(rowData, 0, rowData.Length, bytesPerPixel, new byte[0], false, options);
    }

    internal static byte[] EncodeRow(byte[] rowData, int bytesPerPixel, byte[] previousRow, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);
        ArgumentNullException.ThrowIfNull(options);

        return EncodeRowWithOptions(rowData, 0, rowData.Length, bytesPerPixel, previousRow, true, options);
    }

    internal static byte[] EncodeRow(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return EncodeRowWithOptions(rowData, offset, rowDataBytes, bytesPerPixel, new byte[0], false, options);
    }

    internal static byte[] EncodeRow(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(previousRow);
        ArgumentNullException.ThrowIfNull(options);

        return EncodeRowWithOptions(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, true, options);
    }

    internal static byte[] EncodeRowWithOptions(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow, PngEncoderOptions options)
    {
        ValidateFilterMethod(options.FilterMethod);
        ValidateRowFilterStrategy(options.RowFilterStrategy);

        if (options.UseFixedFilter || options.RowFilterStrategy == PngRowFilterStrategy.Fixed)
        {
            return EncodeFixedWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, options.FixedFilterType);
        }

        if (options.RowFilterStrategy == PngRowFilterStrategy.Fast)
        {
            return EncodeFastWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow);
        }

        return EncodeAdaptiveWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow);
    }

    internal static byte[] EncodeFastWithPrevious(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
    {
        if (hasPreviousRow)
        {
            return EncodeUp(rowData, offset, rowDataBytes, previousRow);
        }

        return EncodeSub(rowData, offset, rowDataBytes, bytesPerPixel);
    }

    internal static byte[] EncodeAdaptiveWithPrevious(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
    {
        if (bytesPerPixel <= 0)
        {
            throw new ArgumentOutOfRangeException("bytesPerPixel");
        }

        byte[] bestRow = EncodeNone(rowData, offset, rowDataBytes);

        byte[] candidate = EncodeSub(rowData, offset, rowDataBytes, bytesPerPixel);
        bestRow = ChooseBetterRow(candidate, bestRow);

        if (hasPreviousRow)
        {
            candidate = EncodeUp(rowData, offset, rowDataBytes, previousRow);
        }
        else
        {
            candidate = EncodeUp(rowData, offset, rowDataBytes);
        }

        bestRow = ChooseBetterRow(candidate, bestRow);

        if (hasPreviousRow)
        {
            candidate = EncodeAverage(rowData, offset, rowDataBytes, bytesPerPixel, previousRow);
        }
        else
        {
            candidate = EncodeAverage(rowData, offset, rowDataBytes, bytesPerPixel);
        }

        bestRow = ChooseBetterRow(candidate, bestRow);

        if (hasPreviousRow)
        {
            candidate = EncodePaeth(rowData, offset, rowDataBytes, bytesPerPixel, previousRow);
        }
        else
        {
            candidate = EncodePaeth(rowData, offset, rowDataBytes, bytesPerPixel);
        }

        bestRow = ChooseBetterRow(candidate, bestRow);

        return bestRow;
    }

    internal static byte[] ChooseBetterRow(byte[] candidate, byte[] bestRow)
    {
        long bestScore = CalculateRowScore(bestRow);
        long candidateScore = CalculateRowScore(candidate);
        if (candidateScore < bestScore)
        {
            return candidate;
        }

        return bestRow;
    }

    internal static void ValidateFilterMethod(PngFilterMethod filterMethod)
    {
        if (filterMethod != PngFilterMethod.Adaptive)
        {
            throw new ArgumentOutOfRangeException("FilterMethod");
        }
    }

    internal static void ValidateRowFilterStrategy(PngRowFilterStrategy strategy)
    {
        if (strategy != PngRowFilterStrategy.Fast
            && strategy != PngRowFilterStrategy.Adaptive
            && strategy != PngRowFilterStrategy.Fixed
            && strategy != PngRowFilterStrategy.Exhaustive)
        {
            throw new ArgumentOutOfRangeException("RowFilterStrategy");
        }
    }
}
