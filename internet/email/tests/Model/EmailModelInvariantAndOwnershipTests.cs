using System.Text;
using email;

namespace email.Tests;

public class EmailModelInvariantAndOwnershipTests
{
    [Test]
    public void FailedFixedArityMutationsLeaveTheTreeAndCandidateUnchanged()
    {
        MimeEntity first = Leaf("text", "plain", Borrowed("first"));
        MimeEntity second = Leaf("application", "pgp-signature", Borrowed("signature"));
        MimeEntity signed = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Signed, new EmailHeadersOperations().CreateWith(), Media("multipart", "signed"), null, new List<MimeEntity> { first, second }, null);
        MimeEntity candidate = Leaf("text", "html", Borrowed("candidate"));

        Assert.Throws<InvalidOperationException>(() => signed.AddChild(candidate));
        Assert.Throws<InvalidOperationException>(() => signed.RemoveChildAt(0));

        Assert.That(signed.GetChildren(), Is.EqualTo(new[] { first, second }));
        Assert.That(first.GetParent(), Is.SameAs(signed));
        Assert.That(second.GetParent(), Is.SameAs(signed));
        Assert.That(candidate.HasParent(), Is.False);
    }

    [Test]
    public void MutatingAHeaderCloneInvalidatesOnlyItsCaches()
    {
        EmailHeader original = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Content-Type", Bytes(" text/plain; charset=utf-8"), EmailHeaderValueKind.MediaType);
        object originalStructured = original.GetStructuredValue()!;
        string originalDecoded = original.GetDecodedValue()!;
        EmailHeader clone = original.Clone();

        clone.SetValue(" application/x-new");

        Assert.That(clone.HasDecodedValueCache(), Is.False);
        Assert.That(clone.HasStructuredValueCache(), Is.False);
        Assert.That(original.HasDecodedValueCache(), Is.True);
        Assert.That(original.HasStructuredValueCache(), Is.True);
        Assert.That(original.GetDecodedValue(), Is.SameAs(originalDecoded));
        Assert.That(original.GetStructuredValue(), Is.SameAs(originalStructured));
        Assert.That(((EmailMediaType)clone.GetStructuredValue()!)!.IsType("application", "x-new"), Is.True);
    }

    [Test]
    public void CloneAndDetachPreserveUnknownModelValuesAndExactHeaderBytes()
    {
        EmailSourcePosition position = new EmailSourcePosition(12, 2, 3);
        EmailParameter first = new EmailParameter("X-Flag", Bytes("\"A;B\""), Bytes("A;B"), true, Bytes("X-Flag=\"A;B\""), position);
        EmailParameter second = new EmailParameter("x-flag", Bytes("opaque"), Bytes("opaque"), false, Bytes("x-flag=opaque"), position);
        EmailParameterList parameters = new EmailParameterList(new List<EmailParameter> { first, second }, new List<EmailRfc2231DecodedParameter>());
        EmailMediaType mediaType = new EmailMediaType("X-World", "X-Artifact", parameters, Bytes("X-World/X-Artifact; X-Flag=\"A;B\"; x-flag=opaque"), position, false);
        byte[] exactField = Bytes("X-Opaque :\tfolded\r\n value\r\n");
        EmailHeader unknown = new EmailHeaderOperations().CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssuesAndExactOriginalRawFieldBytes(Bytes("X-Opaque"), Bytes("\tfolded\r\n value"), EmailLineEnding.CrLf, new EmailRange(10, exactField.Length), position, EmailHeaderValueKind.Unknown, EmailStructuredValueParseMode.Tolerant, new EmailIssueList(), exactField);
        MimeEntity original = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { unknown }), mediaType, null, "X-ROT13", null, null, null, null, Borrowed("payload"), null, new List<MimeEntity>(), null, null, null, new EmailRange(10, 80), new EmailIssueList());

        MimeEntity clone = original.Clone();
        clone.DetachSource(100);

        Assert.That(clone.GetMediaType()!.GetTopLevelType(), Is.EqualTo("X-World"));
        Assert.That(clone.GetMediaType()!.GetSubtype(), Is.EqualTo("X-Artifact"));
        Assert.That(clone.GetMediaType()!.IsExtensionType(), Is.True);
        Assert.That(Text(clone.GetMediaType()!.GetRawBytes()), Is.EqualTo("X-World/X-Artifact; X-Flag=\"A;B\"; x-flag=opaque"));
        Assert.That(clone.GetMediaType()!.GetParameters().HasDuplicate("x-flag"), Is.True);
        Assert.That(clone.GetTransferEncodingName(), Is.EqualTo("X-ROT13"));
        Assert.That(clone.HasKnownTransferEncoding(), Is.False);
        Assert.That(Text(clone.GetHeaders()!.GetAt(0).GetRawFieldBytes()), Is.EqualTo(Text(exactField)));
        Assert.That(clone.GetHeaders()!.GetAt(0).GetStructuredValue(), Is.Null);
        Assert.That(clone.GetHeaders()!.GetAt(0).HasSourceRange(), Is.False);
    }

    [Test]
    public void DetachingACloneCopiesOwnedMemoryAndItsOwnership()
    {
        byte[] sensitive = Bytes("owned secret");
        EmailContent owned = new EmailContentOperations().CreateMemory(sensitive, EmailContentOwnership.Owned, true);
        MimeEntity original = SourceLeaf(owned);
        MimeEntity clone = original.Clone();

        clone.DetachSource(100);
        owned.Close();

        Assert.That(sensitive, Is.EqualTo(new byte[sensitive.Length]));
        Assert.That(clone.GetContent(), Is.Not.SameAs(owned));
        Assert.That(clone.GetContent().GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
        Assert.That(Text(clone.GetContent().Materialize(100)), Is.EqualTo("owned secret"));
        Assert.That(clone.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactOnly));
    }

    [Test]
    public void FailedDetachLeavesAllLocalContentAndSourceMetadataAttached()
    {
        EmailContent raw = Borrowed("raw");
        EmailContent decoded = Borrowed("decoded value");
        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media("application", "octet-stream"), null, null, null, null, null, null, raw, decoded, new List<MimeEntity>(), null, null, null, new EmailRange(0, 30), new EmailIssueList());

        Assert.Throws<EmailLimitException>(() => entity.DetachSource(4));

        Assert.That(entity.GetRawContent(), Is.SameAs(raw));
        Assert.That(entity.GetDecodedContent(), Is.SameAs(decoded));
        Assert.That(entity.HasSourceRange(), Is.True);
        Assert.That(entity.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    static MimeEntity SourceLeaf(EmailContent content)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media("application", "octet-stream"), null, null, null, null, null, null, content, null, new List<MimeEntity>(), null, null, null, new EmailRange(0, 30), new EmailIssueList());
    }

    static MimeEntity Leaf(string type, string subtype, EmailContent content)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media(type, subtype), content, new List<MimeEntity>(), null);
    }

    static EmailMediaType Media(string type, string subtype)
    {
        return new EmailMediaType(type, subtype, new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>()), Bytes(type + "/" + subtype), new EmailSourcePosition(0, 1, 1), false);
    }

    static EmailContent Borrowed(string value)
    {
        return new EmailContentOperations().CreateMemory(Bytes(value), EmailContentOwnership.Borrowed, false);
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
