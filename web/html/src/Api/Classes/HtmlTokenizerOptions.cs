using System;
using System.Threading;

namespace html;
/// <summary>Configures incremental tokenization, source tracking, scripting behavior, and resource limits.</summary>
public class HtmlTokenizerOptions : IHtmlTokenizerOptions
{
    internal HtmlLimits limits;
    internal CancellationToken cancellationToken;
    internal bool trackSourceLocations;
    internal bool coalesceCharacterTokens;
    /// <summary>Creates tokenizer options initialized to the library defaults.</summary>
    public HtmlTokenizerOptions()
    {
        this.limits = HtmlLimits.CreateDefault();
        this.cancellationToken = CancellationToken.None;
        this.trackSourceLocations = true;
        this.coalesceCharacterTokens = true;
    }

    /// <summary>Creates an instance initialized with the library defaults.</summary>
    /// <returns>A new independently mutable tokenizer-options object initialized with the library defaults.</returns>
    internal static HtmlTokenizerOptions CreateDefault()
    {
        return new HtmlTokenizerOptions();
    }

    /// <summary>Creates conservative resource limits for processing untrusted input.</summary>
    /// <returns>A new tokenizer-options object using conservative untrusted-input limits.</returns>
    internal static HtmlTokenizerOptions CreateUntrusted()
    {
        HtmlTokenizerOptions options = new HtmlTokenizerOptions();
        options.limits = HtmlLimits.CreateUntrusted();
        return options;
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent tokenizer-options object containing the same settings.</returns>
    public HtmlTokenizerOptions Copy()
    {
        HtmlTokenizerOptions copy = new HtmlTokenizerOptions();
        copy.limits = this.limits.Copy();
        copy.cancellationToken = this.cancellationToken;
        copy.trackSourceLocations = this.trackSourceLocations;
        copy.coalesceCharacterTokens = this.coalesceCharacterTokens;
        return copy;
    }

    internal void Validate()
    {
        if (this.limits == null)
        {
            throw new InvalidOperationException("Tokenizer limits are required.");
        }

        this.limits.Validate();
    }

    /// <summary>Gets an independent copy of the configured resource limits.</summary>
    /// <returns>A mutable copy of the limits; changing it does not affect this object.</returns>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    /// <summary>Sets the limits used by subsequent operations.</summary>
    /// <param name = "value">The validated limits to copy; later changes to the supplied object do not affect these options.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetLimits(HtmlLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.limits = value.Copy();
    }

    /// <summary>Gets the token checked between bounded tokenizer work units.</summary>
    /// <returns>The configured cancellation token.</returns>
    public CancellationToken GetCancellationToken()
    {
        return this.cancellationToken;
    }

    /// <summary>Sets the cancellation token used by subsequent operations.</summary>
    /// <param name = "value">The token whose cancellation stops subsequent tokenization work.</param>
    public void SetCancellationToken(CancellationToken value)
    {
        this.cancellationToken = value;
    }

    /// <summary>Reports whether source positions are retained.</summary>
    /// <returns><c>true</c> when source-location retention is enabled; otherwise, <c>false</c>.</returns>
    public bool GetTrackSourceLocations()
    {
        return this.trackSourceLocations;
    }

    /// <summary>Controls whether source positions are retained.</summary>
    /// <param name = "value"><c>true</c> to retain character offsets, lines, and columns on emitted tokens; otherwise, <c>false</c>.</param>
    public void SetTrackSourceLocations(bool value)
    {
        this.trackSourceLocations = value;
    }

    /// <summary>Reports whether adjacent character data is combined into fewer output tokens.</summary>
    /// <returns><c>true</c> when adjacent character data is combined into fewer tokens; otherwise, <c>false</c>.</returns>
    public bool GetCoalesceCharacterTokens()
    {
        return this.coalesceCharacterTokens;
    }

    /// <summary>Controls whether adjacent character data is combined into fewer tokenizer tokens.</summary>
    /// <param name = "value"><c>true</c> to merge adjacent character data into fewer tokens; otherwise, <c>false</c> to preserve tokenizer emission boundaries.</param>
    public void SetCoalesceCharacterTokens(bool value)
    {
        this.coalesceCharacterTokens = value;
    }
}
