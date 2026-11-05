namespace iso20022;

/// <summary>Parses exact ISO 20022 message identifiers without throwing for invalid input.</summary>
public interface IMessageIdentifierParser
{
    /// <summary>Parses a four-part ISO 20022 message identifier.</summary>
    /// <param name="value">The identifier text.</param>
    /// <returns>The parse result.</returns>
    MessageIdentifierParseResult ParseIdentifier(string? value);
}
