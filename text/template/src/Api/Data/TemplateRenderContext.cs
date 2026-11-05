using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace template;

/// <summary>
/// Holds the public configuration and services associated with one rendering operation.
/// </summary>
/// <remarks>
/// A context is stateful and belongs to one render. Create a new context instead of sharing one across concurrent renders.
/// </remarks>
public class TemplateRenderContext
{
    /// <summary>
    /// Specifies the options for this render context.
    /// </summary>
    public TemplateOptions Options;
    internal TemplateScopeStack Scope;
    /// <summary>
    /// Specifies the member resolver for this render context.
    /// </summary>
    public ITemplateMemberResolver MemberResolver;
    internal TemplateExpressionEvaluator Evaluator;
    /// <summary>
    /// Specifies the writer for this render context.
    /// </summary>
    public TemplateWriter Writer;
    /// <summary>
    /// Specifies the registry for this render context.
    /// </summary>
    public TemplateRegistry Registry;
    /// <summary>
    /// Receives diagnostics discovered while rendering, or <c>null</c> when reporting is disabled.
    /// </summary>
    public ITemplateDiagnosticSink? DiagnosticSink;
    /// <summary>
    /// Receives source mappings after this render completes, or <c>null</c> when collection is disabled.
    /// </summary>
    public ITemplateSourceMapCollector? SourceMapCollector;
    internal TemplateSourceMapState SourceMapState;
    internal string FlowControlSignal;
    internal TemplateValue ReturnValue;
    internal Dictionary<string, long> Counters;
    internal Dictionary<string, int> LiquidForOffsets;
    internal List<string> IncludeStack;
    internal List<TemplateDependency> Dependencies;
    internal Dictionary<string, TemplateBlockOverride> BlockOverrides;
    internal Dictionary<string, TemplateSource> HandlebarsInlinePartials;
    internal Dictionary<string, int> HandlebarsInlinePartialScopeIds;
    internal List<int> HandlebarsInlinePartialScopes;
    internal List<int> HandlebarsInlinePartialScopeCounter;
    internal List<Dictionary<string, TemplateSource?>> HandlebarsInlinePartialSavedSources;
    internal List<Dictionary<string, int>> HandlebarsInlinePartialSavedScopeIds;
    internal List<TemplateValue> HandlebarsPartialBlockContexts;
    internal TemplateRuntimeQuotaState RuntimeQuotas;
    internal bool UseMustacheInheritance;
    internal bool UseHandlebarsCompatibility;
    internal bool UseLiquidCompatibility;
    internal bool UseJinjaCompatibility;
    internal TemplateEscapingMode EscapingMode;
    internal bool AutoEscape;

    /// <summary>
    /// Creates a render context with default options, registry, and member resolver.
    /// </summary>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <returns>The configured render context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateRenderContext Create(TemplateValue model, TextWriter writer)
    {
        return new TemplateRenderContext(model, writer, new TemplateOptions(), new TemplateRegistry(), new TemplateMemberResolver());
    }

    /// <summary>
    /// Creates a render context with the supplied options.
    /// </summary>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The configured render context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateRenderContext Create(TemplateValue model, TextWriter writer, TemplateOptions options)
    {
        return new TemplateRenderContext(model, writer, options, new TemplateRegistry(), new TemplateMemberResolver());
    }

    /// <summary>
    /// Creates a render context with the supplied options and registry.
    /// </summary>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="registry">The registry that supplies callables and named templates.</param>
    /// <returns>The configured render context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateRenderContext Create(TemplateValue model, TextWriter writer, TemplateOptions options, TemplateRegistry registry)
    {
        return new TemplateRenderContext(model, writer, options, registry, new TemplateMemberResolver());
    }

    /// <summary>
    /// Initializes a new template render context with an explicitly supplied member resolver.
    /// </summary>
    /// <param name="model">The root value exposed to the template.</param>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="registry">The registry that supplies callables and named templates.</param>
    /// <param name="memberResolver">The service used to resolve member and index expressions.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateRenderContext(TemplateValue model, TextWriter writer, TemplateOptions options, TemplateRegistry registry, ITemplateMemberResolver memberResolver)
    {
        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

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

        Options = options.Clone();
        if (Options.StrictMode)
        {
            Options.SetStrictMode(true);
        }

        Options.Validate();
        Scope = TemplateScopeStack.Create(model);
        MemberResolver = memberResolver;
        Registry = registry;
        DiagnosticSink = null;
        SourceMapCollector = null;
        SourceMapState = new TemplateSourceMapState();
        Evaluator = new TemplateExpressionEvaluator(Scope, Options, MemberResolver, Registry);
        Evaluator.RenderContext = this;
        Writer = TemplateWriter.Create(writer, Options);
        FlowControlSignal = "";
        ReturnValue = TemplateValue.Missing();
        Counters = new Dictionary<string, long>();
        LiquidForOffsets = new Dictionary<string, int>();
        IncludeStack = new List<string>();
        Dependencies = new List<TemplateDependency>();
        BlockOverrides = new Dictionary<string, TemplateBlockOverride>();
        HandlebarsInlinePartials = new Dictionary<string, TemplateSource>();
        HandlebarsInlinePartialScopeIds = new Dictionary<string, int>();
        HandlebarsInlinePartialScopes = new List<int>();
        HandlebarsInlinePartialScopes.Add(0);
        HandlebarsInlinePartialScopeCounter = new List<int>();
        HandlebarsInlinePartialScopeCounter.Add(0);
        HandlebarsInlinePartialSavedSources = new List<Dictionary<string, TemplateSource?>>();
        HandlebarsInlinePartialSavedScopeIds = new List<Dictionary<string, int>>();
        HandlebarsPartialBlockContexts = new List<TemplateValue>();
        RuntimeQuotas = new TemplateRuntimeQuotaState();
        UseMustacheInheritance = false;
        UseHandlebarsCompatibility = false;
        UseLiquidCompatibility = false;
        UseJinjaCompatibility = false;
        EscapingMode = Options.EscapingMode;
        AutoEscape = Options.AutoEscape;
    }

