namespace odf;

/// <summary>
/// Describes a presentation transition.
/// </summary>
public class OdfSlideTransition
{
    /// <summary>
    /// The transition family.
    /// </summary>
    public OdfTransitionKind Kind;
    /// <summary>The preserved SMIL transition type.</summary>
    public string Type = "";
    /// <summary>The preserved SMIL transition subtype.</summary>
    public string Subtype = "";
    /// <summary>The preserved SMIL transition direction.</summary>
    public string Direction = "";
    /// <summary>The preserved SMIL fade color.</summary>
    public string FadeColor = "";
    /// <summary>The legacy presentation transition mode.</summary>
    public string TransitionType = "";
    /// <summary>The legacy presentation transition style.</summary>
    public string TransitionStyle = "";
    /// <summary>The transition speed token.</summary>
    public string Speed = "";
    /// <summary>The automatic slide duration.</summary>
    public string Duration = "";
    /// <summary>Whether the duration is absent or a valid duration.</summary>
    public bool IsDurationValid;
    /// <summary>Whether the slide uses a valid automatic rehearsal timing.</summary>
    public bool IsRehearsalTiming;
    /// <summary>The transition sound reference.</summary>
    public string SoundHref = "";
    /// <summary>The transition sound XLink type.</summary>
    public string SoundType = "";
    /// <summary>The transition sound XLink show behavior.</summary>
    public string SoundShow = "";
    /// <summary>The transition sound activation behavior.</summary>
    public string SoundActuate = "";
    /// <summary>The preserved play-full token.</summary>
    public string SoundPlayFullValue = "";
    /// <summary>Whether the entire transition sound should play.</summary>
    public bool SoundPlayFull;
    /// <summary>Whether the play-full token is absent or valid.</summary>
    public bool IsSoundPlayFullValid;
    /// <summary>The slide owning the transition.</summary>
    public OdfSlide? Slide;
    /// <summary>The drawing-page style supplying the transition.</summary>
    public OdfStyle? Style;
    /// <summary>The drawing-page-properties element.</summary>
    public OdfNode? Node;
    /// <summary>The transition sound element, when present.</summary>
    public OdfNode? SoundNode;
    internal OdfTransitionKind OriginalKind;
    internal string OriginalType = "";
    internal string OriginalSubtype = "";
    internal string OriginalDirection = "";
    internal string OriginalFadeColor = "";
    internal string OriginalTransitionType = "";
    internal string OriginalTransitionStyle = "";
    internal string OriginalSpeed = "";
    internal string OriginalDuration = "";
    internal string OriginalSoundHref = "";
    internal string OriginalSoundType = "";
    internal string OriginalSoundShow = "";
    internal string OriginalSoundActuate = "";
    internal string OriginalSoundPlayFullValue = "";
}
