namespace odf;

internal static class OdfSchemaRuleCatalog
{
    internal static SchemaRuleSet[] GetVersionRuleSets(OdfVersion version)
    {
        if (version == OdfVersion.Version10)
        {
            return Odf10Rules.AllRuleSets;
        }
        if (version == OdfVersion.Version11)
        {
            return Odf11Rules.AllRuleSets;
        }
        if (version == OdfVersion.Version12)
        {
            return Odf12Rules.AllRuleSets;
        }
        if (version == OdfVersion.Version13)
        {
            return Odf13Rules.AllRuleSets;
        }
        if (version == OdfVersion.Version14)
        {
            return Odf14Rules.AllRuleSets;
        }
        return [];
    }

    internal static SchemaRuleSet? GetRuleSet(
        OdfVersion version,
        string profile)
    {
        SchemaRuleSet[] sets = GetVersionRuleSets(version);
        foreach (SchemaRuleSet set in sets)
        {
            if (set.Profile == profile)
            {
                return set;
            }
        }
        return null;
    }

    internal static SchemaVersionDelta? GetVersionDelta(
        OdfVersion previousVersion,
        OdfVersion currentVersion)
    {
        if (previousVersion == OdfVersion.Version10
            && currentVersion == OdfVersion.Version11)
        {
            return Odf11Rules.DeltasFrom10;
        }
        if (previousVersion == OdfVersion.Version11
            && currentVersion == OdfVersion.Version12)
        {
            return Odf12Rules.DeltasFrom11;
        }
        if (previousVersion == OdfVersion.Version12
            && currentVersion == OdfVersion.Version13)
        {
            return Odf13Rules.DeltasFrom12;
        }
        if (previousVersion == OdfVersion.Version13
            && currentVersion == OdfVersion.Version14)
        {
            return Odf14Rules.DeltasFrom13;
        }
        return null;
    }
}
