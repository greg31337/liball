using System.Text;
using email;

namespace email.Tests;

public class EmailProcessingApiCompletionTests
{
    [Test]
    public void TraversalConvenienceOperationsPreserveBoundedSearchBehavior()
    {
        MimeEntity plain = new EmailEntityFactory().CreateTextWithValue("body");
        MimeEntity attachment = ParseEntity(
            "Content-Type: application/octet-stream\r\n"
            + "Content-Disposition: attachment; filename=report.bin\r\n"
            + "Content-ID: <asset@example.test>\r\n"
            + "Content-Location: files/report.bin\r\n\r\n"
            + "payload");
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { plain, attachment });
        EmailMessage message = MessageWithBody(root);
        EmailTraversal traversal = new EmailTraversal();
        EmailPath attachmentPath = new EmailPathOperations().CreateWithSegments(new List<int> { 1 });

        Assert.That(traversal.DepthFirstWithRootAndMaximumDepth(root, 1), Is.EqualTo(new[] { root, plain, attachment }));
        Assert.That(traversal.DepthFirstWithMessageAndMaximumDepth(message, 1), Is.EqualTo(new[] { root, plain, attachment }));
        Assert.That(traversal.DepthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(message, 1, 3), Is.EqualTo(new[] { root, plain, attachment }));
        Assert.That(traversal.BreadthFirstWithRootAndMaximumDepth(root, 1), Is.EqualTo(new[] { root, plain, attachment }));
        Assert.That(traversal.BreadthFirstWithMessageAndMaximumDepth(message, 1), Is.EqualTo(new[] { root, plain, attachment }));
        Assert.That(traversal.BreadthFirstWithMessageAndMaximumDepthAndMaximumEntityCount(message, 1, 3), Is.EqualTo(new[] { root, plain, attachment }));
        Assert.That(traversal.GetAtPathWithMessageAndPath(message, attachmentPath), Is.SameAs(attachment));
        Assert.That(traversal.GetPathWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(message, attachment, 1, 3)!.GetDisplayText(), Is.EqualTo("root/2"));
        Assert.That(traversal.GetParentWithMessageAndTargetAndMaximumDepthAndMaximumEntityCount(message, attachment, 1, 3), Is.SameAs(root));

