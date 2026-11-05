namespace yaml;

/// <summary>
/// Identifies the lexical role of a scanner token.
/// </summary>
public enum YamlTokenKind
{
    /// <summary>
    /// No lexical kind has been assigned.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Marks the beginning of the token stream.
    /// </summary>
    StreamStart = 1,
    /// <summary>
    /// Marks the end of the token stream.
    /// </summary>
    StreamEnd = 2,
    /// <summary>
    /// Represents the <c>---</c> document-start marker.
    /// </summary>
    DocumentStart = 3,
    /// <summary>
    /// Represents the <c>...</c> document-end marker.
    /// </summary>
    DocumentEnd = 4,
    /// <summary>
    /// Contains a directive line beginning with <c>%</c>.
    /// </summary>
    Directive = 5,
    /// <summary>
    /// Marks an increase in block indentation.
    /// </summary>
    Indent = 6,
    /// <summary>
    /// Marks a return to an earlier block indentation level.
    /// </summary>
    Dedent = 7,
    /// <summary>
    /// Represents a block sequence dash.
    /// </summary>
    BlockSequenceEntry = 8,
    /// <summary>
    /// Marks an explicit or implicit block mapping key.
    /// </summary>
    BlockMappingKey = 9,
    /// <summary>
    /// Represents the colon separating a mapping key and value.
    /// </summary>
    BlockMappingValue = 10,
    /// <summary>
    /// Represents an opening square bracket.
    /// </summary>
    FlowSequenceStart = 11,
    /// <summary>
    /// Represents a closing square bracket.
    /// </summary>
    FlowSequenceEnd = 12,
    /// <summary>
    /// Represents an opening brace.
    /// </summary>
    FlowMappingStart = 13,
    /// <summary>
    /// Represents a closing brace.
    /// </summary>
    FlowMappingEnd = 14,
    /// <summary>
    /// Represents the comma between flow collection entries.
    /// </summary>
    FlowEntry = 15,
    /// <summary>
    /// Contains comment text from a number sign through the line ending.
    /// </summary>
    Comment = 16,
    /// <summary>
    /// Preserves a blank source line for round-trip emission.
    /// </summary>
    BlankLine = 17,
    /// <summary>
    /// Contains a node tag token.
    /// </summary>
    Tag = 18,
    /// <summary>
    /// Contains an anchor declaration without structural resolution.
    /// </summary>
    Anchor = 19,
    /// <summary>
    /// Contains an alias reference without structural resolution.
    /// </summary>
    Alias = 20,
    /// <summary>
    /// Contains unquoted scalar text.
    /// </summary>
    PlainScalar = 21,
    /// <summary>
    /// Contains decoded single-quoted scalar text.
    /// </summary>
    SingleQuotedScalar = 22,
    /// <summary>
    /// Contains decoded double-quoted scalar text.
    /// </summary>
    DoubleQuotedScalar = 23,
    /// <summary>
    /// Contains literal block scalar content.
    /// </summary>
    LiteralScalar = 24,
    /// <summary>
    /// Contains folded block scalar content.
    /// </summary>
    FoldedScalar = 25,
    /// <summary>
    /// Represents invalid input retained for recovery and diagnostics.
    /// </summary>
    Error = 26,
    /// <summary>
    /// Represents non-structural horizontal whitespace.
    /// </summary>
    Whitespace = 27
}

/// <summary>
/// Identifies whether preserved YAML trivia is a comment or a blank source line.
/// </summary>
public enum YamlTriviaKind
{
    /// <summary>
    /// No trivia kind has been assigned.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A source comment beginning with a number sign.
    /// </summary>
    Comment = 1,
    /// <summary>
    /// A preserved blank source line.
    /// </summary>
    BlankLine = 2
}

/// <summary>
/// Identifies where trivia is placed relative to its owning YAML item.
/// </summary>
public enum YamlTriviaPlacement
{
    /// <summary>
    /// No trivia placement has been assigned.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Trivia appears on a line before the owning item.
    /// </summary>
    Before = 1,
    /// <summary>
    /// A comment appears on the same line as the owning item.
    /// </summary>
    Inline = 2,
    /// <summary>
    /// Trivia appears on a later line following the owning item.
    /// </summary>
    Following = 3
}

