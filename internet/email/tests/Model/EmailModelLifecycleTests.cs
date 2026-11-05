using System.Text;
using email;

namespace email.Tests;

public class EmailModelLifecycleTests
{
    [Test]
    public void HeaderAndCollectionClonesPreserveStateButMutateIndependently()
    {
        EmailHeader original = PreservedHeader("Subject", " old", 0, 14);
        original.GetDecodedValue();
        original.SetValue(" new");
        original.GetDecodedValue();
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { original });

        EmailHeaders clone = headers.Clone();
        EmailHeader copied = clone.GetAt(0);

        Assert.That(clone, Is.Not.SameAs(headers));
        Assert.That(copied, Is.Not.SameAs(original));
        Assert.That(copied.IsValueDirty(), Is.True);
        Assert.That(copied.HasDecodedValueCache(), Is.True);
        Assert.That(Text(copied.GetRawValue()), Is.EqualTo(" new"));
        Assert.That(Text(copied.GetOriginalRawValue()), Is.EqualTo(" old"));
        copied.ResetToOriginal();
        Assert.That(Text(copied.GetRawValue()), Is.EqualTo(" old"));
        Assert.That(Text(original.GetRawValue()), Is.EqualTo(" new"));
    }

    [Test]
    public void EntityCloneDuplicatesTreeNodesHeadersAndIssuesWhileSharingLazyContent()
    {
        EmailContent content = Borrowed("body");
        MimeEntity child = SourceLeaf(content, 30, 40);
        child.GetHeaders()!.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("X-Test", Bytes(" value")));
        MimeEntity root = SourceMultipart(new List<MimeEntity> { child }, 20, 100);
        root.AddIssue(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescription(EmailIssueSeverity.Warning, EmailIssueCode.MimeStructureMalformed, "issue"));

        MimeEntity clone = root.Clone();

        Assert.That(clone, Is.Not.SameAs(root));
        Assert.That(clone.GetHeaders(), Is.Not.SameAs(root.GetHeaders()));
        Assert.That(clone.GetChildAt(0), Is.Not.SameAs(child));
        Assert.That(clone.GetChildAt(0).GetParent(), Is.SameAs(clone));
        Assert.That(clone.GetChildAt(0).GetContent(), Is.SameAs(content));
        Assert.That(clone.GetOwnIssues().GetCount(), Is.EqualTo(1));

        clone.GetChildAt(0).GetHeaders()!.GetFirst("X-Test")!.SetValue(" changed");
        Assert.That(Text(child.GetHeaders()!.GetFirst("X-Test")!.GetRawValue()), Is.EqualTo(" value"));
    }

    [Test]
    public void MessageCloneDuplicatesModelAndPreservesDirtyState()
    {
        EmailMessage original = SourceMessage(SourceLeaf(Borrowed("body"), 22, 78));
        original.GetHeaders()!.GetAt(0).SetValue(" changed");
        original.GetEnvelope()!.AddRecipient(Address("copy", "example.test"));

        EmailMessage clone = original.Clone();

        Assert.That(clone, Is.Not.SameAs(original));
        Assert.That(clone.GetHeaders(), Is.Not.SameAs(original.GetHeaders()));
        Assert.That(clone.GetHeaders().GetAt(0), Is.Not.SameAs(original.GetHeaders()!.GetAt(0)));
        Assert.That(clone.GetEnvelope(), Is.Not.SameAs(original.GetEnvelope()));
        Assert.That(clone.GetBody(), Is.Not.SameAs(original.GetBody()));
        Assert.That(clone.GetDirtyState(), Is.EqualTo(original.GetDirtyState()));

        clone.GetHeaders().GetAt(0).SetValue(" clone only");
        clone.GetEnvelope().ClearRecipients();
        Assert.That(Text(original.GetHeaders()!.GetAt(0).GetRawValue()), Is.EqualTo(" changed"));
        Assert.That(original.GetEnvelope()!.GetRecipientCount(), Is.EqualTo(1));
    }

    [Test]
    public void ContentMaterializationIsLazyCachedOwnedAndNonDirty()
    {
        CountingReadStream stream = new CountingReadStream(Bytes("streamed content"));
        EmailContent source = new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed);
        MimeEntity entity = SourceLeaf(source, 10, 30);

        Assert.That(stream.GetReadCount(), Is.Zero);
        Assert.That(entity.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));

        EmailContent first = entity.MaterializeRawContent(100);
        int reads = stream.GetReadCount();
        EmailContent second = entity.MaterializeRawContent(100);

        Assert.That(reads, Is.GreaterThan(0));
        Assert.That(stream.GetReadCount(), Is.EqualTo(reads));
        Assert.That(first, Is.SameAs(second));
        Assert.That(first.GetKind(), Is.EqualTo(EmailContentKind.Memory));
        Assert.That(first.GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
        Assert.That(Text(first.Materialize(100)), Is.EqualTo("streamed content"));
        Assert.That(entity.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void FailedMaterializationDoesNotReplaceOrDirtyTheContent()
    {
        EmailContent source = Borrowed("too large");
        MimeEntity entity = SourceLeaf(source, 0, 20);

        Assert.Throws<EmailLimitException>(() => entity.MaterializeRawContent(2));

        Assert.That(entity.GetRawContent(), Is.SameAs(source));
        Assert.That(entity.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void ContentReplacementPropagatesAndKeepsUnaffectedSiblingRangesClean()
    {
        MimeEntity first = SourceLeaf(Borrowed("first"), 30, 40);
        MimeEntity second = SourceLeaf(Borrowed("second"), 80, 50);
        MimeEntity root = SourceMultipart(new List<MimeEntity> { first, second }, 22, 178);
        EmailMessage message = SourceMessage(root);

        second.ReplaceContent(Borrowed("replacement"));

        Assert.That(second.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        Assert.That(root.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactAndSemantic));
        Assert.That(message.GetCleanRanges(), Has.Count.EqualTo(2));
        Assert.That(message.GetCleanRanges()[0].EqualsRange(new EmailRange(0, 80)), Is.True);
        Assert.That(message.GetCleanRanges()[1].EqualsRange(new EmailRange(130, 70)), Is.True);
        Assert.That(message.IsRangeClean(new EmailRange(30, 40)), Is.True);
        Assert.That(message.IsRangeClean(new EmailRange(80, 50)), Is.False);
    }

    [Test]
    public void DescendantHeaderMutationPropagatesWithoutInvalidatingSiblingBytes()
    {
        MimeEntity first = SourceLeafWithHeader(Borrowed("first"), 30, 40);
        MimeEntity second = SourceLeaf(Borrowed("second"), 80, 50);
        MimeEntity root = SourceMultipart(new List<MimeEntity> { first, second }, 22, 178);
        EmailMessage message = SourceMessage(root);

        first.GetHeaders()!.GetAt(0).SetValue(" changed");

        Assert.That(first.IsExactDirty(), Is.True);
        Assert.That(root.IsExactDirty(), Is.True);
        Assert.That(message.IsExactDirty(), Is.True);
        Assert.That(message.IsRangeClean(new EmailRange(30, 40)), Is.False);
        Assert.That(message.IsRangeClean(new EmailRange(80, 50)), Is.True);
    }

    [Test]
    public void StructuralChildMutationInvalidatesTheContainerRange()
    {
        MimeEntity child = SourceLeaf(Borrowed("first"), 30, 40);
        MimeEntity root = SourceMultipart(new List<MimeEntity> { child }, 22, 178);
        EmailMessage message = SourceMessage(root);

        root.AddChild(new EmailEntityFactory().CreateTextWithValue("new"));

        Assert.That(root.IsSemanticDirty(), Is.True);
        Assert.That(message.GetCleanRanges(), Has.Count.EqualTo(1));
        Assert.That(message.GetCleanRanges()[0].EqualsRange(new EmailRange(0, 22)), Is.True);
    }

    [Test]
    public void DetachSourceRecursivelyMaterializesAndClearsAllSourceMetadata()
    {
        byte[] borrowedBytes = Bytes("borrowed body");
        MimeEntity child = SourceLeaf(new EmailContentOperations().CreateMemory(borrowedBytes, EmailContentOwnership.Borrowed, false), 30, 40);
        MimeEntity root = SourceMultipart(new List<MimeEntity> { child }, 22, 178);
        EmailEnvelope envelope = new EmailEnvelopeOperations().CreateWithSenderAndRecipientsAndMboxFromLineAndMboxFromLineRange(null, new List<EmailAddress>(), Bytes("From sender@example.test\n"), new EmailRange(0, 10));
        EmailMessage message = SourceMessage(root, envelope);

        message.DetachSource(100);
        borrowedBytes[0] = (byte)'X';

        Assert.That(message.HasSourceInfo(), Is.False);
        Assert.That(message.GetEnvelope()!.HasMboxFromLineRange(), Is.False);
        Assert.That(message.GetHeaders()!.GetAt(0).HasSourceRange(), Is.False);
        Assert.That(root.HasSourceRange(), Is.False);
        Assert.That(child.HasSourceRange(), Is.False);
        Assert.That(child.GetContent()!.GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
        Assert.That(Text(child.GetContent()!.Materialize(100)), Is.EqualTo("borrowed body"));
        Assert.That(message.GetDirtyState(), Is.EqualTo(EmailDirtyState.ExactOnly));
        Assert.That(root.IsSemanticDirty(), Is.False);
        Assert.That(child.IsSemanticDirty(), Is.False);
    }

    [Test]
    public void CloneCanDetachFromSharedContentWithoutChangingOriginal()
    {
        byte[] bytes = Bytes("shared");
        MimeEntity original = SourceLeaf(new EmailContentOperations().CreateMemory(bytes, EmailContentOwnership.Borrowed, false), 0, 20);
        MimeEntity clone = original.Clone();

        clone.DetachSource(100);
        bytes[0] = (byte)'X';

        Assert.That(Text(original.GetContent()!.Materialize(100)), Is.EqualTo("Xhared"));
        Assert.That(Text(clone.GetContent()!.Materialize(100)), Is.EqualTo("shared"));
        Assert.That(clone.GetContent(), Is.Not.SameAs(original.GetContent()));
    }

    [Test]
    public void ReplacementRejectsUnsupportedKindsNullAndClosedContent()
    {
        MimeEntity multipart = SourceMultipart(new List<MimeEntity>(), 0, 20);
        MimeEntity nested = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), Media("message", "rfc822"), null, new List<MimeEntity>(), new EmailMessageOperations().CreateWith());
        MimeEntity leaf = SourceLeaf(Borrowed("body"), 0, 20);
        EmailContent closed = Borrowed("closed");
        closed.Close();

        Assert.Throws<InvalidOperationException>(() => multipart.ReplaceContent(Borrowed("bad")));
        Assert.Throws<InvalidOperationException>(() => nested.ReplaceRawContent(Borrowed("bad")));
        Assert.Throws<ArgumentNullException>(() => leaf.ReplaceContent(null!));
        Assert.Throws<ObjectDisposedException>(() => leaf.ReplaceContent(closed));
    }

    [Test]
    public void NestedMessageDirtyStatePropagatesThroughItsEntity()
    {
        EmailMessage nested = SourceMessage(SourceLeaf(Borrowed("nested"), 40, 30));
        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Message, new EmailHeadersOperations().CreateWith(), Media("message", "rfc822"), null, null, null, null, null, null, null, null, new List<MimeEntity>(), nested, null, null, new EmailRange(30, 80), new EmailIssueList());

        nested.GetHeaders()!.GetAt(0).SetValue(" changed");

        Assert.That(nested.IsSemanticDirty(), Is.True);
        Assert.That(entity.IsSemanticDirty(), Is.True);
        Assert.That(entity.IsExactDirty(), Is.True);
        Assert.That(entity.GetDirtySourceRanges()[0].EqualsRange(new EmailRange(30, 80)), Is.True);
    }

    static EmailMessage SourceMessage(MimeEntity body)
    {
        return SourceMessage(body, new EmailEnvelopeOperations().CreateWith());
    }

    static EmailMessage SourceMessage(MimeEntity body, EmailEnvelope envelope)
    {
        EmailHeader header = PreservedHeader("Subject", " old", 0, 14);
        EmailHeaders headers = new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { header });
        EmailContent source = new EmailContentOperations().CreateMemory(new byte[200], EmailContentOwnership.Borrowed, false);
        EmailSourceInfo info = new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(source, new EmailRange(0, 200), new EmailRange(0, 20), new EmailRange(20, 2), new EmailRange(22, 178), null, true);
        return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, body, envelope, info, new EmailIssueList());
    }

    static MimeEntity SourceLeaf(EmailContent content, long start, long length)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), Media("application", "octet-stream"), null, null, null, null, null, null, content, null, new List<MimeEntity>(), null, null, null, new EmailRange(start, length), new EmailIssueList());
    }

    static MimeEntity SourceLeafWithHeader(EmailContent content, long start, long length)
    {
        EmailHeader header = PreservedHeader("Content-Type", " text/plain", start, 26);
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWithPreservedHeaders(new List<EmailHeader> { header }), Media("text", "plain"), null, null, null, null, null, null, content, null, new List<MimeEntity>(), null, null, null, new EmailRange(start, length), new EmailIssueList());
    }

    static MimeEntity SourceMultipart(List<MimeEntity> children, long start, long length)
    {
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Multipart, new EmailHeadersOperations().CreateWith(), Media("multipart", "mixed"), null, null, null, null, null, null, null, null, children, null, null, null, new EmailRange(start, length), new EmailIssueList());
    }

    static EmailHeader PreservedHeader(string name, string value, long start, long length)
    {
        byte[] field = Bytes(name + ":" + value + "\r\n");
        return new EmailHeaderOperations().CreateWithOriginalNameBytesAndOriginalRawValueAndLineEndingAndSourceRangeAndValuePositionAndValueKindAndParseModeAndIssuesAndExactOriginalRawFieldBytes(Bytes(name), Bytes(value), EmailLineEnding.CrLf, new EmailRange(start, length), new EmailSourcePosition(start + name.Length + 1, 1, name.Length + 2), new EmailHeaderRegistry().GetValueKind(name), EmailStructuredValueParseMode.Tolerant, new EmailIssueList(), field);
    }

    static EmailMediaType Media(string type, string subtype)
    {
        return new EmailMediaType(type, subtype, new EmailParameterList(new List<EmailParameter>(), new List<EmailRfc2231DecodedParameter>()), new byte[0], new EmailSourcePosition(0, 1, 1), false);
    }

    static EmailAddress Address(string local, string domain)
    {
        return new EmailAddress(null, local, domain, Bytes(local + "@" + domain), Bytes(local), Bytes(domain), new EmailSourcePosition(0, 1, 1), false, false, false, new List<string>());
    }

    static EmailContent Borrowed(string value)
    {
        return new EmailContentOperations().CreateMemory(Bytes(value), EmailContentOwnership.Borrowed, false);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}

public class CountingReadStream : MemoryStream
{
    int readCount;

    public CountingReadStream(byte[] value) : base(value)
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
