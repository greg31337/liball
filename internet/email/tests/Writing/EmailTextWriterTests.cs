using System.Text;
using email;

namespace email.Tests;

public class EmailTextWriterTests
{
    [Test]
    public void TextEntitySerializationUsesCanonicalByteWriterOutput()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("hello café");
        EmailWriter writer = new EmailWriter();

        EmailTextWriteResult result = writer.WriteTextWithEntity(entity);
        EmailWriteResult canonical = writer.WriteEntityWithEntity(entity);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.HasText(), Is.True);
        Assert.That(result.HasWriteResult(), Is.True);
        Assert.That(result.GetText(), Is.EqualTo(Encoding.UTF8.GetString(canonical.GetBytes())));
        Assert.That(result.GetWriteResult()!.GetBytes(), Is.EqualTo(canonical.GetBytes()));
        Assert.That(result.GetRequestedCharset(), Is.EqualTo("utf-8"));
        Assert.That(result.GetResolvedCharset(), Is.EqualTo("utf-8"));
        Assert.That(result.WereTransferEncodingsDecoded(), Is.True);
        Assert.That(result.GetText(), Does.Contain("hello caf=C3=A9"));
    }

    [Test]
    public void TextMessageSerializationIncludesHeadersAndTextBody()
    {
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.GetHeaders()!.Add(Header("Subject", " sample"));
        message.SetBody(new EmailEntityFactory().CreateTextWithValue("body"));

        EmailTextWriteResult result = new EmailWriter().WriteTextWithMessage(message);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetText(), Does.StartWith("Subject: sample\r\n"));
        Assert.That(result.GetText(), Does.EndWith("\r\n\r\nbody\r\n"));
    }

    [Test]
    public void NewlinePolicyChangesOnlyTextProjection()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("one\ntwo");
        EmailTextWriteOptions options = new EmailTextWriteOptions();
        options.SetNewlinePolicy(EmailTextNewlinePolicy.NormalizeToLf);

        EmailTextWriteResult result = new EmailWriter().WriteTextWithEntityAndOptions(entity, options);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.WasNewlineNormalized(), Is.True);
        Assert.That(result.GetText(), Does.Not.Contain("\r"));
        Assert.That(result.GetText(), Does.EndWith("one\ntwo\n"));
        Assert.That(Encoding.UTF8.GetString(result.GetWriteResult()!.GetBytes()), Does.Contain("\r\n"));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TextNewlineNormalized), Is.True);
    }

    [Test]
    public void Base64TextIsTransferDecodedForEligibilityButWireTextRemainsEncoded()
    {
        MimeEntity entity = ParseEntity("Content-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: base64\r\n\r\naGVsbG8=");

        EmailTextWriteResult result = new EmailWriter().WriteTextWithEntity(entity);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.WereTransferEncodingsDecoded(), Is.True);
        Assert.That(result.GetText(), Does.Contain("Content-Transfer-Encoding: base64"));
        Assert.That(result.GetText(), Does.Contain("aGVsbG8="));
    }

    [Test]
    public void NonTextualLeafReturnsFailureBeforeByteWriting()
    {
        MimeEntity entity = new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 0, 1, 2 });

        EmailTextWriteResult result = new EmailWriter().WriteTextWithEntity(entity);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasText(), Is.False);
        Assert.That(result.HasWriteResult(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TextSerializationNotTextual), Is.True);
        Assert.Throws<InvalidOperationException>(() => result.GetText());
    }

    [Test]
    public void MultipartMustContainOnlyTextualLeaves()
    {
        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtype("mixed");
        multipart.AddChild(new EmailEntityFactory().CreateTextWithValue("text"));
        multipart.AddChild(new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 1 }));

        EmailTextWriteResult result = new EmailWriter().WriteTextWithEntity(multipart);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TextSerializationNotTextual), Is.True);
    }

    [Test]
    public void ExplicitCharsetStrictlyDecodesWrittenBytes()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("café");
        EmailTextWriteOptions options = new EmailTextWriteOptions();
        options.SetCharsetName("us-ascii");
        options.SetWriteOptions(new EmailWriteOptionsOperations().CreateCanonicalSmtpUtf8());

        EmailTextWriteResult result = new EmailWriter().WriteTextWithEntityAndOptions(entity, options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasText(), Is.False);
        Assert.That(result.HasWriteResult(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CharsetInvalidByteSequence), Is.True);
    }

    [Test]
    public void UnsupportedOutputCharsetReturnsDiagnosticResult()
    {
        EmailTextWriteOptions options = new EmailTextWriteOptions();
        options.SetCharsetName("x-no-such-charset");

        EmailTextWriteResult result = new EmailWriter().WriteTextWithEntityAndOptions(new EmailEntityFactory().CreateTextWithValue("body"), options);

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasResolvedCharset(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CharsetUnsupported), Is.True);
    }

    [Test]
    public void CharacterAndByteLimitsAreEnforced()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("a reasonably long body");
        EmailTextWriteOptions characterOptions = new EmailTextWriteOptions();
        characterOptions.SetMaximumCharacters(5);
        EmailTextWriteOptions byteOptions = new EmailTextWriteOptions();
        byteOptions.SetMaximumBytes(10);

        EmailTextWriteResult characters = new EmailWriter().WriteTextWithEntityAndOptions(entity, characterOptions);
        EmailTextWriteResult bytes = new EmailWriter().WriteTextWithEntityAndOptions(entity, byteOptions);

        Assert.That(characters.IsSuccess(), Is.False);
        Assert.That(characters.GetIssues().ContainsCode(EmailIssueCode.CharsetCharacterLimitExceeded), Is.True);
        Assert.That(bytes.IsSuccess(), Is.False);
        Assert.That(bytes.HasText(), Is.False);
    }

    [Test]
    public void TransferDecodingCanBeDisabledExplicitly()
    {
        MimeEntity entity = ParseEntity("Content-Type: text/plain\r\nContent-Transfer-Encoding: x-custom\r\n\r\nbody");
        EmailTextWriteOptions enabled = new EmailTextWriteOptions();
        EmailTextWriteOptions disabled = new EmailTextWriteOptions();
        disabled.SetDecodeTransferEncodings(false);

        EmailTextWriteResult rejected = new EmailWriter().WriteTextWithEntityAndOptions(entity, enabled);
        EmailTextWriteResult written = new EmailWriter().WriteTextWithEntityAndOptions(entity, disabled);

        Assert.That(rejected.IsSuccess(), Is.False);
        Assert.That(rejected.GetIssues().ContainsCode(EmailIssueCode.TextSerializationTransferDecodeFailed), Is.True);
        Assert.That(written.WereTransferEncodingsDecoded(), Is.False);
        Assert.That(written.HasWriteResult(), Is.True);
    }

    [Test]
    public void ThrowFailureBehaviorThrowsForPolicyFailure()
    {
        EmailTextWriteOptions options = new EmailTextWriteOptions();
        options.SetFailureBehavior(EmailTextWriteFailureBehavior.Throw);
        MimeEntity binary = new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 0 });

        Assert.Throws<InvalidOperationException>(() => new EmailWriter().WriteTextWithEntityAndOptions(binary, options));
    }

    [Test]
    public void OptionsCloneDoesNotShareByteWritePolicy()
    {
        EmailTextWriteOptions original = new EmailTextWriteOptions();
        original.SetCharsetName("iso-8859-1");
        original.SetNewlinePolicy(EmailTextNewlinePolicy.NormalizeToCrlf);
        original.SetDecodeTransferEncodings(false);
        original.SetFailureBehavior(EmailTextWriteFailureBehavior.Throw);
        original.SetMaximumBytes(1234);
        original.SetMaximumCharacters(567);
        EmailWriteOptions write = original.GetWriteOptions();
        write.SetMaximumPartCount(12);
        original.SetWriteOptions(write);

        EmailTextWriteOptions clone = original.Clone();
        EmailWriteOptions cloneWrite = clone.GetWriteOptions();
        cloneWrite.SetMaximumPartCount(3);
        clone.SetWriteOptions(cloneWrite);

        Assert.That(original.GetCharsetName(), Is.EqualTo("iso-8859-1"));
        Assert.That(original.GetNewlinePolicy(), Is.EqualTo(EmailTextNewlinePolicy.NormalizeToCrlf));
        Assert.That(original.GetDecodeTransferEncodings(), Is.False);
        Assert.That(original.GetFailureBehavior(), Is.EqualTo(EmailTextWriteFailureBehavior.Throw));
        Assert.That(original.GetMaximumBytes(), Is.EqualTo(1234));
        Assert.That(original.GetMaximumCharacters(), Is.EqualTo(567));
        Assert.That(original.GetWriteOptions().GetMaximumPartCount(), Is.EqualTo(12));
        Assert.That(clone.GetWriteOptions().GetMaximumPartCount(), Is.EqualTo(3));
    }

    [Test]
    public void NullAndInvalidOptionsAreRejected()
    {
        EmailTextWriteOptions options = new EmailTextWriteOptions();
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValue("body");

        Assert.Throws<ArgumentNullException>(() => new EmailWriter().WriteTextWithEntityAndOptions(entity, null!));
        Assert.Throws<ArgumentNullException>(() => new EmailWriter().WriteTextWithMessage(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetWriteOptions(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetCharsetName(null!));
        Assert.Throws<ArgumentException>(() => options.SetCharsetName(""));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetNewlinePolicy((EmailTextNewlinePolicy)3));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetFailureBehavior((EmailTextWriteFailureBehavior)2));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumCharacters(0));
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        return result.GetEntity()!;
    }

    static EmailHeader Header(string name, string rawValue)
    {
        return new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Encoding.UTF8.GetBytes(rawValue), EmailHeaderValueKind.Unstructured);
    }
}
