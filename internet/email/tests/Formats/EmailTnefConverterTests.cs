using System.Text;

namespace email.Tests;

public class EmailTnefConverterTests
{
    [Test]
    public void ConvertsAlternativesRelatedResourcesAndRegularAttachments()
    {
        byte[] source = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.MessagePropertiesTag, PropertyList(
                VariableProperty(0x001F, 0x1000, Unicode("plain body")),
                VariableProperty(0x0102, 0x1013, Encoding.ASCII.GetBytes("<b>html body</b>")))),
            Attachment(PropertyList(
                VariableProperty(0x001F, 0x3707, Unicode("logo.png")),
                VariableProperty(0x001E, 0x370E, String8("image/png")),
                VariableProperty(0x001F, 0x3712, Unicode("logo@example.test")),
                VariableProperty(0x001F, 0x3713, Unicode("https://example.test/logo.png")),
                VariableProperty(0x0102, 0x3701, new byte[] { 1, 2, 3 }))),
            Attachment(PropertyList(
                VariableProperty(0x001F, 0x3707, Unicode("report.pdf")),
                VariableProperty(0x001E, 0x370E, String8("application/pdf")),
                VariableProperty(0x0102, 0x3701, new byte[] { 4, 5, 6 }))));
        EmailTnefParseResult parsed = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);

        EmailTnefConversionResult result = new EmailTnefConverter().ConvertWithParsed(parsed);

        Assert.That(result.IsSuccess(), Is.True, Describe(result.GetIssues()));
        Assert.That(result.GetOriginalParseResult(), Is.SameAs(parsed));
        Assert.That(result.GetOriginalMessage(), Is.SameAs(parsed.GetMessage()));
        Assert.That(result.GetOriginalProperties().Count, Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefConversionLossy), Is.True);
        MimeEntity mixed = result.GetEntity()!;
        Assert.That(mixed!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(mixed.GetChildCount(), Is.EqualTo(2));
        MimeEntity related = mixed.GetChildAt(0);
        MimeEntity regular = mixed.GetChildAt(1);
        Assert.That(related.GetMediaType()!.IsType("multipart", "related"), Is.True);
        Assert.That(related.GetChildCount(), Is.EqualTo(2));
        MimeEntity alternative = related.GetChildAt(0);
        MimeEntity inline = related.GetChildAt(1);
        Assert.That(alternative.GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(Text(alternative.GetChildAt(0)), Is.EqualTo("plain body"));
        Assert.That(Text(alternative.GetChildAt(1)), Is.EqualTo("<b>html body</b>"));
        Assert.Multiple(() =>
        {
            Assert.That(inline.GetMediaType()!.IsType("image", "png"), Is.True);
            Assert.That(inline.GetDisposition()!.IsInline(), Is.True);
            Assert.That(inline.GetContentId()!.GetIdLeft(), Is.EqualTo("logo"));
            Assert.That(inline.GetContentId()!.GetIdRight(), Is.EqualTo("example.test"));
            Assert.That(inline.GetContentLocation(), Is.EqualTo("https://example.test/logo.png"));
            Assert.That(inline.GetDecodedContent()!.Materialize(10), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(regular.GetMediaType()!.IsType("application", "pdf"), Is.True);
            Assert.That(regular.GetDisposition()!.IsAttachment(), Is.True);
            Assert.That(Filename(regular), Is.EqualTo("report.pdf"));
            Assert.That(regular.GetDecodedContent()!.Materialize(10), Is.EqualTo(new byte[] { 4, 5, 6 }));
        });
        List<EmailTnefConvertedAttachment> mappings = result.GetAttachments();
        Assert.That(mappings.Count, Is.EqualTo(2));
        Assert.That(mappings[0].GetEntity(), Is.SameAs(inline));
        Assert.That(mappings[1].GetEntity(), Is.SameAs(regular));
        AssertNoTnefEntity(mixed);
    }

    [Test]
    public void ConvertsEmbeddedMessagesAndRetainsNestedAttachmentMappings()
    {
        byte[] nested = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.MessagePropertiesTag, PropertyList(
                VariableProperty(0x001F, 0x0037, Unicode("Nested subject")),
                VariableProperty(0x001F, 0x1000, Unicode("nested body")))),
            Attachment(PropertyList(
                VariableProperty(0x001F, 0x3707, Unicode("nested.bin")),
                VariableProperty(0x0102, 0x3701, new byte[] { 9, 8, 7 }))));
        byte[] interfaceId = new byte[] { 0x07, 0x03, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        byte[] outer = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attachment(PropertyList(
                FixedProperty(0x0003, 0x3705, UInt32(5)),
                VariableProperty(0x000D, 0x3701, Concat(interfaceId, nested)))));
        EmailTnefParseResult parsed = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(outer);

        EmailTnefConversionResult result = new EmailTnefConverter().ConvertWithParsed(parsed);

        Assert.That(result.IsSuccess(), Is.True, Describe(result.GetIssues()));
        MimeEntity entity = result.GetEntity()!;
        Assert.That(entity!.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(entity.GetMediaType()!.IsType("message", "rfc822"), Is.True);
        Assert.That(entity.GetNestedMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Nested subject"));
        MimeEntity nestedBody = entity.GetNestedMessage()!.GetBody()!;
        Assert.That(nestedBody!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(Text(nestedBody.GetChildAt(0)), Is.EqualTo("nested body"));
        Assert.That(nestedBody.GetChildAt(1).GetDecodedContent()!.Materialize(10), Is.EqualTo(new byte[] { 9, 8, 7 }));
        List<EmailTnefConvertedAttachment> mappings = result.GetAttachments();
        Assert.That(mappings.Count, Is.EqualTo(2));
        Assert.That(mappings.Any(value => ReferenceEquals(value.GetSource(), parsed.GetMessage()!.GetAttachments()[0]) && ReferenceEquals(value.GetEntity(), entity)), Is.True);
        Assert.That(mappings.Any(value => value.GetSource().GetFilename() == "nested.bin" && value.GetEntity()!.GetMediaType()!.IsType("application", "octet-stream")), Is.True);
        AssertNoTnefEntity(entity);
    }

    [Test]
    public void FallsBackForUnsafeMetadataAndMapsSkippedAttachments()
    {
        byte[] source = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.MessagePropertiesTag, PropertyList(
                VariableProperty(0x001F, 0x1000, Unicode("body")))),
            Attachment(PropertyList(
                VariableProperty(0x001F, 0x3707, Unicode("unsafe\r\nname.bin")),
                VariableProperty(0x001E, 0x370E, String8("multipart/mixed")),
                VariableProperty(0x001F, 0x3712, Unicode("not a content id")),
                VariableProperty(0x001F, 0x3713, Unicode("unsafe\nlocation")),
                VariableProperty(0x0102, 0x3701, new byte[] { 6 }))),
            Attachment(PropertyList(
                VariableProperty(0x001F, 0x3707, Unicode("missing.bin")))));

        EmailTnefConversionResult result = new EmailTnefConverter().ConvertWithParsed(new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source));

        Assert.That(result.IsSuccess(), Is.True, Describe(result.GetIssues()));
        Assert.That(result.GetEntity()!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        MimeEntity attachment = result.GetEntity()!.GetChildAt(1);
        Assert.Multiple(() =>
        {
            Assert.That(attachment.GetMediaType()!.IsType("application", "octet-stream"), Is.True);
            Assert.That(attachment.HasContentId(), Is.False);
            Assert.That(attachment.HasContentLocation(), Is.False);
            Assert.That(Filename(attachment), Is.Null);
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefContentTypeInvalid), Is.True);
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefContentIdInvalid), Is.True);
            Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TnefAttachmentSkipped), Is.True);
        });
        List<EmailTnefConvertedAttachment> mappings = result.GetAttachments();
        Assert.That(mappings.Count, Is.EqualTo(2));
        Assert.That(mappings[0].HasEntity(), Is.True);
        Assert.That(mappings[1].HasEntity(), Is.False);
    }

    [Test]
    public void HonorsRichTextPolicyAndEntityCountLimit()
    {
        byte[] source = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.MessagePropertiesTag, PropertyList(
                VariableProperty(0x001F, 0x1000, Unicode("plain")),
                VariableProperty(0x0102, 0x1009, UncompressedRtf("{\\rtf1 rich}")))));
        EmailTnefParseResult parsed = new EmailTnefReader().ParseWithInputUsingReadOnlySpan(source);
        EmailTnefConversionOptions options = new EmailTnefConversionOptions();
        options.SetIncludeRichTextBody(false);

        EmailTnefConversionResult result = new EmailTnefConverter().ConvertWithParsedAndOptions(parsed, options);

        Assert.That(result.IsSuccess(), Is.True, Describe(result.GetIssues()));
        Assert.That(result.GetEntity()!.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(Text(result.GetEntity()!), Is.EqualTo("plain"));
        Assert.That(result.GetIssues().GetAll().Any(value => value.GetCode() == EmailIssueCode.TnefConversionLossy && value.GetDescription()!.Contains("RTF")), Is.True);
        EmailTnefConversionOptions bounded = new EmailTnefConversionOptions();
        bounded.SetMaximumEntityCount(2);
        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => new EmailTnefConverter().ConvertWithParsedAndOptions(parsed, bounded));
        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxTnefConvertedEntityCount"));
    }

    [Test]
    public void ConvertsTnefEntitiesAndReportsMissingParsedMessages()
    {
        byte[] source = Container(
            Attribute(1, EmailTnefReaderConstants.TnefVersionTag, UInt32(0x00010000)),
            Attribute(1, EmailTnefReaderConstants.OemCodePageTag, UInt32(1252)),
            Attribute(1, EmailTnefReaderConstants.BodyTag, String8("legacy body")));
        MimeEntity tnef = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Tnef, new EmailHeadersOperations().CreateWith(), Media("application", "ms-tnef"), new EmailContentOperations().CreateMemoryCopy(source, false), new List<MimeEntity>(), null);

        EmailTnefConversionResult converted = new EmailTnefConverter().ConvertWithEntity(tnef);
        EmailTnefConversionResult missing = new EmailTnefConverter().ConvertWithParsed(new EmailTnefParseResultOperations().CreateWithContainerAndIssuesAndComplete(null, new EmailIssueList(), false));

        Assert.That(converted.IsSuccess(), Is.True, Describe(converted.GetIssues()));
        Assert.That(Text(converted.GetEntity()!), Is.EqualTo("legacy body"));
        Assert.That(converted.GetOriginalParseResult().GetContainer()!.GetSourceBytes(), Is.EqualTo(source));
        Assert.That(missing.IsSuccess(), Is.False);
        Assert.That(missing.HasEntity(), Is.False);
        Assert.That(missing.GetIssues().ContainsCode(EmailIssueCode.TnefConversionFailed), Is.True);
    }

    static byte[] Attachment(byte[] properties)
    {
        return Concat(
            Attribute(2, EmailTnefReaderConstants.AttachmentRenderingDataTag, RenderingData(1, -1)),
            Attribute(2, EmailTnefReaderConstants.AttachmentPropertiesTag, properties));
    }

    static byte[] PropertyList(params byte[][] properties)
    {
        List<byte> result = new List<byte>();
        AddUInt32(result, (uint)properties.Length);
        foreach (byte[] property in properties) result.AddRange(property);
        return result.ToArray();
    }

    static byte[] VariableProperty(int type, int id, byte[] value)
    {
        List<byte> result = PropertyTag(type, id);
        AddUInt32(result, 1);
        AddUInt32(result, (uint)value.Length);
        result.AddRange(value);
        Pad(result, value.Length);
        return result.ToArray();
    }

    static byte[] FixedProperty(int type, int id, byte[] value)
    {
        List<byte> result = PropertyTag(type, id);
        result.AddRange(value);
        Pad(result, value.Length);
        return result.ToArray();
    }

    static List<byte> PropertyTag(int type, int id)
    {
        List<byte> result = new List<byte>();
        AddUInt16(result, type);
        AddUInt16(result, id);
        return result;
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
        return Concat(UInt32((uint)(raw.Length + 12)), UInt32((uint)raw.Length), UInt32(0x414C454D), UInt32(0), raw);
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
        AddUInt16(result, data.Sum(value => value) & 0xffff);
        return result.ToArray();
    }

    static byte[] UInt32(uint value)
    {
        List<byte> result = new List<byte>();
        AddUInt32(result, value);
        return result.ToArray();
    }

    static byte[] Concat(params byte[][] values)
    {
        return values.SelectMany(value => value).ToArray();
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

    static EmailMediaType Media(string type, string subtype)
    {
        return new EmailMediaType(type, subtype, new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>()), new byte[0], new EmailSourcePosition(0, 1, 1), false);
    }

    static string Text(MimeEntity entity)
    {
        return Encoding.UTF8.GetString(entity.GetContent()!.Materialize(1000));
    }

    static string? Filename(MimeEntity entity)
    {
        EmailRfc2231DecodedParameter value = entity.GetDisposition()!.GetParameters().GetDecoded("filename")!;
        return value == null ? null : Encoding.UTF8.GetString(value.GetDecodedBytes())!;
    }

    static string Describe(EmailIssueList issues)
    {
        return string.Join(" | ", issues.GetAll().Select(value => value.GetCode() + ": " + value.GetDescription()));
    }

    static void AssertNoTnefEntity(MimeEntity entity)
    {
        Assert.That(entity.GetKind(), Is.Not.EqualTo(MimeEntityKind.Tnef));
        Assert.That(entity.GetMediaType()!.IsType("application", "ms-tnef"), Is.False);
        int index = 0;
        while (index < entity.GetChildCount())
        {
            AssertNoTnefEntity(entity.GetChildAt(index));
            index++;
        }

        if (entity.HasNestedMessage() && entity.GetNestedMessage()!.HasBody()) AssertNoTnefEntity(entity.GetNestedMessage()!.GetBody()!);
    }
}
