namespace languagecodes;

/// <summary>Represents failure to resolve a required language code.</summary>
public class LanguageCodeNotFoundException : global::System.Exception
{
    /// <summary>Initializes an exception with a description of the unresolved language code.</summary>
    /// <param name="message">The message describing the lookup failure.</param>
    public LanguageCodeNotFoundException(string message) : base(message)
    {
    }
}
