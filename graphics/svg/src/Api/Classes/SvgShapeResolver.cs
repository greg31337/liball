namespace svg;

/// <summary>Resolves SVG shape attributes and geometry properties into bounded used geometry.</summary>
public class SvgShapeResolver : ISvgShapeResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgShapeResolver()
    {
    }

    /// <summary>Resolves one supported SVG shape using secure defaults.</summary>
    /// <param name="node">The circle, ellipse, line, path, polygon, polyline, or rectangle.</param>
    /// <returns>The resolved shape geometry.</returns>
    public SvgShapeResult ResolveWithNode(SvgNode node)
    {
        return new SvgShapeResolver().ResolveWithNodeAndOptions(node, new SvgShapeResolveOptions());
    }

    /// <summary>Resolves one supported SVG shape using explicit style, length, and security context.</summary>
    /// <param name="node">The circle, ellipse, line, path, polygon, polyline, or rectangle.</param>
    /// <param name="options">The computed style, length context, parsers, and bounds.</param>
    /// <returns>The resolved shape geometry or a located failure.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not a supported SVG shape.</exception>
    public SvgShapeResult ResolveWithNodeAndOptions(SvgNode node, SvgShapeResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        SvgShapeKind kind = new SvgShapeResolver().GetKind(node);
        if (node.Kind != SvgNodeKind.Element || kind == SvgShapeKind.None) throw new System.ArgumentOutOfRangeException("node");
        SvgShapeResult result = new SvgShapeResult(node);
        result.Kind = kind;
        if (!new SvgShapeResolver().ValidOptions(options))
        {
            new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Shape resolution options are invalid.", node.SourceSpan);
            return result;
        }

        result.UsesComputedGeometry = options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node);
        result.Displayed = options.AncestorRendered && new SvgShapeResolver().IsDisplayed(node, options);
        result.Visible = new SvgShapeResolver().IsVisible(node, options);
        if (kind == SvgShapeKind.Circle) new SvgShapeResolver().ResolveCircle(node, options, result);
        else if (kind == SvgShapeKind.Ellipse) new SvgShapeResolver().ResolveEllipse(node, options, result);
        else if (kind == SvgShapeKind.Line) new SvgShapeResolver().ResolveLine(node, options, result);
        else if (kind == SvgShapeKind.Path) new SvgShapeResolver().ResolvePath(node, options, result);
        else if (kind == SvgShapeKind.Polygon || kind == SvgShapeKind.Polyline) new SvgShapeResolver().ResolvePoints(node, options, result);
        else new SvgShapeResolver().ResolveRectangle(node, options, result);
        new SvgShapeResolver().ReadPathLength(node, options, result);
        result.Renderable = result.HasGeometry && result.Displayed && result.Visible;
        result.Success = !new SvgShapeResolver().HasError(result.Diagnostics);
        return result;
    }
bool ValidOptions(SvgShapeResolveOptions options)
    {
        if (options.Attributes == null || options.LengthContext == null || options.GeometryLimits == null) return false;
        if (options.GeometryLimits.MaxPathSegments < 1 || options.GeometryLimits.MaxPoints < 1) return false;
        if (options.MaxTokenCharacters < 1 || options.MaxCssPathCharacters < 1 || options.MaxCssPathTokens < 1) return false;
        return new SvgShapeResolver().ValidLengthContext(options.LengthContext);
    }
bool ValidLengthContext(SvgLengthContext context)
    {
        if (context.HasViewport && (!SvgFiniteMath.IsFinite(context.ViewportWidth) || !SvgFiniteMath.IsFinite(context.ViewportHeight) || context.ViewportWidth < 0 || context.ViewportHeight < 0)) return false;
        if (context.HasFontSize && (!SvgFiniteMath.IsFinite(context.FontSize) || context.FontSize < 0)) return false;
        if (context.HasXHeight && (!SvgFiniteMath.IsFinite(context.XHeight) || context.XHeight < 0)) return false;
        if (context.HasZeroAdvance && (!SvgFiniteMath.IsFinite(context.ZeroAdvance) || context.ZeroAdvance < 0)) return false;
        if (context.HasRootFontSize && (!SvgFiniteMath.IsFinite(context.RootFontSize) || context.RootFontSize < 0)) return false;
        return true;
    }
