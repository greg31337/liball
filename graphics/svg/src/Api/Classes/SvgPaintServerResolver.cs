namespace svg;

/// <summary>Resolves SVG linear gradients, radial gradients, and patterns without rendering them.</summary>
public class SvgPaintServerResolver : ISvgPaintServerResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPaintServerResolver()
    {
    }

    /// <summary>Resolves one gradient or pattern using deterministic defaults.</summary>
    /// <param name="node">The paint-server element.</param>
    /// <returns>The resolved template, geometry, and content.</returns>
    public SvgPaintServerResult ResolveWithNode(SvgNode node)
    {
        return new SvgPaintServerResolver().ResolveWithNodeAndOptions(node, new SvgPaintServerResolveOptions());
    }

    /// <summary>Resolves one gradient or pattern with explicit target geometry, styles, references, and limits.</summary>
    /// <param name="node">The paint-server element.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The resolved template, geometry, and content.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not a supported paint server.</exception>
    public SvgPaintServerResult ResolveWithNodeAndOptions(SvgNode node, SvgPaintServerResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        SvgPaintServerKind kind = new SvgPaintServerResolver().Kind(node);
        if (node.Kind != SvgNodeKind.Element || kind == SvgPaintServerKind.None) throw new System.ArgumentOutOfRangeException("node");
        SvgPaintServerResult result = new SvgPaintServerResult(node);
        result.Kind = kind;
        if (!new SvgPaintServerResolver().ValidOptions(options))
        {
            new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Paint-server resolution options are invalid.", node.SourceSpan);
            return result;
        }
        result.ComputedStyle = new SvgPaintServerResolver().RootStyle(node, options);
        new SvgPaintServerResolver().AppendStyleDiagnostics(result, result.ComputedStyle);
        string[] names = new SvgPaintServerResolver().AttributeNames(kind);
        new SvgPaintServerResolver().ResolveTemplates(node, kind, names, options, result);
        new SvgPaintServerResolver().AddDefaults(kind, result);
        if (kind == SvgPaintServerKind.Pattern) new SvgPaintServerResolver().ResolvePattern(options, result);
        else new SvgPaintServerResolver().ResolveGradient(options, result);
        result.Success = !new SvgPaintServerResolver().HasError(result.Diagnostics);
        return result;
    }
void ResolveTemplates(SvgNode node, SvgPaintServerKind kind, string[] names, SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        System.Collections.Generic.List<SvgNode> active = new System.Collections.Generic.List<SvgNode>();
        SvgNode current = node;
        int references = 0;
        while (current != null)
        {
            result.TemplateChain.Add(current);
            result.TemplateVersions.Add(current.Version);
            active.Add(current);
            new SvgPaintServerResolver().CollectAttributes(current, node, names, options, result);
            if (result.ContentSource == null) new SvgPaintServerResolver().CollectContent(current, kind, options, result);
            if (new SvgPaintServerResolver().AllAttributes(names, result) && result.ContentSource != null) break;
            SvgAttribute? href = new SvgPaintServerResolver().FindHref(current);
            if (href == null) break;
            if (result.TemplateChain.Count >= options.Limits.MaxPaintServerDepth)
            {
                new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Paint-server template resolution exceeded MaxPaintServerDepth.", href.SourceSpan);
                break;
            }
            if (references >= options.Limits.MaxCount)
            {
                new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Paint-server template resolution exceeded reference MaxCount.", href.SourceSpan);
                break;
            }
            references = references + 1;
            SvgReferenceResolveOptions referenceOptions = new SvgPaintServerResolver().CopyReferenceOptions(options.ReferenceOptions);
            SvgReferenceResult reference = new SvgReferenceResolver().ResolveWithNodeAndOptions(current, referenceOptions);
            result.References.Add(reference);
            new SvgPaintServerResolver().AppendReferenceDiagnostics(result, reference);
            if (!reference.Success || reference.Status != SvgReferenceStatus.Resolved || reference.Target == null)
            {
                if (!new SvgPaintServerResolver().HasError(reference.Diagnostics)) new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "The paint-server template reference could not be resolved.", reference.ReferenceSource);
                break;
            }
            if (!new SvgPaintServerResolver().Compatible(kind, reference.Target))
            {
                new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "The paint-server template target has an incompatible element kind.", reference.TargetSource);
                break;
            }
            if (new SvgPaintServerResolver().Contains(active, reference.Target))
            {
                SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ReferenceCycle, "The paint-server template chain contains a reference cycle.", reference.ReferenceSource);
                if (reference.TargetSource != null) diagnostic.RelatedSpans.Add(reference.TargetSource);
                result.Diagnostics.Add(diagnostic);
                break;
            }
            current = reference.Target;
        }
    }
