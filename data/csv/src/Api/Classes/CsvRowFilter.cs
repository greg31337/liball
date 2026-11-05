using System;

namespace csv;

/// <summary>
/// Wraps an optional predicate that decides whether a row continues through a pipeline.
/// </summary>
public class CsvRowFilter : ICsvRowFilter
{
    /// <summary>
    /// Predicate invoked when filtering is active.
    /// </summary>
    internal Func<CsvRow, bool> IncludeCallback;
    /// <summary>
    /// Whether the filter invokes its predicate.
    /// </summary>
    internal bool HasIncludeCallback;

    /// <summary>
    /// Initializes a row filter with a predicate.
    /// </summary>
    /// <param name="includeCallback">The predicate that selects rows.</param>
    public CsvRowFilter(Func<CsvRow, bool> includeCallback)
    {
        IncludeCallback = includeCallback;
        HasIncludeCallback = true;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvRowFilter CreateDefault()
    {
        CsvRowFilter filter = new CsvRowFilter(IncludeDefault);
        filter.HasIncludeCallback = false;
        return filter;
    }

    /// <summary>
    /// Returns the predicate result, or true when filtering is disabled.
    /// </summary>
    /// <param name="row">The row to process.</param>
    /// <returns>The configured predicate result, or <c>true</c> when filtering is disabled.</returns>
    public bool Include(CsvRow row)
    {
        if (HasIncludeCallback)
        {
            bool include = IncludeCallback(row);
            return include;
        }

        return true;
    }

    internal static bool IncludeDefault(CsvRow row)
    {
        return true;
    }
}
