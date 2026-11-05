using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with ARC set collector.
/// </summary>
public class EmailArcSetCollector : IEmailArcSetCollectorContract
{
    internal static EmailArcSetCollectionResult CollectImplementationWithHeaders(EmailHeaders headers)
    {
        return CollectImplementationWithHeadersAndOptions(headers, new EmailArcSetCollectionOptions());
    }

    /// <summary>
    /// Collects the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <returns>The ARC set collection result produced by the operation.</returns>
    public EmailArcSetCollectionResult CollectWithHeaders(EmailHeaders headers)
    {
        return EmailArcSetCollector.CollectImplementationWithHeaders(headers);
    }

    internal static EmailArcSetCollectionResult CollectImplementationWithHeadersAndOptions(EmailHeaders headers, EmailArcSetCollectionOptions options)
    {
        if (headers == null)
            throw new ArgumentNullException("headers");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailArcSetCollectionOptions effectiveOptions = options.Clone();
        EmailSignatureParserOptions parserOptions = CreateParserOptions(effectiveOptions);
        EmailIssueList issues = new EmailIssueList();
        List<EmailArcHeaderField> fields = new List<EmailArcHeaderField>();
        List<EmailArcHeaderField> unassignedFields = new List<EmailArcHeaderField>();
        EmailArcSetBuilder[] builders = new EmailArcSetBuilder[51];
        int headerIndex = 0;
        while (headerIndex < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(headerIndex);
            EmailSignatureHeaderKind kind = GetKind(header.GetNormalizedName());
            if (kind != EmailSignatureHeaderKind.DkimSignature)
            {
                if (fields.Count >= effectiveOptions.GetMaximumArcHeaderCount())
                {
                    throw new EmailLimitException("MaxArcHeaderCount", effectiveOptions.GetMaximumArcHeaderCount(), fields.Count + 1);
                }

                byte[] rawValue = header.GetRawValue();
                if (rawValue.Length > effectiveOptions.GetMaximumHeaderBytes())
                {
                    throw new EmailLimitException("MaxArcHeaderBytes", effectiveOptions.GetMaximumHeaderBytes(), rawValue.Length);
                }

                EmailArcHeaderField field = ParseField(kind, header, headerIndex, rawValue, parserOptions);
                fields.Add(field);
                issues.AddRange(field.GetIssues());
                if (!field.HasInstance())
                {
                    unassignedFields.Add(field);
                    AddIssue(issues, EmailIssueCode.ArcHeaderUnassigned, "The ARC header field has no valid instance and cannot be assigned to an ARC set.", header.GetValueSourcePosition(), header.GetName());
                }
                else
                {
                    int instance = field.GetInstance();
                    if (builders[instance] == null)
                        builders[instance] = new EmailArcSetBuilder(instance);
                    builders[instance].Add(field);
                }
            }

            headerIndex++;
        }

        return BuildResult(fields, unassignedFields, builders, issues, effectiveOptions);
    }

    /// <summary>
    /// Collects the supplied value.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The ARC set collection result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailArcSetCollectionResult CollectWithHeadersAndOptions(EmailHeaders headers, EmailArcSetCollectionOptions options)
    {
        return EmailArcSetCollector.CollectImplementationWithHeadersAndOptions(headers, options);
    }

    static EmailArcSetCollectionResult BuildResult(List<EmailArcHeaderField> fields, List<EmailArcHeaderField> unassignedFields, EmailArcSetBuilder[] builders, EmailIssueList issues, EmailArcSetCollectionOptions options)
    {
        List<EmailArcSet> sets = new List<EmailArcSet>();
        int maximumInstance = 0;
        int instance = 1;
        while (instance <= 50)
        {
            if (builders[instance] != null)
            {
                if (sets.Count >= options.GetMaximumSetCount())
                {
                    throw new EmailLimitException("MaxArcSetCount", options.GetMaximumSetCount(), sets.Count + 1);
                }

                sets.Add(builders[instance].Build());
                maximumInstance = instance;
            }

            instance++;
        }

        bool continuousInstances = ValidateInstanceSequence(builders, maximumInstance, issues);
        bool completeSets = true;
        bool validChainStatuses = true;
        int setIndex = 0;
        while (setIndex < sets.Count)
        {
            EmailArcSet set = sets[setIndex];
            if (!set.IsComplete())
            {
                completeSets = false;
                AddIssue(issues, EmailIssueCode.ArcSetIncomplete, "ARC set " + set.GetInstance() + " does not contain exactly one ARC-Authentication-Results, ARC-Message-Signature, and ARC-Seal field.", GetSetPosition(set), null);
            }

            if (set.HasDuplicateFields())
            {
                AddIssue(issues, EmailIssueCode.ArcSetDuplicateHeader, "ARC set " + set.GetInstance() + " contains a duplicate ARC header field type.", GetSetPosition(set), null);
            }

            if (!HasValidChainStatus(set))
            {
                validChainStatuses = false;
                AddIssue(issues, EmailIssueCode.ArcChainStatusInvalid, "ARC set " + set.GetInstance() + " has an invalid cv value for its instance.", GetSealPosition(set), "ARC-Seal");
            }

            setIndex++;
        }

        EmailArcSetCollectionStatus status = GetStatus(fields, unassignedFields, sets, issues, continuousInstances, completeSets, validChainStatuses);
        return new EmailArcSetCollectionResult(status, sets, fields, unassignedFields, issues, continuousInstances, completeSets, validChainStatuses, maximumInstance);
    }

