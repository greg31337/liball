namespace tiff;

/// <summary>Contains the values and diagnostics produced by an inspection operation.</summary>
public class TiffInspectionResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the parsed TIFF header.</summary>
    public TiffHeader Header;
    /// <summary>Contains the images.</summary>
    public TiffInspectionImage[] Images;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;
    /// <summary>Contains the file byte count.</summary>
    public int FileByteCount;

    /// <summary>Creates an empty inspection result.</summary>
    public TiffInspectionResult()
    {
        Success = false;
        Header = new TiffHeader();
        Images = new TiffInspectionImage[0];
        Diagnostics = new TiffDiagnostics();
        FileByteCount = 0;
    }

    internal static TiffInspectionResult Create(TiffHeader header, TiffInspectionImage[] images, TiffDiagnostics diagnostics, int fileByteCount)
    {
        TiffInspectionResult value = new TiffInspectionResult();
        value.Initialize(header, images, diagnostics, fileByteCount);
        return value;
    }

    internal void Initialize(TiffHeader header, TiffInspectionImage[] images, TiffDiagnostics diagnostics, int fileByteCount)
    {
        Header = TiffFile.CopyHeader(header);
        Images = CopyImages(images);
        Diagnostics = diagnostics;
        FileByteCount = fileByteCount;
        Success = !Diagnostics.HasErrors();
    }

    internal int ImageCount()
    {
        return Images.Length;
    }

    internal static TiffInspectionImage[] CopyImages(TiffInspectionImage[] images)
    {
        if (images == null)
        {
            return new TiffInspectionImage[0];
        }

        TiffInspectionImage[] copy = new TiffInspectionImage[images.Length];
        int index = 0;
        while (index < images.Length)
        {
            copy[index] = images[index] == null ? new TiffInspectionImage() : images[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
