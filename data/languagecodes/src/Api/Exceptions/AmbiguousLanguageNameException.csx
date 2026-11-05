namespace languagecodes;

/// <summary>Represents failure to select one language from an ambiguous name lookup.</summary>
public class AmbiguousLanguageNameException : global::System.Exception
{
    /// <summary>Initializes an exception with a description of the ambiguous language name.</summary>
    /// <param name="message">The message describing the ambiguity.</param>
    public AmbiguousLanguageNameException(string message) : base(message)
    {
    }
}
