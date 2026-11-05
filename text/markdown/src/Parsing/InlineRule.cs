using System;

namespace markdown;

internal class MarkdownInlineRule : IMarkdownInlineRule
{
    internal string Name;
    internal Func<MarkdownInlineParseContext, bool> Handler;

    internal MarkdownInlineRule(string name, Func<MarkdownInlineParseContext, bool> handler)
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

    public string GetTriggerCharacters()
    {
        return "";
    }

    public bool TryParse(MarkdownInlineParseContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return Handler(context);
    }
}
