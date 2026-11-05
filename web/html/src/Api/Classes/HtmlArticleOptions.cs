using System;

namespace html;
/// <summary>Configures readability-based article extraction and its resource limits.</summary>
public class HtmlArticleOptions : IHtmlArticleOptions, IHtmlArticleConfiguration
{
    internal int minimumContentCharacters;
    internal int minimumCandidateCharacters;
    internal int scoreThreshold;
    internal int maximumTopCandidates;
    internal bool removeUnlikelyCandidates;
    internal bool weightClassesAndIds;
    internal bool cleanConditionally;
    internal bool produceSanitizedHtml;
    internal bool produceText;
    internal bool produceMarkdown;
    internal HtmlCompiledSanitizerPolicy sanitizerPolicy;
    internal HtmlTextOptions textOptions;
    internal HtmlMarkdownOptions markdownOptions;
    internal HtmlLimits limits;
    /// <summary>Creates article-extraction options initialized to the library defaults.</summary>
    public HtmlArticleOptions()
    {
        this.minimumContentCharacters = 140;
        this.minimumCandidateCharacters = 25;
        this.scoreThreshold = 20;
        this.maximumTopCandidates = 5;
        this.removeUnlikelyCandidates = true;
        this.weightClassesAndIds = true;
        this.cleanConditionally = true;
        this.produceSanitizedHtml = false;
        this.produceText = false;
        this.produceMarkdown = false;
        this.sanitizerPolicy = HtmlSanitizerPolicy.CreateHtmlOnly().Compile();
        this.textOptions = new HtmlTextOptions();
        this.markdownOptions = new HtmlMarkdownOptions();
        this.limits = HtmlLimits.CreateDefault();
    }

    /// <summary>Creates an instance initialized with the library defaults.</summary>
    /// <returns>A new independently mutable options object initialized with the default article-extraction thresholds.</returns>
    internal static HtmlArticleOptions CreateDefault()
    {
        return new HtmlArticleOptions();
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent options object containing the same article-extraction settings.</returns>
    public HtmlArticleOptions Copy()
    {
        HtmlArticleOptions copy = new HtmlArticleOptions();
        copy.minimumContentCharacters = this.minimumContentCharacters;
        copy.minimumCandidateCharacters = this.minimumCandidateCharacters;
        copy.scoreThreshold = this.scoreThreshold;
        copy.maximumTopCandidates = this.maximumTopCandidates;
        copy.removeUnlikelyCandidates = this.removeUnlikelyCandidates;
        copy.weightClassesAndIds = this.weightClassesAndIds;
        copy.cleanConditionally = this.cleanConditionally;
        copy.produceSanitizedHtml = this.produceSanitizedHtml;
        copy.produceText = this.produceText;
        copy.produceMarkdown = this.produceMarkdown;
        copy.sanitizerPolicy = CopyPolicy(this.sanitizerPolicy);
        copy.textOptions = this.textOptions.Copy();
        copy.markdownOptions = this.markdownOptions.Copy();
        copy.limits = this.limits.Copy();
        return copy;
    }

    /// <summary>Creates a validated immutable snapshot for concurrent reuse.</summary>
    /// <returns>An immutable compiled article configuration.</returns>
    public HtmlCompiledArticleOptions Compile()
    {
        return new HtmlCompiledArticleOptions(this);
    }

    internal void Validate()
    {
        if (this.minimumContentCharacters < 0)
        {
            throw new InvalidOperationException("The minimum article content length cannot be negative.");
        }

        if (this.minimumCandidateCharacters < 0)
        {
            throw new InvalidOperationException("The minimum article candidate length cannot be negative.");
        }

        if (this.scoreThreshold < 0)
        {
            throw new InvalidOperationException("The article score threshold cannot be negative.");
        }

        if (this.maximumTopCandidates < 1)
        {
            throw new InvalidOperationException("The maximum top article candidate count must be positive.");
        }

        if (this.sanitizerPolicy == null || this.textOptions == null
            || this.markdownOptions == null || this.limits == null)
        {
            throw new InvalidOperationException("Article options contain a missing required value.");
        }

        this.textOptions.Validate();
        this.markdownOptions.Validate();
        this.limits.Validate();
    }

    /// <summary>Gets the minimum text length required for a successful extracted article.</summary>
    /// <returns>The nonnegative required character count.</returns>
    public int GetMinimumContentCharacters()
    {
        return this.minimumContentCharacters;
    }

    /// <summary>Sets the minimum content characters used by subsequent operations.</summary>
    /// <param name = "value">The nonnegative minimum number of text characters accepted as article content.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void SetMinimumContentCharacters(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", value, "The minimum article content length cannot be negative.");
        }

