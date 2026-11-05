namespace chart;
/// <summary>
/// Represents a data matrix used by the chart model or rendering pipeline.
/// </summary>
public class ChartDataMatrix : IChartDataMatrix
{
    internal int Rows;
    internal int Columns;
    internal double[] Values;
    /// <summary>
    /// Initializes a data matrix from the supplied rows and columns.
    /// </summary>
    /// <param name = "rows">The rows.</param>
    /// <param name = "columns">The columns.</param>
    public ChartDataMatrix(int rows, int columns)
    {
        int valueCount = CalculateValueCount(rows, columns);
        Rows = rows;
        Columns = columns;
        Values = new double[valueCount];
    }

    internal static ChartDataMatrix FromValues(int rows, int columns, double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ChartDataMatrix matrix = new ChartDataMatrix(rows, columns);
        if (values.Length != matrix.Values.Length)
        {
            throw new System.ArgumentException("Matrix value count must match rows times columns.");
        }

        System.Array.Copy(values, matrix.Values, values.Length);
        return matrix;
    }

    /// <summary>
    /// Returns the requested value.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    public double GetValue(int row, int column)
    {
        ValidateCoordinates(row, column);
        return Values[row * Columns + column];
    }

    /// <summary>
    /// Sets the value configuration.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <param name = "value">The value.</param>
    public void SetValue(int row, int column, double value)
    {
        ValidateCoordinates(row, column);
        Values[row * Columns + column] = value;
    }

    /// <summary>
    /// Creates an independent copy of the values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public double[] CopyValues()
    {
        return CopyDoubleArray(Values);
    }

    /// <summary>Returns a mutable zero-copy row-major view of all matrix values.</summary>
    /// <returns>A span backed by this matrix's values.</returns>
    public System.Span<double> GetValueSpan()
    {
        return new System.Span<double>(Values);
    }

    /// <summary>Returns a mutable zero-copy view of one matrix row.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A span backed by the requested matrix row.</returns>
    public System.Span<double> GetRowSpan(int row)
    {
        if (row < 0 || row >= Rows)
        {
            throw new System.ArgumentOutOfRangeException("row");
        }

        return new System.Span<double>(Values, row * Columns, Columns);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartDataMatrix Copy()
    {
        return FromValues(Rows, Columns, Values);
    }

    internal void ValidateCoordinates(int row, int column)
    {
        if (row < 0 || row >= Rows)
        {
            throw new System.ArgumentOutOfRangeException("row");
        }

        if (column < 0 || column >= Columns)
        {
            throw new System.ArgumentOutOfRangeException("column");
        }
    }

    internal static void ValidateDimensions(int rows, int columns)
    {
        if (rows <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rows");
        }

        if (columns <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columns");
        }
    }

    internal static int CalculateValueCount(int rows, int columns)
    {
        ValidateDimensions(rows, columns);
        long valueCount = (long)rows * columns;
        if (valueCount > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("columns");
        }

        return (int)valueCount;
    }

    internal static double[] CopyDoubleArray(double[] values)
    {
        double[] copy = new double[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    /// <summary>Returns the current rows.</summary>
    /// <returns>The current field value.</returns>
    public int GetRows()
    {
        return Rows;
    }

    /// <summary>Replaces the current rows.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRows(int value)
    {
        Rows = value;
    }

    /// <summary>Returns the current columns.</summary>
    /// <returns>The current field value.</returns>
    public int GetColumns()
    {
        return Columns;
    }

    /// <summary>Replaces the current columns.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumns(int value)
    {
        Columns = value;
    }

    /// <summary>Returns the current values.</summary>
    /// <returns>The current field value.</returns>
    public double[] GetValues()
    {
        return Values;
    }

    /// <summary>Replaces the current values.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetValues(double[] value)
    {
        Values = value;
    }
}
