using fake;

namespace fake.Tests;

[TestFixture]
public class CultureProviderTests
{
    static Dictionary<string, string> Albums = new Dictionary<string, string>
    {
        ["Aster Example Ensemble"] = "Sample Signals",
        ["Beacon Fixture Trio"] = "Demo Sketches",
        ["Cedar Test Chorus"] = "Fixture Seasons",
        ["Harbor Sample Quartet"] = "Example Tides",
        ["Northstar Demo Band"] = "Test Horizons",
        ["Willow Mock Project"] = "Placeholder Rhythms"
    };

    static Dictionary<string, string> Songs = new Dictionary<string, string>
    {
        ["Aster Example Ensemble"] = "Quiet Circuit",
        ["Beacon Fixture Trio"] = "Open Window",
        ["Cedar Test Chorus"] = "Morning Pattern",
        ["Harbor Sample Quartet"] = "Silver Current",
        ["Northstar Demo Band"] = "Distant Light",
        ["Willow Mock Project"] = "Gentle Motion"
    };

    static Dictionary<string, string> Genres = new Dictionary<string, string>
    {
        ["Aster Example Ensemble"] = "electronic",
        ["Beacon Fixture Trio"] = "jazz",
        ["Cedar Test Chorus"] = "folk",
        ["Harbor Sample Quartet"] = "classical",
        ["Northstar Demo Band"] = "rock",
        ["Willow Mock Project"] = "ambient"
    };

    static Dictionary<string, string> Teams = new Dictionary<string, string>
    {
        ["basketball"] = "Example City Comets",
        ["baseball"] = "Fixture Field Foxes",
        ["football"] = "Sample Bay Sparks",
        ["hockey"] = "Test Harbor Owls",
        ["soccer"] = "Demo Ridge Rovers",
        ["volleyball"] = "Mock Valley Waves"
    };

    static Dictionary<string, string> Positions =
        new Dictionary<string, string>
        {
            ["basketball"] = "guard",
            ["baseball"] = "shortstop",
            ["football"] = "quarterback",
            ["hockey"] = "center",
            ["soccer"] = "midfielder",
            ["volleyball"] = "setter"
        };

    static string[] Art = new string[]
    {
        "collage", "drawing", "painting", "photography", "printmaking",
        "sculpture"
    };

    static string[] Hobbies = new string[]
    {
        "birdwatching", "gardening", "knitting", "model building",
        "photography", "woodworking"
    };

    static string[] Gaming = new string[]
    {
        "board game", "cooperative game", "puzzle game", "role-playing game",
        "simulation game", "strategy game"
    };

    static string[] Marketing = new string[]
    {
        "audience segment", "brand voice", "campaign brief",
        "content calendar", "market research", "product positioning"
    };

    static string[] Fashion = new string[]
    {
        "accessory", "footwear", "outerwear", "sportswear", "tailoring",
        "textile"
    };

    static string[] Construction = new string[]
    {
        "blueprint", "foundation", "framing", "insulation", "masonry",
        "roofing"
    };

    static string[] Appliances = new string[]
    {
        "blender", "dishwasher", "fan", "microwave", "refrigerator",
        "toaster"
    };

    static string[] Weather = new string[]
    {
        "clear skies", "cloud cover", "fog", "rain", "snow", "wind"
    };

    static string[] Military = new string[]
    {
        "administration", "communications", "engineering", "logistics",
        "medical support", "training"
    };

    static string[] NatoAlphabet = new string[]
    {
        "Alfa", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf",
        "Hotel", "India", "Juliett", "Kilo", "Lima", "Mike", "November",
        "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform",
        "Victor", "Whiskey", "X-ray", "Yankee", "Zulu"
    };

    static string[] Hacker = new string[]
    {
        "access control", "audit log", "patching", "sandbox",
        "threat model", "vulnerability disclosure"
    };

    static string[] Programming = new string[]
    {
        "algorithm", "compiler", "debugger", "function", "library",
        "runtime", "test"
    };

    [Test]
    public void MusicFixturesAndAliasesAreCorrelatedAndDeterministic()
    {
        int seed = 13000;
        while (seed < 13100)
        {
            string artist = Fake.Create(seed).Culture().MusicArtist();
            string album = Fake.Create(seed).Culture().MusicAlbum();
            string song = Fake.Create(seed).Culture().MusicSong();
            string genre = Fake.Create(seed).Culture().MusicGenre();

            Assert.Multiple(() =>
            {
                Assert.That(Albums.ContainsKey(artist), Is.True);
                Assert.That(album, Is.EqualTo(Albums[artist]));
                Assert.That(song, Is.EqualTo(Songs[artist]));
                Assert.That(genre, Is.EqualTo(Genres[artist]));
                Assert.That(Fake.Create(seed).Culture().Artist(),
                    Is.EqualTo(artist));
                Assert.That(Fake.Create(seed).Culture().Album(),
                    Is.EqualTo(album));
                Assert.That(Fake.Create(seed).Culture().Song(),
                    Is.EqualTo(song));
                Assert.That(Fake.Create(seed).Culture().Genre(),
                    Is.EqualTo(genre));
            });
            seed++;
        }
    }

