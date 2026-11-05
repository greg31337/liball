using System.Text;
using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class EciTextTranscoderTests
{
    [TestCase(1)]
    [TestCase(3)]
    public void TranscodesIso88591WithExactBytes(int assignment)
    {
        byte[] bytes = EciTextTranscoder.EncodeText("Aé\0ÿ", assignment);
        string text = EciTextTranscoder.DecodeBytes(bytes, assignment);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new byte[] { 0x41, 0xE9, 0x00, 0xFF }));
            Assert.That(text, Is.EqualTo("Aé\0ÿ"));
        });
    }

    [Test]
    public void TranscodesUtf8Strictly()
    {
        byte[] bytes = EciTextTranscoder.EncodeText("A€😀", QrEciAssignments.Utf8);
        string text = EciTextTranscoder.DecodeBytes(bytes, QrEciAssignments.Utf8);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new byte[]
            {
                0x41, 0xE2, 0x82, 0xAC, 0xF0, 0x9F, 0x98, 0x80
            }));
            Assert.That(text, Is.EqualTo("A€😀"));
            Assert.That(() => EciTextTranscoder.EncodeText("\uD800", QrEciAssignments.Utf8),
                Throws.TypeOf<EncoderFallbackException>());
            Assert.That(() => EciTextTranscoder.DecodeBytes(
                new byte[] { 0xC0, 0xAF },
                QrEciAssignments.Utf8), Throws.TypeOf<DecoderFallbackException>());
        });
    }

    [Test]
    public void TranscodesAsciiStrictly()
    {
        byte[] bytes = EciTextTranscoder.EncodeText("A\0~", QrEciAssignments.UsAscii);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new byte[] { 0x41, 0x00, 0x7E }));
            Assert.That(EciTextTranscoder.DecodeBytes(bytes, QrEciAssignments.UsAscii), Is.EqualTo("A\0~"));
            Assert.That(() => EciTextTranscoder.EncodeText("é", QrEciAssignments.UsAscii),
                Throws.TypeOf<EncoderFallbackException>());
            Assert.That(() => EciTextTranscoder.DecodeBytes(
                new byte[] { 0x80 },
                QrEciAssignments.UsAscii), Throws.TypeOf<DecoderFallbackException>());
        });
    }

    [TestCase(25, new byte[] { 0x00, 0x41, 0xD8, 0x3D, 0xDE, 0x00 })]
    [TestCase(33, new byte[] { 0x41, 0x00, 0x3D, 0xD8, 0x00, 0xDE })]
    public void TranscodesUtf16VariantsWithExactByteOrder(int assignment, byte[] expected)
    {
        byte[] bytes = EciTextTranscoder.EncodeText("A😀", assignment);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(expected));
            Assert.That(EciTextTranscoder.DecodeBytes(bytes, assignment), Is.EqualTo("A😀"));
            Assert.That(() => EciTextTranscoder.DecodeBytes(new byte[] { 0x00 }, assignment),
                Throws.TypeOf<DecoderFallbackException>());
        });
    }

    [TestCase(34, new byte[] { 0x00, 0x00, 0x00, 0x41, 0x00, 0x01, 0xF6, 0x00 })]
    [TestCase(35, new byte[] { 0x41, 0x00, 0x00, 0x00, 0x00, 0xF6, 0x01, 0x00 })]
    public void TranscodesUtf32VariantsWithExactByteOrder(int assignment, byte[] expected)
    {
        byte[] bytes = EciTextTranscoder.EncodeText("A😀", assignment);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(expected));
            Assert.That(EciTextTranscoder.DecodeBytes(bytes, assignment), Is.EqualTo("A😀"));
            Assert.That(() => EciTextTranscoder.DecodeBytes(new byte[] { 0, 0, 0 }, assignment),
                Throws.TypeOf<DecoderFallbackException>());
        });
    }

    [Test]
    public void TranscodesStandardShiftJisWithoutVendorMappings()
    {
        string expectedText = "\0A¥\\あ漢";
        byte[] expectedBytes = new byte[] { 0x00, 0x41, 0x5C, 0x81, 0x5F, 0x82, 0xA0, 0x8A, 0xBF };

        byte[] bytes = EciTextTranscoder.EncodeText(expectedText, QrEciAssignments.ShiftJis);
        string text = EciTextTranscoder.DecodeBytes(bytes, QrEciAssignments.ShiftJis);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(expectedBytes));
            Assert.That(text, Is.EqualTo(expectedText));
            Assert.That(() => EciTextTranscoder.EncodeText("①", QrEciAssignments.ShiftJis),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => EciTextTranscoder.DecodeBytes(
                new byte[] { 0x87, 0x40 },
                QrEciAssignments.ShiftJis), Throws.TypeOf<QrDecodeException>());
        });
    }

    [Test]
    public void TranscodesStandardGb2312WithAsciiWithoutVendorMappings()
    {
        string expectedText = "\0A中文・";
        byte[] expectedBytes = new byte[] { 0x00, 0x41, 0xD6, 0xD0, 0xCE, 0xC4, 0xA1, 0xA4 };

        byte[] bytes = EciTextTranscoder.EncodeText(expectedText, QrEciAssignments.Gb2312);
        string text = EciTextTranscoder.DecodeBytes(bytes, QrEciAssignments.Gb2312);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(expectedBytes));
            Assert.That(text, Is.EqualTo(expectedText));
            Assert.That(() => EciTextTranscoder.EncodeText("·", QrEciAssignments.Gb2312),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => EciTextTranscoder.DecodeBytes(
                new byte[] { 0x81, 0x40 },
                QrEciAssignments.Gb2312), Throws.TypeOf<QrDecodeException>());
        });
    }

    [Test]
    public void RejectsIncompleteDedicatedDoubleByteSequences()
    {
        QrDecodeException shiftJis = Assert.Throws<QrDecodeException>(() =>
            EciTextTranscoder.DecodeBytes(new byte[] { 0x82 }, QrEciAssignments.ShiftJis))!;
        QrDecodeException gb2312 = Assert.Throws<QrDecodeException>(() =>
            EciTextTranscoder.DecodeBytes(new byte[] { 0xD6 }, QrEciAssignments.Gb2312))!;

        Assert.Multiple(() =>
        {
            Assert.That(shiftJis.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(gb2312.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
        });
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(20)]
    [TestCase(25)]
    [TestCase(26)]
    [TestCase(27)]
    [TestCase(29)]
    [TestCase(33)]
    [TestCase(34)]
    [TestCase(35)]
    public void ReportsEveryBuiltInAssignmentAsSupported(int assignment)
    {
        Assert.That(EciTextTranscoder.IsSupportedAssignment(assignment), Is.True);
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(28)]
    [TestCase(30)]
    [TestCase(31)]
    [TestCase(32)]
    [TestCase(899)]
    [TestCase(999)]
    public void PreservesUnsupportedAssignmentsByRefusingLossyConversion(int assignment)
    {
        Assert.That(EciTextTranscoder.IsSupportedAssignment(assignment), Is.False);

        QrUnsupportedFeatureException encode = Assert.Throws<QrUnsupportedFeatureException>(() =>
            EciTextTranscoder.EncodeText("payload", assignment))!;
        byte[] bytes = new byte[] { 0x00, 0xFF, 0x41 };
        QrUnsupportedFeatureException decode = Assert.Throws<QrUnsupportedFeatureException>(() =>
            EciTextTranscoder.DecodeBytes(bytes, assignment))!;

        Assert.Multiple(() =>
        {
            Assert.That(encode.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
            Assert.That(encode.FeatureName, Is.EqualTo("ECI"));
            Assert.That(encode.ProtocolValue, Is.EqualTo(assignment));
            Assert.That(decode.ProtocolValue, Is.EqualTo(assignment));
            Assert.That(bytes, Is.EqualTo(new byte[] { 0x00, 0xFF, 0x41 }));
        });
    }

    [Test]
    public void RejectsNullInputsAndOutOfRangeAssignments()
    {
        QrInputException nullText = Assert.Throws<QrInputException>(() =>
            EciTextTranscoder.EncodeText(null!, QrEciAssignments.Utf8))!;
        QrInputException nullBytes = Assert.Throws<QrInputException>(() =>
            EciTextTranscoder.DecodeBytes(null!, QrEciAssignments.Utf8))!;
        QrInputException negative = Assert.Throws<QrInputException>(() =>
            EciTextTranscoder.IsSupportedAssignment(-1))!;
        QrInputException tooLarge = Assert.Throws<QrInputException>(() =>
            EciTextTranscoder.IsSupportedAssignment(1000000))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullText.InputName, Is.EqualTo("text"));
            Assert.That(nullBytes.InputName, Is.EqualTo("bytes"));
            Assert.That(negative.InputName, Is.EqualTo("assignment"));
            Assert.That(tooLarge.InputName, Is.EqualTo("assignment"));
        });
    }

    [Test]
    public void RejectsEncodedInputBeyondTheConfiguredByteLimit()
    {
        string oversizedText = new string('A', QrLimits.DefaultMaximumInputBytes + 1);
        byte[] oversizedBytes = new byte[QrLimits.DefaultMaximumInputBytes + 1];

        QrResourceLimitException encode = Assert.Throws<QrResourceLimitException>(() =>
            EciTextTranscoder.EncodeText(oversizedText, QrEciAssignments.UsAscii))!;
        QrResourceLimitException decode = Assert.Throws<QrResourceLimitException>(() =>
            EciTextTranscoder.DecodeBytes(oversizedBytes, QrEciAssignments.UsAscii))!;

        Assert.Multiple(() =>
        {
            Assert.That(encode.LimitName, Is.EqualTo("MaximumInputBytes"));
            Assert.That(decode.LimitName, Is.EqualTo("MaximumInputBytes"));
        });
    }
}
