namespace fake;

/// <summary>
/// Describes the strong writing direction found in a string.
/// </summary>
public enum WritingDirection
{
    /// <summary>
    /// The string contains no strong directional characters.
    /// </summary>
    Neutral = 0,

    /// <summary>
    /// The string contains only strong left-to-right characters.
    /// </summary>
    LeftToRight = 1,

    /// <summary>
    /// The string contains only strong right-to-left characters.
    /// </summary>
    RightToLeft = 2,

    /// <summary>
    /// The string contains both strong left-to-right and right-to-left characters.
    /// </summary>
    Mixed = 3
}
