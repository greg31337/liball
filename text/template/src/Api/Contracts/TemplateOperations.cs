using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Creates template values and converts CLR values for rendering.
/// </summary>
public interface ITemplateValueFactory
{
    /// <summary>Configures culture and custom conversion behavior.</summary>
    void Configure(TemplateValueConversionPolicy policy);

    /// <summary>Creates a missing value.</summary>
    TemplateValue CreateMissing();

    /// <summary>Creates a null value.</summary>
    TemplateValue CreateNull();

    /// <summary>Creates a Boolean value.</summary>
    TemplateValue CreateBoolean(bool value);

    /// <summary>Creates a numeric value.</summary>
    TemplateValue CreateNumber(decimal value);

    /// <summary>Creates a string value.</summary>
    TemplateValue CreateString(string value);

    /// <summary>Creates a string value that bypasses output escaping.</summary>
    TemplateValue CreateSafeString(string value);

    /// <summary>Creates a list value.</summary>
    TemplateValue CreateList(List<TemplateValue> values);

    /// <summary>Creates a map value.</summary>
    TemplateValue CreateMap(Dictionary<string, TemplateValue> values);

    /// <summary>Creates an object-backed value.</summary>
    TemplateValue CreateObject(object value);

    /// <summary>Creates a callable value.</summary>
    TemplateValue CreateFunction(Func<List<TemplateValue>, TemplateValue> value);

    /// <summary>Creates a template fragment value.</summary>
    TemplateValue CreateFragment(List<TemplateSyntaxNode> nodes);

    /// <summary>Converts a CLR value.</summary>
    TemplateValue Convert(object? value);

    /// <summary>Converts a non-generic enumerable.</summary>
    TemplateValue ConvertEnumerable(IEnumerable values);

    /// <summary>Converts a non-generic dictionary.</summary>
    TemplateValue ConvertDictionary(IDictionary values);
}

/// <summary>
/// Creates built-in syntax profiles and their matching options.
/// </summary>
public interface ITemplateProfileCatalog
{
    /// <summary>Creates the native profile.</summary>
    TemplateProfile CreateNativeProfile();

    /// <summary>Creates native options.</summary>
    TemplateOptions CreateNativeOptions();

    /// <summary>Creates the Mustache profile.</summary>
    TemplateProfile CreateMustacheProfile();

    /// <summary>Creates Mustache options.</summary>
    TemplateOptions CreateMustacheOptions();

    /// <summary>Creates the Handlebars profile.</summary>
    TemplateProfile CreateHandlebarsProfile();

    /// <summary>Creates Handlebars options.</summary>
    TemplateOptions CreateHandlebarsOptions();

    /// <summary>Creates the Liquid profile.</summary>
    TemplateProfile CreateLiquidProfile();

    /// <summary>Creates Liquid options.</summary>
    TemplateOptions CreateLiquidOptions();

    /// <summary>Creates the Jinja profile.</summary>
    TemplateProfile CreateJinjaProfile();

    /// <summary>Creates Jinja options.</summary>
    TemplateOptions CreateJinjaOptions();

    /// <summary>Creates the Django profile.</summary>
    TemplateProfile CreateDjangoProfile();

    /// <summary>Creates Django options.</summary>
    TemplateOptions CreateDjangoOptions();
}

/// <summary>
/// Registers built-in and custom syntax profiles for parsing and rendering.
/// </summary>
public interface ITemplateProfileRegistry
{
    /// <summary>Registers or replaces a custom profile.</summary>
    void Register(TemplateProfile profile);

    /// <summary>Removes a custom profile.</summary>
    bool Remove(string name);

    /// <summary>Reports whether a profile is registered.</summary>
    bool Contains(string name);

    /// <summary>Gets a defensive copy of a registered profile.</summary>
    TemplateProfile GetProfile(string name);

    /// <summary>Creates options for a registered profile and compatibility mode.</summary>
    TemplateOptions CreateOptions(string name, TemplateCompatibilityMode compatibilityMode);

    /// <summary>Gets all registered profile names.</summary>
    List<string> GetProfileNames();

    /// <summary>Parses text using a registered profile.</summary>
    TemplateParseResult Parse(string templateText, string profileName);
}

