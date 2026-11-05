namespace svg;

/// <summary>Defines handling and migration metadata for one SVG 1.1 feature.</summary>
public class SvgLegacyFeatureDefinition
{
    /// <summary>Whether this definition describes an element or attribute.</summary>
    public SvgLegacyFeatureType FeatureType;
    /// <summary>The legacy element kind, or <c>Unknown</c> for an attribute.</summary>
    public SvgElementKind ElementKind;
    /// <summary>The expanded-name namespace URI.</summary>
    public string NamespaceUri;
    /// <summary>The case-sensitive local name.</summary>
    public string LocalName;
    /// <summary>The compatibility handling status.</summary>
    public SvgLegacyHandling Handling;
    /// <summary>The preferred current feature name, or an empty string.</summary>
    public string Replacement;
    /// <summary>A stable explanation of the compatibility decision.</summary>
    public string Detail;

    /// <summary>Creates one validated legacy feature definition.</summary>
    /// <param name="featureType">Whether this is an element or attribute.</param>
    /// <param name="elementKind">The legacy element kind, or <c>Unknown</c>.</param>
    /// <param name="namespaceUri">The non-null expanded-name namespace URI.</param>
    /// <param name="localName">The nonempty case-sensitive local name.</param>
    /// <param name="handling">The compatibility handling status.</param>
    /// <param name="replacement">The preferred current feature, or an empty string.</param>
    /// <param name="detail">The nonempty compatibility explanation.</param>
    public SvgLegacyFeatureDefinition(SvgLegacyFeatureType featureType, SvgElementKind elementKind, string namespaceUri, string localName, SvgLegacyHandling handling, string replacement, string detail)
    {
        if (featureType < SvgLegacyFeatureType.Element || featureType > SvgLegacyFeatureType.Attribute) throw new System.ArgumentOutOfRangeException("featureType");
        if (featureType == SvgLegacyFeatureType.Element && !new SvgNodeNames().IsSvg11LegacyElement(elementKind)) throw new System.ArgumentOutOfRangeException("elementKind");
        if (featureType == SvgLegacyFeatureType.Attribute && elementKind != SvgElementKind.Unknown) throw new System.ArgumentOutOfRangeException("elementKind");
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (localName == null || localName.Length == 0) throw new System.ArgumentOutOfRangeException("localName");
        if (handling < SvgLegacyHandling.Implemented || handling > SvgLegacyHandling.Unsupported) throw new System.ArgumentOutOfRangeException("handling");
        if (replacement == null) throw new System.ArgumentNullException("replacement");
        if (detail == null || detail.Length == 0) throw new System.ArgumentOutOfRangeException("detail");
        FeatureType = featureType;
        ElementKind = elementKind;
        NamespaceUri = namespaceUri;
        LocalName = localName;
        Handling = handling;
        Replacement = replacement;
        Detail = detail;
    }
}

/// <summary>Reports one located legacy element or attribute occurrence.</summary>
public class SvgLegacyOccurrence
{
    /// <summary>The matched legacy feature definition.</summary>
    public SvgLegacyFeatureDefinition Definition;
    /// <summary>The containing or matched element.</summary>
    public SvgNode Node;
    /// <summary>The matched attribute, or <c>null</c> for an element occurrence.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The explicit compatibility diagnostic.</summary>
    public SvgDiagnostic Diagnostic;

    internal SvgLegacyOccurrence(SvgLegacyFeatureDefinition definition, SvgNode node, SvgAttribute? attribute, SvgDiagnostic diagnostic)
    {
        Definition = definition;
        Node = node;
        Attribute = attribute;
        Diagnostic = diagnostic;
    }
}

/// <summary>Contains deterministic legacy-feature analysis for a node subtree.</summary>
public class SvgLegacyAnalysisResult
{
    /// <summary>The analyzed root node, or <c>null</c> for invalid input.</summary>
    public SvgNode? Root;
    /// <summary>The source document version at analysis time.</summary>
    public long SourceVersion;
    /// <summary>The legacy occurrences in document and attribute order.</summary>
    public System.Collections.Generic.List<SvgLegacyOccurrence> Occurrences;
    /// <summary>The explicit compatibility diagnostics in occurrence order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The count of implemented compatibility occurrences.</summary>
    public int ImplementedCount;
    /// <summary>The count of preserve-only occurrences.</summary>
    public int PreserveOnlyCount;
    /// <summary>The count of deprecated occurrences.</summary>
    public int DeprecatedCount;
    /// <summary>The count of unsupported occurrences.</summary>
    public int UnsupportedCount;
    /// <summary>Whether analysis completed for a valid node.</summary>
    public bool Success;

    internal SvgLegacyAnalysisResult(SvgNode? root)
    {
        Root = root;
        SourceVersion = root == null || root.OwnerDocument == null ? 0 : root.OwnerDocument.Version;
        Occurrences = new System.Collections.Generic.List<SvgLegacyOccurrence>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        ImplementedCount = 0;
        PreserveOnlyCount = 0;
        DeprecatedCount = 0;
        UnsupportedCount = 0;
        Success = false;
    }
}
