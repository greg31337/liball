namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgTimingParser"/>.</summary>
public interface ISvgTimingParser
{
    /// <summary>Parses a signed SMIL clock value.</summary>
    /// <param name="value">The clock text.</param>
    /// <param name="maxTokenCharacters">The maximum clock token length.</param>
    /// <returns>A finite seconds value or stable failure.</returns>
    SvgClockParseResult ParseClock(string? value, int maxTokenCharacters);
    /// <summary>Parses one offset, sync, event, repeat, access-key, wallclock, or indefinite timing specifier.</summary>
    /// <param name="value">The timing text.</param>
    /// <param name="maxTokenCharacters">The maximum specifier and clock token length.</param>
    /// <returns>A typed timing specifier or stable failure.</returns>
    SvgTimingParseResult ParseTiming(string? value, int maxTokenCharacters);
    /// <summary>Parses a semicolon-separated SVG begin timing list.</summary>
    /// <param name="value">The begin-list text.</param>
    /// <param name="maxItems">The maximum timing count.</param>
    /// <param name="maxTokenCharacters">The maximum item length.</param>
    /// <returns>A timing list or valid prefix and failure.</returns>
    SvgTimingListParseResult ParseBeginList(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses a semicolon-separated SVG end timing list.</summary>
    /// <param name="value">The end-list text.</param>
    /// <param name="maxItems">The maximum timing count.</param>
    /// <param name="maxTokenCharacters">The maximum item length.</param>
    /// <returns>A timing list or valid prefix and failure.</returns>
    SvgTimingListParseResult ParseEndList(string value, int maxItems, int maxTokenCharacters);
    /// <summary>Parses a bounded semicolon-separated timing list.</summary>
    /// <param name="value">The list text.</param>
    /// <param name="maxItems">The maximum timing count.</param>
    /// <param name="maxTokenCharacters">The maximum item length.</param>
    /// <returns>A timing list or valid prefix and failure.</returns>
    SvgTimingListParseResult ParseTimingList(string? value, int maxItems, int maxTokenCharacters);
}
