namespace icc;

/// <summary>
/// Validates caller-configurable profile operation options without throwing for expected failures.
/// </summary>
public interface IIccOptionsValidator
{
    /// <summary>
    /// Validates profile reading options and their resource limits.
    /// </summary>
    /// <param name="options">The reading options to validate.</param>
    /// <returns>The validation outcome and location details.</returns>
    IccOperationResult ValidateReadOptions(IccReadOptions options);

    /// <summary>
    /// Validates profile writing options and their resource limits.
    /// </summary>
    /// <param name="options">The writing options to validate.</param>
    /// <returns>The validation outcome and location details.</returns>
    IccOperationResult ValidateWriteOptions(IccWriteOptions options);
}
