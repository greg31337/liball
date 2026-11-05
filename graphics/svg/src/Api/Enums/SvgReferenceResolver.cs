namespace svg;

/// <summary>Identifies which SVG reference attribute supplied an effective value.</summary>
public enum SvgReferenceAttributeKind
{
    /// <summary>No reference attribute was selected.</summary>
    None = 0,
    /// <summary>The unnamespaced SVG 2 <c>href</c> attribute was selected.</summary>
    Href = 1,
    /// <summary>The legacy XLink <c>href</c> attribute was selected.</summary>
    XLinkHref = 2
}

/// <summary>Identifies the outcome of general SVG URL-reference processing.</summary>
public enum SvgReferenceStatus
{
    /// <summary>No reference attribute was present.</summary>
    Missing = 0,
    /// <summary>The selected reference or one of its base URIs was invalid.</summary>
    Invalid = 1,
    /// <summary>The reference identifies an external resource that has not been supplied.</summary>
    External = 2,
    /// <summary>The available target document did not contain the requested fragment.</summary>
    TargetNotFound = 3,
    /// <summary>The reference resolved to an available document or element.</summary>
    Resolved = 4
}
