using System.Collections.Generic;

namespace csv;

/// <summary>
/// Stores bounded CSV data and provides projection, sorting, grouping, aggregation, and joins.
/// </summary>
public class CsvTable
{
    /// <summary>
    /// The header used for name lookup.
    /// </summary>
    public CsvHeader Header;
    /// <summary>
    /// Rows held by this object.
    /// </summary>
    public List<CsvRow> Rows;
    /// <summary>
    /// Column metadata associated with the table.
    /// </summary>
    public CsvSchema Schema;
    /// <summary>
    /// Diagnostics associated with this object.
    /// </summary>
    public CsvDiagnosticList Diagnostics;

    /// <summary>
    /// Initializes a new CsvTable with library defaults.
    /// </summary>
    public CsvTable()
    {
        Header = new CsvHeader();
        Rows = new List<CsvRow>();
        Schema = new CsvSchema();
        Diagnostics = CsvDiagnosticList.Create();
    }

    /// <summary>
    /// Appends a row to the table.
    /// </summary>
    /// <param name="row">The row to process.</param>
    internal void AddRow(CsvRow row)
    {
        if (Header.Names.Length > 0)
        {
            row.Header = Header;
        }

        Rows.Add(row);
    }

    /// <summary>
    /// Returns the stored row count.
    /// </summary>
    /// <returns>The number of items in <see cref="Rows"/>.</returns>
    internal int CountRows()
    {
        int count = Rows.Count;
        return count;
    }

    /// <summary>
    /// Creates a table without the requested leading rows.
    /// </summary>
    /// <param name="count">The requested item count.</param>
    /// <returns>A cloned table containing rows after the skipped prefix.</returns>
    internal CsvTable SkipRows(int count)
    {
        CsvTable table = SliceRows(count, -1);
        return table;
    }

    /// <summary>
    /// Creates a table with at most the requested leading rows.
    /// </summary>
    /// <param name="count">The requested item count.</param>
    /// <returns>A cloned table containing at most <c>count</c> rows.</returns>
    internal CsvTable LimitRows(int count)
    {
        CsvTable table = SliceRows(0, count);
        return table;
    }

    /// <summary>
    /// Creates a table from a bounded row window.
    /// </summary>
    /// <param name="skip">Leading rows to omit.</param>
    /// <param name="limit">Maximum rows to include; zero means no limit.</param>
    /// <returns>A cloned table containing the requested row window.</returns>
    internal CsvTable SliceRows(int skip, int limit)
    {
        CsvTable table = new CsvTable();
        table.Header = Header.Clone();
        table.Schema = Schema.Clone();
        table.Diagnostics = Diagnostics.Clone();

        int start = skip;
        if (start < 0)
        {
            start = 0;
        }

        int added = 0;
        int index = start;
        while (index < Rows.Count && ShouldAddSlicedRow(added, limit))
        {
            CsvRow row = Rows[index].Clone();
            table.AddRow(row);
            added = added + 1;
            index = index + 1;
        }

        return table;
    }

