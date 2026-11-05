using System.Collections.Generic;

namespace iso20022;

internal class StreamItemFragment
{
    internal string Xml = string.Empty;
    internal QualifiedNameData Name = new QualifiedNameData();
    internal XmlPathData Path = new XmlPathData();
    internal SourceLocation Location = new SourceLocation();
    internal Dictionary<string, string> Namespaces =
        new Dictionary<string, string>(System.StringComparer.Ordinal);
}
