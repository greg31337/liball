namespace tiff;

/// <summary>Represents geographic key directory used by the public TIFF API.</summary>
public class TiffGeoKeyDirectory
{
    /// <summary>Indicates whether the directory is present.</summary>
    public bool HasDirectory;
    /// <summary>Contains the key directory version.</summary>
    public int KeyDirectoryVersion;
    /// <summary>Contains the key revision.</summary>
    public int KeyRevision;
    /// <summary>Contains the minor revision.</summary>
    public int MinorRevision;
    /// <summary>Contains the number of keys.</summary>
    public int NumberOfKeys;
    /// <summary>Contains the keys.</summary>
    public TiffGeoKeyEntry[] Keys;
    /// <summary>Contains the resolved coordinate model type.</summary>
    public TiffGeoModelType ModelType;
    /// <summary>Contains the resolved raster pixel semantics.</summary>
    public TiffGeoRasterType RasterType;
    /// <summary>Indicates whether a geographic CRS EPSG identifier is present.</summary>
    public bool HasGeographicCrsEpsgCode;
    /// <summary>Contains the geographic CRS EPSG identifier.</summary>
    public int GeographicCrsEpsgCode;
    /// <summary>Indicates whether a projected CRS EPSG identifier is present.</summary>
    public bool HasProjectedCrsEpsgCode;
    /// <summary>Contains the projected CRS EPSG identifier.</summary>
    public int ProjectedCrsEpsgCode;
    /// <summary>Indicates whether a vertical CRS EPSG identifier is present.</summary>
    public bool HasVerticalCrsEpsgCode;
    /// <summary>Contains the vertical CRS EPSG identifier.</summary>
    public int VerticalCrsEpsgCode;
    /// <summary>Indicates whether at least one CRS or model key declares a user-defined value.</summary>
    public bool HasUserDefinedCrs;
    /// <summary>Contains the preferred user-defined CRS citation when available.</summary>
    public string UserDefinedCrsName;
    /// <summary>Contains resolved keys in the private GeoKey identifier range.</summary>
    public TiffGeoKeyEntry[] PrivateKeys;

    /// <summary>Creates a new instance of the TIFF geographic Key Directory class.</summary>
    public TiffGeoKeyDirectory()
    {
        HasDirectory = false;
        KeyDirectoryVersion = 0;
        KeyRevision = 0;
        MinorRevision = 0;
        NumberOfKeys = 0;
        Keys = new TiffGeoKeyEntry[0];
        ModelType = TiffGeoModelType.Unknown;
        RasterType = TiffGeoRasterType.Unknown;
        HasGeographicCrsEpsgCode = false;
        GeographicCrsEpsgCode = 0;
        HasProjectedCrsEpsgCode = false;
        ProjectedCrsEpsgCode = 0;
        HasVerticalCrsEpsgCode = false;
        VerticalCrsEpsgCode = 0;
        HasUserDefinedCrs = false;
        UserDefinedCrsName = "";
        PrivateKeys = new TiffGeoKeyEntry[0];
    }

    /// <summary>Creates a new instance of the TIFF geographic Key Directory class.</summary>
    /// <param name="hasDirectory">The has directory.</param>
    /// <param name="keyDirectoryVersion">The key directory version.</param>
    /// <param name="keyRevision">The key revision.</param>
    /// <param name="minorRevision">The minor revision.</param>
    /// <param name="numberOfKeys">The number of keys.</param>
    /// <param name="keys">The keys.</param>
    internal static TiffGeoKeyDirectory Create(bool hasDirectory, int keyDirectoryVersion, int keyRevision, int minorRevision, int numberOfKeys, TiffGeoKeyEntry[] keys)
    {
        TiffGeoKeyDirectory value = new TiffGeoKeyDirectory();
        value.Initialize(hasDirectory, keyDirectoryVersion, keyRevision, minorRevision, numberOfKeys, keys);
        return value;
    }

    internal void Initialize(bool hasDirectory, int keyDirectoryVersion, int keyRevision, int minorRevision, int numberOfKeys, TiffGeoKeyEntry[] keys)
    {
        HasDirectory = hasDirectory;
        KeyDirectoryVersion = keyDirectoryVersion;
        KeyRevision = keyRevision;
        MinorRevision = minorRevision;
        NumberOfKeys = numberOfKeys;
        Keys = CopyKeys(keys);
    }

    /// <summary>Returns the number of available key entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int KeyCount()
    {
        return Keys.Length;
    }

    /// <summary>Finds the key, or returns the type's documented missing-value result.</summary>
    /// <param name="keyId">The key id.</param>
    /// <returns>The requested value or model.</returns>
    internal TiffGeoKeyEntry FindKey(int keyId)
    {
        int index = 0;
        while (index < Keys.Length)
        {
            if (Keys[index] != null && Keys[index].KeyId == keyId)
            {
                return Keys[index].Copy();
            }

            index = index + 1;
        }

        return new TiffGeoKeyEntry();
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF geographic key directory.</returns>
    internal TiffGeoKeyDirectory Copy()
    {
        TiffGeoKeyDirectory copy = TiffGeoKeyDirectory.Create(HasDirectory, KeyDirectoryVersion, KeyRevision, MinorRevision, NumberOfKeys, Keys);
        copy.ModelType = ModelType;
        copy.RasterType = RasterType;
        copy.HasGeographicCrsEpsgCode = HasGeographicCrsEpsgCode;
        copy.GeographicCrsEpsgCode = GeographicCrsEpsgCode;
        copy.HasProjectedCrsEpsgCode = HasProjectedCrsEpsgCode;
        copy.ProjectedCrsEpsgCode = ProjectedCrsEpsgCode;
        copy.HasVerticalCrsEpsgCode = HasVerticalCrsEpsgCode;
        copy.VerticalCrsEpsgCode = VerticalCrsEpsgCode;
        copy.HasUserDefinedCrs = HasUserDefinedCrs;
        copy.UserDefinedCrsName = UserDefinedCrsName == null ? "" : UserDefinedCrsName;
        copy.PrivateKeys = CopyKeys(PrivateKeys);
        return copy;
    }

    internal static TiffGeoKeyEntry[] CopyKeys(TiffGeoKeyEntry[] keys)
    {
        if (keys == null)
        {
            return new TiffGeoKeyEntry[0];
        }

        TiffGeoKeyEntry[] copy = new TiffGeoKeyEntry[keys.Length];
        int index = 0;
        while (index < keys.Length)
        {
            if (keys[index] == null)
            {
                copy[index] = new TiffGeoKeyEntry();
            }
            else
            {
                copy[index] = keys[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
