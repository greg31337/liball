using System;

namespace htmlgui;

/// <summary>Describes a semantic table or interactive data grid.</summary>
public class UiTable
{
    /// <summary>The stable table key.</summary>
    public string Key = string.Empty;
    /// <summary>The optional caption.</summary>
    public string? Caption;
    /// <summary>The column descriptions.</summary>
    public UiTableColumn[] Columns = Array.Empty<UiTableColumn>();
    /// <summary>The materialized rows.</summary>
    public UiTableRow[] Rows = Array.Empty<UiTableRow>();
    /// <summary>The summary cells.</summary>
    public UiTableSummary[] Summaries = Array.Empty<UiTableSummary>();
    /// <summary>The active filter descriptions.</summary>
    public UiFilter[] Filters = Array.Empty<UiFilter>();
    /// <summary>The row groups in presentation order.</summary>
    public UiGroup[] Groups = Array.Empty<UiGroup>();
    /// <summary>The row selection state.</summary>
    public UiSelection Selection = new UiSelection();
    /// <summary>The current edit state.</summary>
    public UiEditState? EditState;
    /// <summary>Whether keyboard grid interaction is enabled.</summary>
    public bool IsInteractive;
    /// <summary>Whether row details can be expanded.</summary>
    public bool HasRowDetails;
}

/// <summary>Describes one semantic or interactive table column.</summary>
public class UiTableColumn
{
    /// <summary>The stable column key.</summary>
    public string Key = string.Empty;
    /// <summary>The visible header.</summary>
    public string Header = string.Empty;
    /// <summary>The optional bound field key.</summary>
    public string? FieldKey;
    /// <summary>The optional width.</summary>
    public UiLength? Width;
    /// <summary>The current sort direction.</summary>
    public UiSortDirection SortDirection;
    /// <summary>Whether sorting is permitted.</summary>
    public bool IsSortable;
    /// <summary>Whether resizing is permitted.</summary>
    public bool IsResizable;
    /// <summary>Whether reordering is permitted.</summary>
    public bool IsReorderable;
    /// <summary>Whether the column is hidden.</summary>
    public bool IsHidden;
    /// <summary>Whether the column is frozen.</summary>
    public bool IsFrozen;
}

/// <summary>Describes one keyed table row.</summary>
public class UiTableRow
{
    /// <summary>The stable row key.</summary>
    public string Key = string.Empty;
    /// <summary>The row cells.</summary>
    public UiTableCell[] Cells = Array.Empty<UiTableCell>();
    /// <summary>The optional expanded detail content.</summary>
    public UiNode? DetailContent;
    /// <summary>Whether the row is selected.</summary>
    public bool IsSelected;
    /// <summary>Whether row details are expanded.</summary>
    public bool IsExpanded;
    /// <summary>Whether the row is disabled.</summary>
    public bool IsDisabled;
}

/// <summary>Describes one table cell.</summary>
public class UiTableCell
{
    /// <summary>The stable column key.</summary>
    public string ColumnKey = string.Empty;
    /// <summary>The portable cell value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The optional rendered cell content.</summary>
    public UiNode? Content;
    /// <summary>Whether the cell is a row or column header.</summary>
    public bool IsHeader;
    /// <summary>Whether the cell permits inline editing.</summary>
    public bool IsEditable;
    /// <summary>Whether a column span is supplied.</summary>
    public bool HasColumnSpan;
    /// <summary>The column span.</summary>
    public int ColumnSpan;
    /// <summary>Whether a row span is supplied.</summary>
    public bool HasRowSpan;
    /// <summary>The row span.</summary>
    public int RowSpan;
}

/// <summary>Describes one table summary value.</summary>
public class UiTableSummary
{
    /// <summary>The stable target column key.</summary>
    public string ColumnKey = string.Empty;
    /// <summary>The visible summary label.</summary>
    public string Label = string.Empty;
    /// <summary>The portable summary value.</summary>
    public UiValue Value = new UiValue();
}

/// <summary>Describes an active inline table edit.</summary>
public class UiEditState
{
    /// <summary>The stable edited row key.</summary>
    public string RowKey = string.Empty;
    /// <summary>The stable edited column key.</summary>
    public string ColumnKey = string.Empty;
    /// <summary>The value before editing began.</summary>
    public UiValue OriginalValue = new UiValue();
    /// <summary>The current editor value.</summary>
    public UiValue CurrentValue = new UiValue();
    /// <summary>Whether the value changed.</summary>
    public bool IsDirty;
    /// <summary>Whether validation is pending.</summary>
    public bool IsPending;
    /// <summary>Whether the current value is valid.</summary>
    public bool IsValid = true;
}
