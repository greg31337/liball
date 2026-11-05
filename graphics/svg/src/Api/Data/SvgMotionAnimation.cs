namespace svg;

/// <summary>Configures bounded motion path resolution and sampling.</summary>
public class SvgMotionAnimationOptions
{
    /// <summary>The reference policy used for child mpath elements.</summary>
    public SvgReferenceResolveOptions ReferenceOptions;
    /// <summary>The geometry options used for referenced path elements.</summary>
    public SvgShapeResolveOptions ShapeOptions;
    /// <summary>The path measurement tolerance and limits.</summary>
    public SvgPathMeasureOptions MeasureOptions;
    /// <summary>The point-value composition and interpolation options.</summary>
    public SvgAnimationCompositionOptions CompositionOptions;
    /// <summary>The maximum inline path command count.</summary>
    public int MaxPathSegments;
    /// <summary>The maximum numeric token character count.</summary>
    public int MaxTokenCharacters;
    /// <summary>The maximum key timing and spline item count.</summary>
    public int MaxKeyItems;

    /// <summary>Creates secure deterministic motion defaults.</summary>
    public SvgMotionAnimationOptions()
    {
        ReferenceOptions = new SvgReferenceResolveOptions();
        ShapeOptions = new SvgShapeResolveOptions();
        MeasureOptions = new SvgPathMeasureOptions();
        CompositionOptions = new SvgAnimationCompositionOptions();
        MaxPathSegments = 1000000;
        MaxTokenCharacters = 4096;
        MaxKeyItems = 1000000;
    }
}

/// <summary>Contains resolved motion geometry, timing, rotation, and provenance.</summary>
public class SvgMotionAnimationPlan
{
    /// <summary>The target-resolved animateMotion element.</summary>
    public SvgAnimation Animation;
    /// <summary>The selected motion geometry source.</summary>
    public SvgMotionPathSourceKind SourceKind;
    /// <summary>The selected child mpath element, or <c>null</c>.</summary>
    public SvgNode? MPath;
    /// <summary>The mpath reference result, or <c>null</c>.</summary>
    public SvgReferenceResult? Reference;
    /// <summary>The referenced path element, or <c>null</c>.</summary>
    public SvgNode? PathNode;
    /// <summary>The resolved path data, or <c>null</c> for point-value motion.</summary>
    public SvgPathData? PathData;
    /// <summary>The authored referenced pathLength calibration, or <c>null</c>.</summary>
    public SvgPathLength? PathLength;
    /// <summary>The measured path metrics, or <c>null</c> for point-value motion.</summary>
    public SvgPathMetricResult? Metrics;
    /// <summary>The exact measurement options retained for path sampling.</summary>
    public SvgPathMeasureOptions? Measurement;
    /// <summary>The point-value composition plan, or <c>null</c> for path motion.</summary>
    public SvgAnimationValuePlan? ValuePlan;
    /// <summary>The interpolation calculation mode.</summary>
    public SvgAnimationCalcMode CalcMode;
    /// <summary>The normalized key times.</summary>
    public double[] KeyTimes;
    /// <summary>The normalized path-distance key points.</summary>
    public double[] KeyPoints;
    /// <summary>The per-interval cubic Bézier splines.</summary>
    public System.Collections.Generic.List<SvgKeySpline> KeySplines;
    /// <summary>The rotation behavior.</summary>
    public SvgMotionRotateKind RotateKind;
    /// <summary>The fixed rotation in degrees.</summary>
    public double RotateAngle;
    /// <summary>The motion composition behavior.</summary>
    public SvgAnimationAdditive Additive;
    /// <summary>The repeat accumulation behavior.</summary>
    public SvgAnimationAccumulate Accumulate;
    /// <summary>Located diagnostics in deterministic discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The stable motion resolution failure.</summary>
    public SvgMotionAnimationError Error;
    /// <summary>Whether motion resolution succeeded.</summary>
    public bool Success;

    internal SvgMotionAnimationPlan(SvgAnimation animation)
    {
        Animation = animation;
        SourceKind = SvgMotionPathSourceKind.None;
        MPath = null;
        Reference = null;
        PathNode = null;
        PathData = null;
        PathLength = null;
        Metrics = null;
        Measurement = null;
        ValuePlan = null;
        CalcMode = SvgAnimationCalcMode.Paced;
        KeyTimes = new double[0];
        KeyPoints = new double[0];
        KeySplines = new System.Collections.Generic.List<SvgKeySpline>();
        RotateKind = SvgMotionRotateKind.Angle;
        RotateAngle = 0;
        Additive = SvgAnimationAdditive.Replace;
        Accumulate = SvgAnimationAccumulate.None;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Error = SvgMotionAnimationError.None;
        Success = false;
    }
}

/// <summary>Contains one sampled motion frame and composed transform.</summary>
public class SvgMotionAnimationResult
{
    /// <summary>Whether motion sampling succeeded.</summary>
    public bool Success;
    /// <summary>The stable sampling failure.</summary>
    public SvgMotionAnimationError Error;
    /// <summary>The sampled user-space path point after repeat accumulation.</summary>
    public SvgPoint? Point;
    /// <summary>The sampled unit forward tangent.</summary>
    public SvgVector? Tangent;
    /// <summary>The normalized distance fraction selected by key timing.</summary>
    public double PathProgress;
    /// <summary>The calibrated authored path distance.</summary>
    public double CalibratedDistance;
    /// <summary>The corresponding actual user-space path distance.</summary>
    public double ActualDistance;
    /// <summary>The effective rotation in degrees.</summary>
    public double RotationDegrees;
    /// <summary>The supplemental motion transform around the requested origin.</summary>
    public SvgTransformList? MotionTransform;
    /// <summary>The base transform followed by the supplemental motion transform.</summary>
    public SvgTransformList? ComposedTransform;
    /// <summary>The nonnegative repeat iteration applied to accumulation.</summary>
    public int RepeatIteration;

    /// <summary>Creates one motion sample result.</summary>
    /// <param name="success">Whether sampling succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="point">The sampled point.</param>
    /// <param name="tangent">The sampled tangent.</param>
    /// <param name="pathProgress">The normalized path distance.</param>
    /// <param name="calibratedDistance">The authored calibrated distance.</param>
    /// <param name="actualDistance">The actual user-space distance.</param>
    /// <param name="rotationDegrees">The effective rotation.</param>
    /// <param name="motionTransform">The supplemental motion transform.</param>
    /// <param name="composedTransform">The base and motion transform composition.</param>
    /// <param name="repeatIteration">The applied repeat iteration.</param>
    public SvgMotionAnimationResult(bool success, SvgMotionAnimationError error, SvgPoint? point, SvgVector? tangent, double pathProgress, double calibratedDistance, double actualDistance, double rotationDegrees, SvgTransformList? motionTransform, SvgTransformList? composedTransform, int repeatIteration)
    {
        Success = success;
        Error = error;
        Point = point;
        Tangent = tangent;
        PathProgress = pathProgress;
        CalibratedDistance = calibratedDistance;
        ActualDistance = actualDistance;
        RotationDegrees = rotationDegrees;
        MotionTransform = motionTransform;
        ComposedTransform = composedTransform;
        RepeatIteration = repeatIteration;
    }
}
