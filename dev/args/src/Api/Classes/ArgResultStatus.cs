namespace args;

/// <summary>
/// Provides read-only failure-status queries for operation result objects.
/// </summary>
public class ArgResultStatus : IArgResultStatus
{
    /// <summary>
    /// Creates a result-status query service.
    /// </summary>
    public ArgResultStatus()
    {
    }

    /// <summary>
    /// Returns whether a parse result contains one or more errors.
    /// </summary>
    /// <param name="result">The parse result to inspect.</param>
    /// <returns>True when parsing produced errors; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">The result is null.</exception>
    public bool ParseFailed(ArgParseResult result)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        return result.HasErrors();
    }

    /// <summary>
    /// Returns whether a validation result contains one or more errors.
    /// </summary>
    /// <param name="result">The validation result to inspect.</param>
    /// <returns>True when parsing or validation produced errors; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">The result is null.</exception>
    public bool ValidationFailed(ArgValidationResult result)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        return result.HasErrors();
    }

    /// <summary>
    /// Returns whether a conversion result is unsuccessful.
    /// </summary>
    /// <param name="result">The conversion result to inspect.</param>
    /// <returns>True when conversion failed; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">The result is null.</exception>
    public bool ConversionFailed(ArgConvertResult result)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        return !result.Success;
    }

    /// <summary>
    /// Returns whether a run contains validation errors or a nonzero process-style exit code.
    /// </summary>
    /// <param name="result">The run result to inspect.</param>
    /// <returns>True when the run failed; otherwise false.</returns>
    /// <exception cref="System.ArgumentNullException">The result is null.</exception>
    public bool RunFailed(ArgRunResult result)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        if (result.HasErrors())
        {
            return true;
        }

        return result.ExitCode != 0;
    }
}
