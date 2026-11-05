namespace icc;

/// <summary>Validates profile operation options without throwing for expected failures.</summary>
public class IccOptionsValidator : IIccOptionsValidator
{
    internal OptionsValidationCore Core;

    /// <summary>Initializes an options validator.</summary>
    public IccOptionsValidator()
    {
        Core = new OptionsValidationCore();
    }

    /// <summary>Validates profile reading options.</summary>
    /// <param name="options">The reading options to validate.</param>
    /// <returns>The validation outcome.</returns>
    public IccOperationResult ValidateReadOptions(IccReadOptions options)
    {
        return Core.ValidateRead(options);
    }

    /// <summary>Validates profile writing options.</summary>
    /// <param name="options">The writing options to validate.</param>
    /// <returns>The validation outcome.</returns>
    public IccOperationResult ValidateWriteOptions(IccWriteOptions options)
    {
        return Core.ValidateWrite(options);
    }
}
