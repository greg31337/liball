using System.Text;

namespace pop3.Tests;

public class Pop3CapabilityBuilderTests
{
    [Test]
    public void AuthorizationCapabilitiesReflectProvidersAndTransportSecurity()
    {
        Pop3ServerOptions options = CreateFullOptions();
        ContextExtension extension = new ContextExtension();
        options.Extensions = new IPop3ServerExtension[] { extension };
        Pop3ServerStateMachine state = OpenState(options, false);
        Pop3CapabilityBuilder builder = new Pop3CapabilityBuilder();

        Pop3CapabilitySet plaintext = builder.BuildCapabilities(options, state);

        Assert.That(CapabilityNames(plaintext), Is.EqualTo(new[]
        {
            "TOP", "SASL", "RESP-CODES", "PIPELINING", "UIDL",
            "IMPLEMENTATION", "STLS", "AUTH-RESP-CODE", "UTF8", "LANG",
            "LOGIN-DELAY", "EXPIRE", "X-SESSION"
        }));
        Assert.That(plaintext.SupportsUser, Is.False);
        Assert.That(plaintext.SaslMechanisms, Is.EqualTo(new[] { "SCRAM-SHA-256" }));
        Assert.That(plaintext.SupportsStls, Is.True);
        Assert.That(plaintext.SupportsUtf8User, Is.True);
        Assert.That(plaintext.LoginDelaySeconds, Is.EqualTo(60));
        Assert.That(plaintext.LoginDelayPerUser, Is.True);
        Assert.That(plaintext.ExpireNever, Is.True);
        Assert.That(plaintext.ExpirePerUser, Is.True);
        Assert.That(plaintext.Implementation, Is.EqualTo("Example POP3 Server"));
        Assert.That(extension.LastContext!.State,
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(extension.LastContext.IsSecure, Is.False);
        Assert.That(extension.LastContext.IsAuthenticated, Is.False);
        Assert.That(extension.LastContext.Capabilities.Entries,
            Has.Length.EqualTo(10));

        state.ReplaceTransportStream(state.GetTransportStream(), true);
        Pop3CapabilitySet secure = builder.BuildCapabilities(options, state);

        Assert.That(secure.SupportsUser, Is.True);
        Assert.That(secure.SaslMechanisms,
            Is.EqualTo(new[] { "SCRAM-SHA-256", "PLAIN" }));
        Assert.That(secure.SupportsStls, Is.False);
        Assert.That(extension.LastContext!.IsSecure, Is.True);
        state.CloseConnection();
    }

    [Test]
    public void TransactionCapabilitiesReflectAuthenticationUtf8AndLanguageState()
    {
        Pop3ServerOptions options = CreateFullOptions();
        options.AllowUnprotectedCredentials = true;
        ContextExtension extension = new ContextExtension();
        options.Extensions = new IPop3ServerExtension[] { extension };
        Pop3ServerStateMachine state = OpenState(options, false);
        state.EnableUtf8Mode();
        state.SelectLanguage("sv");
        state.EnterTransaction();

        Pop3CapabilitySet capabilities =
            new Pop3CapabilityBuilder().BuildCapabilities(options, state);

        Assert.That(capabilities.SupportsTop, Is.True);
        Assert.That(capabilities.SupportsUser, Is.True);
        Assert.That(capabilities.SaslMechanisms,
            Is.EqualTo(new[] { "SCRAM-SHA-256", "PLAIN" }));
        Assert.That(capabilities.SupportsStls, Is.False);
        Assert.That(capabilities.HasLoginDelay, Is.True);
        Assert.That(capabilities.LoginDelaySeconds, Is.EqualTo(7));
        Assert.That(capabilities.LoginDelayPerUser, Is.False);
        Assert.That(capabilities.HasExpire, Is.True);
        Assert.That(capabilities.ExpireNever, Is.False);
        Assert.That(capabilities.ExpireDays, Is.EqualTo(30));
        Assert.That(capabilities.ExpirePerUser, Is.False);
        Assert.That(extension.LastContext!.State,
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(extension.LastContext.IsAuthenticated, Is.True);
        Assert.That(extension.LastContext.IsUtf8Enabled, Is.True);
        Assert.That(extension.LastContext.SelectedLanguage, Is.EqualTo("sv"));
        state.CloseConnection();
    }

    [Test]
    public void MissingProvidersSuppressProviderBackedCapabilities()
    {
        Pop3ServerOptions options = CreateOptions();
        options.EnableUtf8 = true;
        options.EnableUtf8User = true;
        options.LanguageProvider = null;
        Pop3ServerStateMachine state = OpenState(options, false);

        Pop3CapabilitySet capabilities =
            new Pop3CapabilityBuilder().BuildCapabilities(options, state);

        Assert.That(CapabilityNames(capabilities), Is.EqualTo(new[]
        {
            "RESP-CODES", "PIPELINING", "UTF8"
        }));
        Assert.That(capabilities.SupportsTop, Is.False);
        Assert.That(capabilities.SupportsUser, Is.False);
        Assert.That(capabilities.SupportsUidl, Is.False);
        Assert.That(capabilities.SupportsAuthenticationResponseCode, Is.False);
        Assert.That(capabilities.SupportsUtf8User, Is.False);
        Assert.That(capabilities.SupportsLang, Is.False);
        state.CloseConnection();
    }

    [Test]
    public void SaslMechanismsAreCanonicalizedDeduplicatedAndSecurityFiltered()
    {
        Pop3ServerOptions options = CreateOptions();
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new SaslFactory("scram-sha-256"),
            new SaslFactory("SCRAM-SHA-256"),
            new SaslFactory("plain")
        };
        Pop3ServerStateMachine state = OpenState(options, false);
        Pop3CapabilityBuilder builder = new Pop3CapabilityBuilder();

        Assert.That(builder.BuildCapabilities(options, state).SaslMechanisms,
            Is.EqualTo(new[] { "SCRAM-SHA-256" }));

        options.AllowUnprotectedCredentials = true;
        Assert.That(builder.BuildCapabilities(options, state).SaslMechanisms,
            Is.EqualTo(new[] { "SCRAM-SHA-256", "PLAIN" }));
        state.CloseConnection();
    }

    [TestCase("")]
    [TestCase("TOO-LONG-MECHANISM-123")]
    [TestCase("PLAIN!")]
    [TestCase("PL AIN")]
    public void InvalidSaslMechanismNamesAreRejected(string mechanism)
    {
        Pop3ServerOptions options = CreateOptions();
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new SaslFactory(mechanism)
        };
        Pop3ServerStateMachine state = OpenState(options, false);

        Assert.Throws<InvalidDataException>(() =>
            new Pop3CapabilityBuilder().BuildCapabilities(options, state));
        state.CloseConnection();
    }

