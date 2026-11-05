using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF header.
/// </summary>
public class EmailTnefHeader : IEmailTnefHeaderContract
{
    uint signature;
    int legacyKey;
    EmailRange sourceRange;
    /// <summary>
    /// Creates a new TNEF header.
    /// </summary>
    /// <param name = "signature">The signature used by the operation.</param>
    /// <param name = "legacyKey">The legacy key used by the operation.</param>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    public EmailTnefHeader(uint signature, int legacyKey, EmailRange sourceRange)
    {
        this.signature = signature;
        this.legacyKey = legacyKey;
        this.sourceRange = sourceRange;
    }

    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <returns>The current signature.</returns>
    public uint GetSignature()
    {
        return signature;
    }

    /// <summary>
    /// Determines whether the current value or state is signature valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature valid; otherwise, <c>false</c>.</returns>
    public bool IsSignatureValid()
    {
        return signature == EmailTnefReaderConstants.TnefSignature;
    }

    /// <summary>
    /// Gets the legacy key.
    /// </summary>
    /// <returns>The current legacy key.</returns>
    public int GetLegacyKey()
    {
        return legacyKey;
    }

    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    public EmailRange GetSourceRange()
    {
        return sourceRange;
    }
}
