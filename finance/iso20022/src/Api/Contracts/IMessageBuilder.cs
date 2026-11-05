namespace iso20022;

/// <summary>Builds exact ISO 20022 messages from generated semantic template inputs.</summary>
public interface IMessageBuilder
{
    /// <summary>Builds and optionally validates one exact target message.</summary>
    /// <param name="request">The template input, exact target identity, and validation policy.</param>
    /// <returns>The constructed message and complete target validation outcome.</returns>
    BuilderResult BuildMessage(BuilderRequest? request);
}
