namespace svg;

/// <summary>Identifies the source selected for an accessible name.</summary>
public enum SvgAccessibleNameSource
{
    /// <summary>No accessible name was available.</summary>
    None = 0,
    /// <summary>The name came from <c>aria-labelledby</c>.</summary>
    AriaLabelledBy = 1,
    /// <summary>The name came from <c>aria-label</c>.</summary>
    AriaLabel = 2,
    /// <summary>The name came from a direct <c>title</c> child.</summary>
    Title = 3
}

/// <summary>Identifies the source selected for an accessible description.</summary>
public enum SvgAccessibleDescriptionSource
{
    /// <summary>No accessible description was available.</summary>
    None = 0,
    /// <summary>The description came from <c>aria-describedby</c>.</summary>
    AriaDescribedBy = 1,
    /// <summary>The description came from a direct <c>desc</c> child.</summary>
    Description = 2
}

/// <summary>Identifies whether a mapped ARIA value is a state or property.</summary>
public enum SvgAccessibilityValueKind
{
    /// <summary>The value represents mutable or current state.</summary>
    State = 0,
    /// <summary>The value represents an accessibility property or relationship.</summary>
    Property = 1
}