    [Test]
    public void SportTeamAndPositionFixturesAreCorrelated()
    {
        int seed = 13001;
        while (seed < 13101)
        {
            string sport = Fake.Create(seed).Culture().Sport();
            string team = Fake.Create(seed).Culture().Team();
            string position = Fake.Create(seed).Culture().Position();

            Assert.Multiple(() =>
            {
                Assert.That(Teams.ContainsKey(sport), Is.True);
                Assert.That(team, Is.EqualTo(Teams[sport]));
                Assert.That(position, Is.EqualTo(Positions[sport]));
                Assert.That(Fake.Create(seed).Culture().Sport(),
                    Is.EqualTo(sport));
                Assert.That(Fake.Create(seed).Culture().Team(),
                    Is.EqualTo(team));
                Assert.That(Fake.Create(seed).Culture().Position(),
                    Is.EqualTo(position));
            });
            seed++;
        }
    }

    [Test]
    public void GeneralCultureCategoriesUseReviewedVocabulary()
    {
        AssertCategory(
            value => Fake.Create(value).Culture().Art(),
            value => Fake.Create(value).Culture().Art(),
            Art);
        AssertCategory(
            value => Fake.Create(value).Culture().Hobby(),
            value => Fake.Create(value).Culture().Hobby(),
            Hobbies);
        AssertCategory(
            value => Fake.Create(value).Culture().Gaming(),
            value => Fake.Create(value).Culture().Gaming(),
            Gaming);
        AssertCategory(
            value => Fake.Create(value).Culture().Marketing(),
            value => Fake.Create(value).Culture().Marketing(),
            Marketing);
        AssertCategory(
            value => Fake.Create(value).Culture().Fashion(),
            value => Fake.Create(value).Culture().Fashion(),
            Fashion);
        AssertCategory(
            value => Fake.Create(value).Culture().Construction(),
            value => Fake.Create(value).Culture().Construction(),
            Construction);
        AssertCategory(
            value => Fake.Create(value).Culture().Appliance(),
            value => Fake.Create(value).Culture().Appliance(),
            Appliances);
        AssertCategory(
            value => Fake.Create(value).Culture().Weather(),
            value => Fake.Create(value).Culture().Weather(),
            Weather);
        AssertCategory(
            value => Fake.Create(value).Culture().Military(),
            value => Fake.Create(value).Culture().Military(),
            Military);
        AssertCategory(
            value => Fake.Create(value).Culture().Hacker(),
            value => Fake.Create(value).Culture().Hacker(),
            Hacker);
        AssertCategory(
            value => Fake.Create(value).Culture().Programming(),
            value => Fake.Create(value).Culture().Programming(),
            Programming);
    }

