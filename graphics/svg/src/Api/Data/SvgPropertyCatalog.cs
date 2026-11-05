namespace svg;

/// <summary>Describes one property grammar available to supports-query evaluation.</summary>
public class SvgPropertySupportEntry
{
    /// <summary>The normalized CSS property name.</summary>
    public string Name;
    /// <summary>The value validation kind.</summary>
    public SvgPropertyValueSupportKind ValueKind;
    /// <summary>The accepted normalized keywords.</summary>
    public System.Collections.Generic.List<string> Keywords;
    /// <summary>An additionally required capability identifier, or an empty string.</summary>
    public string RequiredCapability;
    /// <summary>The canonical initial value text, or an empty string for caller-defined entries without metadata.</summary>
    public string InitialValue;
    /// <summary>Whether the computed property inherits by default.</summary>
    public bool Inherited;
    /// <summary>The canonical value grammar notation.</summary>
    public string Grammar;
    /// <summary>The external basis used by percentages in the property grammar.</summary>
    public SvgPropertyPercentageBasis PercentageBasis;
    /// <summary>The property's animation interpolation category.</summary>
    public SvgPropertyAnimationType AnimationType;
    /// <summary>The corresponding unqualified SVG presentation attribute name, or an empty string.</summary>
    public string PresentationAttributeName;
    /// <summary>Whether the property has an SVG presentation-attribute mapping.</summary>
    public bool IsPresentationAttribute;
    /// <summary>Whether the entry represents a shorthand property.</summary>
    public bool IsShorthand;
    /// <summary>Whether the entry exists for SVG 1.1 compatibility.</summary>
    public bool IsLegacy;
    /// <summary>The ordered longhand names set by a shorthand.</summary>
    public System.Collections.Generic.List<string> Longhands;

    /// <summary>Creates a property support entry.</summary>
    /// <param name="name">The nonempty property name.</param>
    /// <param name="valueKind">The value validation kind.</param>
    /// <param name="keywords">The accepted keywords.</param>
    /// <param name="requiredCapability">An additionally required capability identifier, or an empty string.</param>
    public SvgPropertySupportEntry(string name, SvgPropertyValueSupportKind valueKind, string[] keywords, string requiredCapability)
    {
        if (name == null || name.Length == 0) throw new System.ArgumentOutOfRangeException("name");
        if (valueKind < SvgPropertyValueSupportKind.Any || valueKind > SvgPropertyValueSupportKind.Transform) throw new System.ArgumentOutOfRangeException("valueKind");
        if (keywords == null) throw new System.ArgumentNullException("keywords");
        if (requiredCapability == null) throw new System.ArgumentNullException("requiredCapability");
        Name = NormalizeName(name);
        ValueKind = valueKind;
        Keywords = new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < keywords.Length)
        {
            if (keywords[index] == null || keywords[index].Length == 0) throw new System.ArgumentException("Property keywords must be nonempty.", "keywords");
            Keywords.Add(keywords[index].ToLowerInvariant());
            index = index + 1;
        }
        RequiredCapability = requiredCapability;
        InitialValue = "";
        Inherited = false;
        Grammar = "<declaration-value>";
        PercentageBasis = SvgPropertyPercentageBasis.None;
        AnimationType = SvgPropertyAnimationType.Discrete;
        PresentationAttributeName = "";
        IsPresentationAttribute = false;
        IsShorthand = false;
        IsLegacy = false;
        Longhands = new System.Collections.Generic.List<string>();
    }

    /// <summary>Creates a complete property metadata entry.</summary>
    /// <param name="name">The nonempty property name.</param>
    /// <param name="valueKind">The value validation kind.</param>
    /// <param name="keywords">The accepted keywords.</param>
    /// <param name="requiredCapability">An additionally required capability identifier, or an empty string.</param>
    /// <param name="initialValue">The canonical nonempty initial value.</param>
    /// <param name="inherited">Whether the property inherits by default.</param>
    /// <param name="grammar">The canonical nonempty value grammar notation.</param>
    /// <param name="percentageBasis">The percentage resolution basis.</param>
    /// <param name="animationType">The interpolation category.</param>
    /// <param name="presentationAttributeName">The SVG presentation attribute name, or an empty string.</param>
    /// <param name="isShorthand">Whether the property is a shorthand.</param>
    /// <param name="isLegacy">Whether the property is retained for SVG 1.1 compatibility.</param>
    /// <param name="longhands">The ordered shorthand longhands.</param>
    /// <returns>A new complete property metadata entry.</returns>
    internal static SvgPropertySupportEntry CreateMetadata(string name, SvgPropertyValueSupportKind valueKind, string[] keywords, string requiredCapability, string initialValue, bool inherited, string grammar, SvgPropertyPercentageBasis percentageBasis, SvgPropertyAnimationType animationType, string presentationAttributeName, bool isShorthand, bool isLegacy, string[] longhands)
    {
        if (name == null || name.Length == 0) throw new System.ArgumentOutOfRangeException("name");
        if (valueKind < SvgPropertyValueSupportKind.Any || valueKind > SvgPropertyValueSupportKind.Transform) throw new System.ArgumentOutOfRangeException("valueKind");
        if (keywords == null) throw new System.ArgumentNullException("keywords");
        if (requiredCapability == null) throw new System.ArgumentNullException("requiredCapability");
        if (initialValue == null || initialValue.Length == 0) throw new System.ArgumentOutOfRangeException("initialValue");
        if (grammar == null || grammar.Length == 0) throw new System.ArgumentOutOfRangeException("grammar");
        if (percentageBasis < SvgPropertyPercentageBasis.None || percentageBasis > SvgPropertyPercentageBasis.ObjectBoundingBox) throw new System.ArgumentOutOfRangeException("percentageBasis");
        if (animationType < SvgPropertyAnimationType.NotAnimatable || animationType > SvgPropertyAnimationType.List) throw new System.ArgumentOutOfRangeException("animationType");
        if (presentationAttributeName == null) throw new System.ArgumentNullException("presentationAttributeName");
        if (longhands == null) throw new System.ArgumentNullException("longhands");
        SvgPropertySupportEntry result = new SvgPropertySupportEntry(name, valueKind, keywords, requiredCapability);
        result.InitialValue = initialValue;
        result.Inherited = inherited;
        result.Grammar = grammar;
        result.PercentageBasis = percentageBasis;
        result.AnimationType = animationType;
        result.PresentationAttributeName = presentationAttributeName;
        result.IsPresentationAttribute = presentationAttributeName.Length != 0;
        result.IsShorthand = isShorthand;
        result.IsLegacy = isLegacy;
        int longhandIndex = 0;
        while (longhandIndex < longhands.Length)
        {
            if (longhands[longhandIndex] == null || longhands[longhandIndex].Length == 0) throw new System.ArgumentException("Shorthand longhands must be nonempty.", "longhands");
            result.Longhands.Add(NormalizeName(longhands[longhandIndex]));
            longhandIndex = longhandIndex + 1;
        }
        if (result.IsShorthand && result.Longhands.Count == 0) throw new System.ArgumentException("A shorthand requires at least one longhand.", "longhands");
        if (!result.IsShorthand && result.Longhands.Count != 0) throw new System.ArgumentException("A longhand cannot declare shorthand longhands.", "longhands");
        return result;
    }

    internal static string NormalizeName(string name)
    {
        return name.StartsWith("--", System.StringComparison.Ordinal) ? name : name.ToLowerInvariant();
    }
}

