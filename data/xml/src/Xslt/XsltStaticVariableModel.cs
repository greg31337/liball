namespace xml;

internal class XsltStaticVariable
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XmlName Name;
    internal string ExpandedName;
    internal string? Select;
    internal XdmSequence Value;
    internal bool IsParameter;
    internal bool IsRequired;
    internal bool WasSupplied;
    internal int ImportPrecedence;
    internal int DeclarationOrder;

    internal XsltStaticVariable(XmlNode declaration, XsltStylesheetModule module, XmlName name, string expandedName, XdmSequence value)
    {
        Declaration = declaration;
        Module = module;
        Name = name;
        ExpandedName = expandedName;
        Select = null;
        Value = value;
        IsParameter = false;
        IsRequired = false;
        WasSupplied = false;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
    }
}
