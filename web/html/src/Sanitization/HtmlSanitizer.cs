using System;
using System.Threading;

namespace html;

/// <summary>Applies a compiled security policy to HTML nodes and reports every retained change.</summary>
internal static class HtmlSanitizer
{
    internal const int MaximumStabilizationPasses = 8;

    /// <summary>Sanitizes HTML by applying the compiled element, attribute, URL, and style rules.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="policy">The sanitizer policy whose rules are applied.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The sanitized output, completion status, diagnostics, and bounded change report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlSanitizeResult Sanitize(string html,
        HtmlCompiledSanitizerPolicy policy, HtmlParseOptions options,
        CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        ValidatePolicyAndOptions(policy, options);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlDocument contextDocument = new HtmlDocument("",
            options.GetBaseUrl(), HtmlDocumentMode.NoQuirks,
            new HtmlDiagnostic[0], HtmlSourceSpan.CreateUnknown());
        HtmlNode context = contextDocument.CreateElement("body");
        HtmlParseResult parsed = Html.ParseFragment(html,
            new HtmlFragmentContext(context), options, cancellationToken);
        if (!parsed.IsSuccess() || !parsed.IsComplete()
            || parsed.WasTruncated() || parsed.GetFragment() == null)
        {
            return CreateFailure(options.GetLimits(),
                parsed.GetDiagnostics(), parsed.WasTruncated(),
                parsed.WasTruncated() || !parsed.IsComplete(),
                parsed.GetDiagnosticCount(),
                parsed.GetDroppedDiagnosticCount());
        }

