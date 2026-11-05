namespace UniversalHtml.FuzzRunner;

internal static class FuzzGenerators
{
    static readonly string[] HtmlTags =
    [
        "a", "b", "div", "form", "li", "math", "p", "script",
        "select", "span", "svg", "table", "template", "textarea"
    ];

    static readonly string[] VoidHtmlTags =
    [
        "br", "hr", "img", "input", "meta"
    ];

    static readonly string[] AttributeNames =
    [
        "class", "data-x", "href", "id", "name", "onclick", "src",
        "style", "title", "xlink:href"
    ];

    static readonly string[] TextPieces =
    [
        "alpha", "&amp;", "&#x3c;", "<", ">", "\0", "\r\n", " ",
        "é", "𝄞", "<!--", "</script", "{{value}}"
    ];

    static readonly string[] SelectorNames =
    [
        "a", "article", "div", "input", "li", "main", "p", "span",
        "svg", "table"
    ];

    static readonly string[] CssProperties =
    [
        "background-image", "behavior", "color", "display", "font-size",
        "margin", "opacity", "position", "width", "--custom"
    ];

    static readonly string[] Schemes =
    [
        "data", "file", "ftp", "http", "https", "javascript", "mailto"
    ];

    public static GeneratedCase Generate(FuzzTarget target, int caseIndex,
        ulong caseSeed, FuzzBudget budget, int maximumLength,
        int maximumDepth)
    {
        DeterministicRandom random = new DeterministicRandom(caseSeed);
        GrammarWriter writer = new GrammarWriter(budget, maximumLength);
        switch (target)
        {
            case FuzzTarget.Html:
                GenerateHtml(writer, random, maximumDepth);
                break;
            case FuzzTarget.Selector:
                GenerateSelector(writer, random, maximumDepth);
                break;
            case FuzzTarget.Css:
                GenerateCss(writer, random, maximumDepth);
                break;
            case FuzzTarget.Url:
                writer.Append(GenerateUrl(random, maximumDepth));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(target));
        }

