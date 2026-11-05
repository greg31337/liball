namespace svg;

/// <summary>Records canvas-like drawing operations as editable SVG elements and definitions.</summary>
/// <remarks>Drawing state is snapshotted onto generated nodes; no platform graphics object or background renderer is used.</remarks>
public class SvgCanvas
{
    SvgDocumentApi? documents;
    SvgNodeApi? nodes;
    SvgCanvasState? state;
    System.Collections.Generic.List<SvgCanvasState>? states;
    int layerDepth;
    SvgNode? definitionParent;
    SvgNode? activeDefinition;
    SvgCanvasState? definitionState;
    SvgPathBuilder? definitionPath;
    int definitionLayerDepth;
    int definitionSaveDepth;

    /// <summary>The editable document builder receiving recorded nodes.</summary>
    public SvgBuilder? Builder;
    /// <summary>The original element selected as the recording root.</summary>
    public SvgNode? RecordingRoot;
    /// <summary>The active mutable path.</summary>
    public SvgPathBuilder? CurrentPath;
    /// <summary>The node most recently drawn or generated.</summary>
    public SvgNode? LastNode;
    /// <summary>The most recently generated definition.</summary>
    public SvgNode? LastDefinition;
    /// <summary>The automatic ID of the most recently generated definition, or an empty string.</summary>
    public string? LastDefinitionId;
    /// <summary>The positive maximum save-state stack depth.</summary>
    public int MaximumSaveDepth;
    /// <summary>The positive maximum open layer depth.</summary>
    public int MaximumLayerDepth;
    /// <summary>The positive maximum command count for each active path.</summary>
    public int MaximumPathCommands;
    /// <summary>The first sticky canvas failure, or <c>None</c>.</summary>
    public SvgCanvasError Error;
    /// <summary>The stable human-readable failure message.</summary>
    public string? Message;
    /// <summary>The underlying path failure, or <c>None</c>.</summary>
    public SvgPathBuilderError PathError;
    /// <summary>The underlying geometry failure, or <c>None</c>.</summary>
    public SvgGeometryError GeometryError;
    /// <summary>The underlying document mutation failure, or <c>None</c>.</summary>
    public SvgMutationError MutationError;

    /// <summary>Creates a canvas recording into a new SVG root.</summary>
    public SvgCanvas()
    {
        Initialize(new SvgBuilder());
    }

