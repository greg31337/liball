using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailStandardHeaderParser"/>.
/// </summary>
public interface IEmailStandardHeaderParserContract
{
    /// <summary>Parses a supported field value.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "input">The field value bytes.</param>
    /// <param name = "mode">The parse mode.</param>
    /// <returns>The parse result.</returns>
    EmailStandardHeaderParseResult ParseWithFieldNameAndInputAndMode(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParseMode mode);
    /// <summary>Parses a supported field value with explicit options.</summary>
    /// <param name = "fieldName">The field name.</param>
    /// <param name = "input">The field value bytes.</param>
    /// <param name = "options">The parser options.</param>
    /// <param name = "initialPosition">The initial source position.</param>
    /// <returns>The parse result.</returns>
    EmailStandardHeaderParseResult ParseWithFieldNameAndInputAndOptionsAndInitialPosition(string fieldName, ReadOnlySpan<byte> input, EmailStructuredValueParserOptions options, EmailSourcePosition? initialPosition);
}
