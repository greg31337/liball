namespace odf;

/// <summary>Describes one physical table-row run without expanding repetitions.</summary>
public class OdfRow
{
    /// <summary>The zero-based first logical row represented by this run.</summary>
    public long StartIndex;
    /// <summary>The number of logical rows represented by this run.</summary>
    public long RepeatCount = 1;
    /// <summary>Whether the row belongs to a header-row container.</summary>
    public bool IsHeader;
    /// <summary>The number of containing row-group elements.</summary>
    public int GroupDepth;
    /// <summary>The referenced table-row style name.</summary>
    public string StyleName = "";
    /// <summary>The resolved table-row style.</summary>
    public OdfStyle? Style;
    /// <summary>The default table-row style.</summary>
    public OdfStyle? DefaultStyle;
    /// <summary>The default style name for cells in the row.</summary>
    public string DefaultCellStyleName = "";
    /// <summary>The resolved default style for cells in the row.</summary>
    public OdfStyle? DefaultCellStyle;
    /// <summary>The preserved row visibility token.</summary>
    public string Visibility = "visible";
    /// <summary>Whether the row is visible.</summary>
    public bool IsVisible = true;
    /// <summary>The compact physical cell runs in logical column order.</summary>
    public OdfCell[] Cells = [];
    /// <summary>The containing table.</summary>
    public OdfTable? Table;
    /// <summary>The underlying table-row element.</summary>
    public OdfNode? Node;
    internal string OriginalStyleName = "";
    internal string OriginalDefaultCellStyleName = "";
    internal string OriginalVisibility = "visible";
    internal bool OriginalIsVisible = true;
}
