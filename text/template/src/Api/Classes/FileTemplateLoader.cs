using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace template;

/// <summary>
/// Loads named templates from a fixed root directory while rejecting rooted paths and directory traversal.
/// </summary>
/// <remarks>
/// Template names are relative to <c>RootPath</c>. Resolved paths must remain inside that root after full-path normalization.
/// </remarks>
public class FileTemplateLoader : ITemplateLoader, IFileTemplateLoader
{
    /// <summary>
    /// Specifies the root path for this file template loader.
    /// </summary>
    internal string RootPath;
    internal string PhysicalRootPath;
    internal TemplateLoaderOptions Options;
    internal TemplateSecurityPolicy? SecurityPolicy;

    /// <summary>
    /// Initializes a new file template loader and validates any supplied values.
    /// </summary>
    /// <param name="rootPath">The directory within which template names are resolved.</param>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public FileTemplateLoader(string rootPath)
    {
        if (rootPath == null)
        {
            throw new ArgumentNullException("rootPath");
        }

        if (rootPath.Length == 0)
        {
            throw new ArgumentException("Loader root is required.", "rootPath");
        }

        RootPath = NormalizeRootPath(rootPath);
        PhysicalRootPath = ResolvePhysicalPath(RootPath, true);
        Options = new TemplateLoaderOptions();
        SecurityPolicy = null;
    }

    /// <summary>
    /// Configures text decoding and maximum decoded template length.
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
    /// Sets the security policy used to authorize the configured root and every resolved template path.
    /// </summary>
    /// <param name="securityPolicy">The policy that supplies allowed loader roots.</param>
    public void SetSecurityPolicy(TemplateSecurityPolicy securityPolicy)
    {
        if (securityPolicy == null)
        {
            throw new ArgumentNullException("securityPolicy");
        }

        securityPolicy.Validate();
        if (!securityPolicy.IsLoaderPathAllowed(RootPath))
        {
            throw new InvalidOperationException("The configured loader root is not allowed by the security policy.");
        }

        SecurityPolicy = securityPolicy;
    }

    /// <summary>
    /// Gets the normalized loader root.
    /// </summary>
    /// <returns>The normalized root path.</returns>
    public string GetRootPath()
    {
        return RootPath;
    }

    /// <summary>
    /// Determines whether the requested item exists without returning its contents.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool ContainsTemplate(string name)
    {
        string path = GetTemplatePath(name);
        return File.Exists(path);
    }

    /// <summary>
    /// Returns a defensive copy of the named template, or <c>null</c> when no template is registered.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The matching source, or <c>null</c> when it is not found.</returns>
    public TemplateSource? Load(string name)
    {
        string path = GetTemplatePath(name);
        if (!File.Exists(path))
        {
            return null;
        }

        string text = File.ReadAllText(path, Options.Encoding);
        return CreateSource(NormalizeTemplateName(name), path, text);
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
        string path = GetTemplatePath(name);
        string normalizedName = NormalizeTemplateName(name);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource?>(cancellationToken);
        }

        if (!File.Exists(path))
        {
            return Task.FromResult<TemplateSource?>(null);
        }

