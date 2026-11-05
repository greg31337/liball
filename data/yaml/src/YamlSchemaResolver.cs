using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Applies schema tags to composed YAML streams, documents, or individual nodes.
/// </summary>
internal static class YamlSchemaResolver
{
    /// <summary>
    /// Applies schema-derived tags to every node in a YAML stream in place.
    /// </summary>
    /// <param name="stream">The YAML stream to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    internal static void Resolve(YamlStream? stream, YamlSchemaOptions? options)
    {
        if (stream == null)
        {
            return;
        }

        int index = 0;
        while (index < stream.Documents.Count)
        {
            ResolveDocument(stream.Documents[index], options);
            index = index + 1;
        }
    }

    /// <summary>
    /// Applies schema-derived tags to every node in a document in place.
    /// </summary>
    /// <param name="document">The YAML document to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    internal static void ResolveDocument(YamlDocument? document, YamlSchemaOptions? options)
    {
        if (document == null || document.Root == null)
        {
            return;
        }

        ResolveNode(document.Root, options);
    }

    /// <summary>
    /// Applies schema-derived tags to a node and all reachable child nodes in place.
    /// </summary>
    /// <param name="node">The YAML node to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    internal static void ResolveNode(YamlNode? node, YamlSchemaOptions? options)
    {
        if (node == null)
        {
            return;
        }

        if (node.Kind == YamlNodeKind.Empty)
        {
            ResolveEmpty(node);
        }
        else if (node.Kind == YamlNodeKind.Scalar)
        {
            ResolveScalar(node, options);
        }
        else if (node.Kind == YamlNodeKind.Sequence)
        {
            ResolveSequence(node, options);
        }
        else if (node.Kind == YamlNodeKind.Mapping)
        {
            ResolveMapping(node, options);
        }
    }

    internal static void ResolveEmpty(YamlNode node)
    {
        if (node.Tag == "")
        {
            node.Tag = YamlScalarResolver.NullTag;
            return;
        }

        node.Tag = YamlScalarResolver.NormalizeStandardTag(node.Tag);
    }

    internal static void ResolveScalar(YamlNode node, YamlSchemaOptions? options)
    {
        if (node.Tag != "")
        {
            node.Tag = YamlScalarResolver.NormalizeStandardTag(node.Tag);
            return;
        }

        if (!YamlScalarResolver.ShouldResolveScalars(options))
        {
            return;
        }

        node.Tag = YamlScalarResolver.ResolveScalarTag(node.Value, node.ScalarStyle, options);
    }

    internal static void ResolveSequence(YamlNode node, YamlSchemaOptions? options)
    {
        ApplyCollectionTag(node, YamlScalarResolver.SequenceTag);
        int index = 0;
        while (index < node.Children.Count)
        {
            ResolveNode(node.Children[index], options);
            index = index + 1;
        }
    }

    internal static void ResolveMapping(YamlNode node, YamlSchemaOptions? options)
    {
        ApplyCollectionTag(node, YamlScalarResolver.MappingTag);
        int index = 0;
        while (index < node.Pairs.Count)
        {
            ResolvePair(node.Pairs[index], options);
            index = index + 1;
        }
    }

    internal static void ResolvePair(YamlPair pair, YamlSchemaOptions? options)
    {
        if (pair == null)
        {
            return;
        }

        if (pair.Key != null)
        {
            ApplyMergeKeyTag(pair.Key, options);
            ResolveNode(pair.Key, options);
        }

        if (pair.Value != null)
        {
            ResolveNode(pair.Value, options);
        }
    }

    internal static void ApplyCollectionTag(YamlNode node, string defaultTag)
    {
        if (node.Tag == "")
        {
            node.Tag = defaultTag;
            return;
        }

        node.Tag = YamlScalarResolver.NormalizeStandardTag(node.Tag);
    }

    internal static void ApplyMergeKeyTag(YamlNode node, YamlSchemaOptions? options)
    {
        if (options == null || !options.AllowMergeKeys)
        {
            return;
        }

        if (node.Kind != YamlNodeKind.Scalar)
        {
            return;
        }

        if (node.Tag != "" || node.Value != "<<")
        {
            return;
        }

        if (node.ScalarStyle != YamlScalarStyle.Plain && node.ScalarStyle != YamlScalarStyle.Any)
        {
            return;
        }

        node.Tag = YamlScalarResolver.MergeTag;
    }

    internal static void ValidateUnknownTags(YamlStream stream, YamlSchemaOptions options, YamlComposerResult result)
    {
        if (stream == null || options == null || result == null || options.UnknownTagPolicy == YamlUnknownTagPolicy.Preserve)
        {
            return;
        }

        int index = 0;
        while (index < stream.Documents.Count)
        {
            ValidateUnknownTagsInNode(stream.Documents[index].Root, options, result, new Dictionary<YamlNode, YamlNode>());
            index = index + 1;
        }
    }

    internal static void ValidateUnknownTagsInNode(YamlNode? node, YamlSchemaOptions options, YamlComposerResult result, Dictionary<YamlNode, YamlNode> path)
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
        ValidateNodeTag(node, options, result);

        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            ValidateUnknownTagsInNode(node.Children[childIndex], options, result, path);
            childIndex = childIndex + 1;
        }

        int pairIndex = 0;
        while (pairIndex < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[pairIndex];
            ValidateUnknownTagsInNode(pair.Key, options, result, path);
            ValidateUnknownTagsInNode(pair.Value, options, result, path);
            pairIndex = pairIndex + 1;
        }

        path.Remove(node);
    }

    internal static void ValidateNodeTag(YamlNode node, YamlSchemaOptions options, YamlComposerResult result)
    {
        if (node == null || IsKnownTag(node.Tag, options))
        {
            return;
        }

        YamlDiagnosticSeverity severity = YamlDiagnosticSeverity.Warning;
        if (options.UnknownTagPolicy == YamlUnknownTagPolicy.Error)
        {
            severity = YamlDiagnosticSeverity.Error;
        }

        result.AddDiagnostic(YamlDiagnostic.Create(severity, "YAMLS006", "Unknown YAML tag '" + node.Tag + "' was found.", node.Span));
    }

    internal static bool IsKnownTag(string tag, YamlSchemaOptions? options)
    {
        string value = tag == null ? "" : tag;
        if (value == "")
        {
            return true;
        }

        if (YamlScalarResolver.IsStandardTag(value))
        {
            return true;
        }

        if (options != null && options.CustomTags != null && options.CustomTags.ContainsTag(value))
        {
            return true;
        }

        return false;
    }
}
