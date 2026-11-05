using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrEncoderOracleTests
{
    [TestCaseSource(nameof(OracleCases))]
    public void GeneratedSymbolMatchesIndependentEncoder(JsonElement vector, string oracle, string expectedHash)
    {
        QrSymbolKind kind = Enum.Parse<QrSymbolKind>(vector.GetProperty("family").GetString()!);
        QrEncodingMode mode = Enum.Parse<QrEncodingMode>(vector.GetProperty("mode").GetString()!);
        QrErrorCorrectionLevel level = Enum.Parse<QrErrorCorrectionLevel>(vector.GetProperty("level").GetString()!);
        int version = vector.GetProperty("version").GetInt32();
        int width = vector.GetProperty("width").GetInt32();
        int height = vector.GetProperty("height").GetInt32();
        QrMaskPattern mask = (QrMaskPattern)vector.GetProperty("mask").GetInt32();
        string text = vector.GetProperty("text").GetString()!;
        byte[] bytes = mode == QrEncodingMode.Byte ? Encoding.Latin1.GetBytes(text) : [];
        QrSegment segment = new QrSegment
        {
            Mode = mode, Text = mode == QrEncodingMode.Byte ? null : text,
            Data = bytes, CharacterCount = mode == QrEncodingMode.Byte ? bytes.Length : text.Length
        };
        IQrEncoder encoder = new QrEncoder();
        QrSymbol symbol = encoder.EncodeSegments([segment], new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = kind,
            MinimumVersion = kind == QrSymbolKind.rMQR ? 0 : version,
            MaximumVersion = kind == QrSymbolKind.rMQR ? 40 : version,
            MinimumWidth = width, MaximumWidth = width, MinimumHeight = height, MaximumHeight = height,
            ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = level,
            BoostErrorCorrectionLevel = false, MaskPattern = mask, QuietZoneModules = 0,
            OptimizeSegments = false
        });

        Assert.Multiple(() =>
        {
            Assert.That(symbol.SymbolKind, Is.EqualTo(kind));
            Assert.That(symbol.Version, Is.EqualTo(version));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(level));
            Assert.That(symbol.MaskPattern, Is.EqualTo(mask));
            Assert.That(symbol.Matrix!.Width, Is.EqualTo(width));
            Assert.That(symbol.Matrix.Height, Is.EqualTo(height));
            Assert.That(HashMatrix(symbol.Matrix), Is.EqualTo(expectedHash), $"Matrix differs from {oracle}.");
        });
    }

    static IEnumerable<TestCaseData> OracleCases()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "tests", "Fixtures", "Golden", "cross-oracle-encoders.json.gz"));
        using FileStream file = File.OpenRead(path);
        using GZipStream stream = new GZipStream(file, CompressionMode.Decompress);
        using JsonDocument document = JsonDocument.Parse(stream);
        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
            foreach (JsonProperty oracle in vector.GetProperty("hashes").EnumerateObject())
                yield return new TestCaseData(vector.Clone(), oracle.Name, oracle.Value.GetString()!)
                    .SetName("CrossOracle_" + oracle.Name + "_" + vector.GetProperty("id").GetString());
    }

    static string HashMatrix(QrMatrix matrix)
    {
        byte[] rows = new byte[(matrix.Width + 1) * matrix.Height];
        for (int y = 0; y < matrix.Height; y++)
        {
            for (int x = 0; x < matrix.Width; x++)
                rows[y * (matrix.Width + 1) + x] = matrix.Values[y * matrix.Width + x] switch
                {
                    QrModuleValue.Dark => (byte)'1', QrModuleValue.Light => (byte)'0', _ => (byte)'?'
                };
            rows[y * (matrix.Width + 1) + matrix.Width] = (byte)'\n';
        }
        return Convert.ToHexStringLower(SHA256.HashData(rows));
    }
}
