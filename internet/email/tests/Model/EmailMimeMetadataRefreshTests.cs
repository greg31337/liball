using System.Text;
using email;

namespace email.Tests;

public class EmailMimeMetadataRefreshTests
{
    [Test]
    public void CleanRefreshPreservesReferencesSourceRangesAndDirtyState()
    {
        const string source = "Content-Type: text/plain; charset=utf-8\r\nContent-Disposition: inline\r\nContent-Transfer-Encoding: 8bit\r\nContent-ID: <one@example.test>\r\nContent-Location: cid:one\r\nContent-Language: en, fr\r\nContent-Description: Example\r\n\r\nbody";
        MimeEntity entity = Parse(source);
        EmailMediaType mediaType = entity.GetMediaType()!;
        EmailDisposition disposition = entity.GetDisposition()!;
        EmailMessageId contentId = entity.GetContentId()!;
        EmailContentLanguage language = entity.GetContentLanguage()!;
        EmailRange range = entity.GetSourceRange()!;

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.HasChanges(), Is.False);
            Assert.That(result.HasConflicts(), Is.False);
            Assert.That(entity.GetMediaType(), Is.SameAs(mediaType));
            Assert.That(entity.GetDisposition(), Is.SameAs(disposition));
            Assert.That(entity.GetContentId(), Is.SameAs(contentId));
            Assert.That(entity.GetContentLanguage(), Is.SameAs(language));
            Assert.That(entity.GetSourceRange(), Is.SameAs(range));
            Assert.That(entity.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
        });
    }

    [Test]
    public void RefreshUpdatesEveryDerivedContentHeaderAndSpecializedKind()
    {
        const string source = "Content-Type: text/plain\r\nContent-Disposition: inline\r\nContent-Transfer-Encoding: 7bit\r\nContent-ID: <old@example.test>\r\nContent-Location: cid:old\r\nContent-Language: en\r\nContent-Description: Old\r\n\r\nbody";
        MimeEntity entity = Parse(source);
        EmailHeaders headers = entity.GetHeaders()!;
        headers.GetFirst("Content-Type")!.SetValue("text/calendar; method=REQUEST");
        headers.GetFirst("Content-Disposition")!.SetValue("attachment; filename=invite.ics");
        headers.GetFirst("Content-Transfer-Encoding")!.SetValue("BaSe64");
        headers.GetFirst("Content-ID")!.SetValue("<new@example.test>");
        headers.GetFirst("Content-Location")!.SetValue("https://example.test/invite.ics");
        headers.GetFirst("Content-Language")!.SetValue("fr, de");
        headers.GetFirst("Content-Description")!.SetValue("=?utf-8?Q?New_invitation?=");

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.DidKindChange(), Is.True);
            Assert.That(result.GetPreviousKind(), Is.EqualTo(MimeEntityKind.Leaf));
            Assert.That(result.GetCurrentKind(), Is.EqualTo(MimeEntityKind.Calendar));
            Assert.That(result.GetChangedValueCount(), Is.GreaterThanOrEqualTo(8));
            Assert.That(entity.GetMediaType()!.IsType("text", "calendar"), Is.True);
            Assert.That(entity.GetDisposition()!.IsAttachment(), Is.True);
            Assert.That(entity.GetTransferEncodingName(), Is.EqualTo("BaSe64"));
            Assert.That(entity.GetContentId()!.GetIdLeft(), Is.EqualTo("new"));
            Assert.That(entity.GetContentLocation(), Is.EqualTo("https://example.test/invite.ics"));
            Assert.That(entity.GetContentLanguage()!.GetAll(), Is.EqualTo(new[] { "fr", "de" }));
            Assert.That(entity.GetContentDescription(), Is.EqualTo("New invitation"));
            Assert.That(Text(entity.GetHeaders()!.GetFirst("Content-Type")!.GetUnfoldedValue()), Is.EqualTo("text/calendar; method=REQUEST"));
            Assert.That(entity.IsDirty(), Is.True);
        });
    }

    [Test]
    public void RemovingContentHeadersRefreshesDefaultsAndClearsOptionalMetadata()
    {
        MimeEntity entity = Parse("Content-Type: application/octet-stream\r\nContent-Disposition: attachment\r\nContent-Transfer-Encoding: base64\r\nContent-ID: <one@example.test>\r\nContent-Location: cid:one\r\nContent-Language: en\r\nContent-Description: One\r\n\r\nYm9keQ==");
        EmailHeaders headers = entity.GetHeaders()!;
        string[] names = { "Content-Type", "Content-Disposition", "Content-Transfer-Encoding", "Content-ID", "Content-Location", "Content-Language", "Content-Description" };
        foreach (string name in names) headers.RemoveAll(name);

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(entity.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(entity.GetMediaType()!.WasDefaulted(), Is.True);
        Assert.That(entity.HasDisposition(), Is.False);
        Assert.That(entity.WasTransferEncodingDefaulted(), Is.True);
        Assert.That(entity.HasContentId(), Is.False);
        Assert.That(entity.HasContentLocation(), Is.False);
        Assert.That(entity.HasContentLanguage(), Is.False);
        Assert.That(entity.HasContentDescription(), Is.False);
    }

    [Test]
    public void UnknownValuesAndRawHeadersRemainLossless()
    {
        MimeEntity entity = Parse("Content-Type: application/octet-stream\r\nContent-Transfer-Encoding: 7bit\r\n\r\npayload");
        EmailHeader contentType = entity.GetHeaders()!.GetFirst("Content-Type")!;
        EmailHeader transfer = entity.GetHeaders()!.GetFirst("Content-Transfer-Encoding")!;
        contentType.SetValue("X-World/X-Artifact; X-Mode=Opaque");
        transfer.SetValue("X-RoT13");

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(entity.GetMediaType()!.GetTopLevelType(), Is.EqualTo("X-World"));
        Assert.That(entity.GetMediaType()!.GetSubtype(), Is.EqualTo("X-Artifact"));
        Assert.That(entity.GetTransferEncodingName(), Is.EqualTo("X-RoT13"));
        Assert.That(entity.HasKnownTransferEncoding(), Is.False);
        Assert.That(Text(contentType.GetUnfoldedValue()), Is.EqualTo("X-World/X-Artifact; X-Mode=Opaque"));
        Assert.That(Text(transfer.GetUnfoldedValue()), Is.EqualTo("X-RoT13"));
    }

    [Test]
    public void IncompatibleKindChangeReportsConflictAndRetainsConsistentDerivedType()
    {
        MimeEntity entity = Parse("Content-Type: text/plain\r\nContent-Description: old\r\n\r\nbody");
        entity.GetHeaders()!.GetFirst("Content-Type")!.SetValue("multipart/mixed; boundary=x");
        entity.GetHeaders()!.GetFirst("Content-Description")!.SetValue("new");

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasConflicts(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.MimeMetadataConflict), Is.True);
        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(entity.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(entity.GetContentDescription(), Is.EqualTo("new"));
        Assert.That(Text(entity.GetHeaders()!.GetFirst("Content-Type")!.GetUnfoldedValue()), Does.StartWith("multipart/mixed"));
    }

    [Test]
    public void DuplicateSingletonsReportModeSensitiveConflictsAndUseFirstValue()
    {
        MimeEntity tolerantEntity = Parse("Content-Type: text/plain\r\nContent-ID: <first@example.test>\r\n\r\nbody");
        tolerantEntity.GetHeaders()!.Add(Header("Content-ID", "<second@example.test>", EmailHeaderValueKind.ContentId));
        EmailMimeMetadataRefreshResult tolerant = tolerantEntity.RefreshMetadata();

        MimeEntity strictEntity = Parse("Content-Type: text/plain\r\nContent-ID: <first@example.test>\r\n\r\nbody");
        strictEntity.GetHeaders()!.Add(Header("Content-ID", "<second@example.test>", EmailHeaderValueKind.ContentId));
        EmailParseOptions strictOptions = new EmailParseOptions();
        strictOptions.SetMode(EmailParseMode.Strict);
        EmailMimeMetadataRefreshResult strict = strictEntity.RefreshMetadataWithOptions(strictOptions);

        Assert.That(tolerant.HasConflicts(), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerantEntity.GetContentId()!.GetIdLeft(), Is.EqualTo("first"));
        Assert.That(strict.HasConflicts(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strictEntity.GetContentId()!.GetIdLeft(), Is.EqualTo("first"));
    }

    [Test]
    public void RefreshCreatesAndClearsPartialMessageMetadata()
    {
        MimeEntity entity = Parse("Content-Type: text/plain\r\n\r\nfragment");
        entity.GetHeaders()!.GetFirst("Content-Type")!.SetValue("message/partial; id=series; number=2; total=3");

        EmailMimeMetadataRefreshResult partial = entity.RefreshMetadata();

        Assert.That(partial.IsSuccess(), Is.True);
        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.PartialMessage));
        Assert.That(entity.GetPartialMessageMetadata()!.GetFragmentId(), Is.EqualTo("series"));
        Assert.That(entity.GetPartialMessageMetadata()!.GetFragmentNumber(), Is.EqualTo(2));
        Assert.That(entity.GetPartialMessageMetadata()!.GetTotalFragments(), Is.EqualTo(3));

        entity.GetHeaders()!.GetFirst("Content-Type")!.SetValue("application/ms-tnef");
        EmailMimeMetadataRefreshResult tnef = entity.RefreshMetadata();
        Assert.That(tnef.IsSuccess(), Is.True);
        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.Tnef));
        Assert.That(entity.HasPartialMessageMetadata(), Is.False);
    }

    [Test]
    public void RefreshBuildsExternalBodyMetadataFromRetainedRawContent()
    {
        MimeEntity entity = Parse("Content-Type: text/plain\r\n\r\nContent-ID: <remote@example.test>\r\n\r\nphantom");
        entity.GetHeaders()!.GetFirst("Content-Type")!.SetValue("message/external-body; access-type=url; url=\"https://example.test/object\"");

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.ExternalBody));
        Assert.That(entity.GetExternalBodyMetadata()!.GetNormalizedAccessType(), Is.EqualTo("url"));
        Assert.That(entity.GetExternalBodyMetadata()!.GetUrl(), Is.EqualTo("https://example.test/object"));
        Assert.That(entity.GetExternalBodyMetadata()!.GetReferencedHeaders().Contains("Content-ID"), Is.True);
        Assert.That(entity.GetExternalBodyMetadata()!.HasPhantomBodyRange(), Is.True);
    }

    [Test]
    public void CompatibleMultipartSpecializationUpdatesKindWithoutRebuildingChildren()
    {
        const string source = "Content-Type: multipart/mixed; boundary=x\r\n\r\n--x\r\nContent-Type: text/plain\r\n\r\none\r\n--x\r\nContent-Type: application/octet-stream\r\n\r\ntwo\r\n--x--\r\n";
        MimeEntity entity = Parse(source);
        MimeEntity first = entity.GetChildAt(0);
        MimeEntity second = entity.GetChildAt(1);
        entity.GetHeaders()!.GetFirst("Content-Type")!.SetValue("multipart/signed; boundary=x; protocol=application/pgp-signature");

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.Signed));
        Assert.That(entity.GetChildAt(0), Is.SameAs(first));
        Assert.That(entity.GetChildAt(1), Is.SameAs(second));
    }

    [Test]
    public void TransferEncodingConflictDoesNotDiscardDecodedOnlyContent()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader>
        {
            Header("Content-Type", "text/plain", EmailHeaderValueKind.MediaType),
            Header("Content-Transfer-Encoding", "base64", EmailHeaderValueKind.ContentTransferEncoding)
        });
        EmailContent decoded = new EmailContentOperations().CreateMemoryCopy(Bytes("decoded"), false);
        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, headers, ParseMediaType("text/plain"), null, "base64", null, null, null, null, null, decoded, new List<MimeEntity>(), null, null, null, null, new EmailIssueList());
        headers.GetFirst("Content-Transfer-Encoding")!.SetValue("quoted-printable");

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.That(result.HasConflicts(), Is.True);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(entity.GetTransferEncodingName(), Is.EqualTo("base64"));
        Assert.That(entity.GetDecodedContent(), Is.SameAs(decoded));
    }

    [Test]
    public void TransferEncodingRefreshInvalidatesStaleDecodedContentWhenRawContentRemains()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader>
        {
            Header("Content-Type", "text/plain", EmailHeaderValueKind.MediaType),
            Header("Content-Transfer-Encoding", "7bit", EmailHeaderValueKind.ContentTransferEncoding)
        });
        EmailContent raw = new EmailContentOperations().CreateMemoryCopy(Bytes("raw"), false);
        EmailContent decoded = new EmailContentOperations().CreateMemoryCopy(Bytes("decoded"), false);
        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, headers, ParseMediaType("text/plain"), null, "7bit", null, null, null, null, raw, decoded, new List<MimeEntity>(), null, null, null, null, new EmailIssueList());
        headers.GetFirst("Content-Transfer-Encoding")!.SetValue("base64");

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadata();

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(entity.GetTransferEncodingName(), Is.EqualTo("base64"));
        Assert.That(entity.GetRawContent(), Is.SameAs(raw));
        Assert.That(entity.HasDecodedContent(), Is.False);
        Assert.That(entity.HasBodyIngestMetadata(), Is.False);
    }

    [Test]
    public void MultipartDigestChildDefaultRemainsStableDuringRefresh()
    {
        const string source = "Content-Type: multipart/digest; boundary=x\r\n\r\n--x\r\n\r\nFrom: a@example.test\r\n\r\ninside\r\n--x--\r\n";
        MimeEntity digest = Parse(source);
        MimeEntity child = digest.GetChildAt(0);
        EmailMediaType original = child.GetMediaType()!;

        EmailMimeMetadataRefreshResult result = child.RefreshMetadata();

        Assert.That(result.HasChanges(), Is.False);
        Assert.That(child.GetKind(), Is.EqualTo(MimeEntityKind.Message));
        Assert.That(child.GetMediaType(), Is.SameAs(original));
        Assert.That(child.IsDirty(), Is.False);
    }

    [Test]
    public void RefreshValidatesArgumentsAndConfiguredLimits()
    {
        MimeEntity entity = Parse("Content-Type: text/plain\r\n\r\nbody");
        Assert.Throws<ArgumentNullException>(() => entity.RefreshMetadataWithOptions(null!));

        entity.GetHeaders()!.GetFirst("Content-Type")!.SetValue("application/octet-stream; name=long-value");
        EmailParseOptions options = new EmailParseOptions();
        EmailLimits limits = options.GetLimits();
        EmailParsingLimits parsing = limits.GetParsingLimits();
        parsing.SetMaxLineBytes(20);
        limits.SetParsingLimits(parsing);
        options.SetLimits(limits);

        EmailMimeMetadataRefreshResult result = entity.RefreshMetadataWithOptions(options);
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().GetCount(), Is.GreaterThan(0));
    }

    static MimeEntity Parse(string source) => new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source)).GetEntity()!;
    static EmailHeader Header(string name, string value, EmailHeaderValueKind kind) => new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Bytes(value), kind);
    static EmailMediaType ParseMediaType(string value) => new EmailMediaTypeParser().ParseWithInputAndMode(Bytes(value), EmailStructuredValueParseMode.Strict).GetValue();
    static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);
    static string Text(byte[] value) => Encoding.UTF8.GetString(value);
}
