namespace qrcode;

internal class IntegerLookupTable
{
    int[][] Rows;

    internal IntegerLookupTable(int[][] rows)
    {
        if (rows == null)
        {
            throw new QrInputException("The lookup rows cannot be null.", "rows");
        }

        Rows = new int[rows.Length][];
        for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            int[] source = rows[rowIndex];
            if (source == null)
            {
                throw new QrInputException("A lookup row cannot be null.", "rows");
            }

            int[] destination = new int[source.Length];
            global::System.ReadOnlySpan<int> sourceSpan = source;
            sourceSpan.CopyTo(destination);
            Rows[rowIndex] = destination;
        }
    }

    internal int GetRowCount()
    {
        return Rows.Length;
    }

    internal int GetColumnCount(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Length)
        {
            throw new QrInputException("The row index is outside the lookup table.", "rowIndex");
        }

        return Rows[rowIndex].Length;
    }

    internal int GetValue(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Length)
        {
            throw new QrInputException("The row index is outside the lookup table.", "rowIndex");
        }

        int[] row = Rows[rowIndex];
        if (columnIndex < 0 || columnIndex >= row.Length)
        {
            throw new QrInputException("The column index is outside the lookup row.", "columnIndex");
        }

        return row[columnIndex];
    }

    internal int[] CreateRowCopy(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Length)
        {
            throw new QrInputException("The row index is outside the lookup table.", "rowIndex");
        }

        int[] source = Rows[rowIndex];
        int[] result = new int[source.Length];
        global::System.ReadOnlySpan<int> sourceSpan = source;
        sourceSpan.CopyTo(result);
        return result;
    }

    internal void CopyRow(int rowIndex, global::System.Span<int> destination)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Length)
        {
            throw new QrInputException("The row index is outside the lookup table.", "rowIndex");
        }

        int[] source = Rows[rowIndex];
        if (destination.Length < source.Length)
        {
            throw new QrInputException("The destination is shorter than the lookup row.", "destination");
        }

        global::System.ReadOnlySpan<int> sourceSpan = source;
        sourceSpan.CopyTo(destination);
    }
}
