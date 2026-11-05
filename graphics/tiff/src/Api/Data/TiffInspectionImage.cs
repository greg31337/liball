namespace tiff;

/// <summary>Represents one image in a TIFF inspection result.</summary>
public class TiffInspectionImage
{
    /// <summary>Identifies the zero-based image directory in the file.</summary>
    public int ImageIndex;
    /// <summary>Contains the normalized image layout and pixel information.</summary>
    public TiffImageInfo Info;
    /// <summary>Contains the tags.</summary>
    public TiffIfdEntry[] Tags;
    /// <summary>Contains the subimage count.</summary>
    public int SubImageCount;

    /// <summary>Creates an empty inspection image.</summary>
    public TiffInspectionImage()
    {
        ImageIndex = TiffDiagnostic.NoImageIndex;
        Info = new TiffImageInfo();
        Tags = new TiffIfdEntry[0];
        SubImageCount = 0;
    }

    internal static TiffInspectionImage Create(int imageIndex, TiffImageInfo info, TiffIfdEntry[] tags, int subImageCount)
    {
        TiffInspectionImage value = new TiffInspectionImage();
        value.Initialize(imageIndex, info, tags, subImageCount);
        return value;
    }

    internal void Initialize(int imageIndex, TiffImageInfo info, TiffIfdEntry[] tags, int subImageCount)
    {
        ImageIndex = imageIndex;
        Info = info == null ? new TiffImageInfo() : info.Copy();
        Tags = TiffDirectory.CopyEntries(tags);
        SubImageCount = subImageCount;
    }

    internal TiffInspectionImage Copy()
    {
        return Create(ImageIndex, Info, Tags, SubImageCount);
    }
}
