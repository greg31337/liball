namespace csv;

/// <summary>
/// Provides bounded access and non-mutating transformations for in-memory CSV tables.
/// </summary>
/// <remarks>Join results retain left column names and rename colliding right columns with <c>_right</c>, followed by a numeric suffix when needed.</remarks>
public interface ICsvTableOperations
{
    /// <summary>Returns the number of stored rows.</summary>
    /// <param name="table">The table to inspect.</param>
    /// <returns>The number of rows in the table.</returns>
    int CountRows(CsvTable table);

    /// <summary>Returns a row by zero-based position.</summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="index">The zero-based row position.</param>
    /// <returns>The row at <c>index</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The index is outside the table.</exception>
    CsvRow GetRow(CsvTable table, int index);

    /// <summary>Creates an independent copy of a table.</summary>
    /// <param name="table">The table to clone.</param>
    /// <returns>An independent copy of the table.</returns>
    CsvTable Clone(CsvTable table);

    /// <summary>Creates a table without the requested leading rows.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="count">The number of leading rows to omit; negative values omit none.</param>
    /// <returns>An independent table containing the remaining rows.</returns>
    CsvTable SkipRows(CsvTable table, int count);

    /// <summary>Creates a table containing at most the requested leading rows.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="count">The maximum row count; negative values include every row.</param>
    /// <returns>An independent table containing the requested leading rows.</returns>
    CsvTable LimitRows(CsvTable table, int count);

    /// <summary>Creates a table from a bounded row window.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="skip">The number of leading rows to omit; negative values omit none.</param>
    /// <param name="limit">The maximum row count; negative values include every remaining row.</param>
    /// <returns>An independent table containing the requested row window.</returns>
    CsvTable SliceRows(CsvTable table, int skip, int limit);

    /// <summary>Returns a column position by name.</summary>
    /// <param name="table">The table to inspect.</param>
    /// <param name="name">The column name.</param>
    /// <returns>The zero-based column position, or -1 when absent.</returns>
    int GetColumnIndex(CsvTable table, string name);

    /// <summary>Projects columns by zero-based position.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="indexes">Source column positions in result order.</param>
    /// <returns>An independent projected table.</returns>
    CsvTable SelectColumns(CsvTable table, int[] indexes);

    /// <summary>Projects columns by name.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="names">Source column names in result order.</param>
    /// <returns>An independent projected table.</returns>
    CsvTable SelectColumnsByName(CsvTable table, string[] names);

    /// <summary>Creates a projection in the supplied column order.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="indexes">Source column positions in result order.</param>
    /// <returns>An independent reordered table.</returns>
    CsvTable ReorderColumns(CsvTable table, int[] indexes);

    /// <summary>Clones a table and renames a column selected by name.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="oldName">The existing column name.</param>
    /// <param name="newName">The replacement column name.</param>
    /// <returns>An independent table containing the renamed column.</returns>
    CsvTable RenameColumn(CsvTable table, string oldName, string newName);

    /// <summary>Clones a table and renames a column selected by position.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="index">The zero-based column position.</param>
    /// <param name="newName">The replacement column name.</param>
    /// <returns>An independent table containing the renamed column.</returns>
    CsvTable RenameColumnAt(CsvTable table, int index, string newName);

    /// <summary>Lexically sorts a cloned table by column position.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="columnIndex">The zero-based column position.</param>
    /// <param name="ascending">True for ascending order; false for descending order.</param>
    /// <returns>An independently sorted table.</returns>
    CsvTable SortRowsByColumn(CsvTable table, int columnIndex, bool ascending);

    /// <summary>Lexically sorts a cloned table by column name.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="name">The column name.</param>
    /// <param name="ascending">True for ascending order; false for descending order.</param>
    /// <returns>An independently sorted table.</returns>
    CsvTable SortRowsByName(CsvTable table, string name, bool ascending);

