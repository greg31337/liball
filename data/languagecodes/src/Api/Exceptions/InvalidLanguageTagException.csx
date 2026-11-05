namespace languagecodes;

/// <summary>Represents failure caused by an invalid BCP 47 language tag.</summary>
public class InvalidLanguageTagException : global::System.Exception
{
    /// <summary>Initializes an exception with a description of the invalid language tag.</summary>
    /// <param name="message">The message describing why the language tag is invalid.</param>
    public InvalidLanguageTagException(string message) : base(message)
    {
    }
}
