using System.Text;
using email;

namespace email.Tests;

public class EmailTextDecoderTests
{
    [Test]
    public void DeclaredCharsetIsAppliedAfterContentTransferDecoding()
    {
        MimeEntity entity = ParseEntity(
            "Content-Type: text/plain; charset=windows-1252\r\nContent-Transfer-Encoding: base64\r\n\r\n",
            Encoding.ASCII.GetBytes("gA=="));

        EmailTextDecodeResult result = new EmailTextDecoder().DecodeWithEntity(entity);

        Assert.That(result.GetText(), Is.EqualTo("€"));
        Assert.That(result.GetDeclaredCharset(), Is.EqualTo("windows-1252"));
        Assert.That(result.GetSelectedCharset(), Is.EqualTo("windows-1252"));
        Assert.That(result.GetCharsetSource(), Is.EqualTo(EmailTextCharsetSource.Declared));
        Assert.That(result.HasBom(), Is.False);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void BomOverridesAConflictingDeclarationAndReportsSelection()
    {
        byte[] body = new byte[] { 0xef, 0xbb, 0xbf, 0xc3, 0xa9 };
        MimeEntity entity = ParseEntity(
            "Content-Type: text/plain; charset=iso-8859-1\r\nContent-Transfer-Encoding: binary\r\n\r\n",
            body);

        EmailTextDecodeResult result = new EmailTextDecoder().DecodeWithEntity(entity);

        Assert.That(result.GetText(), Is.EqualTo("é"));
        Assert.That(result.GetCharsetSource(), Is.EqualTo(EmailTextCharsetSource.ByteOrderMark));
        Assert.That(result.GetSelectedCharset(), Is.EqualTo("utf-8"));
        Assert.That(result.GetBomCharset(), Is.EqualTo("utf-8"));
        Assert.That(result.GetBomByteCount(), Is.EqualTo(3));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.CharsetBomMismatch), Is.True);
    }

    [Test]
    public void MissingAndUnsupportedDeclarationsUseConfigurableFallback()
    {
        byte[] utf8 = Encoding.UTF8.GetBytes("café");
        MimeEntity missing = ParseEntity(
            "Content-Type: text/plain\r\nContent-Transfer-Encoding: binary\r\n\r\n",
            utf8);
        MimeEntity unsupported = ParseEntity(
            "Content-Type: text/plain; charset=x-unknown\r\nContent-Transfer-Encoding: binary\r\n\r\n",
            utf8);

        EmailTextDecodeOptions utf8Fallback = new EmailTextDecodeOptions();
        utf8Fallback.SetFallbackCharset("utf-8");
        EmailTextDecodeResult missingResult = new EmailTextDecoder().DecodeWithEntityAndOptions(missing, utf8Fallback);
        EmailTextDecodeResult unsupportedResult = new EmailTextDecoder().DecodeWithEntityAndOptions(unsupported, utf8Fallback);

        Assert.That(missingResult.GetText(), Is.EqualTo("café"));
        Assert.That(missingResult.HasDeclaredCharset(), Is.False);
        Assert.That(missingResult.GetCharsetSource(), Is.EqualTo(EmailTextCharsetSource.Fallback));
        Assert.That(missingResult.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Information));
        Assert.That(unsupportedResult.GetText(), Is.EqualTo("café"));
        Assert.That(unsupportedResult.GetCharsetSource(), Is.EqualTo(EmailTextCharsetSource.Fallback));
        Assert.That(unsupportedResult.GetIssues().ContainsCode(EmailIssueCode.TextCharsetFallbackUsed), Is.True);
        Assert.That(unsupportedResult.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(unsupportedResult.IsSuccess(), Is.True);

        EmailTextDecodeOptions noFallback = new EmailTextDecodeOptions();
        noFallback.ClearFallbackCharset();
        EmailTextDecodeResult unresolved = new EmailTextDecoder().DecodeWithEntityAndOptions(unsupported, noFallback);
        Assert.That(unresolved.HasSelectedCharset(), Is.False);
        Assert.That(unresolved.GetCharsetSource(), Is.EqualTo(EmailTextCharsetSource.None));
        Assert.That(unresolved.GetText(), Is.Empty);
        Assert.That(unresolved.IsSuccess(), Is.False);
    }

    [Test]
    public void NewlinePoliciesNormalizeMixedInputDeterministically()
    {
        byte[] input = Encoding.UTF8.GetBytes("a\r\nb\rc\nd");
        EmailTextDecodeOptions lf = new EmailTextDecodeOptions();
        lf.SetNewlinePolicy(EmailTextNewlinePolicy.NormalizeToLf);
        EmailTextDecodeOptions crlf = new EmailTextDecodeOptions();
        crlf.SetNewlinePolicy(EmailTextNewlinePolicy.NormalizeToCrlf);

        EmailTextDecodeResult lfResult = new EmailTextDecoder().DecodeWithInputAndDeclaredCharsetAndOptions(input, "utf-8", lf);
        EmailTextDecodeResult crlfResult = new EmailTextDecoder().DecodeWithInputAndDeclaredCharsetAndOptions(input, "utf-8", crlf);
        EmailTextDecodeResult preserved = new EmailTextDecoder().DecodeWithInputAndDeclaredCharset(input, "utf-8");

        Assert.That(lfResult.GetText(), Is.EqualTo("a\nb\nc\nd"));
        Assert.That(crlfResult.GetText(), Is.EqualTo("a\r\nb\r\nc\r\nd"));
        Assert.That(preserved.GetText(), Is.EqualTo("a\r\nb\rc\nd"));
        Assert.That(lfResult.WasNewlineNormalized(), Is.True);
        Assert.That(crlfResult.WasNewlineNormalized(), Is.True);
        Assert.That(preserved.WasNewlineNormalized(), Is.False);
        Assert.That(lfResult.GetIssues().ContainsCode(EmailIssueCode.TextNewlineNormalized), Is.True);
    }

    [Test]
    public void UnicodeNormalizationIsExplicitAndReportsChanges()
    {
        byte[] input = Encoding.UTF8.GetBytes("e\u0301 \uFB00");
        EmailTextDecodeOptions nfc = new EmailTextDecodeOptions();
        nfc.SetUnicodeNormalization(EmailTextUnicodeNormalization.Nfc);
        EmailTextDecodeOptions nfkc = new EmailTextDecodeOptions();
        nfkc.SetUnicodeNormalization(EmailTextUnicodeNormalization.Nfkc);

        EmailTextDecodeResult nfcResult = new EmailTextDecoder().DecodeWithInputAndDeclaredCharsetAndOptions(input, "utf-8", nfc);
        EmailTextDecodeResult nfkcResult = new EmailTextDecoder().DecodeWithInputAndDeclaredCharsetAndOptions(input, "utf-8", nfkc);

        Assert.That(nfcResult.GetText(), Is.EqualTo("é ﬀ"));
        Assert.That(nfkcResult.GetText(), Is.EqualTo("é ff"));
        Assert.That(nfcResult.WasUnicodeNormalized(), Is.True);
        Assert.That(nfkcResult.WasUnicodeNormalized(), Is.True);
        Assert.That(nfkcResult.GetIssues().ContainsCode(EmailIssueCode.TextUnicodeNormalized), Is.True);
    }

    [Test]
    public void ControlAndBidiDiagnosticsPreserveByDefaultAndCanRemoveExplicitly()
    {
        string suspicious = "A\0B\u202EC\u2066D\t\r\n";
        MimeEntity entity = ParseEntity(
            "Content-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: binary\r\n\r\n",
            Encoding.UTF8.GetBytes(suspicious));
        EmailPath path = new EmailPathOperations().CreateRoot().Append(3);
        EmailAttachmentView attachment = new EmailAttachmentView(entity, path, EmailAttachmentKind.Attachment, "note.txt");

        EmailTextDecodeResult preserved = attachment.DecodeText();
        Assert.That(preserved.GetText(), Is.EqualTo(suspicious));
        Assert.That(preserved.GetControlCharacterCount(), Is.EqualTo(1));
        Assert.That(preserved.GetBidiControlCount(), Is.EqualTo(2));
        Assert.That(preserved.WereControlCharactersRemoved(), Is.False);
        Assert.That(preserved.WereBidiControlsRemoved(), Is.False);
        Assert.That(preserved.GetIssues().ContainsCode(EmailIssueCode.TextControlCharacter), Is.True);
        Assert.That(preserved.GetIssues().ContainsCode(EmailIssueCode.TextBidiControl), Is.True);
        Assert.That(preserved.GetIssues().GetAt(0).GetEntityPath(), Is.SameAs(path));

        EmailTextDecodeOptions remove = new EmailTextDecodeOptions();
        remove.SetRemoveControlCharacters(true);
        remove.SetRemoveBidiControls(true);
        EmailTextDecodeResult removed = attachment.DecodeTextWithOptions(remove);
        Assert.That(removed.GetText(), Is.EqualTo("ABCD\t\r\n"));
        Assert.That(removed.WereControlCharactersRemoved(), Is.True);
        Assert.That(removed.WereBidiControlsRemoved(), Is.True);
    }

    [Test]
    public void CharsetFallbackPolicyAndFinalCharacterLimitRemainBounded()
    {
        byte[] invalid = new byte[] { 0x41, 0xff, 0x42 };
        EmailTextDecodeResult replaced = new EmailTextDecoder().DecodeWithInputAndDeclaredCharset(invalid, "utf-8");
        Assert.That(replaced.GetText(), Is.EqualTo("A�B"));
        Assert.That(replaced.GetUndecodableBytes(), Is.EqualTo(new byte[] { 0xff }));
        Assert.That(replaced.UsedReplacement(), Is.True);

        EmailTextDecodeOptions strict = new EmailTextDecodeOptions();
        EmailCharsetDecodeOptions strictCharset = strict.GetCharsetOptions();
        strictCharset.SetFallbackPolicy(EmailCharsetFallbackPolicy.Strict);
        strict.SetCharsetOptions(strictCharset);
        EmailTextDecodeResult rejected = new EmailTextDecoder().DecodeWithInputAndDeclaredCharsetAndOptions(invalid, "utf-8", strict);
        Assert.That(rejected.GetText(), Is.EqualTo("AB"));
        Assert.That(rejected.IsSuccess(), Is.False);

        EmailTextDecodeOptions expanding = new EmailTextDecodeOptions();
        EmailCharsetDecodeOptions oneCharacter = expanding.GetCharsetOptions();
        oneCharacter.SetMaximumCharacters(1);
        expanding.SetCharsetOptions(oneCharacter);
        expanding.SetUnicodeNormalization(EmailTextUnicodeNormalization.Nfkc);
        EmailTextDecodeResult limited = new EmailTextDecoder().DecodeWithInputAndDeclaredCharsetAndOptions(Encoding.UTF8.GetBytes("ﬀ"), "utf-8", expanding);
        Assert.That(limited.GetText(), Is.EqualTo("f"));
        Assert.That(limited.HasFatalError(), Is.True);
        Assert.That(limited.GetIssues().ContainsCode(EmailIssueCode.CharsetCharacterLimitExceeded), Is.True);

        MimeEntity threeBytes = ParseEntity(
            "Content-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: binary\r\n\r\n",
            Encoding.ASCII.GetBytes("abc"));
        EmailTextDecodeOptions byteLimit = new EmailTextDecodeOptions();
        byteLimit.SetMaximumDecodedBytes(2);
        EmailLimitException limit = Assert.Throws<EmailLimitException>(() => new EmailTextDecoder().DecodeWithEntityAndOptions(threeBytes, byteLimit));
        Assert.That(limit.GetLimitName(), Is.EqualTo("MaxDecodedContentBytes"));
    }

    [Test]
    public void OptionsAndResultsAreSnapshotsAndInputsAreValidated()
    {
        EmailTextDecodeOptions options = new EmailTextDecodeOptions();
        options.SetFallbackCharset("windows-1252");
        options.SetNewlinePolicy(EmailTextNewlinePolicy.NormalizeToLf);
        EmailCharsetDecodeOptions charset = options.GetCharsetOptions();
        charset.SetReplacement("[bad]");
        options.SetCharsetOptions(charset);
        EmailTextDecodeOptions clone = options.Clone();
        options.SetFallbackCharset("utf-8");
        charset.SetReplacement("changed");

        Assert.That(clone.GetFallbackCharset(), Is.EqualTo("windows-1252"));
        Assert.That(clone.GetNewlinePolicy(), Is.EqualTo(EmailTextNewlinePolicy.NormalizeToLf));
        Assert.That(clone.GetCharsetOptions().GetReplacement(), Is.EqualTo("[bad]"));

        EmailTextDecodeResult result = new EmailTextDecoder().DecodeWithInputAndDeclaredCharset(new byte[] { 0xff }, "utf-8");
        byte[] undecodable = result.GetUndecodableBytes();
        undecodable[0] = 0;
        EmailIssueList issues = result.GetIssues();
        issues.Clear();
        Assert.That(result.GetUndecodableBytes(), Is.EqualTo(new byte[] { 0xff }));
        Assert.That(result.GetIssues().GetCount(), Is.EqualTo(1));

        Assert.Throws<ArgumentNullException>(() => new EmailTextDecoder().DecodeWithEntity((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailTextDecoder().DecodeWithInputAndDeclaredCharsetAndOptions(new byte[0], "utf-8", null!));
        Assert.Throws<ArgumentException>(() => new EmailTextDecoder().DecodeWithEntity(new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 1 })));
        Assert.Throws<ArgumentNullException>(() => options.SetFallbackCharset(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDecodedBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetNewlinePolicy((EmailTextNewlinePolicy)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetUnicodeNormalization((EmailTextUnicodeNormalization)99));
        Assert.Throws<ArgumentNullException>(() => options.SetCharsetOptions(null!));
    }

    static MimeEntity ParseEntity(string headers, byte[] body)
    {
        byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
        byte[] source = new byte[headerBytes.Length + body.Length];
        Array.Copy(headerBytes, source, headerBytes.Length);
        Array.Copy(body, 0, source, headerBytes.Length, body.Length);
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(source);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }
}
