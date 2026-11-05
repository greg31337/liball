namespace ttf;

/// <summary>Identifies the script-specific shaping model used for a run.</summary>
public enum TextShapingPlanKind
{
    /// <summary>The default OpenType shaping model.</summary>
    Default = 0,

    /// <summary>The Arabic, Syriac, or Mongolian joining model.</summary>
    Arabic = 1,

    /// <summary>The Hebrew shaping model.</summary>
    Hebrew = 2,

    /// <summary>The Indic shaping model.</summary>
    Indic = 3,

    /// <summary>The Khmer shaping model.</summary>
    Khmer = 4,

    /// <summary>The Myanmar shaping model.</summary>
    Myanmar = 5,

    /// <summary>The Thai or Lao shaping model.</summary>
    ThaiLao = 6,

    /// <summary>The Hangul shaping model.</summary>
    Hangul = 7,

    /// <summary>The Universal Shaping Engine model.</summary>
    Universal = 8
}
