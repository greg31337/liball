using System;
using System.Text;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlProcessingStageTiming"/>.</summary>
public interface IHtmlProcessingStageTiming
{
    /// <summary>Executes the GetStage operation.</summary>
    public HtmlProcessingStage GetStage();
    /// <summary>Executes the GetStatus operation.</summary>
    public HtmlProcessingStageStatus GetStatus();
    /// <summary>Executes the GetElapsedTicks operation.</summary>
    public long GetElapsedTicks();
    /// <summary>Executes the GetElapsed operation.</summary>
    public TimeSpan GetElapsed();
}
