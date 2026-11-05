namespace svg;

/// <summary>Provides lazy, version-cached typed access to attributes on tagged SVG nodes.</summary>
public class SvgTypedAttributeAccessor : ISvgTypedAttributeAccessor
{
    SvgElementCatalog elementCatalog;
    SvgGeometryLimits geometryLimits;
    int maxTokenCharacters;
    int maxListItems;
    int maxTransforms;
    System.Collections.Generic.List<SvgTypedAttributeCacheEntry> cache;
    long cacheHits;
    long cacheMisses;
    long parseCount;

    /// <summary>Creates an accessor with secure parser limits and the default element catalog.</summary>
    public SvgTypedAttributeAccessor()
    {
        SvgTypedAttributeOptions options = new SvgTypedAttributeOptions();
        elementCatalog = options.ElementCatalog;
        geometryLimits = options.GeometryLimits;
        maxTokenCharacters = options.MaxTokenCharacters;
        maxListItems = options.MaxListItems;
        maxTransforms = options.MaxTransforms;
        cache = new System.Collections.Generic.List<SvgTypedAttributeCacheEntry>();
        cacheHits = 0;
        cacheMisses = 0;
        parseCount = 0;
    }

    /// <summary>Creates an accessor from explicit bounded parser options.</summary>
    /// <param name="options">The element catalog and parser limits.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an option or required option member is invalid.</exception>
    /// <returns>A new typed attribute accessor.</returns>
    public SvgTypedAttributeAccessor Create(SvgTypedAttributeOptions options)
    {
        if (options == null || options.ElementCatalog == null || options.GeometryLimits == null || options.MaxTokenCharacters < 1 || options.MaxListItems < 1 || options.MaxTransforms < 1)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        if (options.GeometryLimits.MaxPathSegments < 1 || options.GeometryLimits.MaxPoints < 1)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
        SvgTypedAttributeAccessor result = new SvgTypedAttributeAccessor();
        result.elementCatalog = options.ElementCatalog;
        result.geometryLimits = options.GeometryLimits;
        result.maxTokenCharacters = options.MaxTokenCharacters;
        result.maxListItems = options.MaxListItems;
        result.maxTransforms = options.MaxTransforms;
        return result;
    }

    /// <summary>Gets an unnamespaced attribute value, parsing it only on a cache miss.</summary>
    /// <param name="node">The tagged SVG element.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>A source-preserving tagged typed value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an element or the name is empty.</exception>
    public SvgTypedAttributeValue Get(SvgNode node, string localName)
    {
        return GetNamespaced(node, "", localName);
    }

