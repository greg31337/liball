namespace fake;

/// <summary>
/// Describes the conventional ordering of numeric calendar date components.
/// </summary>
public enum DateOrder
{
    /// <summary>
    /// Places month before day and year.
    /// </summary>
    MonthDayYear = 0,

    /// <summary>
    /// Places day before month and year.
    /// </summary>
    DayMonthYear = 1,

    /// <summary>
    /// Places year before month and day.
    /// </summary>
    YearMonthDay = 2
}