        this.minimumContentCharacters = value;
    }

    /// <summary>Gets the minimum text length for a node to participate in readability scoring.</summary>
    /// <returns>The nonnegative candidate character count.</returns>
    public int GetMinimumCandidateCharacters()
    {
        return this.minimumCandidateCharacters;
    }

    /// <summary>Sets the minimum candidate characters used by subsequent operations.</summary>
    /// <param name = "value">The nonnegative minimum text length for a readability candidate.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void SetMinimumCandidateCharacters(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", value, "The minimum article candidate length cannot be negative.");
        }

        this.minimumCandidateCharacters = value;
    }

    /// <summary>Gets the minimum readability score accepted by the extraction preflight.</summary>
    /// <returns>The nonnegative score threshold.</returns>
    public int GetScoreThreshold()
    {
        return this.scoreThreshold;
    }

    /// <summary>Sets the score threshold used by subsequent operations.</summary>
    /// <param name = "value">The nonnegative readability score required before extraction proceeds.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void SetScoreThreshold(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", value, "The article score threshold cannot be negative.");
        }

        this.scoreThreshold = value;
    }

    /// <summary>Gets how many of the highest-scoring candidate nodes may contribute to article assembly.</summary>
    /// <returns>The positive maximum candidate count.</returns>
    public int GetMaximumTopCandidates()
    {
        return this.maximumTopCandidates;
    }

    /// <summary>Sets the maximum top candidates used by subsequent operations.</summary>
    /// <param name = "value">The positive maximum number of top-scoring candidates to retain.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void SetMaximumTopCandidates(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value", value, "The maximum top article candidate count must be positive.");
        }

        this.maximumTopCandidates = value;
    }

    /// <summary>Reports whether unlikely article candidates are removed.</summary>
    /// <returns><c>true</c> when unlikely article candidates are removed; otherwise, <c>false</c>.</returns>
    public bool GetRemoveUnlikelyCandidates()
    {
        return this.removeUnlikelyCandidates;
    }

    /// <summary>Controls whether readability extraction removes unlikely article candidates.</summary>
    /// <param name = "value"><c>true</c> to discard nodes whose names and class or ID hints commonly identify navigation, advertising, and other non-article content.</param>
    public void SetRemoveUnlikelyCandidates(bool value)
    {
        this.removeUnlikelyCandidates = value;
    }

    /// <summary>Reports whether class and ID hints affect readability scores.</summary>
    /// <returns><c>true</c> when class and ID hints affect readability scores; otherwise, <c>false</c>.</returns>
    public bool GetWeightClassesAndIds()
    {
        return this.weightClassesAndIds;
    }

    /// <summary>Controls whether class and ID hints affect readability scores.</summary>
    /// <param name = "value"><c>true</c> to add or subtract readability weight from recognized class and ID words.</param>
    public void SetWeightClassesAndIds(bool value)
    {
        this.weightClassesAndIds = value;
    }

    /// <summary>Reports whether low-quality containers are removed conditionally.</summary>
    /// <returns><c>true</c> when low-quality article containers are removed conditionally; otherwise, <c>false</c>.</returns>
    public bool GetCleanConditionally()
    {
        return this.cleanConditionally;
    }

    /// <summary>Controls whether readability cleanup removes low-quality containers conditionally.</summary>
    /// <param name = "value"><c>true</c> to remove weak tables, lists, forms, and containers using density and content heuristics.</param>
    public void SetCleanConditionally(bool value)
    {
        this.cleanConditionally = value;
    }

    /// <summary>Reports whether extraction produces sanitized HTML.</summary>
    /// <returns><c>true</c> when sanitized HTML is requested; otherwise, <c>false</c>.</returns>
    public bool GetProduceSanitizedHtml()
    {
        return this.produceSanitizedHtml;
    }

    /// <summary>Controls whether extraction sanitizes and serializes article HTML.</summary>
    /// <param name = "value"><c>true</c> to produce sanitized HTML; otherwise, <c>false</c>.</param>
    public void SetProduceSanitizedHtml(bool value)
    {
        this.produceSanitizedHtml = value;
    }

    /// <summary>Reports whether extraction produces plain text.</summary>
    /// <returns><c>true</c> when plain text is requested; otherwise, <c>false</c>.</returns>
    public bool GetProduceText()
    {
        return this.produceText;
    }

    /// <summary>Controls whether extraction produces plain text from sanitized content.</summary>
    /// <param name = "value"><c>true</c> to produce plain text; otherwise, <c>false</c>.</param>
    public void SetProduceText(bool value)
    {
        this.produceText = value;
    }

    /// <summary>Reports whether extraction produces Markdown.</summary>
    /// <returns><c>true</c> when Markdown is requested; otherwise, <c>false</c>.</returns>
    public bool GetProduceMarkdown()
    {
        return this.produceMarkdown;
    }

    /// <summary>Controls whether extraction produces Markdown from sanitized content.</summary>
    /// <param name = "value"><c>true</c> to produce Markdown; otherwise, <c>false</c>.</param>
    public void SetProduceMarkdown(bool value)
    {
        this.produceMarkdown = value;
    }

    /// <summary>Gets an immutable copy of the policy used for optional sanitized outputs.</summary>
    /// <returns>An independent compiled sanitizer policy.</returns>
    public HtmlCompiledSanitizerPolicy GetSanitizerPolicy()
    {
        return CopyPolicy(this.sanitizerPolicy);
    }

    /// <summary>Sets the policy used for optional sanitized outputs.</summary>
    /// <param name = "value">The compiled sanitizer policy to copy.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetSanitizerPolicy(HtmlCompiledSanitizerPolicy value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.sanitizerPolicy = CopyPolicy(value);
    }

    /// <summary>Gets an independent copy of the optional plain-text settings.</summary>
    /// <returns>A mutable copy of the configured text settings.</returns>
    public HtmlTextOptions GetTextOptions()
    {
        return this.textOptions.Copy();
    }

    /// <summary>Sets the options used for optional plain-text output.</summary>
    /// <param name = "value">The validated text settings to copy.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetTextOptions(HtmlTextOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.textOptions = value.Copy();
        this.textOptions.SetLimits(this.limits);
    }

    /// <summary>Gets an independent copy of the optional Markdown settings.</summary>
    /// <returns>A mutable copy of the configured Markdown settings.</returns>
    public HtmlMarkdownOptions GetMarkdownOptions()
    {
        return this.markdownOptions.Copy();
    }

    /// <summary>Sets the options used for optional Markdown output.</summary>
    /// <param name = "value">The validated Markdown settings to copy.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetMarkdownOptions(HtmlMarkdownOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.markdownOptions = value.Copy();
        this.markdownOptions.SetLimits(this.limits);
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
        this.textOptions.SetLimits(this.limits);
        this.markdownOptions.SetLimits(this.limits);
    }

    internal static HtmlCompiledSanitizerPolicy CopyPolicy(
        HtmlCompiledSanitizerPolicy value)
    {
        return value.CreatePolicy().Compile();
    }
}