SvgShapeKind GetKind(SvgNode? node)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return SvgShapeKind.None;
        switch (node.ElementKind)
        {
            case SvgElementKind.Circle:
                return SvgShapeKind.Circle;
            case SvgElementKind.Ellipse:
                return SvgShapeKind.Ellipse;
            case SvgElementKind.Line:
                return SvgShapeKind.Line;
            case SvgElementKind.Path:
                return SvgShapeKind.Path;
            case SvgElementKind.Polygon:
                return SvgShapeKind.Polygon;
            case SvgElementKind.Polyline:
                return SvgShapeKind.Polyline;
            case SvgElementKind.Rect:
                return SvgShapeKind.Rectangle;
            default:
                return SvgShapeKind.None;
        }
    }
void ResolveCircle(SvgNode node, SvgShapeResolveOptions options, SvgShapeResult result)
    {
        SvgShapeLengthValue cx = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Cx, SvgLengthPercentageBasis.ViewportWidth, false, options, result);
        SvgShapeLengthValue cy = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Cy, SvgLengthPercentageBasis.ViewportHeight, false, options, result);
        SvgShapeLengthValue radius = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.R, SvgLengthPercentageBasis.ViewportDiagonal, false, options, result);
        if (!cx.Success || !cy.Success || !radius.Success) return;
        result.Cx = cx.Value;
        result.Cy = cy.Value;
        result.R = radius.Value;
        result.Closed = true;
        if (radius.Value < 0)
        {
            new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A circle radius cannot be negative.", radius.Source == null ? node.SourceSpan : radius.Source);
            return;
        }
        result.HasGeometry = radius.Value > 0;
    }
void ResolveEllipse(SvgNode node, SvgShapeResolveOptions options, SvgShapeResult result)
    {
        SvgShapeLengthValue cx = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Cx, SvgLengthPercentageBasis.ViewportWidth, false, options, result);
        SvgShapeLengthValue cy = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Cy, SvgLengthPercentageBasis.ViewportHeight, false, options, result);
        SvgShapeLengthValue rx = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Rx, SvgLengthPercentageBasis.ViewportWidth, true, options, result);
        SvgShapeLengthValue ry = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Ry, SvgLengthPercentageBasis.ViewportHeight, true, options, result);
        if (!cx.Success || !cy.Success || !rx.Success || !ry.Success) return;
        result.Cx = cx.Value;
        result.Cy = cy.Value;
        if (!rx.Auto && rx.Value < 0 || !ry.Auto && ry.Value < 0)
        {
            SvgSourceSpan? source = !rx.Auto && rx.Value < 0 ? rx.Source : ry.Source;
            new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "An ellipse radius cannot be negative.", source == null ? node.SourceSpan : source);
            return;
        }
        double usedRx = rx.Auto ? ry.Auto ? 0 : ry.Value : rx.Value;
        double usedRy = ry.Auto ? rx.Auto ? 0 : rx.Value : ry.Value;
        result.Rx = usedRx;
        result.Ry = usedRy;
        result.Closed = true;
        result.HasGeometry = usedRx > 0 && usedRy > 0;
    }
void ResolveLine(SvgNode node, SvgShapeResolveOptions options, SvgShapeResult result)
    {
        SvgShapeLengthValue x1 = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.X1, SvgLengthPercentageBasis.ViewportWidth, false, options, result);
        SvgShapeLengthValue y1 = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Y1, SvgLengthPercentageBasis.ViewportHeight, false, options, result);
        SvgShapeLengthValue x2 = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.X2, SvgLengthPercentageBasis.ViewportWidth, false, options, result);
        SvgShapeLengthValue y2 = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Y2, SvgLengthPercentageBasis.ViewportHeight, false, options, result);
        if (!x1.Success || !y1.Success || !x2.Success || !y2.Success) return;
        result.X1 = x1.Value;
        result.Y1 = y1.Value;
        result.X2 = x2.Value;
        result.Y2 = y2.Value;
        result.HasGeometry = true;
    }
