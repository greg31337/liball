using System.Collections.Generic;

namespace yaml;

/// <summary>
/// Groups parser, composer, schema, emitter, object-conversion, and security settings.
/// </summary>
public class YamlOptions
{
    /// <summary>
    /// Settings used by the scanner and parser stages.
    /// </summary>
    public YamlParserOptions Parser;
    /// <summary>
    /// Settings used while building node graphs from events.
    /// </summary>
    public YamlComposerOptions Composer;
    /// <summary>
    /// The schema selection or schema settings used for scalar and tag resolution.
    /// </summary>
    public YamlSchemaOptions Schema;
    /// <summary>
    /// Settings used when producing YAML text.
    /// </summary>
    public YamlEmitterOptions Emitter;
    /// <summary>
    /// Settings used when converting ordinary CLR objects.
    /// </summary>
    public YamlObjectOptions Object;
    /// <summary>
    /// Resource limits applied while processing YAML.
    /// </summary>
    public YamlSecurityOptions Security;

    /// <summary>
    /// Initializes a new YAML options and initializes every option group with library defaults.
    /// </summary>
    public YamlOptions()
    {
        Parser = new YamlParserOptions();
        Composer = new YamlComposerOptions();
        Schema = new YamlSchemaOptions();
        Emitter = new YamlEmitterOptions();
        Object = new YamlObjectOptions();
        Security = new YamlSecurityOptions();
    }

