using System;

namespace sdp;

/// <summary>
/// Parses SDP text and UTF-8 data using bounded result-based operations.
/// </summary>
public class SdpParser : ISdpParser
{
    internal bool ValidateDescriptionStructure;

    /// <summary>
    /// Initializes an SDP parser.
    /// </summary>
    public SdpParser()
    {
        ValidateDescriptionStructure = true;
    }

    /// <summary>Creates a parse result from a .NET string.</summary>
    /// <param name="input">The SDP text to parse.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>A bounded parse result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input or options argument is null.</exception>
    public SdpParseResult ParseText(string input, SdpParseOptions options)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpParserCore.ParseTextInput(input, options, ValidateDescriptionStructure);
    }

    /// <summary>Creates a parse result from a character span.</summary>
    /// <param name="input">The SDP characters to parse.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>A bounded parse result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the options argument is null.</exception>
    public SdpParseResult ParseCharacters(ReadOnlySpan<char> input, SdpParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpParserCore.ParseCharacterInput(input, options, ValidateDescriptionStructure);
    }

    /// <summary>Creates a parse result from a UTF-8 byte span.</summary>
    /// <param name="input">The UTF-8 SDP bytes to parse.</param>
    /// <param name="options">The parsing options to apply.</param>
    /// <returns>A bounded parse result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the options argument is null.</exception>
    public SdpParseResult ParseUtf8Bytes(ReadOnlySpan<byte> input, SdpParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
        return SdpParserCore.ParseUtf8Input(input, options, ValidateDescriptionStructure);
    }
}