/// <summary>Stores property grammars used by supports declaration conditions.</summary>
public class SvgPropertyCatalog
{
    /// <summary>The mutable property entries in deterministic insertion order.</summary>
    public System.Collections.Generic.List<SvgPropertySupportEntry> Entries;

    /// <summary>Creates an empty property support catalog.</summary>
    public SvgPropertyCatalog()
    {
        Entries = new System.Collections.Generic.List<SvgPropertySupportEntry>();
    }

    /// <summary>Adds or replaces one property entry by ASCII-insensitive name.</summary>
    /// <param name="entry">The property support entry.</param>
    internal void Add(SvgPropertySupportEntry entry)
    {
        if (entry == null) throw new System.ArgumentNullException("entry");
        int index = 0;
        while (index < Entries.Count)
        {
            SvgPropertySupportEntry current = Entries[index];
            if (current != null && PropertyNameEquals(current.Name, entry.Name))
            {
                Entries[index] = entry;
                return;
            }
            index = index + 1;
        }
        Entries.Add(entry);
    }

    /// <summary>Finds a property support entry.</summary>
    /// <param name="name">The authored property name.</param>
    /// <returns>The entry, or <c>null</c>.</returns>
    internal SvgPropertySupportEntry? Find(string? name)
    {
        if (name == null || name.Length == 0) return null;
        int index = 0;
        while (index < Entries.Count)
        {
            SvgPropertySupportEntry entry = Entries[index];
            if (entry != null && PropertyNameEquals(entry.Name, name)) return entry;
            index = index + 1;
        }
        return null;
    }

    /// <summary>Finds the property mapped from an unqualified SVG presentation attribute.</summary>
    /// <param name="localName">The authored attribute local name.</param>
    /// <returns>The mapped property entry, or <c>null</c>.</returns>
    internal SvgPropertySupportEntry? FindPresentationAttribute(string? localName)
    {
        if (localName == null || localName.Length == 0) return null;
        int index = 0;
        while (index < Entries.Count)
        {
            SvgPropertySupportEntry entry = Entries[index];
            if (entry != null && entry.IsPresentationAttribute && string.Equals(entry.PresentationAttributeName, localName, System.StringComparison.Ordinal)) return entry;
            index = index + 1;
        }
        return null;
    }

