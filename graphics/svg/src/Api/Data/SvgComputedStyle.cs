namespace svg;

/// <summary>Contains a parsed CSS cursor value.</summary>
public class SvgComputedCursor
{
    /// <summary>The ordered cursor URL references.</summary>
    public System.Collections.Generic.List<SvgIriReference> Urls;
    /// <summary>The normalized terminal cursor keyword.</summary>
    public string Keyword;
    /// <summary>The preserved complete computed text.</summary>
    public string CssText;

    internal SvgComputedCursor(System.Collections.Generic.List<SvgIriReference> urls, string keyword, string cssText)
    {
        Urls = new System.Collections.Generic.List<SvgIriReference>(urls);
        Keyword = keyword;
        CssText = cssText;
    }
}

/// <summary>Stores one tagged, typed computed property value.</summary>
public class SvgComputedValue
{
    /// <summary>The normalized property name.</summary>
    public string PropertyName;
    /// <summary>The typed payload kind.</summary>
    public SvgComputedValueKind Kind;
    /// <summary>The canonical or preserved computed CSS text.</summary>
    public string CssText;
    /// <summary>The keyword payload, or an empty string.</summary>
    public string Keyword;
    /// <summary>The numeric payload, or zero for another kind.</summary>
    public double Number;
    /// <summary>The length payload, or <c>null</c>.</summary>
    public SvgLength? Length;
    /// <summary>The angle payload, or <c>null</c>.</summary>
    public SvgAngle? Angle;
    /// <summary>The color payload, or <c>null</c>.</summary>
    public SvgColor? Color;
    /// <summary>The paint payload, or <c>null</c>.</summary>
    public SvgPaint? Paint;
    /// <summary>The transform-list payload, or <c>null</c>.</summary>
    public SvgCssTransformList? TransformList;
    /// <summary>The transform-origin payload, or <c>null</c>.</summary>
    public SvgTransformOrigin? TransformOrigin;
    /// <summary>The transform-box payload when its kind applies.</summary>
    public SvgTransformBox TransformBox;
    /// <summary>The font-family payload.</summary>
    public System.Collections.Generic.List<SvgFontFamily> FontFamilies;
    /// <summary>The complete paint-order payload.</summary>
    public System.Collections.Generic.List<SvgPaintOrderComponent> PaintOrder;
    /// <summary>The typed length-list payload, or <c>null</c>.</summary>
    public SvgLengthListValue? LengthList;
    /// <summary>The URL reference payload, or <c>null</c>.</summary>
    public SvgIriReference? Reference;
    /// <summary>The cursor payload, or <c>null</c>.</summary>
    public SvgComputedCursor? Cursor;
    /// <summary>The winning declaration source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The transitive custom-property dependencies.</summary>
    public System.Collections.Generic.List<string> Dependencies;
    /// <summary>Whether conversion recovered to an inherited or initial value.</summary>
    public bool Recovered;
    /// <summary>Whether the property is retained for SVG 1.1 compatibility.</summary>
    public bool IsLegacy;

    internal SvgComputedValue(string propertyName, SvgComputedValueKind kind, string cssText, SvgSourceSpan? source)
    {
        PropertyName = propertyName;
        Kind = kind;
        CssText = cssText;
        Keyword = "";
        Number = 0;
        Length = null;
        Angle = null;
        Color = null;
        Paint = null;
        TransformList = null;
        TransformOrigin = null;
        TransformBox = SvgTransformBox.ViewBox;
        FontFamilies = new System.Collections.Generic.List<SvgFontFamily>();
        PaintOrder = new System.Collections.Generic.List<SvgPaintOrderComponent>();
        LengthList = null;
        Reference = null;
        Cursor = null;
        Source = source;
        Dependencies = new System.Collections.Generic.List<string>();
        Recovered = false;
        IsLegacy = false;
    }
}

/// <summary>Contains a non-ambiguous computed-number lookup.</summary>
public class SvgComputedNumberResult
{
    /// <summary>Whether the requested property is a computed number.</summary>
    public bool Success;
    /// <summary>The finite computed number, or zero.</summary>
    public double Value;

    internal SvgComputedNumberResult(bool success, double value)
    {
        Success = success;
        Value = value;
    }
}

