namespace svg;

/// <summary>Identifies a declarative SVG animation element.</summary>
public enum SvgAnimationKind
{
    /// <summary>The source is not a supported animation element.</summary>
    Unknown = 0,
    /// <summary>A general value animation.</summary>
    Animate = 1,
    /// <summary>A legacy color animation.</summary>
    AnimateColor = 2,
    /// <summary>A motion-path animation.</summary>
    AnimateMotion = 3,
    /// <summary>A transform-list animation.</summary>
    AnimateTransform = 4,
    /// <summary>A discrete value assignment.</summary>
    Set = 5,
    /// <summary>A timed removal of its target element.</summary>
    Discard = 6
}

/// <summary>Identifies the value slot selected by an SVG animation.</summary>
public enum SvgAnimationTargetKind
{
    /// <summary>No target value slot was selected.</summary>
    Unknown = 0,
    /// <summary>An ordinary XML attribute was selected.</summary>
    XmlAttribute = 1,
    /// <summary>An XML presentation attribute was selected.</summary>
    PresentationProperty = 2,
    /// <summary>A CSS property was selected.</summary>
    CssProperty = 3,
    /// <summary>The target transform value was selected.</summary>
    Transform = 4,
    /// <summary>The target supplemental motion transform was selected.</summary>
    Motion = 5
}

/// <summary>Defines stable animation target-resolution failures.</summary>
public enum SvgAnimationTargetError
{
    /// <summary>No target-resolution failure occurred.</summary>
    None = 0,
    /// <summary>The source is not a supported animation element.</summary>
    InvalidAnimationElement = 1,
    /// <summary>The animation target is missing or unavailable.</summary>
    MissingTarget = 2,
    /// <summary>The resolved target cannot receive an SVG animation.</summary>
    InvalidTarget = 3,
    /// <summary>A required attribute name is missing.</summary>
    MissingAttributeName = 4,
    /// <summary>The attribute name is not a bounded XML qualified name.</summary>
    InvalidAttributeName = 5,
    /// <summary>The authored attribute type is invalid.</summary>
    InvalidAttributeType = 6,
    /// <summary>The requested CSS property is unknown.</summary>
    UnknownProperty = 7,
    /// <summary>The requested XML attribute does not apply to the target.</summary>
    InapplicableAttribute = 8,
    /// <summary>The selected property or attribute is not animatable.</summary>
    NotAnimatable = 9
}
