namespace svg;

/// <summary>Resolves SVG filter templates and parses their ordered primitive input graph.</summary>
public class SvgFilterResolver : ISvgFilterResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgFilterResolver()
    {
    }

    /// <summary>Resolves one filter element using deterministic defaults.</summary>
    /// <param name="node">The authored filter element.</param>
    /// <returns>The normalized filter definition and primitive graph.</returns>
    public SvgFilterResult ResolveWithNode(SvgNode node)
    {
        return new SvgFilterResolver().ResolveWithNodeAndOptions(node, new SvgFilterResolveOptions());
    }

    /// <summary>Resolves one filter with explicit geometry, style, reference, and filter limits.</summary>
    /// <param name="node">The authored filter element.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The normalized filter definition and primitive graph.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an SVG filter element.</exception>
    public SvgFilterResult ResolveWithNodeAndOptions(SvgNode node, SvgFilterResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        if (node.Kind != SvgNodeKind.Element || node.NamespaceUri != SvgNamespaces.Svg || node.ElementKind != SvgElementKind.Filter) throw new System.ArgumentOutOfRangeException("node");
        SvgFilterResult result = new SvgFilterResult(node);
        if (!new SvgFilterResolver().ValidOptions(options))
        {
            new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Filter resolution options are invalid.", node.SourceSpan);
            return result;
        }
        result.ComputedStyle = new SvgFilterResolver().FilterStyle(node, options);
        new SvgFilterResolver().AppendStyleDiagnostics(result, result.ComputedStyle);
        new SvgFilterResolver().ResolveTemplates(result, options);
        new SvgFilterResolver().AddDefaults(result, options);
        new SvgFilterResolver().ResolveGeometry(result, options);
        new SvgFilterResolver().ResolvePrimitives(result, options);
        result.Empty = result.Primitives.Count == 0;
        result.Success = !new SvgFilterResolver().HasError(result.Diagnostics);
        bool outputEnabled = result.Primitives.Count != 0 && result.Primitives[result.Primitives.Count - 1].Enabled;
        result.Renderable = result.Success && outputEnabled && result.Region != null && result.Region.Width > 0 && result.Region.Height > 0 && new SvgFilterResolver().BoundingBoxUsable(result.FilterUnits, options) && new SvgFilterResolver().BoundingBoxUsable(result.PrimitiveUnits, options);
        return result;
    }
void ResolveTemplates(SvgFilterResult result, SvgFilterResolveOptions options)
    {
        string[] names = new SvgFilterResolver().FilterAttributeNames();
        System.Collections.Generic.List<SvgNode> active = new System.Collections.Generic.List<SvgNode>();
        SvgNode current = (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        int depth = 0;
        while (current != null)
        {
            result.TemplateChain.Add(current);
            result.TemplateVersions.Add(current.Version);
            active.Add(current);
            new SvgFilterResolver().CollectFilterAttributes(current, result, options, names);
            System.Collections.Generic.List<SvgNode> primitives = new SvgFilterResolver().PrimitiveChildren(current);
            if (result.ContentSource == null && primitives.Count != 0) result.ContentSource = current;
            bool complete = new SvgFilterResolver().AllAttributes(result, names) && result.ContentSource != null;
            SvgAttribute? href = new SvgFilterResolver().FindHref(current);
            if (complete || href == null) break;
            if (depth >= options.ReferenceLimits.MaxEffectDepth)
            {
                new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Filter template resolution exceeded MaxEffectDepth.", href.SourceSpan);
                break;
            }
            if (result.References.Count >= options.ReferenceLimits.MaxCount)
            {
                new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Filter template resolution exceeded reference MaxCount.", href.SourceSpan);
                break;
            }
            SvgReferenceResult reference = new SvgReferenceResolver().ResolveWithNodeAndOptions(current, new SvgFilterResolver().CopyReferenceOptions(options.ReferenceOptions));
            result.References.Add(reference);
            new SvgFilterResolver().AppendReferenceDiagnostics(result, reference);
            if (!reference.Success || reference.Status != SvgReferenceStatus.Resolved || reference.Target == null) break;
            if (reference.Target.Kind != SvgNodeKind.Element || reference.Target.NamespaceUri != SvgNamespaces.Svg || reference.Target.ElementKind != SvgElementKind.Filter)
            {
                new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "A filter href must identify another SVG filter element.", reference.TargetSource);
                break;
            }
            if (new SvgFilterResolver().Contains(active, reference.Target))
            {
                new SvgFilterResolver().AddError(result, SvgDiagnosticCode.ReferenceCycle, "A filter href cycle was detected.", reference.ReferenceSource);
                break;
            }
            current = reference.Target;
            depth = depth + 1;
        }
    }
