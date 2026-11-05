using System;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Identifies whether incremental parsing needs input, made progress, completed, or stopped.</summary>
public enum HtmlParseProgressStatus
{
    /// <summary>Signals that the parser needs another input chunk.</summary>
    NeedMoreInput = 0,
    /// <summary>Signals that one token was accepted and more work may remain.</summary>
    TokenProcessed = 1,
    /// <summary>Signals that parsing completed and a final result is available.</summary>
    Complete = 2,
    /// <summary>Signals that parsing stopped at a configured resource limit.</summary>
    LimitExceeded = 3,
    /// <summary>Signals that parsing stopped because cancellation was requested.</summary>
    Cancelled = 4,
    /// <summary>Signals that parsing stopped because its input source or tokenizer failed.</summary>
    Failed = 5
}
