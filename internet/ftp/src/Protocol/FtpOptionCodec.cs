using System;
using System.Collections.Generic;
using System.Text;

namespace ftp;

internal static class FtpOptionCodec
{
    internal static FtpOptionRequest ParseOptionCommand(FtpCommand? command)
    {
        if (command == null)
        {
            throw new ArgumentNullException("command");
        }
        bool optionCommand = command.Kind == FtpCommandKind.Opts ||
            string.Equals(command.Verb, FtpCommandNames.Opts, StringComparison.OrdinalIgnoreCase);
        if (!optionCommand)
        {
            throw new FtpProtocolException("The FTP option codec requires an OPTS command.", null);
        }
        if (string.IsNullOrEmpty(command.Argument))
        {
            throw new FtpProtocolException("An FTP OPTS command requires a target command name.", null);
        }

        int separator = command.Argument.IndexOf(' ');
        string target;
        string? value;
        if (separator < 0)
        {
            target = command.Argument;
            value = null;
        }
        else
        {
            target = command.Argument.Substring(0, separator);
            value = command.Argument.Substring(separator + 1);
        }

        FtpOptionRequest request = new FtpOptionRequest();
        request.CommandName = NormalizeOptionTarget(target);
        request.Value = value;
        ValidateOptionValue(value);
        ValidateOptionRequestSize(request.CommandName, request.Value);
        return request;
    }

    internal static FtpCommand GenerateOptionCommand(FtpOptionRequest? request)
    {
        FtpOptionRequest normalized = NormalizeOptionRequest(request);
        string argument = normalized.CommandName;
        if (normalized.Value != null)
        {
            argument = argument + " " + normalized.Value;
        }

        FtpCommand command = new FtpCommand();
        command.Kind = FtpCommandKind.Opts;
        command.Verb = FtpCommandNames.Opts;
        command.Argument = argument;
        command.RawText = FtpCommandNames.Opts + " " + argument;
        return command;
    }

    internal static FtpOptionRequest NormalizeOptionRequest(FtpOptionRequest? request)
    {
        if (request == null)
        {
            throw new ArgumentNullException("request");
        }

        FtpOptionRequest normalized = new FtpOptionRequest();
        normalized.CommandName = NormalizeOptionTarget(request.CommandName);
        normalized.Value = request.Value;
        ValidateOptionValue(request.Value);
        ValidateOptionRequestSize(normalized.CommandName, normalized.Value);
        return normalized;
    }

    internal static string NormalizeOptionTarget(string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            throw new FtpProtocolException("An FTP OPTS target command name is required.", null);
        }
        FtpCommandParser.ValidateVerb(target);
        return target.ToUpperInvariant();
    }

    internal static void ValidateOptionValue(string? value)
    {
        if (value != null)
        {
            FtpCommandParser.ValidateDecodedCharacters(value);
        }
    }

    internal static void ValidateOptionRequestSize(string target, string? value)
    {
        string commandText = FtpCommandNames.Opts + " " + target;
        if (value != null)
        {
            commandText = commandText + " " + value;
        }
        if (Encoding.UTF8.GetByteCount(commandText) > FtpDefaults.MaximumControlLineBytes - 2)
        {
            throw new FtpProtocolException("The FTP OPTS command exceeded the control-line size limit.", null);
        }
    }

    internal static FtpOptionFactSelection ParseMlstFactSelection(string? value)
    {
        FtpOptionFactSelection selection = new FtpOptionFactSelection();
        if (value == null)
        {
            selection.Valid = true;
            return selection;
        }
        if (value.Length == 0)
        {
            selection.FailureMessage = "OPTS MLST cannot contain an empty option section.";
            return selection;
        }
        if (value[value.Length - 1] != ';')
        {
            selection.FailureMessage = "Every OPTS MLST fact must end with a semicolon.";
            return selection;
        }

        List<string> facts = new List<string>();
        int start = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == ';')
            {
                if (index == start)
                {
                    selection.FailureMessage = "OPTS MLST cannot contain an empty fact name.";
                    return selection;
                }
                if (facts.Count >= FtpDefaults.MaximumFactCount)
                {
                    selection.FailureMessage = "OPTS MLST exceeded the configured fact limit.";
                    return selection;
                }

                string factText = value.Substring(start, index - start);
                string? normalizedFact = TryNormalizeMlstFact(factText);
                if (normalizedFact == null)
                {
                    selection.FailureMessage = "OPTS MLST contains an invalid fact name.";
                    return selection;
                }
                facts.Add(normalizedFact);
                start = index + 1;
            }
            index++;
        }

        selection.Valid = true;
        selection.Facts = facts.ToArray();
        return selection;
    }

    internal static string? TryNormalizeMlstFact(string? fact)
    {
        if (string.IsNullOrEmpty(fact))
        {
            return null;
        }
        int index = 0;
        while (index < fact.Length)
        {
            char character = fact[index];
            if (character < '!' || character > '~' || character == '*' ||
                character == ';' || character == '=')
            {
                return null;
            }
            index++;
        }
        return fact.ToLowerInvariant();
    }

    internal static bool TryParseUtf8Value(string? value, FtpOptionApplicationResult result)
    {
        if (string.Equals(value, "ON", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "OFF", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        result.Accepted = false;
        return false;
    }

    internal static string[] ParseMlstOptionReply(FtpReply? reply)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireExpectedCode(
            reply, FtpReplyCodes.CommandOkay, "OPTS MLST");
        const string prefix = "MLST OPTS";
        if (string.Equals(checkedReply.Text, prefix, StringComparison.OrdinalIgnoreCase))
        {
            return new string[0];
        }
        if (checkedReply.Text.Length <= prefix.Length || checkedReply.Text[prefix.Length] != ' ' ||
            !string.Equals(checkedReply.Text.Substring(0, prefix.Length), prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new FtpProtocolException("A successful OPTS MLST reply must begin with MLST OPTS.", null);
        }

        FtpOptionFactSelection selection = ParseMlstFactSelection(
            checkedReply.Text.Substring(prefix.Length + 1));
        if (!selection.Valid)
        {
            throw new FtpProtocolException(selection.FailureMessage, null);
        }
        return selection.Facts;
    }

    internal static FtpReply CreateOptionReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }
}