void CollectFilterAttributes(SvgNode node, SvgFilterResult result, SvgFilterResolveOptions options, string[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            string name = names[index];
            if (result.FindAttribute(name) == null)
            {
                SvgAttribute? authored = new SvgFilterResolver().FindAttribute(node, name);
                if (authored != null)
                {
                    SvgTypedAttributeValue typed = options.Attributes.Get(node, name);
                    new SvgFilterResolver().AppendTypedDiagnostics(result, typed);
                    if (typed.Success && typed.Applicable && typed.HasValue) result.Attributes.Add(new SvgFilterAttribute(name, node, authored, typed.Value, !object.ReferenceEquals(node, result.Node), false));
                }
            }
            index = index + 1;
        }
    }
void AddDefaults(SvgFilterResult result, SvgFilterResolveOptions options)
    {
        string[] names = new SvgFilterResolver().FilterAttributeNames();
        int index = 0;
        while (index < names.Length)
        {
            string name = names[index];
            if (result.FindAttribute(name) == null)
            {
                SvgTypedAttributeValue typed = options.Attributes.Get((result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), name);
                new SvgFilterResolver().AppendTypedDiagnostics(result, typed);
                if (typed.Success && typed.HasValue) result.Attributes.Add(new SvgFilterAttribute(name, null, null, typed.Value, false, true));
            }
            index = index + 1;
        }
    }
void ResolveGeometry(SvgFilterResult result, SvgFilterResolveOptions options)
    {
        result.FilterUnits = new SvgFilterResolver().Units(result.FindAttribute(SvgAttributeNames.FilterUnits), SvgPaintServerUnits.ObjectBoundingBox, result);
        result.PrimitiveUnits = new SvgFilterResolver().Units(result.FindAttribute(SvgAttributeNames.PrimitiveUnits), SvgPaintServerUnits.UserSpaceOnUse, result);
        result.FilterTransform = new SvgFilterResolver().UnitMatrix(result.FilterUnits, options, result);
        result.PrimitiveTransform = new SvgFilterResolver().UnitMatrix(result.PrimitiveUnits, options, result);
        double x = new SvgFilterResolver().FilterLength(result.FindAttribute(SvgAttributeNames.X), result.FilterUnits, options, result, SvgLengthPercentageBasis.ViewportWidth, -0.1);
        double y = new SvgFilterResolver().FilterLength(result.FindAttribute(SvgAttributeNames.Y), result.FilterUnits, options, result, SvgLengthPercentageBasis.ViewportHeight, -0.1);
        double width = new SvgFilterResolver().FilterLength(result.FindAttribute(SvgAttributeNames.Width), result.FilterUnits, options, result, SvgLengthPercentageBasis.ViewportWidth, 1.2);
        double height = new SvgFilterResolver().FilterLength(result.FindAttribute(SvgAttributeNames.Height), result.FilterUnits, options, result, SvgLengthPercentageBasis.ViewportHeight, 1.2);
        if (width >= 0 && height >= 0)
        {
            result.Region = SvgRect.Create(x, y, width, height, width == 0 || height == 0);
            SvgRectTransformResult transformed = result.FilterTransform.TransformRect(result.Region);
            if (transformed.Success) result.UserRegion = transformed.Rectangle;
            else new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The filter region could not be transformed.", (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
        }
        if (result.UserRegion != null)
        {
            double area = result.UserRegion.Width * result.UserRegion.Height;
            if (!SvgFiniteMath.IsFinite(area) || area > options.FilterLimits.MaxAreaPixels) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The filter region exceeds MaxAreaPixels.", (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
        }
    }
void ResolvePrimitives(SvgFilterResult result, SvgFilterResolveOptions options)
    {
        if (result.ContentSource == null) return;
        System.Collections.Generic.List<SvgNode> nodes = new SvgFilterResolver().PrimitiveChildren(result.ContentSource);
        if (nodes.Count > options.FilterLimits.MaxPrimitives)
        {
            new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The filter graph exceeds MaxPrimitives.", result.ContentSource.SourceSpan);
            return;
        }
        int index = 0;
        while (index < nodes.Count)
        {
            SvgFilterPrimitive primitive = new SvgFilterPrimitive(nodes[index], index);
            primitive.ComputedStyle = new SvgFilterResolver().PrimitiveStyle(nodes[index], (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), options);
            new SvgFilterResolver().AppendStyleDiagnostics(result, primitive.ComputedStyle);
            primitive.ColorSpace = new SvgFilterResolver().ColorSpace(primitive.ComputedStyle);
            new SvgFilterResolver().ParsePrimitiveAttributes(primitive, result, options);
            new SvgFilterResolver().ParsePrimitiveChildren(primitive, result, options);
            new SvgFilterResolver().ResolveImageReference(primitive, result, options);
            new SvgFilterResolver().ResolvePrimitiveInputs(primitive, result, options);
            new SvgFilterResolver().ResolvePrimitiveRegion(primitive, result, options);
            new SvgFilterResolver().ValidatePrimitive(primitive, result, options);
            result.Primitives.Add(primitive);
            index = index + 1;
        }
    }
