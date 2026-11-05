using System.Security.Cryptography;
using System.Text;
using email;

namespace email.Tests;

public class EmailBodyIngestParserTests
{
    [Test]
    public void StorePolicyStreamsASeekableMessageBodyIntoTemporaryStorage()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            EmailParseOptions options = new EmailParseOptionsOperations().CreateConformingTolerant();
            EmailBodyIngestOptions ingest = new EmailBodyIngestOptions();
            ingest.SetStorageMode(EmailBodyStorageMode.Store);
            options.SetBodyIngestOptions(ingest);
            EmailStorageOptions storage = options.GetStorageOptions();
            storage.SetTemporaryDirectory(directory);
            storage.GetLimits().SetMemoryThresholdBytes(4);
            options.SetStorageOptions(storage);
            MemoryStream source = new MemoryStream(Bytes("Content-Type: application/octet-stream\r\n\r\nstreamed-body"));

            EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingStreamAndEmailParseOptions(source, options);
            MimeEntity body = result.GetMessage()!.GetBody()!;

            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetMessage()!.HasSourceInfo(), Is.False);
            Assert.That(body!.GetRawContent()!.GetKind(), Is.EqualTo(EmailContentKind.TemporaryFile));
            Assert.That(Text(body.GetRawContent()!.Materialize(100)), Is.EqualTo("streamed-body"));
            Assert.That(body.GetBodyIngestMetadata()!.GetStorageMode(), Is.EqualTo(EmailBodyStorageMode.Store));
            Assert.That(body.GetBodyIngestMetadata()!.GetRawByteCount(), Is.EqualTo(13));
            string path = body.GetRawContent()!.GetFilePath()!;
            Assert.That(File.Exists(path), Is.True);

            body.GetRawContent()!.Close();
            Assert.That(File.Exists(path), Is.False);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DecodeOnIngestStoresDecodedBase64AndHashesRawAndDecodedBytesInOnePass()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailBodyIngestOptions ingest = new EmailBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        ingest.SetComputeRawHash(true);
        ingest.SetComputeDecodedHash(true);
        ingest.SetHashAlgorithm(EmailHashAlgorithm.Sha384);
        options.SetBodyIngestOptions(ingest);

        MimeEntity entity = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("Content-Transfer-Encoding: base64\r\n\r\nSGVsbG8h"), options).GetEntity()!;
        EmailBodyIngestMetadata metadata = entity!.GetBodyIngestMetadata()!;
        EmailHashResult expectedRaw = new EmailHash().ComputeExactWithContentAndAlgorithm(Bytes("SGVsbG8h"), EmailHashAlgorithm.Sha384);
        EmailHashResult expectedDecoded = new EmailHash().ComputeExactWithContentAndAlgorithm(Bytes("Hello!"), EmailHashAlgorithm.Sha384);

        Assert.That(entity.GetRawContent()!.GetKind(), Is.EqualTo(EmailContentKind.Slice));
        Assert.That(Text(entity.GetRawContent()!.Materialize(100)), Is.EqualTo("SGVsbG8h"));
        Assert.That(Text(entity.GetDecodedContent()!.Materialize(100)), Is.EqualTo("Hello!"));
        Assert.That(metadata!.WasDecodingAttempted(), Is.True);
        Assert.That(metadata.IsDecodingComplete(), Is.True);
        Assert.That(metadata.GetRawByteCount(), Is.EqualTo(8));
        Assert.That(metadata.GetDecodedByteCount(), Is.EqualTo(6));
        Assert.That(metadata.HasRawHash(), Is.True);
        Assert.That(metadata.GetRawHash()!.GetLowercaseHex(), Is.EqualTo(expectedRaw.GetLowercaseHex()));
        Assert.That(metadata.GetDecodedHash()!.GetLowercaseHex(), Is.EqualTo(expectedDecoded.GetLowercaseHex()));
    }

    [Test]
    public void DiscardPolicyRetainsNoBodyButCanHashDecodedQuotedPrintable()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailBodyIngestOptions ingest = new EmailBodyIngestOptions();
        ingest.SetStorageMode(EmailBodyStorageMode.Discard);
        ingest.SetDecodeOnIngest(true);
        ingest.SetComputeRawHash(true);
        ingest.SetComputeDecodedHash(true);
        options.SetBodyIngestOptions(ingest);

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("Content-Transfer-Encoding: quoted-printable\r\n\r\nA=20B"), options);
        MimeEntity entity = result.GetEntity()!;
        EmailBodyIngestMetadata metadata = entity!.GetBodyIngestMetadata()!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(entity.HasContent(), Is.False);
        Assert.That(entity.HasRawContent(), Is.False);
        Assert.That(entity.HasDecodedContent(), Is.False);
        Assert.That(metadata!.GetStorageMode(), Is.EqualTo(EmailBodyStorageMode.Discard));
        Assert.That(metadata.GetRawByteCount(), Is.EqualTo(5));
        Assert.That(metadata.GetDecodedByteCount(), Is.EqualTo(3));
        Assert.That(metadata.GetRawHash()!.GetLowercaseHex(), Is.EqualTo(Sha256("A=20B")));
        Assert.That(metadata.GetDecodedHash()!.GetLowercaseHex(), Is.EqualTo(Sha256("A B")));
    }

    [Test]
    public void UnknownTransferEncodingSkipsDecodeAndReportsThePolicyOutcome()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailBodyIngestOptions ingest = new EmailBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        ingest.SetComputeDecodedHash(true);
        options.SetBodyIngestOptions(ingest);

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("Content-Transfer-Encoding: x-rot13\r\n\r\npayload"), options);
        MimeEntity entity = result.GetEntity()!;
        EmailBodyIngestMetadata metadata = entity!.GetBodyIngestMetadata()!;

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TransferEncodingUnsupported), Is.True);
        Assert.That(Text(entity.GetRawContent()!.Materialize(100)), Is.EqualTo("payload"));
        Assert.That(entity.HasDecodedContent(), Is.False);
        Assert.That(metadata!.WasDecodingAttempted(), Is.True);
        Assert.That(metadata.IsDecodingComplete(), Is.False);
        Assert.That(metadata.HasDecodedHash(), Is.False);
    }

    [Test]
    public void DecodedByteLimitRetainsOnlyAValidPrefixAndMakesTheResultUnsuccessful()
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailBodyIngestOptions ingest = new EmailBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        ingest.SetComputeDecodedHash(true);
        options.SetBodyIngestOptions(ingest);
        EmailLimits limits = options.GetLimits();
        limits.GetDecodingLimits().SetMaxDecodedBytes(4);
        options.SetLimits(limits);

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("Content-Transfer-Encoding: base64\r\n\r\nQUJDREU="), options);
        MimeEntity entity = result.GetEntity()!;
        EmailBodyIngestMetadata metadata = entity!.GetBodyIngestMetadata()!;

        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.DecodedByteLimitExceeded), Is.True);
        Assert.That(Text(entity.GetDecodedContent()!.Materialize(100)), Is.EqualTo("ABC"));
        Assert.That(metadata!.GetDecodedByteCount(), Is.EqualTo(3));
        Assert.That(metadata.IsDecodingComplete(), Is.False);
        Assert.That(metadata.GetDecodedHash()!.GetHashedByteCount(), Is.EqualTo(3));
    }

    [Test]
    public void OversizeStoredBodyFailsBeforeSpoolingAndLeavesNoTemporaryFile()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
            EmailBodyIngestOptions ingest = options.GetBodyIngestOptions();
            ingest.SetStorageMode(EmailBodyStorageMode.Store);
            options.SetBodyIngestOptions(ingest);
            EmailStorageOptions storage = options.GetStorageOptions();
            storage.SetTemporaryDirectory(directory);
            storage.SetCopyBufferBytes(3);
            storage.GetLimits().SetMemoryThresholdBytes(1);
            storage.GetLimits().SetMaxTemporaryBytes(5);
            options.SetStorageOptions(storage);

            EmailLimitException exception = Assert.Throws<EmailLimitException>(() => new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes("\r\n0123456789"), options));

            Assert.That(exception.GetLimitName(), Is.EqualTo("MaxTemporaryBytes"));
            Assert.That(Directory.GetFiles(directory), Is.Empty);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ParseOptionsCloneBodyIngestConfiguration()
    {
        EmailParseOptions options = new EmailParseOptions();
        EmailBodyIngestOptions ingest = new EmailBodyIngestOptions();
        ingest.SetStorageMode(EmailBodyStorageMode.Store);
        ingest.SetDecodeOnIngest(true);
        ingest.SetComputeRawHash(true);
        ingest.SetComputeDecodedHash(true);
        ingest.SetHashAlgorithm(EmailHashAlgorithm.Sha512);
        options.SetBodyIngestOptions(ingest);

        EmailParseOptions clone = options.Clone();
        ingest.SetStorageMode(EmailBodyStorageMode.Discard);

        Assert.That(clone.GetBodyIngestOptions().GetStorageMode(), Is.EqualTo(EmailBodyStorageMode.Store));
        Assert.That(clone.GetBodyIngestOptions().GetDecodeOnIngest(), Is.True);
        Assert.That(clone.GetBodyIngestOptions().GetComputeRawHash(), Is.True);
        Assert.That(clone.GetBodyIngestOptions().GetComputeDecodedHash(), Is.True);
        Assert.That(clone.GetBodyIngestOptions().GetHashAlgorithm(), Is.EqualTo(EmailHashAlgorithm.Sha512));
    }

    static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "email-ingest-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Bytes(value))).ToLowerInvariant();
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
