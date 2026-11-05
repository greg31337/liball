using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailTnefSemanticParser
{
    const int PidTagMessageClass = 0x001A;
    const int PidTagSubject = 0x0037;
    const int PidTagRecipientType = 0x0C15;
    const int PidTagBody = 0x1000;
    const int PidTagRtfCompressed = 0x1009;
    const int PidTagBodyHtml = 0x1013;
    const int PidTagDisplayName = 0x3001;
    const int PidTagAddressType = 0x3002;
    const int PidTagEmailAddress = 0x3003;
    const int PidTagSmtpAddress = 0x39FE;
    const int PidTagAttachData = 0x3701;
    const int PidTagAttachFilename = 0x3704;
    const int PidTagAttachMethod = 0x3705;
    const int PidTagAttachLongFilename = 0x3707;
    const int PidTagRenderingPosition = 0x370B;
    const int PidTagAttachMimeTag = 0x370E;
    const int PidTagAttachContentId = 0x3712;
    const int PidTagAttachContentLocation = 0x3713;
    const int PidTagAttachmentHidden = 0x7FFE;

    internal static EmailTnefMessage Parse(EmailTnefContainer container, EmailTnefReaderOptions options, EmailIssueList issues)
    {
        int codePage = container.HasCodePage() ? container.GetCodePage() : 437;
        EmailTnefPropertyBudget budget = new EmailTnefPropertyBudget();
        List<EmailTnefProperty> messageProperties = ParseMessageProperties(container, codePage, options, issues, budget);
        List<EmailTnefRecipient> recipients = ParseRecipients(container, codePage, options, issues, budget);
        List<EmailTnefBody> bodies = ParseBodies(container, messageProperties, codePage, options, issues);
        List<EmailTnefAttachment> attachments = ParseAttachments(container, codePage, options, issues, budget);
        string? subject = FirstText(messageProperties, PidTagSubject);
        if (subject == null) subject = LegacyTextWithContainerAndKindAndCodePageAndIssues(container, EmailTnefAttributeKind.Subject, codePage, issues);
        string? messageClass = FirstText(messageProperties, PidTagMessageClass);
        if (messageClass == null) messageClass = LegacyTextWithContainerAndKindAndCodePageAndIssues(container, EmailTnefAttributeKind.MessageClass, codePage, issues);
        messageClass = NormalizeMessageClass(messageClass);
        return new EmailTnefMessage(container, messageProperties, recipients, bodies, attachments, subject, messageClass);
    }

    static List<EmailTnefProperty> ParseMessageProperties(EmailTnefContainer container, int codePage, EmailTnefReaderOptions options, EmailIssueList issues, EmailTnefPropertyBudget budget)
    {
        List<EmailTnefProperty> result = new List<EmailTnefProperty>();
        List<EmailTnefAttribute> attributes = container.GetAttributes();
        int index = 0;
        while (index < attributes.Count)
        {
            EmailTnefAttribute attribute = attributes[index];
            if (attribute.GetLevel() == EmailTnefAttributeLevel.Message && attribute.GetKind() == EmailTnefAttributeKind.MessageProperties && attribute.IsComplete())
            {
                EmailTnefPropertyListResult parsed = EmailTnefPropertyParser.ParseList(attribute.GetData(), 0, attribute.GetDataRange().GetStartOffset(), codePage, options, issues, budget, EmailIssueCode.TnefPropertyMalformed);
                result.AddRange(parsed.GetProperties());
                if (parsed.GetConsumedBytes() != attribute.GetData().Length) AddIssue(issues, EmailIssueCode.TnefPropertyMalformed, "The message property list does not consume the complete TNEF attribute.", attribute.GetDataRange().GetStartOffset() + parsed.GetConsumedBytes(), "The parsed properties and original attribute bytes were retained.");
            }

            index++;
        }

        return result;
    }

    static List<EmailTnefRecipient> ParseRecipients(EmailTnefContainer container, int codePage, EmailTnefReaderOptions options, EmailIssueList issues, EmailTnefPropertyBudget budget)
    {
        List<EmailTnefRecipient> recipients = new List<EmailTnefRecipient>();
        List<EmailTnefAttribute> attributes = container.GetAttributes();
        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            EmailTnefAttribute attribute = attributes[attributeIndex];
            if (attribute.GetLevel() == EmailTnefAttributeLevel.Message && attribute.GetKind() == EmailTnefAttributeKind.RecipientTable && attribute.IsComplete())
            {
                byte[] data = attribute.GetData();
                if (data.Length < 4) AddIssue(issues, EmailIssueCode.TnefRecipientMalformed, "The TNEF recipient table is missing its row count.", attribute.GetDataRange().GetStartOffset(), "The original recipient-table attribute was retained.");
                else
                {
                    long rowCount = ReadUInt32(data, 0);
                    long observedRows = recipients.Count + rowCount;
                    if (observedRows > options.GetMaximumRecipientCount()) throw new EmailLimitException("MaxTnefRecipientCount", options.GetMaximumRecipientCount(), observedRows);
                    int offset = 4;
                    int row = 0;
                    while (row < rowCount && offset < data.Length)
                    {
                        EmailTnefPropertyListResult parsed = EmailTnefPropertyParser.ParseList(data, offset, attribute.GetDataRange().GetStartOffset(), codePage, options, issues, budget, EmailIssueCode.TnefRecipientMalformed);
                        List<EmailTnefProperty> properties = parsed.GetProperties();
                        recipients.Add(CreateRecipient(recipients.Count, properties));
                        offset += parsed.GetConsumedBytes();
                        if (!parsed.IsComplete()) break;
                        row++;
                    }

                    if (row < rowCount) AddIssue(issues, EmailIssueCode.TnefRecipientMalformed, "The TNEF recipient table ends before every declared row is complete.", attribute.GetDataRange().GetStartOffset() + offset, "Every complete recipient row was retained.");
                    else if (offset != data.Length) AddIssue(issues, EmailIssueCode.TnefRecipientMalformed, "The TNEF recipient table has trailing bytes after its rows.", attribute.GetDataRange().GetStartOffset() + offset, "The parsed rows and original attribute bytes were retained.");
                }
            }

            attributeIndex++;
        }

        return recipients;
    }

    static EmailTnefRecipient CreateRecipient(int index, List<EmailTnefProperty> properties)
    {
        long rawKind = FirstInteger(properties, PidTagRecipientType, 0);
        EmailTnefRecipientKind kind = EmailTnefRecipientKind.Unknown;
        if (rawKind == 1) kind = EmailTnefRecipientKind.To;
        else if (rawKind == 2) kind = EmailTnefRecipientKind.Cc;
        else if (rawKind == 3) kind = EmailTnefRecipientKind.Bcc;
        return new EmailTnefRecipient(index, kind, FirstText(properties, PidTagDisplayName), FirstText(properties, PidTagAddressType), FirstText(properties, PidTagEmailAddress), FirstText(properties, PidTagSmtpAddress), properties);
    }

    static List<EmailTnefBody> ParseBodies(EmailTnefContainer container, List<EmailTnefProperty> properties, int codePage, EmailTnefReaderOptions options, EmailIssueList issues)
    {
        List<EmailTnefBody> bodies = new List<EmailTnefBody>();
        EmailTnefProperty? plain = FirstProperty(properties, PidTagBody);
        if (plain != null && plain.GetValueCount() > 0)
        {
            byte[] value = plain.GetValueAt(0);
            string? text = plain.HasTextValue() ? plain.GetTextValue() : DecodeBytes(value, codePage, issues, plain.GetSourceRange().GetStartOffset());
            bodies.Add(new EmailTnefBody(EmailTnefBodyKind.PlainText, text, value, value, CharsetName(plain.GetTypeCode(), codePage), plain, null));
        }
        else
        {
            EmailTnefAttribute? legacyBody = FirstAttribute(container, EmailTnefAttributeKind.Body);
            if (legacyBody != null)
            {
                byte[] value = legacyBody.GetData();
                string text = DecodeBytes(value, codePage, issues, legacyBody.GetDataRange().GetStartOffset());
                bodies.Add(new EmailTnefBody(EmailTnefBodyKind.PlainText, text, value, value, CodePageName(codePage), null, legacyBody));
            }
        }

        EmailTnefProperty? html = FirstProperty(properties, PidTagBodyHtml);
        if (html != null && html.GetValueCount() > 0)
        {
            byte[] value = html.GetValueAt(0);
            string? text = html.HasTextValue() ? html.GetTextValue() : DecodeBytes(value, codePage, issues, html.GetSourceRange().GetStartOffset());
            bodies.Add(new EmailTnefBody(EmailTnefBodyKind.Html, text, value, value, CharsetName(html.GetTypeCode(), codePage), html, null));
        }

        EmailTnefProperty? rtf = FirstProperty(properties, PidTagRtfCompressed);
        if (rtf != null && rtf.GetValueCount() > 0)
        {
            byte[] original = rtf.GetValueAt(0);
            EmailTnefRtfDecodeResult decoded = EmailTnefRtfDecoder.Decode(original, rtf.GetSourceRange().GetStartOffset(), options, issues);
            byte[] content = decoded.GetContent();
            string? text = content.Length == 0 ? null : Encoding.ASCII.GetString(content);
            bodies.Add(new EmailTnefBody(EmailTnefBodyKind.RichText, text, content, original, "us-ascii", rtf, null));
        }

        return bodies;
    }

    static List<EmailTnefAttachment> ParseAttachments(EmailTnefContainer container, int codePage, EmailTnefReaderOptions options, EmailIssueList issues, EmailTnefPropertyBudget budget)
    {
        int count = container.GetAttachmentCount();
        List<List<EmailTnefAttribute>> groups = new List<List<EmailTnefAttribute>>();
        int groupIndex = 0;
        while (groupIndex < count)
        {
            groups.Add(new List<EmailTnefAttribute>());
            groupIndex++;
        }

        List<EmailTnefAttribute> all = container.GetAttributes();
        int attributeIndex = 0;
        while (attributeIndex < all.Count)
        {
            EmailTnefAttribute attribute = all[attributeIndex];
            if (attribute.HasAttachmentIndex() && attribute.GetAttachmentIndex() < groups.Count) groups[attribute.GetAttachmentIndex()].Add(attribute);
            attributeIndex++;
        }

        List<EmailTnefAttachment> result = new List<EmailTnefAttachment>();
        groupIndex = 0;
        while (groupIndex < groups.Count)
        {
            EmailIssueList attachmentIssues = new EmailIssueList();
            EmailTnefAttachment attachment = ParseAttachment(groupIndex, groups[groupIndex], codePage, options, attachmentIssues, budget);
            result.Add(attachment);
            issues.AddRange(attachmentIssues);
            groupIndex++;
        }

        return result;
    }

    static EmailTnefAttachment ParseAttachment(int index, List<EmailTnefAttribute> attributes, int codePage, EmailTnefReaderOptions options, EmailIssueList issues, EmailTnefPropertyBudget budget)
    {
        List<EmailTnefProperty> properties = new List<EmailTnefProperty>();
        int attributeIndex = 0;
        while (attributeIndex < attributes.Count)
        {
            EmailTnefAttribute attribute = attributes[attributeIndex];
            if (attribute.GetKind() == EmailTnefAttributeKind.AttachmentProperties && attribute.IsComplete())
            {
                EmailTnefPropertyListResult parsed = EmailTnefPropertyParser.ParseList(attribute.GetData(), 0, attribute.GetDataRange().GetStartOffset(), codePage, options, issues, budget, EmailIssueCode.TnefAttachmentMalformed);
                properties.AddRange(parsed.GetProperties());
                if (parsed.GetConsumedBytes() != attribute.GetData().Length) AddIssue(issues, EmailIssueCode.TnefAttachmentMalformed, "The attachment property list does not consume the complete TNEF attribute.", attribute.GetDataRange().GetStartOffset() + parsed.GetConsumedBytes(), "The parsed properties and original attribute were retained.");
            }

            attributeIndex++;
        }

        int attachMethod = FirstInt32(properties, PidTagAttachMethod, 0);
        int renderingPosition = FirstInt32(properties, PidTagRenderingPosition, -1);
        EmailTnefAttribute? rendering = FindAttribute(attributes, EmailTnefAttributeKind.AttachmentRenderingData);
        if (rendering != null)
        {
            byte[] renderingData = rendering.GetData();
            if (attachMethod == 0 && renderingData.Length >= 2) attachMethod = renderingData[0] == 2 ? 6 : 1;
            if (renderingPosition < 0 && renderingData.Length >= 6) renderingPosition = ReadSigned32(renderingData, 2);
        }

        string? filename = FirstText(properties, PidTagAttachLongFilename);
        if (filename == null) filename = FirstText(properties, PidTagAttachFilename);
        if (filename == null) filename = FirstText(properties, PidTagDisplayName);
        if (filename == null) filename = LegacyTextWithAttributesAndKindAndCodePageAndIssues(attributes, EmailTnefAttributeKind.AttachmentTitle, codePage, issues);
        if (filename == null) filename = LegacyTextWithAttributesAndKindAndCodePageAndIssues(attributes, EmailTnefAttributeKind.AttachmentTransportFilename, codePage, issues);
        string? contentType = FirstText(properties, PidTagAttachMimeTag);
        string? contentId = FirstText(properties, PidTagAttachContentId);
        string? contentLocation = FirstText(properties, PidTagAttachContentLocation);
        bool hidden = FirstInteger(properties, PidTagAttachmentHidden, 0) != 0;
        byte[] content = new byte[0];
        bool hasContent = false;
        EmailTnefProperty? binary = FindProperty(properties, PidTagAttachData, 258);
        if (binary != null && binary.GetValueCount() > 0)
        {
            content = binary.GetValueAt(0);
            hasContent = true;
        }
        else
        {
            EmailTnefAttribute? legacyData = FindAttribute(attributes, EmailTnefAttributeKind.AttachmentData);
            if (legacyData != null)
            {
                content = legacyData.GetData();
                hasContent = true;
            }
        }

        EmailTnefParseResult? embedded = ParseEmbedded(properties, attachMethod, options, issues);
        return new EmailTnefAttachment(index, attributes, properties, filename, contentType, contentId, contentLocation, content, hasContent, attachMethod, renderingPosition, hidden, embedded, issues);
    }

    static EmailTnefParseResult? ParseEmbedded(List<EmailTnefProperty> properties, int attachMethod, EmailTnefReaderOptions options, EmailIssueList issues)
    {
        EmailTnefProperty? value = FindProperty(properties, PidTagAttachData, 13);
        if (value == null || value.GetValueCount() == 0) return null;
        byte[] data = value.GetValueAt(0);
        byte[] messageInterface = new byte[] { 0x07, 0x03, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        if (data.Length < 16 || !StartsWith(data, messageInterface))
        {
            if (attachMethod == 5) AddIssue(issues, EmailIssueCode.TnefEmbeddedMessageMalformed, "An embedded-message attachment does not contain the IMessage interface identifier.", value.GetSourceRange().GetStartOffset(), "The object property bytes were retained.");
            return null;
        }

        if (options.GetMaximumEmbeddedDepth() == 0)
        {
            AddIssue(issues, EmailIssueCode.TnefEmbeddedMessageMalformed, "The configured embedded-message nesting depth was reached.", value.GetSourceRange().GetStartOffset(), "The embedded TNEF bytes were retained without recursive parsing.");
            return null;
        }

        byte[] nested = Copy(data, 16, data.Length - 16);
        EmailTnefReaderOptions nestedOptions = options.Clone();
        nestedOptions.SetMaximumEmbeddedDepth(options.GetMaximumEmbeddedDepth() - 1);
        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(nested, nestedOptions);
        if (!result.IsSuccess()) AddIssue(issues, EmailIssueCode.TnefEmbeddedMessageMalformed, "The attached embedded TNEF message is malformed.", value.GetSourceRange().GetStartOffset() + 16, "The nested parser result and original object bytes were retained.");
        return result;
    }

    static EmailTnefProperty? FirstProperty(List<EmailTnefProperty> properties, int propertyId)
    {
        EmailTnefProperty? fallback = null;
        int index = 0;
        while (index < properties.Count)
        {
            EmailTnefProperty property = properties[index];
            if (property.GetPropertyId() == propertyId)
            {
                if (property.GetTypeCode() == 31) return property;
                if (fallback == null) fallback = property;
            }

            index++;
        }

        return fallback;
    }

    static EmailTnefProperty? FindProperty(List<EmailTnefProperty> properties, int propertyId, int typeCode)
    {
        int index = 0;
        while (index < properties.Count)
        {
            if (properties[index].GetPropertyId() == propertyId && properties[index].GetTypeCode() == typeCode) return properties[index];
            index++;
        }

        return null;
    }

    static string? FirstText(List<EmailTnefProperty> properties, int propertyId)
    {
        EmailTnefProperty? property = FirstProperty(properties, propertyId);
        return property != null && property.HasTextValue() ? property.GetTextValue() : null;
    }

    static long FirstInteger(List<EmailTnefProperty> properties, int propertyId, long fallback)
    {
        int index = 0;
        while (index < properties.Count)
        {
            if (properties[index].GetPropertyId() == propertyId && properties[index].HasIntegerValue()) return properties[index].GetIntegerValue();
            index++;
        }

        return fallback;
    }

    static int FirstInt32(List<EmailTnefProperty> properties, int propertyId, int fallback)
    {
        long value = FirstInteger(properties, propertyId, fallback);
        return value < int.MinValue || value > int.MaxValue ? fallback : (int)value;
    }

    static EmailTnefAttribute? FirstAttribute(EmailTnefContainer container, EmailTnefAttributeKind kind)
    {
        return FindAttribute(container.GetAttributes(), kind);
    }

    static EmailTnefAttribute? FindAttribute(List<EmailTnefAttribute> attributes, EmailTnefAttributeKind kind)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (attributes[index].GetKind() == kind) return attributes[index];
            index++;
        }

        return null;
    }

    static string? LegacyTextWithContainerAndKindAndCodePageAndIssues(EmailTnefContainer container, EmailTnefAttributeKind kind, int codePage, EmailIssueList issues)
    {
        EmailTnefAttribute? attribute = FirstAttribute(container, kind);
        return attribute == null ? null : DecodeBytes(attribute.GetData(), codePage, issues, attribute.GetDataRange().GetStartOffset());
    }

    static string? LegacyTextWithAttributesAndKindAndCodePageAndIssues(List<EmailTnefAttribute> attributes, EmailTnefAttributeKind kind, int codePage, EmailIssueList issues)
    {
        EmailTnefAttribute? attribute = FindAttribute(attributes, kind);
        return attribute == null ? null : DecodeBytes(attribute.GetData(), codePage, issues, attribute.GetDataRange().GetStartOffset());
    }

    static string DecodeBytes(byte[] value, int codePage, EmailIssueList issues, long offset)
    {
        int length = value.Length > 0 && value[value.Length - 1] == 0 ? value.Length - 1 : value.Length;
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        EmailCharsetLookupResult lookup = registry.Lookup(CodePageName(codePage));
        if (!lookup.IsFound())
        {
            AddIssue(issues, EmailIssueCode.TnefCharsetUnsupported, "TNEF code page " + codePage + " is unavailable.", offset, "The value was decoded as ISO-8859-1 and its original bytes were retained.");
            return Encoding.Latin1.GetString(value, 0, length);
        }

        return (lookup.GetEncoding() ?? throw new InvalidOperationException()).GetString(value, 0, length);
    }

    static string CharsetName(int typeCode, int codePage)
    {
        return (typeCode & 4095) == 31 ? "utf-16le" : CodePageName(codePage);
    }

    static string CodePageName(int codePage)
    {
        return "cp" + (codePage <= 0 ? 437 : codePage);
    }

    static string? NormalizeMessageClass(string? value)
    {
        if (value == null) return null;
        string prefix = "Microsoft Mail v3.0 ";
        string comparable = value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? value.Substring(prefix.Length) : value;
        if (string.Equals(comparable, "IPM.Microsoft Mail.Note", StringComparison.OrdinalIgnoreCase)) return "IPM.Note";
        if (string.Equals(comparable, "IPM.Microsoft Mail.Read Receipt", StringComparison.OrdinalIgnoreCase)) return "Report.IPM.Note.IPNRN";
        if (string.Equals(comparable, "IPM.Microsoft Mail.Non-Delivery", StringComparison.OrdinalIgnoreCase)) return "Report.IPM.Note.NDR";
        if (string.Equals(comparable, "IPM.Microsoft Schedule.MtgRespP", StringComparison.OrdinalIgnoreCase)) return "IPM.Schedule.Meeting.Resp.Pos";
        if (string.Equals(comparable, "IPM.Microsoft Schedule.MtgRespN", StringComparison.OrdinalIgnoreCase)) return "IPM.Schedule.Meeting.Resp.Neg";
        if (string.Equals(comparable, "IPM.Microsoft Schedule.MtgRespA", StringComparison.OrdinalIgnoreCase)) return "IPM.Schedule.Meeting.Resp.Tent";
        if (string.Equals(comparable, "IPM.Microsoft Schedule.MtgReq", StringComparison.OrdinalIgnoreCase)) return "IPM.Schedule.Meeting.Request";
        if (string.Equals(comparable, "IPM.Microsoft Schedule.MtgCncl", StringComparison.OrdinalIgnoreCase)) return "IPM.Schedule.Meeting.Canceled";
        return value;
    }

    static bool StartsWith(byte[] value, byte[] prefix)
    {
        if (value.Length < prefix.Length) return false;
        int index = 0;
        while (index < prefix.Length)
        {
            if (value[index] != prefix[index]) return false;
            index++;
        }

        return true;
    }

    static long ReadUInt32(byte[] value, int offset)
    {
        return (uint)value[offset] | (uint)value[offset + 1] << 8 | (uint)value[offset + 2] << 16 | (uint)value[offset + 3] << 24;
    }

    static int ReadSigned32(byte[] value, int offset)
    {
        return value[offset] | value[offset + 1] << 8 | value[offset + 2] << 16 | value[offset + 3] << 24;
    }

    static byte[] Copy(byte[] value, int offset, int count)
    {
        byte[] result = new byte[count];
        if (count > 0) Array.Copy(value, offset, result, 0, count);
        return result;
    }

    static void AddIssue(EmailIssueList issues, EmailIssueCode code, string description, long offset, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, new EmailSourcePosition(offset, 1, offset + 1), null, null, recovery));
    }
}
