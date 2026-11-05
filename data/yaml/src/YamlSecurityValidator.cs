using System.Collections.Generic;

namespace yaml;

internal class YamlSecurityValidationState
{
    internal int AliasCount;

    internal YamlSecurityValidationState()
    {
        AliasCount = 0;
    }
}

internal static class YamlSecurityValidator
{
    internal static YamlComposerResult ValidateInput(string text, YamlSecurityOptions options)
    {
        YamlComposerResult result = new YamlComposerResult();
        if (options == null || !options.EnforceLimits)
        {
            return result;
        }

        string value = text == null ? "" : text;
        if (!options.AllowsInputLength(value.Length))
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLS001", "YAML input length exceeds the configured security limit.", InputSpan(value)));
        }

        return result;
    }

    internal static void ValidateStream(YamlStream stream, YamlSecurityOptions options, YamlComposerResult result)
    {
        if (stream == null || options == null || result == null || !options.EnforceLimits)
        {
            return;
        }

        YamlSecurityValidationState state = new YamlSecurityValidationState();
        int index = 0;
        while (index < stream.Documents.Count)
        {
            ValidateNode(stream.Documents[index].Root, options, result, state, 1, new Dictionary<YamlNode, YamlNode>());
            index = index + 1;
        }
    }

    internal static void ValidateNode(YamlNode? node, YamlSecurityOptions options, YamlComposerResult result, YamlSecurityValidationState state, int depth, Dictionary<YamlNode, YamlNode> path)
    {
        if (node == null)
        {
            return;
        }

        if (path.ContainsKey(node))
        {
            return;
        }

        path[node] = node;
        ValidateNodeLimits(node, options, result, state, depth);

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            ValidateNode(node.Children[childIndex], options, result, state, depth + 1, path);
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[pairIndex];
            ValidateNode(pair.Key, options, result, state, depth + 1, path);
            ValidateNode(pair.Value, options, result, state, depth + 1, path);
            pairIndex = pairIndex + 1;
        }

        path.Remove(node);
    }

    internal static void ValidateNodeLimits(YamlNode node, YamlSecurityOptions options, YamlComposerResult result, YamlSecurityValidationState state, int depth)
    {
        if (!options.AllowsDepth(depth))
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLS003", "YAML node depth exceeds the configured security limit.", NodeSpan(node)));
        }

        if ((node.Kind == YamlNodeKind.Scalar || node.Kind == YamlNodeKind.Empty) && !options.AllowsScalarLength(node.Value.Length))
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLS002", "YAML scalar length exceeds the configured security limit.", NodeSpan(node)));
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            state.AliasCount = state.AliasCount + 1;
            if (!options.AllowsAliasCount(state.AliasCount))
            {
                result.AddDiagnostic(YamlDiagnostic.Error("YAMLS004", "YAML alias count exceeds the configured security limit.", NodeSpan(node)));
            }
        }

        if (node.Kind == YamlNodeKind.Sequence && !options.AllowsCollectionSize(node.Children.Count))
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLS005", "YAML collection size exceeds the configured security limit.", NodeSpan(node)));
        }

        if (node.Kind == YamlNodeKind.Mapping && !options.AllowsCollectionSize(node.Pairs.Count))
        {
            result.AddDiagnostic(YamlDiagnostic.Error("YAMLS005", "YAML collection size exceeds the configured security limit.", NodeSpan(node)));
        }
    }

    internal static YamlSourceSpan NodeSpan(YamlNode node)
    {
        if (node == null || node.Span == null)
        {
            return YamlSourceSpan.Empty();
        }

        return node.Span;
    }

    internal static YamlSourceSpan InputSpan(string text)
    {
        string value = text == null ? "" : text;
        int endColumn = value.Length == int.MaxValue ? int.MaxValue : value.Length + 1;
        return YamlSourceSpan.FromBounds(0, 1, 1, value.Length, 1, endColumn);
    }
}
