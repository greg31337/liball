using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ftp.Tests;

public class FtpExternalClientInteroperabilityTests
{
    private string RootPath = null!;
    private X509Certificate2? Certificate;

    [SetUp]
    public void SetUp()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "ftp-external-client-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        File.WriteAllText(
            Path.Combine(RootPath, "seed.txt"),
            "external-client-seed",
            Encoding.ASCII);
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
    public void Curl_round_trips_plain_and_explicit_tls_with_active_data()
    {
        string curl = RequireTool(new[] { "curl" }, "--version");
        RequireCertificate();
        int plainPort = ReserveAvailablePort();
        int tlsPort = ReserveDifferentPort(new[] { plainPort });
        int passivePort = ReserveDifferentPort(new[] { plainPort, tlsPort });
        FtpServer server = CreateServer(true);

        try
        {
            server.StartServer(CreateOptions(
                plainPort, tlsPort, passivePort));
            RunCurlRoundTrip(curl, plainPort, false, "curl-plain");
            RunCurlRoundTrip(curl, tlsPort, true, "curl-tls");

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(Path.Combine(
                    RootPath, "curl-plain.bin")),
                    Is.EqualTo("curl-plain-content"));
                Assert.That(File.ReadAllText(Path.Combine(
                    RootPath, "curl-tls.bin")),
                    Is.EqualTo("curl-tls-content"));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Python_ftplib_and_ftp_tls_round_trip_with_active_data()
    {
        string python = RequireTool(
            new[] { "python3", "python" }, "--version");
        RequireCertificate();
        int plainPort = ReserveAvailablePort();
        int tlsPort = ReserveDifferentPort(new[] { plainPort });
        int passivePort = ReserveDifferentPort(new[] { plainPort, tlsPort });
        FtpServer server = CreateServer(true);

        try
        {
            server.StartServer(CreateOptions(
                plainPort, tlsPort, passivePort));
            RunPythonRoundTrip(
                python, plainPort, false, "python-plain");
            RunPythonRoundTrip(
                python, tlsPort, true, "python-tls");

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllText(Path.Combine(
                    RootPath, "python-plain.bin")),
                    Is.EqualTo("python-plain-content"));
                Assert.That(File.ReadAllText(Path.Combine(
                    RootPath, "python-tls.bin")),
                    Is.EqualTo("python-tls-content"));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Fluent_ftp_sequence_negotiates_utf8_and_machine_listing()
    {
        int controlPort = ReserveAvailablePort();
        int passivePort = ReserveDifferentPort(new[] { controlPort });
        FtpServer server = CreateServer(false);

        try
        {
            server.StartServer(CreateOptions(
                controlPort, 0, passivePort));
            using RawFtpSession session = new RawFtpSession(controlPort);
            Assert.That(session.Greeting.Code, Is.EqualTo(220));
            Assert.That(session.Command("USER alice").Code, Is.EqualTo(331));
            Assert.That(session.Command("PASS secret").Code, Is.EqualTo(230));
            RawFtpReply features = session.Command("FEAT");
            RawFtpReply utf8 = session.Command("OPTS UTF8 ON");
            RawFtpReply system = session.Command("SYST");
            RawFtpReply working = session.Command("PWD");
            RawFtpReply type = session.Command("TYPE I");
            string listing = session.ReceivePassiveData("EPSV", "MLSD /");
            RawFtpReply size = session.Command("SIZE /seed.txt");
            RawFtpReply modified = session.Command("MDTM /seed.txt");
            RawFtpReply quit = session.Command("QUIT");

            Assert.Multiple(() =>
            {
                Assert.That(features.Code, Is.EqualTo(211));
                Assert.That(features.Text, Does.Contain("UTF8"));
                Assert.That(features.Text, Does.Contain("MLST"));
                Assert.That(utf8.Code, Is.AnyOf(200, 202));
                Assert.That(system.Code, Is.EqualTo(215));
                Assert.That(working.Code, Is.EqualTo(257));
                Assert.That(type.Code, Is.EqualTo(200));
                Assert.That(listing, Does.Contain("type=file;"));
                Assert.That(listing, Does.Contain("seed.txt"));
                Assert.That(size.Code, Is.EqualTo(213));
                Assert.That(size.Text, Does.Contain("20"));
                Assert.That(modified.Code, Is.EqualTo(213));
                Assert.That(quit.Code, Is.EqualTo(221));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Apache_commons_net_sequence_lists_and_retrieves_in_passive_mode()
    {
        int controlPort = ReserveAvailablePort();
        int passivePort = ReserveDifferentPort(new[] { controlPort });
        FtpServer server = CreateServer(false);

        try
        {
            server.StartServer(CreateOptions(
                controlPort, 0, passivePort));
            using RawFtpSession session = new RawFtpSession(controlPort);
            Assert.That(session.Greeting.Code, Is.EqualTo(220));
            Assert.That(session.Command("USER alice").Code, Is.EqualTo(331));
            Assert.That(session.Command("PASS secret").Code, Is.EqualTo(230));
            RawFtpReply system = session.Command("SYST");
            RawFtpReply ascii = session.Command("TYPE A");
            string listing = session.ReceivePassiveData("PASV", "LIST /");
            RawFtpReply binary = session.Command("TYPE I");
            string content = session.ReceivePassiveData(
                "PASV", "RETR /seed.txt");
            RawFtpReply noop = session.Command("NOOP");
            RawFtpReply quit = session.Command("QUIT");

            Assert.Multiple(() =>
            {
                Assert.That(system.Code, Is.EqualTo(215));
                Assert.That(ascii.Code, Is.EqualTo(200));
                Assert.That(listing, Does.Contain("seed.txt"));
                Assert.That(binary.Code, Is.EqualTo(200));
                Assert.That(content, Is.EqualTo("external-client-seed"));
                Assert.That(noop.Code, Is.EqualTo(200));
                Assert.That(quit.Code, Is.EqualTo(221));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    private void RunCurlRoundTrip(
        string curl,
        int port,
        bool tls,
        string name)
    {
        string uploadPath = Path.Combine(RootPath, name + ".local");
        string downloadPath = Path.Combine(RootPath, name + ".download");
        File.WriteAllText(
            uploadPath, name + "-content", Encoding.ASCII);
        string url = "ftp://127.0.0.1:" + port.ToString() +
            "/" + name + ".bin";
        List<string> common = new List<string>
        {
            "--silent",
            "--show-error",
            "--fail",
            "--ftp-port",
            "-",
            "--user",
            "alice:secret"
        };
        if (tls)
        {
            common.Add("--ssl-reqd");
            common.Add("--insecure");
        }
        List<string> upload = new List<string>(common);
        upload.Add("--upload-file");
        upload.Add(uploadPath);
        upload.Add(url);
        ProcessResult uploaded = RunProcess(curl, upload.ToArray());
        Assert.That(uploaded.ExitCode, Is.Zero, uploaded.Error);
        List<string> download = new List<string>(common);
        download.Add("--output");
        download.Add(downloadPath);
        download.Add(url);
        ProcessResult downloaded = RunProcess(curl, download.ToArray());
        Assert.That(downloaded.ExitCode, Is.Zero, downloaded.Error);
        Assert.That(File.ReadAllText(downloadPath),
            Is.EqualTo(name + "-content"));
    }

    private void RunPythonRoundTrip(
        string python,
        int port,
        bool tls,
        string name)
    {
        string uploadPath = Path.Combine(RootPath, name + ".local");
        string downloadPath = Path.Combine(RootPath, name + ".download");
        File.WriteAllText(
            uploadPath, name + "-content", Encoding.ASCII);
        string script =
            "import ftplib, ssl, sys\n" +
            "host, port, upload, download, remote, use_tls = sys.argv[1:]\n" +
            "if use_tls == '1':\n" +
            "    client = ftplib.FTP_TLS(context=ssl._create_unverified_context())\n" +
            "else:\n" +
            "    client = ftplib.FTP()\n" +
            "client.connect(host, int(port), timeout=10)\n" +
            "if use_tls == '1': client.auth()\n" +
            "client.login('alice', 'secret')\n" +
            "if use_tls == '1': client.prot_p()\n" +
            "client.set_pasv(False)\n" +
            "with open(upload, 'rb') as source:\n" +
            "    client.storbinary('STOR /' + remote, source, blocksize=1024)\n" +
            "with open(download, 'wb') as target:\n" +
            "    client.retrbinary('RETR /' + remote, target.write, blocksize=1024)\n" +
            "names = client.nlst('/')\n" +
            "client.quit()\n" +
            "assert any(remote in item for item in names), names\n" +
            "print('OK')\n";
        ProcessResult result = RunProcess(
            python,
            new[]
            {
                "-c",
                script,
                "127.0.0.1",
                port.ToString(),
                uploadPath,
                downloadPath,
                name + ".bin",
                tls ? "1" : "0"
            });
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        Assert.That(result.Output, Does.Contain("OK"));
        Assert.That(File.ReadAllText(downloadPath),
            Is.EqualTo(name + "-content"));
    }

    private FtpServer CreateServer(bool tls)
    {
        IFtpCertificateProvider certificateProvider;
        if (tls)
        {
            FtpTlsOptions certificateOptions = new FtpTlsOptions();
            certificateOptions.ServerCertificate = Certificate;
            certificateProvider =
                new DefaultFtpCertificateProvider(certificateOptions);
        }
        else
        {
            certificateProvider = new UnavailableCertificateProvider();
        }
        return new FtpServer(
            new LocalFtpFileSystem(RootPath),
            new FixedUserFtpAuthenticator(CreateUsers()),
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(
                Array.Empty<FtpVirtualHostOptions>()),
            certificateProvider,
            new NullFtpEventSink());
    }

    private FtpServerOptions CreateOptions(
        int plainPort,
        int tlsPort,
        int passivePort)
    {
        List<FtpListenerOptions> listeners = new List<FtpListenerOptions>();
        listeners.Add(CreateListener(
            plainPort, FtpSecurityMode.Plain));
        if (tlsPort != 0)
        {
            listeners.Add(CreateListener(
                tlsPort, FtpSecurityMode.ExplicitTlsRequired));
        }
        FtpServerOptions options = new FtpServerOptions();
        options.Listeners = listeners.ToArray();
        options.DataConnection.Mode = FtpDataConnectionMode.Automatic;
        options.DataConnection.PassivePortStart = passivePort;
        options.DataConnection.PassivePortEnd = passivePort;
        options.Tls.ServerCertificate = Certificate;
        options.Tls.CheckCertificateRevocation = false;
        options.Tls.RequireDataSessionReuse = false;
        return options;
    }

    private FtpListenerOptions CreateListener(
        int port,
        FtpSecurityMode securityMode)
    {
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = IPAddress.Loopback.ToString();
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = securityMode;
        listener.DualStack = false;
        listener.AllowPlaintextCredentials =
            securityMode == FtpSecurityMode.Plain;
        listener.Tls.ServerCertificate = Certificate;
        listener.Tls.CheckCertificateRevocation = false;
        listener.Tls.RequireDataSessionReuse = false;
        return listener;
    }

    private static FtpFixedUserOptions CreateUsers()
    {
        byte[] salt = Encoding.ASCII.GetBytes("external-client-salt");
        byte[] password = Encoding.ASCII.GetBytes("secret");
        byte[] salted = new byte[salt.Length + password.Length];
        Buffer.BlockCopy(salt, 0, salted, 0, salt.Length);
        Buffer.BlockCopy(password, 0, salted, salt.Length, password.Length);
        FtpUserRecord user = new FtpUserRecord();
        user.UserName = "alice";
        user.Salt = salt;
        user.PasswordVerifier = SHA256.HashData(salted);
        user.HomeDirectory = "/";
        user.Disabled = false;
        user.AllowWrite = true;
        FtpFixedUserOptions users = new FtpFixedUserOptions();
        users.Users = new[] { user };
        users.RequireProtectedControl = false;
        users.FailureDelayMilliseconds = 0;
        return users;
    }

    private void RequireCertificate()
    {
        if (Certificate != null)
        {
            return;
        }
        Certificate = FtpTlsStreamTests.CreateCertificate();
    }

    private static string RequireTool(
        string[] candidates,
        string versionArgument)
    {
        int index = 0;
        while (index < candidates.Length)
        {
            try
            {
                ProcessResult result = RunProcess(
                    candidates[index], new[] { versionArgument });
                if (result.ExitCode == 0)
                {
                    return candidates[index];
                }
            }
            catch (Exception)
            {
            }
            index++;
        }
        Assert.Ignore(
            "The required external client executable is unavailable.");
        return "";
    }

    private static ProcessResult RunProcess(
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
        using Process process = new Process();
        process.StartInfo = start;
        if (!process.Start())
        {
            throw new InvalidOperationException(
                "The external client process could not be started.");
        }
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15000))
        {
            process.Kill(true);
            process.WaitForExit();
            throw new TimeoutException(
                "The external client process exceeded its test deadline.");
        }
        Task.WaitAll(new Task[] { output, error });
        ProcessResult result = new ProcessResult();
        result.ExitCode = process.ExitCode;
        result.Output = output.Result;
        result.Error = error.Result;
        return result;
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.ExclusiveAddressUse = true;
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static int ReserveDifferentPort(int[] excluded)
    {
        int port = ReserveAvailablePort();
        while (excluded.Contains(port))
        {
            port = ReserveAvailablePort();
        }
        return port;
    }

    private sealed class ProcessResult
    {
        public int ExitCode;
        public string Output = "";
        public string Error = "";
    }

    private sealed class RawFtpReply
    {
        public int Code;
        public string Text = "";
    }

    private sealed class RawFtpSession : IDisposable
    {
        private TcpClient Client;
        private StreamReader Reader;
        private StreamWriter Writer;
        public RawFtpReply Greeting;

        public RawFtpSession(int port)
        {
            Client = new TcpClient(AddressFamily.InterNetwork);
            Client.Connect(IPAddress.Loopback, port);
            NetworkStream stream = Client.GetStream();
            stream.ReadTimeout = 5000;
            stream.WriteTimeout = 5000;
            Reader = new StreamReader(
                stream, Encoding.ASCII, false, 1024, true);
            Writer = new StreamWriter(
                stream, Encoding.ASCII, 1024, true);
            Writer.NewLine = "\r\n";
            Writer.AutoFlush = true;
            Greeting = ReadReply();
        }

        public RawFtpReply Command(string command)
        {
            Writer.WriteLine(command);
            return ReadReply();
        }

        public string ReceivePassiveData(
            string passiveCommand,
            string transferCommand)
        {
            RawFtpReply passive = Command(passiveCommand);
            Assert.That(passive.Code, Is.AnyOf(227, 229));
            int port = passive.Code == 229 ?
                ParseExtendedPassivePort(passive.Text) :
                ParsePassivePort(passive.Text);
            using TcpClient data = new TcpClient(AddressFamily.InterNetwork);
            data.Connect(IPAddress.Loopback, port);
            RawFtpReply preliminary = Command(transferCommand);
            Assert.That(preliminary.Code, Is.EqualTo(150));
            using StreamReader dataReader = new StreamReader(
                data.GetStream(), Encoding.ASCII, false, 1024, true);
            string content = dataReader.ReadToEnd();
            RawFtpReply completed = ReadReply();
            Assert.That(completed.Code, Is.EqualTo(226));
            return content;
        }

        public RawFtpReply ReadReply()
        {
            string? first = Reader.ReadLine();
            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Length, Is.GreaterThanOrEqualTo(3));
            int code = int.Parse(first.Substring(0, 3));
            StringBuilder text = new StringBuilder(first);
            if (first.Length > 3 && first[3] == '-')
            {
                string terminalPrefix = code.ToString() + " ";
                string? line;
                do
                {
                    line = Reader.ReadLine();
                    Assert.That(line, Is.Not.Null);
                    text.Append('\n');
                    text.Append(line);
                }
                while (!line!.StartsWith(
                    terminalPrefix, StringComparison.Ordinal));
            }
            RawFtpReply reply = new RawFtpReply();
            reply.Code = code;
            reply.Text = text.ToString();
            return reply;
        }

        public void Dispose()
        {
            Writer.Dispose();
            Reader.Dispose();
            Client.Dispose();
        }

        private static int ParseExtendedPassivePort(string reply)
        {
            int end = reply.LastIndexOf('|');
            int start = reply.LastIndexOf('|', end - 1);
            return int.Parse(reply.Substring(start + 1, end - start - 1));
        }

        private static int ParsePassivePort(string reply)
        {
            int start = reply.LastIndexOf('(');
            int end = reply.IndexOf(')', start + 1);
            string[] fields = reply.Substring(
                start + 1, end - start - 1).Split(',');
            return int.Parse(fields[4]) * 256 + int.Parse(fields[5]);
        }
    }

    private sealed class UnavailableCertificateProvider : IFtpCertificateProvider
    {
        public FtpCertificateResult SelectServerCertificate(
            FtpCertificateRequest request)
        {
            FtpCertificateResult result = new FtpCertificateResult();
            result.Handled = true;
            result.Succeeded = false;
            result.FailureMessage = "TLS is unavailable in this plain FTP test.";
            return result;
        }
    }
}
