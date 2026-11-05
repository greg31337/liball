using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Represents a partial message collector.
/// </summary>
public class EmailPartialMessageCollector : IEmailPartialMessageCollectorContract
{
    EmailPartialMessageAssemblyOptions? options;
    Dictionary<string, List<EmailPartialMessageFragment>>? groups;
    List<string>? groupIds;
    EmailIssueList? issues;
    int observedFragmentCount;
    int rejectedFragmentCount;
    /// <summary>
    /// Creates a partial message collector using the supplied policy.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailPartialMessageCollector(EmailPartialMessageAssemblyOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        Initialize(options.Clone());
    }

    internal static EmailPartialMessageCollector CreateImplementationWith()
    {
        return new EmailPartialMessageCollector(new EmailPartialMessageAssemblyOptions());
    }

    internal static EmailPartialMessageCollector CreateImplementationWithOptions(EmailPartialMessageAssemblyOptions options)
    {
        return new EmailPartialMessageCollector(options);
    }

    void Initialize(EmailPartialMessageAssemblyOptions suppliedOptions)
    {
        options = suppliedOptions;
        groups = new Dictionary<string, List<EmailPartialMessageFragment>>(StringComparer.Ordinal);
        groupIds = new List<string>();
        issues = new EmailIssueList();
        observedFragmentCount = 0;
        rejectedFragmentCount = 0;
    }

    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    public EmailPartialMessageAssemblyOptions GetOptions()
    {
        return (options ?? throw new InvalidOperationException()).Clone();
    }

    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void Add(EmailMessage message)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (!message.HasBody() || (message.GetBody() ?? throw new InvalidOperationException()).GetKind() != MimeEntityKind.PartialMessage)
            throw new ArgumentException("The message body must be a message/partial entity.", "message");
        AddFragment(message.GetBody(), message.GetHeaders(), message.GetEnvelope());
    }

    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public void AddEntity(MimeEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException("entity");
        if (entity.GetKind() != MimeEntityKind.PartialMessage)
            throw new ArgumentException("The MIME entity must be message/partial.", "entity");
        AddFragment(entity, entity.GetHeaders(), new EmailEnvelopeOperations().CreateWith());
    }

    void AddFragment(MimeEntity? entity, EmailHeaders? outerHeaders, EmailEnvelope? envelope)
    {
        if (observedFragmentCount >= (options ?? throw new InvalidOperationException()).GetMaximumFragmentCount())
            throw new EmailLimitException("MaxPartialFragmentCount", options.GetMaximumFragmentCount(), observedFragmentCount + 1);
        observedFragmentCount++;
        if ((entity ?? throw new InvalidOperationException()).HasContent() && (entity.GetContent() ?? throw new InvalidOperationException()).HasKnownLength() && (entity.GetContent() ?? throw new InvalidOperationException()).GetLength() > options.GetMaximumFragmentBytes())
            throw new EmailLimitException("MaxPartialFragmentBytes", options.GetMaximumFragmentBytes(), (entity.GetContent() ?? throw new InvalidOperationException()).GetLength());
        EmailPartialMessageMetadata? metadata = entity.GetPartialMessageMetadata();
        if (metadata == null || !metadata.HasRequiredMetadata())
        {
            rejectedFragmentCount++;
            (issues ?? throw new InvalidOperationException()).Add(Issue(EmailIssueCode.PartialMessageAssemblyMalformed, "A message/partial fragment without a valid id and number cannot be collected.", "The fragment was counted against limits but was not assigned to a group."));
            return;
        }

        long number = metadata.GetFragmentNumber();
        if (number > options.GetMaximumFragmentCount())
            throw new EmailLimitException("MaxPartialFragmentCount", options.GetMaximumFragmentCount(), number);
        if (metadata.HasTotalFragments() && metadata.GetTotalFragments() > options.GetMaximumFragmentCount())
            throw new EmailLimitException("MaxPartialFragmentCount", options.GetMaximumFragmentCount(), metadata.GetTotalFragments());
        string? id = metadata.GetFragmentId();
        if (!(groups ?? throw new InvalidOperationException()).ContainsKey((id ?? throw new InvalidOperationException())))
        {
            groups.Add(id, new List<EmailPartialMessageFragment>());
            (groupIds ?? throw new InvalidOperationException()).Add(id);
        }

        groups[id].Add(new EmailPartialMessageFragment(entity, outerHeaders ?? throw new InvalidOperationException(), envelope, observedFragmentCount - 1));
    }

    /// <summary>
    /// Gets the observed fragment count.
    /// </summary>
    /// <returns>The current observed fragment count.</returns>
    public int GetObservedFragmentCount()
    {
        return observedFragmentCount;
    }

    /// <summary>
    /// Gets the rejected fragment count.
    /// </summary>
    /// <returns>The current rejected fragment count.</returns>
    public int GetRejectedFragmentCount()
    {
        return rejectedFragmentCount;
    }

    /// <summary>
    /// Gets the group count.
    /// </summary>
    /// <returns>The current group count.</returns>
    public int GetGroupCount()
    {
        return (groupIds ?? throw new InvalidOperationException()).Count;
    }

    /// <summary>
    /// Gets the group ID at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested group ID.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string GetGroupIdAt(int index)
    {
        if (index < 0 || index >= (groupIds ?? throw new InvalidOperationException()).Count)
            throw new ArgumentOutOfRangeException("index");
        return groupIds[index];
    }

    /// <summary>
    /// Gets the group ids.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current group ids.</returns>
    public List<string> GetGroupIds()
    {
        return new List<string>((groupIds ?? throw new InvalidOperationException()));
    }

    /// <summary>
    /// Gets the fragment count.
    /// </summary>
    /// <param name = "fragmentId">The fragment ID used by the operation.</param>
    /// <returns>The current fragment count.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public int GetFragmentCount(string fragmentId)
    {
        if (fragmentId == null)
            throw new ArgumentNullException("fragmentId");
        return (groups ?? throw new InvalidOperationException()).ContainsKey(fragmentId) ? groups[fragmentId].Count : 0;
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

    /// <summary>
    /// Assembles the supplied value.
    /// </summary>
    /// <param name = "fragmentId">The fragment ID used by the operation.</param>
    /// <returns>The partial message assembly result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailPartialMessageAssemblyResult Assemble(string fragmentId)
    {
        if (fragmentId == null)
            throw new ArgumentNullException("fragmentId");
        if (!(groups ?? throw new InvalidOperationException()).ContainsKey(fragmentId))
            throw new ArgumentException("No collected fragment group has the requested id.", "fragmentId");
        EmailPartialMessageAssemblyWorker worker = new EmailPartialMessageAssemblyWorker(fragmentId, groups[fragmentId], (options ?? throw new InvalidOperationException()).Clone());
        return worker.Assemble();
    }

    /// <summary>
    /// Assembles the all.
    /// </summary>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailPartialMessageAssemblyResult> AssembleAll()
    {
        List<EmailPartialMessageAssemblyResult> results = new List<EmailPartialMessageAssemblyResult>();
        int index = 0;
        while (index < (groupIds ?? throw new InvalidOperationException()).Count)
        {
            results.Add(Assemble(groupIds[index]));
            index++;
        }

        return results;
    }

    static EmailIssue Issue(EmailIssueCode code, string description, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, null, null, "Content-Type", recovery);
    }
}
