using System;
using System.Collections.Generic;

namespace email;
internal class EmailMessageAnalyzerWorker
{
    EmailMessage message;
    EmailMessageAnalysisOptions options;
    List<MimeEntity> entities;
    List<EmailAttachmentView> views;
    Dictionary<MimeEntity, EmailEntityAnalysisSize> entitySizes;
    int structuredHeaderBytes;
    int parsedAddressCount;
    int toRecipients;
    int ccRecipients;
    int bccRecipients;
    int resentRecipients;
    EmailDateTime? messageDate;
    List<EmailDateTime> resentDates;
    List<EmailDateTime> receivedDates;
    int dkimHeaders;
    int arcHeaders;
    int authenticationResultsHeaders;
    int receivedSpfHeaders;
    long rawContentBytes;
    bool rawContentComplete;
    long decodedContentBytes;
    bool decodedContentComplete;

    internal EmailMessageAnalyzerWorker(EmailMessage message, EmailMessageAnalysisOptions options)
    {
        this.message = message;
        this.options = options;
        entities = new List<MimeEntity>();
        views = new List<EmailAttachmentView>();
        entitySizes = new Dictionary<MimeEntity, EmailEntityAnalysisSize>();
        structuredHeaderBytes = 0;
        parsedAddressCount = 0;
        resentDates = new List<EmailDateTime>();
        receivedDates = new List<EmailDateTime>();
        rawContentComplete = true;
        decodedContentComplete = true;
    }

    internal EmailMessageSummary Analyze()
    {
        AnalyzeHeaders();
        int maximumDepth = 0;
        if (message.HasBody())
        {
            entities = new EmailTraversal().DepthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(message, options.GetMaximumDepth(), options.GetMaximumEntityCount());
            maximumDepth = GetMaximumDepth(message.GetBody());
            views = new EmailAttachmentFinder().EnumerateWithMessageAndMaximumDepthAndMaximumEntityCount(message, options.GetMaximumDepth(), options.GetMaximumEntityCount());
            AnalyzeEntitySizes();
        }

        List<EmailMediaTypeStatistic> mediaTypes = GetMediaTypeStatistics();
        int leafParts = CountLeafParts();
        int containers = entities.Count - leafParts;
        EmailAttachmentAnalysis attachment = AnalyzeAttachments();
        EmailSecuritySummary security = AnalyzeSecurity();
        EmailIssueList issues = message.GetIssues();
        bool hasRawMessage = message.HasSourceInfo();
        long rawMessageBytes = hasRawMessage ? ((message.GetSourceInfo() ?? throw new InvalidOperationException()).GetMessageRange() ?? throw new InvalidOperationException()).GetLength() : 0;
        return new EmailMessageSummary((message.GetHeaders() ?? throw new InvalidOperationException()).GetCount(), hasRawMessage, rawMessageBytes, rawContentBytes, rawContentComplete, decodedContentBytes, decodedContentComplete, entities.Count, leafParts, containers, maximumDepth, mediaTypes, toRecipients, ccRecipients, bccRecipients, resentRecipients, (message.GetEnvelope() ?? throw new InvalidOperationException()).GetRecipientCount(), messageDate, resentDates, receivedDates, attachment.attachmentCount, attachment.inlineResourceCount, attachment.nestedMessageCount, attachment.rawBytes, attachment.rawComplete, attachment.decodedBytes, attachment.decodedComplete, issues, security);
    }

