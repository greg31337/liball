using System;

namespace markdown;

internal class MarkdownHtmlRenderRule : IHtmlRenderRule
{
    internal string Name;
    internal MarkdownNodeKind FirstKind;
    internal MarkdownNodeKind SecondKind;
    internal bool HasSecondKind;
    internal Func<HtmlRenderContext, MarkdownNode, bool> Handler;

    internal MarkdownHtmlRenderRule(
        string name,
        MarkdownNodeKind kind,
        Func<HtmlRenderContext, MarkdownNode, bool> handler)
    {
        MarkdownRuleRegistration.ValidateName(name);
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        Name = name;
        FirstKind = kind;
        SecondKind = kind;
        HasSecondKind = false;
        Handler = handler;
    }

    internal static MarkdownHtmlRenderRule Create(
        string name,
        MarkdownNodeKind firstKind,
        MarkdownNodeKind secondKind,
        Func<HtmlRenderContext, MarkdownNode, bool> handler)
    {
        MarkdownRuleRegistration.ValidateName(name);
        if (handler == null)
        {
            throw new ArgumentNullException("handler");
        }

        MarkdownHtmlRenderRule rule = new MarkdownHtmlRenderRule(name, firstKind, handler);
        rule.SecondKind = secondKind;
        rule.HasSecondKind = true;
        return rule;
    }

    public string GetName()
    {
        return Name;
    }

    public bool TryRender(HtmlRenderContext context, MarkdownNode node)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind != FirstKind && (!HasSecondKind || node.Kind != SecondKind))
        {
            return false;
        }

        return Handler(context, node);
    }
}
