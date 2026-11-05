namespace svg;

/// <summary>Describes one recoverable SVG path parsing issue.</summary>
public class SvgPathParseIssue
{
    /// <summary>The stable failure kind.</summary>
    public SvgPathParseError Error;
    /// <summary>The exact source offset of the failure.</summary>
    public int ErrorOffset;
    /// <summary>The source offset of the associated command, or <c>-1</c>.</summary>
    public int CommandOffset;

    /// <summary>Creates a path parsing issue.</summary>
    /// <param name="error">The failure kind.</param>
    /// <param name="errorOffset">The exact failure offset.</param>
    /// <param name="commandOffset">The associated command offset.</param>
    public SvgPathParseIssue(SvgPathParseError error, int errorOffset, int commandOffset)
    {
        if (error == SvgPathParseError.None || errorOffset < 0 || commandOffset < -1) throw new System.ArgumentOutOfRangeException("error");
        Error = error;
        ErrorOffset = errorOffset;
        CommandOffset = commandOffset;
    }
}

/// <summary>Represents one authored SVG path command with resolved tracking coordinates.</summary>
public class SvgPathCommand
{
    /// <summary>The command kind.</summary>
    public SvgPathCommandKind Kind;
    /// <summary>Whether the authored command uses relative coordinates.</summary>
    public bool Relative;
    /// <summary>The authored numeric payload in SVG parameter order.</summary>
    public double[] Values;
    /// <summary>The command letter or implicit parameter-group source offset.</summary>
    public int SourceOffset;
    /// <summary>The exclusive source offset after the command payload.</summary>
    public int EndOffset;
    /// <summary>Whether a command letter was explicitly authored for this command.</summary>
    public bool Explicit;
    /// <summary>The resolved current x coordinate before this command.</summary>
    public double StartX;
    /// <summary>The resolved current y coordinate before this command.</summary>
    public double StartY;
    /// <summary>The resolved current x coordinate after this command.</summary>
    public double EndX;
    /// <summary>The resolved current y coordinate after this command.</summary>
    public double EndY;
    /// <summary>The active subpath starting x coordinate after this command.</summary>
    public double SubpathStartX;
    /// <summary>The active subpath starting y coordinate after this command.</summary>
    public double SubpathStartY;
    /// <summary>The zero-based subpath index.</summary>
    public int SubpathIndex;
    /// <summary>Whether the mutable command state was valid when created.</summary>
    public bool IsValid;

    /// <summary>Creates a source-mapped path command and copies its numeric payload.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="relative">Whether coordinates are relative.</param>
    /// <param name="values">The authored numeric payload.</param>
    /// <param name="sourceOffset">The command source offset.</param>
    /// <param name="endOffset">The exclusive command ending offset.</param>
    /// <param name="explicitCommand">Whether a command letter was explicit.</param>
    /// <param name="startX">The resolved starting x coordinate.</param>
    /// <param name="startY">The resolved starting y coordinate.</param>
    /// <param name="endX">The resolved ending x coordinate.</param>
    /// <param name="endY">The resolved ending y coordinate.</param>
    /// <param name="subpathStartX">The active subpath starting x coordinate.</param>
    /// <param name="subpathStartY">The active subpath starting y coordinate.</param>
    /// <param name="subpathIndex">The subpath index.</param>
    public SvgPathCommand(SvgPathCommandKind kind, bool relative, double[] values, int sourceOffset, int endOffset, bool explicitCommand, double startX, double startY, double endX, double endY, double subpathStartX, double subpathStartY, int subpathIndex)
    {
        if (!SvgPathCommandRules.IsKnown(kind) || values == null || values.Length != SvgPathCommandRules.ParameterCount(kind) || sourceOffset < 0 || endOffset <= sourceOffset || subpathIndex < 0) throw new System.ArgumentOutOfRangeException("kind");
        int index = 0;
        while (index < values.Length)
        {
            if (!SvgFiniteMath.IsFinite(values[index])) throw new System.ArgumentOutOfRangeException("values");
            index = index + 1;
        }
        if (!SvgFiniteMath.IsFinite(startX) || !SvgFiniteMath.IsFinite(startY) || !SvgFiniteMath.IsFinite(endX) || !SvgFiniteMath.IsFinite(endY) || !SvgFiniteMath.IsFinite(subpathStartX) || !SvgFiniteMath.IsFinite(subpathStartY)) throw new System.ArgumentOutOfRangeException("startX");
        Kind = kind;
        Relative = relative;
        Values = (double[])values.Clone();
        SourceOffset = sourceOffset;
        EndOffset = endOffset;
        Explicit = explicitCommand;
        StartX = startX;
        StartY = startY;
        EndX = endX;
        EndY = endY;
        SubpathStartX = subpathStartX;
        SubpathStartY = subpathStartY;
        SubpathIndex = subpathIndex;
        IsValid = true;
    }
}