/// <summary>
/// Formats diagnostics and source locations for display.
/// </summary>
public interface ITemplateDiagnosticFormatter
{
    /// <summary>Formats a diagnostic for display.</summary>
    string Format(TemplateDiagnostic diagnostic);

    /// <summary>Gets the one-based line number at an offset.</summary>
    int GetLineNumber(string text, int offset);

    /// <summary>Gets the one-based column number at an offset.</summary>
    int GetColumnNumber(string text, int offset);

    /// <summary>Gets the complete source line at an offset.</summary>
    string GetLineText(string text, int offset);
}

/// <summary>
/// Analyzes compatibility between template syntax profiles.
/// </summary>
public interface ITemplateCompatibilityAnalyzer
{
    /// <summary>Sets the syntax-profile registry used by analysis.</summary>
    void SetProfileRegistry(TemplateProfileRegistry profileRegistry);

    /// <summary>Analyzes text converted from one profile to another.</summary>
    TemplateCompatibilityAnalysis Analyze(string templateText, string sourceProfileName, string targetProfileName);

    /// <summary>Analyzes a native syntax document for a target profile.</summary>
    TemplateCompatibilityAnalysis AnalyzeNative(TemplateDocumentSyntax document, string targetProfileName);
}

/// <summary>
/// Caches parsed templates by source and option identity.
/// </summary>
public interface ITemplateCompilationCache
{
    /// <summary>Sets the syntax-profile registry used when compiling templates.</summary>
    void SetProfileRegistry(TemplateProfileRegistry profileRegistry);

    /// <summary>Gets or compiles a source document.</summary>
    TemplateCompiledTemplate GetOrCompile(TemplateSource source, TemplateOptions options);

    /// <summary>Sets the maximum number of cached entries.</summary>
    void SetCapacity(int capacity);

    /// <summary>Returns the maximum number of cached entries.</summary>
    int GetCapacity();

    /// <summary>Invalidates entries for a source identity.</summary>
    int InvalidateSource(TemplateSource source);

    /// <summary>Invalidates entries with a source version.</summary>
    int InvalidateVersion(string version);

    /// <summary>Invalidates entries with a content hash.</summary>
    int InvalidateContentHash(string contentHash);

    /// <summary>Invalidates entries with a source timestamp.</summary>
    int InvalidateTimestamp(string timestamp);

    /// <summary>Clears cached entries and counters.</summary>
    void Clear();

    /// <summary>Returns the number of cached entries.</summary>
    int Count();

    /// <summary>Creates the cache key for a source and its options.</summary>
    string CreateKey(TemplateSource source, TemplateOptions options);

    /// <summary>Returns the number of cache hits.</summary>
    int GetHitCount();

    /// <summary>Returns the number of cache misses.</summary>
    int GetMissCount();

    /// <summary>Returns the number of capacity evictions.</summary>
    int GetEvictionCount();

    /// <summary>Returns the number of explicitly invalidated entries.</summary>
    int GetInvalidationCount();
}

/// <summary>
/// Builds template dependency graphs.
/// </summary>
public interface ITemplateDependencyGraphBuilder
{
    /// <summary>Sets the syntax-profile registry used when parsing dependencies.</summary>
    void SetProfileRegistry(TemplateProfileRegistry profileRegistry);

    /// <summary>Configures the builder.</summary>
    void Configure(TemplateOptions options, TemplateRegistry registry);

    /// <summary>Builds a graph from template text.</summary>
    TemplateDependencyGraph Build(string templateText);

    /// <summary>Builds a graph from a parsed document.</summary>
    TemplateDependencyGraph BuildDocument(TemplateDocumentSyntax document);
}

/// <summary>
/// Produces readable syntax tree dumps.
/// </summary>
public interface ITemplateSyntaxDumper
{
    /// <summary>Dumps syntax parsed from template text.</summary>
    string Dump(string templateText);

    /// <summary>Dumps a parsed document.</summary>
    string DumpDocument(TemplateDocumentSyntax document);

    /// <summary>Dumps a syntax node.</summary>
    string DumpNode(TemplateSyntaxNode node);

    /// <summary>Dumps an expression node.</summary>
    string DumpExpression(TemplateExpressionSyntax expression);
}

