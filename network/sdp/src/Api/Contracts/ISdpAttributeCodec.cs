namespace sdp;

/// <summary>
/// Converts between generic SDP attributes and supported typed projections.
/// </summary>
public interface ISdpAttributeCodec
{
    /// <summary>Projects a generic attribute into typed data when its name is supported.</summary>
    /// <param name="attribute">The generic attribute to decode.</param>
    /// <returns>A result that retains the generic source and any typed projection.</returns>
    SdpAttributeDecodeResult DecodeAttribute(SdpAttribute attribute);

    /// <summary>Encodes supported typed attribute data into a generic attribute.</summary>
    /// <param name="typedAttribute">The typed attribute data to encode.</param>
    /// <returns>A result containing the generic attribute or a structured diagnostic.</returns>
    SdpAttributeEncodeResult EncodeAttribute(object typedAttribute);
}
