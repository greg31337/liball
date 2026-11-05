using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="CultureProvider"/>.</summary>
public interface ICultureProvider
{
    /// <summary>Selects a project-fictional music artist.</summary>
    /// <returns>A fictional artist correlated with album, song, and genre fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string MusicArtist();
    /// <summary>Selects a project-fictional music album.</summary>
    /// <returns>A fictional album correlated with artist, song, and genre fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string MusicAlbum();
    /// <summary>Selects a project-fictional song title.</summary>
    /// <returns>A fictional song correlated with artist, album, and genre fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string MusicSong();
    /// <summary>Selects a broad music genre.</summary>
    /// <returns>A genre correlated with artist, album, and song fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string MusicGenre();
    /// <summary>Selects a project-fictional music artist.</summary>
    /// <remarks>This is an alias for <see cref="MusicArtist()"/>.</remarks>
    /// <returns>A fictional artist name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Artist();
    /// <summary>Selects a project-fictional music album.</summary>
    /// <remarks>This is an alias for <see cref="MusicAlbum()"/>.</remarks>
    /// <returns>A fictional album title.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Album();
    /// <summary>Selects a project-fictional song title.</summary>
    /// <remarks>This is an alias for <see cref="MusicSong()"/>.</remarks>
    /// <returns>A fictional song title.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Song();
    /// <summary>Selects a broad music genre.</summary>
    /// <remarks>This is an alias for <see cref="MusicGenre()"/>.</remarks>
    /// <returns>A music genre.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Genre();
    /// <summary>Selects a generic art form.</summary>
    /// <returns>An art-form term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Art();
    /// <summary>Selects a generic hobby.</summary>
    /// <returns>A hobby term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Hobby();
    /// <summary>Selects a sport.</summary>
    /// <returns>A sport correlated with team and position fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Sport();
    /// <summary>Selects a project-fictional sports team.</summary>
    /// <returns>A fictional team correlated with sport and position fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Team();
    /// <summary>Selects a sports position.</summary>
    /// <returns>A position correlated with sport and team fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Position();
    /// <summary>Selects a generic gaming term.</summary>
    /// <returns>A gaming vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Gaming();
    /// <summary>Selects a generic marketing term.</summary>
    /// <returns>A marketing vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Marketing();
    /// <summary>Selects a generic fashion term.</summary>
    /// <returns>A fashion vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Fashion();
    /// <summary>Selects a generic construction term.</summary>
    /// <returns>A construction vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Construction();
    /// <summary>Selects a generic household appliance.</summary>
    /// <returns>An appliance name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Appliance();
    /// <summary>Selects a generic weather term.</summary>
    /// <remarks>The value is vocabulary, not a forecast or observation.</remarks>
    /// <returns>A weather term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Weather();
    /// <summary>Selects a non-operational military support term.</summary>
    /// <returns>An administrative or support vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Military();
    /// <summary>Selects a word from the NATO spelling alphabet.</summary>
    /// <returns>One standard spelling-alphabet word.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string NatoAlphabet();
    /// <summary>Selects a benign defensive-security term.</summary>
    /// <remarks>The value provides no intrusion instructions, exploit content, or secrets.</remarks>
    /// <returns>A defensive-security vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Hacker();
    /// <summary>Selects a generic programming term.</summary>
    /// <returns>A programming vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Programming();
    /// <summary>Generates a correlated synthetic general-culture fixture.</summary>
    /// <remarks>Artist and team names are fictional. Military and hacker fields contain benign vocabulary and no operational guidance.</remarks>
    /// <returns>Correlated music, sport, and generic culture vocabulary.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    CultureRecord Record();
}