/// <summary>
/// Normalizes template whitespace and newline styles.
/// </summary>
public interface ITemplateWhitespaceNormalizer
{
    /// <summary>Normalizes text with the selected whitespace mode.</summary>
    string Normalize(string text, TemplateWhitespaceMode mode);

    /// <summary>Normalizes text with template options.</summary>
    string NormalizeWithOptions(string text, TemplateOptions options);

    /// <summary>Converts newlines to line feeds.</summary>
    string ConvertToLineFeeds(string text);

    /// <summary>Converts newlines to carriage-return line-feed pairs.</summary>
    string ConvertToCarriageReturnLineFeeds(string text);

    /// <summary>Trims trailing whitespace from source lines.</summary>
    string TrimTrailingLineWhitespace(string text);

    /// <summary>Minimizes non-significant whitespace.</summary>
    string MinimizeWhitespace(string text);
}

/// <summary>
/// Formats template source and parsed documents.
/// </summary>
public interface ITemplateFormatter
{
    /// <summary>Configures the formatter.</summary>
    void Configure(TemplateFormatterOptions options);

    /// <summary>Formats template text.</summary>
    string Format(string templateText);

    /// <summary>Formats a parsed document.</summary>
    string FormatDocument(TemplateDocumentSyntax document);
}

/// <summary>
/// Exposes file-loader-specific operations.
/// </summary>
public interface IFileTemplateLoader
{
    /// <summary>Configures decoding and maximum template length.</summary>
    void Configure(TemplateLoaderOptions options);

    /// <summary>Sets the policy that restricts loader roots and resolved paths.</summary>
    void SetSecurityPolicy(TemplateSecurityPolicy securityPolicy);

    /// <summary>Gets the normalized loader root.</summary>
    string GetRootPath();

    /// <summary>Reports whether a named template exists.</summary>
    bool ContainsTemplate(string name);

    /// <summary>Loads a template asynchronously without a cancellation token.</summary>
    Task<TemplateSource?> LoadWithoutCancellationAsync(string name);

    /// <summary>Loads a required template.</summary>
    TemplateSource LoadRequired(string name);

    /// <summary>Loads a required template asynchronously.</summary>
    Task<TemplateSource> LoadRequiredAsync(string name);

    /// <summary>Loads a required template with cancellation.</summary>
    Task<TemplateSource> LoadRequiredWithCancellationAsync(string name, CancellationToken cancellationToken);

    /// <summary>Gets the normalized path for a template name.</summary>
    string GetTemplatePath(string name);

    /// <summary>Gets the available template names.</summary>
    List<string> GetTemplateNames();
}

/// <summary>
/// Exposes memory-loader-specific operations.
/// </summary>
public interface IMemoryTemplateLoader
{
    /// <summary>Configures maximum template length.</summary>
    void Configure(TemplateLoaderOptions options);

    /// <summary>Adds a template.</summary>
    void AddTemplate(string name, string text);

    /// <summary>Adds a template source.</summary>
    void AddTemplateSource(string name, TemplateSource source);

    /// <summary>Sets a template.</summary>
    void SetTemplate(string name, string text);

    /// <summary>Sets a template source.</summary>
    void SetTemplateSource(string name, TemplateSource source);

    /// <summary>Reports whether a named template exists.</summary>
    bool ContainsTemplate(string name);

    /// <summary>Loads a template asynchronously without a cancellation token.</summary>
    Task<TemplateSource?> LoadWithoutCancellationAsync(string name);

    /// <summary>Loads a required template.</summary>
    TemplateSource LoadRequired(string name);

    /// <summary>Loads a required template asynchronously.</summary>
    Task<TemplateSource> LoadRequiredAsync(string name);

    /// <summary>Loads a required template with cancellation.</summary>
    Task<TemplateSource> LoadRequiredWithCancellationAsync(string name, CancellationToken cancellationToken);

    /// <summary>Removes a template.</summary>
    bool RemoveTemplate(string name);

    /// <summary>Clears all templates.</summary>
    void Clear();

    /// <summary>Gets the available template names.</summary>
    List<string> GetTemplateNames();
}

