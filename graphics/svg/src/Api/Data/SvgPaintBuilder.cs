namespace svg;

/// <summary>Builds paint servers, markers, clips, masks, and their references in an editable SVG document.</summary>
/// <remarks>Generated definitions are stored in one root <c>defs</c> element and receive deterministic collision-free IDs.</remarks>
public class SvgPaintBuilder
{
    SvgDocumentApi documents;
    SvgNodeApi nodes;

    /// <summary>The document builder that owns generated definitions.</summary>
    public SvgBuilder? Builder;
    /// <summary>The element that receives paint or effect references.</summary>
    public SvgNode? Target;
    /// <summary>The most recently generated definition.</summary>
    public SvgNode? Definition;
    /// <summary>The automatic ID of the current definition, or an empty string.</summary>
    public string Id;
    /// <summary>The first sticky builder failure, or <c>None</c>.</summary>
    public SvgPaintBuilderError Error;
    /// <summary>The stable human-readable failure message.</summary>
    public string Message;
    /// <summary>The underlying document mutation failure, or <c>None</c>.</summary>
    public SvgMutationError MutationError;

    /// <summary>Creates a paint-definition builder for the document builder's current element.</summary>
    /// <param name="builder">The destination document builder.</param>
    public SvgPaintBuilder(SvgBuilder? builder)
    {
        Builder = builder;
        Target = builder == null ? null : builder.Current;
        Definition = null;
        Id = "";
        Error = SvgPaintBuilderError.None;
        Message = "";
        MutationError = SvgMutationError.None;
        documents = new SvgDocumentApi();
        nodes = new SvgNodeApi();
        if (builder == null) Fail(SvgPaintBuilderError.InvalidArgument, "A document builder is required.");
        else if (!builder.Succeeded()) Fail(SvgPaintBuilderError.DocumentBuilderFailed, "The document builder is in an error state.");
        else if (!ValidTarget(Target)) Fail(SvgPaintBuilderError.InvalidState, "An attached target element is required.");
    }

    /// <summary>Tests whether no paint-builder operation has failed.</summary>
    /// <returns><c>true</c> when the builder has no sticky error.</returns>
    internal bool Succeeded()
    {
        return Error == SvgPaintBuilderError.None;
    }

    /// <summary>Clears the sticky paint-builder error without changing generated content.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder ClearError()
    {
        Error = SvgPaintBuilderError.None;
        Message = "";
        MutationError = SvgMutationError.None;
        return this;
    }

