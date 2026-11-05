using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Parses templates and renders them to strings or text writers using configurable syntax, escaping, limits, and registered components.
/// </summary>
/// <remarks>
/// String overloads parse native syntax unless the options select a compatibility profile. Writer overloads enforce the same quotas and escaping rules as string rendering.
/// </remarks>
public class TemplateRenderer : ITemplateRenderer
{
    /// <summary>
    /// Specifies the options for this renderer.
    /// </summary>
    internal TemplateOptions Options;
    /// <summary>
    /// Specifies the registry for this renderer.
    /// </summary>
    internal TemplateRegistry Registry;
    /// <summary>
    /// Specifies the member resolver used for render contexts created by this renderer.
    /// </summary>
    internal ITemplateMemberResolver MemberResolver;
    internal List<TemplateDependency> LastDependencies;
    /// <summary>
    /// Specifies the compilation cache for this renderer.
    /// </summary>
    internal ITemplateCompilationCache CompilationCache;
    internal TemplateProfileRegistry ProfileRegistry;
    internal ITemplateDiagnosticSink? DiagnosticSink;
    internal ITemplateSourceMapCollector? SourceMapCollector;
    internal List<TemplateSourceMapEntry> LastSourceMap;
    internal object RenderStateLock;

    /// <summary>
    /// Initializes a new template renderer and validates any supplied values.
    /// </summary>
    public TemplateRenderer()
    {
        Options = new TemplateOptions();
        Registry = new TemplateRegistry();
        MemberResolver = new TemplateMemberResolver();
        LastDependencies = new List<TemplateDependency>();
        ProfileRegistry = new TemplateProfileRegistry();
        CompilationCache = new TemplateCompilationCache();
        CompilationCache.SetProfileRegistry(ProfileRegistry);
        DiagnosticSink = null;
        SourceMapCollector = null;
        LastSourceMap = new List<TemplateSourceMapEntry>();
        RenderStateLock = new object();
    }

    /// <summary>
    /// Sets the syntax-profile registry used for top-level templates and composed sources.
    /// </summary>
    /// <param name="profileRegistry">The profile registry to use.</param>
    public void SetProfileRegistry(TemplateProfileRegistry profileRegistry)
    {
        if (profileRegistry == null)
        {
            throw new ArgumentNullException("profileRegistry");
        }

        ProfileRegistry = profileRegistry;
        CompilationCache.SetProfileRegistry(profileRegistry);
    }

    /// <summary>
    /// Configures the options, registry, and member resolver used for subsequent renders.
    /// </summary>
    /// <param name="options">The options that control rendering.</param>
    /// <param name="registry">The registry that supplies callables and named templates.</param>
    /// <param name="memberResolver">The service used to resolve member and index expressions.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void Configure(TemplateOptions options, TemplateRegistry registry, ITemplateMemberResolver memberResolver)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        if (memberResolver == null)
        {
            throw new ArgumentNullException("memberResolver");
        }