        return ContinueLoadAsync(File.ReadAllTextAsync(path, Options.Encoding, cancellationToken), normalizedName, path);
    }

    /// <summary>
    /// Returns the named template and throws when it cannot be found.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="FileNotFoundException">Thrown when a required named template does not exist.</exception>
    public TemplateSource LoadRequired(string name)
    {
        TemplateSource? source = Load(name);
        if (source == null)
        {
            throw new FileNotFoundException("Template '" + name + "' was not found.", name);
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
    /// <exception cref="FileNotFoundException">Thrown when a required named template does not exist.</exception>
    public Task<TemplateSource> LoadRequiredWithCancellationAsync(string name, CancellationToken cancellationToken)
    {
        string path = GetTemplatePath(name);
        string normalizedName = NormalizeTemplateName(name);
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<TemplateSource>(cancellationToken);
        }

        if (!File.Exists(path))
        {
            return Task.FromException<TemplateSource>(new FileNotFoundException("Template '" + name + "' was not found.", name));
        }

        return ContinueRequiredLoadAsync(File.ReadAllTextAsync(path, Options.Encoding, cancellationToken), normalizedName, path);
    }

    internal Task<TemplateSource?> ContinueLoadAsync(Task<string> task, string normalizedName, string path)
    {
        return task.ContinueWith((Task<string> completed) =>
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

            return Task.FromResult<TemplateSource?>(CreateSource(normalizedName, path, completed.Result));
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    internal Task<TemplateSource> ContinueRequiredLoadAsync(Task<string> task, string normalizedName, string path)
    {
        return task.ContinueWith((Task<string> completed) =>
        {
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

            return Task.FromResult(CreateSource(normalizedName, path, completed.Result));
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();
    }

    /// <summary>
    /// Returns the template path.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the operation violates template structure, configuration, security, or runtime limits.</exception>
    public string GetTemplatePath(string name)
    {
        string templateName = NormalizeTemplateName(name);
        string combined = Path.Combine(RootPath, templateName);
        string fullPath = Path.GetFullPath(combined);
        if (!IsPathInsideRoot(fullPath, RootPath))
        {
            throw new InvalidOperationException("Template path escapes the configured loader root.");
        }

        ValidateResolvedPath(fullPath);

        return fullPath;
    }

    /// <summary>
    /// Returns the template names.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public List<string> GetTemplateNames()
    {
        List<string> names = new List<string>();
        if (!Directory.Exists(RootPath))
        {
            return names;
        }

        List<string> directories = new List<string>();
        List<string> physicalDirectories = new List<string>();
        directories.Add(RootPath);
        physicalDirectories.Add(ResolvePhysicalPath(RootPath, true));
        int directoryIndex = 0;
        while (directoryIndex < directories.Count)
        {
            string directory = directories[directoryIndex];
            string[] files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
            int fileIndex = 0;
            while (fileIndex < files.Length)
            {
                ValidateResolvedPath(files[fileIndex]);
                string relative = Path.GetRelativePath(RootPath, files[fileIndex]);
                names.Add(NormalizeTemplateNameSeparators(relative));
                fileIndex = fileIndex + 1;
            }

            string[] childDirectories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
            int childIndex = 0;
            while (childIndex < childDirectories.Length)
            {
                ValidateResolvedPath(childDirectories[childIndex]);
                string physicalDirectory = ResolvePhysicalPath(childDirectories[childIndex], true);
                if (!ContainsPath(physicalDirectories, physicalDirectory))
                {
                    physicalDirectories.Add(physicalDirectory);
                    directories.Add(childDirectories[childIndex]);
                }

                childIndex = childIndex + 1;
            }

            directoryIndex = directoryIndex + 1;
        }

        return names;
    }

    internal bool ContainsPath(List<string> paths, string path)
    {
        int index = 0;
        while (index < paths.Count)
        {
            if (PathsEqual(paths[index], path))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateResolvedPath(string path)
    {
        if (SecurityPolicy != null && !SecurityPolicy.IsLoaderPathAllowed(path))
        {
            throw new InvalidOperationException("Template path is outside the loader roots allowed by the security policy.");
        }

        string relative = Path.GetRelativePath(RootPath, path);
        string[] segments = relative.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        string logicalCurrent = RootPath;
        string physicalRoot = ResolvePhysicalPath(RootPath, true);
        string physicalCurrent = physicalRoot;
        int index = 0;
        while (index < segments.Length)
        {
            logicalCurrent = Path.Combine(logicalCurrent, segments[index]);
            string combinedPhysical = Path.GetFullPath(Path.Combine(physicalCurrent, segments[index]));
            if (Directory.Exists(logicalCurrent))
            {
                physicalCurrent = ResolvePhysicalPath(logicalCurrent, true);
            }
            else if (File.Exists(logicalCurrent))
            {
                physicalCurrent = ResolvePhysicalPath(logicalCurrent, false);
            }
            else
            {
                physicalCurrent = combinedPhysical;
            }

            if (!IsPathInsideRoot(physicalCurrent, physicalRoot))
            {
                throw new InvalidOperationException("Template path escapes the configured loader root through a symbolic link.");
            }

            index = index + 1;
        }

        PhysicalRootPath = physicalRoot;
    }

    internal string ResolvePhysicalPath(string path, bool directory)
    {
        if (directory && !Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        if (!directory && !File.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        FileSystemInfo info;
        if (directory)
        {
            info = new DirectoryInfo(path);
        }
        else
        {
            info = new FileInfo(path);
        }

        FileSystemInfo? target = info.ResolveLinkTarget(true);
        if (target != null)
        {
            return Path.GetFullPath(target.FullName);
        }

        return Path.GetFullPath(path);
    }

    internal string NormalizeTemplateName(string name)
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
            throw new InvalidOperationException("Template names must be relative to the configured loader root.");
        }

        return NormalizeTemplateNameSeparators(name);
    }

    internal string NormalizeTemplateNameSeparators(string name)
    {
        string normalized = name.Replace('\\', Path.DirectorySeparatorChar);
        normalized = normalized.Replace('/', Path.DirectorySeparatorChar);
        if (Path.DirectorySeparatorChar != '/')
        {
            normalized = normalized.Replace(Path.DirectorySeparatorChar, '/');
        }

        return normalized;
    }

    internal string NormalizeRootPath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (trimmed.Length == 0)
        {
            return fullPath;
        }

        return trimmed;
    }

    internal bool IsPathInsideRoot(string path, string root)
    {
        if (PathsEqual(path, root))
        {
            return true;
        }

        string rootPrefix = root + Path.DirectorySeparatorChar.ToString();
        return path.StartsWith(rootPrefix, StringComparison.Ordinal);
    }

    internal bool PathsEqual(string left, string right)
    {
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    internal TemplateSource CreateSource(string name, string path, string text)
    {
        if (text.Length > Options.MaximumTemplateLength)
        {
            throw new InvalidOperationException("Maximum template length of " + Options.MaximumTemplateLength.ToString(CultureInfo.InvariantCulture) + " was exceeded.");
        }

        string version = File.GetLastWriteTimeUtc(path).Ticks.ToString(CultureInfo.InvariantCulture);
        string contentHash = TemplateSource.ComputeContentHash(text);
        TemplateSource source = new TemplateSource(name, path, version, contentHash, text);
        source.Timestamp = version;
        return source;
    }
}
