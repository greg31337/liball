namespace tiff;

/// <summary>Contains one key/value field parsed from recognized vendor text.</summary>
public class TiffVendorField
{
    /// <summary>Contains the vendor field key.</summary>
    public string Key;
    /// <summary>Contains the vendor field value without semantic coercion.</summary>
    public string Value;
    /// <summary>Contains the source tag name.</summary>
    public string Source;
    /// <summary>Identifies the source TIFF tag.</summary>
    public int TagId;

    /// <summary>Creates an empty vendor field.</summary>
    public TiffVendorField()
    {
        Key = "";
        Value = "";
        Source = "";
        TagId = 0;
    }

    internal static TiffVendorField Create(string key, string value, string source, int tagId)
    {
        TiffVendorField field = new TiffVendorField();
        field.Key = TiffVendorMetadataMatch.NormalizeString(key);
        field.Value = TiffVendorMetadataMatch.NormalizeString(value);
        field.Source = TiffVendorMetadataMatch.NormalizeString(source);
        field.TagId = tagId;
        return field;
    }

    internal TiffVendorField Copy()
    {
        return TiffVendorField.Create(Key, Value, Source, TagId);
    }
}
