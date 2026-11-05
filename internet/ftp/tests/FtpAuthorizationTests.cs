namespace ftp.Tests;

public class FtpAuthorizationTests
{
    [Test]
    public void Allow_all_authorizer_handles_every_defined_operation_for_authenticated_principals()
    {
        AllowAllFtpAuthorizer authorizer = new AllowAllFtpAuthorizer();
        FtpAuthorizationOperation[] operations =
            Enum.GetValues<FtpAuthorizationOperation>();

        foreach (FtpAuthorizationOperation operation in operations)
        {
            FtpAuthorizationResult result = authorizer.AuthorizeOperation(
                CreateRequest(operation, CreatePrincipal(false)));
            Assert.Multiple(() =>
            {
                Assert.That(result.Handled, Is.True, operation.ToString());
                Assert.That(result.Allowed, Is.True, operation.ToString());
                Assert.That(result.Reason, Is.Null, operation.ToString());
            });
        }

        FtpAuthorizationRequest unauthenticated = CreateRequest(
            FtpAuthorizationOperation.Read, new FtpPrincipal());
        FtpAuthorizationRequest invalidOperation = CreateRequest(
            (FtpAuthorizationOperation)int.MaxValue, CreatePrincipal(false));
        FtpAuthorizationRequest missingDestination = CreateRequest(
            FtpAuthorizationOperation.Rename, CreatePrincipal(false));
        missingDestination.DestinationPath = null;
        FtpAuthorizationRequest unexpectedDestination = CreateRequest(
            FtpAuthorizationOperation.Read, CreatePrincipal(false));
        unexpectedDestination.DestinationPath = "/unexpected";

        Assert.Multiple(() =>
        {
            Assert.That(authorizer.AuthorizeOperation(unauthenticated).Allowed,
                Is.False);
            Assert.That(authorizer.AuthorizeOperation(invalidOperation).Allowed,
                Is.False);
            Assert.That(authorizer.AuthorizeOperation(missingDestination).Allowed,
                Is.False);
            Assert.That(authorizer.AuthorizeOperation(unexpectedDestination).Allowed,
                Is.False);
        });
    }

    [Test]
    public void Authorization_gate_forwards_every_operation_and_fails_closed()
    {
        RecordingFtpAuthorizer provider = new RecordingFtpAuthorizer();
        provider.Decision.Handled = true;
        provider.Decision.Allowed = true;
        FtpAuthorizationGate gate = new FtpAuthorizationGate(
            provider, null, null);
        FtpPrincipal principal = CreatePrincipal(false);
        FtpEntry entry = new FtpEntry();
        entry.Name = "file.txt";

        foreach (FtpAuthorizationOperation operation in
            Enum.GetValues<FtpAuthorizationOperation>())
        {
            FtpAuthorizationRequest request = CreateRequest(operation, principal);
            request.Entry = entry;
            request.ControlProtected = true;
            FtpAuthorizationResult result = gate.AuthorizeOperation(request);
            Assert.That(result.Allowed, Is.True, operation.ToString());
        }

        Assert.Multiple(() =>
        {
            Assert.That(provider.Requests, Has.Count.EqualTo(12));
            Assert.That(provider.Requests.All(request =>
                request.Principal == principal), Is.True);
            Assert.That(provider.Requests.All(request =>
                request.Entry == entry), Is.True);
            Assert.That(provider.Requests.All(request =>
                request.ControlProtected), Is.True);
        });

        provider.Decision.Allowed = false;
        provider.Decision.Reason = "Policy denied this path.";
        FtpAuthorizationResult denied = gate.AuthorizeOperation(
            CreateRequest(FtpAuthorizationOperation.Read, principal));
        provider.Decision.Handled = false;
        FtpAuthorizationResult unhandled = gate.AuthorizeOperation(
            CreateRequest(FtpAuthorizationOperation.Read, principal));
        provider.Throw = true;
        FtpAuthorizationResult faulted = gate.AuthorizeOperation(
            CreateRequest(FtpAuthorizationOperation.Read, principal));
        int requestCount = provider.Requests.Count;
        FtpAuthorizationRequest invalid = CreateRequest(
            FtpAuthorizationOperation.Read, principal);
        invalid.Path = "bad\rpath";
        FtpAuthorizationResult malformed = gate.AuthorizeOperation(invalid);
        FtpAuthorizationResult missingProvider =
            new FtpAuthorizationGate(null, null, null).AuthorizeOperation(
                CreateRequest(FtpAuthorizationOperation.Read, principal));

        Assert.Multiple(() =>
        {
            Assert.That(denied.Handled, Is.True);
            Assert.That(denied.Allowed, Is.False);
            Assert.That(denied.Reason, Is.EqualTo("Policy denied this path."));
            Assert.That(unhandled.Handled, Is.True);
            Assert.That(unhandled.Allowed, Is.False);
            Assert.That(faulted.Handled, Is.True);
            Assert.That(faulted.Allowed, Is.False);
            Assert.That(malformed.Handled, Is.True);
            Assert.That(malformed.Allowed, Is.False);
            Assert.That(provider.Requests, Has.Count.EqualTo(requestCount));
            Assert.That(missingProvider.Handled, Is.True);
            Assert.That(missingProvider.Allowed, Is.False);
        });
    }

