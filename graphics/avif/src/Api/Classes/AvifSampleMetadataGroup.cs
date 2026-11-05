namespace avif;

/// <summary>Describes one AV1 metadata sample-group membership.</summary>
public class AvifSampleMetadataGroup : IAvifSampleMetadataGroup
{
    bool hasParameter;
    uint parameter;

    internal AvifSampleMetadataGroup(ParsedAv1MetadataGroup parsed)
    {
        hasParameter = parsed.HasParameter;
        parameter = parsed.Parameter;
    }

    /// <summary>Reports whether a metadata grouping parameter was provided.</summary>
    /// <returns><c>true</c> when the SampleToGroupBox uses version one.</returns>
    public bool HasParameter()
    {
        return hasParameter;
    }

    /// <summary>Returns the complete metadata grouping parameter.</summary>
    /// <returns>The complete 32-bit parameter, or zero when absent.</returns>
    public uint GetParameter()
    {
        return parameter;
    }

    /// <summary>Returns the AV1 metadata type encoded in the parameter.</summary>
    /// <returns>The eight-bit metadata type, or minus one when the parameter is absent.</returns>
    public int GetMetadataType()
    {
        return hasParameter ? (int)(parameter >> 24) : -1;
    }

    /// <summary>Returns the metadata-specific parameter bits.</summary>
    /// <returns>The low 24 bits, or zero when the parameter is absent.</returns>
    public uint GetMetadataSpecificParameters()
    {
        return hasParameter ? parameter & 16777215U : 0;
    }
}
