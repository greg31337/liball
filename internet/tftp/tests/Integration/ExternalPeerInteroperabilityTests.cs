using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

[Category("Interop")]
public class ExternalPeerInteroperabilityTests
{
    [Test]
    public void ClientTransfersWithTftpHpaServer()
    {
        ExternalInterop.RequireOptIn();
        string serverExecutable = ExternalInterop.RequireTftpHpaServer();
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] downloadContent = Content(777);
        byte[] uploadContent = Content(919);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "hpa-download.bin"),
            downloadContent);
        int port = ExternalInterop.ReserveUdpPort();
        using InteropServerProcess server = ExternalInterop.StartServer(
            serverExecutable,
            [
                "--foreground",
                "--ipv4",
                "--address",
                $"127.0.0.1:{port}",
                "--create",
                "--permissive",
                directory.RootPath
            ]);
        server.AssertRunning("tftp-hpa server");
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, port);
        TftpClient client = new TftpClient(null);
        using MemoryStream destination = new MemoryStream();

        TftpTransferResult readResult = client.DownloadToStream(
            ReadRequest(endpoint, "hpa-download.bin", true, true),
            destination,
            CancellationToken.None);
        using MemoryStream source = new MemoryStream(uploadContent);
        TftpTransferResult writeResult = client.UploadFromStream(
            WriteRequest(
                endpoint,
                "hpa-upload.bin",
                uploadContent.Length,
                true,
                true),
            source,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(downloadContent));
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "hpa-upload.bin")),
                Is.EqualTo(uploadContent));
            Assert.That(readResult.EffectiveBlockSize, Is.EqualTo(64));
            Assert.That(writeResult.EffectiveBlockSize, Is.EqualTo(64));
            Assert.That(readResult.EffectiveTimeoutSeconds, Is.EqualTo(1));
            Assert.That(writeResult.EffectiveTimeoutSeconds, Is.EqualTo(1));
            Assert.That(readResult.HasTransferSize, Is.True);
            Assert.That(readResult.TransferSize,
                Is.EqualTo(downloadContent.Length));
            Assert.That(writeResult.HasTransferSize, Is.True);
            Assert.That(writeResult.TransferSize,
                Is.EqualTo(uploadContent.Length));
        });
    }

    [Test]
    public void ServerTransfersWithTftpHpaClient()
    {
        ExternalInterop.RequireOptIn();
        string clientExecutable = ExternalInterop.RequireTftpHpaClient();
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] downloadContent = Content(777);
        byte[] uploadContent = Content(919);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "server-download.bin"),
            downloadContent);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "client-upload.bin"),
            uploadContent);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);

        InteropProcessResult download = ExternalInterop.RunProcess(
            clientExecutable,
            [
                "-4",
                "-m",
                "octet",
                "-B",
                "64",
                "127.0.0.1",
                harness.ListenerEndPoint.Port.ToString(),
                "-c",
                "get server-download.bin client-download.bin"
            ],
            directory.RootPath,
            15000);
        ExternalInterop.AssertSucceeded(download, "tftp-hpa download");
        InteropProcessResult upload = ExternalInterop.RunProcess(
            clientExecutable,
            [
                "-4",
                "-m",
                "octet",
                "-B",
                "64",
                "127.0.0.1",
                harness.ListenerEndPoint.Port.ToString(),
                "-c",
                "put client-upload.bin server-upload.bin"
            ],
            directory.RootPath,
            15000);
        ExternalInterop.AssertSucceeded(upload, "tftp-hpa upload");
        harness.WaitForCompletedTransfers(2);

        Assert.Multiple(() =>
        {
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "client-download.bin")),
                Is.EqualTo(downloadContent));
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "server-upload.bin")),
                Is.EqualTo(uploadContent));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void ClientTransfersWithTftpyServer()
    {
        ExternalInterop.RequireOptIn();
        string pythonExecutable = ExternalInterop.RequireTftpy();
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] downloadContent = Content(777);
        byte[] uploadContent = Content(919);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "tftpy-download.bin"),
            downloadContent);
        int port = ExternalInterop.ReserveUdpPort();
        const string script =
            "import sys,tftpy; server=tftpy.TftpServer(sys.argv[1]); " +
            "server.listen('127.0.0.1',int(sys.argv[2]),timeout=0.1,retries=3)";
        using InteropServerProcess server = ExternalInterop.StartServer(
            pythonExecutable,
            ["-c", script, directory.RootPath, port.ToString()]);
        server.AssertRunning("TFTPy server");
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, port);
        TftpClient client = new TftpClient(null);
        using MemoryStream destination = new MemoryStream();

        TftpTransferResult readResult = client.DownloadToStream(
            ReadRequest(endpoint, "tftpy-download.bin", false, true),
            destination,
            CancellationToken.None);
        using MemoryStream source = new MemoryStream(uploadContent);
        TftpTransferResult writeResult = client.UploadFromStream(
            WriteRequest(
                endpoint,
                "tftpy-upload.bin",
                uploadContent.Length,
                false,
                false),
            source,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(downloadContent));
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "tftpy-upload.bin")),
                Is.EqualTo(uploadContent));
            Assert.That(readResult.EffectiveBlockSize, Is.EqualTo(64));
            Assert.That(writeResult.EffectiveBlockSize, Is.EqualTo(64));
            Assert.That(readResult.HasTransferSize, Is.True);
            Assert.That(readResult.TransferSize,
                Is.EqualTo(downloadContent.Length));
            Assert.That(writeResult.HasTransferSize, Is.False);
        });
    }

    [Test]
    public void ServerTransfersWithTftpyClient()
    {
        ExternalInterop.RequireOptIn();
        string pythonExecutable = ExternalInterop.RequireTftpy();
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] downloadContent = Content(777);
        byte[] uploadContent = Content(919);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "server-download.bin"),
            downloadContent);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "client-upload.bin"),
            uploadContent);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        const string downloadScript =
            "import sys,tftpy; client=tftpy.TftpClient(" +
            "'127.0.0.1',int(sys.argv[1])," +
            "options={'blksize':64,'tsize':0}); " +
            "client.download(sys.argv[2],sys.argv[3],timeout=2,retries=3)";
        InteropProcessResult download = ExternalInterop.RunProcess(
            pythonExecutable,
            [
                "-c",
                downloadScript,
                harness.ListenerEndPoint.Port.ToString(),
                "server-download.bin",
                Path.Combine(directory.RootPath, "client-download.bin")
            ],
            directory.RootPath,
            15000);
        ExternalInterop.AssertSucceeded(download, "TFTPy download");
        const string uploadScript =
            "import sys,tftpy; client=tftpy.TftpClient(" +
            "'127.0.0.1',int(sys.argv[1])," +
            "options={'blksize':64,'tsize':int(sys.argv[4])}); " +
            "client.upload(sys.argv[2],sys.argv[3],timeout=2,retries=3)";
        InteropProcessResult upload = ExternalInterop.RunProcess(
            pythonExecutable,
            [
                "-c",
                uploadScript,
                harness.ListenerEndPoint.Port.ToString(),
                "server-upload.bin",
                Path.Combine(directory.RootPath, "client-upload.bin"),
                uploadContent.Length.ToString()
            ],
            directory.RootPath,
            15000);
        ExternalInterop.AssertSucceeded(upload, "TFTPy upload");
        harness.WaitForCompletedTransfers(2);

        Assert.Multiple(() =>
        {
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "client-download.bin")),
                Is.EqualTo(downloadContent));
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "server-upload.bin")),
                Is.EqualTo(uploadContent));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(2));
        });
    }

    private static TftpReadRequest ReadRequest(
        IPEndPoint endpoint,
        string filename,
        bool requestTimeout,
        bool requestTransferSize)
    {
        TftpReadRequest request = new TftpReadRequest();
        SetEndpoint(request.Endpoint, endpoint);
        request.RemoteFilename = filename;
        request.Options = TransferOptions(
            requestTimeout,
            requestTransferSize);
        return request;
    }

    private static TftpWriteRequest WriteRequest(
        IPEndPoint endpoint,
        string filename,
        long sourceSize,
        bool requestTimeout,
        bool requestTransferSize)
    {
        TftpWriteRequest request = new TftpWriteRequest();
        SetEndpoint(request.Endpoint, endpoint);
        request.RemoteFilename = filename;
        request.Options = TransferOptions(
            requestTimeout,
            requestTransferSize);
        request.HasKnownSourceSize = true;
        request.KnownSourceSize = sourceSize;
        return request;
    }

    private static TftpTransferOptions TransferOptions(
        bool requestTimeout,
        bool requestTransferSize)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.Mode = TftpTransferMode.Octet;
        options.RequestBlockSize = true;
        options.BlockSize = 64;
        options.RequestTimeout = requestTimeout;
        options.TimeoutSeconds = 1;
        options.RequestTransferSize = requestTransferSize;
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 2;
        options.MaximumRetries = 4;
        options.MaximumTransferDurationSeconds = 30;
        options.DallyDurationSeconds = 0;
        return options;
    }

    private static void SetEndpoint(
        TftpEndpoint endpoint,
        IPEndPoint peerEndPoint)
    {
        endpoint.Host = peerEndPoint.Address.ToString();
        endpoint.Port = peerEndPoint.Port;
        endpoint.AddressFamily = AddressFamily.InterNetwork;
        endpoint.LocalAddress = IPAddress.Loopback;
    }

    private static byte[] Content(int length)
    {
        byte[] content = new byte[length];
        int index = 0;
        while (index < length)
        {
            content[index] = (byte)((index * 17) % 251);
            index = index + 1;
        }

        return content;
    }
}

