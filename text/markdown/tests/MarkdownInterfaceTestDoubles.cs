namespace markdown.Tests;

internal class TestBlockRule : IMarkdownBlockRule
{
    internal string Name;
    internal Func<MarkdownBlockParseContext, bool> Handler;

    internal TestBlockRule(string name, Func<MarkdownBlockParseContext, bool> handler)
    {
        Name = name;
        Handler = handler;
    }

    public string GetName()
    {
        return Name;
    }

    public bool TryParse(MarkdownBlockParseContext context)
    {
        return Handler(context);
    }
}

internal class TestInlineRule : IMarkdownInlineRule
{
    internal string Name;
    internal string TriggerCharacters;
    internal Func<MarkdownInlineParseContext, bool> Handler;

    internal TestInlineRule(string name, Func<MarkdownInlineParseContext, bool> handler)
    {
        Name = name;
        TriggerCharacters = "";
        Handler = handler;
    }

    internal TestInlineRule(
        string name,
        string triggerCharacters,
        Func<MarkdownInlineParseContext, bool> handler)
    {
        Name = name;
        TriggerCharacters = triggerCharacters;
        Handler = handler;
    }

    public string GetName()
    {
        return Name;
    }

    public string GetTriggerCharacters()
    {
        return TriggerCharacters;
    }

    public bool TryParse(MarkdownInlineParseContext context)
    {
        return Handler(context);
    }
}

internal class TestTransform : IMarkdownTransform
{
    internal string Name;
    internal Action<MarkdownDocument, MarkdownTransformContext> Handler;

    internal TestTransform(string name, Action<MarkdownDocument, MarkdownTransformContext> handler)
    {
        Name = name;
        Handler = handler;
    }

    public string GetName()
    {
        return Name;
    }

    public void Apply(MarkdownDocument document, MarkdownTransformContext context)
    {
        Handler(document, context);
    }
}

internal class TestHtmlRenderRule : IHtmlRenderRule
{
    internal string Name;
    internal Func<HtmlRenderContext, MarkdownNode, bool> Handler;

    internal TestHtmlRenderRule(string name, Func<HtmlRenderContext, MarkdownNode, bool> handler)
    {
        Name = name;
        Handler = handler;
    }

    public string GetName()
    {
        return Name;
    }

    public bool TryRender(HtmlRenderContext context, MarkdownNode node)
    {
        return Handler(context, node);
    }
}

internal class TestNodeVisitor : IMarkdownNodeVisitor
{
    internal Func<MarkdownVisitContext, MarkdownTreeWalkResult> Enter;
    internal Action<MarkdownVisitContext> Leave;

    internal TestNodeVisitor(
        Func<MarkdownVisitContext, MarkdownTreeWalkResult> enter,
        Action<MarkdownVisitContext> leave)
    {
        Enter = enter;
        Leave = leave;
    }

    public MarkdownTreeWalkResult VisitEnter(MarkdownVisitContext context)
    {
        return Enter(context);
    }

    public void VisitLeave(MarkdownVisitContext context)
    {
        Leave(context);
    }
}
