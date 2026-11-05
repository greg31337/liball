namespace svg;

internal class SvgIdRewriteEntry
{
    public SvgNode Node;
    public SvgAttribute Attribute;
    public string OldId;
    public string NewId;

    public SvgIdRewriteEntry(SvgNode node, SvgAttribute attribute, string oldId, string newId)
    {
        Node = node;
        Attribute = attribute;
        OldId = oldId;
        NewId = newId;
    }
}

internal class SvgIdTextReplacement
{
    public int Start;
    public int End;
    public string Text;

    public SvgIdTextReplacement(int start, int end, string text)
    {
        Start = start;
        End = end;
        Text = text;
    }
}

internal static class SvgIdRewriter
{
    internal static void Prefix(SvgProcessContext context, SvgIdPrefixOptions options)
    {
        if (!Valid(options))
        {
            context.Fail("Identifier prefix settings are invalid.");
            return;
        }
        Rewrite(context, options.Prefix, false, options.AdditionalIdReferenceAttributes, options.MaximumCssCharacters, options.MaximumCssTokens);
    }

    internal static void PrefixSubtree(SvgProcessContext context, SvgNode root, SvgIdPrefixOptions options)
    {
        if (root == null || !Valid(options))
        {
            context.Fail("Identifier prefix settings are invalid.");
            return;
        }
        RewriteNodes(context, CollectNodes(root), options.Prefix, false, options.AdditionalIdReferenceAttributes, options.MaximumCssCharacters, options.MaximumCssTokens);
    }

    internal static void Minify(SvgProcessContext context, SvgIdMinifyOptions options)
    {
        if (!Valid(options))
        {
            context.Fail("Identifier minification settings are invalid.");
            return;
        }
        Rewrite(context, "", true, options.AdditionalIdReferenceAttributes, options.MaximumCssCharacters, options.MaximumCssTokens);
    }

    static bool Valid(SvgIdPrefixOptions options)
    {
        return options != null && options.Prefix != null && options.Prefix.Length != 0 && ValidNames(options.AdditionalIdReferenceAttributes) && options.MaximumCssCharacters > 0 && options.MaximumCssTokens > 0;
    }

    static bool Valid(SvgIdMinifyOptions options)
    {
        return options != null && ValidNames(options.AdditionalIdReferenceAttributes) && options.MaximumCssCharacters > 0 && options.MaximumCssTokens > 0;
    }

