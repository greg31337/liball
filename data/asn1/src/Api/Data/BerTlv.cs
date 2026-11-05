namespace asn1;

/// <summary>
/// Contains one schema-less BER TLV node and its retained encoding.
/// </summary>
public class BerTlv
{
    internal int EncodedOffset = -1;
    internal bool UseEncodedData;

    /// <summary>
    /// Contains the TLV tag.
    /// </summary>
    public AsnTag Tag = new AsnTag();

    /// <summary>
    /// Indicates whether the TLV uses constructed encoding.
    /// </summary>
    public bool Constructed;

    /// <summary>
    /// Indicates whether the TLV uses indefinite length.
    /// </summary>
    public bool IndefiniteLength;

    /// <summary>
    /// Contains primitive contents.
    /// </summary>
    public byte[] Content = [];

    /// <summary>
    /// Contains constructed child nodes in encoded order.
    /// </summary>
    public BerTlv[] Children = [];

    /// <summary>
    /// Contains the exact original TLV encoding when retained.
    /// </summary>
    public byte[] EncodedData = [];

    internal bool IsValid()
    {
        if (this.Tag == null
            || !this.Tag.IsValid()
            || this.Content == null
            || this.Children == null
            || this.EncodedData == null)
        {
            return false;
        }

        if (!this.Constructed)
        {
            return !this.IndefiniteLength && this.Children.Length == 0;
        }

        if (this.Content.Length != 0)
        {
            return false;
        }

        foreach (BerTlv child in this.Children)
        {
            if (child == null || !child.IsValid())
            {
                return false;
            }
        }

        return true;
    }
}