    [Test]
    public void Authorization_gate_enforces_virtual_host_anonymous_policy()
    {
        AllowAllFtpAuthorizer provider = new AllowAllFtpAuthorizer();
        FtpVirtualHostOptions host = new FtpVirtualHostOptions();
        host.HostName = "ftp.example";
        host.RootPath = "/srv/ftp";
        FtpAuthorizationGate gate = new FtpAuthorizationGate(
            provider, host, null);
        FtpPrincipal anonymous = CreatePrincipal(true);

        FtpAuthorizationResult disabled = gate.AuthorizeOperation(
            CreateRequest(FtpAuthorizationOperation.List, anonymous));
        host.AllowAnonymous = true;
        FtpAuthorizationResult listing = gate.AuthorizeOperation(
            CreateRequest(FtpAuthorizationOperation.List, anonymous));
        FtpAuthorizationResult reading = gate.AuthorizeOperation(
            CreateRequest(FtpAuthorizationOperation.Read, anonymous));
        FtpAuthorizationResult metadata = gate.AuthorizeOperation(
            CreateRequest(FtpAuthorizationOperation.ReadMetadata, anonymous));
        FtpAuthorizationOperation[] writes = new[]
        {
            FtpAuthorizationOperation.Write,
            FtpAuthorizationOperation.Append,
            FtpAuthorizationOperation.CreateDirectory,
            FtpAuthorizationOperation.DeleteFile,
            FtpAuthorizationOperation.DeleteDirectory,
            FtpAuthorizationOperation.Rename,
            FtpAuthorizationOperation.WriteMetadata,
            FtpAuthorizationOperation.Site,
            FtpAuthorizationOperation.ServerToServer
        };
        FtpAuthorizationResult[] deniedWrites = writes.Select(operation =>
            gate.AuthorizeOperation(CreateRequest(operation, anonymous))).ToArray();
        host.AllowAnonymousWrites = true;
        FtpAuthorizationResult[] allowedWrites = writes.Select(operation =>
            gate.AuthorizeOperation(CreateRequest(operation, anonymous))).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(disabled.Allowed, Is.False);
            Assert.That(listing.Allowed, Is.True);
            Assert.That(reading.Allowed, Is.True);
            Assert.That(metadata.Allowed, Is.True);
            Assert.That(deniedWrites.All(result => !result.Allowed), Is.True);
            Assert.That(allowedWrites.All(result => result.Allowed), Is.True);
        });
    }

    [Test]
    public void Authorization_gate_builds_filesystem_site_and_fxp_requests()
    {
        RecordingFtpAuthorizer provider = new RecordingFtpAuthorizer();
        provider.Decision.Handled = true;
        provider.Decision.Allowed = true;
        FtpAuthorizationGate gate = new FtpAuthorizationGate(
            provider, null, null);
        FtpPrincipal principal = CreatePrincipal(false);
        FtpFileSystemRequest fileRequest = new FtpFileSystemRequest();
        fileRequest.Path = "/source";
        fileRequest.DestinationPath = "/target";
        FtpEntry entry = new FtpEntry();

        FtpAuthorizationResult file = gate.AuthorizeFileSystemOperation(
            principal,
            FtpAuthorizationOperation.Rename,
            fileRequest,
            entry,
            true);
        FtpAuthorizationResult site = gate.AuthorizeSiteOperation(
            principal, "CHMOD 640 /source", true);
        FtpAuthorizationResult fxp = gate.AuthorizeServerTransfer(
            principal, "/source", "/target", true);

        Assert.Multiple(() =>
        {
            Assert.That(file.Allowed, Is.True);
            Assert.That(site.Allowed, Is.True);
            Assert.That(fxp.Allowed, Is.True);
            Assert.That(provider.Requests, Has.Count.EqualTo(3));
            Assert.That(provider.Requests[0].Operation,
                Is.EqualTo(FtpAuthorizationOperation.Rename));
            Assert.That(provider.Requests[0].Path, Is.EqualTo("/source"));
            Assert.That(provider.Requests[0].DestinationPath,
                Is.EqualTo("/target"));
            Assert.That(provider.Requests[0].Entry, Is.SameAs(entry));
            Assert.That(provider.Requests[1].Operation,
                Is.EqualTo(FtpAuthorizationOperation.Site));
            Assert.That(provider.Requests[1].Path,
                Is.EqualTo("CHMOD 640 /source"));
            Assert.That(provider.Requests[2].Operation,
                Is.EqualTo(FtpAuthorizationOperation.ServerToServer));
        });
    }

    private static FtpPrincipal CreatePrincipal(bool anonymous)
    {
        FtpPrincipal principal = new FtpPrincipal();
        principal.Name = anonymous ? "anonymous" : "alice";
        principal.Authenticated = true;
        principal.Anonymous = anonymous;
        return principal;
    }

    private static FtpAuthorizationRequest CreateRequest(
        FtpAuthorizationOperation operation,
        FtpPrincipal principal)
    {
        FtpAuthorizationRequest request = new FtpAuthorizationRequest();
        request.Principal = principal;
        request.Operation = operation;
        request.Path = "/source";
        if (operation == FtpAuthorizationOperation.Rename ||
            operation == FtpAuthorizationOperation.ServerToServer)
        {
            request.DestinationPath = "/target";
        }
        return request;
    }
}

public class RecordingFtpAuthorizer : IFtpAuthorizer
{
    public List<FtpAuthorizationRequest> Requests;
    public FtpAuthorizationResult Decision;
    public bool Throw;

    public RecordingFtpAuthorizer()
    {
        Requests = new List<FtpAuthorizationRequest>();
        Decision = new FtpAuthorizationResult();
        Throw = false;
    }

    public FtpAuthorizationResult AuthorizeOperation(
        FtpAuthorizationRequest request)
    {
        if (Throw)
        {
            throw new InvalidOperationException("Provider fault.");
        }
        Requests.Add(request);
        return Decision;
    }
}
