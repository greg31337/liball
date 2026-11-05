namespace cbor;

/// <summary>Validates generic CBOR values against compiled CDDL schemas.</summary>
public interface ICddlValidator
{
    /// <summary>Validates a CBOR value against a selected schema rule.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="value">The CBOR value to validate.</param>
    /// <param name="options">The validation selection and budgets.</param>
    /// <returns>The structured validation result.</returns>
    CddlValidationResult ValidateCbor(
        CddlSchema schema,
        CborValue value,
        CddlValidationOptions options);

    /// <summary>Decodes and validates exactly one encoded CBOR data item.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="decodeOptions">The decoding policies and limits.</param>
    /// <param name="validationOptions">The CDDL validation selection and budgets.</param>
    /// <returns>The structured decoding or validation result.</returns>
    CddlValidationResult ValidateEncodedCbor(
        CddlSchema schema,
        byte[] source,
        CborDecodeOptions decodeOptions,
        CddlValidationOptions validationOptions);
}
