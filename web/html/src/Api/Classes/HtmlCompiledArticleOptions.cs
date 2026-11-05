using System;

namespace html;
/// <summary>Contains a validated immutable snapshot of article-extraction settings.</summary>
public class HtmlCompiledArticleOptions : IHtmlArticleConfiguration, IHtmlCompiledArticleOptionsApi
{
    internal HtmlArticleOptions options;

    internal HtmlCompiledArticleOptions(HtmlArticleOptions optionsValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        optionsValue.Validate();
        this.options = optionsValue.Copy();
    }

    /// <summary>Returns this validated immutable configuration.</summary>
    public HtmlCompiledArticleOptions Compile()
    {
        return this;
    }

    /// <summary>Gets the minimum readable content length.</summary>
    public int GetMinimumContentCharacters()
    {
        return this.options.GetMinimumContentCharacters();
    }

    /// <summary>Gets the minimum candidate length.</summary>
    public int GetMinimumCandidateCharacters()
    {
        return this.options.GetMinimumCandidateCharacters();
    }

    /// <summary>Gets the candidate score threshold.</summary>
    public int GetScoreThreshold()
    {
        return this.options.GetScoreThreshold();
    }

    /// <summary>Gets the maximum number of top candidates.</summary>
    public int GetMaximumTopCandidates()
    {
        return this.options.GetMaximumTopCandidates();
    }

    /// <summary>Reports whether unlikely candidates are removed.</summary>
    public bool GetRemoveUnlikelyCandidates()
    {
        return this.options.GetRemoveUnlikelyCandidates();
    }

    /// <summary>Reports whether class and identifier weights are used.</summary>
    public bool GetWeightClassesAndIds()
    {
        return this.options.GetWeightClassesAndIds();
    }

    /// <summary>Reports whether conditional cleanup is enabled.</summary>
    public bool GetCleanConditionally()
    {
        return this.options.GetCleanConditionally();
    }

    /// <summary>Reports whether sanitized HTML is produced.</summary>
    public bool GetProduceSanitizedHtml()
    {
        return this.options.GetProduceSanitizedHtml();
    }

    /// <summary>Reports whether plain text is produced.</summary>
    public bool GetProduceText()
    {
        return this.options.GetProduceText();
    }

    /// <summary>Reports whether Markdown is produced.</summary>
    public bool GetProduceMarkdown()
    {
        return this.options.GetProduceMarkdown();
    }

    /// <summary>Gets an immutable sanitizer policy snapshot.</summary>
    public HtmlCompiledSanitizerPolicy GetSanitizerPolicy()
    {
        return this.options.GetSanitizerPolicy();
    }

    /// <summary>Gets an immutable text-options snapshot.</summary>
    public HtmlCompiledTextOptions GetTextOptions()
    {
        return this.options.GetTextOptions().Compile();
    }

    /// <summary>Gets an immutable Markdown-options snapshot.</summary>
    public HtmlCompiledMarkdownOptions GetMarkdownOptions()
    {
        return this.options.GetMarkdownOptions().Compile();
    }

    /// <summary>Gets a defensive copy of the resource limits.</summary>
    public HtmlLimits GetLimits()
    {
        return this.options.GetLimits();
    }

    internal HtmlArticleOptions CreateOptions()
    {
        return this.options.Copy();
    }
}
