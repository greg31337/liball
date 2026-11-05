using System;

namespace html;

internal class HtmlSanitizerUrlDecision
{
    internal bool allowed;
    internal string value;
    internal bool rewritten;

    internal HtmlSanitizerUrlDecision(bool allowedValue, string valueValue,
        bool rewrittenValue)
    {
        if (valueValue == null)
        {
            throw new ArgumentNullException("valueValue");
        }

        if (!allowedValue && valueValue.Length != 0)
        {
            throw new ArgumentException(
                "A rejected URL decision cannot contain a value.",
                "valueValue");
        }

        this.allowed = allowedValue;
        this.value = valueValue;
        this.rewritten = rewrittenValue;
    }

    internal bool IsAllowed()
    {
        return this.allowed;
    }

    internal string GetValue()
    {
        return this.value;
    }

    internal bool WasRewritten()
    {
        return this.rewritten;
    }
}
