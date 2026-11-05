using System.IO;
using System.IO.Compression;
using System.Text;

namespace http.Tests;

public class HttpContentCodingTests
{
    [Test]
    public void IdentityReturnsAnOwnedCopyAndHonorsTheOutputLimit()
    {
        IHttpContentCoding coding = new HttpContentCoding();
        byte[] content = new byte[] { 1, 2, 3 };

        HttpContentCodingResult encoded = coding.EncodeContent(
            content,
            HttpContentCodings.Identity,
            3);
        HttpContentCodingResult limited = coding.DecodeContent(
            content,
            HttpContentCodings.Identity,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Outcome, Is.EqualTo(HttpContentCodingOutcome.Identity));
            Assert.That(encoded.Content, Is.EqualTo(content));
            Assert.That(encoded.Content, Is.Not.SameAs(content));
            Assert.That(encoded.AppliedCodings,
                Is.EqualTo(new[] { HttpContentCodings.Identity }));
            Assert.That(limited.Outcome,
                Is.EqualTo(HttpContentCodingOutcome.LimitExceeded));
            Assert.That(limited.RemainingCodings,
                Is.EqualTo(new[] { HttpContentCodings.Identity }));
        });
    }

    [Test]
    public void GzipDeflateAndBrotliRoundTripEmptyAndTextContent()
    {
        IHttpContentCoding service = new HttpContentCoding();
        string[] codings =
        {
            HttpContentCodings.Gzip,
            HttpContentCodings.Deflate,
            HttpContentCodings.Br
        };
        byte[][] inputs =
        {
            Array.Empty<byte>(),
            Encoding.UTF8.GetBytes("content coding round trip content coding round trip")
        };

        Assert.Multiple(() =>
        {
            foreach (string coding in codings)
            {
                foreach (byte[] input in inputs)
                {
                    HttpContentCodingResult encoded = service.EncodeContent(
                        input,
                        coding,
                        4096);
                    HttpContentCodingResult decoded = service.DecodeContent(
                        encoded.Content,
                        coding,
                        4096);

                    Assert.That(encoded.Outcome,
                        Is.EqualTo(HttpContentCodingOutcome.Encoded), coding);
                    Assert.That(decoded.Outcome,
                        Is.EqualTo(HttpContentCodingOutcome.Decoded), coding);
                    Assert.That(decoded.Content, Is.EqualTo(input), coding);
                    Assert.That(encoded.AppliedCodings, Is.EqualTo(new[] { coding }), coding);
                    Assert.That(decoded.AppliedCodings, Is.EqualTo(new[] { coding }), coding);
                }
            }
        });
    }

    [Test]
    public void EmptyCompressedInputIsMalformedForEveryTransformCoding()
    {
        IHttpContentCoding service = new HttpContentCoding();
        string[] codings =
        {
            HttpContentCodings.Gzip,
            HttpContentCodings.Deflate,
            HttpContentCodings.Br
        };

        Assert.Multiple(() =>
        {
            foreach (string coding in codings)
            {
                HttpContentCodingResult result = service.DecodeContent(
                    Array.Empty<byte>(),
                    coding,
                    1024);
                Assert.That(result.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.Malformed), coding);
                Assert.That(result.RemainingCodings,
                    Is.EqualTo(new[] { coding }), coding);
            }
        });
    }

    [Test]
    public void DeflateUsesTheRequiredZlibWrapperAndGzipAliasIsSupported()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] content = Encoding.ASCII.GetBytes("wrapped deflate data");

        HttpContentCodingResult deflate = service.EncodeContent(
            content,
            HttpContentCodings.Deflate,
            1024);
        HttpContentCodingResult gzip = service.EncodeContent(
            content,
            HttpContentCodings.XGzip,
            1024);
        HttpContentCodingResult decoded = service.DecodeContent(
            gzip.Content,
            HttpContentCodings.XGzip,
            1024);

        Assert.Multiple(() =>
        {
            Assert.That(deflate.Content[0], Is.EqualTo(0x78));
            Assert.That(gzip.Outcome, Is.EqualTo(HttpContentCodingOutcome.Encoded));
            Assert.That(decoded.Content, Is.EqualTo(content));
        });
    }

    [Test]
    public void CodingStacksEncodeInOrderAndDecodeInReverseOrder()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] content = Encoding.UTF8.GetBytes(
            "stacked representation data stacked representation data");
        List<string> codings = new List<string>
        {
            HttpContentCodings.Gzip,
            HttpContentCodings.Br
        };

        HttpContentCodingResult gzip = service.EncodeContent(
            content,
            HttpContentCodings.Gzip,
            4096);
        HttpContentCodingResult manual = service.EncodeContent(
            gzip.Content,
            HttpContentCodings.Br,
            4096);
        HttpContentCodingResult encoded = service.EncodeCodingStack(content, codings, 4096);
        HttpContentCodingResult decoded = service.DecodeCodingStack(
            encoded.Content,
            codings,
            4096);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Outcome, Is.EqualTo(HttpContentCodingOutcome.Encoded));
            Assert.That(encoded.Content, Is.EqualTo(manual.Content));
            Assert.That(encoded.AppliedCodings,
                Is.EqualTo(new[] { HttpContentCodings.Gzip, HttpContentCodings.Br }));
            Assert.That(decoded.Outcome, Is.EqualTo(HttpContentCodingOutcome.Decoded));
            Assert.That(decoded.Content, Is.EqualTo(content));
            Assert.That(decoded.AppliedCodings,
                Is.EqualTo(new[] { HttpContentCodings.Br, HttpContentCodings.Gzip }));
        });
    }

    [Test]
    public void UnknownCodingsReturnExactStackProgress()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] content = Encoding.ASCII.GetBytes("unknown coding progress");
        List<string> encodeStack = new List<string>
        {
            HttpContentCodings.Gzip,
            "example",
            HttpContentCodings.Br
        };
        HttpContentCodingResult expectedGzip = service.EncodeContent(
            content,
            HttpContentCodings.Gzip,
            4096);

        HttpContentCodingResult encoded = service.EncodeCodingStack(
            content,
            encodeStack,
            4096);
        HttpContentCodingResult br = service.EncodeContent(
            content,
            HttpContentCodings.Br,
            4096);
        HttpContentCodingResult decoded = service.DecodeCodingStack(
            br.Content,
            new List<string> { "example", HttpContentCodings.Br },
            4096);
        HttpContentCodingResult obsolete = service.DecodeContent(
            content,
            HttpContentCodings.Compress,
            4096);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Outcome, Is.EqualTo(HttpContentCodingOutcome.Unknown));
            Assert.That(encoded.Content, Is.EqualTo(expectedGzip.Content));
            Assert.That(encoded.AppliedCodings,
                Is.EqualTo(new[] { HttpContentCodings.Gzip }));
            Assert.That(encoded.RemainingCodings,
                Is.EqualTo(new[] { "example", HttpContentCodings.Br }));
            Assert.That(decoded.Outcome, Is.EqualTo(HttpContentCodingOutcome.Unknown));
            Assert.That(decoded.Content, Is.EqualTo(content));
            Assert.That(decoded.AppliedCodings,
                Is.EqualTo(new[] { HttpContentCodings.Br }));
            Assert.That(decoded.RemainingCodings, Is.EqualTo(new[] { "example" }));
            Assert.That(obsolete.Outcome, Is.EqualTo(HttpContentCodingOutcome.Unknown));
            Assert.That(obsolete.Content, Is.EqualTo(content));
            Assert.That(obsolete.Content, Is.Not.SameAs(content));
            Assert.That(obsolete.RemainingCodings,
                Is.EqualTo(new[] { HttpContentCodings.Compress }));
        });
    }

    [Test]
    public void CorruptStreamsReturnMalformedResultsWithoutPartialContent()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] corrupt = new byte[] { 0xff, 0x00, 0xfe, 0x01, 0x02 };
        string[] codings =
        {
            HttpContentCodings.Gzip,
            HttpContentCodings.Deflate,
            HttpContentCodings.Br
        };

        Assert.Multiple(() =>
        {
            foreach (string coding in codings)
            {
                HttpContentCodingResult result = service.DecodeContent(corrupt, coding, 1024);
                Assert.That(result.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.Malformed), coding);
                Assert.That(result.Content, Is.EqualTo(corrupt), coding);
                Assert.That(result.AppliedCodings, Is.Empty, coding);
                Assert.That(result.RemainingCodings, Is.EqualTo(new[] { coding }), coding);
            }
        });
    }

    [Test]
    public void ChecksumFramedStreamsRejectTruncationAtEveryByteBoundary()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] content = Encoding.ASCII.GetBytes(
            "truncation detection requires enough repeated content to compress");
        string[] codings =
        {
            HttpContentCodings.Gzip,
            HttpContentCodings.Deflate
        };

        Assert.Multiple(() =>
        {
            foreach (string coding in codings)
            {
                HttpContentCodingResult encoded = service.EncodeContent(
                    content,
                    coding,
                    4096);
                int length = 0;
                while (length < encoded.Content.Length)
                {
                    byte[] truncated = new byte[length];
                    Array.Copy(encoded.Content, truncated, length);
                    HttpContentCodingResult result = service.DecodeContent(
                        truncated,
                        coding,
                        4096);
                    Assert.That(result.Outcome,
                        Is.EqualTo(HttpContentCodingOutcome.Malformed),
                        coding + " truncated at " + length);
                    Assert.That(result.Content, Is.EqualTo(truncated),
                        coding + " truncated content at " + length);
                    length = length + 1;
                }
            }
        });
    }

    [Test]
    public void EncodingAndDecodingStopAtConfiguredOutputLimits()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] content = new byte[32768];
        int index = 0;
        while (index < content.Length)
        {
            content[index] = (byte)(index % 7);
            index = index + 1;
        }

        string[] codings =
        {
            HttpContentCodings.Gzip,
            HttpContentCodings.Deflate,
            HttpContentCodings.Br
        };

        Assert.Multiple(() =>
        {
            foreach (string coding in codings)
            {
                HttpContentCodingResult encoded = service.EncodeContent(
                    content,
                    coding,
                    4096);
                HttpContentCodingResult exactEncoding = service.EncodeContent(
                    content,
                    coding,
                    encoded.Content.Length);
                HttpContentCodingResult encodeLimit = service.EncodeContent(
                    content,
                    coding,
                    encoded.Content.Length - 1);
                HttpContentCodingResult exactDecoding = service.DecodeContent(
                    encoded.Content,
                    coding,
                    content.Length);
                HttpContentCodingResult decodeLimit = service.DecodeContent(
                    encoded.Content,
                    coding,
                    content.Length - 1);

                Assert.That(encoded.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.Encoded), coding);
                Assert.That(exactEncoding.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.Encoded), coding);
                Assert.That(encodeLimit.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.LimitExceeded), coding);
                Assert.That(exactDecoding.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.Decoded), coding);
                Assert.That(exactDecoding.Content, Is.EqualTo(content), coding);
                Assert.That(decodeLimit.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.LimitExceeded), coding);
                Assert.That(decodeLimit.Content, Is.EqualTo(encoded.Content), coding);
                Assert.That(decodeLimit.AppliedCodings, Is.Empty, coding);
            }
        });
    }

    [Test]
    public void EncodingsInteroperateWithIndependentPlatformStreams()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] content = Encoding.UTF8.GetBytes(
            "independent content-coding interoperability payload");
        string[] codings =
        {
            HttpContentCodings.Gzip,
            HttpContentCodings.Deflate,
            HttpContentCodings.Br
        };

        Assert.Multiple(() =>
        {
            foreach (string coding in codings)
            {
                HttpContentCodingResult encoded = service.EncodeContent(
                    content,
                    coding,
                    4096);
                byte[] decodedExternally = DecodeWithPlatformStream(
                    encoded.Content,
                    coding);
                byte[] encodedExternally = EncodeWithPlatformStream(content, coding);
                HttpContentCodingResult decoded = service.DecodeContent(
                    encodedExternally,
                    coding,
                    4096);

                Assert.That(decodedExternally, Is.EqualTo(content), coding);
                Assert.That(decoded.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.Decoded), coding);
                Assert.That(decoded.Content, Is.EqualTo(content), coding);
            }
        });
    }

    [Test]
    public void IntegrityMetadataCorruptionIsRejectedWithoutPartialOutput()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] content = Encoding.ASCII.GetBytes(
            "integrity metadata must cover the complete decoded representation");
        string[] codings =
        {
            HttpContentCodings.Gzip,
            HttpContentCodings.Deflate
        };

        Assert.Multiple(() =>
        {
            foreach (string coding in codings)
            {
                HttpContentCodingResult encoded = service.EncodeContent(
                    content,
                    coding,
                    4096);
                byte[] corrupt = encoded.Content.ToArray();
                corrupt[corrupt.Length - 1] = (byte)(corrupt[corrupt.Length - 1] ^ 1);

                HttpContentCodingResult result = service.DecodeContent(
                    corrupt,
                    coding,
                    4096);

                Assert.That(result.Outcome,
                    Is.EqualTo(HttpContentCodingOutcome.Malformed), coding);
                Assert.That(result.Content, Is.EqualTo(corrupt), coding);
                Assert.That(result.AppliedCodings, Is.Empty, coding);
                Assert.That(result.RemainingCodings,
                    Is.EqualTo(new[] { coding }), coding);
            }
        });
    }

    [Test]
    public void EmptyCodingStacksAreIdentityOperations()
    {
        IHttpContentCoding service = new HttpContentCoding();
        byte[] content = new byte[] { 4, 5, 6 };

        HttpContentCodingResult encoded = service.EncodeCodingStack(
            content,
            new List<string>(),
            3);
        HttpContentCodingResult decoded = service.DecodeCodingStack(
            content,
            new List<string>(),
            3);
        HttpContentCodingResult limited = service.EncodeCodingStack(
            content,
            new List<string>(),
            2);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Outcome, Is.EqualTo(HttpContentCodingOutcome.Identity));
            Assert.That(decoded.Outcome, Is.EqualTo(HttpContentCodingOutcome.Identity));
            Assert.That(limited.Outcome,
                Is.EqualTo(HttpContentCodingOutcome.LimitExceeded));
            Assert.That(encoded.Content, Is.EqualTo(content));
            Assert.That(encoded.Content, Is.Not.SameAs(content));
            Assert.That(encoded.AppliedCodings, Is.Empty);
            Assert.That(decoded.AppliedCodings, Is.Empty);
        });
    }

    [Test]
    public void InvalidArgumentsThrowStableContentCodingExceptions()
    {
        IHttpContentCoding service = new HttpContentCoding();

        HttpContentCodingException missingContent = Assert.Throws<HttpContentCodingException>(() =>
            service.EncodeContent(null!, HttpContentCodings.Gzip, 10))!;
        HttpContentCodingException invalidToken = Assert.Throws<HttpContentCodingException>(() =>
            service.DecodeContent(Array.Empty<byte>(), "bad coding", 10))!;
        HttpContentCodingException invalidLimit = Assert.Throws<HttpContentCodingException>(() =>
            service.EncodeContent(Array.Empty<byte>(), HttpContentCodings.Gzip, -1))!;
        HttpContentCodingException invalidStack = Assert.Throws<HttpContentCodingException>(() =>
            service.EncodeCodingStack(
                Array.Empty<byte>(),
                new List<string> { HttpContentCodings.Gzip, "bad coding" },
                10))!;

        Assert.Multiple(() =>
        {
            Assert.That(missingContent.DiagnosticCode, Is.EqualTo("invalid-content"));
            Assert.That(invalidToken.DiagnosticCode, Is.EqualTo("invalid-content-coding"));
            Assert.That(invalidLimit.DiagnosticCode, Is.EqualTo("invalid-output-limit"));
            Assert.That(invalidStack.Coding, Is.EqualTo("bad coding"));
        });
    }

    static byte[] EncodeWithPlatformStream(byte[] content, string coding)
    {
        MemoryStream destination = new MemoryStream();
        Stream encoder;
        if (coding == HttpContentCodings.Gzip)
        {
            encoder = new GZipStream(destination, CompressionLevel.SmallestSize, true);
        }
        else if (coding == HttpContentCodings.Deflate)
        {
            encoder = new ZLibStream(destination, CompressionLevel.SmallestSize, true);
        }
        else
        {
            encoder = new BrotliStream(destination, CompressionLevel.SmallestSize, true);
        }

        encoder.Write(content, 0, content.Length);
        encoder.Dispose();
        byte[] result = destination.ToArray();
        destination.Dispose();
        return result;
    }

    static byte[] DecodeWithPlatformStream(byte[] content, string coding)
    {
        MemoryStream source = new MemoryStream(content, false);
        Stream decoder;
        if (coding == HttpContentCodings.Gzip)
        {
            decoder = new GZipStream(source, CompressionMode.Decompress, false);
        }
        else if (coding == HttpContentCodings.Deflate)
        {
            decoder = new ZLibStream(source, CompressionMode.Decompress, false);
        }
        else
        {
            decoder = new BrotliStream(source, CompressionMode.Decompress, false);
        }

        MemoryStream destination = new MemoryStream();
        decoder.CopyTo(destination);
        decoder.Dispose();
        source.Dispose();
        byte[] result = destination.ToArray();
        destination.Dispose();
        return result;
    }
}
