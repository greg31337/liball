namespace chart;
/// <summary>
/// Represents a data column used by the chart model or rendering pipeline.
/// </summary>
public class ChartDataColumn : IChartDataColumn
{
    internal string Name;
    internal ChartDataKind Kind;
    internal int Length;
    internal double[] NumberValues;
    internal string? [] TextValues;
    internal System.DateTime[] DateTimeValues;
    internal string? [] CategoryValues;
    internal string? [] CategoryOrder;
    internal ChartDataMatrix? MatrixValue;
    /// <summary>
    /// Initializes a data column from the supplied name and kind.
    /// </summary>
    /// <param name = "name">The stable name used to identify the value or report argument errors.</param>
    /// <param name = "kind">The kind.</param>
    public ChartDataColumn(string name, ChartDataKind kind)
    {
        ValidateName(name);
        Name = name;
        Kind = kind;
        Length = 0;
        NumberValues = new double[0];
        TextValues = new string? [0];
        DateTimeValues = new System.DateTime[0];
        CategoryValues = new string? [0];
        CategoryOrder = new string? [0];
        MatrixValue = null;
    }

    internal static ChartDataColumn CreateNumber(string name, double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ChartDataColumn column = new ChartDataColumn(name, ChartDataKind.Number);
        column.NumberValues = ChartDataMatrix.CopyDoubleArray(values);
        column.Length = values.Length;
        return column;
    }

    internal static ChartDataColumn CreateText(string name, string? [] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ChartDataColumn column = new ChartDataColumn(name, ChartDataKind.Text);
        column.TextValues = CopyNullableStringArray(values);
        column.Length = values.Length;
        return column;
    }

    internal static ChartDataColumn CreateDateTime(string name, System.DateTime[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ChartDataColumn column = new ChartDataColumn(name, ChartDataKind.DateTime);
        column.DateTimeValues = CopyDateTimeArray(values);
        column.Length = values.Length;
        return column;
    }

    internal static ChartDataColumn CreateCategory(string name, string? [] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ChartDataColumn column = new ChartDataColumn(name, ChartDataKind.Category);
        column.CategoryValues = CopyNullableStringArray(values);
        column.CategoryOrder = BuildCategoryOrder(values);
        column.Length = values.Length;
        return column;
    }

    internal static ChartDataColumn CreateMatrix(string name, ChartDataMatrix matrix)
    {
        if (matrix == null)
        {
            throw new System.ArgumentNullException("matrix");
        }

        ChartDataColumn column = new ChartDataColumn(name, ChartDataKind.Matrix);
        column.MatrixValue = matrix.Copy();
        column.Length = matrix.Values.Length;
        return column;
    }

    /// <summary>
    /// Returns the requested number.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting double.</returns>
    public double GetNumber(int index)
    {
        EnsureKind(ChartDataKind.Number);
        ValidateIndex(index);
        return NumberValues[index];
    }

    /// <summary>
    /// Returns the requested text.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The text value, or <c>null</c> when the value is missing.</returns>
    public string? GetText(int index)
    {
        EnsureKind(ChartDataKind.Text);
        ValidateIndex(index);
        return TextValues[index];
    }

    /// <summary>
    /// Returns the requested date time.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting date time.</returns>
    public System.DateTime GetDateTime(int index)
    {
        EnsureKind(ChartDataKind.DateTime);
        ValidateIndex(index);
        return DateTimeValues[index];
    }

    /// <summary>
    /// Returns the requested category.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The category value, or <c>null</c> when the value is missing.</returns>
    public string? GetCategory(int index)
    {
        EnsureKind(ChartDataKind.Category);
        ValidateIndex(index);
        return CategoryValues[index];
    }

    /// <summary>
    /// Returns the requested matrix value.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    public double GetMatrixValue(int row, int column)
    {
        EnsureKind(ChartDataKind.Matrix);
        ChartDataMatrix matrix = GetMatrix();
        return matrix.GetValue(row, column);
    }

    /// <summary>
    /// Returns the requested matrix.
    /// </summary>
    /// <returns>The resulting data matrix.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public ChartDataMatrix GetMatrix()
    {
        EnsureKind(ChartDataKind.Matrix);
        ChartDataMatrix? matrix = MatrixValue;
        if (matrix == null)
        {
            throw new System.InvalidOperationException("Matrix column has no matrix value.");
        }

        return matrix;
    }

    /// <summary>
    /// Creates an independent copy of the number values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public double[] CopyNumberValues()
    {
        EnsureKind(ChartDataKind.Number);
        return ChartDataMatrix.CopyDoubleArray(NumberValues);
    }

    /// <summary>Returns a read-only zero-copy view of the numeric values.</summary>
    /// <returns>A read-only span backed by this numeric column.</returns>
    public System.ReadOnlySpan<double> GetNumberSpan()
    {
        EnsureKind(ChartDataKind.Number);
        return new System.ReadOnlySpan<double>(NumberValues);
    }

    /// <summary>
    /// Creates an independent copy of the text values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public string? [] CopyTextValues()
    {
        EnsureKind(ChartDataKind.Text);
        return CopyNullableStringArray(TextValues);
    }

    /// <summary>
    /// Creates an independent copy of the date time values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public System.DateTime[] CopyDateTimeValues()
    {
        EnsureKind(ChartDataKind.DateTime);
        return CopyDateTimeArray(DateTimeValues);
    }

    /// <summary>
    /// Creates an independent copy of the category values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public string? [] CopyCategoryValues()
    {
        EnsureKind(ChartDataKind.Category);
        return CopyNullableStringArray(CategoryValues);
    }

    /// <summary>
    /// Creates an independent copy of the category order.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public string? [] CopyCategoryOrder()
    {
        EnsureKind(ChartDataKind.Category);
        return CopyNullableStringArray(CategoryOrder);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartDataColumn Copy()
    {
        ChartDataColumn column = new ChartDataColumn(Name, Kind);
        column.Length = Length;
        column.NumberValues = ChartDataMatrix.CopyDoubleArray(NumberValues);
        column.TextValues = CopyNullableStringArray(TextValues);
        column.DateTimeValues = CopyDateTimeArray(DateTimeValues);
        column.CategoryValues = CopyNullableStringArray(CategoryValues);
        column.CategoryOrder = CopyNullableStringArray(CategoryOrder);
        if (MatrixValue != null)
        {
            column.MatrixValue = MatrixValue.Copy();
        }

        return column;
    }

    internal void ValidateIndex(int index)
    {
        if (index < 0 || index >= Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }

    internal void EnsureKind(ChartDataKind kind)
    {
        if (Kind != kind)
        {
            throw new System.InvalidOperationException("Column has a different data kind.");
        }
    }

    internal static void ValidateName(string name)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new System.ArgumentException("Column name cannot be empty.");
        }
    }

    internal static string? [] BuildCategoryOrder(string? [] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        string? [] unique = new string? [values.Length];
        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            string? value = values[index];
            if (!ContainsString(unique, count, value))
            {
                unique[count] = value;
                count = count + 1;
            }

            index = index + 1;
        }

        string? [] result = new string? [count];
        System.Array.Copy(unique, result, count);
        return result;
    }

