namespace svg;

/// <summary>Builds typed standard SVG filter chains with deterministic results and input wiring.</summary>
public class SvgFilterBuilder
{
    SvgDocumentApi documents;
    SvgNodeApi nodes;
    System.Collections.Generic.List<string> results;

    /// <summary>The document builder that owns generated filters.</summary>
    public SvgBuilder? Builder;
    /// <summary>The element that receives the generated filter reference.</summary>
    public SvgNode? Target;
    /// <summary>The current generated filter definition.</summary>
    public SvgNode? Filter;
    /// <summary>The most recently generated top-level filter primitive.</summary>
    public SvgNode? LastPrimitive;
    /// <summary>The most recently generated function, light, or merge child.</summary>
    public SvgNode? LastChild;
    /// <summary>The automatic filter ID, or an empty string.</summary>
    public string Id;
    /// <summary>The result name of the most recent primitive, or an empty string.</summary>
    public string LastResult;
    /// <summary>The positive maximum number of generated primitives.</summary>
    public int MaximumPrimitives;
    /// <summary>The first sticky builder failure, or <c>None</c>.</summary>
    public SvgFilterBuilderError Error;
    /// <summary>The stable human-readable failure message.</summary>
    public string Message;
    /// <summary>The underlying document mutation failure, or <c>None</c>.</summary>
    public SvgMutationError MutationError;

    /// <summary>Creates a filter builder for the document builder's current element.</summary>
    /// <param name="builder">The destination document builder.</param>
    public SvgFilterBuilder(SvgBuilder? builder)
    {
        Builder = builder;
        Target = builder == null ? null : builder.Current;
        Filter = null;
        LastPrimitive = null;
        LastChild = null;
        Id = "";
        LastResult = "";
        MaximumPrimitives = 256;
        Error = SvgFilterBuilderError.None;
        Message = "";
        MutationError = SvgMutationError.None;
        documents = new SvgDocumentApi();
        nodes = new SvgNodeApi();
        results = new System.Collections.Generic.List<string>();
        if (builder == null) Fail(SvgFilterBuilderError.InvalidArgument, "A document builder is required.");
        else if (!builder.Succeeded()) Fail(SvgFilterBuilderError.DocumentBuilderFailed, "The document builder is in an error state.");
        else if (!ValidTarget(Target)) Fail(SvgFilterBuilderError.InvalidState, "An attached target element is required.");
    }

    /// <summary>Tests whether no filter-builder operation has failed.</summary>
    /// <returns><c>true</c> when the builder has no sticky error.</returns>
    internal bool Succeeded()
    {
        return Error == SvgFilterBuilderError.None;
    }

    /// <summary>Returns the number of generated top-level primitives in the current filter.</summary>
    /// <returns>The primitive count.</returns>
    internal int GetPrimitiveCount()
    {
        return results.Count;
    }

    /// <summary>Clears the sticky filter-builder error without changing generated content.</summary>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder ClearError()
    {
        Error = SvgFilterBuilderError.None;
        Message = "";
        MutationError = SvgMutationError.None;
        return this;
    }

