using System;

namespace htmlgui;

/// <summary>Describes a layout container and its sizing, placement, and scrolling options.</summary>
public class UiLayout
{
    /// <summary>The layout algorithm.</summary>
    public UiLayoutKind Kind;
    /// <summary>The primary orientation.</summary>
    public UiOrientation Orientation;
    /// <summary>The cross-axis alignment.</summary>
    public UiAlignment Alignment;
    /// <summary>The main-axis distribution.</summary>
    public UiJustification Justification;
    /// <summary>The gap between children.</summary>
    public UiLength Gap = new UiLength();
    /// <summary>The container padding.</summary>
    public UiSpacing Padding = new UiSpacing();
    /// <summary>The container margin.</summary>
    public UiSpacing Margin = new UiSpacing();
    /// <summary>The optional width.</summary>
    public UiLength? Width;
    /// <summary>The optional height.</summary>
    public UiLength? Height;
    /// <summary>The optional minimum width.</summary>
    public UiLength? MinimumWidth;
    /// <summary>The optional maximum width.</summary>
    public UiLength? MaximumWidth;
    /// <summary>The optional minimum height.</summary>
    public UiLength? MinimumHeight;
    /// <summary>The optional maximum height.</summary>
    public UiLength? MaximumHeight;
    /// <summary>Whether an aspect ratio is supplied.</summary>
    public bool HasAspectRatio;
    /// <summary>The width-to-height ratio when <see cref="UiLayout.HasAspectRatio"/> is true.</summary>
    public double AspectRatio;
    /// <summary>The grid column tracks.</summary>
    public UiGridTrack[] Columns = Array.Empty<UiGridTrack>();
    /// <summary>The grid row tracks.</summary>
    public UiGridTrack[] Rows = Array.Empty<UiGridTrack>();
    /// <summary>The optional placement within a parent grid.</summary>
    public UiGridPlacement? GridPlacement;
    /// <summary>The optional positioning description.</summary>
    public UiPosition? Position;
    /// <summary>The optional scrolling description.</summary>
    public UiScrollOptions? Scroll;
    /// <summary>The laid-out child nodes.</summary>
    public UiNode[] Children = Array.Empty<UiNode>();
    /// <summary>Whether children wrap when space is constrained.</summary>
    public bool Wrap;
    /// <summary>Whether size containment is requested.</summary>
    public bool ContainSize;
    /// <summary>Whether layout containment is requested.</summary>
    public bool ContainLayout;
    /// <summary>Whether paint containment is requested.</summary>
    public bool ContainPaint;
    /// <summary>Whether logical padding incorporates safe-area environment insets.</summary>
    public bool UseSafeAreaInsets;
    /// <summary>Whether the user can resize the container.</summary>
    public bool IsResizable;
    /// <summary>The axis along which resizing is permitted.</summary>
    public UiOrientation ResizeOrientation;
}

/// <summary>Describes an optional unit-aware CSS length or safe expression.</summary>
public class UiLength
{
    /// <summary>Whether a numeric value is explicitly supplied.</summary>
    public bool HasValue;
    /// <summary>The numeric magnitude when <see cref="UiLength.HasValue"/> is true.</summary>
    public double Value;
    /// <summary>The numeric unit.</summary>
    public UiLengthUnit Unit;
    /// <summary>An optional safe expression used instead of a numeric value.</summary>
    public string? Expression;
}

/// <summary>Describes four logical box-edge lengths.</summary>
public class UiSpacing
{
    /// <summary>The block-start edge.</summary>
    public UiLength BlockStart = new UiLength();
    /// <summary>The inline-end edge.</summary>
    public UiLength InlineEnd = new UiLength();
    /// <summary>The block-end edge.</summary>
    public UiLength BlockEnd = new UiLength();
    /// <summary>The inline-start edge.</summary>
    public UiLength InlineStart = new UiLength();
}

/// <summary>Describes one named or repeated CSS grid track.</summary>
public class UiGridTrack
{
    /// <summary>The optional track-line name.</summary>
    public string? Name;
    /// <summary>The minimum track size.</summary>
    public UiLength Minimum = new UiLength();
    /// <summary>The maximum track size.</summary>
    public UiLength Maximum = new UiLength();
    /// <summary>Whether a repeat count is supplied.</summary>
    public bool HasRepeatCount;
    /// <summary>The repeat count when <see cref="UiGridTrack.HasRepeatCount"/> is true.</summary>
    public int RepeatCount;
    /// <summary>Whether automatic placement fills available tracks.</summary>
    public bool AutoFit;
}

/// <summary>Describes a child's row, column, span, or named grid area.</summary>
public class UiGridPlacement
{
    /// <summary>The optional named grid area.</summary>
    public string? Area;
    /// <summary>Whether a row index is supplied.</summary>
    public bool HasRow;
    /// <summary>The one-based row index.</summary>
    public int Row;
    /// <summary>Whether a row span is supplied.</summary>
    public bool HasRowSpan;
    /// <summary>The row span.</summary>
    public int RowSpan;
    /// <summary>Whether a column index is supplied.</summary>
    public bool HasColumn;
    /// <summary>The one-based column index.</summary>
    public int Column;
    /// <summary>Whether a column span is supplied.</summary>
    public bool HasColumnSpan;
    /// <summary>The column span.</summary>
    public int ColumnSpan;
}

/// <summary>Describes logical inset positioning and stacking.</summary>
public class UiPosition
{
    /// <summary>Whether block-start is supplied.</summary>
    public bool HasBlockStart;
    /// <summary>The block-start inset.</summary>
    public UiLength BlockStart = new UiLength();
    /// <summary>Whether inline-end is supplied.</summary>
    public bool HasInlineEnd;
    /// <summary>The inline-end inset.</summary>
    public UiLength InlineEnd = new UiLength();
    /// <summary>Whether block-end is supplied.</summary>
    public bool HasBlockEnd;
    /// <summary>The block-end inset.</summary>
    public UiLength BlockEnd = new UiLength();
    /// <summary>Whether inline-start is supplied.</summary>
    public bool HasInlineStart;
    /// <summary>The inline-start inset.</summary>
    public UiLength InlineStart = new UiLength();
    /// <summary>Whether a z-index is supplied.</summary>
    public bool HasZIndex;
    /// <summary>The z-index.</summary>
    public int ZIndex;
    /// <summary>Whether sticky positioning is requested.</summary>
    public bool IsSticky;
}

/// <summary>Describes overflow, anchoring, and reveal behavior for a scroll container.</summary>
public class UiScrollOptions
{
    /// <summary>The horizontal overflow behavior.</summary>
    public UiOverflowMode Horizontal;
    /// <summary>The vertical overflow behavior.</summary>
    public UiOverflowMode Vertical;
    /// <summary>Whether scroll position is preserved across updates.</summary>
    public bool PreservePosition = true;
    /// <summary>The optional stable anchor key.</summary>
    public string? AnchorKey;
    /// <summary>The optional node identifier to reveal.</summary>
    public string? RevealNodeId;
    /// <summary>Whether smooth scrolling is requested.</summary>
    public bool Smooth;
}
