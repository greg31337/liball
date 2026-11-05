using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Loads named templates from manifest resources in a fixed .NET assembly.
/// </summary>
public class EmbeddedResourceTemplateLoader : ITemplateLoader, IEmbeddedResourceTemplateLoader
{
    internal Assembly Assembly;
    internal TemplateLoaderOptions Options;

    /// <summary>
    /// Initializes an embedded-resource template loader for the supplied assembly.
    /// </summary>
    /// <param name="assembly">The assembly that contains template resources.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public EmbeddedResourceTemplateLoader(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException("assembly");
        }

        Assembly = assembly;
        Options = new TemplateLoaderOptions();
    }

    /// <summary>
    /// Configures text decoding, maximum decoded template length, and the resource-name prefix.
    /// </summary>
    /// <param name="options">The options that control loading.</param>
    public void Configure(TemplateLoaderOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        Options = options.Clone();
    }

    /// <summary>
    /// Reports whether the named manifest resource exists.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns><c>true</c> when the resource exists; otherwise, <c>false</c>.</returns>
    public bool ContainsTemplate(string name)
    {
        string resourceName = GetResourceName(name);
        Stream? stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return false;
        }

        stream.Dispose();
        return true;
    }

    /// <summary>
    /// Returns the named template resource, or <c>null</c> when it is not found.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The matching source, or <c>null</c> when it is not found.</returns>
    public TemplateSource? Load(string name)
    {
        string resourceName = GetResourceName(name);
        Stream? stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return null;
        }

        using (stream)
        {
            using (StreamReader reader = new StreamReader(stream, Options.Encoding, true))
            {
                string text = reader.ReadToEnd();
                return CreateSource(name, resourceName, text);
            }
        }
    }

    /// <summary>
    /// Completes with the named template resource, or <c>null</c> when it is not found.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource?> LoadWithoutCancellationAsync(string name)
    {
        return LoadAsync(name, CancellationToken.None);
    }

    /// <summary>
    /// Completes with the named template resource, or <c>null</c> when it is not found, while honoring cancellation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the matching source, or <c>null</c> when it is not found.</returns>
    public Task<TemplateSource?> LoadAsync(string name, CancellationToken cancellationToken)
    {
        string resourceName = GetResourceName(name);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource?>(cancellationToken);
        }

        Stream? stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return Task.FromResult<TemplateSource?>(null);
        }

        StreamReader reader = new StreamReader(stream, Options.Encoding, true);
        return CompleteResourceLoadAsync(reader.ReadToEndAsync(cancellationToken), reader, name, resourceName);
    }

    /// <summary>
    /// Returns the named template and throws when the resource cannot be found.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The required template source.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the required resource does not exist.</exception>
    public TemplateSource LoadRequired(string name)
    {
        TemplateSource? source = Load(name);
        if (source == null)
        {
            throw new FileNotFoundException("Embedded template resource '" + name + "' was not found.", name);
        }

        return source;
    }

    /// <summary>
    /// Completes with the required template resource.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>A task that completes with the required template source.</returns>
    public Task<TemplateSource> LoadRequiredAsync(string name)
    {
        return LoadRequiredWithCancellationAsync(name, CancellationToken.None);
    }

    /// <summary>
    /// Completes with the required template resource while honoring cancellation.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the required template source.</returns>
    public Task<TemplateSource> LoadRequiredWithCancellationAsync(string name, CancellationToken cancellationToken)
    {
        string resourceName = GetResourceName(name);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource>(cancellationToken);
        }

        Stream? stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return Task.FromException<TemplateSource>(new FileNotFoundException("Embedded template resource '" + name + "' was not found.", name));
        }

        StreamReader reader = new StreamReader(stream, Options.Encoding, true);
        return CompleteRequiredResourceLoadAsync(reader.ReadToEndAsync(cancellationToken), reader, name, resourceName);
    }

    internal Task<TemplateSource?> CompleteResourceLoadAsync(Task<string> task, StreamReader reader, string name, string resourceName)
    {
        return task.ContinueWith((Task<string> completed) =>
        {
            reader.Dispose();
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

            return Task.FromResult<TemplateSource?>(CreateSource(name, resourceName, completed.Result));
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<TemplateSource> CompleteRequiredResourceLoadAsync(Task<string> task, StreamReader reader, string name, string resourceName)
    {
        return task.ContinueWith((Task<string> completed) =>
        {
            reader.Dispose();
            if (completed.IsCanceled)
            {
                return Task.FromCanceled<TemplateSource>(new CancellationToken(true));
            }

            if (completed.IsFaulted)
            {
                Exception error = completed.Exception;
                if (completed.Exception.InnerExceptions.Count == 1)
                {
                    error = completed.Exception.InnerExceptions[0];
                }

                return Task.FromException<TemplateSource>(error);
            }

            return Task.FromResult(CreateSource(name, resourceName, completed.Result));
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    /// <summary>
    /// Returns logical template names for resources matching the configured prefix.
    /// </summary>
    /// <returns>The available logical template names.</returns>
    public List<string> GetTemplateNames()
    {
        List<string> names = new List<string>();
        string[] resourceNames = Assembly.GetManifestResourceNames();
        int index = 0;
        while (index < resourceNames.Length)
        {
            string resourceName = resourceNames[index];
            if (resourceName.StartsWith(Options.ResourcePrefix, StringComparison.Ordinal))
            {
                names.Add(resourceName.Substring(Options.ResourcePrefix.Length));
            }

            index = index + 1;
        }

        return names;
    }

    internal string GetResourceName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Template name is required.", "name");
        }

        if (name.StartsWith("/", StringComparison.Ordinal) || name.StartsWith("\\", StringComparison.Ordinal) || name.Contains(":"))
        {
            throw new InvalidOperationException("Embedded template names must be relative resource names.");
        }

        string normalized = name.Replace('\\', '.').Replace('/', '.');
        if (normalized.StartsWith(".", StringComparison.Ordinal) || normalized.EndsWith(".", StringComparison.Ordinal) || normalized.Contains(".."))
        {
            throw new InvalidOperationException("Embedded template names cannot contain empty or parent segments.");
        }

        return Options.ResourcePrefix + normalized;
    }

    internal TemplateSource CreateSource(string name, string resourceName, string text)
    {
        if (text.Length > Options.MaximumTemplateLength)
        {
            throw new InvalidOperationException("Maximum template length of " + Options.MaximumTemplateLength.ToString(CultureInfo.InvariantCulture) + " was exceeded.");
        }

        string version = Assembly.ManifestModule.ModuleVersionId.ToString("D");
        string contentHash = TemplateSource.ComputeContentHash(text);
        return new TemplateSource(name, resourceName, version, contentHash, text);
    }
}
