using System.IO.Compression;
using System.Text;
using System.Text.Json;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrDecoderOracleTests
{
    [TestCaseSource(nameof(Vectors))]
    public void DecoderMatchesIndependentResultsAndRetainsUnsupportedProfiles(JsonElement vector)
    {
        int width = vector.GetProperty("width").GetInt32();
        int height = vector.GetProperty("height").GetInt32();
        byte[] packed = Convert.FromBase64String(vector.GetProperty("matrix_bits").GetString()!);
        QrMatrix matrix = new()
        {
            Width = width, Height = height,
            Values = Enumerable.Range(0, width * height)
                .Select(i => (packed[i / 8] & (128 >> (i % 8))) != 0 ? QrModuleValue.Dark : QrModuleValue.Light).ToArray()
        };
        QrModuleValue[] before = (QrModuleValue[])matrix.Values.Clone();
        IQrDecoder decoder = new QrDecoder();
        QrAttemptResult attempt = decoder.TryDecodeMatrix(matrix, new QrDecodeOptions());
        Assert.That(attempt.Success, Is.True, attempt.FailureReason.ToString());
        QrDecodeResult result = attempt.Result!;
        QrSymbol symbol = result.Symbol!;
        QrSymbolKind kind = Enum.Parse<QrSymbolKind>(vector.GetProperty("family").GetString()!);
        Assert.Multiple(() =>
        {
            Assert.That(result.Text, Is.EqualTo(vector.GetProperty("text").GetString()));
            Assert.That(result.RawBytes, Is.EqualTo(Convert.FromHexString(vector.GetProperty("raw_hex").GetString()!)));
            Assert.That(symbol.SymbolKind, Is.EqualTo(kind));
            Assert.That(symbol.Version, Is.EqualTo(kind == QrSymbolKind.rMQR ? 0 : vector.GetProperty("version").GetInt32()));
            Assert.That(symbol.Width, Is.EqualTo(width));
            Assert.That(symbol.Height, Is.EqualTo(height));
            Assert.That(symbol.ErrorCorrectionLevel, Is.EqualTo(Enum.Parse<QrErrorCorrectionLevel>(vector.GetProperty("level").GetString()!)));
            Assert.That(symbol.MaskPattern, Is.EqualTo((QrMaskPattern)vector.GetProperty("mask").GetInt32()));
            Assert.That(result.CorrectedErrorCount, Is.Zero);
            Assert.That(result.CorrectedErasureCount, Is.Zero);
            Assert.That(matrix.Values, Is.EqualTo(before));
        });
        if (vector.TryGetProperty("mode", out JsonElement mode))
            Assert.That(result.Segments.Single().Mode, Is.EqualTo(Enum.Parse<QrEncodingMode>(mode.GetString()!)));

        foreach (JsonProperty oracle in vector.GetProperty("oracles").EnumerateObject())
        {
            JsonElement expected = oracle.Value;
            using (Assert.EnterMultipleScope())
            {
                if (oracle.Name == "ZXing")
                {
                    Assert.That(result.Text, Is.EqualTo(expected.GetProperty("text").GetString()), "ZXing text");
                    Assert.That(result.Segments.Where(s => s.Mode == QrEncodingMode.Byte).SelectMany(s => s.Data).ToArray(),
                        Is.EqualTo(Convert.FromBase64String(expected.GetProperty("byte_segments").GetString()!)), "ZXing byte segments");
                    Assert.That(symbol.ErrorCorrectionLevel.ToString()[0].ToString(), Is.EqualTo(expected.GetProperty("level").GetString()), "ZXing ECC");
                    if (expected.TryGetProperty("structured_append", out JsonElement append))
                    {
                        Assert.That(result.StructuredAppend, Is.Not.Null);
                        Assert.That(result.StructuredAppend!.Index, Is.EqualTo(append.GetProperty("index").GetInt32()), "ZXing append index");
                        Assert.That(result.StructuredAppend.Total, Is.EqualTo(append.GetProperty("count").GetInt32()), "ZXing append count");
                        Assert.That(result.StructuredAppend.Parity, Is.EqualTo(append.GetProperty("parity").GetInt32()), "ZXing append parity");
                    }
                    else Assert.That(result.StructuredAppend, Is.Null);
                }
                else
                {
                    byte[] actual = expected.GetProperty("comparison").GetString() == "utf8"
                        ? Encoding.UTF8.GetBytes(result.Text!) : result.RawBytes;
                    Assert.That(actual, Is.EqualTo(Convert.FromHexString(expected.GetProperty("hex").GetString()!)), oracle.Name + " payload");
                    if (oracle.Name == "quirc")
                    {
                        Assert.That(symbol.Version, Is.EqualTo(expected.GetProperty("version").GetInt32()), "quirc version");
                        Assert.That(symbol.ErrorCorrectionLevel.ToString(), Is.EqualTo(expected.GetProperty("level").GetString()), "quirc ECC");
                        Assert.That((int)symbol.MaskPattern, Is.EqualTo(expected.GetProperty("mask").GetInt32()), "quirc mask");
                    }
                }
            }
        }
    }

    static IEnumerable<TestCaseData> Vectors()
    {
        string path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "../../../../tests/Fixtures/Golden/cross-oracle-decoders.json.gz"));
        using FileStream file = File.OpenRead(path);
        using GZipStream stream = new(file, CompressionMode.Decompress);
        using JsonDocument document = JsonDocument.Parse(stream);
        foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
            yield return new TestCaseData(vector.Clone()).SetName("DecoderOracles_" + vector.GetProperty("id").GetString());
    }
}