        return new GeneratedCase(target, caseIndex, caseSeed,
            writer.ToString());
    }

    static void GenerateHtml(GrammarWriter writer,
        DeterministicRandom random, int maximumDepth)
    {
        if (random.Chance(1, 3))
        {
            writer.Append(random.Chance(4, 5)
                ? "<!doctype html>" : "<!DOCTYPE x PUBLIC 'broken'>");
        }

        int count = 1 + random.NextInt(5);
        for (int index = 0; index < count && writer.CanContinue(); index++)
        {
            AppendHtmlNode(writer, random, 0, maximumDepth);
        }
    }

    static void AppendHtmlNode(GrammarWriter writer,
        DeterministicRandom random, int depth, int maximumDepth)
    {
        if (depth >= maximumDepth || random.Chance(2, 7))
        {
            AppendHtmlText(writer, random);
            return;
        }

        int kind = random.NextInt(10);
        if (kind == 0)
        {
            writer.Append("<!--");
            writer.Append(random.Pick(TextPieces));
            if (random.Chance(3, 4))
            {
                writer.Append(random.Chance(1, 4) ? "--!>" : "-->");
            }

            return;
        }

        if (kind == 1)
        {
            writer.Append("</");
            writer.Append(random.Pick(HtmlTags));
            if (random.Chance(4, 5))
            {
                writer.Append('>');
            }

            return;
        }

        bool isVoid = kind == 2;
        string tag = isVoid ? random.Pick(VoidHtmlTags)
            : random.Pick(HtmlTags);
        writer.Append('<');
        writer.Append(random.Chance(1, 10) ? tag.ToUpperInvariant() : tag);
        AppendHtmlAttributes(writer, random, maximumDepth);
        if (random.Chance(1, 12))
        {
            return;
        }

        writer.Append(random.Chance(1, 8) ? "/>" : ">");
        if (isVoid)
        {
            return;
        }

        if (tag == "script" || tag == "textarea")
        {
            AppendRawText(writer, random, tag);
        }
        else
        {
            int children = random.NextInt(4);
            for (int index = 0; index < children && writer.CanContinue();
                index++)
            {
                AppendHtmlNode(writer, random, depth + 1, maximumDepth);
            }
        }

        int closing = random.NextInt(5);
        if (closing == 0)
        {
            return;
        }

        writer.Append("</");
        writer.Append(closing == 1 ? random.Pick(HtmlTags) : tag);
        if (closing != 2)
        {
            writer.Append('>');
        }
    }

    static void AppendHtmlAttributes(GrammarWriter writer,
        DeterministicRandom random, int maximumDepth)
    {
        int count = random.NextInt(4);
        for (int index = 0; index < count && writer.CanContinue(); index++)
        {
            string name = random.Pick(AttributeNames);
            writer.Append(random.Chance(1, 8) ? "\n" : " ");
            writer.Append(name);
            if (random.Chance(1, 5))
            {
                continue;
            }

            writer.Append(random.Chance(1, 10) ? " = " : "=");
            int quote = random.NextInt(4);
            char delimiter = quote == 1 ? '\'' : '"';
            if (quote != 0)
            {
                writer.Append(delimiter);
            }

            if (name == "style")
            {
                GenerateCss(writer, random, 2);
                if (quote != 0 && quote != 3)
                {
                    writer.Append(delimiter);
                }

                continue;
            }

            string value;
            if (name == "href" || name == "src" || name == "xlink:href")
            {
                value = GenerateUrl(random, maximumDepth);
            }
            else
            {
                value = random.Pick(TextPieces) + random.NextInt(100);
            }

            if (quote == 0)
            {
                writer.Append(value.Replace(" ", ""));
            }
            else
            {
                writer.Append(value);
                if (quote != 3)
                {
                    writer.Append(delimiter);
                }
            }
        }
    }

    static void AppendHtmlText(GrammarWriter writer,
        DeterministicRandom random)
    {
        int count = 1 + random.NextInt(4);
        for (int index = 0; index < count && writer.CanContinue(); index++)
        {
            writer.Append(random.Pick(TextPieces));
        }
    }

    static void AppendRawText(GrammarWriter writer,
        DeterministicRandom random, string tag)
    {
        string[] scriptPieces =
        [
            "x < y && z > 0;", "<!--escaped-->", "</scr" + "ipt>",
            "document.write('<b>')", "&not-an-entity;", "\0"
        ];
        writer.Append(random.Pick(scriptPieces));
        if (random.Chance(1, 3))
        {
            writer.Append("</" + tag.ToUpperInvariant() + ">");
        }
    }

    static void GenerateSelector(GrammarWriter writer,
        DeterministicRandom random, int maximumDepth)
    {
        int selectors = 1 + random.NextInt(3);
        for (int index = 0; index < selectors && writer.CanContinue(); index++)
        {
            if (index > 0)
            {
                writer.Append(random.Chance(4, 5) ? ", " : ",,");
            }

            AppendComplexSelector(writer, random, 0, maximumDepth);
        }

        if (random.Chance(1, 8))
        {
            writer.Append(random.Pick([">", "]", ")", "\\", ","]));
        }
    }

    static void AppendComplexSelector(GrammarWriter writer,
        DeterministicRandom random, int depth, int maximumDepth)
    {
        int compounds = 1 + random.NextInt(3);
        for (int index = 0; index < compounds && writer.CanContinue(); index++)
        {
            if (index > 0)
            {
                writer.Append(random.Pick([" ", " > ", " + ", " ~ "]));
            }

            AppendCompoundSelector(writer, random, depth, maximumDepth);
        }
    }

    static void AppendCompoundSelector(GrammarWriter writer,
        DeterministicRandom random, int depth, int maximumDepth)
    {
        if (random.Chance(4, 5))
        {
            if (random.Chance(1, 7))
            {
                writer.Append(random.Pick(["*|", "|", "svg|"]));
            }

            writer.Append(random.Chance(1, 8)
                ? "*" : random.Pick(SelectorNames));
        }

        int suffixes = 1 + random.NextInt(4);
        for (int index = 0; index < suffixes && writer.CanContinue(); index++)
        {
            int suffix = random.NextInt(5);
            if (suffix == 0)
            {
                writer.Append('#');
                AppendSelectorIdentifier(writer, random);
            }
            else if (suffix == 1)
            {
                writer.Append('.');
                AppendSelectorIdentifier(writer, random);
            }
            else if (suffix == 2)
            {
                AppendAttributeSelector(writer, random);
            }
            else
            {
                AppendPseudoClass(writer, random, depth, maximumDepth);
            }
        }
    }

    static void AppendSelectorIdentifier(GrammarWriter writer,
        DeterministicRandom random)
    {
        writer.Append(random.Pick([
            "active", "item-2", "\\31 23", "caf\\e9", "_hidden",
            "--custom", "x\\:y"
        ]));
    }

    static void AppendAttributeSelector(GrammarWriter writer,
        DeterministicRandom random)
    {
        writer.Append('[');
        writer.Append(random.Pick([
            "class", "data-x", "href", "id", "lang", "type", "viewBox"
        ]));
        if (random.Chance(3, 4))
        {
            writer.Append(random.Pick(["=", "~=", "|=", "^=", "$=", "*="]));
            if (random.Chance(1, 3))
            {
                writer.Append('"');
                writer.Append(random.Pick(["x", "a b", "https://", "\\22"]));
                if (!random.Chance(1, 8))
                {
                    writer.Append('"');
                }
            }
            else
            {
                AppendSelectorIdentifier(writer, random);
            }

            if (random.Chance(1, 3))
            {
                writer.Append(random.Chance(1, 2) ? " i" : " s");
            }
        }

        if (!random.Chance(1, 10))
        {
            writer.Append(']');
        }
    }

    static void AppendPseudoClass(GrammarWriter writer,
        DeterministicRandom random, int depth, int maximumDepth)
    {
        string[] simple =
        [
            ":any-link", ":checked", ":empty", ":first-child", ":root",
            ":scope"
        ];
        if (depth >= maximumDepth || random.Chance(1, 2))
        {
            writer.Append(random.Pick(simple));
            return;
        }

        int kind = random.NextInt(5);
        if (kind < 3)
        {
            writer.Append(kind == 0 ? ":is(" : kind == 1 ? ":not(" : ":has(");
            AppendComplexSelector(writer, random, depth + 1, maximumDepth);
            if (!random.Chance(1, 9))
            {
                writer.Append(')');
            }
        }
        else if (kind == 3)
        {
            writer.Append(random.Chance(1, 2)
                ? ":nth-child(" : ":nth-of-type(");
            writer.Append(random.Pick([
                "odd", "even", "2n+1", "-n+3", "+4", "n- 2"
            ]));
            if (random.Chance(1, 3))
            {
                writer.Append(" of ");
                AppendCompoundSelector(writer, random, depth + 1,
                    maximumDepth);
            }

            writer.Append(')');
        }
        else
        {
            writer.Append(random.Chance(1, 2) ? ":lang(" : ":dir(");
            writer.Append(random.Pick(["en", "en-US", "rtl", "auto", "*"]));
            writer.Append(')');
        }
    }

    static void GenerateCss(GrammarWriter writer,
        DeterministicRandom random, int maximumDepth)
    {
        int declarations = 1 + random.NextInt(7);
        for (int index = 0; index < declarations && writer.CanContinue();
            index++)
        {
            if (index > 0)
            {
                writer.Append(random.Chance(5, 6) ? ";" : "/*gap*/");
            }

            string property = random.Pick(CssProperties);
            writer.Append(random.Chance(1, 8)
                ? property.ToUpperInvariant() : property);
            writer.Append(random.Chance(1, 12) ? " " : ":");
            AppendCssValue(writer, random, property, 0, maximumDepth);
            if (random.Chance(1, 4))
            {
                writer.Append(random.Chance(4, 5) ? " !important" : " ! import");
            }
        }

        if (random.Chance(1, 5))
        {
            writer.Append(random.Pick([";", "}", "/*", "\\"]));
        }
    }

    static void AppendCssValue(GrammarWriter writer,
        DeterministicRandom random, string property, int depth,
        int maximumDepth)
    {
        if (property == "background-image" || random.Chance(1, 7))
        {
            writer.Append("url(");
            if (random.Chance(1, 2))
            {
                writer.Append('"');
            }

            writer.Append(GenerateUrl(random, maximumDepth));
            writer.Append(random.Chance(1, 7) ? "" : ")");
            return;
        }

        if (depth < maximumDepth && random.Chance(1, 4))
        {
            writer.Append(random.Pick(["calc(", "min(", "var(", "rgb("]));
            AppendCssValue(writer, random, "width", depth + 1,
                maximumDepth);
            writer.Append(random.Pick([",", "+", " "]));
            AppendCssValue(writer, random, "width", depth + 1,
                maximumDepth);
            if (!random.Chance(1, 8))
            {
                writer.Append(')');
            }

            return;
        }

        writer.Append(random.Pick([
            "0", "-1.5px", "100%", "#f00", "red", "none", "inherit",
            "expression(alert(1))", "\\6a avascript", "1e3"
        ]));
    }

    static string GenerateUrl(DeterministicRandom random, int maximumDepth)
    {
        int kind = random.NextInt(9);
        if (kind == 0)
        {
            return "../" + GeneratePath(random) + "?q="
                + random.NextInt(100) + "#part";
        }

        if (kind == 1)
        {
            return "//" + GenerateHost(random) + "/" + GeneratePath(random);
        }

        if (kind == 2)
        {
            return "#" + random.Pick(["top", "a%20b", "", "x:y"]);
        }

        if (kind == 3)
        {
            return "data:" + random.Pick([
                "text/plain,hello", "image/png;base64,AAAA", "text/html,<svg>"
            ]);
        }

        string scheme = random.Pick(Schemes);
        if (kind == 4)
        {
            scheme = scheme.Substring(0, 1) + "&#x61;" + scheme.Substring(1);
        }
        else if (kind == 5)
        {
            scheme = " " + scheme + "\t";
        }

        string authority = GenerateHost(random);
        if (random.Chance(1, 5))
        {
            authority = "user:pass@" + authority;
        }

        if (random.Chance(1, 4))
        {
            authority += ":" + random.Pick(["0", "80", "443", "65536", "x"]);
        }

        string url = scheme + "://" + authority + "/" + GeneratePath(random);
        if (maximumDepth > 0 && random.Chance(1, 3))
        {
            url += "?next=" + Uri.EscapeDataString(GeneratePath(random));
        }

        return url;
    }

    static string GenerateHost(DeterministicRandom random)
    {
        return random.Pick([
            "example.test", "sub.example.test", "127.0.0.1", "[::1]",
            "xn--bcher-kva.test", "bad host", "example..test"
        ]);
    }

    static string GeneratePath(DeterministicRandom random)
    {
        string first = random.Pick([
            "a", ".", "..", "%2e%2e", "caf%C3%A9", "a:b", "x y"
        ]);
        string second = random.Pick([
            "index.html", "", "%00", "file;v=1", "𝄞"
        ]);
        return first + "/" + second;
    }
}