        options.Validate();
        Options = options.Clone();
        Registry = registry;
        MemberResolver = memberResolver;
    }

    /// <summary>
    /// Sets the compilation cache used for subsequent renders.
    /// </summary>
    /// <param name="compilationCache">The compilation cache to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>compilationCache</c> is <c>null</c>.</exception>
    public void SetCompilationCache(ITemplateCompilationCache compilationCache)
    {
        if (compilationCache == null)
        {
            throw new ArgumentNullException("compilationCache");
        }

        CompilationCache = compilationCache;
        CompilationCache.SetProfileRegistry(ProfileRegistry);
    }

    /// <summary>
    /// Sets the sink that receives diagnostics discovered while parsing and rendering.
    /// </summary>
    /// <param name="diagnosticSink">The sink that receives diagnostics.</param>
    public void SetDiagnosticSink(ITemplateDiagnosticSink diagnosticSink)
    {
        if (diagnosticSink == null)
        {
            throw new ArgumentNullException("diagnosticSink");
        }

        DiagnosticSink = diagnosticSink;
    }

    /// <summary>
    /// Sets the collector that receives mappings after each completed render.
    /// </summary>
    /// <param name="sourceMapCollector">The collector that receives source mappings.</param>
    public void SetSourceMapCollector(ITemplateSourceMapCollector sourceMapCollector)
    {
        if (sourceMapCollector == null)
        {
            throw new ArgumentNullException("sourceMapCollector");
        }

        SourceMapCollector = sourceMapCollector;
    }

    /// <summary>
    /// Parses template text using the renderer's active syntax profile without storing the result in the compilation cache.
    /// </summary>
    /// <param name="templateText">The template source text to parse.</param>
    /// <returns>The parsed document and diagnostics.</returns>
    public TemplateParseResult Preparse(string templateText)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        TemplateSource source = CreateSourceForOptions(templateText, Options);
        TemplateParseResult result = ProfileRegistry.ParseSource(source, Options.SyntaxProfile);
        ReportDiagnostics(result.Diagnostics);
        return result;
    }

    /// <summary>
    /// Parses a source using the renderer's active syntax profile without storing the result in the compilation cache.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <returns>The parsed document and diagnostics.</returns>
    public TemplateParseResult PreparseSource(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        ValidateTemplateSource(source, Options);
        TemplateParseResult result = ProfileRegistry.ParseSource(source, Options.SyntaxProfile);
        ReportDiagnostics(result.Diagnostics);
        return result;
    }

    /// <summary>
    /// Compiles and caches template text for later rendering.
    /// </summary>
    /// <param name="templateText">The template source text to compile.</param>
    /// <returns>The reusable compiled template.</returns>
    public TemplateCompiledTemplate Precompile(string templateText)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        return GetOrCompileWithDiagnostics(CreateSourceForOptions(templateText, Options), Options);
    }

    /// <summary>
    /// Compiles and caches a source with its supplied identity metadata for later rendering.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <returns>The reusable compiled template.</returns>
    public TemplateCompiledTemplate PrecompileSource(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        return GetOrCompileWithDiagnostics(source, Options);
    }

    /// <summary>
    /// Creates a renderer that uses the supplied options.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The configured renderer.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateRenderer Create(TemplateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Options = options;
        return renderer;
    }

    /// <summary>
    /// Creates a renderer that uses the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies callables and named templates.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The configured renderer.</returns>
    internal static TemplateRenderer Create(TemplateRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Registry = registry;
        return renderer;
    }

    /// <summary>
    /// Creates a renderer that uses the supplied options and registry.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="registry">The registry that supplies callables and named templates.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The configured renderer.</returns>
    internal static TemplateRenderer Create(TemplateOptions options, TemplateRegistry registry)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Options = options;
        renderer.Registry = registry;
        return renderer;
    }

    /// <summary>
    /// Creates a renderer with explicitly supplied configuration and member resolution services.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="registry">The registry that supplies callables and named templates.</param>
    /// <param name="memberResolver">The service used to resolve member and index expressions.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <returns>The configured renderer.</returns>
    internal static TemplateRenderer Create(TemplateOptions options, TemplateRegistry registry, ITemplateMemberResolver memberResolver)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (registry == null)
        {
            throw new ArgumentNullException("registry");
        }

        if (memberResolver == null)
        {
            throw new ArgumentNullException("memberResolver");
        }

        TemplateRenderer renderer = new TemplateRenderer();
        renderer.Options = options;
        renderer.Registry = registry;
        renderer.MemberResolver = memberResolver;
        return renderer;
    }

    /// <summary>
    /// Parses and renders template text and returns buffered output.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string RenderToString(string templateText, TemplateValue model)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (Options.SyntaxProfile == "mustache")
        {
            return RenderMustacheToString(templateText, model);
        }

        TemplateCompiledTemplate compiled = GetOrCompileWithDiagnostics(CreateSourceForOptions(templateText, Options), Options);
        return RenderSyntaxToString(compiled.Document, model);
    }

    /// <summary>
    /// Parses and renders template text asynchronously and completes with buffered output.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<string> RenderToStringAsync(string templateText, TemplateValue model)
    {
        return RenderToStringWithCancellationAsync(templateText, model, CancellationToken.None);
    }

    /// <summary>
    /// Parses and renders template text asynchronously and completes with buffered output.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the operation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public Task<string> RenderToStringWithCancellationAsync(string templateText, TemplateValue model, CancellationToken cancellationToken)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(cancellationToken);
        }

        if (Options.SyntaxProfile == "mustache")
        {
            return Task.FromResult(RenderMustacheToString(templateText, model));
        }

        TemplateCompiledTemplate compiled = GetOrCompileWithDiagnostics(CreateSourceForOptions(templateText, Options), Options);
        return RenderDocumentToStringWithCancellationAsync(compiled.Document, model, cancellationToken);
    }

    /// <summary>
    /// Renders a parsed document and returns buffered output.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string RenderSyntaxToString(TemplateDocumentSyntax document, TemplateValue model)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        StringWriter textWriter = new StringWriter(CultureInfo.InvariantCulture);
        RenderDocumentToWriter(document, model, textWriter);
        return textWriter.ToString();
    }

    /// <summary>
    /// Renders a parsed document asynchronously and completes with buffered output.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<string> RenderDocumentToStringAsync(TemplateDocumentSyntax document, TemplateValue model)
    {
        return RenderDocumentToStringWithCancellationAsync(document, model, CancellationToken.None);
    }

    /// <summary>
    /// Renders a parsed document asynchronously and completes with buffered output.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the operation result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public Task<string> RenderDocumentToStringWithCancellationAsync(TemplateDocumentSyntax document, TemplateValue model, CancellationToken cancellationToken)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<string>(cancellationToken);
        }

        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext context = CreateRenderContext(model, writer);
        AsyncTemplateRenderPipeline pipeline = new AsyncTemplateRenderPipeline(this, context, cancellationToken);
        return pipeline.RenderRootToStringAsync(document);
    }

    /// <summary>
    /// Parses and renders template text directly to the supplied writer.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RenderToWriter(string templateText, TemplateValue model, TextWriter writer)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (Options.SyntaxProfile == "mustache")
        {
            RenderMustacheToWriter(templateText, model, writer);
            return;
        }

        TemplateCompiledTemplate compiled = GetOrCompileWithDiagnostics(CreateSourceForOptions(templateText, Options), Options);
        RenderDocumentToWriter(compiled.Document, model, writer);
    }

    /// <summary>
    /// Parses and renders template text asynchronously to the supplied writer.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <returns>A task that represents completion of the operation.</returns>
    public Task RenderToWriterAsync(string templateText, TemplateValue model, TextWriter writer)
    {
        return RenderToWriterWithCancellationAsync(templateText, model, writer, CancellationToken.None);
    }

    /// <summary>
    /// Parses and renders template text asynchronously to the supplied writer.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that represents completion of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public Task RenderToWriterWithCancellationAsync(string templateText, TemplateValue model, TextWriter writer, CancellationToken cancellationToken)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (Options.SyntaxProfile == "mustache")
        {
            RenderMustacheToWriter(templateText, model, writer);
            return Task.CompletedTask;
        }

        TemplateCompiledTemplate compiled = GetOrCompileWithDiagnostics(CreateSourceForOptions(templateText, Options), Options);
        return RenderDocumentToWriterWithCancellationAsync(compiled.Document, model, writer, cancellationToken);
    }

    /// <summary>
    /// Converts Mustache source to native syntax, renders it, and returns the output.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string RenderMustacheToString(string templateText, TemplateValue model)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        StringWriter textWriter = new StringWriter(CultureInfo.InvariantCulture);
        RenderMustacheToWriter(templateText, model, textWriter);
        return textWriter.ToString();
    }

    /// <summary>
    /// Converts Mustache source to native syntax and renders it to the supplied writer.
    /// </summary>
    /// <param name="templateText">The template source text to parse or render.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RenderMustacheToWriter(string templateText, TemplateValue model, TextWriter writer)
    {
        if (templateText == null)
        {
            throw new ArgumentNullException("templateText");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        TemplateOptions options = Options.Clone();
        MustacheProfile.Create().ApplyTo(options);
        TemplateSource source = TemplateSource.FromString(templateText);
        ValidateTemplateSource(source, options);
        TemplateRenderContext context = new TemplateRenderContext(model, writer, options, Registry, MemberResolver);
        context.DiagnosticSink = DiagnosticSink;
        context.SourceMapCollector = SourceMapCollector;
        context.UseMustacheInheritance = true;
        context.BeginRender(source);
        MustacheRuntime runtime = new MustacheRuntime(this, context, model);
        string output = runtime.RenderTemplate(templateText);
        context.EnsureOutputLengthAllowed(output.Length);
        context.Writer.Write(output);
        context.Writer.Flush();
        CompleteRender(context);
    }

    /// <summary>
    /// Renders a parsed document directly to the supplied writer.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RenderDocumentToWriter(TemplateDocumentSyntax document, TemplateValue model, TextWriter writer)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        TemplateRenderContext context = new TemplateRenderContext(model, writer, Options, Registry, MemberResolver);
        context.DiagnosticSink = DiagnosticSink;
        context.SourceMapCollector = SourceMapCollector;
        if (Options.SyntaxProfile == "mustache")
        {
            context.UseMustacheInheritance = true;
        }

        if (Options.SyntaxProfile == "handlebars")
        {
            context.UseHandlebarsCompatibility = true;
        }

        if (Options.SyntaxProfile == "liquid")
        {
            context.UseLiquidCompatibility = true;
        }

        if (Options.SyntaxProfile == "jinja" || Options.SyntaxProfile == "django" || Options.SyntaxProfile == "nunjucks")
        {
            context.UseJinjaCompatibility = true;
        }

        RenderStreaming(document, context);
    }

    /// <summary>
    /// Renders a parsed document asynchronously to the supplied writer.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <returns>A task that represents completion of the operation.</returns>
    public Task RenderDocumentToWriterAsync(TemplateDocumentSyntax document, TemplateValue model, TextWriter writer)
    {
        return RenderDocumentToWriterWithCancellationAsync(document, model, writer, CancellationToken.None);
    }

    /// <summary>
    /// Renders a parsed document asynchronously to the supplied writer.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that represents completion of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public Task RenderDocumentToWriterWithCancellationAsync(TemplateDocumentSyntax document, TemplateValue model, TextWriter writer, CancellationToken cancellationToken)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        TemplateRenderContext context = CreateRenderContext(model, writer);
        AsyncTemplateRenderPipeline pipeline = new AsyncTemplateRenderPipeline(this, context, cancellationToken);
        return pipeline.RenderRootToWriterAsync(document);
    }

    internal TemplateRenderContext CreateRenderContext(TemplateValue model, TextWriter writer)
    {
        TemplateRenderContext context = new TemplateRenderContext(model, writer, Options, Registry, MemberResolver);
        context.DiagnosticSink = DiagnosticSink;
        context.SourceMapCollector = SourceMapCollector;
        if (Options.SyntaxProfile == "mustache")
        {
            context.UseMustacheInheritance = true;
        }

        if (Options.SyntaxProfile == "handlebars")
        {
            context.UseHandlebarsCompatibility = true;
        }

        if (Options.SyntaxProfile == "liquid")
        {
            context.UseLiquidCompatibility = true;
        }

        if (Options.SyntaxProfile == "jinja" || Options.SyntaxProfile == "django" || Options.SyntaxProfile == "nunjucks")
        {
            context.UseJinjaCompatibility = true;
        }

        return context;
    }

    internal TemplateSource CreateSourceForOptions(string templateText, TemplateOptions options)
    {
        TemplateSource source = TemplateSource.FromString(templateText);
        ValidateTemplateSource(source, options);
        return source;
    }

    internal TemplateCompiledTemplate GetOrCompileWithDiagnostics(TemplateSource source, TemplateOptions options)
    {
        ValidateTemplateSource(source, options);
        TemplateCompiledTemplate compiled = CompilationCache.GetOrCompile(source, options);
        ReportDiagnostics(compiled.Diagnostics);
        if (options.SyntaxProfile == "liquid" && options.StrictMode && HasErrorDiagnostic(compiled.Diagnostics))
        {
            throw new InvalidOperationException("Liquid template contains a syntax error.");
        }

        return compiled;
    }

    internal bool HasErrorDiagnostic(List<TemplateDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == TemplateDiagnosticSeverity.Error)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateTemplateSource(TemplateSource source, TemplateOptions options)
    {
        source.Validate();
        options.Validate();
        if (source.Length > options.MaximumTemplateLength)
        {
            throw new InvalidOperationException("Maximum template length of " + options.MaximumTemplateLength.ToString(CultureInfo.InvariantCulture) + " was exceeded.");
        }
    }

    internal void ReportDiagnostics(List<TemplateDiagnostic> diagnostics)
    {
        if (DiagnosticSink == null)
        {
            return;
        }

        int index = 0;
        while (index < diagnostics.Count)
        {
            DiagnosticSink.Report(diagnostics[index]);
            index = index + 1;
        }
    }

    /// <summary>
    /// Renders every node in a parsed document through the supplied stateful render context.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="context">The active state for the current render.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void Render(TemplateDocumentSyntax document, TemplateRenderContext context)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (context.SourceMapCollector == null)
        {
            context.SourceMapCollector = SourceMapCollector;
        }

        context.BeginRender(document.Source);
        string output = RenderDocumentToString(document, context);
        string outputBeforeNormalization = output;
        output = NormalizeRenderedWhitespace(output, context.Options.WhitespaceMode);
        context.SourceMapState.TransformOutput(outputBeforeNormalization, output, document.Span);
        if (context.Options.TrimFinalNewline)
        {
            int previousLength = output.Length;
            output = TrimFinalNewline(output);
            context.SourceMapState.TrimEnd(previousLength - output.Length);
        }

        context.FlowControlSignal = "";
        context.Writer.Write(output);
        context.Writer.Flush();
        CompleteRender(context);
    }

    /// <summary>
    /// Renders a parsed document while coalescing adjacent output before writing each completed chunk.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="context">The active state for the current render.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RenderStreaming(TemplateDocumentSyntax document, TemplateRenderContext context)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (context.SourceMapCollector == null)
        {
            context.SourceMapCollector = SourceMapCollector;
        }

        context.BeginRender(document.Source);
        RegisterTopLevelMacros(document, context);
        TemplateStatementSyntax? extendsStatement = FindExtendsStatement(document);
        if (extendsStatement != null)
        {
            string inheritedOutput = RenderDocumentToString(document, context);
            string inheritedBeforeNormalization = inheritedOutput;
            inheritedOutput = NormalizeRenderedWhitespace(inheritedOutput, context.Options.WhitespaceMode);
            context.SourceMapState.TransformOutput(inheritedBeforeNormalization, inheritedOutput, document.Span);
            if (context.Options.TrimFinalNewline)
            {
                int previousLength = inheritedOutput.Length;
                inheritedOutput = TrimFinalNewline(inheritedOutput);
                context.SourceMapState.TrimEnd(previousLength - inheritedOutput.Length);
            }

            context.FlowControlSignal = "";
            context.Writer.Write(inheritedOutput);
            context.Writer.Flush();
            CompleteRender(context);
            return;
        }

        TemplateStreamingRenderState state = new TemplateStreamingRenderState();
        RenderNodesToWriter(document.Children, context, state);
        string streamingBeforeNormalization = state.OutputSoFar;
        NormalizeStreamingWhitespace(state, context.Options.WhitespaceMode);
        context.SourceMapState.TransformOutput(streamingBeforeNormalization, state.OutputSoFar, document.Span);
        if (context.Options.TrimFinalNewline)
        {
            int previousLength = state.OutputSoFar.Length;
            TrimFinalNewlineFromState(state);
            context.SourceMapState.TrimEnd(previousLength - state.OutputSoFar.Length);
        }

        FlushStreamingState(context, state, true);
        context.FlowControlSignal = "";
        context.Writer.Flush();
        CompleteRender(context);
    }

    internal string RenderDocumentToString(TemplateDocumentSyntax document, TemplateRenderContext context)
    {
        RegisterTopLevelMacros(document, context);
        TemplateStatementSyntax? extendsStatement = FindExtendsStatement(document);
        if (extendsStatement == null)
        {
            return RenderNodesToString(document.Children, context);
        }

        if (extendsStatement.Arguments.Count == 0)
        {
            return "";
        }

        string templateName = TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(extendsStatement.Arguments[0]));
        if (templateName == "")
        {
            return "";
        }

        TemplateSource? source = context.Registry.LoadTemplate(templateName);
        if (source == null)
        {
            ReportMissingComposition(context, "template", templateName, extendsStatement.Span);
            if (context.Options.StrictMissingValues)
            {
                throw new InvalidOperationException("Template '" + templateName + "' was not found.");
            }

            return "";
        }

        context.RecordDependency(TemplateDependencyKind.Template, source);
        TemplateCompiledTemplate compiled = CompileSourceForContext(source, context.Options, context);
        TemplateCompositionArguments compositionArguments = EvaluateCompositionArguments(extendsStatement, context, false);
        Dictionary<string, TemplateBlockOverride> previousOverrides = context.BlockOverrides;
        RenderInheritancePreamble(document, context);
        context.BlockOverrides = ComposeBlockOverrides(document, previousOverrides, context);
        EnterIncludeScope("template", source.Name, context);
        TemplateScopeFrame parentFrame = context.Scope.PushIncludeScope(source.Name, compositionArguments.Model);
        BindCompositionArguments(parentFrame, compositionArguments);
        string output = RenderDocumentToString(compiled.Document, context);
        context.Scope.PopFrame();
        context.PopActiveInclude();
        context.BlockOverrides = previousOverrides;
        return output;
    }

    internal void RenderInheritancePreamble(TemplateDocumentSyntax document, TemplateRenderContext context)
    {
        int index = 0;
        while (index < document.Children.Count)
        {
            TemplateSyntaxNode node = document.Children[index];
            if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
            {
                string name = node.Statement.Name;
                if (name == "import" || name == "set" || name == "assign")
                {
                    RenderStatementToString(node.Statement, context);
                }
            }
            else if (node.Kind == TemplateSyntaxKind.Block && node.Block != null && node.Block.Name == "capture")
            {
                RenderCaptureBlockToString(node.Block, context);
            }

            index = index + 1;
        }
    }

    internal TemplateCompiledTemplate CompileSourceForContext(TemplateSource source, TemplateOptions options, TemplateRenderContext context)
    {
        context.CountTemplate(source);
        TemplateCompiledTemplate compiled = CompilationCache.GetOrCompile(source, options);
        ReportContextDiagnostics(context, compiled.Diagnostics);
        if (options.SyntaxProfile == "liquid" && options.StrictMode && HasErrorDiagnostic(compiled.Diagnostics))
        {
            throw new InvalidOperationException("Liquid template contains a syntax error.");
        }

        return compiled;
    }

    internal void ReportContextDiagnostics(TemplateRenderContext context, List<TemplateDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            context.ReportDiagnostic(diagnostics[index]);
            index = index + 1;
        }
    }

    internal void RenderNode(TemplateSyntaxNode node, TemplateRenderContext context)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (node.Kind == TemplateSyntaxKind.Text && node.Text != null)
        {
            context.Writer.Write(node.Text.Text);
            context.SourceMapState.Record(node.Text.Text, node.Text.Span);
        }
        else if (node.Kind == TemplateSyntaxKind.Output && node.Output != null)
        {
            int outputStart = context.SourceMapState.OutputLength;
            TemplateValue value = EvaluateOutputValue(node.Output, context);
            string rendered = EncodeOutputValue(value, context, node.Output.Raw || !node.Output.Escape);
            context.Writer.Write(rendered);
            context.SourceMapState.FinishNode(outputStart, rendered, node.Output.Expression.Span);
        }
        else if (node.Kind == TemplateSyntaxKind.Raw && node.Raw != null)
        {
            context.Writer.Write(node.Raw.Text);
            context.SourceMapState.Record(node.Raw.Text, node.Raw.Span);
        }
        else if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
        {
            int outputStart = context.SourceMapState.OutputLength;
            string rendered = RenderStatementToString(node.Statement, context);
            context.Writer.Write(rendered);
            context.SourceMapState.FinishNode(outputStart, rendered, node.Statement.Span);
        }
        else if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            int outputStart = context.SourceMapState.OutputLength;
            string rendered = RenderBlockToString(node.Block, context);
            context.Writer.Write(rendered);
            context.SourceMapState.FinishNode(outputStart, rendered, node.Block.Span);
        }
    }

    internal TemplateStatementSyntax? FindExtendsStatement(TemplateDocumentSyntax document)
    {
        int index = 0;
        while (index < document.Children.Count)
        {
            TemplateSyntaxNode node = document.Children[index];
            if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null && node.Statement.Name == "extends")
            {
                return node.Statement;
            }

            index = index + 1;
        }

        return null;
    }

    internal void RegisterTopLevelMacros(TemplateDocumentSyntax document, TemplateRenderContext context)
    {
        int index = 0;
        while (index < document.Children.Count)
        {
            TemplateSyntaxNode node = document.Children[index];
            if (node.Kind == TemplateSyntaxKind.Block && node.Block != null && node.Block.Name == "macro")
            {
                RegisterMacroBlock(node.Block, context);
            }

            index = index + 1;
        }
    }

    internal Dictionary<string, TemplateBlockOverride> ComposeBlockOverrides(TemplateDocumentSyntax document, Dictionary<string, TemplateBlockOverride> inheritedOverrides, TemplateRenderContext context)
    {
        Dictionary<string, TemplateBlockOverride> overrides = CloneBlockOverrides(inheritedOverrides);
        int index = 0;
        while (index < document.Children.Count)
        {
            TemplateSyntaxNode node = document.Children[index];
            if (node.Kind == TemplateSyntaxKind.Block && node.Block != null && node.Block.Name == "block")
            {
                string name = GetTemplateBlockName(node.Block, context);
                if (name != "")
                {
                    TemplateBlockOverride blockOverride = new TemplateBlockOverride(name, node.Block);
                    if (overrides.ContainsKey(name))
                    {
                        overrides[name].AppendParent(blockOverride);
                    }
                    else
                    {
                        overrides[name] = blockOverride;
                    }
                }
            }

            index = index + 1;
        }

        return overrides;
    }

    internal Dictionary<string, TemplateBlockOverride> CloneBlockOverrides(Dictionary<string, TemplateBlockOverride> inheritedOverrides)
    {
        Dictionary<string, TemplateBlockOverride> overrides = new Dictionary<string, TemplateBlockOverride>();
        foreach (KeyValuePair<string, TemplateBlockOverride> pair in inheritedOverrides)
        {
            overrides[pair.Key] = pair.Value.Clone();
        }

        return overrides;
    }

    internal void RenderNodes(List<TemplateSyntaxNode> nodes, TemplateRenderContext context)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            RenderNode(nodes[index], context);
            if (context.FlowControlSignal != "")
            {
                break;
            }

            index = index + 1;
        }
    }

    internal void RenderNodesToWriter(List<TemplateSyntaxNode> nodes, TemplateRenderContext context, TemplateStreamingRenderState state)
    {
        int pendingTrim = 0;
        int index = 0;
        while (index < nodes.Count)
        {
            context.CheckRenderTime();
            TemplateSyntaxNode node = nodes[index];
            if (node.Kind == TemplateSyntaxKind.Text && node.Text != null)
            {
                string text = ApplyLeadingTrim(node.Text.Text, pendingTrim);
                context.SourceMapState.Record(text, node.Text.Span);
                AppendStreamingOutput(context, state, text);
                pendingTrim = 0;
            }
            else
            {
                TemplateTrimKind leftTrimKind = GetOpeningLeftTrimKind(node);
                if (leftTrimKind == TemplateTrimKind.Greedy)
                {
                    int previousLength = state.OutputSoFar.Length;
                    TrimTrailingWhitespaceFromState(state);
                    context.SourceMapState.TrimEnd(previousLength - state.OutputSoFar.Length);
                }
                else if (leftTrimKind == TemplateTrimKind.NonGreedy || IsOpeningStandalone(node, context))
                {
                    int previousLength = state.OutputSoFar.Length;
                    TrimStandaloneLeftFromState(state);
                    context.SourceMapState.TrimEnd(previousLength - state.OutputSoFar.Length);
                }

                int outputStart = context.SourceMapState.OutputLength;
                string rendered = RenderNodeToString(node, context, state.OutputSoFar);
                context.SourceMapState.FinishNode(outputStart, rendered, GetNodeSourceSpan(node));
                AppendStreamingOutput(context, state, rendered);
                TemplateTrimKind rightTrimKind = GetClosingRightTrimKind(node);
                if (rightTrimKind == TemplateTrimKind.Greedy)
                {
                    pendingTrim = 1;
                }
                else if (rightTrimKind == TemplateTrimKind.NonGreedy || IsClosingStandalone(node, context))
                {
                    pendingTrim = 2;
                }
            }

            if (context.FlowControlSignal != "")
            {
                break;
            }

            index = index + 1;
        }
    }

    internal void AppendStreamingOutput(TemplateRenderContext context, TemplateStreamingRenderState state, string text)
    {
        if (text == "")
        {
            return;
        }

        state.PendingOutput = state.PendingOutput + text;
        state.OutputSoFar = state.OutputSoFar + text;
        FlushStreamingState(context, state, false);
    }

    internal void FlushStreamingState(TemplateRenderContext context, TemplateStreamingRenderState state, bool flushAll)
    {
        if (!flushAll && context.Options.WhitespaceMode != TemplateWhitespaceMode.Preserve)
        {
            return;
        }

        int flushLength = state.PendingOutput.Length;
        if (!flushAll)
        {
            flushLength = GetStableStreamingFlushLength(state.PendingOutput);
        }

        if (flushLength <= 0)
        {
            return;
        }

        context.Writer.Write(state.PendingOutput.Substring(0, flushLength));
        state.PendingOutput = state.PendingOutput.Substring(flushLength);
    }

    internal string NormalizeRenderedWhitespace(string text, TemplateWhitespaceMode mode)
    {
        TemplateWhitespaceNormalizer normalizer = new TemplateWhitespaceNormalizer();
        return normalizer.Normalize(text, mode);
    }

    internal void NormalizeStreamingWhitespace(TemplateStreamingRenderState state, TemplateWhitespaceMode mode)
    {
        if (mode == TemplateWhitespaceMode.Preserve)
        {
            return;
        }

        state.PendingOutput = NormalizeRenderedWhitespace(state.PendingOutput, mode);
        state.OutputSoFar = NormalizeRenderedWhitespace(state.OutputSoFar, mode);
    }

    internal int GetStableStreamingFlushLength(string text)
    {
        int index = text.Length - 1;
        while (index >= 0 && char.IsWhiteSpace(text[index]))
        {
            index = index - 1;
        }

        return index + 1;
    }

    internal void TrimTrailingWhitespaceFromState(TemplateStreamingRenderState state)
    {
        string trimmed = TrimTrailingWhitespace(state.PendingOutput);
        int removed = state.PendingOutput.Length - trimmed.Length;
        state.PendingOutput = trimmed;
        RemoveOutputSoFarSuffix(state, removed);
    }

    internal void TrimStandaloneLeftFromState(TemplateStreamingRenderState state)
    {
        string trimmed = TrimStandaloneLeft(state.PendingOutput);
        int removed = state.PendingOutput.Length - trimmed.Length;
        state.PendingOutput = trimmed;
        RemoveOutputSoFarSuffix(state, removed);
    }

    internal void TrimFinalNewlineFromState(TemplateStreamingRenderState state)
    {
        string trimmed = TrimFinalNewline(state.PendingOutput);
        int removed = state.PendingOutput.Length - trimmed.Length;
        state.PendingOutput = trimmed;
        RemoveOutputSoFarSuffix(state, removed);
    }

    internal void RemoveOutputSoFarSuffix(TemplateStreamingRenderState state, int length)
    {
        if (length <= 0)
        {
            return;
        }

        if (length >= state.OutputSoFar.Length)
        {
            state.OutputSoFar = "";
            return;
        }

        state.OutputSoFar = state.OutputSoFar.Substring(0, state.OutputSoFar.Length - length);
    }

    internal string RenderNodesToString(List<TemplateSyntaxNode> nodes, TemplateRenderContext context)
    {
        string output = "";
        int pendingTrim = 0;
        int index = 0;
        while (index < nodes.Count)
        {
            context.CheckRenderTime();
            TemplateSyntaxNode node = nodes[index];
            if (node.Kind == TemplateSyntaxKind.Text && node.Text != null)
            {
                string text = ApplyLeadingTrim(node.Text.Text, pendingTrim);
                context.SourceMapState.Record(text, node.Text.Span);
                output = AppendStringOutput(context, output, text);
                pendingTrim = 0;
            }
            else
            {
                TemplateTrimKind leftTrimKind = GetOpeningLeftTrimKind(node);
                if (leftTrimKind == TemplateTrimKind.Greedy)
                {
                    int previousLength = output.Length;
                    output = TrimTrailingWhitespace(output);
                    context.SourceMapState.TrimEnd(previousLength - output.Length);
                }
                else if (leftTrimKind == TemplateTrimKind.NonGreedy || IsOpeningStandalone(node, context))
                {
                    int previousLength = output.Length;
                    output = TrimStandaloneLeft(output);
                    context.SourceMapState.TrimEnd(previousLength - output.Length);
                }

                int outputStart = context.SourceMapState.OutputLength;
                string rendered = RenderNodeToString(node, context, output);
                context.SourceMapState.FinishNode(outputStart, rendered, GetNodeSourceSpan(node));
                output = AppendStringOutput(context, output, rendered);
                TemplateTrimKind rightTrimKind = GetClosingRightTrimKind(node);
                if (rightTrimKind == TemplateTrimKind.Greedy)
                {
                    pendingTrim = 1;
                }
                else if (rightTrimKind == TemplateTrimKind.NonGreedy || IsClosingStandalone(node, context))
                {
                    pendingTrim = 2;
                }
            }

            if (context.FlowControlSignal != "")
            {
                break;
            }

            index = index + 1;
        }

        return output;
    }

    internal TemplateSourceSpan GetNodeSourceSpan(TemplateSyntaxNode node)
    {
        if (node.Output != null)
        {
            return node.Output.Expression.Span;
        }

        return node.Span;
    }

    internal string AppendStringOutput(TemplateRenderContext context, string output, string text)
    {
        if (text == "")
        {
            return output;
        }

        if (text.Length > 2147483647 - output.Length)
        {
            throw new InvalidOperationException("Template output length limit exceeded.");
        }

        context.EnsureOutputLengthAllowed(output.Length + text.Length);
        return output + text;
    }

    internal string RenderNodeToString(TemplateSyntaxNode node, TemplateRenderContext context, string outputSoFar)
    {
        if (node.Kind == TemplateSyntaxKind.Output && node.Output != null)
        {
            return RenderOutputToString(node.Output, context, outputSoFar);
        }

        if (node.Kind == TemplateSyntaxKind.Raw && node.Raw != null)
        {
            return RenderRawToString(node.Raw, context);
        }

        if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
        {
            return RenderStatementToString(node.Statement, context);
        }

        if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            return RenderBlockToString(node.Block, context);
        }

        return "";
    }

    internal void RenderOutput(TemplateOutputSyntax output, TemplateRenderContext context)
    {
        TemplateValue value = EvaluateOutputValue(output, context);
        bool raw = output.Raw || !output.Escape;
        context.Writer.Write(EncodeOutputValue(value, context, raw));
    }

    internal string RenderOutputToString(TemplateOutputSyntax output, TemplateRenderContext context, string outputSoFar)
    {
        TemplateValue value = EvaluateOutputValue(output, context);
        bool raw = output.Raw || !output.Escape;
        string text = EncodeOutputValue(value, context, raw);
        if (context.Options.AutoIndentMultilineValues)
        {
            return ApplyAutoIndent(text, outputSoFar);
        }

        return text;
    }

    internal TemplateValue EvaluateOutputValue(TemplateOutputSyntax output, TemplateRenderContext context)
    {
        if (context.UseHandlebarsCompatibility
            && output.Expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable
            && context.Registry.Helpers.ContainsKey(output.Expression.Text))
        {
            return context.Registry.Helpers[output.Expression.Text](new List<TemplateValue>());
        }

        TemplateValue value = context.Evaluator.Evaluate(output.Expression);
        if (context.UseMustacheInheritance && value.Kind == TemplateValueKind.Function)
        {
            List<TemplateValue> arguments = new List<TemplateValue>();
            return value.Invoke(arguments);
        }

        if (context.UseHandlebarsCompatibility && value.Kind == TemplateValueKind.Function)
        {
            return value.Invoke(new List<TemplateValue>());
        }

        return value;
    }

    internal string RenderRawToString(TemplateRawSyntax raw, TemplateRenderContext context)
    {
        string text = raw.Text;
        string sourceText = raw.Span.GetText();
        int openEnd = sourceText.IndexOf("%}", StringComparison.Ordinal);
        int closeStart = sourceText.LastIndexOf("{%", StringComparison.Ordinal);
        bool openingStandalone = false;
        bool closingStandalone = false;

        if (openEnd >= 0)
        {
            openingStandalone = AllowsStandaloneTrimming(context) && IsStandaloneLine(raw.Span.Source.Text, raw.Span.Start, raw.Span.Start + openEnd + 2);
            TemplateTrimKind openingRightTrim = GetRightTrimKind(sourceText.Substring(0, openEnd + 2));
            if (openingRightTrim == TemplateTrimKind.Greedy)
            {
                text = TrimLeadingWhitespace(text);
            }
            else if (openingRightTrim == TemplateTrimKind.NonGreedy || openingStandalone)
            {
                text = TrimStandaloneRight(text);
            }
        }

        if (closeStart >= 0)
        {
            closingStandalone = AllowsStandaloneTrimming(context) && IsStandaloneLine(raw.Span.Source.Text, raw.Span.Start + closeStart, raw.Span.End);
            TemplateTrimKind closingLeftTrim = GetLeftTrimKind(sourceText.Substring(closeStart));
            if (closingLeftTrim == TemplateTrimKind.Greedy)
            {
                text = TrimTrailingWhitespace(text);
            }
            else if (closingLeftTrim == TemplateTrimKind.NonGreedy || closingStandalone)
            {
                text = TrimStandaloneLeft(text);
            }
        }

        return text;
    }

    internal void RenderStatement(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        context.Writer.Write(RenderStatementToString(statement, context));
    }

    internal string RenderStatementToString(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        if (statement.Name == "return")
        {
            return RenderReturnStatementToString(statement, context);
        }

        if (statement.Name == "break" || statement.Name == "continue")
        {
            context.FlowControlSignal = statement.Name;
            return "";
        }

        if (statement.Name == "set" || statement.Name == "assign")
        {
            RenderAssignmentStatement(statement, context);
            return "";
        }

        if (statement.Name == "increment" || statement.Name == "decrement")
        {
            return RenderCounterStatementToString(statement, context);
        }

        if (statement.Name == "liquidCycle" || statement.Name == "liquidCycleNamed")
        {
            return RenderLiquidCycleToString(statement, context);
        }

        if (statement.Name == "liquidComment")
        {
            return "";
        }

        if (statement.Name == "liquidRenderFor" || statement.Name == "liquidIncludeFor")
        {
            return RenderLiquidTemplateForToString(statement, context, statement.Name == "liquidRenderFor");
        }

        if (statement.Name == "include")
        {
            return RenderNamedTemplateStatementToString(statement, context, false);
        }

        if (statement.Name == "render")
        {
            return RenderNamedTemplateStatementToString(statement, context, true);
        }

        if (statement.Name == "partial")
        {
            return RenderPartialStatementToString(statement, context);
        }

        if (statement.Name == "import")
        {
            return RenderImportStatementToString(statement, context);
        }

        if (statement.Name == "handlebarsPartialBlockBody")
        {
            return RenderHandlebarsPartialBlockBodyToString(context);
        }

        int index = 0;
        while (index < statement.Arguments.Count)
        {
            context.Evaluator.Evaluate(statement.Arguments[index]);
            index = index + 1;
        }

        return "";
    }

    internal string RenderReturnStatementToString(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        TemplateScopeFrame? macroFrame = context.Scope.FindNearestFrame(TemplateScopeFrameKind.Macro);
        if (macroFrame == null)
        {
            int argumentIndex = 0;
            while (argumentIndex < statement.Arguments.Count)
            {
                context.Evaluator.Evaluate(statement.Arguments[argumentIndex]);
                argumentIndex = argumentIndex + 1;
            }

            return "";
        }

        TemplateValue value = TemplateValue.Null();
        if (statement.Arguments.Count > 0)
        {
            value = context.Evaluator.Evaluate(statement.Arguments[0]);
        }

        context.ReturnValue = value;
        context.FlowControlSignal = "return";
        return "";
    }

    internal string RenderImportStatementToString(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        if (statement.Arguments.Count == 0)
        {
            return "";
        }

        string templateName = TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(statement.Arguments[0]));
        if (templateName == "")
        {
            return "";
        }

        TemplateSource? source = context.Registry.LoadTemplate(templateName);
        if (source == null)
        {
            ReportMissingComposition(context, "template", templateName, statement.Span);
            if (context.Options.StrictMissingValues)
            {
                throw new InvalidOperationException("Template '" + templateName + "' was not found.");
            }

            return "";
        }

        context.RecordDependency(TemplateDependencyKind.Template, source);
        TemplateCompiledTemplate compiled = CompileSourceForContext(source, context.Options, context);
        RegisterTopLevelMacros(compiled.Document, context);
        return "";
    }

    internal string RenderPartialStatementToString(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        if (statement.Arguments.Count == 0)
        {
            return "";
        }

        string partialName = TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(statement.Arguments[0]));
        if (partialName == "")
        {
            return "";
        }

        TemplateSource? source = LoadHandlebarsPartial(partialName, context);
        if (source == null)
        {
            ReportMissingComposition(context, "partial", partialName, statement.Span);
            if (context.Options.StrictMissingValues)
            {
                throw new InvalidOperationException("Partial '" + partialName + "' was not found.");
            }

            return "";
        }

        if (context.UseHandlebarsCompatibility)
        {
            string indentation = GetHandlebarsPartialIndentation(statement);
            if (indentation != "")
            {
                string indentedText = IndentHandlebarsPartialSource(source.Text, indentation);
                source = new TemplateSource(source.Name, source.Path, source.Version, TemplateSource.ComputeContentHash(indentedText), indentedText);
            }
        }

        context.RecordDependency(TemplateDependencyKind.Partial, source);
        TemplateCompiledTemplate compiled = CompileSourceForContext(source, context.Options, context);
        TemplateCompositionArguments compositionArguments = EvaluateCompositionArguments(statement, context, false);
        return RenderInheritedCompositionToString(compiled.Document, "partial", source.Name, compositionArguments, context);
    }

    internal string RenderNamedTemplateStatementToString(TemplateStatementSyntax statement, TemplateRenderContext context, bool isolated)
    {
        if (statement.Arguments.Count == 0)
        {
            return "";
        }

        string templateName = TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(statement.Arguments[0]));
        if (templateName == "")
        {
            return "";
        }

        TemplateSource? source = context.Registry.LoadTemplate(templateName);
        if (source == null)
        {
            ReportMissingComposition(context, "template", templateName, statement.Span);
            if (context.Options.StrictMissingValues)
            {
                throw new InvalidOperationException("Template '" + templateName + "' was not found.");
            }

            return "";
        }

        context.RecordDependency(TemplateDependencyKind.Template, source);
        TemplateCompiledTemplate compiled = CompileSourceForContext(source, context.Options, context);
        TemplateCompositionArguments compositionArguments = EvaluateCompositionArguments(statement, context, isolated);
        if (context.UseLiquidCompatibility && statement.Arguments.Count > 1 && compositionArguments.ContextAliases.Count == 0)
        {
            compositionArguments.ContextAliases.Add(GetLiquidTemplateAlias(source.Name));
        }

        if (isolated)
        {
            return RenderIsolatedCompositionToString(compiled.Document, "template", source.Name, compositionArguments, context);
        }

        return RenderInheritedCompositionToString(compiled.Document, "template", source.Name, compositionArguments, context);
    }

    internal TemplateSource? LoadHandlebarsPartial(string partialName, TemplateRenderContext context)
    {
        if (context.UseHandlebarsCompatibility && context.HasVisibleHandlebarsInlinePartial(partialName))
        {
            return context.HandlebarsInlinePartials[partialName];
        }

        return context.Registry.LoadPartial(partialName);
    }

    internal string GetHandlebarsPartialIndentation(TemplateStatementSyntax statement)
    {
        string sourceText = statement.Span.Source.Text;
        int start = statement.Span.Start - 1;
        while (start >= 0 && sourceText[start] != '\r' && sourceText[start] != '\n')
        {
            if (sourceText[start] != ' ' && sourceText[start] != '\t')
            {
                return "";
            }

            start = start - 1;
        }

        return sourceText.Substring(start + 1, statement.Span.Start - start - 1);
    }

    internal string IndentHandlebarsPartialSource(string text, string indentation)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            builder.Append(current);
            if (current == '\n' && index + 1 < text.Length)
            {
                builder.Append(indentation);
            }
            else if (current == '\r' && index + 1 < text.Length && text[index + 1] != '\n')
            {
                builder.Append(indentation);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal TemplateValue GetNamedTemplateModel(TemplateStatementSyntax statement, TemplateRenderContext context, bool isolated)
    {
        if (statement.Arguments.Count > 1)
        {
            return context.Evaluator.Evaluate(statement.Arguments[1]);
        }

        if (isolated)
        {
            return context.Scope.RootValue;
        }

        return context.Scope.GetCurrentValue();
    }

    internal string RenderInheritedTemplateToString(TemplateDocumentSyntax document, string templateKind, string templateName, TemplateValue model, TemplateRenderContext context)
    {
        TemplateCompositionArguments compositionArguments = new TemplateCompositionArguments(model);
        return RenderInheritedCompositionToString(document, templateKind, templateName, compositionArguments, context);
    }

    internal string RenderInheritedCompositionToString(TemplateDocumentSyntax document, string templateKind, string templateName, TemplateCompositionArguments compositionArguments, TemplateRenderContext context)
    {
        EnterIncludeScope(templateKind, templateName, context);
        TemplateScopeFrame frame = context.Scope.PushIncludeScope(templateName, compositionArguments.Model);
        BindCompositionArguments(frame, compositionArguments);
        Dictionary<string, TemplateBlockOverride> previousOverrides = context.BlockOverrides;
        context.BlockOverrides = new Dictionary<string, TemplateBlockOverride>();
        string output = RenderDocumentToString(document, context);
        context.BlockOverrides = previousOverrides;
        context.Scope.PopFrame();
        context.PopActiveInclude();
        return output;
    }

    internal string RenderIsolatedTemplateToString(TemplateDocumentSyntax document, string templateKind, string templateName, TemplateValue model, TemplateRenderContext context)
    {
        TemplateCompositionArguments compositionArguments = new TemplateCompositionArguments(model);
        return RenderIsolatedCompositionToString(document, templateKind, templateName, compositionArguments, context);
    }

    internal string RenderIsolatedCompositionToString(TemplateDocumentSyntax document, string templateKind, string templateName, TemplateCompositionArguments compositionArguments, TemplateRenderContext context)
    {
        StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
        TemplateRenderContext childContext = new TemplateRenderContext(compositionArguments.Model, writer, context.Options, context.Registry, context.MemberResolver);
        childContext.DiagnosticSink = context.DiagnosticSink;
        childContext.UseSourceMapStateFrom(context);
        childContext.EscapingMode = context.EscapingMode;
        childContext.AutoEscape = context.AutoEscape;
        if (!context.UseLiquidCompatibility)
        {
            childContext.Counters = context.Counters;
            childContext.LiquidForOffsets = context.LiquidForOffsets;
        }
        childContext.IncludeStack = context.IncludeStack;
        childContext.Dependencies = context.Dependencies;
        childContext.HandlebarsInlinePartials = context.HandlebarsInlinePartials;
        childContext.HandlebarsInlinePartialScopeIds = context.HandlebarsInlinePartialScopeIds;
        childContext.HandlebarsInlinePartialScopes = context.HandlebarsInlinePartialScopes;
        childContext.HandlebarsInlinePartialScopeCounter = context.HandlebarsInlinePartialScopeCounter;
        childContext.HandlebarsInlinePartialSavedSources = context.HandlebarsInlinePartialSavedSources;
        childContext.HandlebarsInlinePartialSavedScopeIds = context.HandlebarsInlinePartialSavedScopeIds;
        childContext.HandlebarsPartialBlockContexts = context.HandlebarsPartialBlockContexts;
        childContext.UseRuntimeQuotasFrom(context);
        childContext.UseMustacheInheritance = context.UseMustacheInheritance;
        childContext.UseHandlebarsCompatibility = context.UseHandlebarsCompatibility;
        childContext.UseLiquidCompatibility = context.UseLiquidCompatibility;
        childContext.UseJinjaCompatibility = context.UseJinjaCompatibility;
        EnterIncludeScope(templateKind, templateName, childContext);
        TemplateScopeFrame frame = childContext.Scope.PushIncludeScope(templateName, compositionArguments.Model);
        BindCompositionArguments(frame, compositionArguments);
        string output = RenderDocumentToString(document, childContext);
        childContext.Scope.PopFrame();
        childContext.PopActiveInclude();
        if (!context.UseLiquidCompatibility)
        {
            context.FlowControlSignal = childContext.FlowControlSignal;
            context.ReturnValue = childContext.ReturnValue;
        }
        return output;
    }

    internal TemplateCompositionArguments EvaluateCompositionArguments(TemplateStatementSyntax statement, TemplateRenderContext context, bool isolated)
    {
        TemplateCompositionArguments compositionArguments = new TemplateCompositionArguments(GetNamedTemplateModel(statement, context, isolated));
        if (context.UseLiquidCompatibility && !isolated)
        {
            TemplateScopeFrame parameterFrame = context.Scope.PushFrame(TemplateScopeFrameKind.Local, "", context.Scope.GetCurrentValue());
            int liquidParameterIndex = 0;
            while (liquidParameterIndex < statement.NamedArguments.Count)
            {
                TemplateNamedArgumentSyntax parameter = statement.NamedArguments[liquidParameterIndex];
                TemplateValue parameterValue = context.Evaluator.Evaluate(parameter.Value);
                compositionArguments.Parameters[parameter.Name] = parameterValue;
                parameterFrame.SetLocal(parameter.Name, parameterValue);
                liquidParameterIndex = liquidParameterIndex + 1;
            }

            context.Scope.PopFrame();
            int liquidAliasIndex = 0;
            while (liquidAliasIndex < statement.BlockParameters.Count)
            {
                compositionArguments.ContextAliases.Add(statement.BlockParameters[liquidAliasIndex].Name);
                liquidAliasIndex = liquidAliasIndex + 1;
            }

            return compositionArguments;
        }

        int parameterIndex = 0;
        while (parameterIndex < statement.NamedArguments.Count)
        {
            TemplateNamedArgumentSyntax parameter = statement.NamedArguments[parameterIndex];
            compositionArguments.Parameters[parameter.Name] = context.Evaluator.Evaluate(parameter.Value);
            parameterIndex = parameterIndex + 1;
        }

        parameterIndex = 0;
        while (parameterIndex < statement.HashArguments.Count)
        {
            TemplateNamedArgumentSyntax parameter = statement.HashArguments[parameterIndex];
            compositionArguments.Parameters[parameter.Name] = context.Evaluator.Evaluate(parameter.Value);
            parameterIndex = parameterIndex + 1;
        }

        int aliasIndex = 0;
        while (aliasIndex < statement.BlockParameters.Count)
        {
            compositionArguments.ContextAliases.Add(statement.BlockParameters[aliasIndex].Name);
            aliasIndex = aliasIndex + 1;
        }

        return compositionArguments;
    }

    internal void BindCompositionArguments(TemplateScopeFrame frame, TemplateCompositionArguments compositionArguments)
    {
        foreach (KeyValuePair<string, TemplateValue> pair in compositionArguments.Parameters)
        {
            frame.SetLocal(pair.Key, pair.Value);
        }

        int aliasIndex = 0;
        while (aliasIndex < compositionArguments.ContextAliases.Count)
        {
            frame.SetLocal(compositionArguments.ContextAliases[aliasIndex], compositionArguments.Model);
            aliasIndex = aliasIndex + 1;
        }
    }

    internal void EnterIncludeScope(string templateKind, string templateName, TemplateRenderContext context)
    {
        string activeName = templateKind + ":" + templateName;
        int nextDepth = context.IncludeStack.Count + 1;
        context.EnsureIncludeDepthAllowed(nextDepth);

        if (context.HasActiveInclude(activeName))
        {
            throw new InvalidOperationException("Recursive " + templateKind + " include detected for '" + templateName + "'.");
        }

        context.PushActiveInclude(activeName);
    }

    internal void ReportMissingComposition(TemplateRenderContext context, string kind, string name, TemplateSourceSpan span)
    {
        string displayKind = kind;
        if (kind == "template")
        {
            displayKind = "Template";
        }
        else if (kind == "partial")
        {
            displayKind = "Partial";
        }

        context.ReportDiagnostic(new TemplateDiagnostic("COMPOSE001", TemplateDiagnosticSeverity.Warning, displayKind + " '" + name + "' was not found.", span, "Register the dependency or correct its name."));
    }

    /// <summary>
    /// Returns the last dependencies.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public List<TemplateDependency> GetLastDependencies()
    {
        lock (RenderStateLock)
        {
            List<TemplateDependency> dependencies = new List<TemplateDependency>();
            int index = 0;
            while (index < LastDependencies.Count)
            {
                TemplateDependency dependency = LastDependencies[index];
                dependencies.Add(new TemplateDependency(dependency.Kind, dependency.Name, dependency.Path, dependency.Version, dependency.ContentHash));
                index = index + 1;
            }

            return dependencies;
        }
    }

    /// <summary>
    /// Returns defensive copies of source mappings from the most recent completed render.
    /// </summary>
    /// <returns>The ordered source mappings.</returns>
    public List<TemplateSourceMapEntry> GetLastSourceMap()
    {
        lock (RenderStateLock)
        {
            List<TemplateSourceMapEntry> entries = new List<TemplateSourceMapEntry>();
            int index = 0;
            while (index < LastSourceMap.Count)
            {
                TemplateSourceMapEntry entry = LastSourceMap[index];
                entries.Add(new TemplateSourceMapEntry(entry.OutputStart, entry.OutputLength, entry.SourceSpan));
                index = index + 1;
            }

            return entries;
        }
    }

    internal void CompleteRender(TemplateRenderContext context)
    {
        List<TemplateDependency> dependencies = context.GetDependencies();
        List<TemplateSourceMapEntry> entries = context.SourceMapState.CopyEntries();
        lock (RenderStateLock)
        {
            LastDependencies = dependencies;
            LastSourceMap = entries;
            if (context.SourceMapCollector == null)
            {
                return;
            }

            context.SourceMapCollector.Clear();
            int index = 0;
            while (index < LastSourceMap.Count)
            {
                TemplateSourceMapEntry entry = LastSourceMap[index];
                context.SourceMapCollector.Report(new TemplateSourceMapEntry(entry.OutputStart, entry.OutputLength, entry.SourceSpan));
                index = index + 1;
            }
        }
    }

    internal string RenderCounterStatementToString(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        string name = GetCounterName(statement);
        long current = 0;
        if (context.Counters.ContainsKey(name))
        {
            current = context.Counters[name];
        }

        if (statement.Name == "increment")
        {
            if (current == long.MaxValue)
            {
                throw new InvalidOperationException("Template counter limit exceeded.");
            }

            context.Counters[name] = current + 1;
            return current.ToString(CultureInfo.InvariantCulture);
        }

        if (current == long.MinValue)
        {
            throw new InvalidOperationException("Template counter limit exceeded.");
        }

        current = current - 1;
        context.Counters[name] = current;
        return current.ToString(CultureInfo.InvariantCulture);
    }

    internal string RenderLiquidCycleToString(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        int valueStart = 0;
        string group = "";
        if (statement.Name == "liquidCycleNamed")
        {
            if (statement.Arguments.Count < 2)
            {
                return "";
            }

            group = TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(statement.Arguments[0]));
            valueStart = 1;
        }
        else
        {
            int keyIndex = 0;
            while (keyIndex < statement.Arguments.Count)
            {
                if (keyIndex > 0)
                {
                    group = group + ":";
                }

                group = group + statement.Arguments[keyIndex].Text;
                keyIndex = keyIndex + 1;
            }
        }

        int valueCount = statement.Arguments.Count - valueStart;
        if (valueCount <= 0)
        {
            return "";
        }

        string key = "liquid-cycle:" + group;
        long counter = 0;
        if (context.Counters.ContainsKey(key))
        {
            counter = context.Counters[key];
        }

        int selectedIndex = (int)(counter % valueCount) + valueStart;
        if (counter == long.MaxValue)
        {
            throw new InvalidOperationException("Template counter limit exceeded.");
        }

        context.Counters[key] = counter + 1;
        TemplateValue selected = context.Evaluator.Evaluate(statement.Arguments[selectedIndex]);
        return TemplateOutputEncoder.ConvertValueToText(selected);
    }

    internal string RenderLiquidTemplateForToString(TemplateStatementSyntax statement, TemplateRenderContext context, bool isolated)
    {
        if (statement.Arguments.Count < 2)
        {
            return "";
        }

        string templateName = TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(statement.Arguments[0]));
        if (templateName == "")
        {
            return "";
        }

        TemplateSource? source = context.Registry.LoadTemplate(templateName);
        if (source == null)
        {
            ReportMissingComposition(context, "template", templateName, statement.Span);
            if (context.Options.StrictMissingValues)
            {
                throw new InvalidOperationException("Template '" + templateName + "' was not found.");
            }

            return "";
        }

        context.RecordDependency(TemplateDependencyKind.Template, source);
        TemplateCompiledTemplate compiled = CompileSourceForContext(source, context.Options, context);
        TemplateValue iterable = context.Evaluator.Evaluate(statement.Arguments[1]);
        List<TemplateLoopIteration> iterations = CreateLoopIterations(iterable);
        string output = "";
        int index = 0;
        while (index < iterations.Count)
        {
            context.CountLoopIteration();
            TemplateLoopIteration iteration = iterations[index];
            TemplateCompositionArguments arguments = new TemplateCompositionArguments(iteration.CurrentValue);
            AddLiquidCompositionParameters(arguments, statement, context);
            int aliasIndex = 0;
            while (aliasIndex < statement.BlockParameters.Count)
            {
                arguments.ContextAliases.Add(statement.BlockParameters[aliasIndex].Name);
                aliasIndex = aliasIndex + 1;
            }

            if (arguments.ContextAliases.Count == 0)
            {
                arguments.ContextAliases.Add(GetLiquidTemplateAlias(source.Name));
            }

            if (isolated)
            {
                TemplateValue parentLoop = TemplateValue.Missing();
                arguments.Parameters["forloop"] = CreateLoopMetadata(iteration, index, iterations.Count, parentLoop);
                output = AppendStringOutput(context, output, RenderIsolatedCompositionToString(compiled.Document, "template", source.Name, arguments, context));
            }
            else
            {
                output = AppendStringOutput(context, output, RenderInheritedCompositionToString(compiled.Document, "template", source.Name, arguments, context));
            }

            index = index + 1;
        }

        return output;
    }

    internal string GetLiquidTemplateAlias(string templateName)
    {
        string alias = templateName;
        int slashIndex = alias.LastIndexOf('/');
        if (slashIndex >= 0)
        {
            alias = alias.Substring(slashIndex + 1);
        }

        int dotIndex = alias.LastIndexOf('.');
        if (dotIndex > 0)
        {
            alias = alias.Substring(0, dotIndex);
        }

        return alias;
    }

    internal void AddLiquidCompositionParameters(TemplateCompositionArguments arguments, TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        int index = 0;
        while (index < statement.NamedArguments.Count)
        {
            TemplateNamedArgumentSyntax parameter = statement.NamedArguments[index];
            arguments.Parameters[parameter.Name] = context.Evaluator.Evaluate(parameter.Value);
            index = index + 1;
        }
    }

    internal string GetCounterName(TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count == 0)
        {
            return "";
        }

        return statement.Arguments[0].Text;
    }

    internal void RenderAssignmentStatement(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        string name = GetAssignmentVariableName(statement);
        int valueIndex = GetAssignmentValueIndex(statement);
        if (name == "" || valueIndex < 0)
        {
            return;
        }

        TemplateValue value = EvaluateAssignmentValue(statement, valueIndex, context);
        if (context.UseLiquidCompatibility)
        {
            context.Scope.SetLiquidLocal(name, value);
        }
        else
        {
            context.Scope.SetLocal(name, value);
        }
    }

    internal TemplateValue EvaluateAssignmentValue(TemplateStatementSyntax statement, int valueIndex, TemplateRenderContext context)
    {
        if (valueIndex + 1 == statement.Arguments.Count)
        {
            return context.Evaluator.Evaluate(statement.Arguments[valueIndex]);
        }

        string expressionText = GetStatementArgumentRangeText(statement, valueIndex);
        TemplateParseResult parseResult = TemplateParser.ParseNative("{{ " + expressionText + " }}");
        if (parseResult.HasDiagnostics() || parseResult.Document.Children.Count == 0)
        {
            return TemplateValue.Missing();
        }

        TemplateSyntaxNode node = parseResult.Document.Children[0];
        if (node.Output == null)
        {
            return TemplateValue.Missing();
        }

        return context.Evaluator.Evaluate(node.Output.Expression);
    }

    internal string GetStatementArgumentRangeText(TemplateStatementSyntax statement, int startIndex)
    {
        TemplateExpressionSyntax first = statement.Arguments[startIndex];
        TemplateExpressionSyntax last = statement.Arguments[statement.Arguments.Count - 1];
        string sourceText = first.Span.Source.Text;
        return sourceText.Substring(first.Span.Start, last.Span.End - first.Span.Start);
    }

    internal string GetAssignmentVariableName(TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count == 0)
        {
            return "";
        }

        TemplateExpressionSyntax expression = statement.Arguments[0];
        string text = expression.Text;
        if (text.Length > 1 && text[0] == '@')
        {
            text = text.Substring(1);
        }

        if (!IsValidLoopVariableName(text))
        {
            return "";
        }

        if (expression.ExpressionKind != TemplateExpressionSyntaxKind.Variable && expression.ExpressionKind != TemplateExpressionSyntaxKind.Path)
        {
            return "";
        }

        return text;
    }

    internal int GetAssignmentValueIndex(TemplateStatementSyntax statement)
    {
        if (statement.Arguments.Count < 2)
        {
            return -1;
        }

        if (statement.Arguments[1].Text == "=")
        {
            if (statement.Arguments.Count < 3)
            {
                return -1;
            }

            return 2;
        }

        return 1;
    }

    internal void RenderBlock(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        context.Writer.Write(RenderBlockToString(block, context));
    }

    internal string RenderBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        bool handlebarsScope = context.UseHandlebarsCompatibility && block.Name != "handlebarsInlinePartial";
        if (handlebarsScope)
        {
            context.PushHandlebarsInlinePartialScope();
        }

        string output = RenderBlockCoreToString(block, context);
        if (handlebarsScope)
        {
            context.PopHandlebarsInlinePartialScope();
        }

        return output;
    }

    internal string RenderBlockCoreToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (block.Name == "if")
        {
            return RenderIfBlockToString(block, context, false);
        }

        if (block.Name == "unless")
        {
            return RenderIfBlockToString(block, context, true);
        }

        if (block.Name == "case")
        {
            return RenderCaseBlockToString(block, context);
        }

        if (block.Name == "for")
        {
            return RenderForBlockToString(block, context);
        }

        if (block.Name == "liquidTableRow")
        {
            return RenderLiquidTableRowToString(block, context);
        }

        if (block.Name == "while")
        {
            return RenderWhileBlockToString(block, context);
        }

        if (block.Name == "with")
        {
            return RenderWithBlockToString(block, context);
        }

        if (block.Name == "capture")
        {
            return RenderCaptureBlockToString(block, context);
        }

        if (block.Name == "block")
        {
            return RenderTemplateBlockToString(block, context);
        }

        if (block.Name == "macro")
        {
            return RenderMacroBlockToString(block, context);
        }

        if (block.Name == "filter")
        {
            return RenderFilterBlockToString(block, context);
        }

        if (block.Name == "call")
        {
            return RenderCallBlockToString(block, context);
        }

        if (block.Name == "mustacheSection")
        {
            return RenderMustacheSectionBlockToString(block, context);
        }

        if (block.Name == "mustacheInverted")
        {
            return RenderMustacheInvertedBlockToString(block, context);
        }

        if (block.Name == "handlebarsBlock")
        {
            return RenderHandlebarsBlockHelperToString(block, context);
        }

        if (block.Name == "handlebarsEach")
        {
            return RenderHandlebarsEachBlockToString(block, context);
        }

        if (block.Name == "handlebarsPartialBlock")
        {
            return RenderHandlebarsPartialBlockToString(block, context);
        }

        if (block.Name == "handlebarsInlinePartial")
        {
            return RenderHandlebarsInlinePartialToString(block, context);
        }

        if (block.Name == "jinjaAutoescape")
        {
            return RenderJinjaAutoescapeBlockToString(block, context);
        }

        RenderStatementToString(block.StartStatement, context);
        return RenderBlockChildrenToString(block, context);
    }

    internal string RenderMustacheSectionBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        TemplateValue value = EvaluateFirstBlockArgument(block, context);
        if (value.Kind == TemplateValueKind.Function)
        {
            return RenderMustacheLambdaSectionToString(value, block, context);
        }

        if (!TemplateTruthiness.IsTruthy(value, context.Options))
        {
            return "";
        }

        if (value.Kind == TemplateValueKind.List || value.Kind == TemplateValueKind.Map || value.Kind == TemplateValueKind.Fragment)
        {
            return RenderMustacheCollectionSectionToString(value, block, context);
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return RenderBlockChildrenToString(block, context);
        }

        context.Scope.PushFrame(TemplateScopeFrameKind.Local, "", value);
        string output = RenderBlockChildrenToString(block, context);
        context.Scope.PopFrame();
        return output;
    }

    internal string RenderMustacheInvertedBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        TemplateValue value = EvaluateFirstBlockArgument(block, context);
        if (TemplateTruthiness.IsTruthy(value, context.Options))
        {
            return "";
        }

        return RenderBlockChildrenToString(block, context);
    }

    internal TemplateValue EvaluateFirstBlockArgument(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (block.StartStatement.Arguments.Count == 0)
        {
            return TemplateValue.Missing();
        }

        return context.Evaluator.Evaluate(block.StartStatement.Arguments[0]);
    }

    internal string RenderMustacheCollectionSectionToString(TemplateValue value, TemplateBlockSyntax block, TemplateRenderContext context)
    {
        List<TemplateLoopIteration> iterations = CreateLoopIterations(value);
        string output = "";
        int index = 0;
        while (index < iterations.Count)
        {
            context.CountLoopIteration();
            TemplateLoopIteration iteration = iterations[index];
            context.Scope.PushLoopScope(iteration.CurrentValue);
            output = AppendStringOutput(context, output, RenderBlockChildrenToString(block, context));
            context.Scope.PopFrame();
            index = index + 1;
        }

        return output;
    }

    internal string RenderMustacheLambdaSectionToString(TemplateValue value, TemplateBlockSyntax block, TemplateRenderContext context)
    {
        context.EnterRecursion();
        List<TemplateValue> arguments = new List<TemplateValue>();
        arguments.Add(TemplateValue.FromSafeString(RenderBlockChildrenToString(block, context)));
        TemplateValue result = value.Invoke(arguments);
        string text = TemplateOutputEncoder.ConvertValueToText(result);
        if (text == "")
        {
            context.ExitRecursion();
            return "";
        }

        TemplateSource source = TemplateSource.FromString(MustacheTemplateConverter.ConvertToNative(text));
        TemplateCompiledTemplate compiled = CompileSourceForContext(source, context.Options, context);
        string output = RenderNodesToString(compiled.Document.Children, context);
        context.ExitRecursion();
        return output;
    }

    internal string RenderHandlebarsBlockHelperToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (block.StartStatement.Arguments.Count == 0)
        {
            return RenderBlockChildrenToString(block, context);
        }

        TemplateExpressionSyntax callExpression = block.StartStatement.Arguments[0];
        string helperName = GetCallableName(callExpression, context);
        if (helperName == "")
        {
            return RenderBlockChildrenToString(block, context);
        }

        List<TemplateValue> arguments = new List<TemplateValue>();
        AddBlockCallArguments(arguments, callExpression, block.StartStatement, 1, context);
        if (!context.Registry.BlockHelpers.ContainsKey(helperName))
        {
            TemplateValue scopedValue = context.Evaluator.Evaluate(callExpression);
            if (scopedValue.Kind != TemplateValueKind.Missing)
            {
                return RenderHandlebarsScopedBlockToString(scopedValue, block, context);
            }

            if (context.Options.StrictUnknownFunctions)
            {
                throw new InvalidOperationException("Unknown template block helper '" + helperName + "'.");
            }

            return RenderHandlebarsElseBodyToString(block, context);
        }

        TemplateBlockHelperContext helperContext = new TemplateBlockHelperContext(arguments, block.Children, block.ElseChildren, context);
        TemplateValue result = context.Registry.BlockHelpers[helperName](helperContext);
        if (result == null)
        {
            throw new InvalidOperationException("Template block helper returned null.");
        }

        return EncodeReusableResult(result, context);
    }

    internal string RenderHandlebarsEachBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        TemplateValue iterable = EvaluateFirstBlockArgument(block, context);
        List<TemplateLoopIteration> iterations = CreateLoopIterations(iterable);
        if (iterations.Count == 0)
        {
            return RenderHandlebarsElseBodyToString(block, context);
        }

        string itemAlias = GetOptionalStringArgument(block, context, 1);
        string indexAlias = GetOptionalStringArgument(block, context, 2);
        string output = "";
        int index = 0;
        while (index < iterations.Count)
        {
            context.CountLoopIteration();
            TemplateLoopIteration iteration = iterations[index];
            TemplateScopeFrame frame = context.Scope.PushLoopScope(iteration.CurrentValue);
            BindHandlebarsEachLocals(frame, iteration, index, iterations.Count, itemAlias, indexAlias, context);
            output = AppendStringOutput(context, output, RenderBlockChildrenToString(block, context));
            context.Scope.PopFrame();
            index = index + 1;
        }

        return output;
    }

    internal void BindHandlebarsEachLocals(TemplateScopeFrame frame, TemplateLoopIteration iteration, int index, int count, string itemAlias, string indexAlias, TemplateRenderContext context)
    {
        frame.SetLocal("parentIndex", context.Scope.GetLocal("index"));
        frame.SetLocal("parentKey", context.Scope.GetLocal("key"));
        frame.SetLocal("parentFirst", context.Scope.GetLocal("first"));
        frame.SetLocal("parentLast", context.Scope.GetLocal("last"));
        frame.SetLocal("index", TemplateValue.FromNumber(index));
        frame.SetLocal("key", iteration.Key);
        frame.SetLocal("value", iteration.Value);
        frame.SetLocal("first", TemplateValue.FromBoolean(index == 0));
        frame.SetLocal("last", TemplateValue.FromBoolean(index + 1 == count));
        if (itemAlias != "")
        {
            frame.SetLocal(itemAlias, iteration.CurrentValue);
        }

        if (indexAlias != "")
        {
            frame.SetLocal(indexAlias, TemplateValue.FromNumber(index));
        }
    }

    internal string RenderHandlebarsScopedBlockToString(TemplateValue value, TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (!TemplateTruthiness.IsTruthy(value, context.Options))
        {
            return RenderHandlebarsElseBodyToString(block, context);
        }

        if (value.Kind == TemplateValueKind.List || value.Kind == TemplateValueKind.Fragment)
        {
            List<TemplateLoopIteration> iterations = CreateLoopIterations(value);
            string output = "";
            int index = 0;
            while (index < iterations.Count)
            {
                context.CountLoopIteration();
                context.Scope.PushLoopScope(iterations[index].CurrentValue);
                output = AppendStringOutput(context, output, RenderBlockChildrenToString(block, context));
                context.Scope.PopFrame();
                index = index + 1;
            }

            return output;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return RenderBlockChildrenToString(block, context);
        }

        context.Scope.PushFrame(TemplateScopeFrameKind.Local, "", value);
        string scopedOutput = RenderBlockChildrenToString(block, context);
        context.Scope.PopFrame();
        return scopedOutput;
    }

    internal string RenderHandlebarsPartialBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (block.StartStatement.Arguments.Count == 0)
        {
            return RenderBlockChildrenToString(block, context);
        }

        string partialName = TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(block.StartStatement.Arguments[0]));
        if (partialName == "")
        {
            return "";
        }

        TemplateSource? source = LoadHandlebarsPartial(partialName, context);
        if (source == null)
        {
            if (context.Options.StrictMissingValues)
            {
                throw new InvalidOperationException("Partial '" + partialName + "' was not found.");
            }

            return RenderBlockChildrenToString(block, context);
        }

        context.RecordDependency(TemplateDependencyKind.Partial, source);
        TemplateCompiledTemplate compiled = CompileSourceForContext(source, context.Options, context);
        TemplateCompositionArguments compositionArguments = EvaluateCompositionArguments(block.StartStatement, context, false);
        compositionArguments.Parameters["partialBlock"] = TemplateValue.FromFragment(block.Children);
        compositionArguments.Parameters["partialBlockContext"] = context.Scope.GetCurrentValue();
        return RenderInheritedCompositionToString(compiled.Document, "partial", source.Name, compositionArguments, context);
    }

    internal string RenderHandlebarsPartialBlockBodyToString(TemplateRenderContext context)
    {
        TemplateValue partialBlock = context.Scope.GetLocal("partialBlock");
        if (partialBlock.Kind != TemplateValueKind.Fragment)
        {
            return "";
        }

        TemplateValue partialBlockContext = context.Scope.GetLocal("partialBlockContext");
        context.HandlebarsPartialBlockContexts.Add(partialBlockContext);
        string output = RenderNodesToString(partialBlock.FragmentValue, context);
        context.HandlebarsPartialBlockContexts.RemoveAt(context.HandlebarsPartialBlockContexts.Count - 1);
        return output;
    }

    internal string RenderHandlebarsInlinePartialToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        string partialName = GetOptionalStringArgument(block, context, 0);
        if (partialName == "")
        {
            return "";
        }

        string content = GetHandlebarsInlinePartialSource(block);
        string contentHash = TemplateSource.ComputeContentHash(content);
        context.SetHandlebarsInlinePartial(partialName, new TemplateSource(partialName, null, null, contentHash, content));
        return "";
    }

    internal string GetHandlebarsInlinePartialSource(TemplateBlockSyntax block)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < block.Children.Count)
        {
            builder.Append(block.Children[index].Span.GetText());
            index = index + 1;
        }

        return builder.ToString();
    }

    internal string RenderHandlebarsElseBodyToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        int index = 0;
        while (index < block.ElseChildren.Count)
        {
            TemplateSyntaxNode node = block.ElseChildren[index];
            if (node.Statement != null && node.Statement.Name == "else")
            {
                return RenderBlockRangeToString(block, index + 1, block.ElseChildren.Count, context);
            }

            index = index + 1;
        }

        return "";
    }

    internal string GetOptionalStringArgument(TemplateBlockSyntax block, TemplateRenderContext context, int index)
    {
        if (index < 0 || index >= block.StartStatement.Arguments.Count)
        {
            return "";
        }

        return TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(block.StartStatement.Arguments[index]));
    }

    internal string RenderJinjaAutoescapeBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        TemplateEscapingMode previousEscapingMode = context.EscapingMode;
        bool previousAutoEscape = context.AutoEscape;
        bool enabled = true;
        if (block.StartStatement.Arguments.Count > 0)
        {
            enabled = TemplateTruthiness.IsTruthy(context.Evaluator.Evaluate(block.StartStatement.Arguments[0]), context.Options);
        }

        if (enabled)
        {
            context.EscapingMode = TemplateEscapingMode.Html;
            context.AutoEscape = true;
        }
        else
        {
            context.EscapingMode = TemplateEscapingMode.None;
            context.AutoEscape = false;
        }

        string output = RenderBlockChildrenToString(block, context);
        context.EscapingMode = previousEscapingMode;
        context.AutoEscape = previousAutoEscape;
        return output;
    }

    internal string RenderTemplateBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        string blockName = GetTemplateBlockName(block, context);
        if (blockName == "")
        {
            return RenderBlockChildrenToString(block, context);
        }

        if (context.BlockOverrides.ContainsKey(blockName))
        {
            return RenderBlockOverrideToString(context.BlockOverrides[blockName], block, context);
        }

        return RenderBlockChildrenToString(block, context);
    }

    internal string RenderBlockOverrideToString(TemplateBlockOverride blockOverride, TemplateBlockSyntax parentBlock, TemplateRenderContext context)
    {
        bool hadSuper = context.Evaluator.Functions.ContainsKey("super");
        Func<List<TemplateValue>, TemplateValue>? previousSuper = null;
        if (hadSuper)
        {
            previousSuper = context.Evaluator.Functions["super"];
        }

        TemplateSuperFunction superFunction = new TemplateSuperFunction(this, blockOverride, parentBlock, context);
        context.Evaluator.Functions["super"] = superFunction.Invoke;

        string output = RenderNodesToString(blockOverride.Block.Children, context);
        if (hadSuper && previousSuper != null)
        {
            context.Evaluator.Functions["super"] = previousSuper;
        }
        else
        {
            context.Evaluator.Functions.Remove("super");
        }

        return output;
    }

    internal string RenderSuperBlockToString(TemplateBlockOverride blockOverride, TemplateBlockSyntax parentBlock, TemplateRenderContext context)
    {
        context.EnterRecursion();
        string output;
        if (blockOverride.Parent != null)
        {
            output = RenderBlockOverrideToString(blockOverride.Parent, parentBlock, context);
        }
        else
        {
            output = RenderNodesToString(parentBlock.Children, context);
        }

        context.ExitRecursion();
        return output;
    }

    internal string GetTemplateBlockName(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (block.StartStatement.Arguments.Count == 0)
        {
            return "";
        }

        TemplateExpressionSyntax expression = block.StartStatement.Arguments[0];
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable || expression.ExpressionKind == TemplateExpressionSyntaxKind.Path)
        {
            return expression.Text;
        }

        return TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(expression));
    }

    internal string RenderMacroBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        RegisterMacroBlock(block, context);
        return "";
    }

    internal void RegisterMacroBlock(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        string macroName = "";
        if (block.StartStatement.Arguments.Count > 0)
        {
            TemplateExpressionSyntax nameExpression = block.StartStatement.Arguments[0];
            if (nameExpression.ExpressionKind == TemplateExpressionSyntaxKind.Variable || nameExpression.ExpressionKind == TemplateExpressionSyntaxKind.Path)
            {
                macroName = nameExpression.Text;
            }
        }

        if (block.StartStatement.Arguments.Count == 0 || !IsMacroIdentifier(block.StartStatement.Arguments[0], macroName))
        {
            throw new InvalidOperationException("Template macro must have a valid name.");
        }

        ValidateMacroParameters(block, macroName);

        TemplateMacroFunction macroFunction = new TemplateMacroFunction(this, block, context);
        context.Evaluator.Functions[macroName] = macroFunction.Invoke;
    }

    internal TemplateValue RenderMacroToValue(TemplateBlockSyntax block, TemplateRenderContext context, List<TemplateValue> arguments)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (arguments == null)
        {
            throw new ArgumentNullException("arguments");
        }

        context.EnterRecursion();
        string previousSignal = context.FlowControlSignal;
        TemplateValue previousReturnValue = context.ReturnValue;
        context.FlowControlSignal = "";
        context.ReturnValue = TemplateValue.Missing();
        string macroName = GetMacroName(block, context);
        ValidateMacroArguments(block, macroName, arguments);
        TemplateScopeFrame frame = context.Scope.PushMacroScope(macroName);
        BindMacroArguments(frame, block, context, arguments);
        string output = RenderBlockChildrenToString(block, context);
        context.Scope.PopFrame();
        string macroSignal = context.FlowControlSignal;
        TemplateValue macroReturnValue = context.ReturnValue;
        context.FlowControlSignal = previousSignal;
        context.ReturnValue = previousReturnValue;
        context.ExitRecursion();
        if (macroSignal == "return")
        {
            return macroReturnValue;
        }

        return TemplateValue.FromSafeString(output);
    }

    internal void BindMacroArguments(TemplateScopeFrame frame, TemplateBlockSyntax block, TemplateRenderContext context, List<TemplateValue> arguments)
    {
        TemplateValue namedArguments = GetMacroNamedArguments(block, arguments);
        int positionalCount = arguments.Count;
        if (!namedArguments.IsMissing())
        {
            positionalCount = positionalCount - 1;
        }

        int parameterIndex = 1;
        int argumentIndex = 0;
        while (parameterIndex < block.StartStatement.Arguments.Count)
        {
            string parameterName = GetMacroArgumentName(block.StartStatement.Arguments[parameterIndex], context);
            if (parameterName != "")
            {
                if (argumentIndex < positionalCount)
                {
                    frame.SetLocal(parameterName, arguments[argumentIndex]);
                }
                else if (!namedArguments.IsMissing() && namedArguments.MapValue.ContainsKey(parameterName))
                {
                    frame.SetLocal(parameterName, namedArguments.MapValue[parameterName]);
                }
                else
                {
                    frame.SetLocal(parameterName, TemplateValue.Missing());
                }

                argumentIndex = argumentIndex + 1;
            }

            parameterIndex = parameterIndex + 1;
        }

        int defaultIndex = 0;
        while (defaultIndex < block.StartStatement.NamedArguments.Count)
        {
            TemplateNamedArgumentSyntax parameter = block.StartStatement.NamedArguments[defaultIndex];
            if (argumentIndex < positionalCount)
            {
                frame.SetLocal(parameter.Name, arguments[argumentIndex]);
            }
            else if (!namedArguments.IsMissing() && namedArguments.MapValue.ContainsKey(parameter.Name))
            {
                frame.SetLocal(parameter.Name, namedArguments.MapValue[parameter.Name]);
            }
            else
            {
                frame.SetLocal(parameter.Name, context.Evaluator.Evaluate(parameter.Value));
            }

            argumentIndex = argumentIndex + 1;
            defaultIndex = defaultIndex + 1;
        }
    }

    internal void ValidateMacroParameters(TemplateBlockSyntax block, string macroName)
    {
        List<string> parameterNames = new List<string>();
        int index = 1;
        while (index < block.StartStatement.Arguments.Count)
        {
            TemplateExpressionSyntax expression = block.StartStatement.Arguments[index];
            string parameterName = "";
            if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable || expression.ExpressionKind == TemplateExpressionSyntaxKind.Path)
            {
                parameterName = expression.Text;
            }

            if (!IsValidLoopVariableName(parameterName))
            {
                throw new InvalidOperationException("Template macro '" + macroName + "' has an invalid parameter name.");
            }

            if (parameterNames.Contains(parameterName))
            {
                throw new InvalidOperationException("Template macro '" + macroName + "' has duplicate parameter '" + parameterName + "'.");
            }

            parameterNames.Add(parameterName);
            index = index + 1;
        }

        int defaultIndex = 0;
        while (defaultIndex < block.StartStatement.NamedArguments.Count)
        {
            string parameterName = block.StartStatement.NamedArguments[defaultIndex].Name;
            if (!IsValidLoopVariableName(parameterName))
            {
                throw new InvalidOperationException("Template macro '" + macroName + "' has an invalid parameter name.");
            }

            if (parameterNames.Contains(parameterName))
            {
                throw new InvalidOperationException("Template macro '" + macroName + "' has duplicate parameter '" + parameterName + "'.");
            }

            parameterNames.Add(parameterName);
            defaultIndex = defaultIndex + 1;
        }
    }

    internal bool IsMacroIdentifier(TemplateExpressionSyntax expression, string name)
    {
        if (expression.ExpressionKind != TemplateExpressionSyntaxKind.Variable && expression.ExpressionKind != TemplateExpressionSyntaxKind.Path)
        {
            return false;
        }

        return IsValidLoopVariableName(name);
    }

    internal void ValidateMacroArguments(TemplateBlockSyntax block, string macroName, List<TemplateValue> arguments)
    {
        int requiredCount = block.StartStatement.Arguments.Count - 1;
        int parameterCount = requiredCount + block.StartStatement.NamedArguments.Count;
        TemplateValue namedArguments = GetMacroNamedArguments(block, arguments);
        int positionalCount = arguments.Count;
        if (!namedArguments.IsMissing())
        {
            positionalCount = positionalCount - 1;
        }

        if (namedArguments.IsMissing() && block.StartStatement.NamedArguments.Count == 0 && positionalCount != requiredCount)
        {
            throw new InvalidOperationException("Template macro '" + macroName + "' expects " + requiredCount + " arguments but received " + positionalCount + ".");
        }

        if (positionalCount > parameterCount)
        {
            throw new InvalidOperationException("Template macro '" + macroName + "' expects at most " + parameterCount + " arguments but received " + positionalCount + ".");
        }

        int requiredIndex = positionalCount + 1;
        while (requiredIndex <= requiredCount)
        {
            string parameterName = block.StartStatement.Arguments[requiredIndex].Text;
            if (namedArguments.IsMissing() || !namedArguments.MapValue.ContainsKey(parameterName))
            {
                throw new InvalidOperationException("Template macro '" + macroName + "' is missing required parameter '" + parameterName + "'.");
            }

            requiredIndex = requiredIndex + 1;
        }

        if (!namedArguments.IsMissing())
        {
            foreach (KeyValuePair<string, TemplateValue> pair in namedArguments.MapValue)
            {
                if (!IsMacroParameterName(block, pair.Key))
                {
                    throw new InvalidOperationException("Template macro '" + macroName + "' does not declare parameter '" + pair.Key + "'.");
                }
            }
        }
    }

    internal TemplateValue GetMacroNamedArguments(TemplateBlockSyntax block, List<TemplateValue> arguments)
    {
        if (arguments.Count == 0)
        {
            return TemplateValue.Missing();
        }

        TemplateValue candidate = arguments[arguments.Count - 1];
        if (candidate.Kind != TemplateValueKind.Map)
        {
            return TemplateValue.Missing();
        }

        foreach (KeyValuePair<string, TemplateValue> pair in candidate.MapValue)
        {
            if (IsMacroParameterName(block, pair.Key))
            {
                return candidate;
            }
        }

        return TemplateValue.Missing();
    }

    internal bool IsMacroParameterName(TemplateBlockSyntax block, string name)
    {
        int index = 1;
        while (index < block.StartStatement.Arguments.Count)
        {
            if (block.StartStatement.Arguments[index].Text == name)
            {
                return true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < block.StartStatement.NamedArguments.Count)
        {
            if (block.StartStatement.NamedArguments[index].Name == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string GetMacroName(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (block.StartStatement.Arguments.Count == 0)
        {
            return "";
        }

        return GetMacroArgumentName(block.StartStatement.Arguments[0], context);
    }

    internal string GetMacroArgumentName(TemplateExpressionSyntax expression, TemplateRenderContext context)
    {
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable || expression.ExpressionKind == TemplateExpressionSyntaxKind.Path)
        {
            return expression.Text;
        }

        return TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(expression));
    }

    internal string RenderFilterBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        string body = RenderBlockChildrenToString(block, context);
        if (context.FlowControlSignal != "")
        {
            return body;
        }

        if (block.StartStatement.Arguments.Count == 0)
        {
            return body;
        }

        TemplateExpressionSyntax filterExpression = block.StartStatement.Arguments[0];
        string filterName = GetCallableName(filterExpression, context);
        if (filterName == "")
        {
            return body;
        }

        List<TemplateValue> arguments = new List<TemplateValue>();
        arguments.Add(TemplateValue.FromSafeString(body));
        AddBlockCallArguments(arguments, filterExpression, block.StartStatement, 1, context);
        if (context.Evaluator.Filters.ContainsKey(filterName))
        {
            TemplateValue result = context.Evaluator.Filters[filterName](arguments);
            if (result == null)
            {
                throw new InvalidOperationException("Template filter returned null.");
            }

            return EncodeReusableResult(result, context);
        }

        if (context.Options.StrictUnknownFunctions)
        {
            throw new InvalidOperationException("Unknown template filter '" + filterName + "'.");
        }

        return body;
    }

    internal string RenderCallBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        if (block.StartStatement.Arguments.Count == 0)
        {
            return RenderBlockChildrenToString(block, context);
        }

        string body = RenderBlockChildrenToString(block, context);
        if (context.FlowControlSignal != "")
        {
            return body;
        }

        TemplateExpressionSyntax callExpression = block.StartStatement.Arguments[0];
        string callableName = GetCallableName(callExpression, context);
        if (callableName == "")
        {
            return body;
        }

        List<TemplateValue> arguments = new List<TemplateValue>();
        AddBlockCallArguments(arguments, callExpression, block.StartStatement, 1, context);
        if (context.UseJinjaCompatibility)
        {
            arguments.Add(TemplateValue.FromFunction((List<TemplateValue> callerArguments) =>
            {
                return TemplateValue.FromSafeString(body);
            }));
        }
        else
        {
            arguments.Add(TemplateValue.FromSafeString(body));
        }
        TemplateValue result = InvokeFunctionForCallBlock(callableName, arguments, context);
        if (result.IsMissing())
        {
            return "";
        }

        return EncodeReusableResult(result, context);
    }

    internal void AddBlockCallArguments(List<TemplateValue> arguments, TemplateExpressionSyntax callExpression, TemplateStatementSyntax statement, int startIndex, TemplateRenderContext context)
    {
        if (callExpression.ExpressionKind == TemplateExpressionSyntaxKind.Call)
        {
            int index = 0;
            while (index < callExpression.Arguments.Count)
            {
                arguments.Add(context.Evaluator.Evaluate(callExpression.Arguments[index]));
                index = index + 1;
            }

            context.Evaluator.AddCallArgumentMap(arguments, callExpression.NamedArguments, callExpression.HashArguments);

            return;
        }

        int statementIndex = startIndex;
        while (statementIndex < statement.Arguments.Count)
        {
            arguments.Add(context.Evaluator.Evaluate(statement.Arguments[statementIndex]));
            statementIndex = statementIndex + 1;
        }

        context.Evaluator.AddCallArgumentMap(arguments, statement.NamedArguments, statement.HashArguments);
    }

    internal string GetCallableName(TemplateExpressionSyntax expression, TemplateRenderContext context)
    {
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Call)
        {
            return expression.Name;
        }

        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Variable || expression.ExpressionKind == TemplateExpressionSyntaxKind.Path)
        {
            return expression.Text;
        }

        return TemplateOutputEncoder.ConvertValueToText(context.Evaluator.Evaluate(expression));
    }

    internal TemplateValue InvokeFunctionForCallBlock(string name, List<TemplateValue> arguments, TemplateRenderContext context)
    {
        if (context.Evaluator.Functions.ContainsKey(name))
        {
            TemplateValue result = context.Evaluator.Functions[name](arguments);
            if (result == null)
            {
                throw new InvalidOperationException("Template function returned null.");
            }

            return result;
        }

        TemplateValue scoped = context.Evaluator.ResolveVariable(name, null);
        if (scoped.Kind == TemplateValueKind.Function)
        {
            return scoped.Invoke(arguments);
        }

        if (context.Options.StrictUnknownFunctions)
        {
            throw new InvalidOperationException("Unknown template function '" + name + "'.");
        }

        return TemplateValue.Missing();
    }

    internal string EncodeReusableResult(TemplateValue result, TemplateRenderContext context)
    {
        return EncodeOutputValue(result, context, false);
    }

    internal string EncodeOutputValue(TemplateValue value, TemplateRenderContext context, bool raw)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        string text = TemplateOutputEncoder.ConvertValueToText(value);
        if (raw || value.IsSafeString() || !context.AutoEscape || context.EscapingMode == TemplateEscapingMode.None)
        {
            return text;
        }

        if (context.Options.OutputEncoder == null)
        {
            if (context.UseHandlebarsCompatibility && context.EscapingMode == TemplateEscapingMode.Html)
            {
                return TemplateOutputEncoder.EncodeHandlebarsHtml(text);
            }

            return TemplateOutputEncoder.Encode(text, context.EscapingMode);
        }

        string encoded = context.Options.OutputEncoder.Encode(text, context.EscapingMode);
        if (encoded == null)
        {
            throw new InvalidOperationException("The configured template output encoder returned null.");
        }

        return encoded;
    }

    internal string RenderIfBlockToString(TemplateBlockSyntax block, TemplateRenderContext context, bool invertStartCondition)
    {
        if (EvaluateCondition(block.StartStatement, context, invertStartCondition))
        {
            return RenderBlockChildrenToString(block, context);
        }

        int index = 0;
        while (index < block.ElseChildren.Count)
        {
            TemplateSyntaxNode node = block.ElseChildren[index];
            if (!IsConditionalBranchNode(node))
            {
                index = index + 1;
            }
            else
            {
                int nextBranch = FindNextConditionalBranch(block.ElseChildren, index + 1);
                if (node.Statement != null && node.Statement.Name == "else")
                {
                    return RenderBlockRangeToString(block, index + 1, nextBranch, context);
                }

                if (node.Statement != null && node.Statement.Name == "elseif")
                {
                    if (EvaluateCondition(node.Statement, context, false))
                    {
                        return RenderBlockRangeToString(block, index + 1, nextBranch, context);
                    }
                }

                index = nextBranch;
            }
        }

        return "";
    }

    internal string RenderCaseBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        TemplateValue caseValue = TemplateValue.Missing();
        if (block.StartStatement.Arguments.Count > 0)
        {
            caseValue = context.Evaluator.Evaluate(block.StartStatement.Arguments[0]);
        }

        int elseStart = -1;
        int elseEnd = -1;
        int index = 0;
        while (index < block.ElseChildren.Count)
        {
            TemplateSyntaxNode node = block.ElseChildren[index];
            if (!IsCaseBranchNode(node))
            {
                index = index + 1;
            }
            else
            {
                int nextBranch = FindNextCaseBranch(block.ElseChildren, index + 1);
                if (node.Statement != null && node.Statement.Name == "when")
                {
                    if (WhenMatchesCase(caseValue, node.Statement, context))
                    {
                        return RenderBlockRangeToString(block, index + 1, nextBranch, context);
                    }
                }
                else if (node.Statement != null && node.Statement.Name == "else")
                {
                    if (elseStart < 0)
                    {
                        elseStart = index + 1;
                        elseEnd = nextBranch;
                    }
                }

                index = nextBranch;
            }
        }

        if (elseStart >= 0)
        {
            return RenderBlockRangeToString(block, elseStart, elseEnd, context);
        }

        return "";
    }

    internal string RenderForBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        TemplateForLoopSpec spec = ParseForLoopSpec(block.StartStatement);
        if (!spec.IsValid())
        {
            RenderStatementToString(block.StartStatement, context);
            return "";
        }

        TemplateValue iterable = context.Evaluator.Evaluate(block.StartStatement.Arguments[spec.IterableArgumentIndex]);
        List<TemplateLoopIteration> iterations = CreateLoopIterations(iterable);
        TemplateForLoopOptions options = EvaluateForLoopOptions(block.StartStatement, spec, context);
        string liquidOffsetKey = GetLiquidOffsetKey(block.StartStatement, spec);
        if (context.UseLiquidCompatibility && options.ContinueOffset && context.LiquidForOffsets.ContainsKey(liquidOffsetKey))
        {
            options.Offset = context.LiquidForOffsets[liquidOffsetKey];
        }

        iterations = ApplyForLoopOptions(iterations, options);
        if (context.UseLiquidCompatibility)
        {
            context.LiquidForOffsets[liquidOffsetKey] = options.Offset + iterations.Count;
        }
        if (iterations.Count == 0)
        {
            string emptyOutput = RenderForEmptyBranchToString(block, context);
            if (context.FlowControlSignal == "break" || context.FlowControlSignal == "continue")
            {
                context.FlowControlSignal = "";
            }

            return emptyOutput;
        }

        string output = "";
        int index = 0;
        while (index < iterations.Count)
        {
            context.CountLoopIteration();
            TemplateLoopIteration iteration = iterations[index];
            TemplateValue parentLoop = context.Scope.GetLocal("loop");
            TemplateScopeFrame frame = context.Scope.PushLoopScope(iteration.CurrentValue);
            BindLoopVariables(frame, spec, iteration, index, iterations.Count, parentLoop);
            output = AppendStringOutput(context, output, RenderBlockChildrenToString(block, context));
            string signal = context.FlowControlSignal;
            context.Scope.PopFrame();
            if (signal == "break")
            {
                context.FlowControlSignal = "";
                break;
            }

            if (signal == "continue")
            {
                context.FlowControlSignal = "";
                index = index + 1;
                continue;
            }

            if (signal != "")
            {
                break;
            }

            index = index + 1;
        }

        return output;
    }

    internal string RenderLiquidTableRowToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        TemplateForLoopSpec spec = ParseForLoopSpec(block.StartStatement);
        if (!spec.IsValid())
        {
            return "";
        }

        TemplateValue iterable = context.Evaluator.Evaluate(block.StartStatement.Arguments[spec.IterableArgumentIndex]);
        List<TemplateLoopIteration> iterations = CreateLoopIterations(iterable);
        TemplateForLoopOptions options = EvaluateForLoopOptions(block.StartStatement, spec, context);
        iterations = ApplyForLoopOptions(iterations, options);
        int columns = iterations.Count;
        if (options.HasColumns)
        {
            columns = options.Columns;
        }

        if (columns < 1)
        {
            columns = 1;
        }

        StringBuilder output = new StringBuilder();
        output.Append("<tr class=\"row1\">\n");
        int index = 0;
        while (index < iterations.Count)
        {
            context.CountLoopIteration();
            int columnIndex = index % columns;
            int rowIndex = index / columns;
            if (index > 0 && columnIndex == 0)
            {
                output.Append("</tr>\n<tr class=\"row");
                output.Append((rowIndex + 1).ToString(CultureInfo.InvariantCulture));
                output.Append("\">");
            }

            TemplateLoopIteration iteration = iterations[index];
            TemplateScopeFrame frame = context.Scope.PushLoopScope(iteration.CurrentValue);
            if (spec.VariableNames.Count > 0)
            {
                frame.SetLocal(spec.VariableNames[0], iteration.CurrentValue);
            }

            frame.SetLocal("key", iteration.Key);
            frame.SetLocal("value", iteration.Value);
            frame.SetLocal("tablerowloop", CreateTableRowMetadata(iteration, index, iterations.Count, columnIndex, rowIndex, columns));
            string body = RenderBlockChildrenToString(block, context);
            string signal = context.FlowControlSignal;
            context.Scope.PopFrame();
            output.Append("<td class=\"col");
            output.Append((columnIndex + 1).ToString(CultureInfo.InvariantCulture));
            output.Append("\">");
            output.Append(body);
            output.Append("</td>");
            if (signal == "break")
            {
                context.FlowControlSignal = "";
                break;
            }

            if (signal == "continue")
            {
                context.FlowControlSignal = "";
            }
            else if (signal != "")
            {
                break;
            }

            index = index + 1;
        }

        output.Append("</tr>\n");
        return output.ToString();
    }

    internal TemplateValue CreateTableRowMetadata(TemplateLoopIteration iteration, int index, int length, int columnIndex, int rowIndex, int columns)
    {
        TemplateValue metadata = CreateLoopMetadata(iteration, index, length, TemplateValue.Missing());
        metadata.MapValue["col"] = TemplateValue.FromNumber(columnIndex + 1);
        metadata.MapValue["col0"] = TemplateValue.FromNumber(columnIndex);
        metadata.MapValue["row"] = TemplateValue.FromNumber(rowIndex + 1);
        metadata.MapValue["col_first"] = TemplateValue.FromBoolean(columnIndex == 0);
        metadata.MapValue["col_last"] = TemplateValue.FromBoolean(columnIndex + 1 == columns || index + 1 == length);
        return metadata;
    }

    internal string RenderWhileBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        string output = "";
        while (EvaluateCondition(block.StartStatement, context, false))
        {
            context.CountLoopIteration();
            output = AppendStringOutput(context, output, RenderBlockChildrenToString(block, context));
            string signal = context.FlowControlSignal;
            if (signal == "break")
            {
                context.FlowControlSignal = "";
                break;
            }

            if (signal == "continue")
            {
                context.FlowControlSignal = "";
                continue;
            }

            if (signal != "")
            {
                break;
            }
        }

        return output;
    }

    internal string RenderWithBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        TemplateValue current = TemplateValue.Missing();
        if (block.StartStatement.Arguments.Count > 0)
        {
            current = context.Evaluator.Evaluate(block.StartStatement.Arguments[0]);
        }

        if (!TemplateTruthiness.IsTruthy(current, context.Options))
        {
            return RenderHandlebarsElseBodyToString(block, context);
        }

        TemplateScopeFrame frame = context.Scope.PushFrame(TemplateScopeFrameKind.Local, "", current);
        if (context.UseHandlebarsCompatibility && block.StartStatement.BlockParameters.Count > 0)
        {
            frame.SetLocal(block.StartStatement.BlockParameters[0].Name, current);
        }

        string output = RenderBlockChildrenToString(block, context);
        context.Scope.PopFrame();
        return output;
    }

    internal string RenderCaptureBlockToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        string name = GetAssignmentVariableName(block.StartStatement);
        if (name == "")
        {
            return "";
        }

        string output = RenderBlockChildrenToString(block, context);
        if (context.UseLiquidCompatibility)
        {
            context.Scope.SetLiquidLocal(name, TemplateValue.FromString(output));
        }
        else
        {
            context.Scope.SetLocal(name, TemplateValue.FromString(output));
        }
        return "";
    }

    internal string RenderForEmptyBranchToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        int index = 0;
        while (index < block.ElseChildren.Count)
        {
            TemplateSyntaxNode node = block.ElseChildren[index];
            if (!IsForEmptyBranchNode(node))
            {
                index = index + 1;
            }
            else
            {
                int nextBranch = FindNextForEmptyBranch(block.ElseChildren, index + 1);
                return RenderBlockRangeToString(block, index + 1, nextBranch, context);
            }
        }

        return "";
    }

    internal bool EvaluateCondition(TemplateStatementSyntax statement, TemplateRenderContext context, bool invert)
    {
        bool result = false;
        if (statement.Arguments.Count > 0)
        {
            TemplateValue value = context.Evaluator.Evaluate(statement.Arguments[0]);
            result = TemplateTruthiness.IsTruthy(value, context.Options);
        }

        if (invert)
        {
            return !result;
        }

        return result;
    }

    internal int FindNextConditionalBranch(List<TemplateSyntaxNode> nodes, int startIndex)
    {
        int index = startIndex;
        while (index < nodes.Count)
        {
            if (IsConditionalBranchNode(nodes[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return nodes.Count;
    }

    internal bool IsConditionalBranchNode(TemplateSyntaxNode node)
    {
        if (node.Kind != TemplateSyntaxKind.Statement || node.Statement == null)
        {
            return false;
        }

        return node.Statement.Name == "elseif" || node.Statement.Name == "else";
    }

    internal int FindNextCaseBranch(List<TemplateSyntaxNode> nodes, int startIndex)
    {
        int index = startIndex;
        while (index < nodes.Count)
        {
            if (IsCaseBranchNode(nodes[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return nodes.Count;
    }

    internal bool IsCaseBranchNode(TemplateSyntaxNode node)
    {
        if (node.Kind != TemplateSyntaxKind.Statement || node.Statement == null)
        {
            return false;
        }

        return node.Statement.Name == "when" || node.Statement.Name == "else";
    }

    internal int FindNextForEmptyBranch(List<TemplateSyntaxNode> nodes, int startIndex)
    {
        int index = startIndex;
        while (index < nodes.Count)
        {
            if (IsForEmptyBranchNode(nodes[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return nodes.Count;
    }

    internal bool IsForEmptyBranchNode(TemplateSyntaxNode node)
    {
        if (node.Kind != TemplateSyntaxKind.Statement || node.Statement == null)
        {
            return false;
        }

        return node.Statement.Name == "empty" || node.Statement.Name == "else";
    }

    internal TemplateForLoopSpec ParseForLoopSpec(TemplateStatementSyntax statement)
    {
        TemplateForLoopSpec spec = new TemplateForLoopSpec();
        int inIndex = FindForInArgumentIndex(statement);
        if (inIndex <= 0 || inIndex + 1 >= statement.Arguments.Count)
        {
            return spec;
        }

        int index = 0;
        while (index < inIndex)
        {
            string variableName = GetLoopVariableName(statement.Arguments[index]);
            if (variableName == "")
            {
                return new TemplateForLoopSpec();
            }

            spec.VariableNames.Add(variableName);
            index = index + 1;
        }

        spec.IterableArgumentIndex = inIndex + 1;
        AddForLoopOptions(statement, spec, spec.IterableArgumentIndex + 1);
        return spec;
    }

    internal int FindForInArgumentIndex(TemplateStatementSyntax statement)
    {
        int index = 0;
        while (index < statement.Arguments.Count)
        {
            if (statement.Arguments[index].Text == "in")
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void AddForLoopOptions(TemplateStatementSyntax statement, TemplateForLoopSpec spec, int startIndex)
    {
        int index = startIndex;
        while (index < statement.Arguments.Count)
        {
            string text = statement.Arguments[index].Text;
            if (text == "reversed")
            {
                spec.Reversed = true;
                index = index + 1;
            }
            else if (text == "limit" && index + 1 < statement.Arguments.Count)
            {
                spec.LimitArgumentIndex = index + 1;
                index = index + 2;
            }
            else if (text == "offset" && index + 1 < statement.Arguments.Count)
            {
                spec.OffsetArgumentIndex = index + 1;
                index = index + 2;
            }
            else if (text == "cols" && index + 1 < statement.Arguments.Count)
            {
                spec.ColumnsArgumentIndex = index + 1;
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }
        }
    }

    internal TemplateForLoopOptions EvaluateForLoopOptions(TemplateStatementSyntax statement, TemplateForLoopSpec spec, TemplateRenderContext context)
    {
        TemplateForLoopOptions options = new TemplateForLoopOptions();
        options.Reversed = spec.Reversed;
        if (spec.LimitArgumentIndex >= 0)
        {
            options.Limit = ToLoopOptionCount(context.Evaluator.Evaluate(statement.Arguments[spec.LimitArgumentIndex]));
            options.HasLimit = true;
        }

        if (spec.OffsetArgumentIndex >= 0)
        {
            TemplateValue offsetValue = context.Evaluator.Evaluate(statement.Arguments[spec.OffsetArgumentIndex]);
            if (context.UseLiquidCompatibility && offsetValue.IsStringLike() && offsetValue.StringValue == "__liquid_continue__")
            {
                options.ContinueOffset = true;
            }
            else
            {
                options.Offset = ToLoopOptionCount(offsetValue);
            }
        }

        if (spec.ColumnsArgumentIndex >= 0)
        {
            options.Columns = ToLoopOptionCount(context.Evaluator.Evaluate(statement.Arguments[spec.ColumnsArgumentIndex]));
            options.HasColumns = true;
        }

        return options;
    }

    internal string GetLiquidOffsetKey(TemplateStatementSyntax statement, TemplateForLoopSpec spec)
    {
        if (spec.IterableArgumentIndex < 0 || spec.IterableArgumentIndex >= statement.Arguments.Count)
        {
            return "";
        }

        return statement.Arguments[spec.IterableArgumentIndex].Text;
    }

    internal int ToLoopOptionCount(TemplateValue value)
    {
        if (value.Kind == TemplateValueKind.Number)
        {
            if (value.NumberValue <= 0m)
            {
                return 0;
            }

            if (value.NumberValue > 2147483647m)
            {
                return 2147483647;
            }

            return (int)value.NumberValue;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                return 1;
            }

            return 0;
        }

        if (value.IsStringLike())
        {
            decimal parsed = 0m;
            if (decimal.TryParse(value.StringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                return ToLoopOptionCount(TemplateValue.FromNumber(parsed));
            }
        }

        return 0;
    }

    internal List<TemplateLoopIteration> ApplyForLoopOptions(List<TemplateLoopIteration> iterations, TemplateForLoopOptions options)
    {
        List<TemplateLoopIteration> selected = new List<TemplateLoopIteration>();
        int start = options.Offset;
        if (start < 0)
        {
            start = 0;
        }

        int end = iterations.Count;
        if (start < end && options.HasLimit && options.Limit < end - start)
        {
            end = start + options.Limit;
        }

        int index = start;
        while (index < end && index < iterations.Count)
        {
            selected.Add(iterations[index]);
            index = index + 1;
        }

        if (options.Reversed)
        {
            return ReverseLoopIterations(selected);
        }

        return selected;
    }

    internal List<TemplateLoopIteration> ReverseLoopIterations(List<TemplateLoopIteration> iterations)
    {
        List<TemplateLoopIteration> reversed = new List<TemplateLoopIteration>();
        int index = iterations.Count - 1;
        while (index >= 0)
        {
            reversed.Add(iterations[index]);
            index = index - 1;
        }

        return reversed;
    }

    internal string GetLoopVariableName(TemplateExpressionSyntax expression)
    {
        if (expression.ExpressionKind != TemplateExpressionSyntaxKind.Variable && expression.ExpressionKind != TemplateExpressionSyntaxKind.Path)
        {
            return "";
        }

        if (!IsValidLoopVariableName(expression.Text))
        {
            return "";
        }

        return expression.Text;
    }

    internal bool IsValidLoopVariableName(string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        if (!IsLoopVariableStart(name[0]))
        {
            return false;
        }

        int index = 1;
        while (index < name.Length)
        {
            if (!IsLoopVariablePart(name[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsLoopVariableStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    internal bool IsLoopVariablePart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    internal void BindLoopVariables(TemplateScopeFrame frame, TemplateForLoopSpec spec, TemplateLoopIteration iteration, int index, int length, TemplateValue parentLoop)
    {
        if (spec.VariableNames.Count == 1)
        {
            frame.SetLocal(spec.VariableNames[0], iteration.CurrentValue);
        }
        else
        {
            frame.SetLocal(spec.VariableNames[0], iteration.Key);
            frame.SetLocal(spec.VariableNames[1], iteration.Value);
        }

        frame.SetLocal("key", iteration.Key);
        frame.SetLocal("value", iteration.Value);
        frame.SetLocal("loop", CreateLoopMetadata(iteration, index, length, parentLoop));
        frame.SetLocal("forloop", CreateLoopMetadata(iteration, index, length, parentLoop));
    }

    internal TemplateValue CreateLoopMetadata(TemplateLoopIteration iteration, int index, int length, TemplateValue parentLoop)
    {
        TemplateValue index0 = TemplateValue.FromNumber(index);
        TemplateValue index1 = TemplateValue.FromNumber(index + 1);
        TemplateValue reverseIndex = TemplateValue.FromNumber(length - index);
        TemplateValue reverseIndex0 = TemplateValue.FromNumber(length - index - 1);
        Dictionary<string, TemplateValue> map = new Dictionary<string, TemplateValue>();
        map["index0"] = index0;
        map["index"] = index1;
        map["first"] = TemplateValue.FromBoolean(index == 0);
        map["last"] = TemplateValue.FromBoolean(index + 1 == length);
        map["length"] = TemplateValue.FromNumber(length);
        map["reverseIndex"] = reverseIndex;
        map["reverseIndex0"] = reverseIndex0;
        map["revindex"] = reverseIndex;
        map["revindex0"] = reverseIndex0;
        map["counter"] = index1;
        map["counter0"] = index0;
        map["revcounter"] = reverseIndex;
        map["revcounter0"] = reverseIndex0;
        map["rindex"] = reverseIndex;
        map["rindex0"] = reverseIndex0;
        map["key"] = iteration.Key;
        map["value"] = iteration.Value;
        map["depth"] = TemplateValue.FromNumber(GetLoopDepth(parentLoop));
        map["parent"] = parentLoop;
        map["parentLoop"] = parentLoop;
        map["parentloop"] = parentLoop;
        return TemplateValue.FromMap(map);
    }

    internal decimal GetLoopDepth(TemplateValue parentLoop)
    {
        if (parentLoop.Kind != TemplateValueKind.Map)
        {
            return 1m;
        }

        TemplateValue parentDepth = parentLoop.GetMapItem("depth");
        if (parentDepth.Kind != TemplateValueKind.Number)
        {
            return 1m;
        }

        return parentDepth.NumberValue + 1m;
    }

    internal List<TemplateLoopIteration> CreateLoopIterations(TemplateValue iterable)
    {
        List<TemplateLoopIteration> iterations = new List<TemplateLoopIteration>();
        if (iterable.Kind == TemplateValueKind.List)
        {
            AddListIterations(iterations, iterable.ListValue);
        }
        else if (iterable.Kind == TemplateValueKind.Map)
        {
            AddMapIterations(iterations, iterable.MapValue);
        }
        else if (iterable.IsStringLike())
        {
            AddStringIterations(iterations, iterable.StringValue);
        }
        else if (iterable.Kind == TemplateValueKind.Object && iterable.ObjectValue != null)
        {
            TemplateValue converted = TemplateValueConverter.Convert(iterable.ObjectValue);
            if (!(converted.Kind == TemplateValueKind.Object && converted.ObjectValue == iterable.ObjectValue))
            {
                return CreateLoopIterations(converted);
            }
        }

        return iterations;
    }

    internal void AddListIterations(List<TemplateLoopIteration> iterations, List<TemplateValue> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            TemplateValue key = TemplateValue.FromNumber(index);
            iterations.Add(new TemplateLoopIteration(values[index], key, values[index], false));
            index = index + 1;
        }
    }

    internal void AddMapIterations(List<TemplateLoopIteration> iterations, Dictionary<string, TemplateValue> values)
    {
        foreach (KeyValuePair<string, TemplateValue> pair in values)
        {
            TemplateValue key = TemplateValue.FromString(pair.Key);
            TemplateValue current = CreateMapIterationValue(key, pair.Value);
            iterations.Add(new TemplateLoopIteration(current, key, pair.Value, true));
        }
    }

    internal void AddStringIterations(List<TemplateLoopIteration> iterations, string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            TemplateValue key = TemplateValue.FromNumber(index);
            TemplateValue current = TemplateValue.FromString(value[index].ToString());
            iterations.Add(new TemplateLoopIteration(current, key, current, false));
            index = index + 1;
        }
    }

    internal TemplateValue CreateMapIterationValue(TemplateValue key, TemplateValue value)
    {
        Dictionary<string, TemplateValue> map = new Dictionary<string, TemplateValue>();
        map["key"] = key;
        map["value"] = value;
        return TemplateValue.FromMap(map);
    }

    internal bool WhenMatchesCase(TemplateValue caseValue, TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        int index = 0;
        while (index < statement.Arguments.Count)
        {
            TemplateValue whenValue = context.Evaluator.Evaluate(statement.Arguments[index]);
            if (CaseValueMatches(caseValue, whenValue))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool CaseValueMatches(TemplateValue caseValue, TemplateValue whenValue)
    {
        if (whenValue.Kind == TemplateValueKind.List)
        {
            int index = 0;
            while (index < whenValue.ListValue.Count)
            {
                if (TemplateValuesEqual(caseValue, whenValue.ListValue[index]))
                {
                    return true;
                }

                index = index + 1;
            }

            return false;
        }

        return TemplateValuesEqual(caseValue, whenValue);
    }

    internal bool TemplateValuesEqual(TemplateValue left, TemplateValue right)
    {
        if ((left.IsMissing() || left.IsNull()) && (right.IsMissing() || right.IsNull()))
        {
            return true;
        }

        if (left.Kind == TemplateValueKind.Number && right.Kind == TemplateValueKind.Number)
        {
            return left.NumberValue == right.NumberValue;
        }

        if (left.Kind == TemplateValueKind.Boolean && right.Kind == TemplateValueKind.Boolean)
        {
            return left.BooleanValue == right.BooleanValue;
        }

        if (left.IsStringLike() || right.IsStringLike())
        {
            return TemplateOutputEncoder.ConvertValueToText(left) == TemplateOutputEncoder.ConvertValueToText(right);
        }

        return left.Kind == right.Kind && left.ObjectValue == right.ObjectValue;
    }

    internal string RenderNodeRangeToString(List<TemplateSyntaxNode> nodes, int startIndex, int endIndex, TemplateRenderContext context)
    {
        List<TemplateSyntaxNode> selected = new List<TemplateSyntaxNode>();
        int index = startIndex;
        while (index < endIndex)
        {
            selected.Add(nodes[index]);
            index = index + 1;
        }

        return RenderNodesToString(selected, context);
    }

    internal string RenderBlockChildrenToString(TemplateBlockSyntax block, TemplateRenderContext context)
    {
        string output = RenderNodesToString(block.Children, context);
        TemplateStatementSyntax? closingStatement = block.EndStatement;
        int index = 0;
        while (index < block.ElseChildren.Count)
        {
            TemplateSyntaxNode node = block.ElseChildren[index];
            if (node.Kind == TemplateSyntaxKind.Statement && node.Statement != null)
            {
                closingStatement = node.Statement;
                break;
            }

            index = index + 1;
        }

        return ApplyInnerWhitespace(output, block.StartStatement, closingStatement, context);
    }

    internal string RenderBlockRangeToString(TemplateBlockSyntax block, int startIndex, int endIndex, TemplateRenderContext context)
    {
        string output = RenderNodeRangeToString(block.ElseChildren, startIndex, endIndex, context);
        TemplateStatementSyntax openingStatement = block.StartStatement;
        if (startIndex > 0)
        {
            TemplateSyntaxNode openingNode = block.ElseChildren[startIndex - 1];
            if (openingNode.Statement != null)
            {
                openingStatement = openingNode.Statement;
            }
        }

        TemplateStatementSyntax? closingStatement = block.EndStatement;
        if (endIndex < block.ElseChildren.Count)
        {
            TemplateSyntaxNode closingNode = block.ElseChildren[endIndex];
            if (closingNode.Statement != null)
            {
                closingStatement = closingNode.Statement;
            }
        }

        return ApplyInnerWhitespace(output, openingStatement, closingStatement, context);
    }

    internal string ApplyInnerWhitespace(string output, TemplateStatementSyntax openingStatement, TemplateStatementSyntax? closingStatement, TemplateRenderContext context)
    {
        TemplateTrimKind openingTrim = GetRightTrimKind(openingStatement.Span.GetText());
        if (openingTrim == TemplateTrimKind.Greedy)
        {
            output = TrimLeadingWhitespace(output);
        }
        else if (openingTrim == TemplateTrimKind.NonGreedy || IsStatementStandalone(openingStatement, context))
        {
            output = TrimStandaloneRight(output);
        }

        if (closingStatement == null)
        {
            return output;
        }

        TemplateTrimKind closingTrim = GetLeftTrimKind(closingStatement.Span.GetText());
        if (closingTrim == TemplateTrimKind.Greedy)
        {
            output = TrimTrailingWhitespace(output);
        }
        else if (closingTrim == TemplateTrimKind.NonGreedy || IsStatementStandalone(closingStatement, context))
        {
            output = TrimStandaloneLeft(output);
        }

        return output;
    }

    internal bool IsStatementStandalone(TemplateStatementSyntax statement, TemplateRenderContext context)
    {
        if (!AllowsStandaloneTrimming(context))
        {
            return false;
        }

        return IsStandaloneLine(statement.Span.Source.Text, statement.Span.Start, statement.Span.End);
    }

    internal TemplateTrimKind GetOpeningLeftTrimKind(TemplateSyntaxNode node)
    {
        if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            return GetLeftTrimKind(node.Block.StartStatement.Span.GetText());
        }

        string text = node.Span.GetText();
        return GetLeftTrimKind(text);
    }

    internal TemplateTrimKind GetClosingRightTrimKind(TemplateSyntaxNode node)
    {
        if (node.Kind == TemplateSyntaxKind.Block && node.Block != null && node.Block.EndStatement != null)
        {
            return GetRightTrimKind(node.Block.EndStatement.Span.GetText());
        }

        string text = node.Span.GetText();
        return GetRightTrimKind(text);
    }

    internal TemplateTrimKind GetLeftTrimKind(string text)
    {
        if (text.Length < 3)
        {
            return TemplateTrimKind.None;
        }

        if (text[2] == '-')
        {
            return TemplateTrimKind.Greedy;
        }

        if (text[2] == '~')
        {
            return TemplateTrimKind.NonGreedy;
        }

        return TemplateTrimKind.None;
    }

    internal TemplateTrimKind GetRightTrimKind(string text)
    {
        if (text.Length < 3)
        {
            return TemplateTrimKind.None;
        }

        if (text[text.Length - 3] == '-')
        {
            return TemplateTrimKind.Greedy;
        }

        if (text[text.Length - 3] == '~')
        {
            return TemplateTrimKind.NonGreedy;
        }

        return TemplateTrimKind.None;
    }

    internal bool IsOpeningStandalone(TemplateSyntaxNode node, TemplateRenderContext context)
    {
        if (!AllowsStandaloneTrimming(context))
        {
            return false;
        }

        if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            TemplateSourceSpan span = node.Block.StartStatement.Span;
            return IsStandaloneLine(span.Source.Text, span.Start, span.End);
        }

        if (node.Kind == TemplateSyntaxKind.Raw && node.Raw != null)
        {
            string sourceText = node.Raw.Span.GetText();
            int openEnd = sourceText.IndexOf("%}", StringComparison.Ordinal);
            if (openEnd < 0)
            {
                return false;
            }

            return IsStandaloneLine(node.Raw.Span.Source.Text, node.Raw.Span.Start, node.Raw.Span.Start + openEnd + 2);
        }

        if (node.Kind != TemplateSyntaxKind.Comment && node.Kind != TemplateSyntaxKind.Statement)
        {
            return false;
        }

        return IsStandaloneLine(node.Span.Source.Text, node.Span.Start, node.Span.End);
    }

    internal bool IsClosingStandalone(TemplateSyntaxNode node, TemplateRenderContext context)
    {
        if (!AllowsStandaloneTrimming(context))
        {
            return false;
        }

        if (node.Kind == TemplateSyntaxKind.Block && node.Block != null && node.Block.EndStatement != null)
        {
            TemplateSourceSpan span = node.Block.EndStatement.Span;
            return IsStandaloneLine(span.Source.Text, span.Start, span.End);
        }

        if (node.Kind == TemplateSyntaxKind.Raw && node.Raw != null)
        {
            string sourceText = node.Raw.Span.GetText();
            int closeStart = sourceText.LastIndexOf("{%", StringComparison.Ordinal);
            if (closeStart < 0)
            {
                return false;
            }

            return IsStandaloneLine(node.Raw.Span.Source.Text, node.Raw.Span.Start + closeStart, node.Raw.Span.End);
        }

        return IsOpeningStandalone(node, context);
    }

    internal bool AllowsStandaloneTrimming(TemplateRenderContext context)
    {
        TemplateProfile profile = ProfileRegistry.GetProfile(context.Options.SyntaxProfile);
        return profile.StandaloneStatementTrimming;
    }

    internal bool IsStandaloneLine(string sourceText, int start, int end)
    {
        int before = start - 1;
        while (before >= 0 && sourceText[before] != '\r' && sourceText[before] != '\n')
        {
            if (sourceText[before] != ' ' && sourceText[before] != '\t')
            {
                return false;
            }

            before = before - 1;
        }

        int after = end;
        while (after < sourceText.Length && sourceText[after] != '\r' && sourceText[after] != '\n')
        {
            if (sourceText[after] != ' ' && sourceText[after] != '\t')
            {
                return false;
            }

            after = after + 1;
        }

        return true;
    }

    internal string ApplyLeadingTrim(string text, int trimMode)
    {
        if (trimMode == 1)
        {
            return TrimLeadingWhitespace(text);
        }

        if (trimMode == 2)
        {
            return TrimStandaloneRight(text);
        }

        return text;
    }

    internal string TrimLeadingWhitespace(string text)
    {
        int index = 0;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            index = index + 1;
        }

        return text.Substring(index);
    }

    internal string TrimTrailingWhitespace(string text)
    {
        int end = text.Length;
        while (end > 0 && char.IsWhiteSpace(text[end - 1]))
        {
            end = end - 1;
        }

        return text.Substring(0, end);
    }

    internal string TrimStandaloneLeft(string text)
    {
        int end = text.Length;
        while (end > 0 && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        return text.Substring(0, end);
    }

    internal string TrimStandaloneRight(string text)
    {
        int index = 0;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
        {
            index = index + 1;
        }

        if (index + 1 < text.Length && text[index] == '\r' && text[index + 1] == '\n')
        {
            return text.Substring(index + 2);
        }

        if (index < text.Length && (text[index] == '\r' || text[index] == '\n'))
        {
            return text.Substring(index + 1);
        }

        return text.Substring(index);
    }

    internal string TrimFinalNewline(string text)
    {
        if (text.Length >= 2 && text[text.Length - 2] == '\r' && text[text.Length - 1] == '\n')
        {
            return text.Substring(0, text.Length - 2);
        }

        if (text.Length >= 1 && (text[text.Length - 1] == '\r' || text[text.Length - 1] == '\n'))
        {
            return text.Substring(0, text.Length - 1);
        }

        return text;
    }

    internal string ApplyAutoIndent(string text, string outputSoFar)
    {
        string indentation = GetCurrentIndentation(outputSoFar);
        if (indentation.Length == 0)
        {
            return text;
        }

        string result = "";
        int index = 0;
        while (index < text.Length)
        {
            result = result + text[index].ToString();
            if (text[index] == '\n' && index + 1 < text.Length)
            {
                result = result + indentation;
            }
            else if (text[index] == '\r')
            {
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    result = result + text[index + 1].ToString();
                    index = index + 1;
                }

                if (index + 1 < text.Length)
                {
                    result = result + indentation;
                }
            }

            index = index + 1;
        }

        return result;
    }

    internal string GetCurrentIndentation(string output)
    {
        int index = output.Length - 1;
        while (index >= 0 && output[index] != '\r' && output[index] != '\n')
        {
            index = index - 1;
        }

        string indentation = "";
        index = index + 1;
        while (index < output.Length)
        {
            if (output[index] == ' ' || output[index] == '\t')
            {
                indentation = indentation + output[index].ToString();
            }
            else
            {
                return "";
            }

            index = index + 1;
        }

        return indentation;
    }
}
