namespace iso20022;

/// <summary>Contains the exception-less result of parsing a message identifier.</summary>
public class MessageIdentifierParseResult
{
    /// <summary>Whether parsing succeeded.</summary>
    public bool Success;
    /// <summary>The exact parsed identifier when successful.</summary>
    public MessageIdentifier? Identifier;
    /// <summary>A stable error code when parsing failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when parsing failed.</summary>
    public string ErrorMessage = string.Empty;
}
