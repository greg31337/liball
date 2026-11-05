using System.Collections.Generic;
using System.Text;

namespace yaml;

/// <summary>
/// Converts YAML streams, documents, and nodes into deterministic YAML text.
/// </summary>
internal static class YamlEmitter
{
    /// <summary>
    /// Emits all documents in a YAML stream, inserting document markers when required for an unambiguous stream.
    /// </summary>
    /// <param name="stream">The YAML stream to process.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string Emit(YamlStream? stream)
    {
        return Emit(stream, null);
    }

    /// <summary>
    /// Emits all documents in a YAML stream, inserting document markers when required for an unambiguous stream.
    /// </summary>
    /// <param name="stream">The YAML stream to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string Emit(YamlStream? stream, YamlEmitterOptions? options)
    {
        if (stream == null)
        {
            return "";
        }

        YamlEmitterOptions effective = EffectiveOptions(options);
        EnsureStreamCanEmit(stream);
        StringBuilder builder = new StringBuilder();
        bool multipleDocuments = stream.Documents.Count > 1;
        int index = 0;
        while (index < stream.Documents.Count)
        {
            if (index > 0 && !effective.WriteDocumentEnd && stream.Documents[index].Directives.Count > 0)
            {
                AppendLine(builder, "...");
            }

            EmitDocumentToBuilder(builder, stream.Documents[index], effective, multipleDocuments);
            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Emits one YAML document, including configured directives, markers, comments, and styles.
    /// </summary>
    /// <param name="document">The YAML document to process.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string EmitDocument(YamlDocument? document)
    {
        return EmitDocument(document, null);
    }

    /// <summary>
    /// Emits one YAML document, including configured directives, markers, comments, and styles.
    /// </summary>
    /// <param name="document">The YAML document to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string EmitDocument(YamlDocument? document, YamlEmitterOptions? options)
    {
        if (document == null)
        {
            return "";
        }

        YamlEmitterOptions effective = EffectiveOptions(options);
        EnsureDocumentCanEmit(document);
        StringBuilder builder = new StringBuilder();
        EmitDocumentToBuilder(builder, document, effective, false);
        return builder.ToString();
    }

    /// <summary>
    /// Emits one node as standalone YAML without a document wrapper.
    /// </summary>
    /// <param name="node">The YAML node to process.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string EmitNode(YamlNode? node)
    {
        return EmitNode(node, null);
    }

    /// <summary>
    /// Emits one node as standalone YAML without a document wrapper.
    /// </summary>
    /// <param name="node">The YAML node to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string EmitNode(YamlNode? node, YamlEmitterOptions? options)
    {
        YamlEmitterOptions effective = EffectiveOptions(options);
        EnsureNodeCanEmit(node);
        return EmitRootNode(node, effective);
    }

    internal static YamlEmitterOptions EffectiveOptions(YamlEmitterOptions? options)
    {
        YamlEmitterOptions effective = options == null ? new YamlEmitterOptions() : options.Copy();
        effective.Normalize();
        return effective;
    }

    internal static void EnsureStreamCanEmit(YamlStream stream)
    {
        if (stream.Documents == null)
        {
            throw YamlException.Create("YAML emission cannot process a null document collection.");
        }

        int index = 0;
        while (index < stream.Documents.Count)
        {
            YamlDocument? document = stream.Documents[index];
            if (document == null)
            {
                throw YamlException.Create("YAML emission cannot process a null document.");
            }

            EnsureDocumentCanEmit(document);
            index = index + 1;
        }
    }

    internal static void EnsureDocumentCanEmit(YamlDocument document)
    {
        if (document.Root != null)
        {
            EnsureNodeCanEmit(document.Root);
        }
    }

    internal static void EnsureNodeCanEmit(YamlNode? node)
    {
        InspectEmissionNode(node, new Dictionary<YamlNode, YamlNode>());
    }

    internal static void InspectEmissionNode(YamlNode? node, Dictionary<YamlNode, YamlNode> path)
    {
        if (node == null || node.Kind == YamlNodeKind.Empty || node.Kind == YamlNodeKind.Scalar)
        {
            return;
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            if (node.AliasName == null || node.AliasName == "")
            {
                throw YamlException.Create("YAML emission cannot process an alias without an anchor name.");
            }

            return;
        }

        if (node.Kind != YamlNodeKind.Sequence && node.Kind != YamlNodeKind.Mapping)
        {
            throw YamlException.Create("YAML emission cannot process an unknown node kind.");
        }

        if (path.ContainsKey(node))
        {
            throw YamlException.Create("YAML emission cannot process a direct collection cycle; use an alias node for recursive references.");
        }

        path[node] = node;
        if (node.Kind == YamlNodeKind.Sequence)
        {
            if (node.Children == null)
            {
                throw YamlException.Create("YAML emission cannot process a sequence with a null child collection.");
            }

            int childIndex = 0;
            while (childIndex < node.Children.Count)
            {
                InspectEmissionNode(node.Children[childIndex], path);
                childIndex = childIndex + 1;
            }
        }
        else
        {
            if (node.Pairs == null)
            {
                throw YamlException.Create("YAML emission cannot process a mapping with a null pair collection.");
            }

            int pairIndex = 0;
            while (pairIndex < node.Pairs.Count)
            {
                YamlPair? pair = node.Pairs[pairIndex];
                if (pair == null)
                {
                    throw YamlException.Create("YAML emission cannot process a null mapping pair.");
                }

                InspectEmissionNode(pair.Key, path);
                InspectEmissionNode(pair.Value, path);
                pairIndex = pairIndex + 1;
            }
        }

        path.Remove(node);
    }

    internal static void EmitDocumentToBuilder(StringBuilder builder, YamlDocument document, YamlEmitterOptions options, bool multipleDocuments)
    {
        if (builder == null || document == null || options == null)
        {
            return;
        }

        EmitDocumentLeadingTrivia(builder, document, options, 0);
        EmitDirectives(builder, document);
        bool hasDirectives = document.Directives.Count > 0;
        bool writeStart = options.WriteDocumentStart || hasDirectives || multipleDocuments;
        if (writeStart)
        {
            AppendLine(builder, "---");
        }

        if (document.Root != null)
        {
            EmitNodeLeadingTrivia(builder, document.Root, options, 0);
            if (document.Root.Kind != YamlNodeKind.Empty || !multipleDocuments)
            {
                string rootText = EmitRootNode(document.Root, options);
                if (CanAppendInlineTrailingComment(document.Root, options))
                {
                    rootText = rootText + InlineTrailingComment(document.Root, null, options);
                }

                AppendLine(builder, rootText);
            }

            EmitNodeFollowingTriviaAfterLine(builder, document.Root, options, 0);
        }
        else if (options.WriteDocumentEnd && !writeStart)
        {
            AppendLine(builder, "");
        }

        EmitTriviaFollowingAfterLine(builder, document.Trivia, options, 0);
        if (options.WriteDocumentEnd)
        {
            AppendLine(builder, "...");
        }
    }

    internal static void EmitDirectives(StringBuilder builder, YamlDocument document)
    {
        if (builder == null || document == null)
        {
            return;
        }

        int index = 0;
        while (index < document.Directives.Count)
        {
            AppendLine(builder, EmitDirective(document.Directives[index]));
            index = index + 1;
        }
    }

    internal static string EmitDirective(YamlDirective directive)
    {
        if (directive == null)
        {
            return "";
        }

        if (directive.IsYamlVersion())
        {
            string value = directive.Value == "" ? VersionToText(directive.Version) : directive.Value;
            return "%YAML " + value;
        }

        if (directive.IsTag())
        {
            return "%TAG " + directive.Handle + " " + directive.Prefix;
        }

        if (directive.Value == "")
        {
            return "%" + directive.Name;
        }

        return "%" + directive.Name + " " + directive.Value;
    }

    internal static string EmitRootNode(YamlNode? node, YamlEmitterOptions options)
    {
        if (node == null)
        {
            return "";
        }

        if (node.Kind == YamlNodeKind.Empty)
        {
            return "";
        }

        if (node.Kind == YamlNodeKind.Scalar)
        {
            if (UseBlockScalar(node, options))
            {
                return EmitBlockScalar(node, options, 0);
            }

            return EmitScalar(node, options);
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            return "*" + node.AliasName;
        }

        if (node.Kind == YamlNodeKind.Sequence)
        {
            if (UseFlowCollection(node, options) || node.Children.Count == 0)
            {
                return EmitNodePropertiesPrefix(node) + EmitFlowSequence(node, options);
            }

            return EmitBlockCollection(node, options, 0);
        }

        if (node.Kind == YamlNodeKind.Mapping)
        {
            if (UseFlowCollection(node, options) || node.Pairs.Count == 0)
            {
                return EmitNodePropertiesPrefix(node) + EmitFlowMapping(node, options);
            }

            return EmitBlockCollection(node, options, 0);
        }

        return "";
    }

    internal static string EmitBlockNode(YamlNode? node, YamlEmitterOptions options, int indent)
    {
        if (node == null || node.Kind == YamlNodeKind.Empty)
        {
            return "";
        }

        if (node.Kind == YamlNodeKind.Sequence)
        {
            if (UseFlowCollection(node, options) || node.Children.Count == 0)
            {
                return IndentText(indent) + EmitNodePropertiesPrefix(node) + EmitFlowSequence(node, options);
            }

            return EmitBlockCollection(node, options, indent);
        }

        if (node.Kind == YamlNodeKind.Mapping)
        {
            if (UseFlowCollection(node, options) || node.Pairs.Count == 0)
            {
                return IndentText(indent) + EmitNodePropertiesPrefix(node) + EmitFlowMapping(node, options);
            }

            return EmitBlockCollection(node, options, indent);
        }

        if (node.Kind == YamlNodeKind.Scalar && UseBlockScalar(node, options))
        {
            return EmitBlockScalar(node, options, indent);
        }

        return IndentText(indent) + EmitInlineNode(node, options);
    }

    internal static string EmitBlockSequence(YamlNode node, YamlEmitterOptions options, int indent)
    {
        if (node == null || node.Children.Count == 0)
        {
            return "[]";
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < node.Children.Count)
        {
            if (index > 0)
            {
                builder.Append("\n");
            }

            YamlNode child = node.Children[index];
            EmitNodeLeadingTrivia(builder, child, options, indent);
            builder.Append(IndentText(indent));
            if (UseBlockScalar(child, options))
            {
                builder.Append("- ");
                builder.Append(BlockScalarHeader(child, options));
                builder.Append(InlineTrailingComment(child, null, options));
                string content = EmitBlockScalarContent(child, options, indent + options.Indent);
                if (content != "")
                {
                    builder.Append("\n");
                    builder.Append(content);
                }
            }
            else if (NeedsNestedBlock(child, options))
            {
                string properties = EmitNodeProperties(child);
                if (properties != "")
                {
                    builder.Append("- ");
                    builder.Append(properties);
                    builder.Append("\n");
                    builder.Append(EmitBlockCollectionBody(child, options, indent + options.Indent));
                }
                else
                {
                    builder.Append("-");
                    builder.Append("\n");
                    builder.Append(EmitBlockNode(child, options, indent + options.Indent));
                }
            }
            else if (child == null || child.Kind == YamlNodeKind.Empty)
            {
                builder.Append("-");
            }
            else
            {
                builder.Append("- ");
                builder.Append(EmitInlineNode(child, options));
                builder.Append(InlineTrailingComment(child, null, options));
            }

            EmitNodeTrailingTrivia(builder, child, options, indent);

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string EmitBlockMapping(YamlNode node, YamlEmitterOptions options, int indent)
    {
        if (node == null || node.Pairs.Count == 0)
        {
            return "{}";
        }

        List<YamlPair> pairs = OrderedPairs(node, options);
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < pairs.Count)
        {
            if (index > 0)
            {
                builder.Append("\n");
            }

            YamlPair pair = pairs[index];
            EmitPairLeadingTrivia(builder, pair, options, indent);
            builder.Append(IndentText(indent));
            builder.Append(EmitMappingKey(pair.Key, options));
            builder.Append(":");
            if (pair.Value == null || pair.Value.Kind == YamlNodeKind.Empty)
            {
                builder.Append(InlineTrailingComment(pair.Value, pair, options));
            }
            else if (UseBlockScalar(pair.Value, options))
            {
                builder.Append(" ");
                builder.Append(BlockScalarHeader(pair.Value, options));
                builder.Append(InlineTrailingComment(pair.Value, pair, options));
                string content = EmitBlockScalarContent(pair.Value, options, indent + options.Indent);
                if (content != "")
                {
                    builder.Append("\n");
                    builder.Append(content);
                }
            }
            else if (NeedsNestedBlock(pair.Value, options))
            {
                string properties = EmitNodeProperties(pair.Value);
                if (properties != "")
                {
                    builder.Append(" ");
                    builder.Append(properties);
                    builder.Append("\n");
                    builder.Append(EmitBlockCollectionBody(pair.Value, options, indent + options.Indent));
                }
                else
                {
                    builder.Append("\n");
                    builder.Append(EmitBlockNode(pair.Value, options, indent + options.Indent));
                }
            }
            else
            {
                builder.Append(" ");
                builder.Append(EmitInlineNode(pair.Value, options));
                builder.Append(InlineTrailingComment(pair.Value, pair, options));
            }

            EmitPairTrailingTrivia(builder, pair, options, indent);

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string EmitBlockCollection(YamlNode node, YamlEmitterOptions options, int indent)
    {
        if (node == null)
        {
            return "";
        }

        string body = EmitBlockCollectionBody(node, options, indent);

        string properties = EmitNodeProperties(node);
        if (properties == "")
        {
            return body;
        }

        return IndentText(indent) + properties + "\n" + body;
    }

    internal static string EmitBlockCollectionBody(YamlNode node, YamlEmitterOptions options, int indent)
    {
        if (node == null)
        {
            return "";
        }

        if (node.Kind == YamlNodeKind.Sequence)
        {
            return EmitBlockSequence(node, options, indent);
        }

        if (node.Kind == YamlNodeKind.Mapping)
        {
            return EmitBlockMapping(node, options, indent);
        }

        return "";
    }

    internal static bool NeedsNestedBlock(YamlNode node, YamlEmitterOptions options)
    {
        if (node == null)
        {
            return false;
        }

        if (node.Kind == YamlNodeKind.Sequence)
        {
            return node.Children.Count > 0 && !UseFlowCollection(node, options);
        }

        if (node.Kind == YamlNodeKind.Mapping)
        {
            return node.Pairs.Count > 0 && !UseFlowCollection(node, options);
        }

        return false;
    }

    internal static string EmitInlineNode(YamlNode? node, YamlEmitterOptions options)
    {
        if (node == null || node.Kind == YamlNodeKind.Empty)
        {
            return "";
        }

        if (node.Kind == YamlNodeKind.Scalar)
        {
            if (UseBlockScalar(node, options))
            {
                return EmitFlowFallbackScalar(node, options);
            }

            return EmitScalar(node, options);
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            return "*" + node.AliasName;
        }

        if (node.Kind == YamlNodeKind.Sequence)
        {
            if (node.Children.Count == 0 || UseFlowCollection(node, options))
            {
                return EmitNodePropertiesPrefix(node) + EmitFlowSequence(node, options);
            }

            return EmitBlockSequence(node, options, 0);
        }

        if (node.Kind == YamlNodeKind.Mapping)
        {
            if (node.Pairs.Count == 0 || UseFlowCollection(node, options))
            {
                return EmitNodePropertiesPrefix(node) + EmitFlowMapping(node, options);
            }

            return EmitBlockMapping(node, options, 0);
        }

        return "";
    }

    internal static string EmitMappingKey(YamlNode? node, YamlEmitterOptions options)
    {
        if (node != null && node.Kind == YamlNodeKind.Sequence)
        {
            return EmitFlowSequence(node, options);
        }

        if (node != null && node.Kind == YamlNodeKind.Mapping)
        {
            return EmitFlowMapping(node, options);
        }

        return EmitInlineNode(node, options);
    }

    internal static string EmitMappingKeySingleLine(YamlNode? node, YamlEmitterOptions options)
    {
        if (node != null && node.Kind == YamlNodeKind.Sequence)
        {
            return EmitFlowSequenceSingleLine(node, options);
        }

        if (node != null && node.Kind == YamlNodeKind.Mapping)
        {
            return EmitFlowMappingSingleLine(node, options);
        }

        return EmitFlowNodeSingleLine(node, options);
    }

    internal static string EmitFlowNode(YamlNode? node, YamlEmitterOptions options)
    {
        if (node == null || node.Kind == YamlNodeKind.Empty)
        {
            return "null";
        }

        if (node.Kind == YamlNodeKind.Scalar)
        {
            if (UseBlockScalar(node, options))
            {
                return EmitFlowFallbackScalar(node, options);
            }

            return EmitScalar(node, options);
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            return "*" + node.AliasName;
        }

        if (node.Kind == YamlNodeKind.Sequence)
        {
            return EmitNodePropertiesPrefix(node) + EmitFlowSequence(node, options);
        }

        if (node.Kind == YamlNodeKind.Mapping)
        {
            return EmitNodePropertiesPrefix(node) + EmitFlowMapping(node, options);
        }

        return "null";
    }

    internal static string EmitFlowNodeSingleLine(YamlNode? node, YamlEmitterOptions options)
    {
        if (node == null || node.Kind == YamlNodeKind.Empty)
        {
            return "null";
        }

        if (node.Kind == YamlNodeKind.Scalar)
        {
            if (UseBlockScalar(node, options))
            {
                return EmitFlowFallbackScalar(node, options);
            }

            return EmitScalar(node, options);
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            return "*" + node.AliasName;
        }

        if (node.Kind == YamlNodeKind.Sequence)
        {
            return EmitNodePropertiesPrefix(node) + EmitFlowSequenceSingleLine(node, options);
        }

        if (node.Kind == YamlNodeKind.Mapping)
        {
            return EmitNodePropertiesPrefix(node) + EmitFlowMappingSingleLine(node, options);
        }

        return "null";
    }

    internal static string EmitFlowSequence(YamlNode node, YamlEmitterOptions options)
    {
        string text = EmitFlowSequenceSingleLine(node, options);
        if (ShouldWrapFlow(text, options) || FlowSequenceHasTrivia(node))
        {
            return EmitFlowSequenceMultiline(node, options);
        }

        return text;
    }

    internal static string EmitFlowSequenceSingleLine(YamlNode node, YamlEmitterOptions options)
    {
        if (node == null || node.Children.Count == 0)
        {
            return "[]";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("[");
        int index = 0;
        while (index < node.Children.Count)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(EmitFlowNodeSingleLine(node.Children[index], options));
            index = index + 1;
        }

        builder.Append("]");
        return builder.ToString();
    }

    internal static string EmitFlowSequenceMultiline(YamlNode node, YamlEmitterOptions options)
    {
        if (node == null || node.Children.Count == 0)
        {
            return "[]";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("[\n");
        int index = 0;
        while (index < node.Children.Count)
        {
            YamlNode child = node.Children[index];
            EmitNodeLeadingTrivia(builder, child, options, options.Indent);
            builder.Append(IndentText(options.Indent));
            builder.Append(EmitFlowNode(child, options));
            if (index + 1 < node.Children.Count)
            {
                builder.Append(",");
            }

            builder.Append(InlineTrailingComment(child, null, options));
            builder.Append("\n");
            EmitNodeFollowingTriviaAfterLine(builder, child, options, options.Indent);
            index = index + 1;
        }

        builder.Append("]");
        return builder.ToString();
    }

    internal static string EmitFlowMapping(YamlNode node, YamlEmitterOptions options)
    {
        string text = EmitFlowMappingSingleLine(node, options);
        if (ShouldWrapFlow(text, options) || FlowMappingHasTrivia(node))
        {
            return EmitFlowMappingMultiline(node, options);
        }

        return text;
    }

    internal static string EmitFlowMappingSingleLine(YamlNode node, YamlEmitterOptions options)
    {
        if (node == null || node.Pairs.Count == 0)
        {
            return "{}";
        }

        List<YamlPair> pairs = OrderedPairs(node, options);
        StringBuilder builder = new StringBuilder();
        builder.Append("{");
        int index = 0;
        while (index < pairs.Count)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            YamlPair pair = pairs[index];
            builder.Append(EmitMappingKeySingleLine(pair.Key, options));
            builder.Append(": ");
            builder.Append(EmitFlowNodeSingleLine(pair.Value, options));
            index = index + 1;
        }

        builder.Append("}");
        return builder.ToString();
    }

    internal static string EmitFlowMappingMultiline(YamlNode node, YamlEmitterOptions options)
    {
        if (node == null || node.Pairs.Count == 0)
        {
            return "{}";
        }

        List<YamlPair> pairs = OrderedPairs(node, options);
        StringBuilder builder = new StringBuilder();
        builder.Append("{\n");
        int index = 0;
        while (index < pairs.Count)
        {
            YamlPair pair = pairs[index];
            EmitPairLeadingTrivia(builder, pair, options, options.Indent);
            builder.Append(IndentText(options.Indent));
            builder.Append(EmitMappingKey(pair.Key, options));
            builder.Append(": ");
            builder.Append(EmitFlowNode(pair.Value, options));
            if (index + 1 < pairs.Count)
            {
                builder.Append(",");
            }

            builder.Append(InlineTrailingComment(pair.Value, pair, options));
            builder.Append("\n");
            if (HasTriviaPlacement(pair.Trivia, YamlTriviaPlacement.Following))
            {
                EmitTriviaFollowingAfterLine(builder, pair.Trivia, options, options.Indent);
            }
            else if (pair.Value != null)
            {
                EmitNodeFollowingTriviaAfterLine(builder, pair.Value, options, options.Indent);
            }

            index = index + 1;
        }

        builder.Append("}");
        return builder.ToString();
    }

    internal static bool FlowSequenceHasTrivia(YamlNode node)
    {
        if (node == null)
        {
            return false;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            YamlNode child = node.Children[index];
            if (child.Trivia.Count > 0
                || child.LeadingComments.Count > 0
                || child.TrailingComments.Count > 0
                || child.LeadingBlankLines.Count > 0
                || child.TrailingBlankLines.Count > 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool FlowMappingHasTrivia(YamlNode node)
    {
        if (node == null)
        {
            return false;
        }

        int index = 0;
        while (index < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[index];
            if (pair.Trivia.Count > 0
                || pair.LeadingComments.Count > 0
                || pair.TrailingComments.Count > 0
                || pair.LeadingBlankLines.Count > 0
                || pair.TrailingBlankLines.Count > 0
                || pair.Key != null && pair.Key.Trivia.Count > 0
                || pair.Value != null && pair.Value.Trivia.Count > 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ShouldWrapFlow(string text, YamlEmitterOptions options)
    {
        if (options == null || options.LineWidth <= 0)
        {
            return false;
        }

        return text != null && text.Length > options.LineWidth;
    }

    internal static List<YamlPair> OrderedPairs(YamlNode node, YamlEmitterOptions options)
    {
        List<YamlPair> pairs = new List<YamlPair>();
        if (node == null || node.Pairs == null)
        {
            return pairs;
        }

        int index = 0;
        while (index < node.Pairs.Count)
        {
            pairs.Add(node.Pairs[index]);
            index = index + 1;
        }

        if (options == null || !options.SortStringKeys)
        {
            return pairs;
        }

        index = 1;
        while (index < pairs.Count)
        {
            YamlPair current = pairs[index];
            int insert = index;
            while (insert > 0 && ComparePairsForEmission(pairs[insert - 1], current) > 0)
            {
                pairs[insert] = pairs[insert - 1];
                insert = insert - 1;
            }

            pairs[insert] = current;
            index = index + 1;
        }

        return pairs;
    }

    internal static int ComparePairsForEmission(YamlPair left, YamlPair right)
    {
        bool leftScalar = PairHasScalarKey(left);
        bool rightScalar = PairHasScalarKey(right);
        if (leftScalar && rightScalar)
        {
            return string.CompareOrdinal(PairKeyText(left), PairKeyText(right));
        }

        if (leftScalar)
        {
            return -1;
        }

        if (rightScalar)
        {
            return 1;
        }

        return 0;
    }

    internal static bool PairHasScalarKey(YamlPair pair)
    {
        return pair != null && pair.Key != null && pair.Key.Kind == YamlNodeKind.Scalar;
    }

    internal static string PairKeyText(YamlPair pair)
    {
        if (pair == null || pair.Key == null || pair.Key.Value == null)
        {
            return "";
        }

        return pair.Key.Value;
    }

    internal static bool UseFlowCollection(YamlNode node, YamlEmitterOptions options)
    {
        if (node == null)
        {
            return false;
        }

        if (node.CollectionStyle == YamlCollectionStyle.Flow)
        {
            return true;
        }

        return node.CollectionStyle == YamlCollectionStyle.Any
            && options != null
            && options.DefaultCollectionStyle == YamlCollectionStyle.Flow;
    }

    internal static string EmitNodePropertiesPrefix(YamlNode node)
    {
        string properties = EmitNodeProperties(node);
        if (properties == "")
        {
            return "";
        }

        return properties + " ";
    }

    internal static string EmitNodeProperties(YamlNode node)
    {
        if (node == null || node.Kind == YamlNodeKind.Alias)
        {
            return "";
        }

        string properties = "";
        if (node.Anchor != "")
        {
            properties = "&" + node.Anchor;
        }

        string tag = EmitTagProperty(node.Tag);
        if (node.TagPresentation != "" && node.Tag == node.ParsedTag)
        {
            tag = node.TagPresentation;
        }
        if (tag != "")
        {
            if (properties != "")
            {
                properties = properties + " ";
            }

            properties = properties + tag;
        }

        return properties;
    }

    internal static string EmitTagProperty(string tag)
    {
        if (tag == null || tag == "" || IsBuiltInTag(tag))
        {
            return "";
        }

        if (tag[0] == '!')
        {
            return tag;
        }

        return "!<" + tag + ">";
    }

    internal static bool IsBuiltInTag(string tag)
    {
        string normalized = YamlScalarResolver.NormalizeStandardTag(tag);
        return normalized == YamlScalarResolver.NullTag
            || normalized == YamlScalarResolver.BoolTag
            || normalized == YamlScalarResolver.IntTag
            || normalized == YamlScalarResolver.FloatTag
            || normalized == YamlScalarResolver.FailsafeStringTag
            || normalized == YamlScalarResolver.SequenceTag
            || normalized == YamlScalarResolver.MappingTag
            || normalized == YamlScalarResolver.TimestampTag
            || normalized == YamlScalarResolver.BinaryTag
            || normalized == YamlScalarResolver.SetTag
            || normalized == YamlScalarResolver.OrderedMapTag
            || normalized == YamlScalarResolver.PairsTag
            || normalized == YamlScalarResolver.MergeTag;
    }

    internal static string EmitRootScalar(YamlNode node)
    {
        return EmitScalar(node, new YamlEmitterOptions());
    }

    internal static string EmitScalar(YamlNode node, YamlEmitterOptions options)
    {
        if (node == null)
        {
            return "";
        }

        string tag = YamlScalarResolver.NormalizeStandardTag(node.Tag);
        if (ScalarRequiresSafeQuoting(node, options))
        {
            return EmitInvalidScalar(node, options);
        }

        if (tag == YamlScalarResolver.NullTag)
        {
            return EmitNodePropertiesPrefix(node) + "null";
        }

        string value = ScalarText(node);
        YamlScalarStyle style = EffectiveScalarStyle(node, options);
        if (style == YamlScalarStyle.Literal || style == YamlScalarStyle.Folded)
        {
            return EmitBlockScalar(node, options, 0);
        }

        if (style == YamlScalarStyle.SingleQuoted)
        {
            return EmitNodePropertiesPrefix(node) + EmitSingleQuotedScalar(value);
        }

        if (style == YamlScalarStyle.DoubleQuoted)
        {
            return EmitNodePropertiesPrefix(node) + EmitDoubleQuotedScalar(value);
        }

        if (!CanEmitPlainScalar(node))
        {
            return EmitNodePropertiesPrefix(node) + EmitFallbackQuotedScalar(value, options);
        }

        return EmitNodePropertiesPrefix(node) + value;
    }

    internal static string EmitInvalidScalar(YamlNode node, YamlEmitterOptions options)
    {
        if (options.InvalidValuePolicy == YamlInvalidValuePolicy.Error)
        {
            throw YamlException.Create("YAML scalar value cannot be emitted using its requested form.");
        }

        string value = ScalarText(node);
        if (!IsYamlPrintableText(value) || ContainsLineBreak(value))
        {
            return EmitNodePropertiesPrefix(node) + EmitDoubleQuotedScalar(value);
        }

        return EmitNodePropertiesPrefix(node) + EmitFallbackQuotedScalar(value, options);
    }

    internal static string EmitFlowFallbackScalar(YamlNode node, YamlEmitterOptions options)
    {
        if (options.InvalidValuePolicy == YamlInvalidValuePolicy.Error)
        {
            throw YamlException.Create("A block scalar cannot be emitted in this inline or flow context.");
        }

        return EmitNodePropertiesPrefix(node) + EmitDoubleQuotedScalar(ScalarText(node));
    }

    internal static bool ScalarRequiresSafeQuoting(YamlNode node, YamlEmitterOptions options)
    {
        if (node.Value == null)
        {
            return true;
        }

        string value = node.Value;
        if (!IsYamlPrintableText(value) || !ScalarTagMatchesValue(node, value))
        {
            return true;
        }

        YamlScalarStyle style = EffectiveScalarStyle(node, options);
        if (style == YamlScalarStyle.Plain || style == YamlScalarStyle.Any)
        {
            return !CanEmitPlainScalar(node);
        }

        if (style == YamlScalarStyle.SingleQuoted)
        {
            return ContainsLineBreak(value);
        }

        return style != YamlScalarStyle.DoubleQuoted
            && style != YamlScalarStyle.Literal
            && style != YamlScalarStyle.Folded;
    }

    internal static bool ScalarTagMatchesValue(YamlNode node, string value)
    {
        string tag = YamlScalarResolver.NormalizeStandardTag(node.Tag);
        if (tag == "" || tag == YamlScalarResolver.FailsafeStringTag)
        {
            return true;
        }

        if (tag == YamlScalarResolver.NullTag)
        {
            return YamlScalarResolver.IsYaml11Null(value);
        }

        if (tag == YamlScalarResolver.BoolTag)
        {
            return YamlScalarResolver.IsYaml11Bool(value);
        }

        if (tag == YamlScalarResolver.IntTag)
        {
            return YamlScalarResolver.IsYaml11Integer(value);
        }

        if (tag == YamlScalarResolver.FloatTag)
        {
            return YamlScalarResolver.IsYaml11Float(value)
                || YamlScalarResolver.IsYaml11Integer(value);
        }

        if (tag == YamlScalarResolver.TimestampTag)
        {
            return YamlScalarResolver.IsTimestamp(value, YamlScalarStyle.Plain);
        }

        if (tag == YamlScalarResolver.BinaryTag)
        {
            return IsBase64Text(value);
        }

        if (tag == YamlScalarResolver.MergeTag)
        {
            return value == "<<";
        }

        return tag != YamlScalarResolver.SequenceTag
            && tag != YamlScalarResolver.MappingTag
            && tag != YamlScalarResolver.SetTag
            && tag != YamlScalarResolver.OrderedMapTag
            && tag != YamlScalarResolver.PairsTag;
    }

    internal static bool IsBase64Text(string value)
    {
        int characterCount = 0;
        bool padding = false;
        int paddingCount = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == ' ' || current == '\t' || current == '\r' || current == '\n')
            {
                index = index + 1;
                continue;
            }

            if (current == '=')
            {
                padding = true;
                paddingCount = paddingCount + 1;
                if (paddingCount > 2)
                {
                    return false;
                }
            }
            else
            {
                bool alphabet = current >= 'A' && current <= 'Z'
                    || current >= 'a' && current <= 'z'
                    || current >= '0' && current <= '9'
                    || current == '+'
                    || current == '/';
                if (!alphabet || padding)
                {
                    return false;
                }
            }

            characterCount = characterCount + 1;
            index = index + 1;
        }

        return characterCount == 0 || characterCount % 4 == 0;
    }

    internal static bool ContainsLineBreak(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r' || value[index] == '\n')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsYamlPrintableText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current >= '\uD800' && current <= '\uDBFF')
            {
                if (index + 1 >= value.Length || value[index + 1] < '\uDC00' || value[index + 1] > '\uDFFF')
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (current >= '\uDC00' && current <= '\uDFFF')
            {
                return false;
            }

            if (current == '\t' || current == '\n' || current == '\r')
            {
                index = index + 1;
                continue;
            }

            if (current < ' ' || current >= '\u007F' && current <= '\u0084'
                || current >= '\u0086' && current <= '\u009F'
                || current == '\uFFFE' || current == '\uFFFF')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string EmitBlockScalar(YamlNode node, YamlEmitterOptions options, int indent)
    {
        if (node == null)
        {
            return "";
        }

        string content = EmitBlockScalarContent(node, options, indent + options.Indent);
        string marker = IndentText(indent) + BlockScalarHeader(node, options);
        if (content == "")
        {
            return marker;
        }

        return marker + "\n" + content;
    }

    internal static string EmitBlockScalarContent(YamlNode node, YamlEmitterOptions options, int indent)
    {
        string text = NormalizeLineBreaks(ScalarText(node));
        if (text == "")
        {
            return "";
        }

        if (text.Length > 0 && text[text.Length - 1] == '\n')
        {
            text = text.Substring(0, text.Length - 1);
        }

        if (text == "")
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            int start = index;
            while (index < text.Length && text[index] != '\n')
            {
                index = index + 1;
            }

            if (index > start)
            {
                builder.Append(IndentText(indent));
                builder.Append(text.Substring(start, index - start));
            }
            if (index < text.Length)
            {
                builder.Append("\n");
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static string BlockScalarIndicator(YamlNode node, YamlEmitterOptions options)
    {
        string indicator = "|";
        YamlScalarStyle style = EffectiveScalarStyle(node, options);
        if (style == YamlScalarStyle.Folded)
        {
            indicator = ">";
        }

        if (BlockScalarNeedsIndentIndicator(node) && options.Indent > 0 && options.Indent <= 9)
        {
            indicator = indicator + options.Indent.ToString();
        }

        return indicator;
    }

    internal static bool BlockScalarNeedsIndentIndicator(YamlNode node)
    {
        string value = ScalarText(node);
        int index = 0;
        while (index < value.Length && value[index] == '\n')
        {
            index = index + 1;
        }

        return index < value.Length && (value[index] == ' ' || value[index] == '\t');
    }

    internal static string BlockScalarHeader(YamlNode node, YamlEmitterOptions options)
    {
        return EmitNodePropertiesPrefix(node) + BlockScalarIndicator(node, options);
    }

    internal static bool UseBlockScalar(YamlNode node, YamlEmitterOptions options)
    {
        if (node == null || node.Kind != YamlNodeKind.Scalar)
        {
            return false;
        }

        YamlScalarStyle style = EffectiveScalarStyle(node, options);
        if (style != YamlScalarStyle.Literal && style != YamlScalarStyle.Folded)
        {
            return false;
        }

        string value = node.Value == null ? "" : node.Value;
        return node.Value != null
            && IsYamlPrintableText(value)
            && ScalarTagMatchesValue(node, value);
    }

    internal static string NormalizeLineBreaks(string value)
    {
        string text = value == null ? "" : value;
        text = text.Replace("\r\n", "\n");
        return text.Replace("\r", "\n");
    }

    internal static string ScalarText(YamlNode node)
    {
        if (node == null || node.Value == null)
        {
            return "";
        }

        return node.Value;
    }

    internal static YamlScalarStyle EffectiveScalarStyle(YamlNode node, YamlEmitterOptions options)
    {
        if (node != null && node.ScalarStyle != YamlScalarStyle.Any)
        {
            return node.ScalarStyle;
        }

        if (options != null)
        {
            return options.DefaultScalarStyle;
        }

        return YamlScalarStyle.Any;
    }

    internal static string EmitFallbackQuotedScalar(string value, YamlEmitterOptions options)
    {
        if (options != null && options.DefaultScalarStyle == YamlScalarStyle.DoubleQuoted)
        {
            return EmitDoubleQuotedScalar(value);
        }

        return EmitSingleQuotedScalar(value);
    }

    internal static bool CanEmitPlainScalar(YamlNode node)
    {
        if (node == null)
        {
            return false;
        }

        string tag = YamlScalarResolver.NormalizeStandardTag(node.Tag);
        if (tag == YamlScalarResolver.NullTag
            || tag == YamlScalarResolver.BoolTag
            || tag == YamlScalarResolver.IntTag
            || tag == YamlScalarResolver.FloatTag)
        {
            return true;
        }

        string value = node.Value == null ? "" : node.Value;
        if (!IsPlainScalarTextSafe(value))
        {
            return false;
        }

        if (tag == YamlScalarResolver.FailsafeStringTag && PlainScalarWouldResolveAsNonString(value))
        {
            return false;
        }

        return true;
    }

    internal static bool PlainScalarWouldResolveAsNonString(string value)
    {
        string tag = YamlScalarResolver.ResolveScalarTag(value, YamlScalarStyle.Plain, null);
        return tag != "" && tag != YamlScalarResolver.FailsafeStringTag;
    }

    internal static bool IsPlainScalarTextSafe(string value)
    {
        if (value == null || value == "")
        {
            return false;
        }

        if (StartsOrEndsWithWhitespace(value))
        {
            return false;
        }

        if (StartsWithDocumentMarker(value))
        {
            return false;
        }

        if (StartsWithPlainIndicator(value))
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '\r' || current == '\n' || current == '\t')
            {
                return false;
            }

            if (current == '['
                || current == ']'
                || current == '{'
                || current == '}'
                || current == ',')
            {
                return false;
            }

            if (current == ':' && (index + 1 >= value.Length || IsWhitespace(value[index + 1])))
            {
                return false;
            }

            if (current == '#' && (index == 0 || IsWhitespace(value[index - 1])))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool StartsOrEndsWithWhitespace(string value)
    {
        if (value == null || value == "")
        {
            return false;
        }

        return IsWhitespace(value[0]) || IsWhitespace(value[value.Length - 1]);
    }

    internal static bool StartsWithDocumentMarker(string value)
    {
        if (value == null)
        {
            return false;
        }

        if (value == "---" || value == "...")
        {
            return true;
        }

        return (value.Length > 3 && value.Substring(0, 4) == "--- ")
            || (value.Length > 3 && value.Substring(0, 4) == "... ");
    }

    internal static bool StartsWithPlainIndicator(string value)
    {
        if (value == null || value == "")
        {
            return false;
        }

        char current = value[0];
        return current == '-'
            || current == '?'
            || current == ':'
            || current == '['
            || current == ']'
            || current == '{'
            || current == '}'
            || current == ','
            || current == '&'
            || current == '*'
            || current == '#'
            || current == '!'
            || current == '|'
            || current == '>'
            || current == '\''
            || current == '"'
            || current == '%'
            || current == '@'
            || current == '`';
    }

    internal static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t';
    }

    internal static string EmitSingleQuotedScalar(string value)
    {
        string text = value == null ? "" : value;
        return "'" + text.Replace("'", "''") + "'";
    }

    internal static string EmitDoubleQuotedScalar(string value)
    {
        string text = value == null ? "" : value;
        StringBuilder builder = new StringBuilder();
        builder.Append("\"");
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '\\')
            {
                builder.Append("\\\\");
            }
            else if (current == '"')
            {
                builder.Append("\\\"");
            }
            else if (current == '\n')
            {
                builder.Append("\\n");
            }
            else if (current == '\r')
            {
                builder.Append("\\r");
            }
            else if (current == '\t')
            {
                builder.Append("\\t");
            }
            else if (current >= '\uD800' && current <= '\uDBFF')
            {
                if (index + 1 < text.Length && text[index + 1] >= '\uDC00' && text[index + 1] <= '\uDFFF')
                {
                    builder.Append(current);
                    index = index + 1;
                    builder.Append(text[index]);
                }
                else
                {
                    builder.Append("\\uFFFD");
                }
            }
            else if (current >= '\uDC00' && current <= '\uDFFF')
            {
                builder.Append("\\uFFFD");
            }
            else if (current < ' ')
            {
                builder.Append("\\x");
                builder.Append(((int)current).ToString("X2"));
            }
            else if (current >= '\u007F' && current <= '\u009F')
            {
                builder.Append("\\x");
                builder.Append(((int)current).ToString("X2"));
            }
            else if (current == '\uFFFE' || current == '\uFFFF')
            {
                builder.Append("\\uFFFD");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        builder.Append("\"");
        return builder.ToString();
    }

    internal static void EmitDocumentLeadingTrivia(StringBuilder builder, YamlDocument document, YamlEmitterOptions options, int indent)
    {
        if (builder == null || document == null || options == null || !options.EmitComments)
        {
            return;
        }

        if (HasTriviaPlacement(document.Trivia, YamlTriviaPlacement.Before))
        {
            EmitTriviaBefore(builder, document.Trivia, indent);
            return;
        }

        EmitBlankLines(builder, document.LeadingBlankLines);
        EmitCommentLines(builder, document.LeadingComments, indent);
    }

    internal static void EmitNodeLeadingTrivia(StringBuilder builder, YamlNode? node, YamlEmitterOptions options, int indent)
    {
        if (builder == null || node == null || options == null || !options.EmitComments)
        {
            return;
        }

        if (HasTriviaPlacement(node.Trivia, YamlTriviaPlacement.Before))
        {
            EmitTriviaBefore(builder, node.Trivia, indent);
            return;
        }

        EmitBlankLines(builder, node.LeadingBlankLines);
        EmitCommentLines(builder, node.LeadingComments, indent);
    }

    internal static void EmitPairLeadingTrivia(StringBuilder builder, YamlPair pair, YamlEmitterOptions options, int indent)
    {
        if (builder == null || pair == null || options == null || !options.EmitComments)
        {
            return;
        }

        if (HasTriviaPlacement(pair.Trivia, YamlTriviaPlacement.Before))
        {
            EmitTriviaBefore(builder, pair.Trivia, indent);
            return;
        }

        EmitBlankLines(builder, pair.LeadingBlankLines);
        EmitCommentLines(builder, pair.LeadingComments, indent);
        EmitNodeLeadingTrivia(builder, pair.Key, options, indent);
    }

    internal static void EmitNodeTrailingTrivia(StringBuilder builder, YamlNode? node, YamlEmitterOptions options, int indent)
    {
        if (builder == null || node == null || options == null || !options.EmitComments)
        {
            return;
        }

        if (node.Trivia.Count > 0)
        {
            EmitTriviaFollowingInBlock(builder, node.Trivia, indent);
            return;
        }

        EmitBlankLines(builder, node.TrailingBlankLines);
    }

    internal static void EmitPairTrailingTrivia(StringBuilder builder, YamlPair pair, YamlEmitterOptions options, int indent)
    {
        if (builder == null || pair == null || options == null || !options.EmitComments)
        {
            return;
        }

        if (HasTriviaPlacement(pair.Trivia, YamlTriviaPlacement.Following))
        {
            EmitTriviaFollowingInBlock(builder, pair.Trivia, indent);
            return;
        }

        if (pair.Value != null && HasTriviaPlacement(pair.Value.Trivia, YamlTriviaPlacement.Following))
        {
            EmitTriviaFollowingInBlock(builder, pair.Value.Trivia, indent);
            return;
        }

        EmitBlankLines(builder, pair.TrailingBlankLines);
        if (pair.Value != null && pair.Value.Trivia.Count == 0)
        {
            EmitBlankLines(builder, pair.Value.TrailingBlankLines);
        }
    }

    internal static void EmitTriviaBefore(StringBuilder builder, List<YamlTrivia> trivia, int indent)
    {
        int index = 0;
        while (index < trivia.Count)
        {
            YamlTrivia item = trivia[index];
            if (item != null && item.Placement == YamlTriviaPlacement.Before)
            {
                if (item.Kind == YamlTriviaKind.BlankLine)
                {
                    builder.Append("\n");
                }
                else if (item.Kind == YamlTriviaKind.Comment)
                {
                    builder.Append(IndentText(indent));
                    AppendLine(builder, NormalizeComment(item.Text));
                }
            }

            index = index + 1;
        }
    }

    internal static void EmitTriviaFollowingInBlock(StringBuilder builder, List<YamlTrivia> trivia, int indent)
    {
        int index = 0;
        while (index < trivia.Count)
        {
            YamlTrivia item = trivia[index];
            if (item != null && item.Placement == YamlTriviaPlacement.Following)
            {
                builder.Append("\n");
                if (item.Kind == YamlTriviaKind.Comment)
                {
                    builder.Append(IndentText(indent));
                    builder.Append(NormalizeComment(item.Text));
                }
            }

            index = index + 1;
        }
    }

    internal static void EmitTriviaFollowingAfterLine(StringBuilder builder, List<YamlTrivia> trivia, YamlEmitterOptions options, int indent)
    {
        if (builder == null || trivia == null || options == null || !options.EmitComments)
        {
            return;
        }

        int index = 0;
        while (index < trivia.Count)
        {
            YamlTrivia item = trivia[index];
            if (item != null && item.Placement == YamlTriviaPlacement.Following)
            {
                if (item.Kind == YamlTriviaKind.BlankLine)
                {
                    builder.Append("\n");
                }
                else if (item.Kind == YamlTriviaKind.Comment)
                {
                    builder.Append(IndentText(indent));
                    AppendLine(builder, NormalizeComment(item.Text));
                }
            }

            index = index + 1;
        }
    }

    internal static void EmitNodeFollowingTriviaAfterLine(StringBuilder builder, YamlNode? node, YamlEmitterOptions options, int indent)
    {
        if (node == null)
        {
            return;
        }

        EmitTriviaFollowingAfterLine(builder, node.Trivia, options, indent);
    }

    internal static bool HasTriviaPlacement(List<YamlTrivia> trivia, YamlTriviaPlacement placement)
    {
        if (trivia == null)
        {
            return false;
        }

        int index = 0;
        while (index < trivia.Count)
        {
            if (trivia[index] != null && trivia[index].Placement == placement)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void EmitBlankLines(StringBuilder builder, List<string> blankLines)
    {
        if (builder == null || blankLines == null)
        {
            return;
        }

        int index = 0;
        while (index < blankLines.Count)
        {
            builder.Append("\n");
            index = index + 1;
        }
    }

    internal static void EmitCommentLines(StringBuilder builder, List<string> comments, int indent)
    {
        if (builder == null || comments == null)
        {
            return;
        }

        int index = 0;
        while (index < comments.Count)
        {
            builder.Append(IndentText(indent));
            AppendLine(builder, NormalizeComment(comments[index]));
            index = index + 1;
        }
    }

    internal static bool CanAppendInlineTrailingComment(YamlNode? node, YamlEmitterOptions options)
    {
        if (node == null || options == null || !options.EmitComments)
        {
            return false;
        }

        if ((node.Kind == YamlNodeKind.Sequence || node.Kind == YamlNodeKind.Mapping)
            && node.CollectionStyle != YamlCollectionStyle.Flow)
        {
            return false;
        }

        if (UseBlockScalar(node, options))
        {
            return false;
        }

        if (node.Trivia.Count > 0)
        {
            return FirstTriviaComment(node.Trivia, YamlTriviaPlacement.Inline) != "";
        }

        return node.TrailingComments.Count > 0;
    }

    internal static string InlineTrailingComment(YamlNode? node, YamlPair? pair, YamlEmitterOptions options)
    {
        if (options == null || !options.EmitComments)
        {
            return "";
        }

        string comment = "";
        if (pair != null)
        {
            if (pair.Trivia.Count > 0)
            {
                comment = FirstTriviaComment(pair.Trivia, YamlTriviaPlacement.Inline);
            }
            else
            {
                comment = FirstComment(pair.TrailingComments);
            }
        }

        if (comment == "" && node != null)
        {
            if (node.Trivia.Count > 0)
            {
                comment = FirstTriviaComment(node.Trivia, YamlTriviaPlacement.Inline);
            }
            else
            {
                comment = FirstComment(node.TrailingComments);
            }
        }

        if (comment == "")
        {
            return "";
        }

        return " " + NormalizeComment(comment);
    }

    internal static string FirstTriviaComment(List<YamlTrivia> trivia, YamlTriviaPlacement placement)
    {
        if (trivia == null)
        {
            return "";
        }

        int index = 0;
        while (index < trivia.Count)
        {
            YamlTrivia item = trivia[index];
            if (item != null && item.Kind == YamlTriviaKind.Comment && item.Placement == placement)
            {
                return item.Text == null ? "" : item.Text;
            }

            index = index + 1;
        }

        return "";
    }

    internal static string FirstComment(List<string> comments)
    {
        if (comments == null || comments.Count == 0)
        {
            return "";
        }

        return comments[0] == null ? "" : comments[0];
    }

    internal static string NormalizeComment(string comment)
    {
        string text = comment == null ? "" : comment;
        if (text == "")
        {
            return "#";
        }

        if (text[0] == '#')
        {
            return text;
        }

        return "# " + text;
    }

    internal static string VersionToText(YamlVersion version)
    {
        if (version == YamlVersion.Version11)
        {
            return "1.1";
        }

        if (version == YamlVersion.Version12)
        {
            return "1.2";
        }

        return "";
    }

    internal static string IndentText(int indent)
    {
        if (indent <= 0)
        {
            return "";
        }

        return new string(' ', indent);
    }

    internal static void AppendLine(StringBuilder builder, string text)
    {
        if (builder == null)
        {
            return;
        }

        builder.Append(text == null ? "" : text);
        builder.Append("\n");
    }
}
