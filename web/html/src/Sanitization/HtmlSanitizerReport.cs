using System;
using System.Threading;
using System.Globalization;

namespace html;

internal class HtmlSanitizerReportCollector
{
    internal const int ReasonCount = 9003;

    internal HtmlSanitizerChange[] changes;
    internal long[] reasonCounts;
    internal int maximumChanges;
    internal int count;
    internal long totalCount;
    internal long droppedCount;
    internal bool truncated;
    internal bool limitExceeded;
    internal bool cancelled;

    internal HtmlSanitizerReportCollector(int maximumChangesValue)
    {
        if (maximumChangesValue <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumChangesValue");
        }

        this.maximumChanges = maximumChangesValue;
        int capacity = maximumChangesValue < 8 ? maximumChangesValue : 8;
        this.changes = new HtmlSanitizerChange[capacity];
        this.reasonCounts = new long[ReasonCount];
        this.count = 0;
        this.totalCount = 0;
        this.droppedCount = 0;
        this.truncated = false;
        this.limitExceeded = false;
        this.cancelled = false;
    }

    internal bool Add(HtmlSanitizerChangeKind kind,
        HtmlSanitizerChangeReason reason, string originalName,
        string originalValue, HtmlSourceSpan span)
    {
        HtmlSanitizerChange change = new HtmlSanitizerChange(kind, reason,
            originalName, originalValue, span);
        this.IncrementTotalCount();
        this.IncrementReasonCount(reason);
        if (this.count >= this.maximumChanges)
        {
            this.truncated = true;
            this.IncrementDroppedCount();
            return false;
        }

        this.EnsureCapacity(this.count + 1);
        this.changes[this.count] = change;
        this.count = this.count + 1;
        return true;
    }

    internal void MarkLimitExceeded()
    {
        this.limitExceeded = true;
    }

    internal void MarkCancelled()
    {
        this.cancelled = true;
    }

    internal HtmlSanitizerReport CreateReport()
    {
        HtmlSanitizerChange[] retained =
            new HtmlSanitizerChange[this.count];
        Array.Copy(this.changes, retained, this.count);
        return new HtmlSanitizerReport(retained, this.reasonCounts,
            this.totalCount, this.droppedCount, this.truncated,
            this.limitExceeded, this.cancelled);
    }

    internal void EnsureCapacity(int required)
    {
        if (required <= this.changes.Length)
        {
            return;
        }

        int capacity = this.changes.Length > int.MaxValue / 2
            ? int.MaxValue : this.changes.Length * 2;
        if (capacity < required)
        {
            capacity = required;
        }

        if (capacity > this.maximumChanges)
        {
            capacity = this.maximumChanges;
        }

        HtmlSanitizerChange[] expanded =
            new HtmlSanitizerChange[capacity];
        Array.Copy(this.changes, expanded, this.count);
        this.changes = expanded;
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

    internal void IncrementReasonCount(HtmlSanitizerChangeReason reason)
    {
        int index = (int)reason;
        if (this.reasonCounts[index] < long.MaxValue)
        {
            this.reasonCounts[index] = this.reasonCounts[index] + 1;
        }
    }
}

internal static class HtmlSanitizerAttributeReporting
{
    internal static HtmlSanitizerAttributeApplyResult Apply(HtmlNode element,
        HtmlCompiledSanitizerPolicy policy, int nestedMarkupDepth,
        HtmlSanitizerReportCollector collector, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (collector == null)
        {
            throw new ArgumentNullException("collector");
        }

        HtmlAttribute[] original = element.GetAttributesStorage();
        HtmlSanitizerAttributeApplyResult result =
            HtmlSanitizerAttributes.ApplyWithResult(element, policy,
                nestedMarkupDepth, limits, cancellationToken);
        HtmlAttribute[] finalAttributes = element.GetAttributesStorage();
        ReportStyleChanges(original, result.GetStyleChanges(), collector);
        ReportAttributeChanges(element, original, finalAttributes, policy,
            result.GetStyleChanges(), collector);
        return result;
    }

    internal static void ReportStyleChanges(HtmlAttribute[] original,
        HtmlSanitizerStyleChange[] changes,
        HtmlSanitizerReportCollector collector)
    {
        HtmlAttribute? style = Find(original, "", "style");
        HtmlSourceSpan span = style == null
            ? HtmlSourceSpan.CreateUnknown() : style.GetSourceSpan();
        string value = style == null ? "" : style.GetValue();
        int index = 0;
        while (index < changes.Length)
        {
            HtmlSanitizerStyleChange change = changes[index];
            HtmlSanitizerChangeKind kind = change.GetAction()
                    == HtmlSanitizerStyleDeclarationAction.Removed
                ? HtmlSanitizerChangeKind.Removed
                : HtmlSanitizerChangeKind.Replaced;
            HtmlSanitizerChangeReason reason = change.GetAction()
                    == HtmlSanitizerStyleDeclarationAction.Removed
                ? HtmlSanitizerChangeReason.CssPropertyRemoved
                : HtmlSanitizerChangeReason.CssDeclarationRewritten;
            collector.Add(kind, reason, change.GetPropertyName(), value,
                span);
            index = index + 1;
        }
    }

