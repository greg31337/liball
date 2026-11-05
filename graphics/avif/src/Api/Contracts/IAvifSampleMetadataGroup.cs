namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifSampleMetadataGroup"/>.</summary>
public interface IAvifSampleMetadataGroup
{
    /// <summary>Reports whether a metadata grouping parameter was provided.</summary>
    /// <returns><c>true</c> when the SampleToGroupBox uses version one.</returns>
    bool HasParameter();

    /// <summary>Returns the complete metadata grouping parameter.</summary>
    /// <returns>The complete 32-bit parameter, or zero when absent.</returns>
    uint GetParameter();

    /// <summary>Returns the AV1 metadata type encoded in the parameter.</summary>
    /// <returns>The eight-bit metadata type, or minus one when the parameter is absent.</returns>
    int GetMetadataType();

    /// <summary>Returns the metadata-specific parameter bits.</summary>
    /// <returns>The low 24 bits, or zero when the parameter is absent.</returns>
    uint GetMetadataSpecificParameters();
}