    /// <summary>Creates a canvas with positive viewport dimensions and a matching view box.</summary>
    /// <param name="width">The positive viewport width.</param><param name="height">The positive viewport height.</param>
    /// <returns>A new canvas.</returns>
    internal static SvgCanvas CreateWithViewport(double width, double height)
    {
        SvgCanvas result = new SvgCanvas();
        if (!Finite(width) || !Finite(height) || width <= 0 || height <= 0)
        {
            result.Fail(SvgCanvasError.InvalidArgument, "Positive finite canvas dimensions are required.");
            return result;
        }
        (result.Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SetAttribute(SvgAttributeNames.Width, Number(width)).SetAttribute(SvgAttributeNames.Height, Number(height)).SetAttribute(SvgAttributeNames.ViewBox, "0 0 " + Number(width) + " " + Number(height));
        if (!result.Builder.Succeeded()) result.Fail(SvgCanvasError.DocumentBuilderFailed, result.Builder.Message);
        return result;
    }

    /// <summary>Creates a canvas recording into an existing builder's current element.</summary>
    /// <param name="builder">The destination document builder.</param>
    /// <returns>A new canvas.</returns>
    internal static SvgCanvas CreateForBuilder(SvgBuilder builder)
    {
        SvgCanvas result = new SvgCanvas();
        result.Initialize(builder);
        return result;
    }

    /// <summary>Tests whether no canvas operation has failed.</summary>
    /// <returns><c>true</c> when the canvas has no sticky failure.</returns>
    internal bool Succeeded()
    {
        return Error == SvgCanvasError.None;
    }

    /// <summary>Returns the number of saved drawing states.</summary>
    /// <returns>The save stack depth.</returns>
    internal int GetSaveDepth()
    {
        return (states ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count;
    }

    /// <summary>Returns the number of currently open SVG layer groups.</summary>
    /// <returns>The layer depth.</returns>
    internal int GetLayerDepth()
    {
        return layerDepth;
    }

    /// <summary>Clears the sticky canvas error without changing output or drawing state.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas ClearError()
    {
        Error = SvgCanvasError.None;
        Message = "";
        PathError = SvgPathBuilderError.None;
        GeometryError = SvgGeometryError.None;
        MutationError = SvgMutationError.None;
        return this;
    }

    /// <summary>Pushes an independent copy of the complete drawing state.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Save()
    {
        if (Error != SvgCanvasError.None) return this;
        if (MaximumSaveDepth < 1) return Fail(SvgCanvasError.InvalidArgument, "MaximumSaveDepth must be positive.");
        if ((states ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count >= MaximumSaveDepth) return Fail(SvgCanvasError.SaveLimitExceeded, "The canvas exceeds MaximumSaveDepth.");
        states.Add((state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Copy());
        return this;
    }

    /// <summary>Restores the most recently saved drawing state.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Restore()
    {
        if (Error != SvgCanvasError.None) return this;
        if ((states ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count == 0 || definitionParent != null && states.Count == definitionSaveDepth) return Fail(SvgCanvasError.InvalidState, "No saved drawing state is available in the current recording scope.");
        state = states[states.Count - 1];
        states.RemoveAt(states.Count - 1);
        return this;
    }

    /// <summary>Post-multiplies the current transformation by a finite affine matrix.</summary>
    /// <param name="matrix">The affine matrix to concatenate.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Transform(SvgMatrix matrix)
    {
        if (Error != SvgCanvasError.None) return this;
        SvgMatrixOperationResult result = new SvgGeometry().Multiply((state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform, matrix);
        if (!result.Success)
        {
            GeometryError = result.Error;
            return Fail(SvgCanvasError.TransformFailed, "The affine canvas transformation failed.");
        }
        state.Transform = (result.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        return this;
    }

    /// <summary>Replaces the current transformation with a finite affine matrix.</summary>
    /// <param name="matrix">The replacement affine matrix.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetTransform(SvgMatrix matrix)
    {
        if (Error != SvgCanvasError.None) return this;
        SvgMatrixOperationResult result = new SvgGeometry().Multiply(Identity(), matrix);
        if (!result.Success)
        {
            GeometryError = result.Error;
            return Fail(SvgCanvasError.TransformFailed, "The replacement canvas transformation is invalid.");
        }
        (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform = (result.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        return this;
    }

    /// <summary>Resets the current transformation to identity.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas ResetTransform()
    {
        if (Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform = Identity();
        return this;
    }

    /// <summary>Returns an independent snapshot of the current transformation.</summary>
    /// <returns>The copied affine matrix.</returns>
    internal SvgMatrix GetTransform()
    {
        return CopyMatrix((state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform);
    }

    /// <summary>Concatenates a translation.</summary>
    /// <param name="x">The finite x translation.</param><param name="y">The finite y translation.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Translate(double x, double y)
    {
        if (!Finite(x) || !Finite(y)) return Fail(SvgCanvasError.InvalidArgument, "Finite translation coordinates are required.");
        return Transform(new SvgMatrix(1, 0, 0, 1, x, y));
    }

    /// <summary>Concatenates a uniform scale.</summary>
    /// <param name="factor">The finite scale factor.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Scale(double factor)
    {
        return ScaleAxes(factor, factor);
    }

    /// <summary>Concatenates independent horizontal and vertical scales.</summary>
    /// <param name="x">The finite horizontal scale.</param><param name="y">The finite vertical scale.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas ScaleAxes(double x, double y)
    {
        if (!Finite(x) || !Finite(y)) return Fail(SvgCanvasError.InvalidArgument, "Finite scale factors are required.");
        return Transform(new SvgMatrix(x, 0, 0, y, 0, 0));
    }

    /// <summary>Concatenates a clockwise rotation in degrees.</summary>
    /// <param name="degrees">The finite rotation angle in degrees.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Rotate(double degrees)
    {
        if (!Finite(degrees)) return Fail(SvgCanvasError.InvalidArgument, "A finite rotation angle is required.");
        SvgTransform transform = new SvgTransform(SvgTransformKind.Rotate, new double[] { degrees });
        SvgTransformMatrixResult result = transform.ToMatrix();
        if (!result.Success) return Fail(SvgCanvasError.TransformFailed, "The rotation could not be represented by a finite matrix.");
        return Transform((result.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
    }

    /// <summary>Starts a new empty current path using the configured command limit.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas BeginPath()
    {
        if (Error != SvgCanvasError.None) return this;
        if (MaximumPathCommands < 1) return Fail(SvgCanvasError.InvalidArgument, "MaximumPathCommands must be positive.");
        CurrentPath = SvgPathBuilder.Create(MaximumPathCommands);
        return this;
    }

    /// <summary>Appends independent validated path data to the current path.</summary>
    /// <param name="path">The path data to append.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas AppendPath(SvgPathData path)
    {
        if (Error != SvgCanvasError.None) return this;
        (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Append(path);
        return AcceptPath();
    }

    /// <summary>Adds an absolute move command to the current path.</summary>
    /// <param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas MoveTo(double x, double y) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).MoveTo(x, y); return AcceptPath(); }

    /// <summary>Adds an absolute line command to the current path.</summary>
    /// <param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas LineTo(double x, double y) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).LineTo(x, y); return AcceptPath(); }

    /// <summary>Adds an absolute cubic Bézier command to the current path.</summary>
    /// <param name="control1X">The first control x coordinate.</param><param name="control1Y">The first control y coordinate.</param><param name="control2X">The second control x coordinate.</param><param name="control2Y">The second control y coordinate.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas CubicTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CubicTo(control1X, control1Y, control2X, control2Y, x, y); return AcceptPath(); }

    /// <summary>Adds an absolute quadratic Bézier command to the current path.</summary>
    /// <param name="controlX">The control x coordinate.</param><param name="controlY">The control y coordinate.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas QuadraticTo(double controlX, double controlY, double x, double y) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).QuadraticTo(controlX, controlY, x, y); return AcceptPath(); }

    /// <summary>Adds an endpoint-parameterized elliptical arc to the current path.</summary>
    /// <param name="radiusX">The nonnegative horizontal radius.</param><param name="radiusY">The nonnegative vertical radius.</param><param name="rotation">The x-axis rotation in degrees.</param><param name="largeArc">Whether to use the large arc.</param><param name="sweep">Whether to use the positive-angle sweep.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas ArcTo(double radiusX, double radiusY, double rotation, bool largeArc, bool sweep, double x, double y) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ArcTo(radiusX, radiusY, rotation, largeArc, sweep, x, y); return AcceptPath(); }

    /// <summary>Adds a circular center arc as a new subpath.</summary>
    /// <param name="centerX">The center x coordinate.</param><param name="centerY">The center y coordinate.</param><param name="radius">The nonnegative radius.</param><param name="startAngle">The start angle in degrees.</param><param name="sweepAngle">The signed sweep angle in degrees.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Arc(double centerX, double centerY, double radius, double startAngle, double sweepAngle) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Arc(centerX, centerY, radius, startAngle, sweepAngle); return AcceptPath(); }

    /// <summary>Adds an axis-aligned rectangle as a new current subpath.</summary>
    /// <param name="x">The left coordinate.</param><param name="y">The top coordinate.</param><param name="width">The nonnegative width.</param><param name="height">The nonnegative height.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Rectangle(double x, double y, double width, double height) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Rectangle(x, y, width, height); return AcceptPath(); }

    /// <summary>Adds a circle as a new current subpath.</summary>
    /// <param name="centerX">The center x coordinate.</param><param name="centerY">The center y coordinate.</param><param name="radius">The nonnegative radius.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Circle(double centerX, double centerY, double radius) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Circle(centerX, centerY, radius); return AcceptPath(); }

    /// <summary>Adds an axis-aligned ellipse as a new current subpath.</summary>
    /// <param name="centerX">The center x coordinate.</param><param name="centerY">The center y coordinate.</param><param name="radiusX">The nonnegative horizontal radius.</param><param name="radiusY">The nonnegative vertical radius.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Ellipse(double centerX, double centerY, double radiusX, double radiusY) { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Ellipse(centerX, centerY, radiusX, radiusY); return AcceptPath(); }

    /// <summary>Closes the active current subpath.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas ClosePath() { if (Error == SvgCanvasError.None) (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Close(); return AcceptPath(); }

    /// <summary>Sets the persistent fill paint used by later fill operations.</summary>
    /// <param name="paint">A complete valid SVG paint value.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetFill(string paint)
    {
        if (Error != SvgCanvasError.None) return this;
        string? validated = Paint(paint);
        if (validated == null) return Fail(SvgCanvasError.InvalidArgument, "A complete valid fill paint is required.");
        (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Fill = validated;
        return this;
    }

    /// <summary>Sets the persistent stroke paint used by later stroke operations.</summary>
    /// <param name="paint">A complete valid SVG paint value.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetStroke(string paint)
    {
        if (Error != SvgCanvasError.None) return this;
        string? validated = Paint(paint);
        if (validated == null) return Fail(SvgCanvasError.InvalidArgument, "A complete valid stroke paint is required.");
        (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Stroke = validated;
        return this;
    }

    /// <summary>Sets the persistent nonnegative stroke width.</summary>
    /// <param name="width">The finite nonnegative stroke width.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetStrokeWidth(double width)
    {
        if (!Finite(width) || width < 0) return Fail(SvgCanvasError.InvalidArgument, "A finite nonnegative stroke width is required.");
        if (Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).StrokeWidth = width;
        return this;
    }

    /// <summary>Sets the persistent object opacity from zero through one.</summary>
    /// <param name="opacity">The finite bounded opacity.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetOpacity(double opacity)
    {
        if (!Finite(opacity) || opacity < 0 || opacity > 1) return Fail(SvgCanvasError.InvalidArgument, "Opacity must be finite and between zero and one.");
        if (Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Opacity = opacity;
        return this;
    }

    /// <summary>Fills the current path with the persistent fill paint.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Fill()
    {
        return DrawPath(true, false);
    }

    /// <summary>Sets and uses a fill paint for the current path.</summary>
    /// <param name="paint">A complete valid SVG paint value.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas FillWithPaint(string paint)
    {
        SetFill(paint);
        return DrawPath(true, false);
    }

    /// <summary>Strokes the current path with the persistent stroke paint and width.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Stroke()
    {
        return DrawPath(false, true);
    }

    /// <summary>Sets and uses a stroke paint and width for the current path.</summary>
    /// <param name="paint">A complete valid SVG paint value.</param><param name="width">The finite nonnegative stroke width.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas StrokeWithPaint(string paint, double width)
    {
        SetStroke(paint).SetStrokeWidth(width);
        return DrawPath(false, true);
    }

    /// <summary>Fills and strokes the current path in one editable path element.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas FillStroke()
    {
        return DrawPath(true, true);
    }

    /// <summary>Intersects the persistent clip with the current path through a generated clip-path definition.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas Clip()
    {
        if (Error != SvgCanvasError.None) return this;
        SvgPathBuildResult built = (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToPathElement();
        if (!built.Success) return PathFailure(built.Error, built.Message);
        SvgDefinitionCreateResult created = SvgDefinitionBuilderSupport.Create(Builder, documents, nodes, SvgElementKind.ClipPath, "svg-clip-");
        if (!created.Success) return MutationFailure(created.MutationError, created.Message);
        SvgMutationResult mutation = SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, created.Definition, SvgAttributeNames.ClipPathUnits, "userSpaceOnUse");
        if (!mutation.Success) return Accept(mutation);
        ApplyTransform(built.Element);
        if (Error != SvgCanvasError.None) return this;
        if ((state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Clip.Length != 0)
        {
            mutation = SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, built.Element, SvgCssPropertyNames.ClipPath, state.Clip);
            if (!mutation.Success) return Accept(mutation);
        }
        mutation = (nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).AppendChild(created.Definition, built.Element);
        if (!mutation.Success) return Accept(mutation);
        LastDefinition = created.Definition;
        LastDefinitionId = created.Id;
        state.Clip = SvgDefinitionBuilderSupport.Url(created.Id);
        LastNode = created.Definition;
        return this;
    }

    /// <summary>Uses an attached paint-server definition as the persistent fill paint.</summary>
    /// <param name="definition">A linear gradient, radial gradient, or pattern with an ID.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas UseFillDefinition(SvgNode definition)
    {
        string? reference = ResourceReference(definition, 1);
        if (reference != null && Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Fill = reference;
        return this;
    }

    /// <summary>Uses an attached paint-server definition as the persistent stroke paint.</summary>
    /// <param name="definition">A linear gradient, radial gradient, or pattern with an ID.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas UseStrokeDefinition(SvgNode definition)
    {
        string? reference = ResourceReference(definition, 1);
        if (reference != null && Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Stroke = reference;
        return this;
    }

    /// <summary>Uses an attached clip-path definition for subsequent drawing.</summary>
    /// <param name="definition">An attached clip-path definition with an ID.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas UseClipDefinition(SvgNode definition)
    {
        string? reference = ResourceReference(definition, 2);
        if (reference != null && Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Clip = reference;
        return this;
    }

    /// <summary>Uses an attached mask definition for subsequent drawing.</summary>
    /// <param name="definition">An attached mask definition with an ID.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas UseMaskDefinition(SvgNode definition)
    {
        string? reference = ResourceReference(definition, 3);
        if (reference != null && Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Mask = reference;
        return this;
    }

    /// <summary>Uses an attached filter definition for subsequent drawing.</summary>
    /// <param name="definition">An attached filter definition with an ID.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas UseFilterDefinition(SvgNode definition)
    {
        string? reference = ResourceReference(definition, 4);
        if (reference != null && Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Filter = reference;
        return this;
    }

    /// <summary>Draws an SVG image with default aspect-ratio preservation.</summary>
    /// <param name="href">The nonempty image reference.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param><param name="width">The nonnegative destination width.</param><param name="height">The nonnegative destination height.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas DrawImage(string href, double x, double y, double width, double height)
    {
        return DrawImageWithAspectRatio(href, x, y, width, height, "xMidYMid meet");
    }

    /// <summary>Draws an SVG image with explicit preserve-aspect-ratio text.</summary>
    /// <param name="href">The nonempty image reference.</param><param name="x">The destination x coordinate.</param><param name="y">The destination y coordinate.</param><param name="width">The nonnegative destination width.</param><param name="height">The nonnegative destination height.</param><param name="aspectRatio">The nonempty preserveAspectRatio text.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas DrawImageWithAspectRatio(string href, double x, double y, double width, double height, string aspectRatio)
    {
        if (Error != SvgCanvasError.None) return this;
        if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(aspectRatio) || !SvgViewBoxParser.ParseAspectRatio(aspectRatio, 4096).Success || !FiniteValues(new double[] { x, y, width, height }) || width < 0 || height < 0) return Fail(SvgCanvasError.InvalidArgument, "An image reference, finite coordinates, nonnegative dimensions, and a valid aspect ratio are required.");
        SvgNode image = (documents ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CreateElement(SvgElementNames.Image, "", SvgElementNames.Image, SvgNamespaces.Svg);
        SetNodeAttribute(image, SvgAttributeNames.Href, href.Trim());
        SetNodeAttribute(image, SvgAttributeNames.X, Number(x));
        SetNodeAttribute(image, SvgAttributeNames.Y, Number(y));
        SetNodeAttribute(image, SvgAttributeNames.Width, Number(width));
        SetNodeAttribute(image, SvgAttributeNames.Height, Number(height));
        SetNodeAttribute(image, SvgAttributeNames.PreserveAspectRatio, aspectRatio.Trim());
        if (Error == SvgCanvasError.None) ApplyState(image, false, false);
        if (Error == SvgCanvasError.None) AppendDrawing(image);
        return this;
    }

    /// <summary>Sets the persistent text font family and positive size.</summary>
    /// <param name="family">The nonempty CSS font-family text.</param><param name="size">The positive finite font size.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetFont(string family, double size)
    {
        if (string.IsNullOrWhiteSpace(family) || !Finite(size) || size <= 0) return Fail(SvgCanvasError.InvalidArgument, "A font family and positive finite size are required.");
        if (Error == SvgCanvasError.None)
        {
            (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FontFamily = family.Trim();
            state.FontSize = size;
        }
        return this;
    }

    /// <summary>Sets persistent normal, italic, or oblique font style text.</summary>
    /// <param name="style">The supported font style.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetFontStyle(string style)
    {
        if (style != "normal" && style != "italic" && style != "oblique") return Fail(SvgCanvasError.InvalidArgument, "A normal, italic, or oblique font style is required.");
        if (Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FontStyle = style;
        return this;
    }

    /// <summary>Sets persistent CSS font-weight text.</summary>
    /// <param name="weight">The nonempty font-weight text.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetFontWeight(string weight)
    {
        if (string.IsNullOrWhiteSpace(weight)) return Fail(SvgCanvasError.InvalidArgument, "A font weight is required.");
        if (Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FontWeight = weight.Trim();
        return this;
    }

    /// <summary>Sets persistent start, middle, or end text anchoring.</summary>
    /// <param name="anchor">The SVG text-anchor value.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetTextAnchor(string anchor)
    {
        if (anchor != "start" && anchor != "middle" && anchor != "end") return Fail(SvgCanvasError.InvalidArgument, "A start, middle, or end text anchor is required.");
        if (Error == SvgCanvasError.None) (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).TextAnchor = anchor;
        return this;
    }

    /// <summary>Draws filled text with the persistent fill and font state.</summary>
    /// <param name="text">The exact text content.</param><param name="x">The finite x position.</param><param name="y">The finite y position.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas FillText(string text, double x, double y)
    {
        return DrawText(text, x, y, true, false);
    }

    /// <summary>Draws stroked text with the persistent stroke and font state.</summary>
    /// <param name="text">The exact text content.</param><param name="x">The finite x position.</param><param name="y">The finite y position.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas StrokeText(string text, double x, double y)
    {
        return DrawText(text, x, y, false, true);
    }

    /// <summary>Begins an anonymous full-opacity SVG layer group.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas BeginLayer()
    {
        return BeginNamedLayer("", 1);
    }

    /// <summary>Begins an anonymous SVG layer group with bounded opacity.</summary>
    /// <param name="opacity">The layer opacity from zero through one.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas BeginOpacityLayer(double opacity)
    {
        return BeginNamedLayer("", opacity);
    }

    /// <summary>Begins an identified SVG layer group with bounded opacity.</summary>
    /// <param name="id">The optional unique ID.</param><param name="opacity">The layer opacity from zero through one.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas BeginNamedLayer(string? id, double opacity)
    {
        if (Error != SvgCanvasError.None) return this;
        if (MaximumLayerDepth < 1) return Fail(SvgCanvasError.InvalidArgument, "MaximumLayerDepth must be positive.");
        if (layerDepth >= MaximumLayerDepth) return Fail(SvgCanvasError.LayerLimitExceeded, "The canvas exceeds MaximumLayerDepth.");
        if (!Finite(opacity) || opacity < 0 || opacity > 1 || id == null || id.Length != 0 && !ValidIdentifier(id)) return Fail(SvgCanvasError.InvalidArgument, "A valid optional layer ID and bounded opacity are required.");
        if (id.Length != 0 && ((Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Document ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Index.GetById(id) != null) return Fail(SvgCanvasError.InvalidArgument, "The layer ID is already present in the document.");
        SvgNode group = (documents ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CreateElement(SvgElementNames.G, "", SvgElementNames.G, SvgNamespaces.Svg);
        if (id.Length != 0) SetNodeAttribute(group, SvgAttributeNames.Id, id);
        if (opacity != 1) SetNodeAttribute(group, SvgCssPropertyNames.Opacity, Number(opacity));
        if (Error != SvgCanvasError.None) return this;
        SvgMutationResult mutation = (nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).AppendChild((Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Current, group);
        if (!mutation.Success) return Accept(mutation);
        Builder.Current = group;
        Builder.LastNode = group;
        LastNode = group;
        layerDepth = layerDepth + 1;
        return this;
    }

    /// <summary>Ends the current SVG layer group.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas EndLayer()
    {
        if (Error != SvgCanvasError.None) return this;
        if (layerDepth == 0 || (Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Current == null || (Builder.Current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parent == null || Builder.Current.ElementKind != SvgElementKind.G) return Fail(SvgCanvasError.InvalidState, "No open canvas layer is available.");
        SvgNode group = Builder.Current;
        Builder.Current = group.Parent;
        Builder.LastNode = group;
        LastNode = group;
        layerDepth = layerDepth - 1;
        return this;
    }

    /// <summary>Begins recording canvas output into a deterministic SVG resource definition.</summary>
    /// <param name="kind">A supported paint, clip, mask, marker, or filter definition kind.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas BeginDefinition(SvgElementKind kind)
    {
        if (Error != SvgCanvasError.None) return this;
        if (definitionParent != null) return Fail(SvgCanvasError.InvalidState, "Nested canvas definition recording is not supported.");
        if (MaximumPathCommands < 1) return Fail(SvgCanvasError.InvalidArgument, "MaximumPathCommands must be positive.");
        string? prefix = DefinitionPrefix(kind);
        if (prefix == null) return Fail(SvgCanvasError.InvalidArgument, "The requested SVG definition kind is not supported by the canvas.");
        SvgDefinitionCreateResult created = SvgDefinitionBuilderSupport.Create(Builder, documents, nodes, kind, prefix);
        if (!created.Success) return MutationFailure(created.MutationError, created.Message);
        definitionParent = (Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Current;
        definitionState = state;
        definitionPath = CurrentPath;
        definitionLayerDepth = layerDepth;
        definitionSaveDepth = (states ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count;
        state = new SvgCanvasState();
        CurrentPath = SvgPathBuilder.Create(MaximumPathCommands);
        Builder.Current = created.Definition;
        Builder.LastNode = created.Definition;
        activeDefinition = created.Definition;
        LastDefinition = created.Definition;
        LastDefinitionId = created.Id;
        LastNode = created.Definition;
        return this;
    }

    /// <summary>Sets an unqualified attribute on the active recorded definition.</summary>
    /// <param name="name">The non-ID XML attribute name.</param><param name="value">The exact attribute value.</param>
    /// <returns>This canvas.</returns>
    internal SvgCanvas SetDefinitionAttribute(string name, string? value)
    {
        if (Error != SvgCanvasError.None) return this;
        if (definitionParent == null || activeDefinition == null) return Fail(SvgCanvasError.InvalidState, "No canvas definition is being recorded.");
        if (!SvgDefinitionBuilderSupport.ValidName(name) || name == SvgAttributeNames.Id || value == null) return Fail(SvgCanvasError.InvalidArgument, "A valid non-ID definition attribute and value are required.");
        return Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, activeDefinition, name, value));
    }

    /// <summary>Ends definition recording and restores the previous insertion point and canvas state.</summary>
    /// <returns>This canvas.</returns>
    internal SvgCanvas EndDefinition()
    {
        if (Error != SvgCanvasError.None) return this;
        if (definitionParent == null) return Fail(SvgCanvasError.InvalidState, "No canvas definition is being recorded.");
        if (layerDepth != definitionLayerDepth || (states ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count != definitionSaveDepth) return Fail(SvgCanvasError.InvalidState, "Definition recording must balance every layer and saved state.");
        (Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Current = definitionParent;
        Builder.LastNode = activeDefinition;
        state = definitionState;
        CurrentPath = definitionPath;
        LastDefinition = activeDefinition;
        LastDefinitionId = AttributeValue(activeDefinition, SvgAttributeNames.Id);
        definitionParent = null;
        activeDefinition = null;
        definitionState = null;
        definitionPath = null;
        LastNode = LastDefinition;
        return this;
    }

    /// <summary>Returns the editable SVG document containing all output recorded so far.</summary>
    /// <returns>The canvas document, including output retained before a failure.</returns>
    internal SvgDocument? Build()
    {
        return Builder == null ? null : Builder.Document;
    }

    internal void Initialize(SvgBuilder builder)
    {
        Builder = builder;
        RecordingRoot = builder == null ? null : builder.Current;
        CurrentPath = new SvgPathBuilder();
        LastNode = null;
        LastDefinition = null;
        LastDefinitionId = "";
        MaximumSaveDepth = 1024;
        MaximumLayerDepth = 1024;
        MaximumPathCommands = 1000000;
        Error = SvgCanvasError.None;
        Message = "";
        PathError = SvgPathBuilderError.None;
        GeometryError = SvgGeometryError.None;
        MutationError = SvgMutationError.None;
        documents = new SvgDocumentApi();
        nodes = new SvgNodeApi();
        state = new SvgCanvasState();
        states = new System.Collections.Generic.List<SvgCanvasState>();
        layerDepth = 0;
        definitionParent = null;
        activeDefinition = null;
        definitionState = null;
        definitionPath = null;
        definitionLayerDepth = 0;
        definitionSaveDepth = 0;
        if (builder == null) Fail(SvgCanvasError.InvalidArgument, "A destination document builder is required.");
        else if (!builder.Succeeded()) Fail(SvgCanvasError.DocumentBuilderFailed, "The destination document builder is in an error state.");
        else if (!ValidRecordingNode(RecordingRoot)) Fail(SvgCanvasError.InvalidState, "An attached SVG recording element is required.");
    }

    internal SvgCanvas DrawPath(bool fill, bool stroke)
    {
        if (Error != SvgCanvasError.None) return this;
        SvgPathBuildResult built = (CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToPathElement();
        if (!built.Success) return PathFailure(built.Error, built.Message);
        ApplyState(built.Element, fill, stroke);
        if (Error == SvgCanvasError.None) AppendDrawing(built.Element);
        return this;
    }

    internal SvgCanvas DrawText(string text, double x, double y, bool fill, bool stroke)
    {
        if (Error != SvgCanvasError.None) return this;
        if (text == null || !Finite(x) || !Finite(y)) return Fail(SvgCanvasError.InvalidArgument, "Text and finite coordinates are required.");
        SvgNode element = (documents ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).CreateElement(SvgElementNames.Text, "", SvgElementNames.Text, SvgNamespaces.Svg);
        SetNodeAttribute(element, SvgAttributeNames.X, Number(x));
        SetNodeAttribute(element, SvgAttributeNames.Y, Number(y));
        SetNodeAttribute(element, SvgCssPropertyNames.FontFamily, (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).FontFamily);
        SetNodeAttribute(element, SvgCssPropertyNames.FontSize, Number(state.FontSize));
        SetNodeAttribute(element, SvgCssPropertyNames.FontStyle, state.FontStyle);
        SetNodeAttribute(element, SvgCssPropertyNames.FontWeight, state.FontWeight);
        SetNodeAttribute(element, SvgCssPropertyNames.TextAnchor, state.TextAnchor);
        ApplyState(element, fill, stroke);
        if (Error != SvgCanvasError.None) return this;
        SvgNode content = documents.CreateText(text);
        SvgMutationResult mutation = (nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).AppendChild(element, content);
        if (!mutation.Success) return Accept(mutation);
        AppendDrawing(element);
        return this;
    }

    internal void ApplyState(SvgNode? element, bool fill, bool stroke)
    {
        if (fill) SetNodeAttribute(element, SvgCssPropertyNames.Fill, (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Fill);
        else if (stroke) SetNodeAttribute(element, SvgCssPropertyNames.Fill, "none");
        if (stroke)
        {
            SetNodeAttribute(element, SvgCssPropertyNames.Stroke, (state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Stroke);
            SetNodeAttribute(element, SvgCssPropertyNames.StrokeWidth, Number(state.StrokeWidth));
        }
        else if (fill) SetNodeAttribute(element, SvgCssPropertyNames.Stroke, "none");
        ApplyTransform(element);
        if ((state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Opacity != 1) SetNodeAttribute(element, SvgCssPropertyNames.Opacity, Number(state.Opacity));
        if (state.Clip.Length != 0) SetNodeAttribute(element, SvgCssPropertyNames.ClipPath, state.Clip);
        if (state.Mask.Length != 0) SetNodeAttribute(element, SvgCssPropertyNames.Mask, state.Mask);
        if (state.Filter.Length != 0) SetNodeAttribute(element, SvgCssPropertyNames.Filter, state.Filter);
    }

    internal void ApplyTransform(SvgNode? element)
    {
        if (!IdentityMatrix((state ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Transform)) SetNodeAttribute(element, SvgAttributeNames.Transform, MatrixText(state.Transform));
    }

    internal void AppendDrawing(SvgNode? element)
    {
        if (Error != SvgCanvasError.None) return;
        if (!ValidRecordingNode((Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Current))
        {
            Fail(SvgCanvasError.InvalidState, "An attached SVG insertion element is required.");
            return;
        }
        SvgMutationResult mutation = (nodes ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).AppendChild(Builder.Current, element);
        if (!mutation.Success)
        {
            Accept(mutation);
            return;
        }
        Builder.LastNode = element;
        LastNode = element;
    }

    internal void SetNodeAttribute(SvgNode? element, string name, string value)
    {
        if (Error != SvgCanvasError.None) return;
        Accept(SvgDefinitionBuilderSupport.SetAttribute(documents, nodes, element, name, value));
    }

    internal SvgCanvas AcceptPath()
    {
        if (Error != SvgCanvasError.None) return this;
        if ((CurrentPath ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Succeeded()) return this;
        return PathFailure(CurrentPath.Error, CurrentPath.Message);
    }

    internal SvgCanvas PathFailure(SvgPathBuilderError error, string? message)
    {
        PathError = error;
        return Fail(SvgCanvasError.PathFailed, (message ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length == 0 ? "The current path operation failed." : message);
    }

    internal SvgCanvas Accept(SvgMutationResult mutation)
    {
        if (mutation.Success) return this;
        return MutationFailure(mutation.Error, mutation.Message);
    }

    internal SvgCanvas MutationFailure(SvgMutationError error, string message)
    {
        MutationError = error;
        return Fail(SvgCanvasError.MutationFailed, message);
    }

    internal SvgCanvas Fail(SvgCanvasError error, string? message)
    {
        if (Error == SvgCanvasError.None)
        {
            Error = error;
            Message = message;
        }
        return this;
    }

    internal string? ResourceReference(SvgNode definition, int expected)
    {
        if (Error != SvgCanvasError.None) return null;
        if (definition == null || definition.Kind != SvgNodeKind.Element || !object.ReferenceEquals(definition.OwnerDocument, (Builder ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Document))
        {
            Fail(SvgCanvasError.InvalidArgument, "An attached definition in the canvas document is required.");
            return null;
        }
        bool valid = expected == 1 && (definition.ElementKind == SvgElementKind.LinearGradient || definition.ElementKind == SvgElementKind.RadialGradient || definition.ElementKind == SvgElementKind.Pattern) || expected == 2 && definition.ElementKind == SvgElementKind.ClipPath || expected == 3 && definition.ElementKind == SvgElementKind.Mask || expected == 4 && definition.ElementKind == SvgElementKind.Filter;
        if (!valid)
        {
            Fail(SvgCanvasError.InvalidArgument, "The definition has the wrong SVG resource kind.");
            return null;
        }
        string id = AttributeValue(definition, SvgAttributeNames.Id);
        if (id.Length == 0)
        {
            Fail(SvgCanvasError.InvalidArgument, "The definition must have a nonempty ID.");
            return null;
        }
        return SvgDefinitionBuilderSupport.Url(id);
    }

    internal static string? DefinitionPrefix(SvgElementKind kind)
    {
        if (kind == SvgElementKind.LinearGradient || kind == SvgElementKind.RadialGradient) return "svg-gradient-";
        if (kind == SvgElementKind.Pattern) return "svg-pattern-";
        if (kind == SvgElementKind.Marker) return "svg-marker-";
        if (kind == SvgElementKind.ClipPath) return "svg-clip-";
        if (kind == SvgElementKind.Mask) return "svg-mask-";
        if (kind == SvgElementKind.Filter) return "svg-filter-";
        return null;
    }

    internal static string? Paint(string value)
    {
        SvgPaintParseResult parsed = new SvgPaintParser().Parse(value, 32, 4096, false);
        return parsed.Success ? (parsed.Paint ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText() : null;
    }

    internal static string AttributeValue(SvgNode? element, string name)
    {
        int index = 0;
        while (index < (element ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute.Value;
            index = index + 1;
        }
        return "";
    }

    internal static bool ValidRecordingNode(SvgNode? node)
    {
        return node != null && node.Kind == SvgNodeKind.Element && node.OwnerDocument != null;
    }

    internal static bool ValidIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsWhiteSpace(value[index])) return false;
            index = index + 1;
        }
        return true;
    }

    internal static bool Finite(double value)
    {
        return SvgFiniteMath.IsFinite(value);
    }

    internal static bool FiniteValues(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!Finite(values[index])) return false;
            index = index + 1;
        }
        return true;
    }

    internal static SvgMatrix Identity()
    {
        return new SvgMatrix(1, 0, 0, 1, 0, 0);
    }

    internal static SvgMatrix CopyMatrix(SvgMatrix matrix)
    {
        return new SvgMatrix(matrix.A, matrix.B, matrix.C, matrix.D, matrix.E, matrix.F);
    }

    internal static bool IdentityMatrix(SvgMatrix matrix)
    {
        return matrix.A == 1 && matrix.B == 0 && matrix.C == 0 && matrix.D == 1 && matrix.E == 0 && matrix.F == 0;
    }

    internal static string MatrixText(SvgMatrix matrix)
    {
        return "matrix(" + Number(matrix.A) + " " + Number(matrix.B) + " " + Number(matrix.C) + " " + Number(matrix.D) + " " + Number(matrix.E) + " " + Number(matrix.F) + ")";
    }

    internal static string Number(double value)
    {
        return SvgNumberFormatter.FormatShortest(value).Text;
    }
}