    /// <summary>Selects another attached element in the same document as the filter target.</summary>
    /// <param name="target">The attached target element.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder UseTarget(SvgNode target)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (!ValidTarget(target)) return Fail(SvgFilterBuilderError.InvalidArgument, "The target must be an attached element in the builder document.");
        Target = target;
        return this;
    }

    /// <summary>Creates a filter definition and immediately references it from the target.</summary>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Begin()
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (MaximumPrimitives < 1) return Fail(SvgFilterBuilderError.InvalidArgument, "MaximumPrimitives must be positive.");
        if (!(Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Succeeded()) return Fail(SvgFilterBuilderError.DocumentBuilderFailed, "The document builder is in an error state.");
        if (!ValidTarget(Target)) return Fail(SvgFilterBuilderError.InvalidState, "An attached target element is required.");
        SvgDefinitionCreateResult created = SvgDefinitionBuilderSupport.Create(Builder, documents, nodes, SvgElementKind.Filter, "svg-filter-");
        if (!created.Success)
        {
            MutationError = created.MutationError;
            return Fail(SvgFilterBuilderError.MutationFailed, created.Message);
        }
        Filter = created.Definition;
        Id = created.Id;
        LastPrimitive = null;
        LastChild = null;
        LastResult = "";
        results.Clear();
        return Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, Target, SvgCssPropertyNames.Filter, SvgDefinitionBuilderSupport.Url(Id)));
    }

    /// <summary>Creates or replaces an unqualified attribute on the current filter.</summary>
    /// <param name="name">The unqualified XML attribute name.</param><param name="value">The exact attribute value.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder SetAttribute(string name, string? value)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (Filter == null) return Fail(SvgFilterBuilderError.InvalidState, "A current filter is required.");
        if (!SvgDefinitionBuilderSupport.ValidName(name) || value == null || name == SvgAttributeNames.Id) return Fail(SvgFilterBuilderError.InvalidArgument, "A valid non-ID attribute name and value are required.");
        return Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, Filter, name, value));
    }

    /// <summary>Adds any recognized top-level standard filter primitive and wires its first input to the preceding result.</summary>
    /// <param name="kind">The standard primitive element kind.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Primitive(SvgElementKind kind)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (Filter == null) return Fail(SvgFilterBuilderError.InvalidState, "Begin must create a filter before primitives are added.");
        if (!FilterPrimitive(kind)) return Fail(SvgFilterBuilderError.InvalidArgument, "A recognized top-level filter primitive kind is required.");
        if (MaximumPrimitives < 1) return Fail(SvgFilterBuilderError.InvalidArgument, "MaximumPrimitives must be positive.");
        if (results.Count >= MaximumPrimitives) return Fail(SvgFilterBuilderError.PrimitiveLimitExceeded, "The filter exceeds MaximumPrimitives.");
        string? localName = SvgDefinitionBuilderSupport.LocalName(kind);
        SvgNode primitive = documents.CreateElement((localName ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), "", (localName ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgNamespaces.Svg);
        string resultName = AllocateResultName();
        SvgMutationResult mutation = SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, primitive, SvgAttributeNames.Result, resultName);
        if (!mutation.Success) return Accept(mutation);
        if (InputCount(kind) > 0 && kind != SvgElementKind.FeMerge)
        {
            string input = LastResult.Length == 0 ? "SourceGraphic" : LastResult;
            mutation = SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, primitive, SvgAttributeNames.In, input);
            if (!mutation.Success) return Accept(mutation);
        }
        mutation = nodes.AppendChild(Filter, primitive);
        if (!mutation.Success) return Accept(mutation);
        LastPrimitive = primitive;
        LastChild = null;
        LastResult = resultName;
        results.Add(resultName);
        return this;
    }

    /// <summary>Creates or replaces an unqualified attribute on the most recent primitive.</summary>
    /// <param name="name">The unqualified XML attribute name.</param><param name="value">The exact attribute value.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder SetPrimitiveAttribute(string name, string? value)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (LastPrimitive == null) return Fail(SvgFilterBuilderError.InvalidState, "A current filter primitive is required.");
        if (!SvgDefinitionBuilderSupport.ValidName(name) || value == null || name == SvgAttributeNames.Result || name == SvgAttributeNames.In || name == SvgAttributeNames.In2) return Fail(SvgFilterBuilderError.InvalidArgument, "Use the named graph methods for result and input attributes.");
        return Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, LastPrimitive, name, value));
    }

    /// <summary>Connects the most recent primitive's first input to an available standard or preceding result.</summary>
    /// <param name="input">The standard input or preceding result name.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Input(string input)
    {
        if (Error == SvgFilterBuilderError.None && (LastPrimitive == null || InputCount(LastPrimitive.ElementKind) < 1)) return Fail(SvgFilterBuilderError.InvalidState, "The current primitive does not accept an input.");
        return SetInput(SvgAttributeNames.In, input);
    }

    /// <summary>Connects the most recent two-input primitive's second input to an available standard or preceding result.</summary>
    /// <param name="input">The standard input or preceding result name.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder SecondInput(string input)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (LastPrimitive == null || InputCount(LastPrimitive.ElementKind) < 2) return Fail(SvgFilterBuilderError.InvalidState, "The current primitive does not accept a second input.");
        return SetInput(SvgAttributeNames.In2, input);
    }

    /// <summary>Replaces the automatic result name of the most recent primitive.</summary>
    /// <param name="name">A unique nonempty result name.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Result(string? name)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (LastPrimitive == null || results.Count == 0) return Fail(SvgFilterBuilderError.InvalidState, "A current filter primitive is required.");
        string trimmed = name == null ? "" : name.Trim();
        if (!ValidResultName(trimmed) || SvgDefinitionBuilderSupport.StandardFilterInput(trimmed) || HasResult(trimmed, results.Count - 1)) return Fail(SvgFilterBuilderError.InvalidArgument, "A unique nonstandard result name without whitespace is required.");
        SvgMutationResult mutation = SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, LastPrimitive, SvgAttributeNames.Result, trimmed);
        if (!mutation.Success) return Accept(mutation);
        results[results.Count - 1] = trimmed;
        LastResult = trimmed;
        return this;
    }

    /// <summary>Adds a valid transfer-function, light-source, or merge-node child to the current primitive.</summary>
    /// <param name="kind">The supported child element kind.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Child(SvgElementKind kind)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (LastPrimitive == null || !AllowedChild(LastPrimitive.ElementKind, kind)) return Fail(SvgFilterBuilderError.InvalidState, "The requested child is not valid for the current primitive.");
        if (kind != SvgElementKind.FeMergeNode && (HasChild(kind) || LightKind(kind) && HasLightChild())) return Fail(SvgFilterBuilderError.InvalidArgument, "The current primitive already has that singleton child.");
        string? localName = SvgDefinitionBuilderSupport.LocalName(kind);
        SvgNode child = documents.CreateElement((localName ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), "", (localName ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgNamespaces.Svg);
        SvgMutationResult mutation = nodes.AppendChild(LastPrimitive, child);
        if (!mutation.Success) return Accept(mutation);
        LastChild = child;
        return this;
    }

    /// <summary>Creates or replaces an unqualified attribute on the most recent primitive child.</summary>
    /// <param name="name">The unqualified XML attribute name.</param><param name="value">The exact attribute value.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder SetChildAttribute(string name, string? value)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (LastChild == null) return Fail(SvgFilterBuilderError.InvalidState, "A current primitive child is required.");
        if (!SvgDefinitionBuilderSupport.ValidName(name) || value == null) return Fail(SvgFilterBuilderError.InvalidArgument, "A valid child attribute name and value are required.");
        return Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, LastChild, name, value));
    }

    /// <summary>Adds an feBlend primitive.</summary>
    /// <param name="secondInput">The second standard or preceding input.</param><param name="mode">The SVG blend mode.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Blend(string secondInput, string mode)
    {
        if (!AvailableInput(secondInput, results.Count) || !BlendMode(mode)) return Fail(SvgFilterBuilderError.InvalidArgument, "An available second input and supported blend mode are required.");
        Primitive(SvgElementKind.FeBlend);
        if (Error == SvgFilterBuilderError.None) SecondInput(secondInput).SetPrimitiveAttribute(SvgAttributeNames.Mode, mode.Trim());
        return this;
    }

    /// <summary>Adds an feColorMatrix primitive.</summary>
    /// <param name="type">The matrix, saturate, hueRotate, or luminanceToAlpha operation.</param><param name="values">The operation values.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder ColorMatrix(string type, double[] values)
    {
        if (!ValidColorMatrix(type, values)) return Fail(SvgFilterBuilderError.InvalidArgument, "The color-matrix type and value count are inconsistent.");
        Primitive(SvgElementKind.FeColorMatrix);
        if (Error == SvgFilterBuilderError.None)
        {
            SetPrimitiveAttribute(SvgAttributeNames.Type, type);
            if (values.Length != 0) SetPrimitiveAttribute(SvgAttributeNames.Values, NumberList(values));
        }
        return this;
    }

    /// <summary>Adds an feComponentTransfer primitive.</summary>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder ComponentTransfer()
    {
        return Primitive(SvgElementKind.FeComponentTransfer);
    }

    /// <summary>Adds a non-arithmetic feComposite primitive.</summary>
    /// <param name="secondInput">The second standard or preceding input.</param><param name="operation">The compositing operator.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Composite(string secondInput, string operation)
    {
        if (!AvailableInput(secondInput, results.Count) || !CompositeOperation(operation) || operation == "arithmetic") return Fail(SvgFilterBuilderError.InvalidArgument, "An available second input and non-arithmetic composite operator are required.");
        Primitive(SvgElementKind.FeComposite);
        if (Error == SvgFilterBuilderError.None) SecondInput(secondInput).SetPrimitiveAttribute(SvgAttributeNames.Operator, operation);
        return this;
    }

    /// <summary>Adds an arithmetic feComposite primitive.</summary>
    /// <param name="secondInput">The second standard or preceding input.</param><param name="k1">The input-product coefficient.</param><param name="k2">The first-input coefficient.</param><param name="k3">The second-input coefficient.</param><param name="k4">The constant coefficient.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder ArithmeticComposite(string secondInput, double k1, double k2, double k3, double k4)
    {
        if (!AvailableInput(secondInput, results.Count) || !Finite(new double[] { k1, k2, k3, k4 })) return Fail(SvgFilterBuilderError.InvalidArgument, "An available second input and finite coefficients are required.");
        Primitive(SvgElementKind.FeComposite);
        if (Error == SvgFilterBuilderError.None) SecondInput(secondInput).SetPrimitiveAttribute(SvgAttributeNames.Operator, "arithmetic").SetPrimitiveAttribute(SvgAttributeNames.K1, Number(k1)).SetPrimitiveAttribute(SvgAttributeNames.K2, Number(k2)).SetPrimitiveAttribute(SvgAttributeNames.K3, Number(k3)).SetPrimitiveAttribute(SvgAttributeNames.K4, Number(k4));
        return this;
    }

    /// <summary>Adds an feConvolveMatrix primitive with an explicitly sized kernel.</summary>
    /// <param name="orderX">The positive kernel width.</param><param name="orderY">The positive kernel height.</param><param name="kernel">The finite row-major kernel values.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder ConvolveMatrix(int orderX, int orderY, double[]? kernel)
    {
        long count = (long)orderX * orderY;
        if (orderX < 1 || orderY < 1 || kernel == null || count != kernel.Length || !Finite(kernel)) return Fail(SvgFilterBuilderError.InvalidArgument, "Positive kernel dimensions and one finite value per kernel cell are required.");
        Primitive(SvgElementKind.FeConvolveMatrix);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.Order, orderX.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + orderY.ToString(System.Globalization.CultureInfo.InvariantCulture)).SetPrimitiveAttribute(SvgAttributeNames.KernelMatrix, NumberList(kernel));
        return this;
    }

    /// <summary>Adds an feDiffuseLighting primitive.</summary>
    /// <param name="surfaceScale">The finite surface scale.</param><param name="diffuseConstant">The nonnegative diffuse constant.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder DiffuseLighting(double surfaceScale, double diffuseConstant)
    {
        if (!Finite(new double[] { surfaceScale, diffuseConstant }) || diffuseConstant < 0) return Fail(SvgFilterBuilderError.InvalidArgument, "A finite surface scale and nonnegative diffuse constant are required.");
        Primitive(SvgElementKind.FeDiffuseLighting);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.SurfaceScale, Number(surfaceScale)).SetPrimitiveAttribute(SvgAttributeNames.DiffuseConstant, Number(diffuseConstant));
        return this;
    }

    /// <summary>Adds an feDisplacementMap primitive.</summary>
    /// <param name="secondInput">The displacement-map input.</param><param name="scale">The finite displacement scale.</param><param name="xChannel">The R, G, B, or A x channel.</param><param name="yChannel">The R, G, B, or A y channel.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder DisplacementMap(string secondInput, double scale, string xChannel, string yChannel)
    {
        if (!AvailableInput(secondInput, results.Count) || !SvgFiniteMath.IsFinite(scale) || !Channel(xChannel) || !Channel(yChannel)) return Fail(SvgFilterBuilderError.InvalidArgument, "An available map input, finite scale, and RGBA channels are required.");
        Primitive(SvgElementKind.FeDisplacementMap);
        if (Error == SvgFilterBuilderError.None) SecondInput(secondInput).SetPrimitiveAttribute(SvgAttributeNames.Scale, Number(scale)).SetPrimitiveAttribute(SvgAttributeNames.XChannelSelector, xChannel).SetPrimitiveAttribute(SvgAttributeNames.YChannelSelector, yChannel);
        return this;
    }

    /// <summary>Adds an feDropShadow primitive.</summary>
    /// <param name="dx">The finite x offset.</param><param name="dy">The finite y offset.</param><param name="standardDeviation">The nonnegative blur deviation.</param><param name="color">A valid flood color.</param><param name="opacity">The opacity from zero through one.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder DropShadow(double dx, double dy, double standardDeviation, string color, double opacity)
    {
        if (!ValidShadow(dx, dy, standardDeviation, color, opacity)) return Fail(SvgFilterBuilderError.InvalidArgument, "Finite shadow geometry, a valid color, and bounded opacity are required.");
        Primitive(SvgElementKind.FeDropShadow);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.Dx, Number(dx)).SetPrimitiveAttribute(SvgAttributeNames.Dy, Number(dy)).SetPrimitiveAttribute(SvgAttributeNames.StdDeviation, Number(standardDeviation)).SetPrimitiveAttribute(SvgCssPropertyNames.FloodColor, color.Trim()).SetPrimitiveAttribute(SvgCssPropertyNames.FloodOpacity, Number(opacity));
        return this;
    }

    /// <summary>Adds an feFlood primitive.</summary>
    /// <param name="color">A valid flood color.</param><param name="opacity">The opacity from zero through one.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Flood(string color, double opacity)
    {
        if (!ValidColorOpacity(color, opacity)) return Fail(SvgFilterBuilderError.InvalidArgument, "A valid color and opacity from zero through one are required.");
        Primitive(SvgElementKind.FeFlood);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgCssPropertyNames.FloodColor, color.Trim()).SetPrimitiveAttribute(SvgCssPropertyNames.FloodOpacity, Number(opacity));
        return this;
    }

    /// <summary>Adds an isotropic feGaussianBlur primitive.</summary>
    /// <param name="standardDeviation">The nonnegative blur deviation.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder GaussianBlur(double standardDeviation)
    {
        return GaussianBlurAxes(standardDeviation, standardDeviation);
    }

    /// <summary>Adds an anisotropic feGaussianBlur primitive.</summary>
    /// <param name="deviationX">The nonnegative horizontal deviation.</param><param name="deviationY">The nonnegative vertical deviation.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder GaussianBlurAxes(double deviationX, double deviationY)
    {
        if (!Finite(new double[] { deviationX, deviationY }) || deviationX < 0 || deviationY < 0) return Fail(SvgFilterBuilderError.InvalidArgument, "Finite nonnegative blur deviations are required.");
        Primitive(SvgElementKind.FeGaussianBlur);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.StdDeviation, Number(deviationX) + " " + Number(deviationY));
        return this;
    }

    /// <summary>Adds an feImage primitive.</summary>
    /// <param name="href">The nonempty image or element reference.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Image(string href)
    {
        if (string.IsNullOrWhiteSpace(href)) return Fail(SvgFilterBuilderError.InvalidArgument, "A nonempty image reference is required.");
        Primitive(SvgElementKind.FeImage);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.Href, href.Trim());
        return this;
    }

    /// <summary>Adds an feMerge primitive with ordered named inputs.</summary>
    /// <param name="inputs">One or more available standard or preceding inputs.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Merge(string[]? inputs)
    {
        if (inputs == null || inputs.Length == 0) return Fail(SvgFilterBuilderError.InvalidArgument, "At least one merge input is required.");
        int index = 0;
        while (index < inputs.Length)
        {
            if (!AvailableInput(inputs[index], results.Count)) return Fail(SvgFilterBuilderError.InvalidArgument, "Every merge input must be standard or produced by a preceding primitive.");
            index = index + 1;
        }
        Primitive(SvgElementKind.FeMerge);
        index = 0;
        while (index < inputs.Length && Error == SvgFilterBuilderError.None)
        {
            Child(SvgElementKind.FeMergeNode).SetChildAttribute(SvgAttributeNames.In, inputs[index].Trim());
            index = index + 1;
        }
        return this;
    }

    /// <summary>Adds an isotropic feMorphology primitive.</summary>
    /// <param name="operation">The erode or dilate operation.</param><param name="radius">The nonnegative morphology radius.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Morphology(string operation, double radius)
    {
        return MorphologyAxes(operation, radius, radius);
    }

    /// <summary>Adds an anisotropic feMorphology primitive.</summary>
    /// <param name="operation">The erode or dilate operation.</param><param name="radiusX">The nonnegative horizontal radius.</param><param name="radiusY">The nonnegative vertical radius.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder MorphologyAxes(string operation, double radiusX, double radiusY)
    {
        if (operation != "erode" && operation != "dilate" || !Finite(new double[] { radiusX, radiusY }) || radiusX < 0 || radiusY < 0) return Fail(SvgFilterBuilderError.InvalidArgument, "An erode or dilate operation and finite nonnegative radii are required.");
        Primitive(SvgElementKind.FeMorphology);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.Operator, operation).SetPrimitiveAttribute(SvgAttributeNames.Radius, Number(radiusX) + " " + Number(radiusY));
        return this;
    }

    /// <summary>Adds an feOffset primitive.</summary>
    /// <param name="dx">The finite x offset.</param><param name="dy">The finite y offset.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Offset(double dx, double dy)
    {
        if (!Finite(new double[] { dx, dy })) return Fail(SvgFilterBuilderError.InvalidArgument, "Finite offsets are required.");
        Primitive(SvgElementKind.FeOffset);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.Dx, Number(dx)).SetPrimitiveAttribute(SvgAttributeNames.Dy, Number(dy));
        return this;
    }

    /// <summary>Adds an feSpecularLighting primitive.</summary>
    /// <param name="surfaceScale">The finite surface scale.</param><param name="specularConstant">The nonnegative specular constant.</param><param name="specularExponent">The exponent from one through 128.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder SpecularLighting(double surfaceScale, double specularConstant, double specularExponent)
    {
        if (!Finite(new double[] { surfaceScale, specularConstant, specularExponent }) || specularConstant < 0 || specularExponent < 1 || specularExponent > 128) return Fail(SvgFilterBuilderError.InvalidArgument, "Valid finite specular-lighting parameters are required.");
        Primitive(SvgElementKind.FeSpecularLighting);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.SurfaceScale, Number(surfaceScale)).SetPrimitiveAttribute(SvgAttributeNames.SpecularConstant, Number(specularConstant)).SetPrimitiveAttribute(SvgAttributeNames.SpecularExponent, Number(specularExponent));
        return this;
    }

    /// <summary>Adds an feTile primitive.</summary>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Tile()
    {
        return Primitive(SvgElementKind.FeTile);
    }

    /// <summary>Adds an feTurbulence primitive.</summary>
    /// <param name="baseFrequency">The nonnegative base frequency.</param><param name="octaves">The positive octave count.</param><param name="seed">The finite seed.</param><param name="type">The turbulence or fractalNoise type.</param><param name="stitchTiles">Whether tile boundaries are stitched.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder Turbulence(double baseFrequency, int octaves, double seed, string type, bool stitchTiles)
    {
        if (!Finite(new double[] { baseFrequency, seed }) || baseFrequency < 0 || octaves < 1 || type != "turbulence" && type != "fractalNoise") return Fail(SvgFilterBuilderError.InvalidArgument, "Valid turbulence frequency, octave, seed, and type values are required.");
        Primitive(SvgElementKind.FeTurbulence);
        if (Error == SvgFilterBuilderError.None) SetPrimitiveAttribute(SvgAttributeNames.BaseFrequency, Number(baseFrequency)).SetPrimitiveAttribute(SvgAttributeNames.NumOctaves, octaves.ToString(System.Globalization.CultureInfo.InvariantCulture)).SetPrimitiveAttribute(SvgAttributeNames.Seed, Number(seed)).SetPrimitiveAttribute(SvgAttributeNames.Type, type).SetPrimitiveAttribute(SvgAttributeNames.StitchTiles, stitchTiles ? "stitch" : "noStitch");
        return this;
    }

    /// <summary>Adds an identity component-transfer function for one RGBA channel kind.</summary>
    /// <param name="channel">An feFuncR, feFuncG, feFuncB, or feFuncA kind.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder IdentityFunction(SvgElementKind channel)
    {
        return TransferFunction(channel, "identity");
    }

    /// <summary>Adds a table component-transfer function.</summary>
    /// <param name="channel">An RGBA transfer-function kind.</param><param name="values">One or more finite table values.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder TableFunction(SvgElementKind channel, double[]? values)
    {
        if (values == null || values.Length == 0 || !Finite(values)) return Fail(SvgFilterBuilderError.InvalidArgument, "One or more finite table values are required.");
        TransferFunction(channel, "table");
        if (Error == SvgFilterBuilderError.None) SetChildAttribute(SvgAttributeNames.TableValues, NumberList(values));
        return this;
    }

    /// <summary>Adds a discrete component-transfer function.</summary>
    /// <param name="channel">An RGBA transfer-function kind.</param><param name="values">One or more finite step values.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder DiscreteFunction(SvgElementKind channel, double[]? values)
    {
        if (values == null || values.Length == 0 || !Finite(values)) return Fail(SvgFilterBuilderError.InvalidArgument, "One or more finite discrete values are required.");
        TransferFunction(channel, "discrete");
        if (Error == SvgFilterBuilderError.None) SetChildAttribute(SvgAttributeNames.TableValues, NumberList(values));
        return this;
    }

    /// <summary>Adds a linear component-transfer function.</summary>
    /// <param name="channel">An RGBA transfer-function kind.</param><param name="slope">The finite slope.</param><param name="intercept">The finite intercept.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder LinearFunction(SvgElementKind channel, double slope, double intercept)
    {
        if (!Finite(new double[] { slope, intercept })) return Fail(SvgFilterBuilderError.InvalidArgument, "Finite linear transfer parameters are required.");
        TransferFunction(channel, "linear");
        if (Error == SvgFilterBuilderError.None) SetChildAttribute(SvgAttributeNames.Slope, Number(slope)).SetChildAttribute(SvgAttributeNames.Intercept, Number(intercept));
        return this;
    }

    /// <summary>Adds a gamma component-transfer function.</summary>
    /// <param name="channel">An RGBA transfer-function kind.</param><param name="amplitude">The finite amplitude.</param><param name="exponent">The finite exponent.</param><param name="offset">The finite offset.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder GammaFunction(SvgElementKind channel, double amplitude, double exponent, double offset)
    {
        if (!Finite(new double[] { amplitude, exponent, offset })) return Fail(SvgFilterBuilderError.InvalidArgument, "Finite gamma transfer parameters are required.");
        TransferFunction(channel, "gamma");
        if (Error == SvgFilterBuilderError.None) SetChildAttribute(SvgAttributeNames.Amplitude, Number(amplitude)).SetChildAttribute(SvgAttributeNames.Exponent, Number(exponent)).SetChildAttribute(SvgAttributeNames.Offset, Number(offset));
        return this;
    }

    /// <summary>Adds an feDistantLight child to the current lighting primitive.</summary>
    /// <param name="azimuth">The finite azimuth in degrees.</param><param name="elevation">The finite elevation in degrees.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder DistantLight(double azimuth, double elevation)
    {
        if (!Finite(new double[] { azimuth, elevation })) return Fail(SvgFilterBuilderError.InvalidArgument, "Finite distant-light angles are required.");
        Child(SvgElementKind.FeDistantLight);
        if (Error == SvgFilterBuilderError.None) SetChildAttribute(SvgAttributeNames.Azimuth, Number(azimuth)).SetChildAttribute(SvgAttributeNames.Elevation, Number(elevation));
        return this;
    }

    /// <summary>Adds an fePointLight child to the current lighting primitive.</summary>
    /// <param name="x">The finite x coordinate.</param><param name="y">The finite y coordinate.</param><param name="z">The finite z coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder PointLight(double x, double y, double z)
    {
        if (!Finite(new double[] { x, y, z })) return Fail(SvgFilterBuilderError.InvalidArgument, "Finite point-light coordinates are required.");
        Child(SvgElementKind.FePointLight);
        if (Error == SvgFilterBuilderError.None) SetChildAttribute(SvgAttributeNames.X, Number(x)).SetChildAttribute(SvgAttributeNames.Y, Number(y)).SetChildAttribute("z", Number(z));
        return this;
    }

    /// <summary>Adds an feSpotLight child to the current lighting primitive.</summary>
    /// <param name="x">The light x coordinate.</param><param name="y">The light y coordinate.</param><param name="z">The light z coordinate.</param><param name="targetX">The target x coordinate.</param><param name="targetY">The target y coordinate.</param><param name="targetZ">The target z coordinate.</param><param name="specularExponent">The finite specular exponent.</param><param name="coneAngle">The finite limiting cone angle.</param>
    /// <returns>This builder.</returns>
    internal SvgFilterBuilder SpotLight(double x, double y, double z, double targetX, double targetY, double targetZ, double specularExponent, double coneAngle)
    {
        if (!Finite(new double[] { x, y, z, targetX, targetY, targetZ, specularExponent, coneAngle })) return Fail(SvgFilterBuilderError.InvalidArgument, "Finite spot-light parameters are required.");
        Child(SvgElementKind.FeSpotLight);
        if (Error == SvgFilterBuilderError.None) SetChildAttribute(SvgAttributeNames.X, Number(x)).SetChildAttribute(SvgAttributeNames.Y, Number(y)).SetChildAttribute("z", Number(z)).SetChildAttribute("pointsAtX", Number(targetX)).SetChildAttribute("pointsAtY", Number(targetY)).SetChildAttribute("pointsAtZ", Number(targetZ)).SetChildAttribute(SvgAttributeNames.SpecularExponent, Number(specularExponent)).SetChildAttribute("limitingConeAngle", Number(coneAngle));
        return this;
    }

    /// <summary>Returns the current attached filter definition, or <c>null</c> after failure.</summary>
    /// <returns>The generated filter.</returns>
    internal SvgNode? Build()
    {
        return Error == SvgFilterBuilderError.None ? Filter : null;
    }

    internal SvgFilterBuilder TransferFunction(SvgElementKind channel, string type)
    {
        if (!TransferKind(channel) || LastPrimitive == null || LastPrimitive.ElementKind != SvgElementKind.FeComponentTransfer) return Fail(SvgFilterBuilderError.InvalidState, "A current component-transfer primitive and RGBA channel are required.");
        Child(channel);
        if (Error == SvgFilterBuilderError.None) SetChildAttribute(SvgAttributeNames.Type, type);
        return this;
    }

    internal SvgFilterBuilder SetInput(string attributeName, string input)
    {
        if (Error != SvgFilterBuilderError.None) return this;
        if (LastPrimitive == null) return Fail(SvgFilterBuilderError.InvalidState, "A current filter primitive is required.");
        if (!AvailableInput(input, results.Count - 1)) return Fail(SvgFilterBuilderError.InvalidArgument, "The input must be standard or produced by a preceding primitive.");
        return Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, LastPrimitive, attributeName, input.Trim()));
    }

    internal SvgFilterBuilder Accept(SvgMutationResult mutation)
    {
        if (mutation.Success) return this;
        MutationError = mutation.Error;
        return Fail(SvgFilterBuilderError.MutationFailed, mutation.Message);
    }

    internal SvgFilterBuilder Fail(SvgFilterBuilderError error, string message)
    {
        if (Error == SvgFilterBuilderError.None)
        {
            Error = error;
            Message = message;
        }
        return this;
    }

    internal bool ValidTarget(SvgNode? target)
    {
        return Builder != null && Builder.Document != null && target != null && target.Kind == SvgNodeKind.Element && object.ReferenceEquals(target.OwnerDocument, Builder.Document);
    }

    internal bool AvailableInput(string input, int count)
    {
        if (input == null) return false;
        string trimmed = input.Trim();
        if (trimmed.Length == 0) return false;
        if (SvgDefinitionBuilderSupport.StandardFilterInput(trimmed)) return true;
        return HasResult(trimmed, count);
    }

    internal bool HasResult(string name, int count)
    {
        int index = 0;
        while (index < count && index < results.Count)
        {
            if (results[index] == name) return true;
            index = index + 1;
        }
        return false;
    }

    internal bool HasChild(SvgElementKind kind)
    {
        int index = 0;
        while (index < (LastPrimitive ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            if (LastPrimitive.Children[index].Kind == SvgNodeKind.Element && LastPrimitive.Children[index].ElementKind == kind) return true;
            index = index + 1;
        }
        return false;
    }

    internal bool HasLightChild()
    {
        int index = 0;
        while (index < (LastPrimitive ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Count)
        {
            if (LastPrimitive.Children[index].Kind == SvgNodeKind.Element && LightKind(LastPrimitive.Children[index].ElementKind)) return true;
            index = index + 1;
        }
        return false;
    }

    internal string AllocateResultName()
    {
        int suffix = results.Count + 1;
        string candidate = "svg-filter-result-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        while (HasResult(candidate, results.Count))
        {
            suffix = suffix + 1;
            candidate = "svg-filter-result-" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return candidate;
    }

    internal static bool FilterPrimitive(SvgElementKind kind)
    {
        return kind == SvgElementKind.FeBlend || kind == SvgElementKind.FeColorMatrix || kind == SvgElementKind.FeComponentTransfer || kind == SvgElementKind.FeComposite || kind == SvgElementKind.FeConvolveMatrix || kind == SvgElementKind.FeDiffuseLighting || kind == SvgElementKind.FeDisplacementMap || kind == SvgElementKind.FeDropShadow || kind == SvgElementKind.FeFlood || kind == SvgElementKind.FeGaussianBlur || kind == SvgElementKind.FeImage || kind == SvgElementKind.FeMerge || kind == SvgElementKind.FeMorphology || kind == SvgElementKind.FeOffset || kind == SvgElementKind.FeSpecularLighting || kind == SvgElementKind.FeTile || kind == SvgElementKind.FeTurbulence;
    }

    internal static int InputCount(SvgElementKind kind)
    {
        if (kind == SvgElementKind.FeFlood || kind == SvgElementKind.FeImage || kind == SvgElementKind.FeTurbulence || kind == SvgElementKind.FeMerge) return 0;
        if (kind == SvgElementKind.FeBlend || kind == SvgElementKind.FeComposite || kind == SvgElementKind.FeDisplacementMap) return 2;
        return 1;
    }

    internal static bool AllowedChild(SvgElementKind parent, SvgElementKind child)
    {
        if (parent == SvgElementKind.FeMerge) return child == SvgElementKind.FeMergeNode;
        if (parent == SvgElementKind.FeComponentTransfer) return TransferKind(child);
        if (parent == SvgElementKind.FeDiffuseLighting || parent == SvgElementKind.FeSpecularLighting) return child == SvgElementKind.FeDistantLight || child == SvgElementKind.FePointLight || child == SvgElementKind.FeSpotLight;
        return false;
    }

    internal static bool TransferKind(SvgElementKind kind)
    {
        return kind == SvgElementKind.FeFuncR || kind == SvgElementKind.FeFuncG || kind == SvgElementKind.FeFuncB || kind == SvgElementKind.FeFuncA;
    }

    internal static bool LightKind(SvgElementKind kind)
    {
        return kind == SvgElementKind.FeDistantLight || kind == SvgElementKind.FePointLight || kind == SvgElementKind.FeSpotLight;
    }

    internal static bool BlendMode(string value)
    {
        return value == "normal" || value == "multiply" || value == "screen" || value == "overlay" || value == "darken" || value == "lighten" || value == "color-dodge" || value == "color-burn" || value == "hard-light" || value == "soft-light" || value == "difference" || value == "exclusion" || value == "hue" || value == "saturation" || value == "color" || value == "luminosity" || value == "plus-darker" || value == "plus-lighter";
    }

    internal static bool CompositeOperation(string value)
    {
        return value == "over" || value == "in" || value == "out" || value == "atop" || value == "xor" || value == "lighter" || value == "arithmetic";
    }

    internal static bool Channel(string value)
    {
        return value == "R" || value == "G" || value == "B" || value == "A";
    }

    internal static bool ValidColorMatrix(string type, double[]? values)
    {
        if (values == null || !Finite(values)) return false;
        if (type == "matrix") return values.Length == 20;
        if (type == "saturate" || type == "hueRotate") return values.Length == 1;
        return type == "luminanceToAlpha" && values.Length == 0;
    }

    internal static bool ValidShadow(double dx, double dy, double deviation, string color, double opacity)
    {
        return Finite(new double[] { dx, dy, deviation }) && deviation >= 0 && ValidColorOpacity(color, opacity);
    }

    internal static bool ValidColorOpacity(string color, double opacity)
    {
        return color != null && new SvgColorParser().Parse(color, 4096).Success && SvgFiniteMath.IsFinite(opacity) && opacity >= 0 && opacity <= 1;
    }

    internal static bool ValidResultName(string? value)
    {
        if (value == null || value.Length == 0 || value.Length > 4096) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsWhiteSpace(value[index])) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool Finite(double[]? values)
    {
        if (values == null) return false;
        int index = 0;
        while (index < values.Length)
        {
            if (!SvgFiniteMath.IsFinite(values[index])) return false;
            index = index + 1;
        }
        return true;
    }

    internal static string Number(double value)
    {
        return SvgNumberFormatter.FormatShortest(value).Text;
    }

    internal static string NumberList(double[] values)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < values.Length)
        {
            if (index != 0) text.Append(' ');
            text.Append(Number(values[index]));
            index = index + 1;
        }
        return text.ToString();
    }
}
