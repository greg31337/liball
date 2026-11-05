using fake;

namespace fake.Tests;

[TestFixture]
public class MediaProviderConformanceTests
{
    static string[] Adjectives = new string[]
    {
        "Amber", "Distant", "Gentle", "Hidden",
        "Open", "Quiet", "Silver", "Turning"
    };

    static string[] Subjects = new string[]
    {
        "Archive", "Bridge", "Garden", "Harbor",
        "Lantern", "Map", "Orchard", "Signal"
    };

    static string[] Developments = new string[]
    {
        "a careful review reveals a new question",
        "a routine decision changes the next step",
        "neighbors compare practical plans",
        "participants document a small discovery",
        "the group prepares for an ordinary challenge",
        "volunteers organize a shared response"
    };

    static string[] MovieGenres = new string[]
    {
        "adventure", "comedy", "drama", "family",
        "mystery", "science fiction"
    };

    static string[] TelevisionGenres = new string[]
    {
        "comedy", "documentary", "drama", "educational",
        "mystery", "science fiction"
    };

    static string[] PodcastCategories = new string[]
    {
        "arts", "education", "fiction", "history", "science", "society"
    };

    static string[] NewsCategories = new string[]
    {
        "community", "culture", "education", "environment",
        "science", "technology"
    };

    static string[] NewsSubjects = new string[]
    {
        "Community archive", "Garden group", "Harbor workshop",
        "Local library", "Neighborhood studio", "Orchard cooperative"
    };

    static string[] NewsActions = new string[]
    {
        "announces a public workshop", "begins a seasonal review",
        "hosts a planning session", "opens a volunteer project",
        "shares a progress update", "starts a neighborhood survey"
    };

