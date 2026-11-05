namespace ftp;

internal class FtpAuthorizationGate
{
    internal IFtpAuthorizer? Provider;
    internal FtpVirtualHostOptions? Host;
    internal IFtpEventSink? EventSink;

    internal FtpAuthorizationGate(
        IFtpAuthorizer? provider,
        FtpVirtualHostOptions? host,
        IFtpEventSink? eventSink)
    {
        Provider = provider;
        Host = host;
        EventSink = eventSink;
    }

    internal FtpAuthorizationResult AuthorizeOperation(
        FtpAuthorizationRequest? request)
    {
        string? validationFailure = ValidateRequest(request);
        if (validationFailure != null)
        {
            return RecordDecision(
                request, CreateDeniedResult(validationFailure));
        }
        if (request == null)
        {
            return RecordDecision(
                request, CreateDeniedResult("Authorization denied."));
        }
        if (!AllowsAnonymousOperation(request))
        {
            return RecordDecision(request, CreateDeniedResult(
                "Anonymous access is not permitted for this operation."));
        }
        if (Provider == null)
        {
            return RecordDecision(
                request, CreateDeniedResult("Authorization denied."));
        }

        FtpFaultIsolationResult invocation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return Provider.AuthorizeOperation(request);
            });
        FtpAuthorizationResult? decision =
            invocation.Value as FtpAuthorizationResult;
        if (!invocation.Succeeded)
        {
            FtpEvent faultEvent =
                FtpEventPipeline.CreateFaultEvent("authorization provider");
            FtpEventPipeline.RecordEvent(EventSink, faultEvent);
        }
        if (!invocation.Succeeded || decision == null || !decision.Handled)
        {
            return RecordDecision(
                request, CreateDeniedResult("Authorization denied."));
        }
        if (!decision.Allowed)
        {
            string reason = "Authorization denied.";
            if (IsSafeReason(decision.Reason) && decision.Reason != null)
            {
                reason = decision.Reason;
            }
            return RecordDecision(request, CreateDeniedResult(reason));
        }
        return RecordDecision(request, CreateAllowedResult());
    }

    internal FtpAuthorizationResult RecordDecision(
        FtpAuthorizationRequest? request,
        FtpAuthorizationResult result)
    {
        FtpEvent eventData = FtpEventPipeline.CreateAuthorizationEvent(
            request, result);
        FtpEventPipeline.RecordEvent(EventSink, eventData);
        return result;
    }

    internal FtpAuthorizationResult AuthorizeFileSystemOperation(
        FtpPrincipal? principal,
        FtpAuthorizationOperation operation,
        FtpFileSystemRequest? fileRequest,
        FtpEntry? entry,
        bool controlProtected)
    {
        if (fileRequest == null)
        {
            return CreateDeniedResult("Authorization denied.");
        }
        FtpAuthorizationRequest request = new FtpAuthorizationRequest();
        request.Principal = principal == null ?
            new FtpPrincipal() : principal;
        request.Operation = operation;
        request.Path = fileRequest.Path;
        request.DestinationPath = fileRequest.DestinationPath;
        request.Entry = entry;
        request.ControlProtected = controlProtected;
        return AuthorizeOperation(request);
    }

    internal FtpAuthorizationResult AuthorizeSiteOperation(
        FtpPrincipal? principal,
        string? siteRequest,
        bool controlProtected)
    {
        FtpAuthorizationRequest request = new FtpAuthorizationRequest();
        request.Principal = principal == null ?
            new FtpPrincipal() : principal;
        request.Operation = FtpAuthorizationOperation.Site;
        request.Path = siteRequest == null ? "" : siteRequest;
        request.ControlProtected = controlProtected;
        return AuthorizeOperation(request);
    }

    internal FtpAuthorizationResult AuthorizeServerTransfer(
        FtpPrincipal? principal,
        string? sourcePath,
        string? targetPath,
        bool controlProtected)
    {
        FtpAuthorizationRequest request = new FtpAuthorizationRequest();
        request.Principal = principal == null ?
            new FtpPrincipal() : principal;
        request.Operation = FtpAuthorizationOperation.ServerToServer;
        request.Path = sourcePath == null ? "" : sourcePath;
        request.DestinationPath = targetPath;
        request.ControlProtected = controlProtected;
        return AuthorizeOperation(request);
    }

    internal bool AllowsAnonymousOperation(FtpAuthorizationRequest request)
    {
        if (!request.Principal.Anonymous || Host == null)
        {
            return true;
        }
        if (!Host.AllowAnonymous)
        {
            return false;
        }
        if (IsWriteOperation(request.Operation) && !Host.AllowAnonymousWrites)
        {
            return false;
        }
        return true;
    }

    internal static string? ValidateRequest(FtpAuthorizationRequest? request)
    {
        if (request == null || request.Principal == null ||
            !request.Principal.Authenticated ||
            string.IsNullOrWhiteSpace(request.Principal.Name) ||
            FtpConfigurationValidator.HasInvalidText(request.Principal.Name))
        {
            return "An authenticated principal is required.";
        }
        if (!IsKnownOperation(request.Operation))
        {
            return "The authorization operation is invalid.";
        }
        if (string.IsNullOrWhiteSpace(request.Path) ||
            FtpConfigurationValidator.HasInvalidText(request.Path))
        {
            return "A valid normalized authorization path is required.";
        }

        bool destinationRequired =
            request.Operation == FtpAuthorizationOperation.Rename ||
            request.Operation == FtpAuthorizationOperation.ServerToServer;
        if (destinationRequired &&
            (string.IsNullOrWhiteSpace(request.DestinationPath) ||
             FtpConfigurationValidator.HasInvalidOptionalText(
                 request.DestinationPath)))
        {
            return "A valid normalized destination path is required.";
        }
        if (!destinationRequired && request.DestinationPath != null)
        {
            return "This authorization operation does not accept a destination path.";
        }
        return null;
    }

    internal static bool IsKnownOperation(FtpAuthorizationOperation operation)
    {
        switch (operation)
        {
            case FtpAuthorizationOperation.List:
            case FtpAuthorizationOperation.Read:
            case FtpAuthorizationOperation.Write:
            case FtpAuthorizationOperation.Append:
            case FtpAuthorizationOperation.CreateDirectory:
            case FtpAuthorizationOperation.DeleteFile:
            case FtpAuthorizationOperation.DeleteDirectory:
            case FtpAuthorizationOperation.Rename:
            case FtpAuthorizationOperation.ReadMetadata:
            case FtpAuthorizationOperation.WriteMetadata:
            case FtpAuthorizationOperation.Site:
            case FtpAuthorizationOperation.ServerToServer:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsWriteOperation(FtpAuthorizationOperation operation)
    {
        switch (operation)
        {
            case FtpAuthorizationOperation.Write:
            case FtpAuthorizationOperation.Append:
            case FtpAuthorizationOperation.CreateDirectory:
            case FtpAuthorizationOperation.DeleteFile:
            case FtpAuthorizationOperation.DeleteDirectory:
            case FtpAuthorizationOperation.Rename:
            case FtpAuthorizationOperation.WriteMetadata:
            case FtpAuthorizationOperation.Site:
            case FtpAuthorizationOperation.ServerToServer:
                return true;
            default:
                return false;
        }
    }

    internal static bool IsSafeReason(string? reason)
    {
        return !string.IsNullOrWhiteSpace(reason) &&
            !FtpConfigurationValidator.HasInvalidText(reason);
    }

    internal static FtpAuthorizationResult CreateAllowedResult()
    {
        FtpAuthorizationResult result = new FtpAuthorizationResult();
        result.Handled = true;
        result.Allowed = true;
        return result;
    }

    internal static FtpAuthorizationResult CreateDeniedResult(string reason)
    {
        FtpAuthorizationResult result = new FtpAuthorizationResult();
        result.Handled = true;
        result.Allowed = false;
        result.Reason = reason;
        return result;
    }
}
