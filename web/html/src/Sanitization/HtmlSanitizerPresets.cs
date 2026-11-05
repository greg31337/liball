namespace html;

internal static class HtmlSanitizerPresets
{
    internal static HtmlCompiledSanitizerPolicy CreateTextOnly()
    {
        return new HtmlSanitizerPolicy().Compile();
    }

    internal static HtmlCompiledSanitizerPolicy CreateBasicFormatting()
    {
        return CreateBasicFormattingPolicy().Compile();
    }

    internal static HtmlCompiledSanitizerPolicy CreateRichText()
    {
        return CreateRichTextPolicy().Compile();
    }

    internal static HtmlCompiledSanitizerPolicy CreateHtmlOnly()
    {
        HtmlSanitizerPolicy policy = CreateRichTextPolicy();
        AllowElements(policy, new string[]
        {
            "audio", "canvas", "col", "colgroup", "datalist", "fieldset",
            "legend", "menu", "meter", "optgroup", "option", "picture",
            "progress", "source", "track", "video"
        });
        AllowAttributes(policy, "audio", new string[]
        {
            "controls", "loop", "muted", "preload", "src"
        });
        AllowAttributes(policy, "video", new string[]
        {
            "controls", "height", "loop", "muted", "playsinline",
            "poster", "preload", "src", "width"
        });
        AllowAttributes(policy, "source", new string[]
        {
            "height", "media", "src", "type", "width"
        });
        AllowAttributes(policy, "track", new string[]
        {
            "default", "kind", "label", "src", "srclang"
        });
        AllowAttributes(policy, "col", new string[] { "span" });
        AllowAttributes(policy, "colgroup", new string[] { "span" });
        AllowAttributes(policy, "fieldset", new string[] { "disabled" });
        AllowAttributes(policy, "optgroup", new string[]
        {
            "disabled", "label"
        });
        AllowAttributes(policy, "option", new string[]
        {
            "disabled", "label", "selected", "value"
        });
        AllowAttributes(policy, "canvas", new string[]
        {
            "height", "width"
        });
        AllowAttributes(policy, "meter", new string[]
        {
            "high", "low", "max", "min", "optimum", "value"
        });
        AllowAttributes(policy, "progress", new string[]
        {
            "max", "value"
        });
        return policy.Compile();
    }

    internal static HtmlSanitizerPolicy CreateBasicFormattingPolicy()
    {
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        AllowElements(policy, new string[]
        {
            "b", "br", "code", "em", "i", "mark", "p", "pre", "s",
            "small", "span", "strong", "sub", "sup", "u"
        });
        policy.AllowAttribute("dir");
        policy.AllowAttribute("lang");
        policy.AllowAttribute("title");
        return policy;
    }

    internal static HtmlSanitizerPolicy CreateRichTextPolicy()
    {
        HtmlSanitizerPolicy policy = CreateBasicFormattingPolicy();
        AllowElements(policy, new string[]
        {
            "a", "abbr", "address", "article", "aside", "bdi", "bdo",
            "blockquote", "caption", "cite", "data", "dd", "del",
            "details", "dfn", "div", "dl", "dt", "figcaption", "figure",
            "footer", "h1", "h2", "h3", "h4", "h5", "h6", "header",
            "hgroup", "hr", "img", "ins", "kbd", "li", "main", "nav",
            "ol", "q", "rp", "rt", "ruby", "samp", "section", "summary",
            "table", "tbody", "td", "tfoot", "th", "thead", "time", "tr",
            "ul", "var", "wbr"
        });
        policy.AllowAttribute("class");
        AllowAttributes(policy, "a", new string[]
        {
            "href", "hreflang", "rel", "target"
        });
        AllowAttributes(policy, "blockquote", new string[] { "cite" });
        AllowAttributes(policy, "q", new string[] { "cite" });
        AllowAttributes(policy, "data", new string[] { "value" });
        AllowAttributes(policy, "del", new string[] { "cite", "datetime" });
        AllowAttributes(policy, "ins", new string[] { "cite", "datetime" });
        AllowAttributes(policy, "details", new string[] { "open" });
        AllowAttributes(policy, "img", new string[]
        {
            "alt", "height", "loading", "src", "width"
        });
        AllowAttributes(policy, "ol", new string[]
        {
            "reversed", "start", "type"
        });
        AllowAttributes(policy, "li", new string[] { "value" });
        AllowAttributes(policy, "td", new string[]
        {
            "colspan", "headers", "rowspan"
        });
        AllowAttributes(policy, "th", new string[]
        {
            "colspan", "headers", "rowspan", "scope"
        });
        AllowAttributes(policy, "time", new string[] { "datetime" });
        policy.AllowUrlScheme("http");
        policy.AllowUrlScheme("https");
        policy.AllowUrlScheme("mailto");
        policy.AllowUrlScheme("tel");
        return policy;
    }

    internal static void AllowElements(HtmlSanitizerPolicy policy, string[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            policy.AllowElement(names[index]);
            index = index + 1;
        }
    }

    internal static void AllowAttributes(HtmlSanitizerPolicy policy,
        string elementName, string[] attributeNames)
    {
        int index = 0;
        while (index < attributeNames.Length)
        {
            policy.AllowAttributeOnElement(attributeNames[index], elementName);
            index = index + 1;
        }
    }
}
