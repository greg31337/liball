namespace svg;

/// <summary>Identifies an explicitly injected animation timing event.</summary>
public enum SvgAnimationEventKind
{
    /// <summary>An element event such as click or mouseover.</summary>
    Event = 0,
    /// <summary>An access-key activation.</summary>
    AccessKey = 1,
    /// <summary>A caller-resolved wallclock instant.</summary>
    Wallclock = 2
}
