using System;
using System.IO;

namespace geo;

/// <summary>Reads and writes RFC 8142 GeoJSON text sequences.</summary>
public interface IGeoJsonSequenceCodec
{
    /// <summary>Reads every framed record and reports malformed records without stopping recovery.</summary>
    /// <param name="stream">The readable UTF-8 sequence stream.</param>
    /// <returns>One parsing result for each record encountered.</returns>
    GeoJsonReadResult[] ReadGeoJsonSequence(Stream stream);

    /// <summary>Writes documents as record-separator-prefixed, line-feed-terminated records.</summary>
    /// <param name="documents">The documents to write in sequence order.</param>
    /// <param name="stream">The writable UTF-8 destination stream.</param>
    void WriteGeoJsonSequence(GeoJsonDocument[] documents, Stream stream);

    /// <summary>Attempts to read one complete RFC 8142 framed record.</summary>
    /// <param name="record">A record including its record separator and line-feed terminator.</param>
    /// <returns>The structured parsing result.</returns>
    GeoJsonReadResult TryReadGeoJsonRecord(ReadOnlySpan<byte> record);
}