/// <summary>
/// Identifies the structural role of a parser event.
/// </summary>
public enum YamlEventKind
{
    /// <summary>
    /// No structural event kind has been assigned.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Begins one YAML stream.
    /// </summary>
    StreamStart = 1,
    /// <summary>
    /// Ends one YAML stream.
    /// </summary>
    StreamEnd = 2,
    /// <summary>
    /// Begins one document, whether its marker was explicit or implicit.
    /// </summary>
    DocumentStart = 3,
    /// <summary>
    /// Ends one document, whether its marker was explicit or implicit.
    /// </summary>
    DocumentEnd = 4,
    /// <summary>
    /// Begins a sequence and records its block or flow style.
    /// </summary>
    SequenceStart = 5,
    /// <summary>
    /// Ends the current sequence.
    /// </summary>
    SequenceEnd = 6,
    /// <summary>
    /// Begins a mapping and records its block or flow style.
    /// </summary>
    MappingStart = 7,
    /// <summary>
    /// Ends the current mapping.
    /// </summary>
    MappingEnd = 8,
    /// <summary>
    /// Carries scalar text, style, tag, anchor, and source metadata.
    /// </summary>
    Scalar = 9,
    /// <summary>
    /// Carries an alias name for later anchor resolution.
    /// </summary>
    Alias = 10,
    /// <summary>
    /// Carries preserved comment or blank-line trivia.
    /// </summary>
    Comment = 11,
    /// <summary>
    /// Carries a structured parser diagnostic.
    /// </summary>
    Diagnostic = 12,
    /// <summary>
    /// Carries a parsed <c>%YAML</c> version directive.
    /// </summary>
    YamlVersionDirective = 13,
    /// <summary>
    /// Carries a parsed <c>%TAG</c> handle declaration.
    /// </summary>
    TagDirective = 14,
    /// <summary>
    /// Carries an unrecognized directive preserved for diagnostics and re-emission.
    /// </summary>
    ReservedDirective = 15
}

/// <summary>
/// Identifies the data shape represented by a YAML node.
/// </summary>
public enum YamlNodeKind
{
    /// <summary>
    /// The node has not been initialized with a supported shape.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// An omitted YAML node with no scalar text.
    /// </summary>
    Empty = 1,
    /// <summary>
    /// A scalar value represented by text and an optional resolved tag.
    /// </summary>
    Scalar = 2,
    /// <summary>
    /// An ordered collection of child nodes.
    /// </summary>
    Sequence = 3,
    /// <summary>
    /// An ordered collection of key and value pairs.
    /// </summary>
    Mapping = 4,
    /// <summary>
    /// A reference to a named anchor and, when resolved, its target node.
    /// </summary>
    Alias = 5
}

/// <summary>
/// Identifies how a path segment navigates its current node.
/// </summary>
public enum YamlPathSegmentKind
{
    /// <summary>
    /// The segment cannot navigate a node.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Selects a mapping value by a scalar string key.
    /// </summary>
    MappingKey = 1,
    /// <summary>
    /// Selects a sequence child by its zero-based index.
    /// </summary>
    SequenceIndex = 2
}

/// <summary>
/// Selects how a scalar is written or records how it appeared in the source.
/// </summary>
public enum YamlScalarStyle
{
    /// <summary>
    /// Lets parsing retain the observed style or emission choose a safe style.
    /// </summary>
    Any = 0,
    /// <summary>
    /// Uses unquoted plain scalar notation when the text is legal and unambiguous.
    /// </summary>
    Plain = 1,
    /// <summary>
    /// Uses single quotes, escaping an embedded quote by doubling it.
    /// </summary>
    SingleQuoted = 2,
    /// <summary>
    /// Uses double quotes and YAML escape sequences.
    /// </summary>
    DoubleQuoted = 3,
    /// <summary>
    /// Uses a literal block scalar so line breaks are preserved.
    /// </summary>
    Literal = 4,
    /// <summary>
    /// Uses a folded block scalar so eligible line breaks become spaces.
    /// </summary>
    Folded = 5
}