/// <summary>
/// Exposes assembly-manifest-resource loader operations.
/// </summary>
public interface IEmbeddedResourceTemplateLoader
{
    /// <summary>Configures decoding, maximum template length, and the resource prefix.</summary>
    void Configure(TemplateLoaderOptions options);

    /// <summary>Reports whether a named template resource exists.</summary>
    bool ContainsTemplate(string name);

    /// <summary>Loads a template asynchronously without a cancellation token.</summary>
    Task<TemplateSource?> LoadWithoutCancellationAsync(string name);

    /// <summary>Loads a required template.</summary>
    TemplateSource LoadRequired(string name);

    /// <summary>Loads a required template asynchronously.</summary>
    Task<TemplateSource> LoadRequiredAsync(string name);

    /// <summary>Loads a required template with cancellation.</summary>
    Task<TemplateSource> LoadRequiredWithCancellationAsync(string name, CancellationToken cancellationToken);

    /// <summary>Gets the available logical template names.</summary>
    List<string> GetTemplateNames();
}

/// <summary>
/// Tokenizes a template source.
/// </summary>
public interface ITemplateLexer
{
    /// <summary>Sets the sink that receives lexer diagnostics.</summary>
    void SetDiagnosticSink(ITemplateDiagnosticSink diagnosticSink);

    /// <summary>Configures the syntax profile.</summary>
    void Configure(TemplateProfile profile);

    /// <summary>Lexes the configured source.</summary>
    TemplateLexerResult Lex();
}

/// <summary>
/// Parses a tokenized template source.
/// </summary>
public interface ITemplateParser
{
    /// <summary>Sets the sink that receives lexer and parser diagnostics.</summary>
    void SetDiagnosticSink(ITemplateDiagnosticSink diagnosticSink);

    /// <summary>Parses the configured token stream.</summary>
    TemplateParseResult Parse();
}

/// <summary>
/// Registers template functions, helpers, partials, and loaders.
/// </summary>
public interface ITemplateRegistry
{
    /// <summary>Registers a function.</summary>
    void RegisterFunction(string name, Func<List<TemplateValue>, TemplateValue> function);

    /// <summary>Registers a filter.</summary>
    void RegisterFilter(string name, Func<List<TemplateValue>, TemplateValue> filter);

    /// <summary>Registers a test.</summary>
    void RegisterTest(string name, Func<List<TemplateValue>, TemplateValue> test);

    /// <summary>Registers a helper.</summary>
    void RegisterHelper(string name, Func<List<TemplateValue>, TemplateValue> helper);

    /// <summary>Registers an asynchronous function.</summary>
    void RegisterAsyncFunction(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> function);

    /// <summary>Registers an asynchronous filter.</summary>
    void RegisterAsyncFilter(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> filter);

    /// <summary>Registers an asynchronous test.</summary>
    void RegisterAsyncTest(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> test);

    /// <summary>Registers an asynchronous helper.</summary>
    void RegisterAsyncHelper(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> helper);

    /// <summary>Registers a block helper.</summary>
    void RegisterBlockHelper(string name, Func<TemplateBlockHelperContext, TemplateValue> blockHelper);

    /// <summary>Registers a partial.</summary>
    void RegisterPartial(string name, string text);

    /// <summary>Registers a partial source.</summary>
    void RegisterPartialSource(string name, TemplateSource source);

    /// <summary>Reports whether a partial exists.</summary>
    bool ContainsPartial(string name);

    /// <summary>Loads a partial.</summary>
    TemplateSource? LoadPartial(string name);

    /// <summary>Loads a partial asynchronously.</summary>
    Task<TemplateSource?> LoadPartialAsync(string name);

    /// <summary>Loads a partial with cancellation.</summary>
    Task<TemplateSource?> LoadPartialWithCancellationAsync(string name, CancellationToken cancellationToken);

    /// <summary>Removes a partial.</summary>
    bool RemovePartial(string name);

    /// <summary>Gets registered partial names.</summary>
    List<string> GetPartialNames();

    /// <summary>Sets the memory loader.</summary>
    void SetMemoryLoader(MemoryTemplateLoader loader);

    /// <summary>Sets the file loader.</summary>
    void SetFileLoader(FileTemplateLoader loader);

    /// <summary>Adds a template loader.</summary>
    void AddTemplateLoader(ITemplateLoader loader);