void CollectAttributes(SvgNode source, SvgNode root, string[] names, SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        int index = 0;
        while (index < names.Length)
        {
            string name = names[index];
            if (result.FindAttribute(name) == null)
            {
                SvgAttribute? attribute = new SvgPaintServerResolver().FindAttribute(source, name);
                if (attribute != null)
                {
                    SvgTypedAttributeValue typed = options.Attributes.Get(source, name);
                    new SvgPaintServerResolver().AppendTypedDiagnostics(result, typed);
                    if (typed.Success && typed.HasValue && typed.Applicable) result.Attributes.Add(new SvgPaintServerAttribute(name, source, attribute, typed.Value, !object.ReferenceEquals(source, root), false));
                }
            }
            index = index + 1;
        }
    }
void CollectContent(SvgNode source, SvgPaintServerKind kind, SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        System.Collections.Generic.List<SvgNode> content = new SvgPaintServerResolver().RelevantChildren(source, kind);
        if (content.Count == 0) return;
        if (kind != SvgPaintServerKind.Pattern && content.Count > options.MaxStops)
        {
            new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Gradient content exceeded MaxStops.", content[options.MaxStops].SourceSpan);
            return;
        }
        result.ContentSource = source;
        int index = 0;
        while (index < content.Count)
        {
            result.Content.Add(content[index]);
            index = index + 1;
        }
    }
void AddDefaults(SvgPaintServerKind kind, SvgPaintServerResult result)
    {
        if (kind == SvgPaintServerKind.LinearGradient)
        {
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.GradientUnits, "objectBoundingBox");
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.GradientTransform, new SvgPaintServerResolver().EmptyTransform());
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.SpreadMethod, "pad");
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.X1, new SvgLength(0, SvgLengthUnit.Percentage));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.Y1, new SvgLength(0, SvgLengthUnit.Percentage));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.X2, new SvgLength(100, SvgLengthUnit.Percentage));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.Y2, new SvgLength(0, SvgLengthUnit.Percentage));
        }
        else if (kind == SvgPaintServerKind.RadialGradient)
        {
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.GradientUnits, "objectBoundingBox");
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.GradientTransform, new SvgPaintServerResolver().EmptyTransform());
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.SpreadMethod, "pad");
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.Cx, new SvgLength(50, SvgLengthUnit.Percentage));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.Cy, new SvgLength(50, SvgLengthUnit.Percentage));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.R, new SvgLength(50, SvgLengthUnit.Percentage));
            SvgPaintServerAttribute? cx = result.FindAttribute(SvgAttributeNames.Cx);
            SvgPaintServerAttribute? cy = result.FindAttribute(SvgAttributeNames.Cy);
            if (result.FindAttribute(SvgAttributeNames.Fx) == null) result.Attributes.Add(new SvgPaintServerAttribute(SvgAttributeNames.Fx, (cx ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceNode, cx.Attribute, cx.Value, cx.Inherited, true));
            if (result.FindAttribute(SvgAttributeNames.Fy) == null) result.Attributes.Add(new SvgPaintServerAttribute(SvgAttributeNames.Fy, (cy ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceNode, cy.Attribute, cy.Value, cy.Inherited, true));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.Fr, new SvgLength(0, SvgLengthUnit.Percentage));
        }
        else
        {
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.PatternUnits, "objectBoundingBox");
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.PatternContentUnits, "userSpaceOnUse");
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.PatternTransform, new SvgPaintServerResolver().EmptyTransform());
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.X, new SvgLength(0, SvgLengthUnit.Number));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.Y, new SvgLength(0, SvgLengthUnit.Number));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.Width, new SvgLength(0, SvgLengthUnit.Number));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.Height, new SvgLength(0, SvgLengthUnit.Number));
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.ViewBox, null);
            new SvgPaintServerResolver().AddDefault(result, SvgAttributeNames.PreserveAspectRatio, new SvgAspectRatio(SvgAspectRatioAlignment.XMidYMid, SvgAspectRatioScale.Meet, false));
        }
    }
