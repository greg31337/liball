namespace docx;

/// <summary>
/// Provides package-root and part-scoped relationship operations.
/// </summary>
public interface IDocxPackageRelationships
{
    /// <summary>
    /// Returns relationships for the package root or a package part.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <returns>A new list containing the relationships in declaration order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source part name is malformed.</exception>
    List<DocxRelationship> GetRelationships(string sourcePartName);

    /// <summary>
    /// Finds a relationship by source and identifier.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>The matching relationship, or null when it is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source part name is malformed.</exception>
    DocxRelationship? FindRelationship(string sourcePartName, string relationshipId);

    /// <summary>
    /// Resolves an internal relationship target to a normalized package part name.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>The resolved part name, or an empty string when the relationship is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a source or target is malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the relationship target is external.</exception>
    string ResolveRelationshipTarget(string sourcePartName, string relationshipId);

    /// <summary>
    /// Allocates the next available relationship identifier for a source.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or an existing package part name for part relationships.</param>
    /// <returns>The next available relationship identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source part name is malformed.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a non-root source part does not exist.</exception>
    string AllocateRelationshipId(string sourcePartName);

    /// <summary>
    /// Creates a relationship using the next available identifier.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or an existing package part name for part relationships.</param>
    /// <param name="type">The relationship type URI.</param>
    /// <param name="target">The relative or absolute target value.</param>
    /// <param name="external">Whether the target is external to the package.</param>
    /// <returns>The created relationship.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a non-root source part does not exist.</exception>
    DocxRelationship CreateRelationship(string sourcePartName, string type, string target, bool external);

    /// <summary>
    /// Creates a relationship with a caller-supplied identifier.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or an existing package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <param name="type">The relationship type URI.</param>
    /// <param name="target">The relative or absolute target value.</param>
    /// <param name="external">Whether the target is external to the package.</param>
    /// <returns>The created relationship.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the identifier already exists for the source.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a non-root source part does not exist.</exception>
    DocxRelationship CreateRelationshipWithId(string sourcePartName, string relationshipId, string type, string target, bool external);

    /// <summary>
    /// Updates an existing relationship.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <param name="type">The replacement relationship type URI.</param>
    /// <param name="target">The replacement relative or absolute target value.</param>
    /// <param name="external">Whether the replacement target is external to the package.</param>
    /// <returns>Whether the relationship existed and was updated.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty or malformed.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when a non-root source part does not exist.</exception>
    bool UpdateRelationship(string sourcePartName, string relationshipId, string type, string target, bool external);

    /// <summary>
    /// Removes a relationship by source and identifier.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>Whether the relationship existed and was removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the source part name is malformed.</exception>
    bool RemoveRelationship(string sourcePartName, string relationshipId);

    /// <summary>
    /// Returns internal relationships whose resolved target parts are absent.
    /// </summary>
    /// <returns>A new list containing the orphan relationship descriptions.</returns>
    List<DocxOrphanRelationship> GetOrphanRelationships();

    /// <summary>
    /// Indicates whether a relationship has a missing internal target part.
    /// </summary>
    /// <param name="sourcePartName">Use <c>/</c> for package relationships or a package part name for part relationships.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <returns>Whether the existing relationship has a missing internal target.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a source or target is malformed.</exception>
    bool IsOrphanRelationship(string sourcePartName, string relationshipId);
}