    /// <summary>
    /// Creates a complete option set initialized to the library defaults.
    /// </summary>
    /// <returns>The resulting YAML options.</returns>
    internal static YamlOptions Default()
    {
        return new YamlOptions();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void ResetToDefaults()
    {
        Parser.ResetToDefaults();
        Composer.ResetToDefaults();
        Schema.ResetToDefaults();
        Emitter.ResetToDefaults();
        Object.ResetToDefaults();
        Security.ResetToDefaults();
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML options.</returns>
    internal YamlOptions Copy()
    {
        YamlOptions copy = new YamlOptions();
        copy.Parser = Parser.Copy();
        copy.Composer = Composer.Copy();
        copy.Schema = Schema.Copy();
        copy.Emitter = Emitter.Copy();
        copy.Object = Object.Copy();
        copy.Security = Security.Copy();
        return copy;
    }
}

/// <summary>
/// Controls token parsing, directive handling, comments, recovery, and diagnostic limits.
/// </summary>
public class YamlParserOptions
{
    /// <summary>
    /// The YAML version selected by this value.
    /// </summary>
    public YamlVersion Version;
    /// <summary>
    /// Whether parsing continues after recoverable syntax errors to collect more diagnostics.
    /// </summary>
    public bool Recover;
    /// <summary>
    /// Whether comments and blank lines are included in the token and event streams.
    /// </summary>
    public bool IncludeComments;
    /// <summary>
    /// Whether YAML directives are accepted before document content.
    /// </summary>
    public bool AllowDirectives;
    /// <summary>
    /// The maximum number of diagnostics collected; a non-positive value means no separate limit.
    /// </summary>
    public int MaxDiagnostics;

    /// <summary>
    /// Initializes a new YAML parser options with YAML 1.2 parser defaults.
    /// </summary>
    public YamlParserOptions()
    {
        ResetToDefaults();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void ResetToDefaults()
    {
        Version = YamlVersion.Version12;
        Recover = false;
        IncludeComments = true;
        AllowDirectives = true;
        MaxDiagnostics = 100;
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML parser options.</returns>
    internal YamlParserOptions Copy()
    {
        YamlParserOptions copy = new YamlParserOptions();
        copy.Version = Version;
        copy.Recover = Recover;
        copy.IncludeComments = IncludeComments;
        copy.AllowDirectives = AllowDirectives;
        copy.MaxDiagnostics = MaxDiagnostics;
        return copy;
    }
}

/// <summary>
/// Controls graph composition, duplicate keys, aliases, merge keys, and comments.
/// </summary>
public class YamlComposerOptions
{
    /// <summary>
    /// The action taken when equivalent mapping keys occur more than once.
    /// </summary>
    public YamlDuplicateKeyPolicy DuplicateKeyPolicy;
    /// <summary>
    /// The action taken when a tag is not built in or registered.
    /// </summary>
    public YamlUnknownTagPolicy UnknownTagPolicy;
    /// <summary>
    /// Whether aliases are linked to the anchor nodes they reference.
    /// </summary>
    public bool ResolveAliases;
    /// <summary>
    /// Whether an alias may point back into the graph currently being composed.
    /// </summary>
    public bool AllowRecursiveAliases;
    /// <summary>
    /// Whether YAML merge keys are recognized and may be applied.
    /// </summary>
    public bool AllowMergeKeys;
    /// <summary>
    /// Whether comments and blank lines are attached to the document model.
    /// </summary>
    public bool PreserveComments;

    /// <summary>
    /// Initializes a new YAML composer options with safe graph-composition defaults.
    /// </summary>
    public YamlComposerOptions()
    {
        ResetToDefaults();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void ResetToDefaults()
    {
        DuplicateKeyPolicy = YamlDuplicateKeyPolicy.Error;
        UnknownTagPolicy = YamlUnknownTagPolicy.Preserve;
        ResolveAliases = true;
        AllowRecursiveAliases = true;
        AllowMergeKeys = false;
        PreserveComments = true;
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML composer options.</returns>
    internal YamlComposerOptions Copy()
    {
        YamlComposerOptions copy = new YamlComposerOptions();
        copy.DuplicateKeyPolicy = DuplicateKeyPolicy;
        copy.UnknownTagPolicy = UnknownTagPolicy;
        copy.ResolveAliases = ResolveAliases;
        copy.AllowRecursiveAliases = AllowRecursiveAliases;
        copy.AllowMergeKeys = AllowMergeKeys;
        copy.PreserveComments = PreserveComments;
        return copy;
    }
}

/// <summary>
/// Controls scalar resolution, tag handling, and the YAML schema applied to nodes.
/// </summary>
public class YamlSchemaOptions
{
    /// <summary>
    /// The schema selection or schema settings used for scalar and tag resolution.
    /// </summary>
    public YamlSchemaKind Schema;
    /// <summary>
    /// Whether plain scalar text receives a schema-derived tag.
    /// </summary>
    public bool ResolveScalars;
    /// <summary>
    /// Whether YAML 1.1 boolean words such as yes and on are recognized.
    /// </summary>
    public bool AllowYaml11Booleans;
    /// <summary>
    /// Whether timestamp-like plain scalars may receive the standard timestamp tag.
    /// </summary>
    public bool AllowTimestamp;
    /// <summary>
    /// Whether the standard binary tag is accepted for base-64 content.
    /// </summary>
    public bool AllowBinary;
    /// <summary>
    /// Whether YAML merge keys are recognized and may be applied.
    /// </summary>
    public bool AllowMergeKeys;
    /// <summary>
    /// The action taken when a tag is not built in or registered.
    /// </summary>
    public YamlUnknownTagPolicy UnknownTagPolicy;
    /// <summary>
    /// Custom tag registrations checked in addition to the selected schema.
    /// </summary>
    public YamlTagRegistry CustomTags;

    /// <summary>
    /// Initializes a new YAML schema options with the YAML 1.2 core schema.
    /// </summary>
    public YamlSchemaOptions()
    {
        CustomTags = new YamlTagRegistry();
        ResetToDefaults();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void ResetToDefaults()
    {
        Schema = YamlSchemaKind.Core;
        ResolveScalars = true;
        AllowYaml11Booleans = false;
        AllowTimestamp = false;
        AllowBinary = false;
        AllowMergeKeys = false;
        UnknownTagPolicy = YamlUnknownTagPolicy.Preserve;
        CustomTags = new YamlTagRegistry();
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML schema options.</returns>
    internal YamlSchemaOptions Copy()
    {
        YamlSchemaOptions copy = new YamlSchemaOptions();
        copy.Schema = Schema;
        copy.ResolveScalars = ResolveScalars;
        copy.AllowYaml11Booleans = AllowYaml11Booleans;
        copy.AllowTimestamp = AllowTimestamp;
        copy.AllowBinary = AllowBinary;
        copy.AllowMergeKeys = AllowMergeKeys;
        copy.UnknownTagPolicy = UnknownTagPolicy;
        copy.CustomTags = CustomTags.Copy();
        return copy;
    }
}

/// <summary>
/// Controls YAML formatting, collection styles, comments, markers, and key ordering.
/// </summary>
public class YamlEmitterOptions
{
    /// <summary>
    /// The number of spaces used for each emitted indentation level.
    /// </summary>
    public int Indent;
    /// <summary>
    /// The preferred output width used when wrapping flow collections and block scalars.
    /// </summary>
    public int LineWidth;
    /// <summary>
    /// Whether to emit the document-start marker.
    /// </summary>
    public bool WriteDocumentStart;
    /// <summary>
    /// Whether to emit the document-end marker.
    /// </summary>
    public bool WriteDocumentEnd;
    /// <summary>
    /// Whether mappings with scalar string keys are emitted in ordinal key order.
    /// </summary>
    public bool SortStringKeys;
    /// <summary>
    /// Whether preserved comments and blank lines are written.
    /// </summary>
    public bool EmitComments;
    /// <summary>
    /// The fallback presentation style for scalars that do not request a specific style.
    /// </summary>
    public YamlScalarStyle DefaultScalarStyle;
    /// <summary>
    /// The fallback presentation style for collections that do not request a specific style.
    /// </summary>
    public YamlCollectionStyle DefaultCollectionStyle;
    /// <summary>
    /// The action taken when a scalar cannot be emitted using its requested YAML form.
    /// </summary>
    public YamlInvalidValuePolicy InvalidValuePolicy;

    /// <summary>
    /// Initializes a new YAML emitter options with deterministic block-style formatting defaults.
    /// </summary>
    public YamlEmitterOptions()
    {
        ResetToDefaults();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void ResetToDefaults()
    {
        Indent = 2;
        LineWidth = 80;
        WriteDocumentStart = false;
        WriteDocumentEnd = false;
        SortStringKeys = false;
        EmitComments = true;
        DefaultScalarStyle = YamlScalarStyle.Any;
        DefaultCollectionStyle = YamlCollectionStyle.Block;
        InvalidValuePolicy = YamlInvalidValuePolicy.QuoteAsString;
    }

    internal void Normalize()
    {
        if (Indent < 1)
        {
            Indent = 1;
        }

        if (Indent > 8)
        {
            Indent = 8;
        }

        if (LineWidth < 0)
        {
            LineWidth = 0;
        }

        if (InvalidValuePolicy != YamlInvalidValuePolicy.Error
            && InvalidValuePolicy != YamlInvalidValuePolicy.QuoteAsString)
        {
            InvalidValuePolicy = YamlInvalidValuePolicy.QuoteAsString;
        }
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML emitter options.</returns>
    internal YamlEmitterOptions Copy()
    {
        YamlEmitterOptions copy = new YamlEmitterOptions();
        copy.Indent = Indent;
        copy.LineWidth = LineWidth;
        copy.WriteDocumentStart = WriteDocumentStart;
        copy.WriteDocumentEnd = WriteDocumentEnd;
        copy.SortStringKeys = SortStringKeys;
        copy.EmitComments = EmitComments;
        copy.DefaultScalarStyle = DefaultScalarStyle;
        copy.DefaultCollectionStyle = DefaultCollectionStyle;
        copy.InvalidValuePolicy = InvalidValuePolicy;
        return copy;
    }
}

/// <summary>
/// Controls conversion between YAML values and ordinary objects with public fields.
/// </summary>
public class YamlObjectOptions
{
    /// <summary>
    /// The naming convention used to translate public field names to YAML keys.
    /// </summary>
    public YamlNamingConvention NamingConvention;
    /// <summary>
    /// Whether object conversion reads and writes public instance fields.
    /// </summary>
    public bool IncludePublicFields;
    /// <summary>
    /// Whether object serialization omits public fields whose value is null.
    /// </summary>
    public bool IgnoreNulls;
    /// <summary>
    /// Whether object conversion omits or ignores values equal to the field type's default value.
    /// </summary>
    public bool IgnoreDefaults;
    /// <summary>
    /// Whether deserialization fails when a YAML mapping key has no matching public field.
    /// </summary>
    public bool ErrorOnUnknownMembers;
    /// <summary>
    /// Whether every configured required member must occur during deserialization.
    /// </summary>
    public bool RequireConfiguredMembers;
    /// <summary>
    /// Whether registered type information is represented with YAML tags.
    /// </summary>
    public bool UseTypeTags;
    /// <summary>
    /// The CLR public field names that must be present during object deserialization.
    /// </summary>
    public List<string> RequiredMembers;
    /// <summary>
    /// Custom converters for tagged scalar values.
    /// </summary>
    public YamlScalarConverterRegistry ScalarConverters;
    /// <summary>
    /// Custom converters that transform complete CLR values.
    /// </summary>
    public YamlTypeConverterRegistry TypeConverters;

    /// <summary>
    /// Initializes a new YAML object options with public-field conversion enabled.
    /// </summary>
    public YamlObjectOptions()
    {
        RequiredMembers = new List<string>();
        ScalarConverters = new YamlScalarConverterRegistry();
        TypeConverters = new YamlTypeConverterRegistry();
        ResetToDefaults();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void ResetToDefaults()
    {
        NamingConvention = YamlNamingConvention.Exact;
        IncludePublicFields = true;
        IgnoreNulls = false;
        IgnoreDefaults = false;
        ErrorOnUnknownMembers = true;
        RequireConfiguredMembers = false;
        UseTypeTags = false;
        RequiredMembers = new List<string>();
        ScalarConverters = new YamlScalarConverterRegistry();
        TypeConverters = new YamlTypeConverterRegistry();
    }

    /// <summary>
    /// Appends the supplied value while preserving the existing order.
    /// </summary>
    /// <param name="fieldName">The CLR public field name, before naming-convention conversion.</param>
    internal void AddRequiredMember(string? fieldName)
    {
        string text = fieldName == null ? "" : fieldName;
        if (text == "")
        {
            return;
        }

        if (!HasRequiredMember(text))
        {
            RequiredMembers.Add(text);
        }
    }

    /// <summary>
    /// Determines whether a CLR public field name is configured as required.
    /// </summary>
    /// <param name="fieldName">The CLR public field name, before naming-convention conversion.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool HasRequiredMember(string? fieldName)
    {
        string text = fieldName == null ? "" : fieldName;
        int index = 0;
        while (index < RequiredMembers.Count)
        {
            if (RequiredMembers[index] == text)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Returns the number of required member entries.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int RequiredMemberCount()
    {
        return RequiredMembers.Count;
    }

    /// <summary>
    /// Removes all stored entries and returns this instance to an empty state.
    /// </summary>
    internal void ClearRequiredMembers()
    {
        RequiredMembers.Clear();
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML object options.</returns>
    internal YamlObjectOptions Copy()
    {
        YamlObjectOptions copy = new YamlObjectOptions();
        copy.NamingConvention = NamingConvention;
        copy.IncludePublicFields = IncludePublicFields;
        copy.IgnoreNulls = IgnoreNulls;
        copy.IgnoreDefaults = IgnoreDefaults;
        copy.ErrorOnUnknownMembers = ErrorOnUnknownMembers;
        copy.RequireConfiguredMembers = RequireConfiguredMembers;
        copy.UseTypeTags = UseTypeTags;
        int index = 0;
        while (index < RequiredMembers.Count)
        {
            copy.RequiredMembers.Add(RequiredMembers[index]);
            index = index + 1;
        }

        copy.ScalarConverters = ScalarConverters.Copy();
        copy.TypeConverters = TypeConverters.Copy();
        return copy;
    }
}

/// <summary>
/// Sets resource limits used while processing potentially untrusted YAML.
/// </summary>
public class YamlSecurityOptions
{
    /// <summary>
    /// Whether configured security limits are checked.
    /// </summary>
    public bool EnforceLimits;
    /// <summary>
    /// The maximum accepted input length in UTF-16 characters.
    /// </summary>
    public int MaxInputLength;
    /// <summary>
    /// The maximum accepted scalar length in UTF-16 characters.
    /// </summary>
    public int MaxScalarLength;
    /// <summary>
    /// The maximum collection nesting depth.
    /// </summary>
    public int MaxDepth;
    /// <summary>
    /// The maximum number of alias nodes accepted in a composed graph.
    /// </summary>
    public int MaxAliasCount;
    /// <summary>
    /// The maximum number of items or pairs accepted in one collection.
    /// </summary>
    public int MaxCollectionSize;
    /// <summary>
    /// The maximum number of diagnostics collected; a non-positive value means no separate limit.
    /// </summary>
    public int MaxDiagnostics;

    /// <summary>
    /// Initializes a new YAML security options with resource limits enabled.
    /// </summary>
    public YamlSecurityOptions()
    {
        ResetToDefaults();
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    internal void ResetToDefaults()
    {
        EnforceLimits = true;
        MaxInputLength = 16777216;
        MaxScalarLength = 1048576;
        MaxDepth = 100;
        MaxAliasCount = 100;
        MaxCollectionSize = 100000;
        MaxDiagnostics = 1000;
    }

    /// <summary>
    /// Determines whether the supplied value is within the configured limit.
    /// </summary>
    /// <param name="length">The length used by the operation.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool AllowsInputLength(int length)
    {
        return AllowsLimit(length, MaxInputLength);
    }

    /// <summary>
    /// Determines whether the supplied value is within the configured limit.
    /// </summary>
    /// <param name="length">The length used by the operation.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool AllowsScalarLength(int length)
    {
        return AllowsLimit(length, MaxScalarLength);
    }

    /// <summary>
    /// Determines whether the supplied value is within the configured limit.
    /// </summary>
    /// <param name="depth">The depth used by the operation.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool AllowsDepth(int depth)
    {
        return AllowsLimit(depth, MaxDepth);
    }

    /// <summary>
    /// Determines whether the supplied value is within the configured limit.
    /// </summary>
    /// <param name="count">The number of characters or items to process.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool AllowsAliasCount(int count)
    {
        return AllowsLimit(count, MaxAliasCount);
    }

    /// <summary>
    /// Determines whether the supplied value is within the configured limit.
    /// </summary>
    /// <param name="count">The number of characters or items to process.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool AllowsCollectionSize(int count)
    {
        return AllowsLimit(count, MaxCollectionSize);
    }

    /// <summary>
    /// Determines whether the supplied value is within the configured limit.
    /// </summary>
    /// <param name="count">The number of characters or items to process.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool AllowsDiagnosticCount(int count)
    {
        return AllowsLimit(count, MaxDiagnostics);
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML security options.</returns>
    internal YamlSecurityOptions Copy()
    {
        YamlSecurityOptions copy = new YamlSecurityOptions();
        copy.EnforceLimits = EnforceLimits;
        copy.MaxInputLength = MaxInputLength;
        copy.MaxScalarLength = MaxScalarLength;
        copy.MaxDepth = MaxDepth;
        copy.MaxAliasCount = MaxAliasCount;
        copy.MaxCollectionSize = MaxCollectionSize;
        copy.MaxDiagnostics = MaxDiagnostics;
        return copy;
    }

    internal bool AllowsLimit(int value, int limit)
    {
        if (!EnforceLimits)
        {
            return true;
        }

        if (limit < 0)
        {
            return true;
        }

        return value <= limit;
    }
}
