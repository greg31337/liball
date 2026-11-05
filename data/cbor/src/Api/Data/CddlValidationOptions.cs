namespace cbor;

/// <summary>Holds CDDL validation selection, reporting, feature, and budget policies.</summary>
public class CddlValidationOptions
{
    /// <summary>An explicit root rule name, or null to use the compiled root.</summary>
    public string? RootRuleName;
    /// <summary>The enabled feature names.</summary>
    public string[] EnabledFeatures = [];
    /// <summary>Specifies how unregistered or disabled control operators are handled.</summary>
    public CddlUnknownControlPolicy UnknownControlPolicy =
        CddlUnknownControlPolicy.Reject;
    /// <summary>Indicates whether validation collects multiple issues.</summary>
    public bool CollectIssues = true;
    /// <summary>The maximum number of returned validation issues.</summary>
    public int MaxIssues = 100;
    /// <summary>The maximum validation rule and data nesting depth.</summary>
    public int MaxNestingDepth = 256;
    /// <summary>The maximum number of validation steps.</summary>
    public int MaxSteps = 1000000;
    /// <summary>The maximum number of explored alternatives.</summary>
    public int MaxAlternatives = 100000;
    /// <summary>The maximum number of memoized validation states.</summary>
    public int MaxMemoEntries = 100000;
    /// <summary>The maximum total decoded bytes for embedded-data and text-conversion controls.</summary>
    public int MaxEmbeddedBytes = 16777216;
    /// <summary>The maximum number of pattern, formatted-text, or converted-number matching states.</summary>
    public int MaxPatternStates = 100000;
}
