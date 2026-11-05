namespace iso20022;

/// <summary>Compares normalized ISO 20022 repository graphs with deterministic matching.</summary>
public class RepositoryComparer : IRepositoryComparer
{
    /// <summary>Initializes a repository graph comparer.</summary>
    public RepositoryComparer()
    {
    }

    /// <summary>Compares two repository snapshots and returns deterministic graph differences.</summary>
    /// <param name="request">The source, target, overrides, matching policy, and limits.</param>
    /// <returns>The stable repository difference result.</returns>
    public RepositoryDiff CompareRepositories(RepositoryComparisonRequest? request)
    {
        if (request == null)
        {
            RepositoryDiff missing = new RepositoryDiff();
            missing.ErrorCode = "comparison.request_missing";
            missing.ErrorMessage = "A repository comparison request is required.";
            return missing;
        }

        if (request.BaselineProfile != null || request.LocalProfile != null ||
            request.TargetProfile != null)
        {
            return ProfileRebaseComparer.CompareProfiles(request);
        }

        RepositoryMatchResult matches = RepositoryMatcher.Match(request);
        if (!matches.Success)
        {
            RepositoryDiff failure = new RepositoryDiff();
            failure.ErrorCode = matches.ErrorCode;
            failure.ErrorMessage = matches.ErrorMessage;
            if (request.Source != null)
            {
                failure.SourceEdition = request.Source.Edition;
            }

            if (request.Target != null)
            {
                failure.TargetEdition = request.Target.Edition;
            }

            return failure;
        }

        return RepositoryGraphDiffer.CreateDifference(request, matches);
    }
}
