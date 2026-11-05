using System.Diagnostics;
using System.Text.Json;
using htmlgui;

namespace htmlgui.Tests;

public class BrowserRuntimeBootstrapTests
{
    [Test]
    public void ExecutesHandshakeValidationSequencingAcknowledgementAndResyncScenarios()
    {
        const string javaScriptRunner = "/usr/bin/osascript";
        if (!File.Exists(javaScriptRunner))
        {
            Assert.Ignore("The platform JavaScript runtime is unavailable.");
        }

        string repository = FindRepositoryRoot();
        string harnessPath = Path.Combine(repository, "tests", "Browser", "htmlgui.runtime.tests.js");
        string runtime = new UiRenderer().RenderBootstrap().Bootstrap;
        string harness = File.ReadAllText(harnessPath);
        string temporaryPath = Path.Combine(Path.GetTempPath(), "htmlgui-runtime-" + Guid.NewGuid().ToString("N") + ".js");
        File.WriteAllText(temporaryPath, runtime + "\n" + harness);

        ProcessStartInfo startInfo = new ProcessStartInfo(javaScriptRunner);
        startInfo.ArgumentList.Add("-l");
        startInfo.ArgumentList.Add("JavaScript");
        startInfo.ArgumentList.Add(temporaryPath);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        using Process process = Process.Start(startInfo)!;
        bool exited = process.WaitForExit(10000);
        string output = process.StandardOutput.ReadToEnd().Trim();
        string error = process.StandardError.ReadToEnd().Trim();
        if (!exited)
        {
            process.Kill(true);
        }
        File.Delete(temporaryPath);

        Assert.Multiple(() =>
        {
            Assert.That(exited, Is.True, "JavaScript runtime timed out");
            Assert.That(process.ExitCode, Is.Zero, error);
            Assert.That(error, Is.Empty);
            Assert.That(output, Is.Not.Empty);
        });
        using JsonDocument result = JsonDocument.Parse(output);
        Assert.Multiple(() =>
        {
            Assert.That(result.RootElement.GetProperty("success").GetBoolean(), Is.True);
            Assert.That(result.RootElement.GetProperty("checks").GetInt32(), Is.EqualTo(113));
            Assert.That(result.RootElement.GetProperty("sent").GetInt32(), Is.GreaterThan(8));
            Assert.That(result.RootElement.GetProperty("delivered").GetInt32(), Is.GreaterThanOrEqualTo(2));
            Assert.That(result.RootElement.GetProperty("domMessages").GetInt32(), Is.GreaterThanOrEqualTo(2));
        });
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "src", "Browser", "htmlgui.runtime.js");
            if (File.Exists(candidate))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("The htmlgui repository root could not be found.");
    }
}
