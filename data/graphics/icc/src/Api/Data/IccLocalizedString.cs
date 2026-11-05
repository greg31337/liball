namespace icc;

/// <summary>Represents one localized Unicode string.</summary>
public class IccLocalizedString
{
    /// <summary>Gets or sets the two-character language code.</summary>
    public string Language;
    /// <summary>Gets or sets the two-character country code.</summary>
    public string Country;
    /// <summary>Gets or sets the localized text.</summary>
    public string Text;
    /// <summary>Gets or sets the original UTF-16 big-endian bytes.</summary>
    public byte[] RawBytes;

    /// <summary>Initializes an empty unspecified locale string.</summary>
    public IccLocalizedString()
    {
        Language = string.Empty;
        Country = string.Empty;
        Text = string.Empty;
        RawBytes = new byte[0];
    }
}
