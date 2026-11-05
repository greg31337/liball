namespace xml;

internal class XsltStreamabilityClassification
{
    internal XsltPosture Posture;
    internal XsltSweep Sweep;
    internal int ConsumingOperandCount;
    internal bool Guaranteed;
    internal string Reason;

    internal XsltStreamabilityClassification(XsltPosture posture, XsltSweep sweep, int consumingOperandCount, bool guaranteed, string reason)
    {
        Posture = posture;
        Sweep = sweep;
        ConsumingOperandCount = consumingOperandCount;
        Guaranteed = guaranteed;
        Reason = reason;
    }
}

internal class XsltStreamabilityEntry
{
    internal string Kind;
    internal string Name;
    internal object Subject;
    internal SourceRange? Range;
    internal XsltStreamabilityClassification Classification;
    internal bool Required;

    internal XsltStreamabilityEntry(string kind, string name, object subject, SourceRange? range, XsltStreamabilityClassification classification, bool required)
    {
        Kind = kind;
        Name = name;
        Subject = subject;
        Range = range;
        Classification = classification;
        Required = required;
    }
}

internal class XsltStreamabilityAnalysis
{
    internal XsltStreamabilityEntry[] Entries;
    internal XsltStreamabilityClassification Package;
    internal string[] Reasons;
    internal SourceRange[] Locations;
    internal XmlName[] Modes;

    internal XsltStreamabilityAnalysis(XsltStreamabilityEntry[] entries, XsltStreamabilityClassification package, string[] reasons, SourceRange[] locations, XmlName[] modes)
    {
        Entries = entries;
        Package = package;
        Reasons = reasons;
        Locations = locations;
        Modes = modes;
    }
}
