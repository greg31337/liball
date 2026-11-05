namespace iso20022;

/// <summary>Compares exact message versions and classifies structural and compatibility impact.</summary>
public interface IMessageVersionComparer
{
    /// <summary>Compares two exact selected message versions.</summary>
    /// <param name="request">The repositories, exact message identifiers, overrides, and limits.</param>
    /// <returns>The stable message-version difference and compatibility result.</returns>
    RepositoryDiff CompareMessageVersions(RepositoryComparisonRequest? request);
}
