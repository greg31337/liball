using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlTokenizerOptions"/>.</summary>
public interface IHtmlTokenizerOptions
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlTokenizerOptions Copy();
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
    /// <summary>Executes the SetLimits operation.</summary>
    public void SetLimits(HtmlLimits value);
    /// <summary>Executes the GetCancellationToken operation.</summary>
    public CancellationToken GetCancellationToken();
    /// <summary>Executes the SetCancellationToken operation.</summary>
    public void SetCancellationToken(CancellationToken value);
    /// <summary>Executes the GetTrackSourceLocations operation.</summary>
    public bool GetTrackSourceLocations();
    /// <summary>Executes the SetTrackSourceLocations operation.</summary>
    public void SetTrackSourceLocations(bool value);
    /// <summary>Executes the GetCoalesceCharacterTokens operation.</summary>
    public bool GetCoalesceCharacterTokens();
    /// <summary>Executes the SetCoalesceCharacterTokens operation.</summary>
    public void SetCoalesceCharacterTokens(bool value);
}
