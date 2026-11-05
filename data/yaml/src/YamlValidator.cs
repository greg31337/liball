using System.Collections.Generic;

namespace yaml;

internal class YamlValidationState
{
    internal YamlResult Result;
    internal YamlComposerOptions Composer;
    internal YamlSchemaOptions Schema;
    internal YamlSecurityOptions Security;
    internal int AliasCount;
    internal List<string> AnchorNames;
    internal List<YamlNode> AnchorNodes;
    internal Dictionary<YamlNode, YamlNode> Collected;
    internal Dictionary<YamlNode, YamlNode> Visited;
    internal Dictionary<YamlNode, YamlNode> Path;

    internal YamlValidationState(YamlOptions? options)
    {
        YamlOptions effective = options == null ? new YamlOptions() : options;
        Result = new YamlResult();
        Composer = effective.Composer == null ? new YamlComposerOptions() : effective.Composer;
        Schema = effective.Schema == null ? new YamlSchemaOptions() : effective.Schema;
        Security = effective.Security == null ? new YamlSecurityOptions() : effective.Security;
        AliasCount = 0;
        AnchorNames = new List<string>();
        AnchorNodes = new List<YamlNode>();
        Collected = new Dictionary<YamlNode, YamlNode>();
        Visited = new Dictionary<YamlNode, YamlNode>();
        Path = new Dictionary<YamlNode, YamlNode>();
    }

    internal void ResetGraph()
    {
        AnchorNames.Clear();
        AnchorNodes.Clear();
        Collected.Clear();
        Visited.Clear();
        Path.Clear();
    }

    internal bool CanAddDiagnostic()
    {
        if (!Security.EnforceLimits || Security.MaxDiagnostics <= 0)
        {
            return true;
        }

        return Security.AllowsDiagnosticCount(Result.Diagnostics.Count + 1);
    }

    internal void AddDiagnostic(YamlDiagnosticSeverity severity, string code, string message, YamlSourceSpan? span)
    {
        if (!CanAddDiagnostic())
        {
            if (severity == YamlDiagnosticSeverity.Error)
            {
                Result.Success = false;
            }

            return;
        }

        Result.AddDiagnostic(YamlDiagnostic.Create(severity, code, message, span));
    }

    internal YamlNode? FindAnchor(string? name)
    {
        string value = name == null ? "" : name;
        int index = 0;
        while (index < AnchorNames.Count)
        {
            if (AnchorNames[index] == value)
            {
                return AnchorNodes[index];
            }

            index = index + 1;
        }

        return null;
    }
}

internal static class YamlValidator
{
    internal static YamlResult ValidateStream(YamlStream? stream, YamlOptions? options)
    {
        YamlValidationState state = new YamlValidationState(options);
        if (stream == null)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML stream to validate is null.", YamlSourceSpan.Empty());
            return state.Result;
        }

