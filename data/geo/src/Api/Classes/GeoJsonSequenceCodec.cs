using System;
using System.IO;

namespace geo;

/// <summary>Provides bounded RFC 8142 GeoJSON text sequence input and output.</summary>
public class GeoJsonSequenceCodec : IGeoJsonSequenceCodec
{
    internal GeoJsonReadOptions ReadOptions;
    internal GeoJsonWriteOptions WriteOptions;

    /// <summary>Creates a sequence codec that snapshots the supplied read and write configuration.</summary>
    /// <param name="readOptions">The per-record bounded parsing configuration.</param>
    /// <param name="writeOptions">The per-record formatting configuration.</param>
    public GeoJsonSequenceCodec(GeoJsonReadOptions readOptions, GeoJsonWriteOptions writeOptions)
    {
        ReadOptions = GeoJsonCodec.CopyAndValidateReadOptions(readOptions);
        WriteOptions = GeoJsonFormatter.CopyAndValidateOptions(writeOptions);
    }

    /// <summary>Reads every framed record and reports malformed records without stopping recovery.</summary>
    /// <param name="stream">The readable UTF-8 sequence stream.</param>
    /// <returns>One parsing result for each record encountered.</returns>
    public GeoJsonReadResult[] ReadGeoJsonSequence(Stream stream)
    {
        return new GeoJsonSequenceReader(stream, ReadOptions).ReadResults();
    }

    /// <summary>Writes documents as record-separator-prefixed, line-feed-terminated records.</summary>
    /// <param name="documents">The documents to write in sequence order.</param>
    /// <param name="stream">The writable UTF-8 destination stream.</param>
    public void WriteGeoJsonSequence(GeoJsonDocument[] documents, Stream stream)
    {
        new GeoJsonSequenceWriter(WriteOptions).WriteDocuments(documents, stream);
    }

    /// <summary>Attempts to read one complete RFC 8142 framed record.</summary>
    /// <param name="record">A record including its record separator and line-feed terminator.</param>
    /// <returns>The structured parsing result.</returns>
    public GeoJsonReadResult TryReadGeoJsonRecord(ReadOnlySpan<byte> record)
    {
        return GeoJsonSequenceReader.ParseFramedRecord(record, ReadOptions);
    }
}
