using System;
using System.Threading;

namespace html;

internal class HtmlTextOperationContext
{
    internal HtmlBudget budget;
    internal HtmlDiagnosticCollector diagnostics;
    internal bool truncated;
    internal bool limitDiagnosticAdded;

    internal HtmlTextOperationContext(HtmlCompiledTextOptions options,
        CancellationToken cancellationToken, HtmlBudget? budgetValue)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        this.budget = budgetValue == null
            ? new HtmlBudget(options.GetLimits(), cancellationToken)
            : budgetValue;
        this.diagnostics = new HtmlDiagnosticCollector(this.budget);
        this.truncated = false;
        this.limitDiagnosticAdded = false;
    }

    internal bool TryVisit(int depth)
    {
        this.budget.CheckCancellation();
        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException("depth", depth,
                "A text conversion depth cannot be negative.");
        }

        if (!this.budget.Allows(HtmlLimitKind.Depth, depth)
            || !this.budget.TryConsume(HtmlLimitKind.Visits, 1))
        {
            this.MarkLimitExceeded();
            return false;
        }

        return true;
    }

    internal bool TryConsumeOutput(int characters)
    {
        if (!this.budget.TryConsume(HtmlLimitKind.OutputCharacters,
            characters))
        {
            this.MarkLimitExceeded();
            return false;
        }

        return true;
    }

    internal bool TryConsumeTableCells(int cells)
    {
        if (!this.budget.TryConsume(HtmlLimitKind.TableCells, cells))
        {
            this.MarkLimitExceeded();
            return false;
        }

        return true;
    }

    internal bool AddDiagnostic(HtmlDiagnostic diagnostic)
    {
        return this.diagnostics.Add(diagnostic);
    }

    internal void AddDiagnostics(HtmlDiagnostic[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        int index = 0;
        while (index < values.Length)
        {
            this.diagnostics.Add(values[index]);
            index = index + 1;
        }
    }

    internal void MarkTruncated()
    {
        this.MarkLimitExceeded();
    }

    internal bool WasTruncated()
    {
        return this.truncated;
    }

    internal HtmlTextResult CreateResult(string text, bool success)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return new HtmlTextResult(success, !this.truncated,
            this.truncated, text, this.diagnostics.GetDiagnostics(),
            this.diagnostics.GetTotalCount(),
            this.diagnostics.GetDroppedCount());
    }

    internal void MarkLimitExceeded()
    {
        this.truncated = true;
        if (this.limitDiagnosticAdded)
        {
            return;
        }

        this.limitDiagnosticAdded = true;
        this.diagnostics.AddDetails(HtmlDiagnosticCode.TextConversionLimitExceeded,
            HtmlDiagnosticSeverity.Warning,
            "A text conversion resource limit was exceeded.",
            HtmlSourceSpan.CreateUnknown());
    }
}
