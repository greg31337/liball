namespace yaml;

/// <summary>
/// Represents one parser event in the YAML stream, document, collection, or node sequence.
/// </summary>
public class YamlEvent
{
    internal string TagPresentation;
    internal string ParsedTag;
    internal bool ImplicitEmpty;
    /// <summary>
    /// The structural role of this event.
    /// </summary>
    public YamlEventKind Kind;
    /// <summary>
    /// The decoded scalar or comment text carried by this event, or an empty string for structural events.
    /// </summary>
    public string Value;
    /// <summary>
    /// The YAML tag, using its expanded form when schema processing has resolved it.
    /// </summary>
    public string Tag;
    /// <summary>
    /// The anchor name declared on this node or event, without the ampersand marker.
    /// </summary>
    public string Anchor;
    /// <summary>
    /// The referenced anchor name, without the asterisk marker.
    /// </summary>
    public string AliasName;
    /// <summary>
    /// The scalar presentation style read from or requested for this value.
    /// </summary>
    public YamlScalarStyle ScalarStyle;
    /// <summary>
    /// The block or flow presentation style read from or requested for this collection.
    /// </summary>
    public YamlCollectionStyle CollectionStyle;
    /// <summary>
    /// The source range occupied by this item.
    /// </summary>
    public YamlSourceSpan Span;
    /// <summary>
    /// The structured diagnostic carried by a diagnostic event.
    /// </summary>
    public YamlDiagnostic Diagnostic;
    /// <summary>
    /// The structured directive carried by a YAML version, tag, or reserved directive event.
    /// </summary>
    public YamlDirective Directive;
    /// <summary>
    /// The comment or blank-line metadata carried by a comment event.
    /// </summary>
    public YamlTrivia Trivia;

    /// <summary>
    /// Initializes a new YAML event with empty or default state.
    /// </summary>
    public YamlEvent()
    {
        Kind = YamlEventKind.Unknown;
        TagPresentation = "";
        ParsedTag = "";
        ImplicitEmpty = false;
        Value = "";
        Tag = "";
        Anchor = "";
        AliasName = "";
        ScalarStyle = YamlScalarStyle.Any;
        CollectionStyle = YamlCollectionStyle.Any;
        Span = YamlSourceSpan.Empty();
        Diagnostic = new YamlDiagnostic();
        Directive = new YamlDirective();
        Trivia = new YamlTrivia();
    }

    /// <summary>
    /// Creates an event with the supplied kind, value, and source range.
    /// </summary>
    /// <param name="kind">The kind assigned to the new value.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML event.</returns>
    internal static YamlEvent Create(YamlEventKind kind, string? value, YamlSourceSpan? span)
    {
        YamlEvent yamlEvent = new YamlEvent();
        yamlEvent.Kind = kind;
        yamlEvent.Value = value == null ? "" : value;
        yamlEvent.Span = span == null ? YamlSourceSpan.Empty() : span;
        return yamlEvent;
    }

    /// <summary>
    /// Creates a scalar node with the requested text and presentation style.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML event.</returns>
    internal static YamlEvent Scalar(string? value, YamlScalarStyle style, YamlSourceSpan? span)
    {
        YamlEvent yamlEvent = Create(YamlEventKind.Scalar, value, span);
        yamlEvent.ScalarStyle = style;
        return yamlEvent;
    }

    /// <summary>
    /// Creates an alias node for the supplied anchor name.
    /// </summary>
    /// <param name="aliasName">The anchor name referenced by the alias, without the asterisk marker.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML event.</returns>
    internal static YamlEvent Alias(string? aliasName, YamlSourceSpan? span)
    {
        YamlEvent yamlEvent = Create(YamlEventKind.Alias, aliasName, span);
        yamlEvent.AliasName = aliasName == null ? "" : aliasName;
        return yamlEvent;
    }

