namespace svg;

/// <summary>Resolves marker, clipping-path, and mask resources into bounded target-space semantics.</summary>
public class SvgEffectResolver : ISvgEffectResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgEffectResolver()
    {
    }

    /// <summary>Resolves one supported effect resource using deterministic defaults.</summary>
    /// <param name="node">The marker, clipPath, or mask element.</param>
    /// <returns>The normalized effect resource.</returns>
    public SvgEffectResult ResolveWithNode(SvgNode node)
    {
        return new SvgEffectResolver().ResolveWithNodeAndOptions(node, new SvgEffectResolveOptions());
    }

    /// <summary>Resolves one supported effect resource with explicit geometry, style, reference, and limit inputs.</summary>
    /// <param name="node">The marker, clipPath, or mask element.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The normalized effect resource.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not a supported SVG effect element.</exception>
    public SvgEffectResult ResolveWithNodeAndOptions(SvgNode node, SvgEffectResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        SvgEffectKind kind = new SvgEffectResolver().Kind(node);
        if (kind == SvgEffectKind.None) throw new System.ArgumentOutOfRangeException("node");
        SvgEffectResult result = new SvgEffectResult(node);
        result.Kind = kind;
        if (!new SvgEffectResolver().ValidOptions(options))
        {
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Effect resolution options are invalid.", node.SourceSpan);
            return result;
        }
        SvgEffectTraversal traversal = new SvgEffectTraversal();
        traversal.Active.Add(node);
        return new SvgEffectResolver().ResolveResource(node, options, traversal, 0, true);
    }
SvgEffectResult ResolveResource(SvgNode node, SvgEffectResolveOptions options, SvgEffectTraversal traversal, int depth, bool outer)
    {
        SvgEffectResult result = new SvgEffectResult(node);
        result.Kind = new SvgEffectResolver().Kind(node);
        result.ComputedStyle = new SvgEffectResolver().ResolveStyle(node, options, outer);
        new SvgEffectResolver().AppendStyleDiagnostics(result, result.ComputedStyle);
        new SvgEffectResolver().CollectChildren(result);
        new SvgEffectResolver().CollectContent(result, options, traversal, depth);
        new SvgEffectResolver().ResolveStyleReferences(node, result.ComputedStyle, result, options, traversal, depth);
        result.Empty = !new SvgEffectResolver().EffectiveContent(result);
        if (result.Kind == SvgEffectKind.Marker) new SvgEffectResolver().ResolveMarker(result, options);
        else if (result.Kind == SvgEffectKind.ClipPath) new SvgEffectResolver().ResolveClipPath(result, options);
        else new SvgEffectResolver().ResolveMask(result, options);
        result.Success = !new SvgEffectResolver().HasError(result.Diagnostics);
        if (!result.Success) result.Renderable = false;
        return result;
    }
