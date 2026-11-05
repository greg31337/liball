namespace avif.Tests;

public class AvifExceptionTests
{
    [Test]
    public void BasicExceptionExposesCodeMessageAndEmptyDiagnostics()
    {
        AvifException exception = AvifException.Create(AvifResultCode.UnsupportedProtection, "encrypted sample");

        Assert.Multiple(() =>
        {
            Assert.That(exception.ResultCode, Is.EqualTo(AvifResultCode.UnsupportedProtection));
            Assert.That(exception.Message, Is.EqualTo("encrypted sample"));
            Assert.That(exception.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ExceptionSnapshotsDiagnosticArray()
    {
        AvifDiagnostic first = AvifDiagnostic.Create(
            AvifResultCode.TruncatedData,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Input,
            "missing bytes");
        AvifDiagnostic replacement = AvifDiagnostic.Create(
            AvifResultCode.InvalidData,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.General,
            "replacement");
        AvifDiagnostic[] supplied = [first];

        AvifException exception = AvifException.Create(AvifResultCode.TruncatedData, "read failed", supplied);
        supplied[0] = replacement;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(exception.Diagnostics[0], Is.SameAs(first));
        });
    }

    [Test]
    public void ExceptionNormalizesNullMessageAndDiagnosticArray()
    {
        string? nullMessage = null;
        AvifDiagnostic[]? nullDiagnostics = null;

        AvifException exception = AvifException.Create(
            AvifResultCode.UnknownError,
            nullMessage,
            nullDiagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.Empty);
            Assert.That(exception.Diagnostics, Is.Empty);
        });
    }
}
