namespace tiff;

/// <summary>Defines a mutable registry of TIFF tag definitions.</summary>
public interface ITiffTagRegistry
{
    /// <summary>Returns the definition count.</summary>
    /// <returns>The definition count.</returns>
    int Count();
    /// <summary>Creates a detached registry copy.</summary>
    /// <returns>The copied registry.</returns>
    TiffTagRegistry Copy();
    /// <summary>Checks for a tag definition.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <returns><c>true</c> when registered.</returns>
    bool Contains(int tagId);
    /// <summary>Finds a tag definition.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <returns>The definition, or <c>null</c>.</returns>
    TiffTagDefinition? Find(int tagId);
    /// <summary>Registers a tag definition.</summary>
    /// <param name="definition">The definition.</param>
    void Register(TiffTagDefinition definition);
    /// <summary>Registers a tag with one permitted data type.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="name">The tag name.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="expectedCount">The expected count.</param>
    void RegisterKnown(int tagId, string name, TiffDataType dataType, long expectedCount);
    /// <summary>Registers a tag with multiple permitted data types.</summary>
    /// <param name="tagId">The tag identifier.</param>
    /// <param name="name">The tag name.</param>
    /// <param name="preferredDataType">The preferred data type.</param>
    /// <param name="allowedDataTypes">The permitted data types.</param>
    /// <param name="expectedCount">The expected count.</param>
    void RegisterKnownTypes(int tagId, string name, TiffDataType preferredDataType, TiffDataType[] allowedDataTypes, long expectedCount);
}
