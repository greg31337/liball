using System;
using System.IO;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifMetadata"/>.</summary>
public interface IAvifMetadata
{
    /// <summary>Returns the construction and validation result.</summary>
    /// <returns>The stable metadata result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the interpreted metadata kind.</summary>
    /// <returns>The metadata kind.</returns>
    AvifMetadataKind GetKind();

    /// <summary>Returns the four-character item type.</summary>
    /// <returns>The item type, or an empty string after failure.</returns>
    string? GetItemType();

    /// <summary>Returns the MIME content type.</summary>
    /// <returns>The MIME type, or an empty string for non-MIME metadata.</returns>
    string? GetContentType();

    /// <summary>Returns the MIME content encoding.</summary>
    /// <returns>The content encoding, or an empty string for non-MIME metadata.</returns>
    string? GetContentEncoding();

    /// <summary>Sets the optional MIME content encoding.</summary>
    /// <param name="value">The encoding token or an empty string.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetContentEncoding(string value);

    /// <summary>Returns the URI metadata item type identifier.</summary>
    /// <returns>The URI identifier, or an empty string for non-URI metadata.</returns>
    string? GetUriType();

    /// <summary>Sets the required URI identifier for a <c>uri </c> metadata item.</summary>
    /// <param name="value">The nonempty URI identifier.</param>
    /// <returns>Success or InvalidArgument.</returns>
    AvifResultCode SetUriType(string value);

    /// <summary>Returns an independent payload copy.</summary>
    /// <returns>The metadata bytes, or an empty array after failure.</returns>
    byte[] GetData();

    /// <summary>Returns the metadata payload byte count without copying it.</summary>
    /// <returns>The payload length, or zero after failure.</returns>
    int GetDataLength();

    /// <summary>Copies the metadata payload into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer, which may be larger than the payload.</param>
    /// <returns>Success, BufferTooSmall, or the metadata failure code.</returns>
    AvifResultCode CopyDataTo(Span<byte> destination);

    /// <summary>Writes the metadata payload without closing the destination stream.</summary>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, InvalidArgument, or the metadata failure code.</returns>
    AvifResultCode WriteDataTo(Stream destination);
}
