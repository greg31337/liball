using System.Text;
using email;

namespace email.Tests;

public class EmailWriterRoundTripTests
{
    [TestCase("sUbJeCt: exact\nX-Fold: one\r\n\ttwo\n\nbody\n")]
    [TestCase("Broken header line\r\nX-Test: value\n\nbody with mixed endings\r")]
    [TestCase("Content-Type: multipart/mixed; boundary=b\r\n\r\npreamble\r\n--b\r\nContent-Type: text/plain\r\n\r\nfirst\r\n--b\r\n\r\nsecond\r\n--b--\r\nepilogue")]
    [TestCase("Bcc: archive@example.test\r\nSubject: retained\r\n\r\nbody")]
    [TestCase("X-Incomplete: value")]
    public void UntouchedParsedMessagesRoundTripByteForByte(string source)
    {
        byte[] bytes = Bytes(source);
        EmailMessage message = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(bytes, new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessageAndOptions(message!, new EmailWriteOptionsOperations().CreateExactPreservation());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetBytes(), Is.EqualTo(bytes));
        Assert.That(result.GetCopiedByteCount(), Is.EqualTo(bytes.LongLength));
        Assert.That(result.GetRegeneratedByteCount(), Is.Zero);
    }

    [Test]
    public void CanonicalRoundTripIsByteStableAndSemanticallyEquivalent()
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("subject", Bytes(" Round trip café"), EmailHeaderValueKind.Unstructured));
        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>()
        {
            new EmailEntityFactory().CreateTextWithValue("hello café"),
            new EmailEntityFactory().CreateBinaryWithValue(new byte[] { 0, 1, 2, 254, 255 })
        });
        EmailMessage message = new EmailMessageOperations().CreateWithHeaders(headers);
        message.SetBody(multipart);
        EmailWriteOptions options = DeterministicOptions("canonical round trip");

        EmailWriteResult first = new EmailWriter().WriteMessageWithMessageAndOptions(message, options);
        EmailParseOptions parseOptions = new EmailParseOptionsOperations().CreatePreserving();
        EmailBodyIngestOptions ingest = parseOptions.GetBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        parseOptions.SetBodyIngestOptions(ingest);
        EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(first.GetBytes(), parseOptions);
        EmailWriteResult second = new EmailWriter().WriteMessageWithMessageAndOptions(parsed.GetMessage()!, options);

        Assert.That(first.IsSuccess(), Is.True);
        Assert.That(parsed.IsComplete(), Is.True);
        Assert.That(second.IsSuccess(), Is.True);
        Assert.That(second.GetBytes(), Is.EqualTo(first.GetBytes()));
        Assert.That(parsed.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetDecodedValue(), Is.EqualTo("Round trip café"));
        Assert.That(parsed.GetMessage()!.GetBody()!.GetChildCount(), Is.EqualTo(2));
        Assert.That(Text(parsed.GetMessage()!.GetBody()!.GetChildAt(0).GetDecodedContent()!.Materialize(100)), Is.EqualTo("hello café"));
        Assert.That(parsed.GetMessage()!.GetBody()!.GetChildAt(1).GetDecodedContent()!.Materialize(100), Is.EqualTo(new byte[] { 0, 1, 2, 254, 255 }));
    }

    [Test]
    public void DeterministicMultipartOutputIsStableAndSeedDependent()
    {
        EmailMessage message = MessageWithBody(new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity>()
        {
            new EmailEntityFactory().CreateTextWithValue("plain"),
            new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<p>html</p>", "html", "us-ascii")
        }));
        EmailWriteOptions firstSeed = DeterministicOptions("seed one");
        EmailWriteOptions secondSeed = DeterministicOptions("seed two");

        byte[] first = new EmailWriter().ToArrayWithMessageAndOptions(message, firstSeed);
        byte[] repeated = new EmailWriter().ToArrayWithMessageAndOptions(message, firstSeed);
        byte[] different = new EmailWriter().ToArrayWithMessageAndOptions(message, secondSeed);

        Assert.That(repeated, Is.EqualTo(first));
        Assert.That(different, Is.Not.EqualTo(first));
        Assert.That(Boundary(new EmailParser().ParseMessageWithInputUsingValueArray(first).GetMessage()!.GetBody()!), Is.EqualTo(new EmailBoundaryGenerator().CreateCandidate(firstSeed, 0)));
        Assert.That(message.GetBody()!.GetMediaType()!.GetParameters().GetFirst("boundary"), Is.Null);
    }

    [Test]
    public void MultipartWriterRetriesWhenAChildContainsTheFirstCandidateDelimiter()
    {
        EmailWriteOptions options = DeterministicOptions("writer collision");
        string rejected = new EmailBoundaryGenerator().CreateCandidate(options, 0);
        string selected = new EmailBoundaryGenerator().CreateCandidate(options, 1);
        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>()
        {
            new EmailEntityFactory().CreateTextWithValue("before\r\n--" + rejected + "\r\nafter")
        });

        EmailWriteResult result = new EmailWriter().WriteEntityWithEntityAndOptions(multipart, options);
        EmailEntityParseResult parsed = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(result.GetBytes(), new EmailParseOptionsOperations().CreatePreserving());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(parsed.IsComplete(), Is.True);
        Assert.That(Boundary(parsed.GetEntity()!), Is.EqualTo(selected));
        Assert.That(Text(parsed.GetEntity()!.GetChildAt(0).GetRawContent()!.Materialize(1000)), Is.EqualTo("before\r\n--" + rejected + "\r\nafter"));
    }

    [Test]
    public void ExactOutputLimitAcceptsTheBoundaryAndRejectsOneByteLess()
    {
        byte[] source = Bytes("Subject: exact\r\n\r\nbody");
        EmailMessage message = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
        EmailWriteOptions acceptedOptions = new EmailWriteOptionsOperations().CreateExactPreservation();
        acceptedOptions.SetMaximumOutputBytes(source.Length);
        EmailWriteOptions rejectedOptions = new EmailWriteOptionsOperations().CreateExactPreservation();
        rejectedOptions.SetMaximumOutputBytes(source.Length - 1);

        EmailWriteResult accepted = new EmailWriter().WriteMessageWithMessageAndOptions(message!, acceptedOptions);
        EmailWriteResult rejected = new EmailWriter().WriteMessageWithMessageAndOptions(message, rejectedOptions);

        Assert.That(accepted.IsSuccess(), Is.True);
        Assert.That(accepted.GetBytes(), Is.EqualTo(source));
        AssertRejected(rejected);
    }

    [Test]
    public void MultipartPartAndDepthLimitsAcceptExactBoundsAndRejectOverflow()
    {
        EmailWriteOptions partAcceptedOptions = DeterministicOptions("parts accepted");
        partAcceptedOptions.SetMaximumPartCount(3);
        EmailWriteOptions partRejectedOptions = DeterministicOptions("parts rejected");
        partRejectedOptions.SetMaximumPartCount(2);
        EmailWriteOptions depthAcceptedOptions = DeterministicOptions("depth accepted");
        depthAcceptedOptions.SetMaximumNestingDepth(2);
        EmailWriteOptions depthRejectedOptions = DeterministicOptions("depth rejected");
        depthRejectedOptions.SetMaximumNestingDepth(1);

        EmailWriteResult partsAccepted = new EmailWriter().WriteEntityWithEntityAndOptions(TwoPartMultipart(), partAcceptedOptions);
        EmailWriteResult partsRejected = new EmailWriter().WriteEntityWithEntityAndOptions(TwoPartMultipart(), partRejectedOptions);
        EmailWriteResult depthAccepted = new EmailWriter().WriteEntityWithEntityAndOptions(ThreeLevelMultipart(), depthAcceptedOptions);
        EmailWriteResult depthRejected = new EmailWriter().WriteEntityWithEntityAndOptions(ThreeLevelMultipart(), depthRejectedOptions);

        Assert.That(partsAccepted.IsSuccess(), Is.True);
        AssertRejected(partsRejected);
        Assert.That(depthAccepted.IsSuccess(), Is.True);
        AssertRejected(depthRejected);
    }

    static MimeEntity TwoPartMultipart()
    {
        return new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>()
        {
            new EmailEntityFactory().CreateTextWithValue("one"),
            new EmailEntityFactory().CreateTextWithValue("two")
        });
    }

    static MimeEntity ThreeLevelMultipart()
    {
        return new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>()
        {
            new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity>()
            {
                new EmailEntityFactory().CreateMultipartWithSubtype("related")
            })
        });
    }

    static EmailMessage MessageWithBody(MimeEntity body)
    {
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(body);
        return message;
    }

    static EmailWriteOptions DeterministicOptions(string seed)
    {
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(Bytes(seed));
        return options;
    }

    static string Boundary(MimeEntity entity)
    {
        return Encoding.ASCII.GetString(entity.GetMediaType()!.GetParameters().GetFirst("boundary")!.GetValueBytes());
    }

    static void AssertRejected(EmailWriteResult result)
    {
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetBytes(), Is.Empty);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.WritePolicyViolation), Is.True);
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
