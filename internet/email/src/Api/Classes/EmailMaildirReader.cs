using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Reads and interprets maildir.
/// </summary>
public class EmailMaildirReader : IEmailMaildirReaderContract
{
    string? rootPath;
    string? newPath;
    string? currentPath;
    EmailMaildirReaderOptions? options;
    List<EmailMaildirEntry>? entries;
    EmailMaildirEntry? current;
    EmailIssueList? issues;
    int nextIndex;
    /// <summary>
    /// Creates a Maildir reader using the supplied policy.
    /// </summary>
    /// <param name = "rootPath">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    public EmailMaildirReader(string rootPath, EmailMaildirReaderOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        Initialize(rootPath, options.Clone());
    }

    internal static EmailMaildirReader CreateImplementationWithRootPath(string rootPath)
    {
        return new EmailMaildirReader(rootPath, new EmailMaildirReaderOptions());
    }

    internal static EmailMaildirReader CreateImplementationWithRootPathAndOptions(string rootPath, EmailMaildirReaderOptions options)
    {
        return new EmailMaildirReader(rootPath, options);
    }

    void Initialize(string suppliedRootPath, EmailMaildirReaderOptions suppliedOptions)
    {
        if (suppliedRootPath == null)
            throw new ArgumentNullException("rootPath");
        if (suppliedRootPath.Length == 0)
            throw new ArgumentException("A Maildir root path cannot be empty.", "rootPath");
        options = suppliedOptions;
        rootPath = Path.GetFullPath(suppliedRootPath);
        CheckPathLength(rootPath);
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException("The Maildir root does not exist: " + rootPath);
        newPath = RequireDirectory("new");
        currentPath = RequireDirectory("cur");
        RequireDirectory("tmp");
        entries = null;
        current = null;
        issues = new EmailIssueList();
        nextIndex = 0;
    }

    /// <summary>
    /// Gets the root path.
    /// </summary>
    /// <returns>The current root path.</returns>
    public string? GetRootPath()
    {
        return rootPath;
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    public EmailMaildirReaderOptions GetOptions()
    {
        return (options ?? throw new InvalidOperationException()).Clone();
    }

    /// <summary>
    /// Enumerates the supplied value.
    /// </summary>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailMaildirEntry> Enumerate()
    {
        EmailMaildirScanState state = new EmailMaildirScanState();
        ScanDirectory(newPath, EmailMaildirFolder.New, state);
        ScanDirectory(currentPath, EmailMaildirFolder.Current, state);
        entries = state.entries;
        issues = state.issues;
        current = null;
        nextIndex = 0;
        return new List<EmailMaildirEntry>(entries);
    }

    /// <summary>
    /// Gets the entries.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current entries.</returns>
    public List<EmailMaildirEntry> GetEntries()
    {
        if (entries == null)
            Enumerate();
        return new List<EmailMaildirEntry>((entries ?? throw new InvalidOperationException()));
    }

    /// <summary>
    /// Reads the next from the input.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ReadNext()
    {
        if (entries == null)
            Enumerate();
        current = null;
        if (nextIndex >= (entries ?? throw new InvalidOperationException()).Count)
            return false;
        current = ReadEntry(entries[nextIndex]);
        entries[nextIndex] = current;
        nextIndex++;
        return true;
    }

    /// <summary>
    /// Reads the next value from the input.
    /// </summary>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <returns>The maildir entry produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    /// <exception cref = "FileNotFoundException">Thrown when the operation encounters a file not found.</exception>
    public EmailMaildirEntry Read(EmailMaildirFolder folder, string fileName)
    {
        ValidateFolder(folder);
        if (fileName == null)
            throw new ArgumentNullException("fileName");
        if (fileName.Length > (options ?? throw new InvalidOperationException()).GetLimits().GetMaxPathCharacters())
            throw new EmailLimitException("MaxMaildirPathCharacters", options.GetLimits().GetMaxPathCharacters(), fileName.Length);
        if (!new EmailMaildirNameOperations().IsSafeFileName(fileName, folder))
            throw new ArgumentException("The requested Maildir filename is not a safe single path segment for the selected folder.", "fileName");
        if (entries == null)
            Enumerate();
        int index = 0;
        while (index < (entries ?? throw new InvalidOperationException()).Count)
        {
            EmailMaildirEntry entry = entries[index];
            if (entry.GetFolder() == folder && string.Equals(entry.GetFileName(), fileName, StringComparison.Ordinal))
            {
                EmailMaildirEntry read = ReadEntry(entry);
                entries[index] = read;
                return read;
            }

            index++;
        }

        string? directory = folder == EmailMaildirFolder.New ? newPath : currentPath;
        throw new FileNotFoundException("The requested Maildir message was not found.", Path.Combine((directory ?? throw new InvalidOperationException()), fileName));
    }

    /// <summary>
    /// Reads the next value from the input.
    /// </summary>
    /// <param name = "relativePath">The file-system or message path to use.</param>
    /// <returns>The maildir entry produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailMaildirEntry ReadRelativePath(string relativePath)
    {
        if (relativePath == null)
            throw new ArgumentNullException("relativePath");
        int maximum = (options ?? throw new InvalidOperationException()).GetLimits().GetMaxPathCharacters();
        if (relativePath.Length > maximum)
            throw new EmailLimitException("MaxMaildirPathCharacters", maximum, relativePath.Length);
        if (relativePath.Length == 0 || Path.IsPathRooted(relativePath) || relativePath.IndexOf('\\') >= 0)
            throw new ArgumentException("A Maildir relative path must identify one file directly beneath new or cur.", "relativePath");
        int separator = relativePath.IndexOf('/');
        if (separator <= 0 || separator != relativePath.LastIndexOf('/') || separator == relativePath.Length - 1)
            throw new ArgumentException("A Maildir relative path must identify one file directly beneath new or cur.", "relativePath");
        string folder = relativePath.Substring(0, separator);
        string fileName = relativePath.Substring(separator + 1);
        if (folder == "new")
            return Read(EmailMaildirFolder.New, fileName);
        if (folder == "cur")
            return Read(EmailMaildirFolder.Current, fileName);
        throw new ArgumentException("A Maildir relative path must begin with new/ or cur/.", "relativePath");
    }

    /// <summary>
    /// Determines whether the current is present or available.
    /// </summary>
    /// <returns><c>true</c> when the current is available; otherwise, <c>false</c>.</returns>
    public bool HasCurrent()
    {
        return current != null;
    }

    /// <summary>
    /// Gets the current.
    /// </summary>
    /// <returns>The current current.</returns>
    public EmailMaildirEntry? GetCurrent()
    {
        return current;
    }

    /// <summary>
    /// Gets the entry count.
    /// </summary>
    /// <returns>The current entry count.</returns>
    public int GetEntryCount()
    {
        return entries == null ? 0 : entries.Count;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    void ScanDirectory(string? directory, EmailMaildirFolder folder, EmailMaildirScanState state)
    {
        string[] paths = Directory.GetFiles((directory ?? throw new InvalidOperationException()), "*", SearchOption.TopDirectoryOnly);
        Array.Sort(paths, StringComparer.Ordinal);
        EmailArchiveLimits limits = (options ?? throw new InvalidOperationException()).GetLimits();
        int pathIndex = 0;
        while (pathIndex < paths.Length)
        {
            string path = Path.GetFullPath(paths[pathIndex]);
            CheckContained(directory, path);
            CheckPathLength(path);
            string fileName = Path.GetFileName(path);
            if (!new EmailMaildirNameOperations().IsSafeFileName(fileName, folder))
            {
                state.issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.MaildirNameMalformed, "A Maildir directory entry does not have a safe name for its folder and was ignored.", null, null, fileName, "The unsafe directory entry was not opened or parsed."));
                pathIndex++;
                continue;
            }

            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new UnauthorizedAccessException("Symbolic links and other reparse points are not accepted as Maildir messages: " + path);
            FileInfo info = new FileInfo(path);
            long length = info.Length;
            if (length > limits.GetMaxEntryBytes())
                throw new EmailLimitException("MaxMaildirEntryBytes", limits.GetMaxEntryBytes(), length);
            if (state.entries.Count >= limits.GetMaxEntryCount())
                throw new EmailLimitException("MaxMaildirEntryCount", limits.GetMaxEntryCount(), (long)state.entries.Count + 1);
            if (length > limits.GetMaxArchiveBytes() - state.archiveBytes)
                throw new EmailLimitException("MaxMaildirArchiveBytes", limits.GetMaxArchiveBytes(), state.archiveBytes + length);
            EmailMaildirName name = new EmailMaildirNameOperations().Parse(fileName, folder);
            EmailIssueList entryIssues = CreateNameIssues(name, folder);
            state.issues.AddRange(entryIssues);
            EmailMaildirEntry entry = new EmailMaildirEntry(state.entries.Count, folder, name, path, length, info.LastWriteTimeUtc, null, entryIssues);
            state.entries.Add(entry);
            state.archiveBytes += length;
            pathIndex++;
        }
    }

    EmailMaildirEntry ReadEntry(EmailMaildirEntry entry)
    {
        string? directory = entry.GetFolder() == EmailMaildirFolder.New ? newPath : currentPath;
        string expectedPath = SafeCombine(directory, entry.GetFileName());
        if (!string.Equals(expectedPath, entry.GetPath(), PathComparison()))
            throw new UnauthorizedAccessException("The Maildir entry path no longer resolves to its enumerated location.");
        FileAttributes attributes = File.GetAttributes(expectedPath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new UnauthorizedAccessException("Symbolic links and other reparse points are not accepted as Maildir messages: " + expectedPath);
        byte[] bytes = ReadBounded(expectedPath, (options ?? throw new InvalidOperationException()).GetLimits().GetMaxEntryBytes());
        EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(bytes, options.GetParseOptions());
        EmailIssueList entryIssues = entry.GetIssues();
        entryIssues.AddRange(parsed.GetIssues());
        if (!parsed.IsComplete())
            entryIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.MaildirMessageMalformed, "The Maildir file is not a complete Internet message.", null, null, entry.GetFileName(), "The parser's partial message and diagnostics were retained."));
        (issues ?? throw new InvalidOperationException()).AddRange(parsed.GetIssues());
        if (!parsed.IsComplete())
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.MaildirMessageMalformed, "A Maildir file is not a complete Internet message.", null, null, entry.GetFileName(), "The parser's partial message and diagnostics were retained."));
        return new EmailMaildirEntry(entry.GetIndex(), entry.GetFolder(), entry.GetName(), entry.GetPath(), bytes.LongLength, entry.GetLastWriteTimeUtc(), parsed.HasMessage() ? parsed.GetMessage() : null, entryIssues);
    }

    byte[] ReadBounded(string path, long maximumBytes)
    {
        using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[81920];
            long total = 0;
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;
                if (read > maximumBytes - total)
                    throw new EmailLimitException("MaxMaildirEntryBytes", maximumBytes, total + read);
                output.Write(buffer, 0, read);
                total += read;
            }

            return output.ToArray();
        }
    }

    EmailIssueList CreateNameIssues(EmailMaildirName name, EmailMaildirFolder folder)
    {
        EmailIssueList result = new EmailIssueList();
        if (folder == EmailMaildirFolder.Current && !name.IsInfoWellFormed())
            result.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.MaildirNameMalformed, "A file in cur does not contain a well-formed Maildir info suffix.", null, null, name.GetFileName(), "The safe unique name and message file were retained."));
        else if (name.GetInfoVersion() == 2 && !name.IsCanonical())
            result.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.MaildirFlagsMalformed, "Maildir version 2 flags are duplicated or not stored in ASCII order.", null, null, name.GetFileName(), "Recognized flags were parsed and unknown flags were preserved."));
        return result;
    }

    string RequireDirectory(string name)
    {
        string path = SafeCombine(rootPath, name);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException("The Maildir subdirectory does not exist: " + path);
        FileAttributes attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new UnauthorizedAccessException("Maildir subdirectories cannot be symbolic links or other reparse points: " + path);
        return path;
    }

    string SafeCombine(string? directory, string name)
    {
        string path = Path.GetFullPath(Path.Combine((directory ?? throw new InvalidOperationException()), name));
        CheckContained(directory, path);
        CheckPathLength(path);
        return path;
    }

    void CheckContained(string directory, string path)
    {
        string prefix = Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, PathComparison()))
            throw new UnauthorizedAccessException("A Maildir path resolved outside its expected directory.");
    }

    void CheckPathLength(string path)
    {
        int maximum = (options ?? throw new InvalidOperationException()).GetLimits().GetMaxPathCharacters();
        if (path.Length > maximum)
            throw new EmailLimitException("MaxMaildirPathCharacters", maximum, path.Length);
    }

    static StringComparison PathComparison()
    {
        return OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    static void ValidateFolder(EmailMaildirFolder folder)
    {
        if (folder < EmailMaildirFolder.New || folder > EmailMaildirFolder.Current)
            throw new ArgumentOutOfRangeException("folder");
    }
}
