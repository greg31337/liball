using System;
using System.IO;

namespace geo;

/// <summary>Reads and writes well-known binary geometry representations.</summary>
public interface IWkbCodec
{
    /// <summary>Reads a geometry from well-known binary bytes.</summary>
    /// <param name="bytes">The well-known binary input.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The input is not valid well-known binary.</exception>
    Geometry ReadWkb(ReadOnlySpan<byte> bytes);

    /// <summary>Attempts to read a geometry without throwing for malformed input.</summary>
    /// <param name="bytes">The well-known binary input.</param>
    /// <returns>The structured parsing result.</returns>
    GeometryReadResult TryReadWkb(ReadOnlySpan<byte> bytes);

    /// <summary>Reads a geometry from a binary stream without closing it.</summary>
    /// <param name="stream">The readable well-known binary stream.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The stream or its content is not valid well-known binary.</exception>
    Geometry ReadWkbStream(Stream stream);

    /// <summary>Reads a geometry from hexadecimal well-known binary text.</summary>
    /// <param name="hex">The hexadecimal well-known binary input.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The input is not valid hexadecimal well-known binary.</exception>
    Geometry ReadWkbHex(string hex);

    /// <summary>Gets the exact configured well-known binary output size.</summary>
    /// <param name="geometry">The geometry to size.</param>
    /// <returns>The required number of output bytes.</returns>
    int GetWkbSize(Geometry geometry);

    /// <summary>Writes a geometry to an owning well-known binary byte array.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <returns>The encoded well-known binary bytes.</returns>
    byte[] WriteWkb(Geometry geometry);

    /// <summary>Attempts to write a geometry into a caller-provided byte span.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <returns>The structured writing result, including the required size.</returns>
    WriteResult TryWriteWkbBytes(Geometry geometry, Span<byte> destination);

    /// <summary>Writes a geometry to a binary stream without closing it.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <param name="stream">The writable destination stream.</param>
    void WriteWkbStream(Geometry geometry, Stream stream);

    /// <summary>Writes a geometry to uppercase hexadecimal well-known binary text.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <returns>The hexadecimal well-known binary text.</returns>
    string WriteWkbHex(Geometry geometry);
}