    [Test]
    public void NatoAlphabetUsesTheCompleteStandardSpellingSet()
    {
        const long seed = 13003;
        CultureProvider first = Fake.Create(seed).Culture();
        CultureProvider second = Fake.Create(seed).Culture();
        HashSet<string> observed = new HashSet<string>();

        int index = 0;
        while (index < 512)
        {
            string value = first.NatoAlphabet();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(second.NatoAlphabet()));
                Assert.That(value, Is.AnyOf(NatoAlphabet));
            });
            observed.Add(value);
            index++;
        }

        Assert.That(observed, Is.EquivalentTo(NatoAlphabet));
    }

    [Test]
    public void CultureRecordsPreserveMusicSportAndSafetySemantics()
    {
        const long seed = 13004;
        CultureProvider first = Fake.Create(seed).Culture();
        CultureProvider second = Fake.Create(seed).Culture();

        int index = 0;
        while (index < 512)
        {
            CultureRecord value = first.Record();
            CultureRecord same = second.Record();

            Assert.Multiple(() =>
            {
                AssertRecordEqual(value, same);
                Assert.That(value.MusicAlbum,
                    Is.EqualTo(Albums[value.MusicArtist]));
                Assert.That(value.MusicSong,
                    Is.EqualTo(Songs[value.MusicArtist]));
                Assert.That(value.MusicGenre,
                    Is.EqualTo(Genres[value.MusicArtist]));
                Assert.That(value.Team, Is.EqualTo(Teams[value.Sport]));
                Assert.That(value.Position,
                    Is.EqualTo(Positions[value.Sport]));
                Assert.That(value.Hacker, Is.AnyOf(Hacker));
                Assert.That(value.Military, Is.AnyOf(Military));
                Assert.That(value.NatoAlphabet, Is.AnyOf(NatoAlphabet));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.RepresentsRealArtistOrTeam, Is.False);
                Assert.That(value.IsOperationalGuidance, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void LimitsFailBeforeCultureEntropy()
    {
        const long seed = 13005;
        FakeOptions tinyOptions = FakeOptions.Create(seed);
        tinyOptions.MaximumOutputSize = 1;
        CultureProvider tinyInspected = Fake.Create(tinyOptions).Culture();
        CultureProvider tinyUntouched = Fake.Create(tinyOptions).Culture();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 5;
        CultureProvider countInspected = Fake.Create(countOptions).Culture();
        CultureProvider countUntouched = Fake.Create(countOptions).Culture();

        Assert.Multiple(() =>
        {
            AssertAllCallsFail(tinyInspected);
            AssertAllCallsFail(countInspected);
            Assert.That(tinyInspected.RandomValue.Long(),
                Is.EqualTo(tinyUntouched.RandomValue.Long()));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
        });
    }

    [Test]
    public void CultureGenerationDoesNotPerturbOtherProviderStreams()
    {
        Fake inspected = Fake.Create(13006);
        Fake untouched = Fake.Create(13006);

        int index = 0;
        while (index < 32)
        {
            inspected.Culture().MusicArtist();
            inspected.Culture().MusicAlbum();
            inspected.Culture().MusicSong();
            inspected.Culture().MusicGenre();
            inspected.Culture().Art();
            inspected.Culture().Hobby();
            inspected.Culture().Sport();
            inspected.Culture().Team();
            inspected.Culture().Position();
            inspected.Culture().Gaming();
            inspected.Culture().Marketing();
            inspected.Culture().Fashion();
            inspected.Culture().Construction();
            inspected.Culture().Appliance();
            inspected.Culture().Weather();
            inspected.Culture().Military();
            inspected.Culture().NatoAlphabet();
            inspected.Culture().Hacker();
            inspected.Culture().Programming();
            inspected.Culture().Record();
            index++;
        }

        Assert.That(inspected.Person().FullName(),
            Is.EqualTo(untouched.Person().FullName()));
    }

    static void AssertCategory(
        Func<long, string> firstFactory,
        Func<long, string> secondFactory,
        string[] expected)
    {
        HashSet<string> observed = new HashSet<string>();
        long seed = 13000;
        while (seed < 13256)
        {
            string first = firstFactory(seed);
            string second = secondFactory(seed);
            Assert.Multiple(() =>
            {
                Assert.That(first, Is.EqualTo(second));
                Assert.That(first, Is.AnyOf(expected));
            });
            observed.Add(first);
            seed++;
        }
        Assert.That(observed, Is.EquivalentTo(expected));
    }

    static void AssertAllCallsFail(CultureProvider value)
    {
        Assert.That(() => value.MusicArtist(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.MusicAlbum(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.MusicSong(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.MusicGenre(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Art(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Hobby(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Sport(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Team(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Position(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Gaming(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Marketing(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Fashion(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Construction(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Appliance(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Weather(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Military(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.NatoAlphabet(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Hacker(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Programming(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => value.Record(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static void AssertRecordEqual(CultureRecord first, CultureRecord second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.MusicArtist, Is.EqualTo(second.MusicArtist));
            Assert.That(first.MusicAlbum, Is.EqualTo(second.MusicAlbum));
            Assert.That(first.MusicSong, Is.EqualTo(second.MusicSong));
            Assert.That(first.MusicGenre, Is.EqualTo(second.MusicGenre));
            Assert.That(first.Art, Is.EqualTo(second.Art));
            Assert.That(first.Hobby, Is.EqualTo(second.Hobby));
            Assert.That(first.Sport, Is.EqualTo(second.Sport));
            Assert.That(first.Team, Is.EqualTo(second.Team));
            Assert.That(first.Position, Is.EqualTo(second.Position));
            Assert.That(first.Gaming, Is.EqualTo(second.Gaming));
            Assert.That(first.Marketing, Is.EqualTo(second.Marketing));
            Assert.That(first.Fashion, Is.EqualTo(second.Fashion));
            Assert.That(first.Construction, Is.EqualTo(second.Construction));
            Assert.That(first.Appliance, Is.EqualTo(second.Appliance));
            Assert.That(first.Weather, Is.EqualTo(second.Weather));
            Assert.That(first.Military, Is.EqualTo(second.Military));
            Assert.That(first.NatoAlphabet,
                Is.EqualTo(second.NatoAlphabet));
            Assert.That(first.Hacker, Is.EqualTo(second.Hacker));
            Assert.That(first.Programming, Is.EqualTo(second.Programming));
            Assert.That(first.IsSynthetic, Is.EqualTo(second.IsSynthetic));
            Assert.That(first.RepresentsRealArtistOrTeam,
                Is.EqualTo(second.RepresentsRealArtistOrTeam));
            Assert.That(first.IsOperationalGuidance,
                Is.EqualTo(second.IsOperationalGuidance));
        });
    }
}
