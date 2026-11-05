using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrMatrixProcessorGoldenTests
{
    [TestCaseSource(nameof(GoldenCases))]
    public void BuildsMatrixWithIndependentGoldenHash(JsonElement vector)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        byte[] expectedCodewords = vector.GetProperty("codewords").GetBytesFromBase64();
        QrSymbol symbol = new QrSymbol
        {
            SymbolKind = Enum.Parse<QrSymbolKind>(vector.GetProperty("family").GetString()!),
            Version = vector.GetProperty("version").GetInt32(),
            ErrorCorrectionLevel = Enum.Parse<QrErrorCorrectionLevel>(vector.GetProperty("level").GetString()!),
            MaskPattern = (QrMaskPattern)vector.GetProperty("mask").GetInt32(),
            Codewords = (byte[])expectedCodewords.Clone()
        };

        QrMatrix matrix = processor.BuildMatrix(symbol, 0);

        Assert.Multiple(() =>
        {
            Assert.That(matrix.Width, Is.EqualTo(vector.GetProperty("width").GetInt32()));
            Assert.That(matrix.Height, Is.EqualTo(vector.GetProperty("height").GetInt32()));
            Assert.That(HashMatrix(matrix), Is.EqualTo(vector.GetProperty("matrix_sha256").GetString()));
            Assert.That(symbol.MaskPattern, Is.EqualTo((QrMaskPattern)vector.GetProperty("mask").GetInt32()));
            Assert.That(symbol.Matrix, Is.SameAs(matrix));
            Assert.That(symbol.Codewords, Is.EqualTo(expectedCodewords), "Building must preserve the input codewords.");
        });
    }

    [TestCaseSource(nameof(GoldenCases))]
    public void ExtractsCodewordsFromIndependentGoldenMatrix(JsonElement vector)
    {
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrMatrix matrix = ReadMatrix(vector);
        QrModuleValue[] originalValues = (QrModuleValue[])matrix.Values.Clone();

        byte[] extracted = processor.ExtractCodewords(
            matrix,
            Enum.Parse<QrSymbolKind>(vector.GetProperty("family").GetString()!),
            vector.GetProperty("version").GetInt32(),
            Enum.Parse<QrErrorCorrectionLevel>(vector.GetProperty("level").GetString()!),
            (QrMaskPattern)vector.GetProperty("mask").GetInt32());

        Assert.Multiple(() =>
        {
            Assert.That(extracted, Is.EqualTo(vector.GetProperty("codewords").GetBytesFromBase64()));
            Assert.That(matrix.Values, Is.EqualTo(originalValues), "Extraction must preserve the sampled modules.");
        });
    }

    static IEnumerable<TestCaseData> GoldenCases()
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "tests", "Fixtures", "Golden", "matrix-combinations.json.gz"));
        using FileStream file = File.OpenRead(path);
        using GZipStream decompressed = new GZipStream(file, CompressionMode.Decompress);
        using JsonDocument document = JsonDocument.Parse(decompressed);
        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
        {
            yield return new TestCaseData(vector.Clone()).SetName("{m}_" + vector.GetProperty("id").GetString());
        }
    }

    static QrMatrix ReadMatrix(JsonElement vector)
    {
        int width = vector.GetProperty("width").GetInt32();
        int height = vector.GetProperty("height").GetInt32();
        byte[] packed = vector.GetProperty("matrix_bits").GetBytesFromBase64();
        QrModuleValue[] values = new QrModuleValue[width * height];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = (packed[index / 8] & (1 << (7 - index % 8))) != 0
                ? QrModuleValue.Dark : QrModuleValue.Light;
        }

        return new QrMatrix { Width = width, Height = height, Values = values };
    }

    static string HashMatrix(QrMatrix matrix)
    {
        // The oracle hashes ASCII module rows, including a final LF, without a quiet zone.
        byte[] rows = new byte[(matrix.Width + 1) * matrix.Height];
        for (int y = 0; y < matrix.Height; y++)
        {
            for (int x = 0; x < matrix.Width; x++)
            {
                QrModuleValue value = matrix.Values[y * matrix.Width + x];
                rows[y * (matrix.Width + 1) + x] = value switch
                {
                    QrModuleValue.Dark => (byte)'1',
                    QrModuleValue.Light => (byte)'0',
                    _ => (byte)'?'
                };
            }

            rows[y * (matrix.Width + 1) + matrix.Width] = (byte)'\n';
        }

        return Convert.ToHexStringLower(SHA256.HashData(rows));
    }
}
