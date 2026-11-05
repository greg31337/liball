namespace tiff;

/// <summary>Contains the values and diagnostics produced by a RGBA 16 read operation.</summary>
public class TiffRgba16ReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the image or region width in pixels.</summary>
    public ulong Width;
    /// <summary>Contains the image or region height in rows.</summary>
    public ulong Length;
    /// <summary>Contains the row byte count.</summary>
    public ulong RowByteCount;
    /// <summary>Contains the total byte count.</summary>
    public ulong TotalByteCount;
    /// <summary>Contains the samples.</summary>
    public ushort[] Samples;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF RGBA 16 Read Result class.</summary>
    public TiffRgba16ReadResult()
    {
        Success = false;
        Width = 0;
        Length = 0;
        RowByteCount = 0;
        TotalByteCount = 0;
        Samples = new ushort[0];
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF RGBA 16 Read Result class.</summary>
    /// <param name="width">The width.</param>
    /// <param name="length">The length.</param>
    /// <param name="rowByteCount">The row byte count.</param>
    /// <param name="totalByteCount">The total byte count.</param>
    /// <param name="samples">The samples.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffRgba16ReadResult Create(ulong width, ulong length, ulong rowByteCount, ulong totalByteCount, ushort[] samples, TiffDiagnostics diagnostics)
    {
        TiffRgba16ReadResult value = new TiffRgba16ReadResult();
        value.Initialize(width, length, rowByteCount, totalByteCount, samples, diagnostics);
        return value;
    }

    internal void Initialize(ulong width, ulong length, ulong rowByteCount, ulong totalByteCount, ushort[] samples, TiffDiagnostics diagnostics)
    {
        Width = width;
        Length = length;
        RowByteCount = rowByteCount;
        TotalByteCount = totalByteCount;
        Samples = TiffAlphaHandler.CopyUShorts(samples);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffRgba16ReadResult Copy()
    {
        TiffRgba16ReadResult copy = TiffRgba16ReadResult.Create(Width, Length, RowByteCount, TotalByteCount, Samples, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