    internal static bool ContainsString(string? [] values, int length, string? value)
    {
        int index = 0;
        while (index < length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string[] CopyStringArray(string[] values)
    {
        string[] copy = new string[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static string? [] CopyNullableStringArray(string? [] values)
    {
        string? [] copy = new string? [values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static System.DateTime[] CopyDateTimeArray(System.DateTime[] values)
    {
        System.DateTime[] copy = new System.DateTime[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    /// <summary>Returns the current name.</summary>
    /// <returns>The current field value.</returns>
    public string GetName()
    {
        return Name;
    }

    /// <summary>Replaces the current name.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetName(string value)
    {
        Name = value;
    }

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartDataKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartDataKind value)
    {
        Kind = value;
    }

    /// <summary>Returns the current length.</summary>
    /// <returns>The current field value.</returns>
    public int GetLength()
    {
        return Length;
    }

    /// <summary>Replaces the current length.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLength(int value)
    {
        Length = value;
    }

    /// <summary>Returns the current number values.</summary>
    /// <returns>The current field value.</returns>
    public double[] GetNumberValues()
    {
        return NumberValues;
    }

    /// <summary>Replaces the current number values.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetNumberValues(double[] value)
    {
        NumberValues = value;
    }

    /// <summary>Returns the current text values.</summary>
    /// <returns>The current field value.</returns>
    public string? [] GetTextValues()
    {
        return TextValues;
    }

    /// <summary>Replaces the current text values.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTextValues(string? [] value)
    {
        TextValues = value;
    }

    /// <summary>Returns the current date time values.</summary>
    /// <returns>The current field value.</returns>
    public System.DateTime[] GetDateTimeValues()
    {
        return DateTimeValues;
    }

    /// <summary>Replaces the current date time values.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetDateTimeValues(System.DateTime[] value)
    {
        DateTimeValues = value;
    }

    /// <summary>Returns the current category values.</summary>
    /// <returns>The current field value.</returns>
    public string? [] GetCategoryValues()
    {
        return CategoryValues;
    }

    /// <summary>Replaces the current category values.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCategoryValues(string? [] value)
    {
        CategoryValues = value;
    }

    /// <summary>Returns the current category order.</summary>
    /// <returns>The current field value.</returns>
    public string? [] GetCategoryOrder()
    {
        return CategoryOrder;
    }

    /// <summary>Replaces the current category order.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCategoryOrder(string? [] value)
    {
        CategoryOrder = value;
    }

    /// <summary>Replaces the current matrix value.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMatrixValue(ChartDataMatrix? value)
    {
        MatrixValue = value;
    }
}
