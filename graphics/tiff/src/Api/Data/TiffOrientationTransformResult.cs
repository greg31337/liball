namespace tiff;

/// <summary>Contains the values and diagnostics produced by a orientation transform operation.</summary>
public class TiffOrientationTransformResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the image or region width in pixels.</summary>
    public ulong Width;
    /// <summary>Contains the image or region height in rows.</summary>
    public ulong Length;
    /// <summary>Contains the bytes per pixel.</summary>
    public int BytesPerPixel;
    /// <summary>Contains the row byte count.</summary>
    public ulong RowByteCount;
    /// <summary>Contains the total byte count.</summary>
    public ulong TotalByteCount;
    /// <summary>Contains the resulting bytes in their documented storage or pixel order.</summary>
    public byte[] Bytes;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF Orientation Transform Result class.</summary>
    public TiffOrientationTransformResult()
    {
        Success = false;
        Width = 0;
        Length = 0;
        BytesPerPixel = 0;
        RowByteCount = 0;
        TotalByteCount = 0;
        Bytes = new byte[0];
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF Orientation Transform Result class.</summary>
    /// <param name="width">The width.</param>
    /// <param name="length">The length.</param>
    /// <param name="bytesPerPixel">The bytes per pixel.</param>
    /// <param name="rowByteCount">The row byte count.</param>
    /// <param name="totalByteCount">The total byte count.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffOrientationTransformResult Create(ulong width, ulong length, int bytesPerPixel, ulong rowByteCount, ulong totalByteCount, byte[] bytes, TiffDiagnostics diagnostics)
    {
        TiffOrientationTransformResult value = new TiffOrientationTransformResult();
        value.Initialize(width, length, bytesPerPixel, rowByteCount, totalByteCount, bytes, diagnostics);
        return value;
    }

    internal void Initialize(ulong width, ulong length, int bytesPerPixel, ulong rowByteCount, ulong totalByteCount, byte[] bytes, TiffDiagnostics diagnostics)
    {
        Width = width;
        Length = length;
        BytesPerPixel = bytesPerPixel;
        RowByteCount = rowByteCount;
        TotalByteCount = totalByteCount;
        Bytes = TiffIfdEntry.CopyBytes(bytes);
        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    internal TiffOrientationTransformResult Copy()
    {
        TiffOrientationTransformResult copy = TiffOrientationTransformResult.Create(Width, Length, BytesPerPixel, RowByteCount, TotalByteCount, Bytes, Diagnostics);
        copy.Success = Success;
        return copy;
    }
}
