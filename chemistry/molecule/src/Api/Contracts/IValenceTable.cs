using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Defines common-valence lookup operations.
/// </summary>
public interface IValenceTable
{
    /// <summary>
    /// Returns a copy of the common valences registered for an element.
    /// </summary>
    /// <param name="atomicNumber">The element's atomic number.</param>
    /// <returns>The common valences in table order, or an empty list for an unknown element.</returns>
    List<int> GetCommonValences(int atomicNumber);

    /// <summary>
    /// Determines whether the valence is allowed for an element.
    /// </summary>
    /// <param name="atomicNumber">The atomic number.</param>
    /// <param name="valence">The valence.</param>
    /// <returns><c>true</c> when the valence is allowed; otherwise, <c>false</c>.</returns>
    bool IsAllowedValence(int atomicNumber, int valence);
}
