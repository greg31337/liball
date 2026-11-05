namespace version;

/// <summary>
/// Represents a maven dependency management.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class MavenDependencyManagement
{
    /// <summary>
    /// Stores the <c>managedDependencies</c> value.
    /// </summary>
    public MavenDependency[] managedDependencies;
    /// <summary>
    /// Stores the <c>importedBoms</c> value.
    /// </summary>
    public MavenImportedBomConstraint[] importedBoms;

    /// <summary>
    /// Initializes a new Maven Dependency Management instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="managedDependencyValues">The managed dependency values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="importedBomValues">The imported bom values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public MavenDependencyManagement(
        MavenDependency[] managedDependencyValues,
        MavenImportedBomConstraint[] importedBomValues)
    {
        System.ArgumentNullException.ThrowIfNull(managedDependencyValues);
        System.ArgumentNullException.ThrowIfNull(importedBomValues);

        managedDependencies = CopyDependencies(managedDependencyValues);
        importedBoms = CopyImportedBoms(importedBomValues);
    }

    /// <summary>
    /// Creates an empty.
    /// </summary>
    /// <returns>The resulting maven dependency management.</returns>
    internal static MavenDependencyManagement CreateEmpty()
    {
        return new MavenDependencyManagement(new MavenDependency[0], new MavenImportedBomConstraint[0]);
    }

    /// <summary>
    /// Gets a managed dependency count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetManagedDependencyCount()
    {
        return managedDependencies.Length;
    }

    /// <summary>
    /// Gets a managed dependency.
    /// </summary>
    /// <param name="dependencyIndex">The zero-based dependency index.</param>
    /// <returns>The resulting maven dependency.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal MavenDependency GetManagedDependency(int dependencyIndex)
    {
        if (dependencyIndex < 0 || dependencyIndex >= managedDependencies.Length)
        {
            throw new System.ArgumentOutOfRangeException("dependencyIndex");
        }

        return managedDependencies[dependencyIndex];
    }

    /// <summary>
    /// Gets a managed dependencies.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal MavenDependency[] GetManagedDependencies()
    {
        return CopyDependencies(managedDependencies);
    }

    /// <summary>
    /// Gets an imported bom count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetImportedBomCount()
    {
        return importedBoms.Length;
    }

    /// <summary>
    /// Gets an imported bom.
    /// </summary>
    /// <param name="importedBomIndex">The zero-based imported bom index.</param>
    /// <returns>The resulting maven imported bom constraint.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal MavenImportedBomConstraint GetImportedBom(int importedBomIndex)
    {
        if (importedBomIndex < 0 || importedBomIndex >= importedBoms.Length)
        {
            throw new System.ArgumentOutOfRangeException("importedBomIndex");
        }

        return importedBoms[importedBomIndex];
    }

    /// <summary>
    /// Gets an imported boms.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal MavenImportedBomConstraint[] GetImportedBoms()
    {
        return CopyImportedBoms(importedBoms);
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

    internal static MavenImportedBomConstraint[] CopyImportedBoms(MavenImportedBomConstraint[] importedBomValues)
    {
        MavenImportedBomConstraint[] copy = new MavenImportedBomConstraint[importedBomValues.Length];
        int index = 0;

        while (index < importedBomValues.Length)
        {
            if (importedBomValues[index] == null)
            {
                throw new System.ArgumentException("Maven imported BOM arrays cannot contain null values.");
            }

            copy[index] = importedBomValues[index];
            index = index + 1;
        }

        return copy;
    }
}
