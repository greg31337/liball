namespace pptx;

/// <summary>
/// Defines common animation start triggers.
/// </summary>
public static class PptxAnimationTrigger
{
    /// <summary>Starts the effect when the slide is clicked.</summary>
    public const string OnClick = "onClick";
    /// <summary>Starts the effect with the preceding effect.</summary>
    public const string WithPrevious = "withPrevious";
    /// <summary>Starts the effect after the preceding effect finishes.</summary>
    public const string AfterPrevious = "afterPrevious";
}
