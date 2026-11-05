using System.Text;
using email;

namespace email.Tests;

public class EmailNormalizerTests
{
    [Test]
    public void DefaultsAreOptInAndReturnAnIndependentUnchangedModel()
    {
        EmailMessage original = ParseMessage("Subject: original\n\nbody\n");

        EmailNormalizationResult result = new EmailNormalizer().NormalizeWithMessage(original);

        Assert.That(result.HasMessage(), Is.True);
        Assert.That(result.HasEntity(), Is.False);
        Assert.That(result.HasChanges(), Is.False);
        Assert.That(result.GetMessage(), Is.Not.SameAs(original));
        Assert.That(result.GetMessage()!.GetHeaders()!.GetFirst("Subject"), Is.Not.SameAs(original.GetHeaders()!.GetFirst("Subject")));
        Assert.That(result.GetMessage()!.GetHeaders()!.GetFirst("Subject")!.GetLineEnding(), Is.EqualTo(EmailLineEnding.Lf));
        Assert.That(original.GetDirtyState(), Is.EqualTo(EmailDirtyState.Clean));
    }

    [Test]
    public void LineEndingsAndFoldingAreNormalizedOnlyWhenSelected()
    {
        EmailMessage original = ParseMessage("Subject: alpha beta gamma delta epsilon zeta eta theta iota kappa lambda\n\nline one\nline two\n");
        EmailNormalizationOptions options = new EmailNormalizationOptions();
        options.SetNormalizeLineEndings(true);
        options.SetRefoldHeaders(true);
        options.SetPreferredHeaderLineLength(30);

        EmailNormalizationResult result = new EmailNormalizer().NormalizeWithMessageAndOptions(original, options);
        EmailHeader subject = result.GetMessage()!.GetHeaders()!.GetFirst("Subject")!;
        byte[] body = result.GetMessage()!.GetBody()!.GetContent()!.Materialize(100);

        Assert.That(subject!.GetLineEnding(), Is.EqualTo(EmailLineEnding.CrLf));
        Assert.That(subject.GetFoldCount(), Is.GreaterThan(0));
        Assert.That(Encoding.ASCII.GetString(body), Is.EqualTo("line one\r\nline two\r\n"));
        Assert.That(HasChange(result, EmailNormalizationChangeKind.LineEnding), Is.True);
        Assert.That(HasChange(result, EmailNormalizationChangeKind.HeaderFolding), Is.True);
        Assert.That(original.GetHeaders()!.GetFirst("Subject")!.GetLineEnding(), Is.EqualTo(EmailLineEnding.Lf));
        Assert.That(Encoding.ASCII.GetString(original.GetBody()!.GetContent()!.Materialize(100)), Is.EqualTo("line one\nline two\n"));
    }

    [Test]
    public void MimeContentTypeCharsetEncodingAndFilenameRepairsStayConsistent()
    {
        EmailMessage missingType = ParseMessage("From: sender@example.test\r\n\r\nplain");
        EmailNormalizationOptions typeOptions = new EmailNormalizationOptions();
        typeOptions.SetRepairContentTypes(true);

        EmailNormalizationResult typeResult = new EmailNormalizer().NormalizeWithMessageAndOptions(missingType, typeOptions);

        Assert.That(typeResult.GetMessage()!.GetHeaders()!.Contains("Content-Type"), Is.True);
        Assert.That(typeResult.GetMessage()!.GetBody()!.GetMediaType()!.WasDefaulted(), Is.False);
        Assert.That(HasChange(typeResult, EmailNormalizationChangeKind.ContentType), Is.True);

        EmailMessage original = ParseMessage(
            "From: sender@example.test\r\n"
            + "Content-Type: text/plain; charset=UTF8\r\n"
            + "Content-Disposition: attachment; filename=\"../../bad?.txt\"\r\n"
            + "Content-Transfer-Encoding: base64\r\n\r\n"
            + "aGVsbG8K");
        EmailNormalizationOptions options = new EmailNormalizationOptions();
        options.SetRepairMimeVersion(true);
        options.SetNormalizeCharsets(true);
        options.SetNormalizeFilenames(true);
        options.SetRepairTransferEncodings(true);
        options.SetNormalizeLineEndings(true);

        EmailNormalizationResult result = new EmailNormalizer().NormalizeWithMessageAndOptions(original, options);
        MimeEntity normalized = result.GetMessage()!.GetBody()!;

        Assert.That(GetValue(result.GetMessage()!.GetHeaders()!, "MIME-Version"), Is.EqualTo("1.0"));
        Assert.That(GetParameter(normalized!.GetMediaType()!, "charset"), Is.EqualTo("utf-8"));
        Assert.That(new EmailFilename().Decode(normalized), Is.EqualTo("bad_.txt"));
        Assert.That(normalized.GetTransferEncodingName(), Is.EqualTo("7bit"));
        Assert.That(Encoding.ASCII.GetString(normalized.GetDecodedContent()!.Materialize(100)), Is.EqualTo("hello\r\n"));
        Assert.That(GetValue(result.GetMessage()!.GetHeaders()!, "Content-Transfer-Encoding"), Is.EqualTo("7bit"));
        Assert.That(HasChange(result, EmailNormalizationChangeKind.MimeVersion), Is.True);
        Assert.That(HasChange(result, EmailNormalizationChangeKind.Charset), Is.True);
        Assert.That(HasChange(result, EmailNormalizationChangeKind.Filename), Is.True);
        Assert.That(HasChange(result, EmailNormalizationChangeKind.TransferEncoding), Is.True);
        Assert.That(GetParameter(original.GetBody()!.GetMediaType()!, "charset"), Is.EqualTo("UTF8"));
        Assert.That(original.GetBody()!.GetTransferEncodingName(), Is.EqualTo("base64"));
    }