    /// <summary>Gets an attribute by expanded XML name, parsing it only on a cache miss.</summary>
    /// <param name="node">The tagged SVG element.</param>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>A source-preserving tagged typed value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an element or the name is empty.</exception>
    public SvgTypedAttributeValue GetNamespaced(SvgNode node, string namespaceUri, string localName)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (localName == null) throw new System.ArgumentNullException("localName");
        if (node.Kind != SvgNodeKind.Element || localName.Length == 0) throw new System.ArgumentOutOfRangeException("node");
        lock (cache)
        {
            return GetCore(node, namespaceUri, localName, 0);
        }
    }

    /// <summary>Clears every cached result while retaining cumulative statistics.</summary>
    public void Clear()
    {
        lock (cache)
        {
            cache.Clear();
        }
    }

    /// <summary>Returns a snapshot of cache and parsing activity.</summary>
    /// <returns>The current statistics snapshot.</returns>
    public SvgTypedAttributeStatistics GetStatistics()
    {
        lock (cache)
        {
            return new SvgTypedAttributeStatistics(cacheHits, cacheMisses, parseCount, cache.Count);
        }
    }

    SvgTypedAttributeValue GetCore(SvgNode node, string namespaceUri, string localName, int defaultDepth)
    {
        int index = 0;
        while (index < cache.Count)
        {
            SvgTypedAttributeCacheEntry entry = cache[index];
            if (object.ReferenceEquals(entry.Node, node) && entry.NamespaceUri == namespaceUri && entry.LocalName == localName)
            {
                if (entry.Version == node.Version)
                {
                    cacheHits = cacheHits + 1;
                    return entry.Result;
                }
                cache.RemoveAt(index);
                continue;
            }
            index = index + 1;
        }

        cacheMisses = cacheMisses + 1;
        SvgTypedAttributeValue result = Compute(node, namespaceUri, localName, defaultDepth);
        cache.Add(new SvgTypedAttributeCacheEntry(node, namespaceUri, localName, result));
        return result;
    }

    SvgTypedAttributeValue Compute(SvgNode node, string namespaceUri, string localName, int defaultDepth)
    {
        SvgTypedAttributeValue result = new SvgTypedAttributeValue(node, namespaceUri, localName);
        SvgElementDefinition? element = elementCatalog.Find(node.ElementKind);
        SvgElementAttributeDefinition? definition = element == null ? null : element.FindAttribute(namespaceUri, localName);
        SvgAttribute? attribute = FindAttribute(node, namespaceUri, localName);
        result.Definition = definition;
        result.Applicable = definition != null;
        result.Attribute = attribute;

        if (attribute != null)
        {
            result.SourceAttribute = attribute;
            result.OriginalText = GetOriginalText(attribute);
            result.EffectiveText = attribute.Value;
            result.Origin = SvgTypedAttributeOrigin.Authored;
            result.HasValue = true;
            return Parse(result);
        }

        if (definition == null || definition.DefaultKind == SvgAttributeDefaultKind.None)
        {
            if (definition != null && definition.Required)
            {
                result.Success = false;
                result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The required attribute '" + localName + "' is missing.", node.SourceSpan));
            }
            return result;
        }

        if (definition.DefaultKind == SvgAttributeDefaultKind.ContextDependent)
        {
            result.Origin = SvgTypedAttributeOrigin.ContextDependentDefault;
            result.ValueKind = SvgTypedAttributeKind.ContextDependent;
            return result;
        }

        if (definition.DefaultKind == SvgAttributeDefaultKind.Literal)
        {
            result.EffectiveText = definition.DefaultValue;
            result.Origin = SvgTypedAttributeOrigin.LiteralDefault;
            result.HasValue = true;
            return Parse(result);
        }

        if (defaultDepth >= 16)
        {
            result.Success = false;
            result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InternalInvariant, "The related attribute default chain is cyclic or too deep.", node.SourceSpan));
            return result;
        }

        SvgTypedAttributeValue related = GetCore(node, "", definition.DefaultValue, defaultDepth + 1);
        result.Origin = SvgTypedAttributeOrigin.OtherAttributeDefault;
        if (!related.Success)
        {
            result.Success = false;
            CopyDiagnostics(related.Diagnostics, result.Diagnostics);
            return result;
        }
        if (!related.HasValue)
        {
            result.ValueKind = related.ValueKind;
            return result;
        }
        result.SourceAttribute = related.SourceAttribute;
        result.EffectiveText = related.EffectiveText;
        result.HasValue = true;
        return Parse(result);
    }

    SvgTypedAttributeValue Parse(SvgTypedAttributeValue result)
    {
        parseCount = parseCount + 1;
        SvgTypedAttributeKind grammar = Grammar(result.Node, result.NamespaceUri, result.LocalName);
        string trimmed = result.EffectiveText.Trim();

        if (grammar == SvgTypedAttributeKind.Length && IsLengthKeyword(result.Node, result.LocalName, trimmed))
        {
            return Success(result, SvgTypedAttributeKind.Keyword, trimmed);
        }
        if (grammar == SvgTypedAttributeKind.Angle && IsAngleKeyword(result.Node, result.LocalName, trimmed))
        {
            return Success(result, SvgTypedAttributeKind.Keyword, trimmed);
        }
        if (grammar == SvgTypedAttributeKind.Text)
        {
            return Success(result, grammar, result.EffectiveText);
        }
        if (grammar == SvgTypedAttributeKind.Number)
        {
            SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(result.EffectiveText, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Value);
            return Failure(result, grammar, null, parsed.ErrorOffset, "number");
        }
        if (grammar == SvgTypedAttributeKind.Integer)
        {
            SvgIntegerParseResult parsed = SvgNumberParser.ParseInteger(result.EffectiveText, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Value);
            return Failure(result, grammar, null, parsed.ErrorOffset, "integer");
        }
        if (grammar == SvgTypedAttributeKind.Length)
        {
            SvgLengthParseResult parsed = SvgLengthParser.ParseLength(result.EffectiveText, true, true, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Length);
            return Failure(result, grammar, parsed.Length, parsed.ErrorOffset, "length");
        }
        if (grammar == SvgTypedAttributeKind.Angle)
        {
            SvgAngleParseResult parsed = SvgScalarUnitParser.ParseAngle(result.EffectiveText, true, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Angle);
            return Failure(result, grammar, parsed.Angle, parsed.ErrorOffset, "angle");
        }
        if (grammar == SvgTypedAttributeKind.PathLength)
        {
            SvgPathLengthParseResult parsed = SvgScalarUnitParser.ParsePathLength(result.EffectiveText, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.PathLength);
            return Failure(result, grammar, parsed.PathLength, parsed.ErrorOffset, "path length");
        }
        if (grammar == SvgTypedAttributeKind.LengthList)
        {
            SvgLengthListParseResult parsed = result.LocalName == "stroke-dasharray" ? new SvgLengthListParser().ParseDashArray(result.EffectiveText, maxListItems, maxTokenCharacters) : new SvgLengthListParser().ParseTextPosition(result.EffectiveText, maxListItems, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Value);
            return Failure(result, grammar, parsed.Value, parsed.ErrorOffset, "length list");
        }
        if (grammar == SvgTypedAttributeKind.AngleList)
        {
            SvgRotateListParseResult parsed = new SvgRotateListParser().Parse(result.EffectiveText, maxListItems, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Items);
            return Failure(result, grammar, parsed.Items, parsed.ErrorOffset, "angle list");
        }
        if (grammar == SvgTypedAttributeKind.NumberList)
        {
            SvgNumberListResult parsed = SvgListParser.ParseCoordinateList(result.EffectiveText, maxListItems, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Values);
            return Failure(result, grammar, parsed.Values, parsed.ErrorOffset, "number list");
        }
        if (grammar == SvgTypedAttributeKind.IntegerList)
        {
            SvgIntegerListResult parsed = SvgListParser.ParseIntegerList(result.EffectiveText, SvgListSeparatorMode.CommaOrWhitespace, maxListItems, maxTokenCharacters, false);
            if (parsed.Success) return Success(result, grammar, parsed.Values);
            return Failure(result, grammar, parsed.Values, parsed.ErrorOffset, "integer list");
        }
        if (grammar == SvgTypedAttributeKind.Points)
        {
            SvgPointListResult parsed = SvgListParser.ParsePoints(result.EffectiveText, geometryLimits, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Coordinates);
            return Failure(result, grammar, parsed.Coordinates, parsed.ErrorOffset, "points list");
        }
        if (grammar == SvgTypedAttributeKind.PathData)
        {
            SvgPathParseResult parsed = new SvgPathParser().Parse(result.EffectiveText, geometryLimits.MaxPathSegments, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.PathData);
            return Failure(result, grammar, parsed.PathData, parsed.ErrorOffset, "path data");
        }
        if (grammar == SvgTypedAttributeKind.TransformList)
        {
            SvgTransformParseResult parsed = SvgTransformParser.Parse(result.EffectiveText, maxTransforms, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.TransformList);
            return Failure(result, grammar, parsed.TransformList, parsed.ErrorOffset, "transform list");
        }
        if (grammar == SvgTypedAttributeKind.ViewBox)
        {
            SvgViewBoxParseResult parsed = SvgViewBoxParser.ParseViewBox(result.EffectiveText, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.ViewBox);
            return Failure(result, grammar, parsed.ViewBox, parsed.ErrorOffset, "viewBox");
        }
        if (grammar == SvgTypedAttributeKind.AspectRatio)
        {
            SvgAspectRatioParseResult parsed = SvgViewBoxParser.ParseAspectRatio(result.EffectiveText, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.AspectRatio);
            return Failure(result, grammar, parsed.AspectRatio, parsed.ErrorOffset, "preserveAspectRatio");
        }
        if (grammar == SvgTypedAttributeKind.Iri)
        {
            SvgIriParseResult parsed = new SvgIriParser().ParseIri(result.EffectiveText, maxTokenCharacters);
            if (parsed.Success) return Success(result, grammar, parsed.Reference);
            return Failure(result, grammar, parsed.Reference, parsed.ErrorOffset, "IRI");
        }
        return Success(result, SvgTypedAttributeKind.Text, result.EffectiveText);
    }

    SvgTypedAttributeValue Success(SvgTypedAttributeValue result, SvgTypedAttributeKind kind, object? value)
    {
        result.Success = true;
        result.ValueKind = kind;
        result.Value = value;
        result.ErrorOffset = -1;
        return result;
    }

    SvgTypedAttributeValue Failure(SvgTypedAttributeValue result, SvgTypedAttributeKind kind, object? value, int errorOffset, string expected)
    {
        result.Success = false;
        result.ValueKind = kind;
        result.Value = value;
        result.ErrorOffset = errorOffset;
        SvgSourceSpan? source = LocateError(result.SourceAttribute, errorOffset);
        if (source == null) source = result.Node.SourceSpan;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidValue, "The attribute '" + result.LocalName + "' is not a valid " + expected + ".", source));
        return result;
    }

    static SvgAttribute? FindAttribute(SvgNode node, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == namespaceUri && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }

    static string GetOriginalText(SvgAttribute attribute)
    {
        if (attribute.RawToken != null && attribute.RawToken.Value != null) return attribute.RawToken.Value;
        return attribute.Value;
    }

    static void CopyDiagnostics(System.Collections.Generic.List<SvgDiagnostic> source, System.Collections.Generic.List<SvgDiagnostic> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    static SvgTypedAttributeKind Grammar(SvgNode node, string namespaceUri, string localName)
    {
        if (localName == "href" && (namespaceUri.Length == 0 || namespaceUri == SvgNamespaces.XLink)) return SvgTypedAttributeKind.Iri;
        if (namespaceUri.Length != 0) return SvgTypedAttributeKind.Text;
        if (localName == "viewBox") return SvgTypedAttributeKind.ViewBox;
        if (localName == "preserveAspectRatio") return SvgTypedAttributeKind.AspectRatio;
        if (localName == "d" || localName == "path") return SvgTypedAttributeKind.PathData;
        if (localName == "points") return SvgTypedAttributeKind.Points;
        if (localName == "transform" || localName == "gradientTransform" || localName == "patternTransform") return SvgTypedAttributeKind.TransformList;
        if (localName == "pathLength") return SvgTypedAttributeKind.PathLength;
        if (IsTextPosition(node.ElementKind, localName)) return SvgTypedAttributeKind.LengthList;
        if ((node.ElementKind == SvgElementKind.Text || node.ElementKind == SvgElementKind.TSpan) && localName == "rotate") return SvgTypedAttributeKind.AngleList;
        if (localName == "stroke-dasharray") return SvgTypedAttributeKind.LengthList;
        if (localName == "orient" || localName == "azimuth" || localName == "elevation" || localName == "limitingConeAngle") return SvgTypedAttributeKind.Angle;
        if (localName == "numOctaves" || localName == "targetX" || localName == "targetY") return SvgTypedAttributeKind.Integer;
        if (localName == "order") return SvgTypedAttributeKind.IntegerList;
        if (localName == "baseFrequency" || localName == "kernelMatrix" || localName == "kernelUnitLength" || localName == "radius" || localName == "stdDeviation" || localName == "tableValues" || node.ElementKind == SvgElementKind.FeColorMatrix && localName == "values") return SvgTypedAttributeKind.NumberList;
        if (IsNumberName(node.ElementKind, localName)) return SvgTypedAttributeKind.Number;
        if (IsLengthName(localName)) return SvgTypedAttributeKind.Length;
        return SvgTypedAttributeKind.Text;
    }

    static bool IsTextPosition(SvgElementKind elementKind, string localName)
    {
        if (elementKind != SvgElementKind.Text && elementKind != SvgElementKind.TSpan) return false;
        return localName == "x" || localName == "y" || localName == "dx" || localName == "dy";
    }

    static bool IsNumberName(SvgElementKind elementKind, string localName)
    {
        if ((elementKind == SvgElementKind.FeFuncA || elementKind == SvgElementKind.FeFuncB || elementKind == SvgElementKind.FeFuncG || elementKind == SvgElementKind.FeFuncR) && (localName == "amplitude" || localName == "offset")) return true;
        if (elementKind == SvgElementKind.FeDistantLight || elementKind == SvgElementKind.FePointLight || elementKind == SvgElementKind.FeSpotLight)
        {
            if (localName == "x" || localName == "y" || localName == "z" || localName == "pointsAtX" || localName == "pointsAtY" || localName == "pointsAtZ") return true;
        }
        return localName == "bias" || localName == "diffuseConstant" || localName == "divisor" || localName == "exponent" || localName == "intercept" || localName == "k" || localName == "k1" || localName == "k2" || localName == "k3" || localName == "k4" || localName == "scale" || localName == "seed" || localName == "slope" || localName == "specularConstant" || localName == "specularExponent" || localName == "surfaceScale" || localName == "stroke-miterlimit" || localName == "opacity" || localName == "fill-opacity" || localName == "stroke-opacity" || localName == "flood-opacity" || localName == "stop-opacity";
    }

    static bool IsLengthName(string localName)
    {
        return localName == "x" || localName == "y" || localName == "x1" || localName == "y1" || localName == "x2" || localName == "y2" || localName == "cx" || localName == "cy" || localName == "r" || localName == "rx" || localName == "ry" || localName == "fx" || localName == "fy" || localName == "fr" || localName == "dx" || localName == "dy" || localName == "width" || localName == "height" || localName == "refX" || localName == "refY" || localName == "markerWidth" || localName == "markerHeight" || localName == "startOffset" || localName == "textLength" || localName == "offset" || localName == "stroke-width" || localName == "stroke-dashoffset" || localName == "font-size";
    }

    static bool IsLengthKeyword(SvgNode node, string localName, string value)
    {
        if (value != "auto") return false;
        if (localName == "width" || localName == "height") return true;
        return (node.ElementKind == SvgElementKind.Rect || node.ElementKind == SvgElementKind.Ellipse) && (localName == "rx" || localName == "ry");
    }

    static bool IsAngleKeyword(SvgNode node, string localName, string value)
    {
        if (node.ElementKind != SvgElementKind.Marker || localName != "orient") return false;
        return value == "auto" || value == "auto-start-reverse";
    }

    static SvgSourceSpan? LocateError(SvgAttribute? attribute, int errorOffset)
    {
        if (attribute == null || attribute.SourceSpan == null) return null;
        SvgXmlAttributeToken? token = attribute.RawToken;
        if (token == null || token.Value == null || token.Value != attribute.Value || errorOffset < 0) return attribute.SourceSpan;
        int quote = token.RawText.IndexOf(token.Quote);
        if (quote < 0) return attribute.SourceSpan;
        int bounded = errorOffset;
        if (bounded > token.Value.Length) bounded = token.Value.Length;
        int relative = quote + 1 + bounded;
        int startLine = token.StartLine;
        int startColumn = token.StartColumn;
        int index = 0;
        while (index < relative && index < token.RawText.Length)
        {
            if (token.RawText[index] == '\n')
            {
                startLine = startLine + 1;
                startColumn = 1;
            }
            else startColumn = startColumn + 1;
            index = index + 1;
        }
        int endLine = startLine;
        int endColumn = startColumn;
        int width = bounded < token.Value.Length ? 1 : 0;
        if (width == 1 && relative < token.RawText.Length && token.RawText[relative] == '\n')
        {
            endLine = endLine + 1;
            endColumn = 1;
        }
        else endColumn = endColumn + width;
        long startOffset = token.StartOffset + relative;
        return new SvgSourceSpan(attribute.SourceSpan.Source, startOffset, startLine, startColumn, startOffset + width, endLine, endColumn);
    }
}
