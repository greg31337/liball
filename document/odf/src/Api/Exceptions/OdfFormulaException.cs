namespace odf;

/// <summary>
/// Represents a failure while parsing, evaluating, or rewriting an OpenDocument formula.
/// </summary>
public class OdfFormulaException : System.Exception
{
    /// <summary>
    /// Initializes an exception with a description of the formula failure.
    /// </summary>
    /// <param name="message">The message that describes the failure.</param>
    public OdfFormulaException(string message) : base(message)
    {
    }
}