void ResolveMarker(SvgEffectResult result, SvgEffectResolveOptions options)
    {
        result.MarkerUnits = new SvgEffectResolver().MarkerUnits(result, options);
        double width = new SvgEffectResolver().MarkerLength((result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.MarkerWidth, options, result, SvgLengthPercentageBasis.ViewportWidth, 3);
        double height = new SvgEffectResolver().MarkerLength(result.Node, SvgAttributeNames.MarkerHeight, options, result, SvgLengthPercentageBasis.ViewportHeight, 3);
        if (width < 0 || height < 0)
        {
            SvgAttribute? invalid = new SvgEffectResolver().FindAttribute(result.Node, width < 0 ? SvgAttributeNames.MarkerWidth : SvgAttributeNames.MarkerHeight);
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A marker width or height cannot be negative.", invalid == null ? result.Node.SourceSpan : invalid.SourceSpan);
            return;
        }
        result.Viewport = SvgRect.Create(0, 0, width, height, width == 0 || height == 0);
        result.ClipsOverflow = new SvgEffectResolver().ClipsOverflow(result.ComputedStyle);
        if (result.ClipsOverflow) result.ClipRect = result.Viewport;
        new SvgEffectResolver().ReadViewBox(result, options);
        new SvgEffectResolver().ReadOrientation(result, options);
        SvgMatrix fit = new SvgEffectResolver().Identity();
        double contentWidth = width;
        double contentHeight = height;
        if (result.ViewBox != null)
        {
            contentWidth = result.ViewBox.Width;
            contentHeight = result.ViewBox.Height;
            if (result.ViewBox.Width == 0 || result.ViewBox.Height == 0) result.Viewport.IsEmpty = true;
            else
            {
                SvgViewBoxFitResult fitted = result.AspectRatio.Fit(result.ViewBox, 0, 0, width, height);
                if (!fitted.Success) new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The marker viewBox could not be fitted.", result.Node.SourceSpan);
                else fit = (fitted.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            }
        }
        result.ContentLengthContext = new SvgEffectResolver().CopyContext(options.LengthContext);
        result.ContentLengthContext.HasViewport = true;
        result.ContentLengthContext.ViewportWidth = contentWidth;
        result.ContentLengthContext.ViewportHeight = contentHeight;
        result.RefX = new SvgEffectResolver().MarkerReference(result.Node, SvgAttributeNames.RefX, contentWidth, options, result);
        result.RefY = new SvgEffectResolver().MarkerReference(result.Node, SvgAttributeNames.RefY, contentHeight, options, result);
        SvgPointTransformResult mapped = (fit ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformPoint(new SvgPoint(result.RefX, result.RefY));
        if (!mapped.Success)
        {
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The marker reference point could not be transformed.", result.Node.SourceSpan);
            return;
        }
        double unitScale = result.MarkerUnits == SvgMarkerUnits.StrokeWidth ? options.StrokeWidth : 1;
        SvgMatrix? local = new SvgEffectResolver().Multiply(new SvgEffectResolver().Translation(-(mapped.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, -mapped.Point.Y), fit, result, result.Node.SourceSpan);
        local = new SvgEffectResolver().Multiply(new SvgEffectResolver().Scale(unitScale, unitScale), local, result, result.Node.SourceSpan);
        local = new SvgEffectResolver().Multiply(new SvgEffectResolver().Rotation(result.AppliedAngle), local, result, result.Node.SourceSpan);
        result.Transform = (new SvgEffectResolver().Multiply(new SvgEffectResolver().Translation(options.MarkerPosition.X, options.MarkerPosition.Y), local, result, result.Node.SourceSpan) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        result.ContentTransform = (result.Transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        result.Renderable = !result.Empty && width > 0 && height > 0 && unitScale > 0 && (result.ViewBox == null || result.ViewBox.Width > 0 && result.ViewBox.Height > 0);
    }
void ResolveClipPath(SvgEffectResult result, SvgEffectResolveOptions options)
    {
        result.Units = new SvgEffectResolver().Units((result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.ClipPathUnits, SvgPaintServerUnits.UserSpaceOnUse, options, result);
        result.ExplicitTransform = (new SvgEffectResolver().ComputedTransform(result.ComputedStyle, result.Units, options, result) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgMatrix units = new SvgEffectResolver().UnitMatrix(result.Units, options, result);
        result.Transform = (new SvgEffectResolver().Multiply(units, result.ExplicitTransform, result, result.Node.SourceSpan) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        result.ContentTransform = (result.Transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        bool box = new SvgEffectResolver().BoundingBoxUsable(result.Units, options);
        result.Renderable = !result.Empty && box;
    }
void ResolveMask(SvgEffectResult result, SvgEffectResolveOptions options)
    {
        result.Units = new SvgEffectResolver().Units((result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.MaskUnits, SvgPaintServerUnits.ObjectBoundingBox, options, result);
        result.ContentUnits = new SvgEffectResolver().Units(result.Node, SvgAttributeNames.MaskContentUnits, SvgPaintServerUnits.UserSpaceOnUse, options, result);
        SvgMatrix units = new SvgEffectResolver().UnitMatrix(result.Units, options, result);
        result.ContentTransform = new SvgEffectResolver().UnitMatrix(result.ContentUnits, options, result);
        result.Transform = units;
        double x = new SvgEffectResolver().EffectLength(result.Node, SvgAttributeNames.X, result.Units, options, result, SvgLengthPercentageBasis.ViewportWidth, -0.1);
        double y = new SvgEffectResolver().EffectLength(result.Node, SvgAttributeNames.Y, result.Units, options, result, SvgLengthPercentageBasis.ViewportHeight, -0.1);
        double width = new SvgEffectResolver().EffectLength(result.Node, SvgAttributeNames.Width, result.Units, options, result, SvgLengthPercentageBasis.ViewportWidth, 1.2);
        double height = new SvgEffectResolver().EffectLength(result.Node, SvgAttributeNames.Height, result.Units, options, result, SvgLengthPercentageBasis.ViewportHeight, 1.2);
        if (width >= 0 && height >= 0)
        {
            result.Region = SvgRect.Create(x, y, width, height, width == 0 || height == 0);
            SvgRectTransformResult transformed = units.TransformRect(result.Region);
            if (transformed.Success) result.UserRegion = transformed.Rectangle;
            else new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The mask region could not be transformed.", result.Node.SourceSpan);
        }
        result.MaskType = new SvgEffectResolver().MaskType(result.ComputedStyle);
        bool box = new SvgEffectResolver().BoundingBoxUsable(result.Units, options) && new SvgEffectResolver().BoundingBoxUsable(result.ContentUnits, options);
        result.Renderable = !result.Empty && width > 0 && height > 0 && box;
    }
void CollectChildren(SvgEffectResult result)
    {
        int index = 0;
        while (index < (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            SvgNode child = result.Node.Children[index];
            if (new SvgEffectResolver().Relevant(child)) result.Children.Add(child);
            index = index + 1;
        }
    }
void CollectContent(SvgEffectResult result, SvgEffectResolveOptions options, SvgEffectTraversal traversal, int depth)
    {
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        int index = result.Children.Count - 1;
        while (index >= 0)
        {
            stack.Add(result.Children[index]);
            index = index - 1;
        }
        while (stack.Count != 0)
        {
            SvgNode node = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (traversal.ContentCount >= options.MaxContentNodes)
            {
                new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Effect content exceeded MaxContentNodes.", node.SourceSpan);
                return;
            }
            traversal.ContentCount = traversal.ContentCount + 1;
            SvgEffectContent content = new SvgEffectContent(node);
            if (node.Kind == SvgNodeKind.Element)
            {
                content.ComputedStyle = new SvgEffectResolver().ResolveStyle(node, options, false);
                new SvgEffectResolver().AppendStyleDiagnostics(result, content.ComputedStyle);
                new SvgEffectResolver().ResolveStyleReferences(node, content.ComputedStyle, result, options, traversal, depth);
                if (node.ElementKind == SvgElementKind.Use) new SvgEffectResolver().ResolveUse(node, content, result, options, traversal);
            }
            result.Content.Add(content);
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                if (new SvgEffectResolver().Relevant(node.Children[childIndex])) stack.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
    }
void ResolveUse(SvgNode node, SvgEffectContent content, SvgEffectResult result, SvgEffectResolveOptions options, SvgEffectTraversal traversal)
    {
        if (!new SvgEffectResolver().TakeReference(result, options, traversal, node.SourceSpan)) return;
        SvgUseResolveOptions useOptions = new SvgUseResolveOptions();
        useOptions.Attributes = options.Attributes;
        useOptions.LengthContext = options.LengthContext;
        useOptions.ReferenceOptions = new SvgEffectResolver().CopyReferenceOptions(options.ReferenceOptions);
        useOptions.Limits = options.Limits;
        useOptions.StyleSheets = options.StyleSheets;
        useOptions.StyleOptions = options.StyleOptions;
        content.Use = new SvgUseResolver().ResolveWithNodeAndOptions(node, useOptions);
        SvgEffectReference edge = new SvgEffectReference(node, SvgAttributeNames.Href);
        edge.Reference = content.Use.Reference;
        edge.Use = content.Use;
        edge.Success = content.Use.Success;
        result.References.Add(edge);
        new SvgEffectResolver().AppendDiagnostics(result, content.Use.Diagnostics);
        if (result.Kind == SvgEffectKind.ClipPath && content.Use.Reference != null && content.Use.Reference.Target != null && !new SvgEffectResolver().ClipUseTarget(content.Use.Reference.Target))
        {
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "A direct use child of clipPath must reference a shape or text element.", content.Use.Reference.TargetSource);
            edge.Success = false;
        }
    }
void ResolveStyleReferences(SvgNode node, SvgComputedStyle? style, SvgEffectResult result, SvgEffectResolveOptions options, SvgEffectTraversal traversal, int depth)
    {
        if (style == null) return;
        new SvgEffectResolver().ResolveComputedReference(node, style.Find(SvgCssPropertyNames.ClipPath), SvgEffectKind.ClipPath, result, options, traversal, depth);
        new SvgEffectResolver().ResolveComputedReference(node, style.Find(SvgCssPropertyNames.Mask), SvgEffectKind.Mask, result, options, traversal, depth);
    }
void ResolveComputedReference(SvgNode node, SvgComputedValue? computed, SvgEffectKind expected, SvgEffectResult result, SvgEffectResolveOptions options, SvgEffectTraversal traversal, int depth)
    {
        if (computed == null || computed.Kind == SvgComputedValueKind.Keyword && string.Equals(computed.Keyword, "none", System.StringComparison.OrdinalIgnoreCase)) return;
        SvgIriReference? iri = null;
        if (computed.Kind == SvgComputedValueKind.Reference) iri = computed.Reference;
        else
        {
            SvgIriParseResult parsed = new SvgIriParser().ParseUrl(computed.CssText, options.ReferenceOptions.MaxUriCharacters);
            if (parsed.Success) iri = parsed.Reference;
        }
        if (iri == null) return;
        if (!new SvgEffectResolver().TakeReference(result, options, traversal, computed.Source)) return;
        SvgEffectReference edge = new SvgEffectReference(node, computed.PropertyName);
        edge.Reference = new SvgReferenceResolver().ResolveValue(node, iri.Value, computed.Source, new SvgEffectResolver().CopyReferenceOptions(options.ReferenceOptions));
        result.References.Add(edge);
        new SvgEffectResolver().AppendReferenceDiagnostics(result, edge.Reference);
        if (!edge.Reference.Success || edge.Reference.Status != SvgReferenceStatus.Resolved || edge.Reference.Target == null) return;
        if (new SvgEffectResolver().Kind(edge.Reference.Target) != expected)
        {
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "A nested effect reference identifies an incompatible resource element.", edge.Reference.TargetSource);
            return;
        }
        if (new SvgEffectResolver().Contains(traversal.Active, edge.Reference.Target))
        {
            edge.Cycle = true;
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.ReferenceCycle, "A nested effect reference cycle was detected.", edge.Reference.ReferenceSource);
            return;
        }
        if (depth >= options.Limits.MaxEffectDepth)
        {
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Nested effect resolution exceeded MaxEffectDepth.", edge.Reference.ReferenceSource);
            return;
        }
        traversal.Active.Add(edge.Reference.Target);
        edge.Effect = new SvgEffectResolver().ResolveResource(edge.Reference.Target, options, traversal, depth + 1, false);
        traversal.Active.RemoveAt(traversal.Active.Count - 1);
        edge.Success = edge.Effect.Success;
        new SvgEffectResolver().AppendDiagnostics(result, edge.Effect.Diagnostics);
    }
SvgComputedStyle? ResolveStyle(SvgNode node, SvgEffectResolveOptions options, bool outer)
    {
        if (outer && options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node)) return options.ComputedStyle;
        SvgStyleResolver resolver = new SvgStyleResolver();
        return resolver.ResolveWithOptions(node, options.StyleSheets, options.StyleOptions);
    }
SvgMarkerUnits MarkerUnits(SvgEffectResult result, SvgEffectResolveOptions options)
    {
        SvgTypedAttributeValue typed = options.Attributes.Get((result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.MarkerUnits);
        new SvgEffectResolver().AppendTypedDiagnostics(result, typed);
        string text = typed.HasValue && typed.Value != null ? ((string)typed.Value).Trim() : "strokeWidth";
        if (text == "strokeWidth") return SvgMarkerUnits.StrokeWidth;
        if (text == "userSpaceOnUse") return SvgMarkerUnits.UserSpaceOnUse;
        new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The markerUnits attribute has an invalid keyword.", new SvgEffectResolver().Source(typed));
        return SvgMarkerUnits.StrokeWidth;
    }
SvgPaintServerUnits Units(SvgNode node, string name, SvgPaintServerUnits initial, SvgEffectResolveOptions options, SvgEffectResult result)
    {
        SvgTypedAttributeValue typed = options.Attributes.Get(node, name);
        new SvgEffectResolver().AppendTypedDiagnostics(result, typed);
        if (!typed.Success || !typed.HasValue || typed.Value == null) return initial;
        string text = ((string)typed.Value).Trim();
        if (text == "userSpaceOnUse") return SvgPaintServerUnits.UserSpaceOnUse;
        if (text == "objectBoundingBox") return SvgPaintServerUnits.ObjectBoundingBox;
        new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "An effect units attribute has an invalid keyword.", new SvgEffectResolver().Source(typed));
        return initial;
    }
double MarkerLength(SvgNode node, string name, SvgEffectResolveOptions options, SvgEffectResult result, SvgLengthPercentageBasis basis, double initial)
    {
        SvgTypedAttributeValue typed = options.Attributes.Get(node, name);
        new SvgEffectResolver().AppendTypedDiagnostics(result, typed);
        if (!typed.Success || !typed.HasValue || typed.ValueKind != SvgTypedAttributeKind.Length || typed.Value == null) return initial;
        SvgLengthEvaluationResult value = ((SvgLength)typed.Value).Evaluate(options.LengthContext, basis);
        if (value.Success) return value.Value;
        new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A marker dimension could not be evaluated.", new SvgEffectResolver().Source(typed));
        return 0;
    }
double MarkerReference(SvgNode node, string name, double basis, SvgEffectResolveOptions options, SvgEffectResult result)
    {
        SvgAttribute? authored = new SvgEffectResolver().FindAttribute(node, name);
        if (authored != null)
        {
            string keyword = authored.Value.Trim();
            if (keyword == "left" || keyword == "top") return 0;
            if (keyword == "center") return basis / 2;
            if (keyword == "right" || keyword == "bottom") return basis;
        }
        SvgTypedAttributeValue typed = options.Attributes.Get(node, name);
        new SvgEffectResolver().AppendTypedDiagnostics(result, typed);
        if (!typed.Success || !typed.HasValue || typed.ValueKind != SvgTypedAttributeKind.Length || typed.Value == null) return 0;
        SvgLength length = (SvgLength)typed.Value;
        if (length.Unit == SvgLengthUnit.Percentage) return basis * length.Value / 100;
        SvgLengthContext context = new SvgEffectResolver().CopyContext(options.LengthContext);
        context.HasViewport = true;
        context.ViewportWidth = basis;
        context.ViewportHeight = basis;
        SvgLengthEvaluationResult value = length.Evaluate(context, SvgLengthPercentageBasis.ViewportWidth);
        if (value.Success) return value.Value;
        new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A marker reference coordinate could not be evaluated.", new SvgEffectResolver().Source(typed));
        return 0;
    }
void ReadViewBox(SvgEffectResult result, SvgEffectResolveOptions options)
    {
        SvgTypedAttributeValue viewBox = options.Attributes.Get((result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.ViewBox);
        new SvgEffectResolver().AppendTypedDiagnostics(result, viewBox);
        if (viewBox.Success && viewBox.HasValue && viewBox.ValueKind == SvgTypedAttributeKind.ViewBox) result.ViewBox = (SvgViewBox?)viewBox.Value;
        SvgTypedAttributeValue aspect = options.Attributes.Get(result.Node, SvgAttributeNames.PreserveAspectRatio);
        new SvgEffectResolver().AppendTypedDiagnostics(result, aspect);
        if (aspect.Success && aspect.HasValue && aspect.ValueKind == SvgTypedAttributeKind.AspectRatio) result.AspectRatio = ((SvgAspectRatio?)aspect.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
    }
void ReadOrientation(SvgEffectResult result, SvgEffectResolveOptions options)
    {
        SvgTypedAttributeValue typed = options.Attributes.Get((result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.Orient);
        new SvgEffectResolver().AppendTypedDiagnostics(result, typed);
        if (typed.Success && typed.ValueKind == SvgTypedAttributeKind.Keyword)
        {
            string? keyword = (string?)typed.Value;
            result.OrientKind = keyword == "auto-start-reverse" ? SvgMarkerOrientKind.AutoStartReverse : SvgMarkerOrientKind.Auto;
            result.AppliedAngle = options.MarkerAutoAngle;
            if (result.OrientKind == SvgMarkerOrientKind.AutoStartReverse && options.MarkerPlacement == SvgMarkerPlacement.Start) result.AppliedAngle = result.AppliedAngle + 180;
            return;
        }
        if (typed.Success && typed.ValueKind == SvgTypedAttributeKind.Angle && typed.Value != null)
        {
            SvgUnitConversionResult degrees = ((SvgAngle)typed.Value).ConvertTo(SvgAngleUnit.Deg);
            if (degrees.Success)
            {
                result.OrientKind = SvgMarkerOrientKind.Angle;
                result.OrientAngle = degrees.Value;
                result.AppliedAngle = degrees.Value;
                return;
            }
        }
        new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The marker orient attribute could not be evaluated.", new SvgEffectResolver().Source(typed));
    }
SvgMaskType MaskType(SvgComputedStyle? style)
    {
        if (style == null) return SvgMaskType.Luminance;
        SvgComputedValue? value = style.Find(SvgCssPropertyNames.MaskType);
        if (value != null && value.Kind == SvgComputedValueKind.Keyword && string.Equals(value.Keyword, "alpha", System.StringComparison.OrdinalIgnoreCase)) return SvgMaskType.Alpha;
        return SvgMaskType.Luminance;
    }
bool ClipsOverflow(SvgComputedStyle? style)
    {
        if (style == null) return true;
        SvgComputedValue? horizontal = style.Find(SvgCssPropertyNames.OverflowX);
        SvgComputedValue? vertical = style.Find(SvgCssPropertyNames.OverflowY);
        bool horizontalVisible = horizontal != null && horizontal.Kind == SvgComputedValueKind.Keyword && string.Equals(horizontal.Keyword, "visible", System.StringComparison.OrdinalIgnoreCase);
        bool verticalVisible = vertical != null && vertical.Kind == SvgComputedValueKind.Keyword && string.Equals(vertical.Keyword, "visible", System.StringComparison.OrdinalIgnoreCase);
        return !horizontalVisible || !verticalVisible;
    }
double EffectLength(SvgNode node, string name, SvgPaintServerUnits units, SvgEffectResolveOptions options, SvgEffectResult result, SvgLengthPercentageBasis basis, double initial)
    {
        SvgTypedAttributeValue typed = options.Attributes.Get(node, name);
        new SvgEffectResolver().AppendTypedDiagnostics(result, typed);
        if (!typed.Success || !typed.HasValue || typed.ValueKind != SvgTypedAttributeKind.Length || typed.Value == null) return initial;
        SvgLength length = (SvgLength)typed.Value;
        if (units == SvgPaintServerUnits.ObjectBoundingBox)
        {
            if (length.Unit == SvgLengthUnit.Number) return length.Value;
            if (length.Unit == SvgLengthUnit.Percentage) return length.Value / 100;
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Object-bounding-box effect geometry accepts only numbers and percentages.", new SvgEffectResolver().Source(typed));
            return 0;
        }
        SvgLengthEvaluationResult value = length.Evaluate(options.LengthContext, basis);
        if (value.Success) return value.Value;
        new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A user-space effect length could not be evaluated.", new SvgEffectResolver().Source(typed));
        return 0;
    }
SvgMatrix? ComputedTransform(SvgComputedStyle? style, SvgPaintServerUnits units, SvgEffectResolveOptions options, SvgEffectResult result)
    {
        if (style == null) return new SvgEffectResolver().Identity();
        SvgComputedValue? value = style.Find(SvgCssPropertyNames.Transform);
        if (value == null || value.Kind != SvgComputedValueKind.TransformList || value.TransformList == null || value.TransformList.IsNone) return new SvgEffectResolver().Identity();
        SvgComputedValue? origin = style.Find(SvgCssPropertyNames.TransformOrigin);
        SvgComputedValue? box = style.Find(SvgCssPropertyNames.TransformBox);
        SvgTransformOrigin? transformOrigin = origin == null ? null : origin.TransformOrigin;
        SvgTransformBox transformBox = box == null ? SvgTransformBox.ViewBox : box.TransformBox;
        if (transformOrigin == null) transformOrigin = new SvgTransformOrigin(new SvgLength(0, SvgLengthUnit.Px), new SvgLength(0, SvgLengthUnit.Px), null, false);
        SvgCssTransformContext context = new SvgCssTransformContext();
        context.LengthContext = options.LengthContext;
        SvgCssTransformReferenceBox reference = new SvgEffectResolver().ReferenceBox(units, options);
        context.ContentBox = reference;
        context.BorderBox = reference;
        context.FillBox = reference;
        context.StrokeBox = reference;
        context.ViewBox = reference;
        SvgCssTransformMatrixResult matrix = value.TransformList.ToMatrix(context, transformOrigin, transformBox);
        if (matrix.Success) return matrix.Matrix;
        new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The clipPath transform could not be evaluated.", value.Source);
        return new SvgEffectResolver().Identity();
    }
SvgCssTransformReferenceBox ReferenceBox(SvgPaintServerUnits units, SvgEffectResolveOptions options)
    {
        if (units == SvgPaintServerUnits.ObjectBoundingBox) return new SvgCssTransformReferenceBox(true, 0, 0, 1, 1);
        if (options.TargetBounds != null) return new SvgCssTransformReferenceBox(true, options.TargetBounds.X, options.TargetBounds.Y, options.TargetBounds.Width, options.TargetBounds.Height);
        if (options.LengthContext.HasViewport) return new SvgCssTransformReferenceBox(true, 0, 0, options.LengthContext.ViewportWidth, options.LengthContext.ViewportHeight);
        return new SvgCssTransformReferenceBox(false, 0, 0, 0, 0);
    }
SvgMatrix UnitMatrix(SvgPaintServerUnits units, SvgEffectResolveOptions options, SvgEffectResult result)
    {
        if (units == SvgPaintServerUnits.UserSpaceOnUse) return new SvgEffectResolver().Identity();
        if (options.TargetBounds == null)
        {
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Object-bounding-box effect units require target bounds.", (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return new SvgEffectResolver().Identity();
        }
        return new SvgMatrix(options.TargetBounds.Width, 0, 0, options.TargetBounds.Height, options.TargetBounds.X, options.TargetBounds.Y);
    }
bool BoundingBoxUsable(SvgPaintServerUnits units, SvgEffectResolveOptions options)
    {
        return units == SvgPaintServerUnits.UserSpaceOnUse || options.TargetBounds != null && options.TargetBounds.Width > 0 && options.TargetBounds.Height > 0;
    }
bool TakeReference(SvgEffectResult result, SvgEffectResolveOptions options, SvgEffectTraversal traversal, SvgSourceSpan? source)
    {
        if (traversal.ReferenceCount >= options.Limits.MaxCount)
        {
            new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Effect resolution exceeded the reference MaxCount.", source);
            return false;
        }
        traversal.ReferenceCount = traversal.ReferenceCount + 1;
        return true;
    }
bool ClipUseTarget(SvgNode? node)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return false;
        SvgElementKind kind = node.ElementKind;
        return kind == SvgElementKind.Path || kind == SvgElementKind.Text || kind == SvgElementKind.Circle || kind == SvgElementKind.Ellipse || kind == SvgElementKind.Line || kind == SvgElementKind.Polygon || kind == SvgElementKind.Polyline || kind == SvgElementKind.Rect;
    }
bool Relevant(SvgNode? node)
    {
        if (node == null) return false;
        if (node.Kind != SvgNodeKind.Element) return node.Kind == SvgNodeKind.Text || node.Kind == SvgNodeKind.CData;
        SvgElementKind kind = node.ElementKind;
        return kind != SvgElementKind.Title && kind != SvgElementKind.Desc && kind != SvgElementKind.Metadata && kind != SvgElementKind.Animate && kind != SvgElementKind.AnimateMotion && kind != SvgElementKind.AnimateTransform && kind != SvgElementKind.Set && kind != SvgElementKind.Discard && kind != SvgElementKind.Script && kind != SvgElementKind.Style;
    }
bool EffectiveContent(SvgEffectResult result)
    {
        int index = 0;
        while (index < result.Content.Count)
        {
            SvgEffectContent content = result.Content[index];
            SvgNode node = (content.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
            if (node != null && node.Kind == SvgNodeKind.Element)
            {
                SvgElementKind kind = node.ElementKind;
                if (kind == SvgElementKind.Use && content.Use != null && content.Use.Success) return true;
                if (kind == SvgElementKind.Path || kind == SvgElementKind.Text || kind == SvgElementKind.Circle || kind == SvgElementKind.Ellipse || kind == SvgElementKind.Line || kind == SvgElementKind.Polygon || kind == SvgElementKind.Polyline || kind == SvgElementKind.Rect || kind == SvgElementKind.Image || kind == SvgElementKind.ForeignObject || kind == SvgElementKind.Audio || kind == SvgElementKind.Canvas || kind == SvgElementKind.Iframe || kind == SvgElementKind.Video) return true;
            }
            index = index + 1;
        }
        return false;
    }
SvgEffectKind Kind(SvgNode? node)
    {
        if (node == null || node.Kind != SvgNodeKind.Element || node.NamespaceUri != SvgNamespaces.Svg) return SvgEffectKind.None;
        if (node.ElementKind == SvgElementKind.Marker) return SvgEffectKind.Marker;
        if (node.ElementKind == SvgElementKind.ClipPath) return SvgEffectKind.ClipPath;
        if (node.ElementKind == SvgElementKind.Mask) return SvgEffectKind.Mask;
        return SvgEffectKind.None;
    }
bool Contains(System.Collections.Generic.List<SvgNode> nodes, SvgNode node)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], node)) return true;
            index = index + 1;
        }
        return false;
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
SvgSourceSpan? Source(SvgTypedAttributeValue? typed)
    {
        if (typed == null) return null;
        return typed.SourceAttribute == null ? typed.Node.SourceSpan : typed.SourceAttribute.SourceSpan;
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
SvgLengthContext CopyContext(SvgLengthContext source)
    {
        SvgLengthContext result = new SvgLengthContext();
        result.HasViewport = source.HasViewport;
        result.ViewportWidth = source.ViewportWidth;
        result.ViewportHeight = source.ViewportHeight;
        result.HasFontSize = source.HasFontSize;
        result.FontSize = source.FontSize;
        result.HasRootFontSize = source.HasRootFontSize;
        result.RootFontSize = source.RootFontSize;
        result.HasXHeight = source.HasXHeight;
        result.XHeight = source.XHeight;
        result.HasZeroAdvance = source.HasZeroAdvance;
        result.ZeroAdvance = source.ZeroAdvance;
        return result;
    }
SvgMatrix? Multiply(SvgMatrix left, SvgMatrix? right, SvgEffectResult result, SvgSourceSpan? source)
    {
        SvgMatrixOperationResult product = left.Multiply((right ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (product.Success) return product.Matrix;
        new SvgEffectResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Effect transform composition produced a non-finite matrix.", source);
        return new SvgEffectResolver().Identity();
    }
SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
SvgMatrix Translation(double x, double y)
    {
        return new SvgMatrix(1, 0, 0, 1, x, y);
    }
SvgMatrix Scale(double x, double y)
    {
        return new SvgMatrix(x, 0, 0, y, 0, 0);
    }
SvgMatrix Rotation(double degrees)
    {
        double radians = degrees * System.Math.PI / 180;
        double cosine = System.Math.Cos(radians);
        double sine = System.Math.Sin(radians);
        return new SvgMatrix(cosine, sine, -sine, cosine, 0, 0);
    }
bool ValidOptions(SvgEffectResolveOptions options)
    {
        return options.Attributes != null && options.ReferenceOptions != null && options.Limits != null && options.LengthContext != null && options.StyleSheets != null && options.StyleOptions != null && options.StyleOptions.Properties != null && options.StyleOptions.Limits != null && options.Limits.MaxEffectDepth > 0 && options.Limits.MaxCount > 0 && options.MaxContentNodes > 0 && SvgFiniteMath.IsFinite(options.StrokeWidth) && options.StrokeWidth >= 0 && options.MarkerPosition != null && SvgFiniteMath.IsFinite(options.MarkerAutoAngle);
    }
void AppendTypedDiagnostics(SvgEffectResult result, SvgTypedAttributeValue typed)
    {
        int index = 0;
        while (index < typed.Diagnostics.Count)
        {
            result.Diagnostics.Add(typed.Diagnostics[index]);
            index = index + 1;
        }
    }
void AppendStyleDiagnostics(SvgEffectResult result, SvgComputedStyle? style)
    {
        if (style == null) return;
        new SvgEffectResolver().AppendDiagnostics(result, style.Diagnostics);
    }
void AppendReferenceDiagnostics(SvgEffectResult result, SvgReferenceResult? reference)
    {
        if (reference == null) return;
        new SvgEffectResolver().AppendDiagnostics(result, reference.Diagnostics);
    }
void AppendDiagnostics(SvgEffectResult result, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            result.Diagnostics.Add(diagnostics[index]);
            index = index + 1;
        }
    }
void AddError(SvgEffectResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
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
