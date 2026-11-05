namespace svg;

/// <summary>Expands SVG <c>use</c> references into bounded source-linked shadow instances.</summary>
public class SvgUseResolver : ISvgUseResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgUseResolver()
    {
    }

    /// <summary>Expands one <c>use</c> element using deterministic defaults.</summary>
    /// <param name="node">The authored <c>use</c> element.</param>
    /// <returns>The source-linked shadow expansion.</returns>
    public SvgUseResult ResolveWithNode(SvgNode node)
    {
        return new SvgUseResolver().ResolveWithNodeAndOptions(node, new SvgUseResolveOptions());
    }

    /// <summary>Expands one <c>use</c> element with explicit references, styles, geometry, and limits.</summary>
    /// <param name="node">The authored <c>use</c> element.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>The source-linked shadow expansion.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an SVG <c>use</c> element.</exception>
    public SvgUseResult ResolveWithNodeAndOptions(SvgNode node, SvgUseResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        if (node.Kind != SvgNodeKind.Element || node.ElementKind != SvgElementKind.Use) throw new System.ArgumentOutOfRangeException("node");
        SvgUseResult result = new SvgUseResult(node);
        if (!new SvgUseResolver().ValidOptions(options))
        {
            new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Use expansion options are invalid.", node.SourceSpan, "");
            return result;
        }

        result.UseStyle = new SvgUseResolver().HostStyle(node, options);
        new SvgUseResolver().AppendStyleDiagnostics(result, result.UseStyle, "host");
        result.PaintContext = new SvgUseResolver().CreatePaintContext(result.UseStyle, null);
        SvgReferenceResolveOptions referenceOptions = new SvgUseResolver().CopyReferenceOptions(options.ReferenceOptions);
        referenceOptions.RequireReference = true;
        result.Reference = new SvgReferenceResolver().ResolveWithNodeAndOptions(node, referenceOptions);
        new SvgUseResolver().AppendReferenceDiagnostics(result, result.Reference, "host");
        if (!result.Reference.Success || result.Reference.Status != SvgReferenceStatus.Resolved || result.Reference.Target == null) return result;
        if (!new SvgUseResolver().ValidTarget(result.Reference.Target))
        {
            new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "A use reference must identify an SVG or HTML-namespaced element.", result.Reference.TargetSource, "host");
            return result;
        }
        if (new SvgUseResolver().IsAncestor(result.Reference.Target, node))
        {
            new SvgUseResolver().AddCycle(result, result.Reference, "host");
            return result;
        }

        System.Collections.Generic.List<SvgNode> active = new System.Collections.Generic.List<SvgNode>();
        active.Add(result.Reference.Target);
        System.Collections.Generic.List<SvgUsePending> stack = new System.Collections.Generic.List<SvgUsePending>();
        stack.Add(new SvgUsePending(result.Reference.Target, new SvgUseResolver().CloneShadowNode(result.Reference.Target), null, "0", result.UseStyle, new SvgUseResolver().CopyContext(options.LengthContext), result.PaintContext, active, 1, true, false, node));
        int referenceCount = 1;
        while (stack.Count != 0)
        {
            SvgUsePending pending = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            if (result.Instances.Count >= options.Limits.MaxUseInstances)
            {
                new SvgUseResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Use expansion exceeded MaxUseInstances.", pending.Source.SourceSpan, pending.Path);
                break;
            }
            SvgUseInstance instance = new SvgUseResolver().CreateInstance(pending, node, options, result);
            if (instance == null) break;
            if (pending.Parent == null) result.Root = instance;
            else if (pending.ShadowAttachment) pending.Parent.ShadowRoot = instance;
            else pending.Parent.Children.Add(instance);
            result.Instances.Add(instance);
            result.SourceMap.Add(new SvgUseSourceMapEntry(instance.InstancePath, instance.SourceNode, node, instance.ShadowHostSource));

            if (instance.IsInstanceRoot)
            {
                SvgNode host = instance.ShadowHostSource;
                SvgComputedStyle? hostStyle = pending.ParentStyle;
                SvgUseViewport? viewport = new SvgUseResolver().ResolvePlacement(host, (instance.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), hostStyle, instance.ComputedStyle, pending.LengthContext, options, result, instance.InstancePath, instance);
                instance.Viewport = viewport;
                if (instance.LengthContext == null) instance.LengthContext = viewport == null ? new SvgUseResolver().CopyContext(pending.LengthContext) : viewport.LengthContext;
                instance.Renderable = viewport == null || viewport.Renderable;
                if (pending.Parent == null)
                {
                    result.Viewport = viewport;
                    result.Transform = instance.Transform;
                }
            }
            else instance.LengthContext = new SvgUseResolver().CopyContext(pending.LengthContext);

            int availableChildren = options.Limits.MaxUseInstances - result.Instances.Count - stack.Count;
            if (availableChildren < 0) availableChildren = 0;
            int scheduledChildren = (instance.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count;
            if (scheduledChildren > availableChildren) scheduledChildren = availableChildren;
            if (scheduledChildren < instance.SourceNode.Children.Count)
            {
                string omittedPath = instance.InstancePath + "/" + scheduledChildren.ToString(System.Globalization.CultureInfo.InvariantCulture);
                SvgNode omitted = instance.SourceNode.Children[scheduledChildren];
                new SvgUseResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Use expansion exceeded MaxUseInstances.", omitted.SourceSpan, omittedPath);
            }
            int childIndex = scheduledChildren - 1;
            while (childIndex >= 0)
            {
                SvgNode child = instance.SourceNode.Children[childIndex];
                SvgNode shadowChild = new SvgUseResolver().CloneShadowNode(child);
                shadowChild.Parent = pending.ShadowNode;
                pending.ShadowNode.Children.Insert(0, shadowChild);
                string childPath = instance.InstancePath + "/" + childIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
                stack.Add(new SvgUsePending(child, shadowChild, instance, childPath, instance.ComputedStyle, instance.LengthContext, instance.PaintContext, pending.ActiveTargets, pending.ReferenceDepth, false, false, pending.ShadowHostSource));
                childIndex = childIndex - 1;
            }

            if (instance.SourceNode.Kind == SvgNodeKind.Element && instance.SourceNode.ElementKind == SvgElementKind.Use)
            {
                if (referenceCount >= options.Limits.MaxCount)
                {
                    new SvgUseResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Use expansion exceeded the reference MaxCount.", instance.SourceNode.SourceSpan, instance.InstancePath);
                    continue;
                }
                referenceCount = referenceCount + 1;
                SvgReferenceResult nestedReference = new SvgReferenceResolver().ResolveWithNodeAndOptions(instance.SourceNode, referenceOptions);
                instance.Reference = nestedReference;
                new SvgUseResolver().AppendReferenceDiagnostics(result, nestedReference, instance.InstancePath);
                if (!nestedReference.Success || nestedReference.Status != SvgReferenceStatus.Resolved || nestedReference.Target == null) continue;
                if (!new SvgUseResolver().ValidTarget(nestedReference.Target))
                {
                    new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "A nested use reference must identify an SVG or HTML-namespaced element.", nestedReference.TargetSource, instance.InstancePath);
                    continue;
                }
                if (pending.ReferenceDepth >= options.Limits.MaxDepth)
                {
                    new SvgUseResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Use expansion exceeded the reference MaxDepth.", nestedReference.ReferenceSource, instance.InstancePath);
                    continue;
                }
                if (new SvgUseResolver().ContainsNode(pending.ActiveTargets, nestedReference.Target) || new SvgUseResolver().IsAncestor(nestedReference.Target, instance.SourceNode))
                {
                    new SvgUseResolver().AddCycle(result, nestedReference, instance.InstancePath);
                    continue;
                }
                System.Collections.Generic.List<SvgNode> nestedActive = new SvgUseResolver().CopyNodes(pending.ActiveTargets);
                nestedActive.Add(nestedReference.Target);
                SvgUsePaintContext nestedPaint = new SvgUseResolver().CreatePaintContext(instance.ComputedStyle, pending.PaintContext);
                string nestedPath = instance.InstancePath + "/shadow";
                if (result.Instances.Count + stack.Count >= options.Limits.MaxUseInstances)
                {
                    new SvgUseResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Use expansion exceeded MaxUseInstances.", nestedReference.TargetSource, nestedPath);
                }
                else stack.Add(new SvgUsePending(nestedReference.Target, new SvgUseResolver().CloneShadowNode(nestedReference.Target), instance, nestedPath, instance.ComputedStyle, instance.LengthContext, nestedPaint, nestedActive, pending.ReferenceDepth + 1, true, true, instance.SourceNode));
            }
        }
        result.Success = result.Root != null && !new SvgUseResolver().HasError(result.Diagnostics);
        return result;
    }
