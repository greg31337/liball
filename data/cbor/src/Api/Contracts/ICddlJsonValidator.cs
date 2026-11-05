namespace cbor;

/// <summary>Validates JSON documents against the CDDL JSON-compatible subset.</summary>
public interface ICddlJsonValidator
{
    /// <summary>Validates a JSON document against a selected schema rule.</summary>
    /// <param name="schema">The compiled schema.</param>
    /// <param name="json">The JSON document text.</param>
    /// <param name="options">The validation selection and budgets.</param>
    /// <returns>The structured validation result.</returns>
    CddlValidationResult ValidateJson(
        CddlSchema schema,
        string json,
        CddlValidationOptions options);
}
