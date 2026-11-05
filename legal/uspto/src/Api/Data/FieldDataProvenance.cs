namespace uspto;

/// <summary>
/// Labels the origin and delivery path of one field in a returned data object.
/// </summary>
public class FieldDataProvenance
{
    /// <summary>The dot-separated field path, or <c>*</c> when the label applies to every field.</summary>
    public string? FieldPath = null;

    /// <summary>The service or dataset from which the field value originated.</summary>
    public DataProvenance Source = DataProvenance.Unknown;

    /// <summary>The direct, cache, local-mirror, or client-derived path through which the field was delivered.</summary>
    public DataProvenance Delivery = DataProvenance.Unknown;

    /// <summary>The source dataset or product identifier.</summary>
    public string? DatasetIdentifier = null;

    /// <summary>The source dataset version.</summary>
    public string? DatasetVersion = null;

    /// <summary>Whether the originating source is authoritative for the labeled field.</summary>
    public bool IsAuthoritative = false;
}