void ParsePrimitiveAttributes(SvgFilterPrimitive primitive, SvgFilterResult result, SvgFilterResolveOptions options)
    {
        string[] names = new SvgFilterResolver().PrimitiveAttributeNames(primitive.ElementKind);
        int index = 0;
        while (index < names.Length)
        {
            SvgTypedAttributeValue typed = options.Attributes.Get((primitive.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), names[index]);
            new SvgFilterResolver().AppendTypedDiagnostics(result, typed);
            primitive.Attributes.Add(new SvgFilterPrimitiveAttribute(names[index], typed));
            index = index + 1;
        }
        SvgAttribute? resultAttribute = new SvgFilterResolver().FindAttribute((primitive.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.Result);
        if (resultAttribute != null)
        {
            primitive.ResultName = resultAttribute.Value.Trim();
            if (primitive.ResultName.Length == 0) new SvgFilterResolver().AddWarning(result, SvgDiagnosticCode.InvalidValue, "An empty filter result name is ignored.", resultAttribute.SourceSpan);
            else if (primitive.ResultName.Length > options.MaxNameCharacters)
            {
                new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "A filter result name exceeds MaxNameCharacters.", resultAttribute.SourceSpan);
                primitive.ResultName = "";
            }
        }
    }
void ParsePrimitiveChildren(SvgFilterPrimitive primitive, SvgFilterResult result, SvgFilterResolveOptions options)
    {
        int index = 0;
        while (index < (primitive.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            SvgNode node = primitive.SourceNode.Children[index];
            string[] names = new SvgFilterResolver().ChildAttributeNames(node.ElementKind);
            if (node.Kind == SvgNodeKind.Element && names.Length != 0)
            {
                SvgFilterPrimitiveChild child = new SvgFilterPrimitiveChild(node);
                int nameIndex = 0;
                while (nameIndex < names.Length)
                {
                    SvgTypedAttributeValue typed = options.Attributes.Get(node, names[nameIndex]);
                    new SvgFilterResolver().AppendTypedDiagnostics(result, typed);
                    child.Attributes.Add(new SvgFilterPrimitiveAttribute(names[nameIndex], typed));
                    nameIndex = nameIndex + 1;
                }
                primitive.Children.Add(child);
            }
            index = index + 1;
        }
    }
void ResolveImageReference(SvgFilterPrimitive primitive, SvgFilterResult result, SvgFilterResolveOptions options)
    {
        if (primitive.ElementKind != SvgElementKind.FeImage || new SvgFilterResolver().FindHref((primitive.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))) == null) return;
        if (result.References.Count >= options.ReferenceLimits.MaxCount)
        {
            new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Filter references exceeded MaxCount while resolving feImage.", primitive.SourceNode.SourceSpan);
            return;
        }
        primitive.Reference = new SvgReferenceResolver().ResolveWithNodeAndOptions(primitive.SourceNode, new SvgFilterResolver().CopyReferenceOptions(options.ReferenceOptions));
        result.References.Add(primitive.Reference);
        new SvgFilterResolver().AppendReferenceDiagnostics(result, primitive.Reference);
        if (new SvgFilterResolver().CanAcquireImage(primitive.Reference, options.ImageOptions))
        {
            primitive.Image = new SvgImageResolver().Resolve(primitive.Reference, options.ImageOptions);
            new SvgFilterResolver().AppendImageDiagnostics(result, primitive.Image);
        }
    }
