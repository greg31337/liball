using System;

namespace iso20022;

/// <summary>Provides strict ISO 20022 message-identifier parsing.</summary>
public class MessageIdentifierParser : IMessageIdentifierParser
{
    /// <summary>Initializes a message-identifier parser.</summary>
    public MessageIdentifierParser()
    {
    }

    /// <summary>Parses a four-part ISO 20022 message identifier.</summary>
    /// <param name="value">The identifier text.</param>
    /// <returns>The parse result.</returns>
    public MessageIdentifierParseResult ParseIdentifier(string? value)
    {
        MessageIdentifierParseResult result = new MessageIdentifierParseResult();
        if (value == null)
        {
            result.ErrorCode = "identifier.null";
            result.ErrorMessage = "A message identifier is required.";
            return result;
        }

        MessageIdentifier? identifier = MessageIdentifierLexical.ParseMessageIdentifier(value.AsSpan());
        if (identifier == null)
        {
            result.ErrorCode = "identifier.invalid";
            result.ErrorMessage = "The message identifier must contain a four-letter lowercase business area, three-digit functionality and variant components, and a two-digit version.";
            return result;
        }

        result.Success = true;
        result.Identifier = identifier;
        return result;
    }
}
