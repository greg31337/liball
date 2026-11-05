using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ftp.Tests;

public class PublicImplementationContractTests
{
    [Test]
    public void Every_public_implementation_maps_exactly_its_declared_contract_operations()
    {
        Type[] implementations =
        {
            typeof(AllowAllFtpAuthorizer),
            typeof(AnonymousFtpAuthenticator),
            typeof(DefaultFtpCertificateProvider),
            typeof(DefaultFtpHostResolver),
            typeof(FixedUserFtpAuthenticator),
            typeof(FtpClient),
            typeof(FtpCommandCodec),
            typeof(FtpListingParser),
            typeof(FtpReplyCodec),
            typeof(FtpServer),
            typeof(LocalFtpFileSystem),
            typeof(NullFtpEventSink),
            typeof(StrictFtpCertificateValidator)
        };
        Type[] contracts =
        {
            typeof(IFtpAuthorizer),
            typeof(IFtpAuthenticator),
            typeof(IFtpCertificateProvider),
            typeof(IFtpHostResolver),
            typeof(IFtpAuthenticator),
            typeof(IFtpClient),
            typeof(IFtpCommandCodec),
            typeof(IFtpListingParser),
            typeof(IFtpReplyCodec),
            typeof(IFtpServer),
            typeof(IFtpFileSystem),
            typeof(IFtpEventSink),
            typeof(IFtpCertificateValidator)
        };
        Type[] discovered = typeof(FtpClient).Assembly
            .GetExportedTypes()
            .Where(type => type.IsClass &&
                type.GetInterfaces().Any(IsFtpContract))
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.That(discovered,
            Is.EqualTo(implementations.OrderBy(type => type.Name).ToArray()));
        Assert.That(implementations, Has.Length.EqualTo(contracts.Length));

        int index = 0;
        while (index < implementations.Length)
        {
            Type implementation = implementations[index];
            Type contract = contracts[index];
            Type[] declaredContracts = implementation.GetInterfaces()
                .Where(IsFtpContract)
                .ToArray();
            MethodInfo[] publicOperations = implementation.GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);
            InterfaceMapping mapping = implementation.GetInterfaceMap(contract);

            Assert.Multiple(() =>
            {
                Assert.That(declaredContracts, Is.EqualTo(new[] { contract }),
                    implementation.Name);
                Assert.That(mapping.InterfaceMethods,
                    Is.EquivalentTo(contract.GetMethods()), implementation.Name);
                Assert.That(mapping.TargetMethods,
                    Is.EquivalentTo(publicOperations), implementation.Name);
                Assert.That(mapping.TargetMethods,
                    Has.All.Matches<MethodInfo>(method =>
                        method.IsPublic &&
                        !method.IsStatic &&
                        method.DeclaringType == implementation),
                    implementation.Name);
                Assert.That(publicOperations.Select(method => method.Name),
                    Is.EquivalentTo(contract.GetMethods()
                        .Select(method => method.Name)), implementation.Name);
            });
            index++;
        }
    }

    [Test]
    public void Policy_and_provider_implementations_execute_through_their_contracts()
    {
        IFtpAuthorizer authorizer = new AllowAllFtpAuthorizer();
        FtpAuthorizationRequest authorization = new FtpAuthorizationRequest();
        authorization.Principal.Name = "alice";
        authorization.Principal.Authenticated = true;
        authorization.Operation = FtpAuthorizationOperation.Read;
        authorization.Path = "/file.txt";

        FtpFixedUserOptions anonymousOptions = new FtpFixedUserOptions();
        anonymousOptions.AllowAnonymous = true;
        anonymousOptions.RequireProtectedControl = false;
        IFtpAuthenticator anonymous =
            new AnonymousFtpAuthenticator(anonymousOptions);
        IFtpAuthenticator fixedUsers =
            new FixedUserFtpAuthenticator(anonymousOptions);
        FtpAuthenticationRequest authentication =
            new FtpAuthenticationRequest();
        authentication.UserName = "anonymous";
        authentication.Password = "mail@example.test";
        authentication.AttemptNumber = 1;

        IFtpHostResolver resolver =
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]);
        FtpVirtualHostRequest host = new FtpVirtualHostRequest();
        host.HostName = "missing.example.test";

        IFtpCertificateProvider certificateProvider =
            (IFtpCertificateProvider)RuntimeHelpers.GetUninitializedObject(
                typeof(DefaultFtpCertificateProvider));

        IFtpCertificateValidator validator =
            new StrictFtpCertificateValidator();
        IFtpEventSink events = new NullFtpEventSink();

        FtpAuthorizationResult allowed =
            authorizer.AuthorizeOperation(authorization);
        FtpAuthenticationResult anonymousResult =
            anonymous.AuthenticateIdentity(authentication);
        FtpAuthenticationResult fixedResult =
            fixedUsers.AuthenticateIdentity(authentication);
        FtpVirtualHostResult hostResult = resolver.ResolveVirtualHost(host);
        FtpCertificateResult certificateResult =
            certificateProvider.SelectServerCertificate(null!);
        FtpCertificateValidationResult validationResult =
            validator.ValidateRemoteCertificate(null!);
        events.RecordEvent(new FtpEvent());

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Handled, Is.True);
            Assert.That(allowed.Allowed, Is.True);
            Assert.That(anonymousResult.Status,
                Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(fixedResult.Status,
                Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(hostResult.Handled, Is.True);
            Assert.That(hostResult.Found, Is.False);
            Assert.That(certificateResult.Handled, Is.True);
            Assert.That(certificateResult.Succeeded, Is.False);
            Assert.That(certificateResult.Certificate, Is.Null);
            Assert.That(validationResult.Handled, Is.True);
            Assert.That(validationResult.Accepted, Is.False);
        });
    }

    [Test]
    public void Provider_implementations_fail_closed_for_missing_requests()
    {
        FtpFixedUserOptions options = new FtpFixedUserOptions();
        options.AllowAnonymous = true;
        options.RequireProtectedControl = false;
        IFtpAuthenticator anonymous =
            new AnonymousFtpAuthenticator(options);
        IFtpAuthenticator fixedUsers =
            new FixedUserFtpAuthenticator(options);
        IFtpAuthorizer authorizer = new AllowAllFtpAuthorizer();
        IFtpHostResolver resolver =
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]);
        IFtpCertificateProvider certificateProvider =
            (IFtpCertificateProvider)RuntimeHelpers.GetUninitializedObject(
                typeof(DefaultFtpCertificateProvider));
        IFtpCertificateValidator validator =
            new StrictFtpCertificateValidator();
        IFtpEventSink eventSink = new NullFtpEventSink();

        FtpAuthenticationResult anonymousResult =
            anonymous.AuthenticateIdentity(null!);
        FtpAuthenticationResult fixedResult =
            fixedUsers.AuthenticateIdentity(null!);
        FtpAuthorizationResult authorization =
            authorizer.AuthorizeOperation(null!);
        FtpVirtualHostResult host = resolver.ResolveVirtualHost(null!);
        FtpCertificateResult certificate =
            certificateProvider.SelectServerCertificate(null!);
        FtpCertificateValidationResult validation =
            validator.ValidateRemoteCertificate(null!);

        Assert.Multiple(() =>
        {
            Assert.That(anonymousResult.Status,
                Is.EqualTo(FtpAuthenticationStatus.Failure));
            Assert.That(fixedResult.Status,
                Is.EqualTo(FtpAuthenticationStatus.Failure));
            Assert.That(authorization.Handled, Is.True);
            Assert.That(authorization.Allowed, Is.False);
            Assert.That(host.Handled, Is.True);
            Assert.That(host.Found, Is.False);
            Assert.That(certificate.Handled, Is.True);
            Assert.That(certificate.Succeeded, Is.False);
            Assert.That(validation.Handled, Is.True);
            Assert.That(validation.Accepted, Is.False);
            Assert.DoesNotThrow(() => eventSink.RecordEvent(null!));
        });
    }

    [Test]
    public void Protocol_lifecycle_and_storage_implementations_execute_through_their_contracts()
    {
        IFtpCommandCodec commands = new FtpCommandCodec();
        FtpCommand command = commands.ParseCommand("noop");
        string commandWire = commands.FormatCommand(command);

        IFtpReplyCodec replies = new FtpReplyCodec();
        using MemoryStream replySource = new MemoryStream(
            Encoding.ASCII.GetBytes("200 Okay\r\n"));
        FtpReply reply = replies.ReadReply(replySource);
        FtpReplyClassification classification = replies.ClassifyReply(reply);
        using MemoryStream replyDestination = new MemoryStream();
        replies.WriteReply(replyDestination, reply);

        IFtpListingParser listings = new FtpListingParser();
        FtpListing listing = listings.ParseNameListing(new[] { "entry.txt" });

        IFtpFileSystem fileSystem = new LocalFtpFileSystem(
            TestContext.CurrentContext.WorkDirectory);
        FtpFixedUserOptions authenticationOptions =
            new FtpFixedUserOptions();
        IFtpClient client = new FtpClient(null);
        IFtpServer server = new FtpServer(
            fileSystem,
            new FixedUserFtpAuthenticator(authenticationOptions),
            new AllowAllFtpAuthorizer(),
            new DefaultFtpHostResolver(new FtpVirtualHostOptions[0]),
            new CertificateProviderStub(),
            new NullFtpEventSink());
        FtpFileSystemRequest fileSystemRequest = new FtpFileSystemRequest();
        fileSystemRequest.Path = "/";
        FtpFileSystemResult root = fileSystem.ResolveEntry(fileSystemRequest);

        Assert.Multiple(() =>
        {
            Assert.That(command.Kind, Is.EqualTo(FtpCommandKind.Noop));
            Assert.That(commandWire, Is.EqualTo("NOOP\r\n"));
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(classification.Succeeded, Is.True);
            Assert.That(Encoding.ASCII.GetString(replyDestination.ToArray()),
                Is.EqualTo("200 Okay\r\n"));
            Assert.That(listing.FormatName, Is.EqualTo("Names"));
            Assert.That(listing.Entries[0].Name, Is.EqualTo("entry.txt"));
            Assert.That(client.GetSessionState(),
                Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(server.GetServerState(),
                Is.EqualTo(FtpServerState.Stopped));
            Assert.That(root.Succeeded, Is.True);
            Assert.That(root.Entry, Is.Not.Null);
            Assert.That(root.Entry!.Type,
                Is.EqualTo(FtpEntryType.Directory));
        });
    }

    private static bool IsFtpContract(Type type)
    {
        return type.IsInterface &&
            type.Namespace == "ftp" &&
            type.Name.StartsWith("IFtp", StringComparison.Ordinal);
    }

    private class CertificateProviderStub : IFtpCertificateProvider
    {
        public FtpCertificateResult SelectServerCertificate(
            FtpCertificateRequest request)
        {
            FtpCertificateResult result = new FtpCertificateResult();
            result.Handled = true;
            return result;
        }
    }
}
