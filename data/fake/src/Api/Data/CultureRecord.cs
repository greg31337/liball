namespace fake;

/// <summary>Represents a correlated synthetic general-culture vocabulary fixture.</summary>
/// <remarks>Artist and team names are fictional. Hacker and military fields are benign vocabulary and provide no operational guidance.</remarks>
public class CultureRecord
{
    /// <summary>The project-fictional music artist.</summary>
    public string MusicArtist = string.Empty;

    /// <summary>The project-fictional album correlated with <c>MusicArtist</c>.</summary>
    public string MusicAlbum = string.Empty;

    /// <summary>The project-fictional song correlated with <c>MusicArtist</c>.</summary>
    public string MusicSong = string.Empty;

    /// <summary>The broad genre correlated with the music fixture.</summary>
    public string MusicGenre = string.Empty;

    /// <summary>A generic art form.</summary>
    public string Art = string.Empty;

    /// <summary>A generic hobby.</summary>
    public string Hobby = string.Empty;

    /// <summary>The sport correlated with <c>Team</c> and <c>Position</c>.</summary>
    public string Sport = string.Empty;

    /// <summary>The project-fictional team correlated with <c>Sport</c>.</summary>
    public string Team = string.Empty;

    /// <summary>The position correlated with <c>Sport</c>.</summary>
    public string Position = string.Empty;

    /// <summary>A generic gaming term.</summary>
    public string Gaming = string.Empty;

    /// <summary>A generic marketing term.</summary>
    public string Marketing = string.Empty;

    /// <summary>A generic fashion term.</summary>
    public string Fashion = string.Empty;

    /// <summary>A generic construction term.</summary>
    public string Construction = string.Empty;

    /// <summary>A generic appliance name.</summary>
    public string Appliance = string.Empty;

    /// <summary>A generic weather term.</summary>
    public string Weather = string.Empty;

    /// <summary>A non-operational military support term.</summary>
    public string Military = string.Empty;

    /// <summary>A word from the NATO spelling alphabet.</summary>
    public string NatoAlphabet = string.Empty;

    /// <summary>A benign defensive-security term.</summary>
    public string Hacker = string.Empty;

    /// <summary>A generic programming term.</summary>
    public string Programming = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the artist or team claims a real entity, always <c>false</c>.</summary>
    public bool RepresentsRealArtistOrTeam;

    /// <summary>Whether the record provides operational military or intrusion guidance, always <c>false</c>.</summary>
    public bool IsOperationalGuidance;

    internal CultureRecord()
    {
    }
}