internal class InteropProcessResult
{
    internal int ExitCode;
    internal bool TimedOut;
    internal string StandardOutput = "";
    internal string StandardError = "";
}

internal class InteropServerProcess : IDisposable
{
    internal Process ChildProcess;
    internal Task<string> StandardOutputTask;
    internal Task<string> StandardErrorTask;

    internal InteropServerProcess(Process childProcess)
    {
        ChildProcess = childProcess;
        StandardOutputTask = childProcess.StandardOutput.ReadToEndAsync();
        StandardErrorTask = childProcess.StandardError.ReadToEndAsync();
    }

    internal void AssertRunning(string peerName)
    {
        Thread.Sleep(250);
        if (ChildProcess.HasExited)
        {
            string output = StandardOutputTask.GetAwaiter().GetResult();
            string error = StandardErrorTask.GetAwaiter().GetResult();
            Assert.Fail(
                $"The {peerName} exited during startup with code " +
                $"{ChildProcess.ExitCode}. stdout: {output} stderr: {error}");
        }
    }

    public void Dispose()
    {
        if (!ChildProcess.HasExited)
        {
            ChildProcess.Kill(true);
        }

        ChildProcess.WaitForExit(5000);
        ChildProcess.Dispose();
    }
}

internal static class ExternalInterop
{
    internal static void RequireOptIn()
    {
        string? enabled = Environment.GetEnvironmentVariable(
            "TFTP_RUN_INTEROP_TESTS");
        if (!string.Equals(enabled, "1", StringComparison.Ordinal))
        {
            Assert.Ignore(
                "External interoperability tests are disabled. Set " +
                "TFTP_RUN_INTEROP_TESTS=1 to enable them.");
        }
    }

