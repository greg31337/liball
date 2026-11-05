using System.Diagnostics;

namespace raw.Tests;

internal static class IndependentToolAdapters
{
    internal static ExternalToolAdapter LibRaw()
    {
        return ExternalToolAdapter.Discover("LibRaw", "RAW_TEST_LIBRAW", new string[] { "raw-identify", "dcraw_emu" }, new string[] { "-v" });
    }

    internal static ExternalToolAdapter Dcraw()
    {
        return ExternalToolAdapter.Discover("dcraw", "RAW_TEST_DCRAW", new string[] { "dcraw" }, new string[] { "-v" });
    }

    internal static ExternalToolAdapter ExifTool()
    {
        return ExternalToolAdapter.Discover("ExifTool", "RAW_TEST_EXIFTOOL", new string[] { "exiftool" }, new string[] { "-ver" });
    }

    internal static ExternalToolAdapter Exiv2()
    {
        return ExternalToolAdapter.Discover("Exiv2", "RAW_TEST_EXIV2", new string[] { "exiv2" }, new string[] { "-V" });
    }

    internal static ExternalToolAdapter AdobeDngConverter()
    {
        return Vendor("Adobe DNG Converter", "RAW_TEST_ADOBE_DNG_CONVERTER", new string[] { "--version" });
    }

    internal static ExternalToolAdapter CanonDigitalPhotoProfessional()
    {
        return Vendor("Canon Digital Photo Professional", "RAW_TEST_CANON_DPP", new string[] { "--version" });
    }

    internal static ExternalToolAdapter NikonNxStudio()
    {
        return Vendor("Nikon NX Studio", "RAW_TEST_NIKON_NX_STUDIO", new string[] { "--version" });
    }

    internal static ExternalToolAdapter Vendor(string name, string environmentVariable, string[] versionArguments)
    {
        return ExternalToolAdapter.Discover(name, environmentVariable, Array.Empty<string>(), versionArguments);
    }
}

internal class ExternalToolAdapter
{
    internal string Name;
    internal string EnvironmentVariable;
    internal string? ExecutablePath;
    internal string UnavailableReason;
    internal string[] VersionArguments;
    internal int TimeoutMilliseconds;

    ExternalToolAdapter(string name, string environmentVariable, string? executablePath, string unavailableReason, string[] versionArguments)
    {
        Name = name;
        EnvironmentVariable = environmentVariable;
        ExecutablePath = executablePath;
        UnavailableReason = unavailableReason;
        VersionArguments = versionArguments == null ? Array.Empty<string>() : versionArguments.ToArray();
        TimeoutMilliseconds = ReadTimeout();
    }

    internal static ExternalToolAdapter Discover(string name, string environmentVariable, string[] candidates, string[] versionArguments)
    {
        string? configured = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            string expanded = Environment.ExpandEnvironmentVariables(configured.Trim());
            if (File.Exists(expanded))
            {
                return new ExternalToolAdapter(name, environmentVariable, Path.GetFullPath(expanded), string.Empty, versionArguments);
            }
            return new ExternalToolAdapter(name, environmentVariable, null, name + " is unavailable because " + environmentVariable + " does not name an existing executable.", versionArguments);
        }

