using System;
using System.IO;

namespace geo;

/// <summary>Reads and writes RFC 7946 GeoJSON documents.</summary>
public interface IGeoJsonCodec
{
    /// <summary>Reads a GeoJSON document from text.</summary>
    /// <param name="json">The GeoJSON text.</param>
    /// <returns>The parsed GeoJSON document.</returns>
    /// <exception cref="GeoJsonException">The input is not valid GeoJSON.</exception>
    GeoJsonDocument ReadGeoJson(string json);

    /// <summary>Attempts to read a GeoJSON document without throwing for malformed input.</summary>
    /// <param name="json">The GeoJSON text.</param>
    /// <returns>The structured parsing result.</returns>
    GeoJsonReadResult TryReadGeoJson(string json);

    /// <summary>Reads a GeoJSON document from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 GeoJSON input.</param>
    /// <returns>The parsed GeoJSON document.</returns>
    /// <exception cref="GeoJsonException">The input is not valid UTF-8 GeoJSON.</exception>
    GeoJsonDocument ReadGeoJsonUtf8(ReadOnlySpan<byte> utf8);

    /// <summary>Reads a GeoJSON document from a stream without closing it.</summary>
    /// <param name="stream">The readable UTF-8 input stream.</param>
    /// <returns>The parsed GeoJSON document.</returns>
    /// <exception cref="GeoJsonException">The stream or its content is not valid GeoJSON.</exception>
    GeoJsonDocument ReadGeoJsonStream(Stream stream);

    /// <summary>Writes a GeoJSON document to text.</summary>
    /// <param name="document">The document to write.</param>
    /// <returns>The formatted GeoJSON text.</returns>
    string WriteGeoJson(GeoJsonDocument document);

    /// <summary>Attempts to write a GeoJSON document into a caller-provided UTF-8 span.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="destination">The UTF-8 destination span.</param>
    /// <returns>The structured writing result, including the required size.</returns>
    WriteResult TryWriteGeoJsonUtf8(GeoJsonDocument document, Span<byte> destination);

    /// <summary>Writes a GeoJSON document to a stream without closing it.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="stream">The writable UTF-8 destination stream.</param>
    void WriteGeoJsonStream(GeoJsonDocument document, Stream stream);
}
