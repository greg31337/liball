using System;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Reports one incremental parser step and any available completed result.</summary>
public class HtmlParseProgress : IHtmlParseProgress
{
    internal HtmlParseProgressStatus status;
    internal int processedTokenCount;
    internal HtmlParseProgress(HtmlParseProgressStatus status, int processedTokenCount)
    {
        this.status = status;
        this.processedTokenCount = processedTokenCount;
    }

    /// <summary>Gets whether parsing needs input, made progress, completed, or stopped.</summary>
    /// <returns>The incremental parser status.</returns>
    public HtmlParseProgressStatus GetStatus()
    {
        return this.status;
    }

    /// <summary>Gets how many tokenizer outputs the parser consumed during this read step.</summary>
    /// <returns>The number of processed tokens.</returns>
    public int GetProcessedTokenCount()
    {
        return this.processedTokenCount;
    }
}