    internal static string RequireTftpHpaClient()
    {
        return RequireTftpHpaExecutable(
            "TFTP_HPA_CLIENT",
            ["tftp"],
            "tftp-hpa client");
    }

    internal static string RequireTftpHpaServer()
    {
        return RequireTftpHpaExecutable(
            "TFTP_HPA_SERVER",
            ["in.tftpd", "tftpd-hpa", "tftpd"],
            "tftp-hpa server");
    }

    internal static string RequireTftpy()
    {
        string? configured = Environment.GetEnvironmentVariable(
            "TFTP_TFTPY_PYTHON");
        string[] candidates;
        if (string.IsNullOrWhiteSpace(configured))
        {
            candidates = ["python3", "python"];
        }
        else
        {
            candidates = [configured];
        }

        foreach (string candidate in candidates)
        {
            string? executable = FindExecutable(candidate);
            if (executable == null)
            {
                continue;
            }

            InteropProcessResult probe = RunProcess(
                executable,
                [
                    "-c",
                    "import tftpy; print(getattr(tftpy,'__version__','unknown'))"
                ],
                null,
                5000);
            if (!probe.TimedOut && probe.ExitCode == 0)
            {
                return executable;
            }
        }

        Assert.Ignore(
            "TFTPy is unavailable. Install it for Python or set " +
            "TFTP_TFTPY_PYTHON to a Python executable that can import tftpy.");
        return "";
    }