        return SanitizeClone(parsed.GetFragment(), policy,
            options.GetLimits(), cancellationToken,
            parsed.GetDiagnostics(), parsed.GetDiagnosticCount(),
            parsed.GetDroppedDiagnosticCount(), false, true);
    }

    /// <summary>Sanitizes HTML by applying the compiled element, attribute, URL, and style rules.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="policy">The sanitizer policy whose rules are applied.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The sanitized output, completion status, diagnostics, and bounded change report.</returns>
    public static HtmlSanitizeResult Sanitize(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy,
        CancellationToken cancellationToken)
    {
        return Sanitize(node, policy, HtmlLimits.CreateDefault(),
            cancellationToken);
    }

    /// <summary>Sanitizes HTML by applying the compiled element, attribute, URL, and style rules.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="policy">The sanitizer policy whose rules are applied.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The sanitized output, completion status, diagnostics, and bounded change report.</returns>
    public static HtmlSanitizeResult Sanitize(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        ValidateNodePolicyAndLimits(node, policy, limits);
        return SanitizeClone(node, policy, limits, cancellationToken,
            new HtmlDiagnostic[0], 0, 0, false, false);
    }

    internal static HtmlSanitizeResult SanitizeForPipeline(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken, HtmlBudget budget)
    {
        ValidateNodePolicyAndLimits(node, policy, limits);
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!HtmlSanitizerInputLimits.Validate(node, budget))
        {
            return CreateFailure(limits, new HtmlDiagnostic[0], true, true,
                0, 0);
        }

        HtmlNode clone = CloneForSanitizing(node);
        HtmlSanitizerReport report =
            HtmlSanitizerNestedContent.ApplyWithReport(clone, policy,
                limits, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (report.WasLimitExceeded()
            || !HtmlSanitizerInputLimits.Validate(clone, budget))
        {
            return CreateFailureWithReport(limits, report,
                new HtmlDiagnostic[0], true, true, 0, 0);
        }

        return new HtmlSanitizeResult(true, report.WasTruncated(), clone,
            "", report, new HtmlDiagnostic[0], 0, 0);
    }

    /// <summary>Sanitizes HTML by applying the compiled element, attribute, URL, and style rules.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="policy">The sanitizer policy whose rules are applied.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The sanitized output, completion status, diagnostics, and bounded change report.</returns>
    public static HtmlSanitizeResult SanitizeInPlace(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy,
        CancellationToken cancellationToken)
    {
        return SanitizeInPlace(node, policy, HtmlLimits.CreateDefault(),
            cancellationToken);
    }

    /// <summary>Sanitizes HTML by applying the compiled element, attribute, URL, and style rules.</summary>
    /// <param name="node">The root of the node or subtree processed by the operation.</param>
    /// <param name="policy">The sanitizer policy whose rules are applied.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The sanitized output, completion status, diagnostics, and bounded change report.</returns>
    public static HtmlSanitizeResult SanitizeInPlace(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        ValidateNodePolicyAndLimits(node, policy, limits);
        HtmlSanitizeResult prepared = SanitizeClone(node, policy, limits,
            cancellationToken, new HtmlDiagnostic[0], 0, 0, false, false);
        if (!prepared.IsSuccess())
        {
            return prepared;
        }

        cancellationToken.ThrowIfCancellationRequested();
        HtmlSanitizerNestedContent.ApplyWithReport(node, policy, limits,
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return new HtmlSanitizeResult(true, prepared.WasTruncated(), node,
            prepared.GetHtml(), prepared.GetReport(),
            prepared.GetDiagnostics(), prepared.GetDiagnosticCount(),
            prepared.GetDroppedDiagnosticCount());
    }

    internal static HtmlSanitizeResult SanitizeClone(HtmlNode? node,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken, HtmlDiagnostic[] diagnostics,
        long diagnosticCount, long droppedDiagnosticCount,
        bool parseTruncated, bool stabilizeOutput)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (node == null)
        {
            return CreateFailure(limits, diagnostics, true, true,
                diagnosticCount, droppedDiagnosticCount);
        }

        if (!HtmlSanitizerInputLimits.Validate(node, limits,
            cancellationToken))
        {
            return CreateFailure(limits, diagnostics, true, true,
                diagnosticCount, droppedDiagnosticCount);
        }

        HtmlNode clone = CloneForSanitizing(node);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlSanitizerReport report =
            HtmlSanitizerNestedContent.ApplyWithReport(clone, policy,
                limits, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (report.WasLimitExceeded()
            || !HtmlSanitizerInputLimits.Validate(clone, limits,
                cancellationToken))
        {
            return CreateFailureWithReport(limits, report, diagnostics,
                true, true, diagnosticCount, droppedDiagnosticCount);
        }

        HtmlSanitizerSerializationResult serialized = SerializeCanonical(
            clone, limits, cancellationToken);
        if (!serialized.IsSuccess())
        {
            return CreateFailureWithReport(limits, report, diagnostics,
                true, true, diagnosticCount, droppedDiagnosticCount);
        }

        string output = serialized.GetHtml();
        cancellationToken.ThrowIfCancellationRequested();
        if (stabilizeOutput)
        {
            return StabilizeStringOutput(output, policy, limits,
                cancellationToken, diagnostics, diagnosticCount,
                droppedDiagnosticCount, report,
                parseTruncated || report.WasTruncated());
        }

        return new HtmlSanitizeResult(true,
            parseTruncated || report.WasTruncated(), clone, output, report,
            diagnostics, diagnosticCount, droppedDiagnosticCount);
    }

    internal static HtmlSanitizeResult StabilizeStringOutput(string output,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken, HtmlDiagnostic[] diagnostics,
        long diagnosticCount, long droppedDiagnosticCount,
        HtmlSanitizerReport report, bool truncated)
    {
        string current = output;
        HtmlSanitizerReport combinedReport = report;
        int pass = 0;
        while (pass < MaximumStabilizationPasses)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlParseOptions options = HtmlParseOptions.CreateDefault();
            options.SetLimits(limits);
            HtmlDocument contextDocument = new HtmlDocument("", "",
                HtmlDocumentMode.NoQuirks, new HtmlDiagnostic[0],
                HtmlSourceSpan.CreateUnknown());
            HtmlNode context = contextDocument.CreateElement("body");
            HtmlParseResult parsed = Html.ParseFragment(current,
                new HtmlFragmentContext(context), options,
                cancellationToken);
            if (!parsed.IsSuccess() || !parsed.IsComplete()
                || parsed.WasTruncated() || parsed.GetFragment() == null)
            {
                return CreateFailureWithReport(limits, combinedReport,
                    diagnostics, true, true, diagnosticCount,
                    droppedDiagnosticCount);
            }

            HtmlNode? fragment = parsed.GetFragment();
            if (fragment == null)
            {
                throw new InvalidOperationException(
                    "Successful stabilization parsing requires a fragment.");
            }

            HtmlSanitizerReport stabilizationReport =
                HtmlSanitizerNestedContent.ApplyWithReport(fragment,
                    policy, limits, cancellationToken);
            combinedReport = HtmlSanitizerReportCombiner.Combine(
                combinedReport, stabilizationReport,
                limits.GetMaximumDiagnostics());
            if (stabilizationReport.WasLimitExceeded()
                || !HtmlSanitizerInputLimits.Validate(fragment, limits,
                    cancellationToken))
            {
                return CreateFailureWithReport(limits, combinedReport,
                    diagnostics, true, true, diagnosticCount,
                    droppedDiagnosticCount);
            }

            HtmlSanitizerSerializationResult serialized =
                SerializeCanonical(fragment, limits, cancellationToken);
            if (!serialized.IsSuccess())
            {
                return CreateFailureWithReport(limits, combinedReport,
                    diagnostics, true, true, diagnosticCount,
                    droppedDiagnosticCount);
            }

            string next = serialized.GetHtml();
            if (next == current)
            {
                return new HtmlSanitizeResult(true,
                    truncated || combinedReport.WasTruncated(),
                    fragment, next, combinedReport, diagnostics,
                    diagnosticCount, droppedDiagnosticCount);
            }

            current = next;
            pass = pass + 1;
        }

        return CreateFailureWithReport(limits, combinedReport, diagnostics,
            true, true, diagnosticCount, droppedDiagnosticCount);
    }

    internal static HtmlSanitizerSerializationResult SerializeCanonical(HtmlNode node,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        HtmlSerializerOptions options =
            HtmlSerializerOptions.CreateCanonical();
        options.SetLimits(limits);
        HtmlSerializationValidator.Validate(node, cancellationToken);
        HtmlSerializationWriter counter = HtmlSerializationWriter.CreateCounter(
            options, cancellationToken);
        counter.WriteOuter(node);
        if (counter.WasLimitExceeded())
        {
            return new HtmlSanitizerSerializationResult(false, "");
        }

        return new HtmlSanitizerSerializationResult(true,
            HtmlSerializer.Serialize(node, options, cancellationToken));
    }

    internal static HtmlNode CloneForSanitizing(HtmlNode node)
    {
        if (node.GetKindStorage() == HtmlNodeKind.Document)
        {
            return node.Clone(true);
        }

        HtmlDocument source = node.GetOwnerDocumentStorage();
        HtmlDocument owner = new HtmlDocument(source.GetUrlStorage(),
            source.GetBaseUrlStorage(), source.GetModeStorage(),
            source.GetDiagnosticsStorage(), HtmlSourceSpan.CreateUnknown());
        return owner.Import(node, true);
    }

    internal static HtmlSanitizeResult CreateFailure(HtmlLimits limits,
        HtmlDiagnostic[] diagnostics, bool truncated, bool limitExceeded,
        long diagnosticCount, long droppedDiagnosticCount)
    {
        HtmlSanitizerReportCollector collector =
            new HtmlSanitizerReportCollector(
                limits.GetMaximumDiagnostics());
        if (limitExceeded)
        {
            collector.MarkLimitExceeded();
        }

        return new HtmlSanitizeResult(false, truncated, null, "",
            collector.CreateReport(), diagnostics, diagnosticCount,
            droppedDiagnosticCount);
    }

    internal static HtmlSanitizeResult CreateFailureWithReport(
        HtmlLimits limits, HtmlSanitizerReport report,
        HtmlDiagnostic[] diagnostics, bool truncated, bool limitExceeded,
        long diagnosticCount, long droppedDiagnosticCount)
    {
        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        HtmlSanitizerReport finalReport = report;
        if (limitExceeded && !report.WasLimitExceeded())
        {
            HtmlSanitizerReportCollector collector =
                new HtmlSanitizerReportCollector(
                    limits.GetMaximumDiagnostics());
            collector.MarkLimitExceeded();
            finalReport = HtmlSanitizerReportCombiner.Combine(report,
                collector.CreateReport(), limits.GetMaximumDiagnostics());
        }

        return new HtmlSanitizeResult(false, truncated, null, "",
            finalReport, diagnostics, diagnosticCount,
            droppedDiagnosticCount);
    }

    internal static void ValidatePolicyAndOptions(
        HtmlCompiledSanitizerPolicy policy, HtmlParseOptions options)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
    }

    internal static void ValidateNodePolicyAndLimits(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
    }
}

