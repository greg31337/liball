namespace version;

/// <summary>
/// Represents a maven dependency model.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenDependencyModel
{
    /// <summary>
    /// Stores the <c>identity</c> value.
    /// </summary>
    public MavenArtifactVersionIdentity identity;
    /// <summary>
    /// Stores the <c>parentReference</c> value.
    /// </summary>
    public MavenParentReference? parentReference;
    /// <summary>
    /// Stores the <c>dependencies</c> value.
    /// </summary>
    public MavenDependency[] dependencies;
    /// <summary>
    /// Stores the <c>dependencyManagement</c> value.
    /// </summary>
    public MavenDependencyManagement dependencyManagement;

    /// <summary>
    /// Initializes a new Maven Dependency Model instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="identityValue">The identity value used by the operation.</param>
    /// <param name="parentReferenceValue">The parent reference value used by the operation.</param>
    /// <param name="dependencyValues">The dependency values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="dependencyManagementValue">The dependency management value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public MavenDependencyModel(
        MavenArtifactVersionIdentity identityValue,
        MavenParentReference? parentReferenceValue,
        MavenDependency[] dependencyValues,
        MavenDependencyManagement dependencyManagementValue)
    {
        System.ArgumentNullException.ThrowIfNull(identityValue);
        System.ArgumentNullException.ThrowIfNull(dependencyValues);
        System.ArgumentNullException.ThrowIfNull(dependencyManagementValue);

        identity = identityValue;
        parentReference = parentReferenceValue;
        dependencies = CopyDependencies(dependencyValues);
        dependencyManagement = dependencyManagementValue;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="identity">The identity used by the operation.</param>
    /// <param name="dependencies">The dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting maven dependency model.</returns>
    internal static MavenDependencyModel Create(
        MavenArtifactVersionIdentity identity,
        MavenDependency[] dependencies)
    {
        return new MavenDependencyModel(identity, null, dependencies, MavenDependencyManagement.CreateEmpty());
    }

    /// <summary>
    /// Gets an identity.
    /// </summary>
    /// <returns>The resulting maven artifact version identity.</returns>
    internal MavenArtifactVersionIdentity GetIdentity()
    {
        return identity;
    }

    /// <summary>
    /// Reports whether this value has parent reference.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasParentReference()
    {
        return parentReference != null;
    }

    /// <summary>
    /// Gets a parent reference.
    /// </summary>
    /// <returns>The resulting maven parent reference.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal MavenParentReference GetParentReference()
    {
        if (parentReference == null)
        {
            throw new System.InvalidOperationException("Maven dependency model does not contain a parent reference.");
        }

        return parentReference;
    }

    /// <summary>
    /// Gets a dependency count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetDependencyCount()
    {
        return dependencies.Length;
    }

    /// <summary>
    /// Gets a dependency.
    /// </summary>
    /// <param name="dependencyIndex">The zero-based dependency index.</param>
    /// <returns>The resulting maven dependency.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal MavenDependency GetDependency(int dependencyIndex)
    {
        if (dependencyIndex < 0 || dependencyIndex >= dependencies.Length)
        {
            throw new System.ArgumentOutOfRangeException("dependencyIndex");
        }

        return dependencies[dependencyIndex];
    }

    /// <summary>
    /// Gets a dependencies.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal MavenDependency[] GetDependencies()
    {
        return CopyDependencies(dependencies);
    }

    /// <summary>
    /// Gets a dependency management.
    /// </summary>
    /// <returns>The resulting maven dependency management.</returns>
    internal MavenDependencyManagement GetDependencyManagement()
    {
        return dependencyManagement;
    }

    internal static MavenDependency[] CopyDependencies(MavenDependency[] dependencyValues)
    {
        MavenDependency[] copy = new MavenDependency[dependencyValues.Length];
        int index = 0;

        while (index < dependencyValues.Length)
        {
            if (dependencyValues[index] == null)
            {
                throw new System.ArgumentException("Maven dependency arrays cannot contain null values.");
            }

            copy[index] = dependencyValues[index];
            index = index + 1;
        }

        return copy;
    }
}