void ResolvePrimitiveRegion(SvgFilterPrimitive primitive, SvgFilterResult result, SvgFilterResolveOptions options)
    {
        SvgRect? defaults = new SvgFilterResolver().DefaultPrimitiveRegion(primitive, result);
        double x = new SvgFilterResolver().PrimitiveLength(primitive, SvgAttributeNames.X, result.PrimitiveUnits, options, result, SvgLengthPercentageBasis.ViewportWidth, (defaults ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X);
        double y = new SvgFilterResolver().PrimitiveLength(primitive, SvgAttributeNames.Y, result.PrimitiveUnits, options, result, SvgLengthPercentageBasis.ViewportHeight, defaults.Y);
        double width = new SvgFilterResolver().PrimitiveLength(primitive, SvgAttributeNames.Width, result.PrimitiveUnits, options, result, SvgLengthPercentageBasis.ViewportWidth, defaults.Width);
        double height = new SvgFilterResolver().PrimitiveLength(primitive, SvgAttributeNames.Height, result.PrimitiveUnits, options, result, SvgLengthPercentageBasis.ViewportHeight, defaults.Height);
        if (width < 0 || height < 0)
        {
            primitive.Enabled = false;
            return;
        }
        primitive.Region = SvgRect.Create(x, y, width, height, width == 0 || height == 0);
        SvgRectTransformResult transformed = result.PrimitiveTransform.TransformRect(primitive.Region);
        if (transformed.Success) primitive.UserRegion = transformed.Rectangle;
        else new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A filter primitive subregion could not be transformed.", (primitive.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
        primitive.Enabled = width > 0 && height > 0;
    }
SvgRect? DefaultPrimitiveRegion(SvgFilterPrimitive primitive, SvgFilterResult result)
    {
        SvgRect? user = null;
        bool filterRegion = primitive.ElementKind == SvgElementKind.FeTile || primitive.Inputs.Count == 0;
        int index = 0;
        while (index < primitive.Inputs.Count)
        {
            SvgFilterInput input = primitive.Inputs[index];
            if (input.Kind == SvgFilterInputKind.Standard || input.Kind == SvgFilterInputKind.TransparentBlack) filterRegion = true;
            else if (input.Primitive != null && input.Primitive.UserRegion != null) user = new SvgFilterResolver().Union(user, input.Primitive.UserRegion);
            index = index + 1;
        }
        if (filterRegion || user == null) user = result.UserRegion;
        if (user == null) return SvgRect.Create(0, 0, 0, 0, true);
        SvgMatrixOperationResult inverse = result.PrimitiveTransform.Invert();
        if (!inverse.Success) return SvgRect.Create(0, 0, 0, 0, true);
        SvgRectTransformResult local = (inverse.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TransformRect(user);
        return local.Success ? local.Rectangle : SvgRect.Create(0, 0, 0, 0, true);
    }
void ResolvePrimitiveInputs(SvgFilterPrimitive primitive, SvgFilterResult result, SvgFilterResolveOptions options)
    {
        SvgFilterPrimitive? previous = primitive.Index == 0 ? null : result.Primitives[primitive.Index - 1];
        int count = new SvgFilterResolver().InputCount(primitive.ElementKind);
        if (primitive.ElementKind == SvgElementKind.FeMerge)
        {
            int index = 0;
            while (index < (primitive.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
            {
                SvgNode child = primitive.SourceNode.Children[index];
                if (child.Kind == SvgNodeKind.Element && child.ElementKind == SvgElementKind.FeMergeNode) primitive.Inputs.Add(new SvgFilterResolver().ResolveInput(child, SvgAttributeNames.In, previous, false, result, options));
                index = index + 1;
            }
            return;
        }
        if (count >= 1) primitive.Inputs.Add(new SvgFilterResolver().ResolveInput((primitive.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.In, previous, false, result, options));
        if (count >= 2) primitive.Inputs.Add(new SvgFilterResolver().ResolveInput((primitive.SourceNode ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgAttributeNames.In2, previous, true, result, options));
    }
SvgFilterInput ResolveInput(SvgNode node, string attributeName, SvgFilterPrimitive? previous, bool secondary, SvgFilterResult result, SvgFilterResolveOptions options)
    {
        SvgFilterInput input = new SvgFilterInput(attributeName);
        SvgAttribute? attribute = new SvgFilterResolver().FindAttribute(node, attributeName);
        if (attribute == null)
        {
            if (!secondary && previous != null)
            {
                input.Kind = SvgFilterInputKind.Previous;
                input.Primitive = previous;
                return input;
            }
            input.Kind = SvgFilterInputKind.Standard;
            input.StandardInput = SvgFilterStandardInput.SourceGraphic;
            return input;
        }
        input.Source = attribute.SourceSpan;
        input.Name = attribute.Value.Trim();
        if (input.Name.Length > options.MaxNameCharacters)
        {
            new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "A filter input name exceeds MaxNameCharacters.", attribute.SourceSpan);
            input.Kind = SvgFilterInputKind.TransparentBlack;
            input.Available = false;
            return input;
        }
        SvgFilterStandardInput standard = new SvgFilterResolver().StandardInput(input.Name);
        if (standard != (SvgFilterStandardInput)(-1))
        {
            input.Kind = SvgFilterInputKind.Standard;
            input.StandardInput = standard;
            return input;
        }
        SvgFilterPrimitive? named = result.FindResult(input.Name, result.Primitives.Count);
        if (named != null)
        {
            input.Kind = SvgFilterInputKind.NamedResult;
            input.Primitive = named;
            return input;
        }
        input.Kind = SvgFilterInputKind.TransparentBlack;
        input.Available = false;
        new SvgFilterResolver().AddWarning(result, SvgDiagnosticCode.InvalidReference, "A filter input name does not identify a preceding result and resolves to transparent black.", attribute.SourceSpan);
        return input;
    }
void ValidatePrimitive(SvgFilterPrimitive primitive, SvgFilterResult result, SvgFilterResolveOptions options)
    {
        if (primitive.ElementKind == SvgElementKind.FeGaussianBlur || primitive.ElementKind == SvgElementKind.FeDropShadow)
        {
            double[] deviations = new SvgFilterResolver().NumberList(primitive, SvgAttributeNames.StdDeviation);
            int index = 0;
            while (index < deviations.Length)
            {
                if (deviations[index] < 0) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A Gaussian blur standard deviation cannot be negative.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.StdDeviation));
                if (deviations[index] > options.FilterLimits.MaxBlurStandardDeviation) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "A Gaussian blur exceeds MaxBlurStandardDeviation.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.StdDeviation));
                index = index + 1;
            }
        }
        if (primitive.ElementKind == SvgElementKind.FeMorphology)
        {
            double[] radii = new SvgFilterResolver().NumberList(primitive, SvgAttributeNames.Radius);
            int index = 0;
            while (index < radii.Length)
            {
                if (radii[index] < 0) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A morphology radius cannot be negative.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.Radius));
                index = index + 1;
            }
        }
        if (primitive.ElementKind == SvgElementKind.FeTurbulence)
        {
            double[] frequencies = new SvgFilterResolver().NumberList(primitive, SvgAttributeNames.BaseFrequency);
            int index = 0;
            while (index < frequencies.Length)
            {
                if (frequencies[index] < 0) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A turbulence base frequency cannot be negative.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.BaseFrequency));
                index = index + 1;
            }
            int octaves = new SvgFilterResolver().Integer(primitive, SvgAttributeNames.NumOctaves, 1);
            if (octaves < 1) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Turbulence numOctaves must be positive.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.NumOctaves));
            if (octaves > options.FilterLimits.MaxTurbulenceOctaves) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Turbulence exceeds MaxTurbulenceOctaves.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.NumOctaves));
        }
        if (primitive.ElementKind == SvgElementKind.FeConvolveMatrix) new SvgFilterResolver().ValidateConvolution(primitive, result, options);
    }
