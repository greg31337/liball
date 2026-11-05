using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace diameter.Tests;

public class FreeDiameterInteroperabilityTests
{
    private const string FreeDiameterVersion = "1.6.0";
    private const string FreeDiameterCommit =
        "5403feb543ed5e720165a4b3a3b4a365cdee28fb";
    private const string RelayIdentity = "relay.interop.test";
    private const string Realm = "interop.test";
    private const string ClientIdentity = "client.interop.test";
    private const string ServerIdentity = "server.interop.test";
    private const uint TestVendorId = 10415;
    private const uint TestVendorAvpCode = 999999;

    [Test]
    public void PinnedFreeDiameterRelaysBaseAccountingVendorAvpsAndDcca()
    {
        FreeDiameterProcess process = FreeDiameterProcess.StartIfEnabled();
        if (!process.Enabled)
        {
            Assert.Ignore(
                "Set DIAMETER_FREEDIAMETER_INTEROP=1 to run the pinned " +
                "freeDiameter " + FreeDiameterVersion + " interoperability topology.");
        }

        try
        {
            using FreeDiameterWirePeer server = new FreeDiameterWirePeer(
                process.Host,
                process.Port,
                ServerIdentity);
            using FreeDiameterWirePeer client = new FreeDiameterWirePeer(
                process.Host,
                process.Port,
                ClientIdentity);

            server.ExchangeCapabilities();
            client.ExchangeCapabilities();

            DiameterMessage watchdog = client.CreateWatchdogRequest();
            client.Send(watchdog);
            DiameterMessage watchdogAnswer = client.Receive();
            AssertAnswer(watchdog, watchdogAnswer);
            Assert.That(ReadResultCode(watchdogAnswer),
                Is.EqualTo(DiameterResultCodes.Success));

            DiameterMessage accounting = client.CreateAccountingRequest();
            accounting.Avps.Add(CreateIdentityAvp(
                DiameterAvpCodes.DestinationHost,
                ServerIdentity));
            DiameterAvp vendorAvp = new DiameterAvp();
            vendorAvp.Code = TestVendorAvpCode;
            vendorAvp.VendorId = TestVendorId;
            vendorAvp.Flags = DiameterAvpFlags.VendorSpecific;
            vendorAvp.Data = new byte[] { 0xde, 0xad, 0xbe, 0xef };
            accounting.Avps.Add(vendorAvp);
            client.Send(accounting);

            DiameterMessage relayedAccounting = server.Receive();
            AssertRelayedRequest(accounting, relayedAccounting);
            DiameterAvp relayedVendor = FindAvp(
                relayedAccounting,
                TestVendorAvpCode,
                TestVendorId);
            Assert.That(relayedVendor.Data,
                Is.EqualTo(vendorAvp.Data));
            server.Send(server.CreateAccountingAnswer(relayedAccounting));

            DiameterMessage accountingAnswer = client.Receive();
            AssertAnswer(accounting, accountingAnswer);
            Assert.That(ReadResultCode(accountingAnswer),
                Is.EqualTo(DiameterResultCodes.Success));

            DiameterMessage creditControl = client.CreateCreditControlRequest();
            client.Send(creditControl);
            DiameterMessage relayedCreditControl = server.Receive();
            AssertRelayedRequest(creditControl, relayedCreditControl);
            server.Send(server.CreateCreditControlAnswer(
                relayedCreditControl));

            DiameterMessage creditControlAnswer = client.Receive();
            AssertAnswer(creditControl, creditControlAnswer);
            Assert.That(ReadResultCode(creditControlAnswer),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(ReadUnsigned32(
                    FindAvp(creditControlAnswer,
                        DiameterAvpCodes.CcRequestType,
                        0)),
                Is.EqualTo((uint)DiameterCreditControlRequestType.Event));

            DiameterMessage disconnect = client.CreateDisconnectRequest();
            client.Send(disconnect);
            DiameterMessage disconnectAnswer = client.Receive();
            AssertAnswer(disconnect, disconnectAnswer);
            Assert.That(ReadResultCode(disconnectAnswer),
                Is.EqualTo(DiameterResultCodes.Success));
        }
        finally
        {
            process.Dispose();
        }
    }

    private static void AssertRelayedRequest(
        DiameterMessage original,
        DiameterMessage relayed)
    {
        Assert.Multiple(() =>
        {
            Assert.That(relayed.CommandCode,
                Is.EqualTo(original.CommandCode));
            Assert.That(relayed.ApplicationId,
                Is.EqualTo(original.ApplicationId));
            Assert.That(relayed.EndToEndIdentifier,
                Is.EqualTo(original.EndToEndIdentifier));
            Assert.That(relayed.Flags & DiameterMessageFlags.Request,
                Is.EqualTo(DiameterMessageFlags.Request));
            Assert.That(Encoding.UTF8.GetString(FindAvp(
                    relayed,
                    DiameterAvpCodes.RouteRecord,
                    0).Data),
                Is.EqualTo(RelayIdentity));
        });
    }

    private static void AssertAnswer(
        DiameterMessage request,
        DiameterMessage answer)
    {
        Assert.Multiple(() =>
        {
            Assert.That(answer.Flags & DiameterMessageFlags.Request,
                Is.EqualTo(DiameterMessageFlags.None));
            Assert.That(answer.CommandCode,
                Is.EqualTo(request.CommandCode));
            Assert.That(answer.ApplicationId,
                Is.EqualTo(request.ApplicationId));
            Assert.That(answer.HopByHopIdentifier,
                Is.EqualTo(request.HopByHopIdentifier));
            Assert.That(answer.EndToEndIdentifier,
                Is.EqualTo(request.EndToEndIdentifier));
        });
    }

    private static uint ReadResultCode(DiameterMessage message)
    {
        return ReadUnsigned32(FindAvp(
            message,
            DiameterAvpCodes.ResultCode,
            0));
    }

    private static uint ReadUnsigned32(DiameterAvp avp)
    {
        Assert.That(avp.Data.Length, Is.EqualTo(4));
        return BinaryPrimitives.ReadUInt32BigEndian(avp.Data);
    }

    private static DiameterAvp FindAvp(
        DiameterMessage message,
        uint code,
        uint vendorId)
    {
        DiameterAvp? result = message.Avps.Find(
            item => item.Code == code && item.VendorId == vendorId);
        Assert.That(result, Is.Not.Null,
            "Expected AVP " + vendorId + ":" + code + ".");
        return result!;
    }

    private static DiameterAvp CreateIdentityAvp(uint code, string value)
    {
        DiameterAvp result = new DiameterAvp();
        result.Code = code;
        result.Flags = DiameterAvpFlags.Mandatory;
        result.Data = Encoding.UTF8.GetBytes(value);
        return result;
    }

    private sealed class FreeDiameterWirePeer : IDisposable
    {
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly DiameterMessageCodec codec = new DiameterMessageCodec();
        private readonly FreeDiameterIdentifierSource identifiers;
        private readonly DiameterBaseCommandFactory baseCommands;
        private readonly CreditControlCommandFactory creditControlCommands;
        private readonly string identity;

        internal FreeDiameterWirePeer(string host, int port, string identity)
        {
            this.identity = identity;
            identifiers = new FreeDiameterIdentifierSource(identity);
            baseCommands = new DiameterBaseCommandFactory(identifiers);
            DiameterIdentity originHost = CreateIdentity(identity);
            DiameterIdentity originRealm = CreateIdentity(Realm);
            creditControlCommands = new CreditControlCommandFactory(
                identifiers,
                originHost,
                originRealm);
            client = new TcpClient(AddressFamily.InterNetwork);
            client.ReceiveTimeout = 10000;
            client.SendTimeout = 10000;
            client.Connect(host, port);
            stream = client.GetStream();
        }

        internal void ExchangeCapabilities()
        {
            DiameterBaseCommandOptions options = CreateOriginOptions();
            options.HostAddresses.Add(System.Net.IPAddress.Loopback);
            options.VendorId = TestVendorId;
            options.ProductName = "diameter-dotnet-interop";
            options.FirmwareRevision = 1;
            options.AuthApplicationIds.Add(
                DiameterApplicationIds.CreditControl);
            options.AcctApplicationIds.Add(
                DiameterApplicationIds.BaseAccounting);
            options.InbandSecurityIds.Add(0);
            DiameterMessage request = baseCommands
                .CreateCapabilitiesExchangeRequest(options);
            Send(request);
            DiameterMessage answer = Receive();
            AssertAnswer(request, answer);
            Assert.Multiple(() =>
            {
                Assert.That(ReadResultCode(answer),
                    Is.EqualTo(DiameterResultCodes.Success));
                Assert.That(Encoding.UTF8.GetString(FindAvp(
                        answer,
                        DiameterAvpCodes.OriginHost,
                        0).Data),
                    Is.EqualTo(RelayIdentity));
            });
        }

        internal DiameterMessage CreateWatchdogRequest()
        {
            return baseCommands.CreateDeviceWatchdogRequest(
                CreateOriginOptions());
        }

        internal DiameterMessage CreateAccountingRequest()
        {
            DiameterBaseCommandOptions options = CreateOriginOptions();
            options.DestinationRealm = CreateIdentity(Realm);
            options.SessionId = identifiers.CreateSessionIdentifier(
                CreateIdentity(identity),
                "accounting");
            options.AccountingRecordType =
                DiameterAccountingRecordType.Event;
            return baseCommands.CreateAccountingRequest(options);
        }

        internal DiameterMessage CreateAccountingAnswer(
            DiameterMessage request)
        {
            DiameterBaseCommandOptions options = CreateOriginOptions();
            options.Request = request;
            options.ResultCode = DiameterResultCodes.Success;
            return baseCommands.CreateAccountingAnswer(options);
        }

        internal DiameterMessage CreateCreditControlRequest()
        {
            DiameterCreditControlRequest request =
                new DiameterCreditControlRequest();
            request.SessionId = identifiers.CreateSessionIdentifier(
                CreateIdentity(identity),
                "credit-control");
            request.DestinationRealm = CreateIdentity(Realm);
            request.DestinationHost = CreateIdentity(ServerIdentity);
            request.ServiceContextId = "interop.test@diameter.test";
            request.RequestType = DiameterCreditControlRequestType.Event;
            request.RequestedAction = DiameterRequestedAction.CheckBalance;
            request.RequestedActionPresent = true;
            return creditControlCommands.CreateCreditControlRequest(request);
        }

        internal DiameterMessage CreateCreditControlAnswer(
            DiameterMessage request)
        {
            DiameterCreditControlAnswer answer =
                new DiameterCreditControlAnswer();
            answer.SessionId = Encoding.UTF8.GetString(FindAvp(
                request,
                DiameterAvpCodes.SessionId,
                0).Data);
            answer.RequestType = DiameterCreditControlRequestType.Event;
            answer.RequestNumber = 0;
            answer.ResultCode = DiameterResultCodes.Success;
            answer.CheckBalanceResult =
                DiameterCheckBalanceResult.EnoughCredit;
            answer.CheckBalanceResultPresent = true;
            return creditControlCommands.CreateCreditControlAnswer(
                request,
                answer);
        }

        internal DiameterMessage CreateDisconnectRequest()
        {
            DiameterBaseCommandOptions options = CreateOriginOptions();
            options.DisconnectCause =
                DiameterDisconnectCause.DoNotWantToTalkToYou;
            return baseCommands.CreateDisconnectPeerRequest(options);
        }

        internal void Send(DiameterMessage message)
        {
            DiameterEncodeResult encoded = codec.EncodeMessageToArray(message);
            Assert.That(encoded.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            stream.Write(encoded.Data, 0, encoded.Data.Length);
        }

        internal DiameterMessage Receive()
        {
            byte[] header = ReadExactly(DiameterProtocolConstants.MessageHeaderSize);
            int length = header[1] << 16 | header[2] << 8 | header[3];
            Assert.That(length,
                Is.GreaterThanOrEqualTo(DiameterProtocolConstants.MessageHeaderSize));
            byte[] data = new byte[length];
            Buffer.BlockCopy(header, 0, data, 0, header.Length);
            byte[] body = ReadExactly(length - header.Length);
            Buffer.BlockCopy(body, 0, data, header.Length, body.Length);
            DiameterDecodeResult decoded = codec.DecodeMessage(
                data,
                false,
                new DiameterLimitConfiguration());
            Assert.That(decoded.Status,
                Is.EqualTo(DiameterCodecStatus.Success));
            return decoded.Message!;
        }

        private byte[] ReadExactly(int count)
        {
            byte[] result = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(result, offset, count - offset);
                if (read == 0)
                {
                    throw new EndOfStreamException(
                        "freeDiameter closed the interoperability connection.");
                }

                offset += read;
            }

            return result;
        }

        private DiameterBaseCommandOptions CreateOriginOptions()
        {
            DiameterBaseCommandOptions result =
                new DiameterBaseCommandOptions();
            result.OriginHost = CreateIdentity(identity);
            result.OriginRealm = CreateIdentity(Realm);
            return result;
        }

        public void Dispose()
        {
            stream.Dispose();
            client.Dispose();
        }
    }

    private sealed class FreeDiameterIdentifierSource : IDiameterIdentifierSource
    {
        private uint hopByHop = 1000;
        private uint endToEnd = 2000;
        private uint session;
        private readonly string originHost;

        internal FreeDiameterIdentifierSource(string originHost)
        {
            this.originHost = originHost;
        }

        public uint CreateHopByHopIdentifier()
        {
            hopByHop += 1;
            return hopByHop;
        }

        public uint CreateEndToEndIdentifier()
        {
            endToEnd += 1;
            return endToEnd;
        }

        public string CreateSessionIdentifier(
            DiameterIdentity originHostValue,
            string? correlationText)
        {
            session += 1;
            return originHost + ";interop;" + session;
        }

        public uint CreateOriginStateIdentifier()
        {
            return 1;
        }
    }

    private sealed class FreeDiameterProcess : IDisposable
    {
        internal bool Enabled;
        internal string Host = "127.0.0.1";
        internal int Port = DiameterProtocolConstants.Port;
        private string containerName = string.Empty;

        internal static FreeDiameterProcess StartIfEnabled()
        {
            FreeDiameterProcess result = new FreeDiameterProcess();
            string enabled = Environment.GetEnvironmentVariable(
                "DIAMETER_FREEDIAMETER_INTEROP") ?? string.Empty;
            if (enabled != "1" &&
                !enabled.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            result.Enabled = true;
            string endpoint = Environment.GetEnvironmentVariable(
                "DIAMETER_FREEDIAMETER_ENDPOINT") ?? string.Empty;
            if (endpoint.Length > 0)
            {
                string[] parts = endpoint.Split(':');
                Assert.That(parts.Length, Is.EqualTo(2));
                result.Host = parts[0];
                Assert.That(int.TryParse(parts[1], out result.Port), Is.True);
                return result;
            }

            result.StartContainer();
            return result;
        }

        private void StartContainer()
        {
            string repository = FindRepositoryRoot();
            string fixture = Path.Combine(
                repository,
                "tests",
                "Interoperability",
                "freeDiameter");
            string image = "diameter-freediameter-interop:" +
                FreeDiameterVersion;
            RunDocker(
                repository,
                "build --pull --tag " + image +
                " --build-arg FREEDIAMETER_COMMIT=" +
                FreeDiameterCommit + " " + Quote(fixture));

            Port = ReservePort();
            containerName = "diameter-freediameter-interop-" +
                Guid.NewGuid().ToString("N");
            RunDocker(
                repository,
                "run --detach --name " + containerName +
                " --publish 127.0.0.1:" + Port + ":3868 " + image);
            WaitUntilReady();
        }

        private void WaitUntilReady()
        {
            Stopwatch timeout = Stopwatch.StartNew();
            while (timeout.Elapsed < TimeSpan.FromSeconds(20))
            {
                try
                {
                    using TcpClient probe = new TcpClient();
                    probe.Connect(Host, Port);
                    return;
                }
                catch (SocketException)
                {
                    Thread.Sleep(100);
                }
            }

            Assert.Fail(
                "The pinned freeDiameter container did not open its TCP port.");
        }

        private static void RunDocker(string workingDirectory, string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "docker";
            start.Arguments = arguments;
            start.WorkingDirectory = workingDirectory;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.UseShellExecute = false;
            Process? process;
            try
            {
                process = Process.Start(start);
            }
            catch (Exception error)
            {
                Assert.Fail(
                    "Docker is required for the opt-in freeDiameter test: " +
                    error.Message);
                return;
            }

            Assert.That(process, Is.Not.Null);
            string output = process!.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.That(process.ExitCode,
                Is.EqualTo(0),
                output + Environment.NewLine + errorOutput);
            process.Dispose();
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current = new DirectoryInfo(
                TestContext.CurrentContext.TestDirectory);
            while (current != null &&
                !File.Exists(Path.Combine(current.FullName, "todo.md")))
            {
                current = current.Parent;
            }

            Assert.That(current, Is.Not.Null,
                "Could not locate the repository root.");
            return current!.FullName;
        }

        private static int ReservePort()
        {
            TcpListener listener = new TcpListener(
                System.Net.IPAddress.Loopback,
                0);
            listener.Start();
            int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        public void Dispose()
        {
            if (containerName.Length > 0)
            {
                try
                {
                    RunDocker(
                        FindRepositoryRoot(),
                        "rm --force " + containerName);
                }
                catch (AssertionException)
                {
                }
            }
        }
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}
