namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgXmlValidator"/>.</summary>
public interface ISvgXmlValidator
{
    /// <summary>Validates one tokenized source and populates decoded and normalized token fields.</summary>
    /// <param name="source">The exact decoded source used to produce the tokens.</param>
    /// <param name="tokens">The ordered source-preserving tokens.</param>
    /// <param name="maxNameCharacters">The maximum UTF-16 character count of one XML name.</param>
    /// <returns>A success result or the earliest located well-formedness failure.</returns>
    SvgXmlValidationResult Validate(string? source, System.Collections.Generic.List<SvgXmlToken>? tokens, int maxNameCharacters);
}
