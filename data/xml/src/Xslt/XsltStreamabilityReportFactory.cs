namespace xml;

internal class XsltStreamabilityReportFactory
{
    internal XsltStreamabilityReport CreateXsltStreamabilityReport(XsltStreamabilityAnalysis analysis, XsltConformanceLevel conformanceLevel, bool schemaAware)
    {
        XsltStreamabilityConstruct[] constructs = CopyXsltStreamabilityConstructs(analysis.Entries);
        XsltStreamabilityReport result = new XsltStreamabilityReport();
        result.Streamability = analysis.Package.Guaranteed ? XsltStreamability.Streamable : XsltStreamability.NotStreamable;
        result.Reasons = (string[])analysis.Reasons.Clone();
        result.Locations = CopyXsltStreamabilityRanges(analysis.Locations);
        result.Modes = CopyXsltStreamabilityNames(analysis.Modes);
        result.Constructs = constructs;
        result.ConformanceLevel = conformanceLevel;
        result.RequiresFallback = HasXsltStreamabilityFallback(constructs);
        result.SchemaAware = schemaAware;
        return result;
    }

    internal XsltStreamabilityConstruct[] CopyXsltStreamabilityConstructs(XsltStreamabilityEntry[] entries)
    {
        XsltStreamabilityConstruct[] result = new XsltStreamabilityConstruct[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            XsltStreamabilityEntry entry = entries[index];
            XsltStreamabilityClassification classification = entry.Classification;
            XsltStreamabilityConstruct construct = new XsltStreamabilityConstruct();
            construct.Kind = entry.Kind;
            construct.Name = entry.Name;
            construct.Streamability = classification.Guaranteed ? XsltStreamability.Streamable : XsltStreamability.NotStreamable;
            construct.Posture = classification.Posture;
            construct.Sweep = classification.Sweep;
            construct.ConsumingOperandCount = classification.ConsumingOperandCount;
            construct.Reason = classification.Reason.Length == 0 ? null : classification.Reason;
            construct.Location = CopyXsltStreamabilityRange(entry.Range);
            construct.Required = entry.Required;
            construct.RequiresFallback = !classification.Guaranteed;
            result[index] = construct;
            index = index + 1;
        }

        return result;
    }

    internal bool HasXsltStreamabilityFallback(XsltStreamabilityConstruct[] constructs)
    {
        int index = 0;
        while (index < constructs.Length)
        {
            if (constructs[index].RequiresFallback)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal SourceRange[] CopyXsltStreamabilityRanges(SourceRange[] ranges)
    {
        SourceRange[] result = new SourceRange[ranges.Length];
        int index = 0;
        while (index < ranges.Length)
        {
            SourceRange? copy = CopyXsltStreamabilityRange(ranges[index]);
            result[index] = copy == null ? new SourceRange() : copy;
            index = index + 1;
        }

        return result;
    }

    internal SourceRange? CopyXsltStreamabilityRange(SourceRange? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceRange result = new SourceRange();
        result.Start = CopyXsltStreamabilityLocation(source.Start);
        result.End = CopyXsltStreamabilityLocation(source.End);
        return result;
    }

    internal SourceLocation? CopyXsltStreamabilityLocation(SourceLocation? source)
    {
        if (source == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = source.SourceUri;
        result.CharacterOffset = source.CharacterOffset;
        result.ByteOffset = source.ByteOffset;
        result.Line = source.Line;
        result.Column = source.Column;
        return result;
    }

    internal XmlName[] CopyXsltStreamabilityNames(XmlName[] names)
    {
        XmlName[] result = new XmlName[names.Length];
        int index = 0;
        while (index < names.Length)
        {
            XmlName source = names[index];
            XmlName copy = new XmlName();
            copy.Prefix = source.Prefix;
            copy.LocalName = source.LocalName;
            copy.NamespaceUri = source.NamespaceUri;
            copy.QualifiedName = source.QualifiedName;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }
}
