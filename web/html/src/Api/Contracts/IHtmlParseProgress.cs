using System;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlParseProgress"/>.</summary>
public interface IHtmlParseProgress
{
    /// <summary>Executes the GetStatus operation.</summary>
    public HtmlParseProgressStatus GetStatus();
    /// <summary>Executes the GetProcessedTokenCount operation.</summary>
    public int GetProcessedTokenCount();
}