    /// <summary>Selects another attached element in the same document as the reference target.</summary>
    /// <param name="target">The attached target element.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder UseTarget(SvgNode target)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (!ValidTarget(target)) return Fail(SvgPaintBuilderError.InvalidArgument, "The target must be an attached element in the builder document.");
        Target = target;
        return this;
    }

    /// <summary>Creates a linear gradient using SVG's default geometry.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder LinearGradient()
    {
        return Create(SvgElementKind.LinearGradient, "svg-gradient-");
    }

    /// <summary>Creates a linear gradient with numeric geometry.</summary>
    /// <param name="x1">The starting x coordinate.</param><param name="y1">The starting y coordinate.</param><param name="x2">The ending x coordinate.</param><param name="y2">The ending y coordinate.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder LinearGradientWithCoordinates(double x1, double y1, double x2, double y2)
    {
        if (!Finite(new double[] { x1, y1, x2, y2 })) return Fail(SvgPaintBuilderError.InvalidArgument, "Finite gradient coordinates are required.");
        Create(SvgElementKind.LinearGradient, "svg-gradient-");
        if (Error == SvgPaintBuilderError.None) SetAttribute(SvgAttributeNames.X1, Number(x1)).SetAttribute(SvgAttributeNames.Y1, Number(y1)).SetAttribute(SvgAttributeNames.X2, Number(x2)).SetAttribute(SvgAttributeNames.Y2, Number(y2));
        return this;
    }

    /// <summary>Creates a radial gradient using SVG's default geometry.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder RadialGradient()
    {
        return Create(SvgElementKind.RadialGradient, "svg-gradient-");
    }

    /// <summary>Creates a radial gradient with numeric end and focal circles.</summary>
    /// <param name="centerX">The end-circle center x coordinate.</param><param name="centerY">The end-circle center y coordinate.</param><param name="radius">The nonnegative end radius.</param><param name="focusX">The focal center x coordinate.</param><param name="focusY">The focal center y coordinate.</param><param name="focalRadius">The nonnegative focal radius no greater than the end radius.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder RadialGradientWithGeometry(double centerX, double centerY, double radius, double focusX, double focusY, double focalRadius)
    {
        if (!Finite(new double[] { centerX, centerY, radius, focusX, focusY, focalRadius }) || radius < 0 || focalRadius < 0 || focalRadius > radius) return Fail(SvgPaintBuilderError.InvalidArgument, "Finite radial-gradient geometry with valid nonnegative radii is required.");
        Create(SvgElementKind.RadialGradient, "svg-gradient-");
        if (Error == SvgPaintBuilderError.None) SetAttribute(SvgAttributeNames.Cx, Number(centerX)).SetAttribute(SvgAttributeNames.Cy, Number(centerY)).SetAttribute(SvgAttributeNames.R, Number(radius)).SetAttribute(SvgAttributeNames.Fx, Number(focusX)).SetAttribute(SvgAttributeNames.Fy, Number(focusY)).SetAttribute(SvgAttributeNames.Fr, Number(focalRadius));
        return this;
    }

    /// <summary>Creates a rectangular pattern definition.</summary>
    /// <param name="x">The tile x coordinate.</param><param name="y">The tile y coordinate.</param><param name="width">The nonnegative tile width.</param><param name="height">The nonnegative tile height.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder Pattern(double x, double y, double width, double height)
    {
        if (!Finite(new double[] { x, y, width, height }) || width < 0 || height < 0) return Fail(SvgPaintBuilderError.InvalidArgument, "Finite pattern coordinates and nonnegative dimensions are required.");
        Create(SvgElementKind.Pattern, "svg-pattern-");
        if (Error == SvgPaintBuilderError.None) SetAttribute(SvgAttributeNames.X, Number(x)).SetAttribute(SvgAttributeNames.Y, Number(y)).SetAttribute(SvgAttributeNames.Width, Number(width)).SetAttribute(SvgAttributeNames.Height, Number(height));
        return this;
    }

    /// <summary>Creates a marker definition with numeric viewport geometry.</summary>
    /// <param name="markerWidth">The nonnegative marker viewport width.</param><param name="markerHeight">The nonnegative marker viewport height.</param><param name="referenceX">The reference x coordinate.</param><param name="referenceY">The reference y coordinate.</param><param name="orient">The marker orientation text.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder Marker(double markerWidth, double markerHeight, double referenceX, double referenceY, string orient)
    {
        if (!Finite(new double[] { markerWidth, markerHeight, referenceX, referenceY }) || markerWidth < 0 || markerHeight < 0 || string.IsNullOrWhiteSpace(orient)) return Fail(SvgPaintBuilderError.InvalidArgument, "Finite marker geometry and an orientation are required.");
        Create(SvgElementKind.Marker, "svg-marker-");
        if (Error == SvgPaintBuilderError.None) SetAttribute(SvgAttributeNames.MarkerWidth, Number(markerWidth)).SetAttribute(SvgAttributeNames.MarkerHeight, Number(markerHeight)).SetAttribute(SvgAttributeNames.RefX, Number(referenceX)).SetAttribute(SvgAttributeNames.RefY, Number(referenceY)).SetAttribute(SvgAttributeNames.Orient, orient.Trim());
        return this;
    }

    /// <summary>Creates a clip-path definition.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder ClipPath()
    {
        return Create(SvgElementKind.ClipPath, "svg-clip-");
    }

    /// <summary>Creates a mask definition using SVG's default region.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder Mask()
    {
        return Create(SvgElementKind.Mask, "svg-mask-");
    }

    /// <summary>Creates a mask definition with numeric region geometry.</summary>
    /// <param name="x">The mask-region x coordinate.</param><param name="y">The mask-region y coordinate.</param><param name="width">The nonnegative mask-region width.</param><param name="height">The nonnegative mask-region height.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder MaskWithBounds(double x, double y, double width, double height)
    {
        if (!Finite(new double[] { x, y, width, height }) || width < 0 || height < 0) return Fail(SvgPaintBuilderError.InvalidArgument, "Finite mask coordinates and nonnegative dimensions are required.");
        Create(SvgElementKind.Mask, "svg-mask-");
        if (Error == SvgPaintBuilderError.None) SetAttribute(SvgAttributeNames.X, Number(x)).SetAttribute(SvgAttributeNames.Y, Number(y)).SetAttribute(SvgAttributeNames.Width, Number(width)).SetAttribute(SvgAttributeNames.Height, Number(height));
        return this;
    }

    /// <summary>Creates or replaces an unqualified attribute on the current definition.</summary>
    /// <param name="name">The unqualified XML attribute name.</param><param name="value">The exact attribute value.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder SetAttribute(string name, string? value)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (Definition == null) return Fail(SvgPaintBuilderError.InvalidState, "A current definition is required.");
        if (!SvgDefinitionBuilderSupport.ValidName(name) || value == null || name == SvgAttributeNames.Id) return Fail(SvgPaintBuilderError.InvalidArgument, "A valid non-ID attribute name and value are required.");
        return Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, Definition, name, value));
    }

    /// <summary>Adds an opaque color stop to the current gradient.</summary>
    /// <param name="offset">The stop offset from zero through one.</param><param name="color">A valid CSS color.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder AddStop(double offset, string color)
    {
        return AddStopWithOpacity(offset, color, 1);
    }

    /// <summary>Adds a color stop with explicit opacity to the current gradient.</summary>
    /// <param name="offset">The stop offset from zero through one.</param><param name="color">A valid CSS color.</param><param name="opacity">The opacity from zero through one.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder AddStopWithOpacity(double offset, string? color, double opacity)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (!GradientDefinition()) return Fail(SvgPaintBuilderError.InvalidState, "A current linear or radial gradient is required.");
        if (!SvgFiniteMath.IsFinite(offset) || !SvgFiniteMath.IsFinite(opacity) || offset < 0 || offset > 1 || opacity < 0 || opacity > 1 || color == null || !new SvgColorParser().Parse(color, 4096).Success) return Fail(SvgPaintBuilderError.InvalidArgument, "A valid color, offset, and opacity are required.");
        SvgNode stop = documents.CreateElement(SvgElementNames.Stop, "", SvgElementNames.Stop, SvgNamespaces.Svg);
        SvgMutationResult mutation = SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, stop, SvgAttributeNames.Offset, Number(offset));
        if (!mutation.Success) return Accept(mutation);
        mutation = SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, stop, SvgCssPropertyNames.StopColor, color.Trim());
        if (!mutation.Success) return Accept(mutation);
        if (opacity != 1)
        {
            mutation = SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, stop, SvgCssPropertyNames.StopOpacity, Number(opacity));
            if (!mutation.Success) return Accept(mutation);
        }
        return Accept(nodes.AppendChild(Definition, stop));
    }

    /// <summary>Appends one detached node to the current pattern, marker, clip path, or mask.</summary>
    /// <param name="node">The detached definition content node.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder AddContent(SvgNode? node)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (!ContentDefinition()) return Fail(SvgPaintBuilderError.InvalidState, "A current pattern, marker, clip path, or mask is required.");
        if (node == null || node.Kind == SvgNodeKind.Document || node.Parent != null) return Fail(SvgPaintBuilderError.InvalidArgument, "Detached non-document definition content is required.");
        return Accept(nodes.AppendChild(Definition, node));
    }

    /// <summary>Runs document-builder operations positioned inside the current pattern, marker, clip path, or mask.</summary>
    /// <param name="content">The synchronous content-generation callback.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder AddContentWithBuilder(System.Action<SvgBuilder>? content)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (!ContentDefinition()) return Fail(SvgPaintBuilderError.InvalidState, "A current pattern, marker, clip path, or mask is required.");
        if (content == null) return Fail(SvgPaintBuilderError.InvalidArgument, "A content callback is required.");
        SvgNode? restore = (Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Current;
        Builder.Current = Definition;
        Builder.LastNode = Definition;
        if (Builder.Succeeded()) content(Builder);
        Builder.Current = restore;
        if (!Builder.Succeeded()) return Fail(SvgPaintBuilderError.DocumentBuilderFailed, Builder.Message);
        return this;
    }

    /// <summary>Sets a validated fill paint value on the target.</summary>
    /// <param name="paint">The complete SVG paint text.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder Fill(string paint)
    {
        return SetPaint(SvgCssPropertyNames.Fill, paint);
    }

    /// <summary>Sets a validated stroke paint value on the target.</summary>
    /// <param name="paint">The complete SVG paint text.</param>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder Stroke(string paint)
    {
        return SetPaint(SvgCssPropertyNames.Stroke, paint);
    }

    /// <summary>Applies the current gradient or pattern as the target fill.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder FillLastElement()
    {
        return ApplyPaint(SvgCssPropertyNames.Fill);
    }

    /// <summary>Applies the current gradient or pattern as the target stroke.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder StrokeLastElement()
    {
        return ApplyPaint(SvgCssPropertyNames.Stroke);
    }

    /// <summary>Applies the current marker to the target's start vertices.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder ApplyMarkerStart()
    {
        return ApplyKind(SvgElementKind.Marker, SvgCssPropertyNames.MarkerStart);
    }

    /// <summary>Applies the current marker to the target's middle vertices.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder ApplyMarkerMid()
    {
        return ApplyKind(SvgElementKind.Marker, SvgCssPropertyNames.MarkerMid);
    }

    /// <summary>Applies the current marker to the target's end vertices.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder ApplyMarkerEnd()
    {
        return ApplyKind(SvgElementKind.Marker, SvgCssPropertyNames.MarkerEnd);
    }

    /// <summary>Applies the current marker to every target vertex position.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder ApplyMarker()
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (Definition == null || Definition.ElementKind != SvgElementKind.Marker) return Fail(SvgPaintBuilderError.InvalidState, "A current marker definition is required.");
        ApplyReference(SvgCssPropertyNames.MarkerStart);
        ApplyReference(SvgCssPropertyNames.MarkerMid);
        ApplyReference(SvgCssPropertyNames.MarkerEnd);
        return this;
    }

    /// <summary>Applies the current clip path to the target.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder ApplyClipPath()
    {
        return ApplyKind(SvgElementKind.ClipPath, SvgCssPropertyNames.ClipPath);
    }

    /// <summary>Applies the current mask to the target.</summary>
    /// <returns>This builder.</returns>
    internal SvgPaintBuilder ApplyMask()
    {
        return ApplyKind(SvgElementKind.Mask, SvgCssPropertyNames.Mask);
    }

    /// <summary>Returns the current attached definition, or <c>null</c>.</summary>
    /// <returns>The most recently generated definition.</returns>
    internal SvgNode? Build()
    {
        return Error == SvgPaintBuilderError.None ? Definition : null;
    }

    internal SvgPaintBuilder Create(SvgElementKind kind, string prefix)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (!(Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Succeeded()) return Fail(SvgPaintBuilderError.DocumentBuilderFailed, "The document builder is in an error state.");
        SvgDefinitionCreateResult created = SvgDefinitionBuilderSupport.Create(Builder, documents, nodes, kind, prefix);
        if (!created.Success)
        {
            MutationError = created.MutationError;
            return Fail(SvgPaintBuilderError.MutationFailed, created.Message);
        }
        Definition = created.Definition;
        Id = created.Id;
        return this;
    }

    internal SvgPaintBuilder SetPaint(string name, string paint)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        SvgPaintParseResult parsed = new SvgPaintParser().Parse(paint, 32, 4096, false);
        if (!parsed.Success) return Fail(SvgPaintBuilderError.InvalidArgument, "A complete valid paint value is required.");
        return SetTargetAttribute(name, (parsed.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText());
    }

    internal SvgPaintBuilder ApplyPaint(string name)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (!GradientDefinition() && (Definition == null || Definition.ElementKind != SvgElementKind.Pattern)) return Fail(SvgPaintBuilderError.InvalidState, "A current gradient or pattern definition is required.");
        return ApplyReference(name);
    }

    internal SvgPaintBuilder ApplyKind(SvgElementKind kind, string name)
    {
        if (Error != SvgPaintBuilderError.None) return this;
        if (Definition == null || Definition.ElementKind != kind) return Fail(SvgPaintBuilderError.InvalidState, "The current definition has the wrong resource kind.");
        return ApplyReference(name);
    }

    internal SvgPaintBuilder ApplyReference(string name)
    {
        return SetTargetAttribute(name, SvgDefinitionBuilderSupport.Url(Id));
    }

    internal SvgPaintBuilder SetTargetAttribute(string name, string value)
    {
        if (!ValidTarget(Target)) return Fail(SvgPaintBuilderError.InvalidState, "An attached target element is required.");
        return Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, Target, name, value));
    }

    internal SvgPaintBuilder Accept(SvgMutationResult mutation)
    {
        if (mutation.Success) return this;
        MutationError = mutation.Error;
        return Fail(SvgPaintBuilderError.MutationFailed, mutation.Message);
    }

    internal SvgPaintBuilder Fail(SvgPaintBuilderError error, string? message)
    {
        if (Error == SvgPaintBuilderError.None)
        {
            Error = error;
            Message = (message ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        return this;
    }

    internal bool ValidTarget(SvgNode? target)
    {
        return Builder != null && Builder.Document != null && target != null && target.Kind == SvgNodeKind.Element && object.ReferenceEquals(target.OwnerDocument, Builder.Document);
    }

    internal bool GradientDefinition()
    {
        return Definition != null && (Definition.ElementKind == SvgElementKind.LinearGradient || Definition.ElementKind == SvgElementKind.RadialGradient);
    }

    internal bool ContentDefinition()
    {
        return Definition != null && (Definition.ElementKind == SvgElementKind.Pattern || Definition.ElementKind == SvgElementKind.Marker || Definition.ElementKind == SvgElementKind.ClipPath || Definition.ElementKind == SvgElementKind.Mask);
    }

    internal static bool Finite(double[] values)
    {
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
}
