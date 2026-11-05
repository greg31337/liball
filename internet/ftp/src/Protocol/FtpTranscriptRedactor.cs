using System;

namespace ftp;

internal static class FtpTranscriptRedactor
{
    internal const string RedactedText = "<redacted>";

    internal static FtpCommand RedactCommand(FtpCommand? command)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }

        FtpCommand copy = new FtpCommand();
        copy.Kind = command.Kind;
        copy.Verb = command.Verb;
        copy.Sensitive = IsSensitiveCommand(command);
        if (copy.Sensitive)
        {
            copy.Argument = RedactedText;
            copy.RawText = command.Verb + " " + RedactedText;
        }
        else
        {
            copy.Argument = command.Argument;
            copy.RawText = command.RawText;
        }
        return copy;
    }

    internal static FtpReply RedactReply(
        FtpCommand? command, FtpReply? reply)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        if (reply == null)
        {
            throw new ArgumentNullException("reply");
        }

        bool sensitive = IsSensitiveCommand(command);
        FtpReply copy = new FtpReply();
        copy.Code = reply.Code;
        copy.Text = sensitive ? RedactedText : reply.Text;
        copy.Lines = CopyReplyLines(reply.Lines, sensitive);
        copy.Classification = CopyClassification(reply.Classification);
        copy.Multiline = reply.Multiline;
        copy.RawText = sensitive ?
            reply.Code.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                " " + RedactedText :
            reply.RawText;
        return copy;
    }

    internal static FtpEvent CreateCommandEvent(FtpCommand? command)
    {
        FtpCommand safeCommand = RedactCommand(command);
        FtpEvent eventData = CreateBaseEvent(safeCommand);
        eventData.Kind = FtpEventKind.Command;
        eventData.Command = safeCommand;
        eventData.Message = safeCommand.RawText;
        return eventData;
    }

    internal static FtpEvent CreateReplyEvent(
        FtpCommand? command, FtpReply? reply)
    {
        FtpCommand safeCommand = RedactCommand(command);
        FtpReply safeReply = RedactReply(command, reply);
        FtpEvent eventData = CreateBaseEvent(safeCommand);
        eventData.Kind = FtpEventKind.Reply;
        eventData.Command = safeCommand;
        eventData.Reply = safeReply;
        eventData.HasReplyCategory = true;
        eventData.ReplyCategory = safeReply.Classification.Category;
        eventData.HasReplyKind = true;
        eventData.ReplyKind = safeReply.Classification.Kind;
        eventData.Message = safeReply.RawText;
        return eventData;
    }

    internal static FtpEvent CreateBaseEvent(FtpCommand command)
    {
        FtpEvent eventData = new FtpEvent();
        eventData.HasCommandKind = true;
        eventData.CommandKind = command.Kind;
        eventData.Timestamp.HasValue = true;
        eventData.Timestamp.ValueUtc = DateTime.UtcNow;
        eventData.Redacted = true;
        return eventData;
    }

    internal static bool IsSensitiveCommand(FtpCommand command)
    {
        if (command.Sensitive ||
            command.Kind == FtpCommandKind.Pass ||
            command.Kind == FtpCommandKind.Acct ||
            command.Kind == FtpCommandKind.Adat ||
            command.Kind == FtpCommandKind.Mic ||
            command.Kind == FtpCommandKind.Conf ||
            command.Kind == FtpCommandKind.Enc)
        {
            return true;
        }

        return string.Equals(command.Verb, FtpCommandNames.Pass, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command.Verb, FtpCommandNames.Acct, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command.Verb, FtpCommandNames.Adat, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command.Verb, FtpCommandNames.Mic, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command.Verb, FtpCommandNames.Conf, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command.Verb, FtpCommandNames.Enc, StringComparison.OrdinalIgnoreCase);
    }

    internal static FtpReplyLine[] CopyReplyLines(
        FtpReplyLine[]? lines, bool sensitive)
    {
        if (lines == null)
        {
            return new FtpReplyLine[0];
        }

        FtpReplyLine[] copy = new FtpReplyLine[lines.Length];
        int index = 0;
        while (index < lines.Length)
        {
            FtpReplyLine? source = lines[index];
            FtpReplyLine line = new FtpReplyLine();
            if (source != null)
            {
                line.HasCode = source.HasCode;
                line.Code = source.Code;
                line.Text = sensitive ? RedactedText : source.Text;
                line.RawText = sensitive ?
                    (source.HasCode ?
                        source.Code.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                            " " + RedactedText :
                        RedactedText) :
                    source.RawText;
                line.TerminatesMultiline = source.TerminatesMultiline;
            }
            copy[index] = line;
            index++;
        }
        return copy;
    }

    internal static FtpReplyClassification CopyClassification(
        FtpReplyClassification? classification)
    {
        FtpReplyClassification copy = new FtpReplyClassification();
        if (classification == null)
        {
            return copy;
        }
        copy.Category = classification.Category;
        copy.Kind = classification.Kind;
        copy.Malformed = classification.Malformed;
        copy.Succeeded = classification.Succeeded;
        copy.TransientFailure = classification.TransientFailure;
        copy.PermanentFailure = classification.PermanentFailure;
        copy.AuthenticationFailure = classification.AuthenticationFailure;
        return copy;
    }
}
