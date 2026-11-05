using System.Diagnostics;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffTestProjectCompileItemTests
{
    [Test]
    [NonParallelizable]
    public void TestProject_CompilesAllTestSourcesExceptBuildOutput()
    {
        string root = FindRepositoryRoot();
        ProcessResult result = GetCompileItems(root);

        Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardError);

        string[] compileItems = ParseCompileItems(result.StandardOutput);
        string[] expectedSources = FindExpectedTestSources(root);

        int index = 0;
        while (index < expectedSources.Length)
        {
            Assert.That(Contains(compileItems, expectedSources[index]), Is.True, expectedSources[index]);
            index = index + 1;
        }

        index = 0;
        while (index < compileItems.Length)
        {
            Assert.That(IsBuildOutput(compileItems[index]), Is.False, compileItems[index]);
            index = index + 1;
        }
    }

    static ProcessResult GetCompileItems(string root)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
        startInfo.WorkingDirectory = root;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(Path.Combine(root, "tests", "tiff.Tests.csproj"));
        startInfo.ArgumentList.Add("-getItem:Compile");
        startInfo.ArgumentList.Add("-noLogo");

        Process process = Process.Start(startInfo)!;
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(120000);
        if (!exited)
        {
            process.Kill(true);
            Assert.Fail("MSBuild compile item evaluation timed out.");
        }

        return new ProcessResult(process.ExitCode, outputTask.Result, errorTask.Result);
    }

    static string[] ParseCompileItems(string output)
    {
        List<string> items = new List<string>();
        JsonDocument document = JsonDocument.Parse(output);
        JsonElement compileItems = document.RootElement.GetProperty("Items").GetProperty("Compile");

        foreach (JsonElement compileItem in compileItems.EnumerateArray())
        {
            string identity = compileItem.GetProperty("Identity").GetString()!;
            items.Add(NormalizePath(identity));
        }

        items.Sort(StringComparer.Ordinal);
        return items.ToArray();
    }

    static string[] FindExpectedTestSources(string root)
    {
        string testsDirectory = Path.Combine(root, "tests");
        string[] files = Directory.GetFiles(testsDirectory, "*.cs", SearchOption.AllDirectories);
        List<string> sources = new List<string>();

        int index = 0;
        while (index < files.Length)
        {
            string relativePath = Path.GetRelativePath(testsDirectory, files[index]);
            relativePath = NormalizePath(relativePath);
            if (!IsBuildOutput(relativePath))
            {
                sources.Add(relativePath);
            }

            index = index + 1;
        }

        sources.Sort(StringComparer.Ordinal);
        return sources.ToArray();
    }

    static bool Contains(string[] values, string value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (string.Equals(values[index], value, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsBuildOutput(string relativePath)
    {
        string normalized = NormalizePath(relativePath);
        if (normalized.StartsWith("bin/", StringComparison.Ordinal))
        {
            return true;
        }

        if (normalized.Contains("/bin/"))
        {
            return true;
        }

        if (normalized.StartsWith("obj/", StringComparison.Ordinal))
        {
            return true;
        }

        if (normalized.Contains("/obj/"))
        {
            return true;
        }

        return false;
    }

    static string NormalizePath(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "tiff.slnx")) && Directory.Exists(Path.Combine(directory, "tests")))
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