    internal bool ShouldAddSlicedRow(int added, int limit)
    {
        if (limit < 0)
        {
            return true;
        }

        if (added < limit)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a row by zero-based position.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <returns>The row at <c>index</c>.</returns>
    internal CsvRow GetRow(int index)
    {
        CsvRow row = Rows[index];
        return row;
    }

    /// <summary>
    /// Returns a column position by name, or -1 when absent.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>The zero-based column position, or -1 when absent.</returns>
    internal int GetColumnIndex(string name)
    {
        if (Header.Names.Length > 0)
        {
            int headerIndex = Header.IndexOf(name);
            return headerIndex;
        }

        int schemaIndex = Schema.GetColumnIndex(name);
        return schemaIndex;
    }

    /// <summary>
    /// Creates a projection using zero-based column positions.
    /// </summary>
    /// <param name="indexes">Source column positions in result order.</param>
    /// <returns>A projected table with cloned header, schema, rows, and diagnostics.</returns>
    internal CsvTable SelectColumns(int[] indexes)
    {
        CsvTable table = CreateProjectedTable(indexes);
        return table;
    }

    /// <summary>
    /// Creates a projection using header names.
    /// </summary>
    /// <param name="names">Header names in result order.</param>
    /// <returns>A projected table with cloned header, schema, rows, and diagnostics.</returns>
    internal CsvTable SelectColumnsByName(string[] names)
    {
        int[] indexes = new int[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            indexes[index] = GetColumnIndex(names[index]);
            index = index + 1;
        }

        CsvTable table = CreateProjectedTable(indexes);
        return table;
    }

    /// <summary>
    /// Creates a projection in the supplied column order.
    /// </summary>
    /// <param name="indexes">Source column positions in result order.</param>
    /// <returns>A projected table with cloned header, schema, rows, and diagnostics.</returns>
    internal CsvTable ReorderColumns(int[] indexes)
    {
        CsvTable table = CreateProjectedTable(indexes);
        return table;
    }

    /// <summary>
    /// Clones the table and renames a column by name.
    /// </summary>
    /// <param name="oldName">The existing name.</param>
    /// <param name="newName">The replacement name.</param>
    /// <returns>A cloned table containing the renamed header and schema entry.</returns>
    internal CsvTable RenameColumn(string oldName, string newName)
    {
        CsvTable table = Clone();
        int index = table.GetColumnIndex(oldName);
        CsvTable renamed = table.RenameColumnAt(index, newName);
        return renamed;
    }

    /// <summary>
    /// Clones the table and renames a column by position.
    /// </summary>
    /// <param name="index">The zero-based position.</param>
    /// <param name="newName">The replacement name.</param>
    /// <returns>A cloned table containing the renamed header and schema entry.</returns>
    internal CsvTable RenameColumnAt(int index, string newName)
    {
        CsvTable table = Clone();
        table.ApplyColumnName(index, newName);
        return table;
    }

    /// <summary>
    /// Clones and lexically sorts by a column position.
    /// </summary>
    /// <param name="columnIndex">The zero-based column.</param>
    /// <param name="ascending">True for ascending order; false for descending.</param>
    /// <returns>A sorted clone; the original table is unchanged.</returns>
    internal CsvTable SortRowsByColumn(int columnIndex, bool ascending)
    {
        CsvTable table = Clone();
        table.SortRowsInPlace(columnIndex, ascending);
        return table;
    }

    /// <summary>
    /// Clones and lexically sorts by a named column.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="ascending">True for ascending order; false for descending.</param>
    /// <returns>A sorted clone; the original table is unchanged.</returns>
    internal CsvTable SortRowsByName(string name, bool ascending)
    {
        int columnIndex = GetColumnIndex(name);
        CsvTable table = SortRowsByColumn(columnIndex, ascending);
        return table;
    }

    /// <summary>
    /// Lexically sorts stored rows in place.
    /// </summary>
    /// <param name="columnIndex">The zero-based column.</param>
    /// <param name="ascending">True for ascending order; false for descending.</param>
    internal void SortRowsInPlace(int columnIndex, bool ascending)
    {
        bool swapped = true;
        int pass = 0;
        while (swapped)
        {
            swapped = false;
            int index = 0;
            while (index + 1 < Rows.Count - pass)
            {
                int comparison = CompareRowsByColumn(Rows[index], Rows[index + 1], columnIndex);
                if (ShouldSwapRows(comparison, ascending))
                {
                    SwapRows(index, index + 1);
                    swapped = true;
                }

                index = index + 1;
            }

            pass = pass + 1;
        }
    }

    internal int CompareRowsByColumn(CsvRow first, CsvRow second, int columnIndex)
    {
        string? firstValue = first.GetField(columnIndex);
        string? secondValue = second.GetField(columnIndex);
        int comparison = string.CompareOrdinal(firstValue, secondValue);
        return comparison;
    }

    internal bool ShouldSwapRows(int comparison, bool ascending)
    {
        if (ascending && comparison > 0)
        {
            return true;
        }

        if (!ascending && comparison < 0)
        {
            return true;
        }

        return false;
    }

    internal void SwapRows(int firstIndex, int secondIndex)
    {
        CsvRow first = Rows[firstIndex];
        Rows[firstIndex] = Rows[secondIndex];
        Rows[secondIndex] = first;
    }

    /// <summary>
    /// Joins matching key values and omits unmatched left rows.
    /// </summary>
    /// <param name="right">The right-hand table.</param>
    /// <param name="leftKeyIndex">The left key position.</param>
    /// <param name="rightKeyIndex">The right key position.</param>
    /// <returns>A new table containing each matching row pair.</returns>
    internal CsvTable InnerJoin(CsvTable right, int leftKeyIndex, int rightKeyIndex)
    {
        CsvTable table = CreateJoinedTable(right, rightKeyIndex);
        int leftIndex = 0;
        while (leftIndex < Rows.Count)
        {
            CsvRow leftRow = Rows[leftIndex];
            string? key = leftRow.GetField(leftKeyIndex);
            int rightIndex = 0;
            while (rightIndex < right.Rows.Count)
            {
                CsvRow rightRow = right.Rows[rightIndex];
                if (key == rightRow.GetField(rightKeyIndex))
                {
                    CsvRow row = CreateJoinedRow(leftRow, rightRow, right, rightKeyIndex);
                    table.AddRow(row);
                }

                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        return table;
    }

    /// <summary>
    /// Creates an inner join using named keys.
    /// </summary>
    /// <param name="right">The right-hand table.</param>
    /// <param name="leftKeyName">The left key name.</param>
    /// <param name="rightKeyName">The right key name.</param>
    /// <returns>A new table containing each matching row pair.</returns>
    internal CsvTable InnerJoinByName(CsvTable right, string leftKeyName, string rightKeyName)
    {
        int leftKeyIndex = GetColumnIndex(leftKeyName);
        int rightKeyIndex = right.GetColumnIndex(rightKeyName);
        CsvTable table = InnerJoin(right, leftKeyIndex, rightKeyIndex);
        return table;
    }

    /// <summary>
    /// Joins matching keys and null-fills unmatched right fields.
    /// </summary>
    /// <param name="right">The right-hand table.</param>
    /// <param name="leftKeyIndex">The left key position.</param>
    /// <param name="rightKeyIndex">The right key position.</param>
    /// <returns>A new table containing matches plus unmatched left rows with null right fields.</returns>
    internal CsvTable LeftJoin(CsvTable right, int leftKeyIndex, int rightKeyIndex)
    {
        CsvTable table = CreateJoinedTable(right, rightKeyIndex);
        int leftIndex = 0;
        while (leftIndex < Rows.Count)
        {
            CsvRow leftRow = Rows[leftIndex];
            string? key = leftRow.GetField(leftKeyIndex);
            bool matched = false;
            int rightIndex = 0;
            while (rightIndex < right.Rows.Count)
            {
                CsvRow rightRow = right.Rows[rightIndex];
                if (key == rightRow.GetField(rightKeyIndex))
                {
                    CsvRow row = CreateJoinedRow(leftRow, rightRow, right, rightKeyIndex);
                    table.AddRow(row);
                    matched = true;
                }

                rightIndex = rightIndex + 1;
            }

            if (!matched)
            {
                CsvRow row = CreateUnmatchedLeftJoinRow(leftRow, right, rightKeyIndex);
                table.AddRow(row);
            }

            leftIndex = leftIndex + 1;
        }

        return table;
    }

    /// <summary>
    /// Creates a left join using named keys.
    /// </summary>
    /// <param name="right">The right-hand table.</param>
    /// <param name="leftKeyName">The left key name.</param>
    /// <param name="rightKeyName">The right key name.</param>
    /// <returns>A new table containing matches plus unmatched left rows with null right fields.</returns>
    internal CsvTable LeftJoinByName(CsvTable right, string leftKeyName, string rightKeyName)
    {
        int leftKeyIndex = GetColumnIndex(leftKeyName);
        int rightKeyIndex = right.GetColumnIndex(rightKeyName);
        CsvTable table = LeftJoin(right, leftKeyIndex, rightKeyIndex);
        return table;
    }

    /// <summary>
    /// Groups rows by exact field text at a column position.
    /// </summary>
    /// <param name="columnIndex">The zero-based column.</param>
    /// <returns>Results in first-seen key order.</returns>
    internal CsvGroup[] GroupRowsByColumn(int columnIndex)
    {
        List<CsvGroup> groups = new List<CsvGroup>();
        int index = 0;
        while (index < Rows.Count)
        {
            CsvRow row = Rows[index];
            string? key = row.GetField(columnIndex);
            CsvGroup group = FindOrCreateGroup(groups, key);
            group.AddRow(row.Clone());
            index = index + 1;
        }

        return groups.ToArray();
    }

    /// <summary>
    /// Groups rows by exact field text in a named column.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>Results in first-seen key order.</returns>
    internal CsvGroup[] GroupRowsByName(string name)
    {
        int columnIndex = GetColumnIndex(name);
        CsvGroup[] groups = GroupRowsByColumn(columnIndex);
        return groups;
    }

    /// <summary>
    /// Counts rows for each distinct key in a column.
    /// </summary>
    /// <param name="columnIndex">The zero-based column.</param>
    /// <returns>Results in first-seen key order.</returns>
    internal CsvAggregate[] CountRowsByColumn(int columnIndex)
    {
        CsvGroup[] groups = GroupRowsByColumn(columnIndex);
        CsvAggregate[] aggregates = new CsvAggregate[groups.Length];
        int index = 0;
        while (index < groups.Length)
        {
            CsvAggregate aggregate = CsvAggregate.Create(groups[index].Key);
            aggregate.Count = groups[index].Count;
            aggregates[index] = aggregate;
            index = index + 1;
        }

        return aggregates;
    }

    /// <summary>
    /// Counts rows for each distinct key in a named column.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <returns>Results in first-seen key order.</returns>
    internal CsvAggregate[] CountRowsByName(string name)
    {
        int columnIndex = GetColumnIndex(name);
        CsvAggregate[] aggregates = CountRowsByColumn(columnIndex);
        return aggregates;
    }

    /// <summary>
    /// Counts and sums decimal values for each key.
    /// </summary>
    /// <param name="groupColumnIndex">The grouping column position.</param>
    /// <param name="valueColumnIndex">The decimal value column position.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>Results in first-seen key order.</returns>
    internal CsvAggregate[] SumRowsByColumn(int groupColumnIndex, int valueColumnIndex, CsvConversionOptions? options)
    {
        CsvConversionOptions conversionOptions = ResolveConversionOptions(options);
        List<CsvAggregate> aggregates = new List<CsvAggregate>();
        int index = 0;
        while (index < Rows.Count)
        {
            CsvRow row = Rows[index];
            string? key = row.GetField(groupColumnIndex);
            CsvAggregate aggregate = FindOrCreateAggregate(aggregates, key);
            aggregate.Count = aggregate.Count + 1;
            aggregate.Sum = aggregate.Sum + CsvConverter.ToDecimal(row.GetField(valueColumnIndex), conversionOptions);
            index = index + 1;
        }

        return aggregates.ToArray();
    }

    /// <summary>
    /// Counts and sums decimal values using named columns.
    /// </summary>
    /// <param name="groupName">The grouping column name.</param>
    /// <param name="valueName">The decimal value column name.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>Results in first-seen key order.</returns>
    internal CsvAggregate[] SumRowsByName(string groupName, string valueName, CsvConversionOptions? options)
    {
        int groupColumnIndex = GetColumnIndex(groupName);
        int valueColumnIndex = GetColumnIndex(valueName);
        CsvAggregate[] aggregates = SumRowsByColumn(groupColumnIndex, valueColumnIndex, options);
        return aggregates;
    }

    internal CsvGroup FindOrCreateGroup(List<CsvGroup> groups, string? key)
    {
        int index = 0;
        while (index < groups.Count)
        {
            if (groups[index].Key == key)
            {
                return groups[index];
            }

            index = index + 1;
        }

        CsvGroup group = CsvGroup.Create(key);
        groups.Add(group);
        return group;
    }

    internal CsvAggregate FindOrCreateAggregate(List<CsvAggregate> aggregates, string? key)
    {
        int index = 0;
        while (index < aggregates.Count)
        {
            if (aggregates[index].Key == key)
            {
                return aggregates[index];
            }

            index = index + 1;
        }

        CsvAggregate aggregate = CsvAggregate.Create(key);
        aggregates.Add(aggregate);
        return aggregate;
    }

    internal CsvConversionOptions ResolveConversionOptions(CsvConversionOptions? options)
    {
        if (options == null)
        {
            CsvConversionOptions defaultOptions = CsvConversionOptions.CreateDefault();
            return defaultOptions;
        }

        return options;
    }

    internal CsvTable CreateJoinedTable(CsvTable right, int rightKeyIndex)
    {
        CsvTable table = new CsvTable();
        table.Header = CreateJoinedHeader(right, rightKeyIndex);
        table.Schema = CreateJoinedSchema(right, rightKeyIndex, table.Header);
        table.Diagnostics = Diagnostics.Clone();
        return table;
    }

    internal CsvHeader CreateJoinedHeader(CsvTable right, int rightKeyIndex)
    {
        CsvHeader header = Header.Clone();
        int leftColumnCount = CountColumns();
        int rightColumnCount = right.CountColumns();
        int joinedColumnCount = CountJoinedColumns(right, rightKeyIndex);
        header.Names = new string[joinedColumnCount];

        int index = 0;
        while (index < leftColumnCount)
        {
            header.Names[index] = GetColumnName(index);
            index = index + 1;
        }

        int rightIndex = 0;
        int targetIndex = leftColumnCount;
        while (rightIndex < rightColumnCount)
        {
            if (rightIndex != rightKeyIndex)
            {
                string name = right.GetColumnName(rightIndex);
                header.Names[targetIndex] = ResolveJoinedColumnName(header, name, targetIndex);
                targetIndex = targetIndex + 1;
            }

            rightIndex = rightIndex + 1;
        }

        return header;
    }

    internal string ResolveJoinedColumnName(CsvHeader header, string name, int assignedCount)
    {
        if (!HasJoinedColumnName(header, name, assignedCount))
        {
            return name;
        }

        string suffixBase = name + "_right";
        string resolved = suffixBase;
        int suffix = 2;
        while (HasJoinedColumnName(header, resolved, assignedCount))
        {
            resolved = suffixBase + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            suffix = suffix + 1;
        }

        return resolved;
    }

    internal bool HasJoinedColumnName(CsvHeader header, string name, int assignedCount)
    {
        string target = header.Normalize(name);
        int index = 0;
        while (index < assignedCount)
        {
            string current = header.Normalize(header.Names[index]);
            if (current == target)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal CsvSchema CreateJoinedSchema(CsvTable right, int rightKeyIndex, CsvHeader header)
    {
        CsvSchema schema = new CsvSchema();
        schema.Diagnostics = Schema.Diagnostics.Clone();
        int leftColumnCount = CountColumns();
        int rightColumnCount = right.CountColumns();

        int index = 0;
        while (index < leftColumnCount)
        {
            CsvColumn column = GetProjectedColumn(index, header.Names[index], index);
            schema.AddColumn(column);
            index = index + 1;
        }

        int rightIndex = 0;
        int targetIndex = leftColumnCount;
        while (rightIndex < rightColumnCount)
        {
            if (rightIndex != rightKeyIndex)
            {
                CsvColumn column = right.GetProjectedColumn(rightIndex, header.Names[targetIndex], targetIndex);
                schema.AddColumn(column);
                targetIndex = targetIndex + 1;
            }

            rightIndex = rightIndex + 1;
        }

        return schema;
    }

    /// <summary>
    /// Returns the widest header, schema, or stored row.
    /// </summary>
    /// <returns>The maximum column count represented by table metadata or rows.</returns>
    internal int CountColumns()
    {
        if (Header.Names.Length > 0)
        {
            return Header.Names.Length;
        }

        if (Schema.Count() > 0)
        {
            return Schema.Count();
        }

        int count = 0;
        int rowIndex = 0;
        while (rowIndex < Rows.Count)
        {
            int rowFieldCount = Rows[rowIndex].CountFields();
            if (rowFieldCount > count)
            {
                count = rowFieldCount;
            }

            rowIndex = rowIndex + 1;
        }

        return count;
    }

    internal int CountJoinedColumns(CsvTable right, int rightKeyIndex)
    {
        int count = CountColumns();
        int rightColumnCount = right.CountColumns();
        int rightIndex = 0;
        while (rightIndex < rightColumnCount)
        {
            if (rightIndex != rightKeyIndex)
            {
                count = count + 1;
            }

            rightIndex = rightIndex + 1;
        }

        return count;
    }

    internal CsvRow CreateJoinedRow(CsvRow leftRow, CsvRow rightRow, CsvTable right, int rightKeyIndex)
    {
        int leftColumnCount = CountColumns();
        int rightColumnCount = right.CountColumns();
        int joinedColumnCount = CountJoinedColumns(right, rightKeyIndex);
        string?[] fields = new string?[joinedColumnCount];

        int index = 0;
        while (index < leftColumnCount)
        {
            fields[index] = leftRow.GetField(index);
            index = index + 1;
        }

        int rightIndex = 0;
        int targetIndex = leftColumnCount;
        while (rightIndex < rightColumnCount)
        {
            if (rightIndex != rightKeyIndex)
            {
                fields[targetIndex] = rightRow.GetField(rightIndex);
                targetIndex = targetIndex + 1;
            }

            rightIndex = rightIndex + 1;
        }

        CsvRow row = CsvRow.Create(fields, leftRow.RowNumber);
        row.Line = leftRow.Line;
        row.Column = leftRow.Column;
        row.CharacterOffset = leftRow.CharacterOffset;
        row.ByteOffset = leftRow.ByteOffset;
        row.RawText = leftRow.RawText;
        row.Diagnostics = leftRow.Diagnostics.Clone();
        return row;
    }

    internal CsvRow CreateUnmatchedLeftJoinRow(CsvRow leftRow, CsvTable right, int rightKeyIndex)
    {
        int leftColumnCount = CountColumns();
        int rightColumnCount = right.CountColumns();
        int joinedColumnCount = CountJoinedColumns(right, rightKeyIndex);
        string?[] fields = new string?[joinedColumnCount];

        int index = 0;
        while (index < leftColumnCount)
        {
            fields[index] = leftRow.GetField(index);
            index = index + 1;
        }

        int rightIndex = 0;
        int targetIndex = leftColumnCount;
        while (rightIndex < rightColumnCount)
        {
            if (rightIndex != rightKeyIndex)
            {
                fields[targetIndex] = null;
                targetIndex = targetIndex + 1;
            }

            rightIndex = rightIndex + 1;
        }

        CsvRow row = CsvRow.Create(fields, leftRow.RowNumber);
        row.Line = leftRow.Line;
        row.Column = leftRow.Column;
        row.CharacterOffset = leftRow.CharacterOffset;
        row.ByteOffset = leftRow.ByteOffset;
        row.RawText = leftRow.RawText;
        row.Diagnostics = leftRow.Diagnostics.Clone();
        return row;
    }

    internal CsvTable CreateProjectedTable(int[] indexes)
    {
        CsvTable table = new CsvTable();
        table.Header = CreateProjectedHeader(indexes);
        table.Schema = CreateProjectedSchema(indexes, table.Header);
        table.Diagnostics = Diagnostics.Clone();

        int rowIndex = 0;
        while (rowIndex < Rows.Count)
        {
            CsvRow row = CreateProjectedRow(Rows[rowIndex], indexes);
            table.AddRow(row);
            rowIndex = rowIndex + 1;
        }

        return table;
    }

    internal CsvHeader CreateProjectedHeader(int[] indexes)
    {
        CsvHeader header = Header.Clone();
        header.Names = new string[indexes.Length];
        int index = 0;
        while (index < indexes.Length)
        {
            header.Names[index] = GetColumnName(indexes[index]);
            index = index + 1;
        }

        return header;
    }

    internal CsvSchema CreateProjectedSchema(int[] indexes, CsvHeader header)
    {
        CsvSchema schema = new CsvSchema();
        schema.Diagnostics = Schema.Diagnostics.Clone();
        int index = 0;
        while (index < indexes.Length)
        {
            CsvColumn column = GetProjectedColumn(indexes[index], header.Names[index], index);
            schema.AddColumn(column);
            index = index + 1;
        }

        return schema;
    }

    internal CsvColumn GetProjectedColumn(int sourceIndex, string name, int targetIndex)
    {
        CsvColumn column = new CsvColumn();
        if (sourceIndex >= 0 && sourceIndex < Schema.Count())
        {
            column = Schema.GetColumn(sourceIndex).Clone();
        }
        else
        {
            column = CsvColumn.Create(name, targetIndex);
        }

        column.Name = name;
        column.Index = targetIndex;
        return column;
    }

    internal CsvRow CreateProjectedRow(CsvRow source, int[] indexes)
    {
        string?[] fields = new string?[indexes.Length];
        int index = 0;
        while (index < indexes.Length)
        {
            fields[index] = source.GetField(indexes[index]);
            index = index + 1;
        }

        CsvRow row = CsvRow.Create(fields, source.RowNumber);
        row.Line = source.Line;
        row.Column = source.Column;
        row.CharacterOffset = source.CharacterOffset;
        row.ByteOffset = source.ByteOffset;
        row.RawText = source.RawText;
        row.Diagnostics = source.Diagnostics.Clone();
        return row;
    }

    internal string GetColumnName(int index)
    {
        if (index >= 0 && index < Header.Names.Length)
        {
            return Header.Names[index];
        }

        if (index >= 0 && index < Schema.Count())
        {
            return Schema.GetColumn(index).Name;
        }

        return "";
    }

    internal void ApplyColumnName(int index, string newName)
    {
        if (index < 0)
        {
            return;
        }

        if (index < Header.Names.Length)
        {
            Header.Names[index] = newName;
        }

        if (index < Schema.Count())
        {
            Schema.GetColumn(index).Name = newName;
        }

        int rowIndex = 0;
        while (rowIndex < Rows.Count)
        {
            Rows[rowIndex].Header = Header;
            rowIndex = rowIndex + 1;
        }
    }

    /// <summary>
    /// Creates an independent copy so later changes do not affect the original.
    /// </summary>
    /// <returns>An independent copy.</returns>
    internal CsvTable Clone()
    {
        CsvTable table = new CsvTable();
        table.Header = Header.Clone();
        table.Schema = Schema.Clone();
        table.Diagnostics = Diagnostics.Clone();

        int index = 0;
        while (index < Rows.Count)
        {
            CsvRow row = Rows[index].Clone();
            table.AddRow(row);
            index = index + 1;
        }

        return table;
    }
}
