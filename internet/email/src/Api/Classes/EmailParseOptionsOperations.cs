using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailParseOptions"/>.
/// </summary>
public class EmailParseOptionsOperations : IEmailParseOptionsOperationsContract
{
    /// <summary>
    /// Creates a strict configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    public EmailParseOptions CreateStrict()
    {
        return EmailParseOptions.CreateStrictImplementation();
    }

    /// <summary>
    /// Creates a conforming tolerant configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    public EmailParseOptions CreateConformingTolerant()
    {
        return EmailParseOptions.CreateConformingTolerantImplementation();
    }

    /// <summary>
    /// Creates a forensic configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    public EmailParseOptions CreateForensic()
    {
        return EmailParseOptions.CreateForensicImplementation();
    }

    /// <summary>
    /// Creates a preserving configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    public EmailParseOptions CreatePreserving()
    {
        return EmailParseOptions.CreatePreservingImplementation();
    }

    /// <summary>
    /// Creates a smtp UTF-8 configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    public EmailParseOptions CreateSmtpUtf8()
    {
        return EmailParseOptions.CreateSmtpUtf8Implementation();
    }

    /// <summary>
    /// Creates a bounded untrusted configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    public EmailParseOptions CreateBoundedUntrusted()
    {
        return EmailParseOptions.CreateBoundedUntrustedImplementation();
    }
}
