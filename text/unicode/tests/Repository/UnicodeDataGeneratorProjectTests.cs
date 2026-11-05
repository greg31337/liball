using System.Diagnostics;
using System.Xml.Linq;
using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodeDataGeneratorProjectTests
{
    [Test]
    public void GeneratorProject_IsIncludedInSolutionAndUsesOnlyBuiltInApis()
    {
        string root = FindRepositoryRoot();
        string solution = File.ReadAllText(Path.Combine(root, "unicode.slnx"));
        XDocument project = XDocument.Load(Path.Combine(root, "tools", "UnicodeDataGenerator", "UnicodeDataGenerator.csproj"));

        Assert.That(solution, Does.Contain("tools/UnicodeDataGenerator/UnicodeDataGenerator.csproj"));
        Assert.That(project.Descendants("TargetFramework").Single().Value, Is.EqualTo("net10.0"));
        Assert.That(project.Descendants("PackageReference"), Is.Empty);
        Assert.That(project.Descendants("ProjectReference"), Is.Empty);
    }

    [Test]
    public void GeneratorProject_ContainsPlannedSourceFiles()
    {
        string root = FindRepositoryRoot();
        string[] expectedFiles =
        [
            Path.Combine("tools", "UnicodeDataGenerator", "UnicodeDataGenerator.csproj"),
            Path.Combine("tools", "UnicodeDataGenerator", "Program.cs"),
            Path.Combine("tools", "UnicodeDataGenerator", "UnicodeDataFiles.cs"),
            Path.Combine("tools", "UnicodeDataGenerator", "UnicodeDataParser.cs"),
            Path.Combine("tools", "UnicodeDataGenerator", "UnicodeTableWriter.cs"),
            Path.Combine("tools", "UnicodeDataGenerator", "UnicodeTestDataWriter.cs"),
        ];

        foreach (string expectedFile in expectedFiles)
        {
            Assert.That(File.Exists(Path.Combine(root, expectedFile)), Is.True, expectedFile);
        }
    }

    [Test]
    public void GeneratorCli_VersionCommandPrintsPinnedUnicodeVersion()
    {
        string root = FindRepositoryRoot();
        string projectPath = Path.Combine(root, "tools", "UnicodeDataGenerator", "UnicodeDataGenerator.csproj");
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", "run --project \"" + projectPath + "\" -- version");
        startInfo.WorkingDirectory = root;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using Process process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        bool exited = process.WaitForExit(30000);

        Assert.That(exited, Is.True, "Generator did not exit within timeout.");
        Assert.That(process.ExitCode, Is.EqualTo(0), error);
        Assert.That(output.Trim(), Is.EqualTo("17.0.0"));
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "unicode.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
