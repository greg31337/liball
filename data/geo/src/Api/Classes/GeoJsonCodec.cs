using System;
using System.IO;

namespace geo;

/// <summary>Reads bounded GeoJSON and writes configured RFC 7946 GeoJSON.</summary>
public class GeoJsonCodec : IGeoJsonCodec
{
    internal GeoJsonReadOptions ReadOptions;
    internal GeoJsonWriteOptions WriteOptions;

    /// <summary>Creates a codec that snapshots the supplied read and write configuration.</summary>
    /// <param name="readOptions">The bounded parsing configuration.</param>
    /// <param name="writeOptions">The formatting configuration.</param>
    public GeoJsonCodec(GeoJsonReadOptions readOptions, GeoJsonWriteOptions writeOptions)
    {
        ReadOptions = CopyAndValidateReadOptions(readOptions);
        WriteOptions = GeoJsonFormatter.CopyAndValidateOptions(writeOptions);
    }

    /// <summary>Reads a GeoJSON document from text.</summary>
    /// <param name="json">The GeoJSON text.</param>
    /// <returns>The parsed GeoJSON document.</returns>
    /// <exception cref="GeoJsonException">The input is not valid GeoJSON.</exception>
    public GeoJsonDocument ReadGeoJson(string json)
    {
        GeoJsonTextEncodingResult encoded = EncodeText(json, ReadOptions.MaximumInputBytes);
        if (encoded.Failure != null) throw new GeoJsonException(encoded.Failure.Message);
        GeoJsonReadResult result = new GeoJsonParser(encoded.Bytes, ReadOptions).ParseDocument();
        return GetSuccessfulDocument(result);
    }

    /// <summary>Attempts to read a GeoJSON document without throwing for malformed input.</summary>
    /// <param name="json">The GeoJSON text.</param>
    /// <returns>The structured parsing result.</returns>
    public GeoJsonReadResult TryReadGeoJson(string json)
    {
        GeoJsonTextEncodingResult encoded = EncodeText(json, ReadOptions.MaximumInputBytes);
        if (encoded.Failure != null)
        {
            GeoJsonReadResult failure = new GeoJsonReadResult();
            failure.Error = encoded.Failure;
            failure.Consumed = (int)encoded.Failure.Offset;
            return failure;
        }
        return new GeoJsonParser(encoded.Bytes, ReadOptions).ParseDocument();
    }

    /// <summary>Reads a GeoJSON document from UTF-8 bytes.</summary>
    /// <param name="utf8">The UTF-8 GeoJSON input.</param>
    /// <returns>The parsed GeoJSON document.</returns>
    /// <exception cref="GeoJsonException">The input is not valid UTF-8 GeoJSON.</exception>
    public GeoJsonDocument ReadGeoJsonUtf8(ReadOnlySpan<byte> utf8)
    {
        GeoJsonReadResult result = new GeoJsonParser(utf8.ToArray(), ReadOptions).ParseDocument();
        return GetSuccessfulDocument(result);
    }

    /// <summary>Reads a GeoJSON document from a stream without closing it.</summary>
    /// <param name="stream">The readable UTF-8 input stream.</param>
    /// <returns>The parsed GeoJSON document.</returns>
    /// <exception cref="GeoJsonException">The stream or its content is not valid GeoJSON.</exception>
    public GeoJsonDocument ReadGeoJsonStream(Stream stream)
    {
        byte[] bytes = ReadBoundedStream(stream);
        GeoJsonReadResult result = new GeoJsonParser(bytes, ReadOptions).ParseDocument();
        return GetSuccessfulDocument(result);
    }

    /// <summary>Writes a GeoJSON document to text.</summary>
    /// <param name="document">The document to write.</param>
    /// <returns>The formatted GeoJSON text.</returns>
    public string WriteGeoJson(GeoJsonDocument document)
    {
        return new GeoJsonFormatter(document, WriteOptions).FormatString();
    }

    /// <summary>Attempts to write a GeoJSON document into a caller-provided UTF-8 span.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="destination">The UTF-8 destination span.</param>
    /// <returns>The structured writing result, including the required size.</returns>
    public WriteResult TryWriteGeoJsonUtf8(GeoJsonDocument document, Span<byte> destination)
    {
        return new GeoJsonFormatter(document, WriteOptions).TryWriteUtf8Bytes(destination);
    }

    /// <summary>Writes a GeoJSON document to a stream without closing it.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="stream">The writable UTF-8 destination stream.</param>
    public void WriteGeoJsonStream(GeoJsonDocument document, Stream stream)
    {
        new GeoJsonFormatter(document, WriteOptions).WriteToStream(stream);
    }

    internal GeoJsonDocument GetSuccessfulDocument(GeoJsonReadResult result)
    {
        if (result.Success && result.Document != null) return result.Document;
        if (result.Error != null) throw new GeoJsonException(result.Error.Message);
        throw new GeoJsonException("GeoJSON parsing failed without an error description.");
    }

