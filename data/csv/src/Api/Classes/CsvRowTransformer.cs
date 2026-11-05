using System;

namespace csv;

/// <summary>
/// Wraps an optional callback that transforms a row during processing.
/// </summary>
public class CsvRowTransformer : ICsvRowTransformer
{
    /// <summary>
    /// Callback invoked when transformation is active.
    /// </summary>
    internal Func<CsvRow, CsvRow?> TransformCallback;
    /// <summary>
    /// Whether the transformer invokes its callback.
    /// </summary>
    internal bool HasTransformCallback;

    /// <summary>
    /// Initializes a row transformer with a callback.
    /// </summary>
    /// <param name="transformCallback">The callback that transforms rows.</param>
    public CsvRowTransformer(Func<CsvRow, CsvRow?> transformCallback)
    {
        TransformCallback = transformCallback;
        HasTransformCallback = true;
    }

    /// <summary>
    /// Creates an instance populated with library defaults.
    /// </summary>
    /// <returns>The initialized result.</returns>
    internal static CsvRowTransformer CreateDefault()
    {
        CsvRowTransformer transformer = new CsvRowTransformer(TransformDefault);
        transformer.HasTransformCallback = false;
        return transformer;
    }

    /// <summary>
    /// Returns the callback result, or the input row when transformation is disabled.
    /// </summary>
    /// <param name="row">The row to process.</param>
    /// <returns>The callback result, or <c>row</c> when transformation is disabled.</returns>
    public CsvRow? Transform(CsvRow row)
    {
        if (HasTransformCallback)
        {
            CsvRow? transformed = TransformCallback(row);
            return transformed;
        }

        return row;
    }

    internal static CsvRow TransformDefault(CsvRow row)
    {
        return row;
    }
}
