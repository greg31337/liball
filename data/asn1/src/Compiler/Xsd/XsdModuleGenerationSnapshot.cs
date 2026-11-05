namespace asn1;

internal class XsdSavedDeclarationState
{
    internal XsdDeclarationDefinition? Value;
    internal string Identifier = "";
    internal string TypeReferenceName = "";
    internal string AsnTypeDefinition = "";
    internal string OccurrenceTypeDefinition = "";
}

internal class XsdSavedSimpleTypeState
{
    internal XsdSimpleTypeDefinition? Value;
    internal string AsnTypeDefinition = "";
    internal string[] EnumerationIdentifiers = [];
}

internal class XsdSavedSimpleUseState
{
    internal XsdSimpleTypeUse? Value;
    internal string AsnTypeDefinition = "";
}

internal class XsdSavedComplexTypeState
{
    internal XsdComplexTypeDefinition? Value;
    internal string TypeReferenceName = "";
    internal string AsnTypeDefinition = "";
}

internal class XsdSavedGroupState
{
    internal XsdModelGroupDefinition? Value;
    internal string TypeReferenceName = "";
    internal string AsnTypeDefinition = "";
}

internal class XsdSavedParticleState
{
    internal XsdParticleDefinition? Value;
    internal string Identifier = "";
    internal string AsnTypeDefinition = "";
    internal string NamedTypeDefinition = "";
}

internal class XsdSavedTypeSubstitutionState
{
    internal XsdTypeSubstitutionDefinition? Value;
    internal string TypeReferenceName = "";
}

internal class XsdSavedSubstitutionGroupState
{
    internal XsdSubstitutionGroupDefinition? Value;
    internal string TypeReferenceName = "";
}
