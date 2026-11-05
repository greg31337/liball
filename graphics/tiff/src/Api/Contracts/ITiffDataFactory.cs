namespace tiff;

/// <summary>Defines construction and mutation operations for public TIFF data models.</summary>
public interface ITiffDataFactory
{
    /// <summary>Creates a tag containing one unsigned value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateUnsignedScalarTag(int tagId, TiffDataType dataType, ulong value);
    /// <summary>Creates a tag containing unsigned values.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateUnsignedArrayTag(int tagId, TiffDataType dataType, ulong[] values);
    /// <summary>Creates a tag containing one signed value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateSignedScalarTag(int tagId, TiffDataType dataType, long value);
    /// <summary>Creates a tag containing signed values.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateSignedArrayTag(int tagId, TiffDataType dataType, long[] values);
    /// <summary>Creates an ASCII tag.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="text">The text.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateAsciiTag(int tagId, string text);
    /// <summary>Creates a multi-string ASCII tag.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="values">The strings.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateAsciiArrayTag(int tagId, string[] values);
    /// <summary>Creates a tag containing one rational value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateRationalScalarTag(int tagId, TiffDataType dataType, long numerator, long denominator);
    /// <summary>Creates a tag containing rational values.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateRationalArrayTag(int tagId, TiffDataType dataType, TiffRational[] values);
    /// <summary>Creates a tag containing one floating-point value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateFloatingScalarTag(int tagId, TiffDataType dataType, double value);
    /// <summary>Creates a tag containing floating-point values.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateFloatingArrayTag(int tagId, TiffDataType dataType, double[] values);
    /// <summary>Creates a raw tag value.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="dataType">The TIFF data type.</param>
    /// <param name="count">The declared element count.</param>
    /// <param name="rawBytes">The raw bytes.</param>
    /// <returns>The tag value.</returns>
    TiffTagValue CreateRawTag(int tagId, TiffDataType dataType, long count, byte[] rawBytes);
    /// <summary>Creates an image definition from tags.</summary>
    /// <param name="tags">The initial tags.</param>
    /// <returns>The image definition.</returns>
    TiffImageDefinition CreateImageDefinition(TiffTagValue[] tags);
    /// <summary>Adds or replaces an image-definition tag.</summary>
    /// <param name="definition">The image definition.</param>
    /// <param name="value">The tag value.</param>
    void SetImageTag(TiffImageDefinition definition, TiffTagValue value);
    /// <summary>Sets one raw strip payload.</summary>
    /// <param name="definition">The image definition.</param>
    /// <param name="stripIndex">The strip index.</param>
    /// <param name="bytes">The raw bytes.</param>
    void SetRawStrip(TiffImageDefinition definition, int stripIndex, byte[] bytes);
    /// <summary>Sets one raw tile payload.</summary>
    /// <param name="definition">The image definition.</param>
    /// <param name="tileIndex">The tile index.</param>
    /// <param name="bytes">The raw bytes.</param>
    void SetRawTile(TiffImageDefinition definition, int tileIndex, byte[] bytes);
}
