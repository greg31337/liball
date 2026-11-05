using System;
using System.Collections.Generic;
using System.Text;

namespace ftp;

internal static class FtpMlsxCodec
{
    internal static FtpEntry ParseControlReply(FtpReply? reply)
    {
        FtpReply checkedReply = FtpReplyExtractor.RequireExpectedCode(
            reply, FtpReplyCodes.RequestedFileActionOkay, "MLST");
        if (!checkedReply.Multiline || checkedReply.Lines.Length < 3)
        {
            throw new FtpProtocolException(
                "A successful FTP MLST reply must contain one multiline entry.", null);
        }

        FtpReplyLine? entryLine = null;
        int index = 1;
        while (index < checkedReply.Lines.Length - 1)
        {
            FtpReplyLine line = checkedReply.Lines[index];
            if (line.RawText.Length > 0 && line.RawText[0] == ' ')
            {
                if (entryLine != null)
                {
                    throw new FtpProtocolException(
                        "An FTP MLST reply cannot contain more than one entry.", null);
                }
                entryLine = line;
            }
            else
            {
                throw new FtpProtocolException(
                    "An FTP MLST entry must use the required indentation space.", null);
            }
            index++;
        }

        if (entryLine == null)
        {
            throw new FtpProtocolException(
                "A successful FTP MLST reply did not contain an entry.", null);
        }
        return ParseControlEntry(entryLine.RawText);
    }

    internal static FtpListing ParseDirectoryText(
        string? listingText,
        bool strict)
    {
        if (listingText == null)
        {
            throw new ArgumentNullException("listingText");
        }

        string[] lines = FtpListingTextCodec.ParseLines(
            listingText, strict);
        FtpEntry[] entries = new FtpEntry[lines.Length];
        int index = 0;
        while (index < lines.Length)
        {
            entries[index] = ParseEntryCore(lines[index], strict);
            index++;
        }

        FtpListing listing = new FtpListing();
        listing.Entries = entries;
        listing.RawLines = lines;
        listing.MachineReadable = true;
        listing.Complete = true;
        listing.FormatName = FtpCommandNames.Mlsd;
        return listing;
    }

    internal static FtpEntry ParseEntry(string? line)
    {
        return ParseEntryCore(line, true);
    }

