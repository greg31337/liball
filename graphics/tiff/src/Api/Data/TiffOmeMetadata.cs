namespace tiff;

/// <summary>Represents OME metadata extracted from or prepared for a TIFF file.</summary>
public class TiffOmeMetadata
{
    /// <summary>Indicates whether the OME xml is present.</summary>
    public bool HasOmeXml;
    /// <summary>Contains the xml.</summary>
    public string Xml;
    /// <summary>Contains the image description text.</summary>
    public string ImageDescriptionText;
    /// <summary>Contains the image description raw bytes.</summary>
    public byte[] ImageDescriptionRawBytes;
    /// <summary>Indicates whether valid OME XML was found but required dataset details are incomplete.</summary>
    public bool IsPartialMetadata;
    /// <summary>Indicates whether plane mappings reference more than one TIFF file.</summary>
    public bool IsMultiFile;
    /// <summary>Contains the OME document UUID.</summary>
    public string Uuid;
    /// <summary>Contains the companion metadata file named by a BinaryOnly element.</summary>
    public string PartialMetadataFileName;
    /// <summary>Contains the companion metadata UUID named by a BinaryOnly element.</summary>
    public string PartialMetadataUuid;
    /// <summary>Contains parsed OME images and their plane mappings.</summary>
    public TiffOmeImage[] Images;
    /// <summary>Contains distinct file references used by plane mappings.</summary>
    public TiffOmeFileReference[] FileReferences;
    /// <summary>Contains source-derived links between OME images and TIFF SubIFD pyramid levels.</summary>
    public TiffOmePyramidLevel[] PyramidLevels;

    /// <summary>Creates a new instance of the TIFF OME Metadata class.</summary>
    public TiffOmeMetadata()
    {
        HasOmeXml = false;
        Xml = "";
        ImageDescriptionText = "";
        ImageDescriptionRawBytes = new byte[0];
        IsPartialMetadata = false;
        IsMultiFile = false;
        Uuid = "";
        PartialMetadataFileName = "";
        PartialMetadataUuid = "";
        Images = new TiffOmeImage[0];
        FileReferences = new TiffOmeFileReference[0];
        PyramidLevels = new TiffOmePyramidLevel[0];
    }

    /// <summary>Creates a new instance of the TIFF OME Metadata class.</summary>
    /// <param name="hasOmeXml">The has OME xml.</param>
    /// <param name="xml">The xml.</param>
    /// <param name="imageDescriptionText">The image description text.</param>
    /// <param name="imageDescriptionRawBytes">The image description raw bytes.</param>
    /// <param name="isPartialMetadata">Whether required OME dataset details are incomplete.</param>
    /// <param name="isMultiFile">Whether mappings reference multiple files.</param>
    /// <param name="uuid">The OME document UUID.</param>
    /// <param name="images">The parsed OME images.</param>
    /// <param name="fileReferences">The distinct file references.</param>
    /// <param name="pyramidLevels">The OME pyramid relationships.</param>
    internal static TiffOmeMetadata Create(bool hasOmeXml, string xml, string imageDescriptionText, byte[] imageDescriptionRawBytes, bool isPartialMetadata, bool isMultiFile, string uuid, TiffOmeImage[] images, TiffOmeFileReference[] fileReferences, TiffOmePyramidLevel[] pyramidLevels)
    {
        TiffOmeMetadata value = new TiffOmeMetadata();
        value.Initialize(hasOmeXml, xml, imageDescriptionText, imageDescriptionRawBytes, isPartialMetadata, isMultiFile, uuid, images, fileReferences, pyramidLevels);
        return value;
    }

    internal void Initialize(bool hasOmeXml, string xml, string imageDescriptionText, byte[] imageDescriptionRawBytes, bool isPartialMetadata, bool isMultiFile, string uuid, TiffOmeImage[] images, TiffOmeFileReference[] fileReferences, TiffOmePyramidLevel[] pyramidLevels)
    {
        HasOmeXml = hasOmeXml;
        Xml = NormalizeString(xml);
        ImageDescriptionText = NormalizeString(imageDescriptionText);
        ImageDescriptionRawBytes = TiffTagValue.CopyByteArray(imageDescriptionRawBytes);
        IsPartialMetadata = isPartialMetadata;
        IsMultiFile = isMultiFile;
        Uuid = NormalizeString(uuid);
        PartialMetadataFileName = "";
        PartialMetadataUuid = "";
        Images = CopyImages(images);
        FileReferences = CopyFileReferences(fileReferences);
        PyramidLevels = CopyPyramidLevels(pyramidLevels);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF OME metadata.</returns>
    internal TiffOmeMetadata Copy()
    {
        TiffOmeMetadata copy = TiffOmeMetadata.Create(HasOmeXml, Xml, ImageDescriptionText, ImageDescriptionRawBytes, IsPartialMetadata, IsMultiFile, Uuid, Images, FileReferences, PyramidLevels);
        copy.PartialMetadataFileName = NormalizeString(PartialMetadataFileName);
        copy.PartialMetadataUuid = NormalizeString(PartialMetadataUuid);
        return copy;
    }

    internal static TiffOmeImage[] CopyImages(TiffOmeImage[] images)
    {
        if (images == null)
        {
            return new TiffOmeImage[0];
        }

        TiffOmeImage[] copy = new TiffOmeImage[images.Length];
        int index = 0;
        while (index < images.Length)
        {
            copy[index] = images[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static TiffOmeFileReference[] CopyFileReferences(TiffOmeFileReference[] references)
    {
        if (references == null)
        {
            return new TiffOmeFileReference[0];
        }

        TiffOmeFileReference[] copy = new TiffOmeFileReference[references.Length];
        int index = 0;
        while (index < references.Length)
        {
            copy[index] = references[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static TiffOmePyramidLevel[] CopyPyramidLevels(TiffOmePyramidLevel[] levels)
    {
        if (levels == null)
        {
            return new TiffOmePyramidLevel[0];
        }

        TiffOmePyramidLevel[] copy = new TiffOmePyramidLevel[levels.Length];
        int index = 0;
        while (index < levels.Length)
        {
            copy[index] = levels[index].Copy();
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
