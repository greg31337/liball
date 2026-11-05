namespace odf;

/// <summary>
/// Identifies an OpenDocument animation construct.
/// </summary>
public enum OdfAnimationKind
{
    /// <summary>
    /// The animation kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A sequential timing container.
    /// </summary>
    Sequence = 1,
    /// <summary>
    /// A parallel timing container.
    /// </summary>
    Parallel = 2,
    /// <summary>
    /// An iterative timing container.
    /// </summary>
    Iterate = 3,
    /// <summary>
    /// A general attribute animation.
    /// </summary>
    Animate = 4,
    /// <summary>
    /// A color animation.
    /// </summary>
    AnimateColor = 5,
    /// <summary>
    /// A motion animation.
    /// </summary>
    AnimateMotion = 6,
    /// <summary>
    /// A transform animation.
    /// </summary>
    AnimateTransform = 7,
    /// <summary>
    /// A discrete value-setting animation.
    /// </summary>
    Set = 8,
    /// <summary>
    /// An animation command.
    /// </summary>
    Command = 9,
    /// <summary>
    /// An audio animation element.
    /// </summary>
    Audio = 10,
    /// <summary>
    /// A transition-filter animation.
    /// </summary>
    TransitionFilter = 11
}
