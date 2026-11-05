namespace tiff;

/// <summary>Contains structured and lossless metadata for one recognized TIFF vendor variant.</summary>
public class TiffVendorVariantMetadata
{
    /// <summary>Indicates whether this vendor variant was recognized.</summary>
    public bool HasMetadata;
    /// <summary>Contains the vendor name.</summary>
    public string Vendor;
    /// <summary>Contains the format name.</summary>
    public string Format;
    /// <summary>Indicates whether the variant describes whole-slide imagery.</summary>
    public bool IsSlide;
    /// <summary>Indicates whether the variant describes scientific imagery.</summary>
    public bool IsScientific;
    /// <summary>Contains exact recognized text sources.</summary>
    public TiffVendorText[] Texts;
    /// <summary>Contains parsed key/value fields in source order.</summary>
    public TiffVendorField[] Fields;
    /// <summary>Contains opaque private payloads preserved from the image directory.</summary>
    public TiffVendorPrivatePayload[] PrivatePayloads;

    /// <summary>Creates empty vendor-variant metadata.</summary>
    public TiffVendorVariantMetadata()
    {
        HasMetadata = false;
        Vendor = "";
        Format = "";
        IsSlide = false;
        IsScientific = false;
        Texts = new TiffVendorText[0];
        Fields = new TiffVendorField[0];
        PrivatePayloads = new TiffVendorPrivatePayload[0];
    }

    internal static TiffVendorVariantMetadata Create(bool hasMetadata, string vendor, string format, bool isSlide, bool isScientific, TiffVendorText[] texts, TiffVendorField[] fields, TiffVendorPrivatePayload[] privatePayloads)
    {
        TiffVendorVariantMetadata metadata = new TiffVendorVariantMetadata();
        metadata.HasMetadata = hasMetadata;
        metadata.Vendor = TiffVendorMetadataMatch.NormalizeString(vendor);
        metadata.Format = TiffVendorMetadataMatch.NormalizeString(format);
        metadata.IsSlide = isSlide;
        metadata.IsScientific = isScientific;
        metadata.Texts = CopyTexts(texts);
        metadata.Fields = CopyFields(fields);
        metadata.PrivatePayloads = CopyPayloads(privatePayloads);
        return metadata;
    }

    internal TiffVendorVariantMetadata Copy()
    {
        return TiffVendorVariantMetadata.Create(HasMetadata, Vendor, Format, IsSlide, IsScientific, Texts, Fields, PrivatePayloads);
    }

    internal static TiffVendorText[] CopyTexts(TiffVendorText[] values)
    {
        if (values == null)
        {
            return new TiffVendorText[0];
        }

        TiffVendorText[] copy = new TiffVendorText[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                copy[index] = new TiffVendorText();
            }
            else
            {
                copy[index] = values[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static TiffVendorField[] CopyFields(TiffVendorField[] values)
    {
        if (values == null)
        {
            return new TiffVendorField[0];
        }

        TiffVendorField[] copy = new TiffVendorField[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                copy[index] = new TiffVendorField();
            }
            else
            {
                copy[index] = values[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static TiffVendorPrivatePayload[] CopyPayloads(TiffVendorPrivatePayload[] values)
    {
        if (values == null)
        {
            return new TiffVendorPrivatePayload[0];
        }

        TiffVendorPrivatePayload[] copy = new TiffVendorPrivatePayload[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                copy[index] = new TiffVendorPrivatePayload();
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
