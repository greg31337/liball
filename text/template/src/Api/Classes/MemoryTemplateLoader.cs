using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Stores named templates in memory and returns defensive source copies for rendering.
/// </summary>
public class MemoryTemplateLoader : ITemplateLoader, IMemoryTemplateLoader
{
    internal Dictionary<string, TemplateSource> Templates;
    internal long VersionCounter;
    internal TemplateLoaderOptions Options;
    internal object SyncRoot;

    /// <summary>
    /// Initializes a new memory template loader and validates any supplied values.
    /// </summary>
    public MemoryTemplateLoader()
    {
        Templates = new Dictionary<string, TemplateSource>();
        VersionCounter = 0;
        Options = new TemplateLoaderOptions();
        SyncRoot = new object();
    }

    /// <summary>
    /// Configures maximum template length for stored and subsequently loaded templates.
    /// </summary>
    /// <param name="options">The options that control loading.</param>
    public void Configure(TemplateLoaderOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        lock (SyncRoot)
        {
            foreach (KeyValuePair<string, TemplateSource> pair in Templates)
            {
                if (pair.Value.Text.Length > options.MaximumTemplateLength)
                {
                    throw new InvalidOperationException("Maximum template length of " + options.MaximumTemplateLength.ToString(CultureInfo.InvariantCulture) + " was exceeded.");
                }
            }

            Options = options.Clone();
        }
    }

    /// <summary>
    /// Adds the supplied template after validating it.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="text">The text to process.</param>
    public void AddTemplate(string name, string text)
    {
        SetTemplate(name, text);
    }

    /// <summary>
    /// Adds the supplied template after validating it.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="source">The template source and its identity metadata.</param>
    public void AddTemplateSource(string name, TemplateSource source)
    {
        SetTemplateSource(name, source);
    }

    /// <summary>
    /// Replaces the configured template after validating the supplied value.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="text">The text to process.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetTemplate(string name, string text)
    {
        RequireTemplateName(name);
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        lock (SyncRoot)
        {
            ValidateLength(text);

            if (VersionCounter == long.MaxValue)
            {
                throw new InvalidOperationException("Template version limit exceeded.");
            }

            VersionCounter = VersionCounter + 1;
            string version = VersionCounter.ToString(CultureInfo.InvariantCulture);
            string contentHash = TemplateSource.ComputeContentHash(text);
            Templates[name] = new TemplateSource(name, null, version, contentHash, text);
        }
    }

    /// <summary>
    /// Replaces the configured template after validating the supplied value.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetTemplateSource(string name, TemplateSource source)
    {
        RequireTemplateName(name);
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source.Validate();
        lock (SyncRoot)
        {
            ValidateLength(source.Text);

            if (VersionCounter == long.MaxValue)
            {
                throw new InvalidOperationException("Template version limit exceeded.");
            }

            VersionCounter = VersionCounter + 1;
            string? version = source.Version;
            if (string.IsNullOrEmpty(version))
            {
                version = VersionCounter.ToString(CultureInfo.InvariantCulture);
            }

            string? contentHash = source.ContentHash;
            if (string.IsNullOrEmpty(contentHash))
            {
                contentHash = TemplateSource.ComputeContentHash(source.Text);
            }

            TemplateSource storedSource = new TemplateSource(name, source.Path, version, contentHash, source.Text);
            storedSource.Timestamp = source.Timestamp;
            Templates[name] = storedSource;
        }
    }

    /// <summary>
    /// Determines whether the requested item exists without returning its contents.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool ContainsTemplate(string name)
    {
        RequireTemplateName(name);
        lock (SyncRoot)
        {
            return Templates.ContainsKey(name);
        }
    }

    /// <summary>
    /// Returns a defensive copy of the named template, or <c>null</c> when no template is registered.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The matching source, or <c>null</c> when it is not found.</returns>
    public TemplateSource? Load(string name)
    {
        RequireTemplateName(name);
        lock (SyncRoot)
        {
            if (!Templates.ContainsKey(name))
            {
                return null;
            }

            TemplateSource source = Templates[name];
            ValidateLength(source.Text);
            return CopySource(source);
        }
    }

    /// <summary>
    /// Completes with the named source, or <c>null</c> when it is not found; cancellation is observed before loading.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource?> LoadWithoutCancellationAsync(string name)
    {
        return LoadAsync(name, CancellationToken.None);
    }

    /// <summary>
    /// Completes with the named source, or <c>null</c> when it is not found; cancellation is observed before loading.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource?> LoadAsync(string name, CancellationToken cancellationToken)
    {
        RequireTemplateName(name);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource?>(cancellationToken);
        }

        return Task.FromResult<TemplateSource?>(Load(name));
    }

    /// <summary>
    /// Returns the named template and throws when it cannot be found.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the operation cannot be completed.</exception>
    public TemplateSource LoadRequired(string name)
    {
        TemplateSource? source = Load(name);
        if (source == null)
        {
            throw new KeyNotFoundException("Template '" + name + "' was not found.");
        }

        return source;
    }

    /// <summary>
    /// Completes with the named template and faults when it cannot be found; cancellation is observed before loading.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource> LoadRequiredAsync(string name)
    {
        return LoadRequiredWithCancellationAsync(name, CancellationToken.None);
    }

    /// <summary>
    /// Completes with the named template and faults when it cannot be found; cancellation is observed before loading.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <param name="cancellationToken">The token used to cancel before or during the operation.</param>
    /// <returns>A task that completes with the operation result.</returns>
    public Task<TemplateSource> LoadRequiredWithCancellationAsync(string name, CancellationToken cancellationToken)
    {
        RequireTemplateName(name);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource>(cancellationToken);
        }

        return Task.FromResult(LoadRequired(name));
    }

    /// <summary>
    /// Removes the named item and reports whether an item was present.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool RemoveTemplate(string name)
    {
        RequireTemplateName(name);
        lock (SyncRoot)
        {
            return Templates.Remove(name);
        }
    }

    /// <summary>
    /// Removes all stored entries and resets associated counters.
    /// </summary>
    public void Clear()
    {
        lock (SyncRoot)
        {
            Templates.Clear();
            VersionCounter = 0;
        }
    }

    /// <summary>
    /// Returns the template names.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public List<string> GetTemplateNames()
    {
        lock (SyncRoot)
        {
            List<string> names = new List<string>();
            foreach (KeyValuePair<string, TemplateSource> pair in Templates)
            {
                names.Add(pair.Key);
            }

            return names;
        }
    }

    internal TemplateSource CopySource(TemplateSource source)
    {
        TemplateSource copy = new TemplateSource(source.Name, source.Path, source.Version, source.ContentHash, source.Text);
        copy.Timestamp = source.Timestamp;
        return copy;
    }

    internal void RequireTemplateName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Template name is required.", "name");
        }
    }

    internal void ValidateLength(string text)
    {
        if (text.Length > Options.MaximumTemplateLength)
        {
            throw new InvalidOperationException("Maximum template length of " + Options.MaximumTemplateLength.ToString(CultureInfo.InvariantCulture) + " was exceeded.");
        }
    }
}
