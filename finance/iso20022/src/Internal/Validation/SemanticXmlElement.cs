using System.Collections.Generic;

namespace iso20022;

internal class SemanticXmlElement
{
    internal DynamicXmlNode Node = new DynamicXmlNode();
    internal SemanticXmlElement? Parent;
    internal List<SemanticXmlElement> Children = new List<SemanticXmlElement>();
    internal string Path = string.Empty;
}