void ValidateConvolution(SvgFilterPrimitive primitive, SvgFilterResult result, SvgFilterResolveOptions options)
    {
        int[] order = new SvgFilterResolver().IntegerList(primitive, SvgAttributeNames.Order);
        int width = order.Length == 0 ? 3 : order[0];
        int height = order.Length < 2 ? width : order[1];
        if (width < 1 || height < 1)
        {
            new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A convolution order must contain positive dimensions.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.Order));
            return;
        }
        long count = (long)width * height;
        if (count > options.FilterLimits.MaxKernelElements) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "A convolution kernel exceeds MaxKernelElements.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.Order));
        double[] kernel = new SvgFilterResolver().NumberList(primitive, SvgAttributeNames.KernelMatrix);
        if (kernel.Length != count) new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The convolution kernelMatrix size must equal the order product.", new SvgFilterResolver().PrimitiveSource(primitive, SvgAttributeNames.KernelMatrix));
    }
SvgSourceSpan? PrimitiveSource(SvgFilterPrimitive? primitive, string name)
    {
        SvgFilterPrimitiveAttribute? attribute = primitive == null ? null : primitive.FindAttribute(name);
        if (attribute != null && attribute.TypedValue != null) return new SvgFilterResolver().TypedSource(attribute.TypedValue);
        return primitive == null || primitive.SourceNode == null ? null : primitive.SourceNode.SourceSpan;
    }
bool CanAcquireImage(SvgReferenceResult? reference, SvgImageResolveOptions? options)
    {
        if (options == null || reference == null || !reference.Success || reference.Status != SvgReferenceStatus.External) return false;
        if (options.LoadOptions != null && options.LoadOptions.ResourceProvider != null) return true;
        return reference.ResourceUri.StartsWith("data:", System.StringComparison.OrdinalIgnoreCase);
    }
void AppendImageDiagnostics(SvgFilterResult result, SvgImageResolveResult? image)
    {
        if (image == null) return;
        int index = 0;
        while (index < image.Diagnostics.Count)
        {
            result.Diagnostics.Add(image.Diagnostics[index]);
            index = index + 1;
        }
    }
SvgFilterColorSpace ColorSpace(SvgComputedStyle? style)
    {
        if (style == null) return SvgFilterColorSpace.LinearRgb;
        SvgComputedValue? value = style.Find(SvgCssPropertyNames.ColorInterpolationFilters);
        if (value == null || value.Kind != SvgComputedValueKind.Keyword) return SvgFilterColorSpace.LinearRgb;
        if (string.Equals(value.Keyword, "srgb", System.StringComparison.OrdinalIgnoreCase)) return SvgFilterColorSpace.Srgb;
        if (string.Equals(value.Keyword, "auto", System.StringComparison.OrdinalIgnoreCase)) return SvgFilterColorSpace.Auto;
        return SvgFilterColorSpace.LinearRgb;
    }
SvgComputedStyle? FilterStyle(SvgNode node, SvgFilterResolveOptions options)
    {
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node)) return options.ComputedStyle;
        SvgStyleResolver resolver = new SvgStyleResolver();
        return resolver.ResolveWithOptions(node, options.StyleSheets, options.StyleOptions);
    }
SvgComputedStyle? PrimitiveStyle(SvgNode source, SvgNode host, SvgFilterResolveOptions options)
    {
        SvgNode hostClone = new SvgFilterResolver().CloneNode(host);
        SvgNode primitiveClone = new SvgFilterResolver().CloneNode(source);
        primitiveClone.Parent = hostClone;
        hostClone.Children.Add(primitiveClone);
        SvgStyleResolver resolver = new SvgStyleResolver();
        return resolver.ResolveWithOptions(primitiveClone, options.StyleSheets, options.StyleOptions);
    }