    [Test]
    public void MissingBoundariesAreRepairedDeterministicallyWithoutChangingTheInput()
    {
        MimeEntity original = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("one"), new EmailEntityFactory().CreateTextWithValue("two") });
        EmailNormalizationOptions options = new EmailNormalizationOptions();
        options.SetRepairBoundaries(true);
        options.SetDeterministicBoundarySeed(Encoding.ASCII.GetBytes("normalization-test"));

        EmailNormalizationResult first = new EmailNormalizer().NormalizeWithEntityAndOptions(original, options);
        EmailNormalizationResult second = new EmailNormalizer().NormalizeWithEntityAndOptions(original, options);
        string? firstBoundary = GetParameter(first.GetEntity()!.GetMediaType()!, "boundary");

        Assert.That(firstBoundary, Does.StartWith(EmailBoundaryGeneratorConstants.BoundaryPrefix));
        Assert.That(GetParameter(second.GetEntity()!.GetMediaType()!, "boundary"), Is.EqualTo(firstBoundary));
        Assert.That(first.GetEntity()!.GetHeaders()!.Contains("Content-Type"), Is.True);
        Assert.That(original.GetMediaType()!.GetParameters().GetFirst("boundary"), Is.Null);
        Assert.That(HasChange(first, EmailNormalizationChangeKind.Boundary), Is.True);
        Assert.That(first.GetEntity()!.IsDirty(), Is.True);
    }

    [Test]
    public void OptionsAndResultsAreDefensiveAndArgumentsAreValidated()
    {
        EmailNormalizationOptions options = new EmailNormalizationOptionsOperations().CreateAll();
        options.SetDeterministicBoundarySeed(new byte[] { 1, 2, 3 });
        EmailNormalizationOptions clone = options.Clone();
        byte[] returnedSeed = clone.GetDeterministicBoundarySeed();
        returnedSeed[0] = 9;
        EmailNormalizationResult result = new EmailNormalizer().NormalizeWithEntityAndOptions(new EmailEntityFactory().CreateTextWithValue("value"), new EmailNormalizationOptions());
        List<EmailNormalizationChange> returnedChanges = result.GetChanges();
        returnedChanges.Add(new EmailNormalizationChange(EmailNormalizationChangeKind.Charset, null!, "field", null, null));

        Assert.That(clone.GetDeterministicBoundarySeed()[0], Is.EqualTo(1));
        Assert.That(result.GetChangeCount(), Is.Zero);
        Assert.Throws<ArgumentNullException>(() => new EmailNormalizer().NormalizeWithMessage((EmailMessage)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailNormalizer().NormalizeWithEntity((MimeEntity)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailNormalizer().NormalizeWithMessageAndOptions(new EmailMessageOperations().CreateWith(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetPreferredHeaderLineLength(19));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumContentBytes(0));
        Assert.Throws<ArgumentNullException>(() => options.SetDeterministicBoundarySeed(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetChangeAt(0));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.ASCII.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }

    static bool HasChange(EmailNormalizationResult result, EmailNormalizationChangeKind kind)
    {
        for (int index = 0; index < result.GetChangeCount(); index++)
        {
            if (result.GetChangeAt(index).GetKind() == kind)
            {
                return true;
            }
        }

        return false;
    }

    static string GetValue(EmailHeaders headers, string name)
    {
        return Encoding.ASCII.GetString(headers.GetFirst(name)!.GetUnfoldedValue()).Trim();
    }

    static string? GetParameter(EmailMediaType mediaType, string name)
    {
        EmailParameter parameter = mediaType.GetParameters().GetFirst(name)!;
        return parameter == null ? null : Encoding.ASCII.GetString(parameter.GetValueBytes())!;
    }
}
