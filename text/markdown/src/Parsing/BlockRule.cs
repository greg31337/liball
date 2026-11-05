using System;

namespace markdown;

internal class MarkdownBlockRule : IMarkdownBlockRule
{
    internal string Name;
    internal Func<MarkdownBlockParseContext, bool> Handler;

    internal MarkdownBlockRule(string name, Func<MarkdownBlockParseContext, bool> handler)
    {
        MarkdownRuleRegistration.ValidateName(name);

        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        Name = name;
        Handler = handler;
    }

    public string GetName()
    {
        return Name;
    }

    public bool TryParse(MarkdownBlockParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return Handler(context);
    }
}