    void AnalyzeHeaders()
    {
        EmailHeaders? headers = message.GetHeaders();
        if ((headers ?? throw new InvalidOperationException()).GetCount() > options.GetMaximumHeaderCount())
        {
            throw new EmailLimitException("MaxAnalysisHeaders", options.GetMaximumHeaderCount(), headers.GetCount());
        }

        int index = 0;
        while (index < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(index);
            string name = header.GetName();
            CountSecurityHeader(name);
            if (IsRecipientHeader(name))
            {
                AnalyzeRecipients(name, header);
            }
            else if (string.Equals(name, "Date", StringComparison.OrdinalIgnoreCase))
            {
                EmailDateTime? parsed = ParseDate(header);
                if (messageDate == null) messageDate = parsed;
            }
            else if (string.Equals(name, "Resent-Date", StringComparison.OrdinalIgnoreCase))
            {
                EmailDateTime? parsed = ParseDate(header);
                if (parsed != null) resentDates.Add(parsed);
            }
            else if (string.Equals(name, "Received", StringComparison.OrdinalIgnoreCase))
            {
                AnalyzeReceivedDate(header);
            }

            index++;
        }
    }

    void AnalyzeRecipients(string name, EmailHeader header)
    {
        byte[] value = AddStructuredHeader(header);
        EmailAddressParserOptions parserOptions = new EmailAddressParserOptions();
        parserOptions.SetMode(EmailAddressParseMode.Tolerant);
        parserOptions.SetMaximumAddresses(options.GetMaximumAddressCount());
        parserOptions.SetMaximumInputBytes(Math.Max(1, value.Length));
        EmailAddressParseResult parsed = new EmailAddressParser().ParseListWithInputAndOptionsAndInitialPosition(value, parserOptions, header.GetValueSourcePosition());
        if (parsed.GetIssues().ContainsCode(EmailIssueCode.AddressLimitExceeded))
        {
            throw new EmailLimitException("MaxAnalysisRecipients", options.GetMaximumAddressCount(), (long)options.GetMaximumAddressCount() + 1);
        }

        int count = parsed.GetAddressList().GetMailboxCount();
        parsedAddressCount = AddLimited("MaxAnalysisRecipients", parsedAddressCount, count, options.GetMaximumAddressCount());
        if (string.Equals(name, "To", StringComparison.OrdinalIgnoreCase)) toRecipients += count;
        else if (string.Equals(name, "Cc", StringComparison.OrdinalIgnoreCase)) ccRecipients += count;
        else if (string.Equals(name, "Bcc", StringComparison.OrdinalIgnoreCase)) bccRecipients += count;
        else resentRecipients += count;
    }

    EmailDateTime? ParseDate(EmailHeader header)
    {
        byte[] value = AddStructuredHeader(header);
        EmailDateParserOptions parserOptions = new EmailDateParserOptions();
        parserOptions.SetMode(EmailDateParseMode.Tolerant);
        parserOptions.SetMaximumInputBytes(Math.Max(1, value.Length));
        EmailDateParseResult parsed = new EmailDateParser().ParseWithInputAndOptionsAndInitialPosition(value, parserOptions, header.GetValueSourcePosition());
        return parsed.HasValue() ? parsed.GetValue() : null;
    }

    void AnalyzeReceivedDate(EmailHeader header)
    {
        byte[] value = AddStructuredHeader(header);
        EmailReceivedParserOptions parserOptions = new EmailReceivedParserOptions();
        parserOptions.SetMode(EmailReceivedParseMode.Tolerant);
        parserOptions.SetMaximumInputBytes(Math.Max(1, value.Length));
        EmailReceivedParseResult parsed = new EmailReceivedParser().ParseWithInputAndOptionsAndInitialPosition(value, parserOptions, header.GetValueSourcePosition());
        if (parsed.HasValue() && (parsed.GetValue() ?? throw new InvalidOperationException()).HasDate()) receivedDates.Add(((parsed.GetValue() ?? throw new InvalidOperationException()).GetDate() ?? throw new InvalidOperationException()));
    }

    byte[] AddStructuredHeader(EmailHeader header)
    {
        byte[] value = header.GetUnfoldedValue();
        structuredHeaderBytes = AddLimited("MaxAnalysisStructuredHeaderBytes", structuredHeaderBytes, value.Length, options.GetMaximumStructuredHeaderBytes());
        return value;
    }

