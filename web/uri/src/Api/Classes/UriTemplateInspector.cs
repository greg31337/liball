namespace uri;

/// <summary>
/// Provides ordered URI Template model inspection.
/// </summary>
public class UriTemplateInspector : IUriTemplateInspector
{
    /// <summary>Initializes a stateless URI Template inspector.</summary>
    public UriTemplateInspector()
    {
    }

    /// <summary>Lists unique variable names in first-occurrence order.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <returns>The case-sensitive variable names.</returns>
    public string[] ListTemplateVariables(UriTemplate template)
    {
        return TemplateInspectorCore.ListVariables(template);
    }

    /// <summary>Lists every variable occurrence in source order.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <returns>The ordered variable specifications.</returns>
    public UriTemplateVariable[] ListTemplateOccurrences(UriTemplate template)
    {
        return TemplateInspectorCore.ListOccurrences(template);
    }

    /// <summary>Calculates the minimum RFC 6570 feature level required by the model.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <returns>The minimum required feature level.</returns>
    public UriTemplateLevel DetermineTemplateLevel(UriTemplate template)
    {
        return TemplateInspectorCore.DetermineLevel(template);
    }
}