        Assert.That(traversal.FindByMediaTypeWithMessageAndTopLevelTypeAndSubtypeAndMaximumDepthAndMaximumEntityCount(message, "application", "octet-stream", 1, 3), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByDispositionWithMessageAndDispositionTypeAndMaximumDepthAndMaximumEntityCount(message, "attachment", 1, 3), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByFilenameWithRootAndFilenameAndMaximumDepthAndMaximumEntityCount(root, "report.bin", 1, 3), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByFilenameWithMessageAndFilename(message, "report.bin"), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByFilenameWithMessageAndFilenameAndMaximumDepthAndMaximumEntityCount(message, "report.bin", 1, 3), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByContentIdWithRootAndContentIdAndMaximumDepthAndMaximumEntityCount(root, "asset@example.test", 1, 3), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByContentIdWithMessageAndContentIdAndMaximumDepthAndMaximumEntityCount(message, "<asset@EXAMPLE.TEST>", 1, 3), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByContentLocationWithRootAndContentLocationAndMaximumDepthAndMaximumEntityCount(root, "files/report.bin", 1, 3), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByContentLocationWithMessageAndContentLocationAndMaximumDepthAndMaximumEntityCount(message, "files/report.bin", 1, 3), Is.EqualTo(new[] { attachment }));
        Assert.That(traversal.FindByKindWithMessageAndKindAndMaximumDepthAndMaximumEntityCount(message, MimeEntityKind.Leaf, 1, 3), Is.EqualTo(new[] { plain, attachment }));
    }

    [Test]
    public void BodyAndAttachmentConvenienceOperationsRetainSelectionsAndRoles()
    {
        MimeEntity plain = new EmailEntityFactory().CreateTextWithValue("plain");
        MimeEntity html = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<p>html</p>", "html", "utf-8");
        MimeEntity alternative = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity> { plain, html });
        EmailMessage message = MessageWithBody(alternative);
        EmailBodySelector selector = new EmailBodySelector();

        Assert.That(selector.SelectWithMessageAndPreference(message, EmailBodyPreference.PlainText).GetBody(), Is.SameAs(plain));
        Assert.That(selector.SelectWithMessageAndMaximumDepthAndMaximumEntityCount(message, 1, 3).GetBody(), Is.SameAs(html));
        Assert.That(selector.SelectWithMessageAndPreferenceAndMaximumDepthAndMaximumEntityCount(message, EmailBodyPreference.PlainText, 1, 3).GetBody(), Is.SameAs(plain));
        Assert.That(selector.GetPlainTextWithRootAndMaximumDepthAndMaximumEntityCount(alternative, 1, 3), Is.SameAs(plain));
        Assert.That(selector.GetPlainTextWithMessageAndMaximumDepthAndMaximumEntityCount(message, 1, 3), Is.SameAs(plain));
        Assert.That(selector.GetHtmlWithRootAndMaximumDepthAndMaximumEntityCount(alternative, 1, 3), Is.SameAs(html));
        Assert.That(selector.GetHtmlWithMessageAndMaximumDepthAndMaximumEntityCount(message, 1, 3), Is.SameAs(html));
        Assert.That(selector.GetAlternativesWithMessageAndMaximumDepthAndMaximumEntityCount(message, 1, 3), Is.EqualTo(new[] { plain, html }));

        EmailAttachmentFinder finder = new EmailAttachmentFinder();
        Assert.That(finder.GetBodiesWithMessage(message).Select(view => view.GetEntity()), Is.EqualTo(new[] { plain, html }));
        Assert.That(finder.GetInlineResourcesWithRoot(alternative), Is.Empty);
        Assert.That(finder.GetNestedMessagesWithRoot(alternative), Is.Empty);
        Assert.That(finder.GetCryptographicPartsWithMessage(message), Is.Empty);
    }

    [Test]
    public void DecodedContentAndAttachmentOperationsProduceIdenticalBytesAndMetadata()
    {
        byte[] payload = Encoding.UTF8.GetBytes("decoded payload");
        EmailContent content = new EmailContentOperations().CreateMemoryCopy(payload, false);
        MimeEntity entity = ParseEntity(
            "Content-Type: text/plain; charset=utf-8\r\n"
            + "Content-Disposition: attachment; filename=\"../notes.txt\"\r\n"
            + "Content-ID: <notes@example.test>\r\n"
            + "Content-Location: files/notes.txt\r\n"
            + "Content-Transfer-Encoding: base64\r\n\r\n"
            + Convert.ToBase64String(payload));
        EmailDecodedContent decoded = new EmailDecodedContent();
        string contentPath = TemporaryPath("content");
        string entityPath = TemporaryPath("entity");
        string attachmentPath = TemporaryPath("attachment");

        try
        {
            using MemoryStream contentOutput = new MemoryStream();
            using MemoryStream entityOutput = new MemoryStream();
            Assert.That(decoded.CopyToWithContentAndOutput(content, contentOutput), Is.EqualTo(payload.LongLength));
            Assert.That(decoded.CopyToWithEntityAndOutput(entity, entityOutput), Is.EqualTo(payload.LongLength));
            Assert.That(contentOutput.ToArray(), Is.EqualTo(payload));
            Assert.That(entityOutput.ToArray(), Is.EqualTo(payload));
            Assert.That(decoded.SaveWithContentAndPath(content, contentPath), Is.EqualTo(payload.LongLength));
            Assert.That(decoded.SaveWithEntityAndPath(entity, entityPath), Is.EqualTo(payload.LongLength));
            Assert.That(File.ReadAllBytes(contentPath), Is.EqualTo(payload));
            Assert.That(File.ReadAllBytes(entityPath), Is.EqualTo(payload));
            Assert.That(decoded.MeasureLengthWithContent(content), Is.EqualTo(payload.LongLength));
            Assert.That(decoded.MeasureLengthWithContentAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(content, 100, "TestDecodedBytes", 100, payload.LongLength), Is.EqualTo(payload.LongLength));
            Assert.That(decoded.MeasureLengthWithEntity(entity), Is.EqualTo(payload.LongLength));
            Assert.That(decoded.ComputeHashWithContentAndAlgorithm(content, EmailHashAlgorithm.Sha256).GetLowercaseHex(), Is.EqualTo(decoded.ComputeHashWithEntityAndAlgorithm(entity, EmailHashAlgorithm.Sha256).GetLowercaseHex()));

            EmailAttachmentView view = new EmailAttachmentFinder().FindWithRoot(entity)[0];
            EmailFilenameOptions filenameOptions = new EmailFilenameOptions();
            Assert.That(view.HasDisposition(), Is.True);
            Assert.That(view.HasContentLocation(), Is.True);
            Assert.That(view.GetSafeFilenameWithOptions(filenameOptions), Is.EqualTo("notes.txt"));
            Assert.That(view.GetSafeFilenameResultWithOptions(filenameOptions).GetOriginalFilename(), Is.EqualTo("../notes.txt"));
            Assert.That(view.SuggestSafeFilenameWithOptions(new List<string> { "NOTES.TXT" }, filenameOptions), Is.EqualTo("notes (2).txt"));
            using MemoryStream attachmentOutput = new MemoryStream();
            Assert.That(view.CopyDecodedContentTo(attachmentOutput), Is.EqualTo(payload.LongLength));
            Assert.That(attachmentOutput.ToArray(), Is.EqualTo(payload));
            Assert.That(view.SaveDecodedContent(attachmentPath), Is.EqualTo(payload.LongLength));
            Assert.That(File.ReadAllBytes(attachmentPath), Is.EqualTo(payload));
            Assert.That(view.MeasureDecodedLength(), Is.EqualTo(payload.LongLength));
            Assert.That(view.ComputeDecodedHash(EmailHashAlgorithm.Sha256).GetLowercaseHex(), Is.EqualTo(decoded.ComputeHashWithContentAndAlgorithm(content, EmailHashAlgorithm.Sha256).GetLowercaseHex()));
            Assert.That(view.InspectContentType().HasFilename(), Is.True);
            Assert.That(view.ProcessTextWithOptions(new EmailTextDecodeOptions(), new EmailFlowedTextOptions()).GetText(), Is.EqualTo("decoded payload"));
        }
        finally
        {
            content.Close();
            DeleteIfPresent(contentPath);
            DeleteIfPresent(entityPath);
            DeleteIfPresent(attachmentPath);
        }
    }

    [Test]
    public void TextAndHtmlConvenienceOperationsExposeBehavioralResults()
    {
        EmailTextAnalysisOptions analysisOptions = new EmailTextAnalysisOptions();
        analysisOptions.SetMaximumSnippetCharacters(12);
        EmailTextProcessor processor = new EmailTextProcessor();
        Assert.That(processor.CreateSnippetWithValueAndOptions("one two three four", analysisOptions), Is.EqualTo("one two thr…"));

        EmailTextAnalysisResult analysis = processor.AnalyzeWithValue("visible\r\n-- \r\nsigner");
        Assert.That(analysis.GetSignatureOffset(), Is.EqualTo(9));
        EmailTextSearchResult search = processor.SearchWithRootAndQuery(new EmailEntityFactory().CreateTextWithValue("Find the needle"), "needle");
        Assert.That(search.GetQuery(), Is.EqualTo("needle"));
        Assert.That(search.IsSuccess(), Is.True);
        Assert.That(search.GetMatch(0).GetEntity().GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(search.GetMatch(0).GetCharacterOffset(), Is.EqualTo(9));
        Assert.That(search.GetMatch(0).GetCharacterLength(), Is.EqualTo(6));

        EmailFlowedTextOptions flowedOptions = new EmailFlowedTextOptions();
        flowedOptions.SetMaximumLineLength(20);
        EmailFlowedText flowed = new EmailFlowedText();
        EmailFlowedTextDecodeResult decodedFlowed = flowed.DecodeResultWithValueAndOptions("one \r\ntwo\r\n", flowedOptions);
        Assert.That(decodedFlowed.GetText(), Is.EqualTo("onetwo\r\n"));
        Assert.That(flowed.DecodeWithValueAndDeleteSpace(flowed.EncodeWithValueAndDeleteSpace("one two three four\r\n", true), true), Is.EqualTo("one two three four\r\n"));
        string structuredEncoding = flowed.EncodeWithValueAndOptionsUsingEmailFlowedTextDecodeResultAndEmailFlowedTextOptions(decodedFlowed, flowedOptions);
        Assert.That(flowed.DecodeWithValueAndOptions(structuredEncoding, flowedOptions), Is.EqualTo(decodedFlowed.GetText()));
        Assert.That(processor.DecodeFlowed("one \r\ntwo\r\n", true), Is.EqualTo("onetwo\r\n"));
        Assert.That(processor.DecodeFlowed(processor.EncodeFlowedWithValue("one two\r\n"), true), Is.EqualTo("one two\r\n"));
        Assert.That(processor.DecodeFlowed(processor.EncodeFlowedWithValueAndOptions("one two\r\n", flowedOptions), true), Is.EqualTo("one two\r\n"));

        MimeEntity text = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("café", "plain", "utf-8");
        EmailPath path = new EmailPathOperations().CreateRoot();
        Assert.That(new EmailTextDecoder().DecodeWithEntityAndOptionsAndPath(text, new EmailTextDecodeOptions(), path).GetText(), Is.EqualTo("café"));

        EmailHtmlLimits limits = new EmailHtmlLimits();
        Assert.That(new EmailHtmlEntityDecoder().DecodeResultWithValueAndLimits("Tom &amp; Jerry", limits).GetText(), Is.EqualTo("Tom & Jerry"));
        MimeEntity html = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<p>x</p><img src='data:image/png;base64,AQID'>", "html", "utf-8");
        EmailHtmlDataUriResult extracted = new EmailHtmlProcessor().ExtractDataUrisWithEntityAndTextOptionsAndHtmlOptions(html, new EmailTextDecodeOptions(), new EmailHtmlDataUriOptions());
        Assert.That(extracted.GetRelatedPartCount(), Is.EqualTo(1));
        Assert.That(extracted.GetTokenizationResult().GetTokenCount(), Is.GreaterThan(0));
        EmailHtmlDataUriExtraction extraction = extracted.GetExtraction(0);
        Assert.That(extraction.GetOriginalUri(), Is.EqualTo("data:image/png;base64,AQID"));
        Assert.That(extraction.GetMediaType().IsType("image", "png"), Is.True);
        Assert.That(extraction.GetDecodedByteCount(), Is.EqualTo(3));
        EmailHtmlSanitizeResult sanitized = new EmailHtmlProcessor().SanitizeWithHtml("<a href='javascript:bad()' onclick='bad()'>safe</a>");
        Assert.That(sanitized.GetTokenizationResult().GetTokenCount(), Is.GreaterThan(0));

        EmailHtmlMutation mutation = new EmailHtmlMutation(EmailHtmlMutationKind.RemovedAttribute, "a", "href", "javascript:bad()", "", 3, "unsafe URL");
        Assert.That(mutation.GetTagName(), Is.EqualTo("a"));
        Assert.That(mutation.GetAttributeName(), Is.EqualTo("href"));
        Assert.That(mutation.GetOriginalValue(), Is.EqualTo("javascript:bad()"));
        Assert.That(mutation.GetReplacementValue(), Is.Empty);
        Assert.That(mutation.GetSourceOffset(), Is.EqualTo(3));
        Assert.That(mutation.GetDescription(), Is.EqualTo("unsafe URL"));
    }

    [Test]
    public void DisplayRelatedComparisonAndChangeResultsExposeTheirOutcomes()
    {
        MimeEntity html = ParseEntity("Content-Type: text/html; charset=utf-8\r\n\r\n<p>Hello</p><img src='cid:logo@example.test'>");
        MimeEntity image = ParseEntity("Content-Type: image/png\r\nContent-ID: <logo@example.test>\r\n\r\npng");
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", new List<MimeEntity> { html, image });
        EmailMessage message = MessageWithBody(root);
        EmailDisplayModel display = new EmailDisplayBuilder().BuildWithMessageAndOptions(message, new EmailDisplayOptions());
        Assert.That(display.GetAlternative(0), Is.SameAs(html));
        Assert.That(display.GetInlineResource(0).GetEntity(), Is.SameAs(image));
        Assert.That(new EmailDisplaySecuritySummary(0, 0, 0, 0, 1).HasReportContent(), Is.True);

        EmailRelatedIndex index = new EmailRelatedIndexOperations().BuildWithMessageAndOptions(message, new EmailRelatedIndexOptions());
        Assert.That(index.GetReferenceAt(0).IsResolved(), Is.True);
        Assert.That(index.GetReferenceAt(0).GetResolvedEntity(), Is.SameAs(image));

        MimeEntity duplicateHtml = html.Clone();
        MimeEntity firstDuplicateImage = image.Clone();
        MimeEntity duplicateImage = ParseEntity("Content-Type: image/png\r\nContent-ID: <logo@example.test>\r\n\r\ncopy");
        MimeEntity duplicateRoot = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", new List<MimeEntity> { duplicateHtml, firstDuplicateImage, duplicateImage });
        EmailRelatedDuplicate duplicate = new EmailRelatedIndexOperations().BuildWithRoot(duplicateRoot).GetDuplicateAt(0);
        Assert.That(duplicate.GetEntityCount(), Is.EqualTo(2));
        Assert.That(duplicate.GetEntityAt(0), Is.SameAs(firstDuplicateImage));

        EmailCanonicalComparisonOptions comparisonOptions = new EmailCanonicalComparisonOptions();
        comparisonOptions.AddIgnoredHeaderName("X-Trace");
        Assert.That(comparisonOptions.GetIgnoredHeaderNameAt(0), Is.EqualTo("X-Trace"));
        MimeEntity equivalent = ParseEntity("Content-Type: multipart/related; boundary=x\r\n\r\n--x\r\nContent-Type: text/html; charset=utf-8\r\n\r\n<p>Hello</p><img src='cid:logo@example.test'>\r\n--x\r\nContent-Type: image/png\r\nContent-ID: <logo@example.test>\r\n\r\npng\r\n--x--\r\n");
        Assert.That(new EmailCanonicalComparer().CompareWithLeftAndRightAndOptionsUsingMimeEntityAndMimeEntityAndEmailCanonicalComparisonOptions(root, equivalent, comparisonOptions).AreEqual(), Is.True);
        Assert.That(new EmailFingerprint().ComputeSemanticWithEntity(root).GetLowercaseHex(), Is.EqualTo(new EmailFingerprint().ComputeSemanticWithEntity(equivalent).GetLowercaseHex()));

        EmailPath path = new EmailPathOperations().CreateRoot();
        EmailNormalizationChange normalization = new EmailNormalizationChange(EmailNormalizationChangeKind.HeaderFolding, path, "Subject", "  hello  ", "hello");
        Assert.That(normalization.HasEntityPath(), Is.True);
        Assert.That(normalization.GetEntityPath(), Is.SameAs(path));
        Assert.That(normalization.GetFieldName(), Is.EqualTo("Subject"));
        Assert.That(normalization.HasOriginalValue(), Is.True);
        Assert.That(normalization.GetOriginalValue(), Is.EqualTo("  hello  "));
        Assert.That(normalization.HasNormalizedValue(), Is.True);
        Assert.That(normalization.GetNormalizedValue(), Is.EqualTo("hello"));

        EmailRedactionChange redaction = new EmailRedactionChange(EmailRedactionKind.Subject, path, "Subject", 2);
        Assert.That(redaction.HasEntityPath(), Is.True);
        Assert.That(redaction.GetEntityPath(), Is.SameAs(path));
        Assert.That(redaction.GetFieldName(), Is.EqualTo("Subject"));
        Assert.That(redaction.GetOccurrenceCount(), Is.EqualTo(2));
        EmailRedactionResult redactionResult = new EmailRedactionResult(null, root, new List<EmailRedactionChange> { redaction });
        Assert.That(redactionResult.GetEntity(), Is.SameAs(root));

        EmailGeneratedValue generated = new EmailGeneratedValue(EmailGenerationKind.MessageId, path, "Message-ID", "<generated@example.test>");
        Assert.That(generated.GetFieldName(), Is.EqualTo("Message-ID"));
        Assert.That(generated.GetValue(), Is.EqualTo("<generated@example.test>"));

        EmailFilenameResult filename = new EmailFilenameResult("../file.txt", "file.txt", false, true, false, false, false, false, false, 0);
        Assert.That(filename.HasOriginalFilename(), Is.True);
        Assert.That(filename.GetOriginalFilename(), Is.EqualTo("../file.txt"));
        Assert.That(new EmailContentTypeAnalysis("application/pdf", "file.pdf", "application/pdf", null, new EmailIssueList()).HasFilename(), Is.True);
    }

    static EmailMessage MessageWithBody(MimeEntity body)
    {
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(body);
        return message;
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.UTF8.GetBytes(source));
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }

    static string TemporaryPath(string label)
    {
        return Path.Combine(Path.GetTempPath(), "email-processing-" + label + "-" + Guid.NewGuid().ToString("N") + ".bin");
    }

    static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
