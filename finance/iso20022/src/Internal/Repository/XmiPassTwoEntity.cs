using System.Collections.Generic;

namespace iso20022;

internal class XmiPassTwoEntity
{
    internal string Identifier = string.Empty;
    internal string TypeName = string.Empty;
    internal string SourceTypeName = string.Empty;
    internal string SourceTypeNamespaceUri = string.Empty;
    internal RepositoryEntityKind Kind;
    internal QualifiedNameData ElementName = new QualifiedNameData();
    internal List<XmiPassTwoAttribute> Attributes = new List<XmiPassTwoAttribute>();
    internal SourceLocation Location = new SourceLocation();
    internal int DocumentOrder;
    internal int Depth;
    internal int ParentDocumentOrder = -1;
    internal string ParentIdentifier = string.Empty;
    internal string TopLevelPartitionIdentifier = string.Empty;
    internal RepositoryEntityKind TopLevelPartitionKind;
    internal bool IsElementAttribute;
    internal XmiPassTwoAttribute? ElementAttribute;
    internal string TextValue = string.Empty;
    internal Dictionary<string, int>? ContainmentPositions;

    internal int TakeContainmentPosition(string featureName)
    {
        Dictionary<string, int>? positions = ContainmentPositions;
        if (positions == null)
        {
            positions = new Dictionary<string, int>(System.StringComparer.Ordinal);
            ContainmentPositions = positions;
        }

        int position = 0;
        if (positions.ContainsKey(featureName))
        {
            position = positions[featureName];
        }

        positions[featureName] = position + 1;
        return position;
    }
}
