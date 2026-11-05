using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlMarkdownOperationContext
{
    internal HtmlBudget budget;
    internal HtmlDiagnosticCollector diagnostics;
    internal bool truncated;
    internal bool failed;
    internal bool limitDiagnosticAdded;

    internal HtmlMarkdownOperationContext(
        HtmlCompiledMarkdownOptions options,
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
        this.failed = false;
        this.limitDiagnosticAdded = false;
    }

    internal bool TryMeasureTree(HtmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        List<HtmlNode> pendingNodes = new List<HtmlNode>();
        List<int> pendingDepths = new List<int>();
        pendingNodes.Add(node);
        pendingDepths.Add(0);
        while (pendingNodes.Count > 0)
        {
            int last = pendingNodes.Count - 1;
            HtmlNode current = pendingNodes[last];
            int depth = pendingDepths[last];
            pendingNodes.RemoveAt(last);
            pendingDepths.RemoveAt(last);
            if (!this.budget.Allows(HtmlLimitKind.Depth, depth)
                || !this.budget.TryConsume(HtmlLimitKind.Visits, 1))
            {
                this.MarkLimitExceeded();
                return false;
            }

            if (IsTableCell(current)
                && !this.budget.TryConsume(HtmlLimitKind.TableCells, 1))
            {
                this.MarkLimitExceeded();
                return false;
            }

            HtmlNodeKind kind = current.GetKindStorage();
            if (kind != HtmlNodeKind.Document
                && kind != HtmlNodeKind.Fragment
                && kind != HtmlNodeKind.Element)
            {
                continue;
            }

            HtmlNode? container = current.GetTemplateContentStorage();
            if (container == null)
            {
                container = current;
            }

            HtmlNode[] children = container.GetChildrenStorage();
            int index = children.Length - 1;
            while (index >= 0)
            {
                pendingNodes.Add(children[index]);
                pendingDepths.Add(depth + 1);
                index = index - 1;
            }
        }

        return true;
    }

    internal string ApplyOutputLimit(string markdown, string marker)
    {
        if (markdown == null)
        {
            throw new ArgumentNullException("markdown");
        }

        if (marker == null)
        {
            throw new ArgumentNullException("marker");
        }

        int maximum = this.budget.GetLimits()
            .GetMaximumOutputCharacters();
        if (!this.budget.TryConsume(HtmlLimitKind.OutputCharacters,
            markdown.Length))
        {
            this.MarkLimitExceeded();
        }

        if (!this.truncated)
        {
            return markdown;
        }

        return AppendTruncationMarker(markdown, marker, maximum);
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

    internal void MarkUnknownElement(
        Exception exception, HtmlNode? node)
    {
        if (exception == null)
        {
            throw new ArgumentNullException("exception");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        this.failed = true;
        this.diagnostics.AddDetails(HtmlDiagnosticCode.MarkdownUnknownElement,
            HtmlDiagnosticSeverity.Error, exception.Message,
            node.GetSourceSpan());
    }

    internal HtmlMarkdownResult CreateResult(string markdown,
        bool success)
    {
        if (markdown == null)
        {
            throw new ArgumentNullException("markdown");
        }

        return new HtmlMarkdownResult(success && !this.failed,
            !this.truncated && !this.failed, this.truncated, markdown,
            this.diagnostics.GetDiagnostics(),
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
        this.diagnostics.AddDetails(
            HtmlDiagnosticCode.MarkdownConversionLimitExceeded,
            HtmlDiagnosticSeverity.Warning,
            "A Markdown conversion resource limit was exceeded.",
            HtmlSourceSpan.CreateUnknown());
    }

    internal static bool IsTableCell(HtmlNode node)
    {
        if (node.GetKindStorage() != HtmlNodeKind.Element
            || node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = node.GetLocalNameStorage();
        return name == "td" || name == "th";
    }

    internal static string AppendTruncationMarker(string markdown, string marker,
        int maximumCharacters)
    {
        if (marker.Length == 0)
        {
            return GetPrefix(markdown, maximumCharacters);
        }

        if (marker.Length >= maximumCharacters)
        {
            return GetPrefix(marker, maximumCharacters);
        }

        int markdownCharacters = maximumCharacters - marker.Length;
        string prefix = markdown.Length <= markdownCharacters
            ? markdown : GetPrefix(markdown, markdownCharacters);
        return prefix + marker;
    }

    internal static string GetPrefix(string value, int maximumCharacters)
    {
        if (value.Length <= maximumCharacters)
        {
            return value;
        }

        StringBuilder builder = new StringBuilder(maximumCharacters);
        TextElementEnumerator elements =
            StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            if (builder.Length + element.Length > maximumCharacters)
            {
                break;
            }

            builder.Append(element);
        }

        return builder.ToString();
    }
}
