using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ftp.Tests;

public class FtpExternalServerInteroperabilityTests
{
    private string RootPath = null!;
    private X509Certificate2? Certificate;

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-external-server-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        Certificate = null;
    }

    [TearDown]
    public void TearDown()
    {
        Certificate?.Dispose();
        Certificate = null;
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }
    }

    [Test]
    public void Client_round_trips_against_pyftpdlib_when_installed()
    {
        string python = RequirePythonModule("pyftpdlib");
        int port = ReserveAvailablePort();
        string script =
            "import sys\n" +
            "from pyftpdlib.authorizers import DummyAuthorizer\n" +
            "from pyftpdlib.handlers import FTPHandler\n" +
            "from pyftpdlib.servers import FTPServer\n" +
            "auth = DummyAuthorizer()\n" +
            "auth.add_user('alice', 'secret', sys.argv[2], perm='elradfmwMT')\n" +
            "FTPHandler.authorizer = auth\n" +
            "server = FTPServer(('127.0.0.1', int(sys.argv[1])), FTPHandler)\n" +
            "print('READY', flush=True)\n" +
            "server.serve_forever()\n";
        using ExternalProcess server = ExternalProcess.Start(
            python,
            new[] { "-c", script, port.ToString(), RootPath });
        WaitForServer(port, server);
        FtpClient client = ConnectClient(
            "127.0.0.1",
            port,
            "alice",
            "secret",
            FtpSecurityMode.Plain,
            true);

        try
        {
            AssertClientRoundTrip(client, "pyftpdlib");
        }
        finally
        {
            client.Disconnect();
        }
    }

    [Test]
    public void Client_round_trips_against_configured_apache_or_container_server()
    {
        string? host = Environment.GetEnvironmentVariable(
            "FTP_INTEROP_APACHE_HOST");
        string? portText = Environment.GetEnvironmentVariable(
            "FTP_INTEROP_APACHE_PORT");
        string? user = Environment.GetEnvironmentVariable(
            "FTP_INTEROP_APACHE_USER");
        string? password = Environment.GetEnvironmentVariable(
            "FTP_INTEROP_APACHE_PASSWORD");
        if (string.IsNullOrWhiteSpace(host) ||
            !int.TryParse(portText, out int port) ||
            string.IsNullOrWhiteSpace(user) || password == null)
        {
            Assert.Ignore(
                "No Apache FtpServer or equivalent container endpoint is configured.");
            return;
        }
        FtpClient client = ConnectClient(
            host,
            port,
            user,
            password,
            FtpSecurityMode.Plain,
            false);
        string name = "apache-" + Guid.NewGuid().ToString("N");

        try
        {
            AssertClientRoundTrip(client, name);
        }
        finally
        {
            client.DeleteFile("/" + name + ".bin");
            client.Disconnect();
        }
    }

    [Test]
    public void Client_round_trips_against_strict_independent_ftp_server()
    {
        using ScriptedInteropServer server =
            new ScriptedInteropServer(null, false);
        server.Start();
        FtpClient client = ConnectClient(
            "127.0.0.1",
            server.Port,
            "alice",
            "secret",
            FtpSecurityMode.Plain,
            false);

        try
        {
            AssertClientRoundTrip(client, "strict");
        }
        finally
        {
            client.Disconnect();
        }
        server.WaitForCompletion();

        Assert.Multiple(() =>
        {
            Assert.That(server.Failure, Is.Null);
            Assert.That(server.Commands, Does.Contain("FEAT"));
            Assert.That(server.Commands.Any(command =>
                command.StartsWith("MLSD", StringComparison.Ordinal)), Is.True);
            Assert.That(server.Commands.Any(command =>
                command.StartsWith("EPSV", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void Client_round_trips_against_quirky_explicit_ftps_server()
    {
        RequireCertificate();
        using ScriptedInteropServer server =
            new ScriptedInteropServer(Certificate, true);
        server.Start();
        FtpClient client = ConnectClient(
            "127.0.0.1",
            server.Port,
            "alice",
            "secret",
            FtpSecurityMode.ExplicitTlsRequired,
            true);
        FtpCommandResult protection =
            client.SetDataProtection(FtpDataProtectionLevel.Private);
        Assert.That(protection.Succeeded, Is.True,
            protection.FailureMessage);

        try
        {
            AssertClientRoundTrip(client, "quirky-tls");
        }
        finally
        {
            client.Disconnect();
        }
        server.WaitForCompletion();

        Assert.Multiple(() =>
        {
            Assert.That(server.Failure, Is.Null);
            Assert.That(server.Commands, Does.Contain("AUTH TLS"));
            Assert.That(server.Commands, Does.Contain("PBSZ 0"));
            Assert.That(server.Commands, Does.Contain("PROT P"));
            Assert.That(server.Commands.Any(command =>
                command.StartsWith("PASV", StringComparison.Ordinal)), Is.True);
            Assert.That(server.Commands.Any(command =>
                command.StartsWith("LIST", StringComparison.Ordinal)), Is.True);
        });
    }

    private static void AssertClientRoundTrip(FtpClient client, string name)
    {
        byte[] content = Encoding.UTF8.GetBytes(name + "-content");
        using MemoryStream source = new MemoryStream(content);
        FtpTransferRequest upload = new FtpTransferRequest();
        upload.Direction = FtpTransferDirection.Upload;
        upload.RemotePath = "/" + name + ".bin";
        upload.Content = source;
        upload.Options.VerificationMode = FtpVerificationMode.None;
        FtpTransferResult uploaded = client.Transfer(upload);
        using MemoryStream destination = new MemoryStream();
        FtpTransferRequest download = new FtpTransferRequest();
        download.Direction = FtpTransferDirection.Download;
        download.RemotePath = "/" + name + ".bin";
        download.Content = destination;
        download.Options.VerificationMode = FtpVerificationMode.None;
        FtpTransferResult downloaded = client.Transfer(download);
        FtpListing listing = client.ListDirectory("/");

        Assert.Multiple(() =>
        {
            Assert.That(uploaded.Succeeded, Is.True,
                uploaded.FailureMessage);
            Assert.That(downloaded.Succeeded, Is.True,
                downloaded.FailureMessage);
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(listing.Complete, Is.True,
                listing.FailureMessage);
            Assert.That(listing.Entries.Any(entry =>
                entry.Name.Contains(name, StringComparison.Ordinal)), Is.True);
        });
    }

    private static FtpClient ConnectClient(
        string host,
        int port,
        string user,
        string password,
        FtpSecurityMode securityMode,
        bool quirky)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = host;
        options.Endpoint.Port = port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = securityMode;
        options.Credentials.UserName = user;
        options.Credentials.Password = password;
        options.AllowPlaintextCredentials =
            securityMode == FtpSecurityMode.Plain;
        options.DataConnection.Mode = FtpDataConnectionMode.Passive;
        options.Tls.CheckCertificateRevocation = false;
        options.Tls.RequireDataSessionReuse = false;
        options.Quirks.LenientReplies = quirky;
        options.Quirks.HonorPassiveAdvertisedAddress = false;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        options.Timeouts.DataConnectionTimeoutMilliseconds = 5000;
        options.Timeouts.TlsHandshakeTimeoutMilliseconds = 5000;
        FtpClient client = new FtpClient(
            securityMode == FtpSecurityMode.Plain ?
                null : new AcceptingCertificateValidator());
        client.Connect(options);
        Assert.That(client.GetSessionState(),
            Is.EqualTo(FtpSessionState.Authenticated));
        return client;
    }

    private void RequireCertificate()
    {
        if (Certificate == null)
        {
            Certificate = FtpTlsStreamTests.CreateCertificate();
        }
    }

    private static string RequirePythonModule(string module)
    {
        string[] candidates = new[] { "python3", "python" };
        int index = 0;
        while (index < candidates.Length)
        {
            ProcessResult result = RunProcess(
                candidates[index],
                new[] { "-c", "import " + module });
            if (result.Started && result.ExitCode == 0)
            {
                return candidates[index];
            }
            index++;
        }
        Assert.Ignore(module + " is not installed for external-server tests.");
        return "";
    }

    private static ProcessResult RunProcess(
        string executable,
        string[] arguments)
    {
        ProcessResult result = new ProcessResult();
        try
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = executable;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            int index = 0;
            while (index < arguments.Length)
            {
                start.ArgumentList.Add(arguments[index]);
                index++;
            }
            using Process process = Process.Start(start)!;
            result.Started = true;
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(5000))
            {
                process.Kill(true);
                process.WaitForExit();
            }
            Task.WaitAll(new Task[] { output, error });
            result.ExitCode = process.ExitCode;
            result.Output = output.Result;
            result.Error = error.Result;
        }
        catch (Exception failure)
        {
            result.Error = failure.Message;
        }
        return result;
    }

    private static void WaitForServer(int port, ExternalProcess process)
    {
        bool ready = SpinWait.SpinUntil(() =>
        {
            if (process.HasExited)
            {
                return true;
            }
            try
            {
                using TcpClient probe = new TcpClient(AddressFamily.InterNetwork);
                probe.Connect(IPAddress.Loopback, port);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }, 5000);
        Assert.That(ready, Is.True);
        Assert.That(process.HasExited, Is.False, process.ReadError());
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private sealed class ProcessResult
    {
        public bool Started;
        public int ExitCode;
        public string Output = "";
        public string Error = "";
    }

    private sealed class AcceptingCertificateValidator : IFtpCertificateValidator
    {
        public FtpCertificateValidationResult ValidateRemoteCertificate(
            FtpCertificateValidationRequest request)
        {
            FtpCertificateValidationResult result =
                new FtpCertificateValidationResult();
            result.Handled = true;
            result.Accepted = request.Certificate != null;
            return result;
        }
    }

    private sealed class ExternalProcess : IDisposable
    {
        private Process Process;

        private ExternalProcess(Process process)
        {
            Process = process;
        }

        public bool HasExited => Process.HasExited;

        public static ExternalProcess Start(
            string executable,
            string[] arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = executable;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            int index = 0;
            while (index < arguments.Length)
            {
                start.ArgumentList.Add(arguments[index]);
                index++;
            }
            Process? process = System.Diagnostics.Process.Start(start);
            Assert.That(process, Is.Not.Null);
            return new ExternalProcess(process!);
        }

        public string ReadError()
        {
            if (!Process.HasExited)
            {
                return "";
            }
            return Process.StandardError.ReadToEnd();
        }

        public void Dispose()
        {
            if (!Process.HasExited)
            {
                Process.Kill(true);
                Process.WaitForExit();
            }
            Process.Dispose();
        }
    }

    private sealed class ScriptedInteropServer : IDisposable
    {
        private TcpListener ControlListener;
        private TcpListener? DataListener;
        private X509Certificate2? Certificate;
        private bool Quirky;
        private bool ProtectedData;
        private Task? ServerTask;
        private Dictionary<string, byte[]> Files;
        public int Port;
        public List<string> Commands;
        public Exception? Failure;

        public ScriptedInteropServer(
            X509Certificate2? certificate,
            bool quirky)
        {
            ControlListener = new TcpListener(IPAddress.Loopback, 0);
            DataListener = null;
            Certificate = certificate;
            Quirky = quirky;
            ProtectedData = false;
            ServerTask = null;
            Files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            Files["/seed.txt"] = Encoding.ASCII.GetBytes("seed");
            Commands = new List<string>();
            Failure = null;
            Port = 0;
        }

        public void Start()
        {
            ControlListener.Start();
            Port = ((IPEndPoint)ControlListener.LocalEndpoint).Port;
            ServerTask = Task.Factory.StartNew(
                () => Run(),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void WaitForCompletion()
        {
            Assert.That(ServerTask, Is.Not.Null);
            Assert.That(ServerTask!.Wait(5000), Is.True);
        }

        public void Dispose()
        {
            DataListener?.Stop();
            ControlListener.Stop();
            if (ServerTask != null && !ServerTask.IsCompleted)
            {
                ServerTask.Wait(5000);
            }
        }

        private void Run()
        {
            try
            {
                using TcpClient accepted = ControlListener.AcceptTcpClient();
                using NetworkStream transport = accepted.GetStream();
                Stream control = transport;
                WriteReply(control, 220, "scripted server ready");
                bool running = true;
                while (running)
                {
                    string command = ReadCommand(control);
                    Commands.Add(command);
                    string verb = command.Split(' ', 2)[0].ToUpperInvariant();
                    string argument = command.Contains(' ') ?
                        command.Substring(command.IndexOf(' ') + 1) : "";
                    if (verb == "AUTH")
                    {
                        WriteReply(control, 234, "begin TLS");
                        SslStream secure = new SslStream(transport, true);
                        secure.AuthenticateAsServer(
                            Certificate!,
                            false,
                            SslProtocols.Tls12,
                            false);
                        control = secure;
                    }
                    else if (verb == "USER")
                    {
                        WriteReply(control, 331, "password required");
                    }
                    else if (verb == "PASS")
                    {
                        WriteReply(control, 230, "logged in");
                    }
                    else if (verb == "FEAT")
                    {
                        WriteFeatures(control);
                    }
                    else if (verb == "PBSZ")
                    {
                        WriteReply(control, 200, "PBSZ accepted");
                    }
                    else if (verb == "PROT")
                    {
                        ProtectedData = argument == "P";
                        WriteReply(control, 200, "PROT accepted");
                    }
                    else if (verb == "OPTS")
                    {
                        string optionReply = argument.StartsWith(
                            "MLST", StringComparison.OrdinalIgnoreCase) ?
                                "MLST OPTS" + argument.Substring(4) :
                                "command accepted";
                        WriteReply(control, 200, optionReply);
                    }
                    else if (verb == "TYPE" || verb == "STRU" ||
                        verb == "MODE" || verb == "NOOP" || verb == "CLNT")
                    {
                        WriteReply(control, 200, "command accepted");
                    }
                    else if (verb == "SYST")
                    {
                        WriteReply(control, 215, "UNIX Type: L8");
                    }
                    else if (verb == "PWD")
                    {
                        WriteReply(control, 257, "\"/\" is current directory");
                    }
                    else if (verb == "CWD")
                    {
                        if (NormalizePath(argument) == "/")
                        {
                            WriteReply(control, 250, "directory changed");
                        }
                        else
                        {
                            WriteReply(control, 550, "directory unavailable");
                        }
                    }
                    else if (verb == "EPSV")
                    {
                        if (Quirky)
                        {
                            WriteReply(control, 500, "EPSV unsupported");
                        }
                        else
                        {
                            OpenPassiveListener();
                            int dataPort = GetDataPort();
                            WriteReply(control, 229,
                                "Entering Extended Passive Mode (|||" +
                                dataPort.ToString() + "|)");
                        }
                    }
                    else if (verb == "PASV")
                    {
                        OpenPassiveListener();
                        int dataPort = GetDataPort();
                        int high = dataPort / 256;
                        int low = dataPort % 256;
                        string address = Quirky ? "127,0,0,2" : "127,0,0,1";
                        WriteReply(control, 227,
                            "Entering Passive Mode (" + address + "," +
                            high.ToString() + "," + low.ToString() + ")");
                    }
                    else if (verb == "MLST")
                    {
                        string path = NormalizePath(argument);
                        if (Files.ContainsKey(path))
                        {
                            WriteText(control,
                                "250-File details" + Ending +
                                " type=file;size=" + Files[path].Length.ToString() +
                                ";modify=20260102030405; " + path + Ending +
                                "250 End" + Ending);
                        }
                        else
                        {
                            WriteReply(control, 550, "file unavailable");
                        }
                    }
                    else if (verb == "SIZE")
                    {
                        string path = NormalizePath(argument);
                        if (Files.ContainsKey(path))
                        {
                            WriteReply(control, 213,
                                Files[path].Length.ToString());
                        }
                        else
                        {
                            WriteReply(control, 550, "file unavailable");
                        }
                    }
                    else if (verb == "MDTM")
                    {
                        string path = NormalizePath(argument);
                        if (Files.ContainsKey(path))
                        {
                            WriteReply(control, 213, "20260102030405");
                        }
                        else
                        {
                            WriteReply(control, 550, "file unavailable");
                        }
                    }
                    else if (verb == "MLSD" || verb == "LIST" ||
                        verb == "NLST")
                    {
                        SendListing(control, verb);
                    }
                    else if (verb == "STOR" || verb == "APPE")
                    {
                        ReceiveFile(control, NormalizePath(argument),
                            verb == "APPE");
                    }
                    else if (verb == "RETR")
                    {
                        SendFile(control, NormalizePath(argument));
                    }
                    else if (verb == "DELE")
                    {
                        Files.Remove(NormalizePath(argument));
                        WriteReply(control, 250, "file deleted");
                    }
                    else if (verb == "QUIT")
                    {
                        WriteReply(control, 221, "goodbye");
                        running = false;
                    }
                    else
                    {
                        WriteReply(control, 502, "command unsupported");
                    }
                }
                if (control != transport)
                {
                    control.Dispose();
                }
            }
            catch (Exception failure)
            {
                Failure = failure;
            }
        }

        private string Ending => Quirky ? "\n" : "\r\n";

        private void WriteFeatures(Stream control)
        {
            if (Quirky)
            {
                WriteReply(control, 500, "FEAT unavailable");
                return;
            }
            WriteText(control,
                "211-Features" + Ending +
                " UTF8" + Ending +
                " MLST type*;size*;modify*;" + Ending +
                " SIZE" + Ending +
                " MDTM" + Ending +
                " EPSV" + Ending +
                "211 End" + Ending);
        }

        private void SendListing(Stream control, string verb)
        {
            WriteReply(control, 150, "opening data connection");
            using TcpClient dataClient = AcceptDataClient();
            using Stream data = CreateDataStream(dataClient);
            StringBuilder listing = new StringBuilder();
            foreach (KeyValuePair<string, byte[]> file in Files)
            {
                string name = file.Key.TrimStart('/');
                if (verb == "MLSD")
                {
                    listing.Append("type=file;size=");
                    listing.Append(file.Value.Length);
                    listing.Append(";modify=20260102030405; ");
                    listing.Append(name);
                }
                else if (verb == "NLST")
                {
                    listing.Append(name);
                }
                else
                {
                    listing.Append("-rw-r--r-- 1 owner group ");
                    listing.Append(file.Value.Length);
                    listing.Append(" Jan 02 2026 ");
                    listing.Append(name);
                }
                listing.Append("\r\n");
            }
            WriteData(data, Encoding.UTF8.GetBytes(listing.ToString()));
            WriteReply(control, 226, "transfer complete");
        }

        private void ReceiveFile(Stream control, string path, bool append)
        {
            WriteReply(control, 150, "opening data connection");
            using TcpClient dataClient = AcceptDataClient();
            using Stream data = CreateDataStream(dataClient);
            using MemoryStream content = new MemoryStream();
            data.CopyTo(content);
            byte[] received = content.ToArray();
            if (append && Files.ContainsKey(path))
            {
                Files[path] = Files[path].Concat(received).ToArray();
            }
            else
            {
                Files[path] = received;
            }
            ShutdownDataStream(data);
            WriteReply(control, 226, "transfer complete");
        }

        private void SendFile(Stream control, string path)
        {
            if (!Files.ContainsKey(path))
            {
                WriteReply(control, 550, "file unavailable");
                return;
            }
            WriteReply(control, 150, "opening data connection");
            using TcpClient dataClient = AcceptDataClient();
            using Stream data = CreateDataStream(dataClient);
            WriteData(data, Files[path]);
            WriteReply(control, 226, "transfer complete");
        }

        private void WriteData(Stream data, byte[] content)
        {
            data.Write(content, 0, content.Length);
            data.Flush();
            ShutdownDataStream(data);
        }

        private void ShutdownDataStream(Stream data)
        {
            SslStream? secure = data as SslStream;
            if (secure != null)
            {
                secure.ShutdownAsync().GetAwaiter().GetResult();
            }
        }

        private Stream CreateDataStream(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            if (!ProtectedData)
            {
                return stream;
            }
            SslStream secure = new SslStream(stream, false);
            secure.AuthenticateAsServer(
                Certificate!, false, SslProtocols.Tls12, false);
            return secure;
        }

        private TcpClient AcceptDataClient()
        {
            if (DataListener == null)
            {
                throw new InvalidOperationException(
                    "A passive data listener was not prepared.");
            }
            TcpListener listener = DataListener;
            DataListener = null;
            TcpClient client = listener.AcceptTcpClient();
            listener.Stop();
            return client;
        }

        private void OpenPassiveListener()
        {
            DataListener?.Stop();
            DataListener = new TcpListener(IPAddress.Loopback, 0);
            DataListener.Start();
        }

        private int GetDataPort()
        {
            if (DataListener == null)
            {
                throw new InvalidOperationException(
                    "A passive data listener is unavailable.");
            }
            return ((IPEndPoint)DataListener.LocalEndpoint).Port;
        }

        private void WriteReply(Stream stream, int code, string text)
        {
            WriteText(stream,
                code.ToString() + " " + text + Ending);
        }

        private static void WriteText(Stream stream, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        private static string ReadCommand(Stream stream)
        {
            using MemoryStream bytes = new MemoryStream();
            bool carriageReturn = false;
            while (true)
            {
                int value = stream.ReadByte();
                if (value < 0)
                {
                    throw new EndOfStreamException();
                }
                if (carriageReturn)
                {
                    if (value != 10)
                    {
                        throw new InvalidDataException(
                            "The FTP command did not use CRLF framing.");
                    }
                    return Encoding.UTF8.GetString(bytes.ToArray());
                }
                if (value == 13)
                {
                    carriageReturn = true;
                }
                else
                {
                    bytes.WriteByte((byte)value);
                }
            }
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }
            return path.StartsWith('/') ? path : "/" + path;
        }
    }
}