    internal void PushHandlebarsInlinePartialScope()
    {
        if (HandlebarsInlinePartialScopeCounter[0] == 2147483647)
        {
            throw new InvalidOperationException("Handlebars inline partial scope limit exceeded.");
        }

        HandlebarsInlinePartialScopeCounter[0] = HandlebarsInlinePartialScopeCounter[0] + 1;
        HandlebarsInlinePartialScopes.Add(HandlebarsInlinePartialScopeCounter[0]);
        HandlebarsInlinePartialSavedSources.Add(new Dictionary<string, TemplateSource?>());
        HandlebarsInlinePartialSavedScopeIds.Add(new Dictionary<string, int>());
    }

    internal void PopHandlebarsInlinePartialScope()
    {
        if (HandlebarsInlinePartialScopes.Count <= 1)
        {
            throw new InvalidOperationException("No Handlebars inline partial block scope exists.");
        }

        Dictionary<string, TemplateSource?> savedSources = HandlebarsInlinePartialSavedSources[HandlebarsInlinePartialSavedSources.Count - 1];
        Dictionary<string, int> savedScopeIds = HandlebarsInlinePartialSavedScopeIds[HandlebarsInlinePartialSavedScopeIds.Count - 1];
        foreach (KeyValuePair<string, TemplateSource?> pair in savedSources)
        {
            if (pair.Value == null)
            {
                HandlebarsInlinePartials.Remove(pair.Key);
                HandlebarsInlinePartialScopeIds.Remove(pair.Key);
            }
            else
            {
                HandlebarsInlinePartials[pair.Key] = pair.Value;
                HandlebarsInlinePartialScopeIds[pair.Key] = savedScopeIds[pair.Key];
            }
        }

        HandlebarsInlinePartialSavedSources.RemoveAt(HandlebarsInlinePartialSavedSources.Count - 1);
        HandlebarsInlinePartialSavedScopeIds.RemoveAt(HandlebarsInlinePartialSavedScopeIds.Count - 1);
        HandlebarsInlinePartialScopes.RemoveAt(HandlebarsInlinePartialScopes.Count - 1);
    }

    internal void SetHandlebarsInlinePartial(string name, TemplateSource source)
    {
        int currentScopeId = HandlebarsInlinePartialScopes[HandlebarsInlinePartialScopes.Count - 1];
        if (currentScopeId != 0)
        {
            Dictionary<string, TemplateSource?> savedSources = HandlebarsInlinePartialSavedSources[HandlebarsInlinePartialSavedSources.Count - 1];
            Dictionary<string, int> savedScopeIds = HandlebarsInlinePartialSavedScopeIds[HandlebarsInlinePartialSavedScopeIds.Count - 1];
            if (!savedSources.ContainsKey(name))
            {
                savedSources[name] = HandlebarsInlinePartials.ContainsKey(name) ? HandlebarsInlinePartials[name] : null;
                savedScopeIds[name] = HandlebarsInlinePartialScopeIds.ContainsKey(name) ? HandlebarsInlinePartialScopeIds[name] : -1;
            }
        }

        HandlebarsInlinePartials[name] = source;
        HandlebarsInlinePartialScopeIds[name] = currentScopeId;
    }

    internal bool HasVisibleHandlebarsInlinePartial(string name)
    {
        if (!HandlebarsInlinePartials.ContainsKey(name) || !HandlebarsInlinePartialScopeIds.ContainsKey(name))
        {
            return false;
        }

        int scopeId = HandlebarsInlinePartialScopeIds[name];
        return HandlebarsInlinePartialScopes.Contains(scopeId);
    }

    internal void BeginRender(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (MemberResolver == null)
        {
            throw new InvalidOperationException("A member resolver is required before rendering.");
        }

        Evaluator.MemberResolver = MemberResolver;
        EscapingMode = Options.EscapingMode;
        AutoEscape = Options.AutoEscape;
        FlowControlSignal = "";
        ReturnValue = TemplateValue.Missing();
        RuntimeQuotas.Reset();
        SourceMapState.Reset();
        RuntimeQuotas.Clock.Start();
        CountTemplate(source);
        CheckRenderTime();
    }

