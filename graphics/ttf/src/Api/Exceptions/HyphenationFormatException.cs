namespace ttf;

/// <summary>Represents malformed caller-supplied hyphenation dictionary data.</summary>
public class HyphenationFormatException : System.Exception
{
    /// <summary>Creates an exception with a caller-supplied message.</summary>
    /// <param name="message">The message that describes the format failure.</param>
    public HyphenationFormatException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with a deterministic default message.</summary>
    /// <returns>A new hyphenation format exception.</returns>
    internal static HyphenationFormatException Create()
    {
        return new HyphenationFormatException(
            "The hyphenation dictionary is malformed.");
    }
}
