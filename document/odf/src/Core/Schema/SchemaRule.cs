namespace odf;

internal class SchemaNamespaceRule
{
    internal string Prefix = "";
    internal string NamespaceName = "";
}

internal class SchemaRule
{
    internal string Name = "";
    internal string Combine = "";
    internal bool IsStart;
    internal int PatternIndex;
}

internal class SchemaElementRule
{
    internal string NamespaceName = "";
    internal string LocalName = "";
    internal int PatternIndex;
    internal int OwnerRuleIndex;
    internal int[] ContentPatternIndexes = [];
}

internal class SchemaAttributeRule
{
    internal string NamespaceName = "";
    internal string LocalName = "";
    internal int PatternIndex;
    internal int OwnerRuleIndex;
    internal int[] ValuePatternIndexes = [];
}

internal class SchemaOccurrenceRule
{
    internal int PatternIndex;
    internal int Minimum;
    internal int Maximum;
}

internal class SchemaRuleSet
{
    internal OdfVersion Version;
    internal string Profile = "";
    internal string SourceName = "";
    internal string DefaultNamespace = "";
    internal string DatatypeLibrary = "";
    internal SchemaNamespaceRule[] Namespaces = [];
    internal SchemaRule[] Rules = [];
    internal SchemaPattern[] Patterns = [];
    internal int[] PatternChildIndexes = [];
    internal SchemaElementRule[] Elements = [];
    internal SchemaAttributeRule[] Attributes = [];
    internal SchemaOccurrenceRule[] Occurrences = [];
    internal SchemaDatatype[] Datatypes = [];
}
