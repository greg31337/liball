namespace iso20022;

/// <summary>Discovers, preserves, resolves, and validates ISO 20022 supplementary data.</summary>
public interface ISupplementaryDataValidator
{
    /// <summary>Processes one raw or generic message under explicit supplementary-data policy.</summary>
    /// <param name="request">The message, local schema services, policy, and resource limits.</param>
    /// <returns>The stable supplementary discovery and validation result.</returns>
    SupplementaryValidationResult ValidateSupplementaryData(
        SupplementaryValidationRequest? request);
}
