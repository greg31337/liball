using System.Text;
using email;

namespace email.Tests;

public class EmailSpecializedMessageParserTests
{
    [Test]
    public void ParsesPartialFragmentMetadataWithoutReassembly()
    {
        const string firstSource = "Content-Type: message/partial; id=\"fragment@example.test\"; number=1; total=2\r\n\r\nContent-Type: text/plain\r\n\r\nfirst half";
        const string secondSource = "Content-Type: message/partial; total=2; number=2; id=\"fragment@example.test\"\r\n\r\nsecond half";

        MimeEntity first = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(firstSource)).GetEntity()!;
        MimeEntity second = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(secondSource)).GetEntity()!;
        EmailPartialMessageMetadata firstMetadata = first!.GetPartialMessageMetadata()!;
        EmailPartialMessageMetadata secondMetadata = second!.GetPartialMessageMetadata()!;

        Assert.That(first.GetKind(), Is.EqualTo(MimeEntityKind.PartialMessage));
        Assert.That(first.HasPartialMessageMetadata(), Is.True);
        Assert.That(firstMetadata!.GetFragmentId(), Is.EqualTo("fragment@example.test"));
        Assert.That(firstMetadata.GetFragmentNumber(), Is.EqualTo(1));
        Assert.That(firstMetadata.GetTotalFragments(), Is.EqualTo(2));
        Assert.That(firstMetadata.HasRequiredMetadata(), Is.True);
        Assert.That(firstMetadata.IsFinalFragment(), Is.False);
        Assert.That(secondMetadata!.GetFragmentNumber(), Is.EqualTo(2));
        Assert.That(secondMetadata.IsFinalFragment(), Is.True);
        Assert.That(first.HasNestedMessage(), Is.False);
        Assert.That(first.GetChildCount(), Is.Zero);
        Assert.That(Text(first.GetRawContent()!.Materialize(1000)), Is.EqualTo("Content-Type: text/plain\r\n\r\nfirst half"));
        Assert.That(Text(second.GetRawContent()!.Materialize(1000)), Is.EqualTo("second half"));
        Assert.That(first.Clone().GetPartialMessageMetadata()!.GetFragmentId(), Is.EqualTo("fragment@example.test"));
    }

    [Test]
    public void EarlierPartialFragmentsMayOmitTheTotal()
    {
        const string source = "Content-Type: message/partial; number=1; id=group\r\n\r\nfragment";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));
        EmailPartialMessageMetadata metadata = result.GetEntity()!.GetPartialMessageMetadata()!;

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.PartialMessageMalformed), Is.False);
        Assert.That(metadata!.HasRequiredMetadata(), Is.True);
        Assert.That(metadata.HasTotalFragments(), Is.False);
        Assert.That(metadata.IsSequenceConsistent(), Is.True);
    }

    [Test]
    public void MalformedPartialParametersProduceModeSensitiveIssuesAndRetainBytes()
    {
        const string source = "Content-Type: message/partial; number=3; total=2\r\nContent-Transfer-Encoding: 8bit\r\n\r\nretained fragment";

        EmailEntityParseResult tolerant = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        EmailEntityParseResult strict = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreateStrict());
        EmailPartialMessageMetadata metadata = tolerant.GetEntity()!.GetPartialMessageMetadata()!;

        Assert.That(tolerant.IsComplete(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.PartialMessageMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.GetIssues().ContainsCode(EmailIssueCode.PartialMessageMalformed), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(metadata!.HasFragmentId(), Is.False);
        Assert.That(metadata.IsSequenceConsistent(), Is.False);
        Assert.That(Text(tolerant.GetEntity()!.GetRawContent()!.Materialize(100)), Is.EqualTo("retained fragment"));
    }

    [Test]
    public void ParsesUrlExternalBodyMetadataHeadersAndPhantomBodyWithoutRetrieval()
    {
        const string body = "Content-Type: application/pdf\r\nContent-ID: <remote@example.test>\r\n\r\nTHIS IS ONLY A PHANTOM BODY";
        const string source = "Content-Type: message/external-body; access-type=URL;\r\n URL=\"https://example.test/long/ path/file.pdf\"; size=1234;\r\n expiration=\"Fri, 14 Jun 1991 19:13:14 -0400 (EDT)\"; permission=read-write; x-retained=yes\r\n\r\n" + body;

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));
        MimeEntity entity = result.GetEntity()!;
        EmailExternalBodyMetadata metadata = entity!.GetExternalBodyMetadata()!;

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(entity.GetKind(), Is.EqualTo(MimeEntityKind.ExternalBody));
        Assert.That(entity.HasExternalBodyMetadata(), Is.True);
        Assert.That(metadata!.GetNormalizedAccessType(), Is.EqualTo("url"));
        Assert.That(metadata.HasValidAccessType(), Is.True);
        Assert.That(metadata.GetUrl(), Is.EqualTo("https://example.test/long/path/file.pdf"));
        Assert.That(metadata.GetSize(), Is.EqualTo(1234));
        Assert.That(metadata.GetEffectivePermission(), Is.EqualTo("read-write"));
        Assert.That(metadata.HasExpiration(), Is.True);
        Assert.That(metadata.GetExpiration()!.GetYear(), Is.EqualTo(1991));
        Assert.That(metadata.GetParameters().GetFirst("x-retained"), Is.Not.Null);
        Assert.That(metadata.AreReferencedHeadersComplete(), Is.True);
        Assert.That(metadata.GetReferencedHeaders().Contains("Content-Type"), Is.True);
        Assert.That(metadata.GetReferencedHeaders().Contains("Content-ID"), Is.True);
        Assert.That(metadata.GetPhantomBodyRange()!.GetStartOffset(), Is.EqualTo(source.IndexOf("THIS IS ONLY", StringComparison.Ordinal)));
        Assert.That(metadata.GetPhantomBodyRange()!.GetLength(), Is.EqualTo(Bytes("THIS IS ONLY A PHANTOM BODY").Length));
        Assert.That(entity.HasNestedMessage(), Is.False);
        Assert.That(entity.GetChildCount(), Is.Zero);
        Assert.That(Text(entity.GetRawContent()!.Materialize(1000)), Is.EqualTo(body));
        Assert.That(entity.Clone().GetExternalBodyMetadata()!.GetUrl(), Is.EqualTo("https://example.test/long/path/file.pdf"));
    }

    [Test]
    public void ParsesMailServerReferenceAndRetainsItsCommandAsPhantomBytes()
    {
        const string body = "Content-Type: application/octet-stream\r\nContent-ID: <job@example.test>\r\n\r\nSEND FILE 42\r\nWITH OPTIONS";
        const string source = "Content-Type: message/external-body; access-type=mail-server; server=\"files@example.test\"; subject=fetch\r\n\r\n" + body;

        MimeEntity entity = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source)).GetEntity()!;
        EmailExternalBodyMetadata metadata = entity!.GetExternalBodyMetadata()!;

        Assert.That(metadata!.GetServer(), Is.EqualTo("files@example.test"));
        Assert.That(metadata.GetSubject(), Is.EqualTo("fetch"));
        Assert.That(metadata.HasPhantomBodyRange(), Is.True);
        Assert.That(Text(entity.GetRawContent()!.Materialize(1000)), Does.EndWith("SEND FILE 42\r\nWITH OPTIONS"));
        Assert.That(entity.HasNestedMessage(), Is.False);
    }

    [Test]
    public void MissingExternalReferenceRequirementsAreReportedWithoutDiscardingExtensions()
    {
        const string source = "Content-Type: message/external-body; access-type=ftp; name=file.bin; permission=unknown; x-method=value\r\nContent-Transfer-Encoding: binary\r\n\r\nContent-Type: application/octet-stream\r\n\r\nphantom";

        EmailEntityParseResult tolerant = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        EmailEntityParseResult strict = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreateStrict());
        EmailExternalBodyMetadata metadata = tolerant.GetEntity()!.GetExternalBodyMetadata()!;

        Assert.That(tolerant.IsComplete(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(EmailIssueCode.ExternalBodyMalformed), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(metadata!.HasKnownPermission(), Is.False);
        Assert.That(metadata.GetEffectivePermission(), Is.EqualTo("read"));
        Assert.That(metadata.GetParameters().GetFirst("x-method"), Is.Not.Null);
        Assert.That(metadata.GetReferencedHeaders().Contains("Content-Type"), Is.True);
        Assert.That(Text(tolerant.GetEntity()!.GetRawContent()!.Materialize(1000)), Does.EndWith("phantom"));
    }

    [Test]
    public void IncompleteExternalBodyHeadersRetainPartialMetadataAndMarkTheParseIncomplete()
    {
        const string source = "Content-Type: message/external-body; access-type=local-file; name=/tmp/reference\r\n\r\nContent-Type: text/plain\r\nContent-ID: <partial@example.test>\r\n";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Bytes(source));
        EmailExternalBodyMetadata metadata = result.GetEntity()!.GetExternalBodyMetadata()!;

        Assert.That(result.IsComplete(), Is.False);
        Assert.That(result.HasEntity(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderBlockIncomplete), Is.True);
        Assert.That(metadata!.AreReferencedHeadersComplete(), Is.False);
        Assert.That(metadata.GetReferencedHeaders().Contains("Content-ID"), Is.True);
        Assert.That(metadata.HasPhantomBodyRange(), Is.False);
        Assert.That(Text(result.GetEntity()!.GetRawContent()!.Materialize(1000)), Does.EndWith("<partial@example.test>\r\n"));
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
