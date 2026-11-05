using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with archive index.
/// </summary>
public class EmailArchiveIndex : IEmailArchiveIndexContract
{
    internal static EmailArchiveIndexRecord CreateImplementationWithMessage(EmailMessage message)
    {
        return CreateImplementationWithMessageAndOptions(message, new EmailArchiveIndexOptions());
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    public EmailArchiveIndexRecord CreateWithMessage(EmailMessage message)
    {
        return EmailArchiveIndex.CreateImplementationWithMessage(message);
    }

    internal static EmailArchiveIndexRecord CreateImplementationWithMessageAndOptions(EmailMessage message, EmailArchiveIndexOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        string? sourcePath = message.HasSourceInfo() && (message.GetSourceInfo() ?? throw new InvalidOperationException()).HasSourceName() ? (message.GetSourceInfo() ?? throw new InvalidOperationException()).GetSourceName() : null;
        EmailRange? messageRange = message.HasSourceInfo() ? (message.GetSourceInfo() ?? throw new InvalidOperationException()).GetMessageRange() : null;
        bool complete = !message.HasSourceInfo() || (message.GetSourceInfo() ?? throw new InvalidOperationException()).IsComplete();
        long byteCount = messageRange == null ? 0 : messageRange.GetLength();
        return CreateRecord(EmailArchiveSourceKind.StandaloneMessage, sourcePath, false, 0, null, messageRange, byteCount, message, complete, message.GetIssues(), null, Snapshot(options));
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArchiveIndexRecord CreateWithMessageAndOptions(EmailMessage message, EmailArchiveIndexOptions options)
    {
        return EmailArchiveIndex.CreateImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailArchiveIndexRecord CreateImplementationWithFile(EmailFileLoadResult file)
    {
        return CreateImplementationWithFileAndOptions(file, new EmailArchiveIndexOptions());
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "file">The file used by the operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    public EmailArchiveIndexRecord CreateWithFile(EmailFileLoadResult file)
    {
        return EmailArchiveIndex.CreateImplementationWithFile(file);
    }

    internal static EmailArchiveIndexRecord CreateImplementationWithFileAndOptions(EmailFileLoadResult file, EmailArchiveIndexOptions options)
    {
        if (file == null)
            throw new ArgumentNullException("file");
        EmailParseResult parsed = file.GetParseResult();
        EmailRange range = new EmailRange(0, file.GetByteCount());
        return CreateRecord(EmailArchiveSourceKind.EmlFile, file.GetPath(), true, 0, range, range, file.GetByteCount(), parsed.HasMessage() ? parsed.GetMessage() : null, parsed.IsComplete(), parsed.GetIssues(), file.GetBytes(), Snapshot(options));
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "file">The file used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArchiveIndexRecord CreateWithFileAndOptions(EmailFileLoadResult file, EmailArchiveIndexOptions options)
    {
        return EmailArchiveIndex.CreateImplementationWithFileAndOptions(file, options);
    }

    internal static EmailArchiveIndexRecord CreateImplementationWithEntryUsingEmailMboxEntry(EmailMboxEntry entry)
    {
        return CreateImplementationWithEntryAndSourcePathAndOptions(entry, null, new EmailArchiveIndexOptions());
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    public EmailArchiveIndexRecord CreateWithEntryUsingEmailMboxEntry(EmailMboxEntry entry)
    {
        return EmailArchiveIndex.CreateImplementationWithEntryUsingEmailMboxEntry(entry);
    }

    internal static EmailArchiveIndexRecord CreateImplementationWithEntryAndOptionsUsingEmailMboxEntryAndEmailArchiveIndexOptions(EmailMboxEntry entry, EmailArchiveIndexOptions options)
    {
        return CreateImplementationWithEntryAndSourcePathAndOptions(entry, null, options);
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    public EmailArchiveIndexRecord CreateWithEntryAndOptionsUsingEmailMboxEntryAndEmailArchiveIndexOptions(EmailMboxEntry entry, EmailArchiveIndexOptions options)
    {
        return EmailArchiveIndex.CreateImplementationWithEntryAndOptionsUsingEmailMboxEntryAndEmailArchiveIndexOptions(entry, options);
    }

    internal static EmailArchiveIndexRecord CreateImplementationWithEntryAndSourcePathAndOptions(EmailMboxEntry entry, string? sourcePath, EmailArchiveIndexOptions options)
    {
        if (entry == null)
            throw new ArgumentNullException("entry");
        bool complete = !entry.GetIssues().ContainsCode(EmailIssueCode.MboxMessageMalformed);
        return CreateRecord(EmailArchiveSourceKind.Mbox, sourcePath, true, entry.GetIndex(), entry.GetArchiveRange(), entry.GetMessageRange(), entry.GetUnescapedMessageByteCount(), entry.HasMessage() ? entry.GetMessage() : null, complete, entry.GetIssues(), null, Snapshot(options));
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <param name = "sourcePath">The file-system or message path to use.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArchiveIndexRecord CreateWithEntryAndSourcePathAndOptions(EmailMboxEntry entry, string? sourcePath, EmailArchiveIndexOptions options)
    {
        return EmailArchiveIndex.CreateImplementationWithEntryAndSourcePathAndOptions(entry, sourcePath, options);
    }

    internal static EmailArchiveIndexRecord CreateImplementationWithEntryUsingEmailMaildirEntry(EmailMaildirEntry entry)
    {
        return CreateImplementationWithEntryAndOptionsUsingEmailMaildirEntryAndEmailArchiveIndexOptions(entry, new EmailArchiveIndexOptions());
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    public EmailArchiveIndexRecord CreateWithEntryUsingEmailMaildirEntry(EmailMaildirEntry entry)
    {
        return EmailArchiveIndex.CreateImplementationWithEntryUsingEmailMaildirEntry(entry);
    }

    internal static EmailArchiveIndexRecord CreateImplementationWithEntryAndOptionsUsingEmailMaildirEntryAndEmailArchiveIndexOptions(EmailMaildirEntry entry, EmailArchiveIndexOptions options)
    {
        if (entry == null)
            throw new ArgumentNullException("entry");
        bool complete = !entry.GetIssues().ContainsCode(EmailIssueCode.MaildirMessageMalformed);
        EmailRange range = new EmailRange(0, entry.GetByteCount());
        return CreateRecord(EmailArchiveSourceKind.Maildir, entry.GetPath(), true, entry.GetIndex(), null, range, entry.GetByteCount(), entry.HasMessage() ? entry.GetMessage() : null, complete, entry.GetIssues(), null, Snapshot(options));
    }

    /// <summary>
    /// Creates a new instance from the supplied values.
    /// </summary>
    /// <param name = "entry">The entry used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The archive index record produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailArchiveIndexRecord CreateWithEntryAndOptionsUsingEmailMaildirEntryAndEmailArchiveIndexOptions(EmailMaildirEntry entry, EmailArchiveIndexOptions options)
    {
        return EmailArchiveIndex.CreateImplementationWithEntryAndOptionsUsingEmailMaildirEntryAndEmailArchiveIndexOptions(entry, options);
    }

    static EmailArchiveIndexRecord CreateRecord(EmailArchiveSourceKind sourceKind, string? sourcePath, bool hasIndex, int index, EmailRange? archiveRange, EmailRange? messageRange, long messageByteCount, EmailMessage? message, bool complete, EmailIssueList issues, byte[]? exactBytes, EmailArchiveIndexOptions options)
    {
        EmailArchiveParseStatus status = GetStatus(message, complete, issues);
        List<EmailArchiveIndexHeader> headers = message == null ? new List<EmailArchiveIndexHeader>() : SelectHeaders(message, options);
        if (exactBytes != null && exactBytes.LongLength > options.GetMaximumHashBytes())
            throw new EmailLimitException("MaxArchiveIndexHashBytes", options.GetMaximumHashBytes(), exactBytes.LongLength);
        EmailHashResult? exactHash = exactBytes == null ? ComputeMessageExactHash(message, options) : new EmailHash().ComputeExactWithContentAndAlgorithm(exactBytes, options.GetHashAlgorithm());
        EmailHashResult? semanticHash = null;
        EmailMessageSummary? summary = null;
        if (message != null)
        {
            summary = new EmailMessageAnalyzer().AnalyzeWithMessageAndOptions(message, options.GetAnalysisOptions());
            if (status == EmailArchiveParseStatus.Success && options.GetIncludeSemanticHash())
                semanticHash = ComputeSemanticHash(message, options);
        }

        long indexedBytes = messageByteCount;
        if (indexedBytes == 0 && exactHash != null)
            indexedBytes = exactHash.GetHashedByteCount();
        return new EmailArchiveIndexRecord(sourceKind, sourcePath, hasIndex, index, archiveRange, messageRange, indexedBytes, status, headers, exactHash, semanticHash, summary, issues);
    }

    static EmailHashResult? ComputeMessageExactHash(EmailMessage? message, EmailArchiveIndexOptions options)
    {
        if (message == null)
            return null;
        long maximum = options.GetMaximumHashBytes();
        if (message.HasSourceInfo() && !message.IsExactDirty())
            return new EmailFingerprint().ComputeExactWithMessageAndAlgorithmAndMaximumBytes(message, options.GetHashAlgorithm(), maximum);
        EmailWriteOptions writeOptions = new EmailWriteOptionsOperations().CreateMinimalRegeneration();
        writeOptions.SetMaximumOutputBytes(maximum);
        EmailWriteResult written = new EmailWriter().WriteMessageWithMessageAndOptions(message, writeOptions);
        if (!written.IsSuccess())
            throw new InvalidOperationException("The message could not be serialized for exact archive indexing.");
        return new EmailHash().ComputeExactWithContentAndAlgorithm(written.GetBytes(), options.GetHashAlgorithm());
    }

    static EmailHashResult? ComputeSemanticHash(EmailMessage message, EmailArchiveIndexOptions options)
    {
        EmailCanonicalComparisonOptions semantic = options.GetSemanticOptions();
        semantic.SetHashAlgorithm(options.GetHashAlgorithm());
        if (semantic.GetMaximumOutputBytes() > options.GetMaximumHashBytes())
            semantic.SetMaximumOutputBytes(options.GetMaximumHashBytes());
        EmailMessage filtered = EmailFingerprintFilter.FilterMessage(message, semantic);
        EmailWriteOptions writeOptions = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        writeOptions.SetDeterministicOutput(true);
        writeOptions.SetDeterministicSeed(semantic.GetDeterministicSeed());
        writeOptions.SetIncludeBlindRecipientHeaders(true);
        writeOptions.SetMaximumOutputBytes(semantic.GetMaximumOutputBytes());
        writeOptions.SetMaximumNestingDepth(semantic.GetMaximumDepth() == 0 ? 1 : semantic.GetMaximumDepth());
        writeOptions.SetMaximumPartCount(semantic.GetMaximumEntityCount());
        EmailWriteResult written = new EmailWriter().WriteMessageWithMessageAndOptions(filtered, writeOptions);
        if (!written.IsSuccess())
            return null;
        return new EmailHash().ComputeExactWithContentAndAlgorithm(written.GetBytes(), semantic.GetHashAlgorithm());
    }

    static List<EmailArchiveIndexHeader> SelectHeaders(EmailMessage message, EmailArchiveIndexOptions options)
    {
        List<EmailArchiveIndexHeader> selected = new List<EmailArchiveIndexHeader>();
        Dictionary<string, int> occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int characters = 0;
        int index = 0;
        while (index < (message.GetHeaders() ?? throw new InvalidOperationException()).GetCount())
        {
            EmailHeader header = (message.GetHeaders() ?? throw new InvalidOperationException()).GetAt(index);
            if (options.ContainsSelectedHeaderName(header.GetName()))
            {
                if (selected.Count >= options.GetMaximumSelectedHeaderCount())
                    throw new EmailLimitException("MaxArchiveIndexHeaders", options.GetMaximumSelectedHeaderCount(), (long)selected.Count + 1);
                string? value = header.GetDecodedValue();
                if ((value ?? throw new InvalidOperationException()).Length > options.GetMaximumSelectedHeaderCharacters() - characters)
                    throw new EmailLimitException("MaxArchiveIndexHeaderCharacters", options.GetMaximumSelectedHeaderCharacters(), (long)characters + value.Length);
                int occurrence = occurrences.ContainsKey(header.GetName()) ? occurrences[header.GetName()] : 0;
                occurrences[header.GetName()] = occurrence + 1;
                selected.Add(new EmailArchiveIndexHeader(header.GetName(), value, occurrence));
                characters += value.Length;
            }

            index++;
        }

        return selected;
    }

    static EmailArchiveParseStatus GetStatus(EmailMessage? message, bool complete, EmailIssueList issues)
    {
        if (message == null)
            return EmailArchiveParseStatus.Failed;
        if (!complete)
            return EmailArchiveParseStatus.Partial;
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Error) ? EmailArchiveParseStatus.CompleteWithErrors : EmailArchiveParseStatus.Success;
    }

    static EmailArchiveIndexOptions Snapshot(EmailArchiveIndexOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        return options.Clone();
    }
}
