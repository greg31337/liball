using System;
using System.Text;

namespace ftp;

internal class FtpExtensionDispatcher
{
    internal IFtpExtensionHandler[] Handlers;

    internal FtpExtensionDispatcher(IFtpExtensionHandler[] handlers)
    {
        Handlers = handlers;
    }

    internal FtpExtensionResult DispatchExtension(
        FtpExtensionContext context)
    {
        int index = 0;
        while (index < Handlers.Length)
        {
            IFtpExtensionHandler handler = Handlers[index];
            FtpFaultIsolationResult invocation =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return handler.HandleExtensionCommand(
                        CopyContext(context));
                });
            FtpExtensionResult? result =
                invocation.Value as FtpExtensionResult;
            if (!invocation.Succeeded || result == null)
            {
                return CreateFailureResult();
            }

            FtpFaultIsolationResult validation =
                FtpFaultIsolationRunner.RunOperation(() =>
                {
                    return CopyValidatedResult(result);
                });
            FtpExtensionResult? safeResult =
                validation.Value as FtpExtensionResult;
            if (!validation.Succeeded || safeResult == null)
            {
                return CreateFailureResult();
            }
            if (safeResult.Disposition !=
                FtpExtensionDisposition.NotHandled)
            {
                return safeResult;
            }
            index++;
        }
        return new FtpExtensionResult();
    }

    internal static bool HandlesCommand(
        FtpCommand command,
        FtpSequenceState state)
    {
        if (command.Kind == FtpCommandKind.Unknown ||
            command.Kind == FtpCommandKind.Algs ||
            command.Kind == FtpCommandKind.Site ||
            command.Kind == FtpCommandKind.Opts ||
            command.Kind == FtpCommandKind.Adat ||
            command.Kind == FtpCommandKind.Mic ||
            command.Kind == FtpCommandKind.Conf ||
            command.Kind == FtpCommandKind.Enc)
        {
            return true;
        }
        if (command.Kind == FtpCommandKind.Auth)
        {
            return !IsTlsMechanism(command.Argument);
        }
        if (command.Kind == FtpCommandKind.Pbsz ||
            command.Kind == FtpCommandKind.Prot ||
            command.Kind == FtpCommandKind.Ccc)
        {
            return state.SecurityExchangeComplete &&
                !state.SecurityExchangeIsTls;
        }
        return IsCompatibilityCommand(command);
    }

    internal static bool IsCompatibilityCommand(FtpCommand command)
    {
        switch (command.Kind)
        {
            case FtpCommandKind.Xcup:
            case FtpCommandKind.Xcwd:
            case FtpCommandKind.Xmkd:
            case FtpCommandKind.Xpwd:
            case FtpCommandKind.Xrmd:
            case FtpCommandKind.Pret:
            case FtpCommandKind.Clnt:
            case FtpCommandKind.Mfmt:
            case FtpCommandKind.Hash:
            case FtpCommandKind.Md5:
            case FtpCommandKind.Xmd5:
            case FtpCommandKind.Xsha1:
            case FtpCommandKind.Xsha256:
            case FtpCommandKind.Xsha512:
            case FtpCommandKind.Xcrc:
            case FtpCommandKind.Rmda:
                return true;
            case FtpCommandKind.Mode:
                return string.Equals(
                    command.Argument, "Z", StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }

    internal static FtpExtensionContext CreateContext(
        FtpCommand command,
        FtpSequenceState state,
        FtpFeatureSet features)
    {
        FtpExtensionContext context = new FtpExtensionContext();
        context.Command = CopyCommand(command);
        context.Principal = FtpEventPipeline.CopyPrincipal(state.Principal);
        context.SessionState = state.SessionState;
        context.CurrentDirectory = state.CurrentDirectory;
        context.ControlProtected = state.ControlProtected;
        context.TransferInProgress = state.TransferInProgress;
        context.Features = FtpServerOptionsSnapshot.CopyFeatureSet(features);
        return context;
    }

    internal static FtpExtensionContext CopyContext(
        FtpExtensionContext source)
    {
        FtpExtensionContext copy = new FtpExtensionContext();
        copy.Command = CopyCommand(source.Command);
        copy.Principal = FtpEventPipeline.CopyPrincipal(source.Principal);
        copy.SessionState = source.SessionState;
        copy.CurrentDirectory = source.CurrentDirectory;
        copy.ControlProtected = source.ControlProtected;
        copy.TransferInProgress = source.TransferInProgress;
        copy.Features = FtpServerOptionsSnapshot.CopyFeatureSet(
            source.Features);
        return copy;
    }

    internal static FtpExtensionResult CopyValidatedResult(
        FtpExtensionResult source)
    {
        bool knownDisposition =
            source.Disposition == FtpExtensionDisposition.NotHandled ||
            source.Disposition == FtpExtensionDisposition.Handled ||
            source.Disposition == FtpExtensionDisposition.Rejected;
        if (!knownDisposition || source.Replies == null)
        {
            throw new FtpProtocolException(
                "The extension handler returned an invalid result.", null);
        }
        if (source.Disposition == FtpExtensionDisposition.NotHandled &&
            (source.Replies.Length != 0 || source.CloseConnection))
        {
            throw new FtpProtocolException(
                "An unhandled extension cannot produce replies or close the connection.",
                null);
        }
        if (source.Disposition != FtpExtensionDisposition.NotHandled &&
            source.Replies.Length == 0)
        {
            throw new FtpProtocolException(
                "A handled extension must produce at least one reply.", null);
        }

        FtpExtensionResult copy = new FtpExtensionResult();
        copy.Disposition = source.Disposition;
        copy.Replies = CopyReplies(source.Replies);
        copy.CloseConnection = source.CloseConnection;
        copy.FailureMessage = IsSafeFailureMessage(source.FailureMessage) ?
            source.FailureMessage : null;
        return copy;
    }

    internal static FtpReply[] CopyReplies(FtpReply[] source)
    {
        FtpReplyWriter validator = new FtpReplyWriter(
            FtpDefaults.MaximumReplyBytes, FtpDefaults.MaximumReplyLines);
        FtpReply[] copy = new FtpReply[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            FtpReply reply = CopyReply(source[index]);
            validator.FormatEncodedReply(reply, Encoding.ASCII);
            copy[index] = reply;
            index++;
        }
        return copy;
    }

    internal static FtpReply CopyReply(FtpReply source)
    {
        if (source == null)
        {
            throw new FtpProtocolException(
                "An extension reply is required.", null);
        }
        FtpReply copy = new FtpReply();
        copy.Code = source.Code;
        copy.Text = source.Text;
        copy.Lines = CopyReplyLines(source.Lines);
        copy.Classification = CopyClassification(source.Classification);
        copy.Multiline = source.Multiline;
        copy.RawText = source.RawText;
        return copy;
    }

    internal static FtpReplyLine[] CopyReplyLines(FtpReplyLine[] source)
    {
        if (source == null)
        {
            throw new FtpProtocolException(
                "An extension reply line collection is required.", null);
        }
        FtpReplyLine[] copy = new FtpReplyLine[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            FtpReplyLine line = source[index];
            if (line == null)
            {
                throw new FtpProtocolException(
                    "An extension reply line is required.", null);
            }
            FtpReplyLine lineCopy = new FtpReplyLine();
            lineCopy.HasCode = line.HasCode;
            lineCopy.Code = line.Code;
            lineCopy.Text = line.Text;
            lineCopy.RawText = line.RawText;
            lineCopy.TerminatesMultiline = line.TerminatesMultiline;
            copy[index] = lineCopy;
            index++;
        }
        return copy;
    }

    internal static FtpReplyClassification CopyClassification(
        FtpReplyClassification source)
    {
        FtpReplyClassification copy = new FtpReplyClassification();
        if (source == null)
        {
            return copy;
        }
        copy.Category = source.Category;
        copy.Kind = source.Kind;
        copy.Malformed = source.Malformed;
        copy.Succeeded = source.Succeeded;
        copy.TransientFailure = source.TransientFailure;
        copy.PermanentFailure = source.PermanentFailure;
        copy.AuthenticationFailure = source.AuthenticationFailure;
        return copy;
    }

    internal static FtpCommand CopyCommand(FtpCommand source)
    {
        FtpCommand copy = new FtpCommand();
        copy.Kind = source.Kind;
        copy.Verb = source.Verb;
        copy.Argument = source.Argument;
        copy.RawText = source.RawText;
        copy.Sensitive = source.Sensitive;
        return copy;
    }

    internal static bool IsTlsMechanism(string? mechanism)
    {
        return string.Equals(
            mechanism, "TLS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                mechanism, "TLS-C", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsSafeFailureMessage(string? message)
    {
        return message == null ||
            !FtpConfigurationValidator.HasInvalidText(message);
    }

    internal static FtpExtensionResult CreateFailureResult()
    {
        FtpExtensionResult result = new FtpExtensionResult();
        result.Disposition = FtpExtensionDisposition.Rejected;
        result.Replies = new FtpReply[]
        {
            FtpServerSession.CreateReply(
                FtpReplyCodes.RequestedActionAbortedLocalError,
                "FTP extension handler failed.")
        };
        result.FailureMessage = "The FTP extension handler failed.";
        return result;
    }
}
