namespace odf;

/// <summary>Describes one physical table-column run without expanding repetitions.</summary>
public class OdfColumn
{
    /// <summary>The zero-based first logical column represented by this run.</summary>
    public long StartIndex;
    /// <summary>The number of logical columns represented by this run.</summary>
    public long RepeatCount = 1;
    /// <summary>Whether the column belongs to a header-column container.</summary>
    public bool IsHeader;
    /// <summary>The number of containing column-group elements.</summary>
    public int GroupDepth;
    /// <summary>The referenced table-column style name.</summary>
    public string StyleName = "";
    /// <summary>The resolved table-column style.</summary>
    public OdfStyle? Style;
    /// <summary>The default table-column style.</summary>
    public OdfStyle? DefaultStyle;
    /// <summary>The default style name for cells in the column.</summary>
    public string DefaultCellStyleName = "";
    /// <summary>The resolved default style for cells in the column.</summary>
    public OdfStyle? DefaultCellStyle;
    /// <summary>The preserved column visibility token.</summary>
    public string Visibility = "visible";
    /// <summary>Whether the column is visible.</summary>
    public bool IsVisible = true;
    /// <summary>The containing table.</summary>
    public OdfTable? Table;
    /// <summary>The underlying table-column element.</summary>
    public OdfNode? Node;
    internal string OriginalStyleName = "";
    internal string OriginalDefaultCellStyleName = "";
    internal string OriginalVisibility = "visible";
    internal bool OriginalIsVisible = true;
}