void ResolveGradient(SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        result.GradientUnits = new SvgPaintServerResolver().ParseUnits(result.FindAttribute(SvgAttributeNames.GradientUnits), result);
        result.SpreadMethod = new SvgPaintServerResolver().ParseSpread(result.FindAttribute(SvgAttributeNames.SpreadMethod), result);
        result.ExplicitTransform = (new SvgPaintServerResolver().Transform(result.FindAttribute(SvgAttributeNames.GradientTransform), result) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgMatrix baseMatrix = new SvgPaintServerResolver().UnitMatrix(result.GradientUnits, options, result);
        result.Transform = (new SvgPaintServerResolver().Multiply(baseMatrix, result.ExplicitTransform, result, (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        if (result.Kind == SvgPaintServerKind.LinearGradient) new SvgPaintServerResolver().ResolveLinear(options, result);
        else new SvgPaintServerResolver().ResolveRadial(options, result);
        new SvgPaintServerResolver().ResolveStops(options, result);
        if (result.Stops.Count == 0)
        {
            result.GradientPaintKind = SvgGradientPaintKind.None;
            result.Renderable = false;
        }
        else if (result.Stops.Count == 1 || new SvgPaintServerResolver().LinearDegenerate(result) || new SvgPaintServerResolver().RadialDegenerate(result))
        {
            SvgGradientStop last = result.Stops[result.Stops.Count - 1];
            result.GradientPaintKind = SvgGradientPaintKind.Solid;
            result.SolidColor = last.Color;
            result.SolidAlpha = last.EffectiveAlpha;
            result.Renderable = !new SvgPaintServerResolver().HasError(result.Diagnostics);
        }
        else
        {
            result.GradientPaintKind = SvgGradientPaintKind.Gradient;
            result.Renderable = !new SvgPaintServerResolver().HasError(result.Diagnostics) && new SvgPaintServerResolver().BoundingBoxUsable(result.GradientUnits, options);
        }
    }
void ResolveLinear(SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        double x1 = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.X1), true, result.GradientUnits, options, result);
        double y1 = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Y1), false, result.GradientUnits, options, result);
        double x2 = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.X2), true, result.GradientUnits, options, result);
        double y2 = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Y2), false, result.GradientUnits, options, result);
        result.Start = new SvgPoint(x1, y1);
        result.End = new SvgPoint(x2, y2);
        result.UserStart = new SvgPaintServerResolver().TransformPoint(result.Transform, result.Start, result, (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
        result.UserEnd = new SvgPaintServerResolver().TransformPoint(result.Transform, result.End, result, result.Node.SourceSpan);
    }
void ResolveRadial(SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        double cx = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Cx), true, result.GradientUnits, options, result);
        double cy = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Cy), false, result.GradientUnits, options, result);
        double fx = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Fx), true, result.GradientUnits, options, result);
        double fy = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Fy), false, result.GradientUnits, options, result);
        double radius = new SvgPaintServerResolver().Radius(result.FindAttribute(SvgAttributeNames.R), result.GradientUnits, options, result);
        double focalRadius = new SvgPaintServerResolver().Radius(result.FindAttribute(SvgAttributeNames.Fr), result.GradientUnits, options, result);
        if (radius < 0 || focalRadius < 0) new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Radial gradient radii cannot be negative.", radius < 0 ? new SvgPaintServerResolver().Source(result.FindAttribute(SvgAttributeNames.R)) : new SvgPaintServerResolver().Source(result.FindAttribute(SvgAttributeNames.Fr)));
        result.Center = new SvgPoint(cx, cy);
        result.Focus = new SvgPoint(fx, fy);
        result.Radius = radius;
        result.FocalRadius = focalRadius;
        result.UserCenter = new SvgPaintServerResolver().TransformPoint(result.Transform, result.Center, result, (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
        result.UserFocus = new SvgPaintServerResolver().TransformPoint(result.Transform, result.Focus, result, result.Node.SourceSpan);
    }
void ResolveStops(SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        double previous = 0;
        int index = 0;
        while (index < result.Content.Count)
        {
            SvgNode source = result.Content[index];
            SvgGradientStop stop = new SvgGradientStop(source);
            SvgTypedAttributeValue offset = options.Attributes.Get(source, SvgAttributeNames.Offset);
            new SvgPaintServerResolver().AppendTypedDiagnostics(result, offset);
            if (offset.SourceAttribute != null) stop.Source = offset.SourceAttribute.SourceSpan;
            double authored = new SvgPaintServerResolver().StopOffset(offset, result);
            stop.AuthoredOffset = authored;
            double normalized = authored;
            if (normalized < 0) normalized = 0;
            if (normalized > 1) normalized = 1;
            if (normalized < previous) normalized = previous;
            stop.Offset = normalized;
            previous = normalized;
            stop.ComputedStyle = new SvgPaintServerResolver().StopStyle(source, (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), result.ComputedStyle, options);
            new SvgPaintServerResolver().AppendStyleDiagnostics(result, stop.ComputedStyle);
            stop.Color = stop.ComputedStyle == null ? null : stop.ComputedStyle.GetColor(SvgCssPropertyNames.StopColor);
            SvgComputedNumberResult? opacity = stop.ComputedStyle == null ? null : stop.ComputedStyle.GetNumber(SvgCssPropertyNames.StopOpacity);
            stop.Opacity = opacity == null || !opacity.Success ? 1 : new SvgPaintServerResolver().Clamp(opacity.Value);
            if (stop.Color == null) new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A gradient stop did not produce a computed stop color.", source.SourceSpan);
            else stop.EffectiveAlpha = stop.Color.Alpha * stop.Opacity;
            result.Stops.Add(stop);
            index = index + 1;
        }
    }
void ResolvePattern(SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        result.PatternUnits = new SvgPaintServerResolver().ParseUnits(result.FindAttribute(SvgAttributeNames.PatternUnits), result);
        result.PatternContentUnits = new SvgPaintServerResolver().ParseUnits(result.FindAttribute(SvgAttributeNames.PatternContentUnits), result);
        result.ExplicitTransform = (new SvgPaintServerResolver().Transform(result.FindAttribute(SvgAttributeNames.PatternTransform), result) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgMatrix unitMatrix = new SvgPaintServerResolver().UnitMatrix(result.PatternUnits, options, result);
        result.Transform = (new SvgPaintServerResolver().Multiply(unitMatrix, result.ExplicitTransform, result, (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        double x = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.X), true, result.PatternUnits, options, result);
        double y = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Y), false, result.PatternUnits, options, result);
        double width = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Width), true, result.PatternUnits, options, result);
        double height = new SvgPaintServerResolver().Coordinate(result.FindAttribute(SvgAttributeNames.Height), false, result.PatternUnits, options, result);
        if (width < 0 || height < 0) new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Pattern width and height cannot be negative.", width < 0 ? new SvgPaintServerResolver().Source(result.FindAttribute(SvgAttributeNames.Width)) : new SvgPaintServerResolver().Source(result.FindAttribute(SvgAttributeNames.Height)));
        if (width >= 0 && height >= 0)
        {
            result.TileBounds = SvgRect.Create(x, y, width, height, width == 0 || height == 0);
            SvgRectTransformResult transformed = (result.Transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformRect(result.TileBounds);
            if (transformed.Success) result.UserTileBounds = transformed.Rectangle;
            else new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The transformed pattern tile bounds were not finite.", result.Node.SourceSpan);
        }
        SvgPaintServerAttribute? viewBox = result.FindAttribute(SvgAttributeNames.ViewBox);
        if (viewBox != null) result.ViewBox = (SvgViewBox?)viewBox.Value;
        SvgPaintServerAttribute? aspect = result.FindAttribute(SvgAttributeNames.PreserveAspectRatio);
        if (aspect != null && aspect.Value != null) result.AspectRatio = (SvgAspectRatio)aspect.Value;
        new SvgPaintServerResolver().ResolvePatternContentTransform(x, y, width, height, options, result);
        result.Renderable = !new SvgPaintServerResolver().HasError(result.Diagnostics) && width > 0 && height > 0 && result.ContentSource != null && new SvgPaintServerResolver().BoundingBoxUsable(result.PatternUnits, options) && (result.ViewBox != null || new SvgPaintServerResolver().BoundingBoxUsable(result.PatternContentUnits, options));
    }
