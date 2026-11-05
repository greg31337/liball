using System;
using System.Globalization;

namespace html;

internal class HtmlDiagnosticCollector
{
    internal HtmlBudget budget;
    internal HtmlDiagnostic[] diagnostics;
    internal int count;
    internal long totalCount;
    internal long droppedCount;
    internal bool truncated;

    internal HtmlDiagnosticCollector(HtmlBudget budget)
    {
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        this.budget = budget;
        int maximum = budget.GetLimits().GetMaximumDiagnostics();
        int capacity = maximum < 8 ? maximum : 8;
        this.diagnostics = new HtmlDiagnostic[capacity];
        this.count = 0;
        this.totalCount = 0;
        this.droppedCount = 0;
        this.truncated = false;
    }

    internal bool Add(HtmlDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        bool accepted = this.budget.TryConsume(HtmlLimitKind.Diagnostics, 1);
        this.IncrementTotalCount();
        if (!accepted)
        {
            this.truncated = true;
            this.IncrementDroppedCount();
            return false;
        }

        this.EnsureCapacity(this.count + 1);
        this.diagnostics[this.count] = diagnostic;
        this.count = this.count + 1;
        return true;
    }

    internal bool AddDetails(HtmlDiagnosticCode code, HtmlDiagnosticSeverity severity, string message, HtmlSourceSpan span)
    {
        return this.Add(HtmlDiagnostic.Create(code, severity, message, span));
    }

    internal int GetCount()
    {
        return this.count;
    }

    internal long GetTotalCount()
    {
        return this.totalCount;
    }

    internal long GetDroppedCount()
    {
        return this.droppedCount;
    }

    internal bool WasTruncated()
    {
        return this.truncated;
    }

    internal HtmlDiagnostic[] GetDiagnostics()
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[this.count];
        Array.Copy(this.diagnostics, result, this.count);
        return result;
    }

    internal void EnsureCapacity(int required)
    {
        if (required <= this.diagnostics.Length)
        {
            return;
        }

        int maximum = this.budget.GetLimits().GetMaximumDiagnostics();
        int capacity;
        if (this.diagnostics.Length > int.MaxValue / 2)
        {
            capacity = int.MaxValue;
        }
        else
        {
            capacity = this.diagnostics.Length * 2;
        }

        if (capacity < required)
        {
            capacity = required;
        }

        if (capacity > maximum)
        {
            capacity = maximum;
        }

        HtmlDiagnostic[] expanded = new HtmlDiagnostic[capacity];
        Array.Copy(this.diagnostics, expanded, this.count);
        this.diagnostics = expanded;
    }

    internal void IncrementTotalCount()
    {
        if (this.totalCount < long.MaxValue)
        {
            this.totalCount = this.totalCount + 1;
        }
    }

    internal void IncrementDroppedCount()
    {
        if (this.droppedCount < long.MaxValue)
        {
            this.droppedCount = this.droppedCount + 1;
        }
    }
}
