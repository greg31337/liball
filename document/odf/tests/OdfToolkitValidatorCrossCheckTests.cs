using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class OdfToolkitValidatorCrossCheckTests
{
    const string ValidatorJarEnvironmentVariable =
        "ODF_TOOLKIT_VALIDATOR_JAR";

    [Test]
    public void SdkMatchesRecordedOdfToolkitOutcomesForSharedCorpus()
    {
        CrossCheckFixture[] corpus = CreateCorpus();

        Assert.Multiple(() =>
        {
            Assert.That(corpus.Count(item => item.ExpectedValid), Is.EqualTo(2));
            Assert.That(corpus.Count(item => !item.ExpectedValid), Is.EqualTo(3));
            Assert.That(
                corpus.Select(item => item.Name),
                Is.Unique);
        });

        IOdfDocumentReader reader = new OdfDocumentReader();
        IOdfValidator validator = new OdfValidator();
        foreach (CrossCheckFixture fixture in corpus)
        {
            bool actual = ValidateWithSdk(
                reader,
                validator,
                fixture);
            Assert.That(
                actual,
                Is.EqualTo(fixture.ExpectedValid),
                fixture.Name);
        }
    }

    [Test]
    public void ConfiguredOdfToolkitValidatorMatchesSdkForSharedCorpus()
    {
        string jarPath = Environment.GetEnvironmentVariable(
            ValidatorJarEnvironmentVariable) ?? "";
        if (jarPath.Length == 0 || !File.Exists(jarPath))
        {
            Assert.Ignore(
                ValidatorJarEnvironmentVariable
                    + " must name the ODF Toolkit validator jar-with-dependencies."
                    + " The recorded-oracle corpus test remains active without Java.");
        }

        ToolkitValidationResult identity = RunToolkit(
            jarPath,
            ["-V"]);
        Assert.Multiple(() =>
        {
            Assert.That(identity.Completed, Is.True, identity.Output);
            Assert.That(identity.IsValid, Is.True, identity.Output);
            Assert.That(
                identity.Output,
                Does.Contain("odfvalidator v").IgnoreCase);
        });

        string directory = Path.Combine(
            Path.GetTempPath(),
            "odf-toolkit-cross-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            IOdfDocumentReader reader = new OdfDocumentReader();
            IOdfValidator validator = new OdfValidator();
            foreach (CrossCheckFixture fixture in CreateCorpus())
            {
                string fixturePath = Path.Combine(directory, fixture.Name);
                File.WriteAllBytes(fixturePath, fixture.Data);

                bool sdkValid = ValidateWithSdk(reader, validator, fixture);
                ToolkitValidationResult toolkit = ValidateWithToolkit(
                    jarPath,
                    fixturePath);

                Assert.Multiple(() =>
                {
                    Assert.That(
                        toolkit.Completed,
                        Is.True,
                        toolkit.Output);
                    Assert.That(
                        toolkit.IsValid,
                        Is.EqualTo(fixture.ExpectedValid),
                        fixture.Name + Environment.NewLine + toolkit.Output);
                    Assert.That(
                        sdkValid,
                        Is.EqualTo(toolkit.IsValid),
                        fixture.Name + Environment.NewLine + toolkit.Output);
                });
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static CrossCheckFixture[] CreateCorpus()
    {
        byte[] text = CreateValidPackage(OdfDocumentKind.Text);
        byte[] spreadsheet = CreateValidPackage(
            OdfDocumentKind.Spreadsheet);
        return
        [
            Fixture("valid-text.odt", true, text),
            Fixture("valid-spreadsheet.ods", true, spreadsheet),
            Fixture(
                "invalid-content-root.odt",
                false,
                ReplaceContentRoot(text)),
            Fixture(
                "missing-manifest.odt",
                false,
                RemovePackageEntry(text, OdfPartNames.Manifest)),
            Fixture(
                "missing-mimetype.odt",
                false,
                RemovePackageEntry(text, OdfPartNames.Mimetype))
        ];
    }

    static CrossCheckFixture Fixture(
        string name,
        bool expectedValid,
        byte[] data)
    {
        CrossCheckFixture result = new CrossCheckFixture();
        result.Name = name;
        result.ExpectedValid = expectedValid;
        result.Data = data;
        return result;
    }

    static byte[] CreateValidPackage(OdfDocumentKind kind)
    {
        IOdfDocumentFactory factory = new OdfDocumentFactory();
        IOdfDocumentWriter documentWriter = new OdfDocumentWriter();
        OdfSaveOptions saveOptions = new OdfSaveOptions
        {
            Deterministic = true,
            PreserveTimestamps = false,
            TargetVersion = OdfVersion.Version13
        };
        byte[] initial = documentWriter.WriteDocumentBytes(
            factory.CreatePackageDocument(kind),
            saveOptions);

        IOdfPackageReader packageReader = new OdfPackageReader();
        OdfPackage package = packageReader.ReadPackageBytes(
            initial,
            new OdfLoadOptions());
        OdfManifest manifest = package.Manifest!;
        manifest.Version = "1.3";
        foreach (OdfManifestEntry entry in manifest.Entries)
        {
            if (entry.FullPath == OdfPartNames.Root)
            {
                entry.Version = "1.3";
            }
        }

        IOdfManifestService manifestService = new OdfManifestService();
        byte[] manifestBytes = manifestService.WriteManifestBytes(
            manifest,
            saveOptions);
        IOdfPackageWriter packageWriter = new OdfPackageWriter();
        OdfEditResult edit = packageWriter.ReplacePackageEntry(
            package,
            OdfPartNames.Manifest,
            manifestBytes);
        Assert.That(edit.Status, Is.EqualTo(OdfEditStatus.Success));
        return packageWriter.WritePackageBytes(package, saveOptions);
    }

    static byte[] ReplaceContentRoot(byte[] source)
    {
        OdfPackage package = new OdfPackageReader().ReadPackageBytes(
            source,
            new OdfLoadOptions());
        string content = Encoding.UTF8.GetString(
            ReadPackageEntry(package, OdfPartNames.Content));
        string invalid = content.Replace(
            "office:document-content",
            "office:not-a-document",
            StringComparison.Ordinal);
        Assert.That(invalid, Is.Not.EqualTo(content));
        using MemoryStream stream = new MemoryStream();
        stream.Write(source, 0, source.Length);
        using (ZipArchive archive = new ZipArchive(
            stream,
            ZipArchiveMode.Update,
            true))
        {
            ZipArchiveEntry? entry = archive.GetEntry(OdfPartNames.Content);
            Assert.That(entry, Is.Not.Null);
            entry!.Delete();
            ZipArchiveEntry replacement = archive.CreateEntry(
                OdfPartNames.Content,
                CompressionLevel.Optimal);
            using Stream output = replacement.Open();
            byte[] data = Encoding.UTF8.GetBytes(invalid);
            output.Write(data, 0, data.Length);
        }
        return stream.ToArray();
    }

    static byte[] RemovePackageEntry(byte[] source, string path)
    {
        using MemoryStream stream = new MemoryStream();
        stream.Write(source, 0, source.Length);
        using (ZipArchive archive = new ZipArchive(
            stream,
            ZipArchiveMode.Update,
            true))
        {
            ZipArchiveEntry? entry = archive.GetEntry(path);
            Assert.That(entry, Is.Not.Null);
            entry!.Delete();
        }
        return stream.ToArray();
    }

    static byte[] ReadPackageEntry(OdfPackage package, string path)
    {
        Assert.That(package.EntryIndex, Is.Not.Null);
        using Stream input = package.EntryIndex!.OpenEntry(path);
        using MemoryStream output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    static bool ValidateWithSdk(
        IOdfDocumentReader reader,
        IOdfValidator validator,
        CrossCheckFixture fixture)
    {
        try
        {
            OdfDocument document = reader.ReadDocumentBytes(
                fixture.Data,
                fixture.Name,
                new OdfLoadOptions());
            OdfDiagnostic[] diagnostics = validator.ValidateDocument(
                document,
                new OdfValidationOptions
                {
                    Mode = OdfValidationMode.Complete,
                    Version = OdfVersion.Version13,
                    IncludeWarnings = true,
                    IncludeInformation = true
                });
            return diagnostics.All(
                item => item.Severity != OdfValidationSeverity.Error
                    && item.Severity != OdfValidationSeverity.Fatal);
        }
        catch (Exception)
        {
            return false;
        }
    }

    static ToolkitValidationResult ValidateWithToolkit(
        string jarPath,
        string fixturePath)
    {
        return RunToolkit(jarPath, ["-w", fixturePath]);
    }

    static ToolkitValidationResult RunToolkit(
        string jarPath,
        string[] arguments)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "java";
        startInfo.ArgumentList.Add("-jar");
        startInfo.ArgumentList.Add(jarPath);
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        using Process process = new Process();
        process.StartInfo = startInfo;
        bool started = process.Start();
        if (!started)
        {
            return new ToolkitValidationResult();
        }
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        bool exited = process.WaitForExit(30000);
        if (!exited)
        {
            process.Kill(true);
            process.WaitForExit();
        }
        ToolkitValidationResult result = new ToolkitValidationResult();
        result.Completed = exited;
        result.Output = standardOutput + standardError;
        result.IsValid = result.Completed
            && process.ExitCode == 0
            && !ContainsValidationError(result.Output);
        return result;
    }

    static bool ContainsValidationError(string output)
    {
        return output.Contains("Error:", StringComparison.OrdinalIgnoreCase)
            || output.Contains("Fatal:", StringComparison.OrdinalIgnoreCase)
            || output.Contains("Exception", StringComparison.OrdinalIgnoreCase);
    }

    class CrossCheckFixture
    {
        public string Name = "";
        public bool ExpectedValid;
        public byte[] Data = [];
    }

    class ToolkitValidationResult
    {
        public bool Completed;
        public bool IsValid;
        public string Output = "";
    }
}