    internal byte[] ReadBoundedStream(Stream stream)
    {
        if (stream == null) throw new GeoJsonException("GeoJSON input stream is null.");
        if (!stream.CanRead) throw new GeoJsonException("GeoJSON input stream is not readable.");
        using (MemoryStream memory = new MemoryStream())
        {
            byte[] buffer = new byte[8192];
            int total = 0;
            int read = stream.Read(buffer, 0, buffer.Length);
            while (read != 0)
            {
                if (read > ReadOptions.MaximumInputBytes - total)
                {
                    throw new GeoJsonException(
                        $"Maximum GeoJSON input size of {ReadOptions.MaximumInputBytes} bytes was exceeded at byte offset {ReadOptions.MaximumInputBytes}.");
                }
                memory.Write(buffer, 0, read);
                total += read;
                read = stream.Read(buffer, 0, buffer.Length);
            }
            return memory.ToArray();
        }
    }

    internal static GeoJsonTextEncodingResult EncodeText(string text, int maximumBytes)
    {
        GeoJsonTextEncodingResult result = new GeoJsonTextEncodingResult();
        if (text == null)
        {
            result.Failure = CreateEncodingFailure(
                ParseErrorKind.UnexpectedEnd, 0, "GeoJSON text input is null at byte offset 0.");
            return result;
        }

        int byteCount = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            int increment = 3;
            if (character < 0x80) increment = 1;
            else if (character < 0x800) increment = 2;
            else if (character >= 0xd800 && character <= 0xdbff)
            {
                if (index + 1 >= text.Length || text[index + 1] < 0xdc00 || text[index + 1] > 0xdfff)
                {
                    result.Failure = CreateEncodingFailure(ParseErrorKind.UnexpectedToken, byteCount,
                        $"GeoJSON text contains an unpaired high surrogate at character offset {index}.");
                    return result;
                }
                increment = 4;
                index++;
            }
            else if (character >= 0xdc00 && character <= 0xdfff)
            {
                result.Failure = CreateEncodingFailure(ParseErrorKind.UnexpectedToken, byteCount,
                    $"GeoJSON text contains an unpaired low surrogate at character offset {index}.");
                return result;
            }
            if (increment > maximumBytes - byteCount)
            {
                result.Failure = CreateEncodingFailure(ParseErrorKind.LimitExceeded, maximumBytes,
                    $"Maximum GeoJSON input size of {maximumBytes} bytes was exceeded at byte offset {maximumBytes}.");
                return result;
            }
            byteCount += increment;
            index++;
        }

        byte[] bytes = new byte[byteCount];
        int destination = 0;
        index = 0;
        while (index < text.Length)
        {
            int value = text[index];
            if (value < 0x80)
            {
                bytes[destination] = (byte)value;
                destination++;
            }
            else if (value < 0x800)
            {
                bytes[destination] = (byte)(0xc0 | value >> 6);
                bytes[destination + 1] = (byte)(0x80 | value & 0x3f);
                destination += 2;
            }
            else if (value >= 0xd800 && value <= 0xdbff)
            {
                int low = text[index + 1];
                int scalar = 0x10000 + ((value - 0xd800) << 10) + low - 0xdc00;
                bytes[destination] = (byte)(0xf0 | scalar >> 18);
                bytes[destination + 1] = (byte)(0x80 | scalar >> 12 & 0x3f);
                bytes[destination + 2] = (byte)(0x80 | scalar >> 6 & 0x3f);
                bytes[destination + 3] = (byte)(0x80 | scalar & 0x3f);
                destination += 4;
                index++;
            }
            else
            {
                bytes[destination] = (byte)(0xe0 | value >> 12);
                bytes[destination + 1] = (byte)(0x80 | value >> 6 & 0x3f);
                bytes[destination + 2] = (byte)(0x80 | value & 0x3f);
                destination += 3;
            }
            index++;
        }
        result.Bytes = bytes;
        return result;
    }

    internal static ParseError CreateEncodingFailure(
        ParseErrorKind kind, int offset, string message)
    {
        ParseError error = new ParseError();
        error.Kind = kind;
        error.Offset = offset;
        error.JsonPath = "$";
        error.Message = message + " JSON path $.";
        return error;
    }

    internal static GeoJsonReadOptions CopyAndValidateReadOptions(GeoJsonReadOptions source)
    {
        if (source == null) throw new GeoJsonException("GeoJSON read options are null.");
        if (source.MaximumCoordinates < 1)
        {
            throw new GeoJsonException("Maximum GeoJSON coordinate count must be positive.");
        }
        if (source.MaximumComponents < 1)
        {
            throw new GeoJsonException("Maximum GeoJSON component count must be positive.");
        }
        if (source.MaximumNestingDepth < 0)
        {
            throw new GeoJsonException("Maximum GeoJSON nesting depth cannot be negative.");
        }
        if (source.MaximumInputBytes < 1)
        {
            throw new GeoJsonException("Maximum GeoJSON input size must be positive.");
        }
        return GeoJsonParser.CopyReadOptions(source);
    }
}
