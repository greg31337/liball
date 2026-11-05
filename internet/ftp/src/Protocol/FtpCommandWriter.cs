using System.IO;
using System.Text;

namespace ftp;

internal static class FtpCommandWriter
{
    internal static string FormatDecodedCommand(FtpCommand? command)
    {
        string line = BuildCommandLine(command);
        ValidateUnicodeText(line);
        if (Encoding.UTF8.GetByteCount(line) > FtpDefaults.MaximumControlLineBytes - 2)
        {
            throw new FtpProtocolException("The formatted FTP command exceeded the control-line byte limit.", null);
        }

        return line + "\r\n";
    }

    internal static byte[] FormatEncodedCommand(FtpCommand? command, Encoding? argumentEncoding)
    {
        if (argumentEncoding == null)
        {
            throw new FtpConfigurationException("A negotiated command encoding is required.", null);
        }

        string line = BuildCommandLine(command);
        ValidateEncoding(argumentEncoding);
        ValidateUnicodeText(line);
        if (argumentEncoding.CodePage == Encoding.ASCII.CodePage)
        {
            ValidateAsciiText(line);
        }

        byte[] lineBytes = argumentEncoding.GetBytes(line);
        if (argumentEncoding.CodePage == Encoding.UTF8.CodePage)
        {
            FtpCommandParser.ValidateUtf8Bytes(lineBytes);
        }

        using MemoryStream destination = new MemoryStream();
        FtpLineWriter writer = new FtpLineWriter(FtpDefaults.MaximumControlLineBytes);
        writer.WriteLine(destination, lineBytes);
        return destination.ToArray();
    }

    internal static string BuildCommandLine(FtpCommand? command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        string verb = ResolveCanonicalVerb(command.Kind, command.Verb);
        FtpCommandKind kind = command.Kind;
        if (kind == FtpCommandKind.Unknown)
        {
            kind = FtpCommandParser.ClassifyVerb(verb);
        }

        FtpCommand normalized = new FtpCommand();
        normalized.Kind = kind;
        normalized.Verb = verb;
        normalized.Argument = command.Argument;
        FtpCommandParser.ValidateNumericCommandFields(normalized);
        command.Sensitive = IsSensitiveCommand(kind, verb);

        if (command.Argument == null)
        {
            return verb;
        }

        FtpCommandParser.ValidateDecodedCharacters(command.Argument);
        return verb + " " + command.Argument;
    }

    internal static string ResolveCanonicalVerb(FtpCommandKind kind, string? suppliedVerb)
    {
        if (kind == FtpCommandKind.Unknown)
        {
            if (suppliedVerb == null)
            {
                throw new FtpProtocolException("An unknown FTP command requires a verb.", null);
            }

            FtpCommandParser.ValidateVerb(suppliedVerb);
            return suppliedVerb.ToUpperInvariant();
        }

        return FtpCommandCatalog.ResolveVerb(kind);
    }

    internal static bool IsSensitiveCommand(FtpCommandKind kind, string verb)
    {
        if (kind == FtpCommandKind.Pass ||
            kind == FtpCommandKind.Acct ||
            kind == FtpCommandKind.Adat ||
            kind == FtpCommandKind.Mic ||
            kind == FtpCommandKind.Conf ||
            kind == FtpCommandKind.Enc)
        {
            return true;
        }

        return verb == FtpCommandNames.Pass ||
            verb == FtpCommandNames.Acct ||
            verb == FtpCommandNames.Adat ||
            verb == FtpCommandNames.Mic ||
            verb == FtpCommandNames.Conf ||
            verb == FtpCommandNames.Enc;
    }

    internal static void ValidateEncoding(Encoding encoding)
    {
        if (encoding.CodePage != Encoding.ASCII.CodePage && encoding.CodePage != Encoding.UTF8.CodePage)
        {
            throw new FtpConfigurationException("FTP command encoding supports negotiated ASCII or UTF-8.", null);
        }
    }

    internal static void ValidateAsciiText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 127)
            {
                throw new FtpProtocolException("The command contains characters outside negotiated ASCII.", null);
            }

            index++;
        }
    }

    internal static void ValidateUnicodeText(string text)
    {
        FtpCommandParser.ValidateDecodedCharacters(text);
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                {
                    throw new FtpProtocolException("The command contains an unpaired Unicode surrogate.", null);
                }

                index = index + 2;
            }
            else if (char.IsLowSurrogate(character))
            {
                throw new FtpProtocolException("The command contains an unpaired Unicode surrogate.", null);
            }
            else
            {
                index++;
            }
        }
    }
}