/// <summary>
/// Selects block or flow notation for a sequence or mapping.
/// </summary>
public enum YamlCollectionStyle
{
    /// <summary>
    /// Lets parsing retain the observed style or emission use its configured default.
    /// </summary>
    Any = 0,
    /// <summary>
    /// Uses indentation-based sequence or mapping notation.
    /// </summary>
    Block = 1,
    /// <summary>
    /// Uses bracketed sequence or braced mapping notation.
    /// </summary>
    Flow = 2
}

/// <summary>
/// Selects how emission handles scalar values that cannot be represented using their requested YAML form.
/// </summary>
public enum YamlInvalidValuePolicy
{
    /// <summary>
    /// Stop emission and throw a <see cref="YamlException"/>.
    /// </summary>
    Error = 0,
    /// <summary>
    /// Emit the scalar text using a safe quoted string representation.
    /// </summary>
    QuoteAsString = 1
}

/// <summary>
/// Identifies a supported YAML language version.
/// </summary>
public enum YamlVersion
{
    /// <summary>
    /// No supported YAML version was specified.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Uses YAML 1.1 compatibility behavior where enabled.
    /// </summary>
    Version11 = 1,
    /// <summary>
    /// Uses YAML 1.2 behavior, which is the library default.
    /// </summary>
    Version12 = 2
}

/// <summary>
/// Selects the scalar-resolution rules applied to plain values.
/// </summary>
public enum YamlSchemaKind
{
    /// <summary>
    /// Resolves only strings, sequences, and mappings.
    /// </summary>
    Failsafe = 0,
    /// <summary>
    /// Uses JSON-compatible null, Boolean, number, string, sequence, and mapping rules.
    /// </summary>
    Json = 1,
    /// <summary>
    /// Uses the YAML 1.2 core schema and is the default.
    /// </summary>
    Core = 2,
    /// <summary>
    /// Enables YAML 1.1-compatible scalar forms such as legacy Boolean and numeric spellings.
    /// </summary>
    Yaml11 = 3
}

/// <summary>
/// Selects how public field names map to YAML mapping keys.
/// </summary>
public enum YamlNamingConvention
{
    /// <summary>
    /// Leaves the CLR public field name unchanged.
    /// </summary>
    Exact = 0,
    /// <summary>
    /// Writes the first word in lowercase and capitalizes later words.
    /// </summary>
    CamelCase = 1,
    /// <summary>
    /// Capitalizes every word without a delimiter.
    /// </summary>
    PascalCase = 2,
    /// <summary>
    /// Joins lowercase words with underscores.
    /// </summary>
    SnakeCase = 3,
    /// <summary>
    /// Joins lowercase words with hyphens.
    /// </summary>
    KebabCase = 4
}

/// <summary>
/// Indicates the importance of a diagnostic.
/// </summary>
public enum YamlDiagnosticSeverity
{
    /// <summary>
    /// Provides information without indicating a failed operation.
    /// </summary>
    Info = 0,
    /// <summary>
    /// Reports suspicious input while allowing the operation to continue.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Indicates that the operation did not complete successfully.
    /// </summary>
    Error = 2
}

/// <summary>
/// Selects how composition handles repeated mapping keys.
/// </summary>
public enum YamlDuplicateKeyPolicy
{
    /// <summary>
    /// Report an error and retain the existing mapping state.
    /// </summary>
    Error = 0,
    /// <summary>
    /// Report a warning and keep processing duplicate keys.
    /// </summary>
    Warn = 1,
    /// <summary>
    /// Keep the first value and ignore later duplicates.
    /// </summary>
    FirstWins = 2,
    /// <summary>
    /// Replace the earlier value with the last duplicate.
    /// </summary>
    LastWins = 3,
    /// <summary>
    /// Keep every pair in its original order.
    /// </summary>
    PreserveAll = 4
}

/// <summary>
/// Selects how schema processing handles tags it does not recognize.
/// </summary>
public enum YamlUnknownTagPolicy
{
    /// <summary>
    /// Keep the tag without adding a diagnostic.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Keep the tag and add a warning.
    /// </summary>
    Warn = 1,
    /// <summary>
    /// Keep the tag and add an error.
    /// </summary>
    Error = 2
}