    /// <summary>
    /// Creates an event that starts a sequence with the requested collection style.
    /// </summary>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML event.</returns>
    internal static YamlEvent SequenceStart(YamlCollectionStyle style, YamlSourceSpan? span)
    {
        YamlEvent yamlEvent = Create(YamlEventKind.SequenceStart, "", span);
        yamlEvent.CollectionStyle = style;
        return yamlEvent;
    }

    /// <summary>
    /// Creates an event that starts a mapping with the requested collection style.
    /// </summary>
    /// <param name="style">The presentation style assigned to the value.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML event.</returns>
    internal static YamlEvent MappingStart(YamlCollectionStyle style, YamlSourceSpan? span)
    {
        YamlEvent yamlEvent = Create(YamlEventKind.MappingStart, "", span);
        yamlEvent.CollectionStyle = style;
        return yamlEvent;
    }

    /// <summary>
    /// Creates a comment event containing comment text without structural interpretation.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="span">The source range associated with the value.</param>
    /// <returns>The resulting YAML event.</returns>
    internal static YamlEvent Comment(string? value, YamlSourceSpan? span)
    {
        YamlEvent yamlEvent = Create(YamlEventKind.Comment, value, span);
        YamlTriviaKind kind = value == null || value == "" ? YamlTriviaKind.BlankLine : YamlTriviaKind.Comment;
        yamlEvent.Trivia = YamlTrivia.Create(kind, YamlTriviaPlacement.Before, value, span);
        return yamlEvent;
    }

    /// <summary>
    /// Creates an event that carries a structured diagnostic through the event stream.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to append.</param>
    /// <returns>The resulting YAML event.</returns>
    internal static YamlEvent DiagnosticEvent(YamlDiagnostic? diagnostic)
    {
        YamlEvent yamlEvent = Create(YamlEventKind.Diagnostic, "", diagnostic == null ? YamlSourceSpan.Empty() : diagnostic.Span);
        if (diagnostic != null)
        {
            yamlEvent.Diagnostic = diagnostic;
        }

        return yamlEvent;
    }

    /// <summary>
    /// Creates an event that carries a parsed directive.
    /// </summary>
    /// <param name="kind">The directive event kind.</param>
    /// <param name="directive">The structured directive.</param>
    /// <returns>The resulting YAML event.</returns>
    internal static YamlEvent DirectiveEvent(YamlEventKind kind, YamlDirective? directive)
    {
        YamlEvent yamlEvent = Create(
            kind,
            directive == null ? "" : directive.Value,
            directive == null ? YamlSourceSpan.Empty() : directive.Span);
        if (directive != null)
        {
            yamlEvent.Directive = directive;
        }

        return yamlEvent;
    }

    /// <summary>
    /// Determines whether this event represents a scalar, alias, or collection start.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsNode()
    {
        return YamlEventRules.IsNodeEvent(Kind);
    }

    /// <summary>
    /// Determines whether this event starts a sequence or mapping.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsCollectionStart()
    {
        return YamlEventRules.IsCollectionStart(Kind);
    }

    /// <summary>
    /// Determines whether this event ends a sequence or mapping.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsCollectionEnd()
    {
        return YamlEventRules.IsCollectionEnd(Kind);
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML event.</returns>
    internal YamlEvent Copy()
    {
        YamlEvent copy = Create(Kind, Value, Span.Copy());
        copy.Tag = Tag;
        copy.TagPresentation = TagPresentation;
        copy.ParsedTag = ParsedTag;
        copy.Anchor = Anchor;
        copy.AliasName = AliasName;
        copy.ScalarStyle = ScalarStyle;
        copy.CollectionStyle = CollectionStyle;
        copy.ImplicitEmpty = ImplicitEmpty;
        copy.Diagnostic = Diagnostic;
        copy.Directive = Directive == null ? new YamlDirective() : Directive.Copy();
        copy.Trivia = Trivia == null ? new YamlTrivia() : Trivia.Copy();
        return copy;
    }
}
