using System.Collections.Generic;

namespace odf;

internal class OdfMediaTypeValue
{
    internal string LexicalValue = "";
    internal string Type = "";
    internal string Subtype = "";
    internal List<string> ParameterNames = new List<string>();
    internal List<string> ParameterValues = new List<string>();
    internal bool IsValid;
}