/// <summary>Contains source-preserving SVG path commands and parse issues.</summary>
public class SvgPathData
{
    /// <summary>The ordered parsed commands.</summary>
    public System.Collections.Generic.List<SvgPathCommand> Commands;
    /// <summary>The complete authored path data.</summary>
    public string Source;
    /// <summary>Whether the complete source parsed without issues.</summary>
    public bool IsValid;
    /// <summary>The ordered recoverable parse issues.</summary>
    public System.Collections.Generic.List<SvgPathParseIssue> Issues;

    /// <summary>Creates path data and copies its command and issue collections.</summary>
    /// <param name="commands">The parsed commands.</param>
    /// <param name="source">The authored path data.</param>
    /// <param name="issues">The parse issues.</param>
    public SvgPathData(System.Collections.Generic.List<SvgPathCommand> commands, string source, System.Collections.Generic.List<SvgPathParseIssue> issues)
    {
        if (commands == null || source == null || issues == null) throw new System.ArgumentNullException("commands");
        Commands = new System.Collections.Generic.List<SvgPathCommand>(commands);
        Source = source;
        Issues = new System.Collections.Generic.List<SvgPathParseIssue>(issues);
        IsValid = issues.Count == 0;
    }

    /// <summary>Returns the number of parsed subpaths.</summary>
    /// <returns>The parsed subpath count.</returns>
    internal int GetSubpathCount()
    {
        int maximum = -1;
        int index = 0;
        while (index < Commands.Count)
        {
            if (Commands[index].SubpathIndex > maximum) maximum = Commands[index].SubpathIndex;
            index = index + 1;
        }
        return maximum + 1;
    }

    /// <summary>Normalizes this path to absolute explicit commands.</summary>
    /// <returns>The normalized path or a stable failure.</returns>
    internal SvgPathNormalizeResult Normalize()
    {
        return new SvgPathNormalizer().NormalizeWithPath(this);
    }

    /// <summary>Normalizes this path with explicit conversion and command-limit options.</summary>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized path or a stable failure.</returns>
    internal SvgPathNormalizeResult NormalizeWithOptions(SvgPathNormalizeOptions options)
    {
        return new SvgPathNormalizer().NormalizeWithPathAndOptions(this, options);
    }

    /// <summary>Normalizes this path and converts quadratic commands to cubic commands.</summary>
    /// <returns>The normalized cubic path or a stable failure.</returns>
    internal SvgPathNormalizeResult NormalizeToCubics()
    {
        return new SvgPathNormalizer().NormalizeToCubicsWithPath(this);
    }

    /// <summary>Applies an affine transform while preserving eligible elliptical arcs.</summary>
    /// <param name="matrix">The finite affine transform.</param>
    /// <returns>The transformed path or a stable failure.</returns>
    internal SvgPathTransformResult Transform(SvgMatrix matrix)
    {
        return new SvgPathTransformer().TransformWithPathAndMatrix(this, matrix);
    }

    /// <summary>Applies an affine transform with explicit arc and command-limit options.</summary>
    /// <param name="matrix">The finite affine transform.</param>
    /// <param name="options">The transform options.</param>
    /// <returns>The transformed path or a stable failure.</returns>
    internal SvgPathTransformResult TransformWithOptions(SvgMatrix matrix, SvgPathTransformOptions options)
    {
        return new SvgPathTransformer().TransformWithPathAndMatrixAndOptions(this, matrix, options);
    }

    /// <summary>Measures this path with deterministic adaptive defaults.</summary>
    /// <returns>The actual and calibrated path lengths or a stable failure.</returns>
    internal SvgPathMetricResult Measure()
    {
        return new SvgPathMetrics().MeasureWithPathAndOptions(this, new SvgPathMeasureOptions());
    }

    /// <summary>Measures this path with explicit tolerance, limits, and pathLength calibration.</summary>
    /// <param name="options">The measurement options.</param>
    /// <returns>The actual and calibrated path lengths or a stable failure.</returns>
    internal SvgPathMetricResult MeasureWithOptions(SvgPathMeasureOptions options)
    {
        return new SvgPathMetrics().MeasureWithPathAndOptions(this, options);
    }

