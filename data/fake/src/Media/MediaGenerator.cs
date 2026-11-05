using System;
using System.Globalization;

namespace fake;

internal static class MediaGenerator
{
    const string MovieMarker = "fake.synthetic-movie.v1";
    const string TelevisionMarker = "fake.synthetic-television.v1";
    const string PodcastMarker = "fake.synthetic-podcast.v1";
    const string NewsMarker = "fake.synthetic-news.v1";
    const double MovieWindowDays = 14610.0;
    const double TelevisionWindowDays = 3652.0;
    const double PodcastWindowDays = 1826.0;
    const double NewsWindowDays = 30.0;

    internal static string MovieTitle(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTitleData(context);
        Guard.OutputSize(
            MaximumMovieTitleLength(),
            context.CapturedOptions.MaximumOutputSize,
            "movieTitle");
        int adjectiveIndex = random.IntFromMaximumExclusive(
            MediaData.TitleAdjectives.Length);
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        return Normalize(
            context,
            ComposeMovieTitle(adjectiveIndex, subjectIndex));
    }

    internal static string MovieSummary(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateSummaryData(context);
        Guard.OutputSize(
            MaximumMovieSummaryLength(),
            context.CapturedOptions.MaximumOutputSize,
            "movieSummary");
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        int developmentIndex = random.IntFromMaximumExclusive(
            MediaData.SummaryDevelopments.Length);
        return Normalize(
            context,
            ComposeMovieSummary(subjectIndex, developmentIndex));
    }

