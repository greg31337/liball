namespace barcode;

/// <summary>
/// Reports invalid or unsupported GS1 data syntax.
/// </summary>
public class Gs1SyntaxException : global::System.Exception
{
    /// <summary>
    /// Creates a GS1 syntax exception with a descriptive message.
    /// </summary>
    /// <param name="message">The message describing the GS1 syntax failure.</param>
    public Gs1SyntaxException(string message) : base(message)
    {
    }
}
