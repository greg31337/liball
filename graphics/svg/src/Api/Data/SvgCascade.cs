namespace svg;

/// <summary>Describes one valid declaration considered by the cascade.</summary>
public class SvgCascadeCandidate
{
    /// <summary>The normalized property name.</summary>
    public string PropertyName;
    /// <summary>The declaration value text.</summary>
    public string ValueText;
    /// <summary>Whether the declaration is important.</summary>
    public bool Important;
    /// <summary>The declaration origin.</summary>
    public SvgStyleOrigin Origin;
    /// <summary>How the declaration entered the cascade.</summary>
    public SvgCascadeSourceKind SourceKind;
    /// <summary>The enclosing layer name, or an empty string.</summary>
    public string LayerName;
    /// <summary>The merged layer order, or <c>-1</c> for an unlayered declaration.</summary>
    public int LayerOrder;
    /// <summary>Whether the declaration has style-attribute specificity.</summary>
    public int InlineSpecificity;
    /// <summary>The matching selector ID specificity.</summary>
    public int IdSpecificity;
    /// <summary>The matching selector class-like specificity.</summary>
    public int ClassSpecificity;
    /// <summary>The matching selector type specificity.</summary>
    public int TypeSpecificity;
    /// <summary>The stable order within this cascade operation.</summary>
    public long SourceOrder;
    /// <summary>The declaration or attribute source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The originating shorthand name, or an empty string for a longhand.</summary>
    public string ShorthandName;
    internal string LayerKey;

    internal SvgCascadeCandidate(string propertyName, string valueText, bool important, SvgStyleOrigin origin, SvgCascadeSourceKind sourceKind, string layerName, int layerOrder, int inlineSpecificity, int idSpecificity, int classSpecificity, int typeSpecificity, long sourceOrder, SvgSourceSpan? source, string shorthandName, string layerKey)
    {
        PropertyName = propertyName;
        ValueText = valueText;
        Important = important;
        Origin = origin;
        SourceKind = sourceKind;
        LayerName = layerName;
        LayerOrder = layerOrder;
        InlineSpecificity = inlineSpecificity;
        IdSpecificity = idSpecificity;
        ClassSpecificity = classSpecificity;
        TypeSpecificity = typeSpecificity;
        SourceOrder = sourceOrder;
        Source = source;
        ShorthandName = shorthandName;
        LayerKey = layerKey;
    }
}

/// <summary>Contains the cascaded text for one property.</summary>
public class SvgCascadedValue
{
    /// <summary>The normalized property name.</summary>
    public string PropertyName;
    /// <summary>The resulting specified value text.</summary>
    public string ValueText;
    /// <summary>The winning value text before custom-property substitution.</summary>
    public string AuthoredValueText;
    /// <summary>How the resulting value was obtained.</summary>
    public SvgCascadedValueKind Kind;
    /// <summary>The effective winning declaration, or <c>null</c> for an undeclared default.</summary>
    public SvgCascadeCandidate? Winner;
    /// <summary>The transitively referenced custom-property names in encounter order.</summary>
    public System.Collections.Generic.List<string> Dependencies;
    /// <summary>The variable failure, or <see cref="SvgCssVariableError.None"/>.</summary>
    public SvgCssVariableError VariableError;
    /// <summary>Whether substitution made the winning declaration invalid at computed-value time.</summary>
    public bool InvalidAtComputedValue;

    internal SvgCascadedValue(string propertyName, string valueText, SvgCascadedValueKind kind, SvgCascadeCandidate? winner)
    {
        PropertyName = propertyName;
        ValueText = valueText;
        AuthoredValueText = winner == null ? valueText : winner.ValueText;
        Kind = kind;
        Winner = winner;
        Dependencies = new System.Collections.Generic.List<string>();
        VariableError = SvgCssVariableError.None;
        InvalidAtComputedValue = false;
    }
}

