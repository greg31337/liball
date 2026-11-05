using fake;

namespace fake.Tests;

[TestFixture]
public class MediaProviderTests
{
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

    [Test]
    public void ScalarOperationsAreDeterministicNonemptyCompositions()
    {
        const long seed = 12600;
        MediaProvider first = Fake.Create(seed).Media();
        MediaProvider second = Fake.Create(seed).Media();

        int index = 0;
        while (index < 128)
        {
            Assert.Multiple(() =>
            {
                Assert.That(first.MovieTitle(), Is.EqualTo(second.MovieTitle()).And.Not.Empty);
                Assert.That(first.MovieSummary(), Is.EqualTo(second.MovieSummary()).And.Not.Empty);
                Assert.That(first.MovieGenre(),
                    Is.EqualTo(second.MovieGenre()).And.AnyOf(MovieGenres));
                Assert.That(first.TelevisionSeriesTitle(),
                    Is.EqualTo(second.TelevisionSeriesTitle()).And.Not.Empty);
                Assert.That(first.TelevisionEpisodeTitle(),
                    Is.EqualTo(second.TelevisionEpisodeTitle()).And.Not.Empty);
                Assert.That(first.TelevisionSummary(),
                    Is.EqualTo(second.TelevisionSummary()).And.Not.Empty);
                Assert.That(first.TelevisionGenre(),
                    Is.EqualTo(second.TelevisionGenre()).And.AnyOf(TelevisionGenres));
                Assert.That(first.PodcastTitle(),
                    Is.EqualTo(second.PodcastTitle()).And.Not.Empty);
                Assert.That(first.PodcastEpisodeTitle(),
                    Is.EqualTo(second.PodcastEpisodeTitle()).And.Not.Empty);
                Assert.That(first.PodcastSummary(),
                    Is.EqualTo(second.PodcastSummary()).And.Not.Empty);
                Assert.That(first.PodcastCategory(),
                    Is.EqualTo(second.PodcastCategory()).And.AnyOf(PodcastCategories));
                Assert.That(first.NewsHeadline(),
                    Is.EqualTo(second.NewsHeadline()).And.Not.Empty);
                Assert.That(first.NewsSummary(),
                    Is.EqualTo(second.NewsSummary()).And.Not.Empty);
                Assert.That(first.NewsCategory(),
                    Is.EqualTo(second.NewsCategory()).And.AnyOf(NewsCategories));
            });
            index = index + 1;
        }
    }

