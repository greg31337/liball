using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class SignatureTests
{
    [Test]
    public void HasSignature_ReturnsTrueForPngSignatureAtStartOfArray()
    {
        byte[] data = new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7
        };

        Assert.That(PngSignature.HasSignature(data), Is.True);
    }

    [Test]
    public void HasSignature_ReturnsTrueForPngSignatureAtArrayOffset()
    {
        byte[] data = new byte[]
        {
            0x00,
            0x01,
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7,
            0x02
        };

        Assert.That(PngSignature.HasSignature(data, 2), Is.True);
        Assert.That(PngSignature.HasSignature(data), Is.False);
    }

    [Test]
    public void HasSignature_ReturnsFalseForMismatchedArrayBytes()
    {
        byte[] data = new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            0x00
        };

        Assert.That(PngSignature.HasSignature(data), Is.False);
    }

    [Test]
    public void HasSignature_ReturnsFalseForShortArray()
    {
        byte[] data = new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2
        };

        Assert.That(PngSignature.HasSignature(data), Is.False);
    }

    [Test]
    public void HasSignature_RejectsInvalidArrayInputs()
    {
        byte[] data = new byte[8];

        Assert.Throws<ArgumentNullException>(() => PngSignature.HasSignature((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => PngSignature.HasSignature(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignature.HasSignature(data, -1));
    }

    [Test]
    public void HasSignature_DetectsSignatureAtCurrentStreamPositionAndRestoresPosition()
    {
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0x00,
            0x01,
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7,
            0x02
        });

        stream.Position = 2;

        Assert.That(PngSignature.HasSignature(stream), Is.True);
        Assert.That(stream.Position, Is.EqualTo(2));
    }

    [Test]
    public void HasSignature_ReturnsFalseForShortStreamAndRestoresPosition()
    {
        MemoryStream stream = new MemoryStream(new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2
        });

        Assert.That(PngSignature.HasSignature(stream), Is.False);
        Assert.That(stream.Position, Is.EqualTo(0));
    }

    [Test]
    public void HasSignature_RejectsNullStream()
    {
        Assert.Throws<ArgumentNullException>(() => PngSignature.HasSignature((Stream)null!));
    }

    [Test]
    public void ValidateSignature_ReturnsValidResultForExactArraySignature()
    {
        byte[] data = new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7
        };

        PngValidationResult result = PngSignature.ValidateSignature(data, PngErrorPolicy.Strict);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void ValidateSignature_StrictModeReportsFatalErrorForMismatch()
    {
        byte[] data = new byte[]
        {
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            0x00
        };

        PngValidationResult result = PngSignature.ValidateSignature(data, PngErrorPolicy.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SIGNATURE_MISMATCH"));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(0));
        Assert.That(result.Errors[0].IsFatal, Is.True);
    }

    [TestCase(PngErrorPolicy.TolerantMetadata)]
    [TestCase(PngErrorPolicy.TolerantRepair)]
    [TestCase(PngErrorPolicy.BestEffortInspection)]
    public void ValidateSignature_TolerantModesReportWarningForMismatch(PngErrorPolicy errorPolicy)
    {
        byte[] data = new byte[]
        {
            0x00,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            PngConstants.SignatureByte7
        };

        PngValidationResult result = PngSignature.ValidateSignature(data, errorPolicy);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Warnings, Has.Length.EqualTo(1));
        Assert.That(result.Warnings[0].Code, Is.EqualTo("PNG_SIGNATURE_MISMATCH"));
        Assert.That(result.Warnings[0].Offset, Is.EqualTo(0));
    }

    [Test]
    public void ValidateSignature_ReportsShortSignatureAtArrayOffset()
    {
        byte[] data = new byte[]
        {
            0xFF,
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1
        };

        PngValidationResult result = PngSignature.ValidateSignature(data, 1, PngErrorPolicy.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SIGNATURE_TOO_SHORT"));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(1));
    }

    [Test]
    public void ValidateSignature_StreamRestoresPositionAndReportsOriginalOffset()
    {
        MemoryStream stream = new MemoryStream(new byte[]
        {
            0xAA,
            0xBB,
            PngConstants.SignatureByte0,
            PngConstants.SignatureByte1,
            PngConstants.SignatureByte2,
            PngConstants.SignatureByte3,
            PngConstants.SignatureByte4,
            PngConstants.SignatureByte5,
            PngConstants.SignatureByte6,
            0x00
        });
        stream.Position = 2;

        PngValidationResult result = PngSignature.ValidateSignature(stream, PngErrorPolicy.Strict);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_SIGNATURE_MISMATCH"));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(2));
        Assert.That(stream.Position, Is.EqualTo(2));
    }

    [Test]
    public void ValidateSignature_RejectsInvalidInputs()
    {
        byte[] data = new byte[8];

        Assert.Throws<ArgumentNullException>(() => PngSignature.ValidateSignature((byte[])null!, PngErrorPolicy.Strict));
        Assert.Throws<ArgumentNullException>(() => PngSignature.ValidateSignature((Stream)null!, PngErrorPolicy.Strict));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignature.ValidateSignature(data, -1, PngErrorPolicy.Strict));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngSignature.ValidateSignature(data, (PngErrorPolicy)99));
    }
}
