using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrSegmenterBoundaryTests
{
    [TestCaseSource(nameof(CountWidths))]
    public void ManualSegmentsAcceptCountFieldMaximumAndRejectOneMore(QrSymbolKind kind, int version, QrEncodingMode mode, int width)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        int maximum = (1 << width) - 1;
        // Header representability is distinct from whether the payload fits a symbol.
        foreach (int length in new[] { 0, 1, maximum - 1, maximum })
        {
            QrSegment segment = Segment(mode, length);
            Assert.DoesNotThrow(() => segmenter.ValidateSegments([segment], kind, version, true, 1, 200000), $"length {length}");
            Assert.That(segment.CharacterCount, Is.EqualTo(length));
        }
        QrInputException error = Assert.Throws<QrInputException>(() =>
            segmenter.ValidateSegments([Segment(mode, maximum + 1)], kind, version, true, 1, 200000))!;
        Assert.That(error.InputName, Is.EqualTo("CharacterCount"));
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidInput));
    }

    static IEnumerable<TestCaseData> CountWidths()
    {
        QrEncodingMode[] modes = [QrEncodingMode.Numeric, QrEncodingMode.Alphanumeric, QrEncodingMode.Byte, QrEncodingMode.Kanji, QrEncodingMode.Hanzi];
        int[][] model2 = [[10, 9, 8, 8, 8], [12, 11, 16, 10, 10], [14, 13, 16, 12, 12]];
        foreach (int version in new[] { 1, 9, 10, 26, 27, 40 })
            for (int mode = 0; mode < modes.Length; mode++)
                yield return new TestCaseData(QrSymbolKind.Model2, version, modes[mode], model2[version < 10 ? 0 : version < 27 ? 1 : 2][mode]);
        foreach (int version in new[] { 1, 9, 10, 14 })
            for (int mode = 0; mode < 4; mode++)
                yield return new TestCaseData(QrSymbolKind.LegacyModel1, version, modes[mode], model2[version < 10 ? 0 : 1][mode]);
        int[][] micro = [[3, 0, 0, 0], [4, 3, 0, 0], [5, 4, 4, 3], [6, 5, 5, 4]];
        for (int version = 1; version <= 4; version++)
            for (int mode = 0; mode < 4; mode++)
                if (micro[version - 1][mode] > 0)
                    yield return new TestCaseData(QrSymbolKind.Micro, version, modes[mode], micro[version - 1][mode]);
        int[][] rectangular =
        [
            [4, 5, 6, 7, 7, 5, 6, 7, 7, 8, 4, 6, 7, 7, 8, 8, 5, 6, 7, 7, 8, 8, 7, 7, 8, 8, 9, 7, 8, 8, 8, 9],
            [3, 5, 5, 6, 6, 5, 5, 6, 6, 7, 4, 5, 6, 6, 7, 7, 5, 6, 6, 7, 7, 8, 6, 7, 7, 7, 8, 6, 7, 7, 8, 8],
            [3, 4, 5, 5, 6, 4, 5, 5, 6, 6, 3, 5, 5, 6, 6, 7, 4, 5, 6, 6, 7, 7, 6, 6, 7, 7, 7, 6, 6, 7, 7, 8],
            [2, 3, 4, 5, 5, 3, 4, 5, 5, 6, 2, 4, 5, 5, 6, 6, 3, 5, 5, 6, 6, 7, 5, 5, 6, 6, 7, 5, 6, 6, 6, 7]
        ];
        for (int version = 0; version < 32; version++)
            for (int mode = 0; mode < 4; mode++)
                yield return new TestCaseData(QrSymbolKind.rMQR, version, modes[mode], rectangular[mode][version]);
    }

    [TestCase(1, QrEncodingMode.Numeric, 7)]
    [TestCase(2, QrEncodingMode.Numeric, 15)]
    [TestCase(2, QrEncodingMode.Alphanumeric, 7)]
    [TestCase(3, QrEncodingMode.Numeric, 31)]
    [TestCase(3, QrEncodingMode.Alphanumeric, 15)]
    [TestCase(3, QrEncodingMode.Byte, 15)]
    [TestCase(3, QrEncodingMode.Kanji, 7)]
    [TestCase(4, QrEncodingMode.Numeric, 63)]
    [TestCase(4, QrEncodingMode.Alphanumeric, 31)]
    [TestCase(4, QrEncodingMode.Byte, 31)]
    [TestCase(4, QrEncodingMode.Kanji, 15)]
    public void MicroOptimizerSplitsAtTheCountFieldBoundary(int version, QrEncodingMode mode, int maximum)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        foreach (int length in new[] { maximum, maximum + 1 })
        {
            string text = Segment(mode, length).Text!;
            if (mode == QrEncodingMode.Byte) text = new string('a', length);
            QrSegment[] segments = segmenter.OptimizeTextSegments(text, QrSymbolKind.Micro, version, [], false, -1, null, 2, 1000);
            Assert.That(segments, Has.Length.EqualTo(length == maximum ? 1 : 2));
            Assert.That(segments.Select(s => s.Mode), Is.All.EqualTo(mode));
            Assert.That(segments.Select(s => s.CharacterCount), Is.All.LessThanOrEqualTo(maximum));
            Assert.That(string.Concat(segments.Select(s => s.Text)), Is.EqualTo(text));
            segmenter.ValidateSegments(segments, QrSymbolKind.Micro, version, false, 2, 1000);
        }
    }

    [TestCase(1, true, 81)]
    [TestCase(9, true, 81)]
    [TestCase(10, true, 99)]
    [TestCase(26, true, 99)]
    [TestCase(27, false, 100)]
    [TestCase(40, false, 100)]
    public void VersionGroupHeadersChangeTheOptimalSegmentation(int version, bool split, int expectedBits)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        QrSegment[] segments = segmenter.OptimizeTextSegments("a12345678a", QrSymbolKind.Model2, version, [], false, -1, null, 10, 100);
        Assert.That(segments.Select(s => s.Mode), Is.EqualTo(split
            ? new[] { QrEncodingMode.Byte, QrEncodingMode.Numeric, QrEncodingMode.Byte }
            : new[] { QrEncodingMode.Byte }));
        int bits = 0;
        foreach (QrSegment segment in segments)
        {
            int count = segment.CharacterCount;
            bits += 4 + (segment.Mode == QrEncodingMode.Byte ? (version < 10 ? 8 : 16) : (version < 10 ? 10 : 12));
            bits += segment.Mode == QrEncodingMode.Byte ? count * 8 : count / 3 * 10 + (count % 3 == 2 ? 7 : count % 3 == 1 ? 4 : 0);
        }
        Assert.That(bits, Is.EqualTo(expectedBits));
        Assert.That(string.Concat(segments.Select(s => s.Text)), Is.EqualTo("a12345678a"));
    }

    static QrSegment Segment(QrEncodingMode mode, int length) => new QrSegment
    {
        Mode = mode, CharacterCount = length, IsHanziCompatibilityMode = mode == QrEncodingMode.Hanzi,
        Data = mode == QrEncodingMode.Byte ? new byte[length] : [],
        Text = mode switch
        {
            QrEncodingMode.Numeric => new string('1', length), QrEncodingMode.Alphanumeric => new string('A', length),
            QrEncodingMode.Kanji => new string('漢', length), QrEncodingMode.Hanzi => new string('汉', length), _ => null
        }
    };
}
