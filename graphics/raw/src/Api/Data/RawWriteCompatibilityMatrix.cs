namespace raw;

/// <summary>Records one application/version result for a registered write profile.</summary>
public class RawWriteCompatibilityEntry
{
    /// <summary>The stable write-profile identifier.</summary>
    public string ProfileId;
    /// <summary>The output format.</summary>
    public RawFormat Format;
    /// <summary>The independently exercised application, or an empty string when unconfirmed.</summary>
    public string Application;
    /// <summary>The exact exercised application version, or an empty string when unconfirmed.</summary>
    public string Version;
    /// <summary>The generated-file identifier, or an empty string.</summary>
    public string SampleId;
    /// <summary>The compatibility operation, or an empty string.</summary>
    public string Check;
    /// <summary>The recorded application result.</summary>
    public RawProfileEvidenceResult Result;
    /// <summary>Additional compatibility details, or an empty string.</summary>
    public string Details;
    /// <summary>Whether the associated writer profile still requires explicit experimental opt-in.</summary>
    public bool Experimental;

    /// <summary>Creates one immutable-by-convention compatibility matrix row.</summary>
    /// <param name="profileId">The write-profile identifier.</param>
    /// <param name="format">The output format.</param>
    /// <param name="application">The application name.</param>
    /// <param name="version">The exact application version.</param>
    /// <param name="sampleId">The generated-file identifier.</param>
    /// <param name="check">The compatibility operation.</param>
    /// <param name="result">The recorded result.</param>
    /// <param name="details">Additional details.</param>
    /// <param name="experimental">Whether the writer remains experimental.</param>
    public RawWriteCompatibilityEntry(string profileId, RawFormat format, string application, string version, string sampleId, string check, RawProfileEvidenceResult result, string details, bool experimental)
    {
        ProfileId = profileId == null ? string.Empty : profileId;
        Format = format;
        Application = application == null ? string.Empty : application;
        Version = version == null ? string.Empty : version;
        SampleId = sampleId == null ? string.Empty : sampleId;
        Check = check == null ? string.Empty : check;
        Result = result;
        Details = details == null ? string.Empty : details;
        Experimental = experimental;
    }

    /// <summary>Returns whether this row records a passed application check with an exact version.</summary>
    /// <returns><c>true</c> for a confirmed application/version row; otherwise <c>false</c>.</returns>
    internal bool IsConfirmed()
    {
        return Application.Length != 0 && Version.Length != 0 && Result == RawProfileEvidenceResult.Passed;
    }
}

/// <summary>Provides deterministic application/version results for registered writer profiles of one format.</summary>
public class RawWriteCompatibilityMatrix
{
    /// <summary>The output format represented by the matrix.</summary>
    public RawFormat Format;
    /// <summary>Rows ordered by profile registration and evidence order.</summary>
    public RawWriteCompatibilityEntry[] Entries;

    /// <summary>Creates a compatibility matrix.</summary>
    /// <param name="format">The output format.</param>
    /// <param name="entries">The ordered rows.</param>
    public RawWriteCompatibilityMatrix(RawFormat format, RawWriteCompatibilityEntry[] entries)
    {
        Format = format;
        Entries = entries == null ? System.Array.Empty<RawWriteCompatibilityEntry>() : entries;
    }

    /// <summary>Returns whether any application/version passed for a profile.</summary>
    /// <param name="profileId">The exact profile identifier.</param>
    /// <returns><c>true</c> when a passed versioned application row exists.</returns>
    internal bool HasConfirmedApplication(string profileId)
    {
        int index = 0;
        while (index < Entries.Length)
        {
            RawWriteCompatibilityEntry entry = Entries[index];
            if (entry != null && entry.ProfileId == profileId && entry.IsConfirmed())
            {
                return true;
            }
            index++;
        }
        return false;
    }

    /// <summary>Returns whether one exact application/version passed for a profile.</summary>
    /// <param name="profileId">The exact profile identifier.</param>
    /// <param name="application">The application name compared using normalized identity text.</param>
    /// <param name="version">The exact application version.</param>
    /// <returns><c>true</c> when the exact combination has a passed result.</returns>
    internal bool IsConfirmed(string profileId, string application, string version)
    {
        string normalized = RawProfileMatcher.NormalizeIdentityText(application);
        int index = 0;
        while (index < Entries.Length)
        {
            RawWriteCompatibilityEntry entry = Entries[index];
            if (entry != null && entry.ProfileId == profileId && entry.Version == version && RawProfileMatcher.NormalizeIdentityText(entry.Application) == normalized && entry.IsConfirmed())
            {
                return true;
            }
            index++;
        }
        return false;
    }

    /// <summary>Returns the rows for one exact profile identifier.</summary>
    /// <param name="profileId">The exact profile identifier.</param>
    /// <returns>Independent row references in matrix order.</returns>
    internal RawWriteCompatibilityEntry[] FindProfile(string profileId)
    {
        System.Collections.Generic.List<RawWriteCompatibilityEntry> matches = new System.Collections.Generic.List<RawWriteCompatibilityEntry>();
        int index = 0;
        while (index < Entries.Length)
        {
            RawWriteCompatibilityEntry entry = Entries[index];
            if (entry != null && entry.ProfileId == profileId)
            {
                matches.Add(entry);
            }
            index++;
        }
        return matches.ToArray();
    }
}