        if (stream.Documents == null)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML stream has no document collection.", stream.Span);
            return state.Result;
        }

        int index = 0;
        while (index < stream.Documents.Count)
        {
            YamlDocument? document = stream.Documents[index];
            if (document == null)
            {
                state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML stream contains a null document.", stream.Span);
            }
            else
            {
                ValidateDocumentGraph(document, state);
            }

            index = index + 1;
        }

        return state.Result;
    }

    internal static YamlResult ValidateDocument(YamlDocument? document, YamlOptions? options)
    {
        YamlValidationState state = new YamlValidationState(options);
        if (document == null)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML document to validate is null.", YamlSourceSpan.Empty());
            return state.Result;
        }

        ValidateDocumentGraph(document, state);
        return state.Result;
    }

    internal static YamlResult ValidateNode(YamlNode? node, YamlOptions? options)
    {
        YamlValidationState state = new YamlValidationState(options);
        if (node == null)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML node graph to validate is null.", YamlSourceSpan.Empty());
            return state.Result;
        }

        ValidateRoot(node, state);
        return state.Result;
    }

    internal static void ValidateDocumentGraph(YamlDocument document, YamlValidationState state)
    {
        state.ResetGraph();
        if (document.Root != null)
        {
            ValidateRoot(document.Root, state);
        }
    }

    internal static void ValidateRoot(YamlNode root, YamlValidationState state)
    {
        CollectAnchors(root, state);
        state.Path.Clear();
        ValidateGraphNode(root, state, 1);
    }

    internal static void CollectAnchors(YamlNode? node, YamlValidationState state)
    {
        if (node == null || state.Collected.ContainsKey(node))
        {
            return;
        }

        state.Collected[node] = node;
        string anchor = node.Anchor == null ? "" : node.Anchor;
        if (anchor != "")
        {
            YamlNode? previous = state.FindAnchor(anchor);
            if (previous == null)
            {
                state.AnchorNames.Add(anchor);
                state.AnchorNodes.Add(node);
            }
            else if (previous != node)
            {
                int anchorIndex = state.AnchorNames.IndexOf(anchor);
                if (anchorIndex >= 0)
                {
                    state.AnchorNodes[anchorIndex] = node;
                }
            }
        }

        if (node.Children != null)
        {
            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                CollectAnchors(node.Children[childIndex], state);
                childIndex = childIndex + 1;
            }
        }

        if (node.Pairs != null)
        {
            int pairIndex = 0;
            while (pairIndex < node.Pairs.Count)
            {
                YamlPair? pair = node.Pairs[pairIndex];
                if (pair != null)
                {
                    CollectAnchors(pair.Key, state);
                    CollectAnchors(pair.Value, state);
                }

                pairIndex = pairIndex + 1;
            }
        }
    }

    internal static void ValidateGraphNode(YamlNode? node, YamlValidationState state, int depth)
    {
        if (node == null)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML graph contains a null node.", YamlSourceSpan.Empty());
            return;
        }

        if (state.Path.ContainsKey(node))
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML graph contains a direct collection cycle.", NodeSpan(node));
            return;
        }

        if (state.Visited.ContainsKey(node))
        {
            return;
        }

        state.Visited[node] = node;
        state.Path[node] = node;
        ValidateNodeShape(node, state);
        ValidateNodeTag(node, state);
        ValidateNodeLimits(node, state, depth);
        ValidateAlias(node, state);
        ValidateDuplicateKeys(node, state);

        if (node.Children != null)
        {
            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                ValidateGraphNode(node.Children[childIndex], state, depth + 1);
                childIndex = childIndex + 1;
            }
        }

        if (node.Pairs != null)
        {
            int pairIndex = 0;
            while (pairIndex < node.Pairs.Count)
            {
                YamlPair? pair = node.Pairs[pairIndex];
                if (pair == null)
                {
                    state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML mapping contains a null pair.", NodeSpan(node));
                }
                else
                {
                    ValidateGraphNode(pair.Key, state, depth + 1);
                    ValidateGraphNode(pair.Value, state, depth + 1);
                }

                pairIndex = pairIndex + 1;
            }
        }

        state.Path.Remove(node);
    }

    internal static void ValidateNodeShape(YamlNode node, YamlValidationState state)
    {
        if (node.Children == null || node.Pairs == null)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML node has a null collection field.", NodeSpan(node));
            return;
        }

        bool invalid = false;
        if (node.Kind == YamlNodeKind.Sequence)
        {
            invalid = node.Pairs.Count != 0;
        }
        else if (node.Kind == YamlNodeKind.Mapping)
        {
            invalid = node.Children.Count != 0;
        }
        else if (node.Kind == YamlNodeKind.Scalar || node.Kind == YamlNodeKind.Alias || node.Kind == YamlNodeKind.Empty)
        {
            invalid = node.Children.Count != 0 || node.Pairs.Count != 0;
        }
        else
        {
            invalid = true;
        }

        if (invalid)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV001", "The YAML node fields do not match its node kind.", NodeSpan(node));
        }
    }

    internal static void ValidateNodeTag(YamlNode node, YamlValidationState state)
    {
        string rawTag = node.Tag == null ? "" : node.Tag;
        if (rawTag == "")
        {
            return;
        }

        if (!IsKnownValidationTag(rawTag, state))
        {
            if (state.Schema.UnknownTagPolicy == YamlUnknownTagPolicy.Warn)
            {
                state.AddDiagnostic(YamlDiagnosticSeverity.Warning, "YAMLS006", "Unknown YAML tag '" + rawTag + "' was found.", NodeSpan(node));
            }
            else if (state.Schema.UnknownTagPolicy == YamlUnknownTagPolicy.Error)
            {
                state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLS006", "Unknown YAML tag '" + rawTag + "' was found.", NodeSpan(node));
            }

            return;
        }

        string tag = YamlScalarResolver.NormalizeStandardTag(rawTag);
        if (!YamlScalarResolver.IsStandardTag(tag))
        {
            ValidateCustomTagKind(node, rawTag, state);
            return;
        }

        bool compatible = StandardTagMatchesNode(tag, node.Kind);
        if (!compatible)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV003", "YAML tag '" + rawTag + "' is not valid for this node kind or schema configuration.", NodeSpan(node));
        }
    }

    internal static void ValidateCustomTagKind(YamlNode node, string tag, YamlValidationState state)
    {
        YamlTagRegistration? registration = FindCustomTag(tag, state);
        if (registration != null && registration.NodeKind != node.Kind)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV003", "YAML tag '" + tag + "' is registered for a different node kind.", NodeSpan(node));
        }
    }

    internal static bool IsKnownValidationTag(string tag, YamlValidationState state)
    {
        if (YamlScalarResolver.IsStandardTag(tag))
        {
            return true;
        }

        return FindCustomTag(tag, state) != null;
    }

    internal static YamlTagRegistration? FindCustomTag(string tag, YamlValidationState state)
    {
        if (state.Schema.CustomTags == null || state.Schema.CustomTags.Registrations == null)
        {
            return null;
        }

        int index = 0;
        while (index < state.Schema.CustomTags.Registrations.Count)
        {
            YamlTagRegistration? registration = state.Schema.CustomTags.Registrations[index];
            if (registration != null && registration.Tag != null && registration.Tag != "" && registration.Tag == tag)
            {
                return registration;
            }

            index = index + 1;
        }

        return null;
    }

    internal static bool StandardTagMatchesNode(string tag, YamlNodeKind kind)
    {
        if (kind == YamlNodeKind.Sequence)
        {
            return tag == YamlScalarResolver.SequenceTag
                || tag == YamlScalarResolver.OrderedMapTag
                || tag == YamlScalarResolver.PairsTag;
        }

        if (kind == YamlNodeKind.Mapping)
        {
            return tag == YamlScalarResolver.MappingTag
                || tag == YamlScalarResolver.SetTag;
        }

        if (kind == YamlNodeKind.Empty)
        {
            return tag == YamlScalarResolver.FailsafeStringTag
                || tag == YamlScalarResolver.NullTag
                || tag == YamlScalarResolver.BoolTag
                || tag == YamlScalarResolver.IntTag
                || tag == YamlScalarResolver.FloatTag
                || tag == YamlScalarResolver.TimestampTag
                || tag == YamlScalarResolver.BinaryTag
                || tag == YamlScalarResolver.MergeTag;
        }

        if (kind == YamlNodeKind.Scalar)
        {
            return tag == YamlScalarResolver.FailsafeStringTag
                || tag == YamlScalarResolver.NullTag
                || tag == YamlScalarResolver.BoolTag
                || tag == YamlScalarResolver.IntTag
                || tag == YamlScalarResolver.FloatTag
                || tag == YamlScalarResolver.TimestampTag
                || tag == YamlScalarResolver.BinaryTag
                || tag == YamlScalarResolver.MergeTag;
        }

        return false;
    }

    internal static void ValidateNodeLimits(YamlNode node, YamlValidationState state, int depth)
    {
        if (!state.Security.EnforceLimits)
        {
            return;
        }

        if (!state.Security.AllowsDepth(depth))
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLS003", "YAML node depth exceeds the configured security limit.", NodeSpan(node));
        }

        string value = node.Value == null ? "" : node.Value;
        if ((node.Kind == YamlNodeKind.Scalar || node.Kind == YamlNodeKind.Empty) && !state.Security.AllowsScalarLength(value.Length))
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLS002", "YAML scalar length exceeds the configured security limit.", NodeSpan(node));
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            state.AliasCount = state.AliasCount + 1;
            if (!state.Security.AllowsAliasCount(state.AliasCount))
            {
                state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLS004", "YAML alias count exceeds the configured security limit.", NodeSpan(node));
            }
        }

        if (node.Kind == YamlNodeKind.Sequence && node.Children != null && !state.Security.AllowsCollectionSize(node.Children.Count))
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLS005", "YAML collection size exceeds the configured security limit.", NodeSpan(node));
        }

        if (node.Kind == YamlNodeKind.Mapping && node.Pairs != null && !state.Security.AllowsCollectionSize(node.Pairs.Count))
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLS005", "YAML collection size exceeds the configured security limit.", NodeSpan(node));
        }
    }

    internal static void ValidateAlias(YamlNode node, YamlValidationState state)
    {
        if (node.Kind != YamlNodeKind.Alias)
        {
            return;
        }

        string name = node.AliasName == null ? "" : node.AliasName;
        YamlNode? target = state.FindAnchor(name);
        if (name == "" || target == null)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLC007", "The YAML alias does not name a declared anchor.", NodeSpan(node));
            return;
        }

        if (state.Composer.ResolveAliases && node.AliasTarget == null)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV002", "The YAML alias has not been linked to its anchor target.", NodeSpan(node));
        }
        else if (node.AliasTarget != null && node.AliasTarget != target)
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLV002", "The YAML alias target does not match its declared anchor.", NodeSpan(node));
        }

        if (!state.Composer.AllowRecursiveAliases && state.Path.ContainsKey(target))
        {
            state.AddDiagnostic(YamlDiagnosticSeverity.Error, "YAMLC008", "A recursive YAML alias is not allowed by the composer settings.", NodeSpan(node));
        }
    }

    internal static void ValidateDuplicateKeys(YamlNode node, YamlValidationState state)
    {
        if (node.Kind != YamlNodeKind.Mapping || node.Pairs == null)
        {
            return;
        }

        if (state.Composer.DuplicateKeyPolicy != YamlDuplicateKeyPolicy.Error
            && state.Composer.DuplicateKeyPolicy != YamlDuplicateKeyPolicy.Warn)
        {
            return;
        }

        int index = 0;
        while (index < node.Pairs.Count)
        {
            YamlPair? pair = node.Pairs[index];
            if (pair != null)
            {
                int previousIndex = 0;
                while (previousIndex < index)
                {
                    YamlPair? previous = node.Pairs[previousIndex];
                    if (previous != null && YamlComposer.KeysEqual(previous.Key, pair.Key))
                    {
                        YamlDiagnosticSeverity severity = state.Composer.DuplicateKeyPolicy == YamlDuplicateKeyPolicy.Error
                            ? YamlDiagnosticSeverity.Error
                            : YamlDiagnosticSeverity.Warning;
                        YamlSourceSpan span = pair.Key == null ? pair.Span : pair.Key.Span;
                        state.AddDiagnostic(severity, "YAMLC009", "Duplicate mapping key was found.", span);
                        previousIndex = index;
                    }

                    previousIndex = previousIndex + 1;
                }
            }

            index = index + 1;
        }
    }

    internal static YamlSourceSpan NodeSpan(YamlNode? node)
    {
        if (node == null || node.Span == null)
        {
            return YamlSourceSpan.Empty();
        }

        return node.Span;
    }
}
