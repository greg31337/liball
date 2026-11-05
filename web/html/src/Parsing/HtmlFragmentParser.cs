using System;

namespace html;

internal class HtmlFragmentParser
{
    internal HtmlNode contextElement;
    internal HtmlNode fragment;
    internal HtmlNode? syntheticRoot;
    internal HtmlTreeBuilder treeBuilder;
    internal HtmlTokenizerState tokenizerState;

    internal HtmlFragmentParser(HtmlNode contextElementValue,
        HtmlNode fragmentValue, HtmlNode? syntheticRootValue,
        HtmlTreeBuilder treeBuilderValue,
        HtmlTokenizerState tokenizerStateValue)
    {
        this.contextElement = contextElementValue;
        this.fragment = fragmentValue;
        this.syntheticRoot = syntheticRootValue;
        this.treeBuilder = treeBuilderValue;
        this.tokenizerState = tokenizerStateValue;
    }

    internal static HtmlFragmentParser Create(HtmlNode target,
        bool scriptingEnabled, HtmlBudget budget)
    {
        HtmlNode? context = GetContextElement(target);
        if (context == null)
        {
            throw new ArgumentException(
                "A fragment target must be an element or template content.",
                "target");
        }

        HtmlDocument document = context.GetOwnerDocumentStorage();
        HtmlNode fragment = document.CreateFragmentStorage(
            HtmlSourceSpan.CreateUnknown());
        HtmlTreeBuilder builder = new HtmlTreeBuilder(document, budget,
            context, fragment);
        builder.SetScriptingEnabled(scriptingEnabled);
        HtmlNode? root = builder.CreateGeneratedHtmlElement("html");
        if (root != null)
        {
            builder.GetOpenElements().Push(root);
        }

        if (context.GetNamespaceStorage() == HtmlNamespace.Html
            && context.GetLocalNameStorage() == "template")
        {
            builder.PushTemplateInsertionMode(HtmlInsertionMode.InTemplate);
        }

        builder.ResetInsertionModeAppropriately();
        HtmlNode? form = FindNearestForm(context);
        if (form != null)
        {
            builder.SetFormElement(form);
        }

        HtmlTokenizerState state = GetTokenizerState(context,
            scriptingEnabled);
        return new HtmlFragmentParser(context, fragment, root, builder,
            state);
    }

    internal HtmlNode GetContextElement()
    {
        return this.contextElement;
    }

    internal HtmlNode GetFragment()
    {
        return this.fragment;
    }

    internal HtmlNode? GetSyntheticRoot()
    {
        return this.syntheticRoot;
    }

    internal HtmlTreeBuilder GetTreeBuilder()
    {
        return this.treeBuilder;
    }

    internal HtmlTokenizerState GetTokenizerState()
    {
        return this.tokenizerState;
    }

    internal void InitializeTokenizer(HtmlTokenizer tokenizer)
    {
        this.InitializeTokenizerControl(new HtmlTokenizerControl(tokenizer));
    }

    internal void InitializeTokenizerControl(IHtmlTokenizerControl tokenizer)
    {
        if (tokenizer == null)
        {
            throw new ArgumentNullException("tokenizer");
        }

        tokenizer.InitializeForFragment(this.tokenizerState,
            this.contextElement.GetLocalNameStorage(),
            this.treeBuilder.IsCdataAllowed());
    }

    internal void UpdateTokenizerCdata(HtmlTokenizer tokenizer)
    {
        this.UpdateTokenizerCdataControl(new HtmlTokenizerControl(tokenizer));
    }

    internal void UpdateTokenizerCdataControl(IHtmlTokenizerControl tokenizer)
    {
        if (tokenizer == null)
        {
            throw new ArgumentNullException("tokenizer");
        }

        tokenizer.SetCdataAllowed(this.treeBuilder.IsCdataAllowed());
    }

    internal static HtmlNode? GetContextElement(HtmlNode target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (target.GetKindStorage() == HtmlNodeKind.Element)
        {
            return target;
        }

        if (target.GetKindStorage() == HtmlNodeKind.Fragment
            && target.GetTemplateHostStorage() != null)
        {
            return target.GetTemplateHostStorage();
        }

        throw new ArgumentException(
            "A fragment target must be an element or template content.",
            "target");
    }

    internal static HtmlNode? FindNearestForm(HtmlNode context)
    {
        HtmlNode? current = context;
        while (current != null)
        {
            if (current.GetKindStorage() == HtmlNodeKind.Element
                && current.GetNamespaceStorage() == HtmlNamespace.Html
                && current.GetLocalNameStorage() == "form")
            {
                return current;
            }

            current = current.GetParentStorage();
        }

        return null;
    }

    internal static HtmlTokenizerState GetTokenizerState(HtmlNode context,
        bool scriptingEnabled)
    {
        if (context.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return HtmlTokenizerState.Data;
        }

        string name = context.GetLocalNameStorage();
        if (name == "title" || name == "textarea")
        {
            return HtmlTokenizerState.Rcdata;
        }

        if (name == "style" || name == "xmp" || name == "iframe"
            || name == "noembed" || name == "noframes"
            || name == "noscript" && scriptingEnabled)
        {
            return HtmlTokenizerState.RawText;
        }

        if (name == "script")
        {
            return HtmlTokenizerState.ScriptData;
        }

        if (name == "plaintext")
        {
            return HtmlTokenizerState.PlainText;
        }

        return HtmlTokenizerState.Data;
    }
}