internal class HtmlSanitizerSerializationResult
{
    internal bool success;
    internal string html;

    internal HtmlSanitizerSerializationResult(bool successValue,
        string htmlValue)
    {
        this.success = successValue;
        this.html = htmlValue;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal string GetHtml()
    {
        return this.html;
    }
}

internal static class HtmlSanitizerReportCombiner
{
    internal const int ReasonCount = 9003;

    internal static HtmlSanitizerReport Combine(HtmlSanitizerReport first,
        HtmlSanitizerReport second, int maximumChanges)
    {
        HtmlSanitizerChange[] firstChanges = first.GetChanges();
        HtmlSanitizerChange[] secondChanges = second.GetChanges();
        int retainedCount = firstChanges.Length + secondChanges.Length;
        if (retainedCount > maximumChanges)
        {
            retainedCount = maximumChanges;
        }

        HtmlSanitizerChange[] retained =
            new HtmlSanitizerChange[retainedCount];
        int count = CopyChanges(firstChanges, retained, 0);
        CopyChanges(secondChanges, retained, count);
        long[] reasonCounts = new long[ReasonCount];
        int reasonIndex = 1;
        while (reasonIndex < reasonCounts.Length)
        {
            reasonCounts[reasonIndex] = AddSaturated(
                first.GetReasonCount(
                    (HtmlSanitizerChangeReason)reasonIndex),
                second.GetReasonCount(
                    (HtmlSanitizerChangeReason)reasonIndex));
            reasonIndex = reasonIndex + 1;
        }

        long total = AddSaturated(first.GetTotalCount(),
            second.GetTotalCount());
        long dropped = total - retainedCount;
        bool truncated = first.WasTruncated() || second.WasTruncated()
            || total > retainedCount;
        return new HtmlSanitizerReport(retained, reasonCounts, total,
            dropped, truncated,
            first.WasLimitExceeded() || second.WasLimitExceeded(),
            first.WasCancelled() || second.WasCancelled());
    }

