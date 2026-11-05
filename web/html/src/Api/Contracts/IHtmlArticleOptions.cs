using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlArticleOptions"/>.</summary>
public interface IHtmlArticleOptions
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlArticleOptions Copy();
    /// <summary>Executes the GetMinimumContentCharacters operation.</summary>
    public int GetMinimumContentCharacters();
    /// <summary>Executes the SetMinimumContentCharacters operation.</summary>
    public void SetMinimumContentCharacters(int value);
    /// <summary>Executes the GetMinimumCandidateCharacters operation.</summary>
    public int GetMinimumCandidateCharacters();
    /// <summary>Executes the SetMinimumCandidateCharacters operation.</summary>
    public void SetMinimumCandidateCharacters(int value);
    /// <summary>Executes the GetScoreThreshold operation.</summary>
    public int GetScoreThreshold();
    /// <summary>Executes the SetScoreThreshold operation.</summary>
    public void SetScoreThreshold(int value);
    /// <summary>Executes the GetMaximumTopCandidates operation.</summary>
    public int GetMaximumTopCandidates();
    /// <summary>Executes the SetMaximumTopCandidates operation.</summary>
    public void SetMaximumTopCandidates(int value);
    /// <summary>Executes the GetRemoveUnlikelyCandidates operation.</summary>
    public bool GetRemoveUnlikelyCandidates();
    /// <summary>Executes the SetRemoveUnlikelyCandidates operation.</summary>
    public void SetRemoveUnlikelyCandidates(bool value);
    /// <summary>Executes the GetWeightClassesAndIds operation.</summary>
    public bool GetWeightClassesAndIds();
    /// <summary>Executes the SetWeightClassesAndIds operation.</summary>
    public void SetWeightClassesAndIds(bool value);
    /// <summary>Executes the GetCleanConditionally operation.</summary>
    public bool GetCleanConditionally();
    /// <summary>Executes the SetCleanConditionally operation.</summary>
    public void SetCleanConditionally(bool value);
    /// <summary>Reports whether extraction produces sanitized HTML.</summary>
    public bool GetProduceSanitizedHtml();
    /// <summary>Controls whether extraction produces sanitized HTML.</summary>
    public void SetProduceSanitizedHtml(bool value);
    /// <summary>Reports whether extraction produces plain text.</summary>
    public bool GetProduceText();
    /// <summary>Controls whether extraction produces plain text.</summary>
    public void SetProduceText(bool value);
    /// <summary>Reports whether extraction produces Markdown.</summary>
    public bool GetProduceMarkdown();
    /// <summary>Controls whether extraction produces Markdown.</summary>
    public void SetProduceMarkdown(bool value);
    /// <summary>Gets the optional-output sanitizer policy.</summary>
    public HtmlCompiledSanitizerPolicy GetSanitizerPolicy();
    /// <summary>Sets the optional-output sanitizer policy.</summary>
    public void SetSanitizerPolicy(HtmlCompiledSanitizerPolicy value);
    /// <summary>Gets the optional plain-text settings.</summary>
    public HtmlTextOptions GetTextOptions();
    /// <summary>Sets the optional plain-text settings.</summary>
    public void SetTextOptions(HtmlTextOptions value);
    /// <summary>Gets the optional Markdown settings.</summary>
    public HtmlMarkdownOptions GetMarkdownOptions();
    /// <summary>Sets the optional Markdown settings.</summary>
    public void SetMarkdownOptions(HtmlMarkdownOptions value);
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
    /// <summary>Executes the SetLimits operation.</summary>
    public void SetLimits(HtmlLimits value);
}
