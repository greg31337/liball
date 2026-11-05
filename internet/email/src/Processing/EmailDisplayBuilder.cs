using System;
using System.Collections.Generic;

namespace email;
internal class EmailDisplayBuilderWorker
{
    EmailDisplayOptions options;

    internal EmailDisplayBuilderWorker(EmailDisplayOptions options)
    {
        this.options = options;
    }

    internal EmailDisplayModel Build(MimeEntity? root, EmailIssueList initialIssues)
    {
        EmailIssueList issues = new EmailIssueList();
        issues.AddRange(initialIssues);
        EmailBodySelection selection = new EmailBodySelector().Choose(root, options.GetBodyPreference(), options.GetMaximumDepth(), options.GetMaximumEntityCount());
        List<EmailAttachmentView> views = new EmailAttachmentFinder().EnumerateWithRootAndMaximumDepthAndMaximumEntityCount(root, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        List<EmailAttachmentView> inlineResources = FilterViews(views, true);
        List<EmailAttachmentView> attachments = FilterViews(views, false);
        EmailDisplaySecuritySummary summary = CreateSummary(root, views);
        List<EmailRelatedReference> missing = new List<EmailRelatedReference>();
        if (!selection.HasBody()) return CreateUnavailable(selection, inlineResources, attachments, missing, summary, issues);

        MimeEntity? body = selection.GetBody();
        EmailTextDecodeResult decoded = new EmailTextProcessor().DecodeWithEntityAndTextOptionsAndFlowedOptions(body ?? throw new InvalidOperationException(), options.GetTextOptions(), options.GetFlowedOptions());
        issues.AddRange(decoded.GetIssues());
        string text;
        string? safeHtml = null;
        EmailHtmlSanitizeResult? sanitized = null;
        if (((body ?? throw new InvalidOperationException()).GetMediaType() ?? throw new InvalidOperationException()).IsType("text", "html"))
        {
            sanitized = new EmailHtmlProcessor().SanitizeWithHtmlAndOptions(decoded.GetText(), options.GetHtmlSanitizeOptions());
            issues.AddRange(sanitized.GetIssues());
            safeHtml = sanitized.GetHtml();
            EmailHtmlTextResult converted = new EmailHtmlProcessor().ToTextWithHtmlAndOptions(safeHtml, options.GetHtmlTextOptions());
            issues.AddRange(converted.GetIssues());
            text = converted.GetText();
            missing = FindMissingReferences(root, body, safeHtml);
            if (missing.Count > 0) issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.DisplayRelatedResourceMissing, missing.Count + " referenced related resource(s) in the sanitized HTML could not be resolved within the MIME tree."));
        }
        else
        {
            text = decoded.GetText();
        }

