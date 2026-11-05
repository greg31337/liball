using ttf;

namespace ttf.Tests;

[NonParallelizable]
internal class FontSubsetDeterminismTests
{
    const string ProbeCulture = "TTF_DETERMINISM_PROBE_CULTURE";
    const string ProbePath = "TTF_DETERMINISM_PROBE_PATH";

    [Test]
    public void OutputIsIndependentOfCurrentCulture()
    {
        System.Globalization.CultureInfo originalCulture =
            System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture =
            System.Globalization.CultureInfo.CurrentUICulture;

        try
        {
            SetCulture("tr-TR");
            byte[] turkish = CreateOutput(false);
            SetCulture("ar-SA");
            byte[] arabic = CreateOutput(false);

            Assert.That(arabic, Is.EqualTo(turkish));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture =
                originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture =
                originalUiCulture;
        }
    }

    [Test]
    public void OutputIsIndependentOfInputInsertionOrder()
    {
        byte[] forward = CreateOutput(false);
        byte[] reverse = CreateOutput(true);

        Assert.That(reverse, Is.EqualTo(forward));
    }

    [Test]
    public void RepeatedBuildsProduceIdenticalOutput()
    {
        FontSubsetBuilder builder = CreateBuilder(false);

        byte[] first = builder.BuildResult().CopyFontBytes();
        byte[] second = builder.BuildResult().CopyFontBytes();
        byte[] fresh = CreateOutput(false);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(fresh, Is.EqualTo(first));
        });
    }

    [Test]
    public void SeparateProcessRunsProduceIdenticalOutput()
    {
        string firstPath = CreateTemporaryPath();
        string secondPath = CreateTemporaryPath();

        try
        {
            RunProbe(firstPath, "tr-TR");
            RunProbe(secondPath, "ar-SA");

            Assert.That(
                System.IO.File.ReadAllBytes(secondPath),
                Is.EqualTo(System.IO.File.ReadAllBytes(firstPath)));
        }
        finally
        {
            DeleteIfPresent(firstPath);
            DeleteIfPresent(secondPath);
        }
    }

    [Test]
    public void ProcessProbeWritesSubsetOutput()
    {
        string? outputPath = System.Environment.GetEnvironmentVariable(
            ProbePath);

        if (outputPath == null)
        {
            return;
        }

        string? culture = System.Environment.GetEnvironmentVariable(
            ProbeCulture);

        if (culture != null)
        {
            SetCulture(culture);
        }

        System.IO.File.WriteAllBytes(outputPath, CreateOutput(false));
    }

    static byte[] CreateOutput(bool reverse)
    {
        return CreateBuilder(reverse).BuildResult().CopyFontBytes();
    }

    static FontSubsetBuilder CreateBuilder(bool reverse)
    {
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();
        options.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);
        FontSubsetBuilder builder = new FontSubsetBuilder(
            HarfBuzzLayoutDifferentialTests.Load().CreateDefaultInstance(),
            options);

        if (reverse)
        {
            builder.AddFeature(Tag("kern"));
            builder.AddFeature(Tag("liga"));
            builder.AddGlyph(2);
            builder.AddCodePoint(0x69);
            builder.AddCodePoint(0x66);
            builder.AddCodePoint(0x41);
        }
        else
        {
            builder.AddCodePoint(0x41);
            builder.AddCodePoint(0x66);
            builder.AddCodePoint(0x69);
            builder.AddGlyph(2);
            builder.AddFeature(Tag("liga"));
            builder.AddFeature(Tag("kern"));
        }

        return builder;
    }

    static void RunProbe(string outputPath, string culture)
    {
        string testDirectory = TestContext.CurrentContext.TestDirectory;
        string projectPath = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(
                testDirectory,
                "..",
                "..",
                "..",
                "ttf.Tests.csproj"));
        System.Diagnostics.ProcessStartInfo start =
            new System.Diagnostics.ProcessStartInfo("dotnet");
        start.ArgumentList.Add("test");
        start.ArgumentList.Add(projectPath);
        start.ArgumentList.Add("--no-build");
        start.ArgumentList.Add("--no-restore");
        start.ArgumentList.Add("--filter");
        start.ArgumentList.Add(
            "FullyQualifiedName=ttf.Tests.FontSubsetDeterminismTests.ProcessProbeWritesSubsetOutput");
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.Environment[ProbePath] = outputPath;
        start.Environment[ProbeCulture] = culture;

        using (System.Diagnostics.Process? process =
            System.Diagnostics.Process.Start(start))
        {
            System.Threading.Tasks.Task<string> standardOutput =
                process!.StandardOutput.ReadToEndAsync();
            System.Threading.Tasks.Task<string> standardError =
                process.StandardError.ReadToEndAsync();
            bool exited = process.WaitForExit(60000);

            if (!exited)
            {
                process.Kill(true);
                Assert.Fail("The deterministic-output probe timed out.");
            }

            string details = standardOutput.GetAwaiter().GetResult() +
                standardError.GetAwaiter().GetResult();
            Assert.That(process.ExitCode, Is.Zero, details);
        }

        Assert.That(System.IO.File.Exists(outputPath), Is.True);
    }

    static string CreateTemporaryPath()
    {
        return System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "ttf-determinism-" +
                System.Guid.NewGuid().ToString("N") +
                ".bin");
    }

    static void DeleteIfPresent(string path)
    {
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    static void SetCulture(string name)
    {
        System.Globalization.CultureInfo culture =
            System.Globalization.CultureInfo.GetCultureInfo(name);
        System.Globalization.CultureInfo.CurrentCulture = culture;
        System.Globalization.CultureInfo.CurrentUICulture = culture;
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
