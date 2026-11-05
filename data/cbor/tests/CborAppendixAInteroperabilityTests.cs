using System.Text.Json;
using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborAppendixAInteroperabilityTests
{
    [TestCaseSource(nameof(AppendixAVectors))]
    public void EveryDownloadedVectorPassesAllApplicablePublicOperations(
        AppendixAVector vector)
    {
        byte[] source = Convert.FromHexString(vector.Hex);
        byte[] base64 = Convert.FromBase64String(vector.Cbor);
        CborDecodeOptions decodeOptions = DecodeOptions();
        CborReadResult decoded = new CborDecoder().Decode(source, decodeOptions);

        Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done), vector.Hex);
        Assert.That(decoded.Value, Is.Not.Null, vector.Hex);
        CborValue value = decoded.Value!;

        byte[] encoded = new CborEncoder().Encode(value, new CborEncodeOptions());
        CborReadResult redecoded = new CborDecoder().Decode(encoded, DecodeOptions());
        CborEncodeOptions deterministicOptions = DeterministicOptions();
        byte[] preferred = new CborEncoder().Encode(value, deterministicOptions);
        CborValidationResult deterministic = new CborValidator()
            .ValidateEncodingWithOptions(source, DeterministicDecodeOptions());

        CborDiagnosticOptions diagnosticOptions = DiagnosticOptions(vector);
        CborDiagnosticConverter diagnosticConverter = new CborDiagnosticConverter();
        string diagnostic = diagnosticConverter.FormatDiagnostic(
            value,
            diagnosticOptions);
        CborReadResult parsedDiagnostic = diagnosticConverter.ParseDiagnostic(
            diagnostic,
            new CborDiagnosticOptions());

        List<CborReadResult> sequence = new CborDecoder().DecodeSequence(
            source,
            decodeOptions);
        byte[] encodedSequence = new CborEncoder().EncodeSequence(
            [value],
            deterministicOptions);

        Assert.Multiple(() =>
        {
            Assert.That(base64, Is.EqualTo(source), vector.Hex);
            Assert.That(decoded.Consumed, Is.EqualTo(source.Length), vector.Hex);
            Assert.That(decoded.Remaining, Is.Zero, vector.Hex);
            Assert.That(decoded.ErrorCode, Is.Null, vector.Hex);
            Assert.That(decoded.Value?.Representation, Is.Not.Null, vector.Hex);
            Assert.That(
                decoded.Value?.Representation?.SourceLength,
                Is.EqualTo(source.Length),
                vector.Hex);
            Assert.That(
                !vector.Roundtrip || encoded.SequenceEqual(source),
                Is.True,
                vector.Hex);
            Assert.That(redecoded.Status, Is.EqualTo(CborReadStatus.Done), vector.Hex);
            Assert.That(
                preferred.SequenceEqual(source),
                Is.EqualTo(vector.Roundtrip),
                vector.Hex);
            Assert.That(
                deterministic.IsValid,
                Is.EqualTo(vector.Roundtrip),
                vector.Hex);
            Assert.That(parsedDiagnostic.Status, Is.EqualTo(CborReadStatus.Done), vector.Hex);
            Assert.That(parsedDiagnostic.Value, Is.Not.Null, vector.Hex);
            Assert.That(sequence, Has.Count.EqualTo(1), vector.Hex);
            Assert.That(sequence[0].Status, Is.EqualTo(CborReadStatus.Done), vector.Hex);
            Assert.That(sequence[0].ItemIndex, Is.Zero, vector.Hex);
            Assert.That(sequence[0].SourceOffset, Is.Zero, vector.Hex);
            Assert.That(sequence[0].Consumed, Is.EqualTo(source.Length), vector.Hex);
            Assert.That(sequence[0].Remaining, Is.Zero, vector.Hex);
            Assert.That(encodedSequence, Is.EqualTo(preferred), vector.Hex);
        });

        AssertSemanticEncoding(value, redecoded.Value!, vector.Hex);
        AssertSemanticEncoding(value, parsedDiagnostic.Value!, vector.Hex);
        if (vector.HasDiagnostic)
        {
            AssertFixtureDiagnostic(vector, diagnosticConverter, diagnosticOptions, diagnostic);
        }
    }

    [Test]
    public void EntireDownloadedVectorSetFormsOneRoundTrippableCborSequence()
    {
        List<AppendixAVector> vectors = LoadVectors();
        byte[] source = vectors
            .SelectMany(vector => Convert.FromHexString(vector.Hex))
            .ToArray();
        List<CborReadResult> decoded = new CborDecoder().DecodeSequence(
            source,
            DecodeOptions());
        List<CborValue> values = decoded.Select(result => result.Value!).ToList();
        byte[] encoded = new CborEncoder().EncodeSequence(
            values,
            DeterministicOptions());
        byte[] expected = vectors
            .SelectMany(vector => PreferredEncoding(vector))
            .ToArray();
        long[] expectedOffsets = new long[vectors.Count];
        int[] expectedLengths = new int[vectors.Count];
        int[] expectedRemaining = new int[vectors.Count];
        int offset = 0;
        int index = 0;
        while (index < vectors.Count)
        {
            int length = Convert.FromHexString(vectors[index].Hex).Length;
            expectedOffsets[index] = offset;
            expectedLengths[index] = length;
            offset = offset + length;
            expectedRemaining[index] = source.Length - offset;
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(vectors, Has.Count.EqualTo(81));
            Assert.That(decoded, Has.Count.EqualTo(vectors.Count));
            Assert.That(decoded.All(result => result.Status == CborReadStatus.Done), Is.True);
            Assert.That(
                decoded.Select(result => result.ItemIndex),
                Is.EqualTo(Enumerable.Range(0, vectors.Count)));
            Assert.That(
                decoded.Select(result => result.SourceOffset),
                Is.EqualTo(expectedOffsets));
            Assert.That(
                decoded.Select(result => result.Consumed),
                Is.EqualTo(expectedLengths));
            Assert.That(
                decoded.Select(result => result.Remaining),
                Is.EqualTo(expectedRemaining));
            Assert.That(decoded[^1].Remaining, Is.Zero);
            Assert.That(encoded, Is.EqualTo(expected));
        });
    }

    private static IEnumerable<TestCaseData> AppendixAVectors()
    {
        foreach (AppendixAVector vector in LoadVectors())
        {
            yield return new TestCaseData(vector).SetName(
                $"AppendixA_{vector.Index:D2}_{vector.Hex}");
        }
    }

    private static List<AppendixAVector> LoadVectors()
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
        List<AppendixAVector> vectors = new List<AppendixAVector>();
        int index = 0;
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            AppendixAVector vector = new AppendixAVector();
            vector.Index = index;
            vector.Cbor = element.GetProperty("cbor").GetString()!;
            vector.Hex = element.GetProperty("hex").GetString()!;
            vector.Roundtrip = element.GetProperty("roundtrip").GetBoolean();
            JsonElement expectedDiagnostic;
            vector.HasDiagnostic = element.TryGetProperty(
                "diagnostic",
                out expectedDiagnostic);
            if (vector.HasDiagnostic)
            {
                vector.Diagnostic = expectedDiagnostic.GetString()!;
            }
            vectors.Add(vector);
            index = index + 1;
        }
        return vectors;
    }

    private static CborDecodeOptions DecodeOptions()
    {
        return new CborDecodeOptions
        {
            DuplicateKeyPolicy = CborDuplicateKeyPolicy.Preserve,
            TagPolicy = CborTagPolicy.Preserve,
            SharedValuePolicy = CborSharedValuePolicy.Preserve,
            RetainRepresentation = true,
            RetainSourceRanges = true
        };
    }

    private static CborDecodeOptions DeterministicDecodeOptions()
    {
        return new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.Deterministic,
            DeterministicEncodingProfile = CborEncodingProfile.CoreDeterministic,
            MaxValidationIssues = 20,
            CollectValidationIssues = true
        };
    }

    private static CborEncodeOptions DeterministicOptions()
    {
        return new CborEncodeOptions
        {
            EncodingProfile = CborEncodingProfile.CoreDeterministic
        };
    }

    private static CborDiagnosticOptions DiagnosticOptions(AppendixAVector vector)
    {
        return new CborDiagnosticOptions
        {
            IncludeRepresentation = vector.Diagnostic.StartsWith("(_", StringComparison.Ordinal)
        };
    }

    private static byte[] PreferredEncoding(AppendixAVector vector)
    {
        CborReadResult decoded = new CborDecoder().Decode(
            Convert.FromHexString(vector.Hex),
            DecodeOptions());
        Assert.That(decoded.Status, Is.EqualTo(CborReadStatus.Done), vector.Hex);
        return new CborEncoder().Encode(decoded.Value!, DeterministicOptions());
    }

    private static void AssertFixtureDiagnostic(
        AppendixAVector vector,
        CborDiagnosticConverter converter,
        CborDiagnosticOptions options,
        string actual)
    {
        CborReadResult expected = converter.ParseDiagnostic(
            vector.Diagnostic,
            new CborDiagnosticOptions());
        Assert.That(expected.Status, Is.EqualTo(CborReadStatus.Done), vector.Hex);
        string canonicalExpected = converter.FormatDiagnostic(expected.Value!, options);
        Assert.That(actual, Is.EqualTo(canonicalExpected), vector.Hex);
    }

    private static void AssertSemanticEncoding(
        CborValue expected,
        CborValue actual,
        string hex)
    {
        CborEncoder encoder = new CborEncoder();
        byte[] expectedEncoding = encoder.Encode(expected, DeterministicOptions());
        byte[] actualEncoding = encoder.Encode(actual, DeterministicOptions());
        Assert.That(actualEncoding, Is.EqualTo(expectedEncoding), hex);
    }

    public class AppendixAVector
    {
        public int Index;
        public string Cbor = "";
        public string Hex = "";
        public bool Roundtrip;
        public bool HasDiagnostic;
        public string Diagnostic = "";

        public override string ToString()
        {
            return Hex;
        }
    }
}