    /// <summary>Finds the point and unit frame at a clamped actual distance.</summary>
    /// <param name="distance">The nonnegative actual distance.</param>
    /// <returns>The point, tangent, and normal or a stable failure.</returns>
    internal SvgPathLocationResult GetLocation(double distance)
    {
        return new SvgPathMetrics().GetLocationWithPathAndDistanceAndOptions(this, distance, new SvgPathMeasureOptions());
    }

    /// <summary>Finds the point and unit frame at a clamped actual or calibrated distance.</summary>
    /// <param name="distance">The nonnegative distance.</param>
    /// <param name="options">The measurement and calibration options.</param>
    /// <returns>The point, tangent, and normal or a stable failure.</returns>
    internal SvgPathLocationResult GetLocationWithOptions(double distance, SvgPathMeasureOptions options)
    {
        return new SvgPathMetrics().GetLocationWithPathAndDistanceAndOptions(this, distance, options);
    }

    /// <summary>Extracts type-preserving geometry between two clamped actual distances.</summary>
    /// <param name="startDistance">The nonnegative start distance.</param>
    /// <param name="endDistance">The nonnegative end distance.</param>
    /// <returns>The extracted normalized path or a stable failure.</returns>
    internal SvgPathExtractResult Extract(double startDistance, double endDistance)
    {
        return new SvgPathMetrics().ExtractWithPathAndStartDistanceAndEndDistanceAndOptions(this, startDistance, endDistance, new SvgPathMeasureOptions());
    }

    /// <summary>Extracts type-preserving geometry between two clamped actual or calibrated distances.</summary>
    /// <param name="startDistance">The nonnegative start distance.</param>
    /// <param name="endDistance">The nonnegative end distance.</param>
    /// <param name="options">The measurement and calibration options.</param>
    /// <returns>The extracted normalized path or a stable failure.</returns>
    internal SvgPathExtractResult ExtractWithOptions(double startDistance, double endDistance, SvgPathMeasureOptions options)
    {
        return new SvgPathMetrics().ExtractWithPathAndStartDistanceAndEndDistanceAndOptions(this, startDistance, endDistance, options);
    }

    /// <summary>Trims this path to type-preserving geometry between two clamped actual distances.</summary>
    /// <param name="startDistance">The nonnegative start distance.</param>
    /// <param name="endDistance">The nonnegative end distance.</param>
    /// <returns>The trimmed normalized path or a stable failure.</returns>
    internal SvgPathExtractResult Trim(double startDistance, double endDistance)
    {
        return new SvgPathMetrics().TrimWithPathAndStartDistanceAndEndDistanceAndOptions(this, startDistance, endDistance, new SvgPathMeasureOptions());
    }

    /// <summary>Trims this path to type-preserving geometry between two clamped actual or calibrated distances.</summary>
    /// <param name="startDistance">The nonnegative start distance.</param>
    /// <param name="endDistance">The nonnegative end distance.</param>
    /// <param name="options">The measurement and calibration options.</param>
    /// <returns>The trimmed normalized path or a stable failure.</returns>
    internal SvgPathExtractResult TrimWithOptions(double startDistance, double endDistance, SvgPathMeasureOptions options)
    {
        return new SvgPathMetrics().TrimWithPathAndStartDistanceAndEndDistanceAndOptions(this, startDistance, endDistance, options);
    }

    /// <summary>Classifies a point against this path's fill using deterministic defaults.</summary>
    /// <param name="point">The point in path user coordinates.</param>
    /// <param name="fillRule">The nonzero or even-odd fill rule.</param>
    /// <returns>The boundary, inside, or outside classification.</returns>
    internal SvgFillContainmentResult ClassifyFill(SvgPoint point, SvgFillRule fillRule)
    {
        return new SvgFillContainment().ClassifyWithPathAndPointAndFillRuleAndOptions(this, point, fillRule, new SvgFillContainmentOptions());
    }

    /// <summary>Classifies a point against this path's fill with explicit tolerance, limits, and transform.</summary>
    /// <param name="point">The point in path or transformed output coordinates.</param>
    /// <param name="fillRule">The nonzero or even-odd fill rule.</param>
    /// <param name="options">The bounded containment options.</param>
    /// <returns>The boundary, inside, or outside classification.</returns>
    internal SvgFillContainmentResult ClassifyFillWithOptions(SvgPoint point, SvgFillRule fillRule, SvgFillContainmentOptions options)
    {
        return new SvgFillContainment().ClassifyWithPathAndPointAndFillRuleAndOptions(this, point, fillRule, options);
    }

