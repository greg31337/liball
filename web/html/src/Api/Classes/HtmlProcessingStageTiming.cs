using System;
using System.Text;

namespace html;
/// <summary>Stores the elapsed duration recorded for one pipeline stage.</summary>
public class HtmlProcessingStageTiming : IHtmlProcessingStageTiming
{
    internal HtmlProcessingStage stage;
    internal HtmlProcessingStageStatus status;
    internal long elapsedTicks;
    internal HtmlProcessingStageTiming(HtmlProcessingStage stageValue, HtmlProcessingStageStatus statusValue, long elapsedTicksValue)
    {
        HtmlProcessingValidation.ValidateStage(stageValue, "stageValue");
        HtmlProcessingValidation.ValidateStatus(statusValue, "statusValue");
        if (elapsedTicksValue < 0)
        {
            throw new ArgumentOutOfRangeException("elapsedTicksValue");
        }

        this.stage = stageValue;
        this.status = statusValue;
        this.elapsedTicks = elapsedTicksValue;
    }

    /// <summary>Gets the pipeline stage represented by this timing record.</summary>
    /// <returns>The decode, parse, extraction, sanitization, serialization, or conversion stage.</returns>
    public HtmlProcessingStage GetStage()
    {
        return this.stage;
    }

    /// <summary>Gets whether this pipeline stage was skipped, completed, or stopped early.</summary>
    /// <returns>The final stage status.</returns>
    public HtmlProcessingStageStatus GetStatus()
    {
        return this.status;
    }

    /// <summary>Gets the measured stage duration in <see cref = "TimeSpan"/> ticks.</summary>
    /// <returns>The nonnegative duration, where each tick is 100 nanoseconds.</returns>
    public long GetElapsedTicks()
    {
        return this.elapsedTicks;
    }

    /// <summary>Gets the measured stage duration as a time interval.</summary>
    /// <returns>A nonnegative duration created from <see cref = "GetElapsedTicks"/>.</returns>
    public TimeSpan GetElapsed()
    {
        return TimeSpan.FromTicks(this.elapsedTicks);
    }
}
