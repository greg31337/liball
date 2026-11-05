using System;
using System.Collections.Generic;

namespace html;
/// <summary>Reports whether a tree walk completed, stopped early, or reached its visit limit.</summary>
public class HtmlTreeWalkResult : IHtmlTreeWalkResult
{
    internal HtmlTreeWalkStatus status;
    internal int visitCount;
    internal HtmlTreeWalkResult(HtmlTreeWalkStatus status, int visitCount)
    {
        this.status = status;
        this.visitCount = visitCount;
    }

    internal static HtmlTreeWalkResult Create(HtmlTreeWalkStatus status, int visitCount)
    {
        if ((int)status < (int)HtmlTreeWalkStatus.Completed || (int)status > (int)HtmlTreeWalkStatus.LimitExceeded)
        {
            throw new ArgumentOutOfRangeException("status", (int)status, "Unknown tree walk status.");
        }

        if (visitCount < 0)
        {
            throw new ArgumentOutOfRangeException("visitCount", visitCount, "A tree walk visit count cannot be negative.");
        }

        return new HtmlTreeWalkResult(status, visitCount);
    }

    /// <summary>Gets why the tree walk ended.</summary>
    /// <returns>Completed, stopped by the callback, or stopped by the visit limit.</returns>
    public HtmlTreeWalkStatus GetStatus()
    {
        return this.status;
    }

    /// <summary>Gets how many enter and exit callbacks consumed the walk budget.</summary>
    /// <returns>The number of visits performed before the walk ended.</returns>
    public int GetVisitCount()
    {
        return this.visitCount;
    }

    /// <summary>Reports whether the tree walk visited the requested subtree without stopping or exceeding its limit.</summary>
    /// <returns><c>true</c> when the walk completed without stopping or exceeding its limit; otherwise, <c>false</c>.</returns>
    public bool IsCompleted()
    {
        return this.status == HtmlTreeWalkStatus.Completed;
    }

    /// <summary>Reports whether the visitor callback requested an early stop.</summary>
    /// <returns><c>true</c> when the callback requested an early stop; otherwise, <c>false</c>.</returns>
    public bool WasStopped()
    {
        return this.status == HtmlTreeWalkStatus.Stopped;
    }

    /// <summary>Reports whether processing exceeded a configured resource limit.</summary>
    /// <returns><c>true</c> when a configured limit was exceeded; otherwise, <c>false</c>.</returns>
    public bool WasLimitExceeded()
    {
        return this.status == HtmlTreeWalkStatus.LimitExceeded;
    }
}
