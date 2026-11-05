using System.Diagnostics;
using System.Text.Json;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxTestProjectTests
{
    [Test]
    public void TestProject_CompilesAllTestSourcesExceptBuildOutput()
    {
        string root = FindRepositoryRoot();
        string testsDirectory = Path.Combine(root, "tests");
        string projectPath = Path.Combine(testsDirectory, "docx.Tests.csproj");
        string[] expectedSources = Directory.GetFiles(testsDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file))
            .Select(file => NormalizeRelativePath(testsDirectory, file))
            .Order(StringComparer.Ordinal)
            .ToArray();

        string[] compileItems = GetCompileItems(projectPath)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.That(compileItems, Is.EqualTo(expectedSources));
    }

    static string[] GetCompileItems(string projectPath)
    {
        string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        ProcessStartInfo startInfo = new ProcessStartInfo(dotnet);
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-getItem:Compile");
        startInfo.ArgumentList.Add("-nologo");
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start dotnet msbuild.");
        bool exited = process.WaitForExit(30000);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("dotnet msbuild did not complete within 30 seconds.");
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        if (process.ExitCode != 0)
        {
            Assert.Fail("dotnet msbuild failed with exit code " + process.ExitCode.ToString() + ".\n" + output + error);
        }

        using JsonDocument document = JsonDocument.Parse(output);
        JsonElement compile = document.RootElement.GetProperty("Items").GetProperty("Compile");
        List<string> items = new List<string>();

        foreach (JsonElement item in compile.EnumerateArray())
        {
            string? identity = item.GetProperty("Identity").GetString();
            if (identity != null)
            {
                items.Add(identity.Replace('\\', '/'));
            }
        }

        return items.ToArray();
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "docx.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
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

    static bool IsBuildOutput(string file)
    {
        string relativePath = file.Replace(Path.DirectorySeparatorChar, '/');
        return relativePath.Contains("/bin/") || relativePath.Contains("/obj/");
    }

    static string NormalizeRelativePath(string root, string file)
    {
        return Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
    }
}
