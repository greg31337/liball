using System.Text;
using email;

namespace email.Tests;

public class EmailInvariantPropertyTests
{
    [Test]
    public void PreservingParseAndExactWriteAreIdentityForGeneratedByteSequences()
    {
        uint state = 0x91e10da5;
        int caseIndex = 0;
        while (caseIndex < 96)
        {
            int length = caseIndex < 24 ? caseIndex : NextInt(ref state, 257);
            byte[] source = RandomBytes(ref state, length);
            EmailParseResult parsed = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving());

            Assert.That(parsed.HasMessage(), Is.True, "case " + caseIndex + ", length " + length);
            EmailWriteResult written = new EmailWriter().WriteMessageWithMessageAndOptions(parsed.GetMessage()!, new EmailWriteOptionsOperations().CreateExactPreservation());

            Assert.That(written.IsSuccess(), Is.True, "case " + caseIndex + ", length " + length);
            Assert.That(written.GetBytes(), Is.EqualTo(source), "case " + caseIndex + ", length " + length);
            caseIndex++;
        }
    }

    [Test]
    public void CanonicalWriteParseWriteIsByteAndSemanticallyStableForGeneratedMessages()
    {
        uint state = 0xc0ffee11;
        int caseIndex = 0;
        while (caseIndex < 32)
        {
            EmailMessage source = GeneratedMessage(ref state, caseIndex);
            EmailWriteOptions options = DeterministicOptions("canonical-property-" + caseIndex);

            EmailWriteResult first = new EmailWriter().WriteMessageWithMessageAndOptions(source, options);
            Assert.That(first.IsSuccess(), Is.True, "first write, case " + caseIndex);
            EmailParseResult firstParse = ParseDecoded(first.GetBytes());
            Assert.That(firstParse.IsComplete(), Is.True, "first parse, case " + caseIndex);

            EmailWriteResult second = new EmailWriter().WriteMessageWithMessageAndOptions(firstParse.GetMessage()!, options);
            Assert.That(second.IsSuccess(), Is.True, "second write, case " + caseIndex);
            Assert.That(second.GetBytes(), Is.EqualTo(first.GetBytes()), "canonical bytes, case " + caseIndex);

            EmailParseResult secondParse = ParseDecoded(second.GetBytes());
            Assert.That(SemanticFingerprint(secondParse.GetMessage()!), Is.EqualTo(SemanticFingerprint(firstParse.GetMessage()!)), "semantics, case " + caseIndex);
            caseIndex++;
        }
    }

    [Test]
    public void TransferCodecEncodeDecodeAreInversesAtEveryFeedBoundary()
    {
        uint state = 0x5eed1234;
        int caseIndex = 0;
        while (caseIndex < 32)
        {
            int length = caseIndex < 16 ? caseIndex : NextInt(ref state, 97);
            byte[] source = RandomBytes(ref state, length);
            int base64LineLength = 4 * (1 + NextInt(ref state, 19));
            int quotedPrintableLineLength = 4 + NextInt(ref state, 73);

            AssertBase64InverseAtEveryBoundary(source, base64LineLength, caseIndex);
            AssertQuotedPrintableInverseAtEveryBoundary(source, quotedPrintableLineLength, caseIndex);
            caseIndex++;
        }
    }

    [Test]
    public void FeedAndTokenReadersAreInvariantAtEveryChunkBoundary()
    {
        byte[] source = Bytes(
            "Subject: boundary property\r\n" +
            "Content-Type: multipart/mixed; boundary=edge\r\n\r\n" +
            "pre\r\n--edge\r\nContent-Type: text/plain\r\n\r\nalpha-beta\r\n" +
            "--edge\r\nContent-Type: message/rfc822\r\n\r\nSubject: nested\r\n\r\nnested-body\r\n" +
            "--edge--\r\nepi");
        EmailParseOptions preserving = new EmailParseOptionsOperations().CreatePreserving();
        EmailParseResult whole = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, preserving);
        byte[] expectedCanonical = new EmailWriter().ToArrayWithMessageAndOptions(whole.GetMessage()!, DeterministicOptions("feed-boundaries"));

        int split = 0;
        while (split <= source.Length)
        {
            EmailFeedParser feed = new EmailFeedParserOperations().CreateWithOptions(preserving);
            feed.Feed(new ReadOnlySpan<byte>(source, 0, split));
            feed.Feed(new ReadOnlySpan<byte>(source, split, source.Length - split));
            EmailParseResult actual = feed.Complete();

            Assert.That(actual.IsComplete(), Is.EqualTo(whole.IsComplete()), "feed split " + split);
            Assert.That(new EmailWriter().ToArrayWithMessageAndOptions(actual.GetMessage()!, DeterministicOptions("feed-boundaries")), Is.EqualTo(expectedCanonical), "feed split " + split);
            feed.Close();
            split++;
        }

        string expectedTokens = TokenFingerprint(source, source.Length + 1);
        int chunkBytes = 1;
        while (chunkBytes <= source.Length + 1)
        {
            Assert.That(TokenFingerprint(source, chunkBytes), Is.EqualTo(expectedTokens), "token chunk bytes " + chunkBytes);
            chunkBytes++;
        }
    }

    static void AssertBase64InverseAtEveryBoundary(byte[] source, int lineLength, int caseIndex)
    {
        byte[] encoded = new EmailBase64().EncodeWithInputAndLineLength(source, lineLength);
        int split = 0;
        while (split <= source.Length)
        {
            EmailBase64Encoder encoder = new EmailBase64Encoder(lineLength);
            byte[] first = encoder.Feed(new ReadOnlySpan<byte>(source, 0, split));
            byte[] second = encoder.Feed(new ReadOnlySpan<byte>(source, split, source.Length - split));
            byte[] tail = encoder.Complete();
            Assert.That(Combine(first, second, tail), Is.EqualTo(encoded), "base64 encode case " + caseIndex + ", split " + split);
            split++;
        }

        split = 0;
        while (split <= encoded.Length)
        {
            EmailBase64Decoder decoder = new EmailBase64Decoder(EmailBase64DecodeMode.Strict, source.Length);
            byte[] first = decoder.Feed(new ReadOnlySpan<byte>(encoded, 0, split));
            byte[] second = decoder.Feed(new ReadOnlySpan<byte>(encoded, split, encoded.Length - split));
            byte[] tail = decoder.Complete();
            Assert.That(Combine(first, second, tail), Is.EqualTo(source), "base64 decode case " + caseIndex + ", split " + split);
            split++;
        }
    }

    static void AssertQuotedPrintableInverseAtEveryBoundary(byte[] source, int lineLength, int caseIndex)
    {
        byte[] encoded = new EmailQuotedPrintable().EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(source, lineLength, EmailQuotedPrintableNewlinePolicy.Encode, true);
        int split = 0;
        while (split <= source.Length)
        {
            EmailQuotedPrintableEncoder encoder = new EmailQuotedPrintableEncoder(lineLength, EmailQuotedPrintableNewlinePolicy.Encode, true);
            byte[] first = encoder.Feed(new ReadOnlySpan<byte>(source, 0, split));
            byte[] second = encoder.Feed(new ReadOnlySpan<byte>(source, split, source.Length - split));
            byte[] tail = encoder.Complete();
            Assert.That(Combine(first, second, tail), Is.EqualTo(encoded), "quoted-printable encode case " + caseIndex + ", split " + split);
            split++;
        }

        split = 0;
        while (split <= encoded.Length)
        {
            EmailQuotedPrintableDecoder decoder = new EmailQuotedPrintableDecoder(EmailQuotedPrintableDecodeMode.Strict, source.Length);
            byte[] first = decoder.Feed(new ReadOnlySpan<byte>(encoded, 0, split));
            byte[] second = decoder.Feed(new ReadOnlySpan<byte>(encoded, split, encoded.Length - split));
            byte[] tail = decoder.Complete();
            Assert.That(Combine(first, second, tail), Is.EqualTo(source), "quoted-printable decode case " + caseIndex + ", split " + split);
            split++;
        }
    }

    static EmailMessage GeneratedMessage(ref uint state, int caseIndex)
    {
        EmailHeaders headers = new EmailHeadersOperations().CreateWith();
        headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Subject", Bytes(" property " + caseIndex + " café " + NextInt(ref state, 10000)), EmailHeaderValueKind.Unstructured));
        EmailMessage message = new EmailMessageOperations().CreateWithHeaders(headers);
        MimeEntity body;
        if (caseIndex % 3 == 0)
        {
            body = new EmailEntityFactory().CreateTextWithValue(RandomText(ref state, 1 + NextInt(ref state, 80)));
        }
        else if (caseIndex % 3 == 1)
        {
            body = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("mixed", new List<MimeEntity>()
            {
                new EmailEntityFactory().CreateTextWithValue(RandomText(ref state, 1 + NextInt(ref state, 50))),
                new EmailEntityFactory().CreateBinaryWithValue(RandomBytes(ref state, NextInt(ref state, 65)))
            });
        }
        else
        {
            EmailMessage nested = new EmailMessageOperations().CreateWith();
            nested.SetBody(new EmailEntityFactory().CreateTextWithValue(RandomText(ref state, 1 + NextInt(ref state, 40))));
            body = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("alternative", new List<MimeEntity>()
            {
                new EmailEntityFactory().CreateTextWithValue(RandomText(ref state, 1 + NextInt(ref state, 50))),
                new EmailEntityFactory().CreateNestedMessage(nested)
            });
        }

        message.SetBody(body);
        return message;
    }

    static EmailParseResult ParseDecoded(byte[] source)
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailBodyIngestOptions ingest = options.GetBodyIngestOptions();
        ingest.SetDecodeOnIngest(true);
        options.SetBodyIngestOptions(ingest);
        return new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, options);
    }

    static string SemanticFingerprint(EmailMessage message)
    {
        StringBuilder output = new StringBuilder();
        EmailHeader subject = message.GetHeaders()!.GetFirst("Subject")!;
        output.Append(subject == null ? "" : subject.GetDecodedValue()).Append('|');
        if (message.HasBody())
        {
            AppendEntityFingerprint(output, message.GetBody()!);
        }

        return output.ToString();
    }

    static void AppendEntityFingerprint(StringBuilder output, MimeEntity entity)
    {
        output.Append('(').Append(entity.GetMediaType()!.GetTopLevelType()).Append('/').Append(entity.GetMediaType()!.GetSubtype());
        if (entity.HasDecodedContent())
        {
            output.Append(':').Append(Convert.ToBase64String(entity.GetDecodedContent()!.Materialize(1000000)));
        }
        else if (entity.HasRawContent())
        {
            output.Append(':').Append(Convert.ToBase64String(entity.GetRawContent()!.Materialize(1000000)));
        }

        int index = 0;
        while (index < entity.GetChildCount())
        {
            AppendEntityFingerprint(output, entity.GetChildAt(index));
            index++;
        }

        if (entity.HasNestedMessage())
        {
            output.Append('{').Append(SemanticFingerprint(entity.GetNestedMessage()!)).Append('}');
        }

        output.Append(')');
    }

    static string TokenFingerprint(byte[] source, int chunkBytes)
    {
        EmailParseOptions options = new EmailParseOptionsOperations().CreatePreserving();
        EmailStorageOptions storage = options.GetStorageOptions();
        storage.SetCopyBufferBytes(chunkBytes);
        options.SetStorageOptions(storage);
        EmailTokenReader reader = new EmailTokenReaderOperations().CreateWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, options);
        StringBuilder output = new StringBuilder();
        MemoryStream body = null!;
        string bodyPrefix = null!;
        while (reader.Read())
        {
            if (reader.GetTokenKind() == EmailTokenKind.BodyChunk)
            {
                string prefix = reader.GetDepth() + ":" + reader.GetPath().GetDisplayText() + ":" + reader.GetBodyChunkKind();
                if (body == null)
                {
                    body = new MemoryStream();
                    bodyPrefix = prefix;
                }

                Assert.That(prefix, Is.EqualTo(bodyPrefix));
                reader.OpenBodyChunk().CopyTo(body);
            }
            else
            {
                FlushBody(output, bodyPrefix!, body!);
                body = null!;
                bodyPrefix = null!;
                output.Append(reader.GetTokenKind()).Append(':').Append(reader.GetDepth()).Append(':').Append(reader.GetPath().GetDisplayText());
                if (reader.GetTokenKind() == EmailTokenKind.Error)
                {
                    output.Append(':').Append(reader.GetIssue()!.GetCode());
                }

                output.Append('|');
            }
        }

        FlushBody(output, bodyPrefix!, body!);
        reader.Close();
        return output.ToString();
    }

    static void FlushBody(StringBuilder output, string prefix, MemoryStream body)
    {
        if (body != null)
        {
            output.Append("Body:").Append(prefix).Append(':').Append(Convert.ToBase64String(body.ToArray())).Append('|');
            body.Close();
        }
    }

    static EmailWriteOptions DeterministicOptions(string seed)
    {
        EmailWriteOptions options = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        options.SetDeterministicOutput(true);
        options.SetDeterministicSeed(Bytes(seed));
        return options;
    }

    static byte[] Combine(params byte[][] chunks)
    {
        int length = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            length += chunks[index].Length;
            index++;
        }

        byte[] combined = new byte[length];
        int offset = 0;
        index = 0;
        while (index < chunks.Length)
        {
            chunks[index].CopyTo(combined, offset);
            offset += chunks[index].Length;
            index++;
        }

        return combined;
    }

    static byte[] RandomBytes(ref uint state, int length)
    {
        byte[] value = new byte[length];
        int index = 0;
        while (index < value.Length)
        {
            value[index] = (byte)Next(ref state);
            index++;
        }

        return value;
    }

    static string RandomText(ref uint state, int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789\r\n\té";
        StringBuilder value = new StringBuilder(length);
        int index = 0;
        while (index < length)
        {
            value.Append(alphabet[NextInt(ref state, alphabet.Length)]);
            index++;
        }

        return value.ToString();
    }

    static int NextInt(ref uint state, int maximum)
    {
        return (int)(Next(ref state) % (uint)maximum);
    }

    static uint Next(ref uint state)
    {
        state = (uint)(((ulong)state * 1664525 + 1013904223) & uint.MaxValue);
        return state;
    }

    static byte[] Bytes(string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }
}
