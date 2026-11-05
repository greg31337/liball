using System;
using System.IO;

namespace geo;

/// <summary>Reads and writes well-known text geometry representations.</summary>
public interface IWktCodec
{
    /// <summary>Reads a geometry from a well-known text string.</summary>
    /// <param name="text">The well-known text input.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The input is not valid well-known text.</exception>
    Geometry ReadWkt(string text);

    /// <summary>Attempts to read a geometry without throwing for malformed input.</summary>
    /// <param name="text">The well-known text input.</param>
    /// <returns>The structured parsing result.</returns>
    GeometryReadResult TryReadWkt(string text);

    /// <summary>Reads a geometry from a character span.</summary>
    /// <param name="characters">The well-known text characters.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The input is not valid well-known text.</exception>
    Geometry ReadWktCharacters(ReadOnlySpan<char> characters);

    /// <summary>Writes a geometry to a canonical well-known text string.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <returns>The canonical well-known text.</returns>
    string WriteWkt(Geometry geometry);

    /// <summary>Attempts to write a geometry into a caller-provided character span.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <param name="destination">The destination character span.</param>
    /// <returns>The structured writing result.</returns>
    WriteResult TryWriteWktCharacters(Geometry geometry, Span<char> destination);

    /// <summary>Writes a geometry to a text writer without closing it.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <param name="writer">The destination text writer.</param>
    void WriteWktToTextWriter(Geometry geometry, TextWriter writer);
}
