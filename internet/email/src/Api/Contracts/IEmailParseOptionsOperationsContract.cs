using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailParseOptions"/>.
/// </summary>
public interface IEmailParseOptionsOperationsContract
{
    /// <summary>
    /// Creates a strict configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    EmailParseOptions CreateStrict();
    /// <summary>
    /// Creates a conforming tolerant configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    EmailParseOptions CreateConformingTolerant();
    /// <summary>
    /// Creates a forensic configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    EmailParseOptions CreateForensic();
    /// <summary>
    /// Creates a preserving configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    EmailParseOptions CreatePreserving();
    /// <summary>
    /// Creates a smtp UTF-8 configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    EmailParseOptions CreateSmtpUtf8();
    /// <summary>
    /// Creates a bounded untrusted configured for the requested behavior.
    /// </summary>
    /// <returns>The parse options produced by the operation.</returns>
    EmailParseOptions CreateBoundedUntrusted();
}