        EmailTextAnalysisResult analysis = new EmailTextProcessor().AnalyzeWithValueAndOptions(text, options.GetAnalysisOptions());
        issues.AddRange(analysis.GetIssues());
        return new EmailDisplayModel(selection, text, safeHtml, analysis, inlineResources, attachments, missing, summary, issues, decoded, sanitized);
    }

    internal EmailDisplayModel BuildEmpty(EmailIssueList initialIssues)
    {
        EmailIssueList issues = new EmailIssueList();
        issues.AddRange(initialIssues);
        EmailBodySelection selection = new EmailBodySelection(null, options.GetBodyPreference(), EmailBodySelectionQuality.Unavailable, EmailBodySelectionReason.NoSupportedBody, null, new List<MimeEntity>(), new List<EmailBodySelectionContext>());
        return CreateUnavailable(selection, new List<EmailAttachmentView>(), new List<EmailAttachmentView>(), new List<EmailRelatedReference>(), new EmailDisplaySecuritySummary(0, 0, 0, 0, 0), issues);
    }

    EmailDisplayModel CreateUnavailable(EmailBodySelection selection, List<EmailAttachmentView> inlineResources, List<EmailAttachmentView> attachments, List<EmailRelatedReference> missing, EmailDisplaySecuritySummary summary, EmailIssueList issues)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.DisplayBodyUnavailable, "No supported decoded body is available for the safe display projection."));
        EmailTextAnalysisResult analysis = new EmailTextProcessor().AnalyzeWithValueAndOptions("", options.GetAnalysisOptions());
        return new EmailDisplayModel(selection, "", null, analysis, inlineResources, attachments, missing, summary, issues, null, null);
    }

    List<EmailRelatedReference> FindMissingReferences(MimeEntity? root, MimeEntity body, string safeHtml)
    {
        EmailRelatedIndexOptions relatedOptions = options.GetRelatedIndexOptions();
        if (relatedOptions.GetMaximumDepth() > options.GetMaximumDepth()) relatedOptions.SetMaximumDepth(options.GetMaximumDepth());
        if (relatedOptions.GetMaximumEntityCount() > options.GetMaximumEntityCount()) relatedOptions.SetMaximumEntityCount(options.GetMaximumEntityCount());
        List<EmailRelatedReference> candidates = new EmailRelatedIndexOperations().BuildWithRootAndOptions(root, relatedOptions).GetReferences();
        EmailHtmlReferenceScanner scanner = new EmailHtmlReferenceScanner(relatedOptions.GetMaximumReferences(), relatedOptions.GetMaximumUrlCharacters());
        List<string> retainedReferences = scanner.Find(safeHtml);
        List<EmailRelatedReference> missing = new List<EmailRelatedReference>();
        List<string> missingKeys = new List<string>();
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            EmailRelatedReference candidate = candidates[candidateIndex];
            if (!candidate.IsMissing() || !ReferenceEquals(candidate.GetSourceEntity(), body))
            {
                candidateIndex++;
                continue;
            }

            int referenceIndex = 0;
            while (referenceIndex < retainedReferences.Count)
            {
                if (string.Equals(candidate.GetOriginalValue(), retainedReferences[referenceIndex], StringComparison.OrdinalIgnoreCase))
                {
                    string missingKey = ((int)candidate.GetKind()).ToString() + ":" + candidate.GetNormalizedValue();
                    if (!missingKeys.Contains(missingKey))
                    {
                        missingKeys.Add(missingKey);
                        missing.Add(candidate);
                    }

                    break;
                }

                referenceIndex++;
            }

            candidateIndex++;
        }

        return missing;
    }

    EmailDisplaySecuritySummary CreateSummary(MimeEntity? root, List<EmailAttachmentView> views)
    {
        List<MimeEntity> entities = new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(root, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        int signed = 0;
        int encrypted = 0;
        int reports = 0;
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.GetKind() == MimeEntityKind.Signed) signed++;
            if (entity.GetKind() == MimeEntityKind.Encrypted) encrypted++;
            if (IsReport(entity.GetMediaType())) reports++;
            index++;
        }

        int signatures = 0;
        int encryptionParts = 0;
        index = 0;
        while (index < views.Count)
        {
            if (views[index].IsSignature()) signatures++;
            if (views[index].IsEncryptionControl() || views[index].IsEncryptedPayload()) encryptionParts++;
            index++;
        }

        return new EmailDisplaySecuritySummary(signed, encrypted, signatures, encryptionParts, reports);
    }

    static List<EmailAttachmentView> FilterViews(List<EmailAttachmentView> views, bool inline)
    {
        List<EmailAttachmentView> selected = new List<EmailAttachmentView>();
        int index = 0;
        while (index < views.Count)
        {
            if (inline && views[index].IsInlineResource()) selected.Add(views[index]);
            if (!inline && views[index].IsDownloadable()) selected.Add(views[index]);
            index++;
        }

        return selected;
    }

    static bool IsReport(EmailMediaType? mediaType)
    {
        return (mediaType ?? throw new InvalidOperationException()).IsType("multipart", "report") || mediaType.IsType("message", "delivery-status") || mediaType.IsType("message", "disposition-notification") || mediaType.IsType("message", "feedback-report");
    }
}
