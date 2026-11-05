namespace svg;

/// <summary>Stores one parsed CSS easing function.</summary>
public class SvgCssEasing
{
    /// <summary>The easing algorithm.</summary>
    public SvgCssEasingKind Kind;
    /// <summary>The first Bézier x coordinate.</summary>
    public double X1;
    /// <summary>The first Bézier y coordinate.</summary>
    public double Y1;
    /// <summary>The second Bézier x coordinate.</summary>
    public double X2;
    /// <summary>The second Bézier y coordinate.</summary>
    public double Y2;
    /// <summary>The positive step count.</summary>
    public int Steps;
    /// <summary>Whether the step discontinuity occurs at interval start.</summary>
    public bool StepStart;

    /// <summary>Creates identity linear easing.</summary>
    public SvgCssEasing()
    {
        Kind = SvgCssEasingKind.Linear; X1 = 0; Y1 = 0; X2 = 1; Y2 = 1; Steps = 1; StepStart = false;
    }

    /// <summary>Maps finite unit progress through this easing function.</summary>
    /// <param name="progress">The progress in the unit interval.</param>
    /// <returns>The eased progress, or not-a-number for invalid mutable state.</returns>
    internal double Apply(double progress)
    {
        if (!SvgFiniteMath.IsFinite(progress) || progress < 0 || progress > 1) return double.NaN;
        if (Kind == SvgCssEasingKind.Linear) return progress;
        if (Kind == SvgCssEasingKind.Steps)
        {
            if (Steps < 1) return double.NaN;
            double scaled = progress * Steps;
            double value = StepStart ? System.Math.Ceiling(scaled) / Steps : System.Math.Floor(scaled) / Steps;
            if (progress == 1) value = 1;
            if (value < 0) value = 0;
            if (value > 1) value = 1;
            return value;
        }
        if (Kind != SvgCssEasingKind.CubicBezier || X1 < 0 || X1 > 1 || X2 < 0 || X2 > 1 || !SvgFiniteMath.IsFinite(Y1) || !SvgFiniteMath.IsFinite(Y2)) return double.NaN;
        double low = 0;
        double high = 1;
        int iteration = 0;
        while (iteration < 40)
        {
            double middle = (low + high) / 2;
            double x = Cubic(middle, X1, X2);
            if (x < progress) low = middle;
            else high = middle;
            iteration = iteration + 1;
        }
        return Cubic((low + high) / 2, Y1, Y2);
    }

    internal static double Cubic(double amount, double first, double second)
    {
        double inverse = 1 - amount;
        return 3 * inverse * inverse * amount * first + 3 * inverse * amount * amount * second + amount * amount * amount;
    }
}

/// <summary>Configures bounded CSS keyframe selection and sampling.</summary>
public class SvgCssAnimationOptions
{
    /// <summary>The property animation metadata.</summary>
    public SvgPropertyCatalog Properties;
    /// <summary>The typed value interpolation options.</summary>
    public SvgAnimationInterpolationOptions Interpolation;
    /// <summary>The maximum animation count in one list.</summary>
    public int MaxAnimations;
    /// <summary>The maximum inspected keyframe blocks.</summary>
    public int MaxKeyframes;
    /// <summary>The maximum animated property tracks.</summary>
    public int MaxProperties;
    /// <summary>The maximum numeric or function token length.</summary>
    public int MaxTokenCharacters;

    /// <summary>Creates secure deterministic CSS animation defaults.</summary>
    public SvgCssAnimationOptions()
    {
        Properties = SvgPropertyCatalog.CreateDefault();
        Interpolation = new SvgAnimationInterpolationOptions();
        MaxAnimations = 1024;
        MaxKeyframes = 100000;
        MaxProperties = 100000;
        MaxTokenCharacters = 4096;
    }
}

/// <summary>Stores one typed property value at a keyframe offset.</summary>
public class SvgCssAnimationKey
{
    /// <summary>The normalized offset.</summary>
    public double Offset;
    /// <summary>The typed value, or <c>null</c> for an implicit underlying endpoint.</summary>
    public SvgAnimationValue? Value;
    /// <summary>The easing applied from this key to the next key.</summary>
    public SvgCssEasing Easing;

