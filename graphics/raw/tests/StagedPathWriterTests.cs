using raw;

namespace raw.Tests;

public class StagedPathWriterTests
{
    [Test]
    public void SuccessfulStageKeepsOriginalUntilSiblingValidationThenReplacesIt()
    {
        string directory = CreateDirectory();
        string destination = Path.Combine(directory, "existing.cr2");
        byte[] original = new byte[] { 1, 2, 3, 4 };
        byte[] replacement = new byte[] { 9, 8, 7, 6, 5 };
        File.WriteAllBytes(destination, original);
        string staged = string.Empty;
        Func<string, bool> validator = path =>
        {
            staged = path;
            Assert.That(File.ReadAllBytes(destination), Is.EqualTo(original));
            Assert.That(Path.GetDirectoryName(path), Is.EqualTo(directory));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(replacement));
            if (!OperatingSystem.IsWindows())
            {
                Assert.That(File.GetUnixFileMode(path), Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite));
            }
            return true;
        };

        RawPathWriteResult result = RawStagedPathWriter.Write(destination, replacement, RawFormat.Cr2, validator);

        Assert.Multiple(() =>
        {
            Assert.That(result.Validated, Is.True);
            Assert.That(result.Committed, Is.True);
            Assert.That(result.DestinationPath, Is.EqualTo(Path.GetFullPath(destination)));
            Assert.That(result.TemporaryPath, Is.Empty);
            Assert.That(result.HasRecoverableTemporaryFile(), Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(File.ReadAllBytes(destination), Is.EqualTo(replacement));
            Assert.That(File.Exists(staged), Is.False);
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
        });
        Directory.Delete(directory, true);
    }

    [Test]
    public void RejectedStageLeavesOriginalUntouchedAndReportsRecoverableSibling()
    {
        string directory = CreateDirectory();
        string destination = Path.Combine(directory, "protected.nef");
        byte[] original = new byte[] { 21, 22, 23 };
        byte[] stagedBytes = new byte[] { 31, 32, 33, 34 };
        File.WriteAllBytes(destination, original);
        Func<string, bool> validator = path =>
        {
            Assert.That(File.ReadAllBytes(destination), Is.EqualTo(original));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(stagedBytes));
            return false;
        };

        RawPathWriteResult result = RawStagedPathWriter.Write(destination, stagedBytes, RawFormat.Nef, validator);

        Assert.Multiple(() =>
        {
            Assert.That(result.Validated, Is.False);
            Assert.That(result.Committed, Is.False);
            Assert.That(result.HasRecoverableTemporaryFile(), Is.True);
            Assert.That(Path.GetDirectoryName(result.TemporaryPath), Is.EqualTo(directory));
            Assert.That(File.Exists(result.TemporaryPath), Is.True);
            Assert.That(File.ReadAllBytes(result.TemporaryPath), Is.EqualTo(stagedBytes));
            Assert.That(File.ReadAllBytes(destination), Is.EqualTo(original));
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(RawDiagnosticCode.OutputValidationFailed));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("destination was not changed").And.Contain("recoverable"));
        });
        Directory.Delete(directory, true);
    }

    [Test]
    public void PublicCr2RewritePathValidatesStagedFileBeforeReplacingDestination()
    {
        string directory = CreateDirectory();
        string destination = Path.Combine(directory, "rewrite.cr2");
        byte[] source = Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options());
        RawDocument document = Open(source);
        RawWriteOptions options = new RawWriteOptions();
        byte[] expected = RawWriter.RewriteCr2(document, options);
        File.WriteAllBytes(destination, new byte[] { 41, 42, 43 });

        RawPathWriteResult result = RawWriter.RewriteCr2(destination, document, options);
        RawDocument reopened = Open(File.ReadAllBytes(destination));

        Assert.Multiple(() =>
        {
            Assert.That(result.Validated, Is.True);
            Assert.That(result.Committed, Is.True);
            Assert.That(result.HasRecoverableTemporaryFile(), Is.False);
            Assert.That(File.ReadAllBytes(destination), Is.EqualTo(expected));
            Assert.That(reopened.Format, Is.EqualTo(RawFormat.Cr2));
            Assert.That(((reopened.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Cr2Header ?? throw new System.InvalidOperationException("A required value is unavailable.")).RawIfdPath, Is.EqualTo("IFD3"));
        });
        Directory.Delete(directory, true);
    }

    [Test]
    public void PublicCustomNefEncodePathCommitsValidatedNewDestination()
    {
        string directory = CreateDirectory();
        string destination = Path.Combine(directory, "encoded.nef");
        RawProfileCatalogue catalogue = NefEncoderTests.QualifiedLossyCatalogue();
        RawNefWriteInput input = NefEncoderTests.ExtremeInput(8, 4, 8);
        RawWriteOptions options = NefEncoderTests.Options();
        options.AllowExperimentalProfiles = false;
        options.ProfileName = "test.nikon.nef.lossy.write";
        options.AllowLossy = true;
        options.DataLossConsent = true;

        RawPathWriteResult result = RawWriter.EncodeNef(destination, input, catalogue, options);
        RawDocument reopened = Open(File.ReadAllBytes(destination));

        Assert.Multiple(() =>
        {
            Assert.That(result.Validated, Is.True);
            Assert.That(result.Committed, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(File.Exists(destination), Is.True);
            Assert.That(reopened.Format, Is.EqualTo(RawFormat.Nef));
            Assert.That(reopened.PayloadMap.Find(RawPayloadKind.RawImage), Is.Not.Empty);
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
        });
        Directory.Delete(directory, true);
    }

    [Test]
    public void DestinationMustNameAFileInAnExistingDirectory()
    {
        string directory = CreateDirectory();
        string missing = Path.Combine(directory, "missing", "output.cr2");
        Func<string, bool> validator = path => true;

        RawWriteException? exception = Assert.Throws<RawWriteException>(() => RawStagedPathWriter.Write(missing, new byte[] { 1 }, RawFormat.Cr2, validator));

        Assert.Multiple(() =>
        {
            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.That(Directory.GetFiles(directory), Is.Empty);
        });
        Directory.Delete(directory, true);
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "raw-staged-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static RawDocument Open(byte[] source)
    {
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        options.ReadMetadata = true;
        options.ReadPreviews = true;
        options.ReadRawPayload = true;
        return RawCodec.Open(source, null, options);
    }
}
