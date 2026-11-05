namespace asn1;

internal class OpaquePayloadPolicy
{
    internal bool CanReuseOpenType(
        AsnOpenTypeValue open,
        AsnEncodingRules targetRules)
    {
        return open != null
            && open.IsValid()
            && open.Unresolved
            && open.HasOriginalData
            && open.HasEncodingRules
            && open.EncodingRules == targetRules;
    }

    internal bool CanReuseUnknownExtension(
        AsnUnknownExtension extension,
        AsnEncodingRules targetRules)
    {
        return extension != null
            && extension.IsValid()
            && extension.HasEncodingRules
            && extension.EncodingRules == targetRules;
    }

    internal bool CanReuseUnknownField(
        AsnUnknownField field,
        AsnEncodingRules targetRules)
    {
        return field != null
            && field.IsValid()
            && field.HasEncodingRules
            && field.EncodingRules == targetRules;
    }

    internal bool CanReuseBinarySlice(
        bool hasEncodingRules,
        AsnEncodingRules sourceRules,
        AsnEncodingRules targetRules,
        bool hasBinaryData,
        byte[] data)
    {
        return hasEncodingRules
            && sourceRules == targetRules
            && hasBinaryData
            && data != null;
    }
}
