using System;

namespace fake;

/// <summary>
/// Describes one quarter of a fiscal year using inclusive calendar dates.
/// </summary>
public class FiscalPeriod
{
    /// <summary>
    /// The calendar year in which the fiscal year starts.
    /// </summary>
    public int FiscalYear;

    /// <summary>
    /// The one-based fiscal quarter number.
    /// </summary>
    public int Quarter;

    /// <summary>
    /// The inclusive first date of the quarter.
    /// </summary>
    public DateOnly Start;

    /// <summary>
    /// The inclusive last representable date of the quarter.
    /// </summary>
    public DateOnly End;

    internal FiscalPeriod(int fiscalYear, int quarter, DateOnly start, DateOnly end)
    {
        FiscalYear = fiscalYear;
        Quarter = quarter;
        Start = start;
        End = end;
    }
}
