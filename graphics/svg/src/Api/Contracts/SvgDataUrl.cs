namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgDataUrl"/>.</summary>
public interface ISvgDataUrl
{
    /// <summary>Parses a data URL using the secure default decoded-byte limit.</summary>
    /// <param name="value">The complete data URL.</param>
    /// <returns>The decoded payload and media metadata.</returns>
    SvgDataUrlResult ParseWithValue(string value);
    /// <summary>Parses a data URL using resource security limits.</summary>
    /// <param name="value">The complete data URL.</param>
    /// <param name="limits">The configured resource limits.</param>
    /// <returns>The decoded payload and media metadata.</returns>
    SvgDataUrlResult ParseWithValueAndLimits(string value, SvgResourceLimits? limits);
    /// <summary>Parses a data URL with an explicit maximum decoded payload size.</summary>
    /// <param name="value">The complete data URL.</param>
    /// <param name="maxDecodedBytes">The maximum decoded byte count.</param>
    /// <returns>The decoded payload and media metadata, or a stable failure.</returns>
    SvgDataUrlResult ParseWithValueAndMaxDecodedBytes(string? value, long maxDecodedBytes);
    /// <summary>Encodes bytes as a data URL using the secure default decoded-byte limit.</summary>
    /// <param name="bytes">The payload bytes.</param>
    /// <param name="mediaType">A complete media type, or an empty string for the RFC default.</param>
    /// <param name="useBase64">Whether to use base64 instead of percent encoding.</param>
    /// <returns>The encoded data URL or a stable failure.</returns>
    SvgDataUrlEncodeResult EncodeWithBytesAndMediaTypeAndUseBase64(byte[] bytes, string mediaType, bool useBase64);
    /// <summary>Encodes bytes as a data URL using resource security limits.</summary>
    /// <param name="bytes">The payload bytes.</param>
    /// <param name="mediaType">A complete media type, or an empty string for the RFC default.</param>
    /// <param name="useBase64">Whether to use base64 instead of percent encoding.</param>
    /// <param name="limits">The configured resource limits.</param>
    /// <returns>The encoded data URL or a stable failure.</returns>
    SvgDataUrlEncodeResult EncodeWithBytesAndMediaTypeAndUseBase64AndLimits(byte[] bytes, string mediaType, bool useBase64, SvgResourceLimits? limits);
    /// <summary>Encodes bytes as a data URL with an explicit decoded-byte limit.</summary>
    /// <param name="bytes">The payload bytes.</param>
    /// <param name="mediaType">A complete media type, or an empty string for the RFC default.</param>
    /// <param name="useBase64">Whether to use base64 instead of percent encoding.</param>
    /// <param name="maxDecodedBytes">The maximum payload byte count.</param>
    /// <returns>The encoded data URL or a stable failure.</returns>
    SvgDataUrlEncodeResult EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(byte[]? bytes, string? mediaType, bool useBase64, long maxDecodedBytes);
    /// <summary>Encodes UTF-8 text as a data URL with an explicit media type.</summary>
    /// <param name="text">The Unicode text.</param>
    /// <param name="mediaType">A complete text media type.</param>
    /// <param name="useBase64">Whether to use base64 instead of percent encoding.</param>
    /// <returns>The encoded data URL or a stable failure.</returns>
    SvgDataUrlEncodeResult EncodeText(string? text, string mediaType, bool useBase64);
}
