namespace tiff;

/// <summary>Contains the values and diagnostics produced by a series group operation.</summary>
public class TiffSeriesGroupResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the series.</summary>
    public TiffSeries[] Series;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Series Group Result class.</summary>
    public TiffSeriesGroupResult()
    {
        Success = true;
        Series = new TiffSeries[0];
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Series Group Result class.</summary>
    /// <param name="series">The series.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffSeriesGroupResult Create(TiffSeries[] series, TiffDiagnostics diagnostics)
    {
        TiffSeriesGroupResult value = new TiffSeriesGroupResult();
        value.Initialize(series, diagnostics);
        return value;
    }

    internal void Initialize(TiffSeries[] series, TiffDiagnostics diagnostics)
    {
        Series = CopySeries(series);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Returns the number of available series entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int SeriesCount()
    {
        return Series.Length;
    }

    internal static TiffSeries[] CopySeries(TiffSeries[] series)
    {
        if (series == null)
        {
            return new TiffSeries[0];
        }

        TiffSeries[] copy = new TiffSeries[series.Length];
        int index = 0;
        while (index < series.Length)
        {
            if (series[index] == null)
            {
                copy[index] = new TiffSeries();
            }
            else
            {
                copy[index] = series[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
