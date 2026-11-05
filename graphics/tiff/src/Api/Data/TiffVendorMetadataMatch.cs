namespace tiff;

/// <summary>Represents vendor metadata match used by the public TIFF API.</summary>
public class TiffVendorMetadataMatch
{
    /// <summary>Contains the vendor.</summary>
    public string Vendor;
    /// <summary>Contains the format.</summary>
    public string Format;
    /// <summary>Contains the source.</summary>
    public string Source;
    /// <summary>Contains the evidence.</summary>
    public string Evidence;
    /// <summary>Identifies the TIFF tag.</summary>
    public int TagId;
    /// <summary>Indicates whether slide applies.</summary>
    public bool IsSlide;
    /// <summary>Indicates whether scientific applies.</summary>
    public bool IsScientific;

    /// <summary>Creates a new instance of the TIFF Vendor Metadata Match class.</summary>
    public TiffVendorMetadataMatch()
    {
        Vendor = "";
        Format = "";
        Source = "";
        Evidence = "";
        TagId = 0;
        IsSlide = false;
        IsScientific = false;
    }

    /// <summary>Creates a new instance of the TIFF Vendor Metadata Match class.</summary>
    /// <param name="vendor">The vendor.</param>
    /// <param name="format">The format.</param>
    /// <param name="source">The source bytes or samples to process.</param>
    /// <param name="evidence">The evidence.</param>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="isSlide">Whether slide applies.</param>
    /// <param name="isScientific">Whether scientific applies.</param>
    internal static TiffVendorMetadataMatch Create(string vendor, string format, string source, string evidence, int tagId, bool isSlide, bool isScientific)
    {
        TiffVendorMetadataMatch value = new TiffVendorMetadataMatch();
        value.Initialize(vendor, format, source, evidence, tagId, isSlide, isScientific);
        return value;
    }

    internal void Initialize(string vendor, string format, string source, string evidence, int tagId, bool isSlide, bool isScientific)
    {
        Vendor = NormalizeString(vendor);
        Format = NormalizeString(format);
        Source = NormalizeString(source);
        Evidence = NormalizeString(evidence);
        TagId = tagId;
        IsSlide = isSlide;
        IsScientific = isScientific;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF vendor metadata match.</returns>
    internal TiffVendorMetadataMatch Copy()
    {
        return TiffVendorMetadataMatch.Create(Vendor, Format, Source, Evidence, TagId, IsSlide, IsScientific);
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