SvgUseInstance CreateInstance(SvgUsePending pending, SvgNode correspondingUse, SvgUseResolveOptions options, SvgUseResult result)
    {
        SvgUseInstance instance = new SvgUseInstance(pending.Path, pending.Source, pending.Parent, pending.InstanceRoot, correspondingUse, pending.ShadowHostSource);
        instance.PaintContext = pending.PaintContext;
        if (pending.Source.Kind == SvgNodeKind.Element)
        {
            instance.ComputedStyle = new SvgUseResolver().InstanceStyle(pending.ShadowNode, pending.ParentStyle, options);
            new SvgUseResolver().AppendStyleDiagnostics(result, instance.ComputedStyle, pending.Path);
        }
        else instance.ComputedStyle = pending.ParentStyle;
        return instance;
    }
SvgComputedStyle? HostStyle(SvgNode node, SvgUseResolveOptions options)
    {
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node)) return options.ComputedStyle;
        SvgStyleResolver resolver = new SvgStyleResolver();
        return resolver.ResolveWithOptions(node, options.StyleSheets, options.StyleOptions);
    }
SvgComputedStyle InstanceStyle(SvgNode node, SvgComputedStyle? parent, SvgUseResolveOptions options)
    {
        SvgCascadeOptions cascadeOptions = new SvgCascadeOptions();
        cascadeOptions.Properties = options.StyleOptions.Properties;
        cascadeOptions.Limits = options.StyleOptions.Limits;
        cascadeOptions.SelectorOptions = options.StyleOptions.SelectorOptions;
        cascadeOptions.StyleEnvironment = options.StyleOptions.StyleEnvironment;
        cascadeOptions.Parent = parent == null ? null : parent.Cascade;
        cascadeOptions.IncludeUserAgentDefaults = options.StyleOptions.IncludeUserAgentDefaults;
        SvgCascadeResult cascade = new SvgCascade().ResolveWithNodeAndStyleSheetsAndOptions(node, options.StyleSheets, cascadeOptions);
        SvgComputedStyleOptions computedOptions = new SvgComputedStyleOptions();
        computedOptions.Properties = options.StyleOptions.Properties;
        computedOptions.Limits = options.StyleOptions.Limits;
        computedOptions.Parent = parent;
        computedOptions.MaxPaintFallbacks = options.StyleOptions.MaxPaintFallbacks;
        return new SvgComputedStyleConverter().ConvertWithNodeAndCascadeAndOptions(node, cascade, computedOptions);
    }
