using System;

namespace rtp;

/// <summary>
/// Defines RTP header-extension wire and element operations.
/// </summary>
public interface IRtpHeaderExtensionCodec
{
    /// <summary>Calculates the exact encoded extension-envelope size.</summary>
    /// <param name="extension">The extension to size.</param>
    /// <returns>The required destination size in bytes.</returns>
    int GetRtpHeaderExtensionEncodedSize(RtpHeaderExtension extension);

    /// <summary>Encodes one RTP header extension or throws for invalid input or capacity.</summary>
    /// <param name="extension">The extension to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    int EncodeRtpHeaderExtension(RtpHeaderExtension extension, Span<byte> destination);

    /// <summary>Attempts to encode one RTP header extension without expected exceptions.</summary>
    /// <param name="extension">The extension to encode.</param>
    /// <param name="destination">The destination receiving the encoded bytes.</param>
    /// <returns>The encode outcome and written count.</returns>
    RtpCodecResult TryEncodeRtpHeaderExtension(
        RtpHeaderExtension extension,
        Span<byte> destination);

    /// <summary>Decodes one RTP header extension or throws for malformed input.</summary>
    /// <param name="source">The extension-envelope bytes to decode.</param>
    /// <returns>An owned header-extension model.</returns>
    RtpHeaderExtension DecodeRtpHeaderExtension(ReadOnlySpan<byte> source);

    /// <summary>Attempts to decode one RTP header extension without expected exceptions.</summary>
    /// <param name="source">The extension-envelope bytes to decode.</param>
    /// <returns>The decode outcome and consumed count.</returns>
    RtpCodecResult TryDecodeRtpHeaderExtension(ReadOnlySpan<byte> source);

    /// <summary>Adds an element and returns an independently owned extension.</summary>
    /// <param name="extension">The extension to copy and update.</param>
    /// <param name="element">The element to append.</param>
    /// <returns>The updated extension copy.</returns>
    RtpHeaderExtension AddRtpHeaderExtensionElement(
        RtpHeaderExtension extension,
        RtpHeaderExtensionElement element);

    /// <summary>Replaces an identified element and returns an independently owned extension.</summary>
    /// <param name="extension">The extension to copy and update.</param>
    /// <param name="element">The replacement element.</param>
    /// <returns>The updated extension copy.</returns>
    RtpHeaderExtension ReplaceRtpHeaderExtensionElement(
        RtpHeaderExtension extension,
        RtpHeaderExtensionElement element);

    /// <summary>Removes an identified element and returns an independently owned extension.</summary>
    /// <param name="extension">The extension to copy and update.</param>
    /// <param name="identifier">The element identifier to remove.</param>
    /// <returns>The updated extension copy.</returns>
    RtpHeaderExtension RemoveRtpHeaderExtensionElement(
        RtpHeaderExtension extension,
        int identifier);

    /// <summary>Finds an element by identifier.</summary>
    /// <param name="extension">The extension to search.</param>
    /// <param name="identifier">The element identifier.</param>
    /// <returns>An owned element copy, or <c>null</c> when no element exists.</returns>
    RtpHeaderExtensionElement? FindRtpHeaderExtensionElement(
        RtpHeaderExtension extension,
        int identifier);
}
