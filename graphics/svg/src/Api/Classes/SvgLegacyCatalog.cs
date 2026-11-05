namespace svg;

/// <summary>Provides exhaustive SVG 1.1 element, attribute, and XLink compatibility policy.</summary>
public class SvgLegacyCatalog : ISvgLegacyCatalog
{
    System.Collections.Generic.List<SvgLegacyFeatureDefinition> elements;
    System.Collections.Generic.List<SvgLegacyFeatureDefinition> attributes;

    internal SvgLegacyCatalog(System.Collections.Generic.List<SvgLegacyFeatureDefinition> elementDefinitions, System.Collections.Generic.List<SvgLegacyFeatureDefinition> attributeDefinitions)
    {
        elements = elementDefinitions;
        attributes = attributeDefinitions;
    }

    /// <summary>Creates the complete SVG 1.1 compatibility catalog.</summary>
    /// <returns>A new catalog with legacy element, attribute, and XLink entries.</returns>
    internal SvgLegacyCatalog CreateDefault()
    {
        SvgLegacyCatalogBuilder builder = new SvgLegacyCatalogBuilder();
        return builder.Build();
    }

    /// <summary>Finds a legacy element definition.</summary>
    /// <param name="kind">The recognized element kind.</param>
    /// <returns>The definition, or <c>null</c>.</returns>
    public SvgLegacyFeatureDefinition? FindElement(SvgElementKind kind)
    {
        int index = 0;
        while (index < elements.Count)
        {
            if (elements[index].ElementKind == kind) return elements[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Finds a legacy attribute definition by expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The definition, or <c>null</c>.</returns>
    public SvgLegacyFeatureDefinition? FindAttribute(string? namespaceUri, string? localName)
    {
        if (namespaceUri == null || localName == null) return null;
        int index = 0;
        while (index < attributes.Count)
        {
            SvgLegacyFeatureDefinition definition = attributes[index];
            if (definition.NamespaceUri == namespaceUri && definition.LocalName == localName) return definition;
            index = index + 1;
        }
        return null;
    }

    /// <summary>Returns an independent legacy-element definition snapshot.</summary>
    /// <returns>All recognized SVG 1.1 element definitions.</returns>
    public System.Collections.Generic.List<SvgLegacyFeatureDefinition> GetElementEntries()
    {
        return new System.Collections.Generic.List<SvgLegacyFeatureDefinition>(elements);
    }

    /// <summary>Returns an independent legacy-attribute definition snapshot.</summary>
    /// <returns>All recognized SVG 1.1 and XLink attribute definitions.</returns>
    public System.Collections.Generic.List<SvgLegacyFeatureDefinition> GetAttributeEntries()
    {
        return new System.Collections.Generic.List<SvgLegacyFeatureDefinition>(attributes);
    }

    /// <summary>Analyzes one node and optionally its complete subtree.</summary>
    /// <param name="node">The document, element, or other node to inspect.</param>
    /// <param name="includeDescendants">Whether descendants are inspected in document order.</param>
    /// <returns>Located compatibility occurrences and diagnostics.</returns>
    public SvgLegacyAnalysisResult Analyze(SvgNode node, bool includeDescendants)
    {
        SvgLegacyAnalysisResult result = new SvgLegacyAnalysisResult(node);
        if (node == null)
        {
            result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, "A legacy compatibility analysis node is required.", null));
            return result;
        }
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(node);
        while (stack.Count != 0)
        {
            SvgNode current = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (current.Kind == SvgNodeKind.Element) AnalyzeElement(current, result);
            if (includeDescendants)
            {
                int childIndex = current.Children.Count - 1;
                while (childIndex >= 0)
                {
                    stack.Add(current.Children[childIndex]);
                    childIndex = childIndex - 1;
                }
            }
        }
        result.Success = true;
        return result;
    }

    void AnalyzeElement(SvgNode node, SvgLegacyAnalysisResult result)
    {
        SvgLegacyFeatureDefinition? element = FindElement(node.ElementKind);
        if (element != null) AddOccurrence(result, element, node, null, node.SourceSpan);
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            SvgLegacyFeatureDefinition? definition = FindAttribute(attribute.NamespaceUri, attribute.LocalName);
            if (definition != null) AddOccurrence(result, definition, node, attribute, attribute.SourceSpan == null ? node.SourceSpan : attribute.SourceSpan);
            index = index + 1;
        }
    }

    static void AddOccurrence(SvgLegacyAnalysisResult result, SvgLegacyFeatureDefinition definition, SvgNode node, SvgAttribute? attribute, SvgSourceSpan? source)
    {
        SvgDiagnostic diagnostic = Diagnostic(definition, source);
        result.Occurrences.Add(new SvgLegacyOccurrence(definition, node, attribute, diagnostic));
        result.Diagnostics.Add(diagnostic);
        if (definition.Handling == SvgLegacyHandling.Implemented) result.ImplementedCount = result.ImplementedCount + 1;
        else if (definition.Handling == SvgLegacyHandling.PreserveOnly) result.PreserveOnlyCount = result.PreserveOnlyCount + 1;
        else if (definition.Handling == SvgLegacyHandling.Deprecated) result.DeprecatedCount = result.DeprecatedCount + 1;
        else result.UnsupportedCount = result.UnsupportedCount + 1;
    }

    static SvgDiagnostic Diagnostic(SvgLegacyFeatureDefinition definition, SvgSourceSpan? source)
    {
        string feature = definition.FeatureType == SvgLegacyFeatureType.Element ? "element" : "attribute";
        string displayName = definition.NamespaceUri == SvgNamespaces.XLink ? "xlink:" + definition.LocalName : definition.LocalName;
        SvgDiagnosticSeverity severity = definition.Handling == SvgLegacyHandling.Implemented ? SvgDiagnosticSeverity.Info : SvgDiagnosticSeverity.Warning;
        SvgDiagnosticCode code = SvgDiagnosticCode.CompatibilityRecovery;
        string message;
        if (definition.Handling == SvgLegacyHandling.Implemented)
        {
            message = "The legacy SVG 1.1 " + feature + " '" + displayName + "' is implemented for compatibility.";
        }
        else if (definition.Handling == SvgLegacyHandling.PreserveOnly)
        {
            code = SvgDiagnosticCode.PreservedFeature;
            message = "The legacy SVG 1.1 " + feature + " '" + displayName + "' is preserved without semantic processing.";
        }
        else if (definition.Handling == SvgLegacyHandling.Deprecated)
        {
            message = "The legacy SVG 1.1 " + feature + " '" + displayName + "' is deprecated.";
            if (definition.Replacement.Length != 0) message = message + " Use '" + definition.Replacement + "' instead.";
        }
        else
        {
            code = SvgDiagnosticCode.UnsupportedFeature;
            message = "The legacy SVG 1.1 " + feature + " '" + displayName + "' is unsupported and is preserved without semantic processing.";
        }
        return new SvgDiagnostic(severity, code, message + " " + definition.Detail, source);
    }
}
