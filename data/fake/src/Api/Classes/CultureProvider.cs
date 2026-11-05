using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides general culture and vocabulary generation operations.
/// </summary>
public class CultureProvider : ICultureProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal CultureProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("culture");
    }

    /// <summary>Selects a project-fictional music artist.</summary>
    /// <returns>A fictional artist correlated with album, song, and genre fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string MusicArtist()
    {
        return CultureGenerator.MusicArtist(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional music album.</summary>
    /// <returns>A fictional album correlated with artist, song, and genre fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string MusicAlbum()
    {
        return CultureGenerator.MusicAlbum(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional song title.</summary>
    /// <returns>A fictional song correlated with artist, album, and genre fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string MusicSong()
    {
        return CultureGenerator.MusicSong(Context, RandomValue);
    }

    /// <summary>Selects a broad music genre.</summary>
    /// <returns>A genre correlated with artist, album, and song fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string MusicGenre()
    {
        return CultureGenerator.MusicGenre(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional music artist.</summary>
    /// <remarks>This is an alias for <see cref="MusicArtist()"/>.</remarks>
    /// <returns>A fictional artist name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Artist()
    {
        return MusicArtist();
    }

    /// <summary>Selects a project-fictional music album.</summary>
    /// <remarks>This is an alias for <see cref="MusicAlbum()"/>.</remarks>
    /// <returns>A fictional album title.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Album()
    {
        return MusicAlbum();
    }

    /// <summary>Selects a project-fictional song title.</summary>
    /// <remarks>This is an alias for <see cref="MusicSong()"/>.</remarks>
    /// <returns>A fictional song title.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Song()
    {
        return MusicSong();
    }

    /// <summary>Selects a broad music genre.</summary>
    /// <remarks>This is an alias for <see cref="MusicGenre()"/>.</remarks>
    /// <returns>A music genre.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Genre()
    {
        return MusicGenre();
    }

    /// <summary>Selects a generic art form.</summary>
    /// <returns>An art-form term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Art()
    {
        return CultureGenerator.Art(Context, RandomValue);
    }

    /// <summary>Selects a generic hobby.</summary>
    /// <returns>A hobby term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Hobby()
    {
        return CultureGenerator.Hobby(Context, RandomValue);
    }

    /// <summary>Selects a sport.</summary>
    /// <returns>A sport correlated with team and position fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Sport()
    {
        return CultureGenerator.Sport(Context, RandomValue);
    }

    /// <summary>Selects a project-fictional sports team.</summary>
    /// <returns>A fictional team correlated with sport and position fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Team()
    {
        return CultureGenerator.Team(Context, RandomValue);
    }

    /// <summary>Selects a sports position.</summary>
    /// <returns>A position correlated with sport and team fixtures.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Position()
    {
        return CultureGenerator.Position(Context, RandomValue);
    }

    /// <summary>Selects a generic gaming term.</summary>
    /// <returns>A gaming vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Gaming()
    {
        return CultureGenerator.Gaming(Context, RandomValue);
    }

    /// <summary>Selects a generic marketing term.</summary>
    /// <returns>A marketing vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Marketing()
    {
        return CultureGenerator.Marketing(Context, RandomValue);
    }

    /// <summary>Selects a generic fashion term.</summary>
    /// <returns>A fashion vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Fashion()
    {
        return CultureGenerator.Fashion(Context, RandomValue);
    }

    /// <summary>Selects a generic construction term.</summary>
    /// <returns>A construction vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Construction()
    {
        return CultureGenerator.Construction(Context, RandomValue);
    }

    /// <summary>Selects a generic household appliance.</summary>
    /// <returns>An appliance name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Appliance()
    {
        return CultureGenerator.Appliance(Context, RandomValue);
    }

    /// <summary>Selects a generic weather term.</summary>
    /// <remarks>The value is vocabulary, not a forecast or observation.</remarks>
    /// <returns>A weather term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Weather()
    {
        return CultureGenerator.Weather(Context, RandomValue);
    }

    /// <summary>Selects a non-operational military support term.</summary>
    /// <returns>An administrative or support vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Military()
    {
        return CultureGenerator.Military(Context, RandomValue);
    }

    /// <summary>Selects a word from the NATO spelling alphabet.</summary>
    /// <returns>One standard spelling-alphabet word.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string NatoAlphabet()
    {
        return CultureGenerator.NatoAlphabet(Context, RandomValue);
    }

    /// <summary>Selects a benign defensive-security term.</summary>
    /// <remarks>The value provides no intrusion instructions, exploit content, or secrets.</remarks>
    /// <returns>A defensive-security vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Hacker()
    {
        return CultureGenerator.Hacker(Context, RandomValue);
    }

    /// <summary>Selects a generic programming term.</summary>
    /// <returns>A programming vocabulary term.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Programming()
    {
        return CultureGenerator.Programming(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic general-culture fixture.</summary>
    /// <remarks>Artist and team names are fictional. Military and hacker fields contain benign vocabulary and no operational guidance.</remarks>
    /// <returns>Correlated music, sport, and generic culture vocabulary.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public CultureRecord Record()
    {
        return CultureGenerator.Record(Context, RandomValue);
    }
}
