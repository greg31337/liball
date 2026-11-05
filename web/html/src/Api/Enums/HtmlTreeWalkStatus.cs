using System;
using System.Collections.Generic;

namespace html;
/// <summary>Identifies whether a tree walk completed, was stopped by the visitor, or exhausted its visit limit.</summary>
public enum HtmlTreeWalkStatus
{
    /// <summary>Signals that every node within the bound was visited.</summary>
    Completed = 0,
    /// <summary>Signals that a visitor returned <c>false</c> and stopped traversal.</summary>
    Stopped = 1,
    /// <summary>Signals that traversal reached its visit limit.</summary>
    LimitExceeded = 2
}
