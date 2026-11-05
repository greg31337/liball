namespace xml;

internal class XsltAttributeSetAttributePlan
{
    internal XmlNode Instruction;
    internal XsltSequenceItemPlan Item;
    internal XsltAttributeSetDeclarationPlan Declaration;

    internal XsltAttributeSetAttributePlan(XmlNode instruction, XsltSequenceItemPlan item, XsltAttributeSetDeclarationPlan declaration)
    {
        Instruction = instruction;
        Item = item;
        Declaration = declaration;
    }
}

internal class XsltAttributeSetDeclarationPlan
{
    internal XmlNode Declaration;
    internal XsltStylesheetModule Module;
    internal XsltStaticContext StaticContext;
    internal string Name;
    internal string[] UsedNames;
    internal XsltAttributeSetPlan[] UsedSets;
    internal XsltAttributeSetAttributePlan[] Attributes;
    internal bool HasStreamable;
    internal bool Streamable;
    internal bool HasVisibility;
    internal XsltComponentVisibility Visibility;
    internal int ImportPrecedence;
    internal int DeclarationOrder;
    internal XsltComponent? Component;

    internal XsltAttributeSetDeclarationPlan(XmlNode declaration, XsltStylesheetModule module, XsltStaticContext staticContext, string name)
    {
        Declaration = declaration;
        Module = module;
        StaticContext = staticContext;
        Name = name;
        UsedNames = [];
        UsedSets = [];
        Attributes = [];
        HasStreamable = false;
        Streamable = false;
        HasVisibility = false;
        Visibility = XsltComponentVisibility.Private;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
        Component = null;
    }
}

internal class XsltAttributeSetPlan
{
    internal string Name;
    internal XmlNode Origin;
    internal XsltStylesheetModule Module;
    internal XsltAttributeSetDeclarationPlan[] Declarations;
    internal XsltAttributeSetAttributePlan[] ExpandedAttributes;
    internal bool Streamable;
    internal int ImportPrecedence;
    internal int DeclarationOrder;
    internal XsltComponent? Component;
    internal int ExpansionState;

    internal XsltAttributeSetPlan(string name, XmlNode origin, XsltStylesheetModule module)
    {
        Name = name;
        Origin = origin;
        Module = module;
        Declarations = [];
        ExpandedAttributes = [];
        Streamable = false;
        ImportPrecedence = -1;
        DeclarationOrder = -1;
        Component = null;
        ExpansionState = 0;
    }
}

internal class XsltAttributeSetUsePlan
{
    internal XmlNode Node;
    internal XmlNode Attribute;
    internal XsltStylesheetModule Module;
    internal string[] Names;
    internal XsltAttributeSetPlan[] Sets;
    internal XsltAttributeSetAttributePlan[] ExpandedAttributes;

    internal XsltAttributeSetUsePlan(XmlNode node, XmlNode attribute, XsltStylesheetModule module)
    {
        Node = node;
        Attribute = attribute;
        Module = module;
        Names = [];
        Sets = [];
        ExpandedAttributes = [];
    }
}