void ResolveRectangle(SvgNode node, SvgShapeResolveOptions options, SvgShapeResult result)
    {
        SvgShapeLengthValue x = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.X, SvgLengthPercentageBasis.ViewportWidth, false, options, result);
        SvgShapeLengthValue y = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Y, SvgLengthPercentageBasis.ViewportHeight, false, options, result);
        SvgShapeLengthValue width = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Width, SvgLengthPercentageBasis.ViewportWidth, true, options, result);
        SvgShapeLengthValue height = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Height, SvgLengthPercentageBasis.ViewportHeight, true, options, result);
        SvgShapeLengthValue rx = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Rx, SvgLengthPercentageBasis.ViewportWidth, true, options, result);
        SvgShapeLengthValue ry = new SvgShapeResolver().ReadLength(node, SvgAttributeNames.Ry, SvgLengthPercentageBasis.ViewportHeight, true, options, result);
        if (!x.Success || !y.Success || !width.Success || !height.Success || !rx.Success || !ry.Success) return;
        result.X = x.Value;
        result.Y = y.Value;
        result.Width = width.Auto ? 0 : width.Value;
        result.Height = height.Auto ? 0 : height.Value;
        result.Closed = true;
        if (result.Width < 0 || result.Height < 0)
        {
            SvgSourceSpan? dimensionSource = result.Width < 0 ? width.Source : height.Source;
            new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A rectangle width or height cannot be negative.", dimensionSource == null ? node.SourceSpan : dimensionSource);
            return;
        }
        if (!rx.Auto && rx.Value < 0 || !ry.Auto && ry.Value < 0)
        {
            SvgSourceSpan? radiusSource = !rx.Auto && rx.Value < 0 ? rx.Source : ry.Source;
            new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A rectangle corner radius cannot be negative.", radiusSource == null ? node.SourceSpan : radiusSource);
            return;
        }
        double usedRx = rx.Auto ? ry.Auto ? 0 : ry.Value : rx.Value;
        double usedRy = ry.Auto ? rx.Auto ? 0 : rx.Value : ry.Value;
        double halfWidth = result.Width / 2;
        double halfHeight = result.Height / 2;
        result.Rx = usedRx > halfWidth ? halfWidth : usedRx;
        result.Ry = usedRy > halfHeight ? halfHeight : usedRy;
        result.HasGeometry = result.Width > 0 && result.Height > 0;
    }
void ResolvePoints(SvgNode node, SvgShapeResolveOptions options, SvgShapeResult result)
    {
        SvgAttribute? attribute = new SvgShapeResolver().FindAttribute(node, "", SvgAttributeNames.Points);
        string text = attribute == null ? "" : attribute.Value;
        SvgPointListResult parsed = SvgListParser.ParsePoints(text, options.GeometryLimits, options.MaxTokenCharacters);
        int coordinateCount = parsed.PointCount * 2;
        result.Points = new double[coordinateCount];
        int index = 0;
        while (index < coordinateCount)
        {
            result.Points[index] = parsed.Coordinates[index];
            index = index + 1;
        }
        result.PointCount = parsed.PointCount;
        result.Closed = result.Kind == SvgShapeKind.Polygon;
        result.HasGeometry = parsed.PointCount >= 2;
        if (!parsed.Success)
        {
            SvgDiagnosticCode code = parsed.Error == SvgListParseError.ItemLimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgShapeResolver().AddError(result, code, "The shape points list is invalid; its complete point prefix was retained.", attribute == null ? node.SourceSpan : attribute.SourceSpan);
        }
    }
