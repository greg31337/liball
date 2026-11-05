namespace iso20022;

/// <summary>Identifies one caller-selected stage of high-level message processing.</summary>
public enum ProcessingStage
{
    /// <summary>The processing stage is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>Detects the physical layout, namespace, and message identifier.</summary>
    Detection = 1,
    /// <summary>Reads the selected generic or generated message representation.</summary>
    Reading = 2,
    /// <summary>Validates the current message representation.</summary>
    Validation = 3,
    /// <summary>Extracts stable business metadata.</summary>
    MetadataExtraction = 4,
    /// <summary>Migrates the message to an exact target version.</summary>
    Migration = 5,
    /// <summary>Maps the message or canonical model through explicit rules.</summary>
    Mapping = 6,
    /// <summary>Writes the final message representation.</summary>
    Writing = 7
}
