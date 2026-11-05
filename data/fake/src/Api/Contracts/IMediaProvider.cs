namespace fake;

/// <summary>Defines synthetic movie, television, podcast, and news metadata operations.</summary>
public interface IMediaProvider
{
    /// <summary>Composes a project-authored fictional movie title.</summary>
    /// <returns>A title assembled from generic authored vocabulary.</returns>
    string MovieTitle();

    /// <summary>Composes a project-authored fictional movie summary.</summary>
    /// <returns>A short generic summary that is not copied from a published work.</returns>
    string MovieSummary();

    /// <summary>Selects a broad movie genre.</summary>
    /// <returns>A conventional genre label.</returns>
    string MovieGenre();

    /// <summary>Composes a project-authored fictional television-series title.</summary>
    /// <returns>A series title assembled from generic authored vocabulary.</returns>
    string TelevisionSeriesTitle();

    /// <summary>Composes a project-authored fictional television-episode title.</summary>
    /// <returns>An episode title assembled from generic authored vocabulary.</returns>
    string TelevisionEpisodeTitle();

    /// <summary>Composes a project-authored fictional television summary.</summary>
    /// <returns>A short generic summary that is not copied from a broadcast work.</returns>
    string TelevisionSummary();

    /// <summary>Selects a broad television genre.</summary>
    /// <returns>A conventional genre label.</returns>
    string TelevisionGenre();

    /// <summary>Composes a project-authored fictional podcast title.</summary>
    /// <returns>A podcast title assembled from generic authored vocabulary.</returns>
    string PodcastTitle();

    /// <summary>Composes a project-authored fictional podcast-episode title.</summary>
    /// <returns>An episode title assembled from generic authored vocabulary.</returns>
    string PodcastEpisodeTitle();

    /// <summary>Composes a project-authored fictional podcast summary.</summary>
    /// <returns>A short generic summary that is not copied from a published episode.</returns>
    string PodcastSummary();

    /// <summary>Selects a broad podcast category.</summary>
    /// <returns>A conventional category label.</returns>
    string PodcastCategory();

    /// <summary>Composes a project-authored fictional news headline.</summary>
    /// <returns>A generic synthetic headline that does not report a real event.</returns>
    string NewsHeadline();

    /// <summary>Composes a project-authored fictional news summary.</summary>
    /// <returns>A generic synthetic bulletin that does not reproduce published reporting.</returns>
    string NewsSummary();

    /// <summary>Selects a broad news category.</summary>
    /// <returns>A conventional category label.</returns>
    string NewsCategory();

    /// <summary>Generates correlated synthetic movie-shaped metadata.</summary>
    /// <returns>A fictional movie record with explicit safety metadata.</returns>
    MovieRecord Movie();

    /// <summary>Generates correlated synthetic television-episode metadata.</summary>
    /// <returns>A fictional television record with correlated episode fields.</returns>
    TelevisionEpisodeRecord TelevisionEpisode();

    /// <summary>Generates correlated synthetic podcast-episode metadata.</summary>
    /// <returns>A fictional podcast record with correlated episode fields.</returns>
    PodcastEpisodeRecord PodcastEpisode();

    /// <summary>Generates correlated synthetic news-article metadata.</summary>
    /// <returns>A fictional news record that does not claim publication or reporting.</returns>
    NewsArticleRecord NewsArticle();
}
