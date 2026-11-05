namespace ftp;

internal static class FtpMachineListingParser
{
    internal static FtpListing ParseListing(string[]? lines)
    {
        string[] checkedLines = FtpListingParsing.CopyLines(lines);
        FtpEntry[] entries = new FtpEntry[checkedLines.Length];
        int index = 0;
        while (index < checkedLines.Length)
        {
            entries[index] = FtpMlsxCodec.ParseEntry(checkedLines[index]);
            index++;
        }

        FtpListing listing = new FtpListing();
        listing.Entries = entries;
        listing.RawLines = checkedLines;
        listing.MachineReadable = true;
        listing.Complete = true;
        listing.FormatName = "MLSx";
        return listing;
    }
}