SvgPaintServerUnits Units(SvgFilterAttribute? attribute, SvgPaintServerUnits initial, SvgFilterResult result)
    {
        string text = attribute == null || attribute.Value == null ? "" : ((string)attribute.Value).Trim();
        if (text == "userSpaceOnUse") return SvgPaintServerUnits.UserSpaceOnUse;
        if (text == "objectBoundingBox") return SvgPaintServerUnits.ObjectBoundingBox;
        new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A filter units attribute has an invalid keyword.", new SvgFilterResolver().AttributeSource(attribute));
        return initial;
    }
double FilterLength(SvgFilterAttribute? attribute, SvgPaintServerUnits units, SvgFilterResolveOptions options, SvgFilterResult result, SvgLengthPercentageBasis basis, double initial)
    {
        if (attribute == null || attribute.Value == null) return initial;
        return new SvgFilterResolver().Length((SvgLength)attribute.Value, units, options, result, basis, new SvgFilterResolver().AttributeSource(attribute), initial);
    }
double PrimitiveLength(SvgFilterPrimitive primitive, string name, SvgPaintServerUnits units, SvgFilterResolveOptions options, SvgFilterResult result, SvgLengthPercentageBasis basis, double initial)
    {
        SvgFilterPrimitiveAttribute? attribute = primitive.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || attribute.TypedValue.Attribute == null || !attribute.TypedValue.Success || !attribute.TypedValue.HasValue || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.Length || attribute.TypedValue.Value == null) return initial;
        return new SvgFilterResolver().Length((SvgLength)attribute.TypedValue.Value, units, options, result, basis, new SvgFilterResolver().TypedSource(attribute.TypedValue), initial);
    }
double Length(SvgLength length, SvgPaintServerUnits units, SvgFilterResolveOptions options, SvgFilterResult result, SvgLengthPercentageBasis basis, SvgSourceSpan? source, double fallback)
    {
        if (units == SvgPaintServerUnits.ObjectBoundingBox)
        {
            if (length.Unit == SvgLengthUnit.Number) return length.Value;
            if (length.Unit == SvgLengthUnit.Percentage) return length.Value / 100;
            new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "Object-bounding-box filter geometry accepts only numbers and percentages.", source);
            return fallback;
        }
        SvgLengthEvaluationResult evaluated = length.Evaluate(options.LengthContext, basis);
        if (evaluated.Success) return evaluated.Value;
        new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "A user-space filter length could not be evaluated.", source);
        return fallback;
    }
SvgMatrix UnitMatrix(SvgPaintServerUnits units, SvgFilterResolveOptions options, SvgFilterResult result)
    {
        if (units == SvgPaintServerUnits.UserSpaceOnUse) return new SvgFilterResolver().Identity();
        if (options.TargetBounds == null)
        {
            new SvgFilterResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Object-bounding-box filter units require target bounds.", (result.Node ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan);
            return new SvgFilterResolver().Identity();
        }
        return new SvgMatrix(options.TargetBounds.Width, 0, 0, options.TargetBounds.Height, options.TargetBounds.X, options.TargetBounds.Y);
    }
SvgFilterStandardInput StandardInput(string name)
    {
        if (name == "SourceGraphic") return SvgFilterStandardInput.SourceGraphic;
        if (name == "SourceAlpha") return SvgFilterStandardInput.SourceAlpha;
        if (name == "BackgroundImage") return SvgFilterStandardInput.BackgroundImage;
        if (name == "BackgroundAlpha") return SvgFilterStandardInput.BackgroundAlpha;
        if (name == "FillPaint") return SvgFilterStandardInput.FillPaint;
        if (name == "StrokePaint") return SvgFilterStandardInput.StrokePaint;
        return (SvgFilterStandardInput)(-1);
    }
int InputCount(SvgElementKind kind)
    {
        if (kind == SvgElementKind.FeFlood || kind == SvgElementKind.FeImage || kind == SvgElementKind.FeTurbulence) return 0;
        if (kind == SvgElementKind.FeBlend || kind == SvgElementKind.FeComposite || kind == SvgElementKind.FeDisplacementMap) return 2;
        return 1;
    }
System.Collections.Generic.List<SvgNode> PrimitiveChildren(SvgNode filter)
    {
        System.Collections.Generic.List<SvgNode> result = new System.Collections.Generic.List<SvgNode>();
        int index = 0;
        while (index < filter.Children.Count)
        {
            SvgNode child = filter.Children[index];
            if (child.Kind == SvgNodeKind.Element && new SvgFilterResolver().FilterPrimitive(child.ElementKind)) result.Add(child);
            index = index + 1;
        }
        return result;
    }