    [Test]
    public void MovieRecordsCorrelateCompositionalTextDatesAndSafetyMetadata()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2042, 5, 6, 7, 8, 9, TimeSpan.FromHours(3));
        FakeOptions options = FakeOptions.Create("en-US", 12601);
        options.ReferenceTime = reference;
        MediaProvider media = Fake.Create(options).Media();

        int index = 0;
        while (index < 128)
        {
            MovieRecord value = media.Movie();
            string subject = value.Title.Substring(value.Title.IndexOf(' ') + 1);
            Assert.Multiple(() =>
            {
                Assert.That(value.Locale, Is.EqualTo("en-US"));
                Assert.That(value.VocabularyLocale, Is.EqualTo("invariant"));
                Assert.That(value.Summary.ToLowerInvariant(),
                    Does.Contain(subject.ToLowerInvariant()));
                Assert.That(value.Genre, Is.AnyOf(MovieGenres));
                Assert.That(value.Studio, Does.Contain("Example").Or.Contain("Fixture")
                    .Or.Contain("Sample").Or.Contain("Test"));
                Assert.That(value.RuntimeMinutes, Is.InRange(70, 180));
                Assert.That(value.ReleaseDate, Is.LessThanOrEqualTo(reference));
                Assert.That(value.ReleaseDate,
                    Is.GreaterThanOrEqualTo(reference.AddDays(-14610.0)));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.TestMarker, Is.EqualTo("fake.synthetic-movie.v1"));
                Assert.That(value.UsesProjectAuthoredText, Is.True);
                Assert.That(value.RepresentsPublishedWork, Is.False);
                Assert.That(value.RepresentsReleasedMovie, Is.False);
            });
            index = index + 1;
        }
    }

    [Test]
    public void TelevisionAndPodcastRecordsCorrelateEpisodeMetadata()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2043, 6, 7, 8, 9, 10, TimeSpan.Zero);
        FakeOptions options = FakeOptions.Create(12602);
        options.ReferenceTime = reference;
        MediaProvider media = Fake.Create(options).Media();

        int index = 0;
        while (index < 128)
        {
            TelevisionEpisodeRecord television = media.TelevisionEpisode();
            PodcastEpisodeRecord podcast = media.PodcastEpisode();
            string televisionSubject = television.SeriesTitle.Substring(
                "The ".Length,
                television.SeriesTitle.Length - "The ".Length - " Desk".Length);
            string podcastSubject = podcast.PodcastTitle.Substring(
                0,
                podcast.PodcastTitle.Length - " Notes".Length);
            Assert.Multiple(() =>
            {
                Assert.That(television.EpisodeTitle,
                    Does.Contain(televisionSubject));
                Assert.That(television.Summary,
                    Does.Contain(television.SeriesTitle));
                Assert.That(television.Genre, Is.AnyOf(TelevisionGenres));
                Assert.That(television.SeasonNumber, Is.InRange(1, 8));
                Assert.That(television.EpisodeNumber, Is.InRange(1, 24));
                Assert.That(television.EpisodeCode, Is.EqualTo(
                    "S" + television.SeasonNumber.ToString("D2") +
                    "E" + television.EpisodeNumber.ToString("D2")));
                Assert.That(television.DurationMinutes, Is.InRange(20, 60));
                Assert.That(television.AirDate, Is.LessThanOrEqualTo(reference));
                Assert.That(television.IsSynthetic, Is.True);
                Assert.That(television.UsesProjectAuthoredText, Is.True);
                Assert.That(television.RepresentsPublishedWork, Is.False);
                Assert.That(television.RepresentsBroadcastEpisode, Is.False);
                Assert.That(podcast.EpisodeTitle, Does.Contain(podcastSubject));
                Assert.That(podcast.Summary.ToLowerInvariant(),
                    Does.Contain(podcastSubject.ToLowerInvariant()));
                Assert.That(podcast.Category, Is.AnyOf(PodcastCategories));
                Assert.That(podcast.SeasonNumber, Is.InRange(1, 12));
                Assert.That(podcast.EpisodeNumber, Is.InRange(1, 52));
                Assert.That(podcast.EpisodeCode, Is.EqualTo(
                    "S" + podcast.SeasonNumber.ToString("D2") +
                    "E" + podcast.EpisodeNumber.ToString("D2")));
                Assert.That(podcast.DurationMinutes, Is.InRange(10, 120));
                Assert.That(podcast.PublicationDate,
                    Is.LessThanOrEqualTo(reference));
                Assert.That(podcast.IsSynthetic, Is.True);
                Assert.That(podcast.UsesProjectAuthoredText, Is.True);
                Assert.That(podcast.RepresentsPublishedWork, Is.False);
                Assert.That(podcast.RepresentsPublishedEpisode, Is.False);
            });
            index = index + 1;
        }
    }

    [Test]
    public void NewsRecordsAreSyntheticBulletinsRatherThanPublicationClaims()
    {
        DateTimeOffset reference = new DateTimeOffset(
            2044, 7, 8, 9, 10, 11, TimeSpan.Zero);
        FakeOptions options = FakeOptions.Create(12603);
        options.ReferenceTime = reference;
        MediaProvider media = Fake.Create(options).Media();

        int index = 0;
        while (index < 128)
        {
            NewsArticleRecord value = media.NewsArticle();
            string subject = NewsSubject(value.Headline);
            Assert.Multiple(() =>
            {
                Assert.That(value.Headline, Is.Not.Empty);
                Assert.That(value.Summary.ToLowerInvariant(),
                    Does.Contain(subject.ToLowerInvariant()));
                Assert.That(value.Summary, Does.StartWith("This synthetic bulletin"));
                Assert.That(value.Category, Is.AnyOf(NewsCategories));
                Assert.That(value.Outlet, Does.Contain("Example").Or.Contain("Fixture")
                    .Or.Contain("Sample").Or.Contain("Test"));
                Assert.That(value.Byline, Does.Match(
                    "^(Ari|Casey|Ember|Fin|Gray|Hollis) " +
                    "(Demo|Example|Fixture|Sample|Test|Trial)$"));
                Assert.That(value.PublishedAt, Is.LessThanOrEqualTo(reference));
                Assert.That(value.PublishedAt,
                    Is.GreaterThanOrEqualTo(reference.AddDays(-30.0)));
                Assert.That(value.IsSynthetic, Is.True);
                Assert.That(value.TestMarker, Is.EqualTo("fake.synthetic-news.v1"));
                Assert.That(value.UsesProjectAuthoredText, Is.True);
                Assert.That(value.RepresentsPublishedWork, Is.False);
                Assert.That(value.RepresentsReportedEvent, Is.False);
            });
            index = index + 1;
        }
    }

    [Test]
    public void InvariantVocabularyFallsBackDeterministicallyAcrossLocales()
    {
        const long seed = 12604;
        MediaProvider english = Fake.Create("en-US", seed).Media();
        MediaProvider japanese = Fake.Create("ja-JP", seed).Media();
        MovieRecord englishMovie = Fake.Create("en-US", seed).Media().Movie();
        MovieRecord japaneseMovie = Fake.Create("ja-JP", seed).Media().Movie();

        Assert.Multiple(() =>
        {
            Assert.That(japanese.MovieTitle(), Is.EqualTo(english.MovieTitle()));
            Assert.That(japanese.TelevisionSummary(),
                Is.EqualTo(english.TelevisionSummary()));
            Assert.That(japanese.PodcastCategory(),
                Is.EqualTo(english.PodcastCategory()));
            Assert.That(japanese.NewsHeadline(),
                Is.EqualTo(english.NewsHeadline()));
            Assert.That(japaneseMovie.Title, Is.EqualTo(englishMovie.Title));
            Assert.That(japaneseMovie.Summary, Is.EqualTo(englishMovie.Summary));
            Assert.That(japaneseMovie.Locale, Is.EqualTo("ja-JP"));
            Assert.That(japaneseMovie.VocabularyLocale, Is.EqualTo("invariant"));
        });
    }

    [Test]
    public void LimitsFailBeforeEntropyAndMediaCallsPreserveOtherStreams()
    {
        const long seed = 12605;
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 5;
        MediaProvider countLimited = Fake.Create(countOptions).Media();
        MediaProvider countUntouched = Fake.Create(countOptions).Media();
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 40;
        MediaProvider outputLimited = Fake.Create(outputOptions).Media();
        MediaProvider outputUntouched = Fake.Create(outputOptions).Media();
        Fake inspected = Fake.Create(seed);
        Fake untouched = Fake.Create(seed);

        int index = 0;
        while (index < 16)
        {
            inspected.Media().Movie();
            inspected.Media().TelevisionEpisode();
            inspected.Media().PodcastEpisode();
            inspected.Media().NewsArticle();
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(() => countLimited.MovieTitle(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(countLimited.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
            Assert.That(() => outputLimited.Movie(),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(outputLimited.RandomValue.Long(),
                Is.EqualTo(outputUntouched.RandomValue.Long()));
            Assert.That(inspected.Person().FullName(),
                Is.EqualTo(untouched.Person().FullName()));
        });
    }

    static string NewsSubject(string headline)
    {
        string[] values = new string[]
        {
            "Community archive", "Garden group", "Harbor workshop",
            "Local library", "Neighborhood studio", "Orchard cooperative"
        };
        int index = 0;
        while (index < values.Length)
        {
            if (headline.StartsWith(values[index] + " ", StringComparison.Ordinal))
            {
                return values[index];
            }
            index = index + 1;
        }
        throw new InvalidOperationException("The headline did not use an authored subject.");
    }
}