    [Test]
    public void EveryScalarOperationUsesAuthoredGrammarAndCompleteCategories()
    {
        MediaProvider media = Fake.Create(12610).Media();
        HashSet<string> movieGenres = new HashSet<string>();
        HashSet<string> televisionGenres = new HashSet<string>();
        HashSet<string> podcastCategories = new HashSet<string>();
        HashSet<string> newsCategories = new HashSet<string>();

        int index = 0;
        while (index < 512)
        {
            string movieTitle = media.MovieTitle();
            string movieSummary = media.MovieSummary();
            string movieGenre = media.MovieGenre();
            string televisionSeriesTitle = media.TelevisionSeriesTitle();
            string televisionEpisodeTitle = media.TelevisionEpisodeTitle();
            string televisionSummary = media.TelevisionSummary();
            string televisionGenre = media.TelevisionGenre();
            string podcastTitle = media.PodcastTitle();
            string podcastEpisodeTitle = media.PodcastEpisodeTitle();
            string podcastSummary = media.PodcastSummary();
            string podcastCategory = media.PodcastCategory();
            string newsHeadline = media.NewsHeadline();
            string newsSummary = media.NewsSummary();
            string newsCategory = media.NewsCategory();

            Assert.Multiple(() =>
            {
                Assert.That(movieTitle, Does.Match(
                    "^(Amber|Distant|Gentle|Hidden|Open|Quiet|Silver|Turning) " +
                    "(Archive|Bridge|Garden|Harbor|Lantern|Map|Orchard|Signal)$"));
                Assert.That(movieSummary,
                    Does.StartWith("A fictional ensemble explores the ")
                        .And.EndWith("."));
                AssertContainsSubjectAndDevelopment(movieSummary, Subjects);
                Assert.That(movieGenre, Is.AnyOf(MovieGenres));
                Assert.That(televisionSeriesTitle, Does.Match(
                    "^The (Archive|Bridge|Garden|Harbor|Lantern|Map|Orchard|Signal) Desk$"));
                Assert.That(televisionEpisodeTitle, Does.Match(
                    "^The (Amber|Distant|Gentle|Hidden|Open|Quiet|Silver|Turning) " +
                    "(Archive|Bridge|Garden|Harbor|Lantern|Map|Orchard|Signal)$"));
                Assert.That(televisionSummary,
                    Does.StartWith("The fictional team at The ").And.EndWith("."));
                AssertContainsSubjectAndDevelopment(televisionSummary, Subjects);
                Assert.That(televisionGenre, Is.AnyOf(TelevisionGenres));
                Assert.That(podcastTitle, Does.Match(
                    "^(Archive|Bridge|Garden|Harbor|Lantern|Map|Orchard|Signal) Notes$"));
                Assert.That(podcastEpisodeTitle, Does.Match(
                    "^(Amber|Distant|Gentle|Hidden|Open|Quiet|Silver|Turning) " +
                    "(Archive|Bridge|Garden|Harbor|Lantern|Map|Orchard|Signal) Conversation$"));
                Assert.That(podcastSummary,
                    Does.StartWith("A fictional host discusses the ").And.EndWith("."));
                AssertContainsSubjectAndDevelopment(podcastSummary, Subjects);
                Assert.That(podcastCategory, Is.AnyOf(PodcastCategories));
                AssertHeadline(newsHeadline);
                Assert.That(newsSummary,
                    Does.StartWith("This synthetic bulletin describes how the ")
                        .And.EndWith("."));
                AssertContainsSubjectAndDevelopment(newsSummary, NewsSubjects);
                Assert.That(newsCategory, Is.AnyOf(NewsCategories));
            });
            movieGenres.Add(movieGenre);
            televisionGenres.Add(televisionGenre);
            podcastCategories.Add(podcastCategory);
            newsCategories.Add(newsCategory);
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(movieGenres, Is.EquivalentTo(MovieGenres));
            Assert.That(televisionGenres, Is.EquivalentTo(TelevisionGenres));
            Assert.That(podcastCategories, Is.EquivalentTo(PodcastCategories));
            Assert.That(newsCategories, Is.EquivalentTo(NewsCategories));
        });
    }

    [Test]
    public void EveryRecordHasNonemptyBoundedTextAndSyntheticMarkers()
    {
        const int maximumOutputSize = 256;
        FakeOptions options = FakeOptions.Create("en-US", 12611);
        options.MaximumOutputSize = maximumOutputSize;
        MediaProvider media = Fake.Create(options).Media();

        int index = 0;
        while (index < 128)
        {
            MovieRecord movie = media.Movie();
            TelevisionEpisodeRecord television = media.TelevisionEpisode();
            PodcastEpisodeRecord podcast = media.PodcastEpisode();
            NewsArticleRecord news = media.NewsArticle();

            Assert.Multiple(() =>
            {
                AssertMovieText(movie, maximumOutputSize);
                AssertTelevisionText(television, maximumOutputSize);
                AssertPodcastText(podcast, maximumOutputSize);
                AssertNewsText(news, maximumOutputSize);
                Assert.That(movie.TestMarker,
                    Is.EqualTo("fake.synthetic-movie.v1"));
                Assert.That(television.TestMarker,
                    Is.EqualTo("fake.synthetic-television.v1"));
                Assert.That(podcast.TestMarker,
                    Is.EqualTo("fake.synthetic-podcast.v1"));
                Assert.That(news.TestMarker,
                    Is.EqualTo("fake.synthetic-news.v1"));
                Assert.That(movie.IsSynthetic, Is.True);
                Assert.That(television.IsSynthetic, Is.True);
                Assert.That(podcast.IsSynthetic, Is.True);
                Assert.That(news.IsSynthetic, Is.True);
                Assert.That(movie.UsesProjectAuthoredText, Is.True);
                Assert.That(television.UsesProjectAuthoredText, Is.True);
                Assert.That(podcast.UsesProjectAuthoredText, Is.True);
                Assert.That(news.UsesProjectAuthoredText, Is.True);
                Assert.That(movie.RepresentsPublishedWork, Is.False);
                Assert.That(television.RepresentsPublishedWork, Is.False);
                Assert.That(podcast.RepresentsPublishedWork, Is.False);
                Assert.That(news.RepresentsPublishedWork, Is.False);
                Assert.That(movie.RepresentsReleasedMovie, Is.False);
                Assert.That(television.RepresentsBroadcastEpisode, Is.False);
                Assert.That(podcast.RepresentsPublishedEpisode, Is.False);
                Assert.That(news.RepresentsReportedEvent, Is.False);
            });
            index = index + 1;
        }
    }

    [Test]
    public void EveryRecordOperationIsFieldForFieldDeterministic()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2045, 8, 9, 10, 11, 12, TimeSpan.FromHours(-4));
        FakeOptions firstOptions = FakeOptions.Create("fr-CA", 12612);
        firstOptions.ReferenceTime = reference;
        FakeOptions secondOptions = FakeOptions.Create("fr-CA", 12612);
        secondOptions.ReferenceTime = reference;
        MediaProvider first = Fake.Create(firstOptions).Media();
        MediaProvider second = Fake.Create(secondOptions).Media();

        int index = 0;
        while (index < 128)
        {
            MovieRecord firstMovie = first.Movie();
            MovieRecord secondMovie = second.Movie();
            TelevisionEpisodeRecord firstTelevision = first.TelevisionEpisode();
            TelevisionEpisodeRecord secondTelevision = second.TelevisionEpisode();
            PodcastEpisodeRecord firstPodcast = first.PodcastEpisode();
            PodcastEpisodeRecord secondPodcast = second.PodcastEpisode();
            NewsArticleRecord firstNews = first.NewsArticle();
            NewsArticleRecord secondNews = second.NewsArticle();

            Assert.Multiple(() =>
            {
                AssertMovieEquivalent(firstMovie, secondMovie);
                AssertTelevisionEquivalent(firstTelevision, secondTelevision);
                AssertPodcastEquivalent(firstPodcast, secondPodcast);
                AssertNewsEquivalent(firstNews, secondNews);
                Assert.That(secondMovie, Is.Not.SameAs(firstMovie));
                Assert.That(secondTelevision, Is.Not.SameAs(firstTelevision));
                Assert.That(secondPodcast, Is.Not.SameAs(firstPodcast));
                Assert.That(secondNews, Is.Not.SameAs(firstNews));
            });
            index = index + 1;
        }
    }

    [TestCase("de-AT")]
    [TestCase("fr-CA")]
    [TestCase("ja-JP")]
    [TestCase("ar-EG")]
    public void InvariantMediaVocabularyFallsBackForEveryOperation(string locale)
    {
        const long seed = 12613;
        MediaProvider baseline = Fake.Create("en-US", seed).Media();
        MediaProvider localized = Fake.Create(locale, seed).Media();

        Assert.Multiple(() =>
        {
            Assert.That(localized.MovieTitle(), Is.EqualTo(baseline.MovieTitle()));
            Assert.That(localized.MovieSummary(), Is.EqualTo(baseline.MovieSummary()));
            Assert.That(localized.MovieGenre(), Is.EqualTo(baseline.MovieGenre()));
            Assert.That(localized.TelevisionSeriesTitle(),
                Is.EqualTo(baseline.TelevisionSeriesTitle()));
            Assert.That(localized.TelevisionEpisodeTitle(),
                Is.EqualTo(baseline.TelevisionEpisodeTitle()));
            Assert.That(localized.TelevisionSummary(),
                Is.EqualTo(baseline.TelevisionSummary()));
            Assert.That(localized.TelevisionGenre(),
                Is.EqualTo(baseline.TelevisionGenre()));
            Assert.That(localized.PodcastTitle(),
                Is.EqualTo(baseline.PodcastTitle()));
            Assert.That(localized.PodcastEpisodeTitle(),
                Is.EqualTo(baseline.PodcastEpisodeTitle()));
            Assert.That(localized.PodcastSummary(),
                Is.EqualTo(baseline.PodcastSummary()));
            Assert.That(localized.PodcastCategory(),
                Is.EqualTo(baseline.PodcastCategory()));
            Assert.That(localized.NewsHeadline(),
                Is.EqualTo(baseline.NewsHeadline()));
            Assert.That(localized.NewsSummary(),
                Is.EqualTo(baseline.NewsSummary()));
            Assert.That(localized.NewsCategory(),
                Is.EqualTo(baseline.NewsCategory()));
        });

        MovieRecord expectedMovie = baseline.Movie();
        MovieRecord actualMovie = localized.Movie();
        TelevisionEpisodeRecord expectedTelevision = baseline.TelevisionEpisode();
        TelevisionEpisodeRecord actualTelevision = localized.TelevisionEpisode();
        PodcastEpisodeRecord expectedPodcast = baseline.PodcastEpisode();
        PodcastEpisodeRecord actualPodcast = localized.PodcastEpisode();
        NewsArticleRecord expectedNews = baseline.NewsArticle();
        NewsArticleRecord actualNews = localized.NewsArticle();

        Assert.Multiple(() =>
        {
            AssertMovieContentEquivalent(expectedMovie, actualMovie);
            AssertTelevisionContentEquivalent(expectedTelevision, actualTelevision);
            AssertPodcastContentEquivalent(expectedPodcast, actualPodcast);
            AssertNewsContentEquivalent(expectedNews, actualNews);
            Assert.That(actualMovie.Locale, Is.EqualTo(locale));
            Assert.That(actualTelevision.Locale, Is.EqualTo(locale));
            Assert.That(actualPodcast.Locale, Is.EqualTo(locale));
            Assert.That(actualNews.Locale, Is.EqualTo(locale));
            Assert.That(actualMovie.VocabularyLocale, Is.EqualTo("invariant"));
            Assert.That(actualTelevision.VocabularyLocale, Is.EqualTo("invariant"));
            Assert.That(actualPodcast.VocabularyLocale, Is.EqualTo("invariant"));
            Assert.That(actualNews.VocabularyLocale, Is.EqualTo("invariant"));
        });
    }

    [Test]
    public void DatesAndEpisodeCodesRespectReferenceWindowsAndBoundaries()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2046, 9, 10, 11, 12, 13, TimeSpan.FromHours(5.5));
        FakeOptions options = FakeOptions.Create(12614);
        options.ReferenceTime = reference;
        MediaProvider media = Fake.Create(options).Media();

        int index = 0;
        while (index < 256)
        {
            MovieRecord movie = media.Movie();
            TelevisionEpisodeRecord television = media.TelevisionEpisode();
            PodcastEpisodeRecord podcast = media.PodcastEpisode();
            NewsArticleRecord news = media.NewsArticle();
            Assert.Multiple(() =>
            {
                AssertDateWindow(movie.ReleaseDate, reference, 14610.0);
                AssertDateWindow(television.AirDate, reference, 3652.0);
                AssertDateWindow(podcast.PublicationDate, reference, 1826.0);
                AssertDateWindow(news.PublishedAt, reference, 30.0);
                AssertEpisodeCode(
                    television.EpisodeCode,
                    television.SeasonNumber,
                    television.EpisodeNumber);
                AssertEpisodeCode(
                    podcast.EpisodeCode,
                    podcast.SeasonNumber,
                    podcast.EpisodeNumber);
            });
            index = index + 1;
        }

        FakeOptions minimumOptions = FakeOptions.Create(12615);
        minimumOptions.ReferenceTime = DateTimeOffset.MinValue;
        MediaProvider minimum = Fake.Create(minimumOptions).Media();
        FakeOptions maximumOptions = FakeOptions.Create(12615);
        maximumOptions.ReferenceTime = DateTimeOffset.MaxValue;
        MediaProvider maximum = Fake.Create(maximumOptions).Media();

        Assert.Multiple(() =>
        {
            Assert.That(minimum.Movie().ReleaseDate,
                Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(minimum.TelevisionEpisode().AirDate,
                Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(minimum.PodcastEpisode().PublicationDate,
                Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(minimum.NewsArticle().PublishedAt,
                Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(maximum.Movie().ReleaseDate,
                Is.LessThanOrEqualTo(DateTimeOffset.MaxValue));
            Assert.That(maximum.TelevisionEpisode().AirDate,
                Is.LessThanOrEqualTo(DateTimeOffset.MaxValue));
            Assert.That(maximum.PodcastEpisode().PublicationDate,
                Is.LessThanOrEqualTo(DateTimeOffset.MaxValue));
            Assert.That(maximum.NewsArticle().PublishedAt,
                Is.LessThanOrEqualTo(DateTimeOffset.MaxValue));
        });
    }

    [Test]
    public void EveryTextOperationHonorsOutputLimitsBeforeEntropy()
    {
        const long seed = 12616;
        FakeOptions options = FakeOptions.Create(seed);
        options.MaximumOutputSize = 1;
        MediaProvider limited = Fake.Create(options).Media();
        MediaProvider untouched = Fake.Create(options).Media();
        Action[] operations = new Action[]
        {
            () => limited.MovieTitle(),
            () => limited.MovieSummary(),
            () => limited.MovieGenre(),
            () => limited.TelevisionSeriesTitle(),
            () => limited.TelevisionEpisodeTitle(),
            () => limited.TelevisionSummary(),
            () => limited.TelevisionGenre(),
            () => limited.PodcastTitle(),
            () => limited.PodcastEpisodeTitle(),
            () => limited.PodcastSummary(),
            () => limited.PodcastCategory(),
            () => limited.NewsHeadline(),
            () => limited.NewsSummary(),
            () => limited.NewsCategory(),
            () => limited.Movie(),
            () => limited.TelevisionEpisode(),
            () => limited.PodcastEpisode(),
            () => limited.NewsArticle()
        };

        int index = 0;
        while (index < operations.Length)
        {
            Assert.That(operations[index],
                Throws.TypeOf<ArgumentOutOfRangeException>());
            index = index + 1;
        }

        Assert.That(limited.RandomValue.Long(),
            Is.EqualTo(untouched.RandomValue.Long()));
    }

    [Test]
    public void AuthoredMediaNeverMatchesNonReproductionFixtures()
    {
        string[] forbidden = new string[]
        {
            "Star Wars", "The Godfather", "Game of Thrones", "Breaking Bad",
            "Serial", "The Daily", "Disney", "Netflix", "BBC", "CNN",
            "NPR", "Reuters"
        };
        MediaProvider media = Fake.Create(12617).Media();

        int index = 0;
        while (index < 1024)
        {
            MovieRecord movie = media.Movie();
            TelevisionEpisodeRecord television = media.TelevisionEpisode();
            PodcastEpisodeRecord podcast = media.PodcastEpisode();
            NewsArticleRecord news = media.NewsArticle();
            AssertNoForbiddenFixture(movie.Title, forbidden);
            AssertNoForbiddenFixture(movie.Summary, forbidden);
            AssertNoForbiddenFixture(movie.Studio, forbidden);
            AssertNoForbiddenFixture(television.SeriesTitle, forbidden);
            AssertNoForbiddenFixture(television.EpisodeTitle, forbidden);
            AssertNoForbiddenFixture(television.Summary, forbidden);
            AssertNoForbiddenFixture(television.Network, forbidden);
            AssertNoForbiddenFixture(podcast.PodcastTitle, forbidden);
            AssertNoForbiddenFixture(podcast.EpisodeTitle, forbidden);
            AssertNoForbiddenFixture(podcast.Summary, forbidden);
            AssertNoForbiddenFixture(podcast.Publisher, forbidden);
            AssertNoForbiddenFixture(news.Headline, forbidden);
            AssertNoForbiddenFixture(news.Summary, forbidden);
            AssertNoForbiddenFixture(news.Outlet, forbidden);
            index = index + 1;
        }
    }

    static void AssertContainsSubjectAndDevelopment(
        string value,
        string[] subjects)
    {
        string lower = value.ToLowerInvariant();
        bool foundSubject = false;
        bool foundDevelopment = false;
        int index = 0;
        while (index < subjects.Length)
        {
            if (lower.Contains(subjects[index].ToLowerInvariant(),
                    StringComparison.Ordinal))
            {
                foundSubject = true;
            }
            index = index + 1;
        }
        index = 0;
        while (index < Developments.Length)
        {
            if (lower.Contains(Developments[index], StringComparison.Ordinal))
            {
                foundDevelopment = true;
            }
            index = index + 1;
        }
        Assert.Multiple(() =>
        {
            Assert.That(foundSubject, Is.True);
            Assert.That(foundDevelopment, Is.True);
        });
    }

    static void AssertHeadline(string value)
    {
        bool found = false;
        int subjectIndex = 0;
        while (subjectIndex < NewsSubjects.Length)
        {
            int actionIndex = 0;
            while (actionIndex < NewsActions.Length)
            {
                if (value == NewsSubjects[subjectIndex] + " " +
                    NewsActions[actionIndex])
                {
                    found = true;
                }
                actionIndex = actionIndex + 1;
            }
            subjectIndex = subjectIndex + 1;
        }
        Assert.That(found, Is.True);
    }

    static void AssertMovieText(MovieRecord value, int maximumOutputSize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(value.Locale, Is.Not.Empty);
            Assert.That(value.VocabularyLocale, Is.Not.Empty);
            Assert.That(value.Title, Is.Not.Empty);
            Assert.That(value.Summary, Is.Not.Empty);
            Assert.That(value.Genre, Is.Not.Empty);
            Assert.That(value.Studio, Is.Not.Empty);
            Assert.That(MovieTextSize(value), Is.LessThanOrEqualTo(maximumOutputSize));
        });
    }

    static void AssertTelevisionText(
        TelevisionEpisodeRecord value,
        int maximumOutputSize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(value.Locale, Is.Not.Empty);
            Assert.That(value.VocabularyLocale, Is.Not.Empty);
            Assert.That(value.SeriesTitle, Is.Not.Empty);
            Assert.That(value.EpisodeTitle, Is.Not.Empty);
            Assert.That(value.Summary, Is.Not.Empty);
            Assert.That(value.Genre, Is.Not.Empty);
            Assert.That(value.Network, Is.Not.Empty);
            Assert.That(value.EpisodeCode, Is.Not.Empty);
            Assert.That(TelevisionTextSize(value),
                Is.LessThanOrEqualTo(maximumOutputSize));
        });
    }

    static void AssertPodcastText(
        PodcastEpisodeRecord value,
        int maximumOutputSize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(value.Locale, Is.Not.Empty);
            Assert.That(value.VocabularyLocale, Is.Not.Empty);
            Assert.That(value.PodcastTitle, Is.Not.Empty);
            Assert.That(value.EpisodeTitle, Is.Not.Empty);
            Assert.That(value.Summary, Is.Not.Empty);
            Assert.That(value.Category, Is.Not.Empty);
            Assert.That(value.Publisher, Is.Not.Empty);
            Assert.That(value.Host, Is.Not.Empty);
            Assert.That(value.EpisodeCode, Is.Not.Empty);
            Assert.That(PodcastTextSize(value),
                Is.LessThanOrEqualTo(maximumOutputSize));
        });
    }

    static void AssertNewsText(NewsArticleRecord value, int maximumOutputSize)
    {
        Assert.Multiple(() =>
        {
            Assert.That(value.Locale, Is.Not.Empty);
            Assert.That(value.VocabularyLocale, Is.Not.Empty);
            Assert.That(value.Headline, Is.Not.Empty);
            Assert.That(value.Summary, Is.Not.Empty);
            Assert.That(value.Category, Is.Not.Empty);
            Assert.That(value.Outlet, Is.Not.Empty);
            Assert.That(value.Byline, Is.Not.Empty);
            Assert.That(NewsTextSize(value),
                Is.LessThanOrEqualTo(maximumOutputSize));
        });
    }

    static int MovieTextSize(MovieRecord value)
    {
        return value.Locale.Length + value.VocabularyLocale.Length +
            value.Title.Length + value.Summary.Length + value.Genre.Length +
            value.Studio.Length + value.TestMarker.Length;
    }

    static int TelevisionTextSize(TelevisionEpisodeRecord value)
    {
        return value.Locale.Length + value.VocabularyLocale.Length +
            value.SeriesTitle.Length + value.EpisodeTitle.Length +
            value.Summary.Length + value.Genre.Length + value.Network.Length +
            value.EpisodeCode.Length + value.TestMarker.Length;
    }

    static int PodcastTextSize(PodcastEpisodeRecord value)
    {
        return value.Locale.Length + value.VocabularyLocale.Length +
            value.PodcastTitle.Length + value.EpisodeTitle.Length +
            value.Summary.Length + value.Category.Length + value.Publisher.Length +
            value.Host.Length + value.EpisodeCode.Length + value.TestMarker.Length;
    }

    static int NewsTextSize(NewsArticleRecord value)
    {
        return value.Locale.Length + value.VocabularyLocale.Length +
            value.Headline.Length + value.Summary.Length + value.Category.Length +
            value.Outlet.Length + value.Byline.Length + value.TestMarker.Length;
    }

    static void AssertMovieEquivalent(MovieRecord expected, MovieRecord actual)
    {
        AssertMovieContentEquivalent(expected, actual);
        Assert.Multiple(() =>
        {
            Assert.That(actual.Locale, Is.EqualTo(expected.Locale));
            Assert.That(actual.VocabularyLocale,
                Is.EqualTo(expected.VocabularyLocale));
            Assert.That(actual.IsSynthetic, Is.EqualTo(expected.IsSynthetic));
            Assert.That(actual.TestMarker, Is.EqualTo(expected.TestMarker));
            Assert.That(actual.UsesProjectAuthoredText,
                Is.EqualTo(expected.UsesProjectAuthoredText));
            Assert.That(actual.RepresentsPublishedWork,
                Is.EqualTo(expected.RepresentsPublishedWork));
            Assert.That(actual.RepresentsReleasedMovie,
                Is.EqualTo(expected.RepresentsReleasedMovie));
        });
    }

    static void AssertMovieContentEquivalent(MovieRecord expected, MovieRecord actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Title, Is.EqualTo(expected.Title));
            Assert.That(actual.Summary, Is.EqualTo(expected.Summary));
            Assert.That(actual.Genre, Is.EqualTo(expected.Genre));
            Assert.That(actual.Studio, Is.EqualTo(expected.Studio));
            Assert.That(actual.RuntimeMinutes, Is.EqualTo(expected.RuntimeMinutes));
            Assert.That(actual.ReleaseDate, Is.EqualTo(expected.ReleaseDate));
        });
    }

    static void AssertTelevisionEquivalent(
        TelevisionEpisodeRecord expected,
        TelevisionEpisodeRecord actual)
    {
        AssertTelevisionContentEquivalent(expected, actual);
        Assert.Multiple(() =>
        {
            Assert.That(actual.Locale, Is.EqualTo(expected.Locale));
            Assert.That(actual.VocabularyLocale,
                Is.EqualTo(expected.VocabularyLocale));
            Assert.That(actual.IsSynthetic, Is.EqualTo(expected.IsSynthetic));
            Assert.That(actual.TestMarker, Is.EqualTo(expected.TestMarker));
            Assert.That(actual.UsesProjectAuthoredText,
                Is.EqualTo(expected.UsesProjectAuthoredText));
            Assert.That(actual.RepresentsPublishedWork,
                Is.EqualTo(expected.RepresentsPublishedWork));
            Assert.That(actual.RepresentsBroadcastEpisode,
                Is.EqualTo(expected.RepresentsBroadcastEpisode));
        });
    }

    static void AssertTelevisionContentEquivalent(
        TelevisionEpisodeRecord expected,
        TelevisionEpisodeRecord actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.SeriesTitle, Is.EqualTo(expected.SeriesTitle));
            Assert.That(actual.EpisodeTitle, Is.EqualTo(expected.EpisodeTitle));
            Assert.That(actual.Summary, Is.EqualTo(expected.Summary));
            Assert.That(actual.Genre, Is.EqualTo(expected.Genre));
            Assert.That(actual.Network, Is.EqualTo(expected.Network));
            Assert.That(actual.SeasonNumber, Is.EqualTo(expected.SeasonNumber));
            Assert.That(actual.EpisodeNumber, Is.EqualTo(expected.EpisodeNumber));
            Assert.That(actual.EpisodeCode, Is.EqualTo(expected.EpisodeCode));
            Assert.That(actual.DurationMinutes,
                Is.EqualTo(expected.DurationMinutes));
            Assert.That(actual.AirDate, Is.EqualTo(expected.AirDate));
        });
    }

    static void AssertPodcastEquivalent(
        PodcastEpisodeRecord expected,
        PodcastEpisodeRecord actual)
    {
        AssertPodcastContentEquivalent(expected, actual);
        Assert.Multiple(() =>
        {
            Assert.That(actual.Locale, Is.EqualTo(expected.Locale));
            Assert.That(actual.VocabularyLocale,
                Is.EqualTo(expected.VocabularyLocale));
            Assert.That(actual.IsSynthetic, Is.EqualTo(expected.IsSynthetic));
            Assert.That(actual.TestMarker, Is.EqualTo(expected.TestMarker));
            Assert.That(actual.UsesProjectAuthoredText,
                Is.EqualTo(expected.UsesProjectAuthoredText));
            Assert.That(actual.RepresentsPublishedWork,
                Is.EqualTo(expected.RepresentsPublishedWork));
            Assert.That(actual.RepresentsPublishedEpisode,
                Is.EqualTo(expected.RepresentsPublishedEpisode));
        });
    }

    static void AssertPodcastContentEquivalent(
        PodcastEpisodeRecord expected,
        PodcastEpisodeRecord actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.PodcastTitle, Is.EqualTo(expected.PodcastTitle));
            Assert.That(actual.EpisodeTitle, Is.EqualTo(expected.EpisodeTitle));
            Assert.That(actual.Summary, Is.EqualTo(expected.Summary));
            Assert.That(actual.Category, Is.EqualTo(expected.Category));
            Assert.That(actual.Publisher, Is.EqualTo(expected.Publisher));
            Assert.That(actual.Host, Is.EqualTo(expected.Host));
            Assert.That(actual.SeasonNumber, Is.EqualTo(expected.SeasonNumber));
            Assert.That(actual.EpisodeNumber, Is.EqualTo(expected.EpisodeNumber));
            Assert.That(actual.EpisodeCode, Is.EqualTo(expected.EpisodeCode));
            Assert.That(actual.DurationMinutes,
                Is.EqualTo(expected.DurationMinutes));
            Assert.That(actual.PublicationDate,
                Is.EqualTo(expected.PublicationDate));
        });
    }

    static void AssertNewsEquivalent(
        NewsArticleRecord expected,
        NewsArticleRecord actual)
    {
        AssertNewsContentEquivalent(expected, actual);
        Assert.Multiple(() =>
        {
            Assert.That(actual.Locale, Is.EqualTo(expected.Locale));
            Assert.That(actual.VocabularyLocale,
                Is.EqualTo(expected.VocabularyLocale));
            Assert.That(actual.IsSynthetic, Is.EqualTo(expected.IsSynthetic));
            Assert.That(actual.TestMarker, Is.EqualTo(expected.TestMarker));
            Assert.That(actual.UsesProjectAuthoredText,
                Is.EqualTo(expected.UsesProjectAuthoredText));
            Assert.That(actual.RepresentsPublishedWork,
                Is.EqualTo(expected.RepresentsPublishedWork));
            Assert.That(actual.RepresentsReportedEvent,
                Is.EqualTo(expected.RepresentsReportedEvent));
        });
    }

    static void AssertNewsContentEquivalent(
        NewsArticleRecord expected,
        NewsArticleRecord actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Headline, Is.EqualTo(expected.Headline));
            Assert.That(actual.Summary, Is.EqualTo(expected.Summary));
            Assert.That(actual.Category, Is.EqualTo(expected.Category));
            Assert.That(actual.Outlet, Is.EqualTo(expected.Outlet));
            Assert.That(actual.Byline, Is.EqualTo(expected.Byline));
            Assert.That(actual.PublishedAt, Is.EqualTo(expected.PublishedAt));
        });
    }

    static void AssertDateWindow(
        DateTimeOffset value,
        DateTimeOffset reference,
        double windowDays)
    {
        Assert.Multiple(() =>
        {
            Assert.That(value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(value, Is.LessThanOrEqualTo(reference));
            Assert.That(value,
                Is.GreaterThanOrEqualTo(reference.AddDays(-windowDays)));
        });
    }

    static void AssertEpisodeCode(string value, int season, int episode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(season, Is.GreaterThan(0));
            Assert.That(episode, Is.GreaterThan(0));
            Assert.That(value, Is.EqualTo(
                "S" + season.ToString("D2") + "E" + episode.ToString("D2")));
        });
    }

    static void AssertNoForbiddenFixture(string value, string[] forbidden)
    {
        int index = 0;
        while (index < forbidden.Length)
        {
            Assert.That(value.Contains(
                forbidden[index],
                StringComparison.OrdinalIgnoreCase), Is.False);
            index = index + 1;
        }
    }
}