/// <summary>Configures conversion from cascaded text to typed computed values.</summary>
public class SvgComputedStyleOptions
{
    /// <summary>The property metadata catalog.</summary>
    public SvgPropertyCatalog Properties;
    /// <summary>The CSS parsing and collection limits.</summary>
    public SvgCssLimits Limits;
    /// <summary>The parent computed style used for semantic recovery, or <c>null</c>.</summary>
    public SvgComputedStyle? Parent;
    /// <summary>The maximum paint fallback count.</summary>
    public int MaxPaintFallbacks;

    /// <summary>Creates conversion options with secure defaults.</summary>
    public SvgComputedStyleOptions()
    {
        Properties = SvgPropertyCatalog.CreateDefault();
        Limits = new SvgCssLimits();
        Parent = null;
        MaxPaintFallbacks = 16;
    }
}

/// <summary>Contains typed computed values for one SVG element.</summary>
public class SvgComputedStyle
{
    /// <summary>The source element.</summary>
    public SvgNode? Node;
    /// <summary>The source element version at conversion time.</summary>
    public long SourceVersion;
    /// <summary>The source cascade result.</summary>
    public SvgCascadeResult? Cascade;
    /// <summary>The computed longhand values in catalog order.</summary>
    public System.Collections.Generic.List<SvgComputedValue> Values;
    /// <summary>The computed custom-property token streams.</summary>
    public SvgCustomPropertySet CustomProperties;
    /// <summary>Conversion and cascade diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether bounded cascade and conversion completed.</summary>
    public bool Success;

    internal SvgComputedStyle(SvgNode? node, SvgCascadeResult? cascade)
    {
        Node = node;
        SourceVersion = node == null ? 0 : node.Version;
        Cascade = cascade;
        Values = new System.Collections.Generic.List<SvgComputedValue>();
        CustomProperties = new SvgCustomPropertySet();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Finds a computed value by property name.</summary>
    /// <param name="propertyName">The CSS property name.</param>
    /// <returns>The computed value, or <c>null</c>.</returns>
    internal SvgComputedValue? Find(string? propertyName)
    {
        if (propertyName == null) return null;
        int index = 0;
        while (index < Values.Count)
        {
            if (string.Equals(Values[index].PropertyName, propertyName, System.StringComparison.OrdinalIgnoreCase)) return Values[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Returns a computed color payload.</summary>
    /// <param name="propertyName">The color property name.</param>
    /// <returns>The color, or <c>null</c>.</returns>
    internal SvgColor? GetColor(string propertyName)
    {
        SvgComputedValue? value = Find(propertyName);
        return value == null || value.Kind != SvgComputedValueKind.Color ? null : value.Color;
    }

    /// <summary>Returns a computed paint payload.</summary>
    /// <param name="propertyName">The paint property name.</param>
    /// <returns>The paint, or <c>null</c>.</returns>
    internal SvgPaint? GetPaint(string propertyName)
    {
        SvgComputedValue? value = Find(propertyName);
        return value == null || value.Kind != SvgComputedValueKind.Paint ? null : value.Paint;
    }

    /// <summary>Returns a computed length payload.</summary>
    /// <param name="propertyName">The length property name.</param>
    /// <returns>The length, or <c>null</c>.</returns>
    internal SvgLength? GetLength(string propertyName)
    {
        SvgComputedValue? value = Find(propertyName);
        return value == null || value.Kind != SvgComputedValueKind.Length ? null : value.Length;
    }

    /// <summary>Returns a computed number lookup result.</summary>
    /// <param name="propertyName">The number property name.</param>
    /// <returns>The explicit lookup result.</returns>
    internal SvgComputedNumberResult GetNumber(string propertyName)
    {
        SvgComputedValue? value = Find(propertyName);
        return value == null || value.Kind != SvgComputedValueKind.Number ? new SvgComputedNumberResult(false, 0) : new SvgComputedNumberResult(true, value.Number);
    }

    /// <summary>Returns a normalized computed keyword.</summary>
    /// <param name="propertyName">The keyword property name.</param>
    /// <returns>The keyword, or an empty string.</returns>
    internal string GetKeyword(string propertyName)
    {
        SvgComputedValue? value = Find(propertyName);
        return value == null || value.Kind != SvgComputedValueKind.Keyword ? "" : value.Keyword;
    }

    /// <summary>Returns a computed CSS transform list.</summary>
    /// <returns>The transform list, or <c>null</c>.</returns>
    internal SvgCssTransformList? GetTransformList()
    {
        SvgComputedValue? value = Find(SvgCssPropertyNames.Transform);
        return value == null || value.Kind != SvgComputedValueKind.TransformList ? null : value.TransformList;
    }
}
