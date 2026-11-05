using System.IO;

namespace png;

internal class PngFilterDecoder
{
    internal static byte[] DecodeNone(byte[] filteredRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        return DecodeNone(filteredRow, 0, filteredRow.Length - 1);
    }

    internal static byte[] DecodeNone(byte[] filteredRows, int offset, int rowDataBytes)
    {
        ArgumentNullException.ThrowIfNull(filteredRows);

        if (offset < 0 || offset >= filteredRows.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > filteredRows.Length - offset - 1)
        {
            throw new ArgumentOutOfRangeException("rowDataBytes");
        }

        if (filteredRows[offset] != PngConstants.FilterTypeNone)
        {
            throw new InvalidDataException("PNG row filter type is not None.");
        }

        byte[] rowData = new byte[rowDataBytes];
        int index = 0;
        while (index < rowDataBytes)
        {
            rowData[index] = filteredRows[offset + 1 + index];
            index += 1;
        }

        return rowData;
    }

    internal static byte[] DecodeSub(byte[] filteredRow, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        return DecodeSub(filteredRow, 0, filteredRow.Length - 1, bytesPerPixel);
    }

    internal static byte[] DecodeSub(byte[] filteredRows, int offset, int rowDataBytes, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(filteredRows);

        if (offset < 0 || offset >= filteredRows.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > filteredRows.Length - offset - 1)
        {
            throw new ArgumentOutOfRangeException("rowDataBytes");
        }

        if (bytesPerPixel <= 0)
        {
            throw new ArgumentOutOfRangeException("bytesPerPixel");
        }

        if (filteredRows[offset] != PngConstants.FilterTypeSub)
        {
            throw new InvalidDataException("PNG row filter type is not Sub.");
        }

        byte[] rowData = new byte[rowDataBytes];
        int index = 0;
        while (index < rowDataBytes)
        {
            int left = 0;
            if (index >= bytesPerPixel)
            {
                left = rowData[index - bytesPerPixel];
            }

            rowData[index] = (byte)((filteredRows[offset + 1 + index] + left) & 255);
            index += 1;
        }

        return rowData;
    }

    internal static byte[] DecodeUp(byte[] filteredRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        return DecodeUpWithPrevious(filteredRow, 0, filteredRow.Length - 1, new byte[0], false);
    }

    internal static byte[] DecodeUp(byte[] filteredRow, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);
        ArgumentNullException.ThrowIfNull(previousRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        return DecodeUpWithPrevious(filteredRow, 0, filteredRow.Length - 1, previousRow, true);
    }

    internal static byte[] DecodeUp(byte[] filteredRows, int offset, int rowDataBytes)
    {
        return DecodeUpWithPrevious(filteredRows, offset, rowDataBytes, new byte[0], false);
    }

    internal static byte[] DecodeUp(byte[] filteredRows, int offset, int rowDataBytes, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return DecodeUpWithPrevious(filteredRows, offset, rowDataBytes, previousRow, true);
    }

    internal static byte[] DecodeUpWithPrevious(byte[] filteredRows, int offset, int rowDataBytes, byte[] previousRow, bool hasPreviousRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRows);

        if (offset < 0 || offset >= filteredRows.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > filteredRows.Length - offset - 1)
        {
            throw new ArgumentOutOfRangeException("rowDataBytes");
        }

        if (hasPreviousRow && previousRow.Length < rowDataBytes)
        {
            throw new ArgumentOutOfRangeException("previousRow");
        }

        if (filteredRows[offset] != PngConstants.FilterTypeUp)
        {
            throw new InvalidDataException("PNG row filter type is not Up.");
        }

        byte[] rowData = new byte[rowDataBytes];
        int index = 0;
        while (index < rowDataBytes)
        {
            int up = 0;
            if (hasPreviousRow)
            {
                up = previousRow[index];
            }

            rowData[index] = (byte)((filteredRows[offset + 1 + index] + up) & 255);
            index += 1;
        }

        return rowData;
    }

    internal static byte[] DecodeAverage(byte[] filteredRow, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        return DecodeAverageWithPrevious(filteredRow, 0, filteredRow.Length - 1, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] DecodeAverage(byte[] filteredRow, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);
        ArgumentNullException.ThrowIfNull(previousRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        return DecodeAverageWithPrevious(filteredRow, 0, filteredRow.Length - 1, bytesPerPixel, previousRow, true);
    }

    internal static byte[] DecodeAverage(byte[] filteredRows, int offset, int rowDataBytes, int bytesPerPixel)
    {
        return DecodeAverageWithPrevious(filteredRows, offset, rowDataBytes, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] DecodeAverage(byte[] filteredRows, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return DecodeAverageWithPrevious(filteredRows, offset, rowDataBytes, bytesPerPixel, previousRow, true);
    }

    internal static byte[] DecodeAverageWithPrevious(byte[] filteredRows, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRows);

        if (offset < 0 || offset >= filteredRows.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > filteredRows.Length - offset - 1)
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

        if (filteredRows[offset] != PngConstants.FilterTypeAverage)
        {
            throw new InvalidDataException("PNG row filter type is not Average.");
        }

        byte[] rowData = new byte[rowDataBytes];
        int index = 0;
        while (index < rowDataBytes)
        {
            int left = 0;
            if (index >= bytesPerPixel)
            {
                left = rowData[index - bytesPerPixel];
            }

            int up = 0;
            if (hasPreviousRow)
            {
                up = previousRow[index];
            }

            rowData[index] = (byte)((filteredRows[offset + 1 + index] + ((left + up) / 2)) & 255);
            index += 1;
        }

        return rowData;
    }

    internal static byte[] DecodePaeth(byte[] filteredRow, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        return DecodePaethWithPrevious(filteredRow, 0, filteredRow.Length - 1, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] DecodePaeth(byte[] filteredRow, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRow);
        ArgumentNullException.ThrowIfNull(previousRow);

        if (filteredRow.Length == 0)
        {
            throw new ArgumentOutOfRangeException("filteredRow");
        }

        return DecodePaethWithPrevious(filteredRow, 0, filteredRow.Length - 1, bytesPerPixel, previousRow, true);
    }

    internal static byte[] DecodePaeth(byte[] filteredRows, int offset, int rowDataBytes, int bytesPerPixel)
    {
        return DecodePaethWithPrevious(filteredRows, offset, rowDataBytes, bytesPerPixel, new byte[0], false);
    }

    internal static byte[] DecodePaeth(byte[] filteredRows, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return DecodePaethWithPrevious(filteredRows, offset, rowDataBytes, bytesPerPixel, previousRow, true);
    }

    internal static byte[] DecodePaethWithPrevious(byte[] filteredRows, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
    {
        ArgumentNullException.ThrowIfNull(filteredRows);

        if (offset < 0 || offset >= filteredRows.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (rowDataBytes < 0 || rowDataBytes > filteredRows.Length - offset - 1)
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

        if (filteredRows[offset] != PngConstants.FilterTypePaeth)
        {
            throw new InvalidDataException("PNG row filter type is not Paeth.");
        }

        byte[] rowData = new byte[rowDataBytes];
        int index = 0;
        while (index < rowDataBytes)
        {
            int left = 0;
            int upperLeft = 0;
            if (index >= bytesPerPixel)
            {
                left = rowData[index - bytesPerPixel];
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
            rowData[index] = (byte)((filteredRows[offset + 1 + index] + predictor) & 255);
            index += 1;
        }

        return rowData;
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
}
