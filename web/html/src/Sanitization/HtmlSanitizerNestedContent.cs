using System;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlSanitizerNestedMarkupResult
{
    internal bool success;
    internal string value;

    internal HtmlSanitizerNestedMarkupResult(bool successValue,
        string valueValue)
    {
        if (valueValue == null)
        {
            throw new ArgumentNullException("valueValue");
        }

        this.success = successValue;
        this.value = valueValue;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal string GetValue()
    {
        return this.value;
    }
}

internal static class HtmlSanitizerTemplateMarkers
{
    internal const char NeutralSeparator = '\u200B';

    internal static string Neutralize(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        StringBuilder result = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            if (index > 0 && IsOpeningPair(value[index - 1], value[index]))
            {
                result.Append(NeutralSeparator);
            }

            result.Append(value[index]);
            index = index + 1;
        }

        return result.ToString();
    }

    internal static bool ContainsMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        int index = 1;
        while (index < value.Length)
        {
            if (IsOpeningPair(value[index - 1], value[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsOpeningPair(char first, char second)
    {
        return first == '{' && second == '{'
            || first == '$' && second == '{'
            || first == '<' && second == '%';
    }
}

internal static class HtmlSanitizerNestedContent
{
    internal const int MaximumNestedMarkupDepth = 16;

    internal static void Apply(HtmlNode root,
        HtmlCompiledSanitizerPolicy policy)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        ApplyCore(root, policy, 0, null,
            limits, CancellationToken.None);
    }

    internal static void Apply(HtmlNode root,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        ApplyCore(root, policy, nestedMarkupDepth, null,
            limits, CancellationToken.None);
    }

    internal static HtmlSanitizerReport ApplyWithReport(HtmlNode root,
        HtmlCompiledSanitizerPolicy policy)
    {
        return ApplyWithReport(root, policy, HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerReport ApplyWithReport(HtmlNode root,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        return ApplyWithReport(root, policy, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerReport ApplyWithReport(HtmlNode root,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        HtmlSanitizerReportCollector collector =
            new HtmlSanitizerReportCollector(
                limits.GetMaximumDiagnostics());
        ApplyCore(root, policy, 0, collector,
            limits, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        HtmlSanitizerClobberingResult clobbering =
            HtmlSanitizerClobbering.Apply(root, policy);
        cancellationToken.ThrowIfCancellationRequested();
        string[] duplicates = clobbering.GetDuplicateIdentifiers();
        int index = 0;
        while (index < duplicates.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            collector.Add(HtmlSanitizerChangeKind.Replaced,
                HtmlSanitizerChangeReason.DuplicateIdentifier, "id",
                duplicates[index], HtmlSourceSpan.CreateUnknown());
            index = index + 1;
        }

        return collector.CreateReport();
    }

    internal static void ApplyCore(HtmlNode? root,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        HtmlSanitizerReportCollector? collector, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (nestedMarkupDepth < 0)
        {
            throw new ArgumentOutOfRangeException("nestedMarkupDepth");
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (root.GetKindStorage() == HtmlNodeKind.Element)
        {
            ProcessElement(root, policy, nestedMarkupDepth, 0, collector,
                limits, cancellationToken);
        }
        else
        {
            ProcessChildren(root, policy, nestedMarkupDepth, 0, collector,
                limits, cancellationToken);
        }
    }

    internal static HtmlSanitizerNestedMarkupResult SanitizeSrcdoc(string value,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth)
    {
        return SanitizeSrcdoc(value, policy, nestedMarkupDepth,
            HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerNestedMarkupResult SanitizeSrcdoc(string value,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        HtmlLimits limits)
    {
        return SanitizeSrcdoc(value, policy, nestedMarkupDepth, limits,
            CancellationToken.None);
    }

    internal static HtmlSanitizerNestedMarkupResult SanitizeSrcdoc(string value,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        if (nestedMarkupDepth < 0)
        {
            throw new ArgumentOutOfRangeException("nestedMarkupDepth");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        cancellationToken.ThrowIfCancellationRequested();

        if (nestedMarkupDepth > MaximumNestedMarkupDepth)
        {
            return new HtmlSanitizerNestedMarkupResult(false, "");
        }

        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlDocument contextDocument = new HtmlDocument("", "",
            HtmlDocumentMode.NoQuirks, new HtmlDiagnostic[0],
            HtmlSourceSpan.CreateUnknown());
        HtmlNode context = contextDocument.CreateElement("body");
        HtmlParseResult parsed = Html.ParseFragment(value,
            new HtmlFragmentContext(context), options,
            cancellationToken);
        if (!parsed.IsSuccess() || !parsed.IsComplete()
            || parsed.WasTruncated() || parsed.GetFragment() == null)
        {
            return new HtmlSanitizerNestedMarkupResult(false, "");
        }

        HtmlNode? fragment = parsed.GetFragment();
        if (fragment == null)
        {
            throw new InvalidOperationException(
                "Successful nested parsing requires a fragment.");
        }

        ApplyCore(fragment, policy, nestedMarkupDepth, null, limits,
            cancellationToken);
        HtmlSanitizerSerializationResult serialized =
            HtmlSanitizer.SerializeCanonical(fragment, limits,
                cancellationToken);
        return new HtmlSanitizerNestedMarkupResult(serialized.IsSuccess(),
            serialized.GetHtml());
    }

    internal static void ProcessChildren(HtmlNode container,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        int domDepth, HtmlSanitizerReportCollector? collector,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureDomDepth(domDepth, limits.GetMaximumDepth());
        HtmlNode[] children = container.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlNode child = children[index];
            HtmlNodeKind kind = child.GetKindStorage();
            if (kind == HtmlNodeKind.Element)
            {
                ProcessElement(child, policy, nestedMarkupDepth,
                    domDepth + 1, collector, limits,
                    cancellationToken);
            }
            else if (kind == HtmlNodeKind.Text)
            {
                NeutralizeData(child, policy, collector);
            }
            else if (kind == HtmlNodeKind.Comment)
            {
                HtmlSanitizerNodeAction nodeAction =
                    HtmlSanitizerNodes.GetAction(child, policy);
                if (nodeAction == HtmlSanitizerNodeAction.Keep)
                {
                    NeutralizeData(child, policy, collector);
                }
                else
                {
                    AddChange(collector, HtmlSanitizerChangeKind.Removed,
                        HtmlSanitizerChangeReason.CommentNotAllowed,
                        "#comment", child.GetDataStorage(),
                        child.GetSourceSpanStorage());
                    HtmlSanitizerNodes.Apply(child, policy);
                }
            }
            else if (kind == HtmlNodeKind.DocumentType)
            {
                if (HtmlSanitizerNodes.GetAction(child, policy)
                    == HtmlSanitizerNodeAction.Drop)
                {
                    AddChange(collector, HtmlSanitizerChangeKind.Removed,
                        HtmlSanitizerChangeReason.DoctypeNotAllowed,
                        "!doctype", child.GetNameStorage(),
                        child.GetSourceSpanStorage());
                }

                HtmlSanitizerNodes.Apply(child, policy);
            }

            index = index + 1;
        }

        NeutralizeTextBoundaries(container, policy, collector);
    }

    internal static void ProcessElement(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        int domDepth, HtmlSanitizerReportCollector? collector,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureDomDepth(domDepth, limits.GetMaximumDepth());
        HtmlSanitizerElementAction action =
            HtmlSanitizerElements.GetAction(element, policy);
        if (action == HtmlSanitizerElementAction.Keep)
        {
            if (collector == null)
            {
                HtmlSanitizerAttributes.Apply(element, policy,
                    nestedMarkupDepth, limits, cancellationToken);
            }
            else
            {
                HtmlSanitizerAttributeReporting.Apply(element, policy,
                    nestedMarkupDepth, collector, limits,
                    cancellationToken);
            }

            ProcessChildren(element, policy, nestedMarkupDepth,
                domDepth, collector, limits, cancellationToken);
            HtmlNode? templateContent = element.GetTemplateContentStorage();
            if (templateContent != null)
            {
                ProcessChildren(templateContent, policy, nestedMarkupDepth,
                    domDepth, collector, limits,
                    cancellationToken);
            }

            return;
        }

        if (action == HtmlSanitizerElementAction.Unwrap)
        {
            ProcessChildren(element, policy, nestedMarkupDepth,
                domDepth, collector, limits, cancellationToken);
        }
        else if (action == HtmlSanitizerElementAction.ReplaceWithText
            || action == HtmlSanitizerElementAction.EscapeAsText)
        {
            AddElementChange(collector, element, policy, action);
            ReplaceElementWithText(element, action, policy);
            return;
        }

        AddElementChange(collector, element, policy, action);
        HtmlSanitizerElements.Apply(element, action);
    }

    internal static void ReplaceElementWithText(HtmlNode element,
        HtmlSanitizerElementAction action,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (element.GetParentStorage() == null)
        {
            throw new InvalidOperationException(
                "A detached element cannot be replaced by a sanitizer action.");
        }

        string value = action == HtmlSanitizerElementAction.ReplaceWithText
            ? element.GetTextContent() : HtmlSerializer.Serialize(element);
        if (policy.GetNeutralizeServerTemplateMarkers())
        {
            value = HtmlSanitizerTemplateMarkers.Neutralize(value);
        }

        HtmlNode replacement = HtmlNode.CreateTextStorage(
            element.GetOwnerDocumentStorage(), value,
            element.GetSourceSpanStorage());
        element.ReplaceWith(replacement);
    }

    internal static void NeutralizeData(HtmlNode node,
        HtmlCompiledSanitizerPolicy policy,
        HtmlSanitizerReportCollector? collector)
    {
        if (policy.GetNeutralizeServerTemplateMarkers())
        {
            string original = node.GetDataStorage();
            string neutralized = HtmlSanitizerTemplateMarkers.Neutralize(
                original);
            if (neutralized != original)
            {
                AddChange(collector, HtmlSanitizerChangeKind.Replaced,
                    HtmlSanitizerChangeReason.TemplateSyntaxNeutralized,
                    "#text", original, node.GetSourceSpanStorage());
                node.SetData(neutralized);
            }
        }
    }

    internal static void NeutralizeTextBoundaries(HtmlNode container,
        HtmlCompiledSanitizerPolicy policy,
        HtmlSanitizerReportCollector? collector)
    {
        if (!policy.GetNeutralizeServerTemplateMarkers())
        {
            return;
        }

        HtmlNode[] children = container.GetChildrenStorage();
        HtmlNode? previousText = null;
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Text)
            {
                string current = child.GetDataStorage();
                if (previousText != null && current.Length > 0)
                {
                    string previous = previousText.GetDataStorage();
                    if (previous.Length > 0)
                    {
                        string boundary = previous.Substring(
                            previous.Length - 1, 1)
                            + current.Substring(0, 1);
                        if (HtmlSanitizerTemplateMarkers.ContainsMarker(
                            boundary))
                        {
                            AddChange(collector,
                                HtmlSanitizerChangeKind.Replaced,
                                HtmlSanitizerChangeReason
                                    .TemplateSyntaxNeutralized,
                                "#text", current,
                                child.GetSourceSpanStorage());
                            child.SetData("\u200B" + current);
                        }
                    }
                }

                previousText = child;
            }
            else
            {
                previousText = null;
            }

            index = index + 1;
        }
    }

    internal static void AddElementChange(HtmlSanitizerReportCollector? collector,
        HtmlNode element, HtmlCompiledSanitizerPolicy policy,
        HtmlSanitizerElementAction action)
    {
        HtmlSanitizerChangeKind kind = action
                == HtmlSanitizerElementAction.Unwrap
            ? HtmlSanitizerChangeKind.Unwrapped
            : action == HtmlSanitizerElementAction.DropSubtree
                ? HtmlSanitizerChangeKind.Removed
                : HtmlSanitizerChangeKind.Replaced;
        HtmlSanitizerChangeReason reason;
        if (HtmlSanitizerPolicyNames.IsSafeElementLocalName(
                element.GetNamespaceUriStorage(),
                element.GetLocalNameStorage())
            && policy.IsElementBlockedInNamespace(element.GetNamespaceUriStorage(),
                element.GetLocalNameStorage()))
        {
            reason = HtmlSanitizerChangeReason.ElementBlocked;
        }
        else if (action == HtmlSanitizerElementAction.DropSubtree)
        {
            reason = HtmlSanitizerChangeReason.SensitiveElementDropped;
        }
        else
        {
            reason = HtmlSanitizerChangeReason.ElementNotAllowed;
        }

        AddChange(collector, kind, reason, element.GetNameStorage(), "",
            element.GetSourceSpanStorage());
    }

    internal static void AddChange(HtmlSanitizerReportCollector? collector,
        HtmlSanitizerChangeKind kind, HtmlSanitizerChangeReason reason,
        string name, string value, HtmlSourceSpan span)
    {
        if (collector != null)
        {
            collector.Add(kind, reason, name, value, span);
        }
    }

    internal static void EnsureDomDepth(int value, int maximum)
    {
        if (value > maximum)
        {
            throw new InvalidOperationException(
                "Nested sanitizer DOM depth limit was exceeded.");
        }
    }
}
