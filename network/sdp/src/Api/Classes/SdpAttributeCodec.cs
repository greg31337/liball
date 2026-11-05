using System;

namespace sdp;

/// <summary>
/// Converts generic SDP attributes to and from supported typed projections.
/// </summary>
public class SdpAttributeCodec : ISdpAttributeCodec
{
    /// <summary>
    /// Initializes an SDP attribute codec.
    /// </summary>
    public SdpAttributeCodec()
    {
    }

    /// <summary>Projects a generic attribute into typed data when its name is supported.</summary>
    /// <param name="attribute">The generic attribute to decode.</param>
    /// <returns>A result that retains the generic source and any typed projection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the attribute argument is null.</exception>
    public SdpAttributeDecodeResult DecodeAttribute(SdpAttribute attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException("attribute");
        }
        return SdpAttributeDecoderCore.DecodeAttribute(attribute);
    }

    /// <summary>Encodes supported typed attribute data into a generic attribute.</summary>
    /// <param name="typedAttribute">The typed attribute data to encode.</param>
    /// <returns>A result containing the generic attribute or a structured diagnostic.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the typed attribute argument is null.</exception>
    public SdpAttributeEncodeResult EncodeAttribute(object typedAttribute)
    {
        if (typedAttribute == null)
        {
            throw new ArgumentNullException("typedAttribute");
        }
        return SdpAttributeEncoderCore.EncodeAttribute(typedAttribute);
    }
}
