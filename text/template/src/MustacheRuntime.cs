using System;
using System.Collections.Generic;
using System.Text;

namespace template;

internal class MustacheNode
{
    internal string Kind;
    internal string Name;
    internal string Text;
    internal string RawText;
    internal string StartDelimiter;
    internal string EndDelimiter;
    internal string Indentation;
    internal bool Standalone;
    internal List<MustacheNode> Children;

    internal MustacheNode(string kind)
    {
        Kind = kind;
        Name = "";
        Text = "";
        RawText = "";
        StartDelimiter = "{{";
        EndDelimiter = "}}";
        Indentation = "";
        Standalone = false;
        Children = new List<MustacheNode>();
    }
}

internal class MustacheParser
{
    internal string Text;
    internal string StartDelimiter;
    internal string EndDelimiter;
    internal MustacheNode Root;
    internal List<MustacheNode> Stack;
    internal List<int> ContentStarts;

    internal MustacheParser(string text, string startDelimiter, string endDelimiter)
    {
        Text = text;
        StartDelimiter = startDelimiter;
        EndDelimiter = endDelimiter;
        Root = new MustacheNode("root");
        Stack = new List<MustacheNode>();
        Stack.Add(Root);
        ContentStarts = new List<int>();
        ContentStarts.Add(0);
    }

    internal MustacheNode ParseDocument()
    {
        int index = 0;
        while (index < Text.Length)
        {
            int tagStart = Text.IndexOf(StartDelimiter, index, StringComparison.Ordinal);
            if (tagStart < 0)
            {
                AddTextNode(Text.Substring(index));
                index = Text.Length;
                continue;
            }

            bool triple = StartDelimiter == "{{" && StartsWithValue(tagStart, "{{{");
            string tagEndDelimiter = triple ? "}}}" : EndDelimiter;
            int contentStart = tagStart + (triple ? 3 : StartDelimiter.Length);
            int tagEndStart = Text.IndexOf(tagEndDelimiter, contentStart, StringComparison.Ordinal);
            if (tagEndStart < 0)
            {
                throw new InvalidOperationException("Unclosed Mustache tag.");
            }

            int tagEnd = tagEndStart + tagEndDelimiter.Length;
            string tag = Text.Substring(contentStart, tagEndStart - contentStart).Trim();
            string kind = GetTagKind(tag, triple);
            int lineStart = FindLineStart(tagStart);
            int lineEnd = FindLineEnd(tagEnd);
            bool standalone = IsStandaloneTag(kind, lineStart, tagStart, tagEnd, lineEnd);
            int textEnd = standalone && lineStart >= index ? lineStart : tagStart;
            if (textEnd > index)
            {
                AddTextNode(Text.Substring(index, textEnd - index));
            }

            string indentation = standalone && IsWhitespaceOnly(lineStart, tagStart) ? Text.Substring(lineStart, tagStart - lineStart) : "";
            bool lastTagOnLine = IsWhitespaceOnly(tagEnd, lineEnd);
            int nextIndex = standalone && lastTagOnLine ? SkipLineEnding(lineEnd) : tagEnd;
            ProcessTag(tag, kind, triple, standalone, indentation, tagStart, textEnd, nextIndex);
            index = nextIndex;
        }

        if (Stack.Count != 1)
        {
            throw new InvalidOperationException("Unclosed Mustache section '" + Stack[Stack.Count - 1].Name + "'.");
        }

        return Root;
    }

    internal void ProcessTag(string tag, string kind, bool triple, bool standalone, string indentation, int tagStart, int textEnd, int nextIndex)
    {
        if (kind == "comment")
        {
            return;
        }

        if (kind == "delimiter")
        {
            ChangeDelimiters(tag);
            return;
        }

        if (kind == "close")
        {
            CloseNode(tag.Substring(1).Trim(), standalone ? textEnd : tagStart);
            return;
        }

        MustacheNode node = new MustacheNode(kind);
        node.StartDelimiter = StartDelimiter;
        node.EndDelimiter = EndDelimiter;
        node.Standalone = standalone;
        node.Indentation = indentation;
        if (kind == "variable")
        {
            node.Name = tag;
        }
        else if (kind == "unescaped")
        {
            node.Name = triple ? tag : tag.Substring(1).Trim();
        }
        else
        {
            node.Name = tag.Substring(1).Trim();
        }

        CurrentNode().Children.Add(node);
        if (kind == "section" || kind == "inverted" || kind == "parent" || kind == "block")
        {
            Stack.Add(node);
            ContentStarts.Add(nextIndex);
        }
    }

