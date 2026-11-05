using System;

namespace html;
/// <summary>Selects how a matching attribute is retained, removed, transformed, or validated.</summary>
public enum HtmlSanitizerAttributeAction
{
    /// <summary>Retains the matched attribute after any configured normalization.</summary>
    Keep = 0,
    /// <summary>Removes the matched attribute.</summary>
    Drop = 1,
    /// <summary>Renames the matched attribute to the configured namespace and local name.</summary>
    Rename = 2,
    /// <summary>Retains the matched attribute after applying configured normalization steps.</summary>
    Normalize = 3,
    /// <summary>Retains the matched attribute only when its value passes configured validation.</summary>
    Validate = 4,
    /// <summary>Retains the matched attribute after ensuring its value begins with a configured prefix.</summary>
    Prefix = 5
}
