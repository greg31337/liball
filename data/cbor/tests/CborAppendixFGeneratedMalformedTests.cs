using System.Text.Json;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborAppendixFGeneratedMalformedTests
{
    [Test]
    public void EveryAppendixAProperPrefixHasTheExactTooLittleDataFailure()
    {
        List<string> encodings = LoadAppendixAEncodings();
        int truncationPoints = 0;

        foreach (string encoding in encodings)
        {
            byte[] complete = Convert.FromHexString(encoding);
            int length = 0;
            while (length < complete.Length)
            {
                byte[] truncated = complete.AsSpan(0, length).ToArray();
                CborReadResult result = Decode(truncated);

                AssertAtomicFailure(
                    result,
                    "CBOR_UNEXPECTED_END_OF_DATA",
                    length,
                    truncated.Length,
                    $"{encoding} truncated at {length}");
                truncationPoints = truncationPoints + 1;
                length = length + 1;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(encodings, Has.Count.EqualTo(81));
            Assert.That(truncationPoints, Is.EqualTo(507));
        });
    }

    [Test]
    public void EveryAppendixAVectorWithAnExtraItemHasExactTooMuchDataBehavior()
    {
        List<string> encodings = LoadAppendixAEncodings();
        foreach (string encoding in encodings)
        {
            byte[] item = Convert.FromHexString(encoding);
            byte[] source = [.. item, 0x00];
            CborDecoder decoder = new CborDecoder();
            CborReadResult exact = decoder.Decode(source, DecodeOptions());
            CborReadResult first = decoder.DecodeFirst(source, DecodeOptions());
            List<CborReadResult> sequence = decoder.DecodeSequence(
                source,
                DecodeOptions());

            AssertAtomicFailure(
                exact,
                "CBOR_TRAILING_DATA",
                item.Length,
                source.Length,
                encoding);
            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(CborReadStatus.Done), encoding);
                Assert.That(first.Consumed, Is.EqualTo(item.Length), encoding);
                Assert.That(first.Remaining, Is.EqualTo(1), encoding);
                Assert.That(first.ErrorCode, Is.Null, encoding);
                Assert.That(first.ErrorPath, Is.Null, encoding);
                Assert.That(sequence, Has.Count.EqualTo(2), encoding);
                Assert.That(sequence.All(result => result.Status == CborReadStatus.Done),
                    Is.True,
                    encoding);
                Assert.That(sequence[0].Consumed, Is.EqualTo(item.Length), encoding);
                Assert.That(sequence[0].Remaining, Is.EqualTo(1), encoding);
                Assert.That(sequence[1].SourceOffset, Is.EqualTo(item.Length), encoding);
                Assert.That(sequence[1].Consumed, Is.EqualTo(1), encoding);
                Assert.That(sequence[1].Remaining, Is.Zero, encoding);
            });
        }
    }

    [TestCaseSource(nameof(AppendixFSyntaxErrors))]
    public void EveryGeneratedAppendixFSyntaxErrorHasExactPublicDiagnostics(
        MalformedVector vector)
    {
        byte[] source = Convert.FromHexString(vector.Hex);
        CborReadResult result = Decode(source);

        AssertAtomicFailure(
            result,
            vector.ErrorCode,
            vector.ErrorOffset,
            source.Length,
            vector.Name);
    }

    private static IEnumerable<TestCaseData> AppendixFSyntaxErrors()
    {
        for (int majorType = 0; majorType <= 7; majorType++)
        {
            for (int additionalInformation = 28;
                additionalInformation <= 30;
                additionalInformation++)
            {
                byte initial = (byte)((majorType << 5) | additionalInformation);
                yield return SyntaxCase(
                    $"reserved-additional-{majorType}-{additionalInformation}",
                    initial.ToString("x2"),
                    "CBOR_RESERVED_ADDITIONAL_INFORMATION",
                    0);
            }
        }

        for (int simple = 0; simple < 32; simple++)
        {
            yield return SyntaxCase(
                $"reserved-two-byte-simple-{simple}",
                $"f8{simple:x2}",
                "CBOR_INVALID_SIMPLE_VALUE_ENCODING",
                1);
        }

        string[] invalidByteChunks =
        [
            "00", "20", "60", "80", "a0", "c000", "f4", "7f", "9f", "bf"
        ];
        foreach (string chunk in invalidByteChunks)
        {
            yield return SyntaxCase(
                $"wrong-byte-string-chunk-{chunk}",
                $"5f{chunk}ff",
                "CBOR_INVALID_BYTE_STRING_CHUNK",
                1);
        }
        yield return SyntaxCase(
            "nested-indefinite-byte-string",
            "5f5f4100ffff",
            "CBOR_NESTED_INDEFINITE_BYTE_STRING",
            1);

        string[] invalidTextChunks =
        [
            "00", "20", "40", "80", "a0", "c000", "f4", "5f", "9f", "bf"
        ];
        foreach (string chunk in invalidTextChunks)
        {
            yield return SyntaxCase(
                $"wrong-text-string-chunk-{chunk}",
                $"7f{chunk}ff",
                "CBOR_INVALID_TEXT_STRING_CHUNK",
                1);
        }
        yield return SyntaxCase(
            "nested-indefinite-text-string",
            "7f7f6100ffff",
            "CBOR_NESTED_INDEFINITE_TEXT_STRING",
            1);

        yield return SyntaxCase(
            "stray-break",
            "ff",
            "CBOR_UNEXPECTED_BREAK",
            0);
        yield return SyntaxCase(
            "break-in-one-item-array",
            "81ff",
            "CBOR_BREAK_IN_DEFINITE_CONTAINER",
            1);
        yield return SyntaxCase(
            "break-in-second-array-position",
            "8200ff",
            "CBOR_BREAK_IN_DEFINITE_CONTAINER",
            2);
        yield return SyntaxCase(
            "break-in-map-key-position",
            "a1ff00",
            "CBOR_BREAK_IN_DEFINITE_CONTAINER",
            1);
        yield return SyntaxCase(
            "break-in-map-value-position",
            "a100ff",
            "CBOR_BREAK_IN_DEFINITE_CONTAINER",
            2);
        yield return SyntaxCase(
            "break-after-tag",
            "c0ff",
            "CBOR_BREAK_AFTER_TAG",
            1);
        yield return SyntaxCase(
            "nested-break-in-definite-array",
            "9f81ffff",
            "CBOR_BREAK_IN_DEFINITE_CONTAINER",
            2);
        yield return SyntaxCase(
            "odd-indefinite-map-first-pair",
            "bf00ff",
            "CBOR_ODD_INDEFINITE_MAP",
            2);
        yield return SyntaxCase(
            "odd-indefinite-map-second-pair",
            "bf000000ff",
            "CBOR_ODD_INDEFINITE_MAP",
            4);

        yield return SyntaxCase(
            "indefinite-unsigned-integer",
            "1f",
            "CBOR_ILLEGAL_INDEFINITE_HEAD",
            0);
        yield return SyntaxCase(
            "indefinite-negative-integer",
            "3f",
            "CBOR_ILLEGAL_INDEFINITE_HEAD",
            0);
        yield return SyntaxCase(
            "indefinite-tag",
            "df",
            "CBOR_ILLEGAL_INDEFINITE_HEAD",
            0);
    }

    private static TestCaseData SyntaxCase(
        string name,
        string hex,
        string errorCode,
        long errorOffset)
    {
        MalformedVector vector = new MalformedVector();
        vector.Name = name;
        vector.Hex = hex;
        vector.ErrorCode = errorCode;
        vector.ErrorOffset = errorOffset;
        return new TestCaseData(vector).SetName($"AppendixF_{name}");
    }

    private static List<string> LoadAppendixAEncodings()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "..",
            "specs",
            "test-vectors",
            "rfc8949-appendix-a.json"));
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement
            .EnumerateArray()
            .Select(element => element.GetProperty("hex").GetString()!)
            .ToList();
    }

    private static CborReadResult Decode(byte[] source)
    {
        return new CborDecoder().Decode(source, DecodeOptions());
    }

    private static CborDecodeOptions DecodeOptions()
    {
        return new CborDecodeOptions
        {
            DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve,
            TagPolicy = CborTagPolicy.Preserve,
            SharedValuePolicy = CborSharedValuePolicy.Preserve
        };
    }

    private static void AssertAtomicFailure(
        CborReadResult result,
        string errorCode,
        long errorOffset,
        int sourceLength,
        string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Error), context);
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode), context);
            Assert.That(result.ErrorOffset, Is.EqualTo(errorOffset), context);
            Assert.That(result.ErrorPath, Is.EqualTo("$"), context);
            Assert.That(result.Value, Is.Null, context);
            Assert.That(result.Consumed, Is.Zero, context);
            Assert.That(result.Remaining, Is.EqualTo(sourceLength), context);
        });
    }

    public class MalformedVector
    {
        public string Name = "";
        public string Hex = "";
        public string ErrorCode = "";
        public long ErrorOffset;

        public override string ToString()
        {
            return Name;
        }
    }
}
