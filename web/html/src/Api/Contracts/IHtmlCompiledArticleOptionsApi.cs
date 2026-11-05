using System;

namespace html;
/// <summary>Exposes immutable compiled article-extraction settings.</summary>
public interface IHtmlCompiledArticleOptionsApi
{
    /// <summary>Gets the minimum readable content length.</summary>
    public int GetMinimumContentCharacters();
    /// <summary>Gets the minimum candidate length.</summary>
    public int GetMinimumCandidateCharacters();
    /// <summary>Gets the candidate score threshold.</summary>
    public int GetScoreThreshold();
    /// <summary>Gets the maximum number of top candidates.</summary>
    public int GetMaximumTopCandidates();
    /// <summary>Reports whether unlikely candidates are removed.</summary>
    public bool GetRemoveUnlikelyCandidates();
    /// <summary>Reports whether class and identifier weights are used.</summary>
    public bool GetWeightClassesAndIds();
    /// <summary>Reports whether conditional cleanup is enabled.</summary>
    public bool GetCleanConditionally();
    /// <summary>Reports whether sanitized HTML is produced.</summary>
    public bool GetProduceSanitizedHtml();
    /// <summary>Reports whether plain text is produced.</summary>
    public bool GetProduceText();
    /// <summary>Reports whether Markdown is produced.</summary>
    public bool GetProduceMarkdown();
    /// <summary>Gets an immutable sanitizer policy snapshot.</summary>
    public HtmlCompiledSanitizerPolicy GetSanitizerPolicy();
    /// <summary>Gets an immutable text-options snapshot.</summary>
    public HtmlCompiledTextOptions GetTextOptions();
    /// <summary>Gets an immutable Markdown-options snapshot.</summary>
    public HtmlCompiledMarkdownOptions GetMarkdownOptions();
    /// <summary>Gets a defensive copy of the resource limits.</summary>
    public HtmlLimits GetLimits();
}
