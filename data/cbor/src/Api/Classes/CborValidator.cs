namespace cbor;

/// <summary>
/// Provides validation for encoded CBOR data and generic values.
/// </summary>
public class CborValidator : ICborValidator
{
    /// <summary>Creates a CBOR validator.</summary>
    public CborValidator()
    {
    }

    /// <summary>Validates encoded CBOR data at the selected level.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="level">The highest validation layer to apply.</param>
    /// <returns>The validation outcome and issues.</returns>
    public CborValidationResult ValidateEncoding(byte[] source, CborValidationLevel level)
    {
        CborDecodeOptions options = new CborDecodeOptions();
        options.ValidationLevel = level;
        CborValidationCore core = new CborValidationCore(source, options);
        return core.ValidateEncoding();
    }

    /// <summary>Validates encoded CBOR data using configured levels, policies, limits, and issue collection mode.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="options">The decoding and validation policies.</param>
    /// <returns>The validation outcome and issues.</returns>
    public CborValidationResult ValidateEncodingWithOptions(
        byte[] source,
        CborDecodeOptions options)
    {
        CborValidationCore core = new CborValidationCore(source, options);
        return core.ValidateEncoding();
    }

    /// <summary>Validates a generic CBOR value at the selected level.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="level">The highest validation layer to apply.</param>
    /// <returns>The validation outcome and issues.</returns>
    public CborValidationResult ValidateValue(CborValue value, CborValidationLevel level)
    {
        CborDecodeOptions options = new CborDecodeOptions();
        options.ValidationLevel = level;
        CborValidationCore core = new CborValidationCore(null, options);
        return core.ValidateValue(value);
    }

    /// <summary>Validates a generic CBOR value using configured levels, policies, limits, and issue collection mode.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="options">The decoding and validation policies.</param>
    /// <returns>The validation outcome and issues.</returns>
    public CborValidationResult ValidateValueWithOptions(
        CborValue value,
        CborDecodeOptions options)
    {
        CborValidationCore core = new CborValidationCore(null, options);
        return core.ValidateValue(value);
    }
}
