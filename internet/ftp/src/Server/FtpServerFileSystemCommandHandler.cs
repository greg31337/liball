using System;

namespace ftp;

internal static class FtpServerFileSystemCommandHandler
{
    internal static FtpCommandResult HandleCommand(
        FtpServerCommandContext context)
    {
        FtpSequenceValidationResult sequence =
            FtpSequenceValidator.InspectCommand(
                context.Command, context.State.State);
        if (!sequence.Allowed)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                sequence.ReplyCode,
                sequence.Message,
                sequence.ReplyCode == FtpReplyCodes.ServiceUnavailable);
        }
        if (IsHistoricAlias(context.Command.Kind) &&
            !context.Session.Settings.EnableHistoricAliases)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "Historic FTP command aliases are disabled.",
                false);
        }

        switch (context.Command.Kind)
        {
            case FtpCommandKind.Cwd:
            case FtpCommandKind.Xcwd:
                return HandleChangeDirectory(context, false);
            case FtpCommandKind.Cdup:
            case FtpCommandKind.Xcup:
                return HandleChangeDirectory(context, true);
            case FtpCommandKind.Smnt:
                return HandleMount(context);
            case FtpCommandKind.Pwd:
            case FtpCommandKind.Xpwd:
                return HandlePrintDirectory(context);
            case FtpCommandKind.Mkd:
            case FtpCommandKind.Xmkd:
                return HandleCreateDirectory(context);
            case FtpCommandKind.Rmd:
            case FtpCommandKind.Xrmd:
                return HandleDeleteDirectory(context);
            case FtpCommandKind.Rmda:
                return HandleRecursiveDeleteDirectory(context);
            case FtpCommandKind.Dele:
                return HandleDeleteFile(context);
            case FtpCommandKind.Rnfr:
                return HandleRenameFrom(context);
            case FtpCommandKind.Rnto:
                return HandleRenameTo(context);
            default:
                return FtpServerFileTransferCommandHandler.HandleCommand(context);
        }
    }

    internal static bool IsHistoricAlias(FtpCommandKind kind)
    {
        return kind == FtpCommandKind.Xcup ||
            kind == FtpCommandKind.Xcwd ||
            kind == FtpCommandKind.Xmkd ||
            kind == FtpCommandKind.Xpwd ||
            kind == FtpCommandKind.Xrmd;
    }

    internal static FtpCommandResult HandleChangeDirectory(
        FtpServerCommandContext context,
        bool parent)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        string argument = parent ? ".." :
            context.Command.Argument == null ? "" : context.Command.Argument;
        FtpFileSystemRequest request = CreateRequest(
            context,
            FtpPathCodec.ResolveWorkingPath(
                context.State.State.CurrentDirectory, argument),
            null);
        if (!Authorize(
            context, FtpAuthorizationOperation.ReadMetadata, request, null))
        {
            return CreateActionFailureResult(context);
        }

        FtpFileSystemResult result = InvokeFileSystem(context, () =>
        {
            return context.FileSystem.ResolveEntry(request);
        });
        if (!result.Succeeded || !IsDirectoryEntry(result.Entry))
        {
            return CreateActionFailureResult(context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            parent ? FtpReplyCodes.CommandOkay :
                FtpReplyCodes.RequestedFileActionOkay,
            parent ? "Directory changed to parent." :
                "Directory successfully changed.",
            false);
    }

    internal static FtpCommandResult HandleMount(
        FtpServerCommandContext context)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        FtpFileSystemRequest request = CreatePathRequest(context);
        request.Structure = context.State.State.FileStructure;
        FtpFileSystemResult result = InvokeAuthorized(
            context,
            FtpAuthorizationOperation.ReadMetadata,
            request,
            null,
            () =>
            {
                return context.FileSystem.MountStructure(request);
            });
        if (!result.Succeeded)
        {
            return CreateActionFailureResult(context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.RequestedFileActionOkay,
            "Structure mount accepted.",
            false);
    }

    internal static FtpCommandResult HandlePrintDirectory(
        FtpServerCommandContext context)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        string path = context.State.State.CurrentDirectory;
        FtpFileSystemRequest request = CreateRequest(context, path, null);
        if (!Authorize(
            context, FtpAuthorizationOperation.ReadMetadata, request, null))
        {
            return CreateActionFailureResult(context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.PathCreated,
            FtpPathCodec.FormatQuotedPathname(path) +
                " is the current directory.",
            false);
    }

    internal static FtpCommandResult HandleCreateDirectory(
        FtpServerCommandContext context)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        FtpFileSystemRequest request = CreatePathRequest(context);
        ApplyDirectoryCreationMode(context, request);
        FtpFileSystemResult result = InvokeAuthorized(
            context,
            FtpAuthorizationOperation.CreateDirectory,
            request,
            null,
            () =>
            {
                return context.FileSystem.CreateDirectoryEntry(request);
            });
        if (!result.Succeeded)
        {
            return CreateActionFailureResult(context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.PathCreated,
            FtpPathCodec.FormatQuotedPathname(request.Path) +
                " directory created.",
            false);
    }

    internal static FtpCommandResult HandleDeleteDirectory(
        FtpServerCommandContext context)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        FtpFileSystemRequest request = CreatePathRequest(context);
        FtpFileSystemResult result = InvokeAuthorized(
            context,
            FtpAuthorizationOperation.DeleteDirectory,
            request,
            null,
            () =>
            {
                return context.FileSystem.DeleteDirectoryEntry(request);
            });
        return CreateMutationResult(context, result, "Directory removed.");
    }

    internal static FtpCommandResult HandleRecursiveDeleteDirectory(
        FtpServerCommandContext context)
    {
        if (!context.Session.Settings.EnableCompatibilityExtensions)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "Recursive directory compatibility is disabled.",
                false);
        }
        FtpFileSystemRequest request = CreatePathRequest(context);
        request.Recursive = true;
        return RemoveDirectoryTree(context, request);
    }

    internal static FtpCommandResult RemoveDirectoryTree(
        FtpServerCommandContext context,
        FtpFileSystemRequest request)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        if (request.Path == "/")
        {
            return CreateActionFailureResult(context);
        }
        FtpFileSystemResult result = InvokeAuthorized(
            context,
            FtpAuthorizationOperation.DeleteDirectory,
            request,
            null,
            () =>
            {
                return context.FileSystem.DeleteDirectoryEntry(request);
            });
        return CreateMutationResult(
            context, result, "Directory tree removed.");
    }

    internal static FtpCommandResult HandleDeleteFile(
        FtpServerCommandContext context)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        FtpFileSystemRequest request = CreatePathRequest(context);
        FtpFileSystemResult result = InvokeAuthorized(
            context,
            FtpAuthorizationOperation.DeleteFile,
            request,
            null,
            () =>
            {
                return context.FileSystem.DeleteFileEntry(request);
            });
        return CreateMutationResult(context, result, "File deleted.");
    }

    internal static FtpCommandResult HandleRenameFrom(
        FtpServerCommandContext context)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        FtpFileSystemRequest request = CreatePathRequest(context);
        if (!Authorize(
            context, FtpAuthorizationOperation.ReadMetadata, request, null))
        {
            return CreateActionFailureResult(context);
        }
        FtpFileSystemResult result = InvokeFileSystem(context, () =>
        {
            return context.FileSystem.ResolveEntry(request);
        });
        if (!result.Succeeded || result.Entry == null)
        {
            return CreateActionFailureResult(context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.RequestedFileActionPending,
            "File exists; ready for destination name.",
            false);
    }

    internal static FtpCommandResult HandleRenameTo(
        FtpServerCommandContext context)
    {
        if (context.FileSystem == null)
        {
            return CreateUnavailableResult(context);
        }
        string sourceArgument = context.State.State.RenameSource == null ?
            "" : context.State.State.RenameSource;
        string destinationArgument = context.Command.Argument == null ?
            "" : context.Command.Argument;
        string sourcePath = FtpPathCodec.ResolveWorkingPath(
            context.State.State.CurrentDirectory, sourceArgument);
        string destinationPath = FtpPathCodec.ResolveWorkingPath(
            context.State.State.CurrentDirectory, destinationArgument);
        FtpFileSystemRequest request = CreateRequest(
            context, sourcePath, destinationPath);
        FtpFileSystemResult result = InvokeAuthorized(
            context,
            FtpAuthorizationOperation.Rename,
            request,
            null,
            () =>
            {
                return context.FileSystem.RenameEntry(request);
            });
        return CreateMutationResult(context, result, "Rename successful.");
    }

    internal static FtpCommandResult CreateMutationResult(
        FtpServerCommandContext context,
        FtpFileSystemResult result,
        string successText)
    {
        if (!result.Succeeded)
        {
            return CreateActionFailureResult(context);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.RequestedFileActionOkay,
            successText,
            false);
    }

    internal static FtpFileSystemRequest CreatePathRequest(
        FtpServerCommandContext context)
    {
        string argument = context.Command.Argument == null ?
            "" : context.Command.Argument;
        string path = FtpPathCodec.ResolveWorkingPath(
            context.State.State.CurrentDirectory, argument);
        return CreateRequest(context, path, null);
    }

    internal static FtpFileSystemRequest CreateRequest(
        FtpServerCommandContext context,
        string path,
        string? destinationPath)
    {
        FtpFileSystemRequest request = new FtpFileSystemRequest();
        FtpPrincipal? principal = context.State.State.Principal;
        if (principal != null)
        {
            request.Principal = principal;
        }
        request.Path = path;
        request.DestinationPath = destinationPath;
        request.Structure = context.State.State.FileStructure;
        return request;
    }

    internal static void ApplyDirectoryCreationMode(
        FtpServerCommandContext context,
        FtpFileSystemRequest request)
    {
        if (!context.Session.Settings.EnableCompatibilityExtensions)
        {
            return;
        }
        request.HasUnixMode = true;
        request.UnixMode = 511 & ~context.Session.OptionState.UnixUmask;
    }

    internal static bool Authorize(
        FtpServerCommandContext context,
        FtpAuthorizationOperation operation,
        FtpFileSystemRequest request,
        FtpEntry? entry)
    {
        FtpAuthorizationResult result =
            context.AuthorizationGate.AuthorizeFileSystemOperation(
                context.State.State.Principal,
                operation,
                request,
                entry,
                context.State.State.ControlProtected);
        return result.Allowed;
    }

    internal static FtpFileSystemResult InvokeAuthorized(
        FtpServerCommandContext context,
        FtpAuthorizationOperation operation,
        FtpFileSystemRequest request,
        FtpEntry? entry,
        Func<FtpFileSystemResult> fileOperation)
    {
        if (!Authorize(context, operation, request, entry))
        {
            return CreateFileSystemFailure();
        }
        return InvokeFileSystem(context, fileOperation);
    }

    internal static FtpFileSystemResult InvokeFileSystem(
        FtpServerCommandContext context,
        Func<FtpFileSystemResult> fileOperation)
    {
        FtpFaultIsolationResult invocation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return fileOperation();
            });
        FtpFileSystemResult? result =
            invocation.Value as FtpFileSystemResult;
        if (!invocation.Succeeded || result == null)
        {
            if (!invocation.Succeeded)
            {
                context.Session.RecordFaultEvent("file-system provider");
            }
            return CreateFileSystemFailure();
        }
        return result;
    }

    internal static bool IsDirectoryEntry(FtpEntry? entry)
    {
        if (entry == null)
        {
            return false;
        }
        return entry.Type == FtpEntryType.Directory ||
            entry.Type == FtpEntryType.CurrentDirectory ||
            entry.Type == FtpEntryType.ParentDirectory ||
            entry.Type == FtpEntryType.SymbolicLink;
    }

    internal static FtpFileSystemResult CreateFileSystemFailure()
    {
        return new FtpFileSystemResult();
    }

    internal static FtpCommandResult CreateUnavailableResult(
        FtpServerCommandContext context)
    {
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandNotImplemented,
            "The FTP file system is unavailable.",
            false);
    }

    internal static FtpCommandResult CreateActionFailureResult(
        FtpServerCommandContext context)
    {
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.RequestedActionNotTaken,
            "The requested file action was not taken.",
            false);
    }
}