    /// <summary>Creates one keyframe value.</summary>
    /// <param name="offset">The normalized offset.</param>
    /// <param name="value">The typed value or <c>null</c>.</param>
    /// <param name="easing">The outgoing easing.</param>
    public SvgCssAnimationKey(double offset, SvgAnimationValue? value, SvgCssEasing easing)
    {
        Offset = offset; Value = value; Easing = easing;
    }
}

/// <summary>Stores one animated CSS property track.</summary>
public class SvgCssAnimationTrack
{
    /// <summary>The normalized property name.</summary>
    public string PropertyName;
    /// <summary>The typed interpolation family.</summary>
    public SvgAnimationValueKind ValueKind;
    /// <summary>The ordered keyframe values including implicit endpoints.</summary>
    public System.Collections.Generic.List<SvgCssAnimationKey> Keys;

    /// <summary>Creates an empty property track.</summary>
    /// <param name="propertyName">The normalized property name.</param>
    /// <param name="valueKind">The interpolation family.</param>
    public SvgCssAnimationTrack(string propertyName, SvgAnimationValueKind valueKind)
    {
        PropertyName = propertyName; ValueKind = valueKind; Keys = new System.Collections.Generic.List<SvgCssAnimationKey>();
    }
}

/// <summary>Stores one resolved item from the comma-separated animation list.</summary>
public class SvgCssAnimationItem
{
    /// <summary>The zero-based list index.</summary>
    public int Index;
    /// <summary>The case-sensitive keyframes name.</summary>
    public string Name;
    /// <summary>The selected keyframes rule.</summary>
    public SvgCssKeyframes? Keyframes;
    /// <summary>The nonnegative iteration duration in seconds.</summary>
    public double DurationSeconds;
    /// <summary>The signed start delay in seconds.</summary>
    public double DelaySeconds;
    /// <summary>The nonnegative iteration count, or positive infinity.</summary>
    public double IterationCount;
    /// <summary>The playback direction.</summary>
    public SvgCssAnimationDirection Direction;
    /// <summary>The fill behavior.</summary>
    public SvgCssAnimationFillMode FillMode;
    /// <summary>The play state.</summary>
    public SvgCssAnimationPlayState PlayState;
    /// <summary>The default per-keyframe-interval easing.</summary>
    public SvgCssEasing Easing;
    /// <summary>The animated property tracks.</summary>
    public System.Collections.Generic.List<SvgCssAnimationTrack> Tracks;

    internal SvgCssAnimationItem(int index)
    {
        Index = index; Name = ""; Keyframes = null; DurationSeconds = 0; DelaySeconds = 0; IterationCount = 1; Direction = SvgCssAnimationDirection.Normal; FillMode = SvgCssAnimationFillMode.None; PlayState = SvgCssAnimationPlayState.Running; Easing = new SvgCssEasing(); Tracks = new System.Collections.Generic.List<SvgCssAnimationTrack>();
    }
}

/// <summary>Contains a resolved ordered CSS animation list.</summary>
public class SvgCssAnimationPlan
{
    /// <summary>The source cascade result.</summary>
    public SvgCascadeResult Cascade;
    /// <summary>The animations in CSS composite order.</summary>
    public System.Collections.Generic.List<SvgCssAnimationItem> Animations;
    /// <summary>Located planning diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The stable planning failure.</summary>
    public SvgCssAnimationError Error;
    /// <summary>Whether planning succeeded.</summary>
    public bool Success;

    internal SvgCssAnimationPlan(SvgCascadeResult cascade)
    {
        Cascade = cascade; Animations = new System.Collections.Generic.List<SvgCssAnimationItem>(); Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>(); Error = SvgCssAnimationError.None; Success = false;
    }
}

/// <summary>Associates one underlying cascaded property with its typed value.</summary>
public class SvgCssAnimationUnderlyingValue
{
    /// <summary>The normalized property name.</summary>
    public string PropertyName;
    /// <summary>The typed underlying value.</summary>
    public SvgAnimationValue Value;

