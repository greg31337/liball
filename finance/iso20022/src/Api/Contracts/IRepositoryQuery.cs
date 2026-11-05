namespace iso20022;

/// <summary>Queries a normalized repository snapshot by indexed keys and version selectors.</summary>
public interface IRepositoryQuery
{
    /// <summary>Evaluates one conjunctive query against the configured snapshot.</summary>
    /// <param name="request">The indexed keys and selectors to evaluate.</param><returns>The deterministic query result.</returns>
    RepositoryQueryResult QueryRepository(RepositoryQueryRequest? request);
}