void ResolvePatternContentTransform(double x, double y, double width, double height, SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        if (result.ViewBox != null)
        {
            SvgViewBoxFitResult fit = result.AspectRatio.Fit(result.ViewBox, x, y, width < 0 ? 0 : width, height < 0 ? 0 : height);
            if (!fit.Success)
            {
                new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The pattern viewBox could not be fitted into its tile.", new SvgPaintServerResolver().Source(result.FindAttribute(SvgAttributeNames.ViewBox)));
                return;
            }
            result.ContentTransform = (new SvgPaintServerResolver().Multiply(result.Transform, fit.Matrix, result, (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            return;
        }
        SvgMatrix contentBase = new SvgPaintServerResolver().UnitMatrix(result.PatternContentUnits, options, result);
        SvgMatrix? raw = new SvgPaintServerResolver().Multiply(contentBase, result.ExplicitTransform, result, (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
        SvgPoint? tileOrigin = new SvgPaintServerResolver().TransformPoint(result.Transform, new SvgPoint(x, y), result, result.Node.SourceSpan);
        SvgPoint? contentOrigin = new SvgPaintServerResolver().TransformPoint(raw, new SvgPoint(0, 0), result, result.Node.SourceSpan);
        if (tileOrigin == null || contentOrigin == null) return;
        SvgMatrix translation = new SvgMatrix(1, 0, 0, 1, tileOrigin.X - contentOrigin.X, tileOrigin.Y - contentOrigin.Y);
        result.ContentTransform = (new SvgPaintServerResolver().Multiply(translation, raw, result, result.Node.SourceSpan) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
    }
SvgComputedStyle? RootStyle(SvgNode node, SvgPaintServerResolveOptions options)
    {
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node)) return options.ComputedStyle;
        return new SvgStyleResolver().ResolveWithOptions(node, options.StyleSheets, options.StyleOptions);
    }
SvgComputedStyle StopStyle(SvgNode node, SvgNode host, SvgComputedStyle? parent, SvgPaintServerResolveOptions options)
    {
        SvgNode hostClone = new SvgPaintServerResolver().CloneNode(host);
        SvgNode nodeClone = new SvgPaintServerResolver().CloneNode(node);
        nodeClone.Parent = hostClone;
        hostClone.Children.Add(nodeClone);
        SvgCascadeOptions cascadeOptions = new SvgCascadeOptions();
        cascadeOptions.Properties = options.StyleOptions.Properties;
        cascadeOptions.Limits = options.StyleOptions.Limits;
        cascadeOptions.SelectorOptions = options.StyleOptions.SelectorOptions;
        cascadeOptions.StyleEnvironment = options.StyleOptions.StyleEnvironment;
        cascadeOptions.Parent = parent == null ? null : parent.Cascade;
        cascadeOptions.IncludeUserAgentDefaults = options.StyleOptions.IncludeUserAgentDefaults;
        SvgCascadeResult cascade = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(nodeClone, options.StyleSheets, cascadeOptions);
        SvgComputedStyleOptions computedOptions = new SvgComputedStyleOptions();
        computedOptions.Properties = options.StyleOptions.Properties;
        computedOptions.Limits = options.StyleOptions.Limits;
        computedOptions.Parent = parent;
        computedOptions.MaxPaintFallbacks = options.StyleOptions.MaxPaintFallbacks;
        return new SvgComputedStyleConverter().ConvertWithNodeAndCascadeAndOptions(nodeClone, cascade, computedOptions);
    }
double Coordinate(SvgPaintServerAttribute? attribute, bool horizontal, SvgPaintServerUnits units, SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        SvgLength? length = attribute == null ? null : (SvgLength?)attribute.Value;
        if (length == null) return 0;
        if (units == SvgPaintServerUnits.ObjectBoundingBox)
        {
            if (length.Unit == SvgLengthUnit.Number) return length.Value;
            if (length.Unit == SvgLengthUnit.Percentage) return length.Value / 100;
            new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Object-bounding-box paint coordinates must be numbers or percentages.", new SvgPaintServerResolver().Source(attribute));
            return 0;
        }
        SvgLengthPercentageBasis basis = horizontal ? SvgLengthPercentageBasis.ViewportWidth : SvgLengthPercentageBasis.ViewportHeight;
        SvgLengthEvaluationResult evaluated = length.Evaluate(options.LengthContext, basis);
        if (!evaluated.Success)
        {
            new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A user-space paint coordinate could not be evaluated.", new SvgPaintServerResolver().Source(attribute));
            return 0;
        }
        return evaluated.Value;
    }
double Radius(SvgPaintServerAttribute? attribute, SvgPaintServerUnits units, SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        SvgLength? length = attribute == null ? null : (SvgLength?)attribute.Value;
        if (length == null) return 0;
        if (units == SvgPaintServerUnits.ObjectBoundingBox)
        {
            if (length.Unit == SvgLengthUnit.Number) return length.Value;
            if (length.Unit == SvgLengthUnit.Percentage) return length.Value / 100;
            new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Object-bounding-box paint radii must be numbers or percentages.", new SvgPaintServerResolver().Source(attribute));
            return 0;
        }
        SvgLengthEvaluationResult evaluated = length.Evaluate(options.LengthContext, SvgLengthPercentageBasis.ViewportDiagonal);
        if (!evaluated.Success) new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A user-space paint radius could not be evaluated.", new SvgPaintServerResolver().Source(attribute));
        return evaluated.Success ? evaluated.Value : 0;
    }
SvgMatrix UnitMatrix(SvgPaintServerUnits units, SvgPaintServerResolveOptions options, SvgPaintServerResult result)
    {
        if (units == SvgPaintServerUnits.UserSpaceOnUse) return new SvgPaintServerResolver().Identity();
        if (options.TargetBounds == null)
        {
            new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Object-bounding-box paint units require target bounds.", (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return new SvgPaintServerResolver().Identity();
        }
        return new SvgMatrix(options.TargetBounds.Width, 0, 0, options.TargetBounds.Height, options.TargetBounds.X, options.TargetBounds.Y);
    }
SvgPaintServerUnits ParseUnits(SvgPaintServerAttribute? attribute, SvgPaintServerResult result)
    {
        string value = attribute == null || attribute.Value == null ? "" : ((string)attribute.Value).Trim();
        if (value == "userSpaceOnUse") return SvgPaintServerUnits.UserSpaceOnUse;
        if (value == "objectBoundingBox") return SvgPaintServerUnits.ObjectBoundingBox;
        new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A paint-server units attribute has an invalid keyword.", new SvgPaintServerResolver().Source(attribute));
        return SvgPaintServerUnits.ObjectBoundingBox;
    }
SvgGradientSpreadMethod ParseSpread(SvgPaintServerAttribute? attribute, SvgPaintServerResult result)
    {
        string value = attribute == null || attribute.Value == null ? "" : ((string)attribute.Value).Trim();
        if (value == "pad") return SvgGradientSpreadMethod.Pad;
        if (value == "reflect") return SvgGradientSpreadMethod.Reflect;
        if (value == "repeat") return SvgGradientSpreadMethod.Repeat;
        new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The gradient spreadMethod attribute has an invalid keyword.", new SvgPaintServerResolver().Source(attribute));
        return SvgGradientSpreadMethod.Pad;
    }
SvgMatrix? Transform(SvgPaintServerAttribute? attribute, SvgPaintServerResult result)
    {
        SvgTransformList? list = attribute == null ? null : (SvgTransformList?)attribute.Value;
        if (list == null) return new SvgPaintServerResolver().Identity();
        SvgTransformMatrixResult transformed = list.ToMatrix();
        if (!transformed.Success)
        {
            new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A paint-server transform could not be composed.", new SvgPaintServerResolver().Source(attribute));
            return new SvgPaintServerResolver().Identity();
        }
        return transformed.Matrix;
    }
double StopOffset(SvgTypedAttributeValue offset, SvgPaintServerResult result)
    {
        if (!offset.Success || !offset.HasValue || offset.ValueKind != SvgTypedAttributeKind.Length || offset.Value == null) return 0;
        SvgLength length = (SvgLength)offset.Value;
        if (length.Unit == SvgLengthUnit.Number) return length.Value;
        if (length.Unit == SvgLengthUnit.Percentage) return length.Value / 100;
        new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A gradient stop offset must be a number or percentage.", offset.SourceAttribute == null ? offset.Node.SourceSpan : offset.SourceAttribute.SourceSpan);
        return 0;
    }
System.Collections.Generic.List<SvgNode> RelevantChildren(SvgNode node, SvgPaintServerKind kind)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < node.Children.Count)
        {
            SvgNode child = node.Children[index];
            if (child.Kind == SvgNodeKind.Element)
            {
                if (kind == SvgPaintServerKind.Pattern)
                {
                    if (!new SvgPaintServerResolver().Descriptive(child.ElementKind)) result.Add(child);
                }
                else if (child.ElementKind == SvgElementKind.Stop) result.Add(child);
            }
            index = index + 1;
        }
        return result;
    }
bool Descriptive(SvgElementKind kind)
    {
        return kind == SvgElementKind.Title || kind == SvgElementKind.Desc || kind == SvgElementKind.Metadata;
    }
string[] AttributeNames(SvgPaintServerKind kind)
    {
        if (kind == SvgPaintServerKind.LinearGradient) return [SvgAttributeNames.X1, SvgAttributeNames.Y1, SvgAttributeNames.X2, SvgAttributeNames.Y2, SvgAttributeNames.GradientTransform, SvgAttributeNames.GradientUnits, SvgAttributeNames.SpreadMethod];
        if (kind == SvgPaintServerKind.RadialGradient) return [SvgAttributeNames.Cx, SvgAttributeNames.Cy, SvgAttributeNames.R, SvgAttributeNames.Fx, SvgAttributeNames.Fy, SvgAttributeNames.Fr, SvgAttributeNames.GradientTransform, SvgAttributeNames.GradientUnits, SvgAttributeNames.SpreadMethod];
        return [SvgAttributeNames.X, SvgAttributeNames.Y, SvgAttributeNames.Width, SvgAttributeNames.Height, SvgAttributeNames.ViewBox, SvgAttributeNames.PreserveAspectRatio, SvgAttributeNames.PatternTransform, SvgAttributeNames.PatternUnits, SvgAttributeNames.PatternContentUnits];
    }
SvgPaintServerKind Kind(SvgNode? node)
    {
        if (node == null) return SvgPaintServerKind.None;
        if (node.ElementKind == SvgElementKind.LinearGradient) return SvgPaintServerKind.LinearGradient;
        if (node.ElementKind == SvgElementKind.RadialGradient) return SvgPaintServerKind.RadialGradient;
        if (node.ElementKind == SvgElementKind.Pattern) return SvgPaintServerKind.Pattern;
        return SvgPaintServerKind.None;
    }
bool Compatible(SvgPaintServerKind kind, SvgNode? target)
    {
        if (target == null || target.Kind != SvgNodeKind.Element || target.NamespaceUri != SvgNamespaces.Svg) return false;
        if (kind == SvgPaintServerKind.Pattern) return target.ElementKind == SvgElementKind.Pattern;
        return target.ElementKind == SvgElementKind.LinearGradient || target.ElementKind == SvgElementKind.RadialGradient;
    }
bool AllAttributes(string[] names, SvgPaintServerResult result)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (result.FindAttribute(names[index]) == null) return false;
            index = index + 1;
        }
        return true;
    }