    internal void UseRuntimeQuotasFrom(TemplateRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        RuntimeQuotas = context.RuntimeQuotas;
        Evaluator.RenderContext = this;
    }

    internal void UseSourceMapStateFrom(TemplateRenderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        SourceMapCollector = context.SourceMapCollector;
        SourceMapState = context.SourceMapState;
    }

    internal void CountLoopIteration()
    {
        if (RuntimeQuotas.LoopIterationCount >= Options.MaximumLoopIterations)
        {
            throw new InvalidOperationException("Maximum loop iteration count of " + Options.MaximumLoopIterations.ToString() + " was exceeded.");
        }

        RuntimeQuotas.LoopIterationCount = RuntimeQuotas.LoopIterationCount + 1;
        CheckRenderTime();
    }

    internal void CountTemplate(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (source.Length > Options.MaximumTemplateLength)
        {
            throw new InvalidOperationException("Maximum template length of " + Options.MaximumTemplateLength.ToString() + " was exceeded.");
        }

        if (RuntimeQuotas.TemplateCount >= Options.MaximumTemplateCount)
        {
            throw new InvalidOperationException("Maximum template count of " + Options.MaximumTemplateCount.ToString() + " was exceeded.");
        }

        RuntimeQuotas.TemplateCount = RuntimeQuotas.TemplateCount + 1;
        CheckRenderTime();
    }

    internal void EnterRecursion()
    {
        if (RuntimeQuotas.RecursionDepth >= Options.MaximumRecursionDepth)
        {
            throw new InvalidOperationException("Maximum recursion depth of " + Options.MaximumRecursionDepth.ToString() + " was exceeded.");
        }

        RuntimeQuotas.RecursionDepth = RuntimeQuotas.RecursionDepth + 1;
        CheckRenderTime();
    }

    internal void ExitRecursion()
    {
        if (RuntimeQuotas.RecursionDepth > 0)
        {
            RuntimeQuotas.RecursionDepth = RuntimeQuotas.RecursionDepth - 1;
        }
    }

    internal void EnsureOutputLengthAllowed(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (length > Options.MaximumOutputLength)
        {
            throw new InvalidOperationException("Template output length limit exceeded.");
        }

        CheckRenderTime();
    }

    internal void EnsureIncludeDepthAllowed(int depth)
    {
        if (depth < 0)
        {
            throw new ArgumentOutOfRangeException("depth");
        }

        if (depth > Options.MaximumIncludeDepth)
        {
            throw new InvalidOperationException("Maximum include depth of " + Options.MaximumIncludeDepth.ToString() + " was exceeded.");
        }

        CheckRenderTime();
    }

    internal void CheckRenderTime()
    {
        if (Options.MaximumRenderMilliseconds == 0)
        {
            return;
        }

        if (!RuntimeQuotas.Clock.IsRunning)
        {
            return;
        }

        if (RuntimeQuotas.Clock.ElapsedMilliseconds > Options.MaximumRenderMilliseconds)
        {
            throw new InvalidOperationException("Maximum render time of " + Options.MaximumRenderMilliseconds.ToString() + " milliseconds was exceeded.");
        }
    }

    internal void ReportDiagnostic(TemplateDiagnostic diagnostic)
    {
        if (DiagnosticSink != null)
        {
            DiagnosticSink.Report(diagnostic);
        }
    }

    internal bool HasActiveInclude(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = 0;
        while (index < IncludeStack.Count)
        {
            if (string.Equals(IncludeStack[index], name, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void PushActiveInclude(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        IncludeStack.Add(name);
    }

    internal void PopActiveInclude()
    {
        if (IncludeStack.Count == 0)
        {
            throw new InvalidOperationException("No active include scope exists.");
        }

        IncludeStack.RemoveAt(IncludeStack.Count - 1);
    }

    internal void RecordDependency(TemplateDependencyKind kind, TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        int index = 0;
        while (index < Dependencies.Count)
        {
            TemplateDependency existing = Dependencies[index];
            if (existing.Kind == kind && string.Equals(existing.Name, source.Name, StringComparison.Ordinal))
            {
                return;
            }

            index = index + 1;
        }

        Dependencies.Add(TemplateDependency.FromSource(kind, source));
    }

    /// <summary>
    /// Returns the dependencies.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal List<TemplateDependency> GetDependencies()
    {
        List<TemplateDependency> dependencies = new List<TemplateDependency>();
        int index = 0;
        while (index < Dependencies.Count)
        {
            TemplateDependency dependency = Dependencies[index];
            dependencies.Add(new TemplateDependency(dependency.Kind, dependency.Name, dependency.Path, dependency.Version, dependency.ContentHash));
            index = index + 1;
        }

        return dependencies;
    }
}
