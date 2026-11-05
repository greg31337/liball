namespace ftp;

/// <summary>
/// Parses bounded machine-readable, human-readable, and name-only FTP listings.
/// </summary>
public class FtpListingParser : IFtpListingParser
{
    /// <summary>
    /// Creates a stateless FTP listing parser.
    /// </summary>
    public FtpListingParser()
    {
    }

    /// <summary>
    /// Parses RFC 3659 MLSx lines while preserving all facts and raw text.
    /// </summary>
    /// <param name="lines">The raw machine-readable lines in server order.</param>
    /// <returns>The parsed machine-readable listing.</returns>
    public FtpListing ParseMachineListing(string[] lines)
    {
        return FtpMachineListingParser.ParseListing(lines);
    }

    /// <summary>
    /// Detects and parses a common human-readable LIST or STAT format.
    /// </summary>
    /// <param name="lines">The raw human-readable lines in server order.</param>
    /// <returns>The detected listing with raw fallback entries.</returns>
    public FtpListing ParseHumanListing(string[] lines)
    {
        return FtpHumanListingParser.ParseListing(lines);
    }

    /// <summary>
    /// Parses NLST name lines without inferring entry types or safe paths.
    /// </summary>
    /// <param name="lines">The raw name lines in server order.</param>
    /// <returns>The name-only listing.</returns>
    public FtpListing ParseNameListing(string[] lines)
    {
        return FtpNameListingParser.ParseListing(lines);
    }
}
