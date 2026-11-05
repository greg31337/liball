namespace chart;
/// <summary>Specifies calendar intervals used for time tick generation.</summary>
public enum ChartTimeInterval
{
    /// <summary>Uses calendar milliseconds.</summary>
    Millisecond = 0,
    /// <summary>Uses calendar seconds.</summary>
    Second = 1,
    /// <summary>Uses calendar minutes.</summary>
    Minute = 2,
    /// <summary>Uses calendar hours.</summary>
    Hour = 3,
    /// <summary>Uses calendar days.</summary>
    Day = 4,
    /// <summary>Uses Monday-aligned calendar weeks.</summary>
    Week = 5,
    /// <summary>Uses calendar months.</summary>
    Month = 6,
    /// <summary>Uses calendar quarters.</summary>
    Quarter = 7,
    /// <summary>Uses calendar years.</summary>
    Year = 8
}
