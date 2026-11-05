namespace svg;

/// <summary>Configures bounded animation value selection and composition.</summary>
public class SvgAnimationCompositionOptions
{
    /// <summary>The typed interpolation and parsing options.</summary>
    public SvgAnimationInterpolationOptions Interpolation;
    /// <summary>The maximum semicolon-separated values count.</summary>
    public int MaxValues;
    /// <summary>The maximum repeat iteration accepted for accumulation.</summary>
    public int MaxRepeatAccumulations;

    /// <summary>Creates deterministic bounded composition defaults.</summary>
    public SvgAnimationCompositionOptions()
    {
        Interpolation = new SvgAnimationInterpolationOptions();
        MaxValues = 1000000;
        MaxRepeatAccumulations = 1000000;
    }
}

/// <summary>Contains one parsed and validated animation value-selection plan.</summary>
public class SvgAnimationValuePlan
{
    /// <summary>The target-resolved animation.</summary>
    public SvgAnimation Animation;
    /// <summary>The selected value family.</summary>
    public SvgAnimationValueKind ValueKind;
    /// <summary>The selected authored value combination.</summary>
    public SvgAnimationValueSourceKind SourceKind;
    /// <summary>The effective keyframe values before underlying-value expansion.</summary>
    public System.Collections.Generic.List<SvgAnimationValue> Values;
    /// <summary>The normalized key times.</summary>
    public double[] KeyTimes;
    /// <summary>The per-segment cubic Bézier key splines.</summary>
    public System.Collections.Generic.List<SvgKeySpline> KeySplines;
    /// <summary>The motion key points.</summary>
    public double[] KeyPoints;
    /// <summary>The interpolation mode.</summary>
    public SvgAnimationCalcMode CalcMode;
    /// <summary>The underlying-value composition mode.</summary>
    public SvgAnimationAdditive Additive;
    /// <summary>The repeat accumulation mode.</summary>
    public SvgAnimationAccumulate Accumulate;
    /// <summary>Located validation diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The stable value-plan failure.</summary>
    public SvgAnimationCompositionError Error;
    /// <summary>Whether selection and validation succeeded.</summary>
    public bool Success;

    internal SvgAnimationValuePlan(SvgAnimation animation, SvgAnimationValueKind kind)
    {
        Animation = animation;
        ValueKind = kind;
        SourceKind = SvgAnimationValueSourceKind.None;
        Values = new System.Collections.Generic.List<SvgAnimationValue>();
        KeyTimes = new double[0];
        KeySplines = new System.Collections.Generic.List<SvgKeySpline>();
        KeyPoints = new double[0];
        CalcMode = SvgAnimationCalcMode.Unknown;
        Additive = SvgAnimationAdditive.Unknown;
        Accumulate = SvgAnimationAccumulate.Unknown;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        Error = SvgAnimationCompositionError.None;
        Success = false;
    }
}

/// <summary>Contains one sampled and underlying-composed typed animation value.</summary>
public class SvgAnimationCompositionResult
{
    /// <summary>Whether sampling and composition succeeded.</summary>
    public bool Success;
    /// <summary>The stable composition failure.</summary>
    public SvgAnimationCompositionError Error;
    /// <summary>The interpolated value before underlying composition.</summary>
    public SvgAnimationValue? EffectValue;
    /// <summary>The final underlying-composed value.</summary>
    public SvgAnimationValue? Value;
    /// <summary>The selected zero-based keyframe segment.</summary>
    public int SegmentIndex;
    /// <summary>The effective local interpolation progress.</summary>
    public double EffectiveProgress;
    /// <summary>The repeat iteration applied to accumulation.</summary>
    public int RepeatIteration;

    /// <summary>Creates one animation composition result.</summary>
    /// <param name="success">Whether composition succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="effectValue">The uncomposed effect value.</param>
    /// <param name="value">The composed value.</param>
    /// <param name="segmentIndex">The selected segment.</param>
    /// <param name="effectiveProgress">The local progress.</param>
    /// <param name="repeatIteration">The repeat iteration.</param>
    public SvgAnimationCompositionResult(bool success, SvgAnimationCompositionError error, SvgAnimationValue? effectValue, SvgAnimationValue? value, int segmentIndex, double effectiveProgress, int repeatIteration)
    {
        Success = success;
        Error = error;
        EffectValue = effectValue;
        Value = value;
        SegmentIndex = segmentIndex;
        EffectiveProgress = effectiveProgress;
        RepeatIteration = repeatIteration;
    }
}
