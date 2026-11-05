namespace iso20022;

/// <summary>Compares two exact selected ISO 20022 message-version graphs.</summary>
public class MessageVersionComparer : IMessageVersionComparer
{
    /// <summary>Initializes an exact message-version comparer.</summary>
    public MessageVersionComparer()
    {
    }

    /// <summary>Compares two exact selected message versions.</summary>
    /// <param name="request">The repositories, exact message identifiers, overrides, and limits.</param>
    /// <returns>The stable message-version difference and compatibility result.</returns>
    public RepositoryDiff CompareMessageVersions(RepositoryComparisonRequest? request)
    {
        if (request == null)
        {
            return CreateFailure(
                string.Empty,
                string.Empty,
                "comparison.request_missing",
                "A message-version comparison request is required.");
        }

        string sourceEdition = request.Source == null ? string.Empty : request.Source.Edition;
        string targetEdition = request.Target == null ? string.Empty : request.Target.Edition;
        if (request.Source == null || request.Target == null)
        {
            return CreateFailure(
                sourceEdition,
                targetEdition,
                "comparison.repository_required",
                "Both source and target repository snapshots are required.");
        }

        if (request.SourceMessageIdentifier == null)
        {
            return CreateFailure(
                sourceEdition,
                targetEdition,
                "comparison.source_message_identifier_required",
                "An exact source message identifier is required.");
        }

        if (request.TargetMessageIdentifier == null)
        {
            return CreateFailure(
                sourceEdition,
                targetEdition,
                "comparison.target_message_identifier_required",
                "An exact target message identifier is required.");
        }

        MessageGraphSelection sourceSelection = MessageGraphSelector.SelectMessageGraph(
            request.Source,
            request.SourceMessageIdentifier,
            "source");
        if (!sourceSelection.Success)
        {
            return CreateFailure(
                sourceEdition,
                targetEdition,
                sourceSelection.ErrorCode,
                sourceSelection.ErrorMessage);
        }

        MessageGraphSelection targetSelection = MessageGraphSelector.SelectMessageGraph(
            request.Target,
            request.TargetMessageIdentifier,
            "target");
        if (!targetSelection.Success)
        {
            return CreateFailure(
                sourceEdition,
                targetEdition,
                targetSelection.ErrorCode,
                targetSelection.ErrorMessage);
        }

        RepositoryMatchResult matches = RepositoryMatcher.Match(request);
        if (!matches.Success)
        {
            return CreateFailure(
                sourceEdition,
                targetEdition,
                matches.ErrorCode,
                matches.ErrorMessage);
        }

        return RepositoryGraphDiffer.CreateScopedDifference(
            request,
            matches,
            sourceSelection.Identifiers,
            targetSelection.Identifiers);
    }

    internal RepositoryDiff CreateFailure(
        string sourceEdition,
        string targetEdition,
        string code,
        string message)
    {
        RepositoryDiff failure = new RepositoryDiff();
        failure.SourceEdition = sourceEdition;
        failure.TargetEdition = targetEdition;
        failure.ErrorCode = code;
        failure.ErrorMessage = message;
        return failure;
    }
}
