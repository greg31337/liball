using System;
using System.IO;

namespace email;
/// <summary>
/// Provides operations for working with file.
/// </summary>
public class EmailFile : IEmailFileContract
{
    /// <summary>
    /// The file buffer bytes value exposed by this API.
    /// </summary>
    internal const int FileBufferBytes = 81920;
    internal static EmailFileLoadResult LoadImplementationWithPath(string path)
    {
        return LoadImplementationWithPathAndOptions(path, new EmailFileLoadOptions());
    }

    /// <summary>
    /// Loads the supplied value.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The file load result produced by the operation.</returns>
    public EmailFileLoadResult LoadWithPath(string path)
    {
        return EmailFile.LoadImplementationWithPath(path);
    }

    internal static EmailFileLoadResult LoadImplementationWithPathAndOptions(string path, EmailFileLoadOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailFileLoadOptions snapshot = options.Clone();
        string fullPath = ValidatePath(path, snapshot.GetMaximumPathCharacters(), snapshot.GetRequireEmlExtension(), true);
        FileAttributes attributes = File.GetAttributes(fullPath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new UnauthorizedAccessException("Symbolic links and other reparse points are not accepted as .eml inputs: " + fullPath);
        EmailParseOptions parseOptions = snapshot.GetParseOptions();
        long maximumBytes = parseOptions.GetLimits().GetParsingLimits().GetMaxMessageBytes();
        byte[] bytes = ReadBounded(fullPath, maximumBytes);
        EmailParseResult parsed = AttachSourceName(new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(bytes, parseOptions), fullPath);
        return new EmailFileLoadResult(fullPath, bytes, new FileInfo(fullPath).LastWriteTimeUtc, parsed);
    }

    /// <summary>
    /// Loads the supplied value.
    /// </summary>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The file load result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "UnauthorizedAccessException">Thrown when the operation encounters an unauthorized access.</exception>
    public EmailFileLoadResult LoadWithPathAndOptions(string path, EmailFileLoadOptions options)
    {
        return EmailFile.LoadImplementationWithPathAndOptions(path, options);
    }

    internal static EmailFileSaveResult SaveImplementation(EmailMessage message, string path, EmailFileSaveOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailFileSaveOptions snapshot = options.Clone();
        string fullPath = ValidatePath(path, snapshot.GetMaximumPathCharacters(), snapshot.GetRequireEmlExtension(), false);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException("The .eml destination directory does not exist: " + directory);
        FileAttributes directoryAttributes = File.GetAttributes(directory);
        if ((directoryAttributes & FileAttributes.ReparsePoint) != 0)
            throw new UnauthorizedAccessException("The .eml destination directory cannot be a symbolic link or other reparse point: " + directory);
        bool exists = File.Exists(fullPath);
        if (exists)
        {
            FileAttributes attributes = File.GetAttributes(fullPath);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new UnauthorizedAccessException("A symbolic link or reparse point cannot be overwritten as an .eml file: " + fullPath);
            if (snapshot.GetOverwritePolicy() == EmailFileOverwritePolicy.Fail)
                throw new IOException("The .eml destination already exists: " + fullPath);
        }

        EmailWriteResult writeResult = new EmailWriter().WriteMessageWithMessageAndOptions(message, snapshot.GetWriteOptions());
        if (!writeResult.IsSuccess())
            return new EmailFileSaveResult(fullPath, writeResult, false, false, false);
        string temporaryPath = CreateTemporaryPath(directory, snapshot.GetMaximumPathCharacters());
        EmailFileStagedSave staged = new EmailFileStagedSave(temporaryPath);
        staged.Write(writeResult.GetBytes(), snapshot.GetFlushToDisk());
        staged.Publish(fullPath, snapshot.GetOverwritePolicy() == EmailFileOverwritePolicy.Overwrite);
        staged.Close();
        return new EmailFileSaveResult(fullPath, writeResult, exists, snapshot.GetFlushToDisk(), true);
    }

    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The file save result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "DirectoryNotFoundException">Thrown when the operation encounters a directory not found.</exception>
    /// <exception cref = "IOException">Thrown when the underlying storage or stream operation fails.</exception>
    /// <exception cref = "UnauthorizedAccessException">Thrown when the operation encounters an unauthorized access.</exception>
    public EmailFileSaveResult Save(EmailMessage message, string path, EmailFileSaveOptions options)
    {
        return EmailFile.SaveImplementation(message, path, options);
    }

    static string ValidatePath(string path, int maximumCharacters, bool requireEmlExtension, bool mustExist)
    {
        if (path == null)
            throw new ArgumentNullException("path");
        if (path.Length == 0)
            throw new ArgumentException("An .eml path cannot be empty.", "path");
        string fullPath = Path.GetFullPath(path);
        if (fullPath.Length > maximumCharacters)
            throw new EmailLimitException("MaxEmailFilePathCharacters", maximumCharacters, fullPath.Length);
        if (requireEmlExtension && !string.Equals(Path.GetExtension(fullPath), ".eml", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("An .eml path must use the .eml filename extension.", "path");
        if (mustExist && !File.Exists(fullPath))
            throw new FileNotFoundException("The .eml file does not exist.", fullPath);
        if (Directory.Exists(fullPath))
            throw new ArgumentException("An .eml path must identify a file, not a directory.", "path");
        return fullPath;
    }

    static byte[] ReadBounded(string path, long maximumBytes)
    {
        FileInfo info = new FileInfo(path);
        if (info.Length > maximumBytes)
            throw new EmailLimitException("MaxMessageBytes", maximumBytes, info.Length);
        using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[FileBufferBytes];
            long total = 0;
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;
                if (read > maximumBytes - total)
                    throw new EmailLimitException("MaxMessageBytes", maximumBytes, total + read);
                output.Write(buffer, 0, read);
                total += read;
            }

            return output.ToArray();
        }
    }

    static EmailParseResult AttachSourceName(EmailParseResult parsed, string path)
    {
        if (!parsed.HasMessage() || !(parsed.GetMessage() ?? throw new InvalidOperationException()).HasSourceInfo())
            return parsed;
        EmailMessage? original = parsed.GetMessage();
        EmailSourceInfo? source = (original ?? throw new InvalidOperationException()).GetSourceInfo();
        EmailSourceInfo named = new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete((source ?? throw new InvalidOperationException()).GetContent(), source.GetMessageRange(), source.HasHeaderBlockRange() ? source.GetHeaderBlockRange() : null, source.HasSeparatorRange() ? source.GetSeparatorRange() : null, source.HasBodyRange() ? source.GetBodyRange() : null, path, source.IsComplete());
        EmailMessage message = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(original.GetHeaders(), original.HasBody() ? original.GetBody() : null, (original.GetEnvelope() ?? throw new InvalidOperationException()).Clone(), named, original.GetOwnIssues());
        return new EmailParseResult(message, parsed.GetIssues(), parsed.GetConsumedByteCount(), parsed.GetSourceByteCount(), parsed.IsComplete());
    }

    static string CreateTemporaryPath(string directory, int maximumCharacters)
    {
        string path = Path.GetFullPath(Path.Combine(directory, ".email-save-" + Guid.NewGuid().ToString("N")));
        if (path.Length > maximumCharacters)
            throw new EmailLimitException("MaxEmailFilePathCharacters", maximumCharacters, path.Length);
        return path;
    }
}
