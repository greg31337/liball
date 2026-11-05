using System.Text;

namespace email.Tests;

public class EmailTnefSemanticTests
{
    [Test]
    public void ParsesCommonPropertiesRecipientsBodiesAndAttachmentMetadata()
    {
        byte[] rtf = UncompressedRtf("{\\rtf1\\ansi hello}");
        byte[] messageProperties = PropertyList(
            VariableProperty(0x001F, 0x0037, Unicode("Résumé")),
            VariableProperty(0x001E, 0x001A, String8("IPM.Note")),
            VariableProperty(0x001F, 0x1000, Unicode("plain body")),
            VariableProperty(0x0102, 0x1013, Encoding.ASCII.GetBytes("<b>html</b>")),
            VariableProperty(0x0102, 0x1009, rtf));
        byte[] recipientTable = RecipientTable(PropertyList(
            FixedProperty(0x0003, 0x0C15, UInt32(1)),
            VariableProperty(0x001F, 0x3001, Unicode("Ada Lovelace")),
            VariableProperty(0x001E, 0x39FE, String8("ada@example.test"))));
        byte[] attachmentProperties = PropertyList(
            FixedProperty(0x0003, 0x3705, UInt32(1)),
            FixedProperty(0x0003, 0x370B, UInt32(42)),
            FixedProperty(0x000B, 0x7FFE, UInt16(1)),
            VariableProperty(0x001F, 0x3707, Unicode("résumé.pdf")),
            VariableProperty(0x001E, 0x370E, String8("application/pdf")),
            VariableProperty(0x001F, 0x3712, Unicode("document@example.test")),
            VariableProperty(0x001F, 0x3713, Unicode("https://example.test/document")),
            VariableProperty(0x0102, 0x3701, new byte[] { 1, 2, 3, 4 }));
        byte[] source = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.MessagePropertiesTag, messageProperties),
            Attribute(1, EmailTnefReaderConstants.RecipientTableTag, recipientTable),
            Attribute(2, EmailTnefReaderConstants.AttachmentRenderingDataTag, RenderingData(1, 99)),
            Attribute(2, EmailTnefReaderConstants.AttachmentPropertiesTag, attachmentProperties));

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);

        Assert.That(result.IsSuccess(), Is.True, string.Join(" | ", result.GetIssues().GetAll().Select(value => value.GetDescription())));
        Assert.That(result.HasMessage(), Is.True);
        EmailTnefMessage message = result.GetMessage()!;
        Assert.Multiple(() =>
        {
            Assert.That(message!.GetSubject(), Is.EqualTo("Résumé"));
            Assert.That(message.GetMessageClass(), Is.EqualTo("IPM.Note"));
            Assert.That(message.GetProperties().Count, Is.EqualTo(5));
            Assert.That(message.GetRecipientCount(), Is.EqualTo(1));
            Assert.That(message.GetBodyCount(), Is.EqualTo(3));
            Assert.That(message.GetAttachmentCount(), Is.EqualTo(1));
        });

        EmailTnefRecipient recipient = message!.GetRecipients()[0];
        Assert.Multiple(() =>
        {
            Assert.That(recipient.GetKind(), Is.EqualTo(EmailTnefRecipientKind.To));
            Assert.That(recipient.GetDisplayName(), Is.EqualTo("Ada Lovelace"));
            Assert.That(recipient.GetSmtpAddress(), Is.EqualTo("ada@example.test"));
            Assert.That(recipient.GetPreferredAddress(), Is.EqualTo("ada@example.test"));
        });

        Assert.That(message.GetBodies().Single(value => value.GetKind() == EmailTnefBodyKind.PlainText).GetText(), Is.EqualTo("plain body"));
        Assert.That(message.GetBodies().Single(value => value.GetKind() == EmailTnefBodyKind.Html).GetText(), Is.EqualTo("<b>html</b>"));
        EmailTnefBody rich = message.GetBodies().Single(value => value.GetKind() == EmailTnefBodyKind.RichText);
        Assert.That(rich.GetText(), Is.EqualTo("{\\rtf1\\ansi hello}"));
        Assert.That(rich.GetOriginalContent(), Is.EqualTo(rtf));

        EmailTnefAttachment attachment = message.GetAttachments()[0];
        Assert.Multiple(() =>
        {
            Assert.That(attachment.GetFilename(), Is.EqualTo("résumé.pdf"));
            Assert.That(attachment.GetContentType(), Is.EqualTo("application/pdf"));
            Assert.That(attachment.GetContentId(), Is.EqualTo("document@example.test"));
            Assert.That(attachment.GetContentLocation(), Is.EqualTo("https://example.test/document"));
            Assert.That(attachment.GetAttachMethod(), Is.EqualTo(1));
            Assert.That(attachment.GetRenderingPosition(), Is.EqualTo(42));
            Assert.That(attachment.IsHidden(), Is.True);
            Assert.That(attachment.HasContent(), Is.True);
            Assert.That(attachment.GetContent(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(attachment.GetProperties().Count, Is.EqualTo(8));
        });
    }

    [Test]
    public void ParsesNamedAndMultivaluePropertiesWithoutLosingRawEncoding()
    {
        Guid propertySet = new Guid("00062008-0000-0000-c000-000000000046");
        byte[] numeric = NamedNumericVariableProperty(0x001F, 0x8000, propertySet, 0x8503, Unicode("blue"));
        byte[] named = NamedStringVariableProperty(0x001E, 0x8001, propertySet, "X-Color", String8("green"));
        byte[] multi = MultiVariableProperty(0x101F, 0x7000, Unicode("one"), Unicode("two"));
        byte[] source = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.MessagePropertiesTag, PropertyList(numeric, named, multi)));

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);

        Assert.That(result.IsSuccess(), Is.True, string.Join(" | ", result.GetIssues().GetAll().Select(value => value.GetDescription())));
        List<EmailTnefProperty> properties = result.GetMessage()!.GetProperties();
        EmailTnefProperty first = properties[0];
        EmailTnefProperty second = properties[1];
        EmailTnefProperty third = properties[2];
        Assert.Multiple(() =>
        {
            Assert.That(first.GetNamedKind(), Is.EqualTo(EmailTnefNamedPropertyKind.Numeric));
            Assert.That(first.GetNamedPropertySet(), Is.EqualTo(propertySet.ToString("D")));
            Assert.That(first.GetNamedNumericId(), Is.EqualTo(0x8503));
            Assert.That(first.GetTextValue(), Is.EqualTo("blue"));
            Assert.That(first.GetRawEncoding(), Is.EqualTo(numeric));
            Assert.That(second.GetNamedKind(), Is.EqualTo(EmailTnefNamedPropertyKind.String));
            Assert.That(second.GetNamedStringId(), Is.EqualTo("X-Color"));
            Assert.That(second.GetTextValue(), Is.EqualTo("green"));
            Assert.That(third.IsMultiValued(), Is.True);
            Assert.That(third.GetValueCount(), Is.EqualTo(2));
            Assert.That(third.GetTextValueAt(0), Is.EqualTo("one"));
            Assert.That(third.GetTextValueAt(1), Is.EqualTo("two"));
        });

        byte[] returned = first.GetValueAt(0);
        returned[0] = 0;
        Assert.That(first.GetTextValue(), Is.EqualTo("blue"));
        Assert.That(first.GetValueAt(0), Is.EqualTo(Unicode("blue")));
    }

    [Test]
    public void SignedIntegerPropertiesRetainNegativeBitPatterns()
    {
        byte[] source = MessageWithProperties(
            FixedProperty(0x0003, 0x4000, UInt32(uint.MaxValue)),
            FixedProperty(0x0014, 0x4001, new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }));

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetMessage()!.GetProperties()[0].GetIntegerValue(), Is.EqualTo(-1));
        Assert.That(result.GetMessage()!.GetProperties()[1].GetIntegerValue(), Is.EqualTo(-1));
    }

    [Test]
    public void UsesLegacyBodyAndAttachmentAttributesAsFallbacks()
    {
        byte[] messageClass = Attribute(1, EmailTnefReaderConstants.MessageClassTag, String8("Microsoft Mail v3.0 IPM.Microsoft Mail.Note"));
        messageClass[^1] ^= 1;
        byte[] source = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.SubjectTag, String8("Legacy subject")),
            messageClass,
            Attribute(1, EmailTnefReaderConstants.BodyTag, String8("Legacy body")),
            Attribute(2, EmailTnefReaderConstants.AttachmentRenderingDataTag, RenderingData(1, 12)),
            Attribute(2, EmailTnefReaderConstants.AttachmentTransportFilenameTag, String8("transport.bin")),
            Attribute(2, EmailTnefReaderConstants.AttachmentTitleTag, String8("legacy.bin")),
            Attribute(2, EmailTnefReaderConstants.AttachmentDataTag, new byte[] { 7, 8, 9 }));

        EmailTnefParseResult result = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefChecksumMismatch), Is.True);
        Assert.That(result.GetIssues().GetAll().Single(value => value.GetCode() == EmailIssueCode.TnefChecksumMismatch).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        EmailTnefMessage message = result.GetMessage()!;
        Assert.Multiple(() =>
        {
            Assert.That(message!.GetSubject(), Is.EqualTo("Legacy subject"));
            Assert.That(message.GetMessageClass(), Is.EqualTo("IPM.Note"));
            Assert.That(message.GetBodies()[0].GetText(), Is.EqualTo("Legacy body"));
            Assert.That(message.GetAttachments()[0].GetFilename(), Is.EqualTo("legacy.bin"));
            Assert.That(message.GetAttachments()[0].GetContent(), Is.EqualTo(new byte[] { 7, 8, 9 }));
            Assert.That(message.GetAttachments()[0].GetAttachMethod(), Is.EqualTo(1));
            Assert.That(message.GetAttachments()[0].GetRenderingPosition(), Is.EqualTo(12));
        });
    }

    [Test]
    public void DecompressesLzfuRichTextAndReportsCrcCorruption()
    {
        byte[] compressed = CompressedLiteralRtf("example");
        byte[] source = MessageWithProperties(VariableProperty(0x0102, 0x1009, compressed));

        EmailTnefParseResult valid = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);
        compressed[12] ^= 1;
        EmailTnefParseResult corrupt = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(MessageWithProperties(VariableProperty(0x0102, 0x1009, compressed)));

        Assert.That(valid.IsSuccess(), Is.True);
        Assert.That(valid.GetMessage()!.GetBodies()[0].GetText(), Is.EqualTo("example"));
        Assert.That(corrupt.IsSuccess(), Is.False);
        Assert.That(corrupt.GetIssues().ContainsCode(EmailIssueCode.TnefRtfMalformed), Is.True);
        Assert.That(corrupt.GetMessage()!.GetBodies()[0].GetContent(), Is.EqualTo(Encoding.ASCII.GetBytes("example")));
        Assert.That(corrupt.GetMessage()!.GetBodies()[0].GetOriginalContent(), Is.EqualTo(compressed));
    }

    [Test]
    public void ParsesEmbeddedTnefMessageAndHonorsNestingLimit()
    {
        byte[] nested = MessageWithProperties(VariableProperty(0x001F, 0x0037, Unicode("Nested subject")));
        byte[] interfaceId = new byte[] { 0x07, 0x03, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        byte[] attachmentProperties = PropertyList(
            FixedProperty(0x0003, 0x3705, UInt32(5)),
            VariableProperty(0x000D, 0x3701, Concat(interfaceId, nested)));
        byte[] outer = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(2, EmailTnefReaderConstants.AttachmentRenderingDataTag, RenderingData(1, -1)),
            Attribute(2, EmailTnefReaderConstants.AttachmentPropertiesTag, attachmentProperties));

        EmailTnefParseResult parsed = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(outer);
        EmailTnefReaderOptions boundedOptions = new EmailTnefReaderOptions();
        boundedOptions.SetMaximumEmbeddedDepth(0);
        EmailTnefParseResult bounded = new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(outer, boundedOptions);

        EmailTnefAttachment attachment = parsed.GetMessage()!.GetAttachments()[0];
        Assert.That(parsed.IsSuccess(), Is.True);
        Assert.That(attachment.HasEmbeddedMessage(), Is.True);
        Assert.That(attachment.GetEmbeddedMessage()!.GetSubject(), Is.EqualTo("Nested subject"));
        Assert.That(attachment.GetEmbeddedParseResult()!.GetContainer()!.GetSourceBytes(), Is.EqualTo(nested));
        Assert.That(bounded.IsSuccess(), Is.False);
        Assert.That(bounded.GetMessage()!.GetAttachments()[0].HasEmbeddedMessage(), Is.False);
        Assert.That(bounded.GetIssues().ContainsCode(EmailIssueCode.TnefEmbeddedMessageMalformed), Is.True);
    }

    [Test]
    public void RetainsMalformedPropertiesAndEnforcesSemanticLimits()
    {
        List<byte> malformedProperty = new List<byte>();
        AddUInt16(malformedProperty, 0x001F);
        AddUInt16(malformedProperty, 0x0037);
        AddUInt32(malformedProperty, 1);
        AddUInt32(malformedProperty, 100);
        malformedProperty.AddRange(new byte[] { 65, 0 });
        byte[] malformed = MessageWithRawPropertyList(1, malformedProperty.ToArray());

        EmailTnefParseResult recovered = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(malformed);

        Assert.That(recovered.IsComplete(), Is.True);
        Assert.That(recovered.IsSuccess(), Is.False);
        Assert.That(recovered.GetIssues().ContainsCode(EmailIssueCode.TnefPropertyMalformed), Is.True);
        Assert.That(recovered.GetMessage()!.GetProperties().Single().IsComplete(), Is.False);
        Assert.That(recovered.GetContainer()!.GetSourceBytes(), Is.EqualTo(malformed));

        byte[] twoProperties = MessageWithProperties(
            VariableProperty(0x001F, 0x0037, Unicode("one")),
            VariableProperty(0x001F, 0x1000, Unicode("two")));
        EmailTnefReaderOptions propertyOptions = new EmailTnefReaderOptions();
        propertyOptions.SetMaximumPropertyCount(1);
        EmailLimitException propertyLimit = Assert.Throws<EmailLimitException>(() => new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(twoProperties, propertyOptions));
        Assert.That(propertyLimit.GetLimitName(), Is.EqualTo("MaxTnefPropertyCount"));

        byte[] recipients = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.RecipientTableTag, RecipientTable(PropertyList(), PropertyList())));
        EmailTnefReaderOptions recipientOptions = new EmailTnefReaderOptions();
        recipientOptions.SetMaximumRecipientCount(1);
        EmailLimitException recipientLimit = Assert.Throws<EmailLimitException>(() => new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(recipients, recipientOptions));
        Assert.That(recipientLimit.GetLimitName(), Is.EqualTo("MaxTnefRecipientCount"));

        EmailTnefReaderOptions rtfOptions = new EmailTnefReaderOptions();
        rtfOptions.SetMaximumDecodedRtfBytes(3);
        EmailLimitException rtfLimit = Assert.Throws<EmailLimitException>(() => new EmailTnefReader().ParseWithInputAndOptionsUsingReadOnlySpanAndEmailTnefReaderOptions(MessageWithProperties(VariableProperty(0x0102, 0x1009, UncompressedRtf("four"))), rtfOptions));
        Assert.That(rtfLimit.GetLimitName(), Is.EqualTo("MaxTnefDecodedRtfBytes"));
    }

    [Test]
    public void SemanticReaderOptionsCloneAllLimitsIndependently()
    {
        EmailTnefReaderOptions options = new EmailTnefReaderOptions();
        options.SetMaximumPropertyCount(11);
        options.SetMaximumPropertyValueCount(12);
        options.SetMaximumRecipientCount(13);
        options.SetMaximumEmbeddedDepth(3);
        options.SetMaximumNamedPropertyCharacters(14);
        options.SetMaximumDecodedRtfBytes(15);

        EmailTnefReaderOptions clone = options.Clone();
        options.SetMaximumPropertyCount(20);

        Assert.Multiple(() =>
        {
            Assert.That(clone.GetMaximumPropertyCount(), Is.EqualTo(11));
            Assert.That(clone.GetMaximumPropertyValueCount(), Is.EqualTo(12));
            Assert.That(clone.GetMaximumRecipientCount(), Is.EqualTo(13));
            Assert.That(clone.GetMaximumEmbeddedDepth(), Is.EqualTo(3));
            Assert.That(clone.GetMaximumNamedPropertyCharacters(), Is.EqualTo(14));
            Assert.That(clone.GetMaximumDecodedRtfBytes(), Is.EqualTo(15));
            Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEmbeddedDepth(-1));
        });
    }

    static byte[] MessageWithProperties(params byte[][] properties)
    {
        return Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.MessagePropertiesTag, PropertyList(properties)));
    }

    static byte[] MessageWithRawPropertyList(int count, byte[] bytes)
    {
        return Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.MessagePropertiesTag, Concat(UInt32((uint)count), bytes)));
    }

    static byte[] PropertyList(params byte[][] properties)
    {
        List<byte> result = new List<byte>();
        AddUInt32(result, (uint)properties.Length);
        foreach (byte[] property in properties) result.AddRange(property);
        return result.ToArray();
    }

    static byte[] RecipientTable(params byte[][] rows)
    {
        List<byte> result = new List<byte>();
        AddUInt32(result, (uint)rows.Length);
        foreach (byte[] row in rows) result.AddRange(row);
        return result.ToArray();
    }

    static byte[] VariableProperty(int type, int id, byte[] value)
    {
        List<byte> result = PropertyTag(type, id);
        AddUInt32(result, 1);
        AddSizedValue(result, value);
        return result.ToArray();
    }

    static byte[] MultiVariableProperty(int type, int id, params byte[][] values)
    {
        List<byte> result = PropertyTag(type, id);
        AddUInt32(result, (uint)values.Length);
        foreach (byte[] value in values) AddSizedValue(result, value);
        return result.ToArray();
    }

    static byte[] FixedProperty(int type, int id, byte[] value)
    {
        List<byte> result = PropertyTag(type, id);
        result.AddRange(value);
        Pad(result, value.Length);
        return result.ToArray();
    }

    static byte[] NamedNumericVariableProperty(int type, int id, Guid propertySet, uint numericId, byte[] value)
    {
        List<byte> result = PropertyTag(type, id);
        result.AddRange(propertySet.ToByteArray());
        AddUInt32(result, 0);
        AddUInt32(result, numericId);
        AddUInt32(result, 1);
        AddSizedValue(result, value);
        return result.ToArray();
    }

    static byte[] NamedStringVariableProperty(int type, int id, Guid propertySet, string name, byte[] value)
    {
        List<byte> result = PropertyTag(type, id);
        result.AddRange(propertySet.ToByteArray());
        AddUInt32(result, 1);
        byte[] encodedName = Unicode(name);
        AddUInt32(result, (uint)encodedName.Length);
        result.AddRange(encodedName);
        Pad(result, encodedName.Length);
        AddUInt32(result, 1);
        AddSizedValue(result, value);
        return result.ToArray();
    }

    static List<byte> PropertyTag(int type, int id)
    {
        List<byte> result = new List<byte>();
        AddUInt16(result, type);
        AddUInt16(result, id);
        return result;
    }

    static void AddSizedValue(List<byte> result, byte[] value)
    {
        AddUInt32(result, (uint)value.Length);
        result.AddRange(value);
        Pad(result, value.Length);
    }

    static byte[] Unicode(string value)
    {
        return Encoding.Unicode.GetBytes(value + "\0");
    }

    static byte[] String8(string value)
    {
        return Encoding.ASCII.GetBytes(value + "\0");
    }

    static byte[] UncompressedRtf(string value)
    {
        byte[] raw = Encoding.ASCII.GetBytes(value);
        List<byte> result = new List<byte>();
        AddUInt32(result, (uint)(raw.Length + 12));
        AddUInt32(result, (uint)raw.Length);
        AddUInt32(result, 0x414C454D);
        AddUInt32(result, 0);
        result.AddRange(raw);
        return result.ToArray();
    }

    static byte[] CompressedLiteralRtf(string value)
    {
        byte[] raw = Encoding.ASCII.GetBytes(value);
        Assert.That(raw.Length, Is.EqualTo(7));
        List<byte> contents = new List<byte>();
        contents.Add(0x80);
        contents.AddRange(raw);
        int writeOffset = 207 + raw.Length;
        contents.Add((byte)(writeOffset >> 4));
        contents.Add((byte)(writeOffset << 4));
        byte[] contentBytes = contents.ToArray();
        List<byte> result = new List<byte>();
        AddUInt32(result, (uint)(contentBytes.Length + 12));
        AddUInt32(result, (uint)raw.Length);
        AddUInt32(result, 0x75465A4C);
        AddUInt32(result, Crc32(contentBytes));
        result.AddRange(contentBytes);
        return result.ToArray();
    }

    static uint Crc32(byte[] value)
    {
        uint crc = 0;
        foreach (byte current in value)
        {
            crc ^= current;
            for (int bit = 0; bit < 8; bit++) crc = (crc & 1) == 0 ? crc >> 1 : crc >> 1 ^ 0xEDB88320;
        }

        return crc;
    }

    static byte[] RenderingData(int type, int position)
    {
        List<byte> result = new List<byte>();
        AddUInt16(result, type);
        AddUInt32(result, (uint)position);
        AddUInt16(result, 0);
        AddUInt16(result, 0);
        AddUInt32(result, 0);
        return result.ToArray();
    }

    static byte[] Container(params byte[][] attributes)
    {
        List<byte> result = new List<byte>();
        AddUInt32(result, EmailTnefReaderConstants.TnefSignature);
        AddUInt16(result, 0x1234);
        foreach (byte[] attribute in attributes) result.AddRange(attribute);
        return result.ToArray();
    }

    static byte[] Attribute(int level, uint tag, byte[] data)
    {
        List<byte> result = new List<byte>();
        result.Add((byte)level);
        AddUInt32(result, tag);
        AddUInt32(result, (uint)data.Length);
        result.AddRange(data);
        int checksum = data.Sum(value => value) & 0xffff;
        AddUInt16(result, checksum);
        return result.ToArray();
    }

    static byte[] UInt16(int value)
    {
        List<byte> result = new List<byte>();
        AddUInt16(result, value);
        return result.ToArray();
    }

    static byte[] UInt32(uint value)
    {
        List<byte> result = new List<byte>();
        AddUInt32(result, value);
        return result.ToArray();
    }

    static byte[] Concat(byte[] first, byte[] second)
    {
        return first.Concat(second).ToArray();
    }

    static void AddUInt16(List<byte> output, int value)
    {
        output.Add((byte)value);
        output.Add((byte)(value >> 8));
    }

    static void AddUInt32(List<byte> output, uint value)
    {
        output.Add((byte)value);
        output.Add((byte)(value >> 8));
        output.Add((byte)(value >> 16));
        output.Add((byte)(value >> 24));
    }

    static void Pad(List<byte> output, int length)
    {
        int padding = (4 - length % 4) % 4;
        while (padding > 0)
        {
            output.Add(0);
            padding--;
        }
    }
}
