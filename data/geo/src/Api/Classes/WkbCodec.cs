using System;
using System.IO;

namespace geo;

/// <summary>Reads bounded well-known binary and writes configured well-known binary.</summary>
public class WkbCodec : IWkbCodec
{
    internal WkbReadOptions ReadOptions;
    internal WkbWriteOptions WriteOptions;

    /// <summary>Creates a codec that snapshots the supplied read and write configuration.</summary>
    /// <param name="readOptions">The bounded parsing configuration.</param>
    /// <param name="writeOptions">The formatting configuration.</param>
    public WkbCodec(WkbReadOptions readOptions, WkbWriteOptions writeOptions)
    {
        ReadOptions = CopyAndValidateReadOptions(readOptions);
        WriteOptions = WkbFormatter.CopyAndValidateOptions(writeOptions);
    }

    /// <summary>Reads a geometry from well-known binary bytes.</summary>
    /// <param name="bytes">The well-known binary input.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The input is not valid well-known binary.</exception>
    public Geometry ReadWkb(ReadOnlySpan<byte> bytes)
    {
        GeometryReadResult result = new WkbParser(bytes.ToArray(), ReadOptions).ParseDocument();
        return GetSuccessfulGeometry(result);
    }

    /// <summary>Attempts to read a geometry without throwing for malformed input.</summary>
    /// <param name="bytes">The well-known binary input.</param>
    /// <returns>The structured parsing result.</returns>
    public GeometryReadResult TryReadWkb(ReadOnlySpan<byte> bytes)
    {
        return new WkbParser(bytes.ToArray(), ReadOptions).ParseDocument();
    }

    /// <summary>Reads a geometry from a binary stream without closing it.</summary>
    /// <param name="stream">The readable well-known binary stream.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The stream or its content is not valid well-known binary.</exception>
    public Geometry ReadWkbStream(Stream stream)
    {
        byte[] bytes = ReadBoundedStream(stream);
        GeometryReadResult result = new WkbParser(bytes, ReadOptions).ParseDocument();
        return GetSuccessfulGeometry(result);
    }

    /// <summary>Reads a geometry from hexadecimal well-known binary text.</summary>
    /// <param name="hex">The hexadecimal well-known binary input.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The input is not valid hexadecimal well-known binary.</exception>
    public Geometry ReadWkbHex(string hex)
    {
        byte[] bytes = DecodeHexText(hex, ReadOptions.MaximumInputBytes);
        GeometryReadResult result = new WkbParser(bytes, ReadOptions).ParseDocument();
        return GetSuccessfulGeometry(result);
    }

    /// <summary>Gets the exact configured well-known binary output size.</summary>
    /// <param name="geometry">The geometry to size.</param>
    /// <returns>The required number of output bytes.</returns>
    public int GetWkbSize(Geometry geometry)
    {
        return WkbSizer.GetConfiguredSize(geometry, WriteOptions);
    }

    /// <summary>Writes a geometry to an owning well-known binary byte array.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <returns>The encoded well-known binary bytes.</returns>
    public byte[] WriteWkb(Geometry geometry)
    {
        return new WkbFormatter(geometry, WriteOptions).FormatBytes();
    }

    /// <summary>Attempts to write a geometry into a caller-provided byte span.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <returns>The structured writing result, including the required size.</returns>
    public WriteResult TryWriteWkbBytes(Geometry geometry, Span<byte> destination)
    {
        return new WkbFormatter(geometry, WriteOptions).TryWriteBytes(destination);
    }

    /// <summary>Writes a geometry to a binary stream without closing it.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <param name="stream">The writable destination stream.</param>
    public void WriteWkbStream(Geometry geometry, Stream stream)
    {
        new WkbFormatter(geometry, WriteOptions).WriteToStream(stream);
    }

    /// <summary>Writes a geometry to uppercase hexadecimal well-known binary text.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <returns>The hexadecimal well-known binary text.</returns>
    public string WriteWkbHex(Geometry geometry)
    {
        return new WkbFormatter(geometry, WriteOptions).FormatHex();
    }