    [Test]
    public void ExtensionCapabilitiesRejectManagedNamesDuplicatesAndBadRegisteredArguments()
    {
        AssertExtensionFailure(Capability("top"));
        AssertExtensionFailure(Capability("X-DUP"), Capability("x-dup"));
        AssertExtensionFailure(Capability("LOGIN-DELAY", "-1"));
        AssertExtensionFailure(Capability("EXPIRE", "NEVER", "GLOBAL"));
        AssertExtensionFailure(Capability("STLS"));
    }

    [TestCase(" leading")]
    [TestCase("trailing ")]
    [TestCase("two  spaces")]
    [TestCase("tab\ttext")]
    [TestCase("nonascii-é")]
    public void InvalidImplementationTextIsRejected(string implementation)
    {
        Pop3ServerOptions options = CreateOptions();
        options.ImplementationText = implementation;
        Pop3ServerStateMachine state = OpenState(options, false);

        Assert.Throws<InvalidDataException>(() =>
            new Pop3CapabilityBuilder().BuildCapabilities(options, state));
        state.CloseConnection();
    }

    [Test]
    public void CapabilityLinesAreValidatedBeforeAnyCapaResponseIsWritten()
    {
        DuplexStream stream = new DuplexStream("CAPA\r\n");
        Pop3ServerOptions options = CreateOptions();
        options.ImplementationText = new string('X', 511);
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        engine.ReadNextCommand(CancellationToken.None);

        Assert.Throws<InvalidDataException>(() =>
            engine.WriteCapabilitiesResponse(CancellationToken.None));
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void AuthorizationCapaWritesEveryCapabilityWithExactFraming()
    {
        DuplexStream stream = new DuplexStream("capa\r\n");
        Pop3ServerOptions options = CreateFullOptions();
        options.AllowUnprotectedCredentials = true;
        options.Extensions = new IPop3ServerExtension[]
        {
            new ContextExtension()
        };
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);
        Pop3CapabilitySet capabilities = engine.WriteCapabilitiesResponse(
            CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(capabilities.Implementation,
            Is.EqualTo("Example POP3 Server"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Capability list follows\r\n" +
            "TOP\r\n" +
            "USER\r\n" +
            "SASL SCRAM-SHA-256 PLAIN\r\n" +
            "RESP-CODES\r\n" +
            "PIPELINING\r\n" +
            "UIDL\r\n" +
            "IMPLEMENTATION Example POP3 Server\r\n" +
            "STLS\r\n" +
            "AUTH-RESP-CODE\r\n" +
            "UTF8 USER\r\n" +
            "LANG\r\n" +
            "LOGIN-DELAY 60 user\r\n" +
            "EXPIRE never user\r\n" +
            "X-SESSION AUTHORIZATION\r\n" +
            ".\r\n"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void TransactionCapaWritesStateSpecificArgumentsWithExactFraming()
    {
        DuplexStream stream = new DuplexStream("CAPA\r\n");
        Pop3ServerOptions options = CreateFullOptions();
        options.AllowUnprotectedCredentials = true;
        options.Extensions = new IPop3ServerExtension[]
        {
            new ContextExtension()
        };
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        engine.GetStateMachine().EnableUtf8Mode();
        engine.GetStateMachine().SelectLanguage("sv");
        engine.GetStateMachine().EnterTransaction();

        engine.ReadNextCommand(CancellationToken.None);
        Pop3CapabilitySet capabilities = engine.WriteCapabilitiesResponse(
            CancellationToken.None);

        Assert.That(capabilities.SupportsStls, Is.False);
        Assert.That(capabilities.LoginDelaySeconds, Is.EqualTo(7));
        Assert.That(capabilities.ExpireDays, Is.EqualTo(30));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Capability list follows\r\n" +
            "TOP\r\n" +
            "USER\r\n" +
            "SASL SCRAM-SHA-256 PLAIN\r\n" +
            "RESP-CODES\r\n" +
            "PIPELINING\r\n" +
            "UIDL\r\n" +
            "IMPLEMENTATION Example POP3 Server\r\n" +
            "AUTH-RESP-CODE\r\n" +
            "UTF8 USER\r\n" +
            "LANG\r\n" +
            "LOGIN-DELAY 7\r\n" +
            "EXPIRE 30\r\n" +
            "X-SESSION TRANSACTION\r\n" +
            ".\r\n"));
        engine.CloseSession();
        stream.Dispose();
    }

    [Test]
    public void ExtensionCapabilityNamesCannotCollideAcrossProviders()
    {
        Pop3ServerOptions options = CreateOptions();
        options.Extensions = new IPop3ServerExtension[]
        {
            new FixedExtension(new[] { Capability("X-FIRST", "one") }),
            new FixedExtension(new[] { Capability("x-first", "two") })
        };
        Pop3ServerStateMachine state = OpenState(options, false);

        Assert.Throws<InvalidDataException>(() =>
            new Pop3CapabilityBuilder().BuildCapabilities(options, state));
        state.CloseConnection();
    }

    [Test]
    public void BuilderRejectsInactiveStateAndNullConfigurationCollections()
    {
        Pop3CapabilityBuilder builder = new Pop3CapabilityBuilder();
        Pop3ServerOptions options = CreateOptions();
        Pop3ServerStateMachine inactive = new Pop3ServerStateMachine();

        Assert.Throws<Pop3StateException>(() =>
            builder.BuildCapabilities(options, inactive));

        Pop3ServerStateMachine active = OpenState(options, false);
        options.SaslFactories = null!;
        Assert.Throws<ArgumentException>(() =>
            builder.BuildCapabilities(options, active));
        options.SaslFactories = Array.Empty<IPop3SaslServerFactory>();
        options.Extensions = null!;
        Assert.Throws<ArgumentException>(() =>
            builder.BuildCapabilities(options, active));
        active.CloseConnection();
    }

    static void AssertExtensionFailure(params Pop3Capability[] capabilities)
    {
        Pop3ServerOptions options = CreateOptions();
        options.Extensions = new IPop3ServerExtension[]
        {
            new FixedExtension(capabilities)
        };
        Pop3ServerStateMachine state = OpenState(options, false);

        Assert.Throws<InvalidDataException>(() =>
            new Pop3CapabilityBuilder().BuildCapabilities(options, state));
        state.CloseConnection();
    }

    static Pop3ServerOptions CreateFullOptions()
    {
        Pop3ServerOptions options = CreateOptions();
        options.SecurityMode = Pop3SecurityMode.StlsIfAvailable;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.MailboxProvider = new MailboxProvider();
        options.SaslFactories = new IPop3SaslServerFactory[]
        {
            new SaslFactory("scram-sha-256"),
            new SaslFactory("PLAIN")
        };
        options.ImplementationText = "Example POP3 Server";
        options.EnableUtf8 = true;
        options.EnableUtf8User = true;
        options.LanguageProvider = new LanguageProvider();
        options.TlsProvider = new TlsProvider();
        return options;
    }

    static Pop3ServerOptions CreateOptions()
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        return options;
    }

    static Pop3ServerStateMachine OpenState(
        Pop3ServerOptions options,
        bool isSecure)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = new MemoryStream();
        connection.IsSecure = isSecure;
        Pop3ServerStateMachine state = new Pop3ServerStateMachine();
        state.OpenConnection(connection, options, 1);
        return state;
    }

