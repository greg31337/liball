namespace image;

/// <summary>Creates commonly initialized public metadata data objects.</summary>
public class ImageMetadataFactory : IImageMetadataFactory
{
    /// <summary>Creates a stateless metadata data factory.</summary>
    public ImageMetadataFactory()
    {
    }

    /// <summary>Creates a geographic coordinate from EXIF-style components.</summary>
    /// <returns>A normalized coordinate.</returns>
    public GpsCoordinate CreateGpsCoordinate(double latitudeDegrees, double latitudeMinutes, double latitudeSeconds, string? latitudeReference, double longitudeDegrees, double longitudeMinutes, double longitudeSeconds, string? longitudeReference, double altitude, string? altitudeReference)
    {
        return GpsCoordinate.FromDegreesMinutesSeconds(latitudeDegrees, latitudeMinutes, latitudeSeconds, latitudeReference, longitudeDegrees, longitudeMinutes, longitudeSeconds, longitudeReference, altitude, altitudeReference);
    }

    /// <summary>Creates an image format description.</summary>
    /// <returns>The initialized format description.</returns>
    public ImageFormatInfo CreateFormatInfo(ImageFormatKind kind, string name, string mimeType, string preferredExtension)
    {
        return ImageFormatInfo.Create(kind, name, mimeType, preferredExtension);
    }

    /// <summary>Creates reader options with the default limits.</summary>
    /// <returns>The default reader options.</returns>
    public ImageMetadataReaderOptions CreateDefaultReaderOptions()
    {
        return ImageMetadataReaderOptions.CreateDefault();
    }

    /// <summary>Creates reader options with conservative limits.</summary>
    /// <returns>The conservative reader options.</returns>
    public ImageMetadataReaderOptions CreateConservativeReaderOptions()
    {
        return ImageMetadataReaderOptions.CreateConservative();
    }

    /// <summary>Creates reader options that omit raw payload capture.</summary>
    /// <returns>The metadata-only reader options.</returns>
    public ImageMetadataReaderOptions CreateMetadataOnlyReaderOptions()
    {
        return ImageMetadataReaderOptions.CreateMetadataOnly();
    }

    /// <summary>Creates a diagnostic.</summary>
    /// <returns>The initialized diagnostic.</returns>
    public MetadataDiagnostic CreateDiagnostic(MetadataSeverity severity, string code, string message)
    {
        return MetadataDiagnostic.Create(severity, code, message);
    }

    /// <summary>Creates a metadata directory.</summary>
    /// <returns>The initialized directory.</returns>
    public MetadataDirectory CreateDirectory(MetadataFamily family, string group, string name)
    {
        return MetadataDirectory.Create(family, group, name);
    }

    /// <summary>Creates a metadata tag.</summary>
    /// <returns>The initialized tag.</returns>
    public MetadataTag CreateTag(string key, string name, MetadataFamily family, MetadataValue value)
    {
        return MetadataTag.Create(key, name, family, value);
    }

    /// <summary>Creates a signed integer metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateIntegerValue(long value)
    {
        return MetadataValue.FromInteger(value);
    }

    /// <summary>Creates an unsigned integer metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateUnsignedIntegerValue(ulong value)
    {
        return MetadataValue.FromUnsignedInteger(value);
    }

    /// <summary>Creates a rational metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateRationalValue(long numerator, long denominator)
    {
        return MetadataValue.FromRational(numerator, denominator);
    }

    /// <summary>Creates a signed rational metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateSignedRationalValue(long numerator, long denominator)
    {
        return MetadataValue.FromSignedRational(numerator, denominator);
    }

    /// <summary>Creates a double metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateDoubleValue(double value)
    {
        return MetadataValue.FromDouble(value);
    }

    /// <summary>Creates a signed integer-array metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateIntegerArrayValue(System.ReadOnlySpan<long> values)
    {
        return MetadataValue.FromIntegerArray(values.ToArray());
    }

    /// <summary>Creates an unsigned integer-array metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateUnsignedIntegerArrayValue(System.ReadOnlySpan<ulong> values)
    {
        return MetadataValue.FromUnsignedIntegerArray(values.ToArray());
    }

    /// <summary>Creates a rational-array metadata value by copying paired numerators and denominators.</summary>
    /// <exception cref="System.ArgumentException">The numerator and denominator counts differ.</exception>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateRationalArrayValue(System.ReadOnlySpan<long> numerators, System.ReadOnlySpan<long> denominators)
    {
        if (numerators.Length != denominators.Length)
        {
            throw new System.ArgumentException("Rational numerator and denominator counts must match.");
        }

        return MetadataValue.FromRationalArray(numerators.ToArray(), denominators.ToArray());
    }

    /// <summary>Creates a signed rational-array metadata value by copying paired numerators and denominators.</summary>
    /// <exception cref="System.ArgumentException">The numerator and denominator counts differ.</exception>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateSignedRationalArrayValue(System.ReadOnlySpan<long> numerators, System.ReadOnlySpan<long> denominators)
    {
        if (numerators.Length != denominators.Length)
        {
            throw new System.ArgumentException("Signed rational numerator and denominator counts must match.");
        }

        return MetadataValue.FromSignedRationalArray(numerators.ToArray(), denominators.ToArray());
    }

    /// <summary>Creates a floating-point-array metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateDoubleArrayValue(System.ReadOnlySpan<double> values)
    {
        return MetadataValue.FromDoubleArray(values.ToArray());
    }

    /// <summary>Creates a string metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateStringValue(string value)
    {
        return MetadataValue.FromString(value);
    }

    /// <summary>Creates a string-list metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateStringListValue(System.ReadOnlySpan<string> values)
    {
        return MetadataValue.FromStringList(values.ToArray());
    }

    /// <summary>Creates a date-text metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateDateTextValue(string value)
    {
        return MetadataValue.FromDateText(value);
    }

    /// <summary>Creates an XML-text metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateXmlTextValue(string value)
    {
        return MetadataValue.FromXmlText(value);
    }

    /// <summary>Creates a byte-array metadata value by copying the supplied values.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateBytesValue(System.ReadOnlySpan<byte> value)
    {
        return MetadataValue.FromBytes(value.ToArray());
    }

    /// <summary>Creates a directory-reference metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateDirectoryValue(MetadataDirectory value)
    {
        return MetadataValue.FromDirectory(value);
    }

    /// <summary>Creates a Boolean metadata value.</summary>
    /// <returns>The initialized value.</returns>
    public MetadataValue CreateBooleanValue(bool value)
    {
        return MetadataValue.FromBoolean(value);
    }
}
