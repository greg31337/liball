namespace image;

/// <summary>Defines convenient construction operations for public metadata data objects.</summary>
public interface IImageMetadataFactory
{
    /// <summary>Creates a geographic coordinate from EXIF-style components.</summary>
    /// <returns>A normalized coordinate.</returns>
    GpsCoordinate CreateGpsCoordinate(double latitudeDegrees, double latitudeMinutes, double latitudeSeconds, string? latitudeReference, double longitudeDegrees, double longitudeMinutes, double longitudeSeconds, string? longitudeReference, double altitude, string? altitudeReference);

    /// <summary>Creates an image format description.</summary>
    /// <returns>The initialized format description.</returns>
    ImageFormatInfo CreateFormatInfo(ImageFormatKind kind, string name, string mimeType, string preferredExtension);

    /// <summary>Creates reader options with the default limits.</summary>
    /// <returns>The default reader options.</returns>
    ImageMetadataReaderOptions CreateDefaultReaderOptions();

    /// <summary>Creates reader options with conservative limits.</summary>
    /// <returns>The conservative reader options.</returns>
    ImageMetadataReaderOptions CreateConservativeReaderOptions();

    /// <summary>Creates reader options that omit raw payload capture.</summary>
    /// <returns>The metadata-only reader options.</returns>
    ImageMetadataReaderOptions CreateMetadataOnlyReaderOptions();

    /// <summary>Creates a diagnostic.</summary>
    /// <returns>The initialized diagnostic.</returns>
    MetadataDiagnostic CreateDiagnostic(MetadataSeverity severity, string code, string message);

    /// <summary>Creates a metadata directory.</summary>
    /// <returns>The initialized directory.</returns>
    MetadataDirectory CreateDirectory(MetadataFamily family, string group, string name);

    /// <summary>Creates a metadata tag.</summary>
    /// <returns>The initialized tag.</returns>
    MetadataTag CreateTag(string key, string name, MetadataFamily family, MetadataValue value);

    /// <summary>Creates a signed integer metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateIntegerValue(long value);

    /// <summary>Creates an unsigned integer metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateUnsignedIntegerValue(ulong value);

    /// <summary>Creates a rational metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateRationalValue(long numerator, long denominator);

    /// <summary>Creates a signed rational metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateSignedRationalValue(long numerator, long denominator);

    /// <summary>Creates a double metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateDoubleValue(double value);

    /// <summary>Creates a signed integer-array metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateIntegerArrayValue(System.ReadOnlySpan<long> values);

    /// <summary>Creates an unsigned integer-array metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateUnsignedIntegerArrayValue(System.ReadOnlySpan<ulong> values);

    /// <summary>Creates a rational-array metadata value by copying paired numerators and denominators.</summary>
    /// <exception cref="System.ArgumentException">The numerator and denominator counts differ.</exception>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateRationalArrayValue(System.ReadOnlySpan<long> numerators, System.ReadOnlySpan<long> denominators);

    /// <summary>Creates a signed rational-array metadata value by copying paired numerators and denominators.</summary>
    /// <exception cref="System.ArgumentException">The numerator and denominator counts differ.</exception>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateSignedRationalArrayValue(System.ReadOnlySpan<long> numerators, System.ReadOnlySpan<long> denominators);

    /// <summary>Creates a floating-point-array metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateDoubleArrayValue(System.ReadOnlySpan<double> values);

    /// <summary>Creates a string metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateStringValue(string value);

    /// <summary>Creates a string-list metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateStringListValue(System.ReadOnlySpan<string> values);

    /// <summary>Creates a date-text metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateDateTextValue(string value);

    /// <summary>Creates an XML-text metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateXmlTextValue(string value);

    /// <summary>Creates a byte-array metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateBytesValue(System.ReadOnlySpan<byte> value);

    /// <summary>Creates a directory-reference metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateDirectoryValue(MetadataDirectory value);

    /// <summary>Creates a Boolean metadata value.</summary>
    /// <returns>The initialized value.</returns>
    MetadataValue CreateBooleanValue(bool value);
}