    static EmailArcSetCollectionStatus GetStatus(List<EmailArcHeaderField> fields, List<EmailArcHeaderField> unassignedFields, List<EmailArcSet> sets, EmailIssueList issues, bool continuousInstances, bool completeSets, bool validChainStatuses)
    {
        if (fields.Count == 0)
            return EmailArcSetCollectionStatus.None;
        if (unassignedFields.Count != 0 || !continuousInstances || !completeSets || !validChainStatuses || issues.HasSeverityAtLeast(EmailIssueSeverity.Error))
            return EmailArcSetCollectionStatus.Invalid;
        int index = 0;
        while (index < sets.Count)
        {
            if (!sets[index].HasValidFields())
                return EmailArcSetCollectionStatus.Invalid;
            index++;
        }

        return EmailArcSetCollectionStatus.Valid;
    }

    static bool ValidateInstanceSequence(EmailArcSetBuilder[] builders, int maximumInstance, EmailIssueList issues)
    {
        bool valid = true;
        int instance = 1;
        while (instance <= maximumInstance)
        {
            if (builders[instance] == null)
            {
                valid = false;
                AddIssue(issues, EmailIssueCode.ArcInstanceSequenceInvalid, "The ARC chain is missing instance " + instance + ".", null, null);
            }

            instance++;
        }

        return valid;
    }

    static bool HasValidChainStatus(EmailArcSet set)
    {
        if (set.GetSealFieldCount() != 1 || !set.GetSealField(0).HasSeal())
            return false;
        EmailArcChainStatus status = set.GetDeclaredChainStatus();
        if (set.GetInstance() == 1)
            return status == EmailArcChainStatus.None;
        return status == EmailArcChainStatus.Pass;
    }

    static EmailSourcePosition? GetSetPosition(EmailArcSet set)
    {
        if (set.GetAuthenticationResultsFieldCount() > 0)
            return set.GetAuthenticationResultsField(0).GetHeader().GetValueSourcePosition();
        if (set.GetMessageSignatureFieldCount() > 0)
            return set.GetMessageSignatureField(0).GetHeader().GetValueSourcePosition();
        if (set.GetSealFieldCount() > 0)
            return set.GetSealField(0).GetHeader().GetValueSourcePosition();
        return null;
    }

    static EmailSourcePosition? GetSealPosition(EmailArcSet set)
    {
        if (set.GetSealFieldCount() == 0)
            return GetSetPosition(set);
        return set.GetSealField(0).GetHeader().GetValueSourcePosition();
    }

    static EmailSignatureParserOptions CreateParserOptions(EmailArcSetCollectionOptions options)
    {
        EmailSignatureParserOptions parserOptions = new EmailSignatureParserOptions();
        parserOptions.SetMode(EmailStructuredValueParseMode.Strict);
        parserOptions.SetMaximumTags(options.GetMaximumTags());
        parserOptions.SetMaximumInputBytes(options.GetMaximumHeaderBytes());
        return parserOptions;
    }

    static EmailSignatureHeaderKind GetKind(string normalizedName)
    {
        if (normalizedName == "arc-message-signature")
            return EmailSignatureHeaderKind.ArcMessageSignature;
        if (normalizedName == "arc-seal")
            return EmailSignatureHeaderKind.ArcSeal;
        if (normalizedName == "arc-authentication-results")
            return EmailSignatureHeaderKind.ArcAuthenticationResults;
        return EmailSignatureHeaderKind.DkimSignature;
    }

    static EmailArcHeaderField ParseField(EmailSignatureHeaderKind kind, EmailHeader header, int headerIndex, byte[] rawValue, EmailSignatureParserOptions options)
    {
        if (kind == EmailSignatureHeaderKind.ArcMessageSignature)
        {
            EmailArcMessageSignatureParseResult result = new EmailArcMessageSignatureParser().ParseWithInputAndOptionsAndInitialPosition(rawValue, options, header.GetValueSourcePosition());
            EmailDkimSignature? value = result.HasValue() ? result.GetValue() : null;
            int instance = value == null ? 0 : value.GetInstance();
            return new EmailArcHeaderField(kind, header, headerIndex, NormalizeInstance(instance), result.IsSuccess(), result.GetIssues(), value, null, null);
        }

        if (kind == EmailSignatureHeaderKind.ArcSeal)
        {
            EmailArcSealParseResult result = new EmailArcSealParser().ParseWithInputAndOptionsAndInitialPosition(rawValue, options, header.GetValueSourcePosition());
            EmailArcSeal? value = result.HasValue() ? result.GetValue() : null;
            int instance = value == null ? 0 : value.GetInstance();
            return new EmailArcHeaderField(kind, header, headerIndex, NormalizeInstance(instance), result.IsSuccess(), result.GetIssues(), null, value, null);
        }

        EmailArcAuthenticationResultsParseResult authenticationResult = new EmailArcAuthenticationResultsParser().ParseWithInputAndOptionsAndInitialPosition(rawValue, options, header.GetValueSourcePosition());
        EmailArcAuthenticationResults? authenticationValue = authenticationResult.HasValue() ? authenticationResult.GetValue() : null;
        int authenticationInstance = authenticationValue == null ? 0 : authenticationValue.GetInstance();
        return new EmailArcHeaderField(kind, header, headerIndex, NormalizeInstance(authenticationInstance), authenticationResult.IsSuccess(), authenticationResult.GetIssues(), null, null, authenticationValue);
    }

    static int NormalizeInstance(int instance)
    {
        return instance >= 1 && instance <= 50 ? instance : 0;
    }

    static void AddIssue(EmailIssueList issues, EmailIssueCode code, string description, EmailSourcePosition? position, string? fieldName)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, position, null, fieldName, "The ARC fields were retained for diagnostics."));
    }
}
