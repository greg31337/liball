using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrSegmenterTests
{
    [Test]
    public void CreatesAutomaticallySelectedTextSegments()
    {
        IQrSegmenter segmenter = new QrSegmenter();

        QrSegment[] numeric = segmenter.CreateTextSegments("12345", -1, false, 5);
        QrSegment[] hanzi = segmenter.CreateTextSegments("汉", -1, true, 2);
        QrSegment[] empty = segmenter.CreateTextSegments("", -1, false, 0);

        Assert.Multiple(() =>
        {
            Assert.That(numeric.Length, Is.EqualTo(1));
            Assert.That(numeric[0].Mode, Is.EqualTo(QrEncodingMode.Numeric));
            Assert.That(numeric[0].Text, Is.EqualTo("12345"));
            Assert.That(hanzi.Length, Is.EqualTo(1));
            Assert.That(hanzi[0].Mode, Is.EqualTo(QrEncodingMode.Hanzi));
            Assert.That(hanzi[0].IsHanziCompatibilityMode, Is.True);
            Assert.That(empty, Is.Empty);
        });
    }

    [Test]
    public void CreatesByteSegmentsWithDefensiveCopies()
    {
        IQrSegmenter segmenter = new QrSegmenter();
        byte[] input = new byte[] { 0, 1, 255 };

        QrSegment segment = segmenter.CreateByteSegment(
            input,
            QrEciAssignments.Binary,
            input.Length);
        input[1] = 99;

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(segment.CharacterCount, Is.EqualTo(3));
            Assert.That(segment.Data, Is.EqualTo(new byte[] { 0, 1, 255 }));
            Assert.That(segment.Data, Is.Not.SameAs(input));
            Assert.That(segment.EciAssignment, Is.EqualTo(QrEciAssignments.Binary));
        });
    }

    [Test]
    public void OptimizesEciDataAndPrependsRequestedControls()
    {
        IQrSegmenter segmenter = new QrSegmenter();
        QrStructuredAppendInfo append = new QrStructuredAppendInfo
        {
            Index = 0,
            Total = 2,
            Parity = 37
        };

        QrSegment[] segments = segmenter.OptimizeTextSegments(
            "a€",
            QrSymbolKind.Model2,
            1,
            new int[] { QrEciAssignments.Utf8, QrEciAssignments.Utf16BigEndian },
            true,
            -1,
            append,
            10,
            100);

        Assert.Multiple(() =>
        {
            Assert.That(GetModes(segments), Is.EqualTo(new QrEncodingMode[]
            {
                QrEncodingMode.StructuredAppend,
                QrEncodingMode.Fnc1First,
                QrEncodingMode.Eci,
                QrEncodingMode.Byte
            }));
            Assert.That(segments[0].StructuredAppend, Is.Not.SameAs(append));
            Assert.That(segments[0].StructuredAppend!.Parity, Is.EqualTo(37));
            Assert.That(segments[2].EciAssignment, Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(segments[3].Data, Is.EqualTo(new byte[] { 0x61, 0xE2, 0x82, 0xAC }));
        });
    }

    [Test]
    public void ValidatesManualSegmentsForTheRequestedProfile()
    {
        IQrSegmenter segmenter = new QrSegmenter();
        QrSegment[] segments = segmenter.CreateTextSegments("12345", -1, false, 5);

        Assert.Multiple(() =>
        {
            Assert.That(() => segmenter.ValidateSegments(
                segments,
                QrSymbolKind.Model2,
                1,
                false,
                1,
                5), Throws.Nothing);
            Assert.That(() => segmenter.ValidateSegments(
                segments,
                QrSymbolKind.Micro,
                1,
                false,
                1,
                5), Throws.Nothing);
            Assert.That(() => segmenter.ValidateSegments(
                segments,
                QrSymbolKind.Model2,
                1,
                false,
                1,
                4), Throws.TypeOf<QrResourceLimitException>());
        });
    }

    [Test]
    public void RejectsInvalidInputsAndControlCombinationsThroughTheContract()
    {
        IQrSegmenter segmenter = new QrSegmenter();
        QrStructuredAppendInfo append = new QrStructuredAppendInfo
        {
            Index = 0,
            Total = 2,
            Parity = 0
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => segmenter.CreateTextSegments(
                null!, -1, false, 10), Throws.TypeOf<QrInputException>());
            Assert.That(() => segmenter.CreateByteSegment(
                null!, -1, 10), Throws.TypeOf<QrInputException>());
            Assert.That(() => segmenter.OptimizeTextSegments(
                "123",
                QrSymbolKind.Micro,
                4,
                new int[0],
                true,
                -1,
                null,
                10,
                10), Throws.TypeOf<QrInputException>());
            Assert.That(() => segmenter.OptimizeTextSegments(
                "123",
                QrSymbolKind.rMQR,
                0,
                new int[0],
                false,
                -1,
                append,
                10,
                10), Throws.TypeOf<QrInputException>());
            Assert.That(() => segmenter.ValidateSegments(
                null!,
                QrSymbolKind.Model2,
                1,
                false,
                10,
                10), Throws.TypeOf<QrInputException>());
        });
    }

    private static QrEncodingMode[] GetModes(QrSegment[] segments)
    {
        QrEncodingMode[] modes = new QrEncodingMode[segments.Length];
        for (int index = 0; index < segments.Length; index++)
        {
            modes[index] = segments[index].Mode;
        }

        return modes;
    }
}
