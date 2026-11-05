using System;
using System.Threading;

namespace html;

internal enum HtmlLimitKind
{
    None = 0,
    InputBytes = 1,
    InputCharacters = 2,
    Tokens = 3,
    AttributesPerTag = 4,
    TokenCharacters = 5,
    NameCharacters = 6,
    AttributeValueCharacters = 7,
    Nodes = 8,
    Depth = 9,
    SelectorComplexity = 10,
    SelectorResults = 11,
    HasWork = 12,
    CssTokens = 13,
    CssDeclarations = 14,
    UrlCharacters = 15,
    DataUriBytes = 16,
    TableCells = 17,
    JsonDepth = 18,
    JsonProperties = 19,
    Visits = 20,
    Diagnostics = 21,
    OutputCharacters = 22
}

internal class HtmlBudget
{
    internal HtmlLimits limits;
    internal CancellationToken cancellationToken;
    internal long[] used;
    internal bool hasExceededLimit;
    internal HtmlLimitKind firstExceededLimit;

    internal HtmlBudget(HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        this.limits = limits.Copy();
        this.cancellationToken = cancellationToken;
        this.used = new long[(int)HtmlLimitKind.OutputCharacters + 1];
        this.hasExceededLimit = false;
        this.firstExceededLimit = HtmlLimitKind.None;
    }

    internal HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    internal bool TryConsume(HtmlLimitKind kind, int amount)
    {
        this.CheckCancellation();
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException("amount", amount, "Consumed work cannot be negative.");
        }

        int maximum = this.GetMaximum(kind);
        int index = (int)kind;
        long current = this.used[index];
        if ((long)amount > (long)maximum - current)
        {
            this.RecordExceededLimit(kind);
            return false;
        }

        this.used[index] = current + amount;
        return true;
    }

    internal bool Allows(HtmlLimitKind kind, int value)
    {
        this.CheckCancellation();
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", value, "Measured work cannot be negative.");
        }

        int maximum = this.GetMaximum(kind);
        if (value > maximum)
        {
            this.RecordExceededLimit(kind);
            return false;
        }

        return true;
    }

    internal int GetUsed(HtmlLimitKind kind)
    {
        this.GetMaximum(kind);
        return (int)this.used[(int)kind];
    }

    internal bool HasExceededLimit()
    {
        return this.hasExceededLimit;
    }

    internal HtmlLimitKind GetFirstExceededLimit()
    {
        return this.firstExceededLimit;
    }

    internal void CheckCancellation()
    {
        this.cancellationToken.ThrowIfCancellationRequested();
    }

    internal int GetMaximum(HtmlLimitKind kind)
    {
        switch (kind)
        {
            case HtmlLimitKind.InputBytes:
                return this.limits.GetMaximumInputBytes();
            case HtmlLimitKind.InputCharacters:
                return this.limits.GetMaximumInputCharacters();
            case HtmlLimitKind.Tokens:
                return this.limits.GetMaximumTokens();
            case HtmlLimitKind.AttributesPerTag:
                return this.limits.GetMaximumAttributesPerTag();
            case HtmlLimitKind.TokenCharacters:
                return this.limits.GetMaximumTokenCharacters();
            case HtmlLimitKind.NameCharacters:
                return this.limits.GetMaximumNameCharacters();
            case HtmlLimitKind.AttributeValueCharacters:
                return this.limits.GetMaximumAttributeValueCharacters();
            case HtmlLimitKind.Nodes:
                return this.limits.GetMaximumNodes();
            case HtmlLimitKind.Depth:
                return this.limits.GetMaximumDepth();
            case HtmlLimitKind.SelectorComplexity:
                return this.limits.GetMaximumSelectorComplexity();
            case HtmlLimitKind.SelectorResults:
                return this.limits.GetMaximumSelectorResults();
            case HtmlLimitKind.HasWork:
                return this.limits.GetMaximumHasWork();
            case HtmlLimitKind.CssTokens:
                return this.limits.GetMaximumCssTokens();
            case HtmlLimitKind.CssDeclarations:
                return this.limits.GetMaximumCssDeclarations();
            case HtmlLimitKind.UrlCharacters:
                return this.limits.GetMaximumUrlCharacters();
            case HtmlLimitKind.DataUriBytes:
                return this.limits.GetMaximumDataUriBytes();
            case HtmlLimitKind.TableCells:
                return this.limits.GetMaximumTableCells();
            case HtmlLimitKind.JsonDepth:
                return this.limits.GetMaximumJsonDepth();
            case HtmlLimitKind.JsonProperties:
                return this.limits.GetMaximumJsonProperties();
            case HtmlLimitKind.Visits:
                return this.limits.GetMaximumVisits();
            case HtmlLimitKind.Diagnostics:
                return this.limits.GetMaximumDiagnostics();
            case HtmlLimitKind.OutputCharacters:
                return this.limits.GetMaximumOutputCharacters();
            default:
                throw new ArgumentOutOfRangeException("kind", (int)kind, "Unknown HTML limit kind.");
        }
    }

    internal void RecordExceededLimit(HtmlLimitKind kind)
    {
        if (!this.hasExceededLimit)
        {
            this.hasExceededLimit = true;
            this.firstExceededLimit = kind;
        }
    }
}