    void AnalyzeEntitySizes()
    {
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            EmailEntityAnalysisSize size = AnalyzeEntitySize(entity);
            entitySizes.Add(entity, size);
            if (size.rawKnown) rawContentBytes = new EmailSafeArithmetic().AddNonNegative(rawContentBytes, size.rawBytes);
            else if (!entity.IsContainer() && entity.GetKind() != MimeEntityKind.Message) rawContentComplete = false;
            if (size.decodedKnown) AddDecodedTotal(size.decodedBytes);
            else if (!entity.IsContainer() && entity.GetKind() != MimeEntityKind.Message) decodedContentComplete = false;
            index++;
        }
    }

    EmailEntityAnalysisSize AnalyzeEntitySize(MimeEntity entity)
    {
        EmailEntityAnalysisSize size = new EmailEntityAnalysisSize();
        if (entity.IsContainer() || entity.GetKind() == MimeEntityKind.Message) return size;
        if (entity.HasRawContent() && (entity.GetRawContent() ?? throw new InvalidOperationException()).HasKnownLength())
        {
            size.rawKnown = true;
            size.rawBytes = (entity.GetRawContent() ?? throw new InvalidOperationException()).GetLength();
        }

        if (entity.HasDecodedContent())
        {
            SetDecodedContentSize(size, entity.GetDecodedContent());
            return size;
        }

        if (!entity.HasRawContent()) return size;
        if (entity.HasTransferEncoding() && !entity.HasKnownTransferEncoding()) return size;
        EmailTransferEncodingKind encoding = entity.GetEffectiveTransferEncodingKind();
        bool identity = encoding == EmailTransferEncodingKind.SevenBit || encoding == EmailTransferEncodingKind.EightBit || encoding == EmailTransferEncodingKind.Binary;
        if (identity && (entity.GetRawContent() ?? throw new InvalidOperationException()).HasKnownLength())
        {
            size.decodedKnown = true;
            size.decodedBytes = (entity.GetRawContent() ?? throw new InvalidOperationException()).GetLength();
            return size;
        }

        if (!options.GetMeasureRepeatableDecodedContent() || !(entity.GetRawContent() ?? throw new InvalidOperationException()).IsRepeatable()) return size;
        size.decodedBytes = MeasureEntity(entity);
        size.decodedKnown = true;
        return size;
    }

    void SetDecodedContentSize(EmailEntityAnalysisSize size, EmailContent? content)
    {
        if ((content ?? throw new InvalidOperationException()).HasKnownLength())
        {
            size.decodedKnown = true;
            size.decodedBytes = content.GetLength();
        }
        else if (options.GetMeasureRepeatableDecodedContent() && content.IsRepeatable())
        {
            size.decodedBytes = MeasureContent(content);
            size.decodedKnown = true;
        }
    }

    long MeasureEntity(MimeEntity entity)
    {
        long remaining = GetDecodedRemaining();
        return new EmailDecodedContent().MeasureLengthWithEntityAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(entity, remaining, "MaxAnalysisDecodedContentBytes", options.GetMaximumDecodedContentBytes(), GetDecodedLimitObserved());
    }

    long MeasureContent(EmailContent content)
    {
        long remaining = GetDecodedRemaining();
        return new EmailDecodedContent().MeasureLengthWithContentAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(content, remaining, "MaxAnalysisDecodedContentBytes", options.GetMaximumDecodedContentBytes(), GetDecodedLimitObserved());
    }

    long GetDecodedRemaining()
    {
        long remaining = options.GetMaximumDecodedContentBytes() - decodedContentBytes;
        if (remaining < 1)
        {
            throw new EmailLimitException("MaxAnalysisDecodedContentBytes", options.GetMaximumDecodedContentBytes(), GetDecodedLimitObserved());
        }

        return remaining;
    }

    void AddDecodedTotal(long value)
    {
        if (!new EmailSafeArithmetic().CanAddNonNegative(decodedContentBytes, value) || value > options.GetMaximumDecodedContentBytes() - decodedContentBytes)
        {
            throw new EmailLimitException("MaxAnalysisDecodedContentBytes", options.GetMaximumDecodedContentBytes(), GetDecodedLimitObserved());
        }

        decodedContentBytes += value;
    }

    long GetDecodedLimitObserved()
    {
        return options.GetMaximumDecodedContentBytes() == long.MaxValue ? long.MaxValue : options.GetMaximumDecodedContentBytes() + 1;
    }

    int GetMaximumDepth(MimeEntity? root)
    {
        int maximum = 0;
        Stack<EmailAnalysisTraversalNode> pending = new Stack<EmailAnalysisTraversalNode>();
        pending.Push(new EmailAnalysisTraversalNode(root, 0));
        while (pending.Count > 0)
        {
            EmailAnalysisTraversalNode node = pending.Pop();
            if (node.depth > maximum) maximum = node.depth;
            MimeEntity? entity = node.entity;
            if ((entity ?? throw new InvalidOperationException()).GetKind() == MimeEntityKind.Message)
            {
                if ((entity.GetNestedMessage() ?? throw new InvalidOperationException()).HasBody()) pending.Push(new EmailAnalysisTraversalNode((entity.GetNestedMessage() ?? throw new InvalidOperationException()).GetBody(), node.depth + 1));
            }
            else
            {
                int child = entity.GetChildCount() - 1;
                while (child >= 0)
                {
                    pending.Push(new EmailAnalysisTraversalNode(entity.GetChildAt(child), node.depth + 1));
                    child--;
                }
            }
        }

        return maximum;
    }

    List<EmailMediaTypeStatistic> GetMediaTypeStatistics()
    {
        List<EmailMutableMediaTypeStatistic> mutable = new List<EmailMutableMediaTypeStatistic>();
        Dictionary<string, EmailMutableMediaTypeStatistic> byType = new Dictionary<string, EmailMutableMediaTypeStatistic>(StringComparer.Ordinal);
        int index = 0;
        while (index < entities.Count)
        {
            EmailMediaType? type = entities[index].GetMediaType();
            string top = (type ?? throw new InvalidOperationException()).GetNormalizedTopLevelType();
            string sub = type.GetNormalizedSubtype();
            string key = top + "/" + sub;
            EmailMutableMediaTypeStatistic? value;
            if (byType.TryGetValue(key, out value)) value.count++;
            else
            {
                value = new EmailMutableMediaTypeStatistic(top, sub);
                byType.Add(key, value);
                mutable.Add(value);
            }

            index++;
        }

        List<EmailMediaTypeStatistic> result = new List<EmailMediaTypeStatistic>();
        index = 0;
        while (index < mutable.Count)
        {
            result.Add(new EmailMediaTypeStatistic(mutable[index].top, mutable[index].subtype, mutable[index].count));
            index++;
        }

        return result;
    }

    int CountLeafParts()
    {
        int count = 0;
        int index = 0;
        while (index < entities.Count)
        {
            if (!entities[index].IsContainer()) count++;
            index++;
        }

        return count;
    }

    EmailAttachmentAnalysis AnalyzeAttachments()
    {
        EmailAttachmentAnalysis result = new EmailAttachmentAnalysis();
        int index = 0;
        while (index < views.Count)
        {
            EmailAttachmentView view = views[index];
            if (view.IsInlineResource()) result.inlineResourceCount++;
            if (view.IsNestedMessage()) result.nestedMessageCount++;
            if (view.IsDownloadable())
            {
                result.attachmentCount++;
                AddAttachmentSize(view.GetEntity(), result);
            }

            index++;
        }

        return result;
    }

    void AddAttachmentSize(MimeEntity entity, EmailAttachmentAnalysis result)
    {
        if (entity.GetKind() == MimeEntityKind.Message)
        {
            EmailMessage? nested = entity.GetNestedMessage();
            if ((nested ?? throw new InvalidOperationException()).HasSourceInfo())
            {
                long length = ((nested.GetSourceInfo() ?? throw new InvalidOperationException()).GetMessageRange() ?? throw new InvalidOperationException()).GetLength();
                result.rawBytes = new EmailSafeArithmetic().AddNonNegative(result.rawBytes, length);
                result.decodedBytes = new EmailSafeArithmetic().AddNonNegative(result.decodedBytes, length);
            }
            else
            {
                result.rawComplete = false;
                result.decodedComplete = false;
            }

            return;
        }

        EmailEntityAnalysisSize? size;
        if (!entitySizes.TryGetValue(entity, out size))
        {
            result.rawComplete = false;
            result.decodedComplete = false;
            return;
        }

        if (size.rawKnown) result.rawBytes = new EmailSafeArithmetic().AddNonNegative(result.rawBytes, size.rawBytes);
        else result.rawComplete = false;
        if (size.decodedKnown) result.decodedBytes = new EmailSafeArithmetic().AddNonNegative(result.decodedBytes, size.decodedBytes);
        else result.decodedComplete = false;
    }

    EmailSecuritySummary AnalyzeSecurity()
    {
        int signed = 0;
        int encrypted = 0;
        int reports = 0;
        int index = 0;
        while (index < entities.Count)
        {
            MimeEntity entity = entities[index];
            if (entity.GetKind() == MimeEntityKind.Signed) signed++;
            if (entity.GetKind() == MimeEntityKind.Encrypted) encrypted++;
            if (IsReport(entity)) reports++;
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

        return new EmailSecuritySummary(signed, encrypted, signatures, encryptionParts, reports, dkimHeaders, arcHeaders, authenticationResultsHeaders, receivedSpfHeaders);
    }

    void CountSecurityHeader(string name)
    {
        if (string.Equals(name, "DKIM-Signature", StringComparison.OrdinalIgnoreCase)) dkimHeaders++;
        if (string.Equals(name, "ARC-Seal", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Message-Signature", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ARC-Authentication-Results", StringComparison.OrdinalIgnoreCase)) arcHeaders++;
        if (string.Equals(name, "Authentication-Results", StringComparison.OrdinalIgnoreCase)) authenticationResultsHeaders++;
        if (string.Equals(name, "Received-SPF", StringComparison.OrdinalIgnoreCase)) receivedSpfHeaders++;
    }

    static bool IsRecipientHeader(string name)
    {
        return string.Equals(name, "To", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Cc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Bcc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Resent-To", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Resent-Cc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "Resent-Bcc", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsReport(MimeEntity entity)
    {
        return (entity.GetMediaType() ?? throw new InvalidOperationException()).IsType("multipart", "report")
            || entity.GetKind() == MimeEntityKind.DeliveryStatus
            || entity.GetKind() == MimeEntityKind.DispositionNotification
            || entity.GetKind() == MimeEntityKind.FeedbackReport;
    }

    static int AddLimited(string name, int current, int added, int maximum)
    {
        long total = (long)current + added;
        if (total > maximum) throw new EmailLimitException(name, maximum, total);
        return (int)total;
    }
}
internal class EmailEntityAnalysisSize
{
    internal bool rawKnown;
    internal long rawBytes;
    internal bool decodedKnown;
    internal long decodedBytes;
}
internal class EmailAttachmentAnalysis
{
    internal int attachmentCount;
    internal int inlineResourceCount;
    internal int nestedMessageCount;
    internal long rawBytes;
    internal bool rawComplete = true;
    internal long decodedBytes;
    internal bool decodedComplete = true;
}
internal class EmailAnalysisTraversalNode
{
    internal MimeEntity? entity;
    internal int depth;

    internal EmailAnalysisTraversalNode(MimeEntity? entity, int depth)
    {
        this.entity = entity;
        this.depth = depth;
    }
}
internal class EmailMutableMediaTypeStatistic
{
    internal string top;
    internal string subtype;
    internal int count;

    internal EmailMutableMediaTypeStatistic(string top, string subtype)
    {
        this.top = top;
        this.subtype = subtype;
        count = 1;
    }
}