    /// <summary>Tests a property and value against this catalog and active capabilities.</summary>
    /// <param name="name">The authored property name.</param>
    /// <param name="value">The authored value text.</param>
    /// <param name="limits">The CSS parsing limits.</param>
    /// <param name="capabilities">The active capability identifiers.</param>
    /// <returns><c>true</c> when the declaration is recognized and supported.</returns>
    internal bool Supports(string? name, string? value, SvgCssLimits? limits, SvgCapabilitySet? capabilities)
    {
        if (name == null || value == null || limits == null || capabilities == null) return false;
        if (!capabilities.Contains(SvgFeatureIdentifiers.CssParsing)) return false;
        if (!DeclarationSyntaxValid(name, value, limits)) return false;
        if (name.StartsWith("--", System.StringComparison.Ordinal)) return true;
        SvgPropertySupportEntry? entry = Find(name);
        if (entry == null) return false;
        if (entry.RequiredCapability.Length != 0 && !capabilities.Contains(entry.RequiredCapability)) return false;
        string trimmed = value.Trim();
        if (CssWideKeyword(trimmed)) return true;
        if (ContainsVariableFunction(trimmed)) return true;
        if (entry.ValueKind == SvgPropertyValueSupportKind.Any) return trimmed.Length != 0;
        if (entry.ValueKind == SvgPropertyValueSupportKind.Keyword) return ContainsKeyword(entry.Keywords, trimmed);
        if (entry.ValueKind == SvgPropertyValueSupportKind.Color) return new SvgColorParser().Parse(trimmed, limits.MaxSelectorTokens).Success;
        if (entry.ValueKind == SvgPropertyValueSupportKind.Paint) return new SvgPaintParser().Parse(trimmed, 16, limits.MaxSelectorTokens, false).Success;
        if (entry.ValueKind == SvgPropertyValueSupportKind.Length)
        {
            SvgLengthParseResult length = SvgLengthParser.ParseCssLength(trimmed, true, false, limits.MaxSelectorTokens);
            if (length.Success) return true;
            SvgNumberParseResult zero = SvgNumberParser.ParseCssNumber(trimmed, limits.MaxSelectorTokens);
            return zero.Success && zero.Value == 0;
        }
        if (entry.ValueKind == SvgPropertyValueSupportKind.Number) return SvgNumberParser.ParseCssNumber(trimmed, limits.MaxSelectorTokens).Success;
        if (entry.ValueKind == SvgPropertyValueSupportKind.Transform) return SvgCssTransformParser.ParseTransform(trimmed, limits.MaxDeclarations, limits.MaxSelectorTokens, false).Success || SvgTransformParser.Parse(trimmed, limits.MaxDeclarations, limits.MaxSelectorTokens).Success;
        return false;
    }

