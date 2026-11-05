using System;

namespace template;

/// <summary>
/// Records the identity of a template or partial loaded during rendering.
/// </summary>
public class TemplateDependency
{
    /// <summary>
    /// Identifies the kind represented by this dependency.
    /// </summary>
    public TemplateDependencyKind Kind;
    /// <summary>
    /// Stores the logical name used to identify this value.
    /// </summary>
    public string Name;
    /// <summary>
    /// Specifies the path for this dependency.
    /// </summary>
    public string? Path;
    /// <summary>
    /// Specifies the version for this dependency.
    /// </summary>
    public string? Version;
    /// <summary>
    /// Specifies the content hash for this dependency.
    /// </summary>
    public string? ContentHash;

    /// <summary>
    /// Initializes a new template dependency and validates any supplied values.
    /// </summary>
    /// <param name="kind">The kind represented by the new value.</param>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="path">The path used by the operation.</param>
    /// <param name="version">The version used by the operation.</param>
    /// <param name="contentHash">The content hash used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateDependency(TemplateDependencyKind kind, string name, string? path, string? version, string? contentHash)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        Kind = kind;
        Name = name;
        Path = path;
        Version = version;
        ContentHash = contentHash;
    }

    /// <summary>
    /// Creates a dependency by copying identity metadata from a template source.
    /// </summary>
    /// <param name="kind">The kind represented by the new value.</param>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <returns>The new dependency.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateDependency FromSource(TemplateDependencyKind kind, TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        return new TemplateDependency(kind, source.Name, source.Path, source.Version, source.ContentHash);
    }
}
