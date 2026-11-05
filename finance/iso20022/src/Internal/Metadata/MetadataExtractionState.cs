using System.Collections.Generic;

namespace iso20022;

internal class MetadataExtractionState
{
    internal MetadataExtractionOptions Options;
    internal MessageMetadata Metadata;
    internal ResourceLimitGuard Guard;
    internal List<QualifiedNameData> Path = new List<QualifiedNameData>();

    internal MetadataExtractionState(
        MetadataExtractionOptions options,
        MessageMetadata metadata,
        ResourceLimitGuard guard)
    {
        Options = options;
        Metadata = metadata;
        Guard = guard;
    }
}
