namespace odf;

/// <summary>
/// Describes the kind of an animation construct.
/// </summary>
public class OdfAnimation
{
    /// <summary>
    /// The animation kind.
    /// </summary>
    public OdfAnimationKind Kind;
    /// <summary>The animation identifier.</summary>
    public string Id = "";
    /// <summary>The XML identifier.</summary>
    public string XmlId = "";
    /// <summary>The presentation timing-node type.</summary>
    public string NodeType = "";
    /// <summary>The presentation preset identifier.</summary>
    public string PresetId = "";
    /// <summary>The presentation preset subtype.</summary>
    public string PresetSubType = "";
    /// <summary>The presentation preset class.</summary>
    public string PresetClass = "";
    /// <summary>The referenced master animation element.</summary>
    public string MasterElement = "";
    /// <summary>The presentation animation group identifier.</summary>
    public string GroupId = "";
    /// <summary>The preserved begin timing expression.</summary>
    public string Begin = "";
    /// <summary>The preserved end timing expression.</summary>
    public string End = "";
    /// <summary>The preserved simple duration.</summary>
    public string Duration = "";
    /// <summary>The preserved minimum active duration.</summary>
    public string Minimum = "";
    /// <summary>The preserved maximum active duration.</summary>
    public string Maximum = "";
    /// <summary>The preserved repeat-count expression.</summary>
    public string RepeatCount = "";
    /// <summary>The preserved repeat duration.</summary>
    public string RepeatDuration = "";
    /// <summary>The preserved restart behavior.</summary>
    public string Restart = "";
    /// <summary>The inherited restart default.</summary>
    public string RestartDefault = "";
    /// <summary>The preserved fill behavior.</summary>
    public string Fill = "";
    /// <summary>The inherited fill default.</summary>
    public string FillDefault = "";
    /// <summary>The container end synchronization behavior.</summary>
    public string EndSync = "";
    /// <summary>The timing speed multiplier.</summary>
    public string Speed = "";
    /// <summary>The acceleration fraction.</summary>
    public string Accelerate = "";
    /// <summary>The deceleration fraction.</summary>
    public string Decelerate = "";
    /// <summary>The preserved auto-reverse token.</summary>
    public string AutoReverse = "";
    /// <summary>The parsed begin timing specifiers.</summary>
    public OdfAnimationTiming[] BeginTimes = [];
    /// <summary>Whether the begin timing list is valid and resolved.</summary>
    public bool IsBeginValid = true;
    /// <summary>The parsed end timing specifiers.</summary>
    public OdfAnimationTiming[] EndTimes = [];
    /// <summary>Whether the end timing list is valid and resolved.</summary>
    public bool IsEndValid = true;
    /// <summary>The parsed simple duration in seconds.</summary>
    public double DurationSeconds;
    /// <summary>Whether the simple duration is indefinite.</summary>
    public bool IsDurationIndefinite;
    /// <summary>Whether the simple duration is media-determined.</summary>
    public bool IsDurationMedia;
    /// <summary>Whether the simple duration is absent or valid.</summary>
    public bool IsDurationValid = true;
    /// <summary>The parsed minimum active duration in seconds.</summary>
    public double MinimumSeconds;
    /// <summary>Whether the minimum duration is media-determined.</summary>
    public bool IsMinimumMedia;
    /// <summary>Whether the minimum duration is absent or valid.</summary>
    public bool IsMinimumValid = true;
    /// <summary>The parsed maximum active duration in seconds.</summary>
    public double MaximumSeconds;
    /// <summary>Whether the maximum duration is indefinite.</summary>
    public bool IsMaximumIndefinite;
    /// <summary>Whether the maximum duration is media-determined.</summary>
    public bool IsMaximumMedia;
    /// <summary>Whether the maximum duration is absent or valid.</summary>
    public bool IsMaximumValid = true;
    /// <summary>The parsed repeat count.</summary>
    public decimal RepeatCountValue;
    /// <summary>Whether the repeat count is indefinite.</summary>
    public bool IsRepeatCountIndefinite;
    /// <summary>Whether the repeat count is absent or valid.</summary>
    public bool IsRepeatCountValid = true;
    /// <summary>The parsed repeat duration in seconds.</summary>
    public double RepeatDurationSeconds;
    /// <summary>Whether the repeat duration is indefinite.</summary>
    public bool IsRepeatDurationIndefinite;
    /// <summary>Whether the repeat duration is absent or valid.</summary>
    public bool IsRepeatDurationValid = true;
    /// <summary>The inherited effective fill behavior.</summary>
    public string EffectiveFill = "auto";
    /// <summary>The inherited fill default used by descendants.</summary>
    public string EffectiveFillDefault = "auto";
    /// <summary>Whether the fill behavior is absent or valid.</summary>
    public bool IsFillValid = true;
    /// <summary>Whether the fill default is absent or valid.</summary>
    public bool IsFillDefaultValid = true;
    /// <summary>The inherited effective restart behavior.</summary>
    public string EffectiveRestart = "always";
    /// <summary>The inherited restart default used by descendants.</summary>
    public string EffectiveRestartDefault = "always";
    /// <summary>Whether the restart behavior is absent or valid.</summary>
    public bool IsRestartValid = true;
    /// <summary>Whether the restart default is absent or valid.</summary>
    public bool IsRestartDefaultValid = true;
    /// <summary>The parsed speed multiplier.</summary>
    public double SpeedValue = 1;
    /// <summary>Whether the speed multiplier is absent or valid.</summary>
    public bool IsSpeedValid = true;
    /// <summary>The parsed acceleration fraction.</summary>
    public decimal AccelerateValue;
    /// <summary>Whether the acceleration fraction is absent or valid.</summary>
    public bool IsAccelerateValid = true;
    /// <summary>The parsed deceleration fraction.</summary>
    public decimal DecelerateValue;
    /// <summary>Whether the deceleration fraction is absent or valid.</summary>
    public bool IsDecelerateValid = true;
    /// <summary>Whether the combined acceleration profile is valid.</summary>
    public bool IsAccelerationValid = true;
    /// <summary>The parsed auto-reverse behavior.</summary>
    public bool AutoReverseEnabled;
    /// <summary>Whether the auto-reverse value is absent or valid.</summary>
    public bool IsAutoReverseValid = true;
    /// <summary>The target element identifier.</summary>
    public string TargetElement = "";
    /// <summary>The target sub-item.</summary>
    public string SubItem = "";
    /// <summary>The animated attribute name.</summary>
    public string AttributeName = "";
    /// <summary>The semicolon-separated animation values.</summary>
    public string Values = "";
    /// <summary>The animation value formula.</summary>
    public string Formula = "";
    /// <summary>The animation start value.</summary>
    public string From = "";
    /// <summary>The animation end value.</summary>
    public string To = "";
    /// <summary>The relative animation value.</summary>
    public string By = "";
    /// <summary>The animation calculation mode.</summary>
    public string CalculationMode = "";
    /// <summary>The normalized key-time list.</summary>
    public string KeyTimes = "";
    /// <summary>The spline control-point list.</summary>
    public string KeySplines = "";
    /// <summary>The additive composition behavior.</summary>
    public string Additive = "";
    /// <summary>The accumulation behavior.</summary>
    public string Accumulate = "";
    /// <summary>The iteration unit.</summary>
    public string IterateType = "";
    /// <summary>The interval between iterated targets.</summary>
    public string IterateInterval = "";
    /// <summary>The parsed iterate interval in seconds.</summary>
    public double IterateIntervalSeconds;
    /// <summary>Whether the iterate interval is absent or valid.</summary>
    public bool IsIterateIntervalValid = true;
    /// <summary>The SVG motion path.</summary>
    public string MotionPath = "";
    /// <summary>The SVG motion origin.</summary>
    public string MotionOrigin = "";
    /// <summary>The SVG transform type.</summary>
    public string TransformType = "";
    /// <summary>The color interpolation space.</summary>
    public string ColorInterpolation = "";
    /// <summary>The color interpolation direction.</summary>
    public string ColorInterpolationDirection = "";
    /// <summary>The transition-filter type.</summary>
    public string TransitionType = "";
    /// <summary>The transition-filter subtype.</summary>
    public string TransitionSubtype = "";
    /// <summary>The transition-filter direction.</summary>
    public string TransitionDirection = "";
    /// <summary>The transition-filter fade color.</summary>
    public string FadeColor = "";
    /// <summary>The transition-filter mode.</summary>
    public string TransitionMode = "";
    /// <summary>The audio resource reference.</summary>
    public string Href = "";
    /// <summary>The audio level.</summary>
    public string AudioLevel = "";
    /// <summary>The command name.</summary>
    public string Command = "";
    /// <summary>The command parameters.</summary>
    public OdfAnimationParameter[] Parameters = [];
    /// <summary>The nested timing and animation children.</summary>
    public OdfAnimation[] Children = [];
    /// <summary>The containing animation, when nested.</summary>
    public OdfAnimation? Parent;
    /// <summary>The drawing object resolved from the target element.</summary>
    public OdfDrawingObject? TargetObject;
    /// <summary>Whether the target is absent or resolves uniquely.</summary>
    public bool IsTargetValid = true;
    /// <summary>Whether more than one drawing object matches the target.</summary>
    public bool IsTargetAmbiguous;
    /// <summary>The animation resolved from the master-element reference.</summary>
    public OdfAnimation? MasterAnimation;
    /// <summary>Whether the master reference is absent or resolves uniquely.</summary>
    public bool IsMasterElementValid = true;
    /// <summary>Whether more than one animation matches the master reference.</summary>
    public bool IsMasterElementAmbiguous;
    /// <summary>Whether the animation identifiers and repeat syntax match the document version.</summary>
    public bool IsVersionValid = true;
    /// <summary>The slide containing the animation.</summary>
    public OdfSlide? Slide;
    /// <summary>The underlying animation element.</summary>
    public OdfNode? Node;
    internal OdfNode[] OriginalParameterNodes = [];
}
