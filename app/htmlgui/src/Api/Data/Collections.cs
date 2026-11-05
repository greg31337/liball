using System;

namespace htmlgui;

/// <summary>Describes a static or interactive keyed collection.</summary>
public class UiCollection
{
    /// <summary>The stable collection key.</summary>
    public string Key = string.Empty;
    /// <summary>The materialized items.</summary>
    public UiCollectionItem[] Items = Array.Empty<UiCollectionItem>();
    /// <summary>The selection state.</summary>
    public UiSelection Selection = new UiSelection();
    /// <summary>The active sort descriptions.</summary>
    public UiSort[] Sorts = Array.Empty<UiSort>();
    /// <summary>The active filter descriptions.</summary>
    public UiFilter[] Filters = Array.Empty<UiFilter>();
    /// <summary>The active groups.</summary>
    public UiGroup[] Groups = Array.Empty<UiGroup>();
    /// <summary>The materialized paging range.</summary>
    public UiPageRange PageRange = new UiPageRange();
    /// <summary>Content shown when the collection is empty.</summary>
    public UiNode? EmptyContent;
    /// <summary>Content shown while items are loading.</summary>
    public UiNode? LoadingContent;
    /// <summary>Content shown after an item-provider error.</summary>
    public UiNode? ErrorContent;
    /// <summary>Whether items can be reordered.</summary>
    public bool CanReorder;
    /// <summary>Whether more items are pending.</summary>
    public bool IsLoading;
    /// <summary>Whether the collection is currently in an error state.</summary>
    public bool HasError;
}

/// <summary>Describes one stable item in a collection.</summary>
public class UiCollectionItem
{
    /// <summary>The stable item key.</summary>
    public string Key = string.Empty;
    /// <summary>The portable item value.</summary>
    public UiValue Value = new UiValue();
    /// <summary>The rendered item content.</summary>
    public UiNode? Content;
    /// <summary>The optional group key.</summary>
    public string? GroupKey;
    /// <summary>Whether the item is disabled.</summary>
    public bool IsDisabled;
    /// <summary>Whether the item is a loading placeholder.</summary>
    public bool IsPlaceholder;
}

/// <summary>Describes collection selection and active-item state.</summary>
public class UiSelection
{
    /// <summary>The permitted selection behavior.</summary>
    public UiSelectionMode Mode;
    /// <summary>The selected stable item keys.</summary>
    public string[] SelectedKeys = Array.Empty<string>();
    /// <summary>The optional active item key.</summary>
    public string? ActiveKey;
    /// <summary>The optional range-selection anchor key.</summary>
    public string? AnchorKey;
}

/// <summary>Describes one ordered sort criterion.</summary>
public class UiSort
{
    /// <summary>The stable field or column key.</summary>
    public string Key = string.Empty;
    /// <summary>The sort direction.</summary>
    public UiSortDirection Direction;
    /// <summary>The zero-based criterion priority.</summary>
    public int Priority;
}

/// <summary>Describes one field filter and its portable operand.</summary>
public class UiFilter
{
    /// <summary>The stable field or column key.</summary>
    public string Key = string.Empty;
    /// <summary>The comparison operator token.</summary>
    public string Operator = string.Empty;
    /// <summary>The comparison operand.</summary>
    public UiValue Value = new UiValue();
    /// <summary>Whether string comparison ignores case.</summary>
    public bool IgnoreCase;
}

/// <summary>Describes one collection group.</summary>
public class UiGroup
{
    /// <summary>The stable group key.</summary>
    public string Key = string.Empty;
    /// <summary>The visible group label.</summary>
    public string Label = string.Empty;
    /// <summary>The stable keys of items in the group.</summary>
    public string[] ItemKeys = Array.Empty<string>();
    /// <summary>Whether the group is expanded.</summary>
    public bool IsExpanded = true;
}

/// <summary>Describes a materialized page or item range.</summary>
public class UiPageRange
{
    /// <summary>The zero-based start index.</summary>
    public int Start;
    /// <summary>The materialized item count.</summary>
    public int Count;
    /// <summary>Whether the total logical count is known.</summary>
    public bool HasTotalCount;
    /// <summary>The total logical count.</summary>
    public int TotalCount;
    /// <summary>Whether the end of available data was reached.</summary>
    public bool IsEnd;
}
