using System;

namespace htmlgui;

/// <summary>Describes a tree or interactive tree grid.</summary>
public class UiTree
{
    /// <summary>The stable tree key.</summary>
    public string Key = string.Empty;
    /// <summary>The materialized root items.</summary>
    public UiTreeItem[] Items = Array.Empty<UiTreeItem>();
    /// <summary>The selection state.</summary>
    public UiSelection Selection = new UiSelection();
    /// <summary>The flattened materialized range.</summary>
    public UiTreeRange Range = new UiTreeRange();
    /// <summary>The current type-ahead matching state.</summary>
    public UiTypeAheadState TypeAhead = new UiTypeAheadState();
    /// <summary>Whether the tree uses grid semantics.</summary>
    public bool IsTreeGrid;
    /// <summary>Whether drag reordering is enabled.</summary>
    public bool CanReorder;
}

/// <summary>Describes one keyed tree item and its materialized children.</summary>
public class UiTreeItem
{
    /// <summary>The stable item key.</summary>
    public string Key = string.Empty;
    /// <summary>The visible item label.</summary>
    public string Label = string.Empty;
    /// <summary>The portable item value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The optional rendered item content.</summary>
    public UiNode? Content;
    /// <summary>The materialized child items.</summary>
    public UiTreeItem[] Children = Array.Empty<UiTreeItem>();
    /// <summary>The accessibility relationship metadata.</summary>
    public UiTreeRelationship Relationship = new UiTreeRelationship();
    /// <summary>Whether the item is expanded.</summary>
    public bool IsExpanded;
    /// <summary>Whether children exist but are not loaded.</summary>
    public bool HasLazyChildren;
    /// <summary>Whether the item is selected.</summary>
    public bool IsSelected;
    /// <summary>Whether the item is disabled.</summary>
    public bool IsDisabled;
    /// <summary>Whether a checked state is supplied.</summary>
    public bool HasCheckState;
    /// <summary>The checked state.</summary>
    public UiCommandCheckState CheckState;
}

/// <summary>Describes parent and positional metadata for an accessible tree item.</summary>
public class UiTreeRelationship
{
    /// <summary>The optional stable parent key.</summary>
    public string? ParentKey;
    /// <summary>The one-based tree depth.</summary>
    public int Depth;
    /// <summary>The one-based position among siblings.</summary>
    public int PositionInSet;
    /// <summary>Whether the sibling set size is known.</summary>
    public bool HasSetSize;
    /// <summary>The total sibling set size.</summary>
    public int SetSize;
}

/// <summary>Describes a materialized range in the flattened tree.</summary>
public class UiTreeRange
{
    /// <summary>The zero-based flattened start index.</summary>
    public int Start;
    /// <summary>The materialized flattened item count.</summary>
    public int Count;
    /// <summary>Whether the total flattened count is known.</summary>
    public bool HasTotalCount;
    /// <summary>The total flattened item count.</summary>
    public int TotalCount;
    /// <summary>The stable anchor item key.</summary>
    public string? AnchorKey;
}
