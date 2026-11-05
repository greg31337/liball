namespace tiff;

/// <summary>Contains recognized Digital Negative metadata and lossless DNG field values.</summary>
public class TiffDngMetadata
{
    /// <summary>Indicates whether at least one DNG field was present.</summary>
    public bool HasDng;
    /// <summary>Contains the four DNG version components.</summary>
    public int[] Version;
    /// <summary>Contains the four backward-compatible DNG version components.</summary>
    public int[] BackwardVersion;
    /// <summary>Contains the unique camera model.</summary>
    public string UniqueCameraModel;
    /// <summary>Contains the UTF-8 localized camera model.</summary>
    public string LocalizedCameraModel;
    /// <summary>Contains all recognized DNG tags in source-directory order.</summary>
    public TiffDngField[] Fields;
    /// <summary>Contains the exact DNGPrivateData payload.</summary>
    public byte[] PrivateData;
    /// <summary>Contains the exact RawDataUniqueID bytes.</summary>
    public byte[] RawDataUniqueId;
    /// <summary>Contains the exact OriginalRawFileData payload.</summary>
    public byte[] OriginalRawFileData;

    /// <summary>Creates empty DNG metadata.</summary>
    public TiffDngMetadata()
    {
        HasDng = false;
        Version = new int[0];
        BackwardVersion = new int[0];
        UniqueCameraModel = "";
        LocalizedCameraModel = "";
        Fields = new TiffDngField[0];
        PrivateData = new byte[0];
        RawDataUniqueId = new byte[0];
        OriginalRawFileData = new byte[0];
    }

    internal static TiffDngMetadata Create(int[] version, int[] backwardVersion, string uniqueCameraModel, string localizedCameraModel, TiffDngField[] fields, byte[] privateData, byte[] rawDataUniqueId, byte[] originalRawFileData)
    {
        TiffDngMetadata metadata = new TiffDngMetadata();
        metadata.Version = CopyIntegers(version);
        metadata.BackwardVersion = CopyIntegers(backwardVersion);
        metadata.UniqueCameraModel = NormalizeString(uniqueCameraModel);
        metadata.LocalizedCameraModel = NormalizeString(localizedCameraModel);
        metadata.Fields = CopyFields(fields);
        metadata.PrivateData = TiffTagValue.CopyByteArray(privateData);
        metadata.RawDataUniqueId = TiffTagValue.CopyByteArray(rawDataUniqueId);
        metadata.OriginalRawFileData = TiffTagValue.CopyByteArray(originalRawFileData);
        metadata.HasDng = metadata.Fields.Length > 0;
        return metadata;
    }

    internal TiffDngMetadata Copy()
    {
        return TiffDngMetadata.Create(Version, BackwardVersion, UniqueCameraModel, LocalizedCameraModel, Fields, PrivateData, RawDataUniqueId, OriginalRawFileData);
    }

    internal static int[] CopyIntegers(int[] values)
    {
        if (values == null)
        {
            return new int[0];
        }

        int[] copy = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static TiffDngField[] CopyFields(TiffDngField[] fields)
    {
        if (fields == null)
        {
            return new TiffDngField[0];
        }

        TiffDngField[] copy = new TiffDngField[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index] == null)
            {
                copy[index] = new TiffDngField();
            }
            else
            {
                copy[index] = fields[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static string NormalizeString(string value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