    internal static string MovieGenre(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, MediaData.MovieGenres, "movieGenreData");
        return ChooseValue(context, random, MediaData.MovieGenres, "movieGenre");
    }

    internal static string TelevisionSeriesTitle(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTitleData(context);
        Guard.OutputSize(
            MaximumTelevisionSeriesTitleLength(),
            context.CapturedOptions.MaximumOutputSize,
            "televisionSeriesTitle");
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        return Normalize(
            context,
            ComposeTelevisionSeriesTitle(subjectIndex));
    }

    internal static string TelevisionEpisodeTitle(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTitleData(context);
        Guard.OutputSize(
            MaximumTelevisionEpisodeTitleLength(),
            context.CapturedOptions.MaximumOutputSize,
            "televisionEpisodeTitle");
        int adjectiveIndex = random.IntFromMaximumExclusive(
            MediaData.TitleAdjectives.Length);
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        return Normalize(
            context,
            ComposeTelevisionEpisodeTitle(adjectiveIndex, subjectIndex));
    }

    internal static string TelevisionSummary(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateSummaryData(context);
        Guard.OutputSize(
            MaximumTelevisionSummaryLength(),
            context.CapturedOptions.MaximumOutputSize,
            "televisionSummary");
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        int developmentIndex = random.IntFromMaximumExclusive(
            MediaData.SummaryDevelopments.Length);
        return Normalize(
            context,
            ComposeTelevisionSummary(subjectIndex, developmentIndex));
    }

    internal static string TelevisionGenre(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(
            context,
            MediaData.TelevisionGenres,
            "televisionGenreData");
        return ChooseValue(
            context,
            random,
            MediaData.TelevisionGenres,
            "televisionGenre");
    }

    internal static string PodcastTitle(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTitleData(context);
        Guard.OutputSize(
            MaximumPodcastTitleLength(),
            context.CapturedOptions.MaximumOutputSize,
            "podcastTitle");
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        return Normalize(context, ComposePodcastTitle(subjectIndex));
    }

    internal static string PodcastEpisodeTitle(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTitleData(context);
        Guard.OutputSize(
            MaximumPodcastEpisodeTitleLength(),
            context.CapturedOptions.MaximumOutputSize,
            "podcastEpisodeTitle");
        int adjectiveIndex = random.IntFromMaximumExclusive(
            MediaData.TitleAdjectives.Length);
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        return Normalize(
            context,
            ComposePodcastEpisodeTitle(adjectiveIndex, subjectIndex));
    }

    internal static string PodcastSummary(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateSummaryData(context);
        Guard.OutputSize(
            MaximumPodcastSummaryLength(),
            context.CapturedOptions.MaximumOutputSize,
            "podcastSummary");
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        int developmentIndex = random.IntFromMaximumExclusive(
            MediaData.SummaryDevelopments.Length);
        return Normalize(
            context,
            ComposePodcastSummary(subjectIndex, developmentIndex));
    }

    internal static string PodcastCategory(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(
            context,
            MediaData.PodcastCategories,
            "podcastCategoryData");
        return ChooseValue(
            context,
            random,
            MediaData.PodcastCategories,
            "podcastCategory");
    }

    internal static string NewsHeadline(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateNewsTextData(context);
        Guard.OutputSize(
            MaximumNewsHeadlineLength(),
            context.CapturedOptions.MaximumOutputSize,
            "newsHeadline");
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.NewsSubjects.Length);
        int actionIndex = random.IntFromMaximumExclusive(
            MediaData.NewsActions.Length);
        return Normalize(
            context,
            ComposeNewsHeadline(subjectIndex, actionIndex));
    }

    internal static string NewsSummary(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateNewsTextData(context);
        ValidateValues(
            context,
            MediaData.SummaryDevelopments,
            "mediaSummaryData");
        Guard.OutputSize(
            MaximumNewsSummaryLength(),
            context.CapturedOptions.MaximumOutputSize,
            "newsSummary");
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.NewsSubjects.Length);
        int developmentIndex = random.IntFromMaximumExclusive(
            MediaData.SummaryDevelopments.Length);
        return Normalize(
            context,
            ComposeNewsSummary(subjectIndex, developmentIndex));
    }

    internal static string NewsCategory(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateValues(context, MediaData.NewsCategories, "newsCategoryData");
        return ChooseValue(
            context,
            random,
            MediaData.NewsCategories,
            "newsCategory");
    }

    internal static MovieRecord Movie(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateMovieData(context);
        Guard.OutputSize(
            MaximumMovieRecordSize(context),
            context.CapturedOptions.MaximumOutputSize,
            "movieRecord");
        int adjectiveIndex = random.IntFromMaximumExclusive(
            MediaData.TitleAdjectives.Length);
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        int developmentIndex = random.IntFromMaximumExclusive(
            MediaData.SummaryDevelopments.Length);
        MovieRecord result = new MovieRecord();
        result.Locale = context.CapturedOptions.Locale;
        result.VocabularyLocale = MediaData.VocabularyLocale;
        result.Title = Normalize(
            context,
            ComposeMovieTitle(adjectiveIndex, subjectIndex));
        result.Summary = Normalize(
            context,
            ComposeMovieSummary(subjectIndex, developmentIndex));
        result.Genre = ChooseValue(
            context,
            random,
            MediaData.MovieGenres,
            "movieGenre");
        result.Studio = ChooseValue(
            context,
            random,
            MediaData.Studios,
            "movieStudio");
        result.RuntimeMinutes = random.IntFromMinimumAndMaximum(70, 181);
        result.ReleaseDate = RecentDate(
            context,
            random,
            MovieWindowDays);
        result.IsSynthetic = true;
        result.TestMarker = MovieMarker;
        result.UsesProjectAuthoredText = true;
        result.RepresentsPublishedWork = false;
        result.RepresentsReleasedMovie = false;
        return result;
    }

    internal static TelevisionEpisodeRecord TelevisionEpisode(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateTelevisionData(context);
        Guard.OutputSize(
            MaximumTelevisionRecordSize(context),
            context.CapturedOptions.MaximumOutputSize,
            "televisionEpisodeRecord");
        int adjectiveIndex = random.IntFromMaximumExclusive(
            MediaData.TitleAdjectives.Length);
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        int developmentIndex = random.IntFromMaximumExclusive(
            MediaData.SummaryDevelopments.Length);
        int seasonNumber = random.IntFromMinimumAndMaximum(1, 9);
        int episodeNumber = random.IntFromMinimumAndMaximum(1, 25);
        TelevisionEpisodeRecord result = new TelevisionEpisodeRecord();
        result.Locale = context.CapturedOptions.Locale;
        result.VocabularyLocale = MediaData.VocabularyLocale;
        result.SeriesTitle = Normalize(
            context,
            ComposeTelevisionSeriesTitle(subjectIndex));
        result.EpisodeTitle = Normalize(
            context,
            ComposeTelevisionEpisodeTitle(adjectiveIndex, subjectIndex));
        result.Summary = Normalize(
            context,
            ComposeTelevisionSummary(subjectIndex, developmentIndex));
        result.Genre = ChooseValue(
            context,
            random,
            MediaData.TelevisionGenres,
            "televisionGenre");
        result.Network = ChooseValue(
            context,
            random,
            MediaData.Networks,
            "televisionNetwork");
        result.SeasonNumber = seasonNumber;
        result.EpisodeNumber = episodeNumber;
        result.EpisodeCode = EpisodeCode(seasonNumber, episodeNumber);
        result.DurationMinutes = random.IntFromMinimumAndMaximum(20, 61);
        result.AirDate = RecentDate(
            context,
            random,
            TelevisionWindowDays);
        result.IsSynthetic = true;
        result.TestMarker = TelevisionMarker;
        result.UsesProjectAuthoredText = true;
        result.RepresentsPublishedWork = false;
        result.RepresentsBroadcastEpisode = false;
        return result;
    }

    internal static PodcastEpisodeRecord PodcastEpisode(
        GenerationContext context,
        FakeRandom random)
    {
        ValidatePodcastData(context);
        Guard.OutputSize(
            MaximumPodcastRecordSize(context),
            context.CapturedOptions.MaximumOutputSize,
            "podcastEpisodeRecord");
        int adjectiveIndex = random.IntFromMaximumExclusive(
            MediaData.TitleAdjectives.Length);
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.TitleSubjects.Length);
        int developmentIndex = random.IntFromMaximumExclusive(
            MediaData.SummaryDevelopments.Length);
        int seasonNumber = random.IntFromMinimumAndMaximum(1, 13);
        int episodeNumber = random.IntFromMinimumAndMaximum(1, 53);
        PodcastEpisodeRecord result = new PodcastEpisodeRecord();
        result.Locale = context.CapturedOptions.Locale;
        result.VocabularyLocale = MediaData.VocabularyLocale;
        result.PodcastTitle = Normalize(
            context,
            ComposePodcastTitle(subjectIndex));
        result.EpisodeTitle = Normalize(
            context,
            ComposePodcastEpisodeTitle(adjectiveIndex, subjectIndex));
        result.Summary = Normalize(
            context,
            ComposePodcastSummary(subjectIndex, developmentIndex));
        result.Category = ChooseValue(
            context,
            random,
            MediaData.PodcastCategories,
            "podcastCategory");
        result.Publisher = ChooseValue(
            context,
            random,
            MediaData.PodcastPublishers,
            "podcastPublisher");
        result.Host = ContributorName(context, random);
        result.SeasonNumber = seasonNumber;
        result.EpisodeNumber = episodeNumber;
        result.EpisodeCode = EpisodeCode(seasonNumber, episodeNumber);
        result.DurationMinutes = random.IntFromMinimumAndMaximum(10, 121);
        result.PublicationDate = RecentDate(
            context,
            random,
            PodcastWindowDays);
        result.IsSynthetic = true;
        result.TestMarker = PodcastMarker;
        result.UsesProjectAuthoredText = true;
        result.RepresentsPublishedWork = false;
        result.RepresentsPublishedEpisode = false;
        return result;
    }

    internal static NewsArticleRecord NewsArticle(
        GenerationContext context,
        FakeRandom random)
    {
        ValidateNewsData(context);
        Guard.OutputSize(
            MaximumNewsRecordSize(context),
            context.CapturedOptions.MaximumOutputSize,
            "newsArticleRecord");
        int subjectIndex = random.IntFromMaximumExclusive(
            MediaData.NewsSubjects.Length);
        int actionIndex = random.IntFromMaximumExclusive(
            MediaData.NewsActions.Length);
        int developmentIndex = random.IntFromMaximumExclusive(
            MediaData.SummaryDevelopments.Length);
        NewsArticleRecord result = new NewsArticleRecord();
        result.Locale = context.CapturedOptions.Locale;
        result.VocabularyLocale = MediaData.VocabularyLocale;
        result.Headline = Normalize(
            context,
            ComposeNewsHeadline(subjectIndex, actionIndex));
        result.Summary = Normalize(
            context,
            ComposeNewsSummary(subjectIndex, developmentIndex));
        result.Category = ChooseValue(
            context,
            random,
            MediaData.NewsCategories,
            "newsCategory");
        result.Outlet = ChooseValue(
            context,
            random,
            MediaData.NewsOutlets,
            "newsOutlet");
        result.Byline = ContributorName(context, random);
        result.PublishedAt = RecentDate(
            context,
            random,
            NewsWindowDays);
        result.IsSynthetic = true;
        result.TestMarker = NewsMarker;
        result.UsesProjectAuthoredText = true;
        result.RepresentsPublishedWork = false;
        result.RepresentsReportedEvent = false;
        return result;
    }

    static string ComposeMovieTitle(int adjectiveIndex, int subjectIndex)
    {
        return MediaData.TitleAdjectives[adjectiveIndex] + " " +
            MediaData.TitleSubjects[subjectIndex];
    }

    static string ComposeMovieSummary(int subjectIndex, int developmentIndex)
    {
        return "A fictional ensemble explores the " +
            MediaData.TitleSubjects[subjectIndex].ToLowerInvariant() +
            " while " + MediaData.SummaryDevelopments[developmentIndex] + ".";
    }

    static string ComposeTelevisionSeriesTitle(int subjectIndex)
    {
        return "The " + MediaData.TitleSubjects[subjectIndex] + " Desk";
    }

    static string ComposeTelevisionEpisodeTitle(
        int adjectiveIndex,
        int subjectIndex)
    {
        return "The " + MediaData.TitleAdjectives[adjectiveIndex] + " " +
            MediaData.TitleSubjects[subjectIndex];
    }

    static string ComposeTelevisionSummary(
        int subjectIndex,
        int developmentIndex)
    {
        return "The fictional team at " +
            ComposeTelevisionSeriesTitle(subjectIndex) +
            " examines the " +
            MediaData.TitleSubjects[subjectIndex].ToLowerInvariant() +
            " as " + MediaData.SummaryDevelopments[developmentIndex] + ".";
    }

    static string ComposePodcastTitle(int subjectIndex)
    {
        return MediaData.TitleSubjects[subjectIndex] + " Notes";
    }

    static string ComposePodcastEpisodeTitle(
        int adjectiveIndex,
        int subjectIndex)
    {
        return MediaData.TitleAdjectives[adjectiveIndex] + " " +
            MediaData.TitleSubjects[subjectIndex] + " Conversation";
    }

    static string ComposePodcastSummary(
        int subjectIndex,
        int developmentIndex)
    {
        return "A fictional host discusses the " +
            MediaData.TitleSubjects[subjectIndex].ToLowerInvariant() +
            " and how " + MediaData.SummaryDevelopments[developmentIndex] + ".";
    }

    static string ComposeNewsHeadline(int subjectIndex, int actionIndex)
    {
        return MediaData.NewsSubjects[subjectIndex] + " " +
            MediaData.NewsActions[actionIndex];
    }

    static string ComposeNewsSummary(int subjectIndex, int developmentIndex)
    {
        return "This synthetic bulletin describes how the " +
            MediaData.NewsSubjects[subjectIndex].ToLowerInvariant() + " says " +
            MediaData.SummaryDevelopments[developmentIndex] + ".";
    }

    static string ContributorName(
        GenerationContext context,
        FakeRandom random)
    {
        string result = MediaData.ContributorGivenNames[
            random.IntFromMaximumExclusive(
                MediaData.ContributorGivenNames.Length)] + " " +
            MediaData.ContributorSurnames[
                random.IntFromMaximumExclusive(
                    MediaData.ContributorSurnames.Length)];
        return Normalize(context, result);
    }

    static string EpisodeCode(int seasonNumber, int episodeNumber)
    {
        return "S" + seasonNumber.ToString("D2", CultureInfo.InvariantCulture) +
            "E" + episodeNumber.ToString("D2", CultureInfo.InvariantCulture);
    }

    static DateTimeOffset RecentDate(
        GenerationContext context,
        FakeRandom random,
        double windowDays)
    {
        return RelativeDateGenerator.Recent(
            random,
            context.ReferenceTime(),
            TimeSpan.FromDays(windowDays));
    }

    static string ChooseValue(
        GenerationContext context,
        FakeRandom random,
        string[] values,
        string outputName)
    {
        string result = values[random.IntFromMaximumExclusive(values.Length)];
        Guard.OutputSize(
            result.Length,
            context.CapturedOptions.MaximumOutputSize,
            outputName);
        return Normalize(context, result);
    }

    static string Normalize(GenerationContext context, string value)
    {
        return UnicodeText.Normalize(
            value,
            context.NormalizationModeValue,
            context.CapturedOptions.MaximumOutputSize);
    }

    static void ValidateTitleData(GenerationContext context)
    {
        ValidateValues(context, MediaData.TitleAdjectives, "mediaTitleData");
        ValidateValues(context, MediaData.TitleSubjects, "mediaTitleData");
    }

    static void ValidateSummaryData(GenerationContext context)
    {
        ValidateValues(context, MediaData.TitleSubjects, "mediaSummaryData");
        ValidateValues(
            context,
            MediaData.SummaryDevelopments,
            "mediaSummaryData");
    }

    static void ValidateNewsTextData(GenerationContext context)
    {
        ValidateValues(context, MediaData.NewsSubjects, "newsTextData");
        ValidateValues(context, MediaData.NewsActions, "newsTextData");
    }

    static void ValidateContributorData(GenerationContext context)
    {
        ValidateValues(
            context,
            MediaData.ContributorGivenNames,
            "mediaContributorData");
        ValidateValues(
            context,
            MediaData.ContributorSurnames,
            "mediaContributorData");
    }

    static void ValidateMovieData(GenerationContext context)
    {
        ValidateTitleData(context);
        ValidateSummaryData(context);
        ValidateValues(context, MediaData.MovieGenres, "movieGenreData");
        ValidateValues(context, MediaData.Studios, "movieStudioData");
    }

    static void ValidateTelevisionData(GenerationContext context)
    {
        ValidateTitleData(context);
        ValidateSummaryData(context);
        ValidateValues(
            context,
            MediaData.TelevisionGenres,
            "televisionGenreData");
        ValidateValues(context, MediaData.Networks, "televisionNetworkData");
    }

    static void ValidatePodcastData(GenerationContext context)
    {
        ValidateTitleData(context);
        ValidateSummaryData(context);
        ValidateContributorData(context);
        ValidateValues(
            context,
            MediaData.PodcastCategories,
            "podcastCategoryData");
        ValidateValues(
            context,
            MediaData.PodcastPublishers,
            "podcastPublisherData");
    }

    static void ValidateNewsData(GenerationContext context)
    {
        ValidateNewsTextData(context);
        ValidateValues(
            context,
            MediaData.SummaryDevelopments,
            "mediaSummaryData");
        ValidateContributorData(context);
        ValidateValues(context, MediaData.NewsCategories, "newsCategoryData");
        ValidateValues(context, MediaData.NewsOutlets, "newsOutletData");
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
                    "A project-authored media data table is malformed.");
            }
            Normalize(context, values[index]);
            index = index + 1;
        }
    }

    static int MaximumMovieTitleLength()
    {
        return MaximumLength(MediaData.TitleAdjectives) + 1 +
            MaximumLength(MediaData.TitleSubjects);
    }

    static int MaximumMovieSummaryLength()
    {
        return "A fictional ensemble explores the ".Length +
            MaximumLength(MediaData.TitleSubjects) + " while ".Length +
            MaximumLength(MediaData.SummaryDevelopments) + 1;
    }

    static int MaximumTelevisionSeriesTitleLength()
    {
        return "The ".Length + MaximumLength(MediaData.TitleSubjects) +
            " Desk".Length;
    }

    static int MaximumTelevisionEpisodeTitleLength()
    {
        return "The ".Length + MaximumLength(MediaData.TitleAdjectives) + 1 +
            MaximumLength(MediaData.TitleSubjects);
    }

    static int MaximumTelevisionSummaryLength()
    {
        return "The fictional team at ".Length +
            MaximumTelevisionSeriesTitleLength() +
            " examines the ".Length + MaximumLength(MediaData.TitleSubjects) +
            " as ".Length + MaximumLength(MediaData.SummaryDevelopments) + 1;
    }

    static int MaximumPodcastTitleLength()
    {
        return MaximumLength(MediaData.TitleSubjects) + " Notes".Length;
    }

    static int MaximumPodcastEpisodeTitleLength()
    {
        return MaximumLength(MediaData.TitleAdjectives) + 1 +
            MaximumLength(MediaData.TitleSubjects) + " Conversation".Length;
    }

    static int MaximumPodcastSummaryLength()
    {
        return "A fictional host discusses the ".Length +
            MaximumLength(MediaData.TitleSubjects) + " and how ".Length +
            MaximumLength(MediaData.SummaryDevelopments) + 1;
    }

    static int MaximumNewsHeadlineLength()
    {
        return MaximumLength(MediaData.NewsSubjects) + 1 +
            MaximumLength(MediaData.NewsActions);
    }

    static int MaximumNewsSummaryLength()
    {
        return "This synthetic bulletin describes how the ".Length +
            MaximumLength(MediaData.NewsSubjects) + " says ".Length +
            MaximumLength(MediaData.SummaryDevelopments) + 1;
    }

    static int MaximumContributorNameLength()
    {
        return MaximumLength(MediaData.ContributorGivenNames) + 1 +
            MaximumLength(MediaData.ContributorSurnames);
    }

    static int MaximumMovieRecordSize(GenerationContext context)
    {
        return context.CapturedOptions.Locale.Length +
            MediaData.VocabularyLocale.Length + MaximumMovieTitleLength() +
            MaximumMovieSummaryLength() + MaximumLength(MediaData.MovieGenres) +
            MaximumLength(MediaData.Studios) + MovieMarker.Length;
    }

    static int MaximumTelevisionRecordSize(GenerationContext context)
    {
        return context.CapturedOptions.Locale.Length +
            MediaData.VocabularyLocale.Length +
            MaximumTelevisionSeriesTitleLength() +
            MaximumTelevisionEpisodeTitleLength() +
            MaximumTelevisionSummaryLength() +
            MaximumLength(MediaData.TelevisionGenres) +
            MaximumLength(MediaData.Networks) + 6 + TelevisionMarker.Length;
    }

    static int MaximumPodcastRecordSize(GenerationContext context)
    {
        return context.CapturedOptions.Locale.Length +
            MediaData.VocabularyLocale.Length + MaximumPodcastTitleLength() +
            MaximumPodcastEpisodeTitleLength() + MaximumPodcastSummaryLength() +
            MaximumLength(MediaData.PodcastCategories) +
            MaximumLength(MediaData.PodcastPublishers) +
            MaximumContributorNameLength() + 6 + PodcastMarker.Length;
    }

    static int MaximumNewsRecordSize(GenerationContext context)
    {
        return context.CapturedOptions.Locale.Length +
            MediaData.VocabularyLocale.Length + MaximumNewsHeadlineLength() +
            MaximumNewsSummaryLength() + MaximumLength(MediaData.NewsCategories) +
            MaximumLength(MediaData.NewsOutlets) +
            MaximumContributorNameLength() + NewsMarker.Length;
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
