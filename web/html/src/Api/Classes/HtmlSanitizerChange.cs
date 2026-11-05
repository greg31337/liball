using System;
using System.Threading;
using System.Globalization;

namespace html;
/// <summary>Describes one concrete change made while processing HTML.</summary>
public class HtmlSanitizerChange : IHtmlSanitizerChange
{
    internal const int MaximumNameLength = 64;
    internal const string RedactedValue = "[redacted]";
    internal HtmlSanitizerChangeKind kind;
    internal HtmlSanitizerChangeReason reason;
    internal string originalName;
    internal bool nameRedacted;
    internal string originalValue;
    internal bool valueRedacted;
    internal HtmlSourceSpan span;
    internal HtmlSanitizerChange(HtmlSanitizerChangeKind kindValue, HtmlSanitizerChangeReason reasonValue, string originalNameValue, string originalValueValue, HtmlSourceSpan spanValue)
    {
        ValidateKind(kindValue);
        ValidateReason(reasonValue);
        if (originalNameValue == null)
        {
            throw new ArgumentNullException("originalNameValue");
        }

        if (originalValueValue == null)
        {
            throw new ArgumentNullException("originalValueValue");
        }

        if (spanValue == null)
        {
            throw new ArgumentNullException("spanValue");
        }

        this.kind = kindValue;
        this.reason = reasonValue;
        this.nameRedacted = !IsSafeName(originalNameValue);
        this.originalName = this.nameRedacted ? "" : originalNameValue;
        this.valueRedacted = originalValueValue.Length > 0;
        this.originalValue = this.valueRedacted ? RedactedValue : "";
        this.span = spanValue;
    }

    /// <summary>Gets how the sanitizer changed the affected content.</summary>
    /// <returns>Removed, replaced, added, or unwrapped.</returns>
    public HtmlSanitizerChangeKind GetKind()
    {
        return this.kind;
    }

    /// <summary>Gets the stable category explaining why the sanitizer made this change.</summary>
    /// <returns>The policy, safety, normalization, limit, or cancellation reason.</returns>
    public HtmlSanitizerChangeReason GetReason()
    {
        return this.reason;
    }

    /// <summary>Formats the reason as a stable diagnostic-style identifier.</summary>
    /// <returns><c>UHS</c> followed by the reason's four-digit numeric value.</returns>
    public string GetReasonIdentifier()
    {
        return "UHS" + ((int)this.reason).ToString("D4", CultureInfo.InvariantCulture);
    }

    /// <summary>Gets the affected element or attribute name when it was safe and small enough to retain.</summary>
    /// <returns>The original name, or an empty string when <see cref = "WasNameRedacted"/> is <c>true</c>.</returns>
    public string GetOriginalName()
    {
        return this.originalName;
    }

    /// <summary>Reports whether the reported name was omitted because it was unsafe or too large to retain.</summary>
    /// <returns><c>true</c> when the reported name was omitted; otherwise, <c>false</c>.</returns>
    public bool WasNameRedacted()
    {
        return this.nameRedacted;
    }

    /// <summary>Gets the privacy-preserving representation of the affected input value.</summary>
    /// <returns>An empty string when no value was present, or the fixed <c>[redacted]</c> marker when <see cref = "WasValueRedacted"/> is <c>true</c>.</returns>
    public string GetOriginalValue()
    {
        return this.originalValue;
    }

    /// <summary>Reports whether the reported value was replaced with the fixed redaction marker.</summary>
    /// <returns><c>true</c> when the reported value was replaced by the redaction marker; otherwise, <c>false</c>.</returns>
    public bool WasValueRedacted()
    {
        return this.valueRedacted;
    }

    /// <summary>Gets the source range associated with the changed content.</summary>
    /// <returns>The recorded source span, which may be an explicitly unknown span when source tracking was unavailable.</returns>
    public HtmlSourceSpan GetSpan()
    {
        return this.span;
    }

    internal static bool IsSafeName(string value)
    {
        if (value.Length > MaximumNameLength)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 32 || current == 127)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void ValidateKind(HtmlSanitizerChangeKind value)
    {
        if ((int)value < (int)HtmlSanitizerChangeKind.Removed || (int)value > (int)HtmlSanitizerChangeKind.Unwrapped)
        {
            throw new ArgumentOutOfRangeException("kindValue");
        }
    }

    internal static void ValidateReason(HtmlSanitizerChangeReason value)
    {
        if (value == HtmlSanitizerChangeReason.None || !IsKnownReason(value))
        {
            throw new ArgumentOutOfRangeException("reasonValue");
        }
    }

    internal static bool IsKnownReason(HtmlSanitizerChangeReason value)
    {
        switch (value)
        {
            case HtmlSanitizerChangeReason.ElementNotAllowed:
            case HtmlSanitizerChangeReason.SensitiveElementDropped:
            case HtmlSanitizerChangeReason.ElementBlocked:
            case HtmlSanitizerChangeReason.CommentNotAllowed:
            case HtmlSanitizerChangeReason.DoctypeNotAllowed:
            case HtmlSanitizerChangeReason.AttributeNotAllowed:
            case HtmlSanitizerChangeReason.AttributeRewritten:
            case HtmlSanitizerChangeReason.AttributeAdded:
            case HtmlSanitizerChangeReason.CustomizedBuiltInNotAllowed:
            case HtmlSanitizerChangeReason.UnsafeUrl:
            case HtmlSanitizerChangeReason.UrlRewritten:
            case HtmlSanitizerChangeReason.LinkHardened:
            case HtmlSanitizerChangeReason.DomClobbering:
            case HtmlSanitizerChangeReason.DuplicateIdentifier:
            case HtmlSanitizerChangeReason.NestedContentRejected:
            case HtmlSanitizerChangeReason.NestedContentRewritten:
            case HtmlSanitizerChangeReason.TemplateSyntaxNeutralized:
            case HtmlSanitizerChangeReason.InlineStyleRemoved:
            case HtmlSanitizerChangeReason.CssPropertyRemoved:
            case HtmlSanitizerChangeReason.CssDeclarationRewritten:
            case HtmlSanitizerChangeReason.CssLimitExceeded:
            case HtmlSanitizerChangeReason.LimitExceeded:
            case HtmlSanitizerChangeReason.Cancelled:
                return true;
            default:
                return false;
        }
    }
}
