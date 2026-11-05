using System;

namespace xlsx;

/// <summary>
/// Represents timeline filtering and selection state.
/// </summary>
public class XlsxTimelineState
{
    /// <summary>Stores the pivot cache identifier.</summary>
    public uint PivotCacheId;
    /// <summary>Indicates whether the pivot cache identifier is present.</summary>
    public bool HasPivotCacheId;
    /// <summary>Stores the filter identifier.</summary>
    public uint FilterId;
    /// <summary>Indicates whether the filter identifier is present.</summary>
    public bool HasFilterId;
    /// <summary>Stores the timeline filter type token.</summary>
    public string FilterType;
    /// <summary>Indicates whether a single date range is active.</summary>
    public bool SingleRange;
    /// <summary>Indicates whether the single-range flag is present.</summary>
    public bool HasSingleRange;
    /// <summary>Stores the selected range start.</summary>
    public System.DateTime StartDate;
    /// <summary>Stores the selected range end.</summary>
    public System.DateTime EndDate;
    /// <summary>Indicates whether a selected date range is present.</summary>
    public bool HasSelection;
    /// <summary>Stores the selected timeline level.</summary>
    public uint Level;
    /// <summary>Indicates whether the selected level is present.</summary>
    public bool HasLevel;

    /// <summary>Initializes empty timeline state.</summary>
    public XlsxTimelineState()
    {
        PivotCacheId = 0;
        HasPivotCacheId = false;
        FilterId = 0;
        HasFilterId = false;
        FilterType = "";
        SingleRange = true;
        HasSingleRange = false;
        StartDate = default(System.DateTime);
        EndDate = default(System.DateTime);
        HasSelection = false;
        Level = 0;
        HasLevel = false;
    }
}