    internal static string RequireTftpHpaExecutable(
        string environmentVariable,
        string[] defaultCandidates,
        string peerName)
    {
        string? configured = Environment.GetEnvironmentVariable(
            environmentVariable);
        string[] candidates = defaultCandidates;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates = [configured];
        }

        foreach (string candidate in candidates)
        {
            string? executable = FindExecutable(candidate);
            if (executable == null)
            {
                continue;
            }

            InteropProcessResult version = RunProcess(
                executable,
                ["-V"],
                null,
                5000);
            string versionText = version.StandardOutput + version.StandardError;
            if (!version.TimedOut &&
                version.ExitCode == 0 &&
                versionText.Contains(
                    "tftp-hpa",
                    StringComparison.OrdinalIgnoreCase))
            {
                return executable;
            }
        }

        Assert.Ignore(
            $"The {peerName} is unavailable. Install tftp-hpa or set " +
            $"{environmentVariable} to its executable.");
        return "";
    }

    internal static string? FindExecutable(string candidate)
    {
        if (candidate.Contains(Path.DirectorySeparatorChar) ||
            candidate.Contains(Path.AltDirectorySeparatorChar))
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            return null;
        }

        string? path = Environment.GetEnvironmentVariable("PATH");
        if (path == null)
        {
            return null;
        }

        foreach (string directory in path.Split(Path.PathSeparator))
        {
            if (directory.Length == 0)
            {
                continue;
            }

            string fullPath = Path.Combine(directory, candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    internal static int ReserveUdpPort()
    {
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        IPEndPoint endpoint = (IPEndPoint)socket.LocalEndPoint!;
        return endpoint.Port;
    }

    internal static InteropServerProcess StartServer(
        string executable,
        string[] arguments)
    {
        ProcessStartInfo startInfo = CreateStartInfo(
            executable,
            arguments,
            null);
        Process? process = Process.Start(startInfo);
        if (process == null)
        {
            throw new AssertionException(
                $"Unable to start external server {executable}.");
        }

        return new InteropServerProcess(process);
    }

    internal static InteropProcessResult RunProcess(
        string executable,
        string[] arguments,
        string? workingDirectory,
        int timeoutMilliseconds)
    {
        ProcessStartInfo startInfo = CreateStartInfo(
            executable,
            arguments,
            workingDirectory);
        using Process process = new Process();
        process.StartInfo = startInfo;
        if (!process.Start())
        {
            throw new AssertionException(
                $"Unable to start external process {executable}.");
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(timeoutMilliseconds);
        if (!exited)
        {
            process.Kill(true);
            process.WaitForExit(5000);
        }

        InteropProcessResult result = new InteropProcessResult();
        result.TimedOut = !exited;
        if (exited)
        {
            result.ExitCode = process.ExitCode;
        }
        else
        {
            result.ExitCode = -1;
        }

        result.StandardOutput = outputTask.GetAwaiter().GetResult();
        result.StandardError = errorTask.GetAwaiter().GetResult();
        return result;
    }

    internal static ProcessStartInfo CreateStartInfo(
        string executable,
        string[] arguments,
        string? workingDirectory)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = executable;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    internal static void AssertSucceeded(
        InteropProcessResult result,
        string operation)
    {
        Assert.That(result.TimedOut, Is.False,
            $"The {operation} timed out. stdout: {result.StandardOutput} " +
            $"stderr: {result.StandardError}");
        Assert.That(result.ExitCode, Is.EqualTo(0),
            $"The {operation} failed. stdout: {result.StandardOutput} " +
            $"stderr: {result.StandardError}");
    }
}
