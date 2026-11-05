using System.Text;
using email;

namespace email.Tests;

public class EmailGeneratorTests
{
    [Test]
    public void DefaultsAreOptInAndReturnAnIndependentExactClone()
    {
        EmailMessage original = ParseMessage("From: sender@example.test\n\nbody\n");
        byte[] before = new EmailWriter().ToArrayWithMessageAndOptions(original, new EmailWriteOptionsOperations().CreateExactPreservation());

        EmailGenerationResult result = new EmailGenerator().GenerateWithMessage(original);
        byte[] after = new EmailWriter().ToArrayWithMessageAndOptions(result.GetMessage()!, new EmailWriteOptionsOperations().CreateExactPreservation());

        Assert.That(result.HasMessage(), Is.True);
        Assert.That(result.HasEntity(), Is.False);
        Assert.That(result.HasGeneratedValues(), Is.False);
        Assert.That(result.GetMessage(), Is.Not.SameAs(original));
        Assert.That(after, Is.EqualTo(before));
        Assert.That(original.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void AllRequestedValuesAreGeneratedConsistentlyAndDeterministically()
    {
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("one"), new EmailEntityFactory().CreateTextWithValue("two") });
        EmailMessage original = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(new EmailHeadersOperations().CreateWith(), root, new EmailEnvelopeOperations().CreateWith(), null, new EmailIssueList());
        EmailGenerationOptions options = new EmailGenerationOptionsOperations().CreateAll();
        options.SetGenerationDate(ParseDate("Fri, 21 Nov 1997 09:55:06 -0600"));
        options.SetIdentifierDomain("Mail.Example.Test");
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(Encoding.ASCII.GetBytes("generation-test"));

        EmailGenerationResult first = new EmailGenerator().GenerateWithMessageAndOptions(original, options);
        EmailGenerationResult second = new EmailGenerator().GenerateWithMessageAndOptions(original, options);
        EmailMessage message = first.GetMessage()!;
        MimeEntity generatedRoot = message!.GetBody()!;
        string messageId = GetHeaderValue(message.GetHeaders()!, "Message-ID");
        string? boundary = GetParameter(generatedRoot!.GetMediaType()!, "boundary");
        HashSet<string> identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        identifiers.Add(messageId);
        identifiers.Add(GetHeaderValue(generatedRoot.GetHeaders()!, "Content-ID"));
        identifiers.Add(GetHeaderValue(generatedRoot.GetChildAt(0).GetHeaders()!, "Content-ID"));
        identifiers.Add(GetHeaderValue(generatedRoot.GetChildAt(1).GetHeaders()!, "Content-ID"));

        Assert.That(GetHeaderValue(message.GetHeaders()!, "Date"), Is.EqualTo("Fri, 21 Nov 1997 09:55:06 -0600"));
        Assert.That(GetHeaderValue(message.GetHeaders()!, "MIME-Version"), Is.EqualTo("1.0"));
        Assert.That(messageId, Does.EndWith("@mail.example.test>"));
        Assert.That(identifiers.Count, Is.EqualTo(4));
        Assert.That(boundary, Does.StartWith(EmailBoundaryGeneratorConstants.BoundaryPrefix));
        Assert.That(GetHeaderValue(message.GetHeaders()!, "Content-Type"), Does.Contain(boundary));
        Assert.That(generatedRoot.GetContentId(), Is.Not.Null);
        Assert.That(generatedRoot.GetChildAt(0).GetContentId(), Is.Not.Null);
        Assert.That(generatedRoot.GetChildAt(1).GetContentId(), Is.Not.Null);
        Assert.That(first.GetGeneratedValueCount(), Is.EqualTo(7));
        Assert.That(FindGenerated(first, EmailGenerationKind.ContentId, "root/1"), Is.Not.Null);
        Assert.That(GetHeaderValue(second.GetMessage()!.GetHeaders()!, "Message-ID"), Is.EqualTo(messageId));
        Assert.That(GetParameter(second.GetMessage()!.GetBody()!.GetMediaType()!, "boundary"), Is.EqualTo(boundary));
        Assert.That(original.GetHeaders()!.Contains("Date"), Is.False);
        Assert.That(original.GetBody()!.GetMediaType()!.GetParameters().GetFirst("boundary"), Is.Null);
    }

    [Test]
    public void ExistingValuesAreNeverOverwritten()
    {
        const string source = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n"
            + "Message-ID: <existing@example.test>\r\n"
            + "MIME-Version: 1.0\r\n"
            + "Content-Type: multipart/mixed; boundary=existing\r\n"
            + "Content-ID: <root@example.test>\r\n\r\n"
            + "--existing\r\nContent-Type: text/plain\r\nContent-ID: <child@example.test>\r\n\r\nbody\r\n"
            + "--existing--\r\n";
        EmailMessage original = ParseMessage(source);
        byte[] before = new EmailWriter().ToArrayWithMessageAndOptions(original, new EmailWriteOptionsOperations().CreateExactPreservation());

        EmailGenerationResult result = new EmailGenerator().GenerateWithMessageAndOptions(original, new EmailGenerationOptionsOperations().CreateAll());
        byte[] after = new EmailWriter().ToArrayWithMessageAndOptions(result.GetMessage()!, new EmailWriteOptionsOperations().CreateExactPreservation());

        Assert.That(result.GetGeneratedValueCount(), Is.Zero);
        Assert.That(after, Is.EqualTo(before));
        Assert.That(GetHeaderValue(result.GetMessage()!.GetHeaders()!, "Message-ID"), Is.EqualTo("<existing@example.test>"));
        Assert.That(GetParameter(result.GetMessage()!.GetBody()!.GetMediaType()!, "boundary"), Is.EqualTo("existing"));
    }

