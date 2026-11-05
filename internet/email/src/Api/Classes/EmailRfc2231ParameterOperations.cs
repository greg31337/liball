using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailRfc2231Parameter"/>.
/// </summary>
public class EmailRfc2231ParameterOperations : IEmailRfc2231ParameterOperationsContract
{
    /// <summary>Creates an RFC 2231 parameter at the default source position.</summary>
    /// <param name = "name">The parameter name.</param>
    /// <param name = "valueBytes">The parameter value bytes.</param>
    /// <param name = "quoted">Whether the source value was quoted.</param>
    /// <returns>The configured parameter.</returns>
    public EmailRfc2231Parameter CreateWithNameAndValueBytesAndQuoted(string name, byte[] valueBytes, bool quoted)
    {
        return EmailRfc2231Parameter.CreateImplementationWithNameAndValueBytesAndQuoted(name, valueBytes, quoted);
    }

    /// <summary>
    /// Creates a new rfc 2231 parameter.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "valueBytes">The source bytes or data to process.</param>
    /// <param name = "quoted">Whether to enable the quoted behavior.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <returns>The configured parameter.</returns>
    public EmailRfc2231Parameter CreateWithNameAndValueBytesAndQuotedAndSourcePosition(string name, byte[] valueBytes, bool quoted, EmailSourcePosition sourcePosition)
    {
        return EmailRfc2231Parameter.CreateImplementationWithNameAndValueBytesAndQuotedAndSourcePosition(name, valueBytes, quoted, sourcePosition);
    }
}
