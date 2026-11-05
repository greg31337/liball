namespace tiff;

/// <summary>Contains exact text that contributed to vendor-variant recognition.</summary>
public class TiffVendorText
{
    /// <summary>Contains the source tag name.</summary>
    public string Source;
    /// <summary>Identifies the source TIFF tag.</summary>
    public int TagId;
    /// <summary>Contains the exact decoded text.</summary>
    public string Text;

    /// <summary>Creates empty vendor text.</summary>
    public TiffVendorText()
    {
        Source = "";
        TagId = 0;
        Text = "";
    }

    internal static TiffVendorText Create(string source, int tagId, string text)
    {
        TiffVendorText value = new TiffVendorText();
        value.Source = TiffVendorMetadataMatch.NormalizeString(source);
        value.TagId = tagId;
        value.Text = TiffVendorMetadataMatch.NormalizeString(text);
        return value;
    }

    internal TiffVendorText Copy()
    {
        return TiffVendorText.Create(Source, TagId, Text);
    }
}
