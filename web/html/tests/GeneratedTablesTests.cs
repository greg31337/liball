using System.Diagnostics;

namespace UniversalHtml.Tests;

[TestFixture]
public class GeneratedTablesTests
{
    [Test]
    public void GeneratedManifestExposesPinnedInputsAsSnapshots()
    {
        Assert.That(GeneratedTableManifest.GetGeneratorVersion(), Is.EqualTo("1.3.0"));
        Assert.That(GeneratedTableManifest.GetInputIds(), Is.EqualTo(new[]
        {
            "css-safe-data", "doctype-foreign-names", "encoding-indexes", "encoding-labels",
            "html-entities", "html-names", "standards"
        }));
        Assert.That(GeneratedTableManifest.GetInputChecksums(), Is.EqualTo(new[]
        {
            "f4e1ae64455c76183a0f650adde0166d20c957ff48e1a2c05690e8aae0eb900d",
            "976db71abc1a118419238cf626f3991f8d2358ee0eaf002753f7cd313a629df9",
            "21e9a2e5df74e9cabd0714ab4be455eb9822ae75c6b37c2bf5321e2dfbd44034",
            "796d0608d654036705cb700a0cd257ad6883e338e28c1d214da308d90542222d",
            "db3704414d6d802bf72368ab810d4ac5465c36834a196cd8f4b48e77c0c0ed56",
            "959ba7439ae01949b117bc7940a02197c6c351c91400ed5cab6f9fc43b47066f",
            "54187f255164508e7a2bf43652eddf6fb3f9f3cc375760cde5e05477e7aac743"
        }));
        Assert.That(GeneratedTableManifest.GetStandardIds(), Is.EqualTo(new[]
        {
            "css-color-4", "css-syntax-3", "css-values-4", "whatwg-encoding", "whatwg-html"
        }));
        Assert.That(GeneratedTableManifest.GetStandardUrls(), Is.EqualTo(new[]
        {
            "https://www.w3.org/TR/2026/CRD-css-color-4-20260806/",
            "https://www.w3.org/TR/2021/CRD-css-syntax-3-20211224/",
            "https://www.w3.org/TR/2024/WD-css-values-4-20240312/",
            "https://encoding.spec.whatwg.org/",
            "https://html.spec.whatwg.org/"
        }));
        Assert.That(GeneratedTableManifest.GetStandardRevisions(), Has.Length.EqualTo(5));

        string[] snapshot = GeneratedTableManifest.GetStandardIds();
        snapshot[0] = "changed";
        Assert.That(GeneratedTableManifest.GetStandardIds()[0], Is.EqualTo("css-color-4"));
    }

    [Test]
    public void CommittedGeneratedOutputIsCurrent()
    {
        string root = FindRepositoryRoot();
        string configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent.Name;
        string tool = Path.Combine(root, "tools", "GenerateTables", "bin", configuration, "net10.0", "GenerateTables.dll");
        Assert.That(File.Exists(tool), Is.True, $"Generator was not built at {tool}");

        ProcessStartInfo startInfo = new("dotnet");
        startInfo.ArgumentList.Add(tool);
        startInfo.ArgumentList.Add("--check");
        startInfo.ArgumentList.Add("--root");
        startInfo.ArgumentList.Add(root);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;

        using Process process = Process.Start(startInfo);
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        bool exited = process.WaitForExit(30000);
        if (!exited)
        {
            process.Kill(true);
            Assert.Fail("GenerateTables --check did not finish within 30 seconds.");
        }

        Assert.That(process.ExitCode, Is.EqualTo(0), standardOutput + Environment.NewLine + standardError);
        Assert.That(standardOutput, Does.Contain("Generated output is current"));
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "html.csproj"))
                && File.Exists(Path.Combine(directory.FullName, "tools", "GenerateTables", "inputs", "manifest.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root from the test output directory.");
    }
}
