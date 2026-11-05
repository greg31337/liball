namespace uri;

/// <summary>
/// Provides bounded RFC 6570 URI Template expansion.
/// </summary>
public class UriTemplateExpander : IUriTemplateExpander
{
    /// <summary>Initializes a stateless URI Template expander.</summary>
    public UriTemplateExpander()
    {
    }

    /// <summary>Expands a URI Template and throws when expansion fails.</summary>
    /// <param name="template">The parsed reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>The expanded URI-reference text.</returns>
    /// <exception cref="UriTemplateExpansionException">Expansion or a configured limit fails.</exception>
    public string ExpandTemplate(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options)
    {
        return TemplateExpanderCore.ExpandStrict(
            template,
            bindings,
            options);
    }

    /// <summary>Expands a URI Template without throwing for expected failure.</summary>
    /// <param name="template">The parsed reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>The expansion outcome.</returns>
    public UriTemplateExpansionResult TryExpandTemplate(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options)
    {
        return TemplateExpanderCore.ExpandResult(
            template,
            bindings,
            options);
    }

    /// <summary>Expands defined variables while preserving unresolved expressions.</summary>
    /// <param name="template">The parsed reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>The partially expanded URI Template text.</returns>
    /// <exception cref="UriTemplateExpansionException">Expansion or a configured limit fails.</exception>
    public string ExpandTemplatePartially(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options)
    {
        return TemplateExpanderCore.ExpandPartial(
            template,
            bindings,
            options);
    }
}
