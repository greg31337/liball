using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrSegmenterBehaviorTests
{
    [TestCase("123", QrEncodingMode.Numeric, "313233", -1, false)]
    [TestCase("ABC", QrEncodingMode.Alphanumeric, "414243", -1, false)]
    [TestCase("aé", QrEncodingMode.Byte, "61E9", -1, false)]
    [TestCase("😀", QrEncodingMode.Byte, "F09F9880", 26, false)]
    [TestCase("漢字", QrEncodingMode.Kanji, "8ABF8E9A", -1, false)]
    [TestCase("汉", QrEncodingMode.Hanzi, "BABA", -1, true)]
    public void CreatesEachPayloadModeWithExactBytesAndInputByteLimits(string text, QrEncodingMode mode, string hex, int eci, bool hanzi)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        byte[] bytes = Convert.FromHexString(hex);
        QrSegment[] segments = segmenter.CreateTextSegments(text, -1, hanzi, bytes.Length);
        Assert.That(segments, Has.Length.EqualTo(1));
        QrSegment segment = segments[0];
        Assert.That(segment.Mode, Is.EqualTo(mode));
        Assert.That(segment.Text, Is.EqualTo(text));
        Assert.That(segment.Data, Is.EqualTo(bytes));
        Assert.That(segment.CharacterCount, Is.EqualTo(mode == QrEncodingMode.Byte ? bytes.Length : text.Length));
        Assert.That(segment.EciAssignment, Is.EqualTo(eci));
        Assert.That(segment.IsHanziCompatibilityMode, Is.EqualTo(hanzi));
        segmenter.ValidateSegments(segments, QrSymbolKind.Model2, 1, hanzi, 1, bytes.Length);
        QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(() =>
            segmenter.CreateTextSegments(text, -1, hanzi, bytes.Length - 1))!;
        Assert.That(error.LimitName, Is.EqualTo("MaximumInputBytes"));
        Assert.That(error.RequestedValue, Is.EqualTo(bytes.Length));
        Assert.That(error.MaximumValue, Is.EqualTo(bytes.Length - 1));
    }

    [TestCase(QrSymbolKind.Model2, 1)]
    [TestCase(QrSymbolKind.Micro, 1)]
    [TestCase(QrSymbolKind.Micro, 2)]
    [TestCase(QrSymbolKind.Micro, 3)]
    [TestCase(QrSymbolKind.Micro, 4)]
    [TestCase(QrSymbolKind.rMQR, 0)]
    [TestCase(QrSymbolKind.LegacyModel1, 1)]
    public void ValidatesEveryModeAgainstItsProfile(QrSymbolKind kind, int version)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        foreach (QrEncodingMode mode in Enum.GetValues<QrEncodingMode>())
        {
            bool allowed = mode != QrEncodingMode.Terminator;
            if (kind == QrSymbolKind.Micro)
                allowed = mode == QrEncodingMode.Numeric || mode == QrEncodingMode.Alphanumeric && version >= 2
                    || (mode == QrEncodingMode.Byte || mode == QrEncodingMode.Kanji) && version >= 3;
            if (kind == QrSymbolKind.rMQR)
                allowed = allowed && mode != QrEncodingMode.Hanzi && mode != QrEncodingMode.StructuredAppend;
            if (kind == QrSymbolKind.LegacyModel1)
                allowed = mode == QrEncodingMode.Numeric || mode == QrEncodingMode.Alphanumeric || mode == QrEncodingMode.Byte || mode == QrEncodingMode.Kanji;
            TestDelegate validate = () => segmenter.ValidateSegments([Segment(mode)], kind, version, true, 1, 100);
            if (allowed) Assert.DoesNotThrow(validate, mode.ToString());
            else Assert.Throws<QrInputException>(validate, mode.ToString());
        }
    }

    [TestCase(QrEncodingMode.Numeric, "１")]
    [TestCase(QrEncodingMode.Numeric, "1A")]
    [TestCase(QrEncodingMode.Alphanumeric, "abc")]
    [TestCase(QrEncodingMode.Alphanumeric, "é")]
    [TestCase(QrEncodingMode.Kanji, "A")]
    [TestCase(QrEncodingMode.Kanji, "😀")]
    [TestCase(QrEncodingMode.Hanzi, "😀")]
    public void RejectsCharactersOutsideTheManualModeRepertoire(QrEncodingMode mode, string text)
    {
        QrSegment segment = Segment(mode);
        segment.Text = text;
        segment.CharacterCount = text.Length;
        IQrSegmenter segmenter = new QrSegmenter();
        Assert.Throws<QrInputException>(() => segmenter.ValidateSegments([segment], QrSymbolKind.Model2, 1, true, 1, 100));
    }

    [TestCase(false, 0xd800)]
    [TestCase(false, 0xdc00)]
    [TestCase(true, 0xd800)]
    [TestCase(true, 0xdc00)]
    public void TextCreationAndOptimizationRejectUnpairedSurrogates(bool optimize, int codeUnit)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        string text = "a" + (char)codeUnit;
        Assert.Throws<QrInputException>(() =>
        {
            if (optimize) Optimize(segmenter, text, []);
            else segmenter.CreateTextSegments(text, -1, false, 100);
        });
    }

    [Test]
    public void EmptyPayloadsAndControlOnlyRequestsRemainDistinct()
    {
        IQrSegmenter segmenter = new QrSegmenter();
        Assert.That(segmenter.CreateTextSegments("", -1, false, 0), Is.Empty);
        QrSegment bytes = segmenter.CreateByteSegment([], 899, 0);
        Assert.That(bytes.Mode, Is.EqualTo(QrEncodingMode.Byte));
        Assert.That(bytes.CharacterCount, Is.Zero);
        Assert.That(bytes.Data, Is.Empty);
        Assert.That(bytes.EciAssignment, Is.EqualTo(899));
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR })
        {
            int version = kind == QrSymbolKind.rMQR ? 0 : 1;
            Assert.That(segmenter.OptimizeTextSegments("", kind, version, [], false, -1, null, 1, 0), Is.Empty);
            segmenter.ValidateSegments([], kind, version, false, 1, 0);
        }
        QrStructuredAppendInfo info = new QrStructuredAppendInfo { Index = 0, Total = 2, Parity = 0 };
        QrSegment[] controls = segmenter.OptimizeTextSegments("", QrSymbolKind.Model2, 1, [], false, 42, info, 2, 0);
        Assert.That(controls.Select(s => s.Mode), Is.EqualTo(new[] { QrEncodingMode.StructuredAppend, QrEncodingMode.Fnc1Second }));
        Assert.That(controls[1].Data, Is.EqualTo(new byte[] { 42 }));
        Assert.That(controls[0].StructuredAppend, Is.Not.SameAs(info));
        Assert.Throws<QrResourceLimitException>(() => segmenter.OptimizeTextSegments("", QrSymbolKind.Model2, 1, [], false, 42, info, 1, 0));
    }

    [TestCase(-2)]
    [TestCase(1000000)]
    public void EmptyOptimizationStillRejectsInvalidEciPriorities(int assignment)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        foreach (string text in new[] { "", "123" })
            Assert.Throws<QrInputException>(() => Optimize(segmenter, text, [assignment]));
    }

    [Test]
    public void EmptyOptimizationStillRejectsUnavailableTranscoders()
    {
        IQrSegmenter segmenter = new QrSegmenter();
        foreach (string text in new[] { "", "123" })
        {
            QrUnsupportedFeatureException error = Assert.Throws<QrUnsupportedFeatureException>(() => Optimize(segmenter, text, [899]))!;
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
            Assert.That(error.ProtocolValue, Is.EqualTo(899));
        }
        Assert.That(Optimize(segmenter, "", [-1, 26, 26]), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ChecksAllManualControlOrderingsAndRejectsDuplicateHeaders(bool secondPosition)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        QrSegment[] items = [Segment(QrEncodingMode.StructuredAppend), Segment(secondPosition ? QrEncodingMode.Fnc1Second : QrEncodingMode.Fnc1First), Segment(QrEncodingMode.Eci), Segment(QrEncodingMode.Byte)];
        var orders = from a in Enumerable.Range(0, 4) from b in Enumerable.Range(0, 4)
                     from c in Enumerable.Range(0, 4) from d in Enumerable.Range(0, 4)
                     where new[] { a, b, c, d }.Distinct().Count() == 4 select new[] { a, b, c, d };
        foreach (int[] order in orders)
        {
            QrSegment[] segments = order.Select(i => items[i]).ToArray();
            string key = string.Concat(order);
            TestDelegate validate = () => segmenter.ValidateSegments(segments, QrSymbolKind.Model2, 1, false, 10, 100);
            if (key == "0123" || key == "0132") Assert.DoesNotThrow(validate, key);
            else Assert.Throws<QrInputException>(validate, key);
        }
        foreach (QrSegment header in items.Take(2))
            Assert.Throws<QrInputException>(() => segmenter.ValidateSegments([header, header], QrSymbolKind.Model2, 1, false, 10, 100));
        segmenter.ValidateSegments([items[2], items[3], items[2], items[3]], QrSymbolKind.Model2, 1, false, 10, 100);
    }

    [TestCase("a12", false)]
    [TestCase("a123", false)]
    [TestCase("a1234", true)]
    public void OptimizerPrefersFewerSegmentsAtAnEqualBitCost(string text, bool split)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        QrSegment[] segments = Optimize(segmenter, text, []);
        // For a123, one byte segment and byte("a") + numeric("123") both cost 44 bits.
        Assert.That(segments.Select(s => s.Mode), Is.EqualTo(split
            ? new[] { QrEncodingMode.Byte, QrEncodingMode.Numeric }
            : new[] { QrEncodingMode.Byte }));
        Assert.That(string.Concat(segments.Select(s => s.Text)), Is.EqualTo(text));
    }

    [TestCase(25, 26, "006120AC")]
    [TestCase(26, 25, "61E282AC")]
    public void OptimizerUsesCallerPriorityForEqualCostEciChoices(int first, int second, string hex)
    {
        IQrSegmenter segmenter = new QrSegmenter();
        int[] priorities = [first, second, first];
        for (int repeat = 0; repeat < 3; repeat++)
        {
            QrSegment[] segments = Optimize(segmenter, "a€", priorities);
            Assert.That(segments.Select(s => s.Mode), Is.EqualTo(new[] { QrEncodingMode.Eci, QrEncodingMode.Byte }));
            Assert.That(segments[0].EciAssignment, Is.EqualTo(first));
            Assert.That(segments[1].Data, Is.EqualTo(Convert.FromHexString(hex)));
            // Both encodings need 12 ECI bits, a 12-bit byte header, and 32 payload bits.
            Assert.That(segments[1].CharacterCount, Is.EqualTo(4));
            segmenter.ValidateSegments(segments, QrSymbolKind.Model2, 1, false, 2, 4);
            segments[1].Data[0] = 255;
        }
        Assert.That(priorities, Is.EqualTo(new[] { first, second, first }));
        QrSegment[] shorter = Optimize(segmenter, "€", priorities);
        Assert.That(shorter[0].EciAssignment, Is.EqualTo(25));
        Assert.That(shorter[1].Data, Is.EqualTo(new byte[] { 0x20, 0xac }));
    }

    static QrSegment[] Optimize(IQrSegmenter segmenter, string text, int[] priorities) =>
        segmenter.OptimizeTextSegments(text, QrSymbolKind.Model2, 1, priorities, false, -1, null, 10, 100);

    static QrSegment Segment(QrEncodingMode mode) => mode switch
    {
        QrEncodingMode.Numeric => new QrSegment { Mode = mode, Text = "1", CharacterCount = 1 },
        QrEncodingMode.Alphanumeric => new QrSegment { Mode = mode, Text = "A", CharacterCount = 1 },
        QrEncodingMode.Byte => new QrSegment { Mode = mode, Data = [65], CharacterCount = 1 },
        QrEncodingMode.Kanji => new QrSegment { Mode = mode, Text = "漢", CharacterCount = 1 },
        QrEncodingMode.Hanzi => new QrSegment { Mode = mode, Text = "汉", CharacterCount = 1, IsHanziCompatibilityMode = true },
        QrEncodingMode.Eci => new QrSegment { Mode = mode, EciAssignment = 26, Data = [26] },
        QrEncodingMode.Fnc1Second => new QrSegment { Mode = mode, Fnc1ApplicationIndicator = 42, Data = [42] },
        QrEncodingMode.StructuredAppend => new QrSegment { Mode = mode, StructuredAppend = new QrStructuredAppendInfo { Index = 0, Total = 2, Parity = 3 } },
        _ => new QrSegment { Mode = mode }
    };
}