bool FilterPrimitive(SvgElementKind kind)
    {
        return kind == SvgElementKind.FeBlend || kind == SvgElementKind.FeColorMatrix || kind == SvgElementKind.FeComponentTransfer || kind == SvgElementKind.FeComposite || kind == SvgElementKind.FeConvolveMatrix || kind == SvgElementKind.FeDiffuseLighting || kind == SvgElementKind.FeDisplacementMap || kind == SvgElementKind.FeDropShadow || kind == SvgElementKind.FeFlood || kind == SvgElementKind.FeGaussianBlur || kind == SvgElementKind.FeImage || kind == SvgElementKind.FeMerge || kind == SvgElementKind.FeMorphology || kind == SvgElementKind.FeOffset || kind == SvgElementKind.FeSpecularLighting || kind == SvgElementKind.FeTile || kind == SvgElementKind.FeTurbulence;
    }
string[] FilterAttributeNames()
    {
        return [SvgAttributeNames.X, SvgAttributeNames.Y, SvgAttributeNames.Width, SvgAttributeNames.Height, SvgAttributeNames.FilterUnits, SvgAttributeNames.PrimitiveUnits];
    }
string[] PrimitiveAttributeNames(SvgElementKind kind)
    {
        string[] common = [SvgAttributeNames.X, SvgAttributeNames.Y, SvgAttributeNames.Width, SvgAttributeNames.Height, SvgAttributeNames.Result];
        if (kind == SvgElementKind.FeBlend) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.In2, SvgAttributeNames.Mode]);
        if (kind == SvgElementKind.FeColorMatrix) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.Type, SvgAttributeNames.Values]);
        if (kind == SvgElementKind.FeComponentTransfer) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In]);
        if (kind == SvgElementKind.FeComposite) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.In2, SvgAttributeNames.Operator, "k1", "k2", "k3", "k4"]);
        if (kind == SvgElementKind.FeConvolveMatrix) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.Order, SvgAttributeNames.KernelMatrix, SvgAttributeNames.Divisor, SvgAttributeNames.Bias, SvgAttributeNames.TargetX, SvgAttributeNames.TargetY, SvgAttributeNames.EdgeMode, SvgAttributeNames.KernelUnitLength, SvgAttributeNames.PreserveAlpha]);
        if (kind == SvgElementKind.FeDiffuseLighting) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.SurfaceScale, SvgAttributeNames.DiffuseConstant, SvgAttributeNames.KernelUnitLength]);
        if (kind == SvgElementKind.FeDisplacementMap) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.In2, SvgAttributeNames.Scale, SvgAttributeNames.XChannelSelector, SvgAttributeNames.YChannelSelector]);
        if (kind == SvgElementKind.FeDropShadow) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.Dx, SvgAttributeNames.Dy, SvgAttributeNames.StdDeviation]);
        if (kind == SvgElementKind.FeGaussianBlur) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.StdDeviation, SvgAttributeNames.EdgeMode]);
        if (kind == SvgElementKind.FeImage) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.Href, SvgAttributeNames.PreserveAspectRatio]);
        if (kind == SvgElementKind.FeMorphology) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.Operator, SvgAttributeNames.Radius]);
        if (kind == SvgElementKind.FeOffset) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.Dx, SvgAttributeNames.Dy]);
        if (kind == SvgElementKind.FeSpecularLighting) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In, SvgAttributeNames.SurfaceScale, SvgAttributeNames.SpecularConstant, SvgAttributeNames.SpecularExponent, SvgAttributeNames.KernelUnitLength]);
        if (kind == SvgElementKind.FeTile) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.In]);
        if (kind == SvgElementKind.FeTurbulence) return new SvgFilterResolver().Combine(common, [SvgAttributeNames.BaseFrequency, SvgAttributeNames.NumOctaves, SvgAttributeNames.Seed, SvgAttributeNames.StitchTiles, SvgAttributeNames.Type]);
        return common;
    }
string[] ChildAttributeNames(SvgElementKind kind)
    {
        if (kind == SvgElementKind.FeMergeNode) return [SvgAttributeNames.In];
        if (kind == SvgElementKind.FeFuncA || kind == SvgElementKind.FeFuncB || kind == SvgElementKind.FeFuncG || kind == SvgElementKind.FeFuncR) return [SvgAttributeNames.Type, SvgAttributeNames.TableValues, SvgAttributeNames.Slope, SvgAttributeNames.Intercept, SvgAttributeNames.Amplitude, SvgAttributeNames.Exponent, SvgAttributeNames.Offset];
        if (kind == SvgElementKind.FeDistantLight) return [SvgAttributeNames.Azimuth, SvgAttributeNames.Elevation];
        if (kind == SvgElementKind.FePointLight) return [SvgAttributeNames.X, SvgAttributeNames.Y, "z"];
        if (kind == SvgElementKind.FeSpotLight) return [SvgAttributeNames.X, SvgAttributeNames.Y, "z", "pointsAtX", "pointsAtY", "pointsAtZ", SvgAttributeNames.SpecularExponent, "limitingConeAngle"];
        return new string[0];
    }
