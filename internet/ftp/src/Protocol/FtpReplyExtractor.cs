using System;

namespace ftp;

internal static class FtpReplyExtractor
{
    internal static FtpGreetingReply ExtractGreeting(FtpReply? reply)
    {
        FtpReply checkedReply = RequireReply(reply, "greeting");
        if (checkedReply.Code != FtpReplyCodes.ServiceReadySoon && checkedReply.Code != FtpReplyCodes.ServiceReady)
        {
            throw new FtpProtocolException("An FTP greeting must use reply code 120 or 220.", null);
        }

        FtpGreetingReply greeting = new FtpGreetingReply();
        greeting.Reply = checkedReply;
        greeting.Ready = checkedReply.Code == FtpReplyCodes.ServiceReady;
        greeting.Delayed = checkedReply.Code == FtpReplyCodes.ServiceReadySoon;
        greeting.Message = checkedReply.Text;
        return greeting;
    }

    internal static long ExtractSize(FtpReply? reply)
    {
        FtpReply checkedReply = RequireExpectedCode(reply, FtpReplyCodes.FileStatus, "SIZE");
        return FtpSizeCodec.ParseSizeValue(checkedReply.Text).Value;
    }

    internal static FtpTimestamp ExtractModificationTime(FtpReply? reply)
    {
        FtpReply checkedReply = RequireExpectedCode(reply, FtpReplyCodes.FileStatus, "MDTM");
        return FtpTimestampCodec.ParseTimestampValue(checkedReply.Text);
    }

    internal static string ExtractSystemType(FtpReply? reply)
    {
        FtpReply checkedReply = RequireExpectedCode(reply, FtpReplyCodes.SystemType, "SYST");
        return checkedReply.Text;
    }

    internal static FtpPassiveReply ExtractPassiveEndpoint(FtpReply? reply)
    {
        return FtpEndpointCodec.ParsePassiveReply(reply);
    }

    internal static FtpPassiveReply ExtractExtendedPassiveEndpoint(FtpReply? reply)
    {
        return FtpEndpointCodec.ParseExtendedPassiveReply(reply);
    }

    internal static string ExtractCreatedPath(FtpReply? reply)
    {
        FtpReply checkedReply = RequireExpectedCode(reply, FtpReplyCodes.PathCreated, "PWD or MKD");
        return FtpPathCodec.ParseQuotedPathname(checkedReply.Text);
    }

    internal static FtpPendingReply ExtractRestartReadiness(FtpReply? reply)
    {
        return ExtractPendingAction(reply, "REST");
    }

    internal static FtpPendingReply ExtractRenameReadiness(FtpReply? reply)
    {
        return ExtractPendingAction(reply, "RNFR");
    }

    internal static string ExtractRestartMarker(FtpReply? reply)
    {
        FtpReply checkedReply = RequireExpectedCode(reply, FtpReplyCodes.RestartMarker, "restart marker");
        return checkedReply.Text;
    }

    internal static FtpNetworkProtocolReply ExtractNetworkProtocolRejection(FtpReply? reply)
    {
        return FtpEndpointCodec.ParseNetworkProtocolReply(reply);
    }

    internal static FtpCommandResult ExtractTransferCompletion(
        FtpCommand? command, FtpReply[]? replies)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        if (replies == null)
        {
            throw new ArgumentNullException("replies");
        }
        if (replies.Length == 0)
        {
            throw new FtpProtocolException("An FTP transfer completion sequence requires a reply.", null);
        }

        FtpReply? preliminary = null;
        FtpReply finalReply;
        FtpReply first = RequireSequenceReply(replies[0]);
        bool hasPreliminary = first.Code == FtpReplyCodes.DataConnectionAlreadyOpen ||
            first.Code == FtpReplyCodes.FileStatusOkayOpeningDataConnection;
        if (hasPreliminary)
        {
            if (replies.Length != 2)
            {
                throw new FtpProtocolException("An FTP preliminary transfer reply must be followed by one terminal reply.", null);
            }

            preliminary = first;
            finalReply = RequireSequenceReply(replies[1]);
        }
        else
        {
            if (replies.Length != 1)
            {
                throw new FtpProtocolException("An FTP transfer completion sequence contains an unexpected extra reply.", null);
            }

            finalReply = first;
        }

        FtpReplyClassification classification = FtpReplyClassifier.ClassifyCode(finalReply.Code);
        bool successful = finalReply.Code == FtpReplyCodes.ClosingDataConnection ||
            finalReply.Code == FtpReplyCodes.RequestedFileActionOkay;
        bool failed = classification.TransientFailure || classification.PermanentFailure;
        if (!successful && !failed)
        {
            throw new FtpProtocolException("An FTP transfer sequence did not end with a valid completion reply.", null);
        }

        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.Replies = replies;
        result.PreliminaryReply = preliminary;
        result.FinalReply = finalReply;
        result.Succeeded = successful;
        result.ConnectionClosed = finalReply.Code == FtpReplyCodes.ServiceUnavailable;
        return result;
    }

    internal static FtpReply RequireReply(FtpReply? reply, string replyName)
    {
        if (reply == null)
        {
            throw new ArgumentNullException("reply");
        }
        if (reply.Text == null)
        {
            throw new FtpProtocolException($"The FTP {replyName} reply text is unavailable.", null);
        }
        return reply;
    }

    internal static FtpReply RequireExpectedCode(FtpReply? reply, int code, string replyName)
    {
        FtpReply checkedReply = RequireReply(reply, replyName);
        if (checkedReply.Code != code)
        {
            throw new FtpProtocolException($"The FTP {replyName} reply must use code {code}.", null);
        }
        return checkedReply;
    }

    internal static FtpPendingReply ExtractPendingAction(FtpReply? reply, string operation)
    {
        FtpReply checkedReply = RequireExpectedCode(
            reply, FtpReplyCodes.RequestedFileActionPending, operation);
        FtpPendingReply pending = new FtpPendingReply();
        pending.Reply = checkedReply;
        pending.Operation = operation;
        pending.Message = checkedReply.Text;
        return pending;
    }

    internal static FtpReply RequireSequenceReply(FtpReply? reply)
    {
        return RequireReply(reply, "transfer sequence");
    }
}
