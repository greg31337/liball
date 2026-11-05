using System.Collections.Generic;

namespace iso20022;

internal class BuiltInSemanticIndex
{
    internal ValidationRequest Request = new ValidationRequest();
    internal ResourceLimits Limits = new ResourceLimits();
    internal List<SemanticXmlElement> Elements = new List<SemanticXmlElement>();
    internal List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    internal int ElementCount;
    internal bool Continue = true;
}