    /// <summary>Constructs this path's closed stroke outline using explicit stroke geometry.</summary>
    /// <param name="style">The stroke width, caps, joins, dashes, tolerance, and limits.</param>
    /// <returns>The nonzero-fill outline and dash placements or a stable failure.</returns>
    internal SvgStrokeOutlineResult StrokeOutline(SvgStrokeStyle style)
    {
        return new SvgStrokeOutliner().Create(this, style);
    }

    /// <summary>Tests whether transformed stroke geometry contains a query point.</summary>
    /// <param name="point">The point in transformed output coordinates.</param>
    /// <param name="options">The stroke style, transform, and vector effect.</param>
    /// <returns>The bounded stroke-containment result.</returns>
    internal SvgStrokeContainmentResult ContainsStroke(SvgPoint point, SvgStrokeGeometryOptions options)
    {
        return new SvgStrokeGeometry().Contains(this, point, options);
    }

    /// <summary>Computes transformed stroke bounds.</summary>
    /// <param name="options">The stroke style, transform, and vector effect.</param>
    /// <returns>The finite transformed bounds or a stable failure.</returns>
    internal SvgStrokeBoundsResult GetStrokeBounds(SvgStrokeGeometryOptions options)
    {
        return new SvgStrokeGeometry().GetBounds(this, options);
    }

    /// <summary>Extracts start, mid, and end marker vertices with path-derived tangents.</summary>
    /// <returns>The ordered marker vertices or a stable failure.</returns>
    internal SvgMarkerVertexResult GetMarkerVertices()
    {
        return new SvgMarkerGeometry().Extract(this, 2000000);
    }

    /// <summary>Resolves marker placement transforms, scaling, orientation, and aggregate bounds.</summary>
    /// <param name="options">The start, mid, and end marker definitions and limits.</param>
    /// <returns>The marker geometry result or a stable failure.</returns>
    internal SvgMarkerGeometryResult GetMarkerGeometry(SvgMarkerGeometryOptions options)
    {
        return new SvgMarkerGeometry().Resolve(this, options);
    }

    /// <summary>Finds this path's non-adjacent self-intersections with deterministic defaults.</summary>
    /// <returns>The ordered self-intersections or a stable failure.</returns>
    internal SvgPathSelfIntersectionResult FindSelfIntersections()
    {
        return new SvgPlanarGeometry().FindSelfIntersectionsWithPath(this);
    }

    /// <summary>Finds this path's non-adjacent self-intersections with explicit tolerance and limits.</summary>
    /// <param name="options">The bounded planar options.</param>
    /// <returns>The ordered self-intersections or a stable failure.</returns>
    internal SvgPathSelfIntersectionResult FindSelfIntersectionsWithOptions(SvgPlanarOptions options)
    {
        return new SvgPlanarGeometry().FindSelfIntersectionsWithPathAndOptions(this, options);
    }

    /// <summary>Flattens and splits this path into directed planar edges with deterministic defaults.</summary>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    internal SvgPlanarSplitResult SplitPlanarEdges()
    {
        return new SvgPlanarGeometry().SplitPathWithPath(this);
    }

    /// <summary>Flattens and splits this path into directed planar edges with explicit tolerance and limits.</summary>
    /// <param name="options">The bounded planar options.</param>
    /// <returns>The source-ordered atomic edges or a stable failure.</returns>
    internal SvgPlanarSplitResult SplitPlanarEdgesWithOptions(SvgPlanarOptions options)
    {
        return new SvgPlanarGeometry().SplitPathWithPathAndOptions(this, options);
    }

    /// <summary>Applies a nonzero-filled Boolean operation against another path.</summary>
    /// <param name="other">The second filled path.</param><param name="operation">The Boolean operation.</param>
    /// <returns>The deterministic flattened result or a stable failure.</returns>
    internal SvgPathBooleanResult Boolean(SvgPathData other, SvgPathBooleanOperation operation)
    {
        return new SvgPathBoolean().ApplyWithFirstAndSecondAndOperation(this, other, operation);
    }

    /// <summary>Applies a filled Boolean operation with explicit rules and limits.</summary>
    /// <param name="other">The second filled path.</param><param name="operation">The Boolean operation.</param><param name="fillRule">This path's fill rule.</param><param name="otherFillRule">The other path's fill rule.</param><param name="options">The bounded Boolean options.</param>
    /// <returns>The deterministic flattened result or a stable failure.</returns>
    internal SvgPathBooleanResult BooleanWithOptions(SvgPathData other, SvgPathBooleanOperation operation, SvgFillRule fillRule, SvgFillRule otherFillRule, SvgPathBooleanOptions options)
    {
        return new SvgPathBoolean().ApplyWithFirstAndSecondAndOperationAndFirstRuleAndSecondRuleAndOptions(this, other, operation, fillRule, otherFillRule, options);
    }

