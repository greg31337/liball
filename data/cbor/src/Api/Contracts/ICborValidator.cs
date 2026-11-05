namespace cbor;

/// <summary>
/// Validates encoded CBOR data and generic CBOR values.
/// </summary>
public interface ICborValidator
{
    /// <summary>Validates encoded CBOR data at the selected level.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="level">The highest validation layer to apply.</param>
    /// <returns>The validation outcome and issues.</returns>
    CborValidationResult ValidateEncoding(byte[] source, CborValidationLevel level);

    /// <summary>Validates encoded CBOR data using configured levels, policies, limits, and issue collection mode.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="options">The decoding and validation policies.</param>
    /// <returns>The validation outcome and issues.</returns>
    CborValidationResult ValidateEncodingWithOptions(byte[] source, CborDecodeOptions options);

    /// <summary>Validates a generic CBOR value at the selected level.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="level">The highest validation layer to apply.</param>
    /// <returns>The validation outcome and issues.</returns>
    CborValidationResult ValidateValue(CborValue value, CborValidationLevel level);

    /// <summary>Validates a generic CBOR value using configured levels, policies, limits, and issue collection mode.</summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="options">The decoding and validation policies.</param>
    /// <returns>The validation outcome and issues.</returns>
    CborValidationResult ValidateValueWithOptions(CborValue value, CborDecodeOptions options);
}
