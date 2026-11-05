using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents an address.
/// </summary>
public class EmailAddress : IEmailAddressContract
{
    string? displayName;
    string localPart;
    string domain;
    byte[] rawBytes;
    byte[] rawLocalPartBytes;
    byte[] rawDomainBytes;
    EmailSourcePosition sourcePosition;
    bool domainLiteral;
    bool internationalized;
    bool obsoleteSyntax;
    List<string> sourceRoute;
    /// <summary>
    /// Creates a new address.
    /// </summary>
    /// <param name = "displayName">The display name used by the operation.</param>
    /// <param name = "localPart">The local part used by the operation.</param>
    /// <param name = "domain">The domain used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "rawLocalPartBytes">The source bytes or data to process.</param>
    /// <param name = "rawDomainBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "domainLiteral">Whether to enable the domain literal behavior.</param>
    /// <param name = "internationalized">Whether to enable the internationalized behavior.</param>
    /// <param name = "obsoleteSyntax">Whether to enable the obsolete syntax behavior.</param>
    /// <param name = "sourceRoute">The source route used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailAddress(string? displayName, string? localPart, string? domain, byte[] rawBytes, byte[] rawLocalPartBytes, byte[] rawDomainBytes, EmailSourcePosition sourcePosition, bool domainLiteral, bool internationalized, bool obsoleteSyntax, List<string> sourceRoute)
    {
        if (localPart == null || localPart.Length == 0)
        {
            throw new ArgumentException("An address local part cannot be empty.", "localPart");
        }

        if (domain == null || domain.Length == 0)
        {
            throw new ArgumentException("An address domain cannot be empty.", "domain");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (rawLocalPartBytes == null)
        {
            throw new ArgumentNullException("rawLocalPartBytes");
        }

        if (rawDomainBytes == null)
        {
            throw new ArgumentNullException("rawDomainBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        if (sourceRoute == null)
        {
            throw new ArgumentNullException("sourceRoute");
        }

        this.displayName = displayName;
        this.localPart = localPart;
        this.domain = domain;
        this.rawBytes = Copy(rawBytes);
        this.rawLocalPartBytes = Copy(rawLocalPartBytes);
        this.rawDomainBytes = Copy(rawDomainBytes);
        this.sourcePosition = sourcePosition;
        this.domainLiteral = domainLiteral;
        this.internationalized = internationalized;
        this.obsoleteSyntax = obsoleteSyntax;
        this.sourceRoute = new List<string>(sourceRoute);
    }

    /// <summary>
    /// Determines whether the display name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the display name is available; otherwise, <c>false</c>.</returns>
    public bool HasDisplayName()
    {
        return displayName != null;
    }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <returns>The current display name.</returns>
    public string? GetDisplayName()
    {
        return displayName;
    }

    /// <summary>
    /// Gets the local part.
    /// </summary>
    /// <returns>The current local part.</returns>
    public string GetLocalPart()
    {
        return localPart;
    }

    /// <summary>
    /// Gets the domain.
    /// </summary>
    /// <returns>The current domain.</returns>
    public string GetDomain()
    {
        return domain;
    }

    /// <summary>
    /// Determines whether the current value or state is domain literal.
    /// </summary>
    /// <returns><c>true</c> when the value or state is domain literal; otherwise, <c>false</c>.</returns>
    public bool IsDomainLiteral()
    {
        return domainLiteral;
    }

    /// <summary>
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    public bool IsInternationalized()
    {
        return internationalized;
    }

    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    public bool UsesObsoleteSyntax()
    {
        return obsoleteSyntax;
    }

    /// <summary>
    /// Determines whether the source route is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source route is available; otherwise, <c>false</c>.</returns>
    public bool HasSourceRoute()
    {
        return sourceRoute.Count > 0;
    }

    /// <summary>
    /// Gets the source route count.
    /// </summary>
    /// <returns>The current source route count.</returns>
    public int GetSourceRouteCount()
    {
        return sourceRoute.Count;
    }

    /// <summary>
    /// Gets the source route at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested source route.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetSourceRouteAt(int index)
    {
        if (index < 0 || index >= sourceRoute.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return sourceRoute[index];
    }

    /// <summary>
    /// Gets the source route.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current source route.</returns>
    public List<string> GetSourceRoute()
    {
        return new List<string>(sourceRoute);
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the raw local part bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw local part bytes.</returns>
    public byte[] GetRawLocalPartBytes()
    {
        return Copy(rawLocalPartBytes);
    }

    /// <summary>
    /// Gets the raw domain bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw domain bytes.</returns>
    public byte[] GetRawDomainBytes()
    {
        return Copy(rawDomainBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Gets the ascii domain.
    /// </summary>
    /// <returns>The current ascii domain.</returns>
    public string GetAsciiDomain()
    {
        if (domainLiteral)
        {
            return domain;
        }

        return new EmailAddressIdn().ToAscii(domain);
    }

    /// <summary>
    /// Gets the unicode domain.
    /// </summary>
    /// <returns>The current unicode domain.</returns>
    public string GetUnicodeDomain()
    {
        if (domainLiteral)
        {
            return domain;
        }

        return new EmailAddressIdn().ToUnicode(domain);
    }

    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    public EmailAddressFormatResult Format(EmailAddressFormatOptions options)
    {
        return new EmailAddressFormatter().FormatWithAddressAndOptions(this, options);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