    /// <summary>Removes and compacts exactly redundant path commands with deterministic defaults.</summary>
    /// <returns>The absolute explicit cleaned path or a stable failure.</returns>
    internal SvgPathCleanupResult Cleanup()
    {
        return new SvgPathCleanup().CleanWithPath(this);
    }

    /// <summary>Removes and compacts exactly redundant commands within an explicit limit.</summary>
    /// <param name="options">The bounded cleanup options.</param>
    /// <returns>The absolute explicit cleaned path or a stable failure.</returns>
    internal SvgPathCleanupResult CleanupWithOptions(SvgPathCleanupOptions options)
    {
        return new SvgPathCleanup().CleanWithPathAndOptions(this, options);
    }

    /// <summary>Simplifies and rounds this path with deterministic measured-error defaults.</summary>
    /// <returns>The bounded lossy result or a stable failure.</returns>
    internal SvgPathSimplifyResult Simplify()
    {
        return new SvgPathSimplifier().SimplifyWithPath(this);
    }

    /// <summary>Simplifies and rounds this path within explicit error and security limits.</summary>
    /// <param name="options">The tolerance, precision, and limits.</param>
    /// <returns>The bounded lossy result or a stable failure.</returns>
    internal SvgPathSimplifyResult SimplifyWithOptions(SvgPathSimplifyOptions options)
    {
        return new SvgPathSimplifier().SimplifyWithPathAndOptions(this, options);
    }

    /// <summary>Computes exact local fill bounds.</summary>
    /// <returns>The explicitly empty or finite fill bounds.</returns>
    internal SvgDrawingBoundsResult GetFillBounds()
    {
        return new SvgDrawingBounds().Fill(this);
    }

    /// <summary>Computes combined fill, stroke, marker, world, and effect-region bounds.</summary>
    /// <param name="options">The component, transform, and effect options.</param>
    /// <returns>The component and final world-space bounds.</returns>
    internal SvgDrawingBoundsResult GetDrawingBounds(SvgDrawingBoundsOptions options)
    {
        return new SvgDrawingBounds().Path(this, options);
    }

    /// <summary>Tests fill, stroke, bounds, clip, and mask geometry at a query point.</summary>
    /// <param name="point">The finite local or transformed query point.</param>
    /// <param name="options">The pointer-event geometry and effect options.</param>
    /// <returns>The component hit decision or stable failure.</returns>
    internal SvgGeometryHitTestResult HitTest(SvgPoint point, SvgGeometryHitTestOptions options)
    {
        return new SvgHitTesting().Path(this, point, options);
    }

    /// <summary>Formats this path with canonical commands, spacing, and exact values.</summary>
    /// <returns>Canonical path text, or an empty string for invalid mutable state.</returns>
    internal string ToText()
    {
        return new SvgPathFormatter().FormatCanonicalWithPath(this).Text;
    }

    /// <summary>Formats this path with compact commands, separators, and exact values.</summary>
    /// <returns>Minified path text, or an empty string for invalid mutable state.</returns>
    internal string ToMinifiedText()
    {
        return new SvgPathFormatter().FormatMinifiedWithPath(this).Text;
    }
}

/// <summary>Contains the result of bounded SVG path parsing and recovery.</summary>
public class SvgPathParseResult
{
    /// <summary>Whether the complete source parsed without issues.</summary>
    public bool Success;
    /// <summary>The first failure kind, or no failure.</summary>
    public SvgPathParseError Error;
    /// <summary>The parsed path and valid recovered commands.</summary>
    public SvgPathData? PathData;
    /// <summary>The first failure offset, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The next unread offset, normally the source length.</summary>
    public int NextOffset;

    /// <summary>Creates a bounded path parse result.</summary>
    /// <param name="success">Whether parsing succeeded without issues.</param>
    /// <param name="error">The first failure kind.</param>
    /// <param name="pathData">The parsed path data.</param>
    /// <param name="errorOffset">The first failure offset.</param>
    /// <param name="nextOffset">The next unread offset.</param>
    public SvgPathParseResult(bool success, SvgPathParseError error, SvgPathData? pathData, int errorOffset, int nextOffset)
    {
        Success = success;
        Error = error;
        PathData = pathData;
        ErrorOffset = errorOffset;
        NextOffset = nextOffset;
    }
}
