namespace asn1;

/// <summary>
/// Contains one preserved unknown extension addition and its rule-specific payload.
/// </summary>
public class AsnUnknownExtension
{
    /// <summary>
    /// Contains the zero-based extension-addition index.
    /// </summary>
    public int Index;

    /// <summary>
    /// Indicates whether the extension addition was present in the encoded value.
    /// </summary>
    public bool Present;

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
    /// Indicates whether EncodedBitOffset and EncodedBitLength identify the original bit range.
    /// </summary>
    public bool HasEncodedBitLocation;

    /// <summary>
    /// Contains the zero-based original bit offset.
    /// </summary>
    public int EncodedBitOffset;

    /// <summary>
    /// Contains the exact encoded bit length.
    /// </summary>
    public int EncodedBitLength;

    /// <summary>
    /// Indicates whether AlignmentPaddingBitCount records preceding PER alignment padding.
    /// </summary>
    public bool HasAlignmentPaddingBitCount;

    /// <summary>
    /// Contains the number of preserved alignment-padding bits preceding the open type.
    /// </summary>
    public int AlignmentPaddingBitCount;

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
        if (this.Index < 0
            || this.BinaryData == null
            || this.TextData == null
            || !this.HasEncodingRules
            || rules < (int)AsnEncodingRules.Ber
            || rules > (int)AsnEncodingRules.Ecn
            || this.Tag != null && !this.Tag.IsValid()
            || this.HasEncodedIndex && this.EncodedIndex < 0
            || this.HasEncodedLocation
                && (this.EncodedOffset < 0
                    || this.EncodedLength < 0
                    || this.HasBinaryData
                        && this.EncodedLength != this.BinaryData.Length)
            || this.HasEncodedBitLocation
                && (this.EncodedBitOffset < 0 || this.EncodedBitLength < 0)
            || this.HasAlignmentPaddingBitCount
                && (this.AlignmentPaddingBitCount < 0
                    || this.AlignmentPaddingBitCount > 7))
        {
            return false;
        }

        if (!this.Present && this.HasEncodedIndex
            && !this.HasBinaryData && !this.HasTextData)
        {
            return true;
        }

        return this.HasBinaryData != this.HasTextData
            && this.HasTextData == textRules;
    }
}
