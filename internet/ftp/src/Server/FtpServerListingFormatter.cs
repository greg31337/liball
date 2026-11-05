using System;
using System.Globalization;
using System.Text;

namespace ftp;

internal static class FtpServerListingFormatter
{
    internal static string FormatDataListing(
        FtpEntry[] entries,
        FtpCommandKind kind,
        string[] selectedFacts,
        bool factsConfigured)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < entries.Length)
        {
            string line;
            if (kind == FtpCommandKind.Nlst)
            {
                line = FormatName(entries[index]);
            }
            else if (kind == FtpCommandKind.Mlsd)
            {
                line = FormatMachineEntry(
                    entries[index], entries[index].Name,
                    selectedFacts, factsConfigured);
            }
            else
            {
                line = FormatHumanEntry(entries[index]);
            }
            ValidateListingLine(line);
            builder.Append(line);
            builder.Append("\r\n");
            index++;
        }
        return builder.ToString();
    }

    internal static string FormatMachineEntry(
        FtpEntry entry,
        string pathname,
        string[] selectedFacts,
        bool factsConfigured)
    {
        FtpEntry selected = new FtpEntry();
        selected.Name = pathname;
        selected.Facts = SelectFacts(
            entry.Facts, selectedFacts, factsConfigured);
        return FtpMlsxCodec.FormatEntry(selected);
    }

    internal static string FormatHumanEntry(FtpEntry entry)
    {
        string name = FormatName(entry);
        string displayedName = name;
        if (entry.Type == FtpEntryType.SymbolicLink &&
            !string.IsNullOrEmpty(entry.LinkTarget))
        {
            displayedName = displayedName + " -> " + entry.LinkTarget;
        }
        DateTime modified = entry.Modified.HasValue ?
            entry.Modified.ValueUtc : DateTime.UnixEpoch;
        string owner = string.IsNullOrEmpty(entry.Owner) ?
            "owner" : entry.Owner;
        string group = string.IsNullOrEmpty(entry.Group) ?
            "group" : entry.Group;
        long size = entry.HasSize ? entry.Size : 0;
        return FormatMode(entry) + " 1 " + owner + " " + group + " " +
            size.ToString(CultureInfo.InvariantCulture) + " " +
            modified.ToString("MMM dd yyyy", CultureInfo.InvariantCulture) +
            " " + displayedName;
    }

    internal static string FormatName(FtpEntry entry)
    {
        if (entry == null || entry.Name == null || entry.Name.Length == 0)
        {
            throw new FtpProtocolException(
                "An FTP listing entry requires a non-empty name.", null);
        }
        ValidateListingLine(entry.Name);
        return entry.Name;
    }

    internal static FtpFact[] SelectFacts(
        FtpFact[] facts,
        string[] selectedFacts,
        bool factsConfigured)
    {
        if (facts == null || selectedFacts == null)
        {
            throw new FtpProtocolException(
                "FTP MLSx fact collections cannot be null.", null);
        }
        if (!factsConfigured)
        {
            return facts;
        }

        FtpFact[] selected = new FtpFact[facts.Length];
        int count = 0;
        int factIndex = 0;
        while (factIndex < facts.Length)
        {
            FtpFact fact = facts[factIndex];
            if (ContainsFact(selectedFacts, fact.Name))
            {
                selected[count] = fact;
                count++;
            }
            factIndex++;
        }
        FtpFact[] result = new FtpFact[count];
        int index = 0;
        while (index < count)
        {
            result[index] = selected[index];
            index++;
        }
        return result;
    }

    internal static bool ContainsFact(string[] facts, string name)
    {
        int index = 0;
        while (index < facts.Length)
        {
            if (string.Equals(
                facts[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string FormatMode(FtpEntry entry)
    {
        char type = entry.Type == FtpEntryType.Directory ||
            entry.Type == FtpEntryType.CurrentDirectory ||
            entry.Type == FtpEntryType.ParentDirectory ? 'd' :
            entry.Type == FtpEntryType.SymbolicLink ? 'l' : '-';
        int mode = entry.HasUnixMode ? entry.UnixMode : 420;
        char[] text = new char[10];
        text[0] = type;
        text[1] = (mode & 256) != 0 ? 'r' : '-';
        text[2] = (mode & 128) != 0 ? 'w' : '-';
        text[3] = (mode & 64) != 0 ? 'x' : '-';
        text[4] = (mode & 32) != 0 ? 'r' : '-';
        text[5] = (mode & 16) != 0 ? 'w' : '-';
        text[6] = (mode & 8) != 0 ? 'x' : '-';
        text[7] = (mode & 4) != 0 ? 'r' : '-';
        text[8] = (mode & 2) != 0 ? 'w' : '-';
        text[9] = (mode & 1) != 0 ? 'x' : '-';
        return new string(text);
    }

    internal static void ValidateListingLine(string line)
    {
        if (line == null ||
            Encoding.UTF8.GetByteCount(line) + 2 >
                FtpDefaults.MaximumControlLineBytes)
        {
            throw new FtpProtocolException(
                "An FTP listing line exceeded its configured limit.", null);
        }
        int index = 0;
        while (index < line.Length)
        {
            if (char.IsControl(line[index]))
            {
                throw new FtpProtocolException(
                    "An FTP listing line cannot contain control characters.",
                    null);
            }
            index++;
        }
    }
}
