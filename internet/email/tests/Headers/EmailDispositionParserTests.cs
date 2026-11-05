using System.Text;
using email;

namespace email.Tests;

public class EmailDispositionParserTests
{
    [Test]
    public void ParsesAttachmentAndQuotedFilename()
    {
        const string input = "Attachment; filename=\"report final.pdf\"; size=1234";

        EmailDispositionParseResult result = Parse(input, EmailStructuredValueParseMode.Strict);

        EmailDisposition value = result.GetValue()!;
        Assert.That(value!.GetDispositionType(), Is.EqualTo("Attachment"));
        Assert.That(value.GetNormalizedDispositionType(), Is.EqualTo("attachment"));
        Assert.That(value.IsAttachment(), Is.True);
        Assert.That(value.IsInline(), Is.False);
        Assert.That(Text(value.GetParameters().GetDecoded("filename")!.GetDecodedBytes()), Is.EqualTo("report final.pdf"));
        Assert.That(Text(value.GetParameters().GetDecoded("size")!.GetDecodedBytes()), Is.EqualTo("1234"));
        Assert.That(Text(value.GetRawBytes()), Is.EqualTo(input));
        Assert.That(result.GetIssues().GetCount(), Is.Zero);
    }

    [Test]
    public void DecodesExtendedFilenameAndPreservesFallback()
    {
        const string input = "attachment; filename=plain.txt; filename*=UTF-8''caf%C3%A9.txt";

        EmailDispositionParseResult result = Parse(input, EmailStructuredValueParseMode.Strict);

        EmailParameterList parameters = result.GetValue()!.GetParameters();
        Assert.That(parameters.GetCount(), Is.EqualTo(2));
        Assert.That(Text(parameters.GetAt(0).GetValueBytes()), Is.EqualTo("plain.txt"));
        Assert.That(Encoding.UTF8.GetString(parameters.GetDecoded("filename")!.GetDecodedBytes()), Is.EqualTo("café.txt"));
        Assert.That(parameters.GetDecoded("filename")!.IsExtended(), Is.True);
    }

    [Test]
    public void TreatsUnknownDispositionAsAttachmentWhilePreservingExtension()
    {
        EmailDisposition value = Parse("X-Preview; filename=image.png", EmailStructuredValueParseMode.Strict).GetValue()!;

        Assert.That(value!.GetDispositionType(), Is.EqualTo("X-Preview"));
        Assert.That(value.IsExtension(), Is.True);
        Assert.That(value.GetEffectiveDispositionType(), Is.EqualTo("attachment"));
    }

    [Test]
    public void MissingDispositionReturnsNoValueWithModeDependentIssue()
    {
        EmailDispositionParseResult tolerant = Parse("; filename=x", EmailStructuredValueParseMode.Tolerant);
        EmailDispositionParseResult strict = Parse("; filename=x", EmailStructuredValueParseMode.Strict);

        Assert.That(tolerant.HasValue(), Is.False);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.DispositionMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.HasValue(), Is.False);
        Assert.That(strict.IsSuccess(), Is.False);
    }

    static EmailDispositionParseResult Parse(string value, EmailStructuredValueParseMode mode)
    {
        return new EmailDispositionParser().ParseWithInputAndMode(Encoding.ASCII.GetBytes(value), mode);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
