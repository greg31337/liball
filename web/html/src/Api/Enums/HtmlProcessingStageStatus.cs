using System;
using System.Text;

namespace html;
/// <summary>Reports whether a requested pipeline stage ran and produced complete output.</summary>
public enum HtmlProcessingStageStatus
{
    /// <summary>The caller did not request this stage.</summary>
    NotRequested = 0,
    /// <summary>The stage completed successfully.</summary>
    Succeeded = 1,
    /// <summary>The stage produced a safe partial result but did not complete.</summary>
    Incomplete = 2,
    /// <summary>The stage failed and produced no usable output.</summary>
    Failed = 3,
    /// <summary>The stage was requested but skipped because a prerequisite did not succeed.</summary>
    Skipped = 4
}
