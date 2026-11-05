namespace tiff;

/// <summary>Describes one non-spatial ImageJ hyperstack axis.</summary>
public class TiffImageJAxis
{
    /// <summary>Identifies the axis.</summary>
    public TiffImageJAxisKind Kind;
    /// <summary>Contains the axis size.</summary>
    public int Size;
    /// <summary>Contains the zero-based storage order, with the fastest-changing axis first.</summary>
    public int StorageOrder;

    /// <summary>Creates an empty ImageJ axis.</summary>
    public TiffImageJAxis()
    {
        Kind = TiffImageJAxisKind.Unknown;
        Size = 0;
        StorageOrder = 0;
    }

    internal static TiffImageJAxis Create(TiffImageJAxisKind kind, int size, int storageOrder)
    {
        TiffImageJAxis value = new TiffImageJAxis();
        value.Kind = kind;
        value.Size = size;
        value.StorageOrder = storageOrder;
        return value;
    }

    internal TiffImageJAxis Copy()
    {
        return TiffImageJAxis.Create(Kind, Size, StorageOrder);
    }
}
