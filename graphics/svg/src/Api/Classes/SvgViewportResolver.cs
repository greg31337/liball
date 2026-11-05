namespace svg;

/// <summary>Establishes root and nested SVG viewports from typed attributes and computed style.</summary>
public class SvgViewportResolver : ISvgViewportResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgViewportResolver()
    {
    }

    /// <summary>Resolves a root viewport against explicit host viewport dimensions.</summary>
    /// <param name="node">The root SVG element.</param>
    /// <param name="hostWidth">The nonnegative host viewport width.</param>
    /// <param name="hostHeight">The nonnegative host viewport height.</param>
    /// <returns>The established root viewport or a located failure.</returns>
    public SvgViewportResult ResolveRoot(SvgNode node, double hostWidth, double hostHeight)
    {
        SvgViewportResolveOptions options = new SvgViewportResolveOptions();
        options.LengthContext.HasViewport = true;
        options.LengthContext.ViewportWidth = hostWidth;
        options.LengthContext.ViewportHeight = hostHeight;
        return new SvgViewportResolver().Resolve(node, options);
    }

    /// <summary>Resolves a nested SVG viewport using a containing viewport result.</summary>
    /// <param name="node">The nested SVG element.</param>
    /// <param name="parentViewport">The nearest containing SVG viewport.</param>
    /// <returns>The established nested viewport or a located failure.</returns>
    public SvgViewportResult ResolveNested(SvgNode node, SvgViewportResult parentViewport)
    {
        SvgViewportResolveOptions options = new SvgViewportResolveOptions();
        options.ParentViewport = parentViewport;
        return new SvgViewportResolver().Resolve(node, options);
    }

    /// <summary>Resolves one SVG element viewport with explicit context, style, and fragment options.</summary>
    /// <param name="node">The SVG element that establishes the viewport.</param>
    /// <param name="options">The containing context and optional computed inputs.</param>
    /// <returns>The established viewport or a located failure.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an SVG <c>svg</c> element.</exception>
    public SvgViewportResult Resolve(SvgNode node, SvgViewportResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        if (node.Kind != SvgNodeKind.Element || node.ElementKind != SvgElementKind.Svg) throw new System.ArgumentOutOfRangeException("node");
        SvgViewportResult result = new SvgViewportResult(node);
        result.IsRoot = new SvgViewportResolver().IsRoot(node);
        result.FragmentIdentifier = options.FragmentIdentifier == null ? "" : options.FragmentIdentifier;
        if (!new SvgViewportResolver().ValidOptions(options))
        {
            new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Viewport resolution options are invalid.", node.SourceSpan);
            return result;
        }

        SvgLengthContext containing = new SvgViewportResolver().ContainingContext(result.IsRoot, options);
        SvgViewportDimension x = result.IsRoot ? new SvgViewportResolver().ConstantDimension(0) : new SvgViewportResolver().ReadDimension(node, SvgCssPropertyNames.X, options, result);
        SvgViewportDimension y = result.IsRoot ? new SvgViewportResolver().ConstantDimension(0) : new SvgViewportResolver().ReadDimension(node, SvgCssPropertyNames.Y, options, result);
        SvgViewportDimension width = new SvgViewportResolver().ReadDimension(node, SvgCssPropertyNames.Width, options, result);
        SvgViewportDimension height = new SvgViewportResolver().ReadDimension(node, SvgCssPropertyNames.Height, options, result);
        if (!x.Success || !y.Success || !width.Success || !height.Success || new SvgViewportResolver().HasError(result.Diagnostics)) return result;

        SvgLengthEvaluationResult evaluatedX = new SvgViewportResolver().EvaluateDimension(x, containing, SvgLengthPercentageBasis.ViewportWidth, 0);
        SvgLengthEvaluationResult evaluatedY = new SvgViewportResolver().EvaluateDimension(y, containing, SvgLengthPercentageBasis.ViewportHeight, 0);
        SvgLengthEvaluationResult evaluatedWidth = new SvgViewportResolver().EvaluateDimension(width, containing, SvgLengthPercentageBasis.ViewportWidth, containing.ViewportWidth);
        SvgLengthEvaluationResult evaluatedHeight = new SvgViewportResolver().EvaluateDimension(height, containing, SvgLengthPercentageBasis.ViewportHeight, containing.ViewportHeight);
        if (!evaluatedX.Success) new SvgViewportResolver().AddLengthError(result, "x", x.Source);
        if (!evaluatedY.Success) new SvgViewportResolver().AddLengthError(result, "y", y.Source);
        if (!evaluatedWidth.Success) new SvgViewportResolver().AddLengthError(result, "width", width.Source);
        if (!evaluatedHeight.Success) new SvgViewportResolver().AddLengthError(result, "height", height.Source);
        if (new SvgViewportResolver().HasError(result.Diagnostics)) return result;
        if (evaluatedWidth.Value < 0 || evaluatedHeight.Value < 0)
        {
            SvgSourceSpan? source = evaluatedWidth.Value < 0 ? width.Source : height.Source;
            new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "An SVG viewport width or height cannot be negative.", source == null ? node.SourceSpan : source);
            return result;
        }

        result.X = result.IsRoot ? 0 : evaluatedX.Value;
        result.Y = result.IsRoot ? 0 : evaluatedY.Value;
        result.Width = evaluatedWidth.Value;
        result.Height = evaluatedHeight.Value;
        result.Bounds = SvgRect.Create(result.X, result.Y, result.Width, result.Height, result.Width == 0 || result.Height == 0);

        SvgTypedAttributeValue viewBoxValue = options.Attributes.Get(node, SvgAttributeNames.ViewBox);
        new SvgViewportResolver().AppendTyped(viewBoxValue, result);
        if (viewBoxValue.Success && viewBoxValue.HasValue) result.ViewBox = (SvgViewBox?)viewBoxValue.Value;
        SvgTypedAttributeValue aspectValue = options.Attributes.Get(node, SvgAttributeNames.PreserveAspectRatio);
        new SvgViewportResolver().AppendTyped(aspectValue, result);
        if (aspectValue.Success && aspectValue.HasValue) result.AspectRatio = ((SvgAspectRatio?)aspectValue.Value ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        if (new SvgViewportResolver().HasError(result.Diagnostics)) return result;

        SvgViewportFragmentOverride fragment = new SvgViewportFragmentOverride();
        if (result.IsRoot && result.FragmentIdentifier.Length != 0)
        {
            fragment = new SvgViewportResolver().ResolveFragment(node, result.FragmentIdentifier, result.Width, result.Height, options, result);
            result.FragmentKind = fragment.Kind;
            result.FragmentTarget = fragment.Target;
            result.ViewSpecificationNode = fragment.ViewNode;
            result.FragmentTransform = fragment.Transform;
            if (!fragment.Success || new SvgViewportResolver().HasError(result.Diagnostics)) return result;
            if (fragment.HasViewBox) result.ViewBox = fragment.ViewBox;
            if (fragment.HasAspectRatio) result.AspectRatio = (fragment.AspectRatio ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }

        SvgMatrix baseTransform = new SvgMatrix(1, 0, 0, 1, result.X, result.Y);
        double userWidth = result.Width;
        double userHeight = result.Height;
        double userX = 0;
        double userY = 0;
        result.Renderable = result.Width > 0 && result.Height > 0;
        if (result.ViewBox != null)
        {
            if (result.ViewBox.Width == 0 || result.ViewBox.Height == 0)
            {
                result.Renderable = false;
            }
            else
            {
                SvgViewBoxFitResult fitted = (result.AspectRatio ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Fit(result.ViewBox, result.X, result.Y, result.Width, result.Height);
                if (!fitted.Success)
                {
                    new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The SVG viewBox could not be fitted to its viewport.", node.SourceSpan);
                    return result;
                }
                baseTransform = (fitted.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                userWidth = result.Width / System.Math.Abs((baseTransform ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).A);
                userHeight = result.Height / System.Math.Abs(baseTransform.D);
                userX = (result.X - baseTransform.E) / baseTransform.A;
                userY = (result.Y - baseTransform.F) / baseTransform.D;
            }
        }
        result.UserBounds = SvgRect.Create(userX, userY, userWidth, userHeight, !result.Renderable);
        result.LengthContext = new SvgViewportResolver().CopyContext(containing);
        result.LengthContext.HasViewport = true;
        result.LengthContext.ViewportWidth = userWidth;
        result.LengthContext.ViewportHeight = userHeight;

        result.Transform = baseTransform;
        if (fragment.Transform != null)
        {
            SvgTransformMatrixResult fragmentMatrix = fragment.Transform.ToMatrix();
            if (!fragmentMatrix.Success)
            {
                new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The SVG view fragment transform could not be composed.", node.SourceSpan);
                return result;
            }
            SvgMatrixOperationResult combined = (fragmentMatrix.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Multiply(baseTransform);
            if (!combined.Success)
            {
                new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The SVG view fragment transform produced a non-finite mapping.", node.SourceSpan);
                return result;
            }
            result.Transform = (combined.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }

        result.ClipsOverflow = new SvgViewportResolver().ClipsOverflow(node, result.IsRoot, options, result);
        if (result.ClipsOverflow) result.ClipRect = SvgRect.Create(result.X, result.Y, result.Width, result.Height, result.Width == 0 || result.Height == 0);
        if (new SvgViewportResolver().HasError(result.Diagnostics)) return result;
        result.Success = true;
        return result;
    }
bool ValidOptions(SvgViewportResolveOptions options)
    {
        if (options.Attributes == null || options.LengthContext == null) return false;
        if (!SvgFiniteMath.IsFinite(options.DefaultRootWidth) || !SvgFiniteMath.IsFinite(options.DefaultRootHeight) || options.DefaultRootWidth < 0 || options.DefaultRootHeight < 0) return false;
        if (options.ParentViewport != null && (!options.ParentViewport.Success || options.ParentViewport.LengthContext == null)) return false;
        return new SvgViewportResolver().ValidContext(options.LengthContext);
    }
bool ValidContext(SvgLengthContext context)
    {
        if (context.HasViewport && (!SvgFiniteMath.IsFinite(context.ViewportWidth) || !SvgFiniteMath.IsFinite(context.ViewportHeight) || context.ViewportWidth < 0 || context.ViewportHeight < 0)) return false;
        if (context.HasFontSize && (!SvgFiniteMath.IsFinite(context.FontSize) || context.FontSize < 0)) return false;
        if (context.HasXHeight && (!SvgFiniteMath.IsFinite(context.XHeight) || context.XHeight < 0)) return false;
        if (context.HasZeroAdvance && (!SvgFiniteMath.IsFinite(context.ZeroAdvance) || context.ZeroAdvance < 0)) return false;
        if (context.HasRootFontSize && (!SvgFiniteMath.IsFinite(context.RootFontSize) || context.RootFontSize < 0)) return false;
        return true;
    }
bool IsRoot(SvgNode node)
    {
        if (node.OwnerDocument != null) return object.ReferenceEquals(node.OwnerDocument.DocumentElement, node);
        return node.Parent == null || node.Parent.Kind == SvgNodeKind.Document;
    }
SvgLengthContext ContainingContext(bool root, SvgViewportResolveOptions options)
    {
        SvgLengthContext context = new SvgViewportResolver().CopyContext(options.LengthContext);
        if (options.ParentViewport != null)
        {
            context.HasViewport = options.ParentViewport.LengthContext.HasViewport;
            context.ViewportWidth = options.ParentViewport.LengthContext.ViewportWidth;
            context.ViewportHeight = options.ParentViewport.LengthContext.ViewportHeight;
        }
        if (root && !context.HasViewport)
        {
            context.HasViewport = true;
            context.ViewportWidth = options.DefaultRootWidth;
            context.ViewportHeight = options.DefaultRootHeight;
        }
        return context;
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
SvgViewportDimension ConstantDimension(double value)
    {
        return new SvgViewportDimension(true, false, new SvgLength(value, SvgLengthUnit.Number), null);
    }
SvgViewportDimension ReadDimension(SvgNode node, string name, SvgViewportResolveOptions options, SvgViewportResult result)
    {
        SvgComputedStyle? style = options.ComputedStyle;
        if (style != null && object.ReferenceEquals(style.Node, node))
        {
            SvgComputedValue? computed = style.Find(name);
            if (computed != null && computed.Kind == SvgComputedValueKind.Length && computed.Length != null)
            {
                return new SvgViewportDimension(true, false, computed.Length, computed.Source);
            }
            if (computed != null && computed.Kind == SvgComputedValueKind.Keyword && string.Equals(computed.Keyword, "auto", System.StringComparison.OrdinalIgnoreCase))
            {
                return new SvgViewportDimension(true, true, null, computed.Source);
            }
        }

        SvgTypedAttributeValue typed = options.Attributes.Get(node, name);
        new SvgViewportResolver().AppendTyped(typed, result);
        SvgSourceSpan? source = typed.SourceAttribute == null ? node.SourceSpan : typed.SourceAttribute.SourceSpan;
        if (!typed.Success) return new SvgViewportDimension(false, false, null, source);
        if (!typed.HasValue || typed.ValueKind == SvgTypedAttributeKind.ContextDependent) return new SvgViewportDimension(true, true, null, source);
        if (typed.ValueKind == SvgTypedAttributeKind.Keyword && string.Equals((string?)typed.Value, "auto", System.StringComparison.OrdinalIgnoreCase)) return new SvgViewportDimension(true, true, null, source);
        if (typed.ValueKind == SvgTypedAttributeKind.Length) return new SvgViewportDimension(true, false, (SvgLength?)typed.Value, source);
        new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The SVG viewport " + name + " value is not a length or auto.", source == null ? node.SourceSpan : source);
        return new SvgViewportDimension(false, false, null, source);
    }
SvgLengthEvaluationResult EvaluateDimension(SvgViewportDimension dimension, SvgLengthContext context, SvgLengthPercentageBasis basis, double autoValue)
    {
        if (dimension.Auto)
        {
            if (!context.HasViewport) return new SvgLengthEvaluationResult(false, SvgLengthEvaluationError.MissingViewport, 0);
            return new SvgLengthEvaluationResult(true, SvgLengthEvaluationError.None, autoValue);
        }
        return (dimension.Length ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Evaluate(context, basis);
    }
void AddLengthError(SvgViewportResult result, string name, SvgSourceSpan? source)
    {
        new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The SVG viewport " + name + " value could not be resolved in the containing length context.", source == null ? (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan : source);
    }
void AppendTyped(SvgTypedAttributeValue typed, SvgViewportResult result)
    {
        int index = 0;
        while (index < typed.Diagnostics.Count)
        {
            result.Diagnostics.Add(typed.Diagnostics[index]);
            index = index + 1;
        }
    }
bool ClipsOverflow(SvgNode node, bool root, SvgViewportResolveOptions options, SvgViewportResult result)
    {
        SvgComputedStyle? style = options.ComputedStyle;
        if (style != null && object.ReferenceEquals(style.Node, node))
        {
            string x = style.GetKeyword(SvgCssPropertyNames.OverflowX);
            string y = style.GetKeyword(SvgCssPropertyNames.OverflowY);
            if (x.Length != 0 && y.Length != 0) return !new SvgViewportResolver().VisibleOverflow(x) || !new SvgViewportResolver().VisibleOverflow(y);
        }

        SvgAttribute? authored = new SvgViewportResolver().FindAttribute(node, "overflow");
        if (authored != null)
        {
            string[]? values = authored.Value.Split((char[]?)null, System.StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 1 && new SvgViewportResolver().OverflowKeyword(values[0])) return !new SvgViewportResolver().VisibleOverflow(values[0]);
            if (values.Length == 2 && new SvgViewportResolver().OverflowKeyword(values[0]) && new SvgViewportResolver().OverflowKeyword(values[1])) return !new SvgViewportResolver().VisibleOverflow(values[0]) || !new SvgViewportResolver().VisibleOverflow(values[1]);
            new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The SVG viewport overflow value is invalid.", authored.SourceSpan);
            return false;
        }
        return !root;
    }
bool OverflowKeyword(string value)
    {
        return string.Equals(value, "visible", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "hidden", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "clip", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "scroll", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "auto", System.StringComparison.OrdinalIgnoreCase);
    }
bool VisibleOverflow(string value)
    {
        return string.Equals(value, "visible", System.StringComparison.OrdinalIgnoreCase);
    }
SvgAttribute? FindAttribute(SvgNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }
SvgViewportFragmentOverride ResolveFragment(SvgNode root, string fragmentIdentifier, double viewportWidth, double viewportHeight, SvgViewportResolveOptions options, SvgViewportResult result)
    {
        SvgViewportFragmentOverride fragment = new SvgViewportFragmentOverride();
        string text = fragmentIdentifier;
        if (text.Length > 0 && text[0] == '#') text = text.Substring(1);
        if (text.Length == 0) return fragment;
        string[] combined = text.Split('&');
        string viewportPart = "";
        int combinedIndex = 0;
        while (combinedIndex < combined.Length)
        {
            string part = combined[combinedIndex].Trim();
            if (!part.StartsWith("t=", System.StringComparison.Ordinal) && part.Length != 0)
            {
                if (viewportPart.Length != 0)
                {
                    return new SvgViewportResolver().FragmentFailure(fragment, result, "The SVG fragment contains more than one viewport selector.", root.SourceSpan);
                }
                viewportPart = part;
            }
            combinedIndex = combinedIndex + 1;
        }
        if (viewportPart.Length == 0) return fragment;
        if (viewportPart.StartsWith("svgView(", System.StringComparison.Ordinal)) return new SvgViewportResolver().ParseSvgView(viewportPart, root, options, result);
        if (viewportPart.StartsWith("xywh=", System.StringComparison.Ordinal)) return new SvgViewportResolver().ParseMediaFragment(viewportPart, viewportWidth, viewportHeight, root, result);

        if (root.OwnerDocument == null)
        {
            return new SvgViewportResolver().FragmentFailure(fragment, result, "A bare SVG fragment target requires an owning document.", root.SourceSpan);
        }
        SvgNode? target = root.OwnerDocument.Index.GetById(viewportPart);
        if (target == null)
        {
            return new SvgViewportResolver().FragmentFailure(fragment, result, "The SVG fragment target was not found: " + viewportPart + ".", root.SourceSpan);
        }
        fragment.Target = target;
        fragment.Kind = SvgViewportFragmentKind.Element;
        if (target.ElementKind == SvgElementKind.View)
        {
            fragment.Kind = SvgViewportFragmentKind.ViewElement;
            fragment.ViewNode = target;
            new SvgViewportResolver().ApplyViewElement(fragment, target, options, result);
        }
        return fragment;
    }
void ApplyViewElement(SvgViewportFragmentOverride fragment, SvgNode view, SvgViewportResolveOptions options, SvgViewportResult result)
    {
        if (new SvgViewportResolver().FindAttribute(view, SvgAttributeNames.ViewBox) != null)
        {
            SvgTypedAttributeValue value = options.Attributes.Get(view, SvgAttributeNames.ViewBox);
            new SvgViewportResolver().AppendTyped(value, result);
            if (!value.Success) fragment.Success = false;
            else
            {
                fragment.HasViewBox = true;
                fragment.ViewBox = (SvgViewBox?)value.Value;
            }
        }
        if (new SvgViewportResolver().FindAttribute(view, SvgAttributeNames.PreserveAspectRatio) != null)
        {
            SvgTypedAttributeValue value = options.Attributes.Get(view, SvgAttributeNames.PreserveAspectRatio);
            new SvgViewportResolver().AppendTyped(value, result);
            if (!value.Success) fragment.Success = false;
            else
            {
                fragment.HasAspectRatio = true;
                fragment.AspectRatio = (SvgAspectRatio?)value.Value;
            }
        }
    }
SvgViewportFragmentOverride ParseSvgView(string text, SvgNode root, SvgViewportResolveOptions options, SvgViewportResult result)
    {
        SvgViewportFragmentOverride fragment = new SvgViewportFragmentOverride();
        fragment.Kind = SvgViewportFragmentKind.SvgView;
        if (text.Length < 10 || text[text.Length - 1] != ')') return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment is not closed.", root.SourceSpan);
        string content = text.Substring(8, text.Length - 9);
        string[] parts = content.Split(';');
        bool viewBoxSeen = false;
        bool aspectSeen = false;
        bool transformSeen = false;
        bool zoomSeen = false;
        int index = 0;
        while (index < parts.Length)
        {
            string part = parts[index].Trim();
            if (part.StartsWith("viewBox(", System.StringComparison.Ordinal) && new SvgViewportResolver().EndsWithClose(part))
            {
                if (viewBoxSeen) return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment repeats viewBox.", root.SourceSpan);
                viewBoxSeen = true;
                SvgViewBoxParseResult parsed = SvgViewBoxParser.ParseViewBox(new SvgViewportResolver().FunctionContent(part, 8), 4096);
                if (!parsed.Success) return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment has an invalid viewBox.", root.SourceSpan);
                fragment.HasViewBox = true;
                fragment.ViewBox = parsed.ViewBox;
            }
            else if (part.StartsWith("preserveAspectRatio(", System.StringComparison.Ordinal) && new SvgViewportResolver().EndsWithClose(part))
            {
                if (aspectSeen) return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment repeats preserveAspectRatio.", root.SourceSpan);
                aspectSeen = true;
                SvgAspectRatioParseResult parsed = SvgViewBoxParser.ParseAspectRatio(new SvgViewportResolver().FunctionContent(part, 20), 4096);
                if (!parsed.Success) return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment has an invalid preserveAspectRatio.", root.SourceSpan);
                fragment.HasAspectRatio = true;
                fragment.AspectRatio = parsed.AspectRatio;
            }
            else if (part.StartsWith("transform(", System.StringComparison.Ordinal) && new SvgViewportResolver().EndsWithClose(part))
            {
                if (transformSeen) return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment repeats transform.", root.SourceSpan);
                transformSeen = true;
                SvgTransformParseResult parsed = SvgTransformParser.Parse(new SvgViewportResolver().FunctionContent(part, 10), 100000, 4096);
                if (!parsed.Success) return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment has an invalid transform.", root.SourceSpan);
                fragment.Transform = parsed.TransformList;
            }
            else if (part.StartsWith("zoomAndPan(", System.StringComparison.Ordinal) && new SvgViewportResolver().EndsWithClose(part))
            {
                if (zoomSeen) return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment repeats zoomAndPan.", root.SourceSpan);
                zoomSeen = true;
                string zoom = new SvgViewportResolver().FunctionContent(part, 11).Trim();
                if (zoom != "magnify" && zoom != "disable") return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment has an invalid zoomAndPan value.", root.SourceSpan);
            }
            else return new SvgViewportResolver().FragmentFailure(fragment, result, "The svgView fragment contains an unknown or malformed parameter.", root.SourceSpan);
            index = index + 1;
        }
        return fragment;
    }
bool EndsWithClose(string value)
    {
        return value.Length > 0 && value[value.Length - 1] == ')';
    }
string FunctionContent(string value, int prefixLength)
    {
        return value.Substring(prefixLength, value.Length - prefixLength - 1);
    }
SvgViewportFragmentOverride ParseMediaFragment(string text, double viewportWidth, double viewportHeight, SvgNode root, SvgViewportResult result)
    {
        SvgViewportFragmentOverride fragment = new SvgViewportFragmentOverride();
        fragment.Kind = SvgViewportFragmentKind.MediaFragment;
        string valuesText = text.Substring(5);
        bool percent = false;
        if (valuesText.StartsWith("pixel:", System.StringComparison.Ordinal)) valuesText = valuesText.Substring(6);
        else if (valuesText.StartsWith("percent:", System.StringComparison.Ordinal))
        {
            percent = true;
            valuesText = valuesText.Substring(8);
        }
        SvgNumberListResult parsed = SvgListParser.ParseCoordinateList(valuesText, 4, 4096);
        if (!parsed.Success || parsed.Values.Length != 4 || parsed.Values[2] < 0 || parsed.Values[3] < 0)
        {
            return new SvgViewportResolver().FragmentFailure(fragment, result, "The xywh SVG media fragment is invalid.", root.SourceSpan);
        }
        double x = parsed.Values[0];
        double y = parsed.Values[1];
        double width = parsed.Values[2];
        double height = parsed.Values[3];
        if (percent)
        {
            x = x * viewportWidth / 100;
            y = y * viewportHeight / 100;
            width = width * viewportWidth / 100;
            height = height * viewportHeight / 100;
        }
        fragment.HasViewBox = true;
        fragment.ViewBox = new SvgViewBox(x, y, width, height);
        return fragment;
    }
SvgViewportFragmentOverride FragmentFailure(SvgViewportFragmentOverride fragment, SvgViewportResult result, string message, SvgSourceSpan? source)
    {
        fragment.Success = false;
        fragment.Kind = SvgViewportFragmentKind.Invalid;
        new SvgViewportResolver().AddError(result, SvgDiagnosticCode.InvalidReference, message, source);
        return fragment;
    }
void AddError(SvgViewportResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
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