    internal static FtpEntry ParseEntryCore(string? line, bool strict)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }
        ValidateEntryLine(line);

        int separator = line.IndexOf(' ');
        if (separator < 0)
        {
            throw new FtpProtocolException(
                "An FTP MLSx entry must contain the required pathname separator.", null);
        }

        string factsText = line.Substring(0, separator);
        string pathname = line.Substring(separator + 1);
        List<FtpFact> facts = new List<FtpFact>();
        if (factsText.Length > 0)
        {
            ParseEntryFacts(factsText, facts, strict);
        }

        FtpEntry entry = new FtpEntry();
        entry.Name = pathname;
        entry.Facts = facts.ToArray();
        entry.RawLine = line;

        int index = 0;
        while (index < entry.Facts.Length)
        {
            ApplyStandardFact(entry, entry.Facts[index], strict);
            index++;
        }
        if (FtpListingParsing.IsHiddenName(entry.Name))
        {
            entry.Hidden = true;
        }
        return entry;
    }

    internal static string FormatEntry(FtpEntry? entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException("entry");
        }
        if (entry.Name == null)
        {
            throw new FtpProtocolException("An FTP MLSx entry pathname cannot be null.", null);
        }
        ValidatePathname(entry.Name);
        if (entry.Facts == null)
        {
            throw new FtpProtocolException("An FTP MLSx entry fact collection cannot be null.", null);
        }
        if (entry.Facts.Length > FtpDefaults.MaximumFactCount)
        {
            throw new FtpProtocolException("An FTP MLSx entry exceeded the configured fact limit.", null);
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < entry.Facts.Length)
        {
            builder.Append(FormatFact(entry.Facts[index]));
            builder.Append(';');
            index++;
        }
        builder.Append(' ');
        builder.Append(entry.Name);

        string line = builder.ToString();
        ValidateEntryLine(line);
        return line;
    }

    internal static FtpEntry ParseControlEntry(string? line)
    {
        if (line == null)
        {
            throw new ArgumentNullException("line");
        }
        if (line.Length == 0 || line[0] != ' ')
        {
            throw new FtpProtocolException(
                "An FTP MLST control-response entry must begin with one indentation space.", null);
        }

        FtpEntry entry = ParseEntry(line.Substring(1));
        entry.RawLine = line;
        return entry;
    }

    internal static string FormatControlEntry(FtpEntry? entry)
    {
        string line = " " + FormatEntry(entry);
        ValidateEntryLine(line);
        return line;
    }

    internal static FtpFact ParseFact(string? factText)
    {
        return ParseFactCore(factText, true);
    }

    internal static FtpFact ParseFactCore(string? factText, bool strict)
    {
        if (factText == null)
        {
            throw new ArgumentNullException("factText");
        }
        if (factText.Length == 0)
        {
            throw new FtpProtocolException("An FTP MLSx fact cannot be empty.", null);
        }

        int equals = factText.IndexOf('=');
        if (equals <= 0)
        {
            throw new FtpProtocolException("An FTP MLSx fact must contain a name and equals sign.", null);
        }

        string originalName = factText.Substring(0, equals);
        string value = factText.Substring(equals + 1);
        string normalizedName = NormalizeFactName(originalName);
        ValidateFactValue(value);
        bool standard = IsStandardFactName(normalizedName);
        if (standard)
        {
            if (!strict && normalizedName == FtpFactNames.Perm)
            {
                ParseLenientPermissionValues(value);
            }
            else
            {
                ValidateStandardFactValue(normalizedName, value);
            }
        }
        else
        {
            ValidateToken(value, "qualified fact value");
        }

        FtpFact fact = new FtpFact();
        fact.Name = normalizedName;
        fact.Value = value;
        fact.RawText = factText;
        fact.Standard = standard;
        return fact;
    }

    internal static string FormatFact(FtpFact? fact)
    {
        if (fact == null)
        {
            throw new ArgumentNullException("fact");
        }
        string normalizedName = NormalizeFactName(fact.Name);
        if (fact.Value == null)
        {
            throw new FtpProtocolException("An FTP MLSx fact value cannot be null.", null);
        }
        ValidateFactValue(fact.Value);
        if (IsStandardFactName(normalizedName))
        {
            ValidateStandardFactValue(normalizedName, fact.Value);
        }
        else
        {
            ValidateToken(fact.Value, "qualified fact value");
        }
        return normalizedName + "=" + fact.Value;
    }

    internal static void ParseEntryFacts(
        string factsText,
        List<FtpFact> facts,
        bool strict)
    {
        if (factsText[factsText.Length - 1] != ';')
        {
            throw new FtpProtocolException(
                "Every FTP MLSx entry fact must end with a semicolon.", null);
        }

        int start = 0;
        int index = 0;
        while (index < factsText.Length)
        {
            if (factsText[index] == ';')
            {
                if (index == start)
                {
                    throw new FtpProtocolException("An FTP MLSx entry cannot contain an empty fact.", null);
                }
                if (facts.Count >= FtpDefaults.MaximumFactCount)
                {
                    throw new FtpProtocolException(
                        "An FTP MLSx entry exceeded the configured fact limit.", null);
                }
                facts.Add(ParseFactCore(
                    factsText.Substring(start, index - start), strict));
                start = index + 1;
            }
            index++;
        }
    }

    internal static void ApplyStandardFact(
        FtpEntry entry,
        FtpFact fact,
        bool strict)
    {
        if (!fact.Standard)
        {
            return;
        }

        switch (fact.Name)
        {
            case FtpFactNames.Type:
                entry.Type = ParseEntryType(fact.Value);
                break;
            case FtpFactNames.Size:
                FtpSizeValue size = FtpSizeCodec.ParseSizeValue(fact.Value);
                entry.HasSize = size.HasValue;
                entry.Size = size.Value;
                break;
            case FtpFactNames.Modify:
                entry.Modified = FtpTimestampCodec.ParseTimestampValue(fact.Value);
                break;
            case FtpFactNames.Create:
                entry.Created = FtpTimestampCodec.ParseTimestampValue(fact.Value);
                break;
            case FtpFactNames.Unique:
                entry.Unique = fact.Value;
                break;
            case FtpFactNames.Perm:
                entry.Permissions = strict ?
                    ParsePermissionValues(fact.Value) :
                    ParseLenientPermissionValues(fact.Value);
                break;
            case FtpFactNames.Lang:
                entry.Language = fact.Value;
                break;
            case FtpFactNames.MediaType:
                entry.MediaType = fact.Value;
                break;
            case FtpFactNames.Charset:
                entry.Charset = fact.Value;
                break;
        }
    }

    internal static FtpEntryType ParseEntryType(string value)
    {
        if (string.Equals(value, "file", StringComparison.OrdinalIgnoreCase))
        {
            return FtpEntryType.File;
        }
        if (string.Equals(value, "dir", StringComparison.OrdinalIgnoreCase))
        {
            return FtpEntryType.Directory;
        }
        if (string.Equals(value, "cdir", StringComparison.OrdinalIgnoreCase))
        {
            return FtpEntryType.CurrentDirectory;
        }
        if (string.Equals(value, "pdir", StringComparison.OrdinalIgnoreCase))
        {
            return FtpEntryType.ParentDirectory;
        }
        ValidateOperatingSystemType(value);
        if (string.Equals(
            value, "OS.unix=slink", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                value, "OS.windows=reparse", StringComparison.OrdinalIgnoreCase))
        {
            return FtpEntryType.SymbolicLink;
        }
        return FtpEntryType.Unknown;
    }

    internal static FtpPermission[] ParsePermissionValues(string value)
    {
        return ParsePermissionValuesCore(value, true);
    }

    internal static FtpPermission[] ParseLenientPermissionValues(string value)
    {
        return ParsePermissionValuesCore(value, false);
    }

    internal static FtpPermission[] ParsePermissionValuesCore(
        string value,
        bool strict)
    {
        List<FtpPermission> permissions = new List<FtpPermission>();
        int index = 0;
        while (index < value.Length)
        {
            char indicator = char.ToLowerInvariant(value[index]);
            FtpPermission permission = FtpPermission.None;
            switch (indicator)
            {
                case 'a': permission = FtpPermission.Append; break;
                case 'c': permission = FtpPermission.CreateFile; break;
                case 'd': permission = FtpPermission.DeleteFile; break;
                case 'e': permission = FtpPermission.EnterDirectory; break;
                case 'f': permission = FtpPermission.Rename; break;
                case 'l': permission = FtpPermission.List; break;
                case 'm': permission = FtpPermission.CreateDirectory; break;
                case 'p': permission = FtpPermission.DeleteDirectory; break;
                case 'r': permission = FtpPermission.Read; break;
                case 'w': permission = FtpPermission.Write; break;
                default:
                    if (strict || indicator < 'a' || indicator > 'z')
                    {
                        throw new FtpProtocolException(
                            "An FTP MLSx permission fact contains an unknown indicator.", null);
                    }
                    break;
            }
            if (permission != FtpPermission.None)
            {
                AppendPermission(permissions, permission);
            }
            index++;
        }
        return permissions.ToArray();
    }

    internal static void AppendPermission(
        List<FtpPermission> permissions, FtpPermission permission)
    {
        int index = 0;
        while (index < permissions.Count)
        {
            if (permissions[index] == permission)
            {
                return;
            }
            index++;
        }
        permissions.Add(permission);
    }

    internal static void ValidateStandardFactValue(string name, string value)
    {
        switch (name)
        {
            case FtpFactNames.Type:
                ParseEntryType(value);
                break;
            case FtpFactNames.Size:
                FtpSizeCodec.ParseSizeValue(value);
                break;
            case FtpFactNames.Modify:
            case FtpFactNames.Create:
                FtpTimestampCodec.ParseTimestampValue(value);
                break;
            case FtpFactNames.Unique:
            case FtpFactNames.Lang:
            case FtpFactNames.Charset:
                ValidateToken(value, "standard fact value");
                break;
            case FtpFactNames.Perm:
                ParsePermissionValues(value);
                break;
            case FtpFactNames.MediaType:
                ValidateMediaType(value);
                break;
        }
    }

    internal static void ValidateOperatingSystemType(string value)
    {
        if (value.Length < 6 ||
            !string.Equals(value.Substring(0, 3), "OS.", StringComparison.OrdinalIgnoreCase))
        {
            throw new FtpProtocolException("An FTP MLSx type fact contains an unknown type.", null);
        }
        int equals = value.IndexOf('=', 3);
        if (equals <= 3 || equals == value.Length - 1)
        {
            throw new FtpProtocolException("An FTP MLSx operating-system type is malformed.", null);
        }
        ValidateToken(value.Substring(3, equals - 3), "operating-system name");
        ValidateToken(value.Substring(equals + 1), "operating-system type");
    }

    internal static void ValidateMediaType(string value)
    {
        int slash = value.IndexOf('/');
        if (slash <= 0 || slash == value.Length - 1 || value.IndexOf('/', slash + 1) >= 0)
        {
            throw new FtpProtocolException("An FTP MLSx media-type fact is malformed.", null);
        }
        ValidateToken(value.Substring(0, slash), "media type");
        ValidateToken(value.Substring(slash + 1), "media subtype");
    }

    internal static string NormalizeFactName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new FtpProtocolException("An FTP MLSx fact name is required.", null);
        }

        string normalized = name.ToLowerInvariant();
        if (IsStandardFactName(normalized))
        {
            return normalized;
        }

        int dot = normalized.IndexOf('.');
        if (dot <= 0 || dot == normalized.Length - 1)
        {
            throw new FtpProtocolException(
                "An unknown FTP MLSx fact must use an OS or X. qualified name.", null);
        }
        ValidateToken(normalized.Substring(0, dot), "fact namespace");
        ValidateToken(normalized.Substring(dot + 1), "qualified fact name");
        return normalized;
    }

    internal static bool IsStandardFactName(string name)
    {
        return name == FtpFactNames.Type || name == FtpFactNames.Size ||
            name == FtpFactNames.Modify || name == FtpFactNames.Create ||
            name == FtpFactNames.Unique || name == FtpFactNames.Perm ||
            name == FtpFactNames.Lang || name == FtpFactNames.MediaType ||
            name == FtpFactNames.Charset;
    }

    internal static void ValidateFactValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != '=' && !IsRchar(character))
            {
                throw new FtpProtocolException(
                    "An FTP MLSx fact value contains a character outside the RFC SCHAR set.", null);
            }
            index++;
        }
    }

    internal static void ValidateToken(string value, string valueName)
    {
        if (value.Length == 0)
        {
            throw new FtpProtocolException($"An FTP MLSx {valueName} token is required.", null);
        }
        int index = 0;
        while (index < value.Length)
        {
            if (!IsRchar(value[index]))
            {
                throw new FtpProtocolException(
                    $"An FTP MLSx {valueName} contains a character outside the RFC token set.", null);
            }
            index++;
        }
    }

    internal static bool IsRchar(char character)
    {
        if ((character >= 'A' && character <= 'Z') ||
            (character >= 'a' && character <= 'z') ||
            (character >= '0' && character <= '9'))
        {
            return true;
        }
        return ",.:!@#$%^&()-_+?/\\'\"".IndexOf(character) >= 0;
    }

    internal static void ValidateEntryLine(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) > FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException("An FTP MLSx entry exceeded the configured line limit.", null);
        }
        ValidatePathname(line);
    }

    internal static void ValidatePathname(string pathname)
    {
        int index = 0;
        while (index < pathname.Length)
        {
            char character = pathname[index];
            if (character == '\r' || character == '\n')
            {
                throw new FtpProtocolException(
                    "An FTP MLSx pathname cannot contain a data-line terminator.", null);
            }
            index++;
        }
    }
}
