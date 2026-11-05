namespace chart;

/// <summary>
/// Checks data data and returns structured validation issues without rendering it.
/// </summary>
internal static class ChartDataValidatorOperations
{
    /// <summary>
    /// The stable validation code for empty series.
    /// </summary>
    public const string EmptySeriesCode = "EmptySeries";
    /// <summary>
    /// The stable validation code for mismatched length.
    /// </summary>
    public const string MismatchedLengthCode = "MismatchedLength";
    /// <summary>
    /// The stable validation code for na n.
    /// </summary>
    public const string NaNCode = "NaN";
    /// <summary>
    /// The stable validation code for infinity.
    /// </summary>
    public const string InfinityCode = "Infinity";
    /// <summary>
    /// The stable validation code for missing category.
    /// </summary>
    public const string MissingCategoryCode = "MissingCategory";
    /// <summary>
    /// The stable validation code for invalid dimensions.
    /// </summary>
    public const string InvalidDimensionsCode = "InvalidDimensions";

    /// <summary>
    /// Validates number series and returns structured issues without rendering.
    /// </summary>
    /// <param name="seriesName">The series name.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidateNumberSeries(string seriesName, double[] xValues, double[] yValues)
    {
        if (xValues == null)
        {
            throw new System.ArgumentNullException("xValues");
        }

        if (yValues == null)
        {
            throw new System.ArgumentNullException("yValues");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (xValues.Length == 0 || yValues.Length == 0)
        {
            result.AddError(EmptySeriesCode, "Series has no data values.", seriesName, -1);
        }

        if (xValues.Length != yValues.Length)
        {
            result.AddError(MismatchedLengthCode, "Series x and y value counts differ.", seriesName, -1);
        }

        ValidateDoubleArray(xValues, "x", result);
        ValidateDoubleArray(yValues, "y", result);
        return result;
    }

    /// <summary>
    /// Validates table and returns structured issues without rendering.
    /// </summary>
    /// <param name="table">The column-oriented data table that supplies encoded fields.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidateTable(ChartDataTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (table.ColumnCount < 0 || table.RowCount < 0 || table.Columns == null || table.ColumnCount > table.Columns.Length)
        {
            result.AddError(InvalidDimensionsCode, "Table dimensions are invalid.", "", -1);
            return result;
        }

        if (table.ColumnCount == 0 || table.RowCount == 0)
        {
            result.AddError(EmptySeriesCode, "Table has no data rows or columns.", "", -1);
        }

        int index = 0;
        while (index < table.ColumnCount)
        {
            ChartDataColumn column = table.Columns[index];
            if (column == null)
            {
                result.AddError(InvalidDimensionsCode, "Table contains a missing column.", "", index);
            }
            else
            {
                if (column.Length != table.RowCount)
                {
                    result.AddError(MismatchedLengthCode, "Column length does not match table row count.", column.Name, -1);
                }

                int previous = 0;
                while (previous < index)
                {
                    ChartDataColumn earlier = table.Columns[previous];
                    if (earlier != null && earlier.Name == column.Name)
                    {
                        result.AddError(ChartConfigurationValidatorConstants.DuplicateIdentifierCode, "Table column name is duplicated.", column.Name, index);
                    }
                    previous = previous + 1;
                }

                ValidateColumnInto(column, result);
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Validates column and returns structured issues without rendering.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidateColumn(ChartDataColumn column)
    {
        if (column == null)
        {
            throw new System.ArgumentNullException("column");
        }

        ChartValidationResult result = new ChartValidationResult();
        ValidateColumnInto(column, result);
        return result;
    }

    /// <summary>
    /// Validates matrix and returns structured issues without rendering.
    /// </summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidateMatrix(ChartDataMatrix matrix)
    {
        if (matrix == null)
        {
            throw new System.ArgumentNullException("matrix");
        }

        ChartValidationResult result = new ChartValidationResult();
        ValidateMatrixInto(matrix, "matrix", result);
        return result;
    }

    internal static void ValidateColumnInto(ChartDataColumn column, ChartValidationResult result)
    {
        if (column.Name == null || column.Name.Length == 0 || column.Length < 0 || (int)column.Kind < 0 || (int)column.Kind > 4)
        {
            result.AddError(InvalidDimensionsCode, "Column identity, kind, or length is invalid.", "column", -1);
            return;
        }
        if (column.Length == 0)
        {
            result.AddError(EmptySeriesCode, "Column has no data values.", column.Name, -1);
        }

        if (column.Kind == ChartDataKind.Number)
        {
            if (column.NumberValues == null || column.NumberValues.Length != column.Length)
            {
                result.AddError(InvalidDimensionsCode, "Numeric column storage does not match its length.", column.Name, -1);
                return;
            }
            ValidateDoubleArray(column.NumberValues, column.Name, result);
        }
        else if (column.Kind == ChartDataKind.Text)
        {
            if (column.TextValues == null || column.TextValues.Length != column.Length)
            {
                result.AddError(InvalidDimensionsCode, "Text column storage does not match its length.", column.Name, -1);
            }
        }
        else if (column.Kind == ChartDataKind.DateTime)
        {
            if (column.DateTimeValues == null || column.DateTimeValues.Length != column.Length)
            {
                result.AddError(InvalidDimensionsCode, "Date-time column storage does not match its length.", column.Name, -1);
            }
        }
        else if (column.Kind == ChartDataKind.Category)
        {
            if (column.CategoryValues == null || column.CategoryValues.Length != column.Length || column.CategoryOrder == null)
            {
                result.AddError(InvalidDimensionsCode, "Category column storage does not match its length.", column.Name, -1);
                return;
            }
            ValidateCategoryColumn(column, result);
        }
        else if (column.Kind == ChartDataKind.Matrix)
        {
            ChartDataMatrix? matrix = column.MatrixValue;
            if (matrix == null)
            {
                result.AddError(InvalidDimensionsCode, "Matrix column has no matrix value.", column.Name, -1);
            }
            else
            {
                if (column.Length != matrix.Values.Length)
                {
                    result.AddError(MismatchedLengthCode, "Matrix column length does not match its matrix value count.", column.Name, -1);
                }
                ValidateMatrixInto(matrix, column.Name, result);
            }
        }
    }

    internal static void ValidateDoubleArray(double[] values, string columnName, ChartValidationResult result)
    {
        if (values == null)
        {
            result.AddError(InvalidDimensionsCode, "Numeric data array is missing.", columnName, -1);
            return;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (double.IsNaN(values[index]))
            {
                result.AddError(NaNCode, "Numeric data contains NaN.", columnName, index);
            }
            else if (double.IsInfinity(values[index]))
            {
                result.AddError(InfinityCode, "Numeric data contains infinity.", columnName, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidateCategoryColumn(ChartDataColumn column, ChartValidationResult result)
    {
        if (column.CategoryValues == null)
        {
            result.AddError(InvalidDimensionsCode, "Category data array is missing.", column.Name, -1);
            return;
        }

        int index = 0;
        while (index < column.CategoryValues.Length)
        {
            string? value = column.CategoryValues[index];
            if (value == null || value.Length == 0)
            {
                result.AddError(MissingCategoryCode, "Category value is missing.", column.Name, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidateMatrixInto(ChartDataMatrix matrix, string columnName, ChartValidationResult result)
    {
        if (matrix.Rows <= 0 || matrix.Columns <= 0 || matrix.Values == null || matrix.Values.Length != (long)matrix.Rows * matrix.Columns)
        {
            result.AddError(InvalidDimensionsCode, "Matrix dimensions are invalid.", columnName, -1);
            return;
        }

        ValidateDoubleArray(matrix.Values, columnName, result);
    }
}
