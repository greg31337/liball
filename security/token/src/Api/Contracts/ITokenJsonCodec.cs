using System;
namespace token;

/// <summary>
/// Provides strict JSON reading and deterministic JSON writing for token data.
/// </summary>
public interface ITokenJsonCodec
{
    /// <summary>Reads one duplicate-safe JSON value from text.</summary>
    /// <param name="text">The JSON text.</param>
    /// <param name="maximumLength">The maximum accepted UTF-8 byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays and objects.</param>
    /// <returns>The decoded value, or stable failure details.</returns>
    JsonValueResult ReadJsonText(string text, int maximumLength, int maximumDepth);

    /// <summary>Reads one duplicate-safe JSON value from strict UTF-8.</summary>
    /// <param name="utf8">The JSON bytes.</param>
    /// <param name="maximumLength">The maximum accepted byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays and objects.</param>
    /// <returns>The decoded value, or stable failure details.</returns>
    JsonValueResult ReadJsonUtf8(byte[] utf8, int maximumLength, int maximumDepth);

    /// <summary>Writes one JSON value with deterministic object-member ordering.</summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="maximumLength">The maximum output UTF-8 byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays and objects.</param>
    /// <returns>The compact JSON text, or stable failure details.</returns>
    TokenTextResult WriteJsonText(JsonValue value, int maximumLength, int maximumDepth);

    /// <summary>Writes one JSON value as strict UTF-8 with deterministic object-member ordering.</summary>
    /// <param name="value">The JSON value.</param>
    /// <param name="maximumLength">The maximum output byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays and objects.</param>
    /// <returns>The compact JSON bytes, or stable failure details.</returns>
    TokenBytesResult WriteJsonUtf8(JsonValue value, int maximumLength, int maximumDepth);
}