    internal Geometry GetSuccessfulGeometry(GeometryReadResult result)
    {
        if (result.Success && result.Geometry != null) return result.Geometry;
        if (result.Error != null) throw new GeometryParseException(result.Error.Message);
        throw new GeometryParseException("WKB parsing failed without an error description.");
    }

    internal byte[] ReadBoundedStream(Stream stream)
    {
        if (stream == null) throw new GeometryParseException("WKB input stream is null.");
        if (!stream.CanRead) throw new GeometryParseException("WKB input stream is not readable.");

        using (MemoryStream memory = new MemoryStream())
        {
            byte[] buffer = new byte[8192];
            int total = 0;
            int read = stream.Read(buffer, 0, buffer.Length);
            while (read != 0)
            {
                if (read > ReadOptions.MaximumInputBytes - total)
                {
                    throw new GeometryParseException(
                        $"Maximum WKB input size of {ReadOptions.MaximumInputBytes} bytes was exceeded at byte offset {ReadOptions.MaximumInputBytes}.");
                }
                memory.Write(buffer, 0, read);
                total += read;
                read = stream.Read(buffer, 0, buffer.Length);
            }
            return memory.ToArray();
        }
    }

    internal static byte[] DecodeHexText(string hex, int maximumInputBytes)
    {
        if (hex == null) throw new GeometryParseException("WKB hexadecimal input is null at character offset 0.");
        if (hex.Length % 2 != 0)
        {
            throw new GeometryParseException(
                $"WKB hexadecimal input has an incomplete byte at character offset {hex.Length - 1}.");
        }
        int byteCount = hex.Length / 2;
        if (byteCount > maximumInputBytes)
        {
            throw new GeometryParseException(
                $"Maximum WKB input size of {maximumInputBytes} bytes was exceeded at character offset {maximumInputBytes * 2}.");
        }

        byte[] bytes = new byte[byteCount];
        int index = 0;
        while (index < byteCount)
        {
            int characterOffset = index * 2;
            int high = GetHexNibble(hex[characterOffset]);
            int low = GetHexNibble(hex[characterOffset + 1]);
            if (high < 0)
            {
                throw new GeometryParseException(
                    $"WKB hexadecimal digit is invalid at character offset {characterOffset}.");
            }
            if (low < 0)
            {
                throw new GeometryParseException(
                    $"WKB hexadecimal digit is invalid at character offset {characterOffset + 1}.");
            }
            bytes[index] = (byte)(high * 16 + low);
            index++;
        }
        return bytes;
    }

    internal static int GetHexNibble(char character)
    {
        if (character >= '0' && character <= '9') return character - '0';
        if (character >= 'A' && character <= 'F') return character - 'A' + 10;
        if (character >= 'a' && character <= 'f') return character - 'a' + 10;
        return -1;
    }

    internal static WkbReadOptions CopyAndValidateReadOptions(WkbReadOptions source)
    {
        if (source == null) throw new InvalidGeometryException("WKB read options are null.");
        if (source.Flavor != WkbFlavor.Ogc && source.Flavor != WkbFlavor.Ewkb)
        {
            throw new InvalidGeometryException("WKB read flavor must be OGC or EWKB.");
        }
        if (source.MaximumCoordinates < 1)
        {
            throw new InvalidGeometryException("Maximum WKB coordinate count must be positive.");
        }
        if (source.MaximumComponents < 1)
        {
            throw new InvalidGeometryException("Maximum WKB component count must be positive.");
        }
        if (source.MaximumNestingDepth < 0)
        {
            throw new InvalidGeometryException("Maximum WKB nesting depth cannot be negative.");
        }
        if (source.MaximumInputBytes < 1)
        {
            throw new InvalidGeometryException("Maximum WKB input size must be positive.");
        }

        WkbReadOptions copy = new WkbReadOptions();
        copy.Flavor = source.Flavor;
        copy.MaximumCoordinates = source.MaximumCoordinates;
        copy.MaximumComponents = source.MaximumComponents;
        copy.MaximumNestingDepth = source.MaximumNestingDepth;
        copy.MaximumInputBytes = source.MaximumInputBytes;
        copy.RequireExactLength = source.RequireExactLength;
        return copy;
    }
}
