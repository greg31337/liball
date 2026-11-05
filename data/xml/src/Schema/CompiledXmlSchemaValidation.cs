namespace xml;

internal class XmlSchemaNodeAssessment
{
    internal XmlNode Node;
    internal CompiledXmlSchemaElementDeclaration? ElementDeclaration;
    internal CompiledXmlSchemaAttributeDeclaration? AttributeDeclaration;
    internal CompiledXmlSchemaSimpleType? SimpleType;
    internal CompiledXmlSchemaComplexType? ComplexType;
    internal XmlSchemaProcessContents ProcessContents;
    internal XmlSchemaValidity Validity;
    internal bool IsNilled;
    internal bool IsDefaulted;
    internal XmlNode? ValueContext;
    internal XmlSchemaSimpleTypeValidationResult? SimpleTypeValidation;

    internal XmlSchemaNodeAssessment(XmlNode node)
    {
        Node = node;
        ElementDeclaration = null;
        AttributeDeclaration = null;
        SimpleType = null;
        ComplexType = null;
        ProcessContents = XmlSchemaProcessContents.Strict;
        Validity = XmlSchemaValidity.NotKnown;
        IsNilled = false;
        IsDefaulted = false;
        ValueContext = null;
        SimpleTypeValidation = null;
    }
}

internal class XmlSchemaLocationHint
{
    internal string? NamespaceUri;
    internal string Location;
    internal string? AbsoluteUri;
    internal bool IsAllowed;
    internal XmlNode Node;

    internal XmlSchemaLocationHint(string? namespaceUri, string location, XmlNode node)
    {
        NamespaceUri = namespaceUri;
        Location = location;
        AbsoluteUri = null;
        IsAllowed = false;
        Node = node;
    }
}

internal class XmlSchemaAssessmentResult
{
    internal XmlSchemaValidity Validity;
    internal XmlSchemaNodeAssessment[] Assessments;
    internal XmlSchemaLocationHint[] LocationHints;
    internal XmlDiagnostic[] Diagnostics;
    internal XmlSchemaTypeInfo[] TypeAnnotations;
    internal bool Succeeded;

    internal XmlSchemaAssessmentResult()
    {
        Validity = XmlSchemaValidity.NotKnown;
        Assessments = [];
        LocationHints = [];
        Diagnostics = [];
        TypeAnnotations = [];
        Succeeded = true;
    }
}

internal class XmlSchemaSelectedType
{
    internal CompiledXmlSchemaSimpleType? SimpleType;
    internal CompiledXmlSchemaComplexType? ComplexType;

    internal XmlSchemaSelectedType()
    {
        SimpleType = null;
        ComplexType = null;
    }
}

internal class XmlSchemaParticleMatchState
{
    internal int Position;
    internal CompiledXmlSchemaElementDeclaration?[] Declarations;
    internal XmlSchemaProcessContents[] Processing;

    internal XmlSchemaParticleMatchState(int position, int count)
    {
        Position = position;
        Declarations = new CompiledXmlSchemaElementDeclaration?[count];
        Processing = new XmlSchemaProcessContents[count];
    }
}
