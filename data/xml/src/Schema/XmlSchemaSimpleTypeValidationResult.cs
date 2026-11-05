namespace xml;

internal class XmlSchemaSimpleTypeValidationResult
{
    internal bool Success;
    internal string NormalizedValue;
    internal string? CanonicalValue;
    internal XdmAtomicValue? AtomicValue;
    internal XdmAtomicValue[] AtomicValues;
    internal CompiledXmlSchemaSimpleType? MemberType;
    internal CompiledXmlSchemaFacet? FailedFacet;

    internal XmlSchemaSimpleTypeValidationResult()
    {
        Success = false;
        NormalizedValue = "";
        CanonicalValue = null;
        AtomicValue = null;
        AtomicValues = [];
        MemberType = null;
        FailedFacet = null;
    }
}