    /// <summary>Registers a named template.</summary>
    void RegisterTemplate(string name, string text);

    /// <summary>Loads a named template.</summary>
    TemplateSource? LoadTemplate(string name);

    /// <summary>Loads a named template asynchronously.</summary>
    Task<TemplateSource?> LoadTemplateAsync(string name);

    /// <summary>Loads a named template with cancellation.</summary>
    Task<TemplateSource?> LoadTemplateWithCancellationAsync(string name, CancellationToken cancellationToken);
}

/// <summary>
/// Renders parsed or textual templates.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>Sets the syntax-profile registry used by rendering and composition.</summary>
    void SetProfileRegistry(TemplateProfileRegistry profileRegistry);

    /// <summary>Configures rendering services.</summary>
    void Configure(TemplateOptions options, TemplateRegistry registry, ITemplateMemberResolver memberResolver);

    /// <summary>Sets the compilation cache.</summary>
    void SetCompilationCache(ITemplateCompilationCache compilationCache);

    /// <summary>Sets the sink that receives parse, evaluation, composition, and security diagnostics.</summary>
    void SetDiagnosticSink(ITemplateDiagnosticSink diagnosticSink);

    /// <summary>Sets the collector that receives mappings for completed renders.</summary>
    void SetSourceMapCollector(ITemplateSourceMapCollector sourceMapCollector);

    /// <summary>Parses template text for application startup without storing it in the compilation cache.</summary>
    TemplateParseResult Preparse(string templateText);

    /// <summary>Parses a source for application startup without storing it in the compilation cache.</summary>
    TemplateParseResult PreparseSource(TemplateSource source);

    /// <summary>Compiles and caches template text for application startup.</summary>
    TemplateCompiledTemplate Precompile(string templateText);

    /// <summary>Compiles and caches a source for application startup.</summary>
    TemplateCompiledTemplate PrecompileSource(TemplateSource source);

    /// <summary>Renders text to a string.</summary>
    string RenderToString(string templateText, TemplateValue model);

    /// <summary>Renders text to a string asynchronously.</summary>
    Task<string> RenderToStringAsync(string templateText, TemplateValue model);

    /// <summary>Renders text to a string with cancellation.</summary>
    Task<string> RenderToStringWithCancellationAsync(string templateText, TemplateValue model, CancellationToken cancellationToken);

    /// <summary>Renders a syntax document to a string.</summary>
    string RenderSyntaxToString(TemplateDocumentSyntax document, TemplateValue model);

    /// <summary>Renders a document to a string asynchronously.</summary>
    Task<string> RenderDocumentToStringAsync(TemplateDocumentSyntax document, TemplateValue model);

    /// <summary>Renders a document to a string with cancellation.</summary>
    Task<string> RenderDocumentToStringWithCancellationAsync(TemplateDocumentSyntax document, TemplateValue model, CancellationToken cancellationToken);

    /// <summary>Renders text to a writer.</summary>
    void RenderToWriter(string templateText, TemplateValue model, TextWriter writer);

    /// <summary>Renders text to a writer asynchronously.</summary>
    Task RenderToWriterAsync(string templateText, TemplateValue model, TextWriter writer);

    /// <summary>Renders text to a writer with cancellation.</summary>
    Task RenderToWriterWithCancellationAsync(string templateText, TemplateValue model, TextWriter writer, CancellationToken cancellationToken);

    /// <summary>Renders Mustache text to a string.</summary>
    string RenderMustacheToString(string templateText, TemplateValue model);

    /// <summary>Renders Mustache text to a writer.</summary>
    void RenderMustacheToWriter(string templateText, TemplateValue model, TextWriter writer);

    /// <summary>Renders a document to a writer.</summary>
    void RenderDocumentToWriter(TemplateDocumentSyntax document, TemplateValue model, TextWriter writer);

    /// <summary>Renders a document to a writer asynchronously.</summary>
    Task RenderDocumentToWriterAsync(TemplateDocumentSyntax document, TemplateValue model, TextWriter writer);

    /// <summary>Renders a document to a writer with cancellation.</summary>
    Task RenderDocumentToWriterWithCancellationAsync(TemplateDocumentSyntax document, TemplateValue model, TextWriter writer, CancellationToken cancellationToken);

    /// <summary>Renders with an existing context.</summary>
    void Render(TemplateDocumentSyntax document, TemplateRenderContext context);

    /// <summary>Renders incrementally with an existing context.</summary>
    void RenderStreaming(TemplateDocumentSyntax document, TemplateRenderContext context);

    /// <summary>Gets dependencies from the most recent render.</summary>
    List<TemplateDependency> GetLastDependencies();

    /// <summary>Gets source mappings from the most recent completed render.</summary>
    List<TemplateSourceMapEntry> GetLastSourceMap();
}

