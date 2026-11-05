namespace iso20022;

/// <summary>Discovers and verifies XML signatures under caller-supplied trust policy.</summary>
public interface IXmlSignatureValidator
{
    /// <summary>Validates every applicable signature in one exact raw XML message.</summary>
    /// <param name="request">The raw XML, trust policy, and resource limits.</param>
    /// <returns>The stable signature validation result.</returns>
    SignatureValidationResult ValidateSignatures(SignatureValidationRequest? request);
}
