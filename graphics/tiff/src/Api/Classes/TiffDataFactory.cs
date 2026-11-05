namespace tiff;

/// <summary>Constructs and mutates public TIFF data models.</summary>
public class TiffDataFactory : ITiffDataFactory
{
    /// <summary>Creates a TIFF data factory.</summary>
    public TiffDataFactory()
    {
    }

    /// <summary>Creates a tag containing one unsigned value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateUnsignedScalarTag(int tagId, TiffDataType dataType, ulong value)
    {
        return TiffTagValue.CreateUnsignedScalar(tagId, dataType, value);
    }

    /// <summary>Creates a tag containing unsigned values.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateUnsignedArrayTag(int tagId, TiffDataType dataType, ulong[] values)
    {
        return TiffTagValue.CreateUnsignedArray(tagId, dataType, values);
    }

    /// <summary>Creates a tag containing one signed value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateSignedScalarTag(int tagId, TiffDataType dataType, long value)
    {
        return TiffTagValue.CreateSignedScalar(tagId, dataType, value);
    }

    /// <summary>Creates a tag containing signed values.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateSignedArrayTag(int tagId, TiffDataType dataType, long[] values)
    {
        return TiffTagValue.CreateSignedArray(tagId, dataType, values);
    }

    /// <summary>Creates an ASCII tag.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="text">The text.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateAsciiTag(int tagId, string text)
    {
        return TiffTagValue.CreateAscii(tagId, text);
    }

    /// <summary>Creates a multi-string ASCII tag.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="values">The strings.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateAsciiArrayTag(int tagId, string[] values)
    {
        return TiffTagValue.CreateAsciiArray(tagId, values);
    }

    /// <summary>Creates a tag containing one rational value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateRationalScalarTag(int tagId, TiffDataType dataType, long numerator, long denominator)
    {
        return TiffTagValue.CreateRationalScalar(tagId, dataType, numerator, denominator);
    }

    /// <summary>Creates a tag containing rational values.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateRationalArrayTag(int tagId, TiffDataType dataType, TiffRational[] values)
    {
        return TiffTagValue.CreateRationalArray(tagId, dataType, values);
    }

    /// <summary>Creates a tag containing one floating-point value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateFloatingScalarTag(int tagId, TiffDataType dataType, double value)
    {
        return TiffTagValue.CreateFloatingScalar(tagId, dataType, value);
    }

    /// <summary>Creates a tag containing floating-point values.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateFloatingArrayTag(int tagId, TiffDataType dataType, double[] values)
    {
        return TiffTagValue.CreateFloatingArray(tagId, dataType, values);
    }

    /// <summary>Creates a raw tag value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="count">The declared element count.</param>
    /// <param name="rawBytes">The raw bytes.</param>
    /// <returns>The tag value.</returns>
    public TiffTagValue CreateRawTag(int tagId, TiffDataType dataType, long count, byte[] rawBytes)
    {
        return TiffTagValue.CreateRaw(tagId, dataType, count, rawBytes);
    }

    /// <summary>Creates an image definition from tags.</summary>
    /// <param name="tags">The initial tags.</param>
    /// <returns>The image definition.</returns>
    public TiffImageDefinition CreateImageDefinition(TiffTagValue[] tags)
    {
        return TiffImageDefinition.Create(tags);
    }

    /// <summary>Adds or replaces an image-definition tag.</summary>
    /// <param name="definition">The image definition.</param>
    /// <param name="value">The tag value.</param>
    public void SetImageTag(TiffImageDefinition definition, TiffTagValue value)
    {
        if (definition != null)
        {
            definition.SetTag(value);
        }
    }

    /// <summary>Sets one raw strip payload.</summary>
    /// <param name="definition">The image definition.</param>
    /// <param name="stripIndex">The strip index.</param>
    /// <param name="bytes">The raw bytes.</param>
    public void SetRawStrip(TiffImageDefinition definition, int stripIndex, byte[] bytes)
    {
        if (definition != null)
        {
            definition.SetRawStrip(stripIndex, bytes);
        }
    }

    /// <summary>Sets one raw tile payload.</summary>
    /// <param name="definition">The image definition.</param>
    /// <param name="tileIndex">The tile index.</param>
    /// <param name="bytes">The raw bytes.</param>
    public void SetRawTile(TiffImageDefinition definition, int tileIndex, byte[] bytes)
    {
        if (definition != null)
        {
            definition.SetRawTile(tileIndex, bytes);
        }
    }
}
