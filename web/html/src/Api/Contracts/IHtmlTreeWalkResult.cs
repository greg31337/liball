using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlTreeWalkResult"/>.</summary>
public interface IHtmlTreeWalkResult
{
    /// <summary>Executes the GetStatus operation.</summary>
    public HtmlTreeWalkStatus GetStatus();
    /// <summary>Executes the GetVisitCount operation.</summary>
    public int GetVisitCount();
    /// <summary>Executes the IsCompleted operation.</summary>
    public bool IsCompleted();
    /// <summary>Executes the WasStopped operation.</summary>
    public bool WasStopped();
    /// <summary>Executes the WasLimitExceeded operation.</summary>
    public bool WasLimitExceeded();
}
