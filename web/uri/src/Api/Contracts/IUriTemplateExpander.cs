namespace uri;

/// <summary>
/// Expands reusable RFC 6570 URI Template models from ordered variable bindings.
/// </summary>
public interface IUriTemplateExpander
{
    /// <summary>Expands a URI Template and throws when expansion fails.</summary>
    /// <param name="template">The parsed reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>The expanded URI-reference text.</returns>
    string ExpandTemplate(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options);

    /// <summary>Expands a URI Template without throwing for expected failure.</summary>
    /// <param name="template">The parsed reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>The expansion outcome.</returns>
    UriTemplateExpansionResult TryExpandTemplate(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options);

    /// <summary>Expands defined variables while preserving unresolved expressions.</summary>
    /// <param name="template">The parsed reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>The partially expanded URI Template text.</returns>
    string ExpandTemplatePartially(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions options);
}
