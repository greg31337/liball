using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ByteEncodingInferenceTests
{
    [Test]
    public void UsesDefaultInterpretationWhenInferenceIsDisabled()
    {
        byte[] utf8Bytes = new byte[] { 0xC3, 0xA9 };

        int assignment = ByteEncodingInference.ResolveAssignment(utf8Bytes, -1, false, false);
        string text = ByteEncodingInference.DecodeText(utf8Bytes, -1, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(assignment, Is.EqualTo(QrEciAssignments.Iso8859_1));
            Assert.That(text, Is.EqualTo("Ã©"));
        });
    }

    [Test]
    public void ExplicitAssignmentAlwaysTakesPrecedence()
    {
        byte[] gb2312Bytes = new byte[] { 0xD6, 0xD0, 0xCE, 0xC4 };

        int assignment = ByteEncodingInference.ResolveAssignment(
            gb2312Bytes,
            QrEciAssignments.Gb2312,
            true,
            true);
        string text = ByteEncodingInference.DecodeText(
            gb2312Bytes,
            QrEciAssignments.Gb2312,
            true,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(assignment, Is.EqualTo(QrEciAssignments.Gb2312));
            Assert.That(text, Is.EqualTo("中文"));
        });
    }

    [TestCase(new byte[] { 0xEF, 0xBB, 0xBF, 0x41 }, 26)]
    [TestCase(new byte[] { 0xFE, 0xFF, 0x00, 0x41 }, 25)]
    [TestCase(new byte[] { 0xFF, 0xFE, 0x41, 0x00 }, 33)]
    [TestCase(new byte[] { 0x00, 0x00, 0xFE, 0xFF }, 34)]
    [TestCase(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }, 35)]
    public void RecognizesUnicodeByteOrderMarksBeforeOtherHeuristics(
        byte[] bytes,
        int expectedAssignment)
    {
        Assert.That(
            ByteEncodingInference.ResolveAssignment(bytes, -1, true, false),
            Is.EqualTo(expectedAssignment));
    }

    [Test]
    public void InfersStrictUtf8OnlyWhenMultibyteEvidenceExists()
    {
        byte[] utf8Bytes = new byte[] { 0x41, 0xE2, 0x82, 0xAC, 0xF0, 0x9F, 0x98, 0x80 };
        byte[] asciiBytes = new byte[] { 0x41, 0x42, 0x43 };
        byte[] invalidUtf8 = new byte[] { 0x80 };

        Assert.Multiple(() =>
        {
            Assert.That(ByteEncodingInference.ResolveAssignment(utf8Bytes, -1, true, false),
                Is.EqualTo(QrEciAssignments.Utf8));
            Assert.That(ByteEncodingInference.DecodeText(utf8Bytes, -1, true, false),
                Is.EqualTo("A€😀"));
            Assert.That(ByteEncodingInference.ResolveAssignment(asciiBytes, -1, true, false),
                Is.EqualTo(QrEciAssignments.Iso8859_1));
            Assert.That(ByteEncodingInference.ResolveAssignment(invalidUtf8, -1, true, false),
                Is.EqualTo(QrEciAssignments.Iso8859_1));
        });
    }

    [Test]
    public void InfersStandardShiftJisAndGb2312Deterministically()
    {
        byte[] shiftJis = new byte[] { 0x82, 0xA0, 0x8A, 0xBF };
        byte[] gb2312 = new byte[] { 0xD6, 0xD0, 0xCE, 0xC4 };
        byte[] validInBoth = new byte[] { 0xE0, 0xA1 };

        Assert.Multiple(() =>
        {
            Assert.That(ByteEncodingInference.ResolveAssignment(shiftJis, -1, true, false),
                Is.EqualTo(QrEciAssignments.ShiftJis));
            Assert.That(ByteEncodingInference.DecodeText(shiftJis, -1, true, false),
                Is.EqualTo("あ漢"));
            Assert.That(ByteEncodingInference.ResolveAssignment(gb2312, -1, true, false),
                Is.EqualTo(QrEciAssignments.Gb2312));
            Assert.That(ByteEncodingInference.DecodeText(gb2312, -1, true, false),
                Is.EqualTo("中文"));
            Assert.That(ByteEncodingInference.ResolveAssignment(validInBoth, -1, true, false),
                Is.EqualTo(QrEciAssignments.ShiftJis));
        });
    }

    [Test]
    public void FallsBackDeterministicallyToDefaultForUnknownBytes()
    {
        byte[] bytes = new byte[] { 0x80, 0xFF, 0x41 };

        int first = ByteEncodingInference.ResolveAssignment(bytes, -1, true, false);
        int second = ByteEncodingInference.ResolveAssignment(bytes, -1, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(QrEciAssignments.Iso8859_1));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(ByteEncodingInference.DecodeText(bytes, -1, true, false),
                Is.EqualTo("\u0080ÿA"));
            Assert.That(bytes, Is.EqualTo(new byte[] { 0x80, 0xFF, 0x41 }));
        });
    }

    [Test]
    public void RequiresExplicitEciForInferredNonDefaultEncoding()
    {
        byte[] utf8Bytes = new byte[] { 0xC3, 0xA9 };
        byte[] asciiBytes = new byte[] { 0x41 };

        QrDecodeException exception = Assert.Throws<QrDecodeException>(() =>
            ByteEncodingInference.ResolveAssignment(utf8Bytes, -1, true, true))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidData));
            Assert.That(ByteEncodingInference.ResolveAssignment(asciiBytes, -1, true, true),
                Is.EqualTo(QrEciAssignments.Iso8859_1));
            Assert.That(ByteEncodingInference.ResolveAssignment(
                utf8Bytes,
                QrEciAssignments.Utf8,
                true,
                true), Is.EqualTo(QrEciAssignments.Utf8));
        });
    }

    [Test]
    public void ExplicitEciPolicyDoesNotEnableInferenceByItself()
    {
        byte[] utf8Bytes = new byte[] { 0xC3, 0xA9 };

        int assignment = ByteEncodingInference.ResolveAssignment(utf8Bytes, -1, false, true);

        Assert.That(assignment, Is.EqualTo(QrEciAssignments.Iso8859_1));
    }

    [Test]
    public void RejectsNullOversizedAndInvalidExplicitInputs()
    {
        byte[] empty = new byte[0];
        QrInputException nullBytes = Assert.Throws<QrInputException>(() =>
            ByteEncodingInference.ResolveAssignment(null!, -1, true, false))!;
        QrInputException invalidSentinel = Assert.Throws<QrInputException>(() =>
            ByteEncodingInference.ResolveAssignment(empty, -2, true, false))!;
        QrInputException tooLargeAssignment = Assert.Throws<QrInputException>(() =>
            ByteEncodingInference.ResolveAssignment(empty, 1000000, true, false))!;
        byte[] oversized = new byte[QrLimits.DefaultMaximumInputBytes + 1];
        QrResourceLimitException oversizedInput = Assert.Throws<QrResourceLimitException>(() =>
            ByteEncodingInference.ResolveAssignment(oversized, -1, true, false))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullBytes.InputName, Is.EqualTo("bytes"));
            Assert.That(invalidSentinel.InputName, Is.EqualTo("explicitAssignment"));
            Assert.That(tooLargeAssignment.InputName, Is.EqualTo("assignment"));
            Assert.That(oversizedInput.LimitName, Is.EqualTo("MaximumInputBytes"));
        });
    }
}
