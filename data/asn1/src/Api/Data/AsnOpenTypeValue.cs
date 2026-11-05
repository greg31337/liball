namespace asn1;

/// <summary>
/// Contains resolved or unresolved open-type data and its original encoding.
/// </summary>
public class AsnOpenTypeValue
{
    /// <summary>
    /// Indicates whether the governing table resolved a concrete type.
    /// </summary>
    public bool Resolved;

    /// <summary>
    /// Indicates whether the governing table could not resolve a concrete type.
    /// </summary>
    public bool Unresolved;

    /// <summary>
    /// Contains the stable identity of the resolved type.
    /// </summary>
    public string ResolvedTypeId = "";

    /// <summary>
    /// Contains the resolved qualified type name.
    /// </summary>
    public string TypeName = "";

    /// <summary>
    /// Contains the decoded value when Resolved is true and decoding succeeded.
    /// </summary>
    public AsnRuntimeValue? Value;

    /// <summary>
    /// Contains the normalized dispatch key when available.
    /// </summary>
    public AsnRuntimeValue? DispatchKey;

    /// <summary>
    /// Contains the encoding rules used for OriginalData.
    /// </summary>
    public AsnEncodingRules EncodingRules;

    /// <summary>
    /// Indicates whether EncodingRules identifies the original payload encoding.
    /// </summary>
    public bool HasEncodingRules;

    /// <summary>
    /// Indicates whether OriginalData contains the original encoded payload.
    /// </summary>
    public bool HasOriginalData;

    /// <summary>
    /// Contains the original encoded payload when HasOriginalData is true.
    /// </summary>
    public byte[] OriginalData = [];

    /// <summary>
    /// Contains the original BER tag when available.
    /// </summary>
    public AsnTag? OriginalTag;

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

    internal string GetResolvedTypeIdentity()
    {
        if (this.ResolvedTypeId != null && this.ResolvedTypeId.Length > 0)
        {
            return this.ResolvedTypeId;
        }

        return this.TypeName == null ? "" : this.TypeName;
    }

    internal bool IsValid()
    {
        if (this.ResolvedTypeId == null
            || this.TypeName == null
            || this.OriginalData == null
            || this.Resolved == this.Unresolved
            || this.HasEncodingRules != this.HasOriginalData
            || this.OriginalTag != null && !this.OriginalTag.IsValid()
            || this.HasEncodedLocation
                && (this.EncodedOffset < 0
                    || this.EncodedLength < 0
                    || this.HasOriginalData
                        && this.EncodedLength != this.OriginalData.Length))
        {
            return false;
        }

        if (!this.HasOriginalData && this.OriginalData.Length != 0)
        {
            return false;
        }

        if (this.HasEncodingRules)
        {
            int rules = (int)this.EncodingRules;
            if (rules < (int)AsnEncodingRules.Ber
                || rules > (int)AsnEncodingRules.Ecn)
            {
                return false;
            }
        }

        if (this.Resolved)
        {
            return this.GetResolvedTypeIdentity().Length > 0
                && this.Value != null;
        }

        return this.GetResolvedTypeIdentity().Length == 0
            && this.Value == null;
    }
}