        string? path = FindOnPath(candidates);
        if (path != null)
        {
            return new ExternalToolAdapter(name, environmentVariable, path, string.Empty, versionArguments);
        }
        string detail = candidates == null || candidates.Length == 0 ? "Set " + environmentVariable + " to opt in to this vendor application." : "Install it on PATH or set " + environmentVariable + ".";
        return new ExternalToolAdapter(name, environmentVariable, null, name + " is not available. " + detail, versionArguments);
    }

    internal bool IsAvailable()
    {
        return ExecutablePath != null;
    }

    internal void IgnoreIfUnavailable()
    {
        if (!IsAvailable())
        {
            Assert.Ignore(UnavailableReason);
        }
    }

    internal ExternalToolResult Run(string[] arguments)
    {
        return Run(arguments, null);
    }

    internal ExternalToolResult Run(string[] arguments, string? workingDirectory)
    {
        if (!IsAvailable())
        {
            throw new InvalidOperationException(UnavailableReason);
        }
        ProcessStartInfo start = new ProcessStartInfo((ExecutablePath ?? throw new System.InvalidOperationException("A required value is unavailable.")));
        int index = 0;
        while (arguments != null && index < arguments.Length)
        {
            start.ArgumentList.Add(arguments[index]);
            index++;
        }
        if (!string.IsNullOrEmpty(workingDirectory))
        {
            start.WorkingDirectory = workingDirectory;
        }
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        Stopwatch elapsed = Stopwatch.StartNew();
        using Process? process = Process.Start(start);
        if (process == null)
        {
            throw new InvalidOperationException(Name + " could not be started.");
        }
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(TimeoutMilliseconds);
        if (!exited)
        {
            process.Kill(true);
            process.WaitForExit();
        }
        Task.WaitAll(output, error);
        elapsed.Stop();
        return new ExternalToolResult(Name, ExecutablePath, exited ? process.ExitCode : -1, output.Result, error.Result, !exited, elapsed.Elapsed);
    }

    internal ExternalToolResult RunOnBytes(string[] arguments, byte[] source, string extension)
    {
        using ExternalToolWorkspace workspace = ExternalToolWorkspace.Create(source, extension);
        return RunOnPath(arguments, workspace.InputPath, workspace.DirectoryPath);
    }

    internal ExternalToolResult RunOnPath(string[] arguments, string path, string workingDirectory)
    {
        string[] complete = new string[(arguments == null ? 0 : arguments.Length) + 1];
        if (arguments != null)
        {
            Array.Copy(arguments, complete, arguments.Length);
        }
        complete[complete.Length - 1] = path;
        return Run(complete, workingDirectory);
    }

    internal string GetVersion()
    {
        if (!IsAvailable())
        {
            return string.Empty;
        }
        ExternalToolResult result = Run(VersionArguments);
        string text = result.StandardOutput.Trim();
        if (text.Length == 0)
        {
            text = result.StandardError.Trim();
        }
        int ending = text.IndexOfAny(new char[] { '\r', '\n' });
        return ending < 0 ? text : text.Substring(0, ending);
    }

    static string? FindOnPath(string[] candidates)
    {
        if (candidates == null || candidates.Length == 0)
        {
            return null;
        }
        string? pathValue = Environment.GetEnvironmentVariable("PATH");
        if (pathValue == null)
        {
            return null;
        }
        string[] directories = pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int candidateIndex = 0;
        while (candidateIndex < candidates.Length)
        {
            string candidateName = candidates[candidateIndex];
            string executableName = OperatingSystem.IsWindows() && Path.GetExtension(candidateName).Length == 0 ? candidateName + ".exe" : candidateName;
            int directoryIndex = 0;
            while (directoryIndex < directories.Length)
            {
                string candidate = Path.Combine(directories[directoryIndex], executableName);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
                directoryIndex++;
            }
            candidateIndex++;
        }
        return null;
    }

    static int ReadTimeout()
    {
        string? configured = Environment.GetEnvironmentVariable("RAW_TEST_TOOL_TIMEOUT_SECONDS");
        if (int.TryParse(configured, out int seconds) && seconds >= 1 && seconds <= 3600)
        {
            return checked(seconds * 1000);
        }
        return 60000;
    }
}

internal class ExternalToolResult
{
    internal string ToolName;
    internal string ExecutablePath;
    internal int ExitCode;
    internal string StandardOutput;
    internal string StandardError;
    internal bool TimedOut;
    internal TimeSpan Elapsed;

    internal ExternalToolResult(string toolName, string executablePath, int exitCode, string standardOutput, string standardError, bool timedOut, TimeSpan elapsed)
    {
        ToolName = toolName;
        ExecutablePath = executablePath;
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
        TimedOut = timedOut;
        Elapsed = elapsed;
    }
}

internal class ExternalToolWorkspace : IDisposable
{
    internal string DirectoryPath;
    internal string InputPath;

    ExternalToolWorkspace(string directoryPath, string inputPath)
    {
        DirectoryPath = directoryPath;
        InputPath = inputPath;
    }

    internal static ExternalToolWorkspace Create(byte[] source, string extension)
    {
        string directory = Path.Combine(Path.GetTempPath(), "raw-tool-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string suffix = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension.StartsWith('.') ? extension : "." + extension;
        string path = Path.Combine(directory, "input" + suffix);
        File.WriteAllBytes(path, source);
        return new ExternalToolWorkspace(directory, path);
    }

    internal string PathFor(string name)
    {
        return Path.Combine(DirectoryPath, name);
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, true);
        }
    }
}