    internal void CloseNode(string name, int rawEnd)
    {
        if (Stack.Count <= 1)
        {
            throw new InvalidOperationException("Unexpected Mustache closing tag '" + name + "'.");
        }

        MustacheNode node = Stack[Stack.Count - 1];
        bool dynamicMatch = node.Name.Length > 1 && node.Name[0] == '*' && node.Name.Substring(1).Trim() == name;
        if (node.Name != name && !dynamicMatch)
        {
            throw new InvalidOperationException("Mustache closing tag '" + name + "' does not match '" + node.Name + "'.");
        }

        int rawStart = ContentStarts[ContentStarts.Count - 1];
        if (rawEnd >= rawStart)
        {
            node.RawText = Text.Substring(rawStart, rawEnd - rawStart);
        }

        Stack.RemoveAt(Stack.Count - 1);
        ContentStarts.RemoveAt(ContentStarts.Count - 1);
    }

    internal void ChangeDelimiters(string tag)
    {
        string value = tag.Substring(1, tag.Length - 2).Trim();
        int separator = 0;
        while (separator < value.Length && !char.IsWhiteSpace(value[separator]))
        {
            separator = separator + 1;
        }

        if (separator == 0 || separator >= value.Length)
        {
            throw new InvalidOperationException("Invalid Mustache delimiter change tag.");
        }

        string nextStart = value.Substring(0, separator);
        string nextEnd = value.Substring(separator).Trim();
        if (nextEnd == "" || ContainsWhitespace(nextEnd))
        {
            throw new InvalidOperationException("Invalid Mustache delimiter change tag.");
        }

        StartDelimiter = nextStart;
        EndDelimiter = nextEnd;
    }

    internal void AddTextNode(string value)
    {
        if (value == "")
        {
            return;
        }

        MustacheNode node = new MustacheNode("text");
        node.Text = value;
        CurrentNode().Children.Add(node);
    }

    internal MustacheNode CurrentNode()
    {
        return Stack[Stack.Count - 1];
    }

    internal string GetTagKind(string tag, bool triple)
    {
        if (triple)
        {
            return "unescaped";
        }

        if (tag == "")
        {
            return "variable";
        }

        char marker = tag[0];
        if (marker == '!')
        {
            return "comment";
        }

        if (marker == '#')
        {
            return "section";
        }

        if (marker == '^')
        {
            return "inverted";
        }

        if (marker == '/')
        {
            return "close";
        }

        if (marker == '>')
        {
            return "partial";
        }

        if (marker == '<')
        {
            return "parent";
        }

        if (marker == '$')
        {
            return "block";
        }

        if (marker == '&')
        {
            return "unescaped";
        }

        if (marker == '=' && tag[tag.Length - 1] == '=')
        {
            return "delimiter";
        }

        return "variable";
    }

    internal bool IsStandaloneTag(string kind, int lineStart, int tagStart, int tagEnd, int lineEnd)
    {
        bool eligible = kind == "comment" || kind == "section" || kind == "inverted" || kind == "close" || kind == "partial" || kind == "delimiter" || kind == "parent" || kind == "block";
        if (!eligible)
        {
            return false;
        }

        if (IsWhitespaceOnly(lineStart, tagStart) && IsWhitespaceOnly(tagEnd, lineEnd))
        {
            return true;
        }

        if (kind == "parent" || kind == "block" || kind == "close")
        {
            return LineContainsOnlyInheritanceTags(lineStart, lineEnd);
        }

        return false;
    }

    internal bool LineContainsOnlyInheritanceTags(int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (Text[index] == ' ' || Text[index] == '\t')
            {
                index = index + 1;
                continue;
            }

            if (index + 2 > end || Text.Substring(index, 2) != "{{")
            {
                return false;
            }

            int close = Text.IndexOf("}}", index + 2, StringComparison.Ordinal);
            if (close < 0 || close + 2 > end)
            {
                return false;
            }

            string tag = Text.Substring(index + 2, close - index - 2).Trim();
            if (tag == "" || (tag[0] != '<' && tag[0] != '$' && tag[0] != '/'))
            {
                return false;
            }

            index = close + 2;
        }

