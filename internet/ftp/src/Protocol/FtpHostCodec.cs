using System;

namespace ftp;

internal static class FtpHostCodec
{
    internal static string ParseHostArgument(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            throw new FtpProtocolException("An FTP HOST argument is required.", null);
        }

        if (argument[0] == '[')
        {
            ValidateIpv6Literal(argument);
            return argument;
        }

        if (argument.IndexOf(':') >= 0 || argument.IndexOf('[') >= 0 ||
            argument.IndexOf(']') >= 0)
        {
            throw new FtpProtocolException(
                "An FTP HOST address must omit a port and bracket IPv6 literals.", null);
        }

        if (Uri.CheckHostName(argument) == UriHostNameType.IPv4)
        {
            ValidateIpv4Literal(argument);
            return argument;
        }

        ValidateDomainName(argument);
        return argument;
    }

    internal static string FormatHostArgument(string? host)
    {
        return ParseHostArgument(host);
    }

    internal static void ValidateIpv6Literal(string argument)
    {
        if (argument.Length < 4 || argument[argument.Length - 1] != ']' ||
            argument.IndexOf('%') >= 0)
        {
            throw new FtpProtocolException("An FTP HOST IPv6 literal is invalid.", null);
        }

        string address = argument.Substring(1, argument.Length - 2);
        if (Uri.CheckHostName(address) != UriHostNameType.IPv6)
        {
            throw new FtpProtocolException("An FTP HOST IPv6 literal is invalid.", null);
        }
    }

    internal static void ValidateIpv4Literal(string argument)
    {
        int partStart = 0;
        int partCount = 0;
        int index = 0;
        while (index <= argument.Length)
        {
            if (index == argument.Length || argument[index] == '.')
            {
                int length = index - partStart;
                if (length < 1 || length > 3 ||
                    (length > 1 && argument[partStart] == '0'))
                {
                    throw new FtpProtocolException("An FTP HOST IPv4 literal is invalid.", null);
                }

                string part = argument.Substring(partStart, length);
                FtpCommandParser.ParseInt32InRange(part, 0, 255, "HOST IPv4 component");
                partCount++;
                partStart = index + 1;
            }
            index++;
        }

        if (partCount != 4)
        {
            throw new FtpProtocolException("An FTP HOST IPv4 literal is invalid.", null);
        }
    }

    internal static void ValidateDomainName(string argument)
    {
        if (argument.Length > 253)
        {
            throw new FtpProtocolException("An FTP HOST domain name exceeds DNS limits.", null);
        }

        int labelStart = 0;
        int index = 0;
        while (index <= argument.Length)
        {
            if (index == argument.Length || argument[index] == '.')
            {
                int labelLength = index - labelStart;
                if (labelLength < 1 || labelLength > 63 ||
                    !IsAsciiLetterOrDigit(argument[labelStart]) ||
                    !IsAsciiLetterOrDigit(argument[index - 1]))
                {
                    throw new FtpProtocolException("An FTP HOST domain label is invalid.", null);
                }

                labelStart = index + 1;
            }
            else
            {
                char character = argument[index];
                if (!IsAsciiLetterOrDigit(character) && character != '-')
                {
                    throw new FtpProtocolException("An FTP HOST domain label is invalid.", null);
                }
            }
            index++;
        }
    }

    internal static bool IsAsciiLetterOrDigit(char character)
    {
        return (character >= 'A' && character <= 'Z') ||
            (character >= 'a' && character <= 'z') ||
            (character >= '0' && character <= '9');
    }
}