string[] Combine(string[] left, string[] right)
    {
        string[] result = new string[left.Length + right.Length];
        int index = 0;
        while (index < left.Length)
        {
            result[index] = left[index];
            index = index + 1;
        }
        int rightIndex = 0;
        while (rightIndex < right.Length)
        {
            result[index] = right[rightIndex];
            index = index + 1;
            rightIndex = rightIndex + 1;
        }
        return result;
    }
double[] NumberList(SvgFilterPrimitive primitive, string name)
    {
        SvgFilterPrimitiveAttribute? attribute = primitive.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.NumberList || attribute.TypedValue.Value == null) return new double[0];
        return (double[])attribute.TypedValue.Value;
    }
int[] IntegerList(SvgFilterPrimitive primitive, string name)
    {
        SvgFilterPrimitiveAttribute? attribute = primitive.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.IntegerList || attribute.TypedValue.Value == null) return new int[0];
        return (int[])attribute.TypedValue.Value;
    }
int Integer(SvgFilterPrimitive primitive, string name, int fallback)
    {
        SvgFilterPrimitiveAttribute? attribute = primitive.FindAttribute(name);
        if (attribute == null || attribute.TypedValue == null || !attribute.TypedValue.Success || attribute.TypedValue.ValueKind != SvgTypedAttributeKind.Integer || attribute.TypedValue.Value == null) return fallback;
        return (int)attribute.TypedValue.Value;
    }
bool AllAttributes(SvgFilterResult result, string[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (result.FindAttribute(names[index]) == null) return false;
            index = index + 1;
        }
        return true;
    }
bool BoundingBoxUsable(SvgPaintServerUnits units, SvgFilterResolveOptions options)
    {
        return units == SvgPaintServerUnits.UserSpaceOnUse || options.TargetBounds != null && options.TargetBounds.Width > 0 && options.TargetBounds.Height > 0;
    }
SvgRect? Union(SvgRect? left, SvgRect? right)
    {
        if (left == null) return right;
        if (right == null) return left;
        double minimumX = System.Math.Min(left.X, right.X);
        double minimumY = System.Math.Min(left.Y, right.Y);
        double maximumX = System.Math.Max(left.X + left.Width, right.X + right.Width);
        double maximumY = System.Math.Max(left.Y + left.Height, right.Y + right.Height);
        return SvgRect.Create(minimumX, minimumY, maximumX - minimumX, maximumY - minimumY, left.IsEmpty && right.IsEmpty);
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
SvgAttribute? FindHref(SvgNode node)
    {
        SvgAttribute? href = new SvgFilterResolver().FindAttribute(node, SvgAttributeNames.Href);
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
SvgSourceSpan? AttributeSource(SvgFilterAttribute? attribute)
    {
        if (attribute == null) return null;
        if (attribute.Attribute != null) return attribute.Attribute.SourceSpan;
        return attribute.SourceNode == null ? null : attribute.SourceNode.SourceSpan;
    }
SvgSourceSpan? TypedSource(SvgTypedAttributeValue? typed)
    {
        if (typed == null) return null;
        return typed.SourceAttribute == null ? typed.Node.SourceSpan : typed.SourceAttribute.SourceSpan;
    }
SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }
bool ValidOptions(SvgFilterResolveOptions options)
    {
        return options.Attributes != null && options.ReferenceOptions != null && options.ReferenceLimits != null && options.FilterLimits != null && options.LengthContext != null && options.StyleSheets != null && options.StyleOptions != null && options.StyleOptions.Properties != null && options.StyleOptions.Limits != null && options.ReferenceLimits.MaxEffectDepth > 0 && options.ReferenceLimits.MaxCount > 0 && options.FilterLimits.MaxPrimitives > 0 && options.FilterLimits.MaxAreaPixels > 0 && options.FilterLimits.MaxKernelElements > 0 && options.FilterLimits.MaxBlurStandardDeviation > 0 && options.FilterLimits.MaxTurbulenceOctaves > 0 && options.MaxNameCharacters > 0;
    }
void AppendTypedDiagnostics(SvgFilterResult result, SvgTypedAttributeValue typed)
    {
        int index = 0;
        while (index < typed.Diagnostics.Count)
        {
            result.Diagnostics.Add(typed.Diagnostics[index]);
            index = index + 1;
        }
    }
void AppendReferenceDiagnostics(SvgFilterResult result, SvgReferenceResult reference)
    {
        int index = 0;
        while (index < reference.Diagnostics.Count)
        {
            result.Diagnostics.Add(reference.Diagnostics[index]);
            index = index + 1;
        }
    }
void AppendStyleDiagnostics(SvgFilterResult result, SvgComputedStyle? style)
    {
        if (style == null) return;
        int index = 0;
        while (index < style.Diagnostics.Count)
        {
            result.Diagnostics.Add(style.Diagnostics[index]);
            index = index + 1;
        }
    }
void AddError(SvgFilterResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
void AddWarning(SvgFilterResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, code, message, source));
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