        return true;
    }

    internal int FindLineStart(int position)
    {
        int index = position - 1;
        while (index >= 0 && Text[index] != '\n' && Text[index] != '\r')
        {
            index = index - 1;
        }

        return index + 1;
    }

    internal int FindLineEnd(int position)
    {
        int index = position;
        while (index < Text.Length && Text[index] != '\n' && Text[index] != '\r')
        {
            index = index + 1;
        }

        return index;
    }

    internal int SkipLineEnding(int position)
    {
        if (position >= Text.Length)
        {
            return position;
        }

        if (Text[position] == '\r' && position + 1 < Text.Length && Text[position + 1] == '\n')
        {
            return position + 2;
        }

        return position + 1;
    }

    internal bool IsWhitespaceOnly(int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (Text[index] != ' ' && Text[index] != '\t')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool StartsWithValue(int index, string value)
    {
        if (value.Length > Text.Length - index)
        {
            return false;
        }

        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            if (Text[index + valueIndex] != value[valueIndex])
            {
                return false;
            }

            valueIndex = valueIndex + 1;
        }

        return true;
    }

    internal bool ContainsWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsWhiteSpace(value[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

internal class MustacheRuntime
{
    internal TemplateRenderer Renderer;
    internal TemplateRenderContext Context;
    internal List<TemplateValue> Scopes;
    internal int CompositionDepth;

    internal MustacheRuntime(TemplateRenderer renderer, TemplateRenderContext context, TemplateValue model)
    {
        Renderer = renderer;
        Context = context;
        Scopes = new List<TemplateValue>();
        Scopes.Add(model);
        CompositionDepth = 0;
    }

    internal string RenderTemplate(string text)
    {
        MustacheNode root = ParseWithDelimiters(text, "{{", "}}");
        Dictionary<string, MustacheNode> overrides = new Dictionary<string, MustacheNode>();
        return RenderNodeList(root.Children, overrides);
    }

    internal MustacheNode ParseWithDelimiters(string text, string startDelimiter, string endDelimiter)
    {
        MustacheParser parser = new MustacheParser(text, startDelimiter, endDelimiter);
        return parser.ParseDocument();
    }

    internal string RenderNodeList(List<MustacheNode> nodes, Dictionary<string, MustacheNode> overrides)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < nodes.Count)
        {
            builder.Append(RenderSingleNode(nodes[index], overrides));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal string RenderSingleNode(MustacheNode node, Dictionary<string, MustacheNode> overrides)
    {
        if (node.Kind == "text")
        {
            return node.Text;
        }

        if (node.Kind == "variable" || node.Kind == "unescaped")
        {
            return RenderInterpolation(node);
        }

        if (node.Kind == "section")
        {
            return RenderSection(node, overrides);
        }

        if (node.Kind == "inverted")
        {
            TemplateValue inverted = ResolveName(node.Name);
            return TemplateTruthiness.IsTruthyMustache(inverted) ? "" : RenderNodeList(node.Children, overrides);
        }

        if (node.Kind == "partial")
        {
            return RenderPartial(node, overrides);
        }

        if (node.Kind == "parent")
        {
            return RenderParent(node, overrides);
        }

        if (node.Kind == "block")
        {
            return RenderBlock(node, overrides);
        }

        return "";
    }

    internal string RenderInterpolation(MustacheNode node)
    {
        TemplateValue value = ResolveName(node.Name);
        if (value.Kind == TemplateValueKind.Function)
        {
            Context.EnterRecursion();
            value = value.Invoke(new List<TemplateValue>());
            string lambdaText = TemplateOutputEncoder.ConvertValueToText(value);
            MustacheNode lambdaRoot = ParseWithDelimiters(lambdaText, "{{", "}}");
            string renderedLambda = RenderNodeList(lambdaRoot.Children, new Dictionary<string, MustacheNode>());
            Context.ExitRecursion();
            value = TemplateValue.FromString(renderedLambda);
        }

        return Renderer.EncodeOutputValue(value, Context, node.Kind == "unescaped");
    }

    internal string RenderSection(MustacheNode node, Dictionary<string, MustacheNode> overrides)
    {
        TemplateValue value = ResolveName(node.Name);
        if (value.Kind == TemplateValueKind.Function)
        {
            Context.EnterRecursion();
            List<TemplateValue> arguments = new List<TemplateValue>();
            arguments.Add(TemplateValue.FromString(node.RawText));
            TemplateValue result = value.Invoke(arguments);
            MustacheNode lambdaRoot = ParseWithDelimiters(TemplateOutputEncoder.ConvertValueToText(result), node.StartDelimiter, node.EndDelimiter);
            string lambdaOutput = RenderNodeList(lambdaRoot.Children, overrides);
            Context.ExitRecursion();
            return lambdaOutput;
        }

        if (!TemplateTruthiness.IsTruthyMustache(value))
        {
            return "";
        }

        if (value.Kind == TemplateValueKind.List)
        {
            StringBuilder listOutput = new StringBuilder();
            int index = 0;
            while (index < value.ListValue.Count)
            {
                Context.CountLoopIteration();
                Scopes.Add(value.ListValue[index]);
                listOutput.Append(RenderNodeList(node.Children, overrides));
                Scopes.RemoveAt(Scopes.Count - 1);
                index = index + 1;
            }

            return listOutput.ToString();
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return RenderNodeList(node.Children, overrides);
        }

        Scopes.Add(value);
        string output = RenderNodeList(node.Children, overrides);
        Scopes.RemoveAt(Scopes.Count - 1);
        return output;
    }

    internal string RenderPartial(MustacheNode node, Dictionary<string, MustacheNode> overrides)
    {
        string name = ResolveComponentName(node.Name);
        if (name == "")
        {
            return "";
        }

        TemplateSource? source = Context.Registry.LoadPartial(name);
        if (source == null)
        {
            return "";
        }

        Context.EnterRecursion();
        CompositionDepth = CompositionDepth + 1;
        Context.EnsureIncludeDepthAllowed(CompositionDepth);
        Context.CountTemplate(source);
        Context.RecordDependency(TemplateDependencyKind.Partial, source);
        string partialText = source.Text;
        if (node.Standalone && node.Indentation != "")
        {
            partialText = ApplyIndentation(partialText, node.Indentation);
        }

        MustacheNode root = ParseWithDelimiters(partialText, "{{", "}}");
        string output = RenderNodeList(root.Children, overrides);
        CompositionDepth = CompositionDepth - 1;
        Context.ExitRecursion();
        return output;
    }

    internal string RenderParent(MustacheNode node, Dictionary<string, MustacheNode> inheritedOverrides)
    {
        string name = ResolveComponentName(node.Name);
        if (name == "")
        {
            return "";
        }

        TemplateSource? source = Context.Registry.LoadTemplate(name);
        if (source == null)
        {
            source = Context.Registry.LoadPartial(name);
        }

        if (source == null)
        {
            return "";
        }

        Dictionary<string, MustacheNode> overrides = CollectOverrides(node.Children, inheritedOverrides);
        Context.EnterRecursion();
        CompositionDepth = CompositionDepth + 1;
        Context.EnsureIncludeDepthAllowed(CompositionDepth);
        Context.CountTemplate(source);
        Context.RecordDependency(TemplateDependencyKind.Template, source);
        string parentText = source.Text;
        if (node.Standalone && node.Indentation != "")
        {
            parentText = ApplyIndentation(parentText, node.Indentation);
        }

        MustacheNode root = ParseWithDelimiters(parentText, "{{", "}}");
        string output = RenderNodeList(root.Children, overrides);
        CompositionDepth = CompositionDepth - 1;
        Context.ExitRecursion();
        return output;
    }

    internal string RenderBlock(MustacheNode node, Dictionary<string, MustacheNode> overrides)
    {
        MustacheNode selected = node;
        if (overrides.ContainsKey(node.Name) && overrides[node.Name] != node)
        {
            selected = overrides[node.Name];
        }

        string output = RenderNodeList(selected.Children, overrides);
        if (selected != node)
        {
            output = NormalizeOverride(output, selected);
        }

        string indentation = node.Indentation;
        if (selected != node)
        {
            string intrinsicIndentation = FindIntrinsicIndentation(node.RawText);
            if (intrinsicIndentation.Length > indentation.Length)
            {
                indentation = intrinsicIndentation;
            }
        }

        if (node.Standalone && indentation != "")
        {
            output = ApplyIndentation(output, indentation);
        }

        if (selected != node && node.Standalone && node.RawText == "" && output != "" && output[output.Length - 1] != '\n' && output[output.Length - 1] != '\r')
        {
            output = output + "\n";
        }

        return output;
    }

    internal Dictionary<string, MustacheNode> CollectOverrides(List<MustacheNode> nodes, Dictionary<string, MustacheNode> inheritedOverrides)
    {
        Dictionary<string, MustacheNode> result = new Dictionary<string, MustacheNode>();
        int inheritedIndex = 0;
        List<string> inheritedNames = new List<string>(inheritedOverrides.Keys);
        while (inheritedIndex < inheritedNames.Count)
        {
            string inheritedName = inheritedNames[inheritedIndex];
            result[inheritedName] = inheritedOverrides[inheritedName];
            inheritedIndex = inheritedIndex + 1;
        }

        int index = 0;
        while (index < nodes.Count)
        {
            MustacheNode current = nodes[index];
            if (current.Kind == "block" && !result.ContainsKey(current.Name))
            {
                result[current.Name] = current;
            }

            index = index + 1;
        }

        return result;
    }

    internal TemplateValue ResolveName(string name)
    {
        string trimmed = name.Trim();
        if (trimmed == ".")
        {
            return Scopes[Scopes.Count - 1];
        }

        if (trimmed == "")
        {
            return TemplateValue.Missing();
        }

        string[] parts = trimmed.Split('.');
        TemplateValue value = TemplateValue.Missing();
        int scopeIndex = Scopes.Count - 1;
        while (scopeIndex >= 0)
        {
            value = Context.MemberResolver.ResolveMember(Scopes[scopeIndex], parts[0]);
            if (value.Kind != TemplateValueKind.Missing)
            {
                break;
            }

            scopeIndex = scopeIndex - 1;
        }

        int partIndex = 1;
        while (partIndex < parts.Length && value.Kind != TemplateValueKind.Missing)
        {
            value = Context.MemberResolver.ResolveMember(value, parts[partIndex]);
            partIndex = partIndex + 1;
        }

        return value;
    }

    internal string ResolveComponentName(string expression)
    {
        string name = expression.Trim();
        if (name.Length > 0 && name[0] == '*')
        {
            string dynamicName = name.Substring(1).Trim();
            if (dynamicName == "" || dynamicName[0] == '*')
            {
                return "";
            }

            TemplateValue value = ResolveName(dynamicName);
            return TemplateOutputEncoder.ConvertValueToText(value);
        }

        return name;
    }

    internal string ApplyIndentation(string text, string indentation)
    {
        if (text == "" || indentation == "")
        {
            return text;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(indentation);
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            builder.Append(current);
            if (current == '\n' && index + 1 < text.Length)
            {
                builder.Append(indentation);
            }
            else if (current == '\r' && index + 1 < text.Length && text[index + 1] != '\n')
            {
                builder.Append(indentation);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal string NormalizeOverride(string text, MustacheNode node)
    {
        string result = text;
        if (!node.Standalone && result.Length > 0 && result[0] == '\n')
        {
            result = result.Substring(1);
        }
        else if (!node.Standalone && result.Length > 1 && result[0] == '\r' && result[1] == '\n')
        {
            result = result.Substring(2);
        }

        return RemoveCommonIndentation(result);
    }

    internal string FindIntrinsicIndentation(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            int lineEnd = index;
            while (lineEnd < text.Length && text[lineEnd] != '\r' && text[lineEnd] != '\n')
            {
                lineEnd = lineEnd + 1;
            }

            int content = index;
            while (content < lineEnd && (text[content] == ' ' || text[content] == '\t'))
            {
                content = content + 1;
            }

            if (content < lineEnd)
            {
                return text.Substring(index, content - index);
            }

            index = lineEnd;
            if (index < text.Length && text[index] == '\r')
            {
                index = index + 1;
            }

            if (index < text.Length && text[index] == '\n')
            {
                index = index + 1;
            }
        }

        return "";
    }

    internal string RemoveCommonIndentation(string text)
    {
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        int minimum = 2147483647;
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex];
            int count = 0;
            while (count < line.Length && (line[count] == ' ' || line[count] == '\t'))
            {
                count = count + 1;
            }

            if (count < line.Length && count < minimum)
            {
                minimum = count;
            }

            lineIndex = lineIndex + 1;
        }

        if (minimum == 2147483647 || minimum == 0)
        {
            return text;
        }

        StringBuilder builder = new StringBuilder();
        lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex];
            builder.Append(line.Length >= minimum ? line.Substring(minimum) : "");
            if (lineIndex + 1 < lines.Length)
            {
                builder.Append("\n");
            }

            lineIndex = lineIndex + 1;
        }

        return builder.ToString();
    }
}
