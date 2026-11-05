namespace tiff;

/// <summary>Describes one physical or virtual TIFF strip.</summary>
public class TiffStripSegment
{
    /// <summary>Contains the strip index in this layout.</summary>
    public ulong StripIndex;
    /// <summary>Contains the physical source strip index.</summary>
    public ulong SourceStripIndex;
    /// <summary>Contains the strip index within its sample plane.</summary>
    public ulong StripIndexInPlane;
    /// <summary>Contains the sample plane index for planar-separate data.</summary>
    public int SampleIndex;
    /// <summary>Contains the first image row represented by the strip.</summary>
    public ulong FirstRow;
    /// <summary>Contains the number of represented image rows.</summary>
    public ulong RowCount;
    /// <summary>Contains the represented image width.</summary>
    public ulong Width;
    /// <summary>Contains the expected decoded byte count.</summary>
    public ulong DecodedByteCount;
    /// <summary>Contains the useful image byte count.</summary>
    public ulong ImageByteCount;
    /// <summary>Contains the encoded byte count stored in the file.</summary>
    public ulong StoredByteCount;
    /// <summary>Contains the encoded byte range in the file.</summary>
    public TiffByteRange ByteRange;
    /// <summary>Indicates whether the strip contains fewer rows than the nominal strip size.</summary>
    public bool IsPartial;
    /// <summary>Indicates whether the strip has no stored payload.</summary>
    public bool IsSparse;
    /// <summary>Indicates whether the strip is a virtual portion of a physical source strip.</summary>
    public bool IsChopped;

    /// <summary>Creates an empty strip-segment description.</summary>
    public TiffStripSegment()
    {
        StripIndex = 0;
        SourceStripIndex = 0;
        StripIndexInPlane = 0;
        SampleIndex = 0;
        FirstRow = 0;
        RowCount = 0;
        Width = 0;
        DecodedByteCount = 0;
        ImageByteCount = 0;
        StoredByteCount = 0;
        ByteRange = new TiffByteRange();
        IsPartial = false;
        IsSparse = true;
        IsChopped = false;
    }

    internal TiffStripSegment Copy()
    {
        TiffStripSegment copy = new TiffStripSegment();
        copy.StripIndex = StripIndex;
        copy.SourceStripIndex = SourceStripIndex;
        copy.StripIndexInPlane = StripIndexInPlane;
        copy.SampleIndex = SampleIndex;
        copy.FirstRow = FirstRow;
        copy.RowCount = RowCount;
        copy.Width = Width;
        copy.DecodedByteCount = DecodedByteCount;
        copy.ImageByteCount = ImageByteCount;
        copy.StoredByteCount = StoredByteCount;
        copy.ByteRange = ByteRange.Copy();
        copy.IsPartial = IsPartial;
        copy.IsSparse = IsSparse;
        copy.IsChopped = IsChopped;
        return copy;
    }
}
