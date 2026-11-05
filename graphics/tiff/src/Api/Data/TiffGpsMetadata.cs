namespace tiff;

/// <summary>Represents GPS metadata extracted from or prepared for a TIFF file.</summary>
public class TiffGpsMetadata
{
    /// <summary>Indicates whether the GPS is present.</summary>
    public bool HasGps;
    /// <summary>Contains the parent image index.</summary>
    public int ParentImageIndex;
    /// <summary>Contains the parent offset.</summary>
    public ulong ParentOffset;
    /// <summary>Contains the GPS offset.</summary>
    public ulong GpsOffset;
    /// <summary>Contains the directory.</summary>
    public TiffDirectory Directory;
    /// <summary>Contains the values.</summary>
    public TiffTagValue[] Values;

    /// <summary>Creates a new instance of the TIFF GPS Metadata class.</summary>
    public TiffGpsMetadata()
    {
        HasGps = false;
        ParentImageIndex = 0;
        ParentOffset = 0;
        GpsOffset = 0;
        Directory = new TiffDirectory();
        Values = new TiffTagValue[0];
    }

    /// <summary>Creates a new instance of the TIFF GPS Metadata class.</summary>
    /// <param name="hasGps">The has GPS.</param>
    /// <param name="parentImageIndex">The parent image index.</param>
    /// <param name="parentOffset">The parent offset.</param>
    /// <param name="gpsOffset">The gps offset.</param>
    /// <param name="directory">The TIFF directory whose tags describe the operation.</param>
    /// <param name="values">The values.</param>
    internal static TiffGpsMetadata Create(bool hasGps, int parentImageIndex, ulong parentOffset, ulong gpsOffset, TiffDirectory directory, TiffTagValue[] values)
    {
        TiffGpsMetadata value = new TiffGpsMetadata();
        value.Initialize(hasGps, parentImageIndex, parentOffset, gpsOffset, directory, values);
        return value;
    }

    internal void Initialize(bool hasGps, int parentImageIndex, ulong parentOffset, ulong gpsOffset, TiffDirectory directory, TiffTagValue[] values)
    {
        HasGps = hasGps;
        ParentImageIndex = parentImageIndex;
        ParentOffset = parentOffset;
        GpsOffset = gpsOffset;
        if (directory == null)
        {
            Directory = new TiffDirectory();
        }
        else
        {
            Directory = directory.Copy();
        }

        Values = TiffExifMetadata.CopyValues(values);
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
    /// <returns>The resulting TIFF GPS metadata.</returns>
    internal TiffGpsMetadata Copy()
    {
        return TiffGpsMetadata.Create(HasGps, ParentImageIndex, ParentOffset, GpsOffset, Directory, Values);
    }
}
