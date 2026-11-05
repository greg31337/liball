using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Registers callable template features, partials, and named-template loaders for use by a renderer.
/// </summary>
/// <remarks>
/// Names use ordinal matching. Registered delegates receive template values and must return a non-null template value.
/// </remarks>
public class TemplateRegistry : ITemplateRegistry
{
    internal Dictionary<string, Func<List<TemplateValue>, TemplateValue>> Functions;
    internal Dictionary<string, Func<List<TemplateValue>, TemplateValue>> Filters;
    internal Dictionary<string, Func<List<TemplateValue>, TemplateValue>> Tests;
    internal Dictionary<string, Func<List<TemplateValue>, TemplateValue>> Helpers;
    internal Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> AsyncFunctions;
    internal Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> AsyncFilters;
    internal Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> AsyncTests;
    internal Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> AsyncHelpers;
    internal Dictionary<string, Func<TemplateBlockHelperContext, TemplateValue>> BlockHelpers;
    internal Dictionary<string, TemplateSource> Partials;
    internal long PartialVersionCounter;
    internal List<ITemplateLoader> TemplateLoaders;
    internal MemoryTemplateLoader? MemoryLoader;
    internal FileTemplateLoader? FileLoader;

    /// <summary>
    /// Initializes a new template registry and validates any supplied values.
    /// </summary>
    public TemplateRegistry()
    {
        Functions = new Dictionary<string, Func<List<TemplateValue>, TemplateValue>>();
        Filters = new Dictionary<string, Func<List<TemplateValue>, TemplateValue>>();
        Tests = new Dictionary<string, Func<List<TemplateValue>, TemplateValue>>();
        Helpers = new Dictionary<string, Func<List<TemplateValue>, TemplateValue>>();
        AsyncFunctions = new Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>>();
        AsyncFilters = new Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>>();
        AsyncTests = new Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>>();
        AsyncHelpers = new Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>>();
        BlockHelpers = new Dictionary<string, Func<TemplateBlockHelperContext, TemplateValue>>();
        Partials = new Dictionary<string, TemplateSource>();
        PartialVersionCounter = 0;
        TemplateLoaders = new List<ITemplateLoader>();
        MemoryLoader = null;
        FileLoader = null;
    }

    /// <summary>
    /// Registers or replaces the named component for later template evaluation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="function">The callable invoked with evaluated template arguments.</param>
    public void RegisterFunction(string name, Func<List<TemplateValue>, TemplateValue> function)
    {
        RegisterValueCallable(Functions, name, function);
    }

    /// <summary>
    /// Registers or replaces the named component for later template evaluation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="filter">The callable invoked with the input followed by filter arguments.</param>
    public void RegisterFilter(string name, Func<List<TemplateValue>, TemplateValue> filter)
    {
        RegisterValueCallable(Filters, name, filter);
    }

    /// <summary>
    /// Registers or replaces the named component for later template evaluation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="test">The callable that returns the test result.</param>
    public void RegisterTest(string name, Func<List<TemplateValue>, TemplateValue> test)
    {
        RegisterValueCallable(Tests, name, test);
    }

    /// <summary>
    /// Registers or replaces the named component for later template evaluation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="helper">The callable invoked for the registered helper name.</param>
    public void RegisterHelper(string name, Func<List<TemplateValue>, TemplateValue> helper)
    {
        RegisterValueCallable(Helpers, name, helper);
    }

    /// <summary>
    /// Registers or replaces an asynchronous function.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="function">The asynchronous callable invoked with evaluated arguments and render cancellation.</param>
    public void RegisterAsyncFunction(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> function)
    {
        RegisterAsyncValueCallable(AsyncFunctions, name, function);
    }

    /// <summary>
    /// Registers or replaces an asynchronous filter.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="filter">The asynchronous callable invoked with the input, filter arguments, and render cancellation.</param>
    public void RegisterAsyncFilter(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> filter)
    {
        RegisterAsyncValueCallable(AsyncFilters, name, filter);
    }

