namespace args;

/// <summary>
/// Reports the outcome of converting one text value to a requested type.
/// </summary>
public class ArgConvertResult
{
    /// <summary>
    /// Whether conversion succeeded.
    /// </summary>
    public bool Success;
    /// <summary>
    /// The converted value when conversion succeeded; otherwise null.
    /// </summary>
    public object? Value;
    /// <summary>
    /// A developer-readable conversion error, or an empty string after success.
    /// </summary>
    public string Error;
    /// <summary>
    /// The logical type name expected by the converter.
    /// </summary>
    public string ExpectedType;
    /// <summary>
    /// The original text passed to the converter.
    /// </summary>
    public string ActualValue;

    /// <summary>
    /// Creates a conversion result with basic success, value, and error information.
    /// </summary>
    /// <param name="success">Whether the conversion succeeded.</param>
    /// <param name="value">The converted value, or null when no value was produced.</param>
    /// <param name="error">The conversion error text, or an empty string after success.</param>
    public ArgConvertResult(bool success, object? value, string error)
    {
        Success = success;
        Value = value;
        Error = error;
        ExpectedType = "";
        ActualValue = "";
    }

    /// <summary>
    /// Creates a conversion result with complete expected-type and source-value details.
    /// </summary>
    /// <param name="success">Whether the conversion succeeded.</param>
    /// <param name="value">The converted value, or null when no value was produced.</param>
    /// <param name="error">The conversion error text, or an empty string after success.</param>
    /// <param name="expectedType">The logical name of the expected target type.</param>
    /// <param name="actualValue">The original text supplied for conversion.</param>
    /// <returns>A conversion result containing complete diagnostic details.</returns>
    internal static ArgConvertResult Detailed(bool success, object? value, string error, string expectedType, string actualValue)
    {
        ArgConvertResult result = new ArgConvertResult(success, value, error);
        result.ExpectedType = expectedType;
        result.ActualValue = actualValue;
        return result;
    }

    /// <summary>
    /// Creates an unsuccessful result with empty diagnostic details.
    /// </summary>
    /// <returns>An empty failed conversion result.</returns>
    internal static ArgConvertResult Empty()
    {
        return new ArgConvertResult(false, null, "");
    }
}
