using System.Net;

namespace ftp;

internal static class FtpServerTransferCommandHandler
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

        switch (context.Command.Kind)
        {
            case FtpCommandKind.Pret:
                return HandlePret(context);
            case FtpCommandKind.Type:
                return HandleRepresentation(context);
            case FtpCommandKind.Stru:
                return HandleStructure(context);
            case FtpCommandKind.Mode:
                return HandleMode(context);
            case FtpCommandKind.Allo:
                return HandleAllocation(context);
            case FtpCommandKind.Rest:
                return HandleRestart(context);
            case FtpCommandKind.Port:
            case FtpCommandKind.Eprt:
                return HandleActiveSetup(context);
            case FtpCommandKind.Pasv:
            case FtpCommandKind.Epsv:
                return HandlePassiveSetup(context);
            case FtpCommandKind.Abor:
                return HandleAbort(context);
            default:
                return FtpServerFileSystemCommandHandler.HandleCommand(context);
        }
    }

    internal static FtpCommandResult HandlePret(
        FtpServerCommandContext context)
    {
        if (!context.Session.Settings.EnablePret)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "PRET compatibility is disabled.",
                false);
        }
        if (context.Session.HasDataConnectionSetup() ||
            context.Session.HasPendingTransfer())
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.BadCommandSequence,
                "PRET must precede data-connection setup.",
                false);
        }
        FtpFaultIsolationResult parsing =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FtpPretExtension.ParseIntent(
                    context.Command.Argument,
                    context.State.State);
            });
        FtpCommand? intent = parsing.Value as FtpCommand;
        if (!parsing.Succeeded || intent == null)
        {
            context.Session.ClearPretIntent();
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestedActionNotTaken,
                "The PRET transfer intent is invalid.",
                false);
        }
        context.Session.SetPretIntent(intent);
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            "Ready to proceed with the declared transfer.",
            false);
    }

    internal static FtpCommandResult HandleRepresentation(
        FtpServerCommandContext context)
    {
        FtpFaultIsolationResult validation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpTransferOptions representation =
                    FtpTransferParameterCodec.ParseRepresentation(
                        context.Command.Argument);
                FtpTransferParameterCodec.ValidateRepresentationForState(
                    representation, context.State.State);
                return null;
            });
        return CreateParameterResult(
            context, validation, "Transfer representation accepted.");
    }

    internal static FtpCommandResult HandleStructure(
        FtpServerCommandContext context)
    {
        FtpFaultIsolationResult validation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpFileStructure structure =
                    FtpTransferParameterCodec.ParseStructure(
                        context.Command.Argument);
                FtpTransferParameterCodec.ValidateStructureForState(
                    structure, context.State.State);
                return null;
            });
        return CreateParameterResult(
            context, validation, "File structure accepted.");
    }

    internal static FtpCommandResult HandleMode(
        FtpServerCommandContext context)
    {
        if (string.Equals(
            context.Command.Argument,
            "Z",
            System.StringComparison.OrdinalIgnoreCase) &&
            (!context.Session.Settings.EnableModeZ ||
             context.FileSystem == null))
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "MODE Z compatibility is disabled.",
                false);
        }
        FtpFaultIsolationResult validation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpTransferMode mode = FtpTransferParameterCodec.ParseMode(
                    context.Command.Argument);
                FtpTransferParameterCodec.ValidateModeForState(
                    mode, context.State.State);
                return null;
            });
        return CreateParameterResult(
            context, validation, "Transfer mode accepted.");
    }

    internal static FtpCommandResult CreateParameterResult(
        FtpServerCommandContext context,
        FtpFaultIsolationResult validation,
        string successText)
    {
        if (!validation.Succeeded)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandParameterNotImplemented,
                "The requested transfer parameter combination is not supported.",
                false);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            successText,
            false);
    }

    internal static FtpCommandResult HandleAllocation(
        FtpServerCommandContext context)
    {
        FtpFaultIsolationResult parsing =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                return FtpArgumentCodec.ParseAllocateArgument(
                    context.Command.Argument);
            });
        FtpAllocationArgument? allocation =
            parsing.Value as FtpAllocationArgument;
        if (!parsing.Succeeded || allocation == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.SyntaxErrorInParameters,
                "The ALLO argument is invalid.",
                false);
        }
        context.Session.PendingAllocation = allocation;
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.CommandOkay,
            "Allocation request accepted.",
            false);
    }

    internal static FtpCommandResult HandleRestart(
        FtpServerCommandContext context)
    {
        FtpFaultIsolationResult validation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                FtpArgumentCodec.ValidateRestartMarker(
                    context.Command.Argument);
                return null;
            });
        if (!validation.Succeeded)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.SyntaxErrorInParameters,
                "The REST marker is invalid.",
                false);
        }
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.RequestedFileActionPending,
            "Restart marker accepted.",
            false);
    }

    internal static FtpCommandResult HandleActiveSetup(
        FtpServerCommandContext context)
    {
        if (context.Session.HasPendingTransfer())
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestedFileActionNotTaken,
                "A data transfer is still in progress.",
                false);
        }
        if (RequiresPretDataSetup(context))
        {
            return CreatePretRequiredResult(context);
        }
        FtpServerActiveConnector? connector = context.Session.ActiveConnector;
        if (connector == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "Active data connections are disabled.",
                false);
        }
        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                context.Session.ClosePassiveSetup();
                if (context.Command.Kind == FtpCommandKind.Port)
                {
                    return connector.ConfigurePort(context.Command.Argument);
                }
                return connector.ConfigureExtendedPort(
                    context.Command.Argument);
            });
        return CreateDataSetupResult(context, operation);
    }

    internal static FtpCommandResult HandlePassiveSetup(
        FtpServerCommandContext context)
    {
        if (context.Session.HasPendingTransfer())
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestedFileActionNotTaken,
                "A data transfer is still in progress.",
                false);
        }
        if (!FtpPretExtension.IsExtendedPassiveAll(context.Command) &&
            RequiresPretDataSetup(context))
        {
            return CreatePretRequiredResult(context);
        }
        FtpPassiveListener? listener = context.Session.PassiveListener;
        if (listener == null)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "Passive data connections are disabled.",
                false);
        }

        FtpFaultIsolationResult operation =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                context.Session.ClearActiveSetup();
                if (context.Command.Kind == FtpCommandKind.Pasv)
                {
                    return context.Session.OpenPassiveSetup(
                        false, FtpAddressFamily.IPv4);
                }

                FtpExtendedPassiveRequest request =
                    FtpEndpointCodec.ParseExtendedPassiveArgument(
                        context.Command.Argument);
                if (request.All)
                {
                    context.Session.EnableExtendedPassiveOnly();
                    return FtpServerSession.CreateReply(
                        FtpReplyCodes.CommandOkay,
                        "EPSV ALL accepted.");
                }
                if (!request.Supported)
                {
                    IPEndPoint? local =
                        context.Connection.Socket.LocalEndPoint as IPEndPoint;
                    FtpAddressFamily family = local == null ?
                        FtpAddressFamily.IPv4 :
                        FtpPeerPolicy.GetAddressFamily(local.Address);
                    int protocol = FtpEndpointCodec.FormatNetworkProtocol(
                        family, "EPSV");
                    return FtpEndpointCodec.CreateNetworkProtocolReply(
                        new int[] { protocol });
                }
                return context.Session.OpenPassiveSetup(
                    true, request.AddressFamily);
            });
        return CreateDataSetupResult(context, operation);
    }

    internal static FtpCommandResult CreateDataSetupResult(
        FtpServerCommandContext context,
        FtpFaultIsolationResult operation)
    {
        FtpReply? reply = operation.Value as FtpReply;
        if (!operation.Succeeded || reply == null)
        {
            FtpConnectionException? connectionFailure =
                operation.Failure as FtpConnectionException;
            int replyCode = connectionFailure != null ?
                FtpReplyCodes.CannotOpenDataConnection :
                FtpReplyCodes.SyntaxErrorInParameters;
            string message = replyCode ==
                FtpReplyCodes.CannotOpenDataConnection ?
                    "The data connection could not be opened." :
                    "The data-connection setup request is invalid or prohibited.";
            return FtpServerSession.CreateCommandResult(
                context.Command,
                replyCode,
                message,
                false);
        }
        FtpCommandResult result = new FtpCommandResult();
        result.Command = context.Command;
        result.FinalReply = reply;
        result.Succeeded = reply.Code >= 200 && reply.Code < 300;
        if (result.Succeeded &&
            context.Session.Settings.EnablePret &&
            !FtpPretExtension.IsExtendedPassiveAll(context.Command))
        {
            context.Session.AcceptPretDataSetup();
        }
        return result;
    }

    internal static bool RequiresPretDataSetup(
        FtpServerCommandContext context)
    {
        return context.Session.Settings.EnablePret &&
            !context.Session.HasPretIntentForSetup();
    }

    internal static FtpCommandResult CreatePretRequiredResult(
        FtpServerCommandContext context)
    {
        return FtpServerSession.CreateCommandResult(
            context.Command,
            FtpReplyCodes.BadCommandSequence,
            "PRET must declare the transfer before data-connection setup.",
            false);
    }

    internal static FtpCommandResult HandleAbort(
        FtpServerCommandContext context)
    {
        bool transferring = context.State.State.TransferInProgress ||
            context.Connection.TransferActive ||
            context.Session.HasPendingTransfer();
        FtpFaultIsolationResult cleanup =
            FtpFaultIsolationRunner.RunOperation(() =>
            {
                context.Session.AbortPendingTransfer();
                context.Session.ResetDataConnections();
                FtpFaultIsolationResult resources =
                    context.Connection.AbortTransferResources();
                if (!resources.Succeeded)
                {
                    throw new FtpConnectionException(
                        "The FTP data transfer could not be aborted.",
                        resources.Failure);
                }
                return null;
            });
        if (!cleanup.Succeeded)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.RequestedActionAbortedLocalError,
                "The FTP data transfer could not be aborted.",
                false);
        }
        if (!transferring)
        {
            return FtpServerSession.CreateCommandResult(
                context.Command,
                FtpReplyCodes.DataConnectionOpenNoTransfer,
                "No transfer is in progress.",
                false);
        }

        FtpCommandResult result = new FtpCommandResult();
        result.Command = context.Command;
        result.Replies = new FtpReply[]
        {
            FtpServerSession.CreateReply(
                FtpReplyCodes.ConnectionClosedTransferAborted,
                "Data connection closed; transfer aborted."),
            FtpServerSession.CreateReply(
                FtpReplyCodes.ClosingDataConnection,
                "Abort command successful.")
        };
        result.FinalReply = result.Replies[1];
        result.Succeeded = true;
        return result;
    }
}
