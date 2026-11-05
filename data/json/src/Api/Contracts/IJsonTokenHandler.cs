namespace json;

/// <summary>Receives tokens from a push-based JSON parser.</summary>
public interface IJsonTokenHandler
{
    /// <summary>Handles one JSON token.</summary>
    /// <param name="token">The token to handle.</param>
    /// <returns>The action the parser should take.</returns>
    JsonHandlerAction HandleToken(JsonToken token);
}