SvgUsePaintContext CreatePaintContext(SvgComputedStyle? host, SvgUsePaintContext? enclosing)
    {
        SvgPaint? fill = host == null ? null : host.GetPaint(SvgCssPropertyNames.Fill);
        SvgPaint? stroke = host == null ? null : host.GetPaint(SvgCssPropertyNames.Stroke);
        if (enclosing != null)
        {
            fill = enclosing.Resolve(fill);
            stroke = enclosing.Resolve(stroke);
        }
        return new SvgUsePaintContext(host, fill, stroke);
    }
SvgUseViewport? ResolvePlacement(SvgNode host, SvgNode target, SvgComputedStyle? hostStyle, SvgComputedStyle? targetStyle, SvgLengthContext containing, SvgUseResolveOptions options, SvgUseResult result, string path, SvgUseInstance instance)
    {
        SvgUseDimension xDimension = new SvgUseResolver().ReadDimension(host, SvgCssPropertyNames.X, hostStyle, options, result, path);
        SvgUseDimension yDimension = new SvgUseResolver().ReadDimension(host, SvgCssPropertyNames.Y, hostStyle, options, result, path);
        SvgLengthEvaluationResult x = new SvgUseResolver().Evaluate(xDimension, containing, SvgLengthPercentageBasis.ViewportWidth, 0);
        SvgLengthEvaluationResult y = new SvgUseResolver().Evaluate(yDimension, containing, SvgLengthPercentageBasis.ViewportHeight, 0);
        if (!x.Success) new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The use x value could not be evaluated.", xDimension.Source, path);
        if (!y.Success) new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The use y value could not be evaluated.", yDimension.Source, path);
        if (!x.Success || !y.Success) return null;
        instance.Transform = new SvgUseResolver().Translation(x.Value, y.Value);
        instance.LengthContext = new SvgUseResolver().CopyContext(containing);
        if (instance.Parent == null)
        {
            result.X = x.Value;
            result.Y = y.Value;
        }
        if (target.ElementKind != SvgElementKind.Symbol && target.ElementKind != SvgElementKind.Svg) return null;

        SvgUseViewport viewport = new SvgUseViewport(target);
        SvgUseDimension hostWidth = new SvgUseResolver().ReadDimension(host, SvgCssPropertyNames.Width, hostStyle, options, result, path);
        SvgUseDimension hostHeight = new SvgUseResolver().ReadDimension(host, SvgCssPropertyNames.Height, hostStyle, options, result, path);
        SvgUseDimension targetWidth = new SvgUseResolver().ReadDimension(target, SvgCssPropertyNames.Width, targetStyle, options, result, path);
        SvgUseDimension targetHeight = new SvgUseResolver().ReadDimension(target, SvgCssPropertyNames.Height, targetStyle, options, result, path);
        SvgUseDimension usedWidth = hostWidth.Auto ? targetWidth : hostWidth;
        SvgUseDimension usedHeight = hostHeight.Auto ? targetHeight : hostHeight;
        viewport.WidthOverridden = !hostWidth.Auto;
        viewport.HeightOverridden = !hostHeight.Auto;
        SvgLengthEvaluationResult width = new SvgUseResolver().Evaluate(usedWidth, containing, SvgLengthPercentageBasis.ViewportWidth, containing.HasViewport ? containing.ViewportWidth : 0);
        SvgLengthEvaluationResult height = new SvgUseResolver().Evaluate(usedHeight, containing, SvgLengthPercentageBasis.ViewportHeight, containing.HasViewport ? containing.ViewportHeight : 0);
        if (!width.Success) new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The use instance width could not be evaluated.", usedWidth.Source, path);
        if (!height.Success) new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The use instance height could not be evaluated.", usedHeight.Source, path);
        if (!width.Success || !height.Success) return viewport;
        if (width.Value < 0 || height.Value < 0)
        {
            new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A use instance width or height cannot be negative.", width.Value < 0 ? usedWidth.Source : usedHeight.Source, path);
            return viewport;
        }
        viewport.Width = width.Value;
        viewport.Height = height.Value;

        double targetX = 0;
        double targetY = 0;
        SvgUseDimension targetXDimension = new SvgUseResolver().ReadDimension(target, SvgCssPropertyNames.X, targetStyle, options, result, path);
        SvgUseDimension targetYDimension = new SvgUseResolver().ReadDimension(target, SvgCssPropertyNames.Y, targetStyle, options, result, path);
        SvgLengthEvaluationResult evaluatedTargetX = new SvgUseResolver().Evaluate(targetXDimension, containing, SvgLengthPercentageBasis.ViewportWidth, 0);
        SvgLengthEvaluationResult evaluatedTargetY = new SvgUseResolver().Evaluate(targetYDimension, containing, SvgLengthPercentageBasis.ViewportHeight, 0);
        if (!evaluatedTargetX.Success) new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The referenced viewport x value could not be evaluated.", targetXDimension.Source, path);
        if (!evaluatedTargetY.Success) new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The referenced viewport y value could not be evaluated.", targetYDimension.Source, path);
        if (evaluatedTargetX.Success) targetX = evaluatedTargetX.Value;
        if (evaluatedTargetY.Success) targetY = evaluatedTargetY.Value;
        viewport.X = targetX;
        viewport.Y = targetY;
        viewport.Bounds = SvgRect.Create(x.Value + targetX, y.Value + targetY, viewport.Width, viewport.Height, viewport.Width == 0 || viewport.Height == 0);

        SvgTypedAttributeValue viewBox = options.Attributes.Get(target, SvgAttributeNames.ViewBox);
        new SvgUseResolver().AppendTypedDiagnostics(result, viewBox, path);
        if (viewBox.Success && viewBox.HasValue && viewBox.ValueKind == SvgTypedAttributeKind.ViewBox) viewport.ViewBox = (SvgViewBox?)viewBox.Value;
        SvgTypedAttributeValue aspect = options.Attributes.Get(target, SvgAttributeNames.PreserveAspectRatio);
        new SvgUseResolver().AppendTypedDiagnostics(result, aspect, path);
        if (aspect.Success && aspect.HasValue && aspect.ValueKind == SvgTypedAttributeKind.AspectRatio) viewport.AspectRatio = ((SvgAspectRatio?)aspect.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));

        SvgMatrix baseTransform = new SvgMatrix(1, 0, 0, 1, x.Value + targetX, y.Value + targetY);
        double userX = 0;
        double userY = 0;
        double userWidth = viewport.Width;
        double userHeight = viewport.Height;
        viewport.Renderable = viewport.Width != 0 && viewport.Height != 0;
        if (viewport.ViewBox != null)
        {
            if (viewport.ViewBox.Width == 0 || viewport.ViewBox.Height == 0) viewport.Renderable = false;
            else
            {
                SvgViewBoxFitResult fit = (viewport.AspectRatio ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Fit(viewport.ViewBox, targetX, targetY, viewport.Width, viewport.Height);
                if (!fit.Success)
                {
                    new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The use instance viewBox could not be fitted.", target.SourceSpan, path);
                    return viewport;
                }
                SvgMatrix? adjusted = new SvgUseResolver().AdjustReferencePoint(target, fit.Matrix, targetX, targetY, viewport, options, result, path);
                SvgMatrixOperationResult translated = new SvgUseResolver().Translation(x.Value, y.Value).Multiply((adjusted ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                if (!translated.Success)
                {
                    new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The use instance transform was not finite.", target.SourceSpan, path);
                    return viewport;
                }
                baseTransform = (translated.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                userWidth = viewport.Width / System.Math.Abs((fit.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).A);
                userHeight = viewport.Height / System.Math.Abs(fit.Matrix.D);
                userX = (targetX - adjusted.E) / adjusted.A;
                userY = (targetY - adjusted.F) / adjusted.D;
            }
        }
        else if (target.ElementKind == SvgElementKind.Symbol)
        {
            SvgMatrix targetTransform = new SvgUseResolver().Translation(targetX, targetY);
            SvgMatrix? adjusted = new SvgUseResolver().AdjustReferencePoint(target, targetTransform, targetX, targetY, viewport, options, result, path);
            SvgMatrixOperationResult translated = new SvgUseResolver().Translation(x.Value, y.Value).Multiply((adjusted ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            if (!translated.Success)
            {
                new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The symbol reference-point transform was not finite.", target.SourceSpan, path);
                return viewport;
            }
            baseTransform = (translated.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        viewport.Transform = (baseTransform ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        viewport.LengthContext = new SvgUseResolver().CopyContext(containing);
        viewport.LengthContext.HasViewport = true;
        viewport.LengthContext.ViewportWidth = userWidth;
        viewport.LengthContext.ViewportHeight = userHeight;
        viewport.ClipsOverflow = true;
        viewport.ClipRect = SvgRect.Create(x.Value + targetX, y.Value + targetY, viewport.Width, viewport.Height, !viewport.Renderable);
        if (viewport.ViewBox == null)
        {
            userX = 0;
            userY = 0;
        }
        if (!SvgFiniteMath.IsFinite(userX) || !SvgFiniteMath.IsFinite(userY))
        {
            new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The use instance viewport produced non-finite user bounds.", target.SourceSpan, path);
        }
        instance.Transform = (viewport.Transform ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        instance.LengthContext = viewport.LengthContext;
        return viewport;
    }
SvgMatrix? AdjustReferencePoint(SvgNode target, SvgMatrix? matrix, double viewportX, double viewportY, SvgUseViewport viewport, SvgUseResolveOptions options, SvgUseResult result, string path)
    {
        if (target.ElementKind != SvgElementKind.Symbol) return matrix;
        SvgAttribute? authoredX = new SvgUseResolver().FindAttribute(target, SvgAttributeNames.RefX);
        SvgAttribute? authoredY = new SvgUseResolver().FindAttribute(target, SvgAttributeNames.RefY);
        if (authoredX == null && authoredY == null) return matrix;
        SvgLengthContext context = new SvgUseResolver().CopyContext(options.LengthContext);
        context.HasViewport = true;
        context.ViewportWidth = viewport.Width / System.Math.Abs((matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).A);
        context.ViewportHeight = viewport.Height / System.Math.Abs(matrix.D);
        double refX = 0;
        double refY = 0;
        if (authoredX != null)
        {
            SvgTypedAttributeValue typedX = options.Attributes.Get(target, SvgAttributeNames.RefX);
            new SvgUseResolver().AppendTypedDiagnostics(result, typedX, path);
            if (typedX.Success && typedX.ValueKind == SvgTypedAttributeKind.Length)
            {
                SvgLengthEvaluationResult? evaluatedX = (((SvgLength?)typedX.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))).Evaluate(context, SvgLengthPercentageBasis.ViewportWidth);
                if (evaluatedX.Success) refX = evaluatedX.Value;
                else new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The symbol refX value could not be evaluated.", authoredX.SourceSpan, path);
            }
        }
        if (authoredY != null)
        {
            SvgTypedAttributeValue typedY = options.Attributes.Get(target, SvgAttributeNames.RefY);
            new SvgUseResolver().AppendTypedDiagnostics(result, typedY, path);
            if (typedY.Success && typedY.ValueKind == SvgTypedAttributeKind.Length)
            {
                SvgLengthEvaluationResult? evaluatedY = (((SvgLength?)typedY.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))).Evaluate(context, SvgLengthPercentageBasis.ViewportHeight);
                if (evaluatedY.Success) refY = evaluatedY.Value;
                else new SvgUseResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The symbol refY value could not be evaluated.", authoredY.SourceSpan, path);
            }
        }
        SvgPointTransformResult point = matrix.TransformPoint(new SvgPoint(refX, refY));
        if (!point.Success) return matrix;
        double adjustX = authoredX == null ? 0 : viewportX - (point.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        double adjustY = authoredY == null ? 0 : viewportY - (point.Point ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Y;
        SvgMatrixOperationResult adjusted = new SvgUseResolver().Translation(adjustX, adjustY).Multiply(matrix);
        return adjusted.Success ? adjusted.Matrix : matrix;
    }
SvgUseDimension ReadDimension(SvgNode node, string name, SvgComputedStyle? style, SvgUseResolveOptions options, SvgUseResult result, string path)
    {
        SvgAttribute? authored = new SvgUseResolver().FindAttribute(node, name);
        if (style != null)
        {
            SvgComputedValue? computed = style.Find(name);
            if (computed != null && computed.Kind == SvgComputedValueKind.Length && computed.Length != null) return new SvgUseDimension(true, false, authored != null, computed.Length, computed.Source == null && authored != null ? authored.SourceSpan : computed.Source);
            if (computed != null && computed.Kind == SvgComputedValueKind.Keyword && string.Equals(computed.Keyword, "auto", System.StringComparison.OrdinalIgnoreCase)) return new SvgUseDimension(true, true, authored != null, null, computed.Source == null && authored != null ? authored.SourceSpan : computed.Source);
        }
        SvgTypedAttributeValue typed = options.Attributes.Get(node, name);
        new SvgUseResolver().AppendTypedDiagnostics(result, typed, path);
        SvgSourceSpan? source = typed.SourceAttribute == null ? node.SourceSpan : typed.SourceAttribute.SourceSpan;
        if (!typed.Success) return new SvgUseDimension(false, false, authored != null, null, source);
        if (!typed.HasValue || typed.ValueKind == SvgTypedAttributeKind.ContextDependent) return new SvgUseDimension(true, true, authored != null, null, source);
        if (typed.ValueKind == SvgTypedAttributeKind.Keyword && string.Equals((string?)typed.Value, "auto", System.StringComparison.OrdinalIgnoreCase)) return new SvgUseDimension(true, true, authored != null, null, source);
        if (typed.ValueKind == SvgTypedAttributeKind.Length) return new SvgUseDimension(true, false, authored != null, (SvgLength?)typed.Value, source);
        return new SvgUseDimension(false, false, authored != null, null, source);
    }
SvgLengthEvaluationResult Evaluate(SvgUseDimension? dimension, SvgLengthContext context, SvgLengthPercentageBasis basis, double autoValue)
    {
        if (dimension == null || !dimension.Success) return new SvgLengthEvaluationResult(false, SvgLengthEvaluationError.InvalidArgument, 0);
        if (dimension.Auto)
        {
            if (!context.HasViewport) return new SvgLengthEvaluationResult(false, SvgLengthEvaluationError.MissingViewport, 0);
            return new SvgLengthEvaluationResult(true, SvgLengthEvaluationError.None, autoValue);
        }
        return (dimension.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Evaluate(context, basis);
    }
bool ValidOptions(SvgUseResolveOptions options)
    {
        return options.Attributes != null && options.LengthContext != null && options.ReferenceOptions != null && options.Limits != null && options.StyleSheets != null && options.StyleOptions != null && options.StyleOptions.Properties != null && options.StyleOptions.Limits != null && options.Limits.MaxDepth > 0 && options.Limits.MaxCount > 0 && options.Limits.MaxUseInstances > 0;
    }
bool ValidTarget(SvgNode? target)
    {
        if (target == null || target.Kind != SvgNodeKind.Element) return false;
        return target.NamespaceUri == SvgNamespaces.Svg || target.NamespaceUri == SvgNamespaces.XHtml;
    }
bool IsAncestor(SvgNode possibleAncestor, SvgNode node)
    {
        SvgNode? current = node;
        while (current != null)
        {
            if (object.ReferenceEquals(current, possibleAncestor)) return true;
            current = current.Parent;
        }
        return false;
    }
bool ContainsNode(System.Collections.Generic.List<SvgNode> nodes, SvgNode candidate)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], candidate)) return true;
            index = index + 1;
        }
        return false;
    }
System.Collections.Generic.List<SvgNode> CopyNodes(System.Collections.Generic.List<SvgNode> source)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }
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
        result.HasXHeight = source.HasXHeight;
        result.XHeight = source.XHeight;
        result.HasZeroAdvance = source.HasZeroAdvance;
        result.ZeroAdvance = source.ZeroAdvance;
        result.HasRootFontSize = source.HasRootFontSize;
        result.RootFontSize = source.RootFontSize;
        result.HasObjectBoundingBox = source.HasObjectBoundingBox;
        result.ObjectBoundingBoxWidth = source.ObjectBoundingBoxWidth;
        result.ObjectBoundingBoxHeight = source.ObjectBoundingBoxHeight;
        return result;
    }
SvgReferenceResolveOptions CopyReferenceOptions(SvgReferenceResolveOptions source)
    {
        SvgReferenceResolveOptions result = new SvgReferenceResolveOptions();
        result.RequireReference = source.RequireReference;
        result.ExternalDocument = source.ExternalDocument;
        result.MaxUriCharacters = source.MaxUriCharacters;
        result.MaxBaseDepth = source.MaxBaseDepth;
        result.NestedDocumentOptions = source.NestedDocumentOptions;
        return result;
    }
SvgMatrix Translation(double x, double y)
    {
        return new SvgMatrix(1, 0, 0, 1, x, y);
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
SvgNode CloneShadowNode(SvgNode source)
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
void AppendReferenceDiagnostics(SvgUseResult result, SvgReferenceResult reference, string path)
    {
        int index = 0;
        while (index < reference.Diagnostics.Count)
        {
            new SvgUseResolver().AddDiagnosticCopy(result, reference.Diagnostics[index], path);
            index = index + 1;
        }
    }
void AppendStyleDiagnostics(SvgUseResult result, SvgComputedStyle? style, string path)
    {
        if (style == null) return;
        int index = 0;
        while (index < style.Diagnostics.Count)
        {
            new SvgUseResolver().AddDiagnosticCopy(result, style.Diagnostics[index], path);
            index = index + 1;
        }
    }
void AppendTypedDiagnostics(SvgUseResult result, SvgTypedAttributeValue typed, string path)
    {
        int index = 0;
        while (index < typed.Diagnostics.Count)
        {
            new SvgUseResolver().AddDiagnosticCopy(result, typed.Diagnostics[index], path);
            index = index + 1;
        }
    }
void AddDiagnosticCopy(SvgUseResult result, SvgDiagnostic source, string path)
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(source.Severity, source.Code, source.Message, source.Source);
        diagnostic.ResourcePath = source.ResourcePath;
        diagnostic.InstancePath = path;
        int index = 0;
        while (index < source.RelatedSpans.Count)
        {
            diagnostic.RelatedSpans.Add(source.RelatedSpans[index]);
            index = index + 1;
        }
        result.Diagnostics.Add(diagnostic);
    }
void AddCycle(SvgUseResult result, SvgReferenceResult reference, string path)
    {
        SvgSourceSpan? source = reference.ReferenceSource == null ? (reference.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan : reference.ReferenceSource;
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ReferenceCycle, "A use-element shadow tree would create a recursive reference.", source);
        diagnostic.InstancePath = path;
        if (reference.TargetSource != null && !object.ReferenceEquals(reference.TargetSource, source)) diagnostic.RelatedSpans.Add(reference.TargetSource);
        result.Diagnostics.Add(diagnostic);
    }
void AddError(SvgUseResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source, string path)
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source);
        diagnostic.InstancePath = path;
        result.Diagnostics.Add(diagnostic);
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
