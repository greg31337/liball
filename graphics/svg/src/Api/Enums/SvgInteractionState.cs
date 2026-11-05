namespace svg;

/// <summary>Defines the caller-reported buttons currently pressed for one pointer.</summary>
public enum SvgPointerButtons
{
    /// <summary>No pointer button is pressed.</summary>
    None = 0,
    /// <summary>The primary pointer button is pressed.</summary>
    Primary = 1,
    /// <summary>The secondary pointer button is pressed.</summary>
    Secondary = 2,
    /// <summary>The auxiliary pointer button is pressed.</summary>
    Auxiliary = 4,
    /// <summary>The back pointer button is pressed.</summary>
    Back = 8,
    /// <summary>The forward pointer button is pressed.</summary>
    Forward = 16
}