    /// <summary>
    /// Registers or replaces an asynchronous test.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="test">The asynchronous callable invoked with evaluated arguments and render cancellation.</param>
    public void RegisterAsyncTest(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> test)
    {
        RegisterAsyncValueCallable(AsyncTests, name, test);
    }

    /// <summary>
    /// Registers or replaces an asynchronous helper.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="helper">The asynchronous callable invoked with evaluated arguments and render cancellation.</param>
    public void RegisterAsyncHelper(string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> helper)
    {
        RegisterAsyncValueCallable(AsyncHelpers, name, helper);
    }

    /// <summary>
    /// Registers or replaces the named component for later template evaluation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="blockHelper">The callable invoked with block arguments and branches.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RegisterBlockHelper(string name, Func<TemplateBlockHelperContext, TemplateValue> blockHelper)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Callable name is required.", "name");
        }

        if (blockHelper == null)
        {
            throw new ArgumentNullException("blockHelper");
        }

        BlockHelpers[name] = blockHelper;
    }

    /// <summary>
    /// Registers or replaces the named component for later template evaluation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RegisterPartial(string name, string text)
    {
        ValidateTemplateName(name);
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string normalizedName = NormalizeTemplateName(name);
        if (PartialVersionCounter == long.MaxValue)
        {
            throw new InvalidOperationException("Partial version limit exceeded.");
        }

        PartialVersionCounter = PartialVersionCounter + 1;
        string version = PartialVersionCounter.ToString(CultureInfo.InvariantCulture);
        string contentHash = TemplateSource.ComputeContentHash(text);
        Partials[normalizedName] = new TemplateSource(normalizedName, null, version, contentHash, text);
    }

    /// <summary>
    /// Registers or replaces the named component for later template evaluation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RegisterPartialSource(string name, TemplateSource source)
    {
        ValidateTemplateName(name);
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source.Validate();
        if (PartialVersionCounter == long.MaxValue)
        {
            throw new InvalidOperationException("Partial version limit exceeded.");
        }

        PartialVersionCounter = PartialVersionCounter + 1;
        string? version = source.Version;
        if (string.IsNullOrEmpty(version))
        {
            version = PartialVersionCounter.ToString(CultureInfo.InvariantCulture);
        }

        string normalizedName = NormalizeTemplateName(name);
        string? contentHash = source.ContentHash;
        if (string.IsNullOrEmpty(contentHash))
        {
            contentHash = TemplateSource.ComputeContentHash(source.Text);
        }

        TemplateSource storedSource = new TemplateSource(normalizedName, source.Path, version, contentHash, source.Text);
        storedSource.Timestamp = source.Timestamp;
        Partials[normalizedName] = storedSource;
    }

    /// <summary>
    /// Determines whether the requested item exists without returning its contents.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool ContainsPartial(string name)
    {
        ValidateTemplateName(name);
        return Partials.ContainsKey(NormalizeTemplateName(name));
    }

    /// <summary>
    /// Returns a defensive copy of the registered partial, or <c>null</c> when the name is absent.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The matching source, or <c>null</c> when it is not found.</returns>
    public TemplateSource? LoadPartial(string name)
    {
        ValidateTemplateName(name);
        string normalizedName = NormalizeTemplateName(name);
        if (!Partials.ContainsKey(normalizedName))
        {
            return null;
        }

        return CopySource(Partials[normalizedName]);
    }

    /// <summary>
    /// Completes with the named source, or <c>null</c> when it is not found; cancellation is observed before loading.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource?> LoadPartialAsync(string name)
    {
        return LoadPartialWithCancellationAsync(name, CancellationToken.None);
    }

    /// <summary>
    /// Completes with the named source, or <c>null</c> when it is not found; cancellation is observed before loading.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource?> LoadPartialWithCancellationAsync(string name, CancellationToken cancellationToken)
    {
        ValidateTemplateName(name);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource?>(cancellationToken);
        }

        return Task.FromResult<TemplateSource?>(LoadPartial(name));
    }

    /// <summary>
    /// Removes the named item and reports whether an item was present.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool RemovePartial(string name)
    {
        ValidateTemplateName(name);
        return Partials.Remove(NormalizeTemplateName(name));
    }

    /// <summary>
    /// Returns the partial names.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public List<string> GetPartialNames()
    {
        List<string> names = new List<string>();
        foreach (KeyValuePair<string, TemplateSource> pair in Partials)
        {
            names.Add(pair.Key);
        }

        return names;
    }

    /// <summary>
    /// Replaces the configured memory loader after validating the supplied value.
    /// </summary>
    /// <param name="loader">The loader used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetMemoryLoader(MemoryTemplateLoader loader)
    {
        if (loader == null)
        {
            throw new ArgumentNullException("loader");
        }

        if (MemoryLoader != null)
        {
            TemplateLoaders.Remove(MemoryLoader);
        }

        TemplateLoaders.Remove(loader);
        MemoryLoader = loader;
        TemplateLoaders.Insert(0, loader);
    }

    /// <summary>
    /// Replaces the configured file loader after validating the supplied value.
    /// </summary>
    /// <param name="loader">The loader used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetFileLoader(FileTemplateLoader loader)
    {
        if (loader == null)
        {
            throw new ArgumentNullException("loader");
        }

        int loaderIndex = -1;
        if (FileLoader != null)
        {
            loaderIndex = TemplateLoaders.IndexOf(FileLoader);
            TemplateLoaders.Remove(FileLoader);
        }

        int existingIndex = TemplateLoaders.IndexOf(loader);
        if (existingIndex >= 0)
        {
            if (loaderIndex < 0)
            {
                loaderIndex = existingIndex;
            }

            TemplateLoaders.Remove(loader);
        }

        FileLoader = loader;
        if (loaderIndex >= 0)
        {
            TemplateLoaders.Insert(loaderIndex, loader);
        }
        else
        {
            TemplateLoaders.Add(loader);
        }
    }

    /// <summary>
    /// Adds a template loader to the ordered fallback chain.
    /// </summary>
    /// <remarks>
    /// Loaders are queried in order until one returns a source. Templates registered directly on this registry remain highest priority.
    /// </remarks>
    /// <param name="loader">The loader appended to the fallback chain.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void AddTemplateLoader(ITemplateLoader loader)
    {
        if (loader == null)
        {
            throw new ArgumentNullException("loader");
        }

        if (!TemplateLoaders.Contains(loader))
        {
            TemplateLoaders.Add(loader);
        }
    }

    /// <summary>
    /// Registers or replaces the named component for later template evaluation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="text">The text to process.</param>
    public void RegisterTemplate(string name, string text)
    {
        ValidateTemplateName(name);
        MemoryTemplateLoader? loader = MemoryLoader;
        if (loader == null)
        {
            loader = new MemoryTemplateLoader();
            SetMemoryLoader(loader);
        }

        loader.SetTemplate(NormalizeTemplateName(name), text);
    }

    /// <summary>
    /// Loads a named template from the first configured loader that contains it.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The matching source, or <c>null</c> when it is not found.</returns>
    public TemplateSource? LoadTemplate(string name)
    {
        ValidateTemplateName(name);
        string normalizedName = NormalizeTemplateName(name);

        int index = 0;
        while (index < TemplateLoaders.Count)
        {
            TemplateSource? source = TemplateLoaders[index].Load(normalizedName);
            if (source != null)
            {
                return source;
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>
    /// Completes with the named source, or <c>null</c> when it is not found; cancellation is observed before loading.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource?> LoadTemplateAsync(string name)
    {
        return LoadTemplateWithCancellationAsync(name, CancellationToken.None);
    }

    /// <summary>
    /// Completes with the named source, or <c>null</c> when it is not found; cancellation is observed before loading.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource?> LoadTemplateWithCancellationAsync(string name, CancellationToken cancellationToken)
    {
        ValidateTemplateName(name);
        string normalizedName = NormalizeTemplateName(name);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource?>(cancellationToken);
        }

        return LoadTemplateLoaderAsync(normalizedName, 0, cancellationToken);
    }

    internal Task<TemplateSource?> LoadTemplateLoaderAsync(string name, int index, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource?>(cancellationToken);
        }

        if (index >= TemplateLoaders.Count)
        {
            return Task.FromResult<TemplateSource?>(null);
        }

        Task<TemplateSource?> loadTask = TemplateLoaders[index].LoadAsync(name, cancellationToken);
        if (loadTask == null)
        {
            return Task.FromException<TemplateSource?>(new InvalidOperationException("Template loader returned null instead of a task."));
        }

        return loadTask.ContinueWith((Task<TemplateSource?> completed) =>
        {
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<TemplateSource?>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                Exception error = completed.Exception;
                if (completed.Exception.InnerExceptions.Count == 1)
                {
                    error = completed.Exception.InnerExceptions[0];
                }

                return Task.FromException<TemplateSource?>(error);
            }

            if (completed.Result != null)
            {
                return Task.FromResult<TemplateSource?>(completed.Result);
            }

            return LoadTemplateLoaderAsync(name, index + 1, cancellationToken);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal void ApplyTo(TemplateExpressionEvaluator evaluator)
    {
        if (evaluator == null)
        {
            throw new ArgumentNullException("evaluator");
        }

        foreach (KeyValuePair<string, Func<List<TemplateValue>, TemplateValue>> pair in Functions)
        {
            evaluator.RegisterFunction(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<string, Func<List<TemplateValue>, TemplateValue>> pair in Filters)
        {
            evaluator.RegisterFilter(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<string, Func<List<TemplateValue>, TemplateValue>> pair in Tests)
        {
            evaluator.RegisterTest(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<string, Func<List<TemplateValue>, TemplateValue>> pair in Helpers)
        {
            evaluator.RegisterFunction(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> pair in AsyncFunctions)
        {
            evaluator.RegisterAsyncFunction(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> pair in AsyncFilters)
        {
            evaluator.RegisterAsyncFilter(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> pair in AsyncTests)
        {
            evaluator.RegisterAsyncTest(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> pair in AsyncHelpers)
        {
            evaluator.RegisterAsyncFunction(pair.Key, pair.Value);
        }
    }

    internal void RegisterValueCallable(Dictionary<string, Func<List<TemplateValue>, TemplateValue>> callables, string name, Func<List<TemplateValue>, TemplateValue> function)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Callable name is required.", "name");
        }

        if (function == null)
        {
            throw new ArgumentNullException("function");
        }

        callables[name] = function;
    }

    internal void RegisterAsyncValueCallable(Dictionary<string, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>>> callables, string name, Func<List<TemplateValue>, CancellationToken, Task<TemplateValue>> function)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Callable name is required.", "name");
        }

        if (function == null)
        {
            throw new ArgumentNullException("function");
        }

        callables[name] = function;
    }

    internal void ValidateTemplateName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Template name is required.", "name");
        }

        if (Path.IsPathRooted(name))
        {
            throw new InvalidOperationException("Template names must be relative.");
        }

        string normalized = NormalizeTemplateName(name);
        if (normalized.Length == 0 || normalized[0] == '/')
        {
            throw new InvalidOperationException("Template names must be relative.");
        }

        if (normalized.IndexOf('\0') >= 0 || normalized.IndexOf(':') >= 0)
        {
            throw new InvalidOperationException("Template name contains an invalid character.");
        }

        string[] segments = normalized.Split(new char[] { '/' }, StringSplitOptions.None);
        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index] == "" || segments[index] == "." || segments[index] == "..")
            {
                throw new InvalidOperationException("Template name contains an invalid path segment.");
            }

            index = index + 1;
        }
    }

    internal TemplateSource CopySource(TemplateSource source)
    {
        TemplateSource copy = new TemplateSource(source.Name, source.Path, source.Version, source.ContentHash, source.Text);
        copy.Timestamp = source.Timestamp;
        return copy;
    }

    internal string NormalizeTemplateName(string name)
    {
        string normalized = name.Replace('\\', '/');
        return normalized;
    }
}
