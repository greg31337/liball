namespace ttf;

/// <summary>Defines conditional OpenType FeatureVariations discovery.</summary>
public interface IFontLayoutFeatureVariations
{
    /// <summary>Gets the major FeatureVariations version.</summary>
    /// <returns>The major version.</returns>
    int GetMajorVersion();

    /// <summary>Gets the minor FeatureVariations version.</summary>
    /// <returns>The minor version.</returns>
    int GetMinorVersion();

    /// <summary>Gets the variation-record count.</summary>
    /// <returns>The record count.</returns>
    int GetRecordCount();

    /// <summary>Gets one record's condition count.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The condition count.</returns>
    int GetConditionCount(int recordIndex);

    /// <summary>Gets one condition format.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="conditionIndex">The zero-based condition index.</param>
    /// <returns>The condition format.</returns>
    int GetConditionFormat(int recordIndex, int conditionIndex);

    /// <summary>Gets one format 1 condition's axis index.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="conditionIndex">The zero-based condition index.</param>
    /// <returns>The variation-axis index.</returns>
    int GetConditionAxisIndex(int recordIndex, int conditionIndex);

    /// <summary>Gets one format 1 condition's inclusive minimum.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="conditionIndex">The zero-based condition index.</param>
    /// <returns>The normalized minimum.</returns>
    double GetConditionMinimum(int recordIndex, int conditionIndex);

    /// <summary>Gets one format 1 condition's inclusive maximum.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="conditionIndex">The zero-based condition index.</param>
    /// <returns>The normalized maximum.</returns>
    double GetConditionMaximum(int recordIndex, int conditionIndex);

    /// <summary>Gets one record's feature-substitution count.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The substitution count.</returns>
    int GetSubstitutionCount(int recordIndex);

    /// <summary>Gets one substituted feature-list index.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="substitutionIndex">The zero-based substitution index.</param>
    /// <returns>The feature-list index.</returns>
    int GetSubstitutedFeatureIndex(int recordIndex, int substitutionIndex);

    /// <summary>Gets one alternate feature table.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="substitutionIndex">The zero-based substitution index.</param>
    /// <returns>The alternate feature model.</returns>
    FontLayoutFeature GetAlternateFeature(int recordIndex, int substitutionIndex);

    /// <summary>Reports whether all supported conditions in a record match.</summary>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <param name="coordinates">The final normalized coordinates.</param>
    /// <returns>True when the record matches; otherwise false.</returns>
    bool Matches(int recordIndex, NormalizedFontCoordinates coordinates);

    /// <summary>Finds the first matching variation record.</summary>
    /// <param name="coordinates">The final normalized coordinates.</param>
    /// <returns>The first matching record index, or -1.</returns>
    int FindMatchingRecord(NormalizedFontCoordinates coordinates);
}
