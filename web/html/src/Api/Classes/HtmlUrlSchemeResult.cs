using System;
using System.Threading;

namespace html;
/// <summary>Reports a detected URL scheme and any deobfuscation or ambiguity flags.</summary>
public class HtmlUrlSchemeResult : IHtmlUrlSchemeResult
{
    internal string deobfuscatedValue;
    internal string scheme;
    internal bool hasScheme;
    internal bool deobfuscated;
    internal bool complete;
    internal bool limitExceeded;
    internal HtmlUrlSchemeResult(string deobfuscatedValue, string scheme, bool hasScheme, bool deobfuscated, bool complete, bool limitExceeded)
    {
        this.deobfuscatedValue = deobfuscatedValue;
        this.scheme = scheme;
        this.hasScheme = hasScheme;
        this.deobfuscated = deobfuscated;
        this.complete = complete;
        this.limitExceeded = limitExceeded;
    }

    internal static HtmlUrlSchemeResult Create(string deobfuscatedValue, string scheme, bool hasScheme, bool deobfuscated, bool complete, bool limitExceeded)
    {
        if (deobfuscatedValue == null)
        {
            throw new ArgumentNullException("deobfuscatedValue");
        }

        if (scheme == null)
        {
            throw new ArgumentNullException("scheme");
        }

        if (hasScheme && scheme.Length == 0 || !hasScheme && scheme.Length != 0)
        {
            throw new ArgumentException("URL scheme presence must agree with its value.", "scheme");
        }

        if (complete == limitExceeded)
        {
            throw new ArgumentException("URL scheme completion must be the inverse of its limit state.", "complete");
        }

        return new HtmlUrlSchemeResult(deobfuscatedValue, scheme, hasScheme, deobfuscated, complete, limitExceeded);
    }

    /// <summary>Gets the inspected text after decoding character references and removing security-ignorable ASCII characters.</summary>
    /// <returns>The deobfuscated input, or an empty string when processing stopped at a resource limit.</returns>
    public string GetDeobfuscatedValue()
    {
        return this.deobfuscatedValue;
    }

    /// <summary>Reports whether the deobfuscated text begins with a syntactically valid URL scheme and colon.</summary>
    /// <returns><c>true</c> when a syntactically valid scheme was found; otherwise, <c>false</c>.</returns>
    public bool HasScheme()
    {
        return this.hasScheme;
    }

    /// <summary>Gets the detected URL scheme in ASCII lowercase without its trailing colon.</summary>
    /// <returns>The lowercase scheme, or an empty string when no scheme was found or processing was incomplete.</returns>
    public string GetScheme()
    {
        return this.scheme;
    }

    /// <summary>Reports whether ignored control or whitespace characters were removed while reading the scheme.</summary>
    /// <returns><c>true</c> when ignored characters were removed from the scheme; otherwise, <c>false</c>.</returns>
    public bool WasDeobfuscated()
    {
        return this.deobfuscated;
    }

    /// <summary>Reports whether the operation produced a complete result.</summary>
    /// <returns><c>true</c> when processing completed without truncation; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return this.complete;
    }

    /// <summary>Reports whether processing exceeded a configured resource limit.</summary>
    /// <returns><c>true</c> when a configured limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.limitExceeded;
    }
}
