namespace tiff;

/// <summary>Identifies a rectangular image region in source-image coordinates.</summary>
public class TiffRegion
{
    /// <summary>Contains the zero-based first column.</summary>
    public ulong Column;
    /// <summary>Contains the zero-based first row.</summary>
    public ulong Row;
    /// <summary>Contains the region width in pixels.</summary>
    public ulong Width;
    /// <summary>Contains the region height in rows.</summary>
    public ulong Length;

    /// <summary>Creates an empty region.</summary>
    public TiffRegion()
    {
        Column = 0;
        Row = 0;
        Width = 0;
        Length = 0;
    }

    internal static TiffRegion Create(ulong column, ulong row, ulong width, ulong length)
    {
        TiffRegion value = new TiffRegion();
        value.Initialize(column, row, width, length);
        return value;
    }

    internal void Initialize(ulong column, ulong row, ulong width, ulong length)
    {
        Column = column;
        Row = row;
        Width = width;
        Length = length;
    }

    internal TiffRegion Copy()
    {
        return TiffRegion.Create(Column, Row, Width, Length);
    }
}
