using System;

namespace rtp;

/// <summary>
/// Provides RTP header-extension envelope and element operations.
/// </summary>
public class RtpHeaderExtensionCodec : IRtpHeaderExtensionCodec
{
    /// <summary>
    /// Initializes an RTP header-extension codec.
    /// </summary>
    public RtpHeaderExtensionCodec()
    {
    }

    /// <summary>Calculates the exact encoded RFC 8285 extension-envelope size.</summary>
    /// <param name="extension">The extension to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    public int GetRtpHeaderExtensionEncodedSize(RtpHeaderExtension extension)
    {
        return RtpHeaderExtensionWireCodec.GetEnvelopeEncodedSize(extension);
    }

    /// <summary>Encodes one RFC 8285 RTP header extension.</summary>
    /// <param name="extension">The extension to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    public int EncodeRtpHeaderExtension(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        return RtpHeaderExtensionWireCodec.EncodeEnvelope(extension, destination);
    }

    /// <summary>Attempts to encode one RFC 8285 RTP header extension.</summary>
    /// <param name="extension">The extension to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The encode outcome and written count.</returns>
    public RtpCodecResult TryEncodeRtpHeaderExtension(
        RtpHeaderExtension extension,
        Span<byte> destination)
    {
        return RtpHeaderExtensionWireCodec.TryEncodeEnvelope(extension, destination);
    }

    /// <summary>Decodes one RFC 8285 RTP header extension.</summary>
    /// <param name="source">The extension-envelope bytes to decode.</param>
    /// <returns>An owned header-extension model.</returns>
    public RtpHeaderExtension DecodeRtpHeaderExtension(ReadOnlySpan<byte> source)
    {
        return RtpHeaderExtensionWireCodec.DecodeEnvelope(source);
    }

    /// <summary>Attempts to decode one RFC 8285 RTP header extension.</summary>
    /// <param name="source">The extension-envelope bytes to decode.</param>
    /// <returns>The decode outcome and consumed count.</returns>
    public RtpCodecResult TryDecodeRtpHeaderExtension(ReadOnlySpan<byte> source)
    {
        return RtpHeaderExtensionWireCodec.TryDecodeEnvelope(source);
    }

    /// <summary>Adds an element and returns an independently owned extension.</summary>
    /// <param name="extension">The extension to copy and update.</param>
    /// <param name="element">The element to append.</param>
    /// <returns>The updated extension copy in its smallest valid format.</returns>
    public RtpHeaderExtension AddRtpHeaderExtensionElement(
        RtpHeaderExtension extension,
        RtpHeaderExtensionElement element)
    {
        return RtpHeaderExtensionWireCodec.AddElementCopy(extension, element);
    }

    /// <summary>Replaces an identified element and returns an independently owned extension.</summary>
    /// <param name="extension">The extension to copy and update.</param>
    /// <param name="element">The replacement element.</param>
    /// <returns>The updated extension copy in its smallest valid format.</returns>
    public RtpHeaderExtension ReplaceRtpHeaderExtensionElement(
        RtpHeaderExtension extension,
        RtpHeaderExtensionElement element)
    {
        return RtpHeaderExtensionWireCodec.ReplaceElementCopy(extension, element);
    }

    /// <summary>Removes an identified element and returns an independently owned extension.</summary>
    /// <param name="extension">The extension to copy and update.</param>
    /// <param name="identifier">The element identifier to remove.</param>
    /// <returns>The updated extension copy in its smallest valid format.</returns>
    public RtpHeaderExtension RemoveRtpHeaderExtensionElement(
        RtpHeaderExtension extension,
        int identifier)
    {
        return RtpHeaderExtensionWireCodec.RemoveElementCopy(extension, identifier);
    }

    /// <summary>Finds an element by identifier.</summary>
    /// <param name="extension">The extension to search.</param>
    /// <param name="identifier">The element identifier.</param>
    /// <returns>An owned element copy, or <c>null</c> when no element exists.</returns>
    public RtpHeaderExtensionElement? FindRtpHeaderExtensionElement(
        RtpHeaderExtension extension,
        int identifier)
    {
        return RtpHeaderExtensionWireCodec.FindElementCopy(extension, identifier);
    }
}
