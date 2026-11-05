namespace tiff;

/// <summary>Identifies one TIFF file referenced by OME plane mappings.</summary>
public class TiffOmeFileReference
{
    /// <summary>Contains the referenced file UUID.</summary>
    public string Uuid;
    /// <summary>Contains the referenced file name.</summary>
    public string FileName;
    /// <summary>Indicates whether the reference identifies the currently opened TIFF file.</summary>
    public bool IsCurrentFile;

    /// <summary>Creates an empty OME file reference.</summary>
    public TiffOmeFileReference()
    {
        Uuid = "";
        FileName = "";
        IsCurrentFile = false;
    }

    internal static TiffOmeFileReference Create(string uuid, string fileName, bool isCurrentFile)
    {
        TiffOmeFileReference value = new TiffOmeFileReference();
        value.Uuid = TiffOmeMetadata.NormalizeString(uuid);
        value.FileName = TiffOmeMetadata.NormalizeString(fileName);
        value.IsCurrentFile = isCurrentFile;
        return value;
    }

    internal TiffOmeFileReference Copy()
    {
        return TiffOmeFileReference.Create(Uuid, FileName, IsCurrentFile);
    }
}
