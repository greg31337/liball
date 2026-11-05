namespace yaml;

/// <summary>
/// Represents a YAML version directive, tag directive, or preserved reserved directive.
/// </summary>
public class YamlDirective
{
    /// <summary>
    /// The directive name without the leading percent marker.
    /// </summary>
    public string Name;
    /// <summary>
    /// The directive's original argument text after its name.
    /// </summary>
    public string Value;
    /// <summary>
    /// The YAML version selected by this value.
    /// </summary>
    public YamlVersion Version;
    /// <summary>
    /// The tag handle, including its exclamation-mark delimiters.
    /// </summary>
    public string Handle;
    /// <summary>
    /// The URI or local prefix expanded by the tag handle.
    /// </summary>
    public string Prefix;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a new YAML directive with empty or default state.
    /// </summary>
    public YamlDirective()
    {
        Name = "";
        Value = "";
        Version = YamlVersion.Unknown;
        Handle = "";
        Prefix = "";
        Span = YamlSourceSpan.Empty();
    }

    /// <summary>
    /// Creates a <c>%YAML</c> directive with its parsed version and original value text.
    /// </summary>
    /// <param name="version">The YAML version represented by the directive.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML directive.</returns>
    internal static YamlDirective YamlVersionDirective(YamlVersion version, string? value, YamlSourceSpan? span)
    {
        YamlDirective directive = new YamlDirective();
        directive.Name = "YAML";
        directive.Version = version;
        directive.Value = value == null ? "" : value;
        directive.Span = span == null ? YamlSourceSpan.Empty() : span;
        return directive;
    }

    /// <summary>
    /// Creates a <c>%TAG</c> directive that maps a handle to a prefix.
    /// </summary>
    /// <param name="handle">The tag handle including exclamation-mark delimiters.</param>
    /// <param name="prefix">The prefix expanded by the tag handle.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML directive.</returns>
    internal static YamlDirective TagDirective(string? handle, string? prefix, YamlSourceSpan? span)
    {
        YamlDirective directive = new YamlDirective();
        directive.Name = "TAG";
        directive.Handle = handle == null ? "" : handle;
        directive.Prefix = prefix == null ? "" : prefix;
        directive.Value = directive.Handle + " " + directive.Prefix;
        directive.Span = span == null ? YamlSourceSpan.Empty() : span;
        return directive;
    }

    /// <summary>
    /// Creates a preserved reserved directive.
    /// </summary>
    /// <param name="name">The directive name without the percent marker.</param>
    /// <param name="value">The original argument text after the directive name.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML directive.</returns>
    internal static YamlDirective ReservedDirective(string? name, string? value, YamlSourceSpan? span)
    {
        YamlDirective directive = new YamlDirective();
        directive.Name = name == null ? "" : name;
        directive.Value = value == null ? "" : value;
        directive.Span = span == null ? YamlSourceSpan.Empty() : span;
        return directive;
    }

    /// <summary>
    /// Determines whether this instance represents a <c>%YAML</c> version directive.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsYamlVersion()
    {
        return Name == "YAML";
    }

    /// <summary>
    /// Determines whether this instance represents a <c>%TAG</c> directive.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsTag()
    {
        return Name == "TAG";
    }

    /// <summary>
    /// Creates an independent copy of this directive and its source range.
    /// </summary>
    /// <returns>The resulting YAML directive.</returns>
    internal YamlDirective Copy()
    {
        YamlDirective copy = new YamlDirective();
        copy.Name = Name;
        copy.Value = Value;
        copy.Version = Version;
        copy.Handle = Handle;
        copy.Prefix = Prefix;
        copy.Span = Span == null ? YamlSourceSpan.Empty() : Span.Copy();
        return copy;
    }
}

/// <summary>
/// Stores the handle, prefix, and source location of a tag directive.
/// </summary>
public class YamlTagDirective
{
    /// <summary>
    /// The tag handle, including its exclamation-mark delimiters.
    /// </summary>
    public string Handle;
    /// <summary>
    /// The URI or local prefix expanded by the tag handle.
    /// </summary>
    public string Prefix;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;

    /// <summary>
    /// Initializes a tag directive with the supplied handle, expansion prefix, and source range.
    /// </summary>
    public YamlTagDirective()
    {
        Handle = "";
        Prefix = "";
        Span = YamlSourceSpan.Empty();
    }

    /// <summary>
    /// Creates a tag directive with the supplied handle, expansion prefix, and source range.
    /// </summary>
    /// <param name="handle">The tag handle including exclamation-mark delimiters.</param>
    /// <param name="prefix">The prefix expanded by the tag handle.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML tag directive.</returns>
    internal static YamlTagDirective Create(string? handle, string? prefix, YamlSourceSpan? span)
    {
        YamlTagDirective directive = new YamlTagDirective();
        directive.Handle = handle == null ? "" : handle;
        directive.Prefix = prefix == null ? "" : prefix;
        directive.Span = span == null ? YamlSourceSpan.Empty() : span;
        return directive;
    }
}
