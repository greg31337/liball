using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Receives diagnostics as template operations discover them.
/// </summary>
public interface ITemplateDiagnosticSink
{
    /// <summary>Reports one diagnostic.</summary>
    void Report(TemplateDiagnostic diagnostic);

    /// <summary>Returns reported diagnostics in report order.</summary>
    List<TemplateDiagnostic> GetDiagnostics();

    /// <summary>Removes all reported diagnostics.</summary>
    void Clear();
}

/// <summary>
/// Receives mappings between rendered output ranges and originating template spans.
/// </summary>
public interface ITemplateSourceMapCollector
{
    /// <summary>Reports one rendered-output mapping.</summary>
    void Report(TemplateSourceMapEntry entry);

    /// <summary>Returns mappings in rendered-output order.</summary>
    List<TemplateSourceMapEntry> GetMappings();

    /// <summary>Returns the mapping containing an output offset, or <c>null</c>.</summary>
    TemplateSourceMapEntry? GetMappingAt(int outputOffset);

    /// <summary>Removes all collected mappings.</summary>
    void Clear();
}

/// <summary>
/// Loads a named template source for registry-based composition.
/// </summary>
/// <remarks>
/// Return <c>null</c> when the loader does not contain the requested name so that the registry can try the next configured loader.
/// </remarks>
public interface ITemplateLoader
{
    /// <summary>
    /// Returns the named template source, or <c>null</c> when it is not found.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The matching source, or <c>null</c> when it is not found.</returns>
    TemplateSource? Load(string name);

    /// <summary>
    /// Completes with the named template source, or <c>null</c> when it is not found.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the matching source, or <c>null</c> when it is not found.</returns>
    Task<TemplateSource?> LoadAsync(string name, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves member and index access while evaluating template expressions.
/// </summary>
public interface ITemplateMemberResolver
{
    /// <summary>
    /// Resolves a named member on the supplied template value.
    /// </summary>
    /// <param name="target">The value on which lookup is performed.</param>
    /// <param name="memberName">The member name written in the template.</param>
    /// <returns>The resolved value, or a missing value when lookup does not succeed.</returns>
    TemplateValue ResolveMember(TemplateValue target, string memberName);

    /// <summary>
    /// Resolves an index or key on the supplied template value.
    /// </summary>
    /// <param name="target">The value on which lookup is performed.</param>
    /// <param name="index">The index or key used for lookup.</param>
    /// <returns>The resolved value, or a missing value when lookup does not succeed.</returns>
    TemplateValue ResolveIndex(TemplateValue target, TemplateValue index);
}

/// <summary>
/// Configures a mutable template member resolver.
/// </summary>
public interface IConfigurableTemplateMemberResolver
{
    /// <summary>
    /// Configures member resolution behavior.
    /// </summary>
    /// <param name="options">The options that control member resolution.</param>
    void Configure(TemplateMemberResolverOptions options);
}

/// <summary>
/// Converts application-specific CLR values into template runtime values.
/// </summary>
/// <remarks>
/// Return <c>null</c> when the converter does not handle the supplied value so that the next converter or a built-in conversion can run.
/// </remarks>
public interface ICustomTemplateValueConverter
{
    /// <summary>
    /// Converts an application-specific value, or returns <c>null</c> when it is not handled.
    /// </summary>
    /// <param name="value">The non-null CLR value to inspect.</param>
    /// <returns>The converted value, or <c>null</c> when this converter does not handle it.</returns>
    TemplateValue? Convert(object value);
}

/// <summary>
/// Maps CLR or dictionary member names to application-specific template names.
/// </summary>
/// <remarks>
/// Return <c>null</c> when no alias is defined for the supplied member name.
/// </remarks>
public interface ITemplateMemberNameMapper
{
    /// <summary>
    /// Returns the template alias for a member name, or <c>null</c> when no alias is defined.
    /// </summary>
    /// <param name="memberName">The CLR or dictionary member name.</param>
    /// <returns>The template alias, or <c>null</c>.</returns>
    string? MapName(string memberName);
}