void ResolvePath(SvgNode node, SvgShapeResolveOptions options, SvgShapeResult result)
    {
        string text = "";
        SvgSourceSpan? source = node.SourceSpan;
        bool none = false;
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            SvgComputedValue? computed = options.ComputedStyle.Find(SvgCssPropertyNames.D);
            if (computed != null)
            {
                source = computed.Source == null ? node.SourceSpan : computed.Source;
                string value = computed.CssText.Trim();
                if (string.Equals(value, "none", System.StringComparison.OrdinalIgnoreCase)) none = true;
                else if (new SvgShapeResolver().ComputedPathIsPresentation(options.ComputedStyle)) text = value;
                else
                {
                    string? decoded = new SvgShapeResolver().DecodeCssPath(value, options);
                    if (decoded == null)
                    {
                        new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The computed d property is not a valid path string.", source);
                        return;
                    }
                    text = decoded;
                }
            }
        }
        else
        {
            SvgAttribute? attribute = new SvgShapeResolver().FindAttribute(node, "", SvgAttributeNames.D);
            if (attribute != null)
            {
                text = attribute.Value;
                source = attribute.SourceSpan;
            }
        }
        if (none)
        {
            result.PathData = null;
            return;
        }
        SvgPathParseResult parsed = new SvgPathParser().Parse(text, options.GeometryLimits.MaxPathSegments, options.MaxTokenCharacters);
        result.PathData = parsed.PathData;
        result.HasGeometry = new SvgShapeResolver().HasDrawingCommand(parsed.PathData);
        if (!parsed.Success)
        {
            SvgDiagnosticCode code = parsed.Error == SvgPathParseError.SegmentLimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgShapeResolver().AddError(result, code, "The shape path data is invalid; its valid command recovery was retained.", source);
        }
    }
bool ComputedPathIsPresentation(SvgComputedStyle style)
    {
        if (style.Cascade == null) return false;
        SvgCascadedValue? value = style.Cascade.Find(SvgCssPropertyNames.D);
        return value != null && value.Winner != null && value.Winner.SourceKind == SvgCascadeSourceKind.PresentationAttribute;
    }
string? DecodeCssPath(string text, SvgShapeResolveOptions options)
    {
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(text, options.MaxCssPathTokens, options.MaxCssPathCharacters);
        if (!tokenized.Success || tokenized.ParseErrorCount != 0) return null;
        System.Collections.Generic.List<SvgCssToken> tokens = new System.Collections.Generic.List<SvgCssToken>();
        int index = 0;
        while (index < tokenized.Tokens.Count)
        {
            SvgCssToken token = tokenized.Tokens[index];
            if (token.Kind != SvgCssTokenKind.Whitespace && token.Kind != SvgCssTokenKind.Comment) tokens.Add(token);
            index = index + 1;
        }
        if (tokens.Count != 3) return null;
        if (tokens[0].Kind != SvgCssTokenKind.Function || !string.Equals(tokens[0].Value, "path", System.StringComparison.OrdinalIgnoreCase)) return null;
        if (tokens[1].Kind != SvgCssTokenKind.String || tokens[2].Kind != SvgCssTokenKind.CloseParenthesis) return null;
        return tokens[1].Value;
    }
bool HasDrawingCommand(SvgPathData? path)
    {
        if (path == null || path.Commands == null) return false;
        int index = 0;
        while (index < path.Commands.Count)
        {
            SvgPathCommandKind kind = path.Commands[index].Kind;
            if (kind != SvgPathCommandKind.MoveTo && kind != SvgPathCommandKind.ClosePath && kind != SvgPathCommandKind.Unknown) return true;
            index = index + 1;
        }
        return false;
    }
