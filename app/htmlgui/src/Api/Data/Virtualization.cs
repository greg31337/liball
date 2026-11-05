namespace htmlgui;

/// <summary>Describes fixed or variable extent collection virtualization.</summary>
public class UiVirtualization
{
    /// <summary>Whether virtualization is enabled.</summary>
    public bool IsEnabled;
    /// <summary>The orientation.</summary>
    public UiOrientation Orientation = UiOrientation.Vertical;
    /// <summary>Whether a fixed item extent is supplied.</summary>
    public bool HasItemExtent;
    /// <summary>The fixed or estimated item extent.</summary>
    public double ItemExtent;
    /// <summary>The overscan item count.</summary>
    public int Overscan;
    /// <summary>Whether the total logical count is known.</summary>
    public bool HasTotalCount;
    /// <summary>The total logical count.</summary>
    public int TotalCount;
    /// <summary>The optional active range request.</summary>
    public UiRangeRequest? Request;
    /// <summary>The optional accepted range result.</summary>
    public UiRangeResult? Result;
    /// <summary>The measured variable-extent items.</summary>
    public UiMeasuredItem[] Measurements = System.Array.Empty<UiMeasuredItem>();
    /// <summary>The optional calculated scroll target.</summary>
    public UiScrollTarget? ScrollTarget;
}

/// <summary>Describes one generated range-provider request.</summary>
public class UiRangeRequest
{
    /// <summary>The request generation.</summary>
    public int Generation;
    /// <summary>The zero-based start index.</summary>
    public int Start;
    /// <summary>The requested count.</summary>
    public int Count;
    /// <summary>The active sorts.</summary>
    public UiSort[] Sorts = System.Array.Empty<UiSort>();
    /// <summary>The active filters.</summary>
    public UiFilter[] Filters = System.Array.Empty<UiFilter>();
    /// <summary>The active group descriptions.</summary>
    public UiGroup[] Groups = System.Array.Empty<UiGroup>();
    /// <summary>The optional parent tree key.</summary>
    public string? ParentKey;
}

/// <summary>Describes a range-provider result.</summary>
public class UiRangeResult
{
    /// <summary>The request generation.</summary>
    public int Generation;
    /// <summary>The zero-based result start.</summary>
    public int Start;
    /// <summary>The returned items.</summary>
    public UiCollectionItem[] Items = System.Array.Empty<UiCollectionItem>();
    /// <summary>Whether the total count is known.</summary>
    public bool HasTotalCount;
    /// <summary>The total count.</summary>
    public int TotalCount;
    /// <summary>Whether the end of data was reached.</summary>
    public bool IsEnd;
    /// <summary>The optional diagnostic.</summary>
    public UiDiagnostic? Diagnostic;
}

/// <summary>Describes one measured variable-extent item.</summary>
public class UiMeasuredItem
{
    /// <summary>The stable item key.</summary>
    public string Key = string.Empty;
    /// <summary>The logical item index.</summary>
    public int Index;
    /// <summary>The measured extent.</summary>
    public double Extent;
    /// <summary>The measurement generation.</summary>
    public int Generation;
}

/// <summary>Describes a calculated scroll destination.</summary>
public class UiScrollTarget
{
    /// <summary>Whether an item index is supplied.</summary>
    public bool HasIndex;
    /// <summary>The target item index.</summary>
    public int Index;
    /// <summary>The optional target item key.</summary>
    public string? Key;
    /// <summary>The start, center, end, or nearest alignment token.</summary>
    public string Alignment = "nearest";
    /// <summary>The calculated scroll offset.</summary>
    public double Offset;
}
