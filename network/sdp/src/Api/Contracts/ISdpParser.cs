using System;

namespace sdp;

/// <summary>
/// Parses SDP from text or UTF-8 input into bounded result objects.
/// </summary>
public interface ISdpParser
{
    /// <summary>Creates a parse result from a .NET string.</summary>
    /// <param name="input">The SDP text to parse.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>A bounded parse result.</returns>
    SdpParseResult ParseText(string input, SdpParseOptions options);

    /// <summary>Creates a parse result from a character span.</summary>
    /// <param name="input">The SDP characters to parse.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>A bounded parse result.</returns>
    SdpParseResult ParseCharacters(ReadOnlySpan<char> input, SdpParseOptions options);

    /// <summary>Creates a parse result from a UTF-8 byte span.</summary>
    /// <param name="input">The UTF-8 SDP bytes to parse.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>A bounded parse result.</returns>
    SdpParseResult ParseUtf8Bytes(ReadOnlySpan<byte> input, SdpParseOptions options);
}