    internal static int CopyChanges(HtmlSanitizerChange[] source,
        HtmlSanitizerChange[] destination, int count)
    {
        int index = 0;
        while (index < source.Length && count < destination.Length)
        {
            destination[count] = source[index];
            count = count + 1;
            index = index + 1;
        }

        return count;
    }

    internal static long AddSaturated(long first, long second)
    {
        return second > long.MaxValue - first
            ? long.MaxValue : first + second;
    }
}

internal class HtmlSanitizerInputLimits
{
    internal HtmlBudget budget;
    internal bool valid;

    internal HtmlSanitizerInputLimits(HtmlBudget budgetValue)
    {
        this.budget = budgetValue;
        this.valid = true;
    }

    internal static bool Validate(HtmlNode? node, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        HtmlSanitizerInputLimits validator =
            new HtmlSanitizerInputLimits(
                new HtmlBudget(limits, cancellationToken));
        validator.Visit(node, 0);
        return validator.valid;
    }

    internal static bool Validate(HtmlNode node, HtmlBudget budget)
    {
        if (budget == null)
        {
            throw new ArgumentNullException("budget");
        }

        HtmlSanitizerInputLimits validator =
            new HtmlSanitizerInputLimits(budget);
        validator.Visit(node, 0);
        return validator.valid;
    }

    internal void Visit(HtmlNode? node, int depth)
    {
        if (node == null)
        {
            this.valid = false;
            return;
        }

        if (!this.valid)
        {
            return;
        }

        this.budget.CheckCancellation();
        if (!this.budget.TryConsume(HtmlLimitKind.Nodes, 1)
            || !this.budget.TryConsume(HtmlLimitKind.Visits, 1)
            || !this.budget.Allows(HtmlLimitKind.Depth, depth))
        {
            this.valid = false;
            return;
        }

        HtmlNodeKind kind = node.GetKindStorage();
        if (kind == HtmlNodeKind.Element)
        {
            this.ValidateElement(node);
        }
        else if (kind == HtmlNodeKind.Text
            || kind == HtmlNodeKind.Comment)
        {
            if (!this.budget.Allows(HtmlLimitKind.TokenCharacters,
                node.GetDataStorage().Length))
            {
                this.valid = false;
                return;
            }
        }

        HtmlNode[] children = node.GetChildrenStorage();
        int index = 0;
        while (index < children.Length && this.valid)
        {
            this.Visit(children[index], depth + 1);
            index = index + 1;
        }

        HtmlNode? templateContent = node.GetTemplateContentStorage();
        if (templateContent != null && this.valid)
        {
            this.Visit(templateContent, depth + 1);
        }
    }

    internal void ValidateElement(HtmlNode element)
    {
        HtmlAttribute[] attributes = element.GetAttributesStorage();
        if (!this.budget.Allows(HtmlLimitKind.AttributesPerTag,
                attributes.Length)
            || !this.budget.Allows(HtmlLimitKind.NameCharacters,
                element.GetNameStorage().Length))
        {
            this.valid = false;
            return;
        }

        int index = 0;
        while (index < attributes.Length && this.valid)
        {
            this.budget.CheckCancellation();
            HtmlAttribute attribute = attributes[index];
            if (!this.budget.Allows(HtmlLimitKind.NameCharacters,
                    attribute.GetName().Length)
                || !this.budget.Allows(
                    HtmlLimitKind.AttributeValueCharacters,
                    attribute.GetValue().Length))
            {
                this.valid = false;
            }

            index = index + 1;
        }
    }
}