    [Test]
    public void BoundaryGenerationDetectsContentCollisions()
    {
        byte[] seed = Encoding.ASCII.GetBytes("collision-seed");
        byte[] context = Encoding.ASCII.GetBytes("boundary:root:0:0");
        byte[] combined = new byte[seed.Length + context.Length];
        seed.CopyTo(combined, 0);
        context.CopyTo(combined, seed.Length);
        EmailWriteOptions boundaryOptions = new EmailWriteOptions();
        boundaryOptions.SetDeterministicOutput(true);
        boundaryOptions.SetDeterministicSeed(combined);
        string colliding = new EmailBoundaryGenerator().CreateCandidate(boundaryOptions, 0);
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("--" + colliding + "\r\npayload") });
        EmailGenerationOptions options = new EmailGenerationOptions();
        options.SetGenerateBoundaries(true);
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(seed);

        EmailGenerationResult result = new EmailGenerator().GenerateWithEntityAndOptions(root, options);
        string? generated = GetParameter(result.GetEntity()!.GetMediaType()!, "boundary");

        Assert.That(generated, Is.Not.EqualTo(colliding));
        Assert.That(generated, Does.StartWith(EmailBoundaryGeneratorConstants.BoundaryPrefix));
        Assert.That(result.GetEntity()!.GetHeaders()!.GetFirst("Content-Type"), Is.Not.Null);
        Assert.That(result.GetGeneratedValueAt(0).GetKind(), Is.EqualTo(EmailGenerationKind.Boundary));
        Assert.That(result.GetEntity()!.IsDirty(), Is.True);
    }

    [Test]
    public void OptionsResultsArgumentsAndTraversalLimitsAreDefensive()
    {
        EmailGenerationOptions options = new EmailGenerationOptions();
        options.SetDeterministicSeed(new byte[] { 1, 2, 3 });
        options.SetGenerationDate(ParseDate("Fri, 21 Nov 1997 09:55:06 -0600"));
        EmailGenerationOptions clone = options.Clone();
        byte[] returnedSeed = clone.GetDeterministicSeed();
        returnedSeed[0] = 9;
        EmailGenerationResult empty = new EmailGenerator().GenerateWithEntity(new EmailEntityFactory().CreateTextWithValue("value"));
        List<EmailGeneratedValue> returned = empty.GetGeneratedValues();
        returned.Add(new EmailGeneratedValue(EmailGenerationKind.Date, null!, "Date", "value"));

        Assert.That(clone.GetDeterministicSeed()[0], Is.EqualTo(1));
        Assert.That(clone.HasGenerationDate(), Is.True);
        clone.ClearGenerationDate();
        Assert.That(clone.HasGenerationDate(), Is.False);
        Assert.That(empty.GetGeneratedValueCount(), Is.Zero);
        Assert.Throws<ArgumentNullException>(() => new EmailGenerator().GenerateWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailGenerator().GenerateWithEntity((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailGenerator().GenerateWithMessageAndOptions(new EmailMessageOperations().CreateWith(), null!));
        Assert.Throws<ArgumentNullException>(() => options.SetDeterministicSeed(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetGenerationDate(null!));
        Assert.Throws<ArgumentException>(() => options.SetIdentifierDomain("-bad.example"));
        Assert.Throws<ArgumentException>(() => options.SetIdentifierDomain("bad..example"));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDepth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumEntityCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.GetGeneratedValueAt(0));

        MimeEntity deep = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("child") });
        EmailGenerationOptions limited = new EmailGenerationOptions();
        limited.SetMaximumDepth(0);
        Assert.That(Assert.Throws<EmailLimitException>(() => new EmailGenerator().GenerateWithEntityAndOptions(deep, limited)).GetLimitName(), Is.EqualTo("MaxTraversalDepth"));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static EmailDateTime ParseDate(string value)
    {
        EmailDateParseResult result = new EmailDateParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailDateParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetValue()!;
    }

    static string GetHeaderValue(EmailHeaders headers, string name)
    {
        return Encoding.ASCII.GetString(headers.GetFirst(name)!.GetUnfoldedValue()).Trim();
    }

    static string? GetParameter(EmailMediaType mediaType, string name)
    {
        EmailParameter parameter = mediaType.GetParameters().GetFirst(name)!;
        return parameter == null ? null : Encoding.ASCII.GetString(parameter.GetValueBytes())!;
    }

    static EmailGeneratedValue FindGenerated(EmailGenerationResult result, EmailGenerationKind kind, string path)
    {
        for (int index = 0; index < result.GetGeneratedValueCount(); index++)
        {
            EmailGeneratedValue generated = result.GetGeneratedValueAt(index);
            if (generated.GetKind() == kind && generated.HasEntityPath() && generated.GetEntityPath().GetDisplayText() == path)
            {
                return generated;
            }
        }

        return null!;
    }
}