    static bool ValidNames(System.Collections.Generic.List<string>? names)
    {
        if (names == null) return false;
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            if (name == null || name.Length == 0 || name.Trim() != name || name.IndexOf(':') >= 0) return false;
            index = index + 1;
        }
        return true;
    }

    static void Rewrite(SvgProcessContext context, string prefix, bool minify, System.Collections.Generic.List<string> additional, long maximumCharacters, int maximumTokens)
    {
        System.Collections.Generic.List<SvgNode> nodes = CollectNodes(context.Document);
        RewriteNodes(context, nodes, prefix, minify, additional, maximumCharacters, maximumTokens);
    }

    static void RewriteNodes(SvgProcessContext context, System.Collections.Generic.List<SvgNode> nodes, string prefix, bool minify, System.Collections.Generic.List<string> additional, long maximumCharacters, int maximumTokens)
    {
        System.Collections.Generic.List<SvgIdRewriteEntry> entries = BuildEntries(context, nodes, prefix, minify);
        if (context.IsFailed() || entries.Count == 0) return;
        System.Collections.Generic.Dictionary<string, string> mapping = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < entries.Count)
        {
            mapping.Add(entries[index].OldId, entries[index].NewId);
            index = index + 1;
        }
        index = 0;
        while (index < nodes.Count && !context.IsFailed())
        {
            RewriteNodeReferences(context, nodes[index], mapping, additional, maximumCharacters, maximumTokens);
            index = index + 1;
        }
        index = 0;
        while (index < entries.Count && !context.IsFailed())
        {
            SvgIdRewriteEntry entry = entries[index];
            ReplaceAttribute(context, entry.Node, entry.Attribute, entry.NewId);
            index = index + 1;
        }
    }

    static System.Collections.Generic.List<SvgIdRewriteEntry> BuildEntries(SvgProcessContext context, System.Collections.Generic.List<SvgNode> nodes, string prefix, bool minify)
    {
        System.Collections.Generic.List<SvgIdRewriteEntry> result = new System.Collections.Generic.List<SvgIdRewriteEntry>();
        System.Collections.Generic.Dictionary<string, SvgNode> owners = new System.Collections.Generic.Dictionary<string, SvgNode>(System.StringComparer.Ordinal);
        System.Collections.Generic.HashSet<string> reserved = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < nodes.Count)
        {
            SvgAttribute? attribute = IdAttribute(nodes[index]);
            if (attribute != null)
            {
                if (attribute.Value.Length == 0 || owners.ContainsKey(attribute.Value))
                {
                    context.Fail("Identifiers must be nonempty and unique before they can be rewritten.");
                    return result;
                }
                owners.Add(attribute.Value, nodes[index]);
                if (!minify && attribute.Value.StartsWith(prefix, System.StringComparison.Ordinal)) reserved.Add(attribute.Value);
            }
            index = index + 1;
        }
        index = 0;
        int ordinal = 0;
        while (index < nodes.Count)
        {
            SvgAttribute? attribute = IdAttribute(nodes[index]);
            if (attribute != null)
            {
                string oldId = attribute.Value;
                string newId;
                if (minify)
                {
                    newId = CompactId(ordinal);
                    while (reserved.Contains(newId))
                    {
                        ordinal = ordinal + 1;
                        newId = CompactId(ordinal);
                    }
                    ordinal = ordinal + 1;
                }
                else if (oldId.StartsWith(prefix, System.StringComparison.Ordinal)) newId = oldId;
                else
                {
                    newId = prefix + oldId;
                    while (reserved.Contains(newId)) newId = prefix + newId;
                }
                reserved.Add(newId);
                if (newId != oldId) result.Add(new SvgIdRewriteEntry(nodes[index], attribute, oldId, newId));
            }
            index = index + 1;
        }
        return result;
    }

    static string CompactId(int ordinal)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int value = ordinal;
        while (true)
        {
            builder.Insert(0, alphabet[value % alphabet.Length]);
            value = value / alphabet.Length - 1;
            if (value < 0) return builder.ToString();
        }
    }

    static void RewriteNodeReferences(SvgProcessContext context, SvgNode node, System.Collections.Generic.Dictionary<string, string> mapping, System.Collections.Generic.List<string> additional, long maximumCharacters, int maximumTokens)
    {
        if (node.Kind != SvgNodeKind.Element) return;
        System.Collections.Generic.List<SvgAttribute> attributes = new System.Collections.Generic.List<SvgAttribute>(node.Attributes);
        int index = 0;
        while (index < attributes.Count && !context.IsFailed())
        {
            SvgAttribute attribute = attributes[index];
            if (!IsIdAttribute(attribute))
            {
                string replacement = RewriteAttribute(attribute, mapping, additional, maximumCharacters, maximumTokens, context);
                if (!context.IsFailed() && replacement != attribute.Value) ReplaceAttribute(context, node, attribute, replacement);
            }
            index = index + 1;
        }
        if (context.IsFailed() || node.ElementKind != SvgElementKind.Style) return;
        string css = new SvgNodeApi().GetTextContent(node);
        string rewritten = RewriteCss(css, mapping, true, maximumCharacters, maximumTokens, context);
        if (!context.IsFailed() && rewritten != css)
        {
            SvgMutationResult mutation = context.Mutations.SetTextContent(node, rewritten);
            if (!mutation.Success) context.Fail(mutation.Message);
        }
    }

    static string RewriteAttribute(SvgAttribute attribute, System.Collections.Generic.Dictionary<string, string> mapping, System.Collections.Generic.List<string> additional, long maximumCharacters, int maximumTokens, SvgProcessContext context)
    {
        string value = attribute.Value;
        if (HrefAttribute(attribute)) value = RewriteHref(value, mapping);
        if (IdReferenceAttribute(attribute) || AdditionalAttribute(attribute, additional)) value = RewriteWords(value, mapping);
        if (TimingAttribute(attribute)) value = RewriteTiming(value, mapping);
        return RewriteCss(value, mapping, false, maximumCharacters, maximumTokens, context);
    }

    static string RewriteHref(string value, System.Collections.Generic.Dictionary<string, string> mapping)
    {
        int start = 0;
        int end = value.Length;
        while (start < end && char.IsWhiteSpace(value[start])) start = start + 1;
        while (end > start && char.IsWhiteSpace(value[end - 1])) end = end - 1;
        if (end - start < 2 || value[start] != '#') return value;
        string id = value.Substring(start + 1, end - start - 1);
        string? replacement;
        if (!mapping.TryGetValue(id, out replacement)) return value;
        return value.Substring(0, start) + "#" + replacement + value.Substring(end);
    }

    static string RewriteWords(string value, System.Collections.Generic.Dictionary<string, string> mapping)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsWhiteSpace(value[index]))
            {
                builder.Append(value[index]);
                index = index + 1;
                continue;
            }
            int start = index;
            while (index < value.Length && !char.IsWhiteSpace(value[index])) index = index + 1;
            string word = value.Substring(start, index - start);
            string? replacement;
            builder.Append(mapping.TryGetValue(word, out replacement) ? replacement : word);
        }
        return builder.ToString();
    }

    static string RewriteTiming(string value, System.Collections.Generic.Dictionary<string, string> mapping)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        int copied = 0;
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length && (char.IsWhiteSpace(value[index]) || value[index] == ';')) index = index + 1;
            int start = index;
            while (index < value.Length && value[index] != '.' && value[index] != ';' && !char.IsWhiteSpace(value[index])) index = index + 1;
            string id = value.Substring(start, index - start);
            string? replacement;
            if (index < value.Length && value[index] == '.' && mapping.TryGetValue(id, out replacement))
            {
                builder.Append(value, copied, start - copied);
                builder.Append(replacement);
                copied = index;
            }
            while (index < value.Length && value[index] != ';') index = index + 1;
        }
        if (copied == 0) return value;
        builder.Append(value, copied, value.Length - copied);
        return builder.ToString();
    }

    static string RewriteCss(string value, System.Collections.Generic.Dictionary<string, string> mapping, bool styleSheet, long maximumCharacters, int maximumTokens, SvgProcessContext context)
    {
        if (value.IndexOf('#') < 0 && value.IndexOf("url", System.StringComparison.OrdinalIgnoreCase) < 0) return value;
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, maximumTokens, maximumCharacters);
        if (!tokenized.Success)
        {
            context.Fail("CSS containing identifier references exceeded the configured rewrite bounds.");
            return value;
        }
        System.Collections.Generic.List<SvgSourceSpan> selectorRanges = new System.Collections.Generic.List<SvgSourceSpan>();
        if (styleSheet)
        {
            SvgCssLimits limits = new SvgCssLimits();
            limits.MaxCharacters = maximumCharacters;
            limits.MaxTokens = maximumTokens;
            limits.MaxSelectorTokens = maximumTokens;
            SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(value, limits);
            if (!parsed.Success || parsed.StyleSheet == null)
            {
                context.Fail("A stylesheet containing identifier references could not be parsed within the configured bounds.");
                return value;
            }
            CollectSelectorRanges(parsed.StyleSheet.Rules, selectorRanges);
        }
        System.Collections.Generic.List<SvgIdTextReplacement> replacements = new System.Collections.Generic.List<SvgIdTextReplacement>();
        int index = 0;
        int previous = -1;
        while (index < tokenized.Tokens.Count)
        {
            SvgCssToken token = tokenized.Tokens[index];
            string? replacement = token.Kind == SvgCssTokenKind.Url ? LocalCssReference(token.Value, mapping) : null;
            if (replacement != null) replacements.Add(new SvgIdTextReplacement(token.StartOffset, token.EndOffset, "url(#" + CssIdentifier(replacement) + ")"));
            else if (token.Kind == SvgCssTokenKind.String && previous >= 0 && tokenized.Tokens[previous].Kind == SvgCssTokenKind.Function && string.Equals(tokenized.Tokens[previous].Value, "url", System.StringComparison.OrdinalIgnoreCase))
            {
                replacement = LocalCssReference(token.Value, mapping);
                if (replacement != null) replacements.Add(new SvgIdTextReplacement(token.StartOffset, token.EndOffset, Quote(token.RawText, "#" + replacement)));
            }
            else if (styleSheet && InRanges(token.StartOffset, selectorRanges))
            {
                replacement = SelectorAttributeReplacement(tokenized.Tokens, index, mapping);
                if (replacement != null) replacements.Add(new SvgIdTextReplacement(token.StartOffset, token.EndOffset, SelectorToken(token, replacement)));
                else if (token.Kind == SvgCssTokenKind.Hash && mapping.ContainsKey(token.Value)) replacements.Add(new SvgIdTextReplacement(token.StartOffset, token.EndOffset, "#" + CssIdentifier(mapping[token.Value])));
            }
            if (token.Kind != SvgCssTokenKind.Whitespace && token.Kind != SvgCssTokenKind.Comment) previous = index;
            index = index + 1;
        }
        return Apply(value, replacements);
    }

    static void CollectSelectorRanges(System.Collections.Generic.List<SvgCssRule> rules, System.Collections.Generic.List<SvgSourceSpan> result)
    {
        int index = 0;
        while (index < rules.Count)
        {
            SvgCssRule rule = rules[index];
            if (rule.Selectors != null && rule.Selectors.Source != null) result.Add(rule.Selectors.Source);
            CollectSelectorRanges(rule.ChildRules, result);
            index = index + 1;
        }
    }

    static bool InRanges(int offset, System.Collections.Generic.List<SvgSourceSpan> ranges)
    {
        int index = 0;
        while (index < ranges.Count)
        {
            if (ranges[index].StartOffset <= offset && offset < ranges[index].EndOffset) return true;
            index = index + 1;
        }
        return false;
    }

    static string? LocalCssReference(string value, System.Collections.Generic.Dictionary<string, string> mapping)
    {
        if (value.Length <= 1 || value[0] != '#') return null;
        string id = value.Substring(1);
        return mapping.ContainsKey(id) ? mapping[id] : null;
    }

    static string? SelectorAttributeReplacement(System.Collections.Generic.List<SvgCssToken> tokens, int tokenIndex, System.Collections.Generic.Dictionary<string, string> mapping)
    {
        SvgCssToken value = tokens[tokenIndex];
        if (value.Kind != SvgCssTokenKind.Ident && value.Kind != SvgCssTokenKind.String && value.Kind != SvgCssTokenKind.Hash) return null;
        int open = tokenIndex - 1;
        while (open >= 0 && tokens[open].Kind != SvgCssTokenKind.OpenSquare && tokens[open].Kind != SvgCssTokenKind.CloseSquare) open = open - 1;
        if (open < 0 || tokens[open].Kind != SvgCssTokenKind.OpenSquare) return null;
        int equals = open + 1;
        while (equals < tokenIndex && !(tokens[equals].Kind == SvgCssTokenKind.Delimiter && tokens[equals].Value == "=")) equals = equals + 1;
        if (equals >= tokenIndex) return null;
        int nameIndex = equals - 1;
        while (nameIndex > open && tokens[nameIndex].Kind != SvgCssTokenKind.Ident) nameIndex = nameIndex - 1;
        if (nameIndex <= open) return null;
        string name = tokens[nameIndex].Value;
        string? id = null;
        if (string.Equals(name, "id", System.StringComparison.OrdinalIgnoreCase)) id = value.Value;
        else if (string.Equals(name, "href", System.StringComparison.OrdinalIgnoreCase) && value.Kind != SvgCssTokenKind.Hash && value.Value.Length > 1 && value.Value[0] == '#') id = value.Value.Substring(1);
        else if (string.Equals(name, "href", System.StringComparison.OrdinalIgnoreCase) && value.Kind == SvgCssTokenKind.Hash) id = value.Value;
        return id != null && mapping.ContainsKey(id) ? mapping[id] : null;
    }

    static string SelectorToken(SvgCssToken token, string replacement)
    {
        bool fragment = token.Value.Length > 0 && token.Value[0] == '#' || token.Kind == SvgCssTokenKind.Hash;
        string value = (fragment ? "#" : "") + replacement;
        if (token.Kind == SvgCssTokenKind.String) return Quote(token.RawText, value);
        if (token.Kind == SvgCssTokenKind.Hash) return "#" + CssIdentifier(replacement);
        return CssIdentifier(value);
    }

    static string Quote(string raw, string value)
    {
        char quote = raw.Length > 0 && raw[0] == '\'' ? '\'' : '"';
        return quote.ToString() + value + quote.ToString();
    }

    static string CssIdentifier(string value)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool safe = character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' || character == '_' || character == '-' || character >= '0' && character <= '9' && index > 0 && !(index == 1 && value[0] == '-');
            if (safe) builder.Append(character);
            else
            {
                builder.Append('\\');
                builder.Append(((int)character).ToString("x", System.Globalization.CultureInfo.InvariantCulture));
                builder.Append(' ');
            }
            index = index + 1;
        }
        return builder.ToString();
    }

    static string Apply(string value, System.Collections.Generic.List<SvgIdTextReplacement> replacements)
    {
        if (replacements.Count == 0) return value;
        int first = 0;
        while (first < replacements.Count - 1)
        {
            int smallest = first;
            int second = first + 1;
            while (second < replacements.Count)
            {
                if (replacements[second].Start < replacements[smallest].Start) smallest = second;
                second = second + 1;
            }
            SvgIdTextReplacement swap = replacements[first];
            replacements[first] = replacements[smallest];
            replacements[smallest] = swap;
            first = first + 1;
        }
        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
        int copied = 0;
        int index = 0;
        while (index < replacements.Count)
        {
            SvgIdTextReplacement replacement = replacements[index];
            if (replacement.Start >= copied)
            {
                builder.Append(value, copied, replacement.Start - copied);
                builder.Append(replacement.Text);
                copied = replacement.End;
            }
            index = index + 1;
        }
        builder.Append(value, copied, value.Length - copied);
        return builder.ToString();
    }

    static bool AdditionalAttribute(SvgAttribute attribute, System.Collections.Generic.List<string> names)
    {
        if (attribute.NamespaceUri.Length != 0) return false;
        int index = 0;
        while (index < names.Count)
        {
            if (attribute.LocalName == names[index]) return true;
            index = index + 1;
        }
        return false;
    }

    static bool IdReferenceAttribute(SvgAttribute attribute)
    {
        if (attribute.NamespaceUri.Length != 0) return false;
        string name = attribute.LocalName;
        return name == "aria-labelledby" || name == "aria-describedby" || name == "aria-controls" || name == "aria-owns" || name == "aria-details" || name == "aria-errormessage" || name == "aria-activedescendant" || name == "aria-flowto";
    }

    static bool TimingAttribute(SvgAttribute attribute)
    {
        return attribute.NamespaceUri.Length == 0 && (attribute.LocalName == "begin" || attribute.LocalName == "end");
    }

    static bool HrefAttribute(SvgAttribute attribute)
    {
        return attribute.LocalName == SvgAttributeNames.Href && (attribute.NamespaceUri.Length == 0 || attribute.NamespaceUri == SvgNamespaces.XLink);
    }

    static SvgAttribute? IdAttribute(SvgNode node)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (IsIdAttribute(node.Attributes[index])) return node.Attributes[index];
            index = index + 1;
        }
        return null;
    }

    static bool IsIdAttribute(SvgAttribute attribute)
    {
        return attribute.NamespaceUri.Length == 0 && attribute.LocalName == SvgAttributeNames.Id;
    }

    static void ReplaceAttribute(SvgProcessContext context, SvgNode node, SvgAttribute current, string value)
    {
        SvgAttribute replacement = context.Documents.CreateAttribute(current.QualifiedName, current.Prefix, current.LocalName, current.NamespaceUri, value);
        SvgMutationResult mutation = context.Mutations.ReplaceAttribute(node, current, replacement);
        if (!mutation.Success) context.Fail(mutation.Message);
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgDocument document)
    {
        return CollectNodes(document.DocumentNode);
    }

    static System.Collections.Generic.List<SvgNode> CollectNodes(SvgNode root)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        System.Collections.Generic.List<SvgNode> pending = new System.Collections.Generic.List<SvgNode>();
        pending.Add(root);
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            int index = node.Children.Count - 1;
            while (index >= 0)
            {
                pending.Add(node.Children[index]);
                index = index - 1;
            }
        }
        return result;
    }
}
