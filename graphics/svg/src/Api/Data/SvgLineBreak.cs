namespace svg;

/// <summary>Configures white-space processing and caller-sized inline layout.</summary>
public class SvgInlineTextOptions
{
    /// <summary>The CSS white-space processing mode.</summary>
    public SvgWhiteSpaceMode WhiteSpace;
    /// <summary>The paragraph base direction.</summary>
    public SvgTextDirection Direction;
    /// <summary>The line and block axis arrangement.</summary>
    public SvgWritingMode WritingMode;
    /// <summary>The requested character orientation in vertical writing.</summary>
    public SvgVerticalOrientation VerticalOrientation;
    /// <summary>The available inline size, or zero for unconstrained layout.</summary>
    public double MaxInlineSize;
    /// <summary>The fallback advance assigned to a grapheme cluster.</summary>
    public double DefaultAdvance;
    /// <summary>The block-axis distance between successive lines.</summary>
    public double LineAdvance;
    /// <summary>The number of fallback advances between tab stops.</summary>
    public int TabSize;
    /// <summary>Whether an otherwise unbreakable sequence may break to avoid overflow.</summary>
    public bool OverflowWrap;
    /// <summary>Optional advances indexed by source grapheme cluster.</summary>
    public System.Collections.Generic.List<double> ClusterAdvances;
    /// <summary>The maximum accepted source grapheme count.</summary>
    public int MaxClusters;
    /// <summary>The maximum generated line count.</summary>
    public int MaxLines;
    /// <summary>The cancellation token observed during bounded processing.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates wrapping horizontal-text defaults.</summary>
    public SvgInlineTextOptions()
    {
        WhiteSpace = SvgWhiteSpaceMode.Normal;
        Direction = SvgTextDirection.Auto;
        WritingMode = SvgWritingMode.HorizontalTopToBottom;
        VerticalOrientation = SvgVerticalOrientation.Mixed;
        MaxInlineSize = 0;
        DefaultAdvance = 1;
        LineAdvance = 1;
        TabSize = 8;
        OverflowWrap = false;
        ClusterAdvances = new System.Collections.Generic.List<double>();
        MaxClusters = 1000000;
        MaxLines = 1000000;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Contains processed layout data for one source grapheme cluster.</summary>
public class SvgInlineTextItem
{
    /// <summary>The source grapheme cluster index.</summary>
    public int SourceClusterIndex;
    /// <summary>The zero-based source UTF-16 index.</summary>
    public int SourceUtf16Start;
    /// <summary>The source UTF-16 code-unit length.</summary>
    public int SourceUtf16Length;
    /// <summary>The normalized rendered text, or empty string.</summary>
    public string Text;
    /// <summary>The caller or fallback inline advance.</summary>
    public double Advance;
    /// <summary>The opportunity following this item before overflow tailoring.</summary>
    public SvgLineBreakOpportunity BreakAfter;
    /// <summary>Whether the item represents collapsible white space.</summary>
    public bool CollapsibleSpace;
    /// <summary>Whether the item represents preserved white space.</summary>
    public bool PreservedSpace;
    /// <summary>Whether the item represents a forced segment break.</summary>
    public bool MandatoryBreak;
    /// <summary>Whether white-space processing removed the item.</summary>
    public bool Discarded;
    /// <summary>Whether line-edge trimming removed the item.</summary>
    public bool Trimmed;
    /// <summary>The containing line index, or minus one.</summary>
    public int LineIndex;
    /// <summary>The logical inline offset within the containing line.</summary>
    public double InlineOffset;
    /// <summary>The resolved horizontal, upright, or sideways orientation.</summary>
    public SvgGlyphOrientation Orientation;

    internal SvgInlineTextItem()
    {
        SourceClusterIndex = -1;
        SourceUtf16Start = 0;
        SourceUtf16Length = 0;
        Text = "";
        Advance = 0;
        BreakAfter = SvgLineBreakOpportunity.Prohibited;
        CollapsibleSpace = false;
        PreservedSpace = false;
        MandatoryBreak = false;
        Discarded = false;
        Trimmed = false;
        LineIndex = -1;
        InlineOffset = 0;
        Orientation = SvgGlyphOrientation.Horizontal;
    }
}

/// <summary>Contains one logical line and its bidirectionally reordered presentation.</summary>
public class SvgInlineTextLine
{
    /// <summary>The zero-based line index.</summary>
    public int Index;
    /// <summary>The source UTF-16 start index.</summary>
    public int SourceUtf16Start;
    /// <summary>The covered source UTF-16 length.</summary>
    public int SourceUtf16Length;
    /// <summary>The logical line text after trimming.</summary>
    public string Text;
    /// <summary>The bidirectionally reordered line text.</summary>
    public string VisualText;
    /// <summary>The total inline advance.</summary>
    public double InlineAdvance;
    /// <summary>The block-axis line offset.</summary>
    public double BlockOffset;
    /// <summary>Whether a preserved segment break ended the line.</summary>
    public bool HardBreak;
    /// <summary>The resolved bidi paragraph level.</summary>
    public int BaseLevel;
    /// <summary>The logical item indices included in the line.</summary>
    public System.Collections.Generic.List<int> ItemIndices;
    /// <summary>The included item indices in visual order.</summary>
    public System.Collections.Generic.List<int> VisualItemIndices;

    internal SvgInlineTextLine()
    {
        Index = -1;
        SourceUtf16Start = 0;
        SourceUtf16Length = 0;
        Text = "";
        VisualText = "";
        InlineAdvance = 0;
        BlockOffset = 0;
        HardBreak = false;
        BaseLevel = 0;
        ItemIndices = new System.Collections.Generic.List<int>();
        VisualItemIndices = new System.Collections.Generic.List<int>();
    }
}

/// <summary>Contains normalized white space, break metadata, line layout, and mappings.</summary>
public class SvgInlineTextResult
{
    /// <summary>Whether processing completed successfully.</summary>
    public bool Success;
    /// <summary>The stable processing outcome.</summary>
    public SvgInlineTextError Error;
    /// <summary>A deterministic failure explanation, or empty string.</summary>
    public string Message;
    /// <summary>The normalized text before line-edge trimming.</summary>
    public string ProcessedText;
    /// <summary>The logical rendered lines joined by U+000A.</summary>
    public string RenderedText;
    /// <summary>The source-cluster processing records.</summary>
    public System.Collections.Generic.List<SvgInlineTextItem> Items;
    /// <summary>The generated lines.</summary>
    public System.Collections.Generic.List<SvgInlineTextLine> Lines;
    /// <summary>Maps each source cluster to its line, using minus one when removed.</summary>
    public System.Collections.Generic.List<int> ClusterToLine;

    internal SvgInlineTextResult()
    {
        Success = false;
        Error = SvgInlineTextError.None;
        Message = "";
        ProcessedText = "";
        RenderedText = "";
        Items = new System.Collections.Generic.List<SvgInlineTextItem>();
        Lines = new System.Collections.Generic.List<SvgInlineTextLine>();
        ClusterToLine = new System.Collections.Generic.List<int>();
    }
}
