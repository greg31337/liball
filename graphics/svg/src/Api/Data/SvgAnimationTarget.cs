namespace svg;

/// <summary>Configures bounded animation target and property resolution.</summary>
public class SvgAnimationTargetOptions
{
    /// <summary>The property metadata used for CSS and presentation properties.</summary>
    public SvgPropertyCatalog PropertyCatalog;
    /// <summary>The element metadata used for XML attribute applicability.</summary>
    public SvgElementCatalog ElementCatalog;
    /// <summary>The reference options used when an animation has an href.</summary>
    public SvgReferenceResolveOptions ReferenceOptions;
    /// <summary>The maximum accepted attribute-name character count.</summary>
    public int MaxAttributeNameCharacters;

    /// <summary>Creates secure target-resolution defaults.</summary>
    public SvgAnimationTargetOptions()
    {
        PropertyCatalog = SvgPropertyCatalog.CreateDefault();
        ElementCatalog = new SvgCatalogFactory().CreateElementCatalog();
        ReferenceOptions = new SvgReferenceResolveOptions();
        MaxAttributeNameCharacters = 4096;
    }
}

/// <summary>Contains one resolved animation target and value-slot classification.</summary>
public class SvgAnimation
{
    /// <summary>The declarative animation kind.</summary>
    public SvgAnimationKind Kind;
    /// <summary>The source animation element.</summary>
    public SvgNode? Source;
    /// <summary>The captured source element version.</summary>
    public long SourceVersion;
    /// <summary>The resolved element receiving the animated value.</summary>
    public SvgNode? Target;
    /// <summary>The captured target element version.</summary>
    public long TargetVersion;
    /// <summary>The selected target value-slot kind.</summary>
    public SvgAnimationTargetKind TargetKind;
    /// <summary>The selected CSS or XML attribute namespace.</summary>
    public SvgAnimationAttributeType AttributeType;
    /// <summary>The authored qualified attribute name, or an empty string for motion.</summary>
    public string AttributeName;
    /// <summary>The resolved XML attribute prefix.</summary>
    public string AttributePrefix;
    /// <summary>The resolved CSS property or XML attribute local name.</summary>
    public string LocalName;
    /// <summary>The resolved XML attribute namespace URI.</summary>
    public string NamespaceUri;
    /// <summary>The selected property metadata, or <c>null</c> for ordinary XML attributes and motion.</summary>
    public SvgPropertySupportEntry? Property;
    /// <summary>The interpolation category required by the selected value slot.</summary>
    public SvgPropertyAnimationType AnimationType;
    /// <summary>The selected href result, or <c>null</c> when the parent is the target.</summary>
    public SvgReferenceResult? TargetReference;
    /// <summary>The authored attributeName source, or <c>null</c>.</summary>
    public SvgAttribute? AttributeNameSource;
    /// <summary>The authored attributeType source, or <c>null</c>.</summary>
    public SvgAttribute? AttributeTypeSource;
    /// <summary>The stable target-resolution failure.</summary>
    public SvgAnimationTargetError Error;
    /// <summary>The parsed begin timing specifiers.</summary>
    public System.Collections.Generic.List<SvgTiming> Begin;
    /// <summary>The parsed end timing specifiers.</summary>
    public System.Collections.Generic.List<SvgTiming> End;
    /// <summary>The simple duration in seconds, or positive infinity.</summary>
    public double DurationSeconds;
    /// <summary>The minimum active duration in seconds.</summary>
    public double MinimumSeconds;
    /// <summary>The maximum active duration in seconds, or positive infinity.</summary>
    public double MaximumSeconds;
    /// <summary>The repeat count, or positive infinity.</summary>
    public double RepeatCount;
    /// <summary>The repeat-duration bound in seconds, or positive infinity.</summary>
    public double RepeatDurationSeconds;
    /// <summary>The restart behavior.</summary>
    public SvgAnimationRestart Restart;
    /// <summary>The post-active fill behavior.</summary>
    public SvgAnimationFill Fill;
    /// <summary>The resolved typed value family.</summary>
    public SvgAnimationValueKind ValueKind;
    /// <summary>The parsed values-list items.</summary>
    public System.Collections.Generic.List<SvgAnimationValue> Values;
    /// <summary>The parsed from value, or <c>null</c>.</summary>
    public SvgAnimationValue? FromValue;
    /// <summary>The parsed to value, or <c>null</c>.</summary>
    public SvgAnimationValue? ToValue;
    /// <summary>The parsed by value, or <c>null</c>.</summary>
    public SvgAnimationValue? ByValue;
    /// <summary>The interpolation calculation mode.</summary>
    public SvgAnimationCalcMode CalcMode;
    /// <summary>The normalized key times.</summary>
    public double[] KeyTimes;
    /// <summary>The parsed cubic Bézier key splines.</summary>
    public System.Collections.Generic.List<SvgKeySpline> KeySplines;
    /// <summary>The parsed motion key points.</summary>
    public double[] KeyPoints;
    /// <summary>The underlying-value composition mode.</summary>
    public SvgAnimationAdditive Additive;
    /// <summary>The repeat accumulation mode.</summary>
    public SvgAnimationAccumulate Accumulate;
    /// <summary>Located diagnostics in deterministic discovery order.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>Whether target and value-slot resolution succeeded.</summary>
    public bool Success;

    internal SvgAnimation(SvgNode? source)
    {
        Kind = SvgAnimationKind.Unknown;
        Source = source;
        SourceVersion = source == null ? 0 : source.Version;
        Target = null;
        TargetVersion = 0;
        TargetKind = SvgAnimationTargetKind.Unknown;
        AttributeType = SvgAnimationAttributeType.Unknown;
        AttributeName = "";
        AttributePrefix = "";
        LocalName = "";
        NamespaceUri = "";
        Property = null;
        AnimationType = SvgPropertyAnimationType.NotAnimatable;
        TargetReference = null;
        AttributeNameSource = null;
        AttributeTypeSource = null;
        Error = SvgAnimationTargetError.None;
        Begin = new System.Collections.Generic.List<SvgTiming>();
        End = new System.Collections.Generic.List<SvgTiming>();
        DurationSeconds = double.PositiveInfinity;
        MinimumSeconds = 0;
        MaximumSeconds = double.PositiveInfinity;
        RepeatCount = 1;
        RepeatDurationSeconds = double.PositiveInfinity;
        Restart = SvgAnimationRestart.Always;
        Fill = SvgAnimationFill.Remove;
        ValueKind = SvgAnimationValueKind.String;
        Values = new System.Collections.Generic.List<SvgAnimationValue>();
        FromValue = null;
        ToValue = null;
        ByValue = null;
        CalcMode = SvgAnimationCalcMode.Linear;
        KeyTimes = new double[0];
        KeySplines = new System.Collections.Generic.List<SvgKeySpline>();
        KeyPoints = new double[0];
        Additive = SvgAnimationAdditive.Replace;
        Accumulate = SvgAnimationAccumulate.None;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Success = false;
    }

    /// <summary>Checks whether the source and target retain their captured versions.</summary>
    /// <returns><c>true</c> when the resolved source snapshot remains current.</returns>
    internal bool IsCurrent()
    {
        return Source != null && Source.Version == SourceVersion && Target != null && Target.Version == TargetVersion;
    }
}
