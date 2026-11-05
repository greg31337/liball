namespace gif;

internal class GifInterlaceRowOrderResult
{
    internal bool Success;
    internal int[] Rows;
    internal GifErrorDiagnostic Error;

    internal GifInterlaceRowOrderResult()
    {
        Success = false;
        Rows = new int[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifDeinterlaceResult
{
    internal bool Success;
    internal byte[] IndexedPixels;
    internal GifErrorDiagnostic Error;

    internal GifDeinterlaceResult()
    {
        Success = false;
        IndexedPixels = new byte[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifInterlaceWriteResult
{
    internal bool Success;
    internal byte[] IndexedPixels;
    internal GifErrorDiagnostic Error;

    internal GifInterlaceWriteResult()
    {
        Success = false;
        IndexedPixels = new byte[0];
        Error = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);
    }
}

internal class GifInterlaceRowMapper
{
    internal GifInterlaceRowOrderResult CreateRowOrder(int height)
    {
        GifInterlaceRowOrderResult result = new GifInterlaceRowOrderResult();

        if (height < 0)
        {
            result.Error = CreateError("Interlaced image height must be nonnegative.");
            return result;
        }

        result.Rows = new int[height];
        int rowCount = FillPass(result.Rows, height, 0, 8, 0);
        rowCount = FillPass(result.Rows, height, 4, 8, rowCount);
        rowCount = FillPass(result.Rows, height, 2, 4, rowCount);
        rowCount = FillPass(result.Rows, height, 1, 2, rowCount);

        if (rowCount != height)
        {
            result.Rows = new int[0];
            result.Error = CreateError("Interlaced row order did not cover the image height exactly.");
            return result;
        }

        result.Success = true;
        return result;
    }

    internal int GetRowForInterlacedIndex(int height, int interlacedRowIndex)
    {
        GifInterlaceRowOrderResult result = CreateRowOrder(height);
        if (!result.Success)
        {
            return -1;
        }

        if (interlacedRowIndex < 0 || interlacedRowIndex >= result.Rows.Length)
        {
            return -1;
        }

        return result.Rows[interlacedRowIndex];
    }

    internal GifInterlaceWriteResult PrepareIndexedPixelsForWrite(byte[] rowMajorPixels, int width, int height, bool interlace)
    {
        if (interlace)
        {
            return InterlaceIndexedPixels(rowMajorPixels, width, height);
        }

        return CopyIndexedPixelsForWrite(rowMajorPixels, width, height);
    }

    internal GifInterlaceWriteResult InterlaceIndexedPixels(byte[] rowMajorPixels, int width, int height)
    {
        GifInterlaceWriteResult result = ValidateWriteIndexedPixels(rowMajorPixels, width, height);
        if (!result.Success)
        {
            return result;
        }

        if (result.IndexedPixels.Length == 0)
        {
            return result;
        }

        GifInterlaceRowOrderResult rowOrder = CreateRowOrder(height);
        if (!rowOrder.Success)
        {
            result.Success = false;
            result.IndexedPixels = new byte[0];
            result.Error = rowOrder.Error;
            return result;
        }

        byte[] interlacedPixels = new byte[result.IndexedPixels.Length];
        int interlacedRow = 0;
        while (interlacedRow < height)
        {
            int sourceRow = rowOrder.Rows[interlacedRow];
            int sourceOffset = sourceRow * width;
            int destinationOffset = interlacedRow * width;
            CopyRow(result.IndexedPixels, sourceOffset, interlacedPixels, destinationOffset, width);
            interlacedRow = interlacedRow + 1;
        }

        result.IndexedPixels = interlacedPixels;
        return result;
    }

    internal GifInterlaceWriteResult CopyIndexedPixelsForWrite(byte[] rowMajorPixels, int width, int height)
    {
        GifInterlaceWriteResult result = ValidateWriteIndexedPixels(rowMajorPixels, width, height);
        if (!result.Success)
        {
            return result;
        }

        byte[] copiedPixels = new byte[result.IndexedPixels.Length];
        CopyRow(result.IndexedPixels, 0, copiedPixels, 0, result.IndexedPixels.Length);
        result.IndexedPixels = copiedPixels;
        return result;
    }

    internal GifInterlaceWriteResult ValidateWriteIndexedPixels(byte[] indexedPixels, int width, int height)
    {
        GifInterlaceWriteResult result = new GifInterlaceWriteResult();

        if (width < 0 || height < 0)
        {
            result.Error = CreateError("Indexed image dimensions must be nonnegative for writing.");
            return result;
        }

        long expectedPixelCount = (long)width * (long)height;
        if (expectedPixelCount > 2147483647)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Indexed image is too large to prepare for writing.", new GifDiagnosticPosition(-1, -1, "Interlace"));
            return result;
        }

        if (indexedPixels == null)
        {
            indexedPixels = new byte[0];
        }

        if (indexedPixels.Length != (int)expectedPixelCount)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Indexed pixel count does not match image dimensions for writing.", new GifDiagnosticPosition(-1, -1, "Interlace"));
            return result;
        }

        result.Success = true;
        result.IndexedPixels = indexedPixels;
        return result;
    }

    internal GifDeinterlaceResult DeinterlaceIndexedPixels(byte[] interlacedPixels, int width, int height)
    {
        GifDeinterlaceResult result = ValidateIndexedPixels(interlacedPixels, width, height);
        if (!result.Success)
        {
            return result;
        }

        if (result.IndexedPixels.Length == 0)
        {
            return result;
        }

        GifInterlaceRowOrderResult rowOrder = CreateRowOrder(height);
        if (!rowOrder.Success)
        {
            result.Success = false;
            result.IndexedPixels = new byte[0];
            result.Error = rowOrder.Error;
            return result;
        }

        byte[] rowMajorPixels = new byte[result.IndexedPixels.Length];
        int sourceRow = 0;
        while (sourceRow < height)
        {
            int destinationRow = rowOrder.Rows[sourceRow];
            int sourceOffset = sourceRow * width;
            int destinationOffset = destinationRow * width;
            CopyRow(result.IndexedPixels, sourceOffset, rowMajorPixels, destinationOffset, width);
            sourceRow = sourceRow + 1;
        }

        result.IndexedPixels = rowMajorPixels;
        return result;
    }

    internal GifDeinterlaceResult ValidateIndexedPixels(byte[] indexedPixels, int width, int height)
    {
        GifDeinterlaceResult result = new GifDeinterlaceResult();

        if (width < 0 || height < 0)
        {
            result.Error = CreateError("Interlaced image dimensions must be nonnegative.");
            return result;
        }

        long expectedPixelCount = (long)width * (long)height;
        if (expectedPixelCount > 2147483647)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.MemoryLimitExceeded, "Interlaced image is too large to allocate.", new GifDiagnosticPosition(-1, -1, "Interlace"));
            return result;
        }

        if (indexedPixels == null)
        {
            indexedPixels = new byte[0];
        }

        if (indexedPixels.Length != (int)expectedPixelCount)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "Interlaced indexed pixel count does not match image dimensions.", new GifDiagnosticPosition(-1, -1, "Interlace"));
            return result;
        }

        result.Success = true;
        result.IndexedPixels = indexedPixels;
        return result;
    }

    internal void CopyRow(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int width)
    {
        int column = 0;
        while (column < width)
        {
            destination[destinationOffset + column] = source[sourceOffset + column];
            column = column + 1;
        }
    }

    internal int FillPass(int[] rows, int height, int startRow, int step, int rowCount)
    {
        if (rows == null)
        {
            return rowCount;
        }

        int row = startRow;
        while (row < height)
        {
            rows[rowCount] = row;
            rowCount = rowCount + 1;
            row = row + step;
        }

        return rowCount;
    }

    internal GifErrorDiagnostic CreateError(string message)
    {
        return new GifErrorDiagnostic(GifErrorCode.InvalidImageRectangle, message, new GifDiagnosticPosition(-1, -1, "Interlace"));
    }
}