    /// <summary>Creates an underlying property value.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The typed value.</param>
    public SvgCssAnimationUnderlyingValue(string propertyName, SvgAnimationValue value)
    {
        if (propertyName == null || propertyName.Length == 0) throw new System.ArgumentOutOfRangeException("propertyName");
        if (value == null) throw new System.ArgumentNullException("value");
        PropertyName = propertyName.ToLowerInvariant(); Value = value;
    }
}

/// <summary>Contains one final animation-origin property value.</summary>
public class SvgCssAnimationSampleValue
{
    /// <summary>The normalized property name.</summary>
    public string PropertyName;
    /// <summary>The final typed value after ordered animation composition.</summary>
    public SvgAnimationValue Value;
    /// <summary>The index of the last animation affecting the property.</summary>
    public int AnimationIndex;

    /// <summary>Creates one sampled property value.</summary>
    /// <param name="propertyName">The normalized property name.</param>
    /// <param name="value">The typed final value.</param>
    /// <param name="animationIndex">The contributing animation index.</param>
    public SvgCssAnimationSampleValue(string propertyName, SvgAnimationValue value, int animationIndex)
    {
        PropertyName = propertyName; Value = value; AnimationIndex = animationIndex;
    }
}

/// <summary>Describes one animation's phase at the sampled caller time.</summary>
public class SvgCssAnimationItemSample
{
    /// <summary>The animation list index.</summary>
    public int AnimationIndex;
    /// <summary>The sampled interval phase.</summary>
    public SvgCssAnimationPhase Phase;
    /// <summary>The zero-based iteration index.</summary>
    public int IterationIndex;
    /// <summary>The direction-adjusted iteration progress.</summary>
    public double Progress;

    /// <summary>Creates one animation timing sample.</summary>
    /// <param name="animationIndex">The animation index.</param>
    /// <param name="phase">The sampled phase.</param>
    /// <param name="iterationIndex">The iteration index.</param>
    /// <param name="progress">The directed progress.</param>
    public SvgCssAnimationItemSample(int animationIndex, SvgCssAnimationPhase phase, int iterationIndex, double progress)
    {
        AnimationIndex = animationIndex; Phase = phase; IterationIndex = iterationIndex; Progress = progress;
    }
}

/// <summary>Contains CSS animation timing and final cascade-composed values.</summary>
public class SvgCssAnimationSampleResult
{
    /// <summary>Whether sampling succeeded.</summary>
    public bool Success;
    /// <summary>The stable sampling failure.</summary>
    public SvgCssAnimationError Error;
    /// <summary>The sampled animation phases.</summary>
    public System.Collections.Generic.List<SvgCssAnimationItemSample> Animations;
    /// <summary>The final affected property values.</summary>
    public System.Collections.Generic.List<SvgCssAnimationSampleValue> Values;

    /// <summary>Creates one CSS animation sample result.</summary>
    /// <param name="success">Whether sampling succeeded.</param>
    /// <param name="error">The stable failure.</param>
    /// <param name="animations">The animation timing samples.</param>
    /// <param name="values">The final affected values.</param>
    public SvgCssAnimationSampleResult(bool success, SvgCssAnimationError error, System.Collections.Generic.List<SvgCssAnimationItemSample> animations, System.Collections.Generic.List<SvgCssAnimationSampleValue> values)
    {
        Success = success; Error = error; Animations = animations; Values = values;
    }

    /// <summary>Finds a final sampled property value.</summary>
    /// <param name="propertyName">The CSS property name.</param>
    /// <returns>The sampled value, or <c>null</c>.</returns>
    internal SvgCssAnimationSampleValue? Find(string propertyName)
    {
        int index = 0;
        while (index < Values.Count)
        {
            if (string.Equals(Values[index].PropertyName, propertyName, System.StringComparison.OrdinalIgnoreCase)) return Values[index];
            index = index + 1;
        }
        return null;
    }
}