/// <summary>
/// Performs static template linting and returns diagnostics without rendering.
/// </summary>
public interface ITemplateLinter
{
    /// <summary>Configures lint options and registered runtime components.</summary>
    void Configure(TemplateOptions options, TemplateRegistry registry);

    /// <summary>Sets the syntax-profile registry used for parsing.</summary>
    void SetProfileRegistry(TemplateProfileRegistry profileRegistry);

    /// <summary>Sets the sink that receives lint diagnostics.</summary>
    void SetDiagnosticSink(ITemplateDiagnosticSink diagnosticSink);

    /// <summary>Lints template text using the configured profile.</summary>
    List<TemplateDiagnostic> Lint(string templateText);

    /// <summary>Lints an already parsed document.</summary>
    List<TemplateDiagnostic> LintDocument(TemplateDocumentSyntax document);
}

/// <summary>
/// Audits templates and configuration for security risks.
/// </summary>
public interface ITemplateSecurityAuditor
{
    /// <summary>Audits a document and its options.</summary>
    List<TemplateDiagnostic> Audit(TemplateDocumentSyntax document, TemplateOptions options);

    /// <summary>Audits a document with member resolver options.</summary>
    List<TemplateDiagnostic> AuditWithMemberResolverOptions(TemplateDocumentSyntax document, TemplateOptions options, TemplateMemberResolverOptions memberResolverOptions);

    /// <summary>Audits template options.</summary>
    List<TemplateDiagnostic> AuditOptions(TemplateOptions options);

    /// <summary>Audits a security policy.</summary>
    List<TemplateDiagnostic> AuditSecurityPolicy(TemplateSecurityPolicy policy);

    /// <summary>Audits member resolver options.</summary>
    List<TemplateDiagnostic> AuditMemberResolverOptions(TemplateMemberResolverOptions options);
}

/// <summary>
/// Maps source offsets to lines and columns.
/// </summary>
public interface ITemplateSourceLineMap
{
    /// <summary>Gets the number of source lines.</summary>
    int GetLineCount();

    /// <summary>Gets the position at an offset.</summary>
    TemplateSourcePosition GetPosition(int offset);

    /// <summary>Gets a source line by number.</summary>
    TemplateSourceLine GetLine(int lineNumber);

    /// <summary>Gets source text for a line.</summary>
    string GetLineText(int lineNumber);

    /// <summary>Gets the offset at a line and column.</summary>
    int GetOffset(int lineNumber, int columnNumber);

    /// <summary>Reports whether an offset begins a line.</summary>
    bool IsOffsetAtLineStart(int offset);

    /// <summary>Reports whether an offset ends a line.</summary>
    bool IsOffsetAtLineEnd(int offset);
}

/// <summary>
/// Writes template output while enforcing output limits.
/// </summary>
public interface ITemplateWriter
{
    /// <summary>Configures the output security policy.</summary>
    void Configure(TemplateSecurityPolicy securityPolicy);

    /// <summary>Gets the remaining permitted output length.</summary>
    int GetRemainingLength();

    /// <summary>Reports whether a length can be written.</summary>
    bool CanWrite(int length);

    /// <summary>Writes text.</summary>
    void Write(string text);

    /// <summary>Writes a character.</summary>
    void WriteCharacter(char value);

    /// <summary>Writes a template value.</summary>
    void WriteValue(TemplateValue value);

    /// <summary>Writes a newline.</summary>
    void WriteLine();

    /// <summary>Writes text followed by a newline.</summary>
    void WriteTextLine(string text);

    /// <summary>Flushes buffered output.</summary>
    void Flush();
}