    /// <summary>Creates the complete property metadata catalog for the feature inventory.</summary>
    /// <returns>A new independent property metadata catalog.</returns>
    internal static SvgPropertyCatalog CreateDefault()
    {
        SvgPropertyCatalog result = new SvgPropertyCatalog();
        result.Add(ShorthandMetadata(SvgCssPropertyNames.Animation, SvgInitialValues.Animation, false, "<single-animation>#", [SvgCssPropertyNames.AnimationName, SvgCssPropertyNames.AnimationDuration, SvgCssPropertyNames.AnimationTimingFunction, SvgCssPropertyNames.AnimationDelay, SvgCssPropertyNames.AnimationIterationCount, SvgCssPropertyNames.AnimationDirection, SvgCssPropertyNames.AnimationFillMode, SvgCssPropertyNames.AnimationPlayState]));
        result.Add(Metadata(SvgCssPropertyNames.AnimationName, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.AnimationName, false, "none | <keyframes-name>#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable));
        result.Add(Metadata(SvgCssPropertyNames.AnimationDuration, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.AnimationDuration, false, "<time>#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable));
        result.Add(Metadata(SvgCssPropertyNames.AnimationTimingFunction, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.AnimationTimingFunction, false, "<easing-function>#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable));
        result.Add(Metadata(SvgCssPropertyNames.AnimationDelay, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.AnimationDelay, false, "<time>#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable));
        result.Add(Metadata(SvgCssPropertyNames.AnimationIterationCount, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.AnimationIterationCount, false, "infinite | <number>#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable));
        result.Add(Metadata(SvgCssPropertyNames.AnimationDirection, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.AnimationDirection, false, "normal | reverse | alternate | alternate-reverse#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable));
        result.Add(Metadata(SvgCssPropertyNames.AnimationFillMode, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.AnimationFillMode, false, "none | forwards | backwards | both#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable));
        result.Add(Metadata(SvgCssPropertyNames.AnimationPlayState, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.AnimationPlayState, false, "running | paused#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable));
        result.Add(Metadata(SvgCssPropertyNames.AlignmentBaseline, SvgPropertyValueSupportKind.Keyword, ["auto", "baseline", "before-edge", "text-before-edge", "middle", "central", "after-edge", "text-after-edge", "ideographic", "alphabetic", "hanging", "mathematical", "top", "center", "bottom"], SvgInitialValues.AlignmentBaseline, false, "<baseline-position>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.BaselineShift, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.BaselineShift, false, "baseline | sub | super | <length-percentage>", SvgPropertyPercentageBasis.LineHeight, SvgPropertyAnimationType.Length));
        result.Add(LegacyMetadata(SvgCssPropertyNames.Clip, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Clip, false, "auto | rect(<top>, <right>, <bottom>, <left>)", SvgPropertyPercentageBasis.ObjectBoundingBox, SvgPropertyAnimationType.ComputedValue));
        result.Add(Metadata(SvgCssPropertyNames.ClipPath, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.ClipPath, false, "none | <url> | <basic-shape> || <geometry-box>", SvgPropertyPercentageBasis.ObjectBoundingBox, SvgPropertyAnimationType.ComputedValue));
        result.Add(Metadata(SvgCssPropertyNames.ClipRule, SvgPropertyValueSupportKind.Keyword, ["nonzero", "evenodd"], SvgInitialValues.ClipRule, true, "nonzero | evenodd", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Color, SvgPropertyValueSupportKind.Color, [], SvgInitialValues.Color, true, "<color>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Color));
        result.Add(LegacyMetadata(SvgCssPropertyNames.ColorProfile, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.ColorProfile, true, "auto | sRGB | <name> | <url>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.ColorInterpolation, SvgPropertyValueSupportKind.Keyword, ["auto", "srgb", "linearrgb"], SvgInitialValues.ColorInterpolation, true, "auto | sRGB | linearRGB", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.ColorInterpolationFilters, SvgPropertyValueSupportKind.Keyword, ["auto", "srgb", "linearrgb"], SvgInitialValues.ColorInterpolationFilters, true, "auto | sRGB | linearRGB", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.ColorRendering, SvgPropertyValueSupportKind.Keyword, ["auto", "optimizespeed", "optimizequality"], SvgInitialValues.ColorRendering, true, "auto | optimizeSpeed | optimizeQuality", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Cursor, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Cursor, true, "[ [ <url> [ <x> <y> ]? , ]* [ auto | default | none | context-menu | help | pointer | progress | wait | cell | crosshair | text | vertical-text | alias | copy | move | no-drop | not-allowed | grab | grabbing | e-resize | n-resize | ne-resize | nw-resize | s-resize | se-resize | sw-resize | w-resize | ew-resize | ns-resize | nesw-resize | nwse-resize | col-resize | row-resize | all-scroll | zoom-in | zoom-out ] ]", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Direction, SvgPropertyValueSupportKind.Keyword, ["ltr", "rtl"], SvgInitialValues.Direction, true, "ltr | rtl", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Display, SvgPropertyValueSupportKind.Keyword, ["none", "contents", "block", "flow-root", "inline", "inline-block", "list-item", "table", "inline-table", "table-row-group", "table-header-group", "table-footer-group", "table-row", "table-cell", "table-column-group", "table-column", "table-caption", "ruby", "ruby-base", "ruby-text", "ruby-base-container", "ruby-text-container"], SvgInitialValues.Display, false, "<display-outside> || <display-inside> | <display-listitem> | <display-internal> | <display-box> | <display-legacy>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.DominantBaseline, SvgPropertyValueSupportKind.Keyword, ["auto", "text-bottom", "alphabetic", "ideographic", "middle", "central", "mathematical", "hanging", "text-top"], SvgInitialValues.DominantBaseline, true, "auto | text-bottom | alphabetic | ideographic | middle | central | mathematical | hanging | text-top", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(LegacyMetadata(SvgCssPropertyNames.EnableBackground, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.EnableBackground, false, "accumulate | new [ <x> <y> <width> <height> ]?", SvgPropertyPercentageBasis.ObjectBoundingBox, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Fill, SvgPropertyValueSupportKind.Paint, [], SvgInitialValues.Fill, true, "<paint>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Paint));
        result.Add(Metadata(SvgCssPropertyNames.FillOpacity, SvgPropertyValueSupportKind.Number, [], SvgInitialValues.FillOpacity, true, "<alpha-value>", SvgPropertyPercentageBasis.Number, SvgPropertyAnimationType.Number));
        result.Add(Metadata(SvgCssPropertyNames.FillRule, SvgPropertyValueSupportKind.Keyword, ["nonzero", "evenodd"], SvgInitialValues.FillRule, true, "nonzero | evenodd", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Filter, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Filter, false, "none | <filter-value-list>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.ComputedValue));
        result.Add(Metadata(SvgCssPropertyNames.FloodColor, SvgPropertyValueSupportKind.Color, [], SvgInitialValues.FloodColor, false, "<color>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Color));
        result.Add(Metadata(SvgCssPropertyNames.FloodOpacity, SvgPropertyValueSupportKind.Number, [], SvgInitialValues.FloodOpacity, false, "<alpha-value>", SvgPropertyPercentageBasis.Number, SvgPropertyAnimationType.Number));
        result.Add(ShorthandMetadata(SvgCssPropertyNames.Font, SvgInitialValues.Font, true, "[ [ <font-style> || <font-variant-css2> || <font-weight> || <font-stretch-css3> ]? <font-size> [ / <line-height> ]? <font-family> ] | <system-family-name>", [SvgCssPropertyNames.FontStyle, SvgCssPropertyNames.FontVariant, SvgCssPropertyNames.FontWeight, SvgCssPropertyNames.FontStretch, SvgCssPropertyNames.FontSize, SvgCssPropertyNames.FontFamily]));
        result.Add(Metadata(SvgCssPropertyNames.FontFamily, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontFamily, true, "<family-name> [ , <family-name> ]*", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.FontFeatureSettings, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontFeatureSettings, true, "normal | <feature-tag-value>#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.FontKerning, SvgPropertyValueSupportKind.Keyword, ["auto", "normal", "none"], SvgInitialValues.FontKerning, true, "auto | normal | none", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.FontSize, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontSize, true, "<absolute-size> | <relative-size> | <length-percentage> | math", SvgPropertyPercentageBasis.FontSize, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.FontSizeAdjust, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontSizeAdjust, true, "none | [ ex-height | cap-height | ch-width | ic-width | ic-height ]? [ from-font | <number> ]", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Number));
        result.Add(Metadata(SvgCssPropertyNames.FontStretch, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontStretch, true, "normal | <percentage> | ultra-condensed | extra-condensed | condensed | semi-condensed | semi-expanded | expanded | extra-expanded | ultra-expanded", SvgPropertyPercentageBasis.Number, SvgPropertyAnimationType.Number));
        result.Add(Metadata(SvgCssPropertyNames.FontStyle, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontStyle, true, "normal | italic | left | right | oblique <angle>{0,2}", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.FontVariant, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontVariant, true, "normal | none | <common-lig-values> || <discretionary-lig-values> || <historical-lig-values> || <contextual-alt-values>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.FontVariationSettings, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontVariationSettings, true, "normal | [ <string> <number> ]#", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.ComputedValue));
        result.Add(Metadata(SvgCssPropertyNames.FontWeight, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.FontWeight, true, "<font-weight-absolute> | bolder | lighter", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Number));
        result.Add(LegacyMetadata(SvgCssPropertyNames.GlyphOrientationHorizontal, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.GlyphOrientationHorizontal, true, "<angle>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(LegacyMetadata(SvgCssPropertyNames.GlyphOrientationVertical, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.GlyphOrientationVertical, true, "auto | <angle>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.ImageRendering, SvgPropertyValueSupportKind.Keyword, ["auto", "smooth", "high-quality", "crisp-edges", "pixelated", "optimizespeed", "optimizequality"], SvgInitialValues.ImageRendering, true, "auto | smooth | high-quality | crisp-edges | pixelated | optimizeSpeed | optimizeQuality", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.InlineSize, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.InlineSize, false, "auto | <length-percentage>", SvgPropertyPercentageBasis.ViewportWidth, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.Isolation, SvgPropertyValueSupportKind.Keyword, ["auto", "isolate"], SvgInitialValues.Isolation, false, "auto | isolate", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(LegacyMetadata(SvgCssPropertyNames.Kerning, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Kerning, true, "auto | normal | <length>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.LetterSpacing, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.LetterSpacing, true, "normal | <length>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.LightingColor, SvgPropertyValueSupportKind.Color, [], SvgInitialValues.LightingColor, false, "<color>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Color));
        result.Add(ShorthandMetadata(SvgCssPropertyNames.Marker, SvgInitialValues.Marker, true, "none | <url>", [SvgCssPropertyNames.MarkerStart, SvgCssPropertyNames.MarkerMid, SvgCssPropertyNames.MarkerEnd]));
        result.Add(Metadata(SvgCssPropertyNames.MarkerEnd, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.MarkerEnd, true, "none | <url>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.MarkerMid, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.MarkerMid, true, "none | <url>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.MarkerStart, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.MarkerStart, true, "none | <url>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Mask, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Mask, false, "<mask-layer>#", SvgPropertyPercentageBasis.ObjectBoundingBox, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.MaskType, SvgPropertyValueSupportKind.Keyword, ["luminance", "alpha"], SvgInitialValues.MaskType, false, "luminance | alpha", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.MixBlendMode, SvgPropertyValueSupportKind.Keyword, ["normal", "multiply", "screen", "overlay", "darken", "lighten", "color-dodge", "color-burn", "hard-light", "soft-light", "difference", "exclusion", "hue", "saturation", "color", "luminosity", "plus-darker", "plus-lighter"], SvgInitialValues.MixBlendMode, false, "<blend-mode>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Opacity, SvgPropertyValueSupportKind.Number, [], SvgInitialValues.Opacity, false, "<opacity-value>", SvgPropertyPercentageBasis.Number, SvgPropertyAnimationType.Number));
        result.Add(ShorthandMetadata(SvgCssPropertyNames.Overflow, SvgInitialValues.Overflow, false, "[ visible | hidden | clip | scroll | auto ]{1,2}", [SvgCssPropertyNames.OverflowX, SvgCssPropertyNames.OverflowY]));
        result.Add(Metadata(SvgCssPropertyNames.OverflowX, SvgPropertyValueSupportKind.Keyword, ["visible", "hidden", "clip", "scroll", "auto"], SvgInitialValues.OverflowX, false, "visible | hidden | clip | scroll | auto", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.OverflowY, SvgPropertyValueSupportKind.Keyword, ["visible", "hidden", "clip", "scroll", "auto"], SvgInitialValues.OverflowY, false, "visible | hidden | clip | scroll | auto", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.PaintOrder, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.PaintOrder, true, "normal | [ fill || stroke || markers ]", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.PointerEvents, SvgPropertyValueSupportKind.Keyword, ["none", "auto", "bounding-box", "visiblepainted", "visiblefill", "visiblestroke", "visible", "painted", "fill", "stroke", "all"], SvgInitialValues.PointerEvents, true, "auto | bounding-box | visiblePainted | visibleFill | visibleStroke | visible | painted | fill | stroke | all | none", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.ShapeRendering, SvgPropertyValueSupportKind.Keyword, ["auto", "optimizespeed", "crispedges", "geometricprecision"], SvgInitialValues.ShapeRendering, true, "auto | optimizeSpeed | crispEdges | geometricPrecision", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.StopColor, SvgPropertyValueSupportKind.Color, [], SvgInitialValues.StopColor, false, "<color>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Color));
        result.Add(Metadata(SvgCssPropertyNames.StopOpacity, SvgPropertyValueSupportKind.Number, [], SvgInitialValues.StopOpacity, false, "<alpha-value>", SvgPropertyPercentageBasis.Number, SvgPropertyAnimationType.Number));
        result.Add(Metadata(SvgCssPropertyNames.Stroke, SvgPropertyValueSupportKind.Paint, [], SvgInitialValues.Stroke, true, "<paint>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Paint));
        result.Add(Metadata(SvgCssPropertyNames.StrokeDashArray, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.StrokeDashArray, true, "none | <length-percentage>#", SvgPropertyPercentageBasis.ViewportSize, SvgPropertyAnimationType.List));
        result.Add(Metadata(SvgCssPropertyNames.StrokeDashOffset, SvgPropertyValueSupportKind.Length, [], SvgInitialValues.StrokeDashOffset, true, "<length-percentage>", SvgPropertyPercentageBasis.ViewportSize, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.StrokeLineCap, SvgPropertyValueSupportKind.Keyword, ["butt", "round", "square"], SvgInitialValues.StrokeLineCap, true, "butt | round | square", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.StrokeLineJoin, SvgPropertyValueSupportKind.Keyword, ["miter", "miter-clip", "round", "bevel", "arcs"], SvgInitialValues.StrokeLineJoin, true, "miter | miter-clip | round | bevel | arcs", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.StrokeMiterLimit, SvgPropertyValueSupportKind.Number, [], SvgInitialValues.StrokeMiterLimit, true, "<number>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Number));
        result.Add(Metadata(SvgCssPropertyNames.StrokeOpacity, SvgPropertyValueSupportKind.Number, [], SvgInitialValues.StrokeOpacity, true, "<alpha-value>", SvgPropertyPercentageBasis.Number, SvgPropertyAnimationType.Number));
        result.Add(Metadata(SvgCssPropertyNames.StrokeWidth, SvgPropertyValueSupportKind.Length, [], SvgInitialValues.StrokeWidth, true, "<length-percentage>", SvgPropertyPercentageBasis.ViewportSize, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.TextAnchor, SvgPropertyValueSupportKind.Keyword, ["start", "middle", "end"], SvgInitialValues.TextAnchor, true, "start | middle | end", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(ShorthandMetadata(SvgCssPropertyNames.TextDecoration, SvgInitialValues.TextDecoration, false, "<text-decoration-line> || <text-decoration-style> || <text-decoration-color>", [SvgCssPropertyNames.TextDecorationLine, SvgCssPropertyNames.TextDecorationStyle, SvgCssPropertyNames.TextDecorationColor]));
        result.Add(Metadata(SvgCssPropertyNames.TextDecorationColor, SvgPropertyValueSupportKind.Color, [], SvgInitialValues.TextDecorationColor, false, "<color>", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Color));
        result.Add(Metadata(SvgCssPropertyNames.TextDecorationLine, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.TextDecorationLine, false, "none | [ underline || overline || line-through || blink ]", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.TextDecorationStyle, SvgPropertyValueSupportKind.Keyword, ["solid", "double", "dotted", "dashed", "wavy"], SvgInitialValues.TextDecorationStyle, false, "solid | double | dotted | dashed | wavy", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.TextRendering, SvgPropertyValueSupportKind.Keyword, ["auto", "optimizespeed", "optimizelegibility", "geometricprecision"], SvgInitialValues.TextRendering, true, "auto | optimizeSpeed | optimizeLegibility | geometricPrecision", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Transform, SvgPropertyValueSupportKind.Transform, [], SvgInitialValues.Transform, false, "none | <transform-list>", SvgPropertyPercentageBasis.ObjectBoundingBox, SvgPropertyAnimationType.Transform));
        result.Add(Metadata(SvgCssPropertyNames.TransformBox, SvgPropertyValueSupportKind.Keyword, ["content-box", "border-box", "fill-box", "stroke-box", "view-box"], SvgInitialValues.TransformBox, false, "content-box | border-box | fill-box | stroke-box | view-box", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.TransformOrigin, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.TransformOrigin, false, "[ left | center | right | top | bottom | <length-percentage> ]{1,2} <length>?", SvgPropertyPercentageBasis.ObjectBoundingBox, SvgPropertyAnimationType.List));
        result.Add(Metadata(SvgCssPropertyNames.UnicodeBidi, SvgPropertyValueSupportKind.Keyword, ["normal", "embed", "isolate", "bidi-override", "isolate-override", "plaintext"], SvgInitialValues.UnicodeBidi, false, "normal | embed | isolate | bidi-override | isolate-override | plaintext", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.VectorEffect, SvgPropertyValueSupportKind.Keyword, ["none", "non-scaling-stroke", "non-scaling-size", "non-rotation", "fixed-position"], SvgInitialValues.VectorEffect, false, "none | non-scaling-stroke | non-scaling-size | non-rotation | fixed-position", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Visibility, SvgPropertyValueSupportKind.Keyword, ["visible", "hidden", "collapse"], SvgInitialValues.Visibility, true, "visible | hidden | collapse", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.WhiteSpace, SvgPropertyValueSupportKind.Keyword, ["normal", "pre", "nowrap", "pre-wrap", "break-spaces", "pre-line"], SvgInitialValues.WhiteSpace, true, "normal | pre | nowrap | pre-wrap | break-spaces | pre-line", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.WordSpacing, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.WordSpacing, true, "normal | <length-percentage>", SvgPropertyPercentageBasis.FontSize, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.WritingMode, SvgPropertyValueSupportKind.Keyword, ["horizontal-tb", "vertical-rl", "vertical-lr", "lr", "lr-tb", "rl", "rl-tb", "tb", "tb-rl"], SvgInitialValues.WritingMode, true, "horizontal-tb | vertical-rl | vertical-lr | lr | lr-tb | rl | rl-tb | tb | tb-rl", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.Discrete));
        result.Add(Metadata(SvgCssPropertyNames.Cx, SvgPropertyValueSupportKind.Length, [], SvgInitialValues.Cx, false, "<length-percentage>", SvgPropertyPercentageBasis.ViewportWidth, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.Cy, SvgPropertyValueSupportKind.Length, [], SvgInitialValues.Cy, false, "<length-percentage>", SvgPropertyPercentageBasis.ViewportHeight, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.D, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.D, false, "none | path(<string>)", SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.ComputedValue));
        result.Add(Metadata(SvgCssPropertyNames.Height, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Height, false, "auto | <length-percentage>", SvgPropertyPercentageBasis.ViewportHeight, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.R, SvgPropertyValueSupportKind.Length, [], SvgInitialValues.R, false, "<length-percentage>", SvgPropertyPercentageBasis.ViewportSize, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.Rx, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Rx, false, "auto | <length-percentage>", SvgPropertyPercentageBasis.ViewportWidth, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.Ry, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Ry, false, "auto | <length-percentage>", SvgPropertyPercentageBasis.ViewportHeight, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.Width, SvgPropertyValueSupportKind.Any, [], SvgInitialValues.Width, false, "auto | <length-percentage>", SvgPropertyPercentageBasis.ViewportWidth, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.X, SvgPropertyValueSupportKind.Length, [], SvgInitialValues.X, false, "<length-percentage>", SvgPropertyPercentageBasis.ViewportWidth, SvgPropertyAnimationType.Length));
        result.Add(Metadata(SvgCssPropertyNames.Y, SvgPropertyValueSupportKind.Length, [], SvgInitialValues.Y, false, "<length-percentage>", SvgPropertyPercentageBasis.ViewportHeight, SvgPropertyAnimationType.Length));
        return result;
    }

    internal static SvgPropertySupportEntry Metadata(string name, SvgPropertyValueSupportKind kind, string[] keywords, string initialValue, bool inherited, string grammar, SvgPropertyPercentageBasis percentageBasis, SvgPropertyAnimationType animationType)
    {
        return SvgPropertySupportEntry.CreateMetadata(name, kind, keywords, "", initialValue, inherited, grammar, percentageBasis, animationType, PresentationAttributeName(name), false, false, []);
    }

    internal static SvgPropertySupportEntry LegacyMetadata(string name, SvgPropertyValueSupportKind kind, string[] keywords, string initialValue, bool inherited, string grammar, SvgPropertyPercentageBasis percentageBasis, SvgPropertyAnimationType animationType)
    {
        return SvgPropertySupportEntry.CreateMetadata(name, kind, keywords, "", initialValue, inherited, grammar, percentageBasis, animationType, PresentationAttributeName(name), false, true, []);
    }

    internal static SvgPropertySupportEntry ShorthandMetadata(string name, string initialValue, bool inherited, string grammar, string[] longhands)
    {
        return SvgPropertySupportEntry.CreateMetadata(name, SvgPropertyValueSupportKind.Any, [], "", initialValue, inherited, grammar, SvgPropertyPercentageBasis.None, SvgPropertyAnimationType.NotAnimatable, PresentationAttributeName(name), true, false, longhands);
    }

    internal static string PresentationAttributeName(string name)
    {
        switch (name)
        {
            case SvgCssPropertyNames.Animation:
            case SvgCssPropertyNames.AnimationName:
            case SvgCssPropertyNames.AnimationDuration:
            case SvgCssPropertyNames.AnimationTimingFunction:
            case SvgCssPropertyNames.AnimationDelay:
            case SvgCssPropertyNames.AnimationIterationCount:
            case SvgCssPropertyNames.AnimationDirection:
            case SvgCssPropertyNames.AnimationFillMode:
            case SvgCssPropertyNames.AnimationPlayState:
            case SvgCssPropertyNames.FontFeatureSettings:
            case SvgCssPropertyNames.FontKerning:
            case SvgCssPropertyNames.FontVariationSettings:
            case SvgCssPropertyNames.MixBlendMode:
            case SvgCssPropertyNames.OverflowX:
            case SvgCssPropertyNames.OverflowY:
            case SvgCssPropertyNames.TextDecorationColor:
            case SvgCssPropertyNames.TextDecorationLine:
            case SvgCssPropertyNames.TextDecorationStyle:
            case SvgCssPropertyNames.TransformBox:
                return "";
            default:
                return name;
        }
    }

    internal static bool DeclarationSyntaxValid(string name, string value, SvgCssLimits limits)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseDeclarationListWithValueAndLimits(name + ":" + value + ";", limits);
        return parsed.Success && parsed.Diagnostics.Count == 0 && parsed.Declarations.Count == 1 && parsed.Declarations[0].IsValid && PropertyNameEquals(parsed.Declarations[0].Name, name);
    }

    internal static bool PropertyNameEquals(string left, string right)
    {
        if (left.StartsWith("--", System.StringComparison.Ordinal) || right.StartsWith("--", System.StringComparison.Ordinal)) return string.Equals(left, right, System.StringComparison.Ordinal);
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool CssWideKeyword(string value)
    {
        return string.Equals(value, "initial", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "inherit", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "unset", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "revert", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "revert-layer", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ContainsVariableFunction(string value)
    {
        return value.IndexOf("var(", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static bool ContainsKeyword(System.Collections.Generic.List<string> keywords, string value)
    {
        int index = 0;
        while (index < keywords.Count)
        {
            if (string.Equals(keywords[index], value, System.StringComparison.OrdinalIgnoreCase)) return true;
            index = index + 1;
        }
        return false;
    }
}