    static string[] CapabilityNames(Pop3CapabilitySet capabilities)
    {
        string[] names = new string[capabilities.Entries.Length];
        int index = 0;
        while (index < names.Length)
        {
            names[index] = capabilities.Entries[index].Name;
            index++;
        }
        return names;
    }

    static Pop3Capability Capability(string name, params string[] parameters)
    {
        Pop3Capability capability = new Pop3Capability();
        capability.Name = name;
        capability.Parameters = parameters;
        return capability;
    }

    sealed class AuthenticationProvider : IPop3AuthenticationProvider
    {
        public Pop3AuthenticationResult CheckLoginDelay(
            string username,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
        }

        public Pop3AuthenticationResult AuthenticateUserPassword(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
        }

        public Pop3AuthenticationResult AuthenticateApop(
            Pop3ApopRequest request,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
        }

        public Pop3AuthenticationResult AuthenticateSaslPlain(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return new Pop3AuthenticationResult();
        }
    }

    sealed class MailboxProvider : IPop3MailboxProvider
    {
        public Pop3MailboxOpenResult OpenMailbox(
            Pop3MailboxOpenRequest request,
            CancellationToken cancellationToken)
        {
            return new Pop3MailboxOpenResult();
        }
    }

    sealed class SaslFactory : IPop3SaslServerFactory
    {
        string Mechanism;

