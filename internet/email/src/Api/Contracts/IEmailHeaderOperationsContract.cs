using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeader"/>.
/// </summary>
public interface IEmailHeaderOperationsContract
{
    /// <summary>Creates a header with an unknown structured value kind.</summary>
    /// <param name = "name">The field name.</param>
    /// <param name = "rawValue">The raw field value.</param>
    /// <returns>The configured header.</returns>
    EmailHeader CreateWithNameAndRawValue(string name, byte[] rawValue);
    /// <summary>
    /// Creates a new header.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "rawValue">The raw value used by the operation.</param>
    /// <param name = "valueKind">The value kind used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured header.</returns>
    EmailHeader CreateWithNameAndRawValueAndValueKind(string name, byte[] rawValue, EmailHeaderValueKind valueKind);
    /// <summary>
    /// Creates a parsed header with source metadata.
    /// </summary>
    /// <param name = "originalNameBytes">The source bytes or data to process.</param>
    /// <param name = "originalRawValue">The original raw value used by the operation.</param>
    /// <param name = "lineEnding">The line ending used by the operation.</param>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    /// <param name = "valuePosition">The value position used by the operation.</param>
    /// <param name = "valueKind">The value kind used by the operation.</param>
    /// <param name = "parseMode">The parse mode used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <returns>The configured header.</returns>
    EmailHeader CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssues(byte[] originalNameBytes, byte[] originalRawValue, EmailLineEnding lineEnding, EmailRange sourceRange, EmailSourcePosition valuePosition, EmailHeaderValueKind valueKind, EmailStructuredValueParseMode parseMode, EmailIssueList issues);
    /// <summary>
    /// Creates a parsed header while retaining the exact original field bytes.
    /// </summary>
    /// <param name = "originalNameBytes">The source bytes or data to process.</param>
    /// <param name = "originalRawValue">The original raw value used by the operation.</param>
    /// <param name = "lineEnding">The line ending used by the operation.</param>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    /// <param name = "valuePosition">The value position used by the operation.</param>
    /// <param name = "valueKind">The value kind used by the operation.</param>
    /// <param name = "parseMode">The parse mode used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "exactOriginalRawFieldBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured header.</returns>
    EmailHeader CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssuesAndExactOriginalRawFieldBytes(byte[]? originalNameBytes, byte[]? originalRawValue, EmailLineEnding lineEnding, EmailRange? sourceRange, EmailSourcePosition? valuePosition, EmailHeaderValueKind valueKind, EmailStructuredValueParseMode parseMode, EmailIssueList? issues, byte[] exactOriginalRawFieldBytes);
}
