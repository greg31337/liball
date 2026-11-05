using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides smarts matcher data and operations for molecular graphs.
/// </summary>
public class SmartsMatcher : ISmartsMatcher
{
    internal SmartsParser Parser;
    internal SubgraphMatcher Matcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartsMatcher"/> class.
    /// </summary>
    public SmartsMatcher()
    {
        Parser = new SmartsParser();
        Matcher = new SubgraphMatcher();
    }

    internal bool IsMatch(Molecule target, string smarts)
    {
        return FindFirst(target, smarts).Success;
    }

    internal bool IsQueryMatch(Molecule target, QueryMolecule query)
    {
        return FindFirstQuery(target, query).Success;
    }

    /// <summary>
    /// Finds the first.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="smarts">The smarts.</param>
    /// <returns>The resulting subgraph match.</returns>
    public SubgraphMatch FindFirst(Molecule target, string smarts)
    {
        SmartsParseResult result = Parser.ParseWithResult(smarts);
        if (!result.Success)
        {
            return new SubgraphMatch();
        }

        return Matcher.FindFirstQuery(target, result.Query);
    }

    /// <summary>
    /// Finds the first.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting subgraph match.</returns>
    public SubgraphMatch FindFirstQuery(Molecule target, QueryMolecule query)
    {
        return Matcher.FindFirstQuery(target, query);
    }

    /// <summary>
    /// Returns every unique atom mapping that satisfies the target and query graphs.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="smarts">The smarts.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<SubgraphMatch> FindAll(Molecule target, string smarts)
    {
        SmartsParseResult result = Parser.ParseWithResult(smarts);
        if (!result.Success)
        {
            return new List<SubgraphMatch>();
        }

        return Matcher.FindAllQuery(target, result.Query);
    }

    /// <summary>
    /// Returns every unique atom mapping that satisfies the target and query graphs.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="query">The query.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    public List<SubgraphMatch> FindAllQuery(Molecule target, QueryMolecule query)
    {
        return Matcher.FindAllQuery(target, query);
    }
}

