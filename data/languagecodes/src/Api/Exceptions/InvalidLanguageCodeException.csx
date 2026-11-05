namespace languagecodes;

/// <summary>Represents failure caused by an invalid language code.</summary>
public class InvalidLanguageCodeException : global::System.Exception
{
    /// <summary>Initializes an exception with a description of the invalid language code.</summary>
    /// <param name="message">The message describing why the code is invalid.</param>
    public InvalidLanguageCodeException(string message) : base(message)
    {
    }
}
