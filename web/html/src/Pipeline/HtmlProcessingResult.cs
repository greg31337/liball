using System;
using System.Text;

namespace html;

internal static class HtmlProcessingValidation
{
    internal static void ValidateStage(HtmlProcessingStage value,
        string parameterName)
    {
        if ((int)value < (int)HtmlProcessingStage.Decode
            || (int)value > (int)HtmlProcessingStage.Markdown)
        {
            throw new ArgumentOutOfRangeException(parameterName,
                (int)value, "Unknown processing stage.");
        }
    }

    internal static void ValidateStatus(HtmlProcessingStageStatus value,
        string parameterName)
    {
        if ((int)value < (int)HtmlProcessingStageStatus.NotRequested
            || (int)value > (int)HtmlProcessingStageStatus.Skipped)
        {
            throw new ArgumentOutOfRangeException(parameterName,
                (int)value, "Unknown processing stage status.");
        }
    }
}
