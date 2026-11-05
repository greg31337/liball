using System.Text;
using email;

namespace email.Tests;

public class EmailNestedMessageParserTests
{
    [Test]
    public void ParsesMessageRfc822IntoANestedMessageWithLocalLazyRanges()
    {
        const string nestedSource = "From: sender@example.test\r\nSubject: nested subject\r\nContent-Type: text/plain\r\nX-Nested: retained\r\n\r\nnested body";
        const string source = "Content-Type: message/rfc822\r\nContent-Description: wrapper\r\n\r\n" + nestedSource;
        byte[] bytes = Bytes(source);

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(bytes);
        MimeEntity entity = result.GetEntity()!;
        EmailMessage nested = entity!.GetNestedMessage()!;
        MimeEntity body = nested!.GetBody()!;

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(entity.GetMediaType()!.IsType("message", "rfc822"), Is.True);
        Assert.That(entity.GetMediaType()!.WasDefaulted(), Is.False);
        Assert.That(nested.GetHeaders()!.Contains("From"), Is.True);
        Assert.That(nested.GetHeaders()!.Contains("Subject"), Is.True);
        Assert.That(nested.GetHeaders()!.Contains("X-Nested"), Is.True);
        Assert.That(body!.GetHeaders()!.GetCount(), Is.EqualTo(1));
        Assert.That(body.GetHeaders()!.Contains("Content-Type"), Is.True);
        Assert.That(body.GetHeaders()!.Contains("Subject"), Is.False);
        Assert.That(Text(body.GetRawContent()!.Materialize(100)), Is.EqualTo("nested body"));
        Assert.That(nested.GetSourceInfo()!.GetContent()!.GetKind(), Is.EqualTo(EmailContentKind.Slice));
        Assert.That(nested.GetSourceInfo()!.GetMessageRange()!.EqualsRange(new EmailRange(0, Bytes(nestedSource).Length)), Is.True);
        Assert.That(body.GetSourceRange()!.EqualsRange(nested.GetSourceInfo()!.GetBodyRange()!), Is.True);
        Assert.That(nested.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        Assert.That(entity.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));

        int bodyOffset = source.LastIndexOf("nested body", StringComparison.Ordinal);
        bytes[bodyOffset] = (byte)'N';
        Assert.That(Text(body.GetRawContent()!.Materialize(100)), Is.EqualTo("Nested body"));
    }

    [Test]
    public void ParsesMessageGlobalUsingTheSameNestedMessageShape()
    {
        const string source = "Content-Type: message/global\r\n\r\nSubject: global message\r\n\r\nglobal body";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));
        MimeEntity entity = result.GetEntity()!;

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(entity!.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(entity.GetMediaType()!.IsType("message", "global"), Is.True);
        Assert.That(entity.GetNestedMessage()!.GetHeaders()!.Contains("Subject"), Is.True);
        Assert.That(entity.GetNestedMessage()!.GetBody()!.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(Text(entity.GetNestedMessage()!.GetBody()!.GetContent()!.Materialize(100)), Is.EqualTo("global body"));
    }

    [Test]
    public void DigestChildrenDefaultToMessageRfc822WhileExplicitTypesWin()
    {
        const string source = "Content-Type: multipart/digest; boundary=d\r\n\r\n--d\r\n\r\nFrom: first@example.test\r\nSubject: first entry\r\nContent-Type: text/plain\r\n\r\nfirst body\r\n--d\r\nContent-Type: text/plain\r\n\r\nexplicit leaf\r\n--d--\r\n";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));
        MimeEntity digest = result.GetEntity()!;
        MimeEntity defaulted = digest!.GetChildAt(0);
        MimeEntity explicitLeaf = digest.GetChildAt(1);

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(defaulted.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(defaulted.GetMediaType()!.IsType("message", "rfc822"), Is.True);
        Assert.That(defaulted.GetMediaType()!.WasDefaulted(), Is.True);
        Assert.That(defaulted.GetHeaders()!.GetCount(), Is.Zero);
        Assert.That(defaulted.GetNestedMessage()!.GetHeaders()!.Contains("From"), Is.True);
        Assert.That(defaulted.GetNestedMessage()!.GetHeaders()!.Contains("Subject"), Is.True);
        Assert.That(Text(defaulted.GetNestedMessage()!.GetBody()!.GetContent()!.Materialize(100)), Is.EqualTo("first body"));
        Assert.That(explicitLeaf.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(explicitLeaf.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(explicitLeaf.GetMediaType()!.WasDefaulted(), Is.False);
        Assert.That(Text(explicitLeaf.GetContent()!.Materialize(100)), Is.EqualTo("explicit leaf"));
    }

    [Test]
    public void MissingChildContentTypeOutsideDigestStillDefaultsToTextPlain()
    {
        const string source = "Content-Type: multipart/mixed; boundary=m\r\n\r\n--m\r\n\r\nplain child\r\n--m--\r\n";

        MimeEntity child = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source)).GetEntity()!.GetChildAt(0);

        Assert.That(child.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(child.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(child.GetMediaType()!.WasDefaulted(), Is.True);
        Assert.That(Text(child.GetContent()!.Materialize(100)), Is.EqualTo("plain child"));
    }

    [Test]
    public void IncompleteNestedHeadersRetainAUsefulPartialMessage()
    {
        const string source = "Content-Type: message/rfc822\r\n\r\nSubject: truncated\r\n";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));
        EmailMessage nested = result.GetEntity()!.GetNestedMessage()!;

        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.HasEntity(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderBlockIncomplete), Is.True);
        Assert.That(nested!.GetHeaders()!.Contains("Subject"), Is.True);
        Assert.That(nested.HasBody(), Is.False);
        Assert.That(nested.HasSourceInfo(), Is.True);
        Assert.That(nested.GetSourceInfo()!.IsComplete(), Is.False);
        Assert.That(nested.GetSourceInfo()!.GetMessageRange()!.GetLength(), Is.EqualTo(Bytes("Subject: truncated\r\n").Length));
    }

    [Test]
    public void NestedMessagesShareTheMimeNestingDepthLimit()
    {
        byte[] source = DoubleNestedMessage();
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        limits.GetParsingLimits().SetMaxNestingDepth(1);
        options.SetLimits(limits);
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        compatibility.SetReturnPartialResults(false);
        options.SetCompatibilityOptions(compatibility);

        EmailLimitException error = Assert.Throws<EmailLimitException>(() => new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, options));

        Assert.That(error.GetLimitName(), Is.EqualTo("MaxNestingDepth"));
        Assert.That(error.GetLimit(), Is.EqualTo(1));
        Assert.That(error.GetObserved(), Is.EqualTo(2));
    }

    [Test]
    public void NestedMessageBodiesShareTheMimePartLimit()
    {
        byte[] source = DoubleNestedMessage();
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailLimits limits = options.GetLimits();
        limits.GetParsingLimits().SetMaxPartCount(1);
        options.SetLimits(limits);
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        compatibility.SetReturnPartialResults(false);
        options.SetCompatibilityOptions(compatibility);

        EmailLimitException error = Assert.Throws<EmailLimitException>(() => new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, options));

        Assert.That(error.GetLimitName(), Is.EqualTo("MaxPartCount"));
        Assert.That(error.GetLimit(), Is.EqualTo(1));
        Assert.That(error.GetObserved(), Is.EqualTo(2));
    }

    static byte[] DoubleNestedMessage()
    {
        return Bytes("Content-Type: message/rfc822\r\n\r\nContent-Type: message/rfc822\r\n\r\nSubject: deepest\r\n\r\nbody");
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
