namespace ftp;

internal static class FtpNameListingParser
{
    internal static FtpListing ParseListing(string[]? lines)
    {
        string[] checkedLines = FtpListingParsing.CopyLines(lines);
        FtpEntry[] entries = new FtpEntry[checkedLines.Length];
        int index = 0;
        while (index < checkedLines.Length)
        {
            FtpEntry entry = new FtpEntry();
            entry.Name = checkedLines[index];
            entry.Hidden = FtpListingParsing.IsHiddenName(entry.Name);
            entry.RawLine = checkedLines[index];
            entries[index] = entry;
            index++;
        }

        FtpListing listing = new FtpListing();
        listing.Entries = entries;
        listing.RawLines = checkedLines;
        listing.MachineReadable = false;
        listing.Complete = true;
        listing.FormatName = "Names";
        return listing;
    }
}
