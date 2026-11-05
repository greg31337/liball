using System.Collections.Generic;

namespace iso20022;

internal class CSharpMemberMetadata
{
    internal string Name = string.Empty;
    internal string XmlName = string.Empty;
    internal string TypeName = string.Empty;
    internal int MinimumOccurs;
    internal bool HasMaximumOccurs;
    internal int MaximumOccurs;
    internal bool IsValueType;
    internal string PresenceName = string.Empty;
    internal GenerationSymbolEntry? ReferencedSymbol;
    internal string SourceIdentifier = string.Empty;
    internal List<ConstraintDefinition> Constraints = new List<ConstraintDefinition>();
}
