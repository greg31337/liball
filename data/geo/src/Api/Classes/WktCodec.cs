using System;
using System.IO;

namespace geo;

/// <summary>Reads bounded well-known text and writes canonical well-known text.</summary>
public class WktCodec : IWktCodec
{
    internal WktReadOptions ReadOptions;
    internal WktWriteOptions WriteOptions;

    /// <summary>Creates a codec that snapshots the supplied read and write configuration.</summary>
    /// <param name="readOptions">The bounded parsing configuration.</param>
    /// <param name="writeOptions">The formatting configuration.</param>
    public WktCodec(WktReadOptions readOptions, WktWriteOptions writeOptions)
    {
        ReadOptions = CopyAndValidateReadOptions(readOptions);
        WriteOptions = WktFormatter.CopyAndValidateOptions(writeOptions);
    }

    /// <summary>Reads a geometry from a well-known text string.</summary>
    /// <param name="text">The well-known text input.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The input is not valid well-known text.</exception>
    public Geometry ReadWkt(string text)
    {
        GeometryReadResult result = new WktParser(text, ReadOptions).ParseDocument();
        if (!result.Success || result.Geometry == null)
        {
            if (result.Error != null)
            {
                throw new GeometryParseException(result.Error.Message);
            }
            throw new GeometryParseException("WKT parsing failed without an error description.");
        }
        return result.Geometry;
    }

    /// <summary>Attempts to read a geometry without throwing for malformed input.</summary>
    /// <param name="text">The well-known text input.</param>
    /// <returns>The structured parsing result.</returns>
    public GeometryReadResult TryReadWkt(string text)
    {
        return new WktParser(text, ReadOptions).ParseDocument();
    }

    /// <summary>Reads a geometry from a character span.</summary>
    /// <param name="characters">The well-known text characters.</param>
    /// <returns>The parsed geometry.</returns>
    /// <exception cref="GeometryParseException">The input is not valid well-known text.</exception>
    public Geometry ReadWktCharacters(ReadOnlySpan<char> characters)
    {
        string text = characters.ToString();
        GeometryReadResult result = new WktParser(text, ReadOptions).ParseDocument();
        if (!result.Success || result.Geometry == null)
        {
            if (result.Error != null)
            {
                throw new GeometryParseException(result.Error.Message);
            }
            throw new GeometryParseException("WKT parsing failed without an error description.");
        }
        return result.Geometry;
    }

    /// <summary>Writes a geometry to a canonical well-known text string.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <returns>The canonical well-known text.</returns>
    public string WriteWkt(Geometry geometry)
    {
        WktFormatter formatter = new WktFormatter(geometry, WriteOptions);
        return formatter.FormatString();
    }

    /// <summary>Attempts to write a geometry into a caller-provided character span.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <param name="destination">The destination character span.</param>
    /// <returns>The structured writing result.</returns>
    public WriteResult TryWriteWktCharacters(Geometry geometry, Span<char> destination)
    {
        WktFormatter formatter = new WktFormatter(geometry, WriteOptions);
        return formatter.TryWriteCharacters(destination);
    }

    /// <summary>Writes a geometry to a text writer without closing it.</summary>
    /// <param name="geometry">The geometry to write.</param>
    /// <param name="writer">The destination text writer.</param>
    public void WriteWktToTextWriter(Geometry geometry, TextWriter writer)
    {
        WktFormatter formatter = new WktFormatter(geometry, WriteOptions);
        formatter.WriteToTextWriter(writer);
    }

    internal static WktReadOptions CopyAndValidateReadOptions(WktReadOptions source)
    {
        if (source == null)
        {
            throw new InvalidGeometryException("WKT read options are null.");
        }
        if (source.MaximumCoordinates < 1)
        {
            throw new InvalidGeometryException("Maximum WKT coordinate count must be positive.");
        }
        if (source.MaximumComponents < 1)
        {
            throw new InvalidGeometryException("Maximum WKT component count must be positive.");
        }
        if (source.MaximumNestingDepth < 0)
        {
            throw new InvalidGeometryException("Maximum WKT nesting depth cannot be negative.");
        }

        WktReadOptions options = new WktReadOptions();
        options.MaximumCoordinates = source.MaximumCoordinates;
        options.MaximumComponents = source.MaximumComponents;
        options.MaximumNestingDepth = source.MaximumNestingDepth;
        options.AllowExtendedTypes = source.AllowExtendedTypes;
        options.AllowSpatialReferenceId = source.AllowSpatialReferenceId;
        return options;
    }
}
