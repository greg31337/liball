using System;
using System.Collections.Generic;

namespace csv;

/// <summary>
/// Stores column metadata and infers a simple schema from a bounded table sample.
/// </summary>
public class CsvSchema
{
    /// <summary>
    /// Column metadata in field order.
    /// </summary>
    public List<CsvColumn> Columns;
    /// <summary>
    /// Diagnostics associated with this object.
    /// </summary>
    public CsvDiagnosticList Diagnostics;

    /// <summary>
    /// Initializes a new CsvSchema with library defaults.
    /// </summary>
    public CsvSchema()
    {
        Columns = new List<CsvColumn>();
        Diagnostics = CsvDiagnosticList.Create();
    }

    /// <summary>
    /// Creates string-typed column metadata from header names.
    /// </summary>
    /// <param name="header">The header to process.</param>
    /// <returns>The initialized result.</returns>
    internal static CsvSchema CreateFromHeader(CsvHeader header)
    {
        CsvSchema schema = new CsvSchema();
        int index = 0;
        while (index < header.Names.Length)
        {
            CsvColumn column = CsvColumn.Create(header.Names[index], index);
            schema.AddColumn(column);
            index = index + 1;
        }

        return schema;
    }

    /// <summary>
    /// Infers column types and nullability from bounded table rows.
    /// </summary>
    /// <param name="table">The source table.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The inferred schema, with diagnostics for values that cannot be classified.</returns>
    internal static CsvSchema InferFromSample(CsvTable table, CsvInferenceOptions? options)
    {
        CsvInferenceOptions inferenceOptions = ResolveOptions(options);
        int rowCount = CountSampleRows(table, inferenceOptions);
        int columnCount = CountSampleColumns(table, rowCount);
        CsvSchema schema = CreateSampleColumns(table, columnCount);
        CsvColumnInference[] columnInferences = CreateColumnInferences(columnCount);

        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            CsvRow row = table.GetRow(rowIndex);
            int columnIndex = 0;
            while (columnIndex < columnCount)
            {
                string? value = "";
                bool isMissing = true;
                if (row.HasField(columnIndex))
                {
                    value = row.GetField(columnIndex);
                    isMissing = false;
                }

                columnInferences[columnIndex].AddValue(value, isMissing, inferenceOptions);
                columnIndex = columnIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }

        int index = 0;
        while (index < columnCount)
        {
            CsvColumn column = schema.GetColumn(index);
            column.TypeKind = columnInferences[index].ResolveType(inferenceOptions);
            column.IsNullable = columnInferences[index].ResolveNullable(inferenceOptions);
            column.FormatHint = columnInferences[index].ResolveFormatHint(column.TypeKind);
            column.SampledCount = columnInferences[index].SampledCount;
            column.MissingCount = columnInferences[index].MissingCount;
            column.NullCount = columnInferences[index].NullCount;
            column.EmptyCount = columnInferences[index].EmptyCount;
            column.NonNullCount = columnInferences[index].NonNullCount;
            index = index + 1;
        }

        return schema;
    }

    /// <summary>
    /// Appends column metadata.
    /// </summary>
    /// <param name="column">Column metadata used for formatting.</param>
    internal void AddColumn(CsvColumn column)
    {
        Columns.Add(column);
    }

    /// <summary>
    /// Returns the number of columns.
    /// </summary>
    /// <returns>The number of items in <see cref="Columns"/>.</returns>
    internal int Count()
    {
        int count = Columns.Count;
        return count;
    }

    /// <summary>
    /// Returns column metadata at a zero-based position.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The column metadata at <c>index</c>.</returns>
    internal CsvColumn GetColumn(int index)
    {
        CsvColumn column = Columns[index];
        return column;
    }

    /// <summary>
    /// Returns a column position by name, or -1 when absent.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The zero-based column position, or -1 when absent.</returns>
    internal int GetColumnIndex(string name)
    {
        int index = 0;
        while (index < Columns.Count)
        {
            CsvColumn column = Columns[index];
            if (column.Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvSchema Clone()
    {
        CsvSchema schema = new CsvSchema();
        int index = 0;
        while (index < Columns.Count)
        {
            CsvColumn column = Columns[index].Clone();
            schema.AddColumn(column);
            index = index + 1;
        }

        schema.Diagnostics = Diagnostics.Clone();
        return schema;
    }

    internal static CsvInferenceOptions ResolveOptions(CsvInferenceOptions? options)
    {
        if (options == null)
        {
            CsvInferenceOptions defaultOptions = CsvInferenceOptions.CreateDefault();
            return defaultOptions;
        }

        return options;
    }

    internal static int CountSampleRows(CsvTable table, CsvInferenceOptions options)
    {
        int availableRows = table.CountRows();
        if (options.MaxSampleRows <= 0)
        {
            return availableRows;
        }

        if (availableRows < options.MaxSampleRows)
        {
            return availableRows;
        }

        return options.MaxSampleRows;
    }

    internal static int CountSampleColumns(CsvTable table, int rowCount)
    {
        int columnCount = table.Header.Names.Length;
        if (table.Schema.Count() > columnCount)
        {
            columnCount = table.Schema.Count();
        }

        int rowIndex = 0;
        while (rowIndex < rowCount)
        {
            CsvRow row = table.GetRow(rowIndex);
            int fieldCount = row.CountFields();
            if (fieldCount > columnCount)
            {
                columnCount = fieldCount;
            }

            rowIndex = rowIndex + 1;
        }

        return columnCount;
    }

    internal static CsvSchema CreateSampleColumns(CsvTable table, int columnCount)
    {
        CsvSchema schema = new CsvSchema();
        int index = 0;
        while (index < columnCount)
        {
            string name = GetSampleColumnName(table, index);
            CsvColumn column = CsvColumn.Create(name, index);
            schema.AddColumn(column);
            index = index + 1;
        }

        return schema;
    }

    internal static string GetSampleColumnName(CsvTable table, int index)
    {
        if (index < table.Header.Names.Length)
        {
            return table.Header.Names[index];
        }

        if (index < table.Schema.Count())
        {
            CsvColumn column = table.Schema.GetColumn(index);
            return column.Name;
        }

        string generated = "Column" + ((long)index + 1).ToString();
        return generated;
    }

    internal static CsvColumnInference[] CreateColumnInferences(int columnCount)
    {
        CsvColumnInference[] inferences = new CsvColumnInference[columnCount];
        int index = 0;
        while (index < columnCount)
        {
            inferences[index] = new CsvColumnInference();
            index = index + 1;
        }

        return inferences;
    }
}
