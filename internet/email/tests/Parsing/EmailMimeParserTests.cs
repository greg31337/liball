using System.Text;
using email;

namespace email.Tests;

public class EmailMimeParserTests
{
    [Test]
    public void MissingContentHeadersCreateADefaultLeafWithoutSyntheticFields()
    {
        byte[] input = Bytes("X-Trace: retained\r\n\r\nplain body");

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(input);
        MimeEntity entity = result.GetEntity()!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(entity!.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(entity.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(entity.GetMediaType()!.WasDefaulted(), Is.True);
        Assert.That(new EmailMimeDefaults().GetEffectiveTextCharset(entity.GetMediaType()), Is.EqualTo("us-ascii"));
        Assert.That(entity.WasTransferEncodingDefaulted(), Is.True);
        Assert.That(entity.GetEffectiveTransferEncodingName(), Is.EqualTo("7bit"));
        Assert.That(entity.GetHeaders()!.Contains("Content-Type"), Is.False);
        Assert.That(entity.GetHeaders()!.GetFirst("X-Trace"), Is.Not.Null);
        Assert.That(entity.GetRawContent()!.GetKind(), Is.EqualTo(EmailContentKind.Slice));
        Assert.That(Text(entity.GetRawContent()!.Materialize(100)), Is.EqualTo("plain body"));
        Assert.That(entity.GetSourceRange()!.EqualsRange(new EmailRange(0, input.Length)), Is.True);
        Assert.That(entity.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void ParsesAllCommonContentMetadataWhileKeepingRawPayloadEncoded()
    {
        const string source = "Content-Type: Text/Plain; Charset=utf-8\r\nContent-Transfer-Encoding: BaSe64\r\nContent-Disposition: attachment; filename=report.txt\r\nContent-ID: <part@example.test>\r\nContent-Location: cid:part\r\nContent-Language: en, fr\r\nContent-Description: =?utf-8?Q?Quarterly_report?=\r\n\r\nU3RpbGwgZW5jb2RlZA==";

        MimeEntity entity = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source)).GetEntity()!;

        Assert.That(entity!.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(entity.GetMediaType()!.WasDefaulted(), Is.False);
        Assert.That(new EmailMimeDefaults().GetEffectiveTextCharset(entity.GetMediaType()), Is.EqualTo("utf-8"));
        Assert.That(entity.GetTransferEncodingName(), Is.EqualTo("BaSe64"));
        Assert.That(entity.GetTransferEncodingKind(), Is.EqualTo(EmailTransferEncodingKind.Base64));
        Assert.That(entity.GetDisposition()!.IsAttachment(), Is.True);
        Assert.That(Text(entity.GetDisposition()!.GetParameters().GetFirst("filename")!.GetValueBytes()), Is.EqualTo("report.txt"));
        Assert.That(entity.GetContentId()!.GetIdLeft(), Is.EqualTo("part"));
        Assert.That(entity.GetContentLocation(), Is.EqualTo("cid:part"));
        Assert.That(entity.GetContentLanguage()!.GetCount(), Is.EqualTo(2));
        Assert.That(entity.GetContentDescription(), Is.EqualTo("Quarterly report"));
        Assert.That(Text(entity.GetRawContent()!.Materialize(100)), Is.EqualTo("U3RpbGwgZW5jb2RlZA=="));
        Assert.That(entity.HasDecodedContent(), Is.False);
    }

    [Test]
    public void PreservesExtensionMediaTypesDuplicateParametersAndUnknownEncodings()
    {
        const string source = "Content-Type: X-World/X-Artifact; x-one=first; X-One=second\r\nContent-Transfer-Encoding: X-ROT13\r\nX-Unknown: opaque\r\n\r\npayload";

        MimeEntity entity = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source)).GetEntity()!;

        Assert.That(entity!.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(entity.GetMediaType()!.GetTopLevelType(), Is.EqualTo("X-World"));
        Assert.That(entity.GetMediaType()!.GetSubtype(), Is.EqualTo("X-Artifact"));
        Assert.That(entity.GetMediaType()!.IsExtensionType(), Is.True);
        Assert.That(entity.GetMediaType()!.GetParameters().HasDuplicate("x-one"), Is.True);
        Assert.That(entity.GetTransferEncodingName(), Is.EqualTo("X-ROT13"));
        Assert.That(entity.HasKnownTransferEncoding(), Is.False);
        Assert.That(entity.GetHeaders()!.Contains("X-Unknown"), Is.True);
        Assert.That(Text(entity.GetHeaders()!.GetFirst("Content-Type")!.GetOriginalRawFieldBytes()), Is.EqualTo("Content-Type: X-World/X-Artifact; x-one=first; X-One=second\r\n"));
    }

    [Test]
    public void WholeMessageParsingBuildsALazyBodyFromOnlyContentHeaders()
    {
        CountingSeekableParserStream stream = new CountingSeekableParserStream(Bytes("Subject: message\r\nContent-Type: application/octet-stream\r\nX-Trace: top\r\n\r\nstream body"));

        EmailParseResult result = new EmailParser().ParseWithInputUsingStream(stream);
        int readsAfterParse = stream.GetReadCount();
        EmailMessage message = result.GetMessage()!;
        MimeEntity body = message!.GetBody()!;

        Assert.That(body!.GetHeaders()!.GetCount(), Is.EqualTo(1));
        Assert.That(body.GetHeaders()!.Contains("Content-Type"), Is.True);
        Assert.That(body.GetHeaders()!.Contains("Subject"), Is.False);
        Assert.That(body.GetHeaders()!.Contains("X-Trace"), Is.False);
        Assert.That(body.GetSourceRange()!.EqualsRange(message.GetSourceInfo()!.GetBodyRange()!), Is.True);
        Assert.That(body.GetRawContent()!.GetKind(), Is.EqualTo(EmailContentKind.Slice));
        Assert.That(stream.GetReadCount(), Is.EqualTo(readsAfterParse));

        Assert.That(Text(body.GetRawContent()!.Materialize(100)), Is.EqualTo("stream body"));
        Assert.That(stream.GetReadCount(), Is.GreaterThan(readsAfterParse));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void MultipartTypeIsClassifiedAndItsChildrenAreParsed()
    {
        const string source = "Content-Type: multipart/x-vendor; boundary=abc\r\n\r\npreamble\r\n--abc\r\n\r\npart\r\n--abc--\r\n";

        MimeEntity entity = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source)).GetEntity()!;

        Assert.That(entity!.GetKind(), Is.EqualTo(MimeEntityKind.Multipart));
        Assert.That(entity.GetMediaType()!.GetSubtype(), Is.EqualTo("x-vendor"));
        Assert.That(entity.GetChildCount(), Is.EqualTo(1));
        Assert.That(Text(entity.GetPreamble()!.Materialize(100)), Is.EqualTo("preamble"));
        Assert.That(Text(entity.GetChildAt(0).GetContent()!.Materialize(100)), Is.EqualTo("part"));
        Assert.That(entity.GetChildAt(0).GetParent(), Is.SameAs(entity));
        Assert.That(entity.HasContent(), Is.False);
        Assert.That(entity.HasSourceRange(), Is.True);
    }

    [Test]
    public void MalformedTransferEncodingFallsBackWithoutDiscardingTheHeader()
    {
        const string source = "Content-Transfer-Encoding: base64; broken\r\n\r\npayload";

        EmailEntityParseResult tolerant = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        EmailEntityParseResult strict = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreateStrict());

        Assert.That(tolerant.GetEntity()!.WasTransferEncodingDefaulted(), Is.True);
        Assert.That(tolerant.GetEntity()!.GetHeaders()!.Contains("Content-Transfer-Encoding"), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.TransferEncodingMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.TransferEncodingMalformed), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    [Test]
    public void ReadOnlyMemoryEntityOwnsTheBytesBehindItsLazySlice()
    {
        byte[] input = Bytes("Content-Type: application/octet-stream\r\n\r\noriginal");

        MimeEntity entity = new EmailParser().ParseEntityWithInputUsingReadOnlyMemory(new ReadOnlyMemory<byte>(input)).GetEntity()!;
        input[input.Length - 1] = (byte)'X';

        Assert.That(Text(entity!.GetContent()!.Materialize(100)), Is.EqualTo("original"));
    }

    [Test]
    public void StandaloneStreamEntityRetainsItsFullRangeAndAllHeaders()
    {
        byte[] input = Bytes("Content-Type: text/plain\r\nX-Entity: retained\r\n\r\nbody");
        MemoryStream stream = new MemoryStream(input);

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingStreamAndEmailParseOptions(stream, new EmailParseOptionsOperations().CreateConformingTolerant());
        MimeEntity entity = result.GetEntity()!;

        Assert.That(entity!.GetSourceRange()!.EqualsRange(new EmailRange(0, input.Length)), Is.True);
        Assert.That(entity.GetHeaders()!.Contains("X-Entity"), Is.True);
        Assert.That(Text(entity.GetContent()!.Materialize(100)), Is.EqualTo("body"));
    }

    [Test]
    public void IncompleteEntityHonorsPartialResultPolicy()
    {
        byte[] input = Bytes("Content-Type: text/plain\r\n");

        EmailEntityParseResult tolerant = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, new EmailParseOptionsOperations().CreatePreserving());
        EmailEntityParseResult strict = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, new EmailParseOptionsOperations().CreateStrict());

        Assert.That(tolerant.IsComplete(), Is.False);
        Assert.That(tolerant.HasEntity(), Is.True);
        Assert.That(tolerant.GetEntity()!.GetContent()!.GetLength(), Is.Zero);
        Assert.That(strict.IsComplete(), Is.False);
        Assert.That(strict.HasEntity(), Is.False);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}

public class CountingSeekableParserStream : MemoryStream
{
    int readCount;

    public CountingSeekableParserStream(byte[] value) : base(value)
    {
        readCount = 0;
    }

    public int GetReadCount()
    {
        return readCount;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        readCount++;
        return base.Read(buffer, offset, count);
    }
}
