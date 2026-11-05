namespace odf;

internal class VersionMigrationStep
{
    internal OdfVersion SourceVersion;
    internal OdfVersion TargetVersion;
    internal bool IsDowngrade;
    internal int AddedCount;
    internal int RemovedCount;
    internal int ChangedCount;
    internal int IncompatibleConstructCount;
    internal SchemaRuleDelta[] Changes = [];
}
