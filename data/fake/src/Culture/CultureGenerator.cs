namespace fake;

internal static class CultureGenerator
{
    internal static string MusicArtist(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateMusicData(context);
        return Value(context, random, CultureData.MusicArtists, "musicArtist");
    }

    internal static string MusicAlbum(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateMusicData(context);
        return Value(context, random, CultureData.MusicAlbums, "musicAlbum");
    }

    internal static string MusicSong(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateMusicData(context);
        return Value(context, random, CultureData.MusicSongs, "musicSong");
    }

    internal static string MusicGenre(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateMusicData(context);
        return Value(context, random, CultureData.MusicGenres, "musicGenre");
    }

    internal static string Sport(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateSportData(context);
        return Value(context, random, CultureData.Sports, "sport");
    }

    internal static string Team(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateSportData(context);
        return Value(context, random, CultureData.Teams, "team");
    }

    internal static string Position(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateSportData(context);
        return Value(context, random, CultureData.Positions, "position");
    }

    internal static string Art(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(context, random, CultureData.Art, "artData", "art");
    }

    internal static string Hobby(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Hobbies,
            "hobbyData",
            "hobby");
    }

    internal static string Gaming(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Gaming,
            "gamingData",
            "gaming");
    }

    internal static string Marketing(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Marketing,
            "marketingData",
            "marketing");
    }

    internal static string Fashion(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Fashion,
            "fashionData",
            "fashion");
    }

    internal static string Construction(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Construction,
            "constructionData",
            "construction");
    }

    internal static string Appliance(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Appliances,
            "applianceData",
            "appliance");
    }

    internal static string Weather(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Weather,
            "cultureWeatherData",
            "weather");
    }

    internal static string Military(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Military,
            "militaryData",
            "military");
    }

    internal static string NatoAlphabet(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.NatoAlphabet,
            "natoAlphabetData",
            "natoAlphabet");
    }

    internal static string Hacker(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Hacker,
            "hackerData",
            "hacker");
    }

    internal static string Programming(
        GenerationContext context,
        FakeRandom random)
    {
        return Category(
            context,
            random,
            CultureData.Programming,
            "programmingData",
            "programming");
    }

    internal static CultureRecord Record(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateAllData(context);
        Guard.OutputSize(
            MaximumRecordSize(),
            context.CapturedOptions.MaximumOutputSize,
            "cultureRecord");
        int musicIndex = random.IntFromMaximumExclusive(CultureData.MusicArtists.Length);
        int sportIndex = random.IntFromMaximumExclusive(CultureData.Sports.Length);
        CultureRecord result = new CultureRecord();
        result.MusicArtist = CultureData.MusicArtists[musicIndex];
        result.MusicAlbum = CultureData.MusicAlbums[musicIndex];
        result.MusicSong = CultureData.MusicSongs[musicIndex];
        result.MusicGenre = CultureData.MusicGenres[musicIndex];
        result.Art = RandomValue(random, CultureData.Art);
        result.Hobby = RandomValue(random, CultureData.Hobbies);
        result.Sport = CultureData.Sports[sportIndex];
        result.Team = CultureData.Teams[sportIndex];
        result.Position = CultureData.Positions[sportIndex];
        result.Gaming = RandomValue(random, CultureData.Gaming);
        result.Marketing = RandomValue(random, CultureData.Marketing);
        result.Fashion = RandomValue(random, CultureData.Fashion);
        result.Construction = RandomValue(random, CultureData.Construction);
        result.Appliance = RandomValue(random, CultureData.Appliances);
        result.Weather = RandomValue(random, CultureData.Weather);
        result.Military = RandomValue(random, CultureData.Military);
        result.NatoAlphabet = RandomValue(random, CultureData.NatoAlphabet);
        result.Hacker = RandomValue(random, CultureData.Hacker);
        result.Programming = RandomValue(random, CultureData.Programming);
        result.IsSynthetic = true;
        result.RepresentsRealArtistOrTeam = false;
        result.IsOperationalGuidance = false;
        return result;
    }

    static string Category(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string dataName,
        string outputName)
    {
        ValidateValues(context, values, dataName);
        return Value(context, random, values, outputName);
    }

    static string Value(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string outputName)
    {
        Guard.OutputSize(
            MaximumLength(values),
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return RandomValue(random, values);
    }

    static string RandomValue(FakeRandom random, string[] values)
    {
        return values[random.IntFromMaximumExclusive(values.Length)];
    }

    static void ValidateAllData(GenerationContext context)
    {
        ValidateMusicData(context);
        ValidateSportData(context);
        ValidateValues(context, CultureData.Art, "artData");
        ValidateValues(context, CultureData.Hobbies, "hobbyData");
        ValidateValues(context, CultureData.Gaming, "gamingData");
        ValidateValues(context, CultureData.Marketing, "marketingData");
        ValidateValues(context, CultureData.Fashion, "fashionData");
        ValidateValues(
            context,
            CultureData.Construction,
            "constructionData");
        ValidateValues(context, CultureData.Appliances, "applianceData");
        ValidateValues(context, CultureData.Weather, "cultureWeatherData");
        ValidateValues(context, CultureData.Military, "militaryData");
        ValidateValues(
            context,
            CultureData.NatoAlphabet,
            "natoAlphabetData");
        ValidateValues(context, CultureData.Hacker, "hackerData");
        ValidateValues(context, CultureData.Programming, "programmingData");
    }

    static void ValidateMusicData(GenerationContext context)
    {
        if (CultureData.MusicArtists.Length != CultureData.MusicAlbums.Length ||
            CultureData.MusicArtists.Length != CultureData.MusicSongs.Length ||
            CultureData.MusicArtists.Length != CultureData.MusicGenres.Length)
        {
            throw FakeException.Create(
                "The project-authored music table has inconsistent column lengths.");
        }
        ValidateValues(context, CultureData.MusicArtists, "musicData");
        ValidateValues(context, CultureData.MusicAlbums, "musicData");
        ValidateValues(context, CultureData.MusicSongs, "musicData");
        ValidateValues(context, CultureData.MusicGenres, "musicData");
    }

    static void ValidateSportData(GenerationContext context)
    {
        if (CultureData.Sports.Length != CultureData.Teams.Length ||
            CultureData.Sports.Length != CultureData.Positions.Length)
        {
            throw FakeException.Create(
                "The project-authored sport table has inconsistent column lengths.");
        }
        ValidateValues(context, CultureData.Sports, "sportData");
        ValidateValues(context, CultureData.Teams, "sportData");
        ValidateValues(context, CultureData.Positions, "sportData");
    }

    static void ValidateValues(
        GenerationContext context,
        string[] values,
        string dataName)
    {
        Guard.Count(
            values.Length,
            context.CapturedOptions.MaximumCount,
            dataName);
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null || values[index].Length == 0)
            {
                throw FakeException.Create(
                    "A project-authored culture data table is malformed.");
            }
            index = index + 1;
        }
    }

    static int MaximumRecordSize()
    {
        return MaximumLength(CultureData.MusicArtists) +
            MaximumLength(CultureData.MusicAlbums) +
            MaximumLength(CultureData.MusicSongs) +
            MaximumLength(CultureData.MusicGenres) +
            MaximumLength(CultureData.Art) +
            MaximumLength(CultureData.Hobbies) +
            MaximumLength(CultureData.Sports) +
            MaximumLength(CultureData.Teams) +
            MaximumLength(CultureData.Positions) +
            MaximumLength(CultureData.Gaming) +
            MaximumLength(CultureData.Marketing) +
            MaximumLength(CultureData.Fashion) +
            MaximumLength(CultureData.Construction) +
            MaximumLength(CultureData.Appliances) +
            MaximumLength(CultureData.Weather) +
            MaximumLength(CultureData.Military) +
            MaximumLength(CultureData.NatoAlphabet) +
            MaximumLength(CultureData.Hacker) +
            MaximumLength(CultureData.Programming);
    }

    static int MaximumLength(string[] values)
    {
        int maximum = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Length > maximum)
            {
                maximum = values[index].Length;
            }
            index = index + 1;
        }
        return maximum;
    }
}
