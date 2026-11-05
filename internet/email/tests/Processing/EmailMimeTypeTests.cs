using System.Text;
using email;

namespace email.Tests;

public class EmailMimeTypeTests
{
    [Test]
    public void ExtensionLookupIsCaseInsensitiveBidirectionalAndFilenameAware()
    {
        Assert.That(new EmailMimeType().GetMediaTypeForExtension(".PDF"), Is.EqualTo("application/pdf"));
        Assert.That(new EmailMimeType().GetMediaTypeForExtension("archive.tar.GZ"), Is.EqualTo("application/gzip"));
        Assert.That(new EmailMimeType().GetMediaTypeForFilename(@"C:\mail\Quarterly.Report.DOCX"), Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
        Assert.That(new EmailMimeType().GetMediaTypeForFilename("README"), Is.Null);
        Assert.That(new EmailMimeType().GetMediaTypeForExtension("unknown"), Is.Null);
        Assert.That(new EmailMimeType().GetPreferredExtensionWithMediaTypeUsingString("IMAGE/JPEG"), Is.EqualTo("jpg"));
        Assert.That(new EmailMimeType().GetPreferredExtensionWithMediaTypeUsingString("application/x-gzip"), Is.EqualTo("gz"));
        Assert.That(new EmailMimeType().GetPreferredExtensionWithMediaTypeUsingString("application/x-private"), Is.Null);

        MimeEntity html = ParseEntity("Content-Type: text/html\r\n\r\n<p>x</p>");
        Assert.That(new EmailMimeType().GetPreferredExtensionWithMediaTypeUsingEmailMediaType(html.GetMediaType()!), Is.EqualTo("html"));
        Assert.Throws<ArgumentNullException>(() => new EmailMimeType().GetMediaTypeForExtension(null!));
        Assert.Throws<ArgumentException>(() => new EmailMimeType().GetMediaTypeForExtension("."));
        Assert.Throws<ArgumentNullException>(() => new EmailMimeType().GetMediaTypeForFilename(null!));
        Assert.Throws<ArgumentException>(() => new EmailMimeType().GetPreferredExtensionWithMediaTypeUsingString("invalid"));
    }

    [Test]
    public void MagicDetectionRecognizesOnlyCompleteStrongPrefixes()
    {
        Assert.That(new EmailMimeType().DetectFromMagic(Encoding.ASCII.GetBytes("%PDF-1.7")), Is.EqualTo("application/pdf"));
        Assert.That(new EmailMimeType().DetectFromMagic(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }), Is.EqualTo("image/png"));
        Assert.That(new EmailMimeType().DetectFromMagic(new byte[] { 0xff, 0xd8, 0xff, 0xe0 }), Is.EqualTo("image/jpeg"));
        Assert.That(new EmailMimeType().DetectFromMagic(Encoding.ASCII.GetBytes("GIF89a")), Is.EqualTo("image/gif"));
        Assert.That(new EmailMimeType().DetectFromMagic(new byte[] { 0x50, 0x4b, 0x03, 0x04 }), Is.EqualTo("application/zip"));
        Assert.That(new EmailMimeType().DetectFromMagic(new byte[] { 0x1f, 0x8b, 0x08 }), Is.EqualTo("application/gzip"));
        Assert.That(new EmailMimeType().DetectFromMagic(Encoding.ASCII.GetBytes("RIFF1234WEBP")), Is.EqualTo("image/webp"));
        Assert.That(new EmailMimeType().DetectFromMagic(Encoding.ASCII.GetBytes("RIFF1234WAVE")), Is.EqualTo("audio/wav"));
        Assert.That(new EmailMimeType().DetectFromMagic(new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x6d, 0x70, 0x34, 0x32 }), Is.EqualTo("video/mp4"));
        Assert.That(new EmailMimeType().DetectFromMagic(new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63 }), Is.EqualTo("image/heic"));
        Assert.That(new EmailMimeType().DetectFromMagic(Encoding.ASCII.GetBytes("%PDF")), Is.Null);
        Assert.That(new EmailMimeType().DetectFromMagic(Encoding.ASCII.GetBytes("ordinary text")), Is.Null);
    }

    [Test]
    public void InspectorSeparatesExtensionAndMagicMismatches()
    {
        MimeEntity extensionMismatch = ParseEntity(
            "Content-Type: application/pdf\r\nContent-Disposition: attachment; filename=report.png\r\n\r\n%PDF-1.7");
        EmailContentTypeAnalysis extension = new EmailContentTypeInspector().InspectWithEntity(extensionMismatch);

        Assert.That(extension.GetDeclaredMediaType(), Is.EqualTo("application/pdf"));
        Assert.That(extension.GetFilename(), Is.EqualTo("report.png"));
        Assert.That(extension.GetExtensionMediaType(), Is.EqualTo("image/png"));
        Assert.That(extension.GetDetectedMediaType(), Is.EqualTo("application/pdf"));
        Assert.That(extension.HasExtensionMismatch(), Is.True);
        Assert.That(extension.HasMagicMismatch(), Is.False);
        Assert.That(extension.GetIssues().ContainsCode(EmailIssueCode.MediaTypeExtensionMismatch), Is.True);

        MimeEntity magicMismatch = ParseEntity(
            "Content-Type: image/png\r\nContent-Disposition: attachment; filename=photo.png\r\n\r\n%PDF-1.7");
        EmailContentTypeAnalysis magic = new EmailContentTypeInspector().InspectWithEntity(magicMismatch);

        Assert.That(magic.HasExtensionMismatch(), Is.False);
        Assert.That(magic.HasMagicMismatch(), Is.True);
        Assert.That(magic.GetSuggestedMediaType(), Is.EqualTo("application/pdf"));
        Assert.That(magic.GetIssues().GetAt(0).GetSeverity(), Is.EqualTo(EmailIssueSeverity.Warning));
        Assert.That(magic.GetIssues().GetAt(0).GetFieldName(), Is.EqualTo("Content-Type"));
    }

    [Test]
    public void GenericDeclarationsAliasesAndZipContainersAvoidFalseMismatchWarnings()
    {
        MimeEntity generic = ParseEntity(
            "Content-Type: application/octet-stream\r\nContent-Disposition: attachment; filename=report.pdf\r\n\r\n%PDF-1.7");
        EmailContentTypeAnalysis genericResult = new EmailContentTypeInspector().InspectWithEntity(generic);
        Assert.That(genericResult.GetDetectedMediaType(), Is.EqualTo("application/pdf"));
        Assert.That(genericResult.GetSuggestedMediaType(), Is.EqualTo("application/pdf"));
        Assert.That(genericResult.GetIssues().GetCount(), Is.Zero);

        MimeEntity document = ParseEntity(
            "Content-Type: application/vnd.openxmlformats-officedocument.wordprocessingml.document\r\n"
            + "Content-Disposition: attachment; filename=document.docx\r\n\r\nPK\x03\x04rest");
        EmailContentTypeAnalysis documentResult = new EmailContentTypeInspector().InspectWithEntity(document);
        Assert.That(documentResult.GetDetectedMediaType(), Is.EqualTo("application/zip"));
        Assert.That(documentResult.GetSuggestedMediaType(), Is.EqualTo("application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
        Assert.That(documentResult.GetIssues().GetCount(), Is.Zero);

        Assert.That(new EmailMimeType().AreCompatible("image/jpg", "image/jpeg"), Is.True);
        Assert.That(new EmailMimeType().AreCompatible("audio/ogg", "application/ogg"), Is.True);
        Assert.That(new EmailMimeType().AreCompatible("application/pdf", "image/png"), Is.False);
    }

    [Test]
    public void AttachmentInspectionUsesDecodedBoundedPrefixAndCarriesEntityPath()
    {
        byte[] decoded = new byte[200000];
        byte[] signature = Encoding.ASCII.GetBytes("%PDF-1.7");
        Array.Copy(signature, decoded, signature.Length);
        byte[] encoded = Encoding.ASCII.GetBytes(Convert.ToBase64String(decoded));
        TrackingContentReadStream stream = new TrackingContentReadStream(encoded);
        MimeEntity entity = ParseEntity(
            "Content-Type: image/png\r\nContent-Transfer-Encoding: base64\r\n\r\niVBORw0KGgo=");
        entity.ReplaceRawContent(new EmailContentOperations().CreateStream(stream, EmailContentOwnership.Borrowed));
        EmailPath path = new EmailPathOperations().CreateRoot().Append(2);
        EmailAttachmentView attachment = new EmailAttachmentView(entity, path, EmailAttachmentKind.Attachment, "photo.png");

        EmailContentTypeAnalysis analysis = attachment.InspectContentType();

        Assert.That(analysis.GetDetectedMediaType(), Is.EqualTo("application/pdf"));
        Assert.That(analysis.HasMagicMismatch(), Is.True);
        Assert.That(analysis.GetIssues().GetAt(0).GetEntityPath(), Is.SameAs(path));
        Assert.That(stream.GetBytesRead(), Is.LessThan(encoded.LongLength));
        Assert.That(stream.GetLargestReadRequest(), Is.LessThan(300));
        entity.GetRawContent()!.Close();
        stream.Close();
    }

    [Test]
    public void InspectorReturnsSnapshotsAndSkipsAbsentOrUndecodableMagic()
    {
        MimeEntity unsupported = ParseEntity(
            "Content-Type: application/x-private\r\nContent-Transfer-Encoding: x-custom\r\n"
            + "Content-Disposition: attachment; filename=data.unknown\r\n\r\n%PDF-1.7");

        EmailContentTypeAnalysis analysis = new EmailContentTypeInspector().InspectWithEntity(unsupported);
        EmailIssueList issues = analysis.GetIssues();
        issues.Clear();

        Assert.That(analysis.HasDetectedMediaType(), Is.False);
        Assert.That(analysis.HasExtensionMediaType(), Is.False);
        Assert.That(analysis.GetSuggestedMediaType(), Is.EqualTo("application/x-private"));
        Assert.That(analysis.GetIssues().GetCount(), Is.Zero);
        Assert.Throws<ArgumentNullException>(() => new EmailContentTypeInspector().InspectWithEntity(null!));
        Assert.Throws<ArgumentException>(() => new EmailContentTypeInspector().InspectWithEntityAndFilename(unsupported, ""));
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.Latin1.GetBytes(source));
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }
}
