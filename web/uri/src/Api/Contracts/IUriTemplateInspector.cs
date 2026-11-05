namespace uri;

/// <summary>
/// Inspects ordered URI Template variables, occurrences, and required feature level.
/// </summary>
public interface IUriTemplateInspector
{
    /// <summary>Lists unique variable names in first-occurrence order.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <returns>The case-sensitive variable names.</returns>
    string[] ListTemplateVariables(UriTemplate template);

    /// <summary>Lists every variable occurrence in source order.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <returns>The ordered variable specifications.</returns>
    UriTemplateVariable[] ListTemplateOccurrences(UriTemplate template);

    /// <summary>Calculates the minimum RFC 6570 feature level required by the model.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <returns>The minimum required feature level.</returns>
    UriTemplateLevel DetermineTemplateLevel(UriTemplate template);
}
