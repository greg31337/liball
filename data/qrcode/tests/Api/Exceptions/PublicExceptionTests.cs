using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PublicExceptionTests
{
    [Test]
    public void InputExceptionRetainsMessageInputNameAndFailureReason()
    {
        QrInputException exception = new QrInputException("Stride is too small.", "Stride");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Stride is too small."));
            Assert.That(exception.InputName, Is.EqualTo("Stride"));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidInput));
        });
    }

    [Test]
    public void CapacityExceptionRetainsExactFitContext()
    {
        QrCapacityException exception = new QrCapacityException(
            "Payload does not fit.",
            QrSymbolKind.Micro,
            96,
            80);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Payload does not fit."));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.CapacityExceeded));
            Assert.That(exception.SymbolKind, Is.EqualTo(QrSymbolKind.Micro));
            Assert.That(exception.RequiredBitCount, Is.EqualTo(96));
            Assert.That(exception.AvailableBitCount, Is.EqualTo(80));
        });
    }

    [Test]
    public void DetectionExceptionRetainsCandidateAndAttemptDiagnostics()
    {
        QrDetectionException exception = new QrDetectionException(
            "Candidate geometry is invalid.",
            QrFailureReason.InvalidGeometry,
            12,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Candidate geometry is invalid."));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidGeometry));
            Assert.That(exception.CandidateCount, Is.EqualTo(12));
            Assert.That(exception.AttemptCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void DecodeExceptionRetainsCandidateAndRecoveryDiagnostics()
    {
        QrDecodeException exception = new QrDecodeException(
            "Payload mode is invalid.",
            QrFailureReason.InvalidMode,
            3,
            7);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Payload mode is invalid."));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.InvalidMode));
            Assert.That(exception.CandidateIndex, Is.EqualTo(3));
            Assert.That(exception.RecoveryAttemptCount, Is.EqualTo(7));
        });
    }

    [Test]
    public void ChecksumExceptionRetainsRepairLimitDiagnostics()
    {
        QrChecksumException exception = new QrChecksumException(
            "Too many codeword errors.",
            10,
            6,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Too many codeword errors."));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
            Assert.That(exception.ErrorCorrectionCodewordCount, Is.EqualTo(10));
            Assert.That(exception.ErrorCount, Is.EqualTo(6));
            Assert.That(exception.ErasureCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void StructuredAppendExceptionRetainsSequenceDiagnostics()
    {
        QrStructuredAppendException exception = new QrStructuredAppendException(
            "A sequence part is missing.",
            QrFailureReason.IncompleteSequence,
            4,
            3,
            0xA5);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("A sequence part is missing."));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.IncompleteSequence));
            Assert.That(exception.ExpectedPartCount, Is.EqualTo(4));
            Assert.That(exception.ActualPartCount, Is.EqualTo(3));
            Assert.That(exception.Parity, Is.EqualTo(0xA5));
        });
    }

    [Test]
    public void UnsupportedFeatureExceptionRetainsFeatureAndProtocolValue()
    {
        QrUnsupportedFeatureException exception = new QrUnsupportedFeatureException(
            "The ECI text conversion is unavailable.",
            QrFailureReason.UnsupportedTextEncoding,
            "ECI",
            28);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("The ECI text conversion is unavailable."));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.UnsupportedTextEncoding));
            Assert.That(exception.FeatureName, Is.EqualTo("ECI"));
            Assert.That(exception.ProtocolValue, Is.EqualTo(28));
        });
    }

    [Test]
    public void ResourceLimitExceptionRetainsRequestedAndMaximumValues()
    {
        QrResourceLimitException exception = new QrResourceLimitException(
            "Image pixel limit exceeded.",
            "MaximumImagePixels",
            70000000,
            67108864);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Image pixel limit exceeded."));
            Assert.That(exception.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
            Assert.That(exception.LimitName, Is.EqualTo("MaximumImagePixels"));
            Assert.That(exception.RequestedValue, Is.EqualTo(70000000));
            Assert.That(exception.MaximumValue, Is.EqualTo(67108864));
        });
    }
}
