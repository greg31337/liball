namespace ttf;

/// <summary>Controls glyph orientation behavior for vertical shaping.</summary>
public enum TextShapingVerticalBehavior
{
    /// <summary>Use Unicode vertical orientation and available vertical features.</summary>
    Automatic = 0,

    /// <summary>Keep every cluster upright and request vertical alternates.</summary>
    Upright = 1,

    /// <summary>Keep horizontal glyph forms for caller-side rotation.</summary>
    Sideways = 2
}