    internal static void ReportAttributeChanges(HtmlNode element,
        HtmlAttribute[] original, HtmlAttribute[] finalAttributes,
        HtmlCompiledSanitizerPolicy policy,
        HtmlSanitizerStyleChange[] styleChanges,
        HtmlSanitizerReportCollector collector)
    {
        int index = 0;
        while (index < original.Length)
        {
            HtmlAttribute before = original[index];
            HtmlAttribute? after = Find(finalAttributes,
                before.GetNamespaceUri(), before.GetLocalName());
            if (after == null)
            {
                collector.Add(HtmlSanitizerChangeKind.Removed,
                    GetRemovalReason(element, before, policy),
                    before.GetName(), before.GetValue(),
                    before.GetSourceSpan());
            }
            else if (!Same(before, after)
                && (!IsStyle(before) || styleChanges.Length == 0))
            {
                collector.Add(HtmlSanitizerChangeKind.Replaced,
                    GetRewriteReason(element, before, after, policy),
                    before.GetName(), before.GetValue(),
                    before.GetSourceSpan());
            }

            index = index + 1;
        }

        index = 0;
        while (index < finalAttributes.Length)
        {
            HtmlAttribute after = finalAttributes[index];
            if (Find(original, after.GetNamespaceUri(),
                after.GetLocalName()) == null)
            {
                collector.Add(HtmlSanitizerChangeKind.Added,
                    GetAdditionReason(element, after), after.GetName(), "",
                    after.GetSourceSpan());
            }

            index = index + 1;
        }
    }

    internal static HtmlSanitizerChangeReason GetRemovalReason(HtmlNode element,
        HtmlAttribute attribute, HtmlCompiledSanitizerPolicy policy)
    {
        if (IsStyle(attribute))
        {
            return HtmlSanitizerChangeReason.InlineStyleRemoved;
        }

        if (HtmlDomUrls.GetAttributeKind(element, attribute)
            != HtmlUrlAttributeKind.None)
        {
            return HtmlSanitizerChangeReason.UnsafeUrl;
        }

        if (IsCustomizedBuiltIn(attribute)
            && !policy.IsCustomizedBuiltInElementAllowed(
                element.GetNamespaceUriStorage(),
                element.GetLocalNameStorage(), attribute.GetValue()))
        {
            return HtmlSanitizerChangeReason.CustomizedBuiltInNotAllowed;
        }

        return HtmlSanitizerChangeReason.AttributeNotAllowed;
    }

    internal static HtmlSanitizerChangeReason GetRewriteReason(HtmlNode element,
        HtmlAttribute before, HtmlAttribute after,
        HtmlCompiledSanitizerPolicy policy)
    {
        if (HtmlDomUrls.GetAttributeKind(element, before)
            != HtmlUrlAttributeKind.None)
        {
            return HtmlSanitizerChangeReason.UrlRewritten;
        }

        if (IsNamedProperty(before)
            && after.GetValue().StartsWith(policy.GetDomClobberPrefix(),
                StringComparison.Ordinal)
            && !before.GetValue().StartsWith(
                policy.GetDomClobberPrefix(), StringComparison.Ordinal))
        {
            return HtmlSanitizerChangeReason.DomClobbering;
        }

        if (IsSrcdoc(element, before))
        {
            return HtmlSanitizerChangeReason.NestedContentRewritten;
        }

        if (HtmlSanitizerTemplateMarkers.ContainsMarker(before.GetValue()))
        {
            return HtmlSanitizerChangeReason.TemplateSyntaxNeutralized;
        }

        return HtmlSanitizerChangeReason.AttributeRewritten;
    }

    internal static HtmlSanitizerChangeReason GetAdditionReason(HtmlNode element,
        HtmlAttribute attribute)
    {
        string localName = FoldAscii(element.GetLocalNameStorage());
        string attributeName = FoldAscii(attribute.GetLocalName());
        if ((localName == "a" || localName == "area")
            && attribute.GetNamespaceUri().Length == 0
            && attributeName == "rel")
        {
            return HtmlSanitizerChangeReason.LinkHardened;
        }

        return HtmlSanitizerChangeReason.AttributeAdded;
    }

    internal static bool IsStyle(HtmlAttribute attribute)
    {
        return attribute.GetNamespaceUri().Length == 0
            && FoldAscii(attribute.GetLocalName()) == "style";
    }

    internal static bool IsCustomizedBuiltIn(HtmlAttribute attribute)
    {
        return attribute.GetNamespaceUri().Length == 0
            && FoldAscii(attribute.GetLocalName()) == "is";
    }

    internal static bool IsNamedProperty(HtmlAttribute attribute)
    {
        if (attribute.GetNamespaceUri().Length != 0)
        {
            return false;
        }

        string name = FoldAscii(attribute.GetLocalName());
        return name == "id" || name == "name";
    }

    internal static bool IsSrcdoc(HtmlNode element, HtmlAttribute attribute)
    {
        return element.GetNamespaceUriStorage()
                == HtmlSanitizerPolicyNames.GetHtmlNamespaceUri()
            && FoldAscii(element.GetLocalNameStorage()) == "iframe"
            && attribute.GetNamespaceUri().Length == 0
            && FoldAscii(attribute.GetLocalName()) == "srcdoc";
    }

    internal static bool Same(HtmlAttribute left, HtmlAttribute right)
    {
        return left.GetName() == right.GetName()
            && left.GetLocalName() == right.GetLocalName()
            && left.GetPrefix() == right.GetPrefix()
            && left.GetNamespaceUri() == right.GetNamespaceUri()
            && left.GetValue() == right.GetValue();
    }

    internal static HtmlAttribute? Find(HtmlAttribute[] values, string namespaceUri,
        string localName)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].GetNamespaceUri() == namespaceUri
                && FoldAscii(values[index].GetLocalName())
                    == FoldAscii(localName))
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static string FoldAscii(string value)
    {
        char[] result = value.ToCharArray();
        int index = 0;
        while (index < result.Length)
        {
            result[index] = Ascii.ToLower(result[index]);
            index = index + 1;
        }

        return new string(result);
    }
}
