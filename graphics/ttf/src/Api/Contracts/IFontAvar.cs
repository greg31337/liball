namespace ttf;

/// <summary>Defines public <c>avar</c> metadata.</summary>
public interface IFontAvar
{
    /// <summary>Gets the major table version.</summary>
    /// <returns>The major version.</returns>
    int GetMajorVersion();

    /// <summary>Gets the minor table version.</summary>
    /// <returns>The minor version.</returns>
    int GetMinorVersion();

    /// <summary>Gets the number of axis segment maps.</summary>
    /// <returns>The map count.</returns>
    int GetAxisMapCount();

    /// <summary>Gets one axis segment map.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The axis map.</returns>
    FontAvarAxisMap GetAxisMap(int index);

    /// <summary>Finds an axis segment map by tag.</summary>
    /// <param name="axisTag">The big-endian axis tag.</param>
    /// <returns>The map, or null.</returns>
    FontAvarAxisMap? FindAxisMap(int axisTag);

    /// <summary>Reports whether version-two coordinate-dependent mappings are present.</summary>
    /// <returns>True when a variation store is present; otherwise false.</returns>
    bool HasVariableMappings();
}
