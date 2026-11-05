namespace tiff;

/// <summary>Represents EXIF metadata extracted from or prepared for a TIFF file.</summary>
public class TiffExifMetadata
{
    /// <summary>Indicates whether the EXIF is present.</summary>
    public bool HasExif;
    /// <summary>Contains the parent image index.</summary>
    public int ParentImageIndex;
    /// <summary>Contains the parent offset.</summary>
    public ulong ParentOffset;
    /// <summary>Contains the EXIF offset.</summary>
    public ulong ExifOffset;
    /// <summary>Contains the directory.</summary>
    public TiffDirectory Directory;
    /// <summary>Contains the values.</summary>
    public TiffTagValue[] Values;
    /// <summary>Contains the interoperability pointers.</summary>
    public TiffIfdPointer[] InteroperabilityPointers;

    /// <summary>Creates a new instance of the TIFF EXIF Metadata class.</summary>
    public TiffExifMetadata()
    {
        HasExif = false;
        ParentImageIndex = 0;
        ParentOffset = 0;
        ExifOffset = 0;
        Directory = new TiffDirectory();
        Values = new TiffTagValue[0];
        InteroperabilityPointers = new TiffIfdPointer[0];
    }

    /// <summary>Creates a new instance of the TIFF EXIF Metadata class.</summary>
    /// <param name="hasExif">The has EXIF.</param>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="exifOffset">The exif offset.</param>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="values">The values.</param>
    /// <param name="interoperabilityPointers">The interoperability pointers.</param>
    internal static TiffExifMetadata Create(bool hasExif, int parentImageIndex, ulong parentOffset, ulong exifOffset, TiffDirectory directory, TiffTagValue[] values, TiffIfdPointer[] interoperabilityPointers)
    {
        TiffExifMetadata value = new TiffExifMetadata();
        value.Initialize(hasExif, parentImageIndex, parentOffset, exifOffset, directory, values, interoperabilityPointers);
        return value;
    }

    internal void Initialize(bool hasExif, int parentImageIndex, ulong parentOffset, ulong exifOffset, TiffDirectory directory, TiffTagValue[] values, TiffIfdPointer[] interoperabilityPointers)
    {
        HasExif = hasExif;
        ParentImageIndex = parentImageIndex;
        ParentOffset = parentOffset;
        ExifOffset = exifOffset;
        if (directory == null)
        {
            Directory = new TiffDirectory();
        }
        else
        {
            Directory = directory.Copy();
        }

        Values = CopyValues(values);
        InteroperabilityPointers = TiffIfdPointerReadResult.CopyPointers(interoperabilityPointers);
    }

    /// <summary>Returns the number of available value entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int ValueCount()
    {
        return Values.Length;
    }

    /// <summary>Finds the value, or returns the type's documented missing-value result.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>A detached copy of the requested value, or <c>null</c> when the tag is absent.</returns>
    internal TiffTagValue? FindValue(int tagId)
    {
        int index = 0;
        while (index < Values.Length)
        {
            if (Values[index] != null && Values[index].TagId == tagId)
            {
                return Values[index].Copy();
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Determines whether the collection contains tag.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool ContainsTag(int tagId)
    {
        return FindValue(tagId) != null;
    }

    /// <summary>Returns the ASCII.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The requested value or model.</returns>
    internal string GetAscii(int tagId)
    {
        TiffTagValue? value = FindValue(tagId);
        if (value == null || !value.IsAscii())
        {
            return "";
        }

        return value.Text;
    }

    /// <summary>Returns the unsigned scalar.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The requested value or model.</returns>
    internal ulong GetUnsignedScalar(int tagId)
    {
        TiffTagValue? value = FindValue(tagId);
        if (value == null || !value.IsUnsigned() || value.UnsignedValues.Length == 0)
        {
            return 0;
        }

        return value.UnsignedValues[0];
    }

    /// <summary>Returns the rational scalar.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>The requested value or model.</returns>
    internal TiffRational GetRationalScalar(int tagId)
    {
        TiffTagValue? value = FindValue(tagId);
        if (value == null || !value.IsRational() || value.RationalValues.Length == 0)
        {
            return new TiffRational();
        }

        return value.RationalValues[0].Copy();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF EXIF metadata.</returns>
    internal TiffExifMetadata Copy()
    {
        return TiffExifMetadata.Create(HasExif, ParentImageIndex, ParentOffset, ExifOffset, Directory, Values, InteroperabilityPointers);
    }

    internal static TiffTagValue[] CopyValues(TiffTagValue[] values)
    {
        if (values == null)
        {
            return new TiffTagValue[0];
        }

        TiffTagValue[] copy = new TiffTagValue[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                copy[index] = new TiffTagValue();
            }
            else
            {
                copy[index] = values[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
