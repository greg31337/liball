using System;
using System.Collections.Generic;

namespace xlsx;

/// <summary>
/// Represents numeric, date, or discrete grouping for a pivot-cache field.
/// </summary>
public class XlsxPivotCacheGrouping
{
    /// <summary>Stores the base cache field index.</summary>
    public int BaseField;
    /// <summary>Indicates whether the base field index is present.</summary>
    public bool HasBaseField;
    /// <summary>Stores the parent cache field index.</summary>
    public int ParentField;
    /// <summary>Indicates whether the parent field index is present.</summary>
    public bool HasParentField;
    /// <summary>Indicates date grouping rather than numeric grouping.</summary>
    public bool DateGrouping;
    /// <summary>Stores the date grouping unit.</summary>
    public XlsxPivotDateGroupBy DateGroupBy;
    /// <summary>Stores the numeric group start.</summary>
    public double StartNumber;
    /// <summary>Stores the numeric group end.</summary>
    public double EndNumber;
    /// <summary>Stores the numeric group interval.</summary>
    public double Interval;
    /// <summary>Stores the date group start.</summary>
    public System.DateTime StartDate;
    /// <summary>Stores the date group end.</summary>
    public System.DateTime EndDate;
    /// <summary>Contains discrete shared-item indexes.</summary>
    public List<int> DiscreteIndexes;

    /// <summary>Initializes empty pivot-cache grouping.</summary>
    public XlsxPivotCacheGrouping()
    {
        BaseField = 0;
        HasBaseField = false;
        ParentField = 0;
        HasParentField = false;
        DateGrouping = false;
        DateGroupBy = XlsxPivotDateGroupBy.Days;
        StartNumber = 0;
        EndNumber = 0;
        Interval = 1;
        StartDate = default(System.DateTime);
        EndDate = default(System.DateTime);
        DiscreteIndexes = new List<int>();
    }
}
