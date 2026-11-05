namespace odf;

/// <summary>Describes one parsed SMIL begin or end timing specifier.</summary>
public class OdfAnimationTiming
{
    /// <summary>The preserved timing expression.</summary>
    public string Expression = "";
    /// <summary>The recognized timing kind.</summary>
    public OdfAnimationTimingKind Kind;
    /// <summary>The signed offset from the timing event, in seconds.</summary>
    public double OffsetSeconds;
    /// <summary>The identifier of the referenced timing source.</summary>
    public string ReferenceId = "";
    /// <summary>The event, key, marker, or sync point name.</summary>
    public string EventName = "";
    /// <summary>The repeat iteration when one is specified.</summary>
    public int RepeatIndex;
    /// <summary>Whether a repeat iteration is specified.</summary>
    public bool IsRepeatIndexSpecified;
    /// <summary>Whether the timing expression is syntactically valid.</summary>
    public bool IsValid;
    /// <summary>The resolved animation timing source.</summary>
    public OdfAnimation? ReferencedAnimation;
    /// <summary>The resolved drawing-object event source.</summary>
    public OdfDrawingObject? ReferencedDrawingObject;
    /// <summary>Whether the timing source is absent or resolves uniquely.</summary>
    public bool IsReferenceValid = true;
    /// <summary>Whether more than one timing source matches the reference.</summary>
    public bool IsReferenceAmbiguous;
}
