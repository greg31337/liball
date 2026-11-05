namespace asn1;

/// <summary>
/// Contains one preserved unknown named field and its rule-specific payload.
/// </summary>
public class AsnUnknownField
{
    /// <summary>
    /// Contains the encoded or XML field name when available.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Indicates whether this field preserves an XML attribute rather than an element.
    /// </summary>
    public bool XmlAttribute;

    /// <summary>
    /// Contains the original XML namespace prefix when available.
    /// </summary>
    public string XmlPrefix = "";

    /// <summary>
    /// Contains the expanded XML namespace name when available.
    /// </summary>
    public string XmlNamespaceUri = "";

    /// <summary>
    /// Contains the preserved BER tag when available.
    /// </summary>
    public AsnTag? Tag;

    /// <summary>
    /// Indicates whether EncodedIndex identifies the original sibling position.
    /// </summary>
    public bool HasEncodedIndex;

    /// <summary>
    /// Contains the zero-based original sibling position.
    /// </summary>
    public int EncodedIndex;

    /// <summary>
    /// Indicates whether EncodedOffset and EncodedLength identify the original byte range.
    /// </summary>
    public bool HasEncodedLocation;

    /// <summary>
    /// Contains the zero-based original byte offset.
    /// </summary>
    public int EncodedOffset;

    /// <summary>
    /// Contains the exact encoded byte length.
    /// </summary>
    public int EncodedLength;

    /// <summary>
    /// Contains the encoding rules associated with the payload.
    /// </summary>
    public AsnEncodingRules EncodingRules;

    /// <summary>
    /// Indicates whether EncodingRules identifies the payload encoding.
    /// </summary>
    public bool HasEncodingRules;

    /// <summary>
    /// Indicates whether BinaryData contains a preserved binary payload.
    /// </summary>
    public bool HasBinaryData;

    /// <summary>
    /// Contains the preserved binary payload when HasBinaryData is true.
    /// </summary>
    public byte[] BinaryData = [];

    /// <summary>
    /// Indicates whether TextData contains a preserved textual payload.
    /// </summary>
    public bool HasTextData;

    /// <summary>
    /// Contains the preserved textual payload when HasTextData is true.
    /// </summary>
    public string TextData = "";

    internal bool IsValid()
    {
        int rules = (int)this.EncodingRules;
        bool textRules = this.EncodingRules == AsnEncodingRules.BasicXer
            || this.EncodingRules == AsnEncodingRules.CanonicalXer
            || this.EncodingRules == AsnEncodingRules.ExtendedXer;
        return this.Name != null
            && this.BinaryData != null
            && this.TextData != null
            && this.XmlPrefix != null
            && this.XmlNamespaceUri != null
            && this.HasEncodingRules
            && rules >= (int)AsnEncodingRules.Ber
            && rules <= (int)AsnEncodingRules.Ecn
            && this.HasBinaryData != this.HasTextData
            && this.HasTextData == textRules
            && (this.Tag == null || this.Tag.IsValid())
            && (!this.HasEncodedIndex || this.EncodedIndex >= 0)
            && (!this.HasEncodedLocation
                || this.EncodedOffset >= 0
                    && this.EncodedLength >= 0
                    && (!this.HasBinaryData
                        || this.EncodedLength == this.BinaryData.Length));
    }
}