SvgShapeLengthValue ReadLength(SvgNode node, string name, SvgLengthPercentageBasis basis, bool allowAuto, SvgShapeResolveOptions options, SvgShapeResult result)
    {
        SvgLength? length = null;
        SvgSourceSpan? source = node.SourceSpan;
        if (new SvgShapeResolver().GeometryProperty(name) && options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            SvgComputedValue? computed = options.ComputedStyle.Find(name);
            if (computed == null)
            {
                new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The computed geometry property '" + name + "' is missing.", node.SourceSpan);
                return new SvgShapeLengthValue(false, false, 0, node.SourceSpan);
            }
            source = computed.Source == null ? node.SourceSpan : computed.Source;
            if (computed.Kind == SvgComputedValueKind.Keyword && allowAuto && string.Equals(computed.Keyword, "auto", System.StringComparison.OrdinalIgnoreCase)) return new SvgShapeLengthValue(true, true, 0, source);
            if (computed.Kind != SvgComputedValueKind.Length || computed.Length == null)
            {
                new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The computed geometry property '" + name + "' is not a length.", source);
                return new SvgShapeLengthValue(false, false, 0, source);
            }
            length = computed.Length;
        }
        else
        {
            SvgTypedAttributeValue value = options.Attributes.Get(node, name);
            new SvgShapeResolver().AppendTyped(value, result);
            source = value.SourceAttribute == null ? node.SourceSpan : value.SourceAttribute.SourceSpan;
            if (!value.Success || !value.HasValue) return new SvgShapeLengthValue(false, false, 0, source);
            if (value.ValueKind == SvgTypedAttributeKind.Keyword && allowAuto && string.Equals((string?)value.Value, "auto", System.StringComparison.OrdinalIgnoreCase)) return new SvgShapeLengthValue(true, true, 0, source);
            if (value.ValueKind != SvgTypedAttributeKind.Length || value.Value == null)
            {
                new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The shape attribute '" + name + "' is not a length.", source);
                return new SvgShapeLengthValue(false, false, 0, source);
            }
            length = (SvgLength)value.Value;
        }
        SvgLengthEvaluationResult evaluated = length.Evaluate(options.LengthContext, basis);
        if (!evaluated.Success)
        {
            new SvgShapeResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The shape length '" + name + "' could not be resolved in the supplied context.", source);
            return new SvgShapeLengthValue(false, false, 0, source);
        }
        return new SvgShapeLengthValue(true, false, evaluated.Value, source);
    }
bool GeometryProperty(string name)
    {
        return name == SvgAttributeNames.Cx || name == SvgAttributeNames.Cy || name == SvgAttributeNames.R || name == SvgAttributeNames.Rx || name == SvgAttributeNames.Ry || name == SvgAttributeNames.X || name == SvgAttributeNames.Y || name == SvgAttributeNames.Width || name == SvgAttributeNames.Height;
    }
void ReadPathLength(SvgNode node, SvgShapeResolveOptions options, SvgShapeResult result)
    {
        SvgAttribute? attribute = new SvgShapeResolver().FindAttribute(node, "", SvgAttributeNames.PathLength);
        if (attribute == null) return;
        result.HasPathLength = true;
        SvgTypedAttributeValue value = options.Attributes.Get(node, SvgAttributeNames.PathLength);
        new SvgShapeResolver().AppendTyped(value, result);
        if (value.ValueKind == SvgTypedAttributeKind.PathLength && value.Value != null) result.PathLength = (SvgPathLength)value.Value;
    }
bool IsDisplayed(SvgNode node, SvgShapeResolveOptions options)
    {
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            string computed = options.ComputedStyle.GetKeyword(SvgCssPropertyNames.Display);
            if (computed.Length != 0) return !string.Equals(computed, "none", System.StringComparison.OrdinalIgnoreCase);
        }
        SvgAttribute? attribute = new SvgShapeResolver().FindAttribute(node, "", SvgCssPropertyNames.Display);
        return attribute == null || !string.Equals(attribute.Value.Trim(), "none", System.StringComparison.OrdinalIgnoreCase);
    }
bool IsVisible(SvgNode node, SvgShapeResolveOptions options)
    {
        string value = "visible";
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            string computed = options.ComputedStyle.GetKeyword(SvgCssPropertyNames.Visibility);
            if (computed.Length != 0) value = computed;
        }
        else
        {
            SvgAttribute? attribute = new SvgShapeResolver().FindAttribute(node, "", SvgCssPropertyNames.Visibility);
            if (attribute != null) value = attribute.Value.Trim();
        }
        return !string.Equals(value, "hidden", System.StringComparison.OrdinalIgnoreCase) && !string.Equals(value, "collapse", System.StringComparison.OrdinalIgnoreCase);
    }
SvgAttribute? FindAttribute(SvgNode node, string namespaceUri, string localName)
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
void AppendTyped(SvgTypedAttributeValue value, SvgShapeResult result)
    {
        int index = 0;
        while (index < value.Diagnostics.Count)
        {
            result.Diagnostics.Add(value.Diagnostics[index]);
            index = index + 1;
        }
    }
bool HasError(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }
void AddError(SvgShapeResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
