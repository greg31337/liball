using System;

namespace ftp;

internal static class FtpEventPipeline
{
    internal static bool RecordEvent(
        IFtpEventSink? eventSink,
        FtpEvent? eventData)
    {
        if (eventSink == null || eventData == null)
        {
            return false;
        }

        FtpEvent safeEvent = CreateRedactedSnapshot(eventData);
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                eventSink.RecordEvent(safeEvent);
                return null;
            });
        return operation.Succeeded;
    }

    internal static FtpEvent CreateAuthenticationEvent(
        FtpAuthenticationRequest? request,
        FtpAuthenticationResult? result)
    {
        FtpEvent eventData = CreateBaseEvent(FtpEventKind.Authentication);
        FtpAuthenticationStatus status = result == null ?
            FtpAuthenticationStatus.Failure : result.Status;
        eventData.HasAuthenticationStatus = true;
        eventData.AuthenticationStatus = status;
        if (result != null && result.Principal != null)
        {
            eventData.Principal = CopyPrincipal(result.Principal);
        }
        else if (request != null && request.Principal != null)
        {
            eventData.Principal = CopyPrincipal(request.Principal);
        }
        eventData.Message = status == FtpAuthenticationStatus.Success ?
            "FTP authentication succeeded." :
            status == FtpAuthenticationStatus.AdditionalInformationRequired ?
                "FTP authentication requires additional information." :
                status == FtpAuthenticationStatus.NotHandled ?
                    "FTP authentication was not handled." :
                    "FTP authentication failed.";
        return eventData;
    }

    internal static FtpEvent CreateAuthorizationEvent(
        FtpAuthorizationRequest? request,
        FtpAuthorizationResult? result)
    {
        FtpEvent eventData = CreateBaseEvent(FtpEventKind.Authorization);
        if (request != null)
        {
            if (FtpAuthorizationGate.IsKnownOperation(request.Operation))
            {
                eventData.HasAuthorizationOperation = true;
                eventData.AuthorizationOperation = request.Operation;
            }
            eventData.Path = request.Path;
            eventData.Principal = CopyPrincipal(request.Principal);
        }
        eventData.Message = result != null && result.Handled && result.Allowed ?
            "FTP authorization allowed the operation." :
            "FTP authorization denied the operation.";
        return eventData;
    }

    internal static FtpEvent CreateFileSystemEvent(
        FtpAuthorizationOperation operation,
        string? path,
        FtpEntry? entry,
        bool succeeded)
    {
        FtpEvent eventData = CreateBaseEvent(FtpEventKind.FileSystem);
        if (FtpAuthorizationGate.IsKnownOperation(operation))
        {
            eventData.HasAuthorizationOperation = true;
            eventData.AuthorizationOperation = operation;
        }
        eventData.Path = path;
        if (entry != null)
        {
            eventData.HasEntryType = true;
            eventData.EntryType = entry.Type;
        }
        eventData.Message = succeeded ?
            "FTP file-system operation succeeded." :
            "FTP file-system operation failed.";
        return eventData;
    }

    internal static FtpEvent CreateTransferEvent(
        FtpEventKind kind,
        FtpTransferProgress? progress)
    {
        FtpEvent eventData = CreateBaseEvent(kind);
        if (progress != null)
        {
            eventData.TransferProgress = CopyTransferProgress(progress);
            eventData.Path = progress.Path;
            eventData.HasTransferDirection = true;
            eventData.TransferDirection = progress.Direction;
        }
        eventData.Message = kind == FtpEventKind.TransferStarted ?
            "FTP transfer started." :
            kind == FtpEventKind.TransferProgress ?
                "FTP transfer progress was recorded." :
                kind == FtpEventKind.TransferCompleted ?
                    "FTP transfer completed." :
                    "FTP transfer activity was recorded.";
        return eventData;
    }

    internal static FtpEvent CreateTlsEvent(
        FtpTlsDiagnostics? diagnostics,
        bool succeeded)
    {
        FtpEvent eventData = CreateBaseEvent(FtpEventKind.Tls);
        if (diagnostics != null)
        {
            eventData.TlsDiagnostics =
                FtpTlsDiagnosticsFactory.CopyDiagnostics(diagnostics);
        }
        eventData.Message = succeeded ?
            "FTP TLS activity succeeded." : "FTP TLS activity failed.";
        return eventData;
    }

    internal static FtpEvent CreatePolicyEvent(string message)
    {
        FtpEvent eventData = CreateBaseEvent(FtpEventKind.PolicyRejection);
        eventData.Message = message;
        return eventData;
    }

    internal static FtpEvent CreateFaultEvent(string activity)
    {
        FtpEvent eventData = CreateBaseEvent(FtpEventKind.Fault);
        eventData.Message = FtpConfigurationValidator.HasInvalidText(activity) ?
            "An isolated FTP operation failed." :
            "The isolated FTP " + activity + " operation failed.";
        return eventData;
    }

    internal static FtpEvent CreateBaseEvent(FtpEventKind kind)
    {
        FtpEvent eventData = new FtpEvent();
        eventData.Kind = kind;
        eventData.Timestamp.HasValue = true;
        eventData.Timestamp.ValueUtc = DateTime.UtcNow;
        eventData.Redacted = true;
        return eventData;
    }

    internal static FtpEvent CreateRedactedSnapshot(FtpEvent source)
    {
        FtpEvent copy = new FtpEvent();
        CopyAvailabilityData(source, copy);
        copy.Kind = source.Kind;
        copy.Timestamp = CopyTimestamp(source.Timestamp);
        copy.Message = source.Message;
        copy.Path = source.Path;
        copy.RemoteEndpoint = CopyEndpoint(source.RemoteEndpoint);
        copy.Principal = CopyPrincipal(source.Principal);
        if (source.Command != null)
        {
            copy.Command = FtpTranscriptRedactor.RedactCommand(source.Command);
        }
        if (source.Reply != null)
        {
            FtpCommand command = source.Command == null ?
                new FtpCommand() : source.Command;
            copy.Reply = FtpTranscriptRedactor.RedactReply(
                command, source.Reply);
        }
        copy.TransferProgress = CopyTransferProgress(source.TransferProgress);
        if (source.TlsDiagnostics != null)
        {
            copy.TlsDiagnostics = FtpTlsDiagnosticsFactory.CopyDiagnostics(
                source.TlsDiagnostics);
        }
        if (copy.Kind == FtpEventKind.Command && copy.Command != null)
        {
            copy.Message = copy.Command.RawText;
        }
        if (copy.Kind == FtpEventKind.Reply && copy.Reply != null)
        {
            copy.Message = copy.Reply.RawText;
        }
        if (!copy.Timestamp.HasValue)
        {
            copy.Timestamp.HasValue = true;
            copy.Timestamp.ValueUtc = DateTime.UtcNow;
        }
        copy.Redacted = true;
        return copy;
    }

    internal static void CopyAvailabilityData(FtpEvent source, FtpEvent copy)
    {
        copy.HasAddressFamily = source.HasAddressFamily;
        copy.AddressFamily = source.AddressFamily;
        copy.HasAuthenticationStatus = source.HasAuthenticationStatus;
        copy.AuthenticationStatus = source.AuthenticationStatus;
        copy.HasAuthorizationOperation = source.HasAuthorizationOperation;
        copy.AuthorizationOperation = source.AuthorizationOperation;
        copy.HasCollisionPolicy = source.HasCollisionPolicy;
        copy.CollisionPolicy = source.CollisionPolicy;
        copy.HasCommandKind = source.HasCommandKind;
        copy.CommandKind = source.CommandKind;
        copy.HasDataConnectionMode = source.HasDataConnectionMode;
        copy.DataConnectionMode = source.DataConnectionMode;
        copy.HasDataProtectionLevel = source.HasDataProtectionLevel;
        copy.DataProtectionLevel = source.DataProtectionLevel;
        copy.HasDirectorySyncMode = source.HasDirectorySyncMode;
        copy.DirectorySyncMode = source.DirectorySyncMode;
        copy.HasEntryType = source.HasEntryType;
        copy.EntryType = source.EntryType;
        copy.HasExtensionDisposition = source.HasExtensionDisposition;
        copy.ExtensionDisposition = source.ExtensionDisposition;
        copy.HasFailurePolicy = source.HasFailurePolicy;
        copy.FailurePolicy = source.FailurePolicy;
        copy.HasFeatureSupport = source.HasFeatureSupport;
        copy.FeatureSupport = source.FeatureSupport;
        copy.HasFileOpenMode = source.HasFileOpenMode;
        copy.FileOpenMode = source.FileOpenMode;
        copy.HasFileStructure = source.HasFileStructure;
        copy.FileStructure = source.FileStructure;
        copy.HasPermission = source.HasPermission;
        copy.Permission = source.Permission;
        copy.HasReplyCategory = source.HasReplyCategory;
        copy.ReplyCategory = source.ReplyCategory;
        copy.HasReplyKind = source.HasReplyKind;
        copy.ReplyKind = source.ReplyKind;
        copy.HasResumePolicy = source.HasResumePolicy;
        copy.ResumePolicy = source.ResumePolicy;
        copy.HasSecurityMode = source.HasSecurityMode;
        copy.SecurityMode = source.SecurityMode;
        copy.HasServerState = source.HasServerState;
        copy.ServerState = source.ServerState;
        copy.HasSessionState = source.HasSessionState;
        copy.SessionState = source.SessionState;
        copy.HasSymlinkPolicy = source.HasSymlinkPolicy;
        copy.SymlinkPolicy = source.SymlinkPolicy;
        copy.HasTextFormat = source.HasTextFormat;
        copy.TextFormat = source.TextFormat;
        copy.HasTransferDirection = source.HasTransferDirection;
        copy.TransferDirection = source.TransferDirection;
        copy.HasTransferMode = source.HasTransferMode;
        copy.TransferMode = source.TransferMode;
        copy.HasTransferType = source.HasTransferType;
        copy.TransferType = source.TransferType;
        copy.HasVerificationMode = source.HasVerificationMode;
        copy.VerificationMode = source.VerificationMode;
    }

    internal static FtpPrincipal? CopyPrincipal(FtpPrincipal? source)
    {
        if (source == null)
        {
            return null;
        }
        FtpPrincipal copy = new FtpPrincipal();
        copy.Name = source.Name;
        copy.Authenticated = source.Authenticated;
        copy.Anonymous = source.Anonymous;
        copy.Account = null;
        copy.CertificateIdentity = null;
        copy.Roles = CopyRoles(source.Roles);
        copy.HomeDirectory = null;
        copy.ResourceLimits =
            FtpServerOptionsSnapshot.CopyResourceLimits(
                source.ResourceLimits);
        return copy;
    }

    internal static string[] CopyRoles(string[]? roles)
    {
        if (roles == null)
        {
            return new string[0];
        }
        string[] copy = new string[roles.Length];
        int index = 0;
        while (index < roles.Length)
        {
            copy[index] = roles[index];
            index++;
        }
        return copy;
    }

    internal static FtpEndpoint? CopyEndpoint(FtpEndpoint? source)
    {
        if (source == null)
        {
            return null;
        }
        FtpEndpoint copy = new FtpEndpoint();
        copy.Host = source.Host;
        copy.Port = source.Port;
        copy.AddressFamily = source.AddressFamily;
        return copy;
    }

    internal static FtpTransferProgress? CopyTransferProgress(
        FtpTransferProgress? source)
    {
        if (source == null)
        {
            return null;
        }
        FtpTransferProgress copy = new FtpTransferProgress();
        copy.Direction = source.Direction;
        copy.Path = source.Path;
        copy.HasExpectedBytes = source.HasExpectedBytes;
        copy.ExpectedBytes = source.ExpectedBytes;
        copy.LogicalBytes = source.LogicalBytes;
        copy.WireBytes = source.WireBytes;
        copy.Elapsed = source.Elapsed;
        copy.Completed = source.Completed;
        return copy;
    }

    internal static FtpTimestamp CopyTimestamp(FtpTimestamp? source)
    {
        FtpTimestamp copy = new FtpTimestamp();
        if (source == null)
        {
            return copy;
        }
        copy.HasValue = source.HasValue;
        copy.ValueUtc = source.ValueUtc;
        copy.FractionalSeconds = source.FractionalSeconds;
        copy.RawValue = source.RawValue;
        copy.LeapSecond = source.LeapSecond;
        return copy;
    }
}
