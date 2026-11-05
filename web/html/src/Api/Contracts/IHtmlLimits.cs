using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlLimits"/>.</summary>
public interface IHtmlLimits
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlLimits Copy();
    /// <summary>Executes the GetMaximumInputBytes operation.</summary>
    public int GetMaximumInputBytes();
    /// <summary>Executes the SetMaximumInputBytes operation.</summary>
    public void SetMaximumInputBytes(int value);
    /// <summary>Executes the GetMaximumInputCharacters operation.</summary>
    public int GetMaximumInputCharacters();
    /// <summary>Executes the SetMaximumInputCharacters operation.</summary>
    public void SetMaximumInputCharacters(int value);
    /// <summary>Executes the GetMaximumTokens operation.</summary>
    public int GetMaximumTokens();
    /// <summary>Executes the SetMaximumTokens operation.</summary>
    public void SetMaximumTokens(int value);
    /// <summary>Executes the GetMaximumAttributesPerTag operation.</summary>
    public int GetMaximumAttributesPerTag();
    /// <summary>Executes the SetMaximumAttributesPerTag operation.</summary>
    public void SetMaximumAttributesPerTag(int value);
    /// <summary>Executes the GetMaximumTokenCharacters operation.</summary>
    public int GetMaximumTokenCharacters();
    /// <summary>Executes the SetMaximumTokenCharacters operation.</summary>
    public void SetMaximumTokenCharacters(int value);
    /// <summary>Executes the GetMaximumNameCharacters operation.</summary>
    public int GetMaximumNameCharacters();
    /// <summary>Executes the SetMaximumNameCharacters operation.</summary>
    public void SetMaximumNameCharacters(int value);
    /// <summary>Executes the GetMaximumAttributeValueCharacters operation.</summary>
    public int GetMaximumAttributeValueCharacters();
    /// <summary>Executes the SetMaximumAttributeValueCharacters operation.</summary>
    public void SetMaximumAttributeValueCharacters(int value);
    /// <summary>Executes the GetMaximumNodes operation.</summary>
    public int GetMaximumNodes();
    /// <summary>Executes the SetMaximumNodes operation.</summary>
    public void SetMaximumNodes(int value);
    /// <summary>Executes the GetMaximumDepth operation.</summary>
    public int GetMaximumDepth();
    /// <summary>Executes the SetMaximumDepth operation.</summary>
    public void SetMaximumDepth(int value);
    /// <summary>Executes the GetMaximumSelectorComplexity operation.</summary>
    public int GetMaximumSelectorComplexity();
    /// <summary>Executes the SetMaximumSelectorComplexity operation.</summary>
    public void SetMaximumSelectorComplexity(int value);
    /// <summary>Executes the GetMaximumSelectorResults operation.</summary>
    public int GetMaximumSelectorResults();
    /// <summary>Executes the SetMaximumSelectorResults operation.</summary>
    public void SetMaximumSelectorResults(int value);
    /// <summary>Executes the GetMaximumHasWork operation.</summary>
    public int GetMaximumHasWork();
    /// <summary>Executes the SetMaximumHasWork operation.</summary>
    public void SetMaximumHasWork(int value);
    /// <summary>Executes the GetMaximumCssTokens operation.</summary>
    public int GetMaximumCssTokens();
    /// <summary>Executes the SetMaximumCssTokens operation.</summary>
    public void SetMaximumCssTokens(int value);
    /// <summary>Executes the GetMaximumCssDeclarations operation.</summary>
    public int GetMaximumCssDeclarations();
    /// <summary>Executes the SetMaximumCssDeclarations operation.</summary>
    public void SetMaximumCssDeclarations(int value);
    /// <summary>Executes the GetMaximumUrlCharacters operation.</summary>
    public int GetMaximumUrlCharacters();
    /// <summary>Executes the SetMaximumUrlCharacters operation.</summary>
    public void SetMaximumUrlCharacters(int value);
    /// <summary>Executes the GetMaximumDataUriBytes operation.</summary>
    public int GetMaximumDataUriBytes();
    /// <summary>Executes the SetMaximumDataUriBytes operation.</summary>
    public void SetMaximumDataUriBytes(int value);
    /// <summary>Executes the GetMaximumTableCells operation.</summary>
    public int GetMaximumTableCells();
    /// <summary>Executes the SetMaximumTableCells operation.</summary>
    public void SetMaximumTableCells(int value);
    /// <summary>Executes the GetMaximumJsonDepth operation.</summary>
    public int GetMaximumJsonDepth();
    /// <summary>Executes the SetMaximumJsonDepth operation.</summary>
    public void SetMaximumJsonDepth(int value);
    /// <summary>Executes the GetMaximumJsonProperties operation.</summary>
    public int GetMaximumJsonProperties();
    /// <summary>Executes the SetMaximumJsonProperties operation.</summary>
    public void SetMaximumJsonProperties(int value);
    /// <summary>Executes the GetMaximumVisits operation.</summary>
    public int GetMaximumVisits();
    /// <summary>Executes the SetMaximumVisits operation.</summary>
    public void SetMaximumVisits(int value);
    /// <summary>Executes the GetMaximumDiagnostics operation.</summary>
    public int GetMaximumDiagnostics();
    /// <summary>Executes the SetMaximumDiagnostics operation.</summary>
    public void SetMaximumDiagnostics(int value);
    /// <summary>Executes the GetMaximumOutputCharacters operation.</summary>
    public int GetMaximumOutputCharacters();
    /// <summary>Executes the SetMaximumOutputCharacters operation.</summary>
    public void SetMaximumOutputCharacters(int value);
}