        internal SaslFactory(string mechanism)
        {
            Mechanism = mechanism;
        }

        public string GetMechanism()
        {
            return Mechanism;
        }

        public IPop3SaslServerSession CreateSession(Pop3ServerConnection connection)
        {
            return null!;
        }
    }

    sealed class LanguageProvider : IPop3LanguageProvider
    {
        public Pop3Language[] GetLanguages()
        {
            return Array.Empty<Pop3Language>();
        }

        public bool SupportsLanguage(string languageTag)
        {
            return false;
        }

        public string GetResponseText(Pop3LanguageTextRequest request)
        {
            return "";
        }
    }

    sealed class TlsProvider : IPop3ServerTlsProvider
    {
        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            return stream;
        }
    }

    sealed class ContextExtension : IPop3ServerExtension
    {
        internal Pop3ExtensionContext? LastContext;

        public Pop3Capability[] GetCapabilities(Pop3ExtensionContext context)
        {
            LastContext = context;
            if (context.State == Pop3SessionState.Authorization)
            {
                return new[]
                {
                    Capability("login-delay", "60", "user"),
                    Capability("expire", "never", "user"),
                    Capability("X-SESSION", "AUTHORIZATION")
                };
            }
            return new[]
            {
                Capability("login-delay", "7"),
                Capability("expire", "30"),
                Capability("X-SESSION", "TRANSACTION")
            };
        }

        public string[] GetCommandNames()
        {
            return Array.Empty<string>();
        }

        public Pop3SessionState[] GetAllowedStates(string commandName)
        {
            return Array.Empty<Pop3SessionState>();
        }

        public Pop3ExtensionResult ExecuteCommand(
            Pop3ExtensionContext context,
            Pop3Command command,
            CancellationToken cancellationToken)
        {
            return new Pop3ExtensionResult();
        }
    }

    sealed class FixedExtension : IPop3ServerExtension
    {
        Pop3Capability[] Capabilities;

        internal FixedExtension(Pop3Capability[] capabilities)
        {
            Capabilities = capabilities;
        }

        public Pop3Capability[] GetCapabilities(Pop3ExtensionContext context)
        {
            return Capabilities;
        }

        public string[] GetCommandNames()
        {
            return Array.Empty<string>();
        }

        public Pop3SessionState[] GetAllowedStates(string commandName)
        {
            return Array.Empty<Pop3SessionState>();
        }

        public Pop3ExtensionResult ExecuteCommand(
            Pop3ExtensionContext context,
            Pop3Command command,
            CancellationToken cancellationToken)
        {
            return new Pop3ExtensionResult();
        }
    }

    sealed class DuplexStream : Stream
    {
        byte[] Input;
        int InputOffset;
        MemoryStream Output = new MemoryStream();

        internal DuplexStream(string input)
        {
            Input = Encoding.ASCII.GetBytes(input);
        }

        internal string WrittenText()
        {
            return Encoding.ASCII.GetString(Output.ToArray());
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int available = Input.Length - InputOffset;
            int copied = Math.Min(count, available);
            if (copied > 0)
            {
                Array.Copy(Input, InputOffset, buffer, offset, copied);
                InputOffset += copied;
            }
            return copied;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Output.Write(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
