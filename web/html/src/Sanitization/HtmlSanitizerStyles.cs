using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal enum HtmlSanitizerStyleDeclarationAction
{
    Removed = 0,
    Replaced = 1
}

internal class HtmlSanitizerStyleChange
{
    internal const int MaximumReportedPropertyNameLength = 64;

    internal HtmlSanitizerStyleDeclarationAction action;
    internal string propertyName;
    internal bool propertyNameRedacted;
    internal HtmlSourceSpan valueSpan;

    internal HtmlSanitizerStyleChange(
        HtmlSanitizerStyleDeclarationAction actionValue,
        string propertyNameValue, HtmlSourceSpan valueSpanValue)
    {
        if ((int)actionValue
                < (int)HtmlSanitizerStyleDeclarationAction.Removed
            || (int)actionValue
                > (int)HtmlSanitizerStyleDeclarationAction.Replaced)
        {
            throw new ArgumentOutOfRangeException("actionValue");
        }

        if (propertyNameValue == null)
        {
            throw new ArgumentNullException("propertyNameValue");
        }

        if (valueSpanValue == null)
        {
            throw new ArgumentNullException("valueSpanValue");
        }

        this.action = actionValue;
        this.propertyNameRedacted = propertyNameValue.Length
            > MaximumReportedPropertyNameLength;
        this.propertyName = this.propertyNameRedacted
            ? "" : propertyNameValue;
        this.valueSpan = valueSpanValue;
    }

    internal HtmlSanitizerStyleDeclarationAction GetAction()
    {
        return this.action;
    }

    internal string GetPropertyName()
    {
        return this.propertyName;
    }

    internal bool WasPropertyNameRedacted()
    {
        return this.propertyNameRedacted;
    }

    internal HtmlSourceSpan GetValueSpan()
    {
        return this.valueSpan;
    }
}

internal class HtmlSanitizerStyleResult
{
    internal bool success;
    internal string value;
    internal HtmlSanitizerStyleChange[] changes;
    internal int parseErrorCount;
    internal bool truncated;

    internal HtmlSanitizerStyleResult(bool successValue, string valueValue,
        HtmlSanitizerStyleChange[] changesValue, int parseErrorCountValue,
        bool truncatedValue)
    {
        if (valueValue == null)
        {
            throw new ArgumentNullException("valueValue");
        }

        if (changesValue == null)
        {
            throw new ArgumentNullException("changesValue");
        }

        if (parseErrorCountValue < 0)
        {
            throw new ArgumentOutOfRangeException("parseErrorCountValue");
        }

        this.success = successValue;
        this.value = valueValue;
        this.changes = CopyChanges(changesValue);
        this.parseErrorCount = parseErrorCountValue;
        this.truncated = truncatedValue;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal string GetValue()
    {
        return this.value;
    }

    internal HtmlSanitizerStyleChange[] GetChanges()
    {
        return CopyChanges(this.changes);
    }

    internal int GetParseErrorCount()
    {
        return this.parseErrorCount;
    }

    internal bool WasTruncated()
    {
        return this.truncated;
    }

    internal static HtmlSanitizerStyleChange[] CopyChanges(
        HtmlSanitizerStyleChange[] values)
    {
        HtmlSanitizerStyleChange[] result =
            new HtmlSanitizerStyleChange[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException(
                    "A style change collection cannot contain null.",
                    "changesValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }
}

internal static class HtmlSanitizerStyles
{
    internal static HtmlSanitizerStyleResult Sanitize(string value,
        HtmlCompiledSanitizerPolicy policy)
    {
        return Sanitize(value, policy, HtmlLimits.CreateDefault());
    }

    internal static HtmlSanitizerStyleResult Sanitize(string value,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits)
    {
        return Sanitize(value, policy, limits, CancellationToken.None);
    }

    internal static HtmlSanitizerStyleResult Sanitize(string value,
        HtmlCompiledSanitizerPolicy policy, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
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
        cancellationToken.ThrowIfCancellationRequested();
        CssDeclarationParseResult parsed = CssDeclarationParser.Parse(value,
            limits, cancellationToken);
        CssDeclaration[] declarations = parsed.GetDeclarations();
        List<HtmlSanitizerStyleChange> changes =
            new List<HtmlSanitizerStyleChange>();
        if (!parsed.IsSuccess() || parsed.WasTruncated())
        {
            AddRemovedChanges(changes, declarations);
            return new HtmlSanitizerStyleResult(false, "",
                changes.ToArray(), parsed.GetErrorCount(), true);
        }

        HtmlSanitizerUrlPolicy urlPolicy = policy.GetGlobalUrlPolicy();
        StringBuilder output = new StringBuilder();
        int index = 0;
        while (index < declarations.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CssDeclaration declaration = declarations[index];
            string name = declaration.GetCanonicalName();
            CssValueSanitizeResult sanitized = policy.IsCssPropertyAllowed(
                name) ? CssValueSanitizer.SanitizeWithPolicy(declaration,
                    urlPolicy, policy, limits, cancellationToken)
                : new CssValueSanitizeResult(false, "", false, false);
            if (!sanitized.IsAllowed())
            {
                changes.Add(CreateChange(
                    HtmlSanitizerStyleDeclarationAction.Removed,
                    declaration));
                index = index + 1;
                continue;
            }

            string serialized = SerializeDeclaration(declaration,
                sanitized.GetValue());
            if (output.Length > 0)
            {
                output.Append(';');
            }

            output.Append(serialized);
            if (serialized != GetOriginalDeclaration(value, declaration))
            {
                changes.Add(CreateChange(
                    HtmlSanitizerStyleDeclarationAction.Replaced,
                    declaration));
            }

            index = index + 1;
        }

        return new HtmlSanitizerStyleResult(true, output.ToString(),
            changes.ToArray(), parsed.GetErrorCount(), false);
    }

    internal static void AddRemovedChanges(List<HtmlSanitizerStyleChange> changes,
        CssDeclaration[] declarations)
    {
        int index = 0;
        while (index < declarations.Length)
        {
            changes.Add(CreateChange(
                HtmlSanitizerStyleDeclarationAction.Removed,
                declarations[index]));
            index = index + 1;
        }
    }

    internal static HtmlSanitizerStyleChange CreateChange(
        HtmlSanitizerStyleDeclarationAction action,
        CssDeclaration declaration)
    {
        return new HtmlSanitizerStyleChange(action,
            declaration.GetCanonicalName(), declaration.GetValueSpan());
    }

    internal static string SerializeDeclaration(CssDeclaration declaration,
        string value)
    {
        StringBuilder result = new StringBuilder();
        result.Append(declaration.GetCanonicalName());
        result.Append(':');
        result.Append(value);
        if (declaration.IsImportant())
        {
            result.Append("!important");
        }

        return result.ToString();
    }

    internal static string GetOriginalDeclaration(string value,
        CssDeclaration declaration)
    {
        HtmlSourceSpan span = declaration.GetSourceSpan();
        if (!span.IsKnown())
        {
            return "";
        }

        int start = span.GetStart().GetCharacterOffset();
        int end = span.GetEnd().GetCharacterOffset();
        if (start < 0 || end < start || end > value.Length)
        {
            return "";
        }

        if (end > start && value[end - 1] == ';')
        {
            end = end - 1;
        }

        return value.Substring(start, end - start);
    }
}
