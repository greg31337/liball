using System;
using System.Collections.Generic;
using System.IO;

namespace geo;

internal class GeoJsonSequenceReader
{
    internal const byte Separator = 0x1e;
    internal const byte Terminator = 0x0a;

    internal Stream Source;
    internal GeoJsonReadOptions Options;
    internal MemoryStream Payload = new MemoryStream();
    internal bool InRecord;
    internal bool HasLastByte;
    internal byte LastByte;
    internal bool PayloadTooLarge;
    internal long PreambleLength;
    internal int RecordIndex;

    internal GeoJsonSequenceReader(Stream source, GeoJsonReadOptions options)
    {
        if (source == null) throw new GeoJsonException("GeoJSON sequence input stream is null.");
        if (!source.CanRead) throw new GeoJsonException("GeoJSON sequence input stream is not readable.");
        Source = source;
        Options = options;
    }

    internal GeoJsonReadResult[] ReadResults()
    {
        List<GeoJsonReadResult> results = new List<GeoJsonReadResult>();
        byte[] buffer = new byte[8192];
        int read = Source.Read(buffer, 0, buffer.Length);
        while (read != 0)
        {
            int index = 0;
            while (index < read)
            {
                ProcessByte(buffer[index], results);
                index++;
            }
            read = Source.Read(buffer, 0, buffer.Length);
        }

        if (InRecord)
        {
            results.Add(FinishRecord());
        }
        else if (PreambleLength != 0)
        {
            results.Add(CreateFailure(
                ParseErrorKind.UnexpectedToken, 0, ClampConsumed(PreambleLength),
                "GeoJSON text sequence content must begin with a record separator at byte offset 0.",
                "SequencePreamble"));
        }
        Payload.Dispose();
        return results.ToArray();
    }

    internal void ProcessByte(byte value, List<GeoJsonReadResult> results)
    {
        if (value == Separator)
        {
            if (InRecord)
            {
                results.Add(FinishRecord());
            }
            else if (PreambleLength != 0)
            {
                results.Add(CreateFailure(
                    ParseErrorKind.UnexpectedToken, 0, ClampConsumed(PreambleLength),
                    "GeoJSON text sequence content must begin with a record separator at byte offset 0.",
                    "SequencePreamble"));
                PreambleLength = 0;
            }
            BeginRecord();
            return;
        }

        if (!InRecord)
        {
            PreambleLength++;
            return;
        }

        if (HasLastByte) StorePayloadByte(LastByte);
        LastByte = value;
        HasLastByte = true;
    }

    internal void BeginRecord()
    {
        Payload.SetLength(0);
        InRecord = true;
        HasLastByte = false;
        LastByte = 0;
        PayloadTooLarge = false;
    }

    internal GeoJsonReadResult FinishRecord()
    {
        string componentPath = "Record[" + RecordIndex + "]";
        RecordIndex++;
        InRecord = false;

        if (!HasLastByte || LastByte != Terminator)
        {
            if (HasLastByte) StorePayloadByte(LastByte);
            return CreateFailure(
                ParseErrorKind.UnexpectedEnd, Payload.Length, ClampConsumed(Payload.Length),
                "GeoJSON text sequence record is missing its line-feed terminator at byte offset " +
                Payload.Length + ".", componentPath);
        }
        if (PayloadTooLarge)
        {
            return CreateFailure(
                ParseErrorKind.LimitExceeded, Options.MaximumInputBytes, Options.MaximumInputBytes,
                "Maximum GeoJSON record size of " + Options.MaximumInputBytes +
                " bytes was exceeded at byte offset " + Options.MaximumInputBytes + ".", componentPath);
        }

        GeoJsonReadResult result = new GeoJsonParser(Payload.ToArray(), Options).ParseDocument();
        if (result.Error != null) result.Error.ComponentPath = componentPath;
        return result;
    }

    internal void StorePayloadByte(byte value)
    {
        if (Payload.Length < Options.MaximumInputBytes)
        {
            Payload.WriteByte(value);
        }
        else
        {
            PayloadTooLarge = true;
        }
    }

    internal static GeoJsonReadResult ParseFramedRecord(
        ReadOnlySpan<byte> record, GeoJsonReadOptions options)
    {
        if (record.Length == 0 || record[0] != Separator)
        {
            return CreateFailure(
                ParseErrorKind.UnexpectedToken, 0, 0,
                "GeoJSON text sequence record must begin with a record separator at byte offset 0.",
                "Record[0]");
        }

        int index = 1;
        while (index < record.Length)
        {
            if (record[index] == Separator)
            {
                return CreateFailure(
                    ParseErrorKind.UnexpectedToken, index, index,
                    "GeoJSON text sequence input contains more than one record at byte offset " + index + ".",
                    "Record[0]");
            }
            index++;
        }
        if (record.Length < 2 || record[record.Length - 1] != Terminator)
        {
            return CreateFailure(
                ParseErrorKind.UnexpectedEnd, record.Length, record.Length,
                "GeoJSON text sequence record is missing its line-feed terminator at byte offset " +
                record.Length + ".", "Record[0]");
        }

        int payloadLength = record.Length - 2;
        if (payloadLength > options.MaximumInputBytes)
        {
            return CreateFailure(
                ParseErrorKind.LimitExceeded, options.MaximumInputBytes, options.MaximumInputBytes,
                "Maximum GeoJSON record size of " + options.MaximumInputBytes +
                " bytes was exceeded at byte offset " + options.MaximumInputBytes + ".", "Record[0]");
        }

        byte[] payload = record.Slice(1, payloadLength).ToArray();
        GeoJsonReadResult result = new GeoJsonParser(payload, options).ParseDocument();
        if (result.Error != null) result.Error.ComponentPath = "Record[0]";
        return result;
    }

    internal static GeoJsonReadResult CreateFailure(
        ParseErrorKind kind, long offset, int consumed, string message, string componentPath)
    {
        ParseError error = new ParseError();
        error.Kind = kind;
        error.Offset = offset;
        error.JsonPath = "$";
        error.ComponentPath = componentPath;
        error.Message = message + " JSON path $. Geometry component path " + componentPath + ".";
        GeoJsonReadResult result = new GeoJsonReadResult();
        result.Error = error;
        result.Consumed = consumed;
        return result;
    }

    internal static int ClampConsumed(long value)
    {
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }
}
