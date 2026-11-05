using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Writes maildir in its wire or storage format.
/// </summary>
public class EmailMaildirWriter : IEmailMaildirWriterContract
{
    /// <summary>
    /// The file buffer bytes value exposed by this API.
    /// </summary>
    internal const int FileBufferBytes = 81920;
    string? rootPath;
    string? temporaryPath;
    string? newPath;
    string? currentPath;
    EmailMaildirWriterOptions? options;
    int deliveryCount;
    /// <summary>
    /// Creates a Maildir writer using the supplied policy.
    /// </summary>
    /// <param name = "rootPath">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    public EmailMaildirWriter(string rootPath, EmailMaildirWriterOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        Initialize(rootPath, options.Clone());
    }

    internal static EmailMaildirWriter CreateImplementationWithRootPath(string rootPath)
    {
        return new EmailMaildirWriter(rootPath, new EmailMaildirWriterOptions());
    }

    internal static EmailMaildirWriter CreateImplementationWithRootPathAndOptions(string rootPath, EmailMaildirWriterOptions options)
    {
        return new EmailMaildirWriter(rootPath, options);
    }

    void Initialize(string suppliedRootPath, EmailMaildirWriterOptions suppliedOptions)
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
        temporaryPath = RequireDirectory("tmp");
        newPath = RequireDirectory("new");
        currentPath = RequireDirectory("cur");
        deliveryCount = 0;
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
    public EmailMaildirWriterOptions GetOptions()
    {
        return (options ?? throw new InvalidOperationException()).Clone();
    }

    /// <summary>
    /// Gets the delivery count.
    /// </summary>
    /// <returns>The current delivery count.</returns>
    public int GetDeliveryCount()
    {
        return deliveryCount;
    }

    /// <summary>
    /// Delivers the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The maildir write result produced by the operation.</returns>
    public EmailMaildirWriteResult Deliver(EmailMessage message)
    {
        return DeliverToFolder(message, EmailMaildirFolder.New, EmailMaildirFlags.None);
    }

    /// <summary>
    /// Delivers the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <param name = "flags">The flags used by the operation.</param>
    /// <returns>The maildir write result produced by the operation.</returns>
    public EmailMaildirWriteResult DeliverToFolder(EmailMessage message, EmailMaildirFolder folder, EmailMaildirFlags flags)
    {
        string uniqueName = new EmailMaildirNameOperations().CreateUniqueNameWithHostName((options ?? throw new InvalidOperationException()).GetHostName());
        return DeliverWithUniqueName(message, uniqueName, folder, flags);
    }

    /// <summary>
    /// Delivers the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "uniqueName">The unique name used by the operation.</param>
    /// <param name = "folder">The folder used by the operation.</param>
    /// <param name = "flags">The flags used by the operation.</param>
    /// <returns>The maildir write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMaildirWriteResult DeliverWithUniqueName(EmailMessage message, string uniqueName, EmailMaildirFolder folder, EmailMaildirFlags flags)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        new EmailMaildirNameOperations().ValidateUniqueName(uniqueName, "uniqueName");
        ValidateFolderAndFlags(folder, flags);
        List<EmailMaildirEntry> entries = EnumerateExisting();
        EmailMaildirDeliveryPlan plan = ResolveDeliveryPlan(uniqueName, folder, flags, entries);
        string? destinationDirectory = folder == EmailMaildirFolder.New ? newPath : currentPath;
        string destinationPath = SafeCombine(destinationDirectory, plan.fileName);
        EmailMaildirStagedFile staged = new EmailMaildirStagedFile(temporaryPath, (options ?? throw new InvalidOperationException()).GetLimits().GetMaxPathCharacters());
        EmailStreamWriteResult writeResult = new EmailWriter().WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(message, staged.GetStream(), options.GetWriteOptions());
        if (!writeResult.IsSuccess())
        {
            staged.Close();
            return new EmailMaildirWriteResult(folder, flags, plan.uniqueName, null, null, writeResult, writeResult.GetOutputByteCount(), plan.collisionCount, false, false, false);
        }

        staged.Complete(options.GetFlushToDisk());
        long byteCount = staged.GetByteCount();
        EmailLimitException? limitViolation = GetResultingLimitViolation(byteCount, entries, plan.overwrittenEntry);
        if (limitViolation != null)
        {
            staged.Close();
            throw limitViolation;
        }

        staged.Publish(destinationPath, plan.overwrittenEntry != null);
        staged.Close();
        deliveryCount++;
        return new EmailMaildirWriteResult(folder, flags, plan.uniqueName, plan.fileName, destinationPath, writeResult, byteCount, plan.collisionCount, plan.overwrittenEntry != null, options.GetFlushToDisk(), true);
    }

    /// <summary>
    /// Atomically moves a Maildir entry while retaining applicable current-folder flags.
    /// </summary>
    /// <param name = "sourceFolder">The folder currently containing the entry.</param>
    /// <param name = "sourceFileName">The current filename.</param>
    /// <param name = "destinationFolder">The destination folder.</param>
    /// <returns>The Maildir operation result.</returns>
    public EmailMaildirOperationResult Move(EmailMaildirFolder sourceFolder, string sourceFileName, EmailMaildirFolder destinationFolder)
    {
        ValidateOperationInput(sourceFolder, sourceFileName, destinationFolder, EmailMaildirFlags.None, false);
        EmailMaildirName sourceName = new EmailMaildirNameOperations().Parse(sourceFileName, sourceFolder);
        EmailMaildirFlags flags = sourceFolder == EmailMaildirFolder.Current && destinationFolder == EmailMaildirFolder.Current ? sourceName.GetFlags() : EmailMaildirFlags.None;
        return MoveCore(sourceFolder, sourceFileName, destinationFolder, flags);
    }

    /// <summary>
    /// Atomically moves a Maildir entry and applies standard current-folder flags in canonical order.
    /// </summary>
    /// <param name = "sourceFolder">The folder currently containing the entry.</param>
    /// <param name = "sourceFileName">The current filename.</param>
    /// <param name = "destinationFolder">The destination folder.</param>
    /// <param name = "flags">The resulting standard flags.</param>
    /// <returns>The Maildir operation result.</returns>
    public EmailMaildirOperationResult MoveWithFlags(EmailMaildirFolder sourceFolder, string sourceFileName, EmailMaildirFolder destinationFolder, EmailMaildirFlags flags)
    {
        ValidateOperationInput(sourceFolder, sourceFileName, destinationFolder, flags, true);
        return MoveCore(sourceFolder, sourceFileName, destinationFolder, flags);
    }

    /// <summary>
    /// Atomically updates a current-folder entry's standard flags while retaining supported unknown flags.
    /// </summary>
    /// <param name = "sourceFileName">The current filename.</param>
    /// <param name = "flags">The resulting standard flags.</param>
    /// <returns>The Maildir operation result.</returns>
    public EmailMaildirOperationResult UpdateFlags(string sourceFileName, EmailMaildirFlags flags)
    {
        ValidateOperationInput(EmailMaildirFolder.Current, sourceFileName, EmailMaildirFolder.Current, flags, true);
        return MoveCore(EmailMaildirFolder.Current, sourceFileName, EmailMaildirFolder.Current, flags);
    }

    EmailMaildirOperationResult MoveCore(EmailMaildirFolder sourceFolder, string sourceFileName, EmailMaildirFolder destinationFolder, EmailMaildirFlags flags)
    {
        EmailMaildirNameOperations names = new EmailMaildirNameOperations();
        EmailMaildirName sourceName = names.Parse(sourceFileName, sourceFolder);
        string sourcePath = SafeCombine(GetDirectory(sourceFolder), sourceFileName);
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("The requested Maildir source entry was not found.", sourcePath);
        FileAttributes attributes = File.GetAttributes(sourcePath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new UnauthorizedAccessException("Symbolic links and other reparse points cannot be moved as Maildir entries: " + sourcePath);

        EmailIssueList issues = new EmailIssueList();
        string preservedUnknown = destinationFolder == EmailMaildirFolder.Current ? GetSupportedUnknownFlags(sourceName, sourceFolder, issues) : string.Empty;
        string candidateUniqueName = sourceName.GetUniqueName();
        int collisionCount = 0;
        while (true)
        {
            string destinationFileName = CreateOperationFileName(candidateUniqueName, destinationFolder, flags, preservedUnknown);
            string destinationPath = SafeCombine(GetDirectory(destinationFolder), destinationFileName);
            if (string.Equals(sourcePath, destinationPath, PathComparison()))
            {
                return new EmailMaildirOperationResult(sourceFolder, destinationFolder, sourceFileName, destinationFileName, sourcePath, destinationPath, sourceName.GetFlags(), flags, preservedUnknown, collisionCount, false, false, true, issues);
            }

            EmailMaildirCollisionState collision = FindOperationCollisions(candidateUniqueName, sourcePath, destinationPath);
            if (collision.GetCount() == 0)
            {
                File.Move(sourcePath, destinationPath, false);
                return new EmailMaildirOperationResult(sourceFolder, destinationFolder, sourceFileName, destinationFileName, sourcePath, destinationPath, sourceName.GetFlags(), flags, preservedUnknown, collisionCount, false, true, true, issues);
            }

            EmailMaildirCollisionPolicy policy = (options ?? throw new InvalidOperationException()).GetCollisionPolicy();
            if (policy == EmailMaildirCollisionPolicy.Overwrite && collision.GetCount() == 1 && collision.HasExactDestination())
            {
                File.Move(sourcePath, destinationPath, true);
                return new EmailMaildirOperationResult(sourceFolder, destinationFolder, sourceFileName, destinationFileName, sourcePath, destinationPath, sourceName.GetFlags(), flags, preservedUnknown, collisionCount, true, true, true, issues);
            }

            if (policy == EmailMaildirCollisionPolicy.GenerateUnique && collisionCount < options.GetMaximumCollisionAttempts())
            {
                candidateUniqueName = names.CreateUniqueNameWithHostName(options.GetHostName());
                collisionCount++;
                continue;
            }

            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.MaildirOperationCollision, "A Maildir entry with the requested destination identity already exists.", null, null, destinationFileName, "The source entry was left unchanged."));
            return new EmailMaildirOperationResult(sourceFolder, destinationFolder, sourceFileName, destinationFileName, sourcePath, destinationPath, sourceName.GetFlags(), flags, preservedUnknown, collisionCount, false, false, false, issues);
        }
    }

    EmailMaildirCollisionState FindOperationCollisions(string uniqueName, string sourcePath, string destinationPath)
    {
        EmailMaildirCollisionState state = new EmailMaildirCollisionState();
        AddOperationCollisions(state, temporaryPath, EmailMaildirFolder.Temporary, uniqueName, sourcePath, destinationPath);
        AddOperationCollisions(state, newPath, EmailMaildirFolder.New, uniqueName, sourcePath, destinationPath);
        AddOperationCollisions(state, currentPath, EmailMaildirFolder.Current, uniqueName, sourcePath, destinationPath);
        return state;
    }

    void AddOperationCollisions(EmailMaildirCollisionState state, string? directory, EmailMaildirFolder folder, string uniqueName, string sourcePath, string destinationPath)
    {
        string[] paths = Directory.GetFiles(directory ?? throw new InvalidOperationException(), "*", SearchOption.TopDirectoryOnly);
        int index = 0;
        EmailMaildirNameOperations names = new EmailMaildirNameOperations();
        while (index < paths.Length)
        {
            string path = Path.GetFullPath(paths[index]);
            if (!string.Equals(path, sourcePath, PathComparison()))
            {
                string fileName = Path.GetFileName(path);
                if (names.IsSafeFileName(fileName, folder))
                {
                    EmailMaildirName parsed = names.Parse(fileName, folder);
                    if (string.Equals(parsed.GetUniqueName(), uniqueName, PathComparison()))
                        state.Add(string.Equals(path, destinationPath, PathComparison()));
                }
            }

            index++;
        }
    }

    string GetSupportedUnknownFlags(EmailMaildirName sourceName, EmailMaildirFolder sourceFolder, EmailIssueList issues)
    {
        if (sourceFolder != EmailMaildirFolder.Current || !sourceName.HasInfo())
            return string.Empty;
        if (sourceName.GetInfoVersion() != 2)
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.MaildirUnknownInfoNotPreserved, "The source entry uses an unsupported Maildir info version.", null, null, sourceName.GetFileName(), "The entry was moved using a canonical version 2 suffix without the unsupported info data."));
            return string.Empty;
        }

        StringBuilder supported = new StringBuilder();
        string unknown = sourceName.GetUnknownFlagText();
        bool dropped = false;
        int index = 0;
        while (index < unknown.Length)
        {
            char value = unknown[index];
            if (EmailMaildirName.IsSupportedUnknownFlagCharacterImplementation(value))
                supported.Append(value);
            else
                dropped = true;
            index++;
        }

        if (dropped)
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.MaildirUnknownInfoNotPreserved, "The source entry contains unsupported unknown Maildir flag characters.", null, null, sourceName.GetFileName(), "Supported unknown flag characters were retained and unsupported characters were omitted."));
        string canonicalName = new EmailMaildirNameOperations().CreateCurrentFileNamePreservingUnknownFlags(sourceName.GetUniqueName(), EmailMaildirFlags.None, supported.ToString());
        return new EmailMaildirNameOperations().Parse(canonicalName, EmailMaildirFolder.Current).GetUnknownFlagText();
    }

    string? GetDirectory(EmailMaildirFolder folder)
    {
        if (folder == EmailMaildirFolder.Temporary)
            return temporaryPath;
        return folder == EmailMaildirFolder.New ? newPath : currentPath;
    }

    static string CreateOperationFileName(string uniqueName, EmailMaildirFolder folder, EmailMaildirFlags flags, string preservedUnknown)
    {
        if (folder == EmailMaildirFolder.Current)
            return new EmailMaildirNameOperations().CreateCurrentFileNamePreservingUnknownFlags(uniqueName, flags, preservedUnknown);
        return new EmailMaildirNameOperations().CreateNewFileName(uniqueName);
    }

    static void ValidateOperationInput(EmailMaildirFolder sourceFolder, string sourceFileName, EmailMaildirFolder destinationFolder, EmailMaildirFlags flags, bool explicitFlags)
    {
        if (sourceFolder < EmailMaildirFolder.New || sourceFolder > EmailMaildirFolder.Temporary)
            throw new ArgumentOutOfRangeException("sourceFolder");
        if (destinationFolder < EmailMaildirFolder.New || destinationFolder > EmailMaildirFolder.Temporary)
            throw new ArgumentOutOfRangeException("destinationFolder");
        if (sourceFileName == null)
            throw new ArgumentNullException("sourceFileName");
        if (!new EmailMaildirNameOperations().IsSafeFileName(sourceFileName, sourceFolder))
            throw new ArgumentException("The source filename is not safe for the selected Maildir folder.", "sourceFileName");
        int numericFlags = (int)flags;
        if (numericFlags < 0 || (numericFlags & ~63) != 0)
            throw new ArgumentOutOfRangeException("flags");
        if (explicitFlags && destinationFolder != EmailMaildirFolder.Current && flags != EmailMaildirFlags.None)
            throw new ArgumentException("Only entries in cur can carry Maildir flags.", "flags");
    }

    List<EmailMaildirEntry> EnumerateExisting()
    {
        EmailMaildirReaderOptions readerOptions = new EmailMaildirReaderOptions();
        readerOptions.SetLimits((options ?? throw new InvalidOperationException()).GetLimits());
        return new EmailMaildirReaderOperations().CreateWithRootPathAndOptions(rootPath ?? throw new InvalidOperationException(), readerOptions).Enumerate();
    }

    EmailMaildirDeliveryPlan ResolveDeliveryPlan(string requestedUniqueName, EmailMaildirFolder folder, EmailMaildirFlags flags, List<EmailMaildirEntry> entries)
    {
        string candidate = requestedUniqueName;
        int collisionCount = 0;
        while (true)
        {
            string fileName = CreateFileName(candidate, folder, flags);
            EmailMaildirEntry? exact = null;
            int uniqueCollisionCount = 0;
            int index = 0;
            while (index < entries.Count)
            {
                EmailMaildirEntry entry = entries[index];
                if (string.Equals(entry.GetUniqueName(), candidate, PathComparison()))
                {
                    uniqueCollisionCount++;
                    if (entry.GetFolder() == folder && string.Equals(entry.GetFileName(), fileName, PathComparison()))
                        exact = entry;
                }

                index++;
            }

            if (uniqueCollisionCount == 0)
                return new EmailMaildirDeliveryPlan(candidate, fileName, collisionCount, null);
            EmailMaildirCollisionPolicy policy = (options ?? throw new InvalidOperationException()).GetCollisionPolicy();
            if (policy == EmailMaildirCollisionPolicy.Fail)
                throw new IOException("A Maildir message with the requested unique name already exists: " + candidate);
            if (policy == EmailMaildirCollisionPolicy.Overwrite)
            {
                if (exact == null || uniqueCollisionCount != 1)
                    throw new IOException("Overwrite requires exactly one existing message at the requested Maildir destination.");
                return new EmailMaildirDeliveryPlan(candidate, fileName, collisionCount, exact);
            }

            if (collisionCount >= options.GetMaximumCollisionAttempts())
                throw new IOException("A collision-free Maildir unique name could not be generated within the configured attempt limit.");
            candidate = new EmailMaildirNameOperations().CreateUniqueNameWithHostName(options.GetHostName());
            collisionCount++;
        }
    }

    EmailLimitException? GetResultingLimitViolation(long byteCount, List<EmailMaildirEntry> entries, EmailMaildirEntry? overwrittenEntry)
    {
        EmailArchiveLimits limits = (options ?? throw new InvalidOperationException()).GetLimits();
        if (byteCount > limits.GetMaxEntryBytes())
            return new EmailLimitException("MaxMaildirEntryBytes", limits.GetMaxEntryBytes(), byteCount);
        long archiveBytes = 0;
        int index = 0;
        while (index < entries.Count)
        {
            archiveBytes += entries[index].GetByteCount();
            index++;
        }

        int resultingCount = entries.Count;
        if (overwrittenEntry == null)
            resultingCount++;
        else
            archiveBytes -= overwrittenEntry.GetByteCount();
        if (resultingCount > limits.GetMaxEntryCount())
            return new EmailLimitException("MaxMaildirEntryCount", limits.GetMaxEntryCount(), resultingCount);
        if (byteCount > limits.GetMaxArchiveBytes() - archiveBytes)
            return new EmailLimitException("MaxMaildirArchiveBytes", limits.GetMaxArchiveBytes(), archiveBytes + byteCount);
        return null;
    }

    static string CreateFileName(string uniqueName, EmailMaildirFolder folder, EmailMaildirFlags flags)
    {
        return folder == EmailMaildirFolder.New ? new EmailMaildirNameOperations().CreateNewFileName(uniqueName) : new EmailMaildirNameOperations().CreateCurrentFileName(uniqueName, flags);
    }

    static void ValidateFolderAndFlags(EmailMaildirFolder folder, EmailMaildirFlags flags)
    {
        if (folder < EmailMaildirFolder.New || folder > EmailMaildirFolder.Current)
            throw new ArgumentOutOfRangeException("folder");
        int numericFlags = (int)flags;
        if (numericFlags < 0 || (numericFlags & ~63) != 0)
            throw new ArgumentOutOfRangeException("flags");
        if (folder == EmailMaildirFolder.New && flags != EmailMaildirFlags.None)
            throw new ArgumentException("Files delivered to new cannot carry a cur info suffix or message flags.", "flags");
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
        string prefix = Path.TrimEndingDirectorySeparator(directory) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, PathComparison()))
            throw new UnauthorizedAccessException("A Maildir path resolved outside its expected directory.");
        CheckPathLength(path);
        return path;
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
}