/// <summary>Configures a bounded cascade operation.</summary>
public class SvgCascadeOptions
{
    /// <summary>The property metadata and grammar catalog.</summary>
    public SvgPropertyCatalog Properties;
    /// <summary>The CSS parsing and declaration limits.</summary>
    public SvgCssLimits Limits;
    /// <summary>The selector scope, dynamic state, and work limit.</summary>
    public SvgSelectorMatchOptions SelectorOptions;
    /// <summary>The media environment used for collected stylesheet entries.</summary>
    public SvgStyleEnvironment StyleEnvironment;
    /// <summary>The parent cascade result used for inheritance, or <c>null</c>.</summary>
    public SvgCascadeResult? Parent;
    /// <summary>Whether the required SVG user-agent defaults participate.</summary>
    public bool IncludeUserAgentDefaults;

    /// <summary>Creates options using the default property catalog and style environments.</summary>
    public SvgCascadeOptions()
    {
        Properties = SvgPropertyCatalog.CreateDefault();
        Limits = new SvgCssLimits();
        SelectorOptions = new SvgSelectorMatchOptions();
        StyleEnvironment = new SvgStyleEnvironment();
        Parent = null;
        IncludeUserAgentDefaults = true;
    }
}

/// <summary>Contains the deterministic output and provenance of one cascade operation.</summary>
public class SvgCascadeResult
{
    /// <summary>The source element used to produce this cascade, or <c>null</c> after invalid arguments.</summary>
    public SvgNode? SourceNode;
    /// <summary>The source element version represented by this cascade.</summary>
    public long SourceVersion;
    /// <summary>Whether collection and selector matching completed within their limits.</summary>
    public bool Success;
    /// <summary>The cascaded longhand and custom-property values.</summary>
    public System.Collections.Generic.List<SvgCascadedValue> Values;
    /// <summary>The valid candidates considered by the cascade.</summary>
    public System.Collections.Generic.List<SvgCascadeCandidate> Candidates;
    /// <summary>Diagnostics produced while collecting and matching declarations.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The valid computed custom-property token streams.</summary>
    public SvgCustomPropertySet CustomProperties;

    internal SvgCascadeResult(bool success, System.Collections.Generic.List<SvgCascadedValue> values, System.Collections.Generic.List<SvgCascadeCandidate> candidates, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        SourceNode = null;
        SourceVersion = 0;
        Success = success;
        Values = values;
        Candidates = candidates;
        Diagnostics = diagnostics;
        CustomProperties = new SvgCustomPropertySet();
    }

    /// <summary>Finds a cascaded value by CSS property name.</summary>
    /// <param name="propertyName">The property name to find.</param>
    /// <returns>The cascaded value, or <c>null</c>.</returns>
    internal SvgCascadedValue? Find(string? propertyName)
    {
        if (propertyName == null || propertyName.Length == 0) return null;
        bool custom = propertyName.StartsWith("--", System.StringComparison.Ordinal);
        int index = 0;
        while (index < Values.Count)
        {
            string candidate = Values[index].PropertyName;
            if (custom)
            {
                if (string.Equals(candidate, propertyName, System.StringComparison.Ordinal)) return Values[index];
            }
            else if (string.Equals(candidate, propertyName, System.StringComparison.OrdinalIgnoreCase)) return Values[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Finds properties that transitively depend on a custom property.</summary>
    /// <param name="customPropertyName">The case-sensitive custom-property name.</param>
    /// <returns>The dependent property names in result order.</returns>
    internal System.Collections.Generic.List<string> FindDependents(string? customPropertyName)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        if (customPropertyName == null || !customPropertyName.StartsWith("--", System.StringComparison.Ordinal)) return result;
        int valueIndex = 0;
        while (valueIndex < Values.Count)
        {
            SvgCascadedValue value = Values[valueIndex];
            int dependencyIndex = 0;
            while (dependencyIndex < value.Dependencies.Count)
            {
                if (string.Equals(value.Dependencies[dependencyIndex], customPropertyName, System.StringComparison.Ordinal))
                {
                    result.Add(value.PropertyName);
                    break;
                }
                dependencyIndex = dependencyIndex + 1;
            }
            valueIndex = valueIndex + 1;
        }
        return result;
    }
}
