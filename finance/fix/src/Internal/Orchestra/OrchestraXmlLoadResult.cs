using System.Collections.Generic;
using System.Xml;

namespace fix;

internal class OrchestraXmlLoadResult
{
    internal bool Success;
    internal XmlDocument? Document;
    internal XmlElement? Root;
    internal OrchestraVersion Version;
    internal bool InterfacesDocument;
    internal string NamespaceUri = "";
    internal List<OrchestraDiagnostic> Diagnostics = [];
    internal Dictionary<XmlNode, OrchestraXmlSourceLocation> Locations = [];

    internal OrchestraXmlSourceLocation? GetLocation(XmlNode node)
    {
        if (node != null && Locations.ContainsKey(node)) return Locations[node];
        return null;
    }
}
