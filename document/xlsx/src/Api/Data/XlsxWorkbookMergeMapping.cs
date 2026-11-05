using System;

namespace xlsx;

/// <summary>
/// Maps a source workbook name, identifier, or part path to its destination value.
/// </summary>
public class XlsxWorkbookMergeMapping
{
    /// <summary>Stores the source name or identifier.</summary>
    public string Source;
    /// <summary>Stores the destination name or identifier.</summary>
    public string Destination;
    /// <summary>Stores the source package part path when applicable.</summary>
    public string SourcePath;
    /// <summary>Stores the destination package part path when applicable.</summary>
    public string DestinationPath;

    /// <summary>Initializes an empty mapping.</summary>
    public XlsxWorkbookMergeMapping()
    {
        Source = "";
        Destination = "";
        SourcePath = "";
        DestinationPath = "";
    }
}
