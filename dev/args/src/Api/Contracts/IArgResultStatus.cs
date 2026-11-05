namespace args;

/// <summary>
/// Defines failure-status queries for parse, validation, conversion, and run results.
/// </summary>
public interface IArgResultStatus
{
    /// <summary>Returns whether a parse result contains errors.</summary>
    bool ParseFailed(ArgParseResult result);
    /// <summary>Returns whether a validation result contains errors.</summary>
    bool ValidationFailed(ArgValidationResult result);
    /// <summary>Returns whether a conversion result is unsuccessful.</summary>
    bool ConversionFailed(ArgConvertResult result);
    /// <summary>Returns whether a run contains validation errors or a nonzero exit code.</summary>
    bool RunFailed(ArgRunResult result);
}
