namespace fake;

/// <summary>Provides synthetic movie, television, podcast, and news metadata.</summary>
public class MediaProvider : IMediaProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;

    internal MediaProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("media");
    }

    /// <summary>Composes a project-authored fictional movie title.</summary>
    /// <returns>A title assembled from generic authored vocabulary.</returns>
    public string MovieTitle()
    {
        return MediaGenerator.MovieTitle(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional movie summary.</summary>
    /// <returns>A short generic summary that is not copied from a published work.</returns>
    public string MovieSummary()
    {
        return MediaGenerator.MovieSummary(Context, RandomValue);
    }

    /// <summary>Selects a broad movie genre.</summary>
    /// <returns>A conventional genre label.</returns>
    public string MovieGenre()
    {
        return MediaGenerator.MovieGenre(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional television-series title.</summary>
    /// <returns>A series title assembled from generic authored vocabulary.</returns>
    public string TelevisionSeriesTitle()
    {
        return MediaGenerator.TelevisionSeriesTitle(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional television-episode title.</summary>
    /// <returns>An episode title assembled from generic authored vocabulary.</returns>
    public string TelevisionEpisodeTitle()
    {
        return MediaGenerator.TelevisionEpisodeTitle(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional television summary.</summary>
    /// <returns>A short generic summary that is not copied from a broadcast work.</returns>
    public string TelevisionSummary()
    {
        return MediaGenerator.TelevisionSummary(Context, RandomValue);
    }

    /// <summary>Selects a broad television genre.</summary>
    /// <returns>A conventional genre label.</returns>
    public string TelevisionGenre()
    {
        return MediaGenerator.TelevisionGenre(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional podcast title.</summary>
    /// <returns>A podcast title assembled from generic authored vocabulary.</returns>
    public string PodcastTitle()
    {
        return MediaGenerator.PodcastTitle(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional podcast-episode title.</summary>
    /// <returns>An episode title assembled from generic authored vocabulary.</returns>
    public string PodcastEpisodeTitle()
    {
        return MediaGenerator.PodcastEpisodeTitle(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional podcast summary.</summary>
    /// <returns>A short generic summary that is not copied from a published episode.</returns>
    public string PodcastSummary()
    {
        return MediaGenerator.PodcastSummary(Context, RandomValue);
    }

    /// <summary>Selects a broad podcast category.</summary>
    /// <returns>A conventional category label.</returns>
    public string PodcastCategory()
    {
        return MediaGenerator.PodcastCategory(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional news headline.</summary>
    /// <returns>A generic synthetic headline that does not report a real event.</returns>
    public string NewsHeadline()
    {
        return MediaGenerator.NewsHeadline(Context, RandomValue);
    }

    /// <summary>Composes a project-authored fictional news summary.</summary>
    /// <returns>A generic synthetic bulletin that does not reproduce published reporting.</returns>
    public string NewsSummary()
    {
        return MediaGenerator.NewsSummary(Context, RandomValue);
    }

    /// <summary>Selects a broad news category.</summary>
    /// <returns>A conventional category label.</returns>
    public string NewsCategory()
    {
        return MediaGenerator.NewsCategory(Context, RandomValue);
    }

    /// <summary>Generates correlated synthetic movie-shaped metadata.</summary>
    /// <returns>A fictional movie record with explicit safety metadata.</returns>
    public MovieRecord Movie()
    {
        return MediaGenerator.Movie(Context, RandomValue);
    }

    /// <summary>Generates correlated synthetic television-episode metadata.</summary>
    /// <returns>A fictional television record with correlated episode fields.</returns>
    public TelevisionEpisodeRecord TelevisionEpisode()
    {
        return MediaGenerator.TelevisionEpisode(Context, RandomValue);
    }

    /// <summary>Generates correlated synthetic podcast-episode metadata.</summary>
    /// <returns>A fictional podcast record with correlated episode fields.</returns>
    public PodcastEpisodeRecord PodcastEpisode()
    {
        return MediaGenerator.PodcastEpisode(Context, RandomValue);
    }

    /// <summary>Generates correlated synthetic news-article metadata.</summary>
    /// <returns>A fictional news record that does not claim publication or reporting.</returns>
    public NewsArticleRecord NewsArticle()
    {
        return MediaGenerator.NewsArticle(Context, RandomValue);
    }
}