    /// <summary>Groups cloned rows by exact field text at a column position.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="columnIndex">The zero-based column position.</param>
    /// <returns>Groups in first-seen key order.</returns>
    CsvGroup[] GroupRowsByColumn(CsvTable table, int columnIndex);

    /// <summary>Groups cloned rows by exact field text in a named column.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="name">The column name.</param>
    /// <returns>Groups in first-seen key order.</returns>
    CsvGroup[] GroupRowsByName(CsvTable table, string name);

    /// <summary>Counts rows for each distinct key at a column position.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="columnIndex">The zero-based column position.</param>
    /// <returns>Aggregates in first-seen key order.</returns>
    CsvAggregate[] CountRowsByColumn(CsvTable table, int columnIndex);

    /// <summary>Counts rows for each distinct key in a named column.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="name">The column name.</param>
    /// <returns>Aggregates in first-seen key order.</returns>
    CsvAggregate[] CountRowsByName(CsvTable table, string name);

    /// <summary>Counts rows and sums decimal values for each key selected by column position.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="groupColumnIndex">The zero-based grouping column position.</param>
    /// <param name="valueColumnIndex">The zero-based decimal value column position.</param>
    /// <param name="options">Conversion options; null uses library defaults.</param>
    /// <returns>Aggregates in first-seen key order.</returns>
    CsvAggregate[] SumRowsByColumn(CsvTable table, int groupColumnIndex, int valueColumnIndex, CsvConversionOptions? options);

    /// <summary>Counts rows and sums decimal values for each key selected by column name.</summary>
    /// <param name="table">The source table.</param>
    /// <param name="groupName">The grouping column name.</param>
    /// <param name="valueName">The decimal value column name.</param>
    /// <param name="options">Conversion options; null uses library defaults.</param>
    /// <returns>Aggregates in first-seen key order.</returns>
    CsvAggregate[] SumRowsByName(CsvTable table, string groupName, string valueName, CsvConversionOptions? options);

    /// <summary>Joins matching key values selected by column position.</summary>
    /// <param name="left">The left-hand table.</param>
    /// <param name="right">The right-hand table.</param>
    /// <param name="leftKeyIndex">The zero-based left key position.</param>
    /// <param name="rightKeyIndex">The zero-based right key position.</param>
    /// <returns>An independent table containing every matching row pair.</returns>
    CsvTable InnerJoin(CsvTable left, CsvTable right, int leftKeyIndex, int rightKeyIndex);

    /// <summary>Joins matching key values selected by column name.</summary>
    /// <param name="left">The left-hand table.</param>
    /// <param name="right">The right-hand table.</param>
    /// <param name="leftKeyName">The left key name.</param>
    /// <param name="rightKeyName">The right key name.</param>
    /// <returns>An independent table containing every matching row pair.</returns>
    CsvTable InnerJoinByName(CsvTable left, CsvTable right, string leftKeyName, string rightKeyName);

    /// <summary>Joins matching keys selected by column position and retains unmatched left rows.</summary>
    /// <param name="left">The left-hand table.</param>
    /// <param name="right">The right-hand table.</param>
    /// <param name="leftKeyIndex">The zero-based left key position.</param>
    /// <param name="rightKeyIndex">The zero-based right key position.</param>
    /// <returns>An independent table with null right fields on unmatched rows.</returns>
    CsvTable LeftJoin(CsvTable left, CsvTable right, int leftKeyIndex, int rightKeyIndex);

    /// <summary>Joins matching keys selected by column name and retains unmatched left rows.</summary>
    /// <param name="left">The left-hand table.</param>
    /// <param name="right">The right-hand table.</param>
    /// <param name="leftKeyName">The left key name.</param>
    /// <param name="rightKeyName">The right key name.</param>
    /// <returns>An independent table with null right fields on unmatched rows.</returns>
    CsvTable LeftJoinByName(CsvTable left, CsvTable right, string leftKeyName, string rightKeyName);
}