void AddDefault(SvgPaintServerResult result, string name, object? value)
    {
        if (result.FindAttribute(name) == null) result.Attributes.Add(new SvgPaintServerAttribute(name, null, null, value, false, true));
    }
bool Contains(System.Collections.Generic.List<SvgNode> nodes, SvgNode target)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], target)) return true;
            index = index + 1;
        }
        return false;
    }
bool BoundingBoxUsable(SvgPaintServerUnits units, SvgPaintServerResolveOptions options)
    {
        return units == SvgPaintServerUnits.UserSpaceOnUse || options.TargetBounds != null && options.TargetBounds.Width > 0 && options.TargetBounds.Height > 0;
    }
bool LinearDegenerate(SvgPaintServerResult result)
    {
        return result.Kind == SvgPaintServerKind.LinearGradient && result.Start != null && result.End != null && result.Start.X == result.End.X && result.Start.Y == result.End.Y;
    }
bool RadialDegenerate(SvgPaintServerResult result)
    {
        return result.Kind == SvgPaintServerKind.RadialGradient && result.Radius == 0;
    }
double Clamp(double value)
    {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
SvgMatrix? Multiply(SvgMatrix left, SvgMatrix? right, SvgPaintServerResult result, SvgSourceSpan? source)
    {
        SvgMatrixOperationResult product = left.Multiply((right ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (product.Success) return product.Matrix;
        new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Paint-server transform composition produced a non-finite matrix.", source);
        return new SvgPaintServerResolver().Identity();
    }
SvgPoint? TransformPoint(SvgMatrix? matrix, SvgPoint point, SvgPaintServerResult result, SvgSourceSpan? source)
    {
        SvgPointTransformResult transformed = (matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(point);
        if (transformed.Success) return transformed.Point;
        new SvgPaintServerResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Paint-server coordinate transformation produced a non-finite point.", source);
        return null;
    }
SvgSourceSpan? Source(SvgPaintServerAttribute? attribute)
    {
        if (attribute == null) return null;
        if (attribute.Attribute != null) return attribute.Attribute.SourceSpan;
        return attribute.SourceNode == null ? null : attribute.SourceNode.SourceSpan;
    }
SvgAttribute? FindAttribute(SvgNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute;
            index = index + 1;
        }
        return null;
    }
SvgAttribute? FindHref(SvgNode node)
    {
        SvgAttribute? href = new SvgPaintServerResolver().FindAttribute(node, SvgAttributeNames.Href);
        if (href != null) return href;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == SvgNamespaces.XLink && attribute.LocalName == SvgAttributeNames.Href) return attribute;
            index = index + 1;
        }
        return null;
    }
SvgNode CloneNode(SvgNode source)
    {
        SvgNode clone = new SvgNode(source.Kind, source.QualifiedName, source.Prefix, source.LocalName, source.NamespaceUri, source.Value);
        clone.OwnerDocument = source.OwnerDocument;
        clone.RawToken = source.RawToken;
        clone.EndToken = source.EndToken;
        clone.SourceSpan = source.SourceSpan;
        clone.SourceMapKind = source.SourceMapKind;
        clone.IsDirty = source.IsDirty;
        clone.Version = source.Version;
        int index = 0;
        while (index < source.Attributes.Count)
        {
            SvgAttribute sourceAttribute = source.Attributes[index];
            SvgAttribute attribute = new SvgAttribute(sourceAttribute.QualifiedName, sourceAttribute.Prefix, sourceAttribute.LocalName, sourceAttribute.NamespaceUri, sourceAttribute.Value);
            attribute.OwnerElement = clone;
            attribute.OwnerDocument = sourceAttribute.OwnerDocument;
            attribute.RawToken = sourceAttribute.RawToken;
            attribute.SourceSpan = sourceAttribute.SourceSpan;
            attribute.SourceMapKind = sourceAttribute.SourceMapKind;
            attribute.IsDirty = sourceAttribute.IsDirty;
            attribute.Version = sourceAttribute.Version;
            clone.Attributes.Add(attribute);
            index = index + 1;
        }
        return clone;
    }
SvgReferenceResolveOptions CopyReferenceOptions(SvgReferenceResolveOptions source)
    {
        SvgReferenceResolveOptions result = new SvgReferenceResolveOptions();
        result.RequireReference = false;
        result.ExternalDocument = source.ExternalDocument;
        result.MaxUriCharacters = source.MaxUriCharacters;
        result.MaxBaseDepth = source.MaxBaseDepth;
        return result;
    }
SvgTransformList EmptyTransform()
    {
        return new SvgTransformList(new System.Collections.Generic.List<SvgTransform>());
    }
SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
bool ValidOptions(SvgPaintServerResolveOptions options)
    {
        return options.Attributes != null && options.ReferenceOptions != null && options.Limits != null && options.LengthContext != null && options.StyleSheets != null && options.StyleOptions != null && options.StyleOptions.Properties != null && options.StyleOptions.Limits != null && options.Limits.MaxPaintServerDepth > 0 && options.Limits.MaxCount > 0 && options.MaxStops > 0;
    }
void AppendReferenceDiagnostics(SvgPaintServerResult result, SvgReferenceResult reference)
    {
        int index = 0;
        while (index < reference.Diagnostics.Count)
        {
            result.Diagnostics.Add(reference.Diagnostics[index]);
            index = index + 1;
        }
    }
void AppendTypedDiagnostics(SvgPaintServerResult result, SvgTypedAttributeValue typed)
    {
        int index = 0;
        while (index < typed.Diagnostics.Count)
        {
            result.Diagnostics.Add(typed.Diagnostics[index]);
            index = index + 1;
        }
    }
void AppendStyleDiagnostics(SvgPaintServerResult result, SvgComputedStyle? style)
    {
        if (style == null) return;
        int index = 0;
        while (index < style.Diagnostics.Count)
        {
            result.Diagnostics.Add(style.Diagnostics[index]);
            index = index + 1;
        }
    }
void AddError(SvgPaintServerResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
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
}
